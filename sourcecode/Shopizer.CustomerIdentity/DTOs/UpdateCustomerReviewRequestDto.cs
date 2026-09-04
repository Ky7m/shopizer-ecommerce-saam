using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.CustomerIdentity.DTOs;

public sealed class UpdateCustomerReviewRequestDto
{
    [JsonPropertyName("rating")]
    [Range(1, 5)]
    public decimal Rating { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }
}
