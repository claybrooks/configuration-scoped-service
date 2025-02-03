using ConfigurationScopedService;

namespace WebApiSample;

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

            using var scope = await _scopeFactory.CreateAsync(stoppingToken).ConfigureAwait(false);

            var service = scope.Service;
            Console.WriteLine(service.IsEnabled() ? $"{_serviceKey} Work value: {service.DoWork()}" : $"{_serviceKey} Disabled");
            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken).ConfigureAwait(false);
        }
    }
}