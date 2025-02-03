using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ConfigurationScopedService.Internal;

internal sealed class OptionsMonitorConfigurationScopedServiceManager<TOptions, TServiceType> : ConfigurationScopedServiceManager<TOptions, TServiceType> where TOptions : class where TServiceType : class
{
    private readonly IDisposable? _changeDisposable;

    public OptionsMonitorConfigurationScopedServiceManager(
        string? optionsName,
        ConfigurationScopeRuntimeOptions runtimeOptions,
        IOptionsMonitor<TOptions> optionsMonitor,
        IServiceFactory<TOptions, TServiceType> serviceFactory,
        ILogger<OptionsMonitorConfigurationScopedServiceManager<TOptions, TServiceType>> logger) : base(optionsName, runtimeOptions, optionsName is null ? optionsMonitor.CurrentValue : optionsMonitor.Get(optionsName), serviceFactory, logger)
    {
        if (optionsName is null)
        {
            _changeDisposable = optionsMonitor.OnChange(ConsumeChange);
        }
        else
        {
            _changeDisposable = optionsMonitor.OnChange((o, name) =>
            {
                if (name == optionsName)
                {
                    ConsumeChange(o);
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
