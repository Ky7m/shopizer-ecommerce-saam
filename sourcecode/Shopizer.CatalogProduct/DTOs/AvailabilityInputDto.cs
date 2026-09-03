using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.Services.Ms02.Contracts;

public sealed class AvailabilityInputDto
{
        [JsonPropertyName("regionCode")]
        [Required]
        [MinLength(1)]
        public string RegionCode { get; set; }

        [JsonPropertyName("quantity")]
        [Range(0, double.MaxValue)]
        public int Quantity { get; set; }

        [JsonPropertyName("active")]
        public bool? Active { get; set; }
}
