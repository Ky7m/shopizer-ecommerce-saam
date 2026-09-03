using System.Security.Claims;
using Shopizer.CatalogProduct.DTOs;

namespace Shopizer.CatalogProduct.Models;

public sealed record RequestContext(string TenantId, string StoreId, string CorrelationId)
{
    public static RequestContext From(HttpContext http)
    {
        var tenant = http.Request.Headers["x-tenant-id"].FirstOrDefault();
        var store = http.Request.Headers["x-store-id"].FirstOrDefault();
        var correlation = http.Request.Headers["x-correlation-id"].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(tenant) || string.IsNullOrWhiteSpace(store))
            throw new DomainException("REQUEST_CONTEXT_REQUIRED", "x-tenant-id and x-store-id are required", 400);
        return new(tenant, store, string.IsNullOrWhiteSpace(correlation) ? Guid.NewGuid().ToString() : correlation);
    }
}

public sealed class DomainException(string code, string message, int statusCode) : Exception(message)
{
    public string Code { get; } = code;
    public int StatusCode { get; } = statusCode;
}

public sealed class ProductRecord
{
    public Guid Id { get; init; }
    public string TenantId { get; set; } = "";
    public string StoreId { get; set; } = "";
    public string Sku { get; set; } = "";
    public string? RefSku { get; set; }
    public string Status { get; set; } = "Draft";
    public bool Visible { get; set; }
    public bool Available { get; set; }
    public bool CanBePurchased { get; set; } = true;
    public DateTimeOffset DateAvailable { get; set; } = DateTimeOffset.UtcNow;
    public string? ManufacturerCode { get; set; }
    public string? ProductTypeCode { get; set; }
    public string? TaxClassCode { get; set; }
    public bool ProductVirtual { get; set; }
    public bool ProductShippable { get; set; }
    public bool ProductFree { get; set; }
    public decimal? Length { get; set; }
    public decimal? Width { get; set; }
    public decimal? Height { get; set; }
    public decimal? Weight { get; set; }
    public decimal ReviewAverage { get; set; }
    public int ReviewCount { get; set; }
    public int SortOrder { get; set; }
}

public sealed class ProductDescriptionRecord
{
    public Guid Id { get; init; }
    public Guid ProductId { get; init; }
    public string LanguageCode { get; init; } = "";
    public string Name { get; set; } = "";
    public string FriendlyUrl { get; set; } = "";
    public string? Description { get; set; }
    public string? Highlights { get; set; }
    public string? Title { get; set; }
    public string? Keywords { get; set; }
    public string? MetaDescription { get; set; }
}

public sealed class CategoryRecord
{
    public Guid Id { get; init; }
    public string TenantId { get; init; } = "";
    public string StoreId { get; init; } = "";
    public string Code { get; set; } = "";
    public Guid? ParentId { get; set; }
    public string? CategoryImageUri { get; set; }
    public int SortOrder { get; set; }
    public string Status { get; set; } = "Draft";
    public bool Visible { get; set; }
    public bool Featured { get; set; }
    public int Depth { get; set; }
    public string Lineage { get; set; } = "";
}

public sealed class CategoryDescriptionRecord
{
    public Guid Id { get; init; }
    public Guid CategoryId { get; init; }
    public string LanguageCode { get; init; } = "";
    public string Name { get; set; } = "";
    public string FriendlyUrl { get; set; } = "";
    public string? Description { get; set; }
    public string? Title { get; set; }
    public string? MetaDescription { get; set; }
}

public sealed class VariantRecord
{
    public Guid Id { get; init; }
    public Guid ProductId { get; init; }
    public string StoreId { get; init; } = "";
    public string Sku { get; set; } = "";
    public string? Code { get; set; }
    public string Status { get; set; } = "Draft";
    public bool Available { get; set; }
    public bool DefaultSelection { get; set; }
    public DateTimeOffset DateAvailable { get; set; } = DateTimeOffset.UtcNow;
    public int SortOrder { get; set; }
}

public sealed class AvailabilityRecord
{
    public Guid Id { get; init; }
    public Guid? ProductId { get; init; }
    public Guid? VariantId { get; init; }
    public string StoreId { get; init; } = "";
    public string RegionCode { get; init; } = "*";
    public int Quantity { get; set; }
    public int ReservedQuantity { get; set; }
    public bool Active { get; set; } = true;
}

