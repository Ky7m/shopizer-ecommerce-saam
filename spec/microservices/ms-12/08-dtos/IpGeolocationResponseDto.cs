using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.Services.Ms12.Contracts;

public sealed class IpGeolocationResponseDto
{
        [JsonPropertyName("resolved")]
        public bool Resolved { get; set; }

        [JsonPropertyName("countryCode")]
        [Required]
        public string CountryCode { get; set; }

        [JsonPropertyName("postalCode")]
        [Required]
        public string PostalCode { get; set; }

        [JsonPropertyName("zoneCode")]
        [Required]
        public string ZoneCode { get; set; }

        [JsonPropertyName("city")]
        [Required]
        public string City { get; set; }
}
