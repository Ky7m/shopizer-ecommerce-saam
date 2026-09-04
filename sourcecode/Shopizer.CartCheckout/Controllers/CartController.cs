using Microsoft.AspNetCore.Mvc;
using Shopizer.CartCheckout.DTOs;
using Shopizer.CartCheckout.Middleware;
using Shopizer.CartCheckout.Models;
using Shopizer.CartCheckout.Services;

namespace Shopizer.CartCheckout.Controllers;

[ApiController]
[Route("api/v1")]
public sealed class CartController(CartService service) : ControllerBase
{
    [HttpPost("cart")]
    public async Task<ActionResult<CartEnvelopeDto>> Add(AddCartItemRequestDto request, CancellationToken ct) =>
        StatusCode(201, await service.AddAsync(request, HttpIdentity.Context(HttpContext), Request.Headers["x-cart-code"].FirstOrDefault(), ct));

    [HttpGet("cart/{code}")]
    public async Task<CartEnvelopeDto> Get(string code, CancellationToken ct) =>
        await service.GetAsync(code, HttpIdentity.Context(HttpContext), ct);

    [HttpPut("cart/{code}")]
    public async Task<CartEnvelopeDto> Update(string code, UpdateCartItemRequestDto request, CancellationToken ct) =>
        await service.UpdateAsync(code, request, HttpIdentity.Context(HttpContext), ct);

    [HttpPost("cart/{code}/multi")]
    public async Task<CartEnvelopeDto> UpdateMultiple(string code, [FromBody] List<MultiCartItemRequestDto> request, CancellationToken ct) =>
        await service.UpdateMultipleAsync(code, request, HttpIdentity.Context(HttpContext), ct);

    [HttpPost("cart/{code}/promo/{promoCode}")]
    public async Task<CartEnvelopeDto> Promo(string code, string promoCode, CancellationToken ct) =>
        await service.ApplyPromotionAsync(code, promoCode, HttpIdentity.Context(HttpContext), ct);

    [HttpDelete("cart/{code}/product/{sku}")]
    public async Task<IActionResult> Remove(string code, string sku, [FromQuery] bool body = false, CancellationToken ct = default)
    {
        var result = await service.RemoveAsync(code, sku, body, HttpIdentity.Context(HttpContext), ct);
        return body ? Ok(result) : NoContent();
    }

    [HttpPost("customers/{id}/cart")]
    public IActionResult DeprecatedCart(string id, AddCartItemRequestDto request) =>
        StatusCode(410, new ErrorResponseDto { Error = "UNSUPPORTED_ENDPOINT", Message = "Customer-ID cart creation is no longer supported", StatusCode = 410, Timestamp = DateTimeOffset.UtcNow.ToString("O") });

    [HttpGet("auth/customer/cart")]
    public async Task<CartEnvelopeDto> CustomerCart([FromQuery] string? cart = null, CancellationToken ct = default)
    {
        HttpIdentity.RequireSubject(HttpContext, "customer");
        return await service.CustomerCartAsync(cart, HttpIdentity.Context(HttpContext), ct);
    }

    [HttpGet("auth/customer/{id}/cart")]
    public async Task<CartEnvelopeDto> CustomerCartById(long id, [FromQuery] string? cart = null, CancellationToken ct = default)
    {
        var principal = HttpIdentity.RequireSubject(HttpContext, "customer");
        var customer = await service.CustomerCartAsync(cart, HttpIdentity.Context(HttpContext), ct);
        if (customer.Cart.CustomerId != id.ToString()) throw new DomainException("CART_SCOPE_MISMATCH", "Cart is not owned by this customer", 403);
        return customer;
    }

    [HttpPost("auth/cart/{code}/checkout")]
    public async Task<ActionResult<CheckoutSubmissionResponseDto>> AuthCheckout(string code, AuthenticatedCheckoutRequestDto request, CancellationToken ct)
    {
        HttpIdentity.RequireSubject(HttpContext, "customer");
        var key = Request.Headers["idempotency-key"].FirstOrDefault() ?? "";
        var result = await service.CheckoutAsync(code, request, null, key, HttpIdentity.Context(HttpContext), ct);
        return Accepted(result.Response);
    }

    [HttpPost("cart/{code}/checkout")]
    public async Task<ActionResult<CheckoutSubmissionResponseDto>> AnonymousCheckout(string code, AnonymousCheckoutRequestDto request, CancellationToken ct)
    {
        var key = Request.Headers["idempotency-key"].FirstOrDefault() ?? "";
        var result = await service.CheckoutAsync(code, null, request, key, HttpIdentity.Context(HttpContext), ct);
        return Accepted(result.Response);
    }

    [HttpGet("auth/cart/{code}/shipping")]
    public async Task<ShippingSummaryDto> AuthShipping(string code, CancellationToken ct)
    {
        HttpIdentity.RequireSubject(HttpContext, "customer");
        var customer = await serviceCustomer(ct);
        return await service.ShippingAsync(code, null, customer, HttpIdentity.Context(HttpContext), ct);
    }

    [HttpPost("cart/{code}/shipping")]
    public async Task<ShippingSummaryDto> AnonymousShipping(string code, ShippingAddressRequestDto request, CancellationToken ct) =>
        await service.ShippingAsync(code, request, null, HttpIdentity.Context(HttpContext), ct);

    [HttpGet("auth/cart/{id}/total")]
    public async Task<TotalSummaryDto> AuthTotal(long id, [FromQuery] string? quote = null, CancellationToken ct = default)
    {
        HttpIdentity.RequireSubject(HttpContext, "customer");
        var customer = await serviceCustomer(ct);
        return await service.TotalByIdAsync(id, quote, customer, HttpIdentity.Context(HttpContext), ct);
    }

    [HttpGet("cart/{code}/total")]
    public async Task<TotalSummaryDto> Total(string code, [FromQuery] string? quote = null, CancellationToken ct = default) =>
        await service.TotalAsync(code, quote, null, HttpIdentity.Context(HttpContext), ct);

    [HttpPost("auth/cart/{code}/payment/init")]
    public async Task<ActionResult<PaymentInitializationResponseDto>> AuthPayment(string code, PaymentInitializationRequestDto request, CancellationToken ct)
    {
        var subject = HttpIdentity.RequireSubject(HttpContext, "customer");
        var customer = await serviceCustomer(ct);
        var key = Request.Headers["idempotency-key"].FirstOrDefault() ?? "";
        var result = await service.InitializePaymentAsync(code, request, customer.Id, key, HttpIdentity.Context(HttpContext), ct);
        return Accepted(result);
    }

    [HttpPost("cart/{code}/payment/init")]
    public async Task<ActionResult<PaymentInitializationResponseDto>> Payment(string code, PaymentInitializationRequestDto request, CancellationToken ct)
    {
        var key = Request.Headers["idempotency-key"].FirstOrDefault() ?? "";
        return Accepted(await service.InitializePaymentAsync(code, request, null, key, HttpIdentity.Context(HttpContext), ct));
    }

    private async Task<CustomerFact> serviceCustomer(CancellationToken ct)
    {
        var customerClient = HttpContext.RequestServices.GetRequiredService<CustomerClient>();
        return await customerClient.CurrentAsync(ct);
    }
}
