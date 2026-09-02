using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.Services.Ms05.Contracts;

public sealed class RefundRequestDto
{
        [JsonPropertyName("amount")]
        public decimal Amount { get; set; }

        [JsonPropertyName("currency")]
        [Required]
        [StringLength(3, MinimumLength = 3)]
        public string Currency { get; set; }

        [JsonPropertyName("reason")]
        [Required]
        [MinLength(1)]
        public string Reason { get; set; }
}
