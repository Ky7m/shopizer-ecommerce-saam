using System.Text.Json.Serialization;

namespace Shopizer.CatalogProduct.DTOs;

public sealed class UpdateCategoryVisibilityRequestDto
{
        [JsonPropertyName("visible")]
        public bool Visible { get; set; }
}
