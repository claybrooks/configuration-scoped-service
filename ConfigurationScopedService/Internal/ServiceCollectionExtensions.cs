using Microsoft.Extensions.DependencyInjection;

namespace MBL.ConfigurationScopedService.Internal;

internal static class ServiceCollectionExtensions
{
    public static bool IsRegistered<TServiceType>(this IServiceCollection services, ServiceLifetime? lifetime = null)
        where TServiceType : class
    {
        return services.Any(x => !x.IsKeyedService && x.ServiceType == typeof(TServiceType) && (lifetime is null || x.Lifetime == lifetime.Value));
    }

    public static bool IsRegisteredKeyed<TServiceType>(this IServiceCollection services, object? serviceKey, ServiceLifetime? lifetime = null)
        where TServiceType : class
    {
        return services.Any(x => x.IsKeyedService && ((x.ServiceKey is not null && x.ServiceKey.Equals(serviceKey)) || (x.ServiceKey is null && serviceKey is null)) && x.ServiceType == typeof(TServiceType) && (lifetime is null || x.Lifetime == lifetime.Value));
    }

    public static bool IsRegisteredSingleton<TServiceType>(this IServiceCollection services)
        where TServiceType : class
        => services.IsRegistered<TServiceType>(ServiceLifetime.Singleton);

    public static bool IsRegisteredScoped<TServiceType>(this IServiceCollection services)
        where TServiceType : class
        => services.IsRegistered<TServiceType>(ServiceLifetime.Scoped);

    public static bool IsRegisteredTransient<TServiceType>(this IServiceCollection services)
        where TServiceType : class
        => services.IsRegistered<TServiceType>(ServiceLifetime.Transient);

    public static bool IsRegisteredKeyedSingleton<TServiceType>(this IServiceCollection services, object serviceKey, ServiceLifetime? lifetime = null)
        where TServiceType : class
        => services.IsRegisteredKeyed<TServiceType>(serviceKey, ServiceLifetime.Singleton);

    public static bool IsRegisteredKeyedScoped<TServiceType>(this IServiceCollection services, object serviceKey, ServiceLifetime? lifetime = null)
        where TServiceType : class
        => services.IsRegisteredKeyed<TServiceType>(serviceKey, ServiceLifetime.Scoped);

    public static bool IsRegisteredKeyedTransient<TServiceType>(this IServiceCollection services, object serviceKey, ServiceLifetime? lifetime = null)
        where TServiceType : class
        => services.IsRegisteredKeyed<TServiceType>(serviceKey, ServiceLifetime.Transient);
}
