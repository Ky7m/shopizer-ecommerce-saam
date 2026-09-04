using Shopizer.MerchantAdministration.Data;
using Shopizer.MerchantAdministration.Middleware;
using Shopizer.MerchantAdministration.Services;
using Shopizer.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddNpgsqlDataSource("shopizerDb");
builder.AddRabbitMQClient("rabbitmq");
builder.Services.AddSingleton<StoreRepository>();
builder.Services.AddSingleton<EventPublisher>();
builder.Services.AddSingleton<SchemaInitializer>();
builder.Services.AddSingleton<TokenService>();
builder.Services.AddScoped<StoreService>();
builder.Services.AddSingleton<FileProviderClient>();
builder.Services.AddHttpClient("file-provider");
builder.Services.AddControllers().ConfigureApiBehaviorOptions(options => options.SuppressModelStateInvalidFilter = true).AddJsonOptions(options =>
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
