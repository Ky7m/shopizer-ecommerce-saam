using System.Text.Json.Serialization;

namespace Shopizer.MerchantAdministration.DTOs;

public sealed class LanguageListResponseDto
{
    [JsonPropertyName("items")]
    public List<string> Items { get; set; } = new();
}
