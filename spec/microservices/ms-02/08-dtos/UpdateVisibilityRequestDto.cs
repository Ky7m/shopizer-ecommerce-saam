using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.Services.Ms02.Contracts;

public sealed class UpdateVisibilityRequestDto
{
        [JsonPropertyName("visible")]
        public bool Visible { get; set; }

        [JsonPropertyName("canBePurchased")]
        public bool CanBePurchased { get; set; }

        [JsonPropertyName("dateAvailable")]
        public string? DateAvailable { get; set; }
}
