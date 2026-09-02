using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.Services.Ms02.Contracts;

public sealed class CalculatePriceRequestDto
{
        [JsonPropertyName("selections")]
        public List<PriceSelectionDto> Selections { get; set; } = new();

        [JsonPropertyName("currencyCode")]
        public string? CurrencyCode { get; set; }

        [JsonPropertyName("countryCode")]
        public string? CountryCode { get; set; }
}
