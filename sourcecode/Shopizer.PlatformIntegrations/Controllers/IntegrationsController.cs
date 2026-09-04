using Microsoft.AspNetCore.Mvc;
using Shopizer.PlatformIntegrations.DTOs;
using Shopizer.PlatformIntegrations.Middleware;
using Shopizer.PlatformIntegrations.Models;
using Shopizer.PlatformIntegrations.Services;

namespace Shopizer.PlatformIntegrations.Controllers;

[ApiController]
[Route("api/v1")]
public sealed class IntegrationsController(IntegrationService service) : ControllerBase
{
    [HttpGet("adapters")]
    public Task<AdapterListResponseDto> ListAdapters(string? moduleType = null, string? environment = null,
        int page = 1, int pageSize = 20, CancellationToken ct = default)
    {
        RequireAuthenticated();
        return service.ListAdaptersAsync(HttpIdentity.Context(HttpContext), moduleType, environment, page, pageSize, ct);
    }

    [HttpPost("adapters/refresh")]
    public async Task<AdapterDto> Refresh(RefreshAdapterRequestDto request, CancellationToken ct)
    {
        RequireAuthenticated();
        return await service.RefreshAdapterAsync(request, HttpIdentity.Context(HttpContext), ct);
    }

    [HttpPost("carrier-quotes/ups")]
    public Task<CarrierQuoteResponseDto> Ups(CarrierQuoteRequestDto request, CancellationToken ct)
    {
        RequireAuthenticated();
        return service.GetUpsQuoteAsync(request, HttpIdentity.Context(HttpContext), ct);
    }

    [HttpPost("carrier-quotes/usps")]
    public Task<CarrierQuoteResponseDto> Usps(CarrierQuoteRequestDto request, CancellationToken ct)
    {
        RequireAuthenticated();
        return service.GetUspsQuoteAsync(request, HttpIdentity.Context(HttpContext), ct);
    }

    [HttpPost("maps/distance")]
    public Task<DistanceResponseDto> Distance(DistanceRequestDto request, CancellationToken ct)
    {
        RequireAuthenticated();
        return service.CalculateDistanceAsync(request, HttpIdentity.Context(HttpContext), ct);
    }

    [HttpPost("geolocation/ip")]
    public Task<IpGeolocationResponseDto> Geolocation(IpGeolocationRequestDto request, CancellationToken ct)
    {
        RequireAuthenticated();
        return service.ResolveIpAsync(request, ct);
    }

    [HttpPost("files")]
    public async Task<ActionResult<UploadedFileAssetDto>> Upload(UploadFileRequestDto request, CancellationToken ct)
    {
        RequireAuthenticated();
        return StatusCode(201, await service.UploadFileAsync(request, HttpIdentity.Context(HttpContext), ct));
    }

    [HttpPost("files/batch")]
    public async Task<ActionResult<FileBatchResponseDto>> UploadBatch(BatchUploadFileRequestDto request, CancellationToken ct)
    {
        RequireAuthenticated();
        return StatusCode(201, await service.UploadFilesAsync(request, HttpIdentity.Context(HttpContext), ct));
    }

    [HttpGet("files")]
    public Task<FileListResponseDto> Files(string storeCode, string contentType, string? folderPath = null, CancellationToken ct = default)
    {
        RequireAuthenticated();
        return service.ListFilesAsync(storeCode, MarkerValue.Create<ContentTypeDto>(contentType), folderPath, HttpIdentity.Context(HttpContext), ct);
    }

    [HttpGet("files/{fileName}")]
    public Task<FileContentResponseDto> File(string fileName, string storeCode, string contentType, string? folderPath = null, CancellationToken ct = default)
    {
        RequireAuthenticated();
        return service.GetFileAsync(fileName, storeCode, MarkerValue.Create<ContentTypeDto>(contentType), folderPath, HttpIdentity.Context(HttpContext), ct);
    }

    [HttpDelete("files/{fileName}")]
    public async Task<IActionResult> DeleteFile(string fileName, string storeCode, string contentType, string? folderPath = null, CancellationToken ct = default)
    {
        RequireAuthenticated();
        await service.DeleteFileAsync(fileName, storeCode, MarkerValue.Create<ContentTypeDto>(contentType), folderPath, HttpIdentity.Context(HttpContext), ct);
        return NoContent();
    }

    [HttpDelete("files")]
    public async Task<IActionResult> DeleteFiles(string storeCode, string? folderPath = null, CancellationToken ct = default)
    {
        RequireAuthenticated();
        await service.DeleteFilesAsync(storeCode, folderPath, HttpIdentity.Context(HttpContext), ct);
        return NoContent();
    }

    [HttpPost("files/folders")]
    public async Task<ActionResult<FolderResponseDto>> CreateFolder(FolderRequestDto request, CancellationToken ct)
    {
        RequireAuthenticated();
        return StatusCode(201, await service.CreateFolderAsync(request, HttpIdentity.Context(HttpContext), ct));
    }

    [HttpGet("files/folders")]
    public Task<FolderListResponseDto> ListFolders(string storeCode, string provider, string? folderPath = null, CancellationToken ct = default)
    {
        RequireAuthenticated();
        return service.ListFoldersAsync(storeCode, MarkerValue.Create<StorageProviderDto>(provider), folderPath, HttpIdentity.Context(HttpContext), ct);
    }

    [HttpDelete("files/folders")]
    public async Task<IActionResult> DeleteFolder(string storeCode, string provider, string folderName, string? folderPath = null, CancellationToken ct = default)
    {
        RequireAuthenticated();
        await service.DeleteFolderAsync(storeCode, MarkerValue.Create<StorageProviderDto>(provider), folderPath, folderName, HttpIdentity.Context(HttpContext), ct);
        return NoContent();
    }

    [HttpPost("emails")]
    public async Task<ActionResult<EmailMessageDto>> QueueEmail(QueueEmailRequestDto request, CancellationToken ct)
    {
        RequireAuthenticated();
        return StatusCode(202, await service.QueueEmailAsync(request, HttpIdentity.Context(HttpContext), ct));
    }

    [HttpGet("delivery-attempts/{attemptId:guid}")]
    public Task<DeliveryAttemptDto> GetAttempt(Guid attemptId, CancellationToken ct)
    {
        RequireAuthenticated();
        return service.GetAttemptAsync(attemptId, HttpIdentity.Context(HttpContext), ct);
    }

    [HttpPost("delivery-attempts/{attemptId:guid}/replay")]
    public async Task<ActionResult<DeliveryAttemptDto>> Replay(Guid attemptId, ReplayRequestDto request, CancellationToken ct)
    {
        RequireAuthenticated();
        return StatusCode(202, await service.ReplayAttemptAsync(attemptId, request, HttpIdentity.Context(HttpContext), ct));
    }

    private void RequireAuthenticated()
    {
        _ = HttpIdentity.Context(HttpContext);
        if (HttpContext.User.Identity?.IsAuthenticated != true)
            throw new DomainException("UNAUTHORIZED", "Authentication token is invalid", 401);
    }
}
