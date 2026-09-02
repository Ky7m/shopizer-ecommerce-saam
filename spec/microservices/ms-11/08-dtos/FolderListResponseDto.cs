using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.Services.Ms11.Contracts;

public sealed class FolderListResponseDto
{
        [JsonPropertyName("path")]
        [Required]
        public string Path { get; set; }

        [JsonPropertyName("folders")]
        public List<string> Folders { get; set; } = new();
}
