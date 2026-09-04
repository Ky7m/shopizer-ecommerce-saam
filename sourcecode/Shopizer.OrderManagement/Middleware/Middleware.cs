using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Filters;
using Shopizer.OrderManagement.DTOs;
using Shopizer.OrderManagement.Models;

namespace Shopizer.OrderManagement.Middleware;

public sealed class ErrorMiddleware(RequestDelegate next, ILogger<ErrorMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try { await next(context); }
        catch (DomainException ex) { await WriteAsync(context, ex.StatusCode, ex.Code, ex.Message); }
        catch (FormatException) { await WriteAsync(context, 400, "INVALID_REQUEST", "A route identifier is invalid"); }
        catch (System.Net.Http.HttpRequestException ex) { logger.LogError(ex, "Order Management downstream boundary failed."); await WriteAsync(context, 503, "DEPENDENCY_UNAVAILABLE", "A required downstream service is unavailable"); }
        catch (Exception ex) { logger.LogError(ex, "Unhandled Order Management failure."); await WriteAsync(context, 500, "INTERNAL_ERROR", "Internal server error"); }
    }

    private static async Task WriteAsync(HttpContext context, int status, string code, string message)
    {
        if (context.Response.HasStarted) return;
        var correlation = context.Request.Headers["x-correlation-id"].FirstOrDefault();
        context.Response.Headers["x-correlation-id"] = correlation ?? "";
        context.Response.StatusCode = status;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(new ErrorResponseDto
        {
            Error = code, Message = message, StatusCode = status, Timestamp = DateTimeOffset.UtcNow.ToString("O"), CorrelationId = correlation
        });
    }
}

public sealed class TokenMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var authorization = context.Request.Headers.Authorization.ToString();
        if (authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                var pieces = authorization[7..].Trim().Split('.');
                if (pieces.Length == 3)
                {
                    using var document = JsonDocument.Parse(Encoding.UTF8.GetString(FromBase64Url(pieces[1])));
                    var root = document.RootElement;
                    var request = RequestContext.From(context);
                    if (root.GetProperty("aud").GetString() == "api" &&
                        root.GetProperty("tenantId").GetString() == request.TenantId &&
                        root.GetProperty("storeId").GetString() == request.StoreId &&
                        DateTimeOffset.FromUnixTimeSeconds(root.GetProperty("exp").GetInt64()) > DateTimeOffset.UtcNow)
                    {
                        var claims = new List<Claim>();
                        foreach (var name in new[] { "sub", "name", "kind", "tenantId", "storeId" })
                            if (root.TryGetProperty(name, out var value)) claims.Add(new Claim(name, value.ToString()));
                        if (root.TryGetProperty("sub", out var subject)) claims.Add(new Claim(ClaimTypes.NameIdentifier, subject.ToString()));
                        if (root.TryGetProperty("name", out var nameClaim)) claims.Add(new Claim(ClaimTypes.Name, nameClaim.ToString()));
                        if (root.TryGetProperty("roles", out var roles) && roles.ValueKind == JsonValueKind.Array)
                            claims.AddRange(roles.EnumerateArray().Select(x => new Claim(ClaimTypes.Role, x.ToString())));
                        context.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Bearer"));
                    }
                }
            }
            catch (Exception) { /* Token parsing is non-rejecting; action authorization is authoritative. */ }
        }
        await next(context);
    }
    private static byte[] FromBase64Url(string value) => Convert.FromBase64String(value.Replace('-', '+').Replace('_', '/') + new string('=', (4 - value.Length % 4) % 4));
}

public static class HttpIdentity
{
    public static RequestContext Context(HttpContext http) => RequestContext.From(http);
    public static long RequireSubject(HttpContext http, string kind, params string[] roles)
    {
        if (http.User.Identity?.IsAuthenticated != true || !http.User.Kind().Equals(kind, StringComparison.OrdinalIgnoreCase))
            throw new DomainException("UNAUTHORIZED", "Authentication token is invalid", 401);
        if (roles.Length > 0 && !http.User.HasRole(roles))
            throw new DomainException("FORBIDDEN", "Order administration permission is required.", 403);
        return http.User.SubjectNumber() ?? throw new DomainException("UNAUTHORIZED", "Authentication token is invalid", 401);
    }
}

public sealed class ModelStateExceptionFilter : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (!context.ModelState.IsValid)
            throw new DomainException("VALIDATION_ERROR", "The request contains invalid fields.", 422);
        await next();
    }
}
