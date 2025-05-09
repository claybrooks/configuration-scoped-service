namespace MBL.ConfigurationScopedService.Internal;

internal sealed class DelegateKeyedServiceFactory<TConfigType, TServiceType> : IServiceFactory<TConfigType, TServiceType> where TConfigType : class where TServiceType : class
{
    private readonly IServiceProvider _serviceProvider;
    private readonly object? _key;
    private readonly Func<IServiceProvider, object?, TConfigType, TServiceType> _factory;

    public DelegateKeyedServiceFactory(IServiceProvider serviceProvider, object? key, Func<IServiceProvider, object?, TConfigType, TServiceType> factory)
    {
        _serviceProvider = serviceProvider;
        _key = key;
        _factory = factory;
    }

    public TServiceType Create(TConfigType config)
    {
        return _factory(_serviceProvider, _key, config);
    }
}