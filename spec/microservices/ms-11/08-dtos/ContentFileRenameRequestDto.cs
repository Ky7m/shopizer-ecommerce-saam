using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.Services.Ms11.Contracts;

public sealed class ContentFileRenameRequestDto
{
        [JsonPropertyName("fileName")]
        [Required]
        [MinLength(1)]
        [RegularExpression(@"^(?!.*\.\.)[^/\\]+$")]
        public string FileName { get; set; }

        [JsonPropertyName("newName")]
        [Required]
        [MinLength(1)]
        [RegularExpression(@"^(?!.*\.\.)[^/\\]+$")]
        public string NewName { get; set; }

        [JsonPropertyName("contentType")]
        public FileContentTypeDto ContentType { get; set; }

        [JsonPropertyName("path")]
        [RegularExpression(@"^/$|^(/[A-Za-z0-9_-]+)+$")]
        public string? Path { get; set; }
}
