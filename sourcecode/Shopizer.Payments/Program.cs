using Microsoft.AspNetCore.Mvc;
using Shopizer.Payments.Data;
using Shopizer.Payments.Middleware;
using Shopizer.Payments.Services;
using Shopizer.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddNpgsqlDataSource("shopizerDb");
builder.AddRabbitMQClient("rabbitmq");
builder.Services.AddSingleton<PaymentRepository>();
builder.Services.AddSingleton<PaymentProviderService>();
builder.Services.AddSingleton<TokenService>();
builder.Services.AddSingleton<EventPublisher>();
builder.Services.AddScoped<PaymentService>();
builder.Services.AddSingleton<SchemaInitializer>();
builder.Services.Configure<ApiBehaviorOptions>(options => options.SuppressModelStateInvalidFilter = true);
builder.Services.AddHttpContextAccessor();
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
    });

var app = builder.Build();

await app.Services.GetRequiredService<SchemaInitializer>().InitializeAsync(CancellationToken.None);
app.UseMiddleware<ErrorMiddleware>();
app.UseMiddleware<TokenMiddleware>();
app.MapControllers();
app.MapDefaultEndpoints();

app.Run();
