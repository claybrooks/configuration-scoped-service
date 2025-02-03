# Rationale

This library provides a safe reload mechanism in response to configuration changes. It is designed to work around the
concurrency issues introduced by the default `IOptionsMonitor` implementation.  See the bottom of the readme for more
information on why `IOptionsMonitor` is  potentially unsafe as a live reload trigger.

This library lifts the logic for live service reloading in a re-useable and generic manner. Code executing in a
highly concurrent environment is hard enough to write as is, let me help you alleviate some of the pain by automating
your live service reloading :)

This library is of the opinion services should be reconstructed in response to configuration changes rather than
managing configuration changes internally in the service code. Reconstructing externally has the following advantages:
 1. Reconstruction code is generic and re-useable. Any service you write can take advantage of it.
 2. Services can be written with the assumption that initial conditions are immutable. This is a **great thing**.
 3. Service logic related to resource re-initialization can be deleted. Just dispose of things in your service as if its lifetime was coming to a natural end.
 4. Remove usages of IOptions/IOptionsSnapshot/IOptionsMonitor in your services. This allows you to reuse the service code in non-ASP.NET Core projects.

## Introduction
This library adds a new service scope for our convenience: ConfigurationScoped.  A service that is ConfigurationScoped
has it's lifetime tied to the lifetime of a specific configuration section. If the configuration section data never
changes during the lifetime of the app, the service is effectively a singleton. If the configuration section data
changes, a new instance of the service is constructed and made available for use. You can think of the lifetime of
ConfigurationScoped as not longer than Singleton but greater than Scoped.

This library uses a ref-counting mechanism to know if a service is being actively used. When a configuration change 
occurs, a new version of the service will be created and the old one will phase out when it's ref-count reaches 0. The 
ref-count reaches 0 when all active scopes for that service instance have been destroyed.

There are two modes of service swapping when a configuration change is encountered, blocking and non blocking. Non
blocking is the default and is recommended for most use cases.

In non blocking mode, the following occurs during a configuration change:
 1. A new instance of the service is created
 2. The new service is swapped in and the old service ref-count is decremented
 3. If the old service ref-count is 0, it is disposed of and removed
 4. If the old service ref-count is greater than 0, it will be disposed of at a later time when its ref-count reaches 0

In blocking mode, the following occurs during a configuration change:
 1. Incoming service scope requests block
 2. All active service scopes are waited on to end (ref-count reaches 0)
 3. The old service is disposed of and removed
 4. A new instance of the service is created and swapped in
 5. Scope requests are completed with the new service instance

Pros/Cons of non blocking mode:
 - (Pro) Service swapping is immediate
 - (Pro) New service scope requests immediately get the new service
 - (Pro) Old service scopes remain untouched and will continue to safely resolve the old instance of the service
 - (Con) Multiple service instances can live at the same time

Pros/Cons of blocking mode:
 - (Pro) Strong guarantee that only one service instance is alive at a time
 - (Con) Service scopes that are slow to end will hold up any new service scope requests (api requests that rely on the new service will block)
 - (Con) New service scope requests will block indefinitely until all active scopes end

### IServiceCollection Registration
Service registration follows the general IServiceCollection registration pattern.  It supports both keyed and non-keyed
registrations as well named and unnamed options instances.

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
Internally, the service type is registered as scoped and will be resolved automagically in an active request scope.
The ref-count of the current service instance is incremented and decremented with the lifetime of the request scope
(incremented on request scope start and decremented on request scope end).

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

    // Behind the scenes, MyService was resolved via IConfigurationScopedServiceScopeFactory<MyService>().Create().Service
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

// Behind the scenes, MyService was resolved via IConfigurationScopedServiceScopeFactory<MyService>().Create().Service
app.MapGet("/test", ([FromServices] MyService myService) =>
{
    // Do something with myService
    return Results.Ok();
});
```

## Usage External To Request Scopes

ConfigurationScoped services can be resolved anywhere with the use of `IConfigurationScopedServiceScopeFactory<TServiceType>`.
The ref-count of the current service instance is incremented and decremented with the lifetime of the manually created
scope (incremented on scope start and decremented on scope end).

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
IOptionsMonitor change notifications are not coordinated with request processing (or with anything for that matter).
They are fired on a background thread and it is up to the application code to deal with it. Most code I see does not
take this into account.

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
            // It is entirely possible a change to MyOptions disabled the feature.
            // But we are already passed the check, the next line will throw an exception.
            return _myService.DoFeatureA();
        }
        return -1;
    }
}
```
The controller implementation does exactly what it's supposed to do.  It checks if FeatureA is enabled before calling
in to DoFeatureA(). However, if FeatureA is disabled in the middle of handling this request we may see an exception
thrown in DoFeatureA().
You can convince yourself of this by running the IOptionsMonitorSample project in the repo and performing the following
steps
 1. Run the IOptionsMonitorSample project and navigate to the swagger page (Should be https://localhost:7028/swagger)
 2. Invoke the "TestWithDelay" endpoint (it is instrumented to delay for 10 seconds between the IsFeatureAEnabled() and DoFeatureA() calls)
 3. While the endpoint is processing, go to appsettings.json in the solution and set the "FeatureAEnabled" option to false and save the file
 4. "TestWithDelay" will eventually throw an exception

There is no form of locking, either internally in MyService or in the controller, that can fix this problem.

**See above on how you can ensure a configuration rug pull doesn't occur mid request :)**