using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Shopizer.PlatformIntegrations.Models;

namespace Shopizer.PlatformIntegrations.Middleware;

public sealed record TokenData(Guid SubjectId, string Kind, string Login, string TenantId, string StoreId, IReadOnlyList<string> Roles);

public sealed class TokenMiddleware(RequestDelegate next, IConfiguration configuration, IHostEnvironment environment)
{
    private readonly byte[] secret = Encoding.UTF8.GetBytes(configuration["PlatformIntegrations:JwtSecret"] ??
        (environment.IsDevelopment() ? "development-ms12-secret-development-ms12-secret" :
            throw new InvalidOperationException("PlatformIntegrations:JwtSecret must be configured outside Development.")));

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Headers.Authorization.ToString() is { Length: > 7 } value &&
            value.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                var token = Validate(value[7..].Trim(), RequestContext.From(context));
                if (token is not null)
                {
                    var claims = new List<Claim>
                    {
                        new(ClaimTypes.NameIdentifier, token.SubjectId.ToString()), new("sub", token.SubjectId.ToString()),
                        new(ClaimTypes.Name, token.Login), new("kind", token.Kind),
                        new("tenantId", token.TenantId), new("storeId", token.StoreId)
                    };
                    claims.AddRange(token.Roles.Select(x => new Claim(ClaimTypes.Role, x)));
                    context.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Bearer"));
                }
            }
            catch (Exception) { /* Authorization is enforced by the controller; malformed tokens remain anonymous. */ }
        }
        await next(context);
    }

    private TokenData? Validate(string raw, RequestContext ctx)
    {
        try
        {
            var parts = raw.Split('.');
            if (parts.Length != 3) return null;
            using var hmac = new HMACSHA512(secret);
            var signature = hmac.ComputeHash(Encoding.UTF8.GetBytes($"{parts[0]}.{parts[1]}"));
            if (!CryptographicOperations.FixedTimeEquals(signature, Decode(parts[2]))) return null;
            using var json = JsonDocument.Parse(Decode(parts[1]));
            var root = json.RootElement;
            if (root.GetProperty("aud").GetString() != "api") return null;
            var tenant = root.GetProperty("tenantId").GetString()!;
            var store = root.GetProperty("storeId").GetString()!;
            if (!tenant.Equals(ctx.TenantId, StringComparison.Ordinal) || !store.Equals(ctx.StoreId, StringComparison.Ordinal) ||
                DateTimeOffset.FromUnixTimeSeconds(root.GetProperty("exp").GetInt64()) <= DateTimeOffset.UtcNow) return null;
            var roles = root.TryGetProperty("roles", out var roleJson) ? roleJson.EnumerateArray().Select(x => x.GetString() ?? "").ToArray() : [];
            return new TokenData(Guid.Parse(root.GetProperty("sub").GetString()!), root.GetProperty("kind").GetString()!,
                root.GetProperty("name").GetString()!, tenant, store, roles);
        }
        catch (Exception) { return null; }
    }

    private static byte[] Decode(string value) => Convert.FromBase64String(value.Replace('-', '+').Replace('_', '/') +
        new string('=', (4 - value.Length % 4) % 4));
}
