using ConfigurationScopedService;
using TestApi.Controllers;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.Configure<MyOptions>(builder.Configuration.GetSection("MyOptions1"));
builder.Services.Configure<MyOptions>(MyKeys.Key2, builder.Configuration.GetSection("MyOptions2"));

builder.Services.AddKeyedConfigurationScoped<MyOptions, MyService>(MyKeys.Key1, (sp, key, config) => new MyService(config));

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

var provider = app.Services.GetRequiredKeyedService<IConfigurationScopedServiceScopeProvider<MyService>>(MyKeys.Key1);
using var scope = provider.CreateScope();
Console.WriteLine(scope.Service.GetValue());
app.Run();
