using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.CustomerIdentity.DTOs;

public sealed class CustomerAttributeDto
{
        [JsonPropertyName("optionId")]
        [Required]
        public string OptionId { get; set; }

        [JsonPropertyName("optionValueId")]
        [Required]
        public string OptionValueId { get; set; }

        [JsonPropertyName("textValue")]
        public string? TextValue { get; set; }
}
