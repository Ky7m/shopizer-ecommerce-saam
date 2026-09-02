using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.Services.Ms07.Contracts;

public sealed class AdditionalPriceLineDto
{
        [JsonPropertyName("code")]
        [Required]
        [RegularExpression(@"^[A-Za-z0-9_]+$")]
        public string Code { get; set; }

        [JsonPropertyName("priceType")]
        [Required]
        public string PriceType { get; set; }

        [JsonPropertyName("finalPrice")]
        [Range(0, double.MaxValue)]
        public decimal FinalPrice { get; set; }
}
