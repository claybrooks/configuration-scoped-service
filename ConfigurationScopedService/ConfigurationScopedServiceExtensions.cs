using ConfigurationScopedService.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ConfigurationScopedService;

public static class ConfigurationScopedServiceExtensions
{
    #region Non Keyed

    #region Default

    public static IServiceCollection AddConfigurationScoped<TOptionsType, TServiceType>(this IServiceCollection services, Func<IServiceProvider, TOptionsType, TServiceType> factory)
        where TOptionsType : class
        where TServiceType : class
        => services.AddConfigurationScoped<TOptionsType, TServiceType>(null, new ConfigurationScopeRuntimeOptions(), factory);

    public static IServiceCollection AddConfigurationScoped<TOptionsType, TServiceType, TImplementationType>(this IServiceCollection services, Func<IServiceProvider, TOptionsType, TImplementationType> factory)
        where TOptionsType : class
        where TServiceType : class
        where TImplementationType : class, TServiceType
        => services.AddConfigurationScoped<TOptionsType, TServiceType, TImplementationType>(null, new ConfigurationScopeRuntimeOptions(), factory);

    public static IServiceCollection AddConfigurationScoped<TOptionsType, TServiceType>(this IServiceCollection services)
        where TOptionsType : class
        where TServiceType : class
        => services.AddConfigurationScoped<TOptionsType, TServiceType>(null, new ConfigurationScopeRuntimeOptions());

    public static IServiceCollection AddConfigurationScoped<TOptionsType, TServiceType, TImplementationType>(this IServiceCollection services)
        where TOptionsType : class
        where TServiceType : class
        where TImplementationType : class, TServiceType
        => services.AddConfigurationScoped<TOptionsType, TServiceType, TImplementationType>(null, new ConfigurationScopeRuntimeOptions());

    #endregion

    #region Runtime Options

    public static IServiceCollection AddConfigurationScoped<TOptionsType, TServiceType>(this IServiceCollection services, ConfigurationScopeRuntimeOptions options, Func<IServiceProvider, TOptionsType, TServiceType> factory)
        where TOptionsType : class
        where TServiceType : class
        => services.AddConfigurationScoped<TOptionsType, TServiceType>(null, options, factory);

    public static IServiceCollection AddConfigurationScoped<TOptionsType, TServiceType, TImplementationType>(this IServiceCollection services, ConfigurationScopeRuntimeOptions options, Func<IServiceProvider, TOptionsType, TImplementationType> factory)
        where TOptionsType : class
        where TServiceType : class
        where TImplementationType : class, TServiceType
        => services.AddConfigurationScoped<TOptionsType, TServiceType, TImplementationType>(null, options, factory);

    public static IServiceCollection AddConfigurationScoped<TOptionsType, TServiceType>(this IServiceCollection services, ConfigurationScopeRuntimeOptions options)
        where TOptionsType : class
        where TServiceType : class
        => services.AddConfigurationScoped<TOptionsType, TServiceType>(null, options);

    public static IServiceCollection AddConfigurationScoped<TOptionsType, TServiceType, TImplementationType>(this IServiceCollection services, ConfigurationScopeRuntimeOptions options)
        where TOptionsType : class
        where TServiceType : class
        where TImplementationType : class, TServiceType
        => services.AddConfigurationScoped<TOptionsType, TServiceType, TImplementationType>(null, options);

    #endregion

    #region Options Name

    public static IServiceCollection AddConfigurationScoped<TOptionsType, TServiceType>(this IServiceCollection services, string? optionsName, Func<IServiceProvider, TOptionsType, TServiceType> factory)
        where TOptionsType : class
        where TServiceType : class
        => services.AddConfigurationScoped<TOptionsType, TServiceType>(optionsName, new ConfigurationScopeRuntimeOptions(), factory);

    public static IServiceCollection AddConfigurationScoped<TOptionsType, TServiceType, TImplementationType>(this IServiceCollection services, string? optionsName, Func<IServiceProvider, TOptionsType, TImplementationType> factory)
        where TOptionsType : class
        where TServiceType : class
        where TImplementationType : class, TServiceType
        => services.AddConfigurationScoped<TOptionsType, TServiceType, TImplementationType>(optionsName, new ConfigurationScopeRuntimeOptions(), factory);

    public static IServiceCollection AddConfigurationScoped<TOptionsType, TServiceType>(this IServiceCollection services, string? optionsName)
        where TOptionsType : class
        where TServiceType : class
        => services.AddConfigurationScoped<TOptionsType, TServiceType>(optionsName, new ConfigurationScopeRuntimeOptions());

