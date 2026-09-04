using System.Text.Json;
using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Shopizer.OrderManagement.DTOs;
using Shopizer.OrderManagement.Middleware;
using Shopizer.OrderManagement.Models;
using Shopizer.OrderManagement.Services;

namespace Shopizer.OrderManagement.Controllers;

[ApiController]
[Route("api/v1")]
public sealed class OrderController(OrderService service) : ControllerBase
{
    private static readonly string[] AdminRoles = ["SUPERADMIN", "ADMIN", "ADMIN_ORDER", "ADMIN_RETAIL"];

    [HttpGet("orders")]
    public Task<OrderListResponseDto> List(int page = 1, int pageSize = 20, string? status = null, string? customerName = null, string? email = null, string? phone = null, long? orderId = null, CancellationToken ct = default)
    {
        HttpIdentity.RequireSubject(HttpContext, "administrator", AdminRoles);
        return service.ListAsync(HttpIdentity.Context(HttpContext), page, pageSize, status, customerName, email, phone, orderId, null, ct);
    }

    [HttpGet("orders/{orderId:long}")]
    public Task<OrderDto> Get(long orderId, CancellationToken ct)
    {
        HttpIdentity.RequireSubject(HttpContext, "administrator", AdminRoles);
        return service.GetAsync(orderId, HttpIdentity.Context(HttpContext), null, ct);
    }

    [HttpGet("me/orders")]
    // @BR-OR-AUTH-002: An authenticated customer can list only orders associated with that customer and store.
    public Task<OrderListResponseDto> MyOrders(int page = 1, int pageSize = 20, string? status = null, CancellationToken ct = default)
    {
        var customer = HttpIdentity.RequireSubject(HttpContext, "customer");
        return service.ListAsync(HttpIdentity.Context(HttpContext), page, pageSize, status, null, null, null, null, customer, ct);
    }

    [HttpGet("me/orders/{orderId:long}")]
    public Task<OrderDto> MyOrder(long orderId, CancellationToken ct)
    {
        var customer = HttpIdentity.RequireSubject(HttpContext, "customer");
        return service.GetAsync(orderId, HttpIdentity.Context(HttpContext), customer, ct);
    }

    [HttpGet("customers/{customerId:long}/orders")]
    public Task<OrderListResponseDto> CustomerOrders(long customerId, int page = 1, int pageSize = 20, CancellationToken ct = default)
    {
        HttpIdentity.RequireSubject(HttpContext, "administrator", AdminRoles);
        return service.ListAsync(HttpIdentity.Context(HttpContext), page, pageSize, null, null, null, null, null, customerId, ct);
    }

    [HttpGet("orders/{orderId:long}/history")]
    public Task<OrderHistoryResponseDto> History(long orderId, CancellationToken ct)
    {
        var admin = HttpContext.User.Kind().Equals("administrator", StringComparison.OrdinalIgnoreCase);
        var subject = admin ? HttpIdentity.RequireSubject(HttpContext, "administrator", AdminRoles) : HttpIdentity.RequireSubject(HttpContext, "customer");
        return service.HistoryAsync(orderId, HttpIdentity.Context(HttpContext), admin ? null : subject, ct);
    }

    [HttpPost("orders/{orderId:long}/history")]
    public async Task<ActionResult<OrderHistoryEntryDto>> AppendHistory(long orderId, AppendHistoryRequestDto request, CancellationToken ct)
    {
        var actor = HttpIdentity.RequireSubject(HttpContext, "administrator", AdminRoles);
        var result = await service.AppendHistoryAsync(orderId, request, HttpIdentity.Context(HttpContext), actor.ToString(CultureInfo.InvariantCulture), Key(), ct);
        return StatusCode(201, result);
    }

    [HttpPut("orders/{orderId:long}/status")]
    public Task<OrderDto> Status(long orderId, TransitionOrderStatusRequestDto request, CancellationToken ct)
    {
        var actor = HttpIdentity.RequireSubject(HttpContext, "administrator", AdminRoles);
        return service.TransitionAsync(orderId, request, HttpIdentity.Context(HttpContext), actor.ToString(CultureInfo.InvariantCulture), Key(), ct);
    }

    [HttpPatch("orders/{orderId:long}/customer-snapshot")]
    public Task<OrderDto> Snapshot(long orderId, CustomerSnapshotUpdateRequestDto request, CancellationToken ct)
    {
        HttpIdentity.RequireSubject(HttpContext, "administrator", AdminRoles);
        return service.UpdateSnapshotAsync(orderId, request, HttpIdentity.Context(HttpContext), Key(), ct);
    }

    [HttpGet("orders/{orderId:long}/payment/next-action")]
    public Task<NextPaymentActionDto> NextPayment(long orderId, CancellationToken ct)
    {
        HttpIdentity.RequireSubject(HttpContext, "administrator", AdminRoles);
        return service.NextPaymentAsync(orderId, HttpIdentity.Context(HttpContext), ct);
    }

