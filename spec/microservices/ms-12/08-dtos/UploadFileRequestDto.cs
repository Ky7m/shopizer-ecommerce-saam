using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.Services.Ms12.Contracts;

public sealed class UploadFileRequestDto
{
        [JsonPropertyName("storeCode")]
        [Required]
        [MinLength(1)]
        public string StoreCode { get; set; }

        [JsonPropertyName("contentType")]
        public ContentTypeDto ContentType { get; set; }

        [JsonPropertyName("folderPath")]
        public string? FolderPath { get; set; }

        [JsonPropertyName("fileName")]
        [Required]
        [MinLength(1)]
        public string FileName { get; set; }

        [JsonPropertyName("mimeType")]
        [Required]
        [MinLength(1)]
        public string MimeType { get; set; }

        [JsonPropertyName("contentBase64")]
        [Required]
        public string ContentBase64 { get; set; }

        [JsonPropertyName("idempotencyKey")]
        [Required]
        [StringLength(256, MinimumLength = 1)]
        public string IdempotencyKey { get; set; }
}
