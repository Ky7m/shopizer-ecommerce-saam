using System.Security.Claims;
using System.Globalization;
using System.Text.Json;
using Shopizer.CartCheckout.DTOs;

namespace Shopizer.CartCheckout.Models;

public sealed record RequestContext(string TenantId, string StoreId, string CorrelationId)
{
    public static RequestContext From(HttpContext http)
    {
        var tenant = http.Request.Headers["x-tenant-id"].FirstOrDefault();
        var store = http.Request.Headers["x-store-id"].FirstOrDefault();
        var correlation = http.Request.Headers["x-correlation-id"].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(tenant) || string.IsNullOrWhiteSpace(store) || string.IsNullOrWhiteSpace(correlation))
            throw new DomainException("REQUEST_CONTEXT_REQUIRED", "x-tenant-id, x-store-id and x-correlation-id are required", 400);
        return new(tenant, store, correlation);
    }
}

public sealed class DomainException(string code, string message, int statusCode) : Exception(message)
{
    public string Code { get; } = code;
    public int StatusCode { get; } = statusCode;
}

public sealed class Cart
{
    public long Id { get; init; }
    public string Code { get; init; } = "";
    public string TenantId { get; init; } = "";
    public string StoreId { get; init; } = "";
    public long? CustomerId { get; set; }
    public Guid? SubmittedOrderId { get; set; }
    public string Status { get; set; } = "OPEN";
    public string? PromoCode { get; set; }
    public DateTimeOffset? PromoAddedAt { get; set; }
    public long Version { get; set; }
    public string CurrencyCode { get; set; } = "CAD";
    public List<CartLine> Items { get; } = [];
}

public sealed class CartLine
{
    public long Id { get; init; }
    public long CartId { get; init; }
    public long ProductId { get; set; }
    public string ProviderProductId { get; set; } = "";
    public string Sku { get; set; } = "";
    public long? VariantId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal SubTotal { get; set; }
    public bool Obsolete { get; set; }
    public List<long> Attributes { get; } = [];
}

public sealed record ProductFact(string Id, long NumericId, string StoreId, string Sku, bool Available,
    bool CanBePurchased, DateTimeOffset? DateAvailable, bool IsVirtual, bool IsShippable, string Name,
    string Currency, decimal? Price, IReadOnlySet<long> AttributeIds);
public sealed record PriceFact(decimal Amount, string Currency, string? Version);
public sealed record ShippingFact(ShippingSummaryDto Summary, string? ProviderVersion);
public sealed record CustomerFact(long Id, AddressDto? Billing, AddressDto? Delivery);

public sealed class CheckoutResult
{
    public required CheckoutSubmissionResponseDto Response { get; init; }
    public required Guid EventId { get; init; }
}

public static class PrincipalExtensions
{
    public static Guid? SubjectId(this ClaimsPrincipal principal) =>
        Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? principal.FindFirstValue("sub"), out var id) ? id : null;
    public static string Kind(this ClaimsPrincipal principal) => principal.FindFirstValue("kind") ?? "";
}

public static class DtoMapper
{
    public static CartEnvelopeDto Cart(Cart cart, string currency, decimal? total = null) => new()
    {
        Cart = new CartDto
        {
            Id = cart.Id.ToString(),
            Code = cart.Code,
            TenantId = cart.TenantId,
            StoreId = cart.StoreId,
            CustomerId = cart.CustomerId?.ToString(),
            SubmittedOrderId = cart.SubmittedOrderId?.ToString(),
            Status = cart.Status switch { "OPEN" => "Open", "COMPLETED" => "Completed", "OBSOLETE" => "Obsolete", _ => cart.Status },
            PromoCode = cart.PromoCode,
            PromoAddedAt = cart.PromoAddedAt?.ToString("O"),
            Currency = currency,
            Items = cart.Items.Select(Item).ToList(),
            SubTotal = Money(cart.Items.Sum(x => x.SubTotal)),
            Total = Money(total ?? cart.Items.Sum(x => x.SubTotal))
        }
    };
    public static CartItemDto Item(CartLine item) => new()
    {
        Id = item.Id.ToString(),
        ProductId = item.ProductId,
        Sku = item.Sku,
        VariantId = item.VariantId,
        Quantity = item.Quantity,
        UnitPrice = Money(item.UnitPrice),
        SubTotal = Money(item.SubTotal),
        Obsolete = item.Obsolete,
        Attributes = item.Attributes.Select(x => new CartAttributeReferenceDto { Id = x }).ToList()
    };
    public static string Money(decimal amount) => amount.ToString("0.0000", CultureInfo.InvariantCulture).TrimEnd('0').TrimEnd('.');
    public static long OpaqueNumericId(string value)
    {
        if (long.TryParse(value, out var number)) return number;
        var bytes = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(value));
        var result = BitConverter.ToInt64(bytes, 0) & long.MaxValue;
        return result == 0 ? 1 : result;
    }
}

public static class JsonHelpers
{
    public static string? String(JsonElement root, string name) =>
        root.TryGetProperty(name, out var value) && value.ValueKind != JsonValueKind.Null ? value.ToString() : null;
    public static bool Bool(JsonElement root, string name, bool fallback = false) =>
        root.TryGetProperty(name, out var value) && value.ValueKind is JsonValueKind.True or JsonValueKind.False ? value.GetBoolean() : fallback;
    public static decimal Decimal(JsonElement root, string name, decimal fallback = 0) =>
        root.TryGetProperty(name, out var value) && value.TryGetDecimal(out var result) ? result : fallback;
}
