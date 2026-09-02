using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.Services.Ms12.Contracts;

public sealed class DeliveryAttemptDto
{
        [JsonPropertyName("attemptId")]
        [Required]
        public string AttemptId { get; set; }

        [JsonPropertyName("operationId")]
        [Required]
        public string OperationId { get; set; }

        [JsonPropertyName("endpointId")]
        [Required]
        public string EndpointId { get; set; }

        [JsonPropertyName("messageId")]
        public string? MessageId { get; set; }

        [JsonPropertyName("operationItemKey")]
        [Required]
        public string OperationItemKey { get; set; }

        [JsonPropertyName("attemptNumber")]
        public int AttemptNumber { get; set; }

        [JsonPropertyName("status")]
        public DeliveryAttemptStatusDto Status { get; set; }

        [JsonPropertyName("providerRequestRef")]
        public string? ProviderRequestRef { get; set; }

        [JsonPropertyName("providerOutcomeCode")]
        public string? ProviderOutcomeCode { get; set; }

        [JsonPropertyName("providerErrorCode")]
        public string? ProviderErrorCode { get; set; }

        [JsonPropertyName("providerErrorMessage")]
        public string? ProviderErrorMessage { get; set; }

        [JsonPropertyName("nextAttemptAt")]
        public string? NextAttemptAt { get; set; }

        [JsonPropertyName("attemptedAt")]
        public string? AttemptedAt { get; set; }

        [JsonPropertyName("completedAt")]
        public string? CompletedAt { get; set; }

        [JsonPropertyName("replayOfAttemptId")]
        public string? ReplayOfAttemptId { get; set; }

        [JsonPropertyName("outboxEventId")]
        public string? OutboxEventId { get; set; }

        [JsonPropertyName("deadLetteredAt")]
        public string? DeadLetteredAt { get; set; }
}
