using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.CustomerIdentity.DTOs;

public sealed class CustomerReviewDto
{
        [JsonPropertyName("id")]
        [Required]
        public string Id { get; set; }

        [JsonPropertyName("reviewerCustomerId")]
        [Required]
        public string ReviewerCustomerId { get; set; }

        [JsonPropertyName("reviewedCustomerId")]
        [Required]
        public string ReviewedCustomerId { get; set; }

        [JsonPropertyName("rating")]
        [Range(1, 5)]
        public decimal Rating { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("reviewDate")]
        [Required]
        public string ReviewDate { get; set; }

        [JsonPropertyName("status")]
        [Required]
        public string Status { get; set; }
}
