using Shopizer.Shipping.Data;
using Shopizer.Shipping.Middleware;
using Shopizer.Shipping.Services;
using Shopizer.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddNpgsqlDataSource("shopizerDb");
builder.AddRabbitMQClient("rabbitmq");
builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<ShippingRepository>();
builder.Services.AddSingleton<SchemaInitializer>();
builder.Services.AddSingleton<EventPublisher>();
builder.Services.AddSingleton<TokenService>();
builder.Services.AddScoped<ShippingService>();
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
