using Microsoft.Extensions.DependencyInjection;

namespace MBL.ConfigurationScopedService.Internal;

internal static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        public bool IsRegistered<TServiceType>(ServiceLifetime? lifetime = null)
            where TServiceType : class
        {
            return services.Any(x => !x.IsKeyedService && x.ServiceType == typeof(TServiceType) && (lifetime is null || x.Lifetime == lifetime.Value));
        }

        public bool IsRegisteredKeyed<TServiceType>(object? serviceKey, ServiceLifetime? lifetime = null)
            where TServiceType : class
        {
            return services.Any(x => x.IsKeyedService && ((x.ServiceKey is not null && x.ServiceKey.Equals(serviceKey)) || (x.ServiceKey is null && serviceKey is null)) && x.ServiceType == typeof(TServiceType) && (lifetime is null || x.Lifetime == lifetime.Value));
        }

        public bool IsRegisteredSingleton<TServiceType>()
            where TServiceType : class
            => services.IsRegistered<TServiceType>(ServiceLifetime.Singleton);

        public bool IsRegisteredScoped<TServiceType>()
            where TServiceType : class
            => services.IsRegistered<TServiceType>(ServiceLifetime.Scoped);

        public bool IsRegisteredTransient<TServiceType>()
            where TServiceType : class
            => services.IsRegistered<TServiceType>(ServiceLifetime.Transient);

        public bool IsRegisteredKeyedSingleton<TServiceType>(object serviceKey, ServiceLifetime? lifetime = null)
            where TServiceType : class
            => services.IsRegisteredKeyed<TServiceType>(serviceKey, ServiceLifetime.Singleton);

        public bool IsRegisteredKeyedScoped<TServiceType>(object serviceKey, ServiceLifetime? lifetime = null)
            where TServiceType : class
            => services.IsRegisteredKeyed<TServiceType>(serviceKey, ServiceLifetime.Scoped);

        public bool IsRegisteredKeyedTransient<TServiceType>(object serviceKey, ServiceLifetime? lifetime = null)
            where TServiceType : class
            => services.IsRegisteredKeyed<TServiceType>(serviceKey, ServiceLifetime.Transient);
    }
}
