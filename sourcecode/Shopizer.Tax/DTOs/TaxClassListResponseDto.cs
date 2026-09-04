using System.Text.Json.Serialization;

namespace Shopizer.Tax.DTOs;

public sealed class TaxClassListResponseDto
{
    [JsonPropertyName("items")]
    public List<TaxClassDto> Items { get; set; } = new();

    [JsonPropertyName("pagination")]
    public PaginationInfoDto Pagination { get; set; }
}
