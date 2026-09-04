using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Shopizer.ContentConfiguration.Middleware;
using Shopizer.ContentConfiguration.Models;
using Shopizer.ContentConfiguration.Services;

namespace Shopizer.ContentConfiguration.Controllers;

[ApiController]
[Route("api/v1")]
public sealed class ContentController(ContentService service) : ControllerBase
{
    [HttpGet("content/pages")]
    public async Task<IActionResult> PublicPages([FromQuery] int page = 0, [FromQuery] int count = 20, [FromQuery] string? language = null, CancellationToken ct = default) =>
        List(await service.ListAsync(HttpIdentity.Context(HttpContext), "PAGE", language, page, count, ct), false);

    [HttpGet("private/content/pages")]
    public async Task<IActionResult> PrivatePages([FromQuery] int page = 0, [FromQuery] int count = 20, [FromQuery] string? language = null, CancellationToken ct = default)
    {
        RequireAdmin(); return List(await service.ListAsync(HttpIdentity.Context(HttpContext), "PAGE", language, page, count, ct), false);
    }

    [HttpGet("content/pages/{code}")]
    public async Task<IActionResult> PublicPage(string code, [FromHeader(Name = "x-language")] string? language, CancellationToken ct = default) =>
        Ok(Project(await service.FindByCodeAsync(code, "PAGE", language, HttpIdentity.Context(HttpContext), false, ct), false, language));

    [HttpGet("private/content/pages/{code}")]
    public async Task<IActionResult> PrivatePage(string code, [FromHeader(Name = "x-language")] string? language, CancellationToken ct = default)
    {
        RequireAdmin(); return Ok(Project(await service.FindByCodeAsync(code, "PAGE", language, HttpIdentity.Context(HttpContext), false, ct), false, language));
    }

