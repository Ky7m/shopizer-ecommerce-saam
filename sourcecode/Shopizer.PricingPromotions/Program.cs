using Microsoft.AspNetCore.Mvc;
using Shopizer.PricingPromotions.Data;
using Shopizer.PricingPromotions.DTOs;
using Shopizer.PricingPromotions.Middleware;
using Shopizer.PricingPromotions.Services;
using Shopizer.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddNpgsqlDataSource("shopizerDb");
builder.AddRabbitMQClient("rabbitmq");
builder.Services.AddSingleton<PricingRepository>();
builder.Services.AddSingleton<EventPublisher>();
builder.Services.AddSingleton<TokenService>();
builder.Services.AddScoped<PricingService>();
builder.Services.AddSingleton<SchemaInitializer>();
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
    })
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            var errors = context.ModelState
                .Where(x => x.Value?.Errors.Count > 0)
                .SelectMany(x => x.Value!.Errors.Select(error => new
                {
                    field = x.Key,
                    message = string.IsNullOrWhiteSpace(error.ErrorMessage)
                        ? "The supplied value is invalid."
                        : error.ErrorMessage
                }))
                .ToArray();
            return new BadRequestObjectResult(new ErrorResponseDto
            {
                Error = "INVALID_REQUEST",
                Message = "The request is invalid.",
                StatusCode = StatusCodes.Status400BadRequest,
                Timestamp = DateTimeOffset.UtcNow.ToString("O"),
                Details = errors
            });
        };
    });

var app = builder.Build();
await app.Services.GetRequiredService<SchemaInitializer>().InitializeAsync(CancellationToken.None);
app.UseMiddleware<ErrorMiddleware>();
app.UseMiddleware<TokenMiddleware>();
app.MapControllers();

app.MapDefaultEndpoints();

app.Run();
