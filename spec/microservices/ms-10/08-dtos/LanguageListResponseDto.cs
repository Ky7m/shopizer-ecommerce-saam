using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.Services.Ms10.Contracts;

public sealed class LanguageListResponseDto
{
        [JsonPropertyName("items")]
        public List<string> Items { get; set; } = new();
}
