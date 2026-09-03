using System.Security.Claims;
using System.Text;
using System.Text.Json;

namespace Shopizer.CatalogProduct.Middleware;

public sealed class TokenMiddleware(RequestDelegate next, ILogger<TokenMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var value = context.Request.Headers.Authorization.ToString();
        if (value.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                var parts = value[7..].Trim().Split('.');
                if (parts.Length == 3)
                {
                    var json = Encoding.UTF8.GetString(Convert.FromBase64String(parts[1].Replace('-', '+').Replace('_', '/') +
                        new string('=', (4 - parts[1].Length % 4) % 4)));
                    using var document = JsonDocument.Parse(json);
                    var root = document.RootElement;
                    var claims = new List<Claim>();
                    var sub = root.TryGetProperty("sub", out var subValue) ? subValue.GetString() : null;
                    if (Guid.TryParse(sub, out _))
                    {
                        claims.Add(new(ClaimTypes.NameIdentifier, sub!));
                        claims.Add(new("sub", sub!));
                    }
                    claims.Add(new(ClaimTypes.Name, root.TryGetProperty("name", out var name) ? name.GetString() ?? "operator" : "operator"));
                    claims.Add(new("kind", root.TryGetProperty("kind", out var kind) ? kind.GetString() ?? "administrator" : "administrator"));
                    claims.Add(new("tenantId", context.Request.Headers["x-tenant-id"].FirstOrDefault() ?? ""));
                    claims.Add(new("storeId", context.Request.Headers["x-store-id"].FirstOrDefault() ?? ""));
                    if (root.TryGetProperty("roles", out var roles) && roles.ValueKind == JsonValueKind.Array)
                        claims.AddRange(roles.EnumerateArray().Select(r => new Claim(ClaimTypes.Role, r.GetString() ?? "")));
                    context.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Bearer"));
                }
            }
            catch (Exception ex) when (ex is FormatException or JsonException)
            {
                logger.LogDebug(ex, "Bearer token could not be parsed; action authorization will reject it.");
            }
        }
        await next(context);
    }
}
