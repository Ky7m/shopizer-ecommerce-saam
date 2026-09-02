using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.Services.Ms08.Contracts;

public sealed class CreateTaxRateRequestDto
{
        [JsonPropertyName("taxClassCode")]
        [Required]
        [StringLength(10, MinimumLength = 1)]
        public string TaxClassCode { get; set; }

        [JsonPropertyName("code")]
        [Required]
        [StringLength(100, MinimumLength = 1)]
        public string Code { get; set; }

        [JsonPropertyName("rate")]
        [Range(0, 100)]
        public decimal Rate { get; set; }

        [JsonPropertyName("priority")]
        [Range(0, double.MaxValue)]
        public int Priority { get; set; }

        [JsonPropertyName("piggyback")]
        public bool Piggyback { get; set; }

        [JsonPropertyName("countryCode")]
        [Required]
        [StringLength(3, MinimumLength = 2)]
        public string CountryCode { get; set; }

        [JsonPropertyName("zoneCode")]
        public string? ZoneCode { get; set; }

        [JsonPropertyName("stateProvince")]
        [MaxLength(100)]
        public string? StateProvince { get; set; }

        [JsonPropertyName("descriptions")]
        public List<TaxRateDescriptionDto> Descriptions { get; set; } = new();
}
