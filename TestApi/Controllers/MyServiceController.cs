using Microsoft.AspNetCore.Mvc;

namespace IOptionsMonitorSample.Controllers;

public class MyOptions
{
    public bool FeatureAEnabled { get; set; }
    public int Value { get; set; }
}

public class MyService
{
    private readonly MyOptions _options;

    public MyService(MyOptions options)
    {
        _options = options;
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

[ApiController]
[Route("[controller]")]
public class MyServiceController : ControllerBase
{
    private readonly MyService _myService;

    public MyServiceController(MyService myService)
    {
        _myService = myService;
    }

    [HttpGet("TestWithDelay")]
    public async Task<int> TestWithDelay()
    {
        if (_myService.IsFeatureAEnabled())
        {
            await Task.Delay(TimeSpan.FromSeconds(10));
            return _myService.DoFeatureA();
        }
        return -1;

    }

    [HttpGet("Test")]
    public Task<int> Test()
    {
        if (_myService.IsFeatureAEnabled())
        {
            return Task.FromResult(_myService.DoFeatureA());
        }
        return Task.FromResult(-1);
    }
}