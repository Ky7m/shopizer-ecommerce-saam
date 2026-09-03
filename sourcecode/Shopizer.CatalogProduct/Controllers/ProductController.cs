using Microsoft.AspNetCore.Mvc;
using Shopizer.CatalogProduct.DTOs;
using Shopizer.CatalogProduct.Middleware;
using Shopizer.CatalogProduct.Services;

namespace Shopizer.CatalogProduct.Controllers;

[ApiController]
[Route("api/v1")]
public sealed class ProductController(CatalogService service) : ControllerBase
{
    [HttpGet("products")]
    public Task<ProductListResponseDto> List([FromQuery] int page=0,[FromQuery] int pageSize=20,[FromQuery] string languageCode="en",[FromQuery] string? countryCode=null,[FromQuery] Guid? categoryId=null,[FromQuery] string? sku=null,[FromQuery] string? name=null,[FromQuery] string? manufacturerCode=null,[FromQuery] bool? available=null,CancellationToken ct=default)
        => service.ListProductsAsync(HttpIdentity.Context(HttpContext),page,pageSize,languageCode,countryCode,categoryId,sku,name,manufacturerCode,available, false,ct);

    [HttpPost("products")]
    public async Task<IActionResult> Create([FromBody] CreateProductRequestDto request,CancellationToken ct=default)
    { HttpIdentity.RequireSubject(HttpContext,"administrator","ADMIN","CATALOG_ADMIN","SUPERADMIN");return StatusCode(201,await service.CreateProductAsync(request,HttpIdentity.Context(HttpContext),ct)); }

    [HttpGet("products/{productId:guid}")]
    public Task<ProductDto> Get(Guid productId,[FromQuery] string languageCode="en",[FromQuery] string? countryCode=null,CancellationToken ct=default)
        => service.GetProductAsync(productId,HttpIdentity.Context(HttpContext),languageCode,countryCode,false,ct);

    [HttpGet("products/slug/{friendlyUrl}")]
    public Task<ProductDto> GetSlug(string friendlyUrl,[FromQuery] string languageCode="en",[FromQuery] string? countryCode=null,CancellationToken ct=default)
        => service.GetProductBySlugAsync(friendlyUrl,HttpIdentity.Context(HttpContext),languageCode,countryCode,ct);

    [HttpGet("products/sku/{sku}")]
    public Task<ProductDto> GetSku(string sku,CancellationToken ct=default)
        => service.GetProductBySkuAsync(sku,HttpIdentity.Context(HttpContext),ct);

    [HttpGet("products/uniqueness")]
    public Task<ExistsResponseDto> ProductUniqueness([FromQuery] string sku,[FromQuery] Guid? excludeProductId=null,CancellationToken ct=default)
    { HttpIdentity.RequireSubject(HttpContext,"administrator","ADMIN","CATALOG_ADMIN","SUPERADMIN");return service.ProductUniquenessAsync(sku,excludeProductId,HttpIdentity.Context(HttpContext),ct); }

    [HttpPut("products/{productId:guid}")]
    public async Task<ProductDto> Update(Guid productId,[FromBody] UpdateProductRequestDto request,CancellationToken ct=default)
    {HttpIdentity.RequireSubject(HttpContext,"administrator","ADMIN","CATALOG_ADMIN","SUPERADMIN");return await service.UpdateProductAsync(productId,request,HttpIdentity.Context(HttpContext),ct);}

    [HttpPatch("products/{productId:guid}/visibility")]
    public async Task<ProductDto> Visibility(Guid productId,[FromBody] UpdateVisibilityRequestDto request,CancellationToken ct=default)
    {HttpIdentity.RequireSubject(HttpContext,"administrator","ADMIN","CATALOG_ADMIN","SUPERADMIN");return await service.UpdateProductVisibilityAsync(productId,request,HttpIdentity.Context(HttpContext),ct);}

    [HttpDelete("products/{productId:guid}")]
    public async Task<DeletionResultDto> Delete(Guid productId,CancellationToken ct=default)
    {HttpIdentity.RequireSubject(HttpContext,"administrator","ADMIN","CATALOG_ADMIN","SUPERADMIN");return await service.DeleteProductAsync(productId,HttpIdentity.Context(HttpContext),ct);}

    [HttpPost("products/{productId:guid}/categories/{categoryId:guid}")]
    public async Task<ProductDto> AttachCategory(Guid productId,Guid categoryId,CancellationToken ct=default)
    {HttpIdentity.RequireSubject(HttpContext,"administrator","ADMIN","CATALOG_ADMIN","SUPERADMIN");return await service.AttachCategoryAsync(productId,categoryId,HttpIdentity.Context(HttpContext),ct);}

    [HttpDelete("products/{productId:guid}/categories/{categoryId:guid}")]
    public async Task<ProductDto> DetachCategory(Guid productId,Guid categoryId,CancellationToken ct=default)
    {HttpIdentity.RequireSubject(HttpContext,"administrator","ADMIN","CATALOG_ADMIN","SUPERADMIN");return await service.DetachCategoryAsync(productId,categoryId,HttpIdentity.Context(HttpContext),ct);}
}
