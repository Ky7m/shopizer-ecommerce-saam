using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.Services.Ms07.Contracts;

public sealed class ProductQuoteRequestDto
{
        [JsonPropertyName("customerId")]
        [StringLength(160, MinimumLength = 1)]
        public string? CustomerId { get; set; }

        [JsonPropertyName("attributes")]
        public List<PricingAttributeDto> Attributes { get; set; } = new();

        [JsonPropertyName("evaluationAt")]
        public string? EvaluationAt { get; set; }
}
