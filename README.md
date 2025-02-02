# Rationale

This library provides a mechanism to safely reload services in response to configuration changes. It is designed to work around 
the concurrency issues introduced by the default `IOptionsMonitor` implementation.  See the bottom of the readme for more information on why `IOptionsMonitor` is  potentially unsafe as a hot-reload trigger.

This library lifts the logic for service reloading in a re-useable and generic manner.  This alleviates developer effort of handling the reload within the service implementation itself. Code that executes in a highly concurrent
environment is hard enough to write as is, let me help you alleviate some of the pain by automating your live service reloading :)

**Because of the ref-counting and swap approach using in this library, there is a potential for multiple instances of your service being alive at the same time.  This is a trade-off for the added safety.
If your service can't coexist with another instance of itself, this library is not for you.**

## Introduction
A service with a ConfigurationScope has it's lifetime tied to the lifetime of a specific configuration section. If the configuration section data never changes
during the lifetime of the app, the service is effectively a singleton. If the configuration section data changes, the service is reconstructed and made available for use.

You can think of the lifetime of ConfigurationScoped as not longer than Singleton but greater than Scoped.

When a configuration change is observed, the following happens:
 1. A new instance of the service is created on a background thread
 2. The new service is swapped in and the old service ref-count is decremented.
 3. If the old service ref-count is 0, it is disposed of and removed.
 4. If the old service ref-count is greater than 0, it will be disposed of at a later time when the ref-count reaches 0.

Any service scope requests that occur after the swap in step 2 will receive the new service instance. Any active scopes that were created before the swap in step 2 will safely resolve the old service. Old service
scopes can live indefinitely and will continue to safely resolve the old service.

### IServiceCollection Registration
Service registration follows the general IServiceCollection registration pattern.  It supports both keyed and non-keyed registrations as well named and unnamed options instances.
```csharp
// Program.cs

// Register against an unnamed options instance
builder.Services.Configure<MyOptions>(builder.Configuration.GetSection("MyOptions"));
builder.Services.AddConfigurationScoped<MyOptions, MyService>((sp, options) => new MyService(options));

// Register against a named options instance
builder.Services.Configure<MyOptions>("Options1", builder.Configuration.GetSection("MyOptions"));
builder.Services.AddConfigurationScoped<MyOptions, MyService>("Options1", (sp, options) => new MyService(options));

// Register keyed
builder.Services.Configure<MyOptions>("Options1", builder.Configuration.GetSection("MyOptions1"));
builder.Services.Configure<MyOptions>("Options2", builder.Configuration.GetSection("MyOptions2"));
builder.Services.AddKeyedConfigurationScoped<MyOptions, MyService>("Options1", "ServiceKey1", (sp, key, options) => new MyService(options));
builder.Services.AddKeyedConfigurationScoped<MyOptions, MyService>("Options2", "ServiceKey2", (sp, key, options) => new MyService(options));

```

### Usage In Controllers
Because the configuration scope factory is registered as Scoped internally, resolving your service in a controller works as normal.  The ref-count of the current service instance is incremented
and decremented with the lifetime of the request scope (incremented on request scope start and decremented on request scope end).
```csharp
// Program.cs
builder.Services.Configure<MyOptions>("OptionsKey", builder.Configuration.GetSection("MyOptions"));
builder.Services.AddKeyedConfigurationScoped<MyOptions, MyService>("ServiceKey", "OptionsKey", (sp, key, options) => new MyService(options));

...

// MyServiceController.cs
[ApiController]
[Route("[controller]")]
public class MyServiceController : ControllerBase
{
    private readonly MyService _myService;

    // Behind the scenes, MyService was resolved via IConfigurationScopedServiceScopeFactory<MyService>().Create()
    public MyServiceController(MyService myService)
    {
        _myService = myService;
    }
}
```

