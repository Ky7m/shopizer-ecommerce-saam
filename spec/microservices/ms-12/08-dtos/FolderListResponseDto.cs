using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.Services.Ms12.Contracts;

public sealed class FolderListResponseDto
{
        [JsonPropertyName("items")]
        public List<string> Items { get; set; } = new();

        [JsonPropertyName("provider")]
        public StorageProviderDto Provider { get; set; }
}
