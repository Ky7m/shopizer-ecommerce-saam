using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.Services.Ms12.Contracts;

public sealed class FolderRequestDto
{
        [JsonPropertyName("storeCode")]
        [Required]
        public string StoreCode { get; set; }

        [JsonPropertyName("provider")]
        public StorageProviderDto Provider { get; set; }

        [JsonPropertyName("folderPath")]
        public string? FolderPath { get; set; }

        [JsonPropertyName("folderName")]
        [Required]
        [MinLength(1)]
        public string FolderName { get; set; }
}
