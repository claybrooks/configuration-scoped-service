using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Nito.AsyncEx;
using Nito.Disposables;

namespace ConfigurationScopedService.Internal;

internal abstract class ConfigurationScopedServiceManager<TOptionsType, TServiceType> : IConfigurationScopedServiceScopeFactory<TServiceType>, IOptionsChangeConsumer<TOptionsType>, IAsyncDisposable 
    where TOptionsType : class
    where TServiceType : class
{
    private readonly string _optionsType;
    private readonly string? _optionsName;
    private readonly ConfigurationScopeRuntimeOptions _runtimeOptions;

    private readonly ILogger _logger;

    private readonly IServiceFactory<TOptionsType, TServiceType> _serviceFactory;

    private readonly CancellationTokenSource _cts = new();
    private readonly Task _backgroundTask;

    private readonly AsyncManualResetEvent _serviceAvailableEvent;

    private TOptionsType _currentOptions;
    private IReferenceCountedDisposable<DisposableServiceWrapper> _refCountedService;

    private readonly ConcurrentQueue<TOptionsType> _incomingOptionsChanges = [];
    private readonly ConcurrentQueue<TServiceType> _requiresDisposal = [];

    private readonly List<OldServiceInfo> _servicesWaitingForPhaseOut = [];

    protected ConfigurationScopedServiceManager(string? optionsName, ConfigurationScopeRuntimeOptions runtimeOptions, TOptionsType initialOptions, IServiceFactory<TOptionsType, TServiceType> serviceFactory, ILogger logger)
    {
        var options_type = typeof(TOptionsType);
        var equatable_type = typeof(IEquatable<TOptionsType>);

        _optionsType = options_type.Name;
        _optionsName = optionsName;
        _logger = logger;
        _serviceFactory = serviceFactory;
        _runtimeOptions = runtimeOptions;
        _currentOptions = initialOptions;

        if (!equatable_type.IsAssignableFrom(options_type))
        {
            _logger.LogWarning($"{options_type} does not implement IEquatable<{options_type.Name}>. It is highly recommended to implement IEquatable<{options_type.Name}> on {options_type.Name} to combat duplicate and unnecessary IOptionsMonitor change notifications.");
        }

        var service = serviceFactory.Create(_currentOptions);

        _refCountedService = DoCreateRefCountedServiceWrapper(service);

        // Start in a set state so that we can immediately serve the current service
        _serviceAvailableEvent = new AsyncManualResetEvent(true);

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

    public async Task<IConfigurationScopedServiceScope<TServiceType>> CreateAsync(CancellationToken cancellationToken)
    {
        await _serviceAvailableEvent.WaitAsync(cancellationToken);
        return new ConfigurationScopedServiceScope(_refCountedService.AddReference());
    }

    public IConfigurationScopedServiceScope<TServiceType> Create(CancellationToken cancellationToken)
    {
        _serviceAvailableEvent.Wait(cancellationToken);
        return new ConfigurationScopedServiceScope(_refCountedService.AddReference());
    }

    protected virtual async ValueTask DisposeCoreAsync()
    {
        try
        {
            _cts.Cancel();
        }
        catch
        {
            // Best effort cancellation
        }

        await _backgroundTask.ConfigureAwait(false);
        
        _cts.Dispose();

        try
        {
            _servicesWaitingForPhaseOut.Add(DoCreateOldServiceInfo(_refCountedService, _currentOptions));
            _refCountedService.Dispose();
        }
        catch (ObjectDisposedException)
        {
            // This is ok. If we are here after an app crash this may already be disposed.
        }

        // If we were already disposed above, the next few lines are no-ops
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
            TOptionsType? options = null;

            // If we are not blocking on swap, always flush the queue
            // If we are blocking on swap, only consume configuration events if our event is set
            // If we are in an unset state, it means we are waiting for all service scopes to end before swapping the
            // new service instance in.  Just let the configs stay queued until the service swap is complete
            if (!_runtimeOptions.BlockOnSwap || _serviceAvailableEvent.IsSet)
            {
                options = DoFlushConfigQueueAndReturnLast();
            }
            
            // Check to make sure the new options value is different before proceeding
            if (options is not null && !options.Equals(_currentOptions))
            {
                _logger.LogInformation($"Configuration change detected for options instance {_optionsName} of type {_optionsType}");
                // If we are blocking on swap, it means we need to wait for all service scopes to end before we can
                // swap in.  So we just decrement our internal ref counter and do the swap in the phase-out method
                if (_runtimeOptions.BlockOnSwap)
                {
                    _serviceAvailableEvent.Reset();
                    DoPhaseOutCurrentService(options);
                }
                // We are not blocking on swap, so we can just swap in the new service
                else
                {
                    DoNonBlockingServiceSwap(options);
                }
            }

            DoManageServicesWaitingForDispose();
            await DoDisposeOfServicesAsync().ConfigureAwait(false);

            // If we are blocking on swap and we are in an unset state, we need to create the new service when the old
            // is disposed of.  This occurs when our _refCountedService.IsDisposed flag is true
            if (_runtimeOptions.BlockOnSwap && !_serviceAvailableEvent.IsSet && _servicesWaitingForPhaseOut.Count == 0)
            {
                try
                {
                    DoSwapInNewService(_currentOptions);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating service instance");
                    throw;
                }
                finally
                {
                    _serviceAvailableEvent.Set();
                }
            }

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

    private void DoPhaseOutCurrentService(TOptionsType config)
    {
        _servicesWaitingForPhaseOut.Add(DoCreateOldServiceInfo(_refCountedService, _currentOptions));
        _refCountedService.Dispose();
        _currentOptions = config;
    }

    private void DoSwapInNewService(TOptionsType config)
    {
        var service = _serviceFactory.Create(config);
        _refCountedService = DoCreateRefCountedServiceWrapper(service);
    }

    private void DoNonBlockingServiceSwap(TOptionsType config)
    {
        var previous_ref_counted_service = _refCountedService;

        try
        {
            var service = _serviceFactory.Create(config);
            _refCountedService = DoCreateRefCountedServiceWrapper(service);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating service instance");
            throw;
        }
        finally
        {
            _servicesWaitingForPhaseOut.Add(DoCreateOldServiceInfo(previous_ref_counted_service, _currentOptions));
            previous_ref_counted_service.Dispose();
            _currentOptions = config;
        }
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