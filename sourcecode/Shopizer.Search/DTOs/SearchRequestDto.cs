using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.Search.DTOs;

public sealed class SearchRequestDto
{
    [JsonPropertyName("query")]
    [Required]
    [StringLength(500, MinimumLength = 1)]
    public string Query { get; set; }

    [JsonPropertyName("count")]
    public int? Count { get; set; }

    [JsonPropertyName("start")]
    public int? Start { get; set; }
}
