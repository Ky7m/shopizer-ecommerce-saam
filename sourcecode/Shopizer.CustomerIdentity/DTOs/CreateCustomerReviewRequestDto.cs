using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.CustomerIdentity.DTOs;

public sealed class CreateCustomerReviewRequestDto
{
        [JsonPropertyName("customerId")]
        [Required]
        public string CustomerId { get; set; }

        [JsonPropertyName("rating")]
        [Range(1, 5)]
        public decimal Rating { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }
}
