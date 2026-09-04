using Shopizer.PlatformIntegrations.Data;
using Shopizer.PlatformIntegrations.DTOs;
using Shopizer.PlatformIntegrations.Middleware;
using Shopizer.PlatformIntegrations.Models;
using Shopizer.PlatformIntegrations.Services;
using Shopizer.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddNpgsqlDataSource("shopizerDb");
builder.AddRabbitMQClient("rabbitmq");
builder.Services.AddHttpClient("external-integrations", client =>
{
    client.Timeout = TimeSpan.FromSeconds(120);
    client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/xml"));
});
builder.Services.AddSingleton<IntegrationRepository>();
builder.Services.AddSingleton<SchemaInitializer>();
builder.Services.AddSingleton<EventPublisher>();
builder.Services.AddHostedService<IntegrationEventConsumer>();
builder.Services.AddScoped<IntegrationService>();
builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
    options.JsonSerializerOptions.Converters.Add(new MarkerJsonConverter<IntegrationTypeDto>());
    options.JsonSerializerOptions.Converters.Add(new MarkerJsonConverter<AdapterStatusDto>());
    options.JsonSerializerOptions.Converters.Add(new MarkerJsonConverter<ContentTypeDto>());
    options.JsonSerializerOptions.Converters.Add(new MarkerJsonConverter<StorageProviderDto>());
    options.JsonSerializerOptions.Converters.Add(new MarkerJsonConverter<FileStatusDto>());
    options.JsonSerializerOptions.Converters.Add(new MarkerJsonConverter<DeliveryAttemptStatusDto>());
    options.JsonSerializerOptions.Converters.Add(new MarkerJsonConverter<DeliveryOperationStatusDto>());
    options.JsonSerializerOptions.Converters.Add(new MarkerJsonConverter<EmailMessageStatusDto>());
    options.JsonSerializerOptions.Converters.Add(new MarkerPayloadJsonConverter<UploadedFileAssetDto>());
});

var app = builder.Build();
await app.Services.GetRequiredService<SchemaInitializer>().InitializeAsync(CancellationToken.None);
app.UseMiddleware<ErrorMiddleware>();
app.UseMiddleware<TokenMiddleware>();
app.MapControllers();
app.MapDefaultEndpoints();
app.Run();
