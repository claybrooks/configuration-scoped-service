namespace ConfigurationScopedService;

public interface IConfigurationScopedServiceScopeFactory<TServiceType>
{
    Task<IConfigurationScopedServiceScope<TServiceType>> CreateAsync(CancellationToken cancellationToken);
    IConfigurationScopedServiceScope<TServiceType> Create(CancellationToken cancellationToken);
}