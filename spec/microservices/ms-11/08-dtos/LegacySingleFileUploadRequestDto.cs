using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.Services.Ms11.Contracts;

public sealed class LegacySingleFileUploadRequestDto
{
        [JsonPropertyName("file")]
        [Required]
        public string File { get; set; }
}
