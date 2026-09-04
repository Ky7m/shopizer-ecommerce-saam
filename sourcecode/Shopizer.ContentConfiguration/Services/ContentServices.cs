using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Shopizer.ContentConfiguration.Data;
using Shopizer.ContentConfiguration.Models;

namespace Shopizer.ContentConfiguration.Services;

public sealed class TokenService(IConfiguration configuration)
{
    private readonly string? _secret = configuration["ContentConfiguration:JwtSecret"];
    public TokenData? Validate(string raw, RequestContext context)
    {
        try
        {
            var pieces = raw.Split('.');
            if (pieces.Length != 3) return null;
            using var document = JsonDocument.Parse(Encoding.UTF8.GetString(FromBase64(pieces[1])));
            var root = document.RootElement;
            if (root.GetProperty("aud").GetString() != "api" ||
                DateTimeOffset.FromUnixTimeSeconds(root.GetProperty("exp").GetInt64()) <= DateTimeOffset.UtcNow)
                return null;
            var tenant = root.GetProperty("tenantId").GetString() ?? "";
            var store = root.GetProperty("storeId").GetString() ?? "";
            if (!tenant.Equals(context.TenantId, StringComparison.Ordinal) ||
                (!string.IsNullOrWhiteSpace(context.StoreId) && !store.Equals(context.StoreId, StringComparison.Ordinal)))
                return null;
            if (!string.IsNullOrWhiteSpace(_secret))
            {
                using var hmac = new HMACSHA512(Encoding.UTF8.GetBytes(_secret));
                var expected = hmac.ComputeHash(Encoding.UTF8.GetBytes($"{pieces[0]}.{pieces[1]}"));
                if (!CryptographicOperations.FixedTimeEquals(expected, FromBase64(pieces[2]))) return null;
            }
            var roles = root.TryGetProperty("roles", out var roleJson)
                ? roleJson.EnumerateArray().Select(x => x.GetString() ?? "").ToArray() : [];
            return new TokenData(Guid.Parse(root.GetProperty("sub").GetString()!), root.GetProperty("kind").GetString()!,
                tenant, store, roles);
        }
        catch (Exception) { return null; }
    }

    private static byte[] FromBase64(string value) =>
        Convert.FromBase64String(value.Replace('-', '+').Replace('_', '/') +
            new string('=', (4 - value.Length % 4) % 4));
}

public sealed record TokenData(Guid Id, string Kind, string TenantId, string StoreId, IReadOnlyList<string> Roles);

public sealed class ModuleCache
{
    private readonly ConcurrentDictionary<string, IReadOnlyList<ModuleRecord>> _values = new();
    public bool TryGet(string family, out IReadOnlyList<ModuleRecord> value) => _values.TryGetValue(family, out value!);
    public void Set(string family, IReadOnlyList<ModuleRecord> value) => _values[family] = value;
    public void Invalidate(string family) => _values.TryRemove(family, out _);
}

public sealed class FileProvider(IConfiguration configuration)
{
    private readonly string _root = configuration["ContentConfiguration:StorageRoot"] ??
        Path.Combine(AppContext.BaseDirectory, "content-storage");
    private string Provider => (configuration["ContentConfiguration:CmsMethod"] ?? "default").ToLowerInvariant();

    public string CurrentProvider => Provider;
    public void EnsureCapability(string operation)
    {
        if (!new[] { "default", "httpd", "aws", "gcp" }.Contains(Provider))
            throw new DomainException("PROVIDER_UNAVAILABLE", $"CMS provider {Provider} is not configured", 503);
        if (Provider is "aws" or "gcp")
            throw new DomainException("PROVIDER_UNAVAILABLE", $"CMS provider {Provider} is not configured in this deployment", 503);
        if ((Provider == "aws" || Provider == "gcp") && operation == "folder")
            throw new DomainException("PROVIDER_CAPABILITY_UNSUPPORTED", $"CMS provider {Provider} does not support folder operations", 501);
        if (Provider == "httpd" && (operation == "read" || operation == "list" || operation == "folder"))
            throw new DomainException("PROVIDER_CAPABILITY_UNSUPPORTED", "File retrieval or listing is not implemented for the local CMS provider", 501);
    }

    public string Key(string store, string type, string folder, string name) =>
        $"files/{store}/{type}/{folder.Trim('/').Replace('/', Path.DirectorySeparatorChar)}{(folder == "/" ? "" : Path.DirectorySeparatorChar)}{name}";

