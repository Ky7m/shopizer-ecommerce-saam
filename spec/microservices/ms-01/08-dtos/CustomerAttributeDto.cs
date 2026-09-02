using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.Services.Ms01.Contracts;

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
