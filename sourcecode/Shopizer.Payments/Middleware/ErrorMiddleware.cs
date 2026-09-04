using Npgsql;
using Shopizer.Payments.DTOs;
using Shopizer.Payments.Models;

namespace Shopizer.Payments.Middleware;

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
            await WriteError(context, 400, "INVALID_REQUEST", "A route identifier is invalid");
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.CheckViolation)
        {
            logger.LogWarning(ex, "Payments database constraint rejected a request.");
            await WriteError(context, 422, "PAYMENT_VALIDATION_FAILED", "The payment request violates a business constraint");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled payments failure");
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
