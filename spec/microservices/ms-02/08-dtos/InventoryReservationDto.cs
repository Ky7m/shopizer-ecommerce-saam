using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.Services.Ms02.Contracts;

public sealed class InventoryReservationDto
{
        [JsonPropertyName("id")]
        [Required]
        public string Id { get; set; }

        [JsonPropertyName("productId")]
        [Required]
        public string ProductId { get; set; }

        [JsonPropertyName("variantId")]
        public string? VariantId { get; set; }

        [JsonPropertyName("availabilityId")]
        public string? AvailabilityId { get; set; }

        [JsonPropertyName("reservationKey")]
        public string? ReservationKey { get; set; }

        [JsonPropertyName("quantity")]
        [Range(1, double.MaxValue)]
        public int Quantity { get; set; }

        [JsonPropertyName("state")]
        [Required]
        public string State { get; set; }

        [JsonPropertyName("expiresAt")]
        [Required]
        public string ExpiresAt { get; set; }

        [JsonPropertyName("committedAt")]
        public string? CommittedAt { get; set; }

        [JsonPropertyName("releasedAt")]
        public string? ReleasedAt { get; set; }
}
