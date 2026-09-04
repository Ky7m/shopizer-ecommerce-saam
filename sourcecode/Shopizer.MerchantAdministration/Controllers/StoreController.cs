using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using Shopizer.MerchantAdministration.DTOs;
using Shopizer.MerchantAdministration.Middleware;
using Shopizer.MerchantAdministration.Models;
using Shopizer.MerchantAdministration.Services;

namespace Shopizer.MerchantAdministration.Controllers;

[ApiController]
[Route("api/v1")]
public sealed class StoreController(StoreService service, FileProviderClient files) : ControllerBase
{
    private static readonly string[] AdminRoles = ["ADMIN", "SUPERADMIN", "ADMIN_RETAIL", "STORE_ADMIN"];
    private void RequireValidModel() { if (!ModelState.IsValid) throw new DomainException("VALIDATION_ERROR", "Request validation failed", 422); }
    private void RequireAdmin() => HttpIdentity.RequireSubject(HttpContext, "administrator", AdminRoles);

    [HttpGet("stores")]
    public async Task<StoreListResponseDto> List(int page = 1, int pageSize = 20, CancellationToken ct = default) { RequireAdmin(); return await service.ListAsync(HttpIdentity.Context(HttpContext), page, pageSize, ct); }
    [HttpPost("stores")]
    public async Task<ActionResult<StoreDto>> Create(CreateStoreRequestDto request, CancellationToken ct) { RequireValidModel(); RequireAdmin(); var context = HttpIdentity.Context(HttpContext); var store = await service.CreateAsync(request, context, ct); return StatusCode(201, DtoMapper.Store(store, request.ParentStoreCode)); }
    [HttpGet("stores/{storeCode}")]
    public async Task<StoreDto> Get(string storeCode, string? language = null, CancellationToken ct = default) { var store = await service.GetAsync(storeCode, HttpIdentity.Context(HttpContext), language, ct); return DtoMapper.Store(store, null); }
    [HttpPut("stores/{storeCode}")]
    public async Task<StoreDto> Update(string storeCode, UpdateStoreRequestDto request, CancellationToken ct) { RequireValidModel(); RequireAdmin(); var store = await service.UpdateAsync(storeCode, request, HttpIdentity.Context(HttpContext), ct); return DtoMapper.Store(store, null); }
    [HttpDelete("stores/{storeCode}")]
    public async Task<IActionResult> Delete(string storeCode, CancellationToken ct) { RequireAdmin(); await service.DeleteAsync(storeCode, HttpIdentity.Context(HttpContext), ct); return NoContent(); }
    [HttpGet("stores/uniqueness")]
    public async Task<EntityExistsResponseDto> Unique([FromQuery] string code, CancellationToken ct) { RequireAdmin(); if (string.IsNullOrWhiteSpace(code)) throw new DomainException("VALIDATION_ERROR", "code is required", 422); return new EntityExistsResponseDto { Exists = await service.ExistsAsync(code, HttpIdentity.Context(HttpContext), ct) }; }
    [HttpGet("stores/names")]
    public async Task<StoreNameListResponseDto> Names(CancellationToken ct) { RequireAdmin(); return await service.NamesAsync(HttpIdentity.Context(HttpContext), ct); }
    [HttpGet("merchants/{merchantCode}/stores")]
    public async Task<StoreListResponseDto> MerchantStores(string merchantCode, int page = 1, int pageSize = 20, CancellationToken ct = default) { RequireAdmin(); return await service.HierarchyAsync(merchantCode, HttpIdentity.Context(HttpContext), page, pageSize, false, ct); }
    [HttpGet("merchants/{merchantCode}/children")]
    public async Task<StoreListResponseDto> Children(string merchantCode, int page = 1, int pageSize = 20, CancellationToken ct = default) { RequireAdmin(); return await service.HierarchyAsync(merchantCode, HttpIdentity.Context(HttpContext), page, pageSize, true, ct); }
    [HttpGet("stores/{storeCode}/languages")]
    public async Task<LanguageListResponseDto> Languages(string storeCode, CancellationToken ct) => await service.LanguagesAsync(storeCode, HttpIdentity.Context(HttpContext), ct);
    [HttpPut("stores/{storeCode}/languages")]
    public async Task<StoreDto> ReplaceLanguages(string storeCode, ReplaceLanguagesRequestDto request, CancellationToken ct) { RequireValidModel(); RequireAdmin(); var store = await service.ReplaceLanguagesAsync(storeCode, request, HttpIdentity.Context(HttpContext), ct); return DtoMapper.Store(store, null); }
    [HttpGet("stores/{storeCode}/branding")]
    public async Task<BrandingDto> Branding(string storeCode, CancellationToken ct) => await service.GetBrandingAsync(storeCode, HttpIdentity.Context(HttpContext), ct);
    [HttpPut("stores/{storeCode}/branding")]
    public async Task<BrandingDto> UpdateBranding(string storeCode, BrandingRequestDto request, CancellationToken ct) { RequireValidModel(); RequireAdmin(); return await service.UpdateBrandingAsync(storeCode, request, HttpIdentity.Context(HttpContext), ct); }
    [HttpPost("stores/{storeCode}/branding/logo")]
    public async Task<ActionResult<BrandingDto>> UploadLogo(string storeCode, CancellationToken ct)
    {
        RequireAdmin();
        string file;
        if (Request.HasFormContentType)
        {
            var form = await Request.ReadFormAsync(ct);
            var upload = form.Files.GetFile("file") ?? throw new DomainException("VALIDATION_ERROR", "file is required", 422);
            await using var stream = upload.OpenReadStream();
            using var memory = new MemoryStream();
            await stream.CopyToAsync(memory, ct);
            file = Convert.ToBase64String(memory.ToArray());
        }
        else
        {
            var request = await JsonSerializer.DeserializeAsync<LogoUploadRequestDto>(Request.Body, cancellationToken: ct)
                ?? throw new DomainException("VALIDATION_ERROR", "file is required", 422);
            if (string.IsNullOrWhiteSpace(request.File)) throw new DomainException("VALIDATION_ERROR", "file is required", 422);
            file = request.File;
        }
        var context = HttpIdentity.Context(HttpContext);
        var store = await service.GetAsync(storeCode, context, null, ct);
        store.LogoUri = await files.UploadAsync(store, file, context, ct);
        return StatusCode(201, await service.UpdateBrandingAsync(storeCode, new BrandingRequestDto { LogoUri = store.LogoUri }, context, ct));
    }
    [HttpDelete("stores/{storeCode}/branding/logo")]
    public async Task<IActionResult> DeleteLogo(string storeCode, CancellationToken ct) { RequireAdmin(); var context = HttpIdentity.Context(HttpContext); var store = await service.GetAsync(storeCode, context, null, ct); await files.DeleteAsync(store, context, ct); await service.UpdateBrandingAsync(storeCode, new BrandingRequestDto { LogoUri = "" }, context, ct); return NoContent(); }
    [HttpPost("stores/signup")]
    public async Task<ActionResult<SignupResponseDto>> Signup(CreateStoreRequestDto request, CancellationToken ct) { RequireValidModel(); return Accepted(await service.CreateSignupAsync(request, HttpIdentity.Context(HttpContext), ct)); }
    [HttpGet("stores/{storeCode}/signup/{token}")]
    public async Task<SignupVerificationResponseDto> VerifySignup(string storeCode, string token, CancellationToken ct) => await service.VerifySignupAsync(storeCode, token, HttpIdentity.Context(HttpContext), ct);
}