public sealed class PriceRecord
{
    public Guid Id { get; init; }
    public Guid AvailabilityId { get; init; }
    public string StoreId { get; init; } = "";
    public string CurrencyCode { get; init; } = "USD";
    public decimal Amount { get; init; }
    public string PriceType { get; init; } = "OneTime";
    public bool DefaultPrice { get; init; }
    public decimal? SpecialAmount { get; init; }
    public DateTimeOffset? SpecialStartAt { get; init; }
    public DateTimeOffset? SpecialEndAt { get; init; }
}

public sealed class MediaRecord
{
    public Guid Id { get; init; }
    public Guid ProductId { get; init; }
    public Guid? VariantId { get; init; }
    public string ImageType { get; init; } = "ExternalUrl";
    public string FileName { get; init; } = "";
    public string? OriginalUri { get; init; }
    public string? TransformedUri { get; init; }
    public string? ProviderKey { get; init; }
    public string? ExternalUrl { get; init; }
    public bool DefaultImage { get; init; }
    public string MediaStatus { get; set; } = "Ready";
}

public sealed class ReservationRecord
{
    public Guid Id { get; init; }
    public string TenantId { get; set; } = "";
    public string StoreId { get; set; } = "";
    public Guid? ProductId { get; init; }
    public Guid? VariantId { get; init; }
    public Guid AvailabilityId { get; init; }
    public string ReservationKey { get; set; } = "";
    public int Quantity { get; init; }
    public string State { get; set; } = "Held";
    public DateTimeOffset ExpiresAt { get; init; }
    public DateTimeOffset? CommittedAt { get; set; }
    public DateTimeOffset? ReleasedAt { get; set; }
}

public sealed record OutboxMessage(Guid Id, string EventType, string Payload, RequestContext Context);

public static class PrincipalExtensions
{
    public static Guid? SubjectId(this ClaimsPrincipal principal) =>
        Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? principal.FindFirstValue("sub"), out var id) ? id : null;
    public static string Kind(this ClaimsPrincipal principal) => principal.FindFirstValue("kind") ?? "";
    public static bool HasRole(this ClaimsPrincipal principal, params string[] roles) =>
        roles.Length == 0 || roles.Any(role => principal.IsInRole(role) ||
            principal.Claims.Any(c => (c.Type == ClaimTypes.Role || c.Type == "role") &&
                                      c.Value.Equals(role, StringComparison.OrdinalIgnoreCase)));
}

public static class DtoMapper
{
    public static AvailabilityDto Availability(AvailabilityRecord a) => new()
    {
        Id = a.Id.ToString(), RegionCode = a.RegionCode, Quantity = a.Quantity,
        ReservedQuantity = a.ReservedQuantity, SellableQuantity = Math.Max(0, a.Quantity - a.ReservedQuantity), Active = a.Active
    };

    public static ProductDescriptionDto Description(ProductDescriptionRecord d) => new()
    {
        LanguageCode = d.LanguageCode, Name = d.Name, FriendlyUrl = d.FriendlyUrl,
        Description = d.Description, Highlights = d.Highlights, Title = d.Title,
        Keywords = d.Keywords, MetaDescription = d.MetaDescription
    };

    public static CategoryDescriptionDto Description(CategoryDescriptionRecord d) => new()
    {
        LanguageCode = d.LanguageCode, Name = d.Name, FriendlyUrl = d.FriendlyUrl,
        Description = d.Description, Title = d.Title, MetaDescription = d.MetaDescription
    };

    public static PriceDto Price(PriceRecord p, DateTimeOffset now)
    {
        var active = p.SpecialAmount.HasValue &&
                     (!p.SpecialStartAt.HasValue || p.SpecialStartAt.Value < now) &&
                     (!p.SpecialEndAt.HasValue || now < p.SpecialEndAt.Value);
        return new()
        {
            Id = p.Id.ToString(), Amount = p.Amount, CurrencyCode = p.CurrencyCode,
            PriceType = p.PriceType, DefaultPrice = p.DefaultPrice, SpecialAmount = p.SpecialAmount,
            SpecialStartAt = p.SpecialStartAt?.ToString("O"), SpecialEndAt = p.SpecialEndAt?.ToString("O"),
            FinalAmount = active ? p.SpecialAmount : p.Amount, Discounted = active
        };
    }
}