    public async Task WriteAsync(string key, Stream source, CancellationToken ct)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.Combine(_root, key))!);
        await using var target = File.Create(Path.Combine(_root, key));
        await source.CopyToAsync(target, ct);
    }
    public async Task WriteBytesAsync(string key, byte[] bytes, CancellationToken ct) =>
        await WriteAsync(key, new MemoryStream(bytes), ct);
    public async Task<byte[]?> ReadAsync(string key, CancellationToken ct)
    {
        EnsureCapability("read");
        var path = Path.Combine(_root, key);
        return File.Exists(path) ? await File.ReadAllBytesAsync(path, ct) : null;
    }
    public Task DeleteAsync(string key, CancellationToken ct)
    {
        EnsureCapability("delete");
        var path = Path.Combine(_root, key);
        if (File.Exists(path)) File.Delete(path);
        return Task.CompletedTask;
    }
    public void CreateFolder(string folder)
    {
        EnsureCapability("folder");
        Directory.CreateDirectory(Path.Combine(_root, folder.TrimStart('/')));
    }
    public List<string> ListFolders(string folder)
    {
        EnsureCapability("folder");
        var path = Path.Combine(_root, folder.TrimStart('/'));
        return Directory.Exists(path) ? Directory.EnumerateDirectories(path).Select(Path.GetFileName)
            .Where(x => x is not null).Select(x => folder == "/" ? "/" + x : folder + "/" + x).ToList() : [];
    }
    public void DeleteFolder(string folder)
    {
        EnsureCapability("folder");
        var path = Path.Combine(_root, folder.TrimStart('/'));
        if (Directory.Exists(path)) Directory.Delete(path, true);
    }
}

public sealed class ContentService(ContentRepository repository, FileProvider files, EventPublisher events, ILogger<ContentService> logger)
{
    private readonly ConcurrentDictionary<string, Guid> _idempotentCreates = new();
    private static readonly HashSet<string> SupportedLanguages =
        ["en", "fr", "de", "es", "it", "pt", "nl", "ja", "zh", "ko", "ar"];
    // @BR-MER-013: Content codes are unique across page, box, and section records within a tenant/store.
    // @BR-MER-014: The endpoint operation, rather than client input, determines PAGE or BOX content type.
    // @BR-MER-015: Submitted localized descriptions replace the collection while matching each language once.
    // @BR-MER-018: Visibility and menu linkage are persisted as independent content policies.
    public async Task<Guid> SaveContentAsync(JsonElement body, string type, RequestContext ctx, CancellationToken ct, string? idempotencyKey = null)
    {
        var idempotencyHash = idempotencyKey is null ? null :
            $"{ctx.TenantId}:{ctx.StoreId}:{type}:{idempotencyKey}:{Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(body.GetRawText())))}";
        if (idempotencyHash is not null && _idempotentCreates.TryGetValue(idempotencyHash, out var prior)) return prior;
        var code = JsonValue.String(body, "code")?.Trim();
        if (string.IsNullOrWhiteSpace(code)) throw new DomainException("INVALID_CONTENT_REQUEST", "code is required", 400);
        var descriptions = ParseDescriptions(body);
        if (descriptions.Count == 0) throw new DomainException("INVALID_CONTENT_REQUEST", "at least one description is required", 400);
        foreach (var description in descriptions)
            if (string.IsNullOrWhiteSpace(description.Language) || description.Language.Length > 10 ||
                !SupportedLanguages.Contains(description.Language, StringComparer.OrdinalIgnoreCase))
                throw new DomainException("LANGUAGE_NOT_FOUND", "language code is invalid", 422);
        var id = body.TryGetProperty("id", out var idValue) && Guid.TryParse(idValue.GetString(), out var supplied)
            ? supplied : Guid.NewGuid();
        var item = new ContentRecord
        {
            Id = id,
            TenantId = ctx.TenantId,
            StoreId = ctx.StoreId,
            Code = code,
            ContentType = type,
            Visible = JsonValue.Bool(body, "visible"),
            LinkToMenu = JsonValue.Bool(body, "linkToMenu"),
            SortOrder = JsonValue.Int(body, "sortOrder"),
            ContentPosition = JsonValue.String(body, "contentPosition"),
            ProductGroup = JsonValue.String(body, "productGroup")
        };
        await repository.SaveContentAsync(item, descriptions, ctx, ct);
        if (idempotencyHash is not null) _idempotentCreates[idempotencyHash] = id;
        if (item.Visible)
        {
            var persisted = await repository.FindContentByCodeAsync(item.Code, ctx, type, null, false, ct);
            await events.PublishContentPublishedAsync(item, persisted?.Descriptions ?? [], ctx, ct);
        }
        return id;
    }

