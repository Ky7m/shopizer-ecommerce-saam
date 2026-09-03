using System.Text.Json.Serialization;

namespace Shopizer.Search.DTOs;

public sealed class SearchResultsResponseDto
{
        [JsonPropertyName("items")]
        public List<SearchResultItemDto> Items { get; set; } = new();

        [JsonPropertyName("pagination")]
        public PaginationInfoDto Pagination { get; set; }
}
