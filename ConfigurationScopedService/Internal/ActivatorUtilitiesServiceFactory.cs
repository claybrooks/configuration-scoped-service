using Microsoft.Extensions.DependencyInjection;

namespace ConfigurationScopedService.Internal;

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