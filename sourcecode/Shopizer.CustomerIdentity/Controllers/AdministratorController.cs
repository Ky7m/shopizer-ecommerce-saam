using Microsoft.AspNetCore.Mvc;
using Shopizer.CustomerIdentity.Data;
using Shopizer.CustomerIdentity.DTOs;
using Shopizer.CustomerIdentity.Middleware;
using Shopizer.CustomerIdentity.Models;
using Shopizer.CustomerIdentity.Services;

namespace Shopizer.CustomerIdentity.Controllers;

[ApiController]
[Route("api/v1")]
public sealed class AdministratorController(IdentityService service, IdentityRepository repository) : ControllerBase
{
    [HttpPost("admin-auth/login")]
    public async Task<AuthenticationResponseDto> Login(AuthenticationRequestDto request, CancellationToken ct) => await service.LoginAsync(request, HttpIdentity.Context(HttpContext), true, ct);

    [HttpGet("admin-auth/refresh")]
    public async Task<AuthenticationResponseDto> Refresh(CancellationToken ct)
    {
        var context = HttpIdentity.Context(HttpContext); HttpIdentity.RequireSubject(HttpContext, "administrator");
        var raw = Request.Headers.Authorization.ToString();
        var token = await HttpContext.RequestServices.GetRequiredService<TokenService>().ValidateAsync(raw[7..].Trim(), context, ct) ?? throw new DomainException("REFRESH_NOT_ALLOWED", "Token cannot be refreshed", 400);
        return await service.RefreshAsync(token, context, ct);
    }

    [HttpGet("users")]
    public async Task<AdministratorListResponseDto> List(int page = 1, int pageSize = 20, string? emailAddress = null, CancellationToken ct = default)
    {
        HttpIdentity.RequireSubject(HttpContext, "administrator", "ADMIN", "SUPERADMIN", "ADMIN_RETAIL", "STORE_ADMIN");
        return await service.ListAdminsAsync(HttpIdentity.Context(HttpContext), page, pageSize, emailAddress, ct);
    }

    [HttpPost("users")]
    public async Task<ActionResult<AdministratorDto>> Create(CreateAdministratorRequestDto request, CancellationToken ct)
    {
        var actor = HttpIdentity.RequireSubject(HttpContext, "administrator", "ADMIN", "SUPERADMIN", "ADMIN_RETAIL", "STORE_ADMIN");
        var context = HttpIdentity.Context(HttpContext);
        return StatusCode(201, await service.CreateAdminAsync(request, context, await IsSuper(actor, context, ct), ct));
    }

    [HttpGet("users/{userId}")]
    public async Task<AdministratorDto> Get(string userId, CancellationToken ct)
    {
        HttpIdentity.RequireSubject(HttpContext, "administrator", "ADMIN", "SUPERADMIN", "ADMIN_RETAIL", "STORE_ADMIN");
        var a = await repository.FindAdminAsync(Guid.Parse(userId), HttpIdentity.Context(HttpContext), ct) ?? throw new DomainException("USER_NOT_FOUND", "User was not found in this store", 404);
        return DtoMapper.Administrator(a);
    }

    [HttpGet("users/me")]
    public async Task<AdministratorDto> Me(CancellationToken ct)
    {
        var id = HttpIdentity.RequireSubject(HttpContext, "administrator"); var a = await repository.FindAdminAsync(id, HttpIdentity.Context(HttpContext), ct) ?? throw new DomainException("USER_NOT_FOUND", "User was not found in this store", 404); if (!a.IsActive) throw new DomainException("USER_INACTIVE", "User account is inactive", 401); return DtoMapper.Administrator(a);
    }

    [HttpPost("users/unique")]
    public async Task<EntityExistsResponseDto> Unique(UniqueUsernameRequestDto request, CancellationToken ct)
    {
        HttpIdentity.RequireSubject(HttpContext, "administrator", "ADMIN", "SUPERADMIN", "ADMIN_RETAIL", "STORE_ADMIN");
        return new EntityExistsResponseDto { Exists = await repository.AdminLoginExistsAsync(request.Username, HttpIdentity.Context(HttpContext), ct) };
    }

    [HttpPut("users/{userId}")]
    public async Task<AdministratorDto> Update(string userId, UpdateAdministratorRequestDto request, CancellationToken ct)
    {
        var actor = HttpIdentity.RequireSubject(HttpContext, "administrator", "ADMIN", "SUPERADMIN", "ADMIN_RETAIL", "STORE_ADMIN"); var context = HttpIdentity.Context(HttpContext);
        return await service.UpdateAdminAsync(Guid.Parse(userId), request, context, await IsSuper(actor, context, ct), actor, ct);
    }

    [HttpPatch("users/{userId}/password")]
    public async Task<IActionResult> Password(string userId, AdministratorPasswordChangeRequestDto request, CancellationToken ct)
    {
        var actor = HttpIdentity.RequireSubject(HttpContext, "administrator"); await service.ChangeAdminPasswordAsync(actor, Guid.Parse(userId), request, HttpIdentity.Context(HttpContext), ct); return NoContent();
    }

    [HttpPatch("users/{userId}/enabled")]
    public async Task<IActionResult> Enabled(string userId, EnabledRequestDto request, CancellationToken ct)
    {
        HttpIdentity.RequireSubject(HttpContext, "administrator", "ADMIN", "SUPERADMIN", "ADMIN_RETAIL", "STORE_ADMIN");
        await service.SetAdminEnabledAsync(Guid.Parse(userId), request.IsActive, HttpIdentity.Context(HttpContext), ct); return NoContent();
    }

    [HttpDelete("users/{userId}")]
    public async Task<IActionResult> Delete(string userId, CancellationToken ct)
    {
        HttpIdentity.RequireSubject(HttpContext, "administrator", "ADMIN", "SUPERADMIN", "ADMIN_RETAIL", "STORE_ADMIN");
        await service.DeleteAdminAsync(Guid.Parse(userId), HttpIdentity.Context(HttpContext), ct); return NoContent();
    }

    [HttpPost("user-password-resets")]
    public async Task<ActionResult<ResetRequestResponseDto>> Reset(ResetRequestDto request, CancellationToken ct) { await service.RequestResetAsync(request, HttpIdentity.Context(HttpContext), true, ct); return Accepted(new ResetRequestResponseDto { Status = "ResetLinkSent" }); }
    [HttpGet("user-password-resets/{storeCode}/{token}")]
    public async Task<ResetTokenValidationResponseDto> Verify(string storeCode, string token, CancellationToken ct) { var r = await service.VerifyResetAsync(storeCode, token, "Administrator", HttpIdentity.Context(HttpContext), ct); return new() { Valid = true, ExpiresAt = r.ExpiresAt.ToString("O") }; }
    [HttpPost("user-password-resets/{storeCode}/{token}")]
    public async Task<IActionResult> Complete(string storeCode, string token, ResetPasswordRequestDto request, CancellationToken ct) { await service.CompleteResetAsync(storeCode, token, request, HttpIdentity.Context(HttpContext), true, ct); return NoContent(); }

    private async Task<bool> IsSuper(Guid id, RequestContext context, CancellationToken ct) => (await repository.FindAdminAsync(id, context, ct))?.Groups.Any(x => x.Equals("SUPERADMIN", StringComparison.OrdinalIgnoreCase)) == true;
}
