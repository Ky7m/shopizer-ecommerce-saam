using System.Text.Json.Serialization;

namespace Shopizer.CatalogProduct.DTOs;

public sealed class ProductVariantListResponseDto
{
        [JsonPropertyName("items")]
        public List<ProductVariantDto> Items { get; set; } = new();

        [JsonPropertyName("pagination")]
        public PaginationInfoDto Pagination { get; set; }
}
