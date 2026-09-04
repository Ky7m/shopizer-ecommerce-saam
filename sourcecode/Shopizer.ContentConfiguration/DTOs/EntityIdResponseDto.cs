using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.ContentConfiguration.DTOs;

public sealed class EntityIdResponseDto
{
    [JsonPropertyName("id")]
    [Required]
    public string Id { get; set; }
}
