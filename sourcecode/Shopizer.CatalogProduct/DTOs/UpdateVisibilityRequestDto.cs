using System.Text.Json.Serialization;

namespace Shopizer.CatalogProduct.DTOs;

public sealed class UpdateVisibilityRequestDto
{
    [JsonPropertyName("visible")]
    public bool Visible { get; set; }

    [JsonPropertyName("canBePurchased")]
    public bool CanBePurchased { get; set; }

    [JsonPropertyName("dateAvailable")]
    public string? DateAvailable { get; set; }
}