    [HttpGet("orders/{orderId:long}/payment-transactions")]
    public Task<PaymentTransactionListResponseDto> PaymentTransactions(long orderId, CancellationToken ct)
    {
        HttpIdentity.RequireSubject(HttpContext, "administrator", AdminRoles);
        return service.PaymentsAsync(orderId, HttpIdentity.Context(HttpContext), ct);
    }

    [HttpGet("orders/capturable")]
    public Task<OrderListResponseDto> Capturable(DateTimeOffset? startDate = null, DateTimeOffset? endDate = null, int page = 1, int pageSize = 20, CancellationToken ct = default)
    {
        HttpIdentity.RequireSubject(HttpContext, "administrator", AdminRoles);
        return service.CapturableAsync(HttpIdentity.Context(HttpContext), startDate, endDate, page, pageSize, ct);
    }

    [HttpPost("orders/{orderId:long}/capture")]
    public async Task<ActionResult<PaymentCommandResponseDto>> Capture(long orderId, PaymentCommandRequestDto request, CancellationToken ct)
    {
        HttpIdentity.RequireSubject(HttpContext, "administrator", AdminRoles);
        return StatusCode(202, await service.CaptureAsync(orderId, request, HttpIdentity.Context(HttpContext), Key(), ct));
    }

    [HttpPost("orders/{orderId:long}/refund")]
    public async Task<ActionResult<RefundCommandResponseDto>> Refund(long orderId, RefundRequestDto request, CancellationToken ct)
    {
        HttpIdentity.RequireSubject(HttpContext, "administrator", AdminRoles);
        return StatusCode(202, await service.RefundAsync(orderId, request, HttpIdentity.Context(HttpContext), Key(), ct));
    }

    [HttpPost("orders/{orderId:long}/cancel")]
    public async Task<ActionResult<CancellationResponseDto>> Cancel(long orderId, CancelOrderRequestDto request, CancellationToken ct)
    {
        var admin = HttpContext.User.Kind().Equals("administrator", StringComparison.OrdinalIgnoreCase);
        var customer = HttpIdentity.RequireSubject(HttpContext, admin ? "administrator" : "customer", admin ? AdminRoles : []);
        return StatusCode(202, await service.CancelAsync(orderId, request, HttpIdentity.Context(HttpContext), Key(), admin ? null : customer, ct));
    }

    [HttpPost("orders/{orderId:long}/fulfillment")]
    public async Task<ActionResult<FulfillmentResponseDto>> RequestFulfillment(long orderId, CancellationToken ct)
    {
        HttpIdentity.RequireSubject(HttpContext, "administrator", AdminRoles);
        return StatusCode(202, await service.RequestFulfillmentAsync(orderId, HttpIdentity.Context(HttpContext), Key(), ct));
    }

    [HttpGet("orders/{orderId:long}/fulfillment")]
    public Task<FulfillmentResponseDto> GetFulfillment(long orderId, CancellationToken ct)
    {
        var admin = HttpContext.User.Kind().Equals("administrator", StringComparison.OrdinalIgnoreCase);
        var subject = admin ? HttpIdentity.RequireSubject(HttpContext, "administrator", AdminRoles) : HttpIdentity.RequireSubject(HttpContext, "customer");
        return service.GetFulfillmentAsync(orderId, HttpIdentity.Context(HttpContext), admin ? null : subject, ct);
    }

    [HttpGet("orders/{orderId:long}/invoice")]
    public async Task<IActionResult> Invoice(long orderId, CancellationToken ct)
    {
        var admin = HttpContext.User.Kind().Equals("administrator", StringComparison.OrdinalIgnoreCase);
        var subject = admin ? HttpIdentity.RequireSubject(HttpContext, "administrator", AdminRoles) : HttpIdentity.RequireSubject(HttpContext, "customer");
        var result = await service.InvoiceAsync(orderId, HttpIdentity.Context(HttpContext), admin ? null : subject, ct);
        return StatusCode(result.Status, result.Response);
    }

    // Internal orchestration boundary: MS-04 submits its immutable checkout snapshot here.
    [HttpPost("internal/order-submissions")]
    public async Task<ActionResult<OrderDto>> Submit(JsonElement body, CancellationToken ct)
    {
        var result = await service.SubmitAsync(body, HttpIdentity.Context(HttpContext), ct);
        return StatusCode(201, result);
    }

    // Internal event boundary used by the message adapter; event ownership remains with the source service.
    [HttpPost("internal/events/{eventType}")]
    public async Task<IActionResult> Event(string eventType, JsonElement body, CancellationToken ct)
    {
        var applied = await service.ApplyEventAsync(eventType, body, HttpIdentity.Context(HttpContext), ct);
        return Ok(new { eventApplied = applied, duplicate = !applied });
    }

    [HttpPost("internal/invoices/{requestId}/available")]
    public async Task<IActionResult> InvoiceAvailable(string requestId, JsonElement body, CancellationToken ct)
    {
        await service.ApplyEventAsync("InvoiceAvailable", body, HttpIdentity.Context(HttpContext), ct);
        return Ok();
    }

    private string Key() => Request.Headers["Idempotency-Key"].FirstOrDefault() ?? "";
}