    [HttpGet("content/pages/name/{name}")]
    public async Task<IActionResult> PageByName(string name, [FromHeader(Name = "x-language")] string language, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(language)) throw new DomainException("INVALID_REQUEST_CONTEXT", "x-language is required", 400);
        return Ok(Project(await service.FindByFriendlyUrlAsync(name, language, HttpIdentity.Context(HttpContext), ct), false, language));
    }

    [HttpGet("private/content/boxes")]
    public async Task<IActionResult> PrivateBoxes([FromQuery] int page = 0, [FromQuery] int count = 20, [FromQuery] string? language = null, CancellationToken ct = default)
    {
        RequireAdmin(); return List(await service.ListAsync(HttpIdentity.Context(HttpContext), "BOX", language, page, count, ct), true);
    }

    [HttpGet("content/boxes")]
    public async Task<IActionResult> PublicBoxes([FromQuery] int page = 0, [FromQuery] int count = 20, [FromQuery] string? language = null, CancellationToken ct = default) =>
        List(await service.ListAsync(HttpIdentity.Context(HttpContext), "BOX", language, page, count, ct), true);

    [HttpGet("private/content/boxes/{code}")]
    public async Task<IActionResult> PrivateBox(string code, [FromHeader(Name = "x-language")] string? language, CancellationToken ct = default)
    {
        RequireAdmin(); return Ok(Project(await service.FindByCodeAsync(code, "BOX", language, HttpIdentity.Context(HttpContext), false, ct), true, language));
    }

    [HttpGet("content/boxes/{code}")]
    public async Task<IActionResult> PublicBox(string code, [FromHeader(Name = "x-language")] string? language, CancellationToken ct = default) =>
        Ok(Project(await service.FindByCodeAsync(code, "BOX", language, HttpIdentity.Context(HttpContext), false, ct), true, language));

    [HttpPost("private/content/page")]
    public async Task<IActionResult> CreatePage([FromBody] JsonElement body, CancellationToken ct = default)
    {
        RequireAdmin(); RequireIdempotency();
        return StatusCode(201, new { id = await service.SaveContentAsync(body, "PAGE", HttpIdentity.Context(HttpContext), ct, Request.Headers["Idempotency-Key"].FirstOrDefault()) });
    }

    [HttpPut("private/content/page/{contentId:guid}")]
    public async Task<IActionResult> UpdatePage(Guid contentId, [FromBody] JsonElement body, CancellationToken ct = default)
    {
        RequireAdmin(); var context = HttpIdentity.Context(HttpContext);
        await service.SaveContentAsync(WithId(body, contentId), "PAGE", context, ct); return NoContent();
    }

    [HttpDelete("private/content/page/{contentId:guid}")]
    public async Task<IActionResult> DeletePage(Guid contentId, CancellationToken ct = default)
    {
        RequireAdmin(); await service.DeleteAsync(contentId, "PAGE", HttpIdentity.Context(HttpContext), ct); return NoContent();
    }

    [HttpGet("private/content/page/{code}/exists")]
    public async Task<IActionResult> PageExists(string code, CancellationToken ct = default)
    {
        RequireAdmin(); return Ok(new { exists = await service.ExistsAsync(code, HttpIdentity.Context(HttpContext), ct) });
    }

    [HttpPost("private/content/box")]
    public async Task<IActionResult> CreateBox([FromBody] JsonElement body, CancellationToken ct = default)
    {
        RequireAdmin(); RequireIdempotency();
        return StatusCode(201, new { id = await service.SaveContentAsync(body, "BOX", HttpIdentity.Context(HttpContext), ct, Request.Headers["Idempotency-Key"].FirstOrDefault()) });
    }

    [HttpPut("private/content/box/{contentId:guid}")]
    public async Task<IActionResult> UpdateBox(Guid contentId, [FromBody] JsonElement body, CancellationToken ct = default)
    {
        RequireAdmin(); await service.SaveContentAsync(WithId(body, contentId), "BOX", HttpIdentity.Context(HttpContext), ct); return NoContent();
    }

    [HttpDelete("private/content/box/{contentId:guid}")]
    public async Task<IActionResult> DeleteBox(Guid contentId, CancellationToken ct = default)
    {
        RequireAdmin(); await service.DeleteAsync(contentId, "BOX", HttpIdentity.Context(HttpContext), ct); return NoContent();
    }

    [HttpGet("private/content/box/{code}/exists")]
    public async Task<IActionResult> BoxExists(string code, CancellationToken ct = default)
    {
        RequireAdmin(); return Ok(new { exists = await service.ExistsAsync(code, HttpIdentity.Context(HttpContext), ct) });
    }

    [HttpGet("private/content/files")]
    public async Task<IActionResult> Files([FromQuery] string contentType, [FromQuery] string path = "/",
        [FromQuery] int page = 0, [FromQuery] int count = 100, CancellationToken ct = default)
    {
        RequireAdmin(); var result = await service.ListFilesAsync(HttpIdentity.Context(HttpContext), NormalizeType(contentType), path, page, count, ct);
        return Ok(new
        {
            items = result.Items.Select(FileResponse),
            page,
            count,
            number = result.Items.Count,
            totalPages = count == 0 ? 0 : (int)Math.Ceiling(result.Total / (double)count),
            recordsTotal = result.Total,
            recordsFiltered = result.Total
        });
    }

    [HttpPost("private/content/files")]
    public async Task<IActionResult> Upload(CancellationToken ct = default)
    {
        RequireAdmin(); RequireIdempotency();
        var form = await Request.ReadFormAsync(ct);
        var file = form.Files.GetFile("file") ?? throw new DomainException("INVALID_REQUEST", "file is required", 400);
        var name = form["fileName"].FirstOrDefault() ?? file.FileName;
        var type = form["contentType"].FirstOrDefault() ?? "";
        var folder = form["path"].FirstOrDefault() ?? "/";
        var record = await service.UploadAsync(name, file.ContentType, type, folder, file.OpenReadStream(),
            HttpIdentity.Context(HttpContext), ct);
        return StatusCode(201, FileResponse(record));
    }

    [HttpGet("private/content/list")]
    public async Task<IActionResult> ImageList([FromQuery] string? parentPath = "/", CancellationToken ct = default)
    {
        RequireAdmin();
        var path = string.IsNullOrWhiteSpace(parentPath) || Uri.UnescapeDataString(parentPath).Contains("/images", StringComparison.OrdinalIgnoreCase)
            ? "/" : Uri.UnescapeDataString(parentPath);
        service.ValidateFolder(path);
        var ctx = HttpIdentity.Context(HttpContext);
        var result = await service.ImagesAsync(ctx, path, ct);
        return Ok(result.Select(x => new
        {
            url = $"/static/images/{ctx.StoreId}/{x.FileName}",
            name = x.FileName,
            size = (long?)null,
            dir = false,
            path = x.ProviderKey,
            id = $"/static/images/{ctx.StoreId}/{x.FileName}"
        }));
    }

    [HttpGet("private/content/folder")]
    public async Task<IActionResult> ImageFolder([FromQuery] string path = "/", CancellationToken ct = default)
    {
        RequireAdmin(); service.ValidateFolder(path);
        var ctx = HttpIdentity.Context(HttpContext);
        var files = await service.ImagesAsync(ctx, path, ct);
        return Ok(new { path, content = files.Select(x => new { name = x.FileName, path = $"/static/images/{ctx.StoreId}/{x.FileName}" }) });
    }

    [HttpPost("private/content/images/add")]
    public async Task<IActionResult> AddImage(CancellationToken ct = default)
    {
        RequireAdmin(); RequireIdempotency();
        var form = await Request.ReadFormAsync(ct);
        var file = form.Files.GetFile("qqfile") ?? throw new DomainException("INVALID_REQUEST", "qqfile is required", 400);
        var name = form["qqfilename"].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(name)) throw new DomainException("INVALID_FILENAME", "Invalid filename", 422);
        try
        {
            await service.UploadAsync(name, file.ContentType, "IMAGE", "/", file.OpenReadStream(), HttpIdentity.Context(HttpContext), ct, true);
            return StatusCode(201, new { success = true, error = (string?)null, preventRetry = true });
        }
        catch (DomainException ex) when (ex.Code == "INVALID_FILENAME")
        {
            return StatusCode(422, new { success = false, error = "Invalid filename", preventRetry = true });
        }
    }

    [HttpPost("private/file")]
    public async Task<IActionResult> LegacyFile(CancellationToken ct = default)
    {
        RequireAdmin(); RequireIdempotency();
        var form = await Request.ReadFormAsync(ct);
        var file = form.Files.GetFile("file") ?? throw new DomainException("INVALID_REQUEST", "file is required", 400);
        var record = await service.UploadAsync(file.FileName, file.ContentType, "", "/", file.OpenReadStream(), HttpIdentity.Context(HttpContext), ct);
        return StatusCode(201, FileResponse(record));
    }

    [HttpPost("private/files")]
    public async Task<IActionResult> LegacyFiles(CancellationToken ct = default)
    {
        RequireAdmin(); RequireIdempotency();
        var form = await Request.ReadFormAsync(ct);
        var files = form.Files.GetFiles("file");
        if (files.Count == 0) throw new DomainException("INVALID_REQUEST", "file is required", 400);
        var result = new List<object>();
        foreach (var file in files)
        {
            var record = await service.UploadAsync(file.FileName, file.ContentType, "", "/", file.OpenReadStream(),
                HttpIdentity.Context(HttpContext), ct);
            result.Add(FileResponse(record));
        }
        return StatusCode(201, new { items = result });
    }

    [HttpGet("private/content/files/{fileName}/download")]
    public async Task<IActionResult> Download(string fileName, [FromQuery] string contentType, [FromQuery] string path = "/", CancellationToken ct = default)
    {
        RequireAdmin(); service.ValidateFolder(path);
        var file = await service.FindFileAsync(fileName, NormalizeType(contentType), path, HttpIdentity.Context(HttpContext), ct);
        return File(await service.ReadFileAsync(file, ct), file.MimeType ?? "application/octet-stream", file.FileName);
    }

    [HttpPost("private/content/images/rename")]
    public async Task<IActionResult> RenameImage([FromBody] JsonElement body, CancellationToken ct = default)
    {
        RequireAdmin(); RequireIdempotency();
        var path = JsonValue.String(body, "path") ?? "";
        var newName = JsonValue.String(body, "newName") ?? "";
        var name = Path.GetFileName(Uri.UnescapeDataString(path));
        var file = await service.FindFileAsync(name, "IMAGE", "/", HttpIdentity.Context(HttpContext), ct);
        await service.RenameAsync(file, newName, HttpIdentity.Context(HttpContext), ct);
        return Ok(new { success = true, error = (string?)null, preventRetry = true });
    }

    [HttpPost("private/content/files/rename")]
    public async Task<IActionResult> Rename([FromBody] JsonElement body, CancellationToken ct = default)
    {
        RequireAdmin(); RequireIdempotency();
        var name = JsonValue.String(body, "fileName") ?? "";
        var newName = JsonValue.String(body, "newName") ?? "";
        var type = NormalizeType(JsonValue.String(body, "contentType") ?? "");
        var path = JsonValue.String(body, "path") ?? "/";
        var file = await service.FindFileAsync(name, type, path, HttpIdentity.Context(HttpContext), ct);
        await service.RenameAsync(file, newName, HttpIdentity.Context(HttpContext), ct);
        return Ok(new { success = true, error = (string?)null, preventRetry = true });
    }

    [HttpDelete("private/content/images/remove")]
    public async Task<IActionResult> RemoveImage([FromQuery] string path, CancellationToken ct = default)
    {
        RequireAdmin(); var ctx = HttpIdentity.Context(HttpContext);
        FileRecord? file = null;
        try { file = await service.FindFileAsync(Path.GetFileName(Uri.UnescapeDataString(path)), "IMAGE", "/", ctx, ct); }
        catch (DomainException ex) when (ex.Code == "FILE_NOT_FOUND") { return NoContent(); }
        await service.DeleteFileAsync(file, ctx, ct); return NoContent();
    }

    [HttpDelete("private/content/files/{fileName}")]
    public async Task<IActionResult> Remove(string fileName, [FromQuery] string contentType, [FromQuery] string path = "/", CancellationToken ct = default)
    {
        RequireAdmin(); var ctx = HttpIdentity.Context(HttpContext);
        FileRecord? file = null;
        try { file = await service.FindFileAsync(fileName, NormalizeType(contentType), path, ctx, ct); }
        catch (DomainException ex) when (ex.Code == "FILE_NOT_FOUND") { return NoContent(); }
        await service.DeleteFileAsync(file, ctx, ct); return NoContent();
    }

    [HttpPost("private/content/folders")]
    public IActionResult CreateFolder([FromBody] JsonElement body)
    {
        RequireAdmin(); var parent = JsonValue.String(body, "path") ?? "/";
        var name = JsonValue.String(body, "folderName") ?? "";
        service.ValidateFolder(parent); service.ValidateFolder(parent == "/" ? "/" + name : parent + "/" + name);
        var folder = parent == "/" ? "/" + name : parent + "/" + name;
        service.CreateFolder(folder);
        return StatusCode(201, new { path = folder, created = true });
    }

    [HttpGet("private/content/folders")]
    public IActionResult ListFolders([FromQuery] string path = "/")
    {
        RequireAdmin(); service.ValidateFolder(path);
        return Ok(new { path, folders = service.ListFolders(path) });
    }

    [HttpDelete("private/content/folders")]
    public IActionResult DeleteFolder([FromQuery] string folderName, [FromQuery] string path = "/")
    {
        RequireAdmin(); service.ValidateFolder(path);
        if (string.IsNullOrWhiteSpace(folderName) || folderName.Contains('/') || folderName.Contains('\\') || folderName.Contains(".."))
            throw new DomainException("INVALID_FOLDER_PATH", "Invalid folder path.", 422);
        service.DeleteFolder(path == "/" ? "/" + folderName : path + "/" + folderName);
        return NoContent();
    }

    [HttpGet("private/content/any/{code}")]
    public async Task<IActionResult> Any(string code, CancellationToken ct = default)
    {
        RequireAdmin();
        var ctx = HttpIdentity.Context(HttpContext);
        ContentRecord? page = null;
        try { page = await service.FindByCodeAsync(code, "PAGE", null, ctx, false, ct); }
        catch (DomainException ex) when (ex.Code == "CONTENT_NOT_FOUND") { }
        ContentRecord? box = null;
        if (page is null)
            try { box = await service.FindByCodeAsync(code, "BOX", null, ctx, false, ct); }
            catch (DomainException ex) when (ex.Code == "CONTENT_NOT_FOUND") { }
        var item = page ?? box ?? await service.FindByCodeAsync(code, "SECTION", null, ctx, false, ct);
        return Ok(new
        {
            id = item.Id,
            code = item.Code,
            contentType = DtoMapper.TitleCase(item.ContentType),
            visible = item.Visible,
            displayedInMenu = item.LinkToMenu,
            descriptions = item.Descriptions.Select(x => Description(x))
        });
    }

    [HttpGet("private/contents/any")]
    public async Task<IActionResult> AnyList(CancellationToken ct = default)
    {
        RequireAdmin(); var ctx = HttpIdentity.Context(HttpContext);
        var pages = await service.ListAsync(ctx, "PAGE", null, 0, 500, ct);
        var boxes = await service.ListAsync(ctx, "BOX", null, 0, 500, ct);
        var sections = await service.ListAsync(ctx, "SECTION", null, 0, 500, ct);
        return Ok(pages.Items.Concat(boxes.Items).Concat(sections.Items).Select(x => new
        {
            id = x.Id,
            code = x.Code,
            contentType = DtoMapper.TitleCase(x.ContentType),
            visible = x.Visible,
            displayedInMenu = x.LinkToMenu,
            descriptions = x.Descriptions.Select(d => Description(d))
        }));
    }

    [HttpGet("content/summary")]
    [HttpDelete("content/folder")]
    [HttpPut("private/content/{contentId}")]
    [HttpDelete("private/content/{contentId}")]
    public IActionResult Retired() => throw new DomainException("LEGACY_OPERATION_RETIRED",
        "This legacy operation was explicitly nonfunctional and is not part of the target contract.", 410);

    [HttpGet("content/images/download")]
    public async Task<IActionResult> DownloadPublicImage([FromQuery] string path, CancellationToken ct = default)
    {
        var ctx = HttpIdentity.Context(HttpContext);
        var name = Path.GetFileName(Uri.UnescapeDataString(path));
        var file = await service.FindFileAsync(name, "IMAGE", "/", ctx, ct);
        var bytes = await service.ReadFileAsync(file, ct);
        return File(bytes, file.MimeType ?? "application/octet-stream", file.FileName);
    }

    private void RequireAdmin() => HttpIdentity.RequireAdministrator(HttpContext);
    private void RequireIdempotency()
    {
        if (string.IsNullOrWhiteSpace(Request.Headers["Idempotency-Key"]))
            throw new DomainException("INVALID_REQUEST", "Idempotency-Key is required.", 400);
    }

    private IActionResult List((List<ContentRecord> Items, long Total) result, bool box)
    {
        var page = int.TryParse(Request.Query["page"], out var p) ? p : 0;
        var count = int.TryParse(Request.Query["count"], out var c) ? c : 20;
        return Ok(new
        {
            items = result.Items.Select(x => Project(x, box, Request.Query["language"].FirstOrDefault())),
            page,
            count,
            number = result.Items.Count,
            totalPages = count == 0 ? 0 : (int)Math.Ceiling(result.Total / (double)count),
            recordsTotal = result.Total,
            recordsFiltered = result.Total
        });
    }

    private static object Project(ContentRecord item, bool box, string? language) => new
    {
        id = item.Id,
        code = item.Code,
        contentType = DtoMapper.TitleCase(item.ContentType),
        contentPosition = item.ContentPosition,
        linkToMenu = item.LinkToMenu,
        productGroup = item.ProductGroup,
        sortOrder = item.SortOrder,
        visible = item.Visible,
        description = language is null ? null : item.Descriptions.Select(x => Description(x, box)).FirstOrDefault(),
        descriptions = language is null ? item.Descriptions.Select(x => Description(x, false)) : null,
        tenantId = item.TenantId,
        storeId = item.StoreId,
        createdAt = item.CreatedAt,
        updatedAt = item.UpdatedAt,
        modifiedBy = item.ModifiedBy
    };
    private static object Description(ContentDescription x, bool cdata = false) => new
    {
        id = x.Id,
        language = x.Language,
        name = x.Name,
        title = x.Title,
        description = cdata ? DtoMapper.CData(x.Description) : x.Description,
        friendlyUrl = x.FriendlyUrl,
        metaKeywords = x.MetaKeywords,
        metaTitle = x.MetaTitle,
        metaDescription = x.MetaDescription
    };
    private static object FileResponse(FileRecord x) => new
    {
        id = x.Id,
        fileName = x.FileName,
        mimeType = x.MimeType,
        contentType = FileType(x.ContentType),
        path = x.FolderPath,
        provider = x.Provider,
        state = x.State,
        downloadPath = $"/api/v1/private/content/files/{Uri.EscapeDataString(x.FileName)}/download?contentType={Uri.EscapeDataString(x.ContentType)}&path={Uri.EscapeDataString(x.FolderPath)}"
    };
    private static string FileType(string type) => type.ToUpperInvariant() switch
    {
        "STATIC_FILE" => "StaticFile",
        "IMAGE" => "Image",
        "LOGO" => "Logo",
        "PRODUCT" => "Product",
        "PRODUCTLG" => "ProductLg",
        "PROPERTY" => "Property",
        "VARIANT" => "Variant",
        "MANUFACTURER" => "Manufacturer",
        "PRODUCT_DIGITAL" => "ProductDigital",
        "API_IMAGE" => "ApiImage",
        "API_FILE" => "ApiFile",
        _ => DtoMapper.TitleCase(type)
    };
    private static string NormalizeType(string type) => type.Trim().ToUpperInvariant() switch
    {
        "STATICFILE" or "STATIC_FILE" => "STATIC_FILE",
        "IMAGE" => "IMAGE",
        "APIIMAGE" or "API_IMAGE" => "IMAGE",
        "APIFILE" or "API_FILE" => "STATIC_FILE",
        "PRODUCTLG" or "PRODUCT_LG" => "PRODUCTLG",
        var value => value
    };
    private static JsonElement WithId(JsonElement body, Guid id)
    {
        using var document = JsonDocument.Parse(body.GetRawText());
        var map = document.RootElement.EnumerateObject().ToDictionary(x => x.Name, x => (object?)x.Value.Clone());
        map["id"] = id.ToString();
        return JsonDocument.Parse(JsonSerializer.Serialize(map)).RootElement.Clone();
    }
}
