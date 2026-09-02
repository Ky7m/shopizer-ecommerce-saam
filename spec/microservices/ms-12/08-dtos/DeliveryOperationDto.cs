using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.Services.Ms12.Contracts;

public sealed class DeliveryOperationDto
{
        [JsonPropertyName("operationId")]
        [Required]
        public string OperationId { get; set; }

        [JsonPropertyName("operationType")]
        [Required]
        public string OperationType { get; set; }

        [JsonPropertyName("idempotencyKey")]
        [Required]
        public string IdempotencyKey { get; set; }

        [JsonPropertyName("requestHash")]
        [Required]
        public string RequestHash { get; set; }

        [JsonPropertyName("itemCount")]
        public int ItemCount { get; set; }

        [JsonPropertyName("status")]
        public DeliveryOperationStatusDto Status { get; set; }

        [JsonPropertyName("createdAt")]
        [Required]
        public string CreatedAt { get; set; }

        [JsonPropertyName("updatedAt")]
        [Required]
        public string UpdatedAt { get; set; }

        [JsonPropertyName("completedAt")]
        public string? CompletedAt { get; set; }
}