    public static IServiceCollection AddConfigurationScoped<TOptionsType, TServiceType, TImplementationType>(this IServiceCollection services, string? optionsName)
        where TOptionsType : class
        where TServiceType : class
        where TImplementationType : class, TServiceType
        => services.AddConfigurationScoped<TOptionsType, TServiceType, TImplementationType>(optionsName, new ConfigurationScopeRuntimeOptions());

    #endregion

    #region All

    public static IServiceCollection AddConfigurationScoped<TOptionsType, TServiceType>(this IServiceCollection services, string? optionsName, ConfigurationScopeRuntimeOptions options, Func<IServiceProvider, TOptionsType, TServiceType> factory)
        where TOptionsType : class
        where TServiceType : class
    {
        return services.DoAddConfigurationScoped<TOptionsType, TServiceType>(optionsName, options, sp => new DelegateServiceFactory<TOptionsType, TServiceType>(sp, factory));
    }

    public static IServiceCollection AddConfigurationScoped<TOptionsType, TServiceType, TImplementationType>(this IServiceCollection services, string? optionsName, ConfigurationScopeRuntimeOptions options, Func<IServiceProvider, TOptionsType, TImplementationType> factory)
        where TOptionsType : class
        where TServiceType : class
        where TImplementationType : class, TServiceType
    {
        return services.DoAddConfigurationScoped<TOptionsType, TServiceType>(optionsName, options, sp => new DelegateServiceFactory<TOptionsType, TImplementationType>(sp, factory));
    }

    public static IServiceCollection AddConfigurationScoped<TOptionsType, TServiceType>(this IServiceCollection services, string? optionsName, ConfigurationScopeRuntimeOptions options)
        where TOptionsType : class
        where TServiceType : class
    {
        return services.DoAddConfigurationScoped<TOptionsType, TServiceType>(optionsName, options, sp => new ActivatorUtilitiesServiceFactory<TOptionsType, TServiceType>(sp));
    }

    public static IServiceCollection AddConfigurationScoped<TOptionsType, TServiceType, TImplementationType>(this IServiceCollection services, string? optionsName, ConfigurationScopeRuntimeOptions options)
        where TOptionsType : class
        where TServiceType : class
        where TImplementationType : class, TServiceType
    {
        return services.DoAddConfigurationScoped<TOptionsType, TServiceType>(optionsName, options, sp => new ActivatorUtilitiesServiceFactory<TOptionsType, TImplementationType>(sp));
    }

    #endregion

    #endregion

    #region Keyed

    #region Default

    public static IServiceCollection AddKeyedConfigurationScoped<TOptionsType, TServiceType>(this IServiceCollection services, object? serviceKey, Func<IServiceProvider, object?, TOptionsType, TServiceType> factory)
        where TOptionsType : class
        where TServiceType : class
        => services.AddKeyedConfigurationScoped<TOptionsType, TServiceType>(serviceKey, null, new ConfigurationScopeRuntimeOptions(), factory);

    public static IServiceCollection AddKeyedConfigurationScoped<TOptionsType, TServiceType, TImplementationType>(this IServiceCollection services, object? serviceKey, Func<IServiceProvider, object?, TOptionsType, TImplementationType> factory)
        where TOptionsType : class
        where TServiceType : class
        where TImplementationType : class, TServiceType
        => services.AddKeyedConfigurationScoped<TOptionsType, TServiceType, TImplementationType>(serviceKey, null, new ConfigurationScopeRuntimeOptions(), factory);
    
    public static IServiceCollection AddKeyedConfigurationScoped<TOptionsType, TServiceType>(this IServiceCollection services, object? serviceKey)
        where TOptionsType : class
        where TServiceType : class
        => services.AddKeyedConfigurationScoped<TOptionsType, TServiceType>(serviceKey, null, new ConfigurationScopeRuntimeOptions());

    public static IServiceCollection AddKeyedConfigurationScoped<TOptionsType, TServiceType, TImplementationType>(this IServiceCollection services, object? serviceKey)
        where TOptionsType : class
        where TServiceType : class
        where TImplementationType : class, TServiceType
        => services.AddKeyedConfigurationScoped<TOptionsType, TServiceType, TImplementationType>(serviceKey, null, new ConfigurationScopeRuntimeOptions());

    #endregion

    #region Runtime Options

    public static IServiceCollection AddKeyedConfigurationScoped<TOptionsType, TServiceType>(this IServiceCollection services, object? serviceKey, ConfigurationScopeRuntimeOptions options, Func<IServiceProvider, object?, TOptionsType, TServiceType> factory)
        where TOptionsType : class
        where TServiceType : class
        => services.AddKeyedConfigurationScoped<TOptionsType, TServiceType>(serviceKey, null, options, factory);

