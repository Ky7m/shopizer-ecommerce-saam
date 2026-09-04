using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.Search.DTOs;

public sealed class AutocompleteRequestDto
{
    [JsonPropertyName("query")]
    [Required]
    [StringLength(500, MinimumLength = 1)]
    public string Query { get; set; }
}
