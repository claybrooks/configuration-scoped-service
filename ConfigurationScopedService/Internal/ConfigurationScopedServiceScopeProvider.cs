using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Nito.Disposables;

namespace ConfigurationScopedService.Internal;

internal abstract class ConfigurationScopedServiceScopeProvider<TConfigType, TServiceType> : IConfigurationScopedServiceScopeProvider<TServiceType>, IAsyncDisposable where TConfigType : class where TServiceType : class
{
    private readonly ConfigurationScopeRuntimeOptions _runtimeOptions;

    private readonly TimeProvider _timeProvider;
    private readonly ILogger _logger;

    private readonly IServiceFactory<TConfigType, TServiceType> _serviceFactory;

    private readonly CancellationTokenSource _cts = new();
    private readonly Task _backgroundTask;

    private TConfigType _currentConfig;
    private IReferenceCountedDisposable<DisposableServiceWrapper> _refCountedService;

    private readonly ConcurrentQueue<TConfigType> _incomingConfigChanges = [];
    private readonly ConcurrentQueue<TServiceType> _requiresDisposal = [];

    private readonly List<OldServiceInfo> _servicesWaitingForPhaseOut = [];

    protected ConfigurationScopedServiceScopeProvider(ConfigurationScopeRuntimeOptions runtimeOptions, TConfigType initialConfig, IServiceFactory<TConfigType, TServiceType> serviceFactory, ILogger logger)
    {
        _timeProvider = TimeProvider.System;
        _logger = logger;
        _serviceFactory = serviceFactory;
        _runtimeOptions = runtimeOptions;
        _currentConfig = initialConfig;

        // TODO Fix this
        var hot_reloadable = serviceFactory.Create(_currentConfig);
        _refCountedService = DoCreateRefCountedServiceWrapper(hot_reloadable);
        _backgroundTask = Task.Factory.StartNew(async () =>
        {
            try
            {
                await DoManageIncomingConfigsAsync(_cts.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
            }
        }, _cts.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
    }

    protected void ConsumeChange(TConfigType config)
    {
        _incomingConfigChanges.Enqueue(config);
    }

    public IConfigurationScopedServiceScope<TServiceType> CreateScope() => new NitoReferenceCountedDisposableConfigurationScopedServiceScopeWrapper(_refCountedService.AddReference());

    protected virtual async ValueTask DisposeCoreAsync()
    {
        await _cts.CancelAsync().ConfigureAwait(false);
        await _backgroundTask.ConfigureAwait(false);
        _cts.Dispose();

        _servicesWaitingForPhaseOut.Add(DoCreateOldServiceInfo(_refCountedService, _currentConfig));
        _refCountedService.Dispose();
        DoManageServicesWaitingForDispose();
        await DoDisposeOfServicesAsync().ConfigureAwait(false);

        if (_servicesWaitingForPhaseOut.Count > 0)
        {
            _logger.LogWarning("There are still services waiting for phase out. This is a bug.");
            if (_runtimeOptions.ThrowOnDispose)
            {
                throw new InvalidOperationException("There are still services waiting for phase out. This is a bug.");
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        await DisposeCoreAsync().ConfigureAwait(false);
        GC.SuppressFinalize(this);
    }

    private async Task DoManageIncomingConfigsAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var config = DoFlushConfigQueueAndReturnLast();

            if (config is not null)
            {
                var service = _serviceFactory.Create(config);
                DoServiceSwap(service, config);
            }

            DoManageServicesWaitingForDispose();
            await DoDisposeOfServicesAsync().ConfigureAwait(false);
            await Task.Delay(100, cancellationToken).ConfigureAwait(false);
        }
    }

    private TConfigType? DoFlushConfigQueueAndReturnLast()
    {
        TConfigType? config = null;

        while (_incomingConfigChanges.TryDequeue(out var next_config))
        {
            config = next_config;
        }

        return config;
    }

    private void DoServiceSwap(TServiceType service, TConfigType config)
    {
        var previous_ref_counted_service = _refCountedService;

        _refCountedService = DoCreateRefCountedServiceWrapper(service);

        _servicesWaitingForPhaseOut.Add(DoCreateOldServiceInfo(previous_ref_counted_service, _currentConfig));
        previous_ref_counted_service.Dispose();

        _currentConfig = config;
    }

    private async ValueTask DoDisposeOfServicesAsync()
    {
        while (_requiresDisposal.TryDequeue(out var service))
        {
            switch (service)
            {
                case IAsyncDisposable async_disposable:
                    await async_disposable.DisposeAsync().ConfigureAwait(false);
                    break;
                case IDisposable disposable:
                    disposable.Dispose();
                    break;
            }
        }
    }

    private void DoManageServicesWaitingForDispose()
    {
        for (var i = _servicesWaitingForPhaseOut.Count - 1; i >= 0; i--)
        {
            var next = _servicesWaitingForPhaseOut[i];
            if (next.Reference.TryGetTarget() is null)
            {
                _servicesWaitingForPhaseOut.RemoveAt(i);
            }
            else if (_runtimeOptions.WarnTime is not null && !next.Lingering && _timeProvider.GetUtcNow() - next.TimeSwapped > _runtimeOptions.WarnTime)
            {
                next.Lingering = true;
                _logger.LogWarning("Service has been waiting for phase out for longer than {WarnTime}. This is a bug.", _runtimeOptions.WarnTime);
            }
        }
    }

    private IReferenceCountedDisposable<DisposableServiceWrapper> DoCreateRefCountedServiceWrapper(TServiceType service)
    {
        return ReferenceCountedDisposable.Create(new DisposableServiceWrapper(service, _requiresDisposal.Enqueue));
    }

    private OldServiceInfo DoCreateOldServiceInfo(IReferenceCountedDisposable<DisposableServiceWrapper> reference, TConfigType config)
    {
        return new OldServiceInfo
        {
            Reference = reference.AddWeakReference(),
            TimeSwapped = _timeProvider.GetUtcNow(),
            Config = config
        };
    }

    internal class OldServiceInfo
    {
        public required DateTimeOffset TimeSwapped { get; init; }
        public required TConfigType Config { get; init; }
        public required IWeakReferenceCountedDisposable<DisposableServiceWrapper> Reference { get; init; }
        public bool Lingering { get; set; }
    }

    private sealed class NitoReferenceCountedDisposableConfigurationScopedServiceScopeWrapper : IConfigurationScopedServiceScope<TServiceType>
    {
        private readonly IReferenceCountedDisposable<DisposableServiceWrapper> _reference;

        public TServiceType Service
        {
            get
            {
                ArgumentNullException.ThrowIfNull(_reference.Target);
                return _reference.Target.Service;
            }
        }

        public NitoReferenceCountedDisposableConfigurationScopedServiceScopeWrapper(IReferenceCountedDisposable<DisposableServiceWrapper> reference)
        {
            _reference = reference;
        }

        public void Dispose()
        {
            _reference.Dispose();
        }
    }

    internal sealed class DisposableServiceWrapper : IDisposable
    {
        private readonly Action<TServiceType> _onDispose;

        public TServiceType Service { get; }

        public DisposableServiceWrapper(TServiceType service, Action<TServiceType> onDispose)
        {
            Service = service;
            _onDispose = onDispose;
        }

        public void Dispose()
        {
            _onDispose(Service);
        }
    }
}