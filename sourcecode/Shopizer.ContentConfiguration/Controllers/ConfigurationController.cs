using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Shopizer.ContentConfiguration.Data;
using Shopizer.ContentConfiguration.DTOs;
using Shopizer.ContentConfiguration.Middleware;
using Shopizer.ContentConfiguration.Models;
using Shopizer.ContentConfiguration.Services;

namespace Shopizer.ContentConfiguration.Controllers;

[ApiController]
[Route("api/v1")]
public sealed class ConfigurationController(ConfigurationService service, ContentRepository repository) : ControllerBase
{
    [HttpGet("config")]
    public async Task<IActionResult> Public(CancellationToken ct = default) =>
        Ok(await service.GetPublicAsync(HttpIdentity.Context(HttpContext), ct));

    [HttpGet("private/configuration")]
    public async Task<IActionResult> Merchant(CancellationToken ct = default)
    {
        RequireAdmin(); var ctx = HttpIdentity.Context(HttpContext);
        var raw = await repository.GetRawConfigurationAsync("CONFIG", ctx, ct);
        if (string.IsNullOrWhiteSpace(raw)) throw new DomainException("CONFIGURATION_NOT_FOUND", "CONFIG was not found for this store.", 404);
        try { return Ok(JsonDocument.Parse(raw).RootElement); }
        catch (JsonException) { throw new DomainException("CONFIGURATION_PARSE_ERROR", "Cannot parse merchant configuration JSON.", 422); }
    }

    [HttpPut("private/configuration")]
    public async Task<IActionResult> SaveMerchant([FromBody] JsonElement body, CancellationToken ct = default)
    {
        RequireAdmin(); return Ok(await service.SaveMerchantConfigAsync(body, HttpIdentity.Context(HttpContext), ct));
    }

    [HttpGet("private/configurations/{key}")]
    public async Task<IActionResult> GetRecord(string key, CancellationToken ct = default)
    {
        RequireAdmin(); var record = await repository.GetConfigurationRecordAsync(key, HttpIdentity.Context(HttpContext), ct);
        if (record is null) throw new DomainException("CONFIGURATION_NOT_FOUND", $"Configuration {key} was not found.", 404);
        return Ok(new
        {
            id = record.Value.Id,
            key,
            type = TitleType(record.Value.Type),
            active = record.Value.Active,
            value = (object?)null,
            valueState = record.Value.Type == "INTEGRATION" ? "Encrypted" :
                (string.IsNullOrWhiteSpace(record.Value.Value) ? "Absent" : "Present")
        });
    }

    [HttpPut("private/configurations/{key}")]
    public async Task<IActionResult> SaveRecord(string key, [FromBody] JsonElement body, CancellationToken ct = default)
    {
        RequireAdmin(); var type = JsonValue.String(body, "type")?.ToUpperInvariant() ?? "INTEGRATION";
        if (type is not ("INTEGRATION" or "SHOP" or "CONFIG" or "SOCIAL"))
            throw new DomainException("INVALID_REQUEST", "Invalid merchant configuration type.", 400);
        var active = JsonValue.Bool(body, "active");
        var value = body.TryGetProperty("value", out var valueElement) ? valueElement.GetRawText() : "{}";
        if (type == "INTEGRATION") value = new ConfigurationProtector(HttpContext.RequestServices.GetRequiredService<IConfiguration>()).Encrypt(value);
        var ctx = HttpIdentity.Context(HttpContext);
        await repository.SaveConfigurationAsync(key, type, active, value, ctx, ct);
        var result = await repository.GetConfigurationRecordAsync(key, ctx, ct) ??
            throw new DomainException("CONFIGURATION_UNAVAILABLE", "Configuration could not be read after save.", 503);
        return Ok(new
        {
            id = result.Id,
            key,
            type = TitleType(result.Type),
            active = result.Active,
            value = (object?)null,
            valueState = type == "INTEGRATION" ? "Encrypted" : "Present"
        });
    }

