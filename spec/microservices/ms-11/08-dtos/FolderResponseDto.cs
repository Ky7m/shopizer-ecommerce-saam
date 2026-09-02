using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.Services.Ms11.Contracts;

public sealed class FolderResponseDto
{
        [JsonPropertyName("path")]
        [Required]
        public string Path { get; set; }

        [JsonPropertyName("created")]
        public bool? Created { get; set; }
}
