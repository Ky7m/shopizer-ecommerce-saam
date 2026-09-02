using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.Services.Ms02.Contracts;

public sealed class CreateVariantRequestDto
{
        [JsonPropertyName("sku")]
        [Required]
        [MinLength(1)]
        public string Sku { get; set; }

        [JsonPropertyName("code")]
        public string? Code { get; set; }

        [JsonPropertyName("defaultSelection")]
        public bool? DefaultSelection { get; set; }

        [JsonPropertyName("available")]
        public bool? Available { get; set; }

        [JsonPropertyName("dateAvailable")]
        public string? DateAvailable { get; set; }

        [JsonPropertyName("availability")]
        public AvailabilityInputDto? Availability { get; set; }
}
