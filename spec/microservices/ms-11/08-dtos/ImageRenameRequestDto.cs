using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.Services.Ms11.Contracts;

public sealed class ImageRenameRequestDto
{
        [JsonPropertyName("path")]
        [Required]
        [MinLength(1)]
        [RegularExpression(@"^(?!.*\.\.).+$")]
        public string Path { get; set; }

        [JsonPropertyName("newName")]
        [Required]
        [MinLength(1)]
        [RegularExpression(@"^(?!.*\.\.)[^/\\]+$")]
        public string NewName { get; set; }
}
