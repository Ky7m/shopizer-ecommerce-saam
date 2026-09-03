using Microsoft.AspNetCore.Mvc;
using Shopizer.CatalogProduct.Middleware;
using Shopizer.CatalogProduct.Services;
using Shopizer.Services.Ms02.Contracts;

namespace Shopizer.CatalogProduct.Controllers;

[ApiController]
[Route("api/v1")]
public sealed class CategoryController(CatalogService service) : ControllerBase
{
    private void Admin()=>HttpIdentity.RequireSubject(HttpContext,"administrator","ADMIN","CATALOG_ADMIN","SUPERADMIN");

    [HttpGet("categories")]
    public Task<CategoryListResponseDto> List([FromQuery]int page=0,[FromQuery]int pageSize=20,[FromQuery]string languageCode="en",[FromQuery]string? name=null,[FromQuery]bool? visible=null,[FromQuery]bool? featured=null,CancellationToken ct=default)
        =>service.ListCategoriesAsync(HttpIdentity.Context(HttpContext),page,pageSize,languageCode,name,visible,featured,ct);
    [HttpPost("categories")]
    public async Task<IActionResult>Create([FromBody]CreateCategoryRequestDto request,CancellationToken ct=default){Admin();return StatusCode(201,await service.CreateCategoryAsync(request,HttpIdentity.Context(HttpContext),ct));}
    [HttpGet("categories/{categoryId:guid}")]
    public Task<CategoryDto>Get(Guid categoryId,[FromQuery]string languageCode="en",CancellationToken ct=default)=>service.GetCategoryAsync(categoryId,HttpIdentity.Context(HttpContext),languageCode,ct);
    [HttpGet("categories/slug/{friendlyUrl}")]
    public Task<CategoryDto>GetSlug(string friendlyUrl,[FromQuery]string languageCode="en",CancellationToken ct=default)=>service.GetCategoryBySlugAsync(friendlyUrl,HttpIdentity.Context(HttpContext),languageCode,ct);
    [HttpGet("categories/uniqueness")]
    public Task<ExistsResponseDto>Uniqueness([FromQuery]string code,[FromQuery]Guid? excludeCategoryId=null,CancellationToken ct=default){Admin();return service.CategoryUniquenessAsync(code,excludeCategoryId,HttpIdentity.Context(HttpContext),ct);}
    [HttpPut("categories/{categoryId:guid}")]
    public async Task<CategoryDto>Update(Guid categoryId,[FromBody]UpdateCategoryRequestDto request,CancellationToken ct=default){Admin();return await service.UpdateCategoryAsync(categoryId,request,HttpIdentity.Context(HttpContext),ct);}
    [HttpPatch("categories/{categoryId:guid}/visibility")]
    public async Task<CategoryDto>Visibility(Guid categoryId,[FromBody]UpdateCategoryVisibilityRequestDto request,CancellationToken ct=default){Admin();return await service.UpdateCategoryVisibilityAsync(categoryId,request,HttpIdentity.Context(HttpContext),ct);}
    [HttpPut("categories/{categoryId:guid}/move/{parentId:guid}")]
    public async Task<CategoryDto>Move(Guid categoryId,Guid parentId,CancellationToken ct=default){Admin();return await service.MoveCategoryAsync(categoryId,parentId,HttpIdentity.Context(HttpContext),ct);}
    [HttpDelete("categories/{categoryId:guid}")]
    public async Task<CategoryDeletionResultDto>Delete(Guid categoryId,[FromQuery]string orphanProductPolicy="Reject",CancellationToken ct=default){Admin();return await service.DeleteCategoryAsync(categoryId,orphanProductPolicy,HttpIdentity.Context(HttpContext),ct);}
    [HttpGet("categories/{categoryId:guid}/products")]
    public Task<ProductListResponseDto>Products(Guid categoryId,[FromQuery]int page=0,[FromQuery]int pageSize=20,[FromQuery]string languageCode="en",[FromQuery]string? countryCode=null,CancellationToken ct=default)=>service.ListCategoryProductsAsync(categoryId,HttpIdentity.Context(HttpContext),page,pageSize,languageCode,countryCode,ct);
}
