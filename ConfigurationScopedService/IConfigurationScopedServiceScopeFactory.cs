namespace ConfigurationScopedService;

public interface IConfigurationScopedServiceScopeFactory<out TServiceType>
{
    IConfigurationScopedServiceScope<TServiceType> Create();
}