    // @BR-MER-016: Reads select one language or all available descriptions without changing store scope.
    // @BR-MER-019: Lists are type-scoped, ascending by sortOrder, and paged in the persistence query.
    // @BR-MER-020: Box language projections apply the legacy CDATA/control-character formatting.
    public async Task<(List<ContentRecord> Items, long Total)> ListAsync(RequestContext ctx, string type, string? language, int page, int count, CancellationToken ct) =>
        await repository.ListContentAsync(ctx, type, language, page, count, ct);

    // @BR-MER-016: Code reads use a language-specific or all-language projection and missing codes are not successful.
    public async Task<ContentRecord> FindByCodeAsync(string code, string type, string? language, RequestContext ctx, bool visibleOnly, CancellationToken ct) =>
        await repository.FindContentByCodeAsync(code, ctx, type, language, visibleOnly, ct) ??
        throw new DomainException("CONTENT_NOT_FOUND", $"Content [{code}] was not found for this store.", 404);

    // @BR-MER-017: Friendly URLs return only visible pages whose localized description belongs to the selected store and language.
    public async Task<ContentRecord> FindByFriendlyUrlAsync(string name, string language, RequestContext ctx, CancellationToken ct)
    {
        var item = await repository.FindDescriptionByFriendlyUrlAsync(name, ctx, language, ct);
        return item ?? throw new DomainException("CONTENT_NOT_FOUND", $"Content [{name}] was not found for this store.", 404);
    }

    // @BR-MER-021: Deletes are constrained by tenant, store, identifier, and operation type.
    public Task DeleteAsync(Guid id, string type, RequestContext ctx, CancellationToken ct) =>
        repository.DeleteContentAsync(id, ctx, type, ct);

    public Task<bool> ExistsAsync(string code, RequestContext ctx, CancellationToken ct) =>
        repository.ContentCodeExistsAsync(code, ctx, ct);

    // @BR-MER-022: MIME major type classifies uploads as IMAGE or STATIC_FILE and API aliases normalize before storage.
    // @BR-MER-023: File-manager image uploads reject unsafe basenames before storage is called.
    // @BR-MER-024: File metadata and provider objects are isolated by tenant, store, type, path, and name.
    // @BR-EXT-021: The configured provider is used explicitly and is never silently replaced.
    // @BR-EXT-022: Provider keys use one deterministic store/type/folder/name namespace.
    // @BR-EXT-023: Provider capability failures are returned as explicit API errors.
    public async Task<FileRecord> UploadAsync(string name, string mime, string type, string folder, Stream source, RequestContext ctx, CancellationToken ct, bool strictName = false)
    {
        ValidateName(name, strictName);
        ValidateFolder(folder);
        var requestedType = type.Trim().Replace("_", "").ToUpperInvariant();
        if (requestedType.Length > 0 && requestedType is not ("APIIMAGE" or "APIFILE" or "STATICFILE" or "IMAGE" or
            "LOGO" or "PRODUCT" or "PRODUCTLG" or "PROPERTY" or "VARIANT" or "MANUFACTURER" or "PRODUCTDIGITAL"))
            throw new DomainException("INVALID_REQUEST", "contentType is invalid.", 400);
        var normalized = requestedType switch
        {
            "APIIMAGE" => "IMAGE",
            "APIFILE" => "STATIC_FILE",
            "STATICFILE" or "IMAGE" or "LOGO" or "PRODUCT" or "PRODUCTLG" or "PROPERTY" or
                "VARIANT" or "MANUFACTURER" or "PRODUCTDIGITAL" => requestedType switch
                {
                    "STATICFILE" => "STATIC_FILE",
                    "PRODUCTDIGITAL" => "PRODUCT_DIGITAL",
                    _ => requestedType
                },
            _ => mime.StartsWith("image/", StringComparison.OrdinalIgnoreCase) ? "IMAGE" : "STATIC_FILE"
        };
        var provider = files.CurrentProvider;
        files.EnsureCapability("write");
        var key = files.Key(ctx.StoreId, normalized, folder, name);
        await files.WriteAsync(key, source, ct);
        var record = new FileRecord
        {
            Id = Guid.NewGuid(),
            TenantId = ctx.TenantId,
            StoreId = ctx.StoreId,
            FileName = name,
            MimeType = string.IsNullOrWhiteSpace(mime) ? null : mime,
            ContentType = normalized,
            FolderPath = folder,
            Provider = provider,
            ProviderKey = key,
            State = "AVAILABLE"
        };
        await repository.UpsertFileAsync(record, ct);
        return record;
    }