### Usage In Minimal APIs
Usage is identical in minimal APIs
```csharp
// Program.cs
builder.Services.Configure<MyOptions>("OptionsKey", builder.Configuration.GetSection("MyOptions"));
builder.Services.AddKeyedConfigurationScoped<MyOptions, MyService>("ServiceKey", "OptionsKey", (sp, key, options) => new MyService(options));

...

// Behind the scenes, MyService was resolved via IConfigurationScopedServiceScopeFactory<MyService>().Create()
app.MapGet("/test", ([FromServices] MyService myService) =>
{
    // Do something with myService
    return Results.Ok();
});
```

## Usage External To Request Scopes

ConfigurationScoped services can be resolved anywhere with the use of `IConfigurationScopedServiceScopeFactory<TServiceType>`.  The ref-count of the current service instance is incremented
and decremented with the lifetime of the manually created scope (incremented on scope start and decremented on scope end).
### Example
```csharp
// Program.cs
builder.Services.Configure<MyOptions>(builder.Configuration.GetSection("MyOptions"));
builder.Services.AddConfigurationScoped<MyOptions, MyService>((sp, config) => new MyService(config));

...

// MyBackgroundService.cs
public class MyBackgroundService : BackgroundService
{
    private readonly IConfigurationScopedServiceScopeFactory<MyService> _myServiceScopeFactory;

    public MyBackgroundService(IConfigurationScopedServiceScopeFactory<MyService> myServiceScopeFactory)
    {
        _myServiceScopeFactory = myServiceScopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (true)
        {
            stoppingToken.ThrowIfCancellationRequested();

            // Ref-count incremented on create
            using (var scope = _myServiceScopeFactory.Create())
            {
                var service = scope.Service;
                // Do something with service
            }
            // Ref-count decremented on dispose
            await Task.Delay(1000, stoppingToken);
        }
    }
}
```

## Why is IOptionsMonitor Potentially Unsafe As a Reload Trigger?
IOptionsMonitor change notifications are not coordinated with request processing, or with anything for that matter. They are fired on a background thread and it is up to the application code to deal with it.
Most of code I see does not take this into account.

Take the following example:
```csharp
// MyService.cs
public class MyService
{
    private MyOptions _options;

    public MyService(IOptionsMonitor<MyOptions> optionsMonitor)
    {
        _options = optionsMonitor.CurrentValue;
        optionsMonitor.OnChange(o => _options = o);
    }

    public bool IsFeatureAEnabled()
    {
        return _options.FeatureAEnabled;
    }

    public int DoFeatureA()
    {
        if (!_options.FeatureAEnabled)
        {
            throw new Exception("Feature A is not enabled.  Callers must check first before invoking this method.");
        }
        // Do something awesome
        return 1;
    }
}

...

//MyServiceController.cs
[ApiController]
[Route("[controller]")]
public class MyServiceController : ControllerBase
{
    private readonly MyService _myService;

    public MyServiceController2(MyService myService)
    {
        _myService = myService;
    }

    [HttpGet]
    public int DoFeatureA()
    {
        if (_myService.IsFeatureAEnabled())
        {
            // It is entirely possible that a change to MyOptions disabled the feature.
            // But we are already passed the check, the next line will throw an exception.
            return _myService.DoFeatureA();
        }
        return -1;
    }
}
```
The controller implementation does exactly what it's supposed to do.  It checks if FeatureA is enabled before calling in to DoFeatureA().
However, if FeatureA is disabled in the middle of handling this request we may see an exception thrown in DoFeatureA().
You can convince yourself of this by running the TestApi project in the repo and performing the following steps
 1. Run the TestApi project and navigate to the swagger page (Should be https://localhost:7028/swagger)
 2. Invoke the "TestWithDelay" endpoint (it is instrumented to delay for 10 seconds between the IsFeatureAEnabled() and DoFeatureA() calls)
 3. While the endpoint is processing, go to appsettings.json in the solution and set the "FeatureAEnabled" option to false and save the file.  (this needs to be done within 10 seconds)
 4. "TestWithDelay" will eventually throw an exception

There is no form of locking, either internally in MyService or externally in the controller, that can fix this problem.
In fact, it is fundamentally impossible to fix this problem in either the service or the controller if your service requires unchanging configuration across function calls.

**See above on how you can ensure a configuration rug pull doesn't happen mid request :)**