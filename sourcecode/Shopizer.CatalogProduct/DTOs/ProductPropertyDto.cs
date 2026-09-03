using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.Services.Ms02.Contracts;

public sealed class ProductPropertyDto
{
        [JsonPropertyName("code")]
        [Required]
        public string Code { get; set; }

        [JsonPropertyName("value")]
        [Required]
        public string Value { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }
}
