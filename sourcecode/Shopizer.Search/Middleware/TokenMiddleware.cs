using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Security.Claims;
using Shopizer.Search.Models;

namespace Shopizer.Search.Middleware;

public sealed class TokenMiddleware(RequestDelegate next, IConfiguration configuration, IHostEnvironment environment)
{
    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Headers.Authorization.ToString() is { Length: > 7 } authorization &&
            authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                var token = authorization[7..].Trim();
                var principal = Validate(token, RequestContext.From(context));
                if (principal is not null)
                {
                    context.User = principal;
                }
            }
            catch (DomainException)
            {
                // Authentication is deliberately non-rejecting. Actions decide whether it is required.
            }
            catch (Exception)
            {
                // A malformed bearer token must not prevent public search from running.
            }
        }

        await next(context);
    }

    private ClaimsPrincipal? Validate(string raw, RequestContext request)
    {
        var pieces = raw.Split('.');
        if (pieces.Length != 3)
        {
            return null;
        }

        using var json = JsonDocument.Parse(Encoding.UTF8.GetString(FromBase64Url(pieces[1])));
        var root = json.RootElement;
        var tenant = root.GetProperty("tenantId").GetString();
        var store = root.GetProperty("storeId").GetString();
        var expiry = DateTimeOffset.FromUnixTimeSeconds(root.GetProperty("exp").GetInt64());
        if (root.GetProperty("aud").GetString() != "api" ||
            tenant != request.TenantId || store != request.StoreId || expiry <= DateTimeOffset.UtcNow)
        {
            return null;
        }

        var secret = configuration["Search:JwtSecret"];
        if (!string.IsNullOrWhiteSpace(secret))
        {
            using var hmac = new HMACSHA512(Encoding.UTF8.GetBytes(secret));
            var expected = hmac.ComputeHash(Encoding.UTF8.GetBytes($"{pieces[0]}.{pieces[1]}"));
            if (!CryptographicOperations.FixedTimeEquals(expected, FromBase64Url(pieces[2])))
            {
                return null;
            }
        }
        else if (!environment.IsDevelopment())
        {
            return null;
        }

        var claims = root.EnumerateObject()
            .Where(p => p.Name is "sub" or "name" or "kind" or "tenantId" or "storeId")
            .Select(p => new Claim(p.Name, p.Value.GetString() ?? ""))
            .ToList();
        if (root.TryGetProperty("roles", out var roles))
        {
            claims.AddRange(roles.EnumerateArray().Select(role =>
                new Claim(ClaimTypes.Role, role.GetString() ?? "")));
        }
        return new ClaimsPrincipal(new ClaimsIdentity(claims, "Bearer"));
    }

    private static byte[] FromBase64Url(string value) =>
        Convert.FromBase64String(value.Replace('-', '+').Replace('_', '/') +
                                 new string('=', (4 - value.Length % 4) % 4));
}
