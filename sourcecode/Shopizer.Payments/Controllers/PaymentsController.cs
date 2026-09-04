using Microsoft.AspNetCore.Mvc;
using Shopizer.Payments.DTOs;
using Shopizer.Payments.Middleware;
using Shopizer.Payments.Models;
using Shopizer.Payments.Services;

namespace Shopizer.Payments.Controllers;

[ApiController]
[Route("api/v1")]
public sealed class PaymentsController(PaymentService service)
    : ControllerBase
{
    [HttpGet("payment-methods")]
    public async Task<ActionResult<PaymentMethodListResponseDto>> ListMethods([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        RequireIdentity();
        return Ok(await service.ListMethodsAsync(Context(), page, pageSize, ct));
    }

    [HttpGet("payment-methods/{code}")]
    public async Task<ActionResult<PaymentMethodDto>> GetMethod(string code, CancellationToken ct)
    {
        return Ok(await service.GetMethodAsync(code, Context(), ct));
    }

    [HttpPut("payment-methods/{code}/configuration")]
    public async Task<ActionResult<PaymentMethodDto>> Configure(string code, ConfigurePaymentMethodRequestDto request, CancellationToken ct)
    {
        RequireIdentity(true);
        return Ok(await service.ConfigureAsync(code, request, Context(), ct));
    }

    [HttpPost("payment-intents")]
    public async Task<ActionResult<PaymentIntentDto>> CreateIntent(CreatePaymentIntentRequestDto request, CancellationToken ct)
    {
        ValidateModel();
        var response = await service.CreateIntentAsync(request, Context(), IdempotencyKey(), ct);
        return StatusCode(StatusCodes.Status201Created, response);
    }

    [HttpGet("payment-intents/{paymentIntentId}")]
    public async Task<ActionResult<PaymentIntentDto>> GetIntent(string paymentIntentId, CancellationToken ct)
    {
        RequireIdentity();
        return Ok(await service.GetIntentAsync(Guid.Parse(paymentIntentId), Context(), ct));
    }

    [HttpPost("payment-intents/{paymentIntentId}/authorize")]
    public async Task<ActionResult<PaymentOperationDto>> Authorize(string paymentIntentId, AuthorizePaymentRequestDto request, CancellationToken ct)
    {
        RequireIdentity();
        var response = await service.AuthorizeAsync(Guid.Parse(paymentIntentId), request, Context(), IdempotencyKey(), ct);
        return StatusCode(StatusCodes.Status201Created, response);
    }

    [HttpPost("payment-intents/{paymentIntentId}/capture")]
    public async Task<ActionResult<PaymentOperationDto>> Capture(string paymentIntentId, CapturePaymentRequestDto request, CancellationToken ct)
    {
        RequireIdentity();
        var response = await service.CaptureAsync(Guid.Parse(paymentIntentId), request, Context(), IdempotencyKey(), ct);
        return StatusCode(StatusCodes.Status201Created, response);
    }

    [HttpPost("payment-intents/{paymentIntentId}/refunds")]
    public async Task<ActionResult<RefundDto>> Refund(string paymentIntentId, RefundPaymentRequestDto request, CancellationToken ct)
    {
        RequireIdentity();
        var response = await service.RefundAsync(Guid.Parse(paymentIntentId), request, Context(), IdempotencyKey(), ct);
        return StatusCode(StatusCodes.Status201Created, response);
    }

    [HttpGet("payment-intents/{paymentIntentId}/transactions")]
    public async Task<ActionResult<PaymentTransactionListResponseDto>> Transactions(string paymentIntentId,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        RequireIdentity();
        return Ok(await service.TransactionsAsync(Guid.Parse(paymentIntentId), Context(), page, pageSize, ct));
    }

    [HttpGet("payment-operations/{paymentOperationId}")]
    public async Task<ActionResult<PaymentOperationDto>> Operation(string paymentOperationId, CancellationToken ct)
    {
        RequireIdentity();
        return Ok(await service.OperationAsync(Guid.Parse(paymentOperationId), Context(), ct));
    }

    [HttpPost("callbacks/{providerCode}")]
    public async Task<ActionResult<CallbackReceiptDto>> Callback(string providerCode, ProviderCallbackRequestDto request, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            var error = ModelState.Values.SelectMany(v => v.Errors).FirstOrDefault()?.ErrorMessage
                        ?? "The callback request is invalid";
            throw new DomainException("VALIDATION_ERROR", error, 400);
        }

        // Provider adapters authenticate this boundary; a customer/admin JWT is not required.
        var result = await service.CallbackAsync(providerCode, request,
            Request.Headers["x-provider-signature"].FirstOrDefault(),
            Request.Headers["x-provider-event-id"].FirstOrDefault(), Context(), ct);
        return Accepted(result);
    }

    [HttpGet("reconciliation/capturable")]
    public async Task<ActionResult<CapturablePaymentListResponseDto>> Capturable(
        [FromQuery] DateTimeOffset? from = null, [FromQuery] DateTimeOffset? to = null,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        RequireIdentity(true);
        return Ok(await service.CapturableAsync(from, to, Context(), page, pageSize, ct));
    }

    private RequestContext Context() => HttpIdentity.Context(HttpContext);

    private void RequireIdentity(bool administratorOnly = false)
    {
        ValidateModel();
        var identity = HttpContext.User.Identity?.IsAuthenticated == true;
        if (!identity) throw new DomainException("UNAUTHORIZED", "Authentication token is invalid", 401);
        var kind = HttpContext.User.Kind();
        if (administratorOnly)
        {
            HttpIdentity.RequireSubject(HttpContext, "administrator");
            return;
        }
        if (kind.Equals("administrator", StringComparison.OrdinalIgnoreCase))
        {
            HttpIdentity.RequireSubject(HttpContext, "administrator");
            return;
        }
        if (!administratorOnly && !kind.Equals("administrator", StringComparison.OrdinalIgnoreCase) &&
            !kind.Equals("service", StringComparison.OrdinalIgnoreCase))
            throw new DomainException("FORBIDDEN", "The authenticated identity is not authorized", 403);
    }

    private void ValidateModel()
    {
        if (!ModelState.IsValid)
        {
            var error = ModelState.Values.SelectMany(v => v.Errors).FirstOrDefault()?.ErrorMessage
                        ?? "The request is invalid";
            var status = ModelState.Values.SelectMany(v => v.Errors)
                .Any(error => error.ErrorMessage?.Contains("required", StringComparison.OrdinalIgnoreCase) == true)
                ? 400 : 422;
            throw new DomainException("VALIDATION_ERROR", error, status);
        }
    }

    private string IdempotencyKey()
    {
        var key = Request.Headers["Idempotency-Key"].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(key))
            throw new DomainException("IDEMPOTENCY_KEY_REQUIRED", "Idempotency-Key is required for payment mutations", 400);
        if (key.Length > 255) throw new DomainException("IDEMPOTENCY_KEY_INVALID", "Idempotency-Key is too long", 400);
        return key;
    }
}
