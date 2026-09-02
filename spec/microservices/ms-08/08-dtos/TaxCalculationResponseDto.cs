using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.Services.Ms08.Contracts;

public sealed class TaxCalculationResponseDto
{
        [JsonPropertyName("quoteId")]
        [Required]
        public string QuoteId { get; set; }

        [JsonPropertyName("currencyCode")]
        [Required]
        public string CurrencyCode { get; set; }

        [JsonPropertyName("jurisdiction")]
        public JurisdictionDto Jurisdiction { get; set; }

        [JsonPropertyName("taxableAmount")]
        public decimal TaxableAmount { get; set; }

        [JsonPropertyName("totalTaxAmount")]
        public decimal TotalTaxAmount { get; set; }

        [JsonPropertyName("taxItems")]
        public List<TaxItemDto> TaxItems { get; set; } = new();
}
