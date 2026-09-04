using System.Security.Claims;
using Shopizer.Shipping.DTOs;

namespace Shopizer.Shipping.Models;

public sealed record RequestContext(string TenantId, string StoreId, string CorrelationId)
{
    public static RequestContext From(HttpContext http)
    {
        var tenant = http.Request.Headers["x-tenant-id"].FirstOrDefault();
        var store = http.Request.Headers["x-store-id"].FirstOrDefault();
        var correlation = http.Request.Headers["x-correlation-id"].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(tenant) || string.IsNullOrWhiteSpace(store))
            throw new DomainException("REQUEST_CONTEXT_REQUIRED", "x-tenant-id and x-store-id are required", 400);
        return new RequestContext(tenant, store,
            string.IsNullOrWhiteSpace(correlation) ? Guid.NewGuid().ToString() : correlation);
    }
}

public sealed class DomainException(string code, string message, int statusCode) : Exception(message)
{
    public string Code { get; } = code;
    public int StatusCode { get; } = statusCode;
}

public sealed class ShippingOriginRecord
{
    public Guid Id { get; init; }
    public string TenantId { get; init; } = "";
    public string StoreId { get; init; } = "";
    public string Address { get; set; } = "";
    public string City { get; set; } = "";
    public string PostalCode { get; set; } = "";
    public string? State { get; set; }
    public string CountryCode { get; set; } = "";
    public string? ZoneCode { get; set; }
    public bool Active { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

public sealed class ShippingPackageRecord
{
    public Guid Id { get; init; }
    public string TenantId { get; init; } = "";
    public string StoreId { get; init; } = "";
    public string Code { get; set; } = "";
    public decimal ShippingWidth { get; set; }
    public decimal ShippingHeight { get; set; }
    public decimal ShippingLength { get; set; }
    public decimal ShippingWeight { get; set; }
    public decimal ShippingMaxWeight { get; set; }
    public int? Treshold { get; set; }
    public string Type { get; set; } = "Item";
    public bool? DefaultPackaging { get; set; }
}

public sealed class ShippingModuleRecord
{
    public string ModuleCode { get; set; } = "";
    public bool Active { get; set; }
    public bool DefaultSelected { get; set; }
    public string Environment { get; set; } = "Test";
    public Dictionary<string, string?> IntegrationKeys { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, object?> IntegrationOptions { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

public sealed class ShippingConfigurationRecord
{
    public string ShippingType { get; set; } = "National";
    public string ShippingBasisType { get; set; } = "Shipping";
    public string ShippingOptionPriceType { get; set; } = "All";
    public string ShippingPackageType { get; set; } = "Item";
    public string? ShippingDescription { get; set; } = "ShortDescription";
    public string? FreeShippingType { get; set; }
    public int? BoxWidth { get; set; }
    public int? BoxHeight { get; set; }
    public int? BoxLength { get; set; }
    public decimal? BoxWeight { get; set; }
    public decimal? MaxWeight { get; set; }
    public bool FreeShippingEnabled { get; set; }
    public decimal? OrderTotalFreeShipping { get; set; }
    public decimal? HandlingFees { get; set; }
    public bool TaxOnShipping { get; set; }
    public List<ShippingPackageRecord> Packages { get; set; } = [];
}

public sealed class ExpeditionConfigurationRecord
{
    public bool InternationalShipping { get; set; }
    public bool TaxOnShipping { get; set; }
    public List<string> ShipToCountry { get; set; } = [];
    public DateTimeOffset UpdatedAt { get; set; }
}

public sealed class ShippingQuoteRecord
{
    public Guid Id { get; init; }
    public string TenantId { get; init; } = "";
    public string StoreId { get; init; } = "";
    public Guid? CartId { get; init; }
    public string ProviderCode { get; init; } = "";
    public ShippingOptionDto Option { get; init; } = new();
    public DeliveryAddressDto Delivery { get; init; } = new();
    public decimal Handling { get; init; }
    public bool FreeShipping { get; init; }
    public DateTimeOffset QuotedAt { get; init; }
    public decimal? DistanceKm { get; init; }
    public decimal? AppliedRate { get; init; }
}

public sealed class ShippingPackageFact
{
    public decimal Height { get; init; }
    public decimal Length { get; init; }
    public decimal Width { get; init; }
    public decimal Weight { get; init; }
    public int Quantity { get; init; } = 1;
}

public sealed record ShippingDecisionFacts(decimal TotalWeight, decimal LargestVolume, decimal LargestDimension,
    string CountryCode, string? Province, decimal? DistanceKm);

public sealed class ShippingOptionResult
{
    public decimal OptionPrice { get; set; }
    public string? OptionPriceText { get; set; }
    public string? OptionName { get; set; }
    public string OptionCode { get; set; } = "";
    public string OptionId { get; set; } = "";
    public DateTimeOffset? OptionDeliveryDate { get; set; }
    public DateTimeOffset? OptionShippingDate { get; set; }
    public string? Description { get; set; }
    public string ShippingModuleCode { get; set; } = "";
    public string? Note { get; set; }
    public int? EstimatedNumberOfDays { get; set; }
    public Guid? ShippingQuoteOptionId { get; set; }
}

public sealed class ShippingSummaryResult
{
    public decimal Shipping { get; set; }
    public decimal Handling { get; set; }
    public string? ShippingModule { get; set; }
    public string? ShippingOption { get; set; }
    public bool FreeShipping { get; set; }
    public bool TaxOnShipping { get; set; }
    public bool? ShippingQuote { get; set; }
    public string? ShippingText { get; set; }
    public string? HandlingText { get; set; }
    public DeliveryAddressDto? Delivery { get; set; }
    public ShippingOptionResult? SelectedShippingOption { get; set; }
    public List<ShippingOptionResult> ShippingOptions { get; set; } = [];
    public Dictionary<string, object?> QuoteInformations { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

public static class PrincipalExtensions
{
    public static Guid? SubjectId(this ClaimsPrincipal principal) =>
        Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier) ??
                      principal.FindFirstValue("sub"), out var id) ? id : null;
    public static string Kind(this ClaimsPrincipal principal) => principal.FindFirstValue("kind") ?? "";
    public static bool HasRole(this ClaimsPrincipal principal, params string[] roles) =>
        roles.Any(role => principal.IsInRole(role) ||
            principal.Claims.Any(c => (c.Type == ClaimTypes.Role || c.Type == "role") &&
                string.Equals(c.Value, role, StringComparison.OrdinalIgnoreCase)));
}
