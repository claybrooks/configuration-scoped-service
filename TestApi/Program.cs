using IOptionsMonitorSample.Controllers;
using MBL.ConfigurationScopedService;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.Configure<MyOptions>(builder.Configuration.GetSection($"{nameof(MyOptions)}"));

builder.Services.AddConfigurationScoped<MyOptions, MyService>( (sp, config) => new MyService(config));

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
