using System.Security.Claims;
using Shopizer.PricingPromotions.DTOs;

namespace Shopizer.PricingPromotions.Models;

public sealed record RequestContext(string TenantId, string StoreId, string CorrelationId)
{
    public static RequestContext From(HttpContext http)
    {
        var tenant = http.Request.Headers["x-tenant-id"].FirstOrDefault();
        var store = http.Request.Headers["x-store-id"].FirstOrDefault();
        var correlation = http.Request.Headers["x-correlation-id"].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(tenant) || string.IsNullOrWhiteSpace(store) ||
            string.IsNullOrWhiteSpace(correlation))
            throw new DomainException("REQUEST_CONTEXT_REQUIRED",
                "x-tenant-id, x-store-id and x-correlation-id are required", 400);
        return new RequestContext(tenant.Trim(), store.Trim(), correlation.Trim());
    }
}

public sealed class DomainException(string code, string message, int statusCode) : Exception(message)
{
    public string Code { get; } = code;
    public int StatusCode { get; } = statusCode;
}

public sealed class PriceList
{
    public Guid Id { get; init; }
    public string TenantId { get; init; } = "";
    public string StoreId { get; init; } = "";
    public string Name { get; init; } = "";
    public string CurrencyCode { get; init; } = "";
    public bool IsActive { get; init; }
}

public sealed record PriceEntry
{
    public Guid Id { get; init; }
    public Guid PriceListId { get; init; }
    public long? LegacyPriceId { get; init; }
    public string ProductSku { get; init; } = "";
    public string? VariantSku { get; init; }
    public long? AvailabilityId { get; init; }
    public string Code { get; init; } = "";
    public decimal Amount { get; init; }
    public string PriceType { get; init; } = "";
    public bool DefaultPrice { get; init; }
    public DateOnly? SpecialStartDate { get; init; }
    public DateOnly? SpecialEndDate { get; init; }
    public decimal? SpecialAmount { get; init; }
    public long? ProductIdentifierId { get; init; }
    public string Currency { get; init; } = "USD";
}

public sealed class Promotion
{
    public Guid Id { get; init; }
    public string TenantId { get; init; } = "";
    public string StoreId { get; init; } = "";
    public string Name { get; init; } = "";
    public string RuleKey { get; init; } = "";
    public decimal DiscountRate { get; init; }
    public DateOnly? ValidFrom { get; init; }
    public DateOnly? ValidUntil { get; init; }
    public bool IsEnabled { get; init; }
}

public sealed record PromotionMatch(Guid Id, string Code, Promotion Promotion);

public sealed record CalculatedPrice(
    PriceEntry Source,
    decimal OriginalPrice,
    decimal FinalPrice,
    bool Discounted,
    decimal? DiscountedPrice,
    int DiscountPercent,
    DateOnly? DiscountEndDate,
    string AvailabilitySource,
    string? SelectedVariantSku,
    decimal AttributeAdjustment,
    IReadOnlyList<CalculatedPrice> AdditionalPrices);

public static class PrincipalExtensions
{
    public static Guid? SubjectId(this ClaimsPrincipal principal) =>
        Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier) ??
                      principal.FindFirstValue("sub"), out var id) ? id : null;

    public static string Kind(this ClaimsPrincipal principal) =>
        principal.FindFirstValue("kind") ?? "";

    public static bool HasRole(this ClaimsPrincipal principal, params string[] roles) =>
        roles.Any(role => principal.Claims.Any(claim =>
            (claim.Type == ClaimTypes.Role || claim.Type == "role") &&
            string.Equals(claim.Value, role, StringComparison.OrdinalIgnoreCase)));
}

public static class DtoMapper
{
    public static PriceDto Price(CalculatedPrice calculated) => Price(calculated.Source, calculated);

    public static PriceDto Price(PriceEntry price, CalculatedPrice? calculated = null)
    {
        calculated ??= new CalculatedPrice(price, price.Amount, price.Amount, false, null, 0,
            null, "Product", price.VariantSku, 0, []);
        return new PriceDto
        {
            Id = price.Id.ToString(),
            LegacyPriceId = price.LegacyPriceId,
            PriceListId = price.PriceListId.ToString(),
            ProductSku = price.ProductSku,
            VariantSku = price.VariantSku,
            AvailabilityId = price.AvailabilityId,
            Code = price.Code,
            Amount = price.Amount,
            PriceType = price.PriceType,
            DefaultPrice = price.DefaultPrice,
            SpecialStartDate = price.SpecialStartDate?.ToString("yyyy-MM-dd"),
            SpecialEndDate = price.SpecialEndDate?.ToString("yyyy-MM-dd"),
            SpecialAmount = price.SpecialAmount,
            ProductIdentifierId = price.ProductIdentifierId,
            Discounted = calculated.Discounted,
            Price = calculated.FinalPrice,
            DiscountedPrice = calculated.DiscountedPrice,
            DiscountPercent = calculated.DiscountPercent,
            DiscountEndDate = calculated.DiscountEndDate?.ToString("yyyy-MM-dd"),
            Currency = price.Currency
        };
    }

    public static AdditionalPriceLineDto Additional(PriceEntry price, CalculatedPrice calculated) => new()
    {
        Code = price.Code,
        PriceType = price.PriceType,
        FinalPrice = calculated.FinalPrice
    };
}
