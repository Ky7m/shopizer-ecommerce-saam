using System.Text.Json.Serialization;

namespace Shopizer.CatalogProduct.DTOs;

public sealed class CalculatePriceRequestDto
{
    [JsonPropertyName("selections")]
    public List<PriceSelectionDto> Selections { get; set; } = new();

    [JsonPropertyName("currencyCode")]
    public string? CurrencyCode { get; set; }

    [JsonPropertyName("countryCode")]
    public string? CountryCode { get; set; }
}
