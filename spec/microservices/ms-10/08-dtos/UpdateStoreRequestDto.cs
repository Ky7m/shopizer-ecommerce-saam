using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.Services.Ms10.Contracts;

public sealed class UpdateStoreRequestDto
{
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("emailAddress")]
        [EmailAddress]
        public string? EmailAddress { get; set; }

        [JsonPropertyName("phone")]
        public string? Phone { get; set; }

        [JsonPropertyName("address")]
        public AddressDto? Address { get; set; }

        [JsonPropertyName("defaultLanguageCode")]
        public string? DefaultLanguageCode { get; set; }

        [JsonPropertyName("currencyCode")]
        public string? CurrencyCode { get; set; }

        [JsonPropertyName("dimensionUnit")]
        public string? DimensionUnit { get; set; }

        [JsonPropertyName("weightUnit")]
        public string? WeightUnit { get; set; }
}
