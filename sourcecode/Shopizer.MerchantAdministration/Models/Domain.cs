using System.Security.Claims;
using Shopizer.MerchantAdministration.DTOs;

namespace Shopizer.MerchantAdministration.Models;

public sealed record RequestContext(string TenantId, string StoreId, string CorrelationId)
{
    public static RequestContext From(HttpContext http)
    {
        var tenant = http.Request.Headers["x-tenant-id"].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(tenant))
            throw new DomainException("REQUEST_CONTEXT_REQUIRED", "x-tenant-id is required", 400);
        var configuredDefault = http.RequestServices.GetRequiredService<IConfiguration>()["MerchantAdministration:DefaultStoreCode"] ?? "default";
        var store = http.Request.Headers["x-store-id"].FirstOrDefault();
        var correlation = http.Request.Headers["x-correlation-id"].FirstOrDefault();
        return new RequestContext(tenant.Trim(), string.IsNullOrWhiteSpace(store) ? configuredDefault : store.Trim(),
            string.IsNullOrWhiteSpace(correlation) ? Guid.NewGuid().ToString() : correlation.Trim());
    }
}

public sealed class DomainException(string code, string message, int statusCode) : Exception(message)
{
    public string Code { get; } = code;
    public int StatusCode { get; } = statusCode;
}

public sealed class StoreRecord
{
    public Guid Id { get; init; }
    public string TenantId { get; init; } = "";
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string EmailAddress { get; set; } = "";
    public string Phone { get; set; } = "";
    public string? StreetAddress { get; set; }
    public string City { get; set; } = "";
    public string PostalCode { get; set; } = "";
    public string CountryCode { get; set; } = "";
    public string? StateProvince { get; set; }
    public string? ZoneCode { get; set; }
    public bool Retailer { get; set; }
    public Guid? ParentStoreId { get; set; }
    public string? ParentStoreCode { get; set; }
    public string DefaultLanguageCode { get; set; } = "en";
    public string CurrencyCode { get; set; } = "USD";
    public string DimensionUnit { get; set; } = "CM";
    public string WeightUnit { get; set; } = "KG";
    public string? TemplateCode { get; set; }
    public string? LogoUri { get; set; }
    public string Status { get; set; } = "Active";
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; set; }
    public List<string> SupportedLanguageCodes { get; } = [];
}

public sealed record SignupRecord(Guid Id, string TenantId, string Code, string PayloadJson, string TokenHash, DateTimeOffset ExpiresAt, DateTimeOffset? ConsumedAt);

public static class PrincipalExtensions
{
    public static Guid? SubjectId(this ClaimsPrincipal principal) =>
        Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? principal.FindFirstValue("sub"), out var id) ? id : null;
    public static string Kind(this ClaimsPrincipal principal) => principal.FindFirstValue("kind") ?? "";
    public static bool HasRole(this ClaimsPrincipal principal, params string[] roles) =>
        roles.Length == 0 || roles.Any(role => principal.IsInRole(role) || principal.Claims.Any(c =>
            (c.Type == ClaimTypes.Role || c.Type == "role") && string.Equals(c.Value, role, StringComparison.OrdinalIgnoreCase)));
}

public static class DtoMapper
{
    public static StoreDto Store(StoreRecord store, string? parentCode) => new()
    {
        Id = store.Id.ToString(),
        TenantId = store.TenantId,
        Code = store.Code,
        Name = store.Name,
        EmailAddress = store.EmailAddress,
        Phone = store.Phone,
        Address = new AddressDto { StreetAddress = store.StreetAddress, City = store.City, PostalCode = store.PostalCode, CountryCode = store.CountryCode, StateProvince = store.StateProvince, ZoneCode = store.ZoneCode },
        DefaultLanguageCode = store.DefaultLanguageCode,
        SupportedLanguageCodes = store.SupportedLanguageCodes.ToList(),
        CurrencyCode = store.CurrencyCode,
        DimensionUnit = store.DimensionUnit,
        WeightUnit = store.WeightUnit,
        Retailer = store.Retailer,
        ParentStoreCode = parentCode ?? store.ParentStoreCode,
        TemplateCode = store.TemplateCode,
        LogoUri = store.LogoUri,
        Status = store.Status
    };

    public static BrandingDto Branding(StoreRecord store) => new() { StoreCode = store.Code, TemplateCode = store.TemplateCode, LogoUri = store.LogoUri };
}
