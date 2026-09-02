using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.Services.Ms05.Contracts;

public sealed class OrderAttributeDto
{
        [JsonPropertyName("identifier")]
        [Required]
        public string Identifier { get; set; }

        [JsonPropertyName("value")]
        [Required]
        public string Value { get; set; }
}
