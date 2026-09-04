using System.Text.Json.Serialization;

namespace Shopizer.CatalogProduct.DTOs;

public sealed class ProductListResponseDto
{
    [JsonPropertyName("items")]
    public List<ProductDto> Items { get; set; } = new();

    [JsonPropertyName("pagination")]
    public PaginationInfoDto Pagination { get; set; }
}
