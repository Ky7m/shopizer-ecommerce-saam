using Microsoft.AspNetCore.Mvc;
using Shopizer.Search.Middleware;
using Shopizer.Search.Models;
using Shopizer.Search.Services;
using Shopizer.Services.Ms03.Contracts;

namespace Shopizer.Search.Controllers;

[ApiController]
[Route("api/v1")]
public sealed class SearchController(SearchService service, IConfiguration configuration) : ControllerBase
{
    [HttpPost("search")]
    public async Task<ActionResult<SearchResultsResponseDto>> SearchAsync(
        [FromBody] SearchRequestDto? request, CancellationToken ct)
    {
        var context = HttpIdentity.Context(HttpContext);
        if (request is null)
        {
            throw new DomainException("REQUEST_BODY_REQUIRED", "A JSON request body is required", 400);
        }

        return Ok(await service.SearchAsync(request, context, Locale(HttpContext), ct));
    }

    [HttpPost("search/autocomplete")]
    public async Task<ActionResult<AutocompleteResponseDto>> AutocompleteAsync(
        [FromBody] AutocompleteRequestDto? request, CancellationToken ct)
    {
        var context = HttpIdentity.Context(HttpContext);
        if (request is null)
        {
            throw new DomainException("REQUEST_BODY_REQUIRED", "A JSON request body is required", 400);
        }

        return Ok(await service.AutocompleteAsync(request, context, Locale(HttpContext), ct));
    }

    [HttpPost("private/system/search/index")]
    public async Task<ActionResult<RebuildAcceptedResponseDto>> RebuildAsync(CancellationToken ct)
    {
        var context = HttpIdentity.Context(HttpContext);
        var subject = HttpIdentity.RequireSubject(HttpContext, "administrator",
            "SUPERADMIN", "ADMIN", "ADMIN_CATALOGUE", "ADMIN_RETAIL");
        var idempotencyKey = Request.Headers["idempotency-key"].FirstOrDefault();
        var result = await service.RequestRebuildAsync(context, subject.ToString(), idempotencyKey ?? "", ct);
        return StatusCode(StatusCodes.Status202Accepted, result);
    }

    private string Locale(HttpContext http)
    {
        var value = http.Request.Headers["x-language"].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(value))
        {
            value = http.Request.Headers["Accept-Language"].FirstOrDefault();
        }

        return string.IsNullOrWhiteSpace(value)
            ? configuration["Search:DefaultLocale"] ?? "en"
            : value;
    }
}
