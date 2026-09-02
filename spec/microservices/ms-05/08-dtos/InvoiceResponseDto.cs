using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.Services.Ms05.Contracts;

public sealed class InvoiceResponseDto
{
        [JsonPropertyName("orderId")]
        public long OrderId { get; set; }

        [JsonPropertyName("requestId")]
        public string? RequestId { get; set; }

        [JsonPropertyName("status")]
        [Required]
        public string Status { get; set; }

        [JsonPropertyName("artifactUrl")]
        public string? ArtifactUrl { get; set; }

        [JsonPropertyName("generatedAt")]
        public string? GeneratedAt { get; set; }
}
