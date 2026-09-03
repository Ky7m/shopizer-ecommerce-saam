using System.Text.Json;
using System.Text.Json.Serialization;
using Shopizer.Search.Data;
using Shopizer.Search.Middleware;
using Shopizer.Search.Models;
using Shopizer.Search.Services;
using Shopizer.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddNpgsqlDataSource("searchdb");
builder.AddRabbitMQClient("rabbitmq");
if (!builder.Environment.IsDevelopment() &&
    string.IsNullOrWhiteSpace(builder.Configuration["Search:JwtSecret"]))
{
    throw new InvalidOperationException("Search:JwtSecret must be configured outside Development.");
}
builder.Services.AddSingleton<SearchRepository>();
builder.Services.AddSingleton<EventPublisher>();
builder.Services.AddSingleton<RebuildQueue>();
builder.Services.AddSingleton<SearchService>();
builder.Services.AddHostedService<SearchRebuildWorker>();
builder.Services.AddSingleton<SchemaInitializer>();
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    options.SerializerOptions.Converters.Add(new RebuildStatusJsonConverter());
});
builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(options => options.SuppressModelStateInvalidFilter = true)
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        options.JsonSerializerOptions.Converters.Add(new RebuildStatusJsonConverter());
    });

var app = builder.Build();
await app.Services.GetRequiredService<SchemaInitializer>().InitializeAsync(CancellationToken.None);
app.UseMiddleware<ErrorMiddleware>();
app.UseMiddleware<TokenMiddleware>();
app.MapControllers();

app.MapDefaultEndpoints();

app.Run();

public sealed class RebuildStatusJsonConverter : JsonConverter<Shopizer.Services.Ms03.Contracts.RebuildStatusDto>
{
    public override Shopizer.Services.Ms03.Contracts.RebuildStatusDto Read(
        ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        reader.GetString();
        return RebuildStatusRegistry.Create("Requested");
    }

    public override void Write(
        Utf8JsonWriter writer, Shopizer.Services.Ms03.Contracts.RebuildStatusDto value,
        JsonSerializerOptions options) =>
        writer.WriteStringValue(RebuildStatusRegistry.Get(value));
}
