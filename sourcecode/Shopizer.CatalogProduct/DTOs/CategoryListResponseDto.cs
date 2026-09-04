using System.Text.Json.Serialization;

namespace Shopizer.CatalogProduct.DTOs;

public sealed class CategoryListResponseDto
{
    [JsonPropertyName("items")]
    public List<CategoryDto> Items { get; set; } = new();

    [JsonPropertyName("pagination")]
    public PaginationInfoDto Pagination { get; set; }
}
