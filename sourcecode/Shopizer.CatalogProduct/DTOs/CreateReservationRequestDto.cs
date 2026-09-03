using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.Services.Ms02.Contracts;

public sealed class CreateReservationRequestDto
{
        [JsonPropertyName("reservationKey")]
        [Required]
        [StringLength(200, MinimumLength = 1)]
        public string ReservationKey { get; set; }

        [JsonPropertyName("variantId")]
        public string? VariantId { get; set; }

        [JsonPropertyName("availabilityId")]
        public string? AvailabilityId { get; set; }

        [JsonPropertyName("regionCode")]
        public string? RegionCode { get; set; }

        [JsonPropertyName("quantity")]
        [Range(1, double.MaxValue)]
        public int Quantity { get; set; }

        [JsonPropertyName("expiresAt")]
        [Required]
        public string ExpiresAt { get; set; }
}
