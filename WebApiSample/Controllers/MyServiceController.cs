using Microsoft.AspNetCore.Mvc;

namespace WebApiSample.Controllers;

[ApiController]
[Route("[controller]")]
public class WeatherForecastController : ControllerBase
{
    [HttpGet("Service1Endpiont")]
    public Task<int> GetService1([FromKeyedServices(MyServiceKeys.Service1)] MyService myService)
    {
        if (!myService.IsEnabled())
        {
            return Task.FromResult(-1);
        }
        return Task.FromResult(myService.DoWork());
    }

    [HttpGet("Service2Endpiont")]
    public Task<int> GetService2([FromKeyedServices(MyServiceKeys.Service2)] MyService myService)
    {
        if (!myService.IsEnabled())
        {
            return Task.FromResult(-1);
        }
        return Task.FromResult(myService.DoWork());
    }
}
