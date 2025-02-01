using Microsoft.AspNetCore.Mvc;

namespace TestApi.Controllers;

public static class MyKeys
{
    public const string Key1 = "key1";
    public const string Key2 = "key2";
}

public class MyOptions
{
    public bool Enabled { get; set; }
    public int Value { get; set; }
}

public interface IMyService
{
    bool IsEnabled();
    int GetValue();
}

public class MyService : IMyService
{
    private readonly MyOptions _options;

    public MyService(MyOptions options)
    {
        _options = options;
    }

    public bool IsEnabled() => _options.Enabled;

    public int GetValue()
    {
        if (!_options.Enabled)
        {
            throw new InvalidOperationException("Service is not enabled");
        }
        return _options.Value;
    }
}

[ApiController]
[Route("[controller]")]
public class MyServiceController : ControllerBase
{
    [HttpGet("TestWithDelay_1")]
    public async Task<int> TestWithDelay1([FromKeyedServices(MyKeys.Key1)] MyService service)
    {
        if (!service.IsEnabled())
        {
            return -1;
        }

        await Task.Delay(TimeSpan.FromSeconds(10));
        return service.GetValue();

    }

    [HttpGet("Test_1")]
    public Task<int> Test1([FromKeyedServices(MyKeys.Key1)] MyService service)
    {
        return !service.IsEnabled() ? Task.FromResult(-1) : Task.FromResult(service.GetValue());
    }

    [HttpGet("TestWithDelay_2")]
    public async Task<int> TestWithDelay2([FromKeyedServices(MyKeys.Key2)] MyService service)
    {
        if (!service.IsEnabled())
        {
            return -1;
        }

        await Task.Delay(TimeSpan.FromSeconds(10));
        return service.GetValue();

    }

    [HttpGet("Test_2")]
    public Task<int> Test2([FromKeyedServices(MyKeys.Key2)] MyService service)
    {
        return !service.IsEnabled() ? Task.FromResult(-1) : Task.FromResult(service.GetValue());
    }
}
