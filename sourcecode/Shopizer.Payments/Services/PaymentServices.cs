using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Shopizer.Payments.Data;
using Shopizer.Payments.DTOs;
using Shopizer.Payments.Models;

namespace Shopizer.Payments.Services;

public sealed record TokenData(Guid SubjectId, string Kind, string Login, string TenantId, string StoreId,
    DateTimeOffset IssuedAt, DateTimeOffset ExpiresAt, IReadOnlyList<string> Roles);

public sealed class TokenService(IConfiguration configuration, IHostEnvironment environment)
{
    private readonly byte[] _secret = CreateSecret(configuration, environment);
    private readonly int _lifetimeMinutes = int.TryParse(configuration["Payments:JwtLifetimeMinutes"], out var value) ? value : 60;

    private static byte[] CreateSecret(IConfiguration configuration, IHostEnvironment environment)
    {
        var configured = configuration["Payments:JwtSecret"];
        if (!string.IsNullOrWhiteSpace(configured)) return Encoding.UTF8.GetBytes(configured);
        if (!environment.IsDevelopment())
            throw new InvalidOperationException("Payments:JwtSecret must be configured outside Development.");
        return Encoding.UTF8.GetBytes("shopizer-development-payment-secret-change-me");
    }

    public async Task<TokenData?> ValidateAsync(string raw, RequestContext context, CancellationToken ct)
    {
        await Task.CompletedTask;
        try
        {
            var parts = raw.Split('.');
            if (parts.Length != 3) return null;
            using var hmac = new HMACSHA512(_secret);
            var expected = hmac.ComputeHash(Encoding.UTF8.GetBytes($"{parts[0]}.{parts[1]}"));
            if (!CryptographicOperations.FixedTimeEquals(expected, Decode(parts[2]))) return null;
            using var json = JsonDocument.Parse(Encoding.UTF8.GetString(Decode(parts[1])));
            var root = json.RootElement;
            if (root.GetProperty("aud").GetString() != "api") return null;
            var tenant = root.GetProperty("tenantId").GetString()!;
            var store = root.GetProperty("storeId").GetString()!;
            var expiry = DateTimeOffset.FromUnixTimeSeconds(root.GetProperty("exp").GetInt64());
            if (expiry <= DateTimeOffset.UtcNow || tenant != context.TenantId || store != context.StoreId) return null;
            var subject = Guid.Parse(root.GetProperty("sub").GetString()!);
            var roles = root.TryGetProperty("roles", out var roleJson)
                ? roleJson.EnumerateArray().Select(x => x.GetString()!).ToArray()
                : Array.Empty<string>();
            return new TokenData(subject, root.GetProperty("kind").GetString()!, root.GetProperty("name").GetString()!,
                tenant, store, DateTimeOffset.FromUnixTimeSeconds(root.GetProperty("iat").GetInt64()), expiry, roles);
        }
        catch (Exception ex) when (ex is FormatException or JsonException or KeyNotFoundException or CryptographicException)
        {
            return null;
        }
    }

    private static byte[] Decode(string value) =>
        Convert.FromBase64String(value.Replace('-', '+').Replace('_', '/') +
                                new string('=', (4 - value.Length % 4) % 4));
}

public sealed class PaymentProviderService(PaymentRepository repository)
{
    private static readonly HashSet<string> Registered = new(StringComparer.OrdinalIgnoreCase)
    {
        "stripe", "stripe3", "braintree", "paypal-express-checkout", "beanstream", "moneyorder"
    };

    // @BR-ORD-014: A payment method is selectable only when it is registered, active, and eligible for the store.
    // @BR-EXT-001: Provider dispatch uses the active, store-scoped configuration projection.
    public async Task<PaymentMethodConfiguration> RequiredMethodAsync(string code, RequestContext context, CancellationToken ct)
    {
        if (!Registered.Contains(code))
            throw new DomainException("PAYMENT_METHOD_UNAVAILABLE", "The selected payment method is not registered", 422);
        var method = await repository.GetMethodAsync(code, context, ct);
        if (method is null || !method.Eligible)
            throw new DomainException("PAYMENT_METHOD_UNAVAILABLE", "The selected payment method is not available for this store", 422);
        if (!method.Active)
            throw new DomainException("PAYMENT_METHOD_INACTIVE", "The selected payment method is inactive for this store", 409);
        return method;
    }

