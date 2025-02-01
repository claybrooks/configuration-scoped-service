using Microsoft.Extensions.DependencyInjection;

namespace ConfigurationScopedService.Internal;

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

internal sealed class ActivatorUtilitiesServiceFactory<TConfigType, TServiceType> : IServiceFactory<TConfigType, TServiceType> where TConfigType : class where TServiceType : class
{
    private readonly IServiceProvider _serviceProvider;

    public ActivatorUtilitiesServiceFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public TServiceType Create(TConfigType config)
    {
        return ActivatorUtilities.CreateInstance<TServiceType>(_serviceProvider);
    }
}
