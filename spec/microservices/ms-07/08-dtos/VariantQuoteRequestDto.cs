using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.Services.Ms07.Contracts;

public sealed class VariantQuoteRequestDto
{
        [JsonPropertyName("parentProductSku")]
        [StringLength(160, MinimumLength = 1)]
        public string? ParentProductSku { get; set; }

        [JsonPropertyName("fallbackMode")]
        [Required]
        public string FallbackMode { get; set; }

        [JsonPropertyName("evaluationAt")]
        public string? EvaluationAt { get; set; }
}