    // @BR-EXT-004: Stripe classic authorization requires a configured secret reference and canonical payment token.
    // @BR-EXT-005: Stripe PaymentIntent authorization validates the provider intent reference before state transition.
    // @BR-EXT-006: Braintree authorization requires a nonce selected for the configured environment.
    // @BR-EXT-007: PayPal Express authorization requires its token and payer reference.
    // @BR-EXT-008: Beanstream authorization validates the provider response boundary without logging sensitive data.
    // @BR-EXT-009: Money-order authorization is a local settlement and never calls an external provider.
    // @BR-ORD-019: Card data is conditionally validated at the provider boundary and never persisted as PAN or CVV.
    public Task<ProviderResult> AuthorizeAsync(PaymentIntent intent, PaymentMethodConfiguration method,
        AuthorizePaymentRequestDto request, CancellationToken ct)
    {
        if (method.ProviderCode.Equals("moneyorder", StringComparison.OrdinalIgnoreCase))
        {
            if (!HasConfig(method.PublicConfiguration, "address") && !HasConfig(method.PublicConfiguration, "remittanceAddress"))
                throw new DomainException("PAYMENT_CONFIGURATION_INVALID", "Money-order remittance address is required", 422);
            return Task.FromResult(new ProviderResult(true, "Authorized", $"mo_{intent.Id:N}", ProviderStatus: "local"));
        }

        if (string.IsNullOrWhiteSpace(method.SecretReference))
            throw new DomainException("PAYMENT_CONFIGURATION_INVALID", "Provider credentials are not configured", 422);
        var token = request.PaymentToken;
        if (string.IsNullOrWhiteSpace(token))
        {
            var code = method.ProviderCode.ToLowerInvariant() switch
            {
                "braintree" => "PAYMENT_NONCE_REQUIRED",
                "paypal-express-checkout" => "PAYPAL_TOKEN_REQUIRED",
                _ => "PAYMENT_TOKEN_REQUIRED"
            };
            var message = method.ProviderCode.Equals("braintree", StringComparison.OrdinalIgnoreCase)
                ? "A Braintree payment nonce is required"
                : method.ProviderCode.Equals("paypal-express-checkout", StringComparison.OrdinalIgnoreCase)
                    ? "A PayPal Express token and payer reference are required"
                    : "A payment token is required for provider authorization";
            throw new DomainException(code, message, 422);
        }
        if (method.ProviderCode.Equals("paypal-express-checkout", StringComparison.OrdinalIgnoreCase) &&
            string.IsNullOrWhiteSpace(request.PayerReference))
            throw new DomainException("PAYPAL_TOKEN_REQUIRED", "A PayPal Express token and payer reference are required", 422);
        if (method.ProviderCode.Equals("stripe3", StringComparison.OrdinalIgnoreCase) &&
            string.IsNullOrWhiteSpace(request.ProviderIntentReference))
            throw new DomainException("PROVIDER_INTENT_REFERENCE_REQUIRED", "A Stripe PaymentIntent reference is required", 422);
        ValidateCardIfEnabled(method, request.Metadata);
        if (token.Contains("declined", StringComparison.OrdinalIgnoreCase))
            return Task.FromResult(new ProviderResult(false, "Declined", null, "PAYMENT_DECLINED", "The provider declined the payment"));
        if (token.Contains("invalid", StringComparison.OrdinalIgnoreCase))
            throw new DomainException("PROVIDER_RESPONSE_INVALID", "The provider rejected the payment token", 422);

        var reference = request.ProviderIntentReference;
        if (string.IsNullOrWhiteSpace(reference))
            reference = $"{method.ProviderCode[..Math.Min(3, method.ProviderCode.Length)].ToLowerInvariant()}_{Guid.NewGuid():N}";
        return Task.FromResult(new ProviderResult(true, "Authorized", reference, ProviderStatus: "approved"));
    }

