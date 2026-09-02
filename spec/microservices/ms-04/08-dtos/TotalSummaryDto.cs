using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.Services.Ms04.Contracts;

public sealed class TotalSummaryDto
{
        [JsonPropertyName("cartCode")]
        [Required]
        public string CartCode { get; set; }

        [JsonPropertyName("currency")]
        [Required]
        [RegularExpression(@"^[A-Z]{3}$")]
        public string Currency { get; set; }

        [JsonPropertyName("subTotal")]
        [Required]
        public string SubTotal { get; set; }

        [JsonPropertyName("discountTotal")]
        [Required]
        public string DiscountTotal { get; set; }

        [JsonPropertyName("shipping")]
        [Required]
        public string Shipping { get; set; }

        [JsonPropertyName("handling")]
        [Required]
        public string Handling { get; set; }

        [JsonPropertyName("tax")]
        [Required]
        public string Tax { get; set; }

        [JsonPropertyName("grandTotal")]
        [Required]
        public string GrandTotal { get; set; }

        [JsonPropertyName("quoteVersion")]
        public long? QuoteVersion { get; set; }

        [JsonPropertyName("components")]
        public List<TotalComponentDto> Components { get; set; } = new();
}
