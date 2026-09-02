using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.Services.Ms03.Contracts;

public sealed class SearchResultsResponseDto
{
        [JsonPropertyName("items")]
        public List<SearchResultItemDto> Items { get; set; } = new();

        [JsonPropertyName("pagination")]
        public PaginationInfoDto Pagination { get; set; }
}
