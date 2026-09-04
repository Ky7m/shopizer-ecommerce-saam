using System.Text.Json;
using Shopizer.OrderManagement.Data;
using Shopizer.OrderManagement.DTOs;
using Shopizer.OrderManagement.Models;

namespace Shopizer.OrderManagement.Services;

public sealed class OrderService(OrderRepository repository, EventPublisher events, ILogger<OrderService> logger)
{
    private static readonly HashSet<string> OrderStatuses = ["ORDERED", "PROCESSED", "DELIVERED", "REFUNDED", "CANCELED"];
    private static readonly HashSet<string> PaymentActions = ["INIT", "AUTHORIZE", "AUTHORIZECAPTURE", "CAPTURE", "REFUND", "VOID", "OK"];

    // @BR-OR-AUTH-001: Administrative reads are always isolated by the request tenant and store.
    // @BR-OR-AUTH-002: Customer-scoped reads are restricted to the authenticated customer's store orders.
    // @BR-OR-ADM-001: Administrative order lists are available only to an authorized order administrator.
    // @BR-OR-READ-001: Lists validate pagination and return projected order envelopes.
    public async Task<OrderListResponseDto> ListAsync(RequestContext context, int page, int pageSize, string? status, string? customerName, string? email, string? phone, long? orderId, long? customerId, CancellationToken ct)
    {
        ValidatePage(page, pageSize);
        var normalized = NormalizeStatus(status);
        var result = await repository.ListAsync(context, page, pageSize, normalized, customerName, email, phone, orderId, customerId, ct);
        return new() { Items = result.Items.Select(x => DtoMapper.Order(x, false)).ToList(), Pagination = Page(page, pageSize, result.Total) };
    }

    // @BR-OR-AUTH-001: An order detail is resolved only inside the supplied tenant/store boundary.
    // @BR-OR-READ-001: Detail reads include immutable snapshots, lines, totals, history, and entitlements.
    public async Task<OrderDto> GetAsync(long id, RequestContext context, long? customerId, CancellationToken ct) =>
        DtoMapper.Order(await repository.FindAsync(id, context, customerId, true, ct) ?? throw NotFound(id));

    // @BR-OR-LIFE-002: Lifecycle history is returned newest first from append-only storage.
    public async Task<OrderHistoryResponseDto> HistoryAsync(long id, RequestContext context, long? customerId, CancellationToken ct)
    {
        _ = await repository.FindAsync(id, context, customerId, false, ct) ?? throw NotFound(id);
        return new() { Items = (await repository.HistoryAsync(id, context, ct)).Select(DtoMapper.HistoryEntry).ToList() };
    }

    // @BR-OR-SUB-001: An accepted order begins in ORDERED and records its acceptance history.
    // @BR-OR-SUB-002: Submission facts are captured as immutable customer, store, address, and currency snapshots.
    // @BR-OR-SUB-003: Every submitted line is validated and stored as a purchased-line snapshot.
    // @BR-OR-SUB-004: The checkout-supplied total is validated against its accepted total components.
    // @BR-OR-DIG-001: Digital file facts are converted into line-bound download entitlements.
    // @BR-OR-FAIL-001: Invalid or failed submission processing is rejected without a partial local aggregate.
    public async Task<OrderDto> SubmitAsync(JsonElement body, RequestContext context, CancellationToken ct)
    {
        var submission = ParseSubmission(body, context);
        try { ValidateSubmission(submission); }
        catch (DomainException ex)
        {
            await events.RecordProcessingFailureAsync(context, submission.SubmissionId, ex.Code, ct);
            throw;
        }
        Order order;
        try { order = await repository.CreateSubmissionAsync(submission, context, ct); }
        catch (Exception ex) when (ex is DomainException or InvalidOperationException)
        {
            await events.RecordProcessingFailureAsync(context, submission.SubmissionId, ex.Message, ct);
            throw;
        }
        await events.PublishPendingAsync(ct);
        return DtoMapper.Order(order);
    }

