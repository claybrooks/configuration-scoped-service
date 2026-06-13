# ConfigurationScopedService

[![Nuget](https://img.shields.io/nuget/v/ConfigurationScopedService)](https://www.nuget.org/packages/ConfigurationScopedService)
![.NET Version](https://img.shields.io/badge/.NET-Standard2.0-blue)

A thread-safe, automated live-reload mechanism for .NET services in response to configuration changes. 

## 💡 Rationale

This library solves the concurrency and data-race issues introduced by the default `IOptionsMonitor` implementation. (See [Why IOptionsMonitor is Potentially Unsafe](#why-is-ioptionsmonitor-potentially-unsafe-as-a-reload-trigger) below).

Writing reliable code in highly concurrent environments is difficult. `ConfigurationScopedService` alleviates this pain by automating live service reloading using an architectural pattern: **services should be completely reconstructed when configuration changes, rather than managing state changes internally.**

### Key Advantages
* **Generic & Reusable:** The reload logic is decoupled from your business logic.
* **Immutable State:** Services are written with the guarantee that initial conditions never change mid-execution.
* **Cleaner Code:** No need for complex resource re-initialization logic. Just dispose of assets naturally in your `Dispose` method.
* **Fewer Dependencies:** Removes the need to inject `IOptions`, `IOptionsSnapshot`, or `IOptionsMonitor` into your service logic.

---

## 🚀 Introduction

This library introduces a new service lifetime scope: **ConfigurationScoped**. 

The lifetime of a `ConfigurationScoped` service is bound to a specific configuration section:
* **No changes:** The service behaves effectively as a **Singleton**.
* **Configuration changes:** A new instance of the service is constructed and made available for all future resolutions.

### Reference Counting
The library tracks active service usage via a reference-counting mechanism. When configuration updates, a new version of the service is instantly swapped in. The old version phases out and is automatically disposed of once its reference count hits zero (meaning all active scopes using it have finished).

### Swapping Modes
You can configure how services swap when a configuration change is detected:

#### 1. Non-Blocking (Default & Recommended)
1. A new service instance is initialized.
2. The new service is swapped in; the old instance's reference count decrements.
3. If the old instance's count hits 0, it is immediately disposed.
4. If the count is > 0, it is safely disposed later when active scopes finish.

* 👍 **Pros:** Swapping is immediate. New requests instantly get the updated service. Existing scopes safely finish using the old service.
* 👎 **Cons:** Multiple instances of the service can temporarily co-exist in memory during the transition.

#### 2. Blocking
1. Incoming service scope requests are paused/blocked.
2. The engine waits for all active service scopes using the old instance to finish.
3. The old service is disposed.
4. A new service instance is initialized and unblocks incoming requests.

* 👍 **Pros:** Guarantees that exactly one instance of the service is alive at any given moment.
* 👎 **Cons:** Slow-running scopes will stall new requests, causing latency or timeouts in downstream systems like APIs.

---

## 🛠️ Installation & Registration

### IServiceCollection Registration
Registration follows standard .NET patterns and fully supports standard, named, and keyed options/services.

```csharp
// Program.cs

// 1. Unnamed options instance
builder.Services.Configure<MyOptions>(builder.Configuration.GetSection("MyOptions"));
builder.Services.AddConfigurationScoped<MyOptions, MyService>((sp, options) => new MyService(options));

// 2. Named options instance
builder.Services.Configure<MyOptions>("Options1", builder.Configuration.GetSection("MyOptions"));
builder.Services.AddConfigurationScoped<MyOptions, MyService>("Options1", (sp, options) => new MyService(options));

// 3. Keyed service registration
builder.Services.Configure<MyOptions>("Options1", builder.Configuration.GetSection("MyOptions1"));
builder.Services.Configure<MyOptions>("Options2", builder.Configuration.GetSection("MyOptions2"));

builder.Services.AddKeyedConfigurationScoped<MyOptions, MyService>("Options1", "ServiceKey1", (sp, key, options) => new MyService(options));
builder.Services.AddKeyedConfigurationScoped<MyOptions, MyService>("Options2", "ServiceKey2", (sp, key, options) => new MyService(options));
```

---

## 💻 Usage Examples

### 1. In Controllers
The service type is registered as scoped internally. It resolves automatically within an active HTTP request scope. Reference counts increment when the request starts and decrement when the request ends.

```csharp
// MyServiceController.cs
[ApiController]
[Route("[controller]")]
public class MyServiceController : ControllerBase
{
    private readonly MyService _myService;

    // Resolved automatically behind the scenes via IConfigurationScopedServiceScopeFactory
    public MyServiceController(MyService myService)
    {
        _myService = myService;
    }
}
```

### 2. In Minimal APIs
Usage is identical and fully compatible with Minimal API parameter binding.

```csharp
app.MapGet("/test", ([FromServices] MyService myService) => 
{
    // Use myService safely here
    return Results.Ok();
});
```

### 3. Outside of Request Scopes (e.g., Background Tasks)
To resolve `ConfigurationScoped` services in background workers, use `IConfigurationScopedServiceScopeFactory<TServiceType>` to safely manage the reference lifespan manually.

```csharp
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
        while (!stoppingToken.IsCancellationRequested)
        {
            // Scope creation increments the reference count
            using (var scope = _myServiceScopeFactory.Create())
            {
                var service = scope.Service;
                // Execute logic with the service instance safely
            } 
            // Disposing the scope decrements the reference count

            await Task.Delay(1000, stoppingToken);
        }
    }
}
```

---

## ⚠️ Why is IOptionsMonitor Potentially Unsafe As a Reload Trigger?

`IOptionsMonitor` change notifications are entirely disconnected from the .NET request pipeline. Updates execute on a background thread asynchronously, leaving application code vulnerable to race conditions and mid-request state modifications.

### The Race Condition Problem

Consider this common but flawed implementation:

```csharp
// MyService.cs
public class MyService
{
    private MyOptions _options;

    public MyService(IOptionsMonitor<MyOptions> optionsMonitor)
    {
        _options = optionsMonitor.CurrentValue;
        optionsMonitor.OnChange(o => _options = o); // Updates via background thread
    }

    public bool IsFeatureAEnabled() => _options.FeatureAEnabled;

    public int DoFeatureA()
    {
        if (!_options.FeatureAEnabled)
        {
            throw new Exception("Feature A is disabled!");
        }
        return 1;
    }
}

// MyServiceController.cs
[HttpGet]
public int Get()
{
    if (_myService.IsFeatureAEnabled())
    {
        // 💥 RACE CONDITION: If appsettings.json is saved and modifies MyOptions 
        // right here, the next line throws an unexpected Exception!
        return _myService.DoFeatureA();
    }
    return -1;
}
```

The controller logic looks safe on paper: it checks if the feature is active before execution. However, if a configuration change occurs exactly *between* the check and the method invocation, your logic flow is now in an invalid state: It would have never reached that line of code if the initial conditions of the request were held constant.

### How to reproduce this issue in the repository:
1. Run the `IOptionsMonitorSample` project and open Swagger (`https://localhost:7028/swagger`).
2. Open `appsettings.json` and get ready to make a change to the `FeatureAEnabled` setting. 
2. IN swagger, execute the `TestWithDelay` endpoint (introduces a 10-second delay between the feature check and execution to give you time to make the settings change).
3. In `appsettings.json` change `"FeatureAEnabled"` to `false`, then save.
4. The request will throw an exception.

`ConfigurationScopedService` prevents this structural "rug pull" entirely, ensuring your application stays predictable and reliable.
