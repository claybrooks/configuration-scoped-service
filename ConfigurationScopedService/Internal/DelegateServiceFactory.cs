namespace MBL.ConfigurationScopedService.Internal;

internal sealed class DelegateServiceFactory<TConfigType, TServiceType> : IServiceFactory<TConfigType, TServiceType> where TConfigType : class where TServiceType : class
{
    private readonly IServiceProvider _serviceProvider;
    private readonly Func<IServiceProvider, TConfigType, TServiceType> _factory;

    public DelegateServiceFactory(IServiceProvider serviceProvider, Func<IServiceProvider, TConfigType, TServiceType> factory)
    {
        _serviceProvider = serviceProvider;
        _factory = factory;
    }

    public TServiceType Create(TConfigType config)
    {
        return _factory(_serviceProvider, config);
    }
}