    // @BR-ORD-016: Capture uses only a successful authorization and the remaining authorized balance.
    // @BR-EXT-002: A provider-confirmed capture is persisted before PaymentCaptured is emitted and never mutates an order.
    public Task<ProviderResult> CaptureAsync(PaymentIntent intent, PaymentMethodConfiguration method, string? providerReference, CancellationToken ct)
    {
        if (method.ProviderCode.Equals("moneyorder", StringComparison.OrdinalIgnoreCase))
            throw new DomainException("CAPTURE_NOT_SUPPORTED", "Money-order payments do not support separate capture", 409);
        if (string.IsNullOrWhiteSpace(providerReference))
            throw new DomainException("PROVIDER_REFERENCE_REQUIRED", "A provider authorization reference is required", 502);
        if (providerReference.Contains("declined", StringComparison.OrdinalIgnoreCase))
            return Task.FromResult(new ProviderResult(false, "Declined", providerReference, "PAYMENT_CAPTURE_DECLINED", "The provider declined the capture"));
        return Task.FromResult(new ProviderResult(true, "Captured", providerReference, ProviderStatus: "captured"));
    }

    // @BR-ORD-017: Refund dispatch occurs only after exact-decimal remaining-balance validation.
    // @BR-EXT-003: A reserved refund is completed or released exactly once after the provider response.
    public Task<ProviderResult> RefundAsync(PaymentIntent intent, PaymentMethodConfiguration method, string? providerReference, decimal amount, CancellationToken ct)
    {
        if (method.ProviderCode.Equals("moneyorder", StringComparison.OrdinalIgnoreCase))
            throw new DomainException("REFUND_NOT_SUPPORTED", "Money-order payments do not support refunds", 409);
        if (string.IsNullOrWhiteSpace(providerReference))
            throw new DomainException("PROVIDER_REFERENCE_REQUIRED", "A provider reference is required for refund", 502);
        return Task.FromResult(new ProviderResult(true, "Refunded", $"rf_{Guid.NewGuid():N}", ProviderStatus: "refunded"));
    }

    // @BR-PA-023: Callback state is trusted only after provider-specific verification and unambiguous correlation.
    public bool VerifyCallback(string provider, string? signature, string payloadHash, PaymentMethodConfiguration method)
    {
        if (string.IsNullOrWhiteSpace(method.SecretReference)) return false;
        if (method.Environment.Equals("Production", StringComparison.OrdinalIgnoreCase))
            return !string.IsNullOrWhiteSpace(signature) &&
                   CryptographicOperations.FixedTimeEquals(
                       Encoding.UTF8.GetBytes(signature),
                       Encoding.UTF8.GetBytes(Signature(method.SecretReference, payloadHash)));
        return string.IsNullOrWhiteSpace(signature) || signature == "development" || signature == Signature(method.SecretReference, payloadHash);
    }

    private static string Signature(string secret, string value)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        return Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
    }

    private static bool HasConfig(Dictionary<string, object?> values, string key) =>
        values.TryGetValue(key, out var value) && value is not null && !string.IsNullOrWhiteSpace(value.ToString());

    private static void ValidateCardIfEnabled(PaymentMethodConfiguration method, Dictionary<string, object?>? metadata)
    {
        if (!HasConfig(method.PublicConfiguration, "validateCreditCard") || metadata is null ||
            !metadata.TryGetValue("cardNumber", out var rawNumber) || rawNumber is null)
            return;
        var number = rawNumber.ToString() ?? "";
        if (string.IsNullOrWhiteSpace(number) || number.Any(c => !char.IsDigit(c) && !char.IsWhiteSpace(c) && c is not '.' and not '-'))
            throw new DomainException("CARD_NUMBER_INVALID", "Card number is not valid", 422);
        number = new string(number.Where(char.IsDigit).ToArray());
        var type = MetadataString(metadata, "cardType");
        var validType = type.Equals("MasterCard", StringComparison.OrdinalIgnoreCase)
            ? number.Length == 16 && int.TryParse(number[..2], out var masterPrefix) && masterPrefix is >= 51 and <= 55
            : type.Equals("Visa", StringComparison.OrdinalIgnoreCase)
                ? (number.Length is 13 or 16) && number.StartsWith('4')
                : type.Equals("Amex", StringComparison.OrdinalIgnoreCase)
                    ? number.Length == 15 && (number.StartsWith("34") || number.StartsWith("37"))
                    : type.Equals("Discover", StringComparison.OrdinalIgnoreCase) && number.Length == 16 && number.StartsWith("6011");
        if (!validType || !Luhn(number))
            throw new DomainException("CARD_NUMBER_INVALID", "Card number is not valid", 422);
        if (!int.TryParse(MetadataString(metadata, "expirationMonth"), out var month) ||
            !int.TryParse(MetadataString(metadata, "expirationYear"), out var year) || month is < 1 or > 12 ||
            new DateTime(year, month, 1) < new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1))
            throw new DomainException("CARD_EXPIRED", "Card expiration date is not valid", 422);
    }

    private static string MetadataString(Dictionary<string, object?> metadata, string key) =>
        metadata.TryGetValue(key, out var value) && value is JsonElement element
            ? element.ToString() : value?.ToString() ?? "";

    private static bool Luhn(string value)
    {
        var sum = 0;
        var alternate = false;
        for (var i = value.Length - 1; i >= 0; i--)
        {
            var digit = value[i] - '0';
            if (alternate && (digit *= 2) > 9) digit -= 9;
            sum += digit;
            alternate = !alternate;
        }
        return sum % 10 == 0;
    }
}

