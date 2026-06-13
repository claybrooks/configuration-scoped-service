using MBL.ConfigurationScopedService.Internal;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MBL.ConfigurationScopedService;

public static class ConfigurationScopedServiceExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddConfigurationScoped<TOptionsType, TServiceType>(Func<IServiceProvider, TOptionsType, TServiceType> factory)
            where TOptionsType : class
            where TServiceType : class
            => services.AddConfigurationScoped(null, new ConfigurationScopeRuntimeOptions(), factory);

        public IServiceCollection AddConfigurationScoped<TOptionsType, TServiceType, TImplementationType>(Func<IServiceProvider, TOptionsType, TImplementationType> factory)
            where TOptionsType : class
            where TServiceType : class
            where TImplementationType : class, TServiceType
            => services.AddConfigurationScoped<TOptionsType, TServiceType, TImplementationType>(null, new ConfigurationScopeRuntimeOptions(), factory);

        public IServiceCollection AddConfigurationScoped<TOptionsType, TServiceType>()
            where TOptionsType : class
            where TServiceType : class
            => services.AddConfigurationScoped<TOptionsType, TServiceType>(null, new ConfigurationScopeRuntimeOptions());

        public IServiceCollection AddConfigurationScoped<TOptionsType, TServiceType, TImplementationType>()
            where TOptionsType : class
            where TServiceType : class
            where TImplementationType : class, TServiceType
            => services.AddConfigurationScoped<TOptionsType, TServiceType, TImplementationType>(null, new ConfigurationScopeRuntimeOptions());

        public IServiceCollection AddConfigurationScoped<TOptionsType, TServiceType>(ConfigurationScopeRuntimeOptions options, Func<IServiceProvider, TOptionsType, TServiceType> factory)
            where TOptionsType : class
            where TServiceType : class
            => services.AddConfigurationScoped(null, options, factory);

        public IServiceCollection AddConfigurationScoped<TOptionsType, TServiceType, TImplementationType>(ConfigurationScopeRuntimeOptions options, Func<IServiceProvider, TOptionsType, TImplementationType> factory)
            where TOptionsType : class
            where TServiceType : class
            where TImplementationType : class, TServiceType
            => services.AddConfigurationScoped<TOptionsType, TServiceType, TImplementationType>(null, options, factory);

        public IServiceCollection AddConfigurationScoped<TOptionsType, TServiceType>(ConfigurationScopeRuntimeOptions options)
            where TOptionsType : class
            where TServiceType : class
            => services.AddConfigurationScoped<TOptionsType, TServiceType>(null, options);

        public IServiceCollection AddConfigurationScoped<TOptionsType, TServiceType, TImplementationType>(ConfigurationScopeRuntimeOptions options)
            where TOptionsType : class
            where TServiceType : class
            where TImplementationType : class, TServiceType
            => services.AddConfigurationScoped<TOptionsType, TServiceType, TImplementationType>(null, options);

        public IServiceCollection AddConfigurationScoped<TOptionsType, TServiceType>(string? optionsName, Func<IServiceProvider, TOptionsType, TServiceType> factory)
            where TOptionsType : class
            where TServiceType : class
            => services.AddConfigurationScoped(optionsName, new ConfigurationScopeRuntimeOptions(), factory);

        public IServiceCollection AddConfigurationScoped<TOptionsType, TServiceType, TImplementationType>(string? optionsName, Func<IServiceProvider, TOptionsType, TImplementationType> factory)
            where TOptionsType : class
            where TServiceType : class
            where TImplementationType : class, TServiceType
            => services.AddConfigurationScoped<TOptionsType, TServiceType, TImplementationType>(optionsName, new ConfigurationScopeRuntimeOptions(), factory);

        public IServiceCollection AddConfigurationScoped<TOptionsType, TServiceType>(string? optionsName)
            where TOptionsType : class
            where TServiceType : class
            => services.AddConfigurationScoped<TOptionsType, TServiceType>(optionsName, new ConfigurationScopeRuntimeOptions());

        public IServiceCollection AddConfigurationScoped<TOptionsType, TServiceType, TImplementationType>(string? optionsName)
            where TOptionsType : class
            where TServiceType : class
            where TImplementationType : class, TServiceType
            => services.AddConfigurationScoped<TOptionsType, TServiceType, TImplementationType>(optionsName, new ConfigurationScopeRuntimeOptions());

        public IServiceCollection AddConfigurationScoped<TOptionsType, TServiceType>(string? optionsName, ConfigurationScopeRuntimeOptions options, Func<IServiceProvider, TOptionsType, TServiceType> factory)
            where TOptionsType : class
            where TServiceType : class
        {
            return services.DoAddConfigurationScoped(optionsName, options, sp => new DelegateServiceFactory<TOptionsType, TServiceType>(sp, factory));
        }

        public IServiceCollection AddConfigurationScoped<TOptionsType, TServiceType, TImplementationType>(string? optionsName, ConfigurationScopeRuntimeOptions options, Func<IServiceProvider, TOptionsType, TImplementationType> factory)
            where TOptionsType : class
            where TServiceType : class
            where TImplementationType : class, TServiceType
        {
            return services.DoAddConfigurationScoped<TOptionsType, TServiceType>(optionsName, options, sp => new DelegateServiceFactory<TOptionsType, TImplementationType>(sp, factory));
        }

        public IServiceCollection AddConfigurationScoped<TOptionsType, TServiceType>(string? optionsName, ConfigurationScopeRuntimeOptions options)
            where TOptionsType : class
            where TServiceType : class
        {
            return services.DoAddConfigurationScoped(optionsName, options, sp => new ActivatorUtilitiesServiceFactory<TOptionsType, TServiceType>(sp));
        }

        public IServiceCollection AddConfigurationScoped<TOptionsType, TServiceType, TImplementationType>(string? optionsName, ConfigurationScopeRuntimeOptions options)
            where TOptionsType : class
            where TServiceType : class
            where TImplementationType : class, TServiceType
        {
            return services.DoAddConfigurationScoped<TOptionsType, TServiceType>(optionsName, options, sp => new ActivatorUtilitiesServiceFactory<TOptionsType, TImplementationType>(sp));
        }
    
        public IServiceCollection AddKeyedConfigurationScoped<TOptionsType, TServiceType>(object? serviceKey, Func<IServiceProvider, object?, TOptionsType, TServiceType> factory)
            where TOptionsType : class
            where TServiceType : class
            => services.AddKeyedConfigurationScoped(serviceKey, null, new ConfigurationScopeRuntimeOptions(), factory);

        public IServiceCollection AddKeyedConfigurationScoped<TOptionsType, TServiceType, TImplementationType>(object? serviceKey, Func<IServiceProvider, object?, TOptionsType, TImplementationType> factory)
            where TOptionsType : class
            where TServiceType : class
            where TImplementationType : class, TServiceType
            => services.AddKeyedConfigurationScoped<TOptionsType, TServiceType, TImplementationType>(serviceKey, null, new ConfigurationScopeRuntimeOptions(), factory);

        public IServiceCollection AddKeyedConfigurationScoped<TOptionsType, TServiceType>(object? serviceKey)
            where TOptionsType : class
            where TServiceType : class
            => services.AddKeyedConfigurationScoped<TOptionsType, TServiceType>(serviceKey, null, new ConfigurationScopeRuntimeOptions());

        public IServiceCollection AddKeyedConfigurationScoped<TOptionsType, TServiceType, TImplementationType>(object? serviceKey)
            where TOptionsType : class
            where TServiceType : class
            where TImplementationType : class, TServiceType
            => services.AddKeyedConfigurationScoped<TOptionsType, TServiceType, TImplementationType>(serviceKey, null, new ConfigurationScopeRuntimeOptions());

        public IServiceCollection AddKeyedConfigurationScoped<TOptionsType, TServiceType>(object? serviceKey, ConfigurationScopeRuntimeOptions options, Func<IServiceProvider, object?, TOptionsType, TServiceType> factory)
            where TOptionsType : class
            where TServiceType : class
            => services.AddKeyedConfigurationScoped(serviceKey, null, options, factory);

        public IServiceCollection AddKeyedConfigurationScoped<TOptionsType, TServiceType, TImplementationType>(object? serviceKey, ConfigurationScopeRuntimeOptions options, Func<IServiceProvider, object?, TOptionsType, TImplementationType> factory)
            where TOptionsType : class
            where TServiceType : class
            where TImplementationType : class, TServiceType
            => services.AddKeyedConfigurationScoped<TOptionsType, TServiceType, TImplementationType>(serviceKey, null, options, factory);

        public IServiceCollection AddKeyedConfigurationScoped<TOptionsType, TServiceType>(object? serviceKey, ConfigurationScopeRuntimeOptions options)
            where TOptionsType : class
            where TServiceType : class
            => services.AddKeyedConfigurationScoped<TOptionsType, TServiceType>(serviceKey, null, options);

        public IServiceCollection AddKeyedConfigurationScoped<TOptionsType, TServiceType, TImplementationType>(object? serviceKey, ConfigurationScopeRuntimeOptions options)
            where TOptionsType : class
            where TServiceType : class
            where TImplementationType : class, TServiceType
            => services.AddKeyedConfigurationScoped<TOptionsType, TServiceType, TImplementationType>(serviceKey, null, options);

        public IServiceCollection AddKeyedConfigurationScoped<TOptionsType, TServiceType>(object? serviceKey, string? optionsName, Func<IServiceProvider, object?, TOptionsType, TServiceType> factory)
            where TOptionsType : class
            where TServiceType : class
            => services.AddKeyedConfigurationScoped(serviceKey, optionsName, new ConfigurationScopeRuntimeOptions(), factory);

        public IServiceCollection AddKeyedConfigurationScoped<TOptionsType, TServiceType, TImplementationType>(object? serviceKey, string? optionsName, Func<IServiceProvider, object?, TOptionsType, TImplementationType> factory)
            where TOptionsType : class
            where TServiceType : class
            where TImplementationType : class, TServiceType
            => services.AddKeyedConfigurationScoped<TOptionsType, TServiceType, TImplementationType>(serviceKey, optionsName, new ConfigurationScopeRuntimeOptions(), factory);

        public IServiceCollection AddKeyedConfigurationScoped<TOptionsType, TServiceType>(object? serviceKey, string? optionsName)
            where TOptionsType : class
            where TServiceType : class
            => services.AddKeyedConfigurationScoped<TOptionsType, TServiceType>(serviceKey, optionsName, new ConfigurationScopeRuntimeOptions());

        public IServiceCollection AddKeyedConfigurationScoped<TOptionsType, TServiceType, TImplementationType>(object? serviceKey, string? optionsName)
            where TOptionsType : class
            where TServiceType : class
            where TImplementationType : class, TServiceType
            => services.AddKeyedConfigurationScoped<TOptionsType, TServiceType, TImplementationType>(serviceKey, optionsName, new ConfigurationScopeRuntimeOptions());

        public IServiceCollection AddKeyedConfigurationScoped<TOptionsType, TServiceType>(object? serviceKey, string? optionsName, ConfigurationScopeRuntimeOptions options, Func<IServiceProvider, object?, TOptionsType, TServiceType> factory)
            where TOptionsType : class
            where TServiceType : class
        {
            return services.DoAddKeyedConfigurationScoped(serviceKey, optionsName, options, (sp, k) => new DelegateKeyedServiceFactory<TOptionsType, TServiceType>(sp, k, factory));
        }

        public IServiceCollection AddKeyedConfigurationScoped<TOptionsType, TServiceType, TImplementationType>(object? serviceKey, string? optionsName, ConfigurationScopeRuntimeOptions options, Func<IServiceProvider, object?, TOptionsType, TImplementationType> factory)
            where TOptionsType : class
            where TServiceType : class
            where TImplementationType : class, TServiceType
        {
            return services.DoAddKeyedConfigurationScoped<TOptionsType, TServiceType>(serviceKey, optionsName, options, (sp, k) => new DelegateKeyedServiceFactory<TOptionsType, TImplementationType>(sp, k, factory));
        }

        public IServiceCollection AddKeyedConfigurationScoped<TOptionsType, TServiceType>(object? serviceKey, string? optionsName, ConfigurationScopeRuntimeOptions options)
            where TOptionsType : class
            where TServiceType : class
        {
            return services.DoAddKeyedConfigurationScoped(serviceKey, optionsName, options, (sp, _) => new ActivatorUtilitiesServiceFactory<TOptionsType, TServiceType>(sp));
        }

        public IServiceCollection AddKeyedConfigurationScoped<TOptionsType, TServiceType, TImplementationType>(object? serviceKey, string? optionsName, ConfigurationScopeRuntimeOptions options)
            where TOptionsType : class
            where TServiceType : class
            where TImplementationType : class, TServiceType
        {
            return services.DoAddKeyedConfigurationScoped<TOptionsType, TServiceType>(serviceKey, optionsName, options, (sp, _) => new ActivatorUtilitiesServiceFactory<TOptionsType, TImplementationType>(sp));
        }
    
        private IServiceCollection DoAddConfigurationScoped<TOptionsType, TServiceType>(string? optionsName, ConfigurationScopeRuntimeOptions runtimeOptions, Func<IServiceProvider, IServiceFactory<TOptionsType, TServiceType>> serviceFactoryFactory) 
            where TOptionsType : class 
            where TServiceType : class
        {
            if (services.IsRegistered<IConfigurationScopedServiceScopeFactory<TServiceType>>())
            {
                throw new InvalidOperationException("Configuration scoped service already registered.  If multiple registrations are required, use keyed registration instead.");
            }

            // Allows callers to resolve a scope provider for TServiceType, which can be used anywhere (request handlers, background workers, etc...) 
            services.TryAddSingleton(sp => new OptionsMonitorConfigurationScopedServiceManager<TOptionsType, TServiceType>(
                optionsName,
                runtimeOptions,
                sp.GetRequiredService<IOptionsMonitor<TOptionsType>>(),
                serviceFactoryFactory(sp),
                sp.GetRequiredService<ILogger<OptionsMonitorConfigurationScopedServiceManager<TOptionsType, TServiceType>>>()));

            services.TryAddSingleton<IConfigurationScopedServiceScopeFactory<TServiceType>>((sp) => sp.GetRequiredService<OptionsMonitorConfigurationScopedServiceManager<TOptionsType, TServiceType>>());
            services.TryAddSingleton<IOptionsChangeConsumer<TOptionsType>>((sp) => sp.GetRequiredService<OptionsMonitorConfigurationScopedServiceManager<TOptionsType, TServiceType>>());

            // This layer of indirection is to allow the refcount decrement to happen on scope end.  The type is internal, so users can't resolve it without doing nasty things.  The resolution used by 
            // callers will be of TServiceType
            services.TryAddScoped(sp =>
            {
                var config_scoped_service = sp.GetRequiredService<IConfigurationScopedServiceScopeFactory<TServiceType>>();

                // Try to get the cancellation token from the service provider scope
                var http_context_accessor = sp.GetService<IHttpContextAccessor>();

                var cancellation_token = http_context_accessor?.HttpContext.RequestAborted ?? CancellationToken.None;
                return new ConfigurationScopeServiceAccessor<TServiceType>(config_scoped_service.Create(cancellation_token));
            });

            // Allows callers to resolve TServiceType tied directly to the request scope
            services.TryAddScoped(sp => sp.GetRequiredService<ConfigurationScopeServiceAccessor<TServiceType>>().Service);
            return services;
        }

        private IServiceCollection DoAddKeyedConfigurationScoped<TOptionsType, TServiceType>(object? serviceKey, string? optionsName, ConfigurationScopeRuntimeOptions runtimeOptions, Func<IServiceProvider, object?, IServiceFactory<TOptionsType, TServiceType>> serviceFactoryFactory) 
            where TOptionsType : class
            where TServiceType : class
        {
            if (services.IsRegisteredKeyed<IConfigurationScopedServiceScopeFactory<TServiceType>>(serviceKey))
            {
                throw new InvalidOperationException("Configuration scoped service already registered with key.  Multiple registrations of a type with the same key is not supported.");
            }

            // Allows callers to resolve a scope provider for TServiceType, which can be used anywhere (request handlers, background workers, etc...) 
            services.TryAddKeyedSingleton(serviceKey, (sp, k) => new OptionsMonitorConfigurationScopedServiceManager<TOptionsType, TServiceType>(
                optionsName,
                runtimeOptions,
                sp.GetRequiredService<IOptionsMonitor<TOptionsType>>(),
                serviceFactoryFactory(sp, k),
                sp.GetRequiredService<ILogger<OptionsMonitorConfigurationScopedServiceManager<TOptionsType, TServiceType>>>()));

            services.TryAddKeyedSingleton<IConfigurationScopedServiceScopeFactory<TServiceType>>(serviceKey, (sp, k) => sp.GetRequiredKeyedService<OptionsMonitorConfigurationScopedServiceManager<TOptionsType, TServiceType>>(k));
            services.TryAddKeyedSingleton<IOptionsChangeConsumer<TOptionsType>>(serviceKey, (sp, k) => sp.GetRequiredKeyedService<OptionsMonitorConfigurationScopedServiceManager<TOptionsType, TServiceType>>(k));

            // This layer of indirection is to allow the refcount decrement to happen on scope end.  The type is internal, so users can't resolve it without doing nasty things.  The resolution used by 
            // callers will be of TServiceType
            services.TryAddKeyedScoped(serviceKey, (sp, k) =>
            {
                var config_scoped_service = sp.GetRequiredKeyedService<IConfigurationScopedServiceScopeFactory<TServiceType>>(k);

                // Try to get the cancellation token from the service provider scope
                var http_context_accessor = sp.GetService<IHttpContextAccessor>();

                var cancellation_token = http_context_accessor?.HttpContext.RequestAborted ?? CancellationToken.None;
                return new ConfigurationScopeServiceAccessor<TServiceType>(config_scoped_service.Create(cancellation_token));
            });

            // Allows callers to resolve TServiceType tied directly to the request scope
            services.TryAddKeyedScoped(serviceKey, (sp, k) => sp.GetRequiredKeyedService<ConfigurationScopeServiceAccessor<TServiceType>>(k).Service);

            return services;
        }
    }
}