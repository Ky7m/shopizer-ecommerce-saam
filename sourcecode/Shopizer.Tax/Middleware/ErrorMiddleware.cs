using System.Text.Json;
using Shopizer.Tax.DTOs;
using Shopizer.Tax.Models;

namespace Shopizer.Tax.Middleware;

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
            await WriteErrorAsync(context, ex.StatusCode, ex.Code, ex.Message);
        }
        catch (FormatException)
        {
            await WriteErrorAsync(context, 400, "INVALID_REQUEST", "A route identifier is invalid");
        }
        catch (JsonException)
        {
            await WriteErrorAsync(context, 400, "INVALID_REQUEST", "The request body is not valid JSON");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled tax service failure");
            await WriteErrorAsync(context, 500, "INTERNAL_ERROR", "Internal server error");
        }
    }

    private static async Task WriteErrorAsync(HttpContext context, int status, string code, string message)
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
