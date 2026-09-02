using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.Services.Ms12.Contracts;

public sealed class PackageDto
{
        [JsonPropertyName("weight")]
        public decimal Weight { get; set; }

        [JsonPropertyName("weightUnit")]
        [Required]
        public string WeightUnit { get; set; }

        [JsonPropertyName("length")]
        public decimal Length { get; set; }

        [JsonPropertyName("width")]
        public decimal Width { get; set; }

        [JsonPropertyName("height")]
        public decimal Height { get; set; }

        [JsonPropertyName("dimensionUnit")]
        [Required]
        public string DimensionUnit { get; set; }
}
