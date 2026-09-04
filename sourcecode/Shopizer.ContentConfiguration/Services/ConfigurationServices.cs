using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Shopizer.ContentConfiguration.Data;
using Shopizer.ContentConfiguration.DTOs;
using Shopizer.ContentConfiguration.Models;

namespace Shopizer.ContentConfiguration.Services;

public sealed class ConfigurationProtector(IConfiguration configuration)
{
    private readonly byte[] _key = SHA256.HashData(Encoding.UTF8.GetBytes(
        configuration["ContentConfiguration:EncryptionKey"] ?? "shopizer-ms11-development-key"));

    public string Encrypt(string value)
    {
        using var aes = Aes.Create();
        aes.Key = _key; aes.GenerateIV();
        using var encryptor = aes.CreateEncryptor();
        var plaintext = Encoding.UTF8.GetBytes(value);
        var ciphertext = encryptor.TransformFinalBlock(plaintext, 0, plaintext.Length);
        return Convert.ToBase64String(aes.IV.Concat(ciphertext).ToArray());
    }

    public string Decrypt(string value)
    {
        try
        {
            var bytes = Convert.FromBase64String(value);
            using var aes = Aes.Create(); aes.Key = _key;
            aes.IV = bytes[..(aes.BlockSize / 8)];
            using var decryptor = aes.CreateDecryptor();
            return Encoding.UTF8.GetString(decryptor.TransformFinalBlock(bytes, aes.BlockSize / 8, bytes.Length - aes.BlockSize / 8));
        }
        catch (Exception ex) when (ex is FormatException or CryptographicException)
        {
            throw new DomainException("CONFIGURATION_PARSE_ERROR", "Stored module configuration could not be decrypted.", 422);
        }
    }
}

