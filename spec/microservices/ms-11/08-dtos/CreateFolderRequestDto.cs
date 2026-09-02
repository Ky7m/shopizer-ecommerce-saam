using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.Services.Ms11.Contracts;

public sealed class CreateFolderRequestDto
{
        [JsonPropertyName("path")]
        [RegularExpression(@"^/$|^(/[A-Za-z0-9_-]+)+$")]
        public string? Path { get; set; }

        [JsonPropertyName("folderName")]
        [Required]
        [MinLength(1)]
        [RegularExpression(@"^[A-Za-z0-9_-]+$")]
        public string FolderName { get; set; }
}
