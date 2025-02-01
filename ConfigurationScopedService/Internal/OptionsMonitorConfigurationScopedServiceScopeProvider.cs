using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ConfigurationScopedService.Internal;

internal sealed class OptionsMonitorConfigurationScopedServiceScopeProvider<TConfigType, TServiceType> : AbstractConfigurationScopedServiceScopeProvider<TConfigType, TServiceType> where TConfigType : class where TServiceType : class
{
    private readonly IDisposable? _changeDisposable;

    public OptionsMonitorConfigurationScopedServiceScopeProvider(
        string? optionsName,
        ConfigurationScopeRuntimeOptions runtimeOptions,
        IOptionsMonitor<TConfigType> optionsMonitor,
        IServiceFactory<TConfigType, TServiceType> serviceFactory,
        ILogger<OptionsMonitorConfigurationScopedServiceScopeProvider<TConfigType, TServiceType>> logger) : base(runtimeOptions, optionsName is null ? optionsMonitor.CurrentValue : optionsMonitor.Get(optionsName), serviceFactory, logger)
    {
        if (optionsName is null)
        {
            _changeDisposable = optionsMonitor.OnChange(AddConfigToQueue);
        }
        else
        {
            _changeDisposable = optionsMonitor.OnChange((o, name) =>
            {
                if (name == optionsName)
                {
                    AddConfigToQueue(o);
                }
            });
        }
    }

    protected override async ValueTask DisposeCoreAsync()
    {
        await base.DisposeCoreAsync().ConfigureAwait(false);
        _changeDisposable?.Dispose();
    }
}
