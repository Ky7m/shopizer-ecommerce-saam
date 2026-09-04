using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Shopizer.Tax.DTOs;
using Shopizer.Tax.Middleware;
using Shopizer.Tax.Models;
using Shopizer.Tax.Services;

namespace Shopizer.Tax.Controllers;

[ApiController]
[Route("api/v1")]
public sealed class TaxController(TaxService service) : ControllerBase
{
    [HttpPost("tax-classes")]
    public async Task<ActionResult<TaxClassDto>> CreateTaxClass(CreateTaxClassRequestDto request, CancellationToken ct)
    {
        HttpIdentity.RequireAdministrator(HttpContext);
        if (request is null) throw new DomainException("INVALID_REQUEST", "Tax-class request body is required", 400);
        return StatusCode(201, await service.CreateTaxClassAsync(request, HttpIdentity.Context(HttpContext), ct));
    }

    [HttpGet("tax-classes")]
    public async Task<TaxClassListResponseDto> ListTaxClasses([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        HttpIdentity.RequireAuthenticated(HttpContext);
        return await service.ListTaxClassesAsync(page, pageSize, HttpIdentity.Context(HttpContext), ct);
    }

    [HttpGet("tax-classes/{id:guid}")]
    public async Task<TaxClassDto> GetTaxClass(Guid id, CancellationToken ct)
    {
        HttpIdentity.RequireAuthenticated(HttpContext);
        return await service.GetTaxClassAsync(id, HttpIdentity.Context(HttpContext), ct);
    }

    [HttpPut("tax-classes/{id:guid}")]
    public async Task<TaxClassDto> UpdateTaxClass(Guid id, UpdateTaxClassRequestDto request, CancellationToken ct)
    {
        HttpIdentity.RequireAdministrator(HttpContext);
        if (request is null) throw new DomainException("INVALID_REQUEST", "Tax-class request body is required", 400);
        return await service.UpdateTaxClassAsync(id, request, HttpIdentity.Context(HttpContext), ct);
    }

    [HttpDelete("tax-classes/{id:guid}")]
    public async Task<DeleteResponseDto> DeleteTaxClass(Guid id, CancellationToken ct)
    {
        HttpIdentity.RequireAdministrator(HttpContext);
        return await service.DeleteTaxClassAsync(id, HttpIdentity.Context(HttpContext), ct);
    }

    [HttpGet("tax-classes/exists")]
    public async Task<ExistsResponseDto> TaxClassExists([FromQuery] string? code, CancellationToken ct)
    {
        HttpIdentity.RequireAuthenticated(HttpContext);
        return await service.TaxClassExistsAsync(code, HttpIdentity.Context(HttpContext), ct);
    }

    [HttpPost("tax-rates")]
    public async Task<ActionResult<TaxRateDto>> CreateTaxRate(CreateTaxRateRequestDto request, CancellationToken ct)
    {
        HttpIdentity.RequireAdministrator(HttpContext);
        if (request is null) throw new DomainException("INVALID_REQUEST", "Tax-rate request body is required", 400);
        return StatusCode(201, await service.CreateTaxRateAsync(request, HttpIdentity.Context(HttpContext), ct));
    }

    [HttpGet("tax-rates")]
    public async Task<TaxRateListResponseDto> ListTaxRates(
        [FromQuery] string? languageCode = "en", [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        HttpIdentity.RequireAuthenticated(HttpContext);
        return await service.ListTaxRatesAsync(languageCode, page, pageSize, HttpIdentity.Context(HttpContext), ct);
    }

    [HttpGet("tax-rates/{id:guid}")]
    public async Task<TaxRateDto> GetTaxRate(Guid id, CancellationToken ct)
    {
        HttpIdentity.RequireAuthenticated(HttpContext);
        return await service.GetTaxRateAsync(id, HttpIdentity.Context(HttpContext), ct);
    }

    [HttpPut("tax-rates/{id:guid}")]
    public async Task<TaxRateDto> UpdateTaxRate(Guid id, [FromBody] JsonElement body, CancellationToken ct)
    {
        HttpIdentity.RequireAdministrator(HttpContext);
        if (body.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
            throw new DomainException("INVALID_REQUEST", "Tax-rate request body is required", 400);
        var request = JsonSerializer.Deserialize<CreateTaxRateRequestDto>(
                          body.GetRawText(),
                          new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                      ?? throw new DomainException("INVALID_REQUEST", "Tax-rate request body is required", 400);
        return await service.UpdateTaxRateAsync(id, request, HttpIdentity.Context(HttpContext), ct);
    }

    [HttpDelete("tax-rates/{id:guid}")]
    public async Task<DeleteResponseDto> DeleteTaxRate(Guid id, CancellationToken ct)
    {
        HttpIdentity.RequireAdministrator(HttpContext);
        return await service.DeleteTaxRateAsync(id, HttpIdentity.Context(HttpContext), ct);
    }

    [HttpGet("tax-rates/exists")]
    public async Task<ExistsResponseDto> TaxRateExists([FromQuery] string? code, CancellationToken ct)
    {
        HttpIdentity.RequireAuthenticated(HttpContext);
        return await service.TaxRateExistsAsync(code, HttpIdentity.Context(HttpContext), ct);
    }

    [HttpGet("tax-configuration")]
    public async Task<TaxConfigurationDto> GetConfiguration(CancellationToken ct)
    {
        HttpIdentity.RequireAuthenticated(HttpContext);
        return await service.GetConfigurationAsync(HttpIdentity.Context(HttpContext), ct);
    }

    [HttpPut("tax-configuration")]
    public async Task<TaxConfigurationDto> SaveConfiguration(UpdateTaxConfigurationRequestDto request, CancellationToken ct)
    {
        HttpIdentity.RequireAdministrator(HttpContext);
        if (request is null) throw new DomainException("INVALID_REQUEST", "Tax-configuration request body is required", 400);
        return await service.SaveConfigurationAsync(request, HttpIdentity.Context(HttpContext), ct);
    }

    [HttpPost("tax-calculations")]
    public async Task<TaxCalculationResponseDto> Calculate(CalculateTaxRequestDto request, CancellationToken ct)
    {
        if (request is null) throw new DomainException("INVALID_REQUEST", "Tax-calculation request body is required", 400);
        return await service.CalculateAsync(request, HttpIdentity.Context(HttpContext), ct);
    }
}
