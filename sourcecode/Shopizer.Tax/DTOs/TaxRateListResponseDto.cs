using System.Text.Json.Serialization;

namespace Shopizer.Tax.DTOs;

public sealed class TaxRateListResponseDto
{
    [JsonPropertyName("items")]
    public List<TaxRateDto> Items { get; set; } = new();

    [JsonPropertyName("pagination")]
    public PaginationInfoDto Pagination { get; set; }
}
