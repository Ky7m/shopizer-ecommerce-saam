using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.Services.Ms03.Contracts;

public sealed class AutocompleteRequestDto
{
        [JsonPropertyName("query")]
        [Required]
        [StringLength(500, MinimumLength = 1)]
        public string Query { get; set; }
}
