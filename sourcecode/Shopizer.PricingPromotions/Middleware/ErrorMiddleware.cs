using Shopizer.PricingPromotions.DTOs;
using Shopizer.PricingPromotions.Models;

namespace Shopizer.PricingPromotions.Middleware;

public sealed class ErrorMiddleware(RequestDelegate next, ILogger<ErrorMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (DomainException ex)
        {
            await WriteError(context, ex.StatusCode, ex.Code, ex.Message);
        }
        catch (FormatException)
        {
            await WriteError(context, 400, "INVALID_REQUEST", "A route identifier or date is invalid.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled pricing and promotions failure");
            await WriteError(context, 500, "INTERNAL_ERROR", "Internal server error");
        }
    }

    private static async Task WriteError(HttpContext context, int status, string code, string message)
    {
        if (context.Response.HasStarted) return;
        context.Response.StatusCode = status;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(new ErrorResponseDto
        {
            Error = code,
            Message = message,
            StatusCode = status,
            Timestamp = DateTimeOffset.UtcNow.ToString("O"),
            CorrelationId = context.Request.Headers["x-correlation-id"].FirstOrDefault()
        });
    }
}
