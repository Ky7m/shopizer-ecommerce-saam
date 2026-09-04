using System.Security.Claims;
using System.Text.Json;
using Shopizer.ContentConfiguration.DTOs;

namespace Shopizer.ContentConfiguration.Models;

public sealed record RequestContext(string TenantId, string StoreId, string CorrelationId)
{
    public static RequestContext From(HttpContext http, bool storeRequired = true)
    {
        var tenant = http.Request.Headers["x-tenant-id"].FirstOrDefault();
        var store = http.Request.Headers["x-store-id"].FirstOrDefault();
        var correlation = http.Request.Headers["x-correlation-id"].FirstOrDefault();
        if (!Guid.TryParse(tenant, out _) || (storeRequired && !Guid.TryParse(store, out _)) ||
            string.IsNullOrWhiteSpace(correlation))
            throw new DomainException("INVALID_REQUEST_CONTEXT",
                "x-tenant-id, x-store-id, and x-correlation-id are required and valid", 400);
        return new RequestContext(tenant!, store ?? "", correlation!);
    }
}

public sealed class DomainException(string code, string message, int statusCode) : Exception(message)
{
    public string Code { get; } = code;
    public int StatusCode { get; } = statusCode;
}

public sealed record ContentDescription(
    Guid Id, string Language, string Name, string? Title, string? Description,
    string? FriendlyUrl, string? MetaKeywords, string? MetaTitle, string? MetaDescription);

public sealed class ContentRecord
{
    public Guid Id { get; init; }
    public string TenantId { get; init; } = "";
    public string StoreId { get; init; } = "";
    public string Code { get; set; } = "";
    public string ContentType { get; set; } = "PAGE";
    public string? ContentPosition { get; set; }
    public bool LinkToMenu { get; set; }
    public string? ProductGroup { get; set; }
    public int SortOrder { get; set; }
    public bool Visible { get; set; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
    public string? ModifiedBy { get; init; }
    public List<ContentDescription> Descriptions { get; } = [];
}

public sealed class FileRecord
{
    public Guid Id { get; init; }
    public string TenantId { get; init; } = "";
    public string StoreId { get; init; } = "";
    public string FileName { get; init; } = "";
    public string? MimeType { get; init; }
    public string ContentType { get; init; } = "STATIC_FILE";
    public string FolderPath { get; init; } = "/";
    public string Provider { get; init; } = "default";
    public string ProviderKey { get; init; } = "";
    public string State { get; init; } = "AVAILABLE";
}

public sealed record ModuleEnvironment(
    string Env, string? Scheme, string? Host, string? Port, string? Uri,
    string? Config1, string? Config2);

public sealed class ModuleRecord
{
    public Guid Id { get; init; }
    public string Family { get; init; } = "";
    public string Code { get; init; } = "";
    public string? Type { get; init; }
    public string? Image { get; init; }
    public bool CustomModule { get; init; }
    public List<string> Regions { get; init; } = [];
    public Dictionary<string, object?> Details { get; init; } = [];
    public List<ModuleEnvironment> Configuration { get; init; } = [];
}

public sealed record ModuleState(string Code, bool Active, bool DefaultSelected, string Environment,
    Dictionary<string, object?> Keys, Dictionary<string, object?> Options);

public sealed class AuthenticatedIdentity(Guid id, string kind, IReadOnlyList<string> roles)
{
    public Guid Id { get; } = id;
    public string Kind { get; } = kind;
    public IReadOnlyList<string> Roles { get; } = roles;
}

public static class PrincipalExtensions
{
    public static Guid? SubjectId(this ClaimsPrincipal principal) =>
        Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier) ??
                      principal.FindFirstValue("sub"), out var id) ? id : null;
    public static string Kind(this ClaimsPrincipal principal) => principal.FindFirstValue("kind") ?? "";
    public static bool HasRole(this ClaimsPrincipal principal, params string[] roles) =>
        roles.Length == 0 || roles.Any(role => principal.IsInRole(role) ||
            principal.Claims.Any(c => (c.Type == ClaimTypes.Role || c.Type == "role") &&
                c.Value.Equals(role, StringComparison.OrdinalIgnoreCase)));
}

public static class DtoMapper
{
    public static ContentItemDto Content(ContentRecord content, string? language, bool box)
    {
        var descriptions = content.Descriptions.Select(d => Description(d, box && language is not null)).ToList();
        return new ContentItemDto
        {
            Id = content.Id.ToString(),
            Code = content.Code,
            ContentType = TitleCase(content.ContentType),
            SortOrder = content.SortOrder,
            Visible = content.Visible,
            Descriptions = language is null ? descriptions : null
        };
    }

    public static ContentDescriptionDto Description(ContentDescription d, bool cdata) => new()
    {
        Id = d.Id.ToString(),
        Language = d.Language,
        Name = d.Name,
        // The generated DTO is intentionally copied verbatim and contains the
        // contract's required identity fields. Full projections use anonymous
        // response objects in the controller for optional fields.
    };

    public static string TitleCase(string type) =>
        type.Equals("BOX", StringComparison.OrdinalIgnoreCase) ? "Box" :
        type.Equals("SECTION", StringComparison.OrdinalIgnoreCase) ? "Section" : "Page";

    public static string? CData(string? value) =>
        value is null ? null : "<![CDATA[" + value.Replace("\r", "").Replace("\n", "").Replace("\t", "") + "]]>";
}

public static class JsonValue
{
    public static string? String(JsonElement root, string name) =>
        root.TryGetProperty(name, out var value) && value.ValueKind != JsonValueKind.Null
            ? value.GetString() : null;
    public static bool Bool(JsonElement root, string name, bool fallback = false) =>
        root.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.True
            ? true : root.TryGetProperty(name, out value) && value.ValueKind == JsonValueKind.False
                ? false : fallback;
    public static int Int(JsonElement root, string name, int fallback = 0) =>
        root.TryGetProperty(name, out var value) && value.TryGetInt32(out var result) ? result : fallback;
    public static Dictionary<string, object?> Object(JsonElement root, string name) =>
        root.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.Object
            ? value.EnumerateObject().ToDictionary(x => x.Name, x => (object?)x.Value.Clone()) : [];
    public static Dictionary<string, object?> CleanObject(Dictionary<string, object?> values, bool paths = false) =>
        values.Where(x => x.Value is not null &&
            (!paths || x.Value is not JsonElement e || (e.ValueKind == JsonValueKind.String &&
                !string.IsNullOrWhiteSpace(e.GetString()))))
            .ToDictionary(x => x.Key, x => x.Value);
}