    // @BR-OR-LIFE-001: Only the declared legal order transition matrix can change an order status.
    // @BR-OR-LIFE-002: The transition reason and actor are appended as immutable history.
    // @BR-OR-RES-001: Idempotency keys replay a status command without a second state change.
    public async Task<OrderDto> TransitionAsync(long id, TransitionOrderStatusRequestDto request, RequestContext context, string actor, string key, CancellationToken ct)
    {
        var status = NormalizeStatus(request.Status is null ? "" : StatusValues.Get(request.Status, ""));
        if (!OrderStatuses.Contains(status)) throw new DomainException("ORDER_STATUS_INVALID", "The requested order status is not declared.", 422);
        return DtoMapper.Order(await repository.TransitionAsync(id, status, request.Reason, actor, context, RequireKey(key), ct));
    }

    // @BR-OR-LIFE-001: Administrative history status is checked against the declared lifecycle states.
    // @BR-OR-LIFE-002: A history command appends a new record and never edits an earlier record.
    public async Task<OrderHistoryEntryDto> AppendHistoryAsync(long id, AppendHistoryRequestDto request, RequestContext context, string actor, string key, CancellationToken ct)
    {
        var status = NormalizeStatus(request.Status is null ? "" : StatusValues.Get(request.Status, ""));
        if (!OrderStatuses.Contains(status)) throw new DomainException("ORDER_STATUS_INVALID", "The history status is not declared.", 422);
        if (request.Source is not ("Admin" or "Payment" or "Fulfillment" or "System")) throw new DomainException("HISTORY_SOURCE_INVALID", "History source is not supported.", 422);
        return DtoMapper.HistoryEntry(await repository.AppendHistoryAsync(id, status, request.Comments, request.Source.ToUpperInvariant(), actor, context, RequireKey(key), ct));
    }

