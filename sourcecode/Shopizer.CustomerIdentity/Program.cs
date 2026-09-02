using Microsoft.AspNetCore.Mvc;
using Shopizer.CustomerIdentity.Data;
using Shopizer.CustomerIdentity.Middleware;
using Shopizer.CustomerIdentity.Services;
using Shopizer.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddNpgsqlDataSource("customeridentitydb");
builder.AddRabbitMQClient("rabbitmq");
builder.Services.AddSingleton<IdentityRepository>();
builder.Services.AddSingleton<PasswordService>();
builder.Services.AddSingleton<TokenService>();
builder.Services.AddSingleton<EventPublisher>();
builder.Services.AddScoped<IdentityService>();
builder.Services.AddSingleton<SchemaInitializer>();
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
    });
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var errors = context.ModelState
            .Where(x => x.Value?.Errors.Count > 0)
            .SelectMany(x => x.Value!.Errors.Select(e => new { field = x.Key, message = string.IsNullOrWhiteSpace(e.ErrorMessage) ? "The value is invalid." : e.ErrorMessage }))
            .ToArray();
        return new ObjectResult(new
        {
            error = "VALIDATION_FAILED",
            message = "Request validation failed",
            statusCode = 422,
            timestamp = DateTimeOffset.UtcNow.ToString("O"),
            correlationId = context.HttpContext.Response.Headers["x-correlation-id"].FirstOrDefault(),
            details = errors
        }) { StatusCode = 422 };
    };
});

var app = builder.Build();
await app.Services.GetRequiredService<SchemaInitializer>().InitializeAsync(CancellationToken.None);
app.UseMiddleware<CorrelationMiddleware>();
app.UseMiddleware<ErrorMiddleware>();
app.UseMiddleware<TokenMiddleware>();
app.MapControllers();
app.MapDefaultEndpoints();
app.Run();
