using Microsoft.AspNetCore.Mvc;
using Shopizer.Shipping.DTOs;
using Shopizer.Shipping.Middleware;
using Shopizer.Shipping.Models;
using Shopizer.Shipping.Services;

namespace Shopizer.Shipping.Controllers;

[ApiController]
[Route("api/v1")]
public sealed class ShippingController(ShippingService service) : ControllerBase
{
    private static readonly string[] ShippingRoles = ["SUPERADMIN", "ADMIN", "SHIPPING", "ADMIN_RETAIL"];

    [HttpGet("auth/cart/{cart}/shipping")]
    public async Task<ShippingSummaryResult> GetAuthenticatedCartShipping(string cart, [FromQuery] string? lang,
        CancellationToken ct)
    {
        HttpIdentity.RequireSubject(HttpContext, "customer");
        var context = HttpIdentity.Context(HttpContext);
        return await service.CalculateAsync(cart, new ShippingAddressRequestDto
        {
            CountryCode = HttpContext.Request.Query["countryCode"].FirstOrDefault() ?? "",
            PostalCode = HttpContext.Request.Query["postalCode"].FirstOrDefault() ?? "",
            Address = HttpContext.Request.Query["address"].FirstOrDefault(),
            City = HttpContext.Request.Query["city"].FirstOrDefault(),
            State = HttpContext.Request.Query["state"].FirstOrDefault(),
            ZoneCode = HttpContext.Request.Query["zoneCode"].FirstOrDefault()
        }, context, HttpContext.Connection.RemoteIpAddress?.ToString(), ct);
    }

    [HttpPost("cart/{cart}/shipping")]
    public async Task<ShippingSummaryResult> CalculateCartShipping(string cart,
        [FromBody] ShippingAddressRequestDto request, CancellationToken ct)
    {
        var context = HttpIdentity.Context(HttpContext);
        return await service.CalculateAsync(cart, request, context,
            HttpContext.Connection.RemoteIpAddress?.ToString(), ct);
    }

    [HttpGet("private/configurations/shipping")]
    public async Task<object> GetShippingConfiguration(CancellationToken ct)
    {
        RequireAdmin();
        return ToConfiguration(await service.GetConfigurationAsync(HttpIdentity.Context(HttpContext), ct));
    }

    [HttpGet("private/modules/shipping")]
    public async Task<List<ShippingModuleSummaryDto>> ListShippingModules(CancellationToken ct)
    {
        RequireAdmin();
        return await service.ListModulesAsync(HttpIdentity.Context(HttpContext), ct);
    }

    [HttpPost("private/modules/shipping")]
    public async Task<object> ConfigureShippingModule([FromBody] ShippingModuleConfigurationRequestDto request,
        CancellationToken ct)
    {
        RequireAdmin();
        var result = await service.SaveModuleAsync(request, HttpIdentity.Context(HttpContext), ct);
        return ToModule(result);
    }

    [HttpGet("private/modules/shipping/{module}")]
    public async Task<object> GetShippingModule(string module, CancellationToken ct)
    {
        RequireAdmin();
        return ToModule(await service.GetModuleAsync(module, HttpIdentity.Context(HttpContext), ct));
    }

    [HttpGet("private/shipping/origin")]
    public async Task<object> GetShippingOrigin(CancellationToken ct)
    {
        RequireAdmin();
        return ToOrigin(await service.GetOriginAsync(HttpIdentity.Context(HttpContext), ct));
    }

    [HttpPost("private/shipping/origin")]
    public async Task<object> SaveShippingOrigin([FromBody] ShippingOriginRequestDto request, CancellationToken ct)
    {
        RequireAdmin();
        return ToOrigin(await service.SaveOriginAsync(request, HttpIdentity.Context(HttpContext), ct));
    }

    [HttpGet("private/shipping/packages")]
    public async Task<List<object>> ListShippingPackages(CancellationToken ct)
    {
        RequireAdmin();
        return (await service.ListPackagesAsync(HttpIdentity.Context(HttpContext), ct))
            .Select(ToPackage).ToList();
    }

    [HttpGet("private/shipping/package/{package}")]
    public async Task<object> GetShippingPackage(string package, CancellationToken ct)
    {
        RequireAdmin();
        return ToPackage(await service.GetPackageAsync(package, HttpIdentity.Context(HttpContext), ct));
    }

