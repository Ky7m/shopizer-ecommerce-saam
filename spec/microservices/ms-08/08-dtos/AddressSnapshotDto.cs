using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.Services.Ms08.Contracts;

public sealed class AddressSnapshotDto
{
        [JsonPropertyName("countryCode")]
        [Required]
        [StringLength(3, MinimumLength = 2)]
        public string CountryCode { get; set; }

        [JsonPropertyName("zoneCode")]
        public string? ZoneCode { get; set; }

        [JsonPropertyName("stateProvince")]
        [MaxLength(100)]
        public string? StateProvince { get; set; }
}
