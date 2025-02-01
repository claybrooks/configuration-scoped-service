namespace ConfigurationScopedService;

public interface IConfigurationScopedServiceScopeProvider<out TServiceType>
{
    IConfigurationScopedServiceScope<TServiceType> CreateScope();
}