using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.Services.Ms09.Contracts;

public sealed class ExpeditionConfigurationRequestDto
{
        [JsonPropertyName("internationalShipping")]
        public bool InternationalShipping { get; set; }

        [JsonPropertyName("taxOnShipping")]
        public bool TaxOnShipping { get; set; }

        [JsonPropertyName("shipToCountry")]
        public List<string> ShipToCountry { get; set; } = new();
}
