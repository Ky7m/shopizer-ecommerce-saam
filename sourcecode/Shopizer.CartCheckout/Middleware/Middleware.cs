using System.Security.Claims;
using Shopizer.CartCheckout.DTOs;
using Shopizer.CartCheckout.Models;

namespace Shopizer.CartCheckout.Middleware;

public sealed class ErrorMiddleware(RequestDelegate next, ILogger<ErrorMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try { await next(context); }
        catch (DomainException ex) { await WriteAsync(context, ex.StatusCode, ex.Code, ex.Message); }
        catch (FormatException) { await WriteAsync(context, 400, "INVALID_REQUEST", "A route identifier is invalid"); }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Cart Checkout downstream request failed.");
            await WriteAsync(context, 503, "CHECKOUT_UNAVAILABLE", "A required downstream service is unavailable");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled Cart Checkout failure.");
            await WriteAsync(context, 500, "INTERNAL_ERROR", "Internal server error");
        }
    }

    private static async Task WriteAsync(HttpContext context, int status, string code, string message)
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

public sealed class TokenMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Headers.Authorization.ToString() is { Length: > 7 } authorization &&
            authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            // MS-01 owns token validation. This non-rejecting projection preserves the
            // principal shape for callers while invalid tokens are rejected by actions.
            var parts = authorization[7..].Split('.');
            if (parts.Length == 3)
            {
                try
                {
                    var payload = parts[1].Replace('-', '+').Replace('_', '/') + new string('=', (4 - parts[1].Length % 4) % 4);
                    using var json = System.Text.Json.JsonDocument.Parse(Convert.FromBase64String(payload));
                    var root = json.RootElement;
                    var claims = new List<Claim>();
                    foreach (var name in new[] { "sub", "name", "kind", "tenantId", "storeId" })
                        if (root.TryGetProperty(name, out var value)) claims.Add(new Claim(name, value.ToString()));
                    if (root.TryGetProperty("sub", out var subject)) claims.Add(new Claim(ClaimTypes.NameIdentifier, subject.ToString()));
                    if (root.TryGetProperty("name", out var login)) claims.Add(new Claim(ClaimTypes.Name, login.ToString()));
                    if (root.TryGetProperty("roles", out var roles) && roles.ValueKind == System.Text.Json.JsonValueKind.Array)
                        claims.AddRange(roles.EnumerateArray().Select(x => new Claim(ClaimTypes.Role, x.ToString())));
                    context.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Bearer"));
                }
                catch (Exception) { /* non-rejecting by contract */ }
            }
        }
        await next(context);
    }
}

public static class HttpIdentity
{
    public static RequestContext Context(HttpContext http) => RequestContext.From(http);
    public static Guid RequireSubject(HttpContext http, string kind, params string[] roles)
    {
        if (http.User.Identity?.IsAuthenticated != true || !http.User.Kind().Equals(kind, StringComparison.OrdinalIgnoreCase))
            throw new DomainException("AUTHENTICATION_REQUIRED", "Authentication token is invalid", 401);
        if (roles.Length > 0 && !http.User.Claims.Any(c => c.Type == ClaimTypes.Role && roles.Contains(c.Value, StringComparer.OrdinalIgnoreCase)))
            throw new DomainException("FORBIDDEN", "The authenticated principal is not authorized", 403);
        return http.User.SubjectId() ?? throw new DomainException("AUTHENTICATION_REQUIRED", "Authentication token is invalid", 401);
    }
}
