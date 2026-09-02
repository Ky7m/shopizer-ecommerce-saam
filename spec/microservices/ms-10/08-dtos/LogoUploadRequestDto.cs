using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.Services.Ms10.Contracts;

public sealed class LogoUploadRequestDto
{
        [JsonPropertyName("file")]
        [Required]
        public string File { get; set; }
}
