using ConfigurationScopedService;
using WebApiSample;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Named options instances of the same type
builder.Services.Configure<MyOptions>(MyOptionKeys.Options1, builder.Configuration.GetSection("MyOptions1"));
builder.Services.Configure<MyOptions>(MyOptionKeys.Options2, builder.Configuration.GetSection("MyOptions2"));

var runtime_options = new ConfigurationScopeRuntimeOptions() { BlockOnSwap = false };

var _throw = false;
// Each MyService is keyed and gets it's own named options instance
builder.Services.AddKeyedConfigurationScoped<MyOptions, MyService>(MyServiceKeys.Service1, MyOptionKeys.Options1, runtime_options, (sp, key, options) =>
{
    if (_throw)
    {
        throw new Exception("Ooops");
    }
    else
    {
        _throw = true;
        return new MyService(options);
    }
});
builder.Services.AddKeyedConfigurationScoped<MyOptions, MyService>(MyServiceKeys.Service2, MyOptionKeys.Options2, runtime_options, (sp, key, options) => new MyService(options));

// Our background service keeps the scope open for 10 seconds.  You can play with the blockonswap flag to see how this
// affects api calls to the service while the backgroundscope is waiting to be release.
builder.Services.AddSingleton<IHostedService, MyBackgroundService>(
    sp => new MyBackgroundService(MyServiceKeys.Service1, sp.GetRequiredKeyedService<IConfigurationScopedServiceScopeFactory<MyService>>(MyServiceKeys.Service1)));

builder.Services.AddSingleton<IHostedService, MyBackgroundService>(
    sp => new MyBackgroundService(MyServiceKeys.Service2, sp.GetRequiredKeyedService<IConfigurationScopedServiceScopeFactory<MyService>>(MyServiceKeys.Service2)));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options =>
    {
        options.EnableTryItOutByDefault();
        options.SwaggerEndpoint("/openapi/v1.json", "Dummy API");
    }); // Adds Swagger UI for interactive API testing
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();

public class MyServiceKeys
{
    public const string Service1 = nameof(Service1);
    public const string Service2 = nameof(Service2);
}

public class MyOptionKeys
{
    public const string Options1 = nameof(Options1);
    public const string Options2 = nameof(Options2);
}

//public class MyOptions
//{
//    public bool Enabled { get; set; }
//    public int WorkValue { get; set; }
//}

public class MyOptions : IEquatable<MyOptions>
{
    public bool Enabled { get; set; }
    public int WorkValue { get; set; }

    public bool Equals(MyOptions? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return Enabled == other.Enabled && WorkValue == other.WorkValue;
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((MyOptions) obj);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Enabled, WorkValue);
    }
}

public class MyService
{
    private readonly MyOptions _options;

    public MyService(MyOptions options)
    {
        _options = options;
    }

    public bool IsEnabled()
    {
        return _options.Enabled;
    }

    public int DoWork()
    {
        if (!IsEnabled())
        {
            throw new InvalidOperationException("Service is not enabled");
        }

        return _options.WorkValue;
    }
}
