using System.Collections.Concurrent;
using System.Security.Claims;
using System.Text.Json;
using Shopizer.PlatformIntegrations.DTOs;

namespace Shopizer.PlatformIntegrations.Models;

public sealed record RequestContext(string TenantId, string StoreId, string CorrelationId)
{
    public static RequestContext From(HttpContext http)
    {
        var tenant = http.Request.Headers["x-tenant-id"].FirstOrDefault();
        var store = http.Request.Headers["x-store-id"].FirstOrDefault();
        var correlation = http.Request.Headers["x-correlation-id"].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(tenant) || string.IsNullOrWhiteSpace(store) ||
            string.IsNullOrWhiteSpace(correlation))
            throw new DomainException("REQUEST_CONTEXT_REQUIRED",
                "x-tenant-id, x-store-id and x-correlation-id are required", 400);
        return new RequestContext(tenant.Trim(), store.Trim(), correlation.Trim());
    }
}

public sealed class DomainException(string code, string message, int statusCode) : Exception(message)
{
    public string Code { get; } = code;
    public int StatusCode { get; } = statusCode;
}

public sealed class IntegrationEndpoint
{
    public Guid Id { get; init; }
    public string TenantId { get; init; } = "";
    public string StoreId { get; init; } = "";
    public string IntegrationType { get; init; } = "";
    public string Provider { get; init; } = "";
    public string Code { get; init; } = "";
    public string Environment { get; init; } = "";
    public string Status { get; set; } = "ACTIVE";
    public string ConfigurationRef { get; init; } = "";
    public string? EndpointUri { get; init; }
    public Dictionary<string, object?> Capabilities { get; init; } = new();
    public Dictionary<string, object?> SupplementalConfiguration { get; init; } = new();
    public int TimeoutMs { get; init; } = 10_000;
    public int MaxAttempts { get; init; } = 3;
}

public sealed class DeliveryAttempt
{
    public Guid AttemptId { get; init; }
    public Guid OperationId { get; init; }
    public Guid EndpointId { get; init; }
    public Guid? MessageId { get; init; }
    public string TenantId { get; init; } = "";
    public string StoreId { get; init; } = "";
    public string OperationItemKey { get; init; } = "";
    public int AttemptNumber { get; init; }
    public string Status { get; set; } = "PENDING";
    public string? ProviderRequestRef { get; set; }
    public string? ProviderOutcomeCode { get; set; }
    public string? ProviderErrorCode { get; set; }
    public string? ProviderErrorMessage { get; set; }
    public DateTimeOffset? NextAttemptAt { get; set; }
    public DateTimeOffset? AttemptedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public Guid? ReplayOfAttemptId { get; init; }
    public Guid? OutboxEventId { get; set; }
    public DateTimeOffset? DeadLetteredAt { get; set; }
}

public sealed class EmailMessage
{
    public Guid MessageId { get; init; }
    public Guid OperationId { get; set; }
    public Guid EndpointId { get; init; }
    public string IdempotencyKey { get; init; } = "";
    public string TemplateKey { get; init; } = "";
    public string Locale { get; init; } = "";
    public string RecipientEmail { get; init; } = "";
    public string SenderEmail { get; init; } = "";
    public string? SenderName { get; init; }
    public string Subject { get; init; } = "";
    public Dictionary<string, object?> TokenPayload { get; init; } = new();
    public string Status { get; set; } = "QUEUED";
    public string? OrderReference { get; init; }
    public DateTimeOffset QueuedAt { get; init; }
    public DateTimeOffset? SentAt { get; set; }
}

public sealed class DeliveryOperation
{
    public Guid OperationId { get; init; }
    public string OperationType { get; init; } = "";
    public string IdempotencyKey { get; init; } = "";
    public string RequestHash { get; init; } = "";
    public int ItemCount { get; init; }
    public string Status { get; set; } = "RECEIVED";
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
}

public sealed record ProviderFile(string FileName, string ContentType, string MimeType, string ProviderKey,
    byte[] Content);

public static class PrincipalExtensions
{
    public static string Kind(this ClaimsPrincipal principal) =>
        principal.FindFirstValue("kind") ?? "";

    public static Guid? SubjectId(this ClaimsPrincipal principal) =>
        Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier) ??
                      principal.FindFirstValue("sub"), out var id) ? id : null;

    public static bool HasRole(this ClaimsPrincipal principal, params string[] roles) =>
        roles.Any(role => principal.IsInRole(role) ||
            principal.Claims.Any(c => (c.Type == ClaimTypes.Role || c.Type == "role") &&
                c.Value.Equals(role, StringComparison.OrdinalIgnoreCase)));
}

public static class MarkerValue
{
    private static readonly ConcurrentDictionary<object, string> Values = new();
    private static readonly ConcurrentDictionary<object, object> Payloads = new();

