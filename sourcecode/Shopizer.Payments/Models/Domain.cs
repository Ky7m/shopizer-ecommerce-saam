using System.Security.Claims;
using Shopizer.Payments.DTOs;

namespace Shopizer.Payments.Models;

public sealed record RequestContext(string TenantId, string StoreId, string CorrelationId)
{
    public static RequestContext From(HttpContext http)
    {
        var tenant = http.Request.Headers["x-tenant-id"].FirstOrDefault();
        var store = http.Request.Headers["x-store-id"].FirstOrDefault();
        var correlation = http.Request.Headers["x-correlation-id"].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(tenant) || string.IsNullOrWhiteSpace(store))
            throw new DomainException("REQUEST_CONTEXT_REQUIRED", "x-tenant-id and x-store-id are required", 400);

        if (tenant.Length > 100 || store.Length > 100)
            throw new DomainException("REQUEST_CONTEXT_INVALID", "Tenant and store identifiers are too long", 400);

        return new RequestContext(tenant, store,
            string.IsNullOrWhiteSpace(correlation) ? Guid.NewGuid().ToString("D") : correlation);
    }
}

public sealed class DomainException(string code, string message, int statusCode) : Exception(message)
{
    public string Code { get; } = code;
    public int StatusCode { get; } = statusCode;
}

public sealed class PaymentIntent
{
    public Guid Id { get; init; }
    public string TenantId { get; init; } = "";
    public string StoreId { get; init; } = "";
    public string CheckoutSessionId { get; init; } = "";
    public string? OrderId { get; set; }
    public string ProviderCode { get; init; } = "";
    public long ProviderConfigVersion { get; init; }
    public decimal Amount { get; init; }
    public string Currency { get; init; } = "";
    public string Status { get; set; } = "Created";
    public decimal AuthorizedAmount { get; set; }
    public decimal CapturedAmount { get; set; }
    public decimal RefundableAmount { get; set; }
    public string? ClientSecretReference { get; set; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; set; }
    public string? CreatedBy { get; init; }
    public string? CorrelationId { get; init; }
}

public sealed class PaymentMethodConfiguration
{
    public string Code { get; init; } = "";
    public string ProviderCode { get; init; } = "";
    public bool Eligible { get; init; }
    public bool Active { get; set; }
    public bool DefaultSelected { get; set; }
    public bool Configurable { get; init; }
    public string Environment { get; set; } = "Test";
    public long ConfigurationVersion { get; set; }
    public string SecretReference { get; set; } = "";
    public Dictionary<string, object?> PublicConfiguration { get; set; } = [];
    public string[] Regions { get; init; } = ["*"];
}

public sealed class PaymentOperation
{
    public Guid Id { get; init; }
    public Guid PaymentIntentId { get; init; }
    public string OperationType { get; init; } = "";
    public string Status { get; set; } = "Requested";
    public decimal RequestedAmount { get; init; }
    public string Currency { get; init; } = "";
    public string IdempotencyKey { get; init; } = "";
    public Guid? ProviderAttemptId { get; set; }
    public string? ProviderReference { get; set; }
    public string? FailureCode { get; set; }
    public string? FailureMessage { get; set; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? CompletedAt { get; set; }
    public string? CorrelationId { get; init; }
}

public sealed class PaymentTransaction
{
    public Guid Id { get; init; }
    public Guid PaymentIntentId { get; init; }
    public Guid? PaymentOperationId { get; init; }
    public string OperationType { get; init; } = "";
    public string Status { get; init; } = "";
    public decimal Amount { get; init; }
    public string Currency { get; init; } = "";
    public string ProviderCode { get; init; } = "";
    public string? ProviderReference { get; init; }
    public string? ProviderStatus { get; init; }
    public string? ProviderCorrelationId { get; init; }
    public Dictionary<string, object?> ProviderDetails { get; init; } = [];
    public DateTimeOffset OccurredAt { get; init; }
    public long SequenceNo { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public string? CorrelationId { get; init; }
}

public sealed class PaymentRefund
{
    public Guid Id { get; init; }
    public Guid PaymentIntentId { get; init; }
    public Guid PaymentOperationId { get; init; }
    public decimal Amount { get; init; }
    public string Currency { get; init; } = "";
    public string Status { get; set; } = "Reserved";
    public string? ProviderReference { get; set; }
    public DateTimeOffset RequestedAt { get; init; }
    public DateTimeOffset? CompletedAt { get; set; }
    public string? CorrelationId { get; init; }
}

public sealed record ProviderResult(
    bool Succeeded,
    string Status,
    string? Reference,
    string? FailureCode = null,
    string? FailureMessage = null,
    string? ClientSecret = null,
    string? ProviderStatus = null);

public sealed record IdempotencyResult(bool IsReplay, PaymentOperation? Operation, string? ResponseSnapshot);

public sealed record AuthenticatedIdentity(Guid Id, string Kind, string Login, string TenantId, string StoreId, IReadOnlyList<string> Roles);

public static class PrincipalExtensions
{
    public static Guid? SubjectId(this ClaimsPrincipal principal) =>
        Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? principal.FindFirstValue("sub"), out var id)
            ? id : null;

