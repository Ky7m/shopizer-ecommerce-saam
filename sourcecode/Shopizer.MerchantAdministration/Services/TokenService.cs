using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Shopizer.MerchantAdministration.Models;

namespace Shopizer.MerchantAdministration.Services;

public sealed record TokenData(Guid SubjectId, string Kind, string Login, string TenantId, string StoreId, DateTimeOffset ExpiresAt, IReadOnlyList<string> Roles);

public sealed class TokenService(IConfiguration configuration, IHostEnvironment environment)
{
    private readonly string? _secret = configuration["MerchantAdministration:JwtSecret"];
    public TokenData? Validate(string raw, RequestContext context)
    {
        try
        {
            var pieces = raw.Split('.'); if (pieces.Length != 3) return null;
            if (!string.IsNullOrWhiteSpace(_secret)) { using var hmac = new HMACSHA512(Encoding.UTF8.GetBytes(_secret)); var expected = hmac.ComputeHash(Encoding.UTF8.GetBytes($"{pieces[0]}.{pieces[1]}")); if (!CryptographicOperations.FixedTimeEquals(expected, FromBase64Url(pieces[2]))) return null; }
            else if (!environment.IsDevelopment()) throw new InvalidOperationException("MerchantAdministration:JwtSecret must be configured outside Development.");
            using var json = JsonDocument.Parse(Encoding.UTF8.GetString(FromBase64Url(pieces[1]))); var root = json.RootElement; if (root.TryGetProperty("aud", out var aud) && aud.GetString() != "api") return null;
            var tenant = root.GetProperty("tenantId").GetString()!; var store = root.GetProperty("storeId").GetString()!; if (!tenant.Equals(context.TenantId, StringComparison.Ordinal) || (context.StoreId is not ("default" or "*") && !store.Equals(context.StoreId, StringComparison.OrdinalIgnoreCase))) return null;
            var expiry = DateTimeOffset.FromUnixTimeSeconds(root.GetProperty("exp").GetInt64()); if (expiry <= DateTimeOffset.UtcNow) return null; var roles = root.TryGetProperty("roles", out var rolesJson) ? rolesJson.EnumerateArray().Select(x => x.GetString()!).ToArray() : Array.Empty<string>(); return new TokenData(Guid.Parse(root.GetProperty("sub").GetString()!), root.GetProperty("kind").GetString()!, root.GetProperty("name").GetString()!, tenant, store, expiry, roles);
        }
        catch (JsonException) { return null; }
        catch (FormatException) { return null; }
        catch (KeyNotFoundException) { return null; }
    }
    private static byte[] FromBase64Url(string value) => Convert.FromBase64String(value.Replace('-', '+').Replace('_', '/') + new string('=', (4 - value.Length % 4) % 4));
}