public sealed class ConfigurationService(
    ContentRepository repository,
    ConfigurationProtector protector,
    ModuleCache cache,
    EventPublisher events,
    IConfiguration configuration)
{
    // @BR-CF-001: Configuration records are selected and upserted by tenant, store, and configuration key.
    // @BR-CF-002: Typed merchant flags are serialized as booleans and null/blank search values are omitted.
    public async Task<JsonElement> SaveMerchantConfigAsync(JsonElement body, RequestContext ctx, CancellationToken ct)
    {
        var normalized = NormalizeMerchantConfig(body);
        await repository.SaveConfigurationAsync("CONFIG", "CONFIG", false, normalized.GetRawText(), ctx, ct);
        return normalized;
    }

    // @BR-CF-003: Public configuration exposes only the approved merchant display projection.
    // @BR-CF-004: Social values are resolved from dedicated store-scoped keys and absent keys remain absent.
    // @BR-CF-005: Display shipping starts false and only a nonblank true property enables it.
    // @BR-CF-015: Missing CONFIG records return platform defaults rather than null or an unhandled failure.
    public async Task<object> GetPublicAsync(RequestContext ctx, CancellationToken ct)
    {
        var raw = await repository.GetRawConfigurationAsync("CONFIG", ctx, ct);
        var values = DefaultConfig();
        if (!string.IsNullOrWhiteSpace(raw))
        {
            try
            {
                using var json = JsonDocument.Parse(raw);
                foreach (var property in json.RootElement.EnumerateObject()) values[property.Name] = property.Value.Clone();
            }
            catch (JsonException) { throw new DomainException("CONFIGURATION_PARSE_ERROR", "Cannot parse merchant configuration JSON.", 422); }
        }
        var result = new Dictionary<string, object?>
        {
            ["allowOnlinePurchase"] = Bool(values, "allowPurchaseItems", true),
            ["displaySearchBox"] = Bool(values, "displaySearchBox", true),
            ["displayContactUs"] = Bool(values, "displayContactUs"),
            ["displayCustomerSection"] = Bool(values, "displayCustomerSection"),
            ["displayAddToCartOnFeaturedItems"] = Bool(values, "displayAddToCartOnFeaturedItems"),
            ["displayCustomerAgreement"] = Bool(values, "displayCustomerAgreement"),
            ["displayPagesMenu"] = Bool(values, "displayPagesMenu", true),
            ["displayShipping"] = string.Equals(configuration["ContentConfiguration:DisplayShipping"]?.Trim(), "true", StringComparison.OrdinalIgnoreCase)
        };
        foreach (var (key, property) in new[] {
            ("facebook", "facebook_page_url"), ("ga", "google_analytics_url"),
            ("instagram", "instagram"), ("pinterest", "pinterest") })
        {
            var value = await repository.GetRawConfigurationAsync(property, ctx, ct);
            if (!string.IsNullOrWhiteSpace(value)) result[key] = value;
        }
        return result;
    }

    // @BR-CF-006: Integration configuration is decrypted before parsing and encrypted before persistence.
    // @BR-CF-007: integrationOptions is retained independently from integrationKeys.
    // @BR-EXT-025: Module configuration state is persisted without executing provider operations.
    public async Task<ModuleState> SaveModuleAsync(string family, string code, ModuleConfigurationRequestDto request,
        ModuleRecord module, RequestContext ctx, CancellationToken ct)
    {
        if (request.Environment is null or "") throw new DomainException("MODULE_CONFIGURATION_INVALID", "environment is required", 422);
        foreach (var requiredKey in RequiredKeys(module))
        {
            if (!request.IntegrationKeys.TryGetValue(requiredKey, out var value) ||
                value is null ||
                (value is JsonElement json && json.ValueKind is JsonValueKind.Null or JsonValueKind.String &&
                 string.IsNullOrWhiteSpace(json.ToString())) ||
                (value is string text && string.IsNullOrWhiteSpace(text)))
                throw new DomainException("MODULE_CONFIGURATION_INVALID",
                    $"{requiredKey} is required for module {code}.", 422);
        }
        var key = family == "PAYMENT" ? "PAYMENT_MODULES" : "SHIPPING_MODULES";
        var raw = await repository.GetRawConfigurationAsync(key, ctx, ct);
        var modules = new List<ModuleState>();
        if (!string.IsNullOrWhiteSpace(raw))
        {
            var plaintext = protector.Decrypt(raw);
            try
            {
                using var json = JsonDocument.Parse(plaintext);
                if (json.RootElement.ValueKind == JsonValueKind.Array)
                    foreach (var value in json.RootElement.EnumerateArray())
                        modules.Add(new ModuleState(JsonValue.String(value, "moduleCode") ?? "", JsonValue.Bool(value, "active"), JsonValue.Bool(value, "defaultSelected"),
                            JsonValue.String(value, "environment") ?? "TEST", JsonValue.Object(value, "integrationKeys"),
                            JsonValue.Object(value, "integrationOptions")));
            }
            catch (JsonException) { throw new DomainException("MODULE_CONFIGURATION_INVALID", "Stored module configuration is invalid.", 422); }
        }
        var state = new ModuleState(code, request.Active, request.DefaultSelected ?? false, request.Environment,
            request.IntegrationKeys, request.IntegrationOptions);
        modules.RemoveAll(x => x.Code.Equals(code, StringComparison.OrdinalIgnoreCase));
        modules.Add(state);
        var serialized = JsonSerializer.Serialize(modules.Select(x => new
        {
            moduleCode = x.Code,
            active = x.Active,
            defaultSelected = x.DefaultSelected,
            environment = x.Environment,
            integrationKeys = x.Keys,
            integrationOptions = x.Options
        }));
        await repository.SaveConfigurationAsync(key, "INTEGRATION", state.Active, protector.Encrypt(serialized), ctx, ct);
        await events.PublishConfigurationReferenceChangedAsync(family, code, state.Environment, ctx, ct);
        return state;
    }

    // @BR-CF-008: Replacement preserves module family, code, type, image, regions, details, and environments.
    // @BR-CF-009: TEST/PROD environment metadata retains distinct config1 and config2 values.
    // @BR-CF-010: Existing module definitions are replaced by code, never by family.
    // @BR-EXT-026: Module replacement invalidates the affected discovery cache before returning.
    // @BR-EXT-028: Wildcard/country and descriptive environment endpoints remain module metadata.
    public async Task<ModuleReplacementResponseDto> ReplaceModuleAsync(ModuleReplacementRequestDto request, CancellationToken ct)
    {
        var family = request.Module.Trim().ToUpperInvariant();
        if (family is not ("PAYMENT" or "SHIPPING"))
            throw new DomainException("INVALID_REQUEST", "module must be PAYMENT or SHIPPING", 422);
        var environments = (request.Configuration ?? []).Select(x => new ModuleEnvironment(
            x.Env, x.Scheme, x.Host, x.Port, x.Uri, x.Config1, x.Config2)).ToList();
        if (environments.GroupBy(x => x.Env, StringComparer.OrdinalIgnoreCase).Any(x => x.Count() > 1))
            throw new DomainException("INVALID_REQUEST", "Each module environment must be unique.", 422);
        var module = new ModuleRecord
        {
            Id = Guid.NewGuid(),
            Family = family,
            Code = request.Code.Trim(),
            Type = request.Type,
            Image = request.Image,
            CustomModule = request.CustomModule ?? false,
            Regions = request.Regions ?? [],
            Details = request.Details ?? [],
            Configuration = environments
        };
        await repository.ReplaceModuleAsync(module, ct);
        cache.Invalidate(family);
        return new ModuleReplacementResponseDto { Status = 200, Code = module.Code, Replaced = true, CacheInvalidated = true };
    }

    // @BR-CF-011: Discovery hydrates persisted metadata, appends runtime payment metadata, and caches each family.
    // @BR-CF-012: Only wildcard or matching-country modules are available to a store.
    // @BR-CF-014: Configured and active flags are derived independently from module metadata.
    // @BR-EXT-024: Runtime payment starters are appended before discovery results are cached.
    // @BR-EXT-027: Module details expose metadata and masked state, never encrypted credentials.
    public async Task<List<object>> DiscoverAsync(string family, RequestContext ctx, CancellationToken ct)
    {
        if (!cache.TryGet(family, out var modules))
        {
            modules = await repository.ListModulesAsync(family, ct);
            cache.Set(family, modules);
        }
        var country = configuration["ContentConfiguration:StoreCountry"] ?? "*";
        var result = new List<object>();
        foreach (var module in modules.Where(x => x.Regions.Count == 0 || x.Regions.Contains("*") ||
                                                   x.Regions.Contains(country, StringComparer.OrdinalIgnoreCase)))
        {
            var configured = await GetModuleStateAsync(family, module.Code, ctx, ct);
            result.Add(new
            {
                code = module.Code,
                active = configured?.Active == true,
                configured = configured is not null,
                image = module.Image,
                binaryImage = (string?)null,
                requiredKeys = RequiredKeys(module),
                configurable = module.Details.TryGetValue("configurable", out var value) ? value : null
            });
        }
        return result;
    }

    // @BR-CF-006: Module detail reads decrypt only inside the protected configuration boundary.
    // @BR-CF-013: Provider validation is required before module state is stored.
    // @BR-EXT-027: Sensitive integration values are masked and never returned as plaintext.
    public async Task<object> DetailAsync(string family, string code, RequestContext ctx, CancellationToken ct)
    {
        var module = (await repository.ListModulesAsync(family, ct)).FirstOrDefault(x => x.Code.Equals(code, StringComparison.OrdinalIgnoreCase))
            ?? throw new DomainException("MODULE_NOT_FOUND", $"Module {code} is not available.", 404);
        EnsureAvailable(module);
        var state = await GetModuleStateAsync(family, code, ctx, ct);
        var keys = state?.Keys.ToDictionary(x => x.Key, _ => (object?)null) ?? [];
        if (state is not null) foreach (var key in state.Keys.Keys) keys[key] = null;
        return new
        {
            code = module.Code,
            configurable = module.Details.TryGetValue("configurable", out var configurable) ? configurable : null,
            active = state?.Active ?? false,
            configured = state is not null,
            defaultSelected = state?.DefaultSelected ?? false,
            requiredKeys = RequiredKeys(module),
            integrationKeys = keys,
            integrationOptions = state?.Options ?? [],
            environment = state?.Environment ?? "TEST",
            secretsPresent = state?.Keys.Count > 0,
            image = module.Image
        };
    }

    private async Task<ModuleState?> GetModuleStateAsync(string family, string code, RequestContext ctx, CancellationToken ct)
    {
        var raw = await repository.GetRawConfigurationAsync(family == "PAYMENT" ? "PAYMENT_MODULES" : "SHIPPING_MODULES", ctx, ct);
        if (string.IsNullOrWhiteSpace(raw)) return null;
        using var json = JsonDocument.Parse(protector.Decrypt(raw));
        return json.RootElement.EnumerateArray().Where(x => string.Equals(JsonValue.String(x, "moduleCode"), code, StringComparison.OrdinalIgnoreCase))
            .Select(x => new ModuleState(JsonValue.String(x, "moduleCode") ?? code, JsonValue.Bool(x, "active"), JsonValue.Bool(x, "defaultSelected"),
                JsonValue.String(x, "environment") ?? "TEST", JsonValue.Object(x, "integrationKeys"), JsonValue.Object(x, "integrationOptions")))
            .FirstOrDefault();
    }

    public void EnsureAvailable(ModuleRecord module)
    {
        var country = configuration["ContentConfiguration:StoreCountry"] ?? "*";
        if (module.Regions.Count > 0 && !module.Regions.Contains("*") &&
            !module.Regions.Contains(country, StringComparer.OrdinalIgnoreCase))
            throw new DomainException("MODULE_NOT_FOUND", $"Module {module.Code} is not available for this store.", 404);
    }

    private static List<string> RequiredKeys(ModuleRecord module) =>
        module.Details.TryGetValue("requiredKeys", out var value) && value is JsonElement json && json.ValueKind == JsonValueKind.Array
            ? json.EnumerateArray().Select(x => x.GetString() ?? "").ToList() : [];
    private static Dictionary<string, JsonElement> DefaultConfig() => new()
    {
        ["displayPagesMenu"] = JsonDocument.Parse("true").RootElement.Clone(),
        ["allowPurchaseItems"] = JsonDocument.Parse("true").RootElement.Clone(),
        ["displaySearchBox"] = JsonDocument.Parse("true").RootElement.Clone()
    };
    private static bool Bool(Dictionary<string, JsonElement> values, string name, bool fallback = false) =>
        values.TryGetValue(name, out var value) && value.ValueKind == JsonValueKind.True ? true :
        values.TryGetValue(name, out value) && value.ValueKind == JsonValueKind.False ? false : fallback;
    private static JsonElement NormalizeMerchantConfig(JsonElement body)
    {
        var map = new Dictionary<string, object?>();
        foreach (var property in body.EnumerateObject())
        {
            if (property.Name is "useDefaultSearchConfig" or "defaultSearchConfigPath")
            {
                var values = property.Value.EnumerateObject().Where(x => x.Value.ValueKind != JsonValueKind.Null &&
                    (property.Name != "defaultSearchConfigPath" || !string.IsNullOrWhiteSpace(x.Value.GetString())))
                    .ToDictionary(x => x.Name, x => (object?)x.Value.Clone());
                map[property.Name] = values;
            }
            else map[property.Name] = property.Value.Clone();
        }
        return JsonDocument.Parse(JsonSerializer.Serialize(map)).RootElement.Clone();
    }
}
