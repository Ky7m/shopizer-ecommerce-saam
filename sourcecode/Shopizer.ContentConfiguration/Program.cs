using Shopizer.ContentConfiguration.Data;
using Shopizer.ContentConfiguration.Middleware;
using Shopizer.ContentConfiguration.Services;
using Shopizer.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddNpgsqlDataSource("shopizerDb");
builder.AddRabbitMQClient("rabbitmq");
builder.Services.AddSingleton<ContentRepository>();
builder.Services.AddSingleton<TokenService>();
builder.Services.AddSingleton<FileProvider>();
builder.Services.AddSingleton<ModuleCache>();
builder.Services.AddSingleton<ConfigurationProtector>();
builder.Services.AddSingleton<EventPublisher>();
builder.Services.AddScoped<ContentService>();
builder.Services.AddScoped<ConfigurationService>();
builder.Services.AddSingleton<SchemaInitializer>();
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.DefaultIgnoreCondition =
            System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
    });

var app = builder.Build();

await app.Services.GetRequiredService<SchemaInitializer>().InitializeAsync(CancellationToken.None);
app.UseMiddleware<ErrorMiddleware>();
app.UseMiddleware<TokenMiddleware>();
app.MapControllers();
app.MapDefaultEndpoints();

app.Run();