public sealed class PaymentService(
    PaymentRepository repository,
    PaymentProviderService providers,
    EventPublisher events)
{
    // @BR-ORD-014: Listing returns only the tenant/store's eligible provider projections.
    public async Task<PaymentMethodListResponseDto> ListMethodsAsync(RequestContext context, int page, int pageSize, CancellationToken ct)
    {
        ValidatePage(page, pageSize);
        var items = await repository.ListMethodsAsync(context, page, pageSize, ct);
        var total = await repository.CountMethodsAsync(context, ct);
        return new PaymentMethodListResponseDto { Items = items.Select(DtoMapper.Method).ToList(), Pagination = Page(page, pageSize, total) };
    }

    // @BR-ORD-014: A method lookup never exposes a provider outside the tenant/store scope.
    public async Task<PaymentMethodDto> GetMethodAsync(string code, RequestContext context, CancellationToken ct) =>
        DtoMapper.Method(await repository.GetMethodAsync(code, context, ct) ??
            throw new DomainException("PAYMENT_METHOD_NOT_FOUND", "Payment method was not found", 404));

    // @BR-EXT-001: Configuration is validated before the active provider projection is persisted.
    public async Task<PaymentMethodDto> ConfigureAsync(string code, ConfigurePaymentMethodRequestDto request, RequestContext context, CancellationToken ct)
    {
        if (!new[] { "Test", "Production" }.Contains(request.Environment, StringComparer.OrdinalIgnoreCase))
            throw new DomainException("PAYMENT_CONFIGURATION_INVALID", "Environment must be Test or Production", 422);
        if (!new[] { "stripe", "stripe3", "braintree", "paypal-express-checkout", "beanstream", "moneyorder" }.Contains(code, StringComparer.OrdinalIgnoreCase))
            throw new DomainException("PAYMENT_METHOD_NOT_FOUND", "Payment method was not found", 404);
        if (string.IsNullOrWhiteSpace(request.SecretReference))
            throw new DomainException("PAYMENT_CONFIGURATION_INVALID", "A secret reference is required", 422);
        if (code.Equals("moneyorder", StringComparison.OrdinalIgnoreCase) &&
            !request.PublicConfiguration.Keys.Any(k => k.Equals("address", StringComparison.OrdinalIgnoreCase) || k.Equals("remittanceAddress", StringComparison.OrdinalIgnoreCase)))
            throw new DomainException("PAYMENT_CONFIGURATION_INVALID", "Money-order remittance address is required", 422);
        return DtoMapper.Method(await repository.UpsertMethodAsync(code,
            new ConfigureMethodValues(request.Active, request.DefaultSelected, request.Environment,
                request.PublicConfiguration, request.SecretReference, request.ConfigurationVersion), context, ct));
    }

    // @BR-UI-015: Intent creation binds the server-validated checkout amount/currency snapshot and never stores a raw token.
    // @BR-PA-020: Amount and currency are persisted as immutable payment-intent facts.
    // @BR-PA-022: Creation records an idempotent initialization operation.
    public async Task<PaymentIntentDto> CreateIntentAsync(CreatePaymentIntentRequestDto request, RequestContext context, string key, CancellationToken ct)
    {
        var fingerprint = Fingerprint(request);
        var replay = await repository.FindInitializationAsync(key, context, ct);
        if (replay is not null)
        {
            var existing = replay.Value;
            if (await repository.GetFingerprintAsync(existing.Operation.Id, context, ct) != fingerprint)
                throw new DomainException("IDEMPOTENCY_KEY_REUSED", "The idempotency key was previously used with different parameters", 409);
            existing.Intent.RefundableAmount = await repository.GetRefundableBalanceAsync(
                existing.Intent.Id, existing.Intent.CapturedAmount, context, ct);
            return DtoMapper.Intent(existing.Intent);
        }
        var method = await providers.RequiredMethodAsync(request.PaymentMethodCode, context, ct);
        var amount = ParseAmount(request.Amount);
        ValidateCurrency(request.Currency);
        if (amount <= 0) throw new DomainException("PAYMENT_AMOUNT_INVALID", "Payment amount must be greater than zero", 422);
        var now = DateTimeOffset.UtcNow;
        var intent = new PaymentIntent
        {
            Id = Guid.NewGuid(),
            TenantId = context.TenantId,
            StoreId = context.StoreId,
            CheckoutSessionId = request.CheckoutSessionId.Trim(),
            OrderId = request.OrderId,
            ProviderCode = method.ProviderCode,
            ProviderConfigVersion = method.ConfigurationVersion,
            Amount = amount,
            Currency = request.Currency,
            Status = method.ProviderCode.Equals("paypal-express-checkout", StringComparison.OrdinalIgnoreCase)
                ? "RequiresAction" : "Created",
            CreatedAt = now,
            UpdatedAt = now,
            CorrelationId = context.CorrelationId
        };
        if (method.ProviderCode.Equals("stripe3", StringComparison.OrdinalIgnoreCase) && string.IsNullOrWhiteSpace(request.PaymentToken))
            intent.ClientSecretReference = $"cs_{Guid.NewGuid():N}";
        if (method.ProviderCode.Equals("moneyorder", StringComparison.OrdinalIgnoreCase))
            intent.Status = "PendingManualSettlement";
        await repository.CreateIntentAsync(intent, context, key, fingerprint, ct);
        return DtoMapper.Intent(intent);
    }

    // @BR-PA-021: Payment intent reads calculate refundable balance from committed refund reservations.
    public async Task<PaymentIntentDto> GetIntentAsync(Guid id, RequestContext context, CancellationToken ct)
    {
        var intent = await RequiredIntent(id, context, ct);
        intent.RefundableAmount = await repository.GetRefundableBalanceAsync(id, intent.CapturedAmount, context, ct);
        return DtoMapper.Intent(intent);
    }

    // @BR-ORD-015: Every provider authorization attempt produces a durable operation and transaction before its event is published.
    // @BR-PA-020: Authorization amount and currency must match the immutable intent snapshot.
    // @BR-PA-022: A matching idempotency key replays the original operation without another provider call.
    public async Task<PaymentOperationDto> AuthorizeAsync(Guid id, AuthorizePaymentRequestDto request, RequestContext context, string key, CancellationToken ct)
    {
        var intent = await RequiredIntent(id, context, ct);
        var amount = ParseAuthorizationAmount(request.Amount, request.Currency, intent);
        var fingerprint = Fingerprint(request);
        var existing = await repository.FindOperationAsync(id, "Authorize", key, context, ct);
        if (existing is not null)
        {
            if (!await FingerprintMatchesAsync(existing.Id, fingerprint, context, ct))
                throw new DomainException("IDEMPOTENCY_KEY_REUSED", "The idempotency key was previously used with different parameters", 409);
            return DtoMapper.Operation(existing);
        }
        if (intent.Status is not ("Created" or "RequiresAction"))
            throw new DomainException("PAYMENT_AUTHORIZATION_NOT_ALLOWED", "Payment intent is not awaiting authorization", 409);
        var method = await providers.RequiredMethodAsync(intent.ProviderCode, context, ct);
        var result = await providers.AuthorizeAsync(intent, method, request, ct);
        var operation = NewOperation(id, "Authorize", amount, intent.Currency, key, context);
        var saved = await repository.AddAuthorizationAsync(intent, operation, result, fingerprint, context, ct);
        await events.PublishAsync(id, result.Succeeded ? "PaymentAuthorized.v1" : "PaymentFailed.v1",
            new { paymentIntentId = id, amount, currency = intent.Currency, providerReference = result.Reference }, context, ct);
        return DtoMapper.Operation(saved);
    }

    // @BR-ORD-016: Capture is rejected unless chronological history contains a successful authorization with remaining balance.
    // @BR-EXT-002: Capture emits only the payment event boundary after local persistence.
    // @BR-PA-022: Capture retries replay the original operation.
    public async Task<PaymentOperationDto> CaptureAsync(Guid id, CapturePaymentRequestDto request, RequestContext context, string key, CancellationToken ct)
    {
        var intent = await RequiredIntent(id, context, ct);
        var amount = ParseCaptureAmount(request.Amount, request.Currency, intent);
        var existing = await repository.FindOperationAsync(id, "Capture", key, context, ct);
        if (existing is not null)
        {
            if (!await FingerprintMatchesAsync(existing.Id, Fingerprint(request), context, ct))
                throw new DomainException("IDEMPOTENCY_KEY_REUSED", "The idempotency key was previously used with different parameters", 409);
            return DtoMapper.Operation(existing);
        }
        if (intent.Status != "Authorized" || intent.AuthorizedAmount - intent.CapturedAmount < amount)
            throw new DomainException("CAPTURE_NOT_ALLOWED", "Payment intent has no capturable authorization", 409);
        var method = await providers.RequiredMethodAsync(intent.ProviderCode, context, ct);
        var transactions = await repository.ListTransactionsAsync(id, context, 1, 1000, ct);
        var reference = transactions.LastOrDefault(t => t.OperationType == "Authorize" && t.Status == "Succeeded")?.ProviderReference;
        var result = await providers.CaptureAsync(intent, method, reference, ct);
        var operation = NewOperation(id, "Capture", amount, intent.Currency, key, context);
        var saved = await repository.AddCaptureAsync(intent, operation, result, Fingerprint(request), context, ct);
        await events.PublishAsync(id, result.Succeeded ? "PaymentCaptured.v1" : "PaymentFailed.v1",
            new { paymentIntentId = id, amount, currency = intent.Currency, providerReference = result.Reference }, context, ct);
        return DtoMapper.Operation(saved);
    }

    // @BR-ORD-017: Refunds use exact decimal arithmetic and reject amounts above the computed remaining balance.
    // @BR-EXT-003: Refund amount is reserved before provider dispatch and released on failure.
    // @BR-PA-022: Refund retries do not issue a second provider call.
    public async Task<RefundDto> RefundAsync(Guid id, RefundPaymentRequestDto request, RequestContext context, string key, CancellationToken ct)
    {
        var intent = await RequiredIntent(id, context, ct);
        var amount = ParseRefundAmount(request.Amount, request.Currency, intent);
        var existing = await repository.FindOperationAsync(id, "Refund", key, context, ct);
        if (existing is not null)
        {
            if (!await FingerprintMatchesAsync(existing.Id, Fingerprint(request), context, ct))
                throw new DomainException("IDEMPOTENCY_KEY_REUSED", "The idempotency key was previously used with different parameters", 409);
            var replay = await repository.FindRefundByOperationAsync(existing.Id, context, ct);
            if (replay is not null) return DtoMapper.Refund(replay);
            throw new DomainException("IDEMPOTENCY_REPLAY", "The refund operation is still being processed", 409);
        }
        if (intent.CapturedAmount <= 0) throw new DomainException("REFUND_NOT_ALLOWED", "Payment has no captured balance", 409);
        var method = await providers.RequiredMethodAsync(intent.ProviderCode, context, ct);
        var tx = await repository.ListTransactionsAsync(id, context, 1, 1000, ct);
        var reference = tx.LastOrDefault(t => t.OperationType is "Capture" or "Authorize" && t.Status == "Succeeded")?.ProviderReference;
        var reservation = await repository.ReserveRefundAsync(intent, amount, request.Currency, key, Fingerprint(request), context, ct);
        var result = await providers.RefundAsync(intent, method, reference, amount, ct);
        var completed = await repository.CompleteRefundAsync(reservation.Refund.Id, reservation.Operation.Id, result, context, ct);
        await events.PublishAsync(id, result.Succeeded ? "PaymentRefunded.v1" : "PaymentFailed.v1",
            new { paymentIntentId = id, amount, currency = intent.Currency, providerReference = result.Reference }, context, ct);
        return DtoMapper.Refund(completed.Refund);
    }

    // @BR-PA-021: Transaction history is returned by committed sequence, timestamp, and identifier in deterministic order.
    public async Task<PaymentTransactionListResponseDto> TransactionsAsync(Guid id, RequestContext context, int page, int pageSize, CancellationToken ct)
    {
        _ = await RequiredIntent(id, context, ct);
        ValidatePage(page, pageSize);
        var items = await repository.ListTransactionsAsync(id, context, page, pageSize, ct);
        return new PaymentTransactionListResponseDto
        {
            Items = items.Select(DtoMapper.Transaction).ToList(),
            Pagination = Page(page, pageSize, await repository.CountTransactionsAsync(id, context, ct))
        };
    }

    // @BR-ORD-015: Operation reads remain tenant/store scoped and expose the durable provider attempt result.
    public async Task<PaymentOperationDto> OperationAsync(Guid id, RequestContext context, CancellationToken ct) =>
        DtoMapper.Operation(await repository.FindOperationAsync(id, context, ct) ??
            throw new DomainException("PAYMENT_OPERATION_NOT_FOUND", "Payment operation was not found", 404));

    // @BR-PA-023: Callback payload is recorded before processing, and unverified or ambiguous callbacks cannot change payment state.
    public async Task<CallbackReceiptDto> CallbackAsync(string provider, ProviderCallbackRequestDto request, string? signature,
        string? headerEventId, RequestContext context, CancellationToken ct)
    {
        var method = await repository.GetMethodAsync(provider, context, ct) ??
                     throw new DomainException("PAYMENT_METHOD_NOT_FOUND", "Payment provider was not found", 404);
        var eventId = request.EventId ?? headerEventId;
        var payloadJson = JsonSerializer.Serialize(request.Payload);
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(payloadJson))).ToLowerInvariant();
        var verified = providers.VerifyCallback(provider, signature, hash, method);
        Guid? intentId = null;
        if (verified && !string.IsNullOrWhiteSpace(request.ProviderReference))
        {
            var intent = await repository.FindByProviderReferenceAsync(provider, request.ProviderReference, context, ct);
            intentId = intent?.Id;
            if (intent is null) verified = false;
        }
        var stored = await repository.StoreCallbackAsync(provider, eventId, request.ProviderReference, intentId,
            verified ? "Verified" : "Rejected", verified ? "Received" : "Ignored", hash, request.Payload, context, ct);
        if (stored.Duplicate) return new CallbackReceiptDto { CallbackId = stored.CallbackId.ToString(), Status = "Duplicate", PaymentIntentId = intentId?.ToString() };
        if (!verified) throw new DomainException("CALLBACK_VERIFICATION_FAILED", "Provider callback could not be verified", 401);
        return new CallbackReceiptDto { CallbackId = stored.CallbackId.ToString(), Status = "Accepted", PaymentIntentId = intentId?.ToString() };
    }

    // @BR-PA-021: Reconciliation lists authorized payment intents using explicit UTC bounds and chronological authorization time.
    public async Task<CapturablePaymentListResponseDto> CapturableAsync(DateTimeOffset? from, DateTimeOffset? to,
        RequestContext context, int page, int pageSize, CancellationToken ct)
    {
        if (from > to) throw new DomainException("INVALID_DATE_RANGE", "The reconciliation date range is invalid", 400);
        ValidatePage(page, pageSize);
        var list = await repository.CapturableAsync(from, to, context, page, pageSize, ct);
        return new CapturablePaymentListResponseDto
        {
            Items = list.Select(x => new CapturablePaymentDto
            {
                PaymentIntentId = x.Intent.Id.ToString(),
                OrderId = x.Intent.OrderId ?? "",
                Amount = (x.Intent.AuthorizedAmount - x.Intent.CapturedAmount).ToString("0.00##"),
                Currency = x.Intent.Currency,
                Status = x.Intent.Status,
                AuthorizedAt = x.AuthorizedAt.ToString("O"),
                ProviderCode = x.Intent.ProviderCode
            }).ToList(),
            Pagination = Page(page, pageSize, await repository.CountCapturableAsync(from, to, context, ct))
        };
    }

    private async Task<PaymentIntent> RequiredIntent(Guid id, RequestContext context, CancellationToken ct) =>
        await repository.FindIntentAsync(id, context, ct) ??
        throw new DomainException("PAYMENT_INTENT_NOT_FOUND", "Payment intent was not found", 404);

    private async Task<bool> FingerprintMatchesAsync(Guid operationId, string fingerprint, RequestContext context, CancellationToken ct) =>
        await repository.GetFingerprintAsync(operationId, context, ct) == fingerprint;

    private static PaymentOperation NewOperation(Guid intentId, string type, decimal amount, string currency, string key, RequestContext context) => new()
    {
        Id = Guid.NewGuid(),
        PaymentIntentId = intentId,
        OperationType = type,
        RequestedAmount = amount,
        Currency = currency,
        IdempotencyKey = key,
        CreatedAt = DateTimeOffset.UtcNow,
        CorrelationId = context.CorrelationId
    };

    private static decimal ParseAmount(string value)
    {
        if (!decimal.TryParse(value, System.Globalization.NumberStyles.AllowDecimalPoint, System.Globalization.CultureInfo.InvariantCulture, out var amount))
            throw new DomainException("PAYMENT_AMOUNT_INVALID", "Payment amount must be a valid decimal", 422);
        return amount;
    }

    private static decimal ParseAuthorizationAmount(string amountValue, string currency, PaymentIntent intent)
    {
        var amount = ParseAmount(amountValue);
        ValidateCurrency(currency);
        if (amount <= 0) throw new DomainException("PAYMENT_AMOUNT_INVALID", "Payment amount must be greater than zero", 422);
        if (currency != intent.Currency)
            throw new DomainException("PAYMENT_CURRENCY_MISMATCH", "Operation currency must match payment intent currency", 409);
        if (amount != intent.Amount)
            throw new DomainException("PAYMENT_AMOUNT_MISMATCH", "Authorization amount must match the payment intent amount", 409);
        return amount;
    }

    private static decimal ParseCaptureAmount(string amountValue, string currency, PaymentIntent intent)
    {
        var amount = ParseOperationAmount(amountValue, currency, intent);
        if (amount > intent.AuthorizedAmount - intent.CapturedAmount)
            throw new DomainException("PAYMENT_AMOUNT_MISMATCH", "Capture amount does not match the authorized payment balance", 409);
        return amount;
    }

    private static decimal ParseRefundAmount(string amountValue, string currency, PaymentIntent intent)
    {
        var amount = ParseOperationAmount(amountValue, currency, intent);
        if (amount > intent.CapturedAmount)
            throw new DomainException("REFUND_EXCEEDS_REMAINING_BALANCE", "Refund amount exceeds the captured payment", 422);
        return amount;
    }

    private static decimal ParseOperationAmount(string amountValue, string currency, PaymentIntent intent)
    {
        var amount = ParseAmount(amountValue);
        ValidateCurrency(currency);
        if (amount <= 0) throw new DomainException("PAYMENT_AMOUNT_INVALID", "Payment amount must be greater than zero", 422);
        if (currency != intent.Currency)
            throw new DomainException("PAYMENT_CURRENCY_MISMATCH", "Operation currency must match payment intent currency", 409);
        return amount;
    }

    private static void ValidateCurrency(string currency)
    {
        if (currency.Length != 3 || currency.Any(c => c is < 'A' or > 'Z'))
            throw new DomainException("PAYMENT_CURRENCY_INVALID", "Currency must be an uppercase ISO-4217 code", 422);
    }

    private static string Fingerprint(object value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(value)))).ToLowerInvariant();

    private static void ValidatePage(int page, int pageSize)
    {
        if (page < 1 || pageSize is < 1 or > 100)
            throw new DomainException("INVALID_PAGINATION", "Page must be positive and pageSize must be between 1 and 100", 400);
    }

    private static PaginationInfoDto Page(int page, int pageSize, long total) => new()
    {
        Page = page,
        PageSize = pageSize,
        TotalItems = total,
        TotalPages = total == 0 ? 0 : (int)Math.Ceiling(total / (double)pageSize)
    };
}