    public static T Create<T>(string value) where T : class, new()
    {
        var marker = new T();
        Values[marker] = value;
        return marker;
    }

    public static string Get(object? marker, string fallback = "") =>
        marker is not null && Values.TryGetValue(marker, out var value) ? value : fallback;

    public static T WithPayload<T>(T marker, object payload) where T : class
    {
        Payloads[marker] = payload;
        return marker;
    }

    public static object? GetPayload(object marker) => Payloads.TryGetValue(marker, out var payload) ? payload : null;
}

public sealed class MarkerJsonConverter<T> : System.Text.Json.Serialization.JsonConverter<T> where T : class, new()
{
    public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String)
            throw new JsonException($"Expected a string enum value for {typeof(T).Name}.");
        return MarkerValue.Create<T>(reader.GetString() ?? "");
    }

    public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options) =>
        writer.WriteStringValue(MarkerValue.Get(value));
}

public sealed class MarkerPayloadJsonConverter<T> : System.Text.Json.Serialization.JsonConverter<T> where T : class, new()
{
    public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var document = JsonDocument.ParseValue(ref reader);
        return MarkerValue.WithPayload(new T(), document.RootElement.Clone());
    }

    public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options) =>
        JsonSerializer.Serialize(writer, MarkerValue.GetPayload(value) ?? new { }, options);
}

public static class DtoMapper
{
    public static AdapterDto Adapter(IntegrationEndpoint endpoint) => new()
    {
        EndpointId = endpoint.Id.ToString(),
        IntegrationType = MarkerValue.Create<IntegrationTypeDto>(Pascal(endpoint.IntegrationType)),
        Provider = endpoint.Provider,
        Code = endpoint.Code,
        Environment = endpoint.Environment,
        Status = MarkerValue.Create<AdapterStatusDto>(Pascal(endpoint.Status)),
        ConfigurationRef = endpoint.ConfigurationRef,
        EndpointUri = endpoint.EndpointUri,
        Capabilities = endpoint.Capabilities,
        SupplementalConfiguration = endpoint.SupplementalConfiguration,
        TimeoutMs = endpoint.TimeoutMs,
        MaxAttempts = endpoint.MaxAttempts
    };

    public static DeliveryAttemptDto Attempt(DeliveryAttempt a) => new()
    {
        AttemptId = a.AttemptId.ToString(),
        OperationId = a.OperationId.ToString(),
        EndpointId = a.EndpointId.ToString(),
        MessageId = a.MessageId?.ToString(),
        OperationItemKey = a.OperationItemKey,
        AttemptNumber = a.AttemptNumber,
        Status = MarkerValue.Create<DeliveryAttemptStatusDto>(Pascal(a.Status)),
        ProviderRequestRef = a.ProviderRequestRef,
        ProviderOutcomeCode = a.ProviderOutcomeCode,
        ProviderErrorCode = a.ProviderErrorCode,
        ProviderErrorMessage = a.ProviderErrorMessage,
        NextAttemptAt = a.NextAttemptAt?.ToString("O"),
        AttemptedAt = a.AttemptedAt?.ToString("O"),
        CompletedAt = a.CompletedAt?.ToString("O"),
        ReplayOfAttemptId = a.ReplayOfAttemptId?.ToString(),
        OutboxEventId = a.OutboxEventId?.ToString(),
        DeadLetteredAt = a.DeadLetteredAt?.ToString("O")
    };

    public static EmailMessageDto Email(EmailMessage message) => new()
    {
        MessageId = message.MessageId.ToString(),
        OperationId = message.OperationId.ToString(),
        EndpointId = message.EndpointId.ToString(),
        IdempotencyKey = message.IdempotencyKey,
        TemplateKey = message.TemplateKey,
        Locale = message.Locale,
        RecipientEmail = message.RecipientEmail,
        SenderEmail = message.SenderEmail,
        SenderName = message.SenderName,
        Subject = message.Subject,
        Status = MarkerValue.Create<EmailMessageStatusDto>(Pascal(message.Status)),
        OrderReference = message.OrderReference,
        QueuedAt = message.QueuedAt.ToString("O"),
        SentAt = message.SentAt?.ToString("O")
    };

    public static FileAssetDto File(ProviderFile file, Guid? operationId = null, Guid? attemptId = null) => new()
    {
        OperationId = operationId?.ToString(),
        FileName = file.FileName,
        ContentType = MarkerValue.Create<ContentTypeDto>(file.ContentType),
        MimeType = file.MimeType,
        ProviderKey = file.ProviderKey,
        Status = MarkerValue.Create<FileStatusDto>("Available"),
        DeliveryAttemptId = attemptId?.ToString()
    };

    public static string Pascal(string value) =>
        value.Length == 0 ? value : char.ToUpperInvariant(value[0]) + value[1..].ToLowerInvariant();
}
