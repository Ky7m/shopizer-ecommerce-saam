using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.Services.Ms01.Contracts;

public sealed class UpdateCustomerReviewRequestDto
{
        [JsonPropertyName("rating")]
        [Range(1, 5)]
        public decimal Rating { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }
}
