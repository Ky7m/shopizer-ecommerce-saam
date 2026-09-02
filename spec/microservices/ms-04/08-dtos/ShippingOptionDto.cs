using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.Services.Ms04.Contracts;

public sealed class ShippingOptionDto
{
        [JsonPropertyName("code")]
        [Required]
        public string Code { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("price")]
        [Required]
        public string Price { get; set; }

        [JsonPropertyName("currency")]
        [Required]
        [RegularExpression(@"^[A-Z]{3}$")]
        public string Currency { get; set; }
}
