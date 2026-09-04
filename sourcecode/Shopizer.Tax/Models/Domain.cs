using System.Security.Claims;
using Shopizer.Tax.DTOs;

namespace Shopizer.Tax.Models;

public sealed record RequestContext(string TenantId, string StoreId, string CorrelationId)
{
    public static RequestContext From(HttpContext http)
    {
        var tenant = http.Request.Headers["x-tenant-id"].FirstOrDefault();
        var store = http.Request.Headers["x-store-id"].FirstOrDefault();
        var correlation = http.Request.Headers["x-correlation-id"].FirstOrDefault();

        if (string.IsNullOrWhiteSpace(tenant) || string.IsNullOrWhiteSpace(store) ||
            string.IsNullOrWhiteSpace(correlation))
        {
            throw new DomainException(
                "REQUEST_CONTEXT_REQUIRED",
                "x-tenant-id, x-store-id, and x-correlation-id are required",
                400);
        }

        return new RequestContext(tenant.Trim(), store.Trim(), correlation.Trim());
    }
}

public sealed class DomainException(string code, string message, int statusCode) : Exception(message)
{
    public string Code { get; } = code;
    public int StatusCode { get; } = statusCode;
}

public sealed class TaxClassEntity
{
    public Guid Id { get; set; }
    public string TenantId { get; init; } = "";
    public string StoreId { get; init; } = "";
    public string Code { get; set; } = "";
    public string Title { get; set; } = "";
}

public sealed class TaxRateEntity
{
    public Guid Id { get; init; }
    public string TenantId { get; init; } = "";
    public string StoreId { get; init; } = "";
    public Guid TaxClassId { get; set; }
    public string TaxClassCode { get; set; } = "";
    public string Code { get; set; } = "";
    public decimal RatePercent { get; set; }
    public int Priority { get; set; }
    public bool Piggyback { get; set; }
    public string CountryCode { get; set; } = "";
    public string? ZoneCode { get; set; }
    public string? StateProvince { get; set; }
    public Guid? ParentRateId { get; set; }
    public List<TaxRateDescriptionEntity> Descriptions { get; } = [];
}

public sealed class TaxRateDescriptionEntity
{
    public Guid Id { get; init; }
    public Guid TaxRateId { get; init; }
    public string LanguageCode { get; set; } = "";
    public string Name { get; set; } = "";
    public string? Title { get; set; }
    public string? Description { get; set; }
}

public sealed class TaxConfigurationEntity
{
    public Guid Id { get; set; }
    public string TenantId { get; init; } = "";
    public string StoreId { get; init; } = "";
    public string TaxBasis { get; set; } = "ShippingAddress";
    public bool CollectTaxIfDifferentProvince { get; set; } = true;
    public string DifferentCountryBehavior { get; set; } = "UseCustomerJurisdiction";
}

public sealed class TaxQuoteEntity
{
    public Guid Id { get; init; }
    public string TenantId { get; init; } = "";
    public string StoreId { get; init; } = "";
    public string? IdempotencyKey { get; init; }
    public string CurrencyCode { get; init; } = "";
    public string Status { get; init; } = "Calculated";
    public Guid? CustomerId { get; init; }
    public Guid? OrderId { get; init; }
    public string? JurisdictionCountryCode { get; init; }
    public string? JurisdictionZoneCode { get; init; }
    public string? JurisdictionStateProvince { get; init; }
    public decimal TaxableAmount { get; init; }
    public decimal TotalTaxAmount { get; init; }
    public DateTimeOffset CalculatedAt { get; init; }
}

public sealed class TaxQuoteItemEntity
{
    public Guid Id { get; init; }
    public Guid TaxQuoteId { get; init; }
    public Guid? TaxClassId { get; init; }
    public string? TaxClassCode { get; init; }
    public string TaxCode { get; init; } = "";
    public string Label { get; init; } = "";
    public decimal RatePercent { get; init; }
    public decimal TaxableAmount { get; init; }
    public decimal TaxAmount { get; init; }
    public bool Piggyback { get; init; }
    public int Priority { get; init; }
}

public sealed record TaxCalculationContext(
    AddressSnapshotDto Jurisdiction,
    TaxConfigurationEntity Configuration,
    decimal TaxableAmount,
    IReadOnlyList<TaxItemDto> Items);

public static class PrincipalExtensions
{
    public static string Kind(this ClaimsPrincipal principal) =>
        principal.FindFirstValue("kind") ?? "";

    public static Guid? SubjectId(this ClaimsPrincipal principal) =>
        Guid.TryParse(
            principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? principal.FindFirstValue("sub"),
            out var id)
            ? id
            : null;

    public static bool HasRole(this ClaimsPrincipal principal, params string[] roles) =>
        roles.Any(role => principal.IsInRole(role) || principal.Claims.Any(claim =>
            (claim.Type == ClaimTypes.Role || claim.Type == "role") &&
            string.Equals(claim.Value, role, StringComparison.OrdinalIgnoreCase)));
}

public static class DtoMapper
{
    public static TaxClassDto TaxClass(TaxClassEntity entity) => new()
    {
        Id = entity.Id.ToString(),
        TenantId = entity.TenantId,
        StoreId = entity.StoreId,
        Code = entity.Code,
        Title = entity.Title
    };

    public static TaxRateDto TaxRate(TaxRateEntity entity, string? languageCode = null) => new()
    {
        Id = entity.Id.ToString(),
        TenantId = entity.TenantId,
        StoreId = entity.StoreId,
        TaxClassCode = entity.TaxClassCode,
        Code = entity.Code,
        Rate = entity.RatePercent,
        Priority = entity.Priority,
        Piggyback = entity.Piggyback,
        CountryCode = entity.CountryCode,
        ZoneCode = entity.ZoneCode,
        StateProvince = entity.StateProvince,
        Descriptions = entity.Descriptions
            .Where(description => languageCode is null ||
                                  string.Equals(description.LanguageCode, languageCode, StringComparison.OrdinalIgnoreCase))
            .Select(description => new TaxRateDescriptionDto
            {
                Id = description.Id.ToString(),
                LanguageCode = description.LanguageCode,
                Name = description.Name,
                Title = description.Title,
                Description = description.Description
            })
            .ToList()
    };

    public static TaxConfigurationDto TaxConfiguration(TaxConfigurationEntity entity) => new()
    {
        TaxBasis = entity.TaxBasis,
        CollectTaxIfDifferentProvince = entity.CollectTaxIfDifferentProvince,
        DifferentCountryBehavior = entity.DifferentCountryBehavior
    };

    public static TaxCalculationResponseDto TaxCalculation(TaxQuoteEntity quote, IEnumerable<TaxQuoteItemEntity> items) =>
        new()
        {
            QuoteId = quote.Id.ToString(),
            CurrencyCode = quote.CurrencyCode,
            Jurisdiction = new JurisdictionDto
            {
                CountryCode = quote.JurisdictionCountryCode ?? "",
                ZoneCode = quote.JurisdictionZoneCode,
                StateProvince = quote.JurisdictionStateProvince
            },
            TaxableAmount = quote.TaxableAmount,
            TotalTaxAmount = quote.TotalTaxAmount,
            TaxItems = items.Select(item => new TaxItemDto
            {
                TaxCode = item.TaxCode,
                Label = item.Label,
                TaxClassCode = item.TaxClassCode,
                TaxRatePercent = item.RatePercent,
                TaxableAmount = item.TaxableAmount,
                TaxAmount = item.TaxAmount,
                Piggyback = item.Piggyback,
                Priority = item.Priority
            }).ToList()
        };
}
