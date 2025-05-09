namespace MBL.ConfigurationScopedService.Internal;

internal sealed class ConfigurationScopeServiceAccessor<TServiceType> : IDisposable
{
    private readonly IConfigurationScopedServiceScope<TServiceType> _scopedServiceScope;

    public ConfigurationScopeServiceAccessor(IConfigurationScopedServiceScope<TServiceType> scopedServiceScope)
    {
        _scopedServiceScope = scopedServiceScope;
    }

    public TServiceType Service => _scopedServiceScope.Service;

    public void Dispose()
    {
        _scopedServiceScope.Dispose();
    }
}