    [HttpGet("private/modules/payment")]
    public async Task<IActionResult> PaymentModules(CancellationToken ct = default)
    {
        RequireAdmin(); return Ok(await service.DiscoverAsync("PAYMENT", HttpIdentity.Context(HttpContext), ct));
    }

    [HttpGet("private/modules/payment/{code}")]
    public async Task<IActionResult> PaymentModule(string code, CancellationToken ct = default)
    {
        RequireAdmin(); return Ok(await service.DetailAsync("PAYMENT", code, HttpIdentity.Context(HttpContext), ct));
    }

    [HttpPut("private/modules/payment/{code}")]
    public async Task<IActionResult> SavePayment(string code, [FromBody] ModuleConfigurationRequestDto request, CancellationToken ct = default)
    {
        RequireAdmin(); return Ok(await SaveModule("PAYMENT", code, request, ct));
    }

    [HttpGet("private/modules/shipping")]
    public async Task<IActionResult> ShippingModules(CancellationToken ct = default)
    {
        RequireAdmin(); return Ok(await service.DiscoverAsync("SHIPPING", HttpIdentity.Context(HttpContext), ct));
    }

    [HttpGet("private/modules/shipping/{code}")]
    public async Task<IActionResult> ShippingModule(string code, CancellationToken ct = default)
    {
        RequireAdmin(); return Ok(await service.DetailAsync("SHIPPING", code, HttpIdentity.Context(HttpContext), ct));
    }

    [HttpPut("private/modules/shipping/{code}")]
    public async Task<IActionResult> SaveShipping(string code, [FromBody] ModuleConfigurationRequestDto request, CancellationToken ct = default)
    {
        RequireAdmin(); return Ok(await SaveModule("SHIPPING", code, request, ct));
    }

    [HttpPost("services/private/system/module")]
    public async Task<IActionResult> ReplaceModule([FromBody] ModuleReplacementRequestDto request, CancellationToken ct = default)
    {
        HttpIdentity.RequireAdministrator(HttpContext);
        RequireIdempotency();
        return Ok(await service.ReplaceModuleAsync(request, ct));
    }

    [HttpGet("private/configurations/payment")]
    [HttpPost("private/configurations/payment")]
    [HttpGet("private/configurations/shipping")]
    [HttpPost("private/configurations/shipping")]
    [HttpPost("services/private/system/optin")]
    [HttpDelete("services/private/system/optin/{code}")]
    [HttpPost("services/private/system/optin/{code}/customer")]
    public IActionResult Retired() => throw new DomainException("LEGACY_OPERATION_RETIRED",
        "This legacy operation was explicitly nonfunctional and is not part of the target contract.", 410);

    private async Task<object> SaveModule(string family, string code, ModuleConfigurationRequestDto request, CancellationToken ct)
    {
        var ctx = HttpIdentity.Context(HttpContext);
        var module = (await repository.ListModulesAsync(family, ct)).FirstOrDefault(x => x.Code.Equals(code, StringComparison.OrdinalIgnoreCase))
            ?? throw new DomainException("MODULE_NOT_FOUND", $"Module {code} is not available.", 404);
        service.EnsureAvailable(module);
        var state = await service.SaveModuleAsync(family, code, request, module, ctx, ct);
        return new
        {
            code,
            active = state.Active,
            configured = true,
            defaultSelected = state.DefaultSelected,
            requiredKeys = new List<string>(),
            integrationKeys = state.Keys.ToDictionary(x => x.Key, _ => (object?)null),
            integrationOptions = state.Options,
            environment = state.Environment,
            secretsPresent = state.Keys.Count > 0
        };
    }

    private void RequireAdmin() => HttpIdentity.RequireAdministrator(HttpContext);
    private void RequireIdempotency()
    {
        if (string.IsNullOrWhiteSpace(Request.Headers["Idempotency-Key"]))
            throw new DomainException("INVALID_REQUEST", "Idempotency-Key is required.", 400);
    }
    private static string TitleType(string value) => value.ToUpperInvariant() switch
    {
        "CONFIG" => "Config",
        "SHOP" => "Shop",
        "SOCIAL" => "Social",
        _ => "Integration"
    };
}
