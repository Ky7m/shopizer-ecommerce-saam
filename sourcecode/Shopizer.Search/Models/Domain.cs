using System.Security.Claims;
using System.Runtime.CompilerServices;
using Shopizer.Search.DTOs;

namespace Shopizer.Search.Models;

public sealed record RequestContext(string TenantId, string StoreId, string CorrelationId)
{
    public static RequestContext From(HttpContext http)
    {
        var tenant = http.Request.Headers["x-tenant-id"].FirstOrDefault()?.Trim();
        var store = http.Request.Headers["x-store-id"].FirstOrDefault()?.Trim();
        var correlation = http.Request.Headers["x-correlation-id"].FirstOrDefault()?.Trim();
        if (string.IsNullOrWhiteSpace(tenant) || string.IsNullOrWhiteSpace(store) ||
            string.IsNullOrWhiteSpace(correlation))
        {
            throw new DomainException("REQUEST_CONTEXT_REQUIRED",
                "x-tenant-id, x-store-id, and x-correlation-id are required", 400);
        }

        return new RequestContext(tenant, store.ToLowerInvariant(), correlation);
    }
}

public sealed class DomainException(string code, string message, int statusCode) : Exception(message)
{
    public string Code { get; } = code;
    public int StatusCode { get; } = statusCode;
}

public sealed class SearchIndex
{
    public Guid Id { get; init; }
    public string TenantId { get; init; } = "";
    public string StoreId { get; init; } = "";
    public string ProviderName { get; init; } = "";
    public string[] ConfiguredLocales { get; init; } = [];
    public long ConfigurationVersion { get; init; }
    public string State { get; init; } = "Configured";
}

public sealed class SearchDocument
{
    public Guid Id { get; init; }
    public Guid SearchIndexId { get; init; }
    public string TenantId { get; init; } = "";
    public string StoreId { get; init; } = "";
    public long ProductId { get; init; }
    public string Locale { get; init; } = "";
    public string ProviderDocumentKey { get; init; } = "";
    public string State { get; init; } = "Active";
    public long? SourceVersion { get; init; }
}

public sealed class SearchRebuildJob
{
    public Guid Id { get; init; }
    public Guid SearchIndexId { get; init; }
    public string TenantId { get; init; } = "";
    public string StoreId { get; init; } = "";
    public string RequestedBy { get; init; } = "";
    public string IdempotencyKey { get; init; } = "";
    public string State { get; init; } = "Requested";
    public DateTimeOffset RequestedAt { get; init; }
    public DateTimeOffset? StartedAt { get; init; }
    public DateTimeOffset? CompletedAt { get; init; }
    public long IndexedDocumentCount { get; init; }
    public long FailedDocumentCount { get; init; }
    public string? ErrorCode { get; init; }
}

public sealed record ProductLocaleProjection(
    string Locale,
    string Name,
    string? Description,
    string? ProductLink,
    string? BrandName,
    string? CategoryName,
    string? ImageUrl,
    decimal? ReviewAverage,
    IReadOnlyDictionary<string, object?> Attributes);

public sealed record InventoryProjection(
    string Sku,
    string? VariantSku,
    decimal Quantity,
    decimal Price,
    decimal? DiscountedPrice,
    IReadOnlyDictionary<string, object?> OptionValues);

public sealed record ProductProjection(
    long ProductId,
    long? SourceVersion,
    IReadOnlyList<ProductLocaleProjection> Locales,
    IReadOnlyList<InventoryProjection> Inventory);

public sealed record ProductChangedEvent(
    string EventType,
    long ProductId,
    long? SourceVersion,
    ProductProjection? Projection,
    string? ComponentType = null,
    string? ComponentId = null,
    bool Deleted = false);

public static class PrincipalExtensions
{
    public static Guid? SubjectId(this ClaimsPrincipal principal) =>
        Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier) ??
                      principal.FindFirstValue("sub"), out var id) ? id : null;

    public static string Kind(this ClaimsPrincipal principal) =>
        principal.FindFirstValue("kind") ?? "";

    public static bool HasRole(this ClaimsPrincipal principal, params string[] roles) =>
        roles.Any(role => principal.IsInRole(role) ||
                          principal.Claims.Any(c =>
                              (c.Type == ClaimTypes.Role || c.Type == "role") &&
                              string.Equals(c.Value, role, StringComparison.OrdinalIgnoreCase)));
}

public static class RebuildStatusRegistry
{
    private static readonly ConditionalWeakTable<RebuildStatusDto, Holder> Values = new();

    public static RebuildStatusDto Create(string state)
    {
        var dto = new RebuildStatusDto();
        Values.Add(dto, new Holder(state));
        return dto;
    }

    public static string Get(RebuildStatusDto value) =>
        Values.TryGetValue(value, out var holder) ? holder.Value : "Requested";

    private sealed record Holder(string Value);
}
