using System.Text.Json.Serialization;

namespace Shopizer.Services.Ms02.Contracts;

public sealed class CategoryListResponseDto
{
        [JsonPropertyName("items")]
        public List<CategoryDto> Items { get; set; } = new();

        [JsonPropertyName("pagination")]
        public PaginationInfoDto Pagination { get; set; }
}
