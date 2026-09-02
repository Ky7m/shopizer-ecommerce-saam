using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.Services.Ms12.Contracts;

public sealed class FileContentResponseDto
{
        [JsonPropertyName("fileName")]
        [Required]
        public string FileName { get; set; }

        [JsonPropertyName("contentType")]
        public ContentTypeDto ContentType { get; set; }

        [JsonPropertyName("mimeType")]
        [Required]
        public string MimeType { get; set; }

        [JsonPropertyName("providerKey")]
        [Required]
        public string ProviderKey { get; set; }

        [JsonPropertyName("contentBase64")]
        [Required]
        public string ContentBase64 { get; set; }
}