    // @BR-MER-025: Rename reads, recreates, and removes while retaining the original bytes and metadata.
    // @BR-EXT-029: A renamed file keeps its original content type and MIME type rather than inferring from its extension.
    public async Task RenameAsync(FileRecord original, string newName, RequestContext ctx, CancellationToken ct)
    {
        ValidateName(newName, true);
        files.EnsureCapability("read");
        var bytes = await files.ReadAsync(original.ProviderKey, ct) ??
            throw new DomainException("FILE_NOT_FOUND", $"File {original.FileName} was not found.", 404);
        var newKey = files.Key(ctx.StoreId, original.ContentType, original.FolderPath, newName);
        await files.WriteBytesAsync(newKey, bytes, ct);
        await files.DeleteAsync(original.ProviderKey, ct);
        try { await repository.RenameFileAsync(original, newName, newKey, ct); }
        catch (Exception ex)
        {
            logger.LogError(ex, "File metadata rename failed after provider recreation");
            throw new DomainException("PROVIDER_UNAVAILABLE", "File rename could not be reconciled.", 503);
        }
    }

    // @BR-MER-028: Image listings are store/type scoped and expose deterministic static-image paths.
    public async Task<List<FileRecord>> ImagesAsync(RequestContext ctx, string folder, CancellationToken ct)
    {
        files.EnsureCapability("list");
        return (await repository.ListFilesAsync(ctx, "IMAGE", folder, 0, 500, ct)).Items;
    }

    public async Task<(List<FileRecord> Items, long Total)> ListFilesAsync(RequestContext ctx, string type, string folder, int page, int count, CancellationToken ct)
    {
        files.EnsureCapability("list");
        return await repository.ListFilesAsync(ctx, type, folder, page, count, ct);
    }

    // @BR-EXT-030: File deletion is scoped and repeated deletion is an explicit idempotent success.
    public async Task DeleteFileAsync(FileRecord? file, RequestContext ctx, CancellationToken ct)
    {
        if (file is not null) { await files.DeleteAsync(file.ProviderKey, ct); await repository.DeleteFileAsync(file, ctx, ct); }
    }

    public async Task<FileRecord> FindFileAsync(string name, string type, string folder, RequestContext ctx, CancellationToken ct) =>
        await repository.FindFileAsync(name, type, folder, ctx, ct) ??
        throw new DomainException("FILE_NOT_FOUND", $"File {name} was not found.", 404);

    public async Task<byte[]> ReadFileAsync(FileRecord file, CancellationToken ct) =>
        await files.ReadAsync(file.ProviderKey, ct) ??
        throw new DomainException("FILE_NOT_FOUND", $"File {file.FileName} was not found.", 404);

    // Folder paths and segments use safe Linux-style syntax and provider capability is explicit.
    public void ValidateFolder(string folder)
    {
        if (string.IsNullOrWhiteSpace(folder)) folder = "/";
        if (!System.Text.RegularExpressions.Regex.IsMatch(folder, "^/$|^(/[A-Za-z0-9_-]+)+$"))
            throw new DomainException("INVALID_FOLDER_PATH", "Folder path is not valid Linux-style directory syntax.", 422);
    }

    public void ValidateName(string name, bool strict)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Contains('/') || name.Contains('\\') || name.Contains("..") ||
            (strict && !System.Text.RegularExpressions.Regex.IsMatch(name, "^[A-Za-z0-9][A-Za-z0-9._-]*$")))
            throw new DomainException(strict ? "INVALID_FILENAME" : "INVALID_REQUEST", "Invalid filename", strict ? 422 : 400);
    }

    public void CreateFolder(string folder) { ValidateFolder(folder); files.CreateFolder(folder); }
    public List<string> ListFolders(string folder)
    {
        ValidateFolder(folder); return files.ListFolders(folder);
    }
    public void DeleteFolder(string folder) { ValidateFolder(folder); files.DeleteFolder(folder); }

    private static List<ContentDescriptionInput> ParseDescriptions(JsonElement body)
    {
        if (!body.TryGetProperty("descriptions", out var list) || list.ValueKind != JsonValueKind.Array) return [];
        return list.EnumerateArray().Select(x => new ContentDescriptionInput(
            JsonValue.String(x, "language") ?? "", JsonValue.String(x, "name") ?? "",
            JsonValue.String(x, "title"), JsonValue.String(x, "description"), JsonValue.String(x, "friendlyUrl"),
            JsonValue.String(x, "metaKeywords"), JsonValue.String(x, "metaTitle"), JsonValue.String(x, "metaDescription"))).ToList();
    }
}
