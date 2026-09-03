using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.Services.Ms02.Contracts;

public sealed class AvailabilityDto
{
        [JsonPropertyName("id")]
        [Required]
        public string Id { get; set; }

        [JsonPropertyName("regionCode")]
        [Required]
        [MinLength(1)]
        public string RegionCode { get; set; }

        [JsonPropertyName("quantity")]
        [Range(0, double.MaxValue)]
        public int Quantity { get; set; }

        [JsonPropertyName("reservedQuantity")]
        [Range(0, double.MaxValue)]
        public int ReservedQuantity { get; set; }

        [JsonPropertyName("sellableQuantity")]
        public int? SellableQuantity { get; set; }

        [JsonPropertyName("active")]
        public bool Active { get; set; }
}
