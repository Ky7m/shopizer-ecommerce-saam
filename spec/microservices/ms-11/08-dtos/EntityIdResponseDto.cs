using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.Services.Ms11.Contracts;

public sealed class EntityIdResponseDto
{
        [JsonPropertyName("id")]
        [Required]
        public string Id { get; set; }
}
