using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.Services.Ms03.Contracts;

public sealed class AutocompleteResponseDto
{
        [JsonPropertyName("suggestions")]
        public List<string> Suggestions { get; set; } = new();
}
