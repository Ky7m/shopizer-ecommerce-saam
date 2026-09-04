using Microsoft.AspNetCore.Mvc;
using Shopizer.PricingPromotions.DTOs;
using Shopizer.PricingPromotions.Middleware;
using Shopizer.PricingPromotions.Services;

namespace Shopizer.PricingPromotions.Controllers;

[ApiController]
[Route("api/v1")]
public sealed class PricingController(PricingService service) : ControllerBase
{
    private static readonly string[] PriceRoles = ["ADMIN", "SUPERADMIN", "PRICING_ADMIN", "STORE_ADMIN"];

    [HttpPost("private/products/{sku}/availabilities/{availabilityId}/prices")]
    public async Task<ActionResult<PriceCreatedResponseDto>> CreateAvailabilityPrice(
        string sku, long availabilityId, AvailabilityPriceCreateRequestDto request, CancellationToken ct)
    {
        HttpIdentity.RequireSubject(HttpContext, "administrator", PriceRoles);
        var created = await service.CreatePriceAsync(sku, availabilityId, request, null,
            HttpIdentity.Context(HttpContext), ct);
        return StatusCode(StatusCodes.Status201Created, created);
    }

    [HttpPost("private/products/{sku}/prices")]
    public async Task<ActionResult<PriceCreatedResponseDto>> CreateProductPrice(
        string sku, ProductPriceCreateRequestDto request, CancellationToken ct)
    {
        HttpIdentity.RequireSubject(HttpContext, "administrator", PriceRoles);
        var created = await service.CreatePriceAsync(sku, null, null, request,
            HttpIdentity.Context(HttpContext), ct);
        return StatusCode(StatusCodes.Status201Created, created);
    }

    [HttpPut("private/products/{sku}/availabilities/{availabilityId}/prices/{priceId}")]
    public async Task<ActionResult<PriceDto>> UpdateAvailabilityPrice(
        string sku, long availabilityId, Guid priceId, PriceUpdateRequestDto request, CancellationToken ct)
    {
        HttpIdentity.RequireSubject(HttpContext, "administrator", PriceRoles);
        return Ok(await service.UpdatePriceAsync(sku, availabilityId, priceId, request,
            HttpIdentity.Context(HttpContext), ct));
    }

    [HttpGet("private/products/{sku}/prices/{priceId}")]
    public async Task<ActionResult<PriceDto>> GetProductPrice(string sku, Guid priceId, CancellationToken ct)
    {
        HttpIdentity.RequireSubject(HttpContext, "administrator", PriceRoles);
        return Ok(await service.GetPriceAsync(sku, priceId, HttpIdentity.Context(HttpContext), ct));
    }

    [HttpGet("private/products/{sku}/availabilities/{availabilityId}/prices")]
    public async Task<ActionResult<PriceListResponseDto>> ListAvailabilityPrices(
        string sku, long availabilityId, CancellationToken ct)
    {
        HttpIdentity.RequireSubject(HttpContext, "administrator", PriceRoles);
        return Ok(await service.ListPricesAsync(sku, availabilityId, HttpIdentity.Context(HttpContext), ct));
    }

    [HttpGet("private/products/{sku}/prices")]
    public async Task<ActionResult<PriceListResponseDto>> ListProductPrices(string sku, CancellationToken ct)
    {
        HttpIdentity.RequireSubject(HttpContext, "administrator", PriceRoles);
        return Ok(await service.ListPricesAsync(sku, null, HttpIdentity.Context(HttpContext), ct));
    }

    [HttpDelete("private/products/{sku}/prices/{priceId}")]
    public async Task<IActionResult> DeleteProductPrice(string sku, Guid priceId, CancellationToken ct)
    {
        HttpIdentity.RequireSubject(HttpContext, "administrator", PriceRoles);
        await service.DeletePriceAsync(sku, priceId, HttpIdentity.Context(HttpContext), ct);
        return NoContent();
    }

    [HttpGet("pricing/products/{sku}/price")]
    public async Task<ActionResult<ProductPriceCalculationResponseDto>> CalculateProductPrice(
        string sku, [FromQuery] string? evaluationAt, [FromQuery] bool includeAdditionalPrices = true,
        CancellationToken ct = default)
    {
        return Ok(await service.CalculateProductPriceAsync(sku, evaluationAt, includeAdditionalPrices,
            HttpIdentity.Context(HttpContext), ct));
    }

    [HttpPost("pricing/products/{sku}/quote")]
    public async Task<ActionResult<ProductPriceCalculationResponseDto>> QuoteProductPrice(
        string sku, ProductQuoteRequestDto request, CancellationToken ct)
    {
        if (request.Attributes is null)
        {
            return BadRequest(new { error = "attributes is required" });
        }

        return Ok(await service.QuoteProductPriceAsync(sku, request, HttpIdentity.Context(HttpContext), ct));
    }

    [HttpPost("pricing/variants/{variantSku}/quote")]
    public async Task<ActionResult<ProductPriceCalculationResponseDto>> QuoteVariantPrice(
        string variantSku, VariantQuoteRequestDto request, CancellationToken ct)
    {
        return Ok(await service.QuoteVariantPriceAsync(variantSku, request,
            HttpIdentity.Context(HttpContext), ct));
    }

    [HttpPost("pricing/promotions/evaluate")]
    public async Task<ActionResult<PromotionEvaluationResponseDto>> EvaluatePromotion(
        PromotionEvaluationRequestDto request, CancellationToken ct)
    {
        return Ok(await service.EvaluatePromotionAsync(request, HttpIdentity.Context(HttpContext), ct));
    }

    [HttpGet("private/pricing/processors")]
    public async Task<ActionResult<ProcessorRegistryResponseDto>> GetPricingProcessors(CancellationToken ct)
    {
        HttpIdentity.RequireSubject(HttpContext, "administrator", PriceRoles);
        return Ok(await service.GetProcessorsAsync());
    }

    [HttpPost("pricing/quotes")]
    public async Task<ActionResult<PricingQuoteResponseDto>> CalculatePricingQuote(
        PricingQuoteRequestDto request, CancellationToken ct)
    {
        return Ok(await service.CalculateQuoteAsync(request, HttpIdentity.Context(HttpContext), ct));
    }
}