    public static IServiceCollection AddKeyedConfigurationScoped<TOptionsType, TServiceType, TImplementationType>(this IServiceCollection services, object? serviceKey, ConfigurationScopeRuntimeOptions options, Func<IServiceProvider, object?, TOptionsType, TImplementationType> factory)
        where TOptionsType : class
        where TServiceType : class
        where TImplementationType : class, TServiceType
        => services.AddKeyedConfigurationScoped<TOptionsType, TServiceType, TImplementationType>(serviceKey, null, options, factory);

    public static IServiceCollection AddKeyedConfigurationScoped<TOptionsType, TServiceType>(this IServiceCollection services, object? serviceKey, ConfigurationScopeRuntimeOptions options)
        where TOptionsType : class
        where TServiceType : class
        => services.AddKeyedConfigurationScoped<TOptionsType, TServiceType>(serviceKey, null, options);

    public static IServiceCollection AddKeyedConfigurationScoped<TOptionsType, TServiceType, TImplementationType>(this IServiceCollection services, object? serviceKey, ConfigurationScopeRuntimeOptions options)
        where TOptionsType : class
        where TServiceType : class
        where TImplementationType : class, TServiceType
        => services.AddKeyedConfigurationScoped<TOptionsType, TServiceType, TImplementationType>(serviceKey, null, options);

    #endregion

    #region Options Name

    public static IServiceCollection AddKeyedConfigurationScoped<TOptionsType, TServiceType>(this IServiceCollection services, object? serviceKey, string? optionsName, Func<IServiceProvider, object?, TOptionsType, TServiceType> factory)
        where TOptionsType : class
        where TServiceType : class
        => services.AddKeyedConfigurationScoped<TOptionsType, TServiceType>(serviceKey, optionsName, new ConfigurationScopeRuntimeOptions(), factory);

    public static IServiceCollection AddKeyedConfigurationScoped<TOptionsType, TServiceType, TImplementationType>(this IServiceCollection services, object? serviceKey, string? optionsName, Func<IServiceProvider, object?, TOptionsType, TImplementationType> factory)
        where TOptionsType : class
        where TServiceType : class
        where TImplementationType : class, TServiceType
        => services.AddKeyedConfigurationScoped<TOptionsType, TServiceType, TImplementationType>(serviceKey, optionsName, new ConfigurationScopeRuntimeOptions(), factory);

    public static IServiceCollection AddKeyedConfigurationScoped<TOptionsType, TServiceType>(this IServiceCollection services, object? serviceKey, string? optionsName)
        where TOptionsType : class
        where TServiceType : class
        => services.AddKeyedConfigurationScoped<TOptionsType, TServiceType>(serviceKey, optionsName, new ConfigurationScopeRuntimeOptions());

    public static IServiceCollection AddKeyedConfigurationScoped<TOptionsType, TServiceType, TImplementationType>(this IServiceCollection services, object? serviceKey, string? optionsName)
        where TOptionsType : class
        where TServiceType : class
        where TImplementationType : class, TServiceType
        => services.AddKeyedConfigurationScoped<TOptionsType, TServiceType, TImplementationType>(serviceKey, optionsName, new ConfigurationScopeRuntimeOptions());

    #endregion

    #region All

    public static IServiceCollection AddKeyedConfigurationScoped<TOptionsType, TServiceType>(this IServiceCollection services, object? serviceKey, string? optionsName, ConfigurationScopeRuntimeOptions options, Func<IServiceProvider, object?, TOptionsType, TServiceType> factory)
        where TOptionsType : class
        where TServiceType : class
    {
        return services.DoAddKeyedConfigurationScoped<TOptionsType, TServiceType>(serviceKey, optionsName, options, (sp, k) => new DelegateKeyedServiceFactory<TOptionsType, TServiceType>(sp, k, factory));
    }

    public static IServiceCollection AddKeyedConfigurationScoped<TOptionsType, TServiceType, TImplementationType>(this IServiceCollection services, object? serviceKey, string? optionsName, ConfigurationScopeRuntimeOptions options, Func<IServiceProvider, object?, TOptionsType, TImplementationType> factory)
        where TOptionsType : class
        where TServiceType : class
        where TImplementationType : class, TServiceType
    {
        return services.DoAddKeyedConfigurationScoped<TOptionsType, TServiceType>(serviceKey, optionsName, options, (sp, k) => new DelegateKeyedServiceFactory<TOptionsType, TImplementationType>(sp, k, factory));
    }

    public static IServiceCollection AddKeyedConfigurationScoped<TOptionsType, TServiceType>(this IServiceCollection services, object? serviceKey, string? optionsName, ConfigurationScopeRuntimeOptions options)
        where TOptionsType : class
        where TServiceType : class
    {
        return services.DoAddKeyedConfigurationScoped<TOptionsType, TServiceType>(serviceKey, optionsName, options, (sp, _) => new ActivatorUtilitiesServiceFactory<TOptionsType, TServiceType>(sp));
    }

