using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.Services.Ms10.Contracts;

public sealed class ReplaceLanguagesRequestDto
{
        [JsonPropertyName("defaultLanguageCode")]
        [Required]
        public string DefaultLanguageCode { get; set; }

        [JsonPropertyName("supportedLanguageCodes")]
        public List<string> SupportedLanguageCodes { get; set; } = new();
}
