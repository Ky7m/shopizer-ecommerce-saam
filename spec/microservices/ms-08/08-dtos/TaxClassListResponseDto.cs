using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.Services.Ms08.Contracts;

public sealed class TaxClassListResponseDto
{
        [JsonPropertyName("items")]
        public List<TaxClassDto> Items { get; set; } = new();

        [JsonPropertyName("pagination")]
        public PaginationInfoDto Pagination { get; set; }
}
