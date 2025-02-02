using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Nito.Disposables;

namespace ConfigurationScopedService.Internal;

internal abstract class ConfigurationScopedServiceManager<TOptionsType, TServiceType> : IConfigurationScopedServiceScopeFactory<TServiceType>, IOptionsChangeConsumer<TOptionsType>, IAsyncDisposable 
    where TOptionsType : class
    where TServiceType : class
{
    private readonly ConfigurationScopeRuntimeOptions _runtimeOptions;

    private readonly ILogger _logger;

    private readonly IServiceFactory<TOptionsType, TServiceType> _serviceFactory;

    private readonly CancellationTokenSource _cts = new();
    private readonly Task _backgroundTask;

    private TOptionsType _currentOptions;
    private IReferenceCountedDisposable<DisposableServiceWrapper> _refCountedService;

    private readonly ConcurrentQueue<TOptionsType> _incomingOptionsChanges = [];
    private readonly ConcurrentQueue<TServiceType> _requiresDisposal = [];

    private readonly List<OldServiceInfo> _servicesWaitingForPhaseOut = [];

    protected ConfigurationScopedServiceManager(ConfigurationScopeRuntimeOptions runtimeOptions, TOptionsType initialOptions, IServiceFactory<TOptionsType, TServiceType> serviceFactory, ILogger logger)
    {
        _logger = logger;
        _serviceFactory = serviceFactory;
        _runtimeOptions = runtimeOptions;
        _currentOptions = initialOptions;

        var service = serviceFactory.Create(_currentOptions);

        _refCountedService = DoCreateRefCountedServiceWrapper(service);

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

    public void ConsumeChange(TOptionsType options)
    {
        _incomingOptionsChanges.Enqueue(options);
    }

    public IConfigurationScopedServiceScope<TServiceType> Create() => new ConfigurationScopedServiceScope(_refCountedService.AddReference());

    protected virtual async ValueTask DisposeCoreAsync()
    {
        _cts.Cancel();
        await _backgroundTask.ConfigureAwait(false);
        _cts.Dispose();

        _servicesWaitingForPhaseOut.Add(DoCreateOldServiceInfo(_refCountedService, _currentOptions));
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

    private TOptionsType? DoFlushConfigQueueAndReturnLast()
    {
        TOptionsType? config = null;

        while (_incomingOptionsChanges.TryDequeue(out var next_config))
        {
            config = next_config;
        }

        return config;
    }

    private void DoServiceSwap(TServiceType service, TOptionsType config)
    {
        var previous_ref_counted_service = _refCountedService;

        _refCountedService = DoCreateRefCountedServiceWrapper(service);

        _servicesWaitingForPhaseOut.Add(DoCreateOldServiceInfo(previous_ref_counted_service, _currentOptions));
        previous_ref_counted_service.Dispose();

        _currentOptions = config;
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
            else if (_runtimeOptions.WarnTime is not null && !next.Lingering && DateTimeOffset.UtcNow - next.TimeSwapped > _runtimeOptions.WarnTime)
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

    private OldServiceInfo DoCreateOldServiceInfo(IReferenceCountedDisposable<DisposableServiceWrapper> reference, TOptionsType config)
    {
        return new OldServiceInfo(reference: reference.AddWeakReference(), timeSwapped: DateTimeOffset.UtcNow, config: config);
    }

    internal class OldServiceInfo
    {
        public OldServiceInfo(IWeakReferenceCountedDisposable<DisposableServiceWrapper> reference, DateTimeOffset timeSwapped, TOptionsType config)
        {
            Reference = reference;
            TimeSwapped = timeSwapped;
            Config = config;
        }

        public DateTimeOffset TimeSwapped { get; }
        public TOptionsType Config { get; }
        public IWeakReferenceCountedDisposable<DisposableServiceWrapper> Reference { get; }
        public bool Lingering { get; set; }
    }

    private sealed class ConfigurationScopedServiceScope : IConfigurationScopedServiceScope<TServiceType>
    {
        private readonly IReferenceCountedDisposable<DisposableServiceWrapper> _reference;

        public TServiceType Service
        {
            get
            {
                if (_reference.Target is null)
                {
                    throw new ArgumentNullException(nameof(_reference.Target), "This should never happen. Report this as a bug!");
                }

                return _reference.Target.Service;
            }
        }

        public ConfigurationScopedServiceScope(IReferenceCountedDisposable<DisposableServiceWrapper> reference)
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