namespace MBL.ConfigurationScopedService;

public interface IConfigurationScopedServiceScope<out TService> : IDisposable
{
    TService Service { get; }
}