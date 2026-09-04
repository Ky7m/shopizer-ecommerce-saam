using System.Text.Json.Serialization;

namespace Shopizer.Search.DTOs;

public sealed class AutocompleteResponseDto
{
    [JsonPropertyName("suggestions")]
    public List<string> Suggestions { get; set; } = new();
}
