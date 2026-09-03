using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.Services.Ms02.Contracts;

public sealed class PriceSelectionDto
{
        [JsonPropertyName("optionId")]
        [Required]
        public string OptionId { get; set; }

        [JsonPropertyName("valueId")]
        [Required]
        public string ValueId { get; set; }
}
