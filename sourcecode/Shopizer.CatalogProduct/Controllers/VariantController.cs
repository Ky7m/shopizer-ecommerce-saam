using Microsoft.AspNetCore.Mvc;
using Shopizer.CatalogProduct.DTOs;
using Shopizer.CatalogProduct.Middleware;
using Shopizer.CatalogProduct.Services;

namespace Shopizer.CatalogProduct.Controllers;

[ApiController]
[Route("api/v1")]
public sealed class VariantController(CatalogService service) : ControllerBase
{
    private void Admin() => HttpIdentity.RequireSubject(HttpContext, "administrator", "ADMIN", "CATALOG_ADMIN", "SUPERADMIN");
    [HttpPost("products/{productId:guid}/variants")]
    public async Task<IActionResult> Create(Guid productId, [FromBody] CreateVariantRequestDto request, CancellationToken ct = default) { Admin(); return StatusCode(201, await service.CreateVariantAsync(productId, request, HttpIdentity.Context(HttpContext), ct)); }
    [HttpGet("products/{productId:guid}/variants")]
    public Task<ProductVariantListResponseDto> List(Guid productId, [FromQuery] int page = 0, [FromQuery] int pageSize = 20, CancellationToken ct = default) => service.ListVariantsAsync(productId, HttpIdentity.Context(HttpContext), page, pageSize, ct);
    [HttpGet("products/{productId:guid}/variants/{variantId:guid}")]
    public Task<ProductVariantDto> Get(Guid productId, Guid variantId, CancellationToken ct = default) => service.GetVariantAsync(productId, variantId, HttpIdentity.Context(HttpContext), ct);
    [HttpPut("products/{productId:guid}/variants/{variantId:guid}")]
    public async Task<ProductVariantDto> Update(Guid productId, Guid variantId, [FromBody] UpdateVariantRequestDto request, CancellationToken ct = default) { Admin(); return await service.UpdateVariantAsync(productId, variantId, request, HttpIdentity.Context(HttpContext), ct); }
    [HttpDelete("products/{productId:guid}/variants/{variantId:guid}")]
    public async Task<DeletionResultDto> Delete(Guid productId, Guid variantId, CancellationToken ct = default) { Admin(); return await service.DeleteVariantAsync(productId, variantId, HttpIdentity.Context(HttpContext), ct); }
    [HttpGet("products/{productId:guid}/variants/uniqueness/{sku}")]
    public Task<ExistsResponseDto> Uniqueness(Guid productId, string sku, CancellationToken ct = default) { Admin(); return service.VariantUniquenessAsync(productId, sku, HttpIdentity.Context(HttpContext), ct); }
    [HttpPost("products/{productId:guid}/options/price")]
    public Task<PriceResponseDto> Price(Guid productId, [FromBody] CalculatePriceRequestDto request, CancellationToken ct = default) => service.CalculatePriceAsync(productId, request, HttpIdentity.Context(HttpContext), ct);
}
