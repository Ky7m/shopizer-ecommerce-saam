using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.Tax.DTOs;

public sealed class JurisdictionDto
{
    [JsonPropertyName("countryCode")]
    [Required]
    public string CountryCode { get; set; }

    [JsonPropertyName("zoneCode")]
    public string? ZoneCode { get; set; }

    [JsonPropertyName("stateProvince")]
    public string? StateProvince { get; set; }
}
