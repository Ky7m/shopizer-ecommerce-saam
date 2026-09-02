using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.Services.Ms10.Contracts;

public sealed class StoreListResponseDto
{
        [JsonPropertyName("items")]
        public List<StoreDto> Items { get; set; } = new();

        [JsonPropertyName("pagination")]
        public PaginationInfoDto Pagination { get; set; }
}
