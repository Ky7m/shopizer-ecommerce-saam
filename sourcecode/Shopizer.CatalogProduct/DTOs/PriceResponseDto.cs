using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.Services.Ms02.Contracts;

public sealed class PriceResponseDto
{
        [JsonPropertyName("finalAmount")]
        [Range(0, double.MaxValue)]
        public decimal FinalAmount { get; set; }

        [JsonPropertyName("originalAmount")]
        public decimal? OriginalAmount { get; set; }

        [JsonPropertyName("currencyCode")]
        [Required]
        public string CurrencyCode { get; set; }

        [JsonPropertyName("priceSource")]
        [Required]
        public string PriceSource { get; set; }

        [JsonPropertyName("discounted")]
        public bool? Discounted { get; set; }

        [JsonPropertyName("matchedSelections")]
        [Range(0, double.MaxValue)]
        public int MatchedSelections { get; set; }
}