    // @BR-OR-ADM-002: Authorized correction changes only the order's persisted snapshot and not the customer master.
    public async Task<OrderDto> UpdateSnapshotAsync(long id, CustomerSnapshotUpdateRequestDto request, RequestContext context, string key, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.EmailAddress) || !request.EmailAddress.Contains('@'))
            throw new DomainException("ADDRESS_INVALID", "A valid emailAddress is required.", 422);
        return DtoMapper.Order(await repository.UpdateSnapshotAsync(id, new CustomerSnapshotUpdateRequest(request.EmailAddress.Trim(), ToAddress(request.BillingAddress), ToAddress(request.DeliveryAddress)), context, RequireKey(key), ct));
    }

    // @BR-OR-PAY-003: The next administrative payment action is derived from the latest timestamped payment outcome.
    public async Task<NextPaymentActionDto> NextPaymentAsync(long id, RequestContext context, CancellationToken ct)
    {
        _ = await repository.FindAsync(id, context, null, false, ct) ?? throw NotFound(id);
        var latest = await repository.LatestPaymentAsync(id, context, ct);
        var next = latest?.Action switch { "AUTHORIZE" => "Capture", "CAPTURE" or "AUTHORIZECAPTURE" => "Refund", "REFUND" => "Ok", _ => "Ok" };
        return new() { OrderId = id, NextAction = next, LastPaymentAction = latest?.Action };
    }

    // @BR-OR-PAY-001: Payment outcome transactions are read as an MS-06-owned projection.
    // @BR-OR-UI-001: Payment transactions provide the administration order-detail payment view.
    public async Task<PaymentTransactionListResponseDto> PaymentsAsync(long id, RequestContext context, CancellationToken ct)
    {
        _ = await repository.FindAsync(id, context, null, false, ct) ?? throw NotFound(id);
        return new() { Items = (await repository.PaymentsAsync(id, context, ct)).Select(x => new PaymentTransactionDto { TransactionId = x.TransactionId, Action = ToPaymentAction(x.Action), Status = x.Status == "SUCCEEDED" ? "Succeeded" : x.Status == "FAILED" ? "Failed" : "Unknown", Amount = x.Amount, Currency = x.Currency, PaymentReference = x.PaymentReference, OccurredAt = x.OccurredAt.ToString("O") }).ToList() };
    }

    // @BR-OR-PAY-004: Capturable orders have a successful authorization and no later capture or refund.
    public async Task<OrderListResponseDto> CapturableAsync(RequestContext context, DateTimeOffset? start, DateTimeOffset? end, int page, int pageSize, CancellationToken ct)
    {
        ValidatePage(page, pageSize);
        if (start is not null && end is not null && start > end) throw new DomainException("DATE_RANGE_INVALID", "startDate must not be after endDate.", 422);
        var items = await repository.CapturableAsync(context, start, end, page, pageSize, ct);
        return new() { Items = items.Select(x => DtoMapper.Order(x, false)).ToList(), Pagination = Page(page, pageSize, items.Count) };
    }

    // @BR-OR-PAY-001: Capture requests cross the MS-06 boundary through a durable command event.
    // @BR-OR-UI-001: Capture returns an explicit processing command result instead of a provider placeholder.
    public async Task<PaymentCommandResponseDto> CaptureAsync(long id, PaymentCommandRequestDto request, RequestContext context, string key, CancellationToken ct)
    {
        if (request.Amount <= 0 || request.Currency.Length != 3) throw new DomainException("PAYMENT_AMOUNT_INVALID", "Capture amount and currency are invalid.", 422);
        var order = await repository.FindAsync(id, context, null, false, ct) ?? throw NotFound(id);
        var latest = await repository.LatestPaymentAsync(id, context, ct);
        if (latest?.Action != "AUTHORIZE" || latest.Status != "SUCCEEDED") throw new DomainException("PAYMENT_ACTION_NOT_ALLOWED", "The order has no capturable authorization.", 409);
        if (request.Amount > latest.Amount) throw new DomainException("PAYMENT_AMOUNT_INVALID", "Capture amount exceeds the authorized balance.", 422);
        var commandId = $"cap-{Guid.NewGuid():N}";
        await events.EnqueueCommandAsync(context, id, "PaymentCaptureRequested", new { eventId = Guid.NewGuid(), eventType = "PaymentCaptureRequested", eventVersion = 1, occurredAt = DateTimeOffset.UtcNow, tenantId = context.TenantId, storeId = context.StoreId, correlationId = context.CorrelationId, orderId = id, commandId, amount = request.Amount, currency = request.Currency.ToUpperInvariant(), paymentReference = request.PaymentReference }, RequireKey(key), ct);
        return new() { OrderId = order.OrderId, CommandId = commandId, Action = "Capture", Status = "Processing", PaymentReference = request.PaymentReference };
    }

    // @BR-OR-REF-001: Refund requests reserve only the remaining captured balance and accumulate durably.
    // @BR-OR-PAY-001: Provider refund execution remains a command boundary owned by MS-06.
    public async Task<RefundCommandResponseDto> RefundAsync(long id, RefundRequestDto request, RequestContext context, string key, CancellationToken ct)
    {
        if (request.Amount <= 0 || request.Currency.Length != 3 || string.IsNullOrWhiteSpace(request.Reason))
            throw new DomainException("REFUND_AMOUNT_INVALID", "Refund amount, currency, and reason are required.", 422);
        var reservation = await repository.ReserveRefundAsync(id, request.Amount, request.Currency.ToUpperInvariant(), request.Reason, RequireKey(key), context, ct);
        return new() { OrderId = id, RefundId = reservation.RefundId, Amount = reservation.Amount, RemainingRefundable = reservation.RemainingRefundable, Status = reservation.Status == "APPLIED" ? "Applied" : "Processing" };
    }

    // @BR-OR-CAN-001: Cancellation applies terminal and fulfillment guards before recording compensation work.
    // @BR-OR-FAIL-001: Compensation remains visible as pending rather than claiming downstream completion.
    public async Task<CancellationResponseDto> CancelAsync(long id, CancelOrderRequestDto request, RequestContext context, string key, long? customerId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Reason)) throw new DomainException("CANCELLATION_REASON_REQUIRED", "Cancellation reason is required.", 422);
        if (customerId is not null && await repository.FindAsync(id, context, customerId, false, ct) is null) throw NotFound(id);
        var result = await repository.CancelAsync(id, request.Reason, RequireKey(key), context, ct);
        return new() { OrderId = result.OrderId, Status = DtoMapper.OrderStatus(result.Status), CompensationState = ToPascal(result.CompensationState) };
    }

    // @BR-OR-FUL-001: Fulfillment requests contain the immutable delivery and purchased-line boundary for MS-09/MS-12.
    public async Task<FulfillmentResponseDto> RequestFulfillmentAsync(long id, RequestContext context, string key, CancellationToken ct)
    {
        var result = await repository.RequestFulfillmentAsync(id, RequireKey(key), context, ct);
        await events.PublishPendingAsync(ct);
        return Fulfillment(result);
    }

    // @BR-OR-FUL-001: Fulfillment reads expose the current provider-coordinated state without executing carrier work.
    public async Task<FulfillmentResponseDto> GetFulfillmentAsync(long id, RequestContext context, long? customerId, CancellationToken ct)
    {
        _ = await repository.FindAsync(id, context, customerId, false, ct) ?? throw NotFound(id);
        return Fulfillment(await repository.GetFulfillmentAsync(id, context, ct) ?? throw new DomainException("FULFILLMENT_NOT_FOUND", "Fulfillment was not found.", 404));
    }

    // @BR-OR-INV-001: Invoice requests publish the accepted snapshot to MS-12 and expose processing until an artifact exists.
    public async Task<(InvoiceResponseDto Response, int Status)> InvoiceAsync(long id, RequestContext context, long? customerId, CancellationToken ct)
    {
        _ = await repository.FindAsync(id, context, customerId, false, ct) ?? throw NotFound(id);
        var state = await repository.RequestInvoiceAsync(id, context, ct);
        await events.PublishPendingAsync(ct);
        return (new() { OrderId = id, RequestId = state.RequestId, Status = ToPascal(state.Status), ArtifactUrl = state.ArtifactUrl, GeneratedAt = state.GeneratedAt?.ToString("O") }, state.Status == "AVAILABLE" ? 200 : 202);
    }

    // @BR-OR-RES-001: Authenticated payment event application is replay-safe and tenant/store scoped.
    public async Task<bool> ApplyEventAsync(string eventType, JsonElement body, RequestContext context, CancellationToken ct)
    {
        var eventId = String(body, "eventId") ?? throw new DomainException("EVENT_INVALID", "Event ID is required.", 422);
        if (eventType.Equals("OrderSubmitted.v1", StringComparison.OrdinalIgnoreCase))
        {
            await SubmitAsync(body, context, ct);
            return true;
        }
        if (eventType.Contains("Payment", StringComparison.OrdinalIgnoreCase))
        {
            var id = Long(body, "orderId"); var amount = Decimal(body, "amount");
            var action = eventType.Contains("Authorized") ? "AUTHORIZE" : eventType.Contains("Captured") ? "CAPTURE" : eventType.Contains("Refunded") ? "REFUND" : eventType.Contains("Voided") ? "VOID" : "FAIL";
            if (action == "REFUND")
            {
                var refundId = String(body, "refundId") ?? String(body, "providerReference") ?? $"rfd-event-{eventId}";
                var refund = await repository.ApplyRefundAsync(id, refundId, amount, String(body, "currency") ?? "", context, eventId, ct);
                return refund.Status == "APPLIED";
            }
            return await repository.ApplyPaymentAsync(new PaymentOutcome(String(body, "paymentIntentId") ?? $"event-{eventId}", id, action, action == "FAIL" ? "FAILED" : "SUCCEEDED", amount, String(body, "currency") ?? "", String(body, "providerReference"), DateTimeOffset.TryParse(String(body, "occurredAt"), out var occurred) ? occurred : DateTimeOffset.UtcNow), context, eventId, ct);
        }
        if (eventType.Equals("ShipmentStatusUpdated", StringComparison.OrdinalIgnoreCase))
            return await repository.ApplyShipmentAsync(Long(body, "orderId"), NormalizeFulfillment(String(body, "status") ?? ""), String(body, "carrierReference"), context, eventId, ct) is not null;
        if (eventType.Equals("InvoiceAvailable", StringComparison.OrdinalIgnoreCase))
        {
            await repository.MarkInvoiceAsync(String(body, "requestId") ?? throw new DomainException("EVENT_INVALID", "Invoice requestId is required.", 422), "AVAILABLE", String(body, "artifactUrl"), context, ct);
            return true;
        }
        throw new DomainException("EVENT_UNSUPPORTED", $"Event type {eventType} is not supported.", 422);
    }

    private static Submission ParseSubmission(JsonElement body, RequestContext context)
    {
        var submissionId = String(body, "submissionId") ?? throw new DomainException("SUBMISSION_ID_REQUIRED", "submissionId is required.", 422);
        var currency = (String(body, "currency") ?? "").Trim().ToUpperInvariant();
        var lines = body.TryGetProperty("lines", out var rawLines) && rawLines.ValueKind == JsonValueKind.Array ? rawLines.EnumerateArray().Select(ParseLine).ToList() : [];
        var totals = body.TryGetProperty("totals", out var rawTotals) && rawTotals.ValueKind == JsonValueKind.Array ? rawTotals.EnumerateArray().Select(ParseTotal).ToList() : [];
        return new(submissionId, NullableLong(body, "customerId"), String(body, "customerEmailAddress") ?? String(body, "customerEmail") ?? "unknown@example.invalid", context.StoreNumber, currency, Decimal(body, "total"), lines, totals, Address(body, "billingAddress") ?? Address(body, "billing"), Address(body, "deliveryAddress") ?? Address(body, "delivery"), String(body, "paymentType"), String(body, "paymentModuleCode"), String(body, "shippingModuleCode"), Bool(body, "customerAgreed"), String(body, "locale"), String(body, "paymentToken"), String(body, "reservationId"), String(body, "comments"));
    }
    private static SubmissionLine ParseLine(JsonElement x)
    {
        var attrs = x.TryGetProperty("attributes", out var a) && a.ValueKind == JsonValueKind.Array
            ? a.EnumerateArray().Select(y => new SubmissionAttribute(Long(y, "optionId"), Long(y, "optionValueId"), String(y, "name") ?? "", String(y, "value") ?? "", Decimal(y, "price"), Bool(y, "free"), NullableDecimal(y, "weight"))).ToList() : [];
        var prices = x.TryGetProperty("prices", out var p) && p.ValueKind == JsonValueKind.Array
            ? p.EnumerateArray().Select(y => new SubmissionPrice(String(y, "code") ?? "accepted", String(y, "name"), Decimal(y, "price", "unitPrice"), NullableDecimal(y, "specialPrice"), ParseDate(y, "specialStartDate"), ParseDate(y, "specialEndDate"), Bool(y, "defaultPrice"))).ToList() : [];
        var days = NullableLong(x, "downloadExpiryDays") ?? 31;
        return new(String(x, "sku") ?? "", String(x, "productName") ?? String(x, "name") ?? "", (int)Long(x, "quantity"), Decimal(x, "unitPrice", "oneTimeCharge"), attrs, prices, String(x, "digitalFileName"), (int)(days > 0 ? days : 31));
    }
    private static SubmissionTotal ParseTotal(JsonElement x) => new(String(x, "code") ?? "", String(x, "title"), String(x, "text"), Decimal(x, "value"), String(x, "module"), String(x, "type"), String(x, "valueType")?.ToUpperInvariant(), (int)Long(x, "sortOrder"));
    private static AddressSnapshot? Address(JsonElement body, string name) { if (!body.TryGetProperty(name, out var x) || x.ValueKind == JsonValueKind.Null) return null; return new(String(x, "firstName") ?? "", String(x, "lastName") ?? "", String(x, "company"), String(x, "address") ?? String(x, "streetAddress") ?? "", String(x, "city") ?? "", String(x, "state") ?? String(x, "stateProvince"), String(x, "countryCode") ?? String(x, "country") ?? "", String(x, "zoneCode") ?? String(x, "zone"), String(x, "postalCode") ?? "", String(x, "telephone") ?? String(x, "phone")); }
    private static AddressSnapshot ToAddress(AddressSnapshotDto x) => new(x.FirstName, x.LastName, x.Company, x.Address, x.City, x.State, x.CountryCode, x.ZoneCode, x.PostalCode, x.Telephone);
    private static void ValidateSubmission(Submission x) { if (x.Lines.Count == 0) throw new DomainException("ORDER_LINES_REQUIRED", "An order must contain at least one purchased line.", 422); if (x.Currency.Length != 3) throw new DomainException("CURRENCY_INVALID", "currency must contain exactly three letters.", 422); if (x.Total < 0 || x.Totals.Any(y => y.Value < 0)) throw new DomainException("TOTAL_INVALID", "Order totals cannot be negative.", 422); if (x.Totals.Count > 0 && x.Totals.Where(y => !string.Equals(y.Type, "REFUND", StringComparison.OrdinalIgnoreCase)).Sum(y => y.Value) != x.Total) throw new DomainException("TOTAL_MISMATCH", "Submitted total does not match accepted total components.", 422); foreach (var line in x.Lines) { if (string.IsNullOrWhiteSpace(line.Sku) || string.IsNullOrWhiteSpace(line.ProductName) || line.Quantity <= 0 || line.UnitPrice < 0) throw new DomainException("LINE_INVALID", "Each purchased line requires a positive quantity and non-negative price.", 422); foreach (var a in line.Attributes) if (a.OptionId <= 0 || a.OptionValueId <= 0 || string.IsNullOrWhiteSpace(a.Name) || string.IsNullOrWhiteSpace(a.Value) || a.Price < 0) throw new DomainException("ATTRIBUTE_INVALID", "The selected product attribute is invalid for this store.", 422); if (line.DigitalFileName is not null && string.IsNullOrWhiteSpace(line.DigitalFileName)) throw new DomainException("DIGITAL_FILE_INVALID", "A digital entitlement requires a file name.", 422); } }
    private static string NormalizeStatus(string? value) => string.IsNullOrWhiteSpace(value) ? "" : value.ToUpperInvariant() switch { "ORDERED" => "ORDERED", "PROCESSED" => "PROCESSED", "DELIVERED" => "DELIVERED", "REFUNDED" => "REFUNDED", "CANCELED" => "CANCELED", _ => throw new DomainException("ORDER_STATUS_INVALID", "The requested order status is not declared.", 422) };
    private static string NormalizeFulfillment(string value) => value.ToUpperInvariant() switch { "REQUESTED" => "REQUESTED", "IN_PROGRESS" or "INPROGRESS" => "IN_PROGRESS", "SHIPPED" => "SHIPPED", "DELIVERED" => "DELIVERED", "CANCELED" => "CANCELED", _ => throw new DomainException("FULFILLMENT_STATUS_INVALID", "The fulfillment status is not declared.", 422) };
    private static FulfillmentResponseDto Fulfillment(Fulfillment x) => new() { OrderId = x.OrderId, FulfillmentId = x.Id.ToString(), Status = DtoMapper.FulfillmentStatus(x.Status), CarrierReference = x.CarrierReference, LastUpdatedAt = x.LastUpdatedAt.ToString("O") };
    private static PaginationInfoDto Page(int page, int size, long total) => new() { Page = page, PageSize = size, TotalItems = total, TotalPages = (int)Math.Ceiling(total / (double)size) };
    private static void ValidatePage(int page, int size) { if (page < 1 || size < 1 || size > 100) throw new DomainException("PAGINATION_INVALID", "page must be at least 1 and pageSize must be between 1 and 100.", 422); }
    private static string RequireKey(string key) => string.IsNullOrWhiteSpace(key) ? throw new DomainException("IDEMPOTENCY_KEY_REQUIRED", "Idempotency-Key is required.", 422) : key;
    private static string ToPascal(string x) => x.Length == 0 ? x : x[0] + x[1..].ToLowerInvariant();
    private static string ToPaymentAction(string x) => x switch { "AUTHORIZE" => "Authorize", "AUTHORIZECAPTURE" => "AuthorizeCapture", "CAPTURE" => "Capture", "REFUND" => "Refund", "INIT" => "Init", _ => "Ok" };
    private static DomainException NotFound(long id) => new("ORDER_NOT_FOUND", $"Order {id} was not found in this store.", 404);
    private static string? String(JsonElement e, string name) => e.TryGetProperty(name, out var x) && x.ValueKind != JsonValueKind.Null ? x.ToString() : null;
    private static decimal Decimal(JsonElement e, string name, string? alternate = null) => e.TryGetProperty(name, out var x) && x.TryGetDecimal(out var value) ? value : alternate is not null && e.TryGetProperty(alternate, out x) && x.TryGetDecimal(out value) ? value : 0m;
    private static decimal? NullableDecimal(JsonElement e, string name) => e.TryGetProperty(name, out var x) && x.TryGetDecimal(out var value) ? value : null;
    private static DateTimeOffset? ParseDate(JsonElement e, string name) => DateTimeOffset.TryParse(String(e, name), out var value) ? value : null;
    private static long Long(JsonElement e, string name) => NullableLong(e, name) ?? throw new DomainException("INVALID_REQUEST", $"{name} is required.", 422);
    private static long? NullableLong(JsonElement e, string name) { if (!e.TryGetProperty(name, out var x) || x.ValueKind == JsonValueKind.Null) return null; return x.ValueKind == JsonValueKind.Number && x.TryGetInt64(out var n) ? n : long.TryParse(x.ToString(), out n) ? n : null; }
    private static bool Bool(JsonElement e, string name) => e.TryGetProperty(name, out var x) && x.ValueKind == JsonValueKind.True && x.GetBoolean();
}
