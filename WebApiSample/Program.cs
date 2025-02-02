using ConfigurationScopedService;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Named options instances of the same type
builder.Services.Configure<MyOptions>(MyOptionKeys.Options1, builder.Configuration.GetSection("MyOptions1"));
builder.Services.Configure<MyOptions>(MyOptionKeys.Options2, builder.Configuration.GetSection("MyOptions2"));

// Each MyService is keyed and gets it's own named options instance
builder.Services.AddKeyedConfigurationScoped<MyOptions, MyService>(MyServiceKeys.Service1, MyOptionKeys.Options1, (sp, key, options) => new MyService(options));
builder.Services.AddKeyedConfigurationScoped<MyOptions, MyService>(MyServiceKeys.Service2, MyOptionKeys.Options2, (sp, key, options) => new MyService(options));

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

public class MyOptions
{
    public bool Enabled { get; set; }
    public int WorkValue { get; set; }
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
