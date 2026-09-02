using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.Services.Ms08.Contracts;

public sealed class TaxCalculationItemDto
{
        [JsonPropertyName("productId")]
        [Required]
        public string ProductId { get; set; }

        [JsonPropertyName("quantity")]
        [Range(1, double.MaxValue)]
        public int Quantity { get; set; }

        [JsonPropertyName("unitAmount")]
        [Range(0, double.MaxValue)]
        public decimal UnitAmount { get; set; }

        [JsonPropertyName("taxClassCode")]
        [Required]
        [StringLength(10, MinimumLength = 1)]
        public string TaxClassCode { get; set; }
}