    [HttpPost("private/shipping/package")]
    public async Task<object> CreateShippingPackage([FromBody] ShippingPackageRequestDto request, CancellationToken ct)
    {
        RequireAdmin();
        return ToPackage(await service.SavePackageAsync(request, null, HttpIdentity.Context(HttpContext), ct));
    }

    [HttpPut("private/shipping/package/{package}")]
    public async Task<object> UpdateShippingPackage(string package, [FromBody] ShippingPackageRequestDto request,
        CancellationToken ct)
    {
        RequireAdmin();
        return ToPackage(await service.SavePackageAsync(request, package, HttpIdentity.Context(HttpContext), ct));
    }

    [HttpDelete("private/shipping/package/{package}")]
    public async Task<ActionResultDto> DeleteShippingPackage(string package, CancellationToken ct)
    {
        RequireAdmin();
        await service.DeletePackageAsync(package, HttpIdentity.Context(HttpContext), ct);
        return new ActionResultDto { Status = "Deleted", ResourceId = package };
    }

    [HttpGet("private/shipping/expedition")]
    public async Task<object> GetExpeditionConfiguration(CancellationToken ct)
    {
        RequireAdmin();
        return ToExpedition(await service.GetExpeditionAsync(HttpIdentity.Context(HttpContext), ct));
    }

    [HttpPost("private/shipping/expedition")]
    public async Task<object> SaveExpeditionConfiguration(
        [FromBody] ExpeditionConfigurationRequestDto request, CancellationToken ct)
    {
        RequireAdmin();
        return ToExpedition(await service.SaveExpeditionAsync(request, HttpIdentity.Context(HttpContext), ct));
    }

    [HttpGet("shipping/country")]
    public async Task<List<ShippingCountryDto>> ListShippingCountries([FromQuery] string? lang, CancellationToken ct) =>
        await service.ListCountriesAsync(HttpIdentity.Context(HttpContext), lang, ct);

    private void RequireAdmin() =>
        HttpIdentity.RequireSubject(HttpContext, "administrator", ShippingRoles);

    private static object ToOrigin(ShippingOriginRecord value) => new
    {
        id = value.Id,
        tenantId = value.TenantId,
        storeId = value.StoreId,
        address = value.Address,
        city = value.City,
        postalCode = value.PostalCode,
        state = value.State,
        countryCode = value.CountryCode,
        zoneCode = value.ZoneCode,
        active = value.Active,
        createdAt = value.CreatedAt,
        updatedAt = value.UpdatedAt
    };

    private static object ToPackage(ShippingPackageRecord value) => new
    {
        id = value.Id,
        code = value.Code,
        shippingWidth = value.ShippingWidth,
        shippingHeight = value.ShippingHeight,
        shippingLength = value.ShippingLength,
        shippingWeight = value.ShippingWeight,
        shippingMaxWeight = value.ShippingMaxWeight,
        treshold = value.Treshold,
        type = value.Type,
        defaultPackaging = value.DefaultPackaging
    };

    private static object ToModule(ShippingModuleRecord value) => new
    {
        moduleCode = value.ModuleCode,
        active = value.Active,
        defaultSelected = value.DefaultSelected,
        environment = value.Environment,
        integrationKeys = value.IntegrationKeys,
        integrationOptions = value.IntegrationOptions,
        configured = true,
        providerCode = value.ModuleCode
    };

    private static object ToConfiguration(ShippingConfigurationRecord value) => new
    {
        shippingType = value.ShippingType,
        shippingBasisType = value.ShippingBasisType,
        shippingOptionPriceType = value.ShippingOptionPriceType,
        shippingPackageType = value.ShippingPackageType,
        shippingDescription = value.ShippingDescription,
        freeShippingType = value.FreeShippingType,
        boxWidth = value.BoxWidth,
        boxHeight = value.BoxHeight,
        boxLength = value.BoxLength,
        boxWeight = value.BoxWeight,
        maxWeight = value.MaxWeight,
        freeShippingEnabled = value.FreeShippingEnabled,
        orderTotalFreeShipping = value.OrderTotalFreeShipping,
        handlingFees = value.HandlingFees,
        taxOnShipping = value.TaxOnShipping,
        packages = value.Packages.Select(ToPackage)
    };

    private static object ToExpedition(ExpeditionConfigurationRecord value) => new
    {
        internationalShipping = value.InternationalShipping,
        taxOnShipping = value.TaxOnShipping,
        shipToCountry = value.ShipToCountry,
        updatedAt = value.UpdatedAt
    };
}
