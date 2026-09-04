using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Shopizer.Tax.Models;

namespace Shopizer.Tax.Services;

public sealed record TokenData(Guid SubjectId, string Kind, string TenantId, string StoreId);

public sealed class TokenService(IConfiguration configuration, IHostEnvironment environment)
{
    private readonly byte[] secret = CreateSecret(configuration, environment);

    private static byte[] CreateSecret(IConfiguration configuration, IHostEnvironment environment)
    {
        var configured = configuration["Tax:JwtSecret"] ?? configuration["CustomerIdentity:JwtSecret"];
        if (!string.IsNullOrWhiteSpace(configured)) return Encoding.UTF8.GetBytes(configured);
        if (!environment.IsDevelopment())
            throw new InvalidOperationException("Tax:JwtSecret must be configured outside Development.");
        return RandomNumberGenerator.GetBytes(64);
    }

    public Task<TokenData?> ValidateAsync(string raw, RequestContext context, CancellationToken ct)
    {
        try
        {
            var pieces = raw.Split('.');
            if (pieces.Length != 3) return Task.FromResult<TokenData?>(null);
            using var hmac = new HMACSHA512(secret);
            var expected = hmac.ComputeHash(Encoding.UTF8.GetBytes($"{pieces[0]}.{pieces[1]}"));
            if (!CryptographicOperations.FixedTimeEquals(expected, FromBase64Url(pieces[2])))
                return Task.FromResult<TokenData?>(null);

            using var document = JsonDocument.Parse(Encoding.UTF8.GetString(FromBase64Url(pieces[1])));
            var root = document.RootElement;
            if (root.GetProperty("aud").GetString() != "api") return Task.FromResult<TokenData?>(null);
            var expiry = DateTimeOffset.FromUnixTimeSeconds(root.GetProperty("exp").GetInt64());
            if (expiry <= DateTimeOffset.UtcNow) return Task.FromResult<TokenData?>(null);
            var tenant = root.GetProperty("tenantId").GetString() ?? "";
            var store = root.GetProperty("storeId").GetString() ?? "";
            if (!string.Equals(tenant, context.TenantId, StringComparison.Ordinal) ||
                !string.Equals(store, context.StoreId, StringComparison.Ordinal))
                return Task.FromResult<TokenData?>(null);
            var kind = root.GetProperty("kind").GetString() ?? "";
            if (!Guid.TryParse(root.GetProperty("sub").GetString(), out var subject) ||
                (kind != "customer" && kind != "administrator"))
                return Task.FromResult<TokenData?>(null);
            return Task.FromResult<TokenData?>(new TokenData(subject, kind, tenant, store));
        }
        catch (FormatException) { return Task.FromResult<TokenData?>(null); }
        catch (JsonException) { return Task.FromResult<TokenData?>(null); }
        catch (KeyNotFoundException) { return Task.FromResult<TokenData?>(null); }
        catch (CryptographicException) { return Task.FromResult<TokenData?>(null); }
    }

    private static byte[] FromBase64Url(string value) =>
        Convert.FromBase64String(value.Replace('-', '+').Replace('_', '/') +
                                 new string('=', (4 - value.Length % 4) % 4));
}
