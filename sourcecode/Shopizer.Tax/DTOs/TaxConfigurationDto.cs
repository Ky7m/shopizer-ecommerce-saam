using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.Tax.DTOs;

public sealed class TaxConfigurationDto
{
    [JsonPropertyName("taxBasis")]
    [Required]
    public string TaxBasis { get; set; }

    [JsonPropertyName("collectTaxIfDifferentProvince")]
    public bool CollectTaxIfDifferentProvince { get; set; }

    [JsonPropertyName("differentCountryBehavior")]
    [Required]
    public string DifferentCountryBehavior { get; set; }
}
