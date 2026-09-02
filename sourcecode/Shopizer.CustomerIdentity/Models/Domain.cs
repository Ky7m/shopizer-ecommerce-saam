using System.Security.Claims;
using Shopizer.CustomerIdentity.DTOs;

namespace Shopizer.CustomerIdentity.Models;

public sealed record RequestContext(string TenantId, string StoreId, string CorrelationId)
{
    public static RequestContext From(HttpContext http)
    {
        var tenant = http.Request.Headers["x-tenant-id"].FirstOrDefault();
        var store = http.Request.Headers["x-store-id"].FirstOrDefault();
        var correlation = http.Request.Headers["x-correlation-id"].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(tenant) || string.IsNullOrWhiteSpace(store))
            throw new DomainException("REQUEST_CONTEXT_REQUIRED", "x-tenant-id and x-store-id are required", 400);
        return new RequestContext(tenant, store, string.IsNullOrWhiteSpace(correlation)
            ? Guid.NewGuid().ToString()
            : correlation);
    }
}

public sealed class DomainException(string code, string message, int statusCode) : Exception(message)
{
    public string Code { get; } = code;
    public int StatusCode { get; } = statusCode;
}

public sealed class CustomerAccount
{
    public Guid Id { get; init; }
    public string TenantId { get; init; } = "";
    public string StoreId { get; set; } = "";
    public string LoginName { get; set; } = "";
    public string EmailAddress { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public string Gender { get; set; } = "M";
    public DateOnly? DateOfBirth { get; set; }
    public string? CompanyName { get; set; }
    public string? Provider { get; set; }
    public string Status { get; set; } = "Active";
    public string DefaultLanguageCode { get; set; } = "en";
    public decimal ReviewAverage { get; set; }
    public int ReviewCount { get; set; }
    public bool Anonymous { get; set; }
    public DateTimeOffset? LastPasswordResetAt { get; set; }
}

public sealed class AddressRecord
{
    public Guid Id { get; init; }
    public Guid CustomerId { get; init; }
    public string AddressType { get; init; } = "";
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string? CompanyName { get; set; }
    public string StreetAddress { get; set; } = "";
    public string City { get; set; } = "";
    public string PostalCode { get; set; } = "";
    public string? StateProvince { get; set; }
    public string? Telephone { get; set; }
    public string CountryCode { get; set; } = "";
    public string? ZoneCode { get; set; }
    public string? Latitude { get; set; }
    public string? Longitude { get; set; }
}

public sealed class AdministratorAccount
{
    public Guid Id { get; init; }
    public string TenantId { get; init; } = "";
    public string StoreId { get; set; } = "";
    public string UserName { get; set; } = "";
    public string EmailAddress { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public bool IsActive { get; set; } = true;
    public string? DefaultLanguageCode { get; set; }
    public DateTimeOffset? LastPasswordResetAt { get; set; }
    public List<string> Groups { get; } = [];
    public List<string> Permissions { get; } = [];
}

public sealed class ReviewRecord
{
    public Guid Id { get; init; }
    public Guid ReviewerCustomerId { get; init; }
    public Guid ReviewedCustomerId { get; init; }
    public decimal Rating { get; set; }
    public string? Description { get; set; }
    public DateTimeOffset ReviewDate { get; init; }
    public string Status { get; set; } = "Published";
}

public sealed class NewsletterRecord
{
    public Guid Id { get; init; }
    public string TenantId { get; init; } = "";
    public string StoreId { get; init; } = "";
    public string CampaignCode { get; init; } = "";
    public string Email { get; init; } = "";
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string Status { get; set; } = "Subscribed";
    public DateTimeOffset SubscribedAt { get; init; }
    public DateTimeOffset? UnsubscribedAt { get; set; }
}

public sealed class ExternalIdentityRecord
{
    public string UserId { get; init; } = "";
    public string ProviderId { get; init; } = "";
    public string ProviderUserId { get; init; } = "";
    public string? AccessToken { get; set; }
    public string? RefreshToken { get; set; }
    public string? DisplayName { get; set; }
    public string? ProfileUrl { get; set; }
    public string? ImageUrl { get; set; }
}

public sealed record AuthenticatedIdentity(Guid Id, string Kind, string LoginName, string StoreId, IReadOnlyList<string> Roles);

public static class PrincipalExtensions
{
    public static Guid? SubjectId(this ClaimsPrincipal principal) =>
        Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? principal.FindFirstValue("sub"), out var id)
            ? id : null;

    public static string Kind(this ClaimsPrincipal principal) => principal.FindFirstValue("kind") ?? "";
    public static bool HasRole(this ClaimsPrincipal principal, params string[] roles) =>
        roles.Any(role => principal.IsInRole(role) || principal.Claims.Any(c =>
            (c.Type == ClaimTypes.Role || c.Type == "role") &&
            string.Equals(c.Value, role, StringComparison.OrdinalIgnoreCase)));
}

public static class DtoMapper
{
    public static AddressDto Address(AddressRecord a) => new()
    {
        AddressType = a.AddressType,
        FirstName = a.FirstName,
        LastName = a.LastName,
        CompanyName = a.CompanyName,
        StreetAddress = a.StreetAddress,
        City = a.City,
        PostalCode = a.PostalCode,
        StateProvince = a.StateProvince,
        Telephone = a.Telephone,
        CountryCode = a.CountryCode,
        ZoneCode = a.ZoneCode,
        Latitude = a.Latitude,
        Longitude = a.Longitude
    };

    public static CustomerDto Customer(CustomerAccount c, IEnumerable<AddressRecord> addresses, IEnumerable<(string OptionId, string ValueId, string? Text)> attributes)
    {
        var list = addresses.ToList();
        return new CustomerDto
        {
            Id = c.Id.ToString(),
            StoreId = c.StoreId,
            LoginName = c.LoginName,
            EmailAddress = c.EmailAddress,
            Gender = c.Gender,
            DateOfBirth = c.DateOfBirth?.ToString("yyyy-MM-dd"),
            CompanyName = c.CompanyName,
            Provider = c.Provider,
            Status = c.Status,
            DefaultLanguageCode = c.DefaultLanguageCode,
            ReviewAverage = c.ReviewAverage,
            ReviewCount = c.ReviewCount,
            Anonymous = c.Anonymous,
            Billing = list.FirstOrDefault(x => x.AddressType == "Billing") is { } b ? Address(b) : null,
            Delivery = list.FirstOrDefault(x => x.AddressType == "Delivery") is { } d ? Address(d) : null,
            Attributes = attributes.Select(x => new CustomerAttributeDto
            {
                OptionId = x.OptionId, OptionValueId = x.ValueId, TextValue = x.Text
            }).ToList()
        };
    }

    public static AdministratorDto Administrator(AdministratorAccount a) => new()
    {
        Id = a.Id.ToString(), StoreId = a.StoreId, UserName = a.UserName,
        EmailAddress = a.EmailAddress, FirstName = a.FirstName, LastName = a.LastName,
        IsActive = a.IsActive, DefaultLanguageCode = a.DefaultLanguageCode,
        Groups = a.Groups.ToList(), Permissions = a.Permissions.ToList()
    };

    public static CustomerReviewDto Review(ReviewRecord r) => new()
    {
        Id = r.Id.ToString(), ReviewerCustomerId = r.ReviewerCustomerId.ToString(),
        ReviewedCustomerId = r.ReviewedCustomerId.ToString(), Rating = r.Rating,
        Description = r.Description, ReviewDate = r.ReviewDate.ToString("O"),
        Status = r.Status
    };
}
