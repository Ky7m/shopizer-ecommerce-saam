using Microsoft.AspNetCore.Mvc;
using Shopizer.CatalogProduct.DTOs;
using Shopizer.CatalogProduct.Middleware;
using Shopizer.CatalogProduct.Models;
using Shopizer.CatalogProduct.Services;

namespace Shopizer.CatalogProduct.Controllers;

[ApiController]
[Route("api/v1")]
public sealed class InventoryMediaController(CatalogService service) : ControllerBase
{
    private void Admin()=>HttpIdentity.RequireSubject(HttpContext,"administrator","ADMIN","CATALOG_ADMIN","SUPERADMIN");
    [HttpGet("products/{productId:guid}/availability")]
    public Task<AvailabilityListResponseDto>Availability(Guid productId,CancellationToken ct=default)=>service.GetAvailabilityAsync(productId,HttpIdentity.Context(HttpContext),ct);
    [HttpPut("products/{productId:guid}/availability")]
    public async Task<AvailabilityListResponseDto>ReplaceAvailability(Guid productId,[FromBody]ReplaceAvailabilityRequestDto request,CancellationToken ct=default){Admin();return await service.ReplaceAvailabilityAsync(productId,request,HttpIdentity.Context(HttpContext),ct);}
    [HttpPost("products/{productId:guid}/reservations")]
    public async Task<IActionResult>Reserve(Guid productId,[FromBody]CreateReservationRequestDto request,CancellationToken ct=default)
    {
        HttpIdentity.RequireSubject(HttpContext,"administrator","ADMIN","CATALOG_ADMIN","SUPERADMIN");
        var idempotencyKey=HttpContext.Request.Headers["Idempotency-Key"].FirstOrDefault();
        if(string.IsNullOrWhiteSpace(idempotencyKey))throw new DomainException("IDEMPOTENCY_KEY_REQUIRED","Idempotency-Key is required for inventory reservations",422);
        if(!string.Equals(idempotencyKey,request.ReservationKey,StringComparison.Ordinal))throw new DomainException("IDEMPOTENCY_KEY_MISMATCH","Idempotency-Key must match reservationKey",422);
        return StatusCode(201,await service.CreateReservationAsync(productId,request,HttpIdentity.Context(HttpContext),ct));
    }
    [HttpPost("reservations/{reservationId:guid}/commit")]
    public async Task<InventoryReservationDto>Commit(Guid reservationId,CancellationToken ct=default){HttpIdentity.RequireSubject(HttpContext,"administrator","ADMIN","CATALOG_ADMIN","SUPERADMIN");return await service.CommitReservationAsync(reservationId,HttpIdentity.Context(HttpContext),ct);}
    [HttpPost("reservations/{reservationId:guid}/release")]
    public async Task<InventoryReservationDto>Release(Guid reservationId,CancellationToken ct=default){HttpIdentity.RequireSubject(HttpContext,"administrator","ADMIN","CATALOG_ADMIN","SUPERADMIN");return await service.ReleaseReservationAsync(reservationId,HttpIdentity.Context(HttpContext),ct);}
    [HttpPost("products/{productId:guid}/media"), Consumes("application/json")]
    public async Task<IActionResult>Media(Guid productId,[FromBody]ExternalMediaRequestDto request,CancellationToken ct=default){Admin();return StatusCode(201,await service.AddExternalMediaAsync(productId,request,HttpIdentity.Context(HttpContext),ct));}
    [HttpPost("products/{productId:guid}/media"), Consumes("multipart/form-data")]
    public async Task<IActionResult>BinaryMedia(Guid productId,[FromForm]IFormFile file,[FromForm]string? fileName=null,[FromForm]bool defaultImage=false,CancellationToken ct=default)
    {
        Admin();
        if(file is null||file.Length==0)throw new DomainException("IMAGE_INVALID","Uploaded content is not readable",422);
        var name=string.IsNullOrWhiteSpace(fileName)?Path.GetFileName(file.FileName):fileName;
        if(string.IsNullOrWhiteSpace(name))throw new DomainException("IMAGE_INVALID","A media file name is required",422);
        return StatusCode(201,await service.AddMediaAsync(productId,name,null,defaultImage,HttpIdentity.Context(HttpContext),ct));
    }
    [HttpDelete("products/{productId:guid}/media/{mediaId:guid}")]
    public async Task<DeletionResultDto>DeleteMedia(Guid productId,Guid mediaId,CancellationToken ct=default){Admin();return await service.DeleteMediaAsync(productId,mediaId,HttpIdentity.Context(HttpContext),ct);}
}
