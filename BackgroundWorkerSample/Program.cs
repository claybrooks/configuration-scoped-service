using ConfigurationScopedService;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = Host.CreateApplicationBuilder();

// Named options instances of the same type
host.Services.Configure<MyOptions>(MyOptionKeys.Options1, host.Configuration.GetSection("MyOptions1"));
host.Services.Configure<MyOptions>(MyOptionKeys.Options2, host.Configuration.GetSection("MyOptions2"));

// Each MyService is keyed and gets it's own named options instance
host.Services.AddKeyedConfigurationScoped<MyOptions, MyService>(MyServiceKeys.Service1, MyOptionKeys.Options1, (sp, key, options) => new MyService(options));
host.Services.AddKeyedConfigurationScoped<MyOptions, MyService>(MyServiceKeys.Service2, MyOptionKeys.Options2, (sp, key, options) => new MyService(options));

// Each background service gets it's own keyed MyService
host.Services.AddSingleton<IHostedService, MyBackgroundService>(
    sp => new MyBackgroundService(MyServiceKeys.Service1, sp.GetRequiredKeyedService<IConfigurationScopedServiceScopeFactory<MyService>>(MyServiceKeys.Service1)));

host.Services.AddSingleton<IHostedService, MyBackgroundService>(
    sp => new MyBackgroundService(MyServiceKeys.Service2, sp.GetRequiredKeyedService<IConfigurationScopedServiceScopeFactory<MyService>>(MyServiceKeys.Service2)));

var app = host.Build();
await app.RunAsync();

internal class MyBackgroundService : BackgroundService
{
    private readonly string _serviceKey;
    private readonly IConfigurationScopedServiceScopeFactory<MyService> _scopeFactory;

    public MyBackgroundService(string serviceKey, IConfigurationScopedServiceScopeFactory<MyService> scopeFactory)
    {
        _serviceKey = serviceKey;
        _scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (true)
        {
            stoppingToken.ThrowIfCancellationRequested();

            using (var scope = await _scopeFactory.CreateAsync(stoppingToken))
            {
                var service = scope.Service;
                if (service.IsEnabled())
                {
                    Console.WriteLine($"{_serviceKey} Work value: {service.DoWork()}");
                }
                else
                {
                    Console.WriteLine($"{_serviceKey} Disabled");
                }
            }

            await Task.Delay(1000, stoppingToken).ConfigureAwait(false);
        }
    }
}

internal class MyServiceKeys
{
    public const string Service1 = nameof(Service1);
    public const string Service2 = nameof(Service2);
}

internal class MyOptionKeys
{
    public const string Options1 = nameof(Options1);
    public const string Options2 = nameof(Options2);
}

internal class MyOptions : IEquatable<MyOptions>
{
    public bool Enabled { get; }
    public int WorkValue { get; }

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

internal class MyService
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