    public static string Kind(this ClaimsPrincipal principal) => principal.FindFirstValue("kind") ?? "";

    public static bool HasRole(this ClaimsPrincipal principal, params string[] roles) =>
        roles.Length == 0 || roles.Any(role => principal.IsInRole(role) ||
            principal.Claims.Any(c => (c.Type == ClaimTypes.Role || c.Type == "role") &&
                                      c.Value.Equals(role, StringComparison.OrdinalIgnoreCase)));
}

public static class DtoMapper
{
    public static PaymentMethodDto Method(PaymentMethodConfiguration method) => new()
    {
        Code = method.Code,
        ProviderCode = method.ProviderCode,
        Eligible = method.Eligible,
        Active = method.Active,
        DefaultSelected = method.DefaultSelected,
        Configurable = method.Configurable,
        Environment = method.Environment,
        ConfigurationVersion = method.ConfigurationVersion,
        PublicConfiguration = method.PublicConfiguration
    };

    public static PaymentIntentDto Intent(PaymentIntent intent) => new()
    {
        PaymentIntentId = intent.Id.ToString(),
        TenantId = intent.TenantId,
        StoreId = intent.StoreId,
        CheckoutSessionId = intent.CheckoutSessionId,
        OrderId = intent.OrderId,
        ProviderCode = intent.ProviderCode,
        ProviderConfigVersion = intent.ProviderConfigVersion,
        Amount = intent.Amount.ToString("0.00##"),
        Currency = intent.Currency,
        Status = intent.Status,
        AuthorizedAmount = intent.AuthorizedAmount.ToString("0.00##"),
        CapturedAmount = intent.CapturedAmount.ToString("0.00##"),
        RefundableAmount = intent.RefundableAmount.ToString("0.00##"),
        ClientSecretReference = intent.ClientSecretReference,
        CreatedAt = intent.CreatedAt.ToString("O"),
        UpdatedAt = intent.UpdatedAt.ToString("O"),
        CreatedBy = intent.CreatedBy,
        CorrelationId = intent.CorrelationId
    };

    public static PaymentOperationDto Operation(PaymentOperation operation) => new()
    {
        PaymentOperationId = operation.Id.ToString(),
        PaymentIntentId = operation.PaymentIntentId.ToString(),
        OperationType = operation.OperationType,
        Status = operation.Status,
        RequestedAmount = operation.RequestedAmount.ToString("0.00##"),
        Currency = operation.Currency,
        IdempotencyKey = operation.IdempotencyKey,
        ProviderAttemptId = operation.ProviderAttemptId?.ToString(),
        ProviderReference = operation.ProviderReference,
        FailureCode = operation.FailureCode,
        FailureMessage = operation.FailureMessage,
        CreatedAt = operation.CreatedAt.ToString("O"),
        CompletedAt = operation.CompletedAt?.ToString("O"),
        CorrelationId = operation.CorrelationId
    };

    public static PaymentTransactionDto Transaction(PaymentTransaction transaction) => new()
    {
        PaymentTransactionId = transaction.Id.ToString(),
        PaymentIntentId = transaction.PaymentIntentId.ToString(),
        PaymentOperationId = transaction.PaymentOperationId?.ToString(),
        OperationType = transaction.OperationType,
        Status = transaction.Status,
        Amount = transaction.Amount.ToString("0.00##"),
        Currency = transaction.Currency,
        ProviderCode = transaction.ProviderCode,
        ProviderReference = transaction.ProviderReference,
        ProviderStatus = transaction.ProviderStatus,
        ProviderCorrelationId = transaction.ProviderCorrelationId,
        ProviderDetails = transaction.ProviderDetails,
        OccurredAt = transaction.OccurredAt.ToString("O"),
        SequenceNo = transaction.SequenceNo,
        CreatedAt = transaction.CreatedAt.ToString("O"),
        CorrelationId = transaction.CorrelationId
    };

    public static RefundDto Refund(PaymentRefund refund) => new()
    {
        RefundId = refund.Id.ToString(),
        PaymentIntentId = refund.PaymentIntentId.ToString(),
        Amount = refund.Amount.ToString("0.00##"),
        Currency = refund.Currency,
        Status = refund.Status,
        ProviderReference = refund.ProviderReference,
        RequestedAt = refund.RequestedAt.ToString("O"),
        CompletedAt = refund.CompletedAt?.ToString("O"),
        CorrelationId = refund.CorrelationId
    };
}
