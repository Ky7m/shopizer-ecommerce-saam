using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.Services.Ms11.Contracts;

public sealed class ContentFolderResponseDto
{
        [JsonPropertyName("path")]
        [Required]
        public string Path { get; set; }

        [JsonPropertyName("content")]
        public List<ContentFolderEntryDto> Content { get; set; } = new();
}
