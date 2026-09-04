using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.Tax.DTOs;

public sealed class DeleteResponseDto
{
    [JsonPropertyName("deleted")]
    public bool Deleted { get; set; }

    [JsonPropertyName("id")]
    [Required]
    public string Id { get; set; }
}