    public static IServiceCollection AddKeyedConfigurationScoped<TOptionsType, TServiceType, TImplementationType>(this IServiceCollection services, object? serviceKey, string? optionsName, ConfigurationScopeRuntimeOptions options)
        where TOptionsType : class
        where TServiceType : class
        where TImplementationType : class, TServiceType
    {
        return services.DoAddKeyedConfigurationScoped<TOptionsType, TServiceType>(serviceKey, optionsName, options, (sp, _) => new ActivatorUtilitiesServiceFactory<TOptionsType, TImplementationType>(sp));
    }

    #endregion

    #endregion

    private static IServiceCollection DoAddConfigurationScoped<TOptionsType, TServiceType>(this IServiceCollection services, string? optionsName, ConfigurationScopeRuntimeOptions runtimeOptions, Func<IServiceProvider, IServiceFactory<TOptionsType, TServiceType>> serviceFactoryFactory) 
        where TOptionsType : class 
        where TServiceType : class
    {
        services.TryAddSingleton(serviceFactoryFactory);

        // Allows callers to resolve a scope provider for TServiceType, which can be used anywhere (request handlers, background workers, etc...) 
        services.TryAddSingleton<IConfigurationScopedServiceScopeProvider<TServiceType>>(sp => new OptionsMonitorConfigurationScopedServiceScopeProvider<TOptionsType, TServiceType>(
            optionsName,
            runtimeOptions,
            sp.GetRequiredService<IOptionsMonitor<TOptionsType>>(),
            sp.GetRequiredService<IServiceFactory<TOptionsType, TServiceType>>(),
            sp.GetRequiredService<ILogger<OptionsMonitorConfigurationScopedServiceScopeProvider<TOptionsType, TServiceType>>>()));

        // This layer of indirection is to allow the refcount decrement to happen on scope end.  The type is internal, so users can't resolve it without doing nasty things.  The resolution used by 
        // callers will be of TServiceType
        services.TryAddScoped(sp =>
        {
            var config_scoped_service = sp.GetRequiredService<IConfigurationScopedServiceScopeProvider<TServiceType>>();
            return new ConfigurationScopeServiceAccessor<TServiceType>(config_scoped_service.CreateScope());
        });

        // Allows callers to resolve TServiceType tied directly to the request scope
        services.TryAddScoped(sp => sp.GetRequiredService<ConfigurationScopeServiceAccessor<TServiceType>>().Service);
        return services;
    }

    private static IServiceCollection DoAddKeyedConfigurationScoped<TOptionsType, TServiceType>(this IServiceCollection services, object? serviceKey, string? optionsName, ConfigurationScopeRuntimeOptions runtimeOptions, Func<IServiceProvider, object?, IServiceFactory<TOptionsType, TServiceType>> serviceFactoryFactory) 
        where TOptionsType : class
        where TServiceType : class
    {
        services.TryAddKeyedSingleton(serviceKey, serviceFactoryFactory);

        // Allows callers to resolve a scope provider for TServiceType, which can be used anywhere (request handlers, background workers, etc...) 
        services.TryAddKeyedSingleton<IConfigurationScopedServiceScopeProvider<TServiceType>>(serviceKey, (sp, k) => new OptionsMonitorConfigurationScopedServiceScopeProvider<TOptionsType, TServiceType>(
            optionsName,
            runtimeOptions,
            sp.GetRequiredService<IOptionsMonitor<TOptionsType>>(),
            sp.GetRequiredKeyedService<IServiceFactory<TOptionsType, TServiceType>>(k),
            sp.GetRequiredService<ILogger<OptionsMonitorConfigurationScopedServiceScopeProvider<TOptionsType, TServiceType>>>()));

        // This layer of indirection is to allow the refcount decrement to happen on scope end.  The type is internal, so users can't resolve it without doing nasty things.  The resolution used by 
        // callers will be of TServiceType
        services.TryAddKeyedScoped(serviceKey, (sp, k) =>
        {
            var config_scoped_service = sp.GetRequiredKeyedService<IConfigurationScopedServiceScopeProvider<TServiceType>>(k);
            return new ConfigurationScopeServiceAccessor<TServiceType>(config_scoped_service.CreateScope());
        });

        // Allows callers to resolve TServiceType tied directly to the request scope
        services.TryAddKeyedScoped(serviceKey, (sp, k) => sp.GetRequiredKeyedService<ConfigurationScopeServiceAccessor<TServiceType>>(k).Service);

        return services;
    }
}