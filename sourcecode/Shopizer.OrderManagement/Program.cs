using Shopizer.OrderManagement.Data;
using Shopizer.OrderManagement.Middleware;
using Shopizer.OrderManagement.Models;
using Shopizer.OrderManagement.Services;
using Shopizer.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddNpgsqlDataSource("shopizerDb");
builder.AddRabbitMQClient("rabbitmq");
builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<SchemaInitializer>();
builder.Services.AddSingleton<OrderRepository>();
builder.Services.AddSingleton<EventPublisher>();
builder.Services.AddHostedService<OutboxDispatcher>();
builder.Services.AddHostedService<OrderEventConsumer>();
builder.Services.AddScoped<OrderService>();
builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(options => options.SuppressModelStateInvalidFilter = true)
    .AddMvcOptions(options => options.Filters.Add<ModelStateExceptionFilter>())
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
        options.JsonSerializerOptions.Converters.Add(new ContractStatusJsonConverterFactory());
    });

var app = builder.Build();

await app.Services.GetRequiredService<SchemaInitializer>().InitializeAsync(CancellationToken.None);
app.UseMiddleware<ErrorMiddleware>();
app.UseMiddleware<TokenMiddleware>();
app.MapControllers();
app.MapDefaultEndpoints();

app.Run();
