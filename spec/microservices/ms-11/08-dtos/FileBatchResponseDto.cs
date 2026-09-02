using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.Services.Ms11.Contracts;

public sealed class FileBatchResponseDto
{
        [JsonPropertyName("items")]
        public List<FileResponseDto> Items { get; set; } = new();
}
