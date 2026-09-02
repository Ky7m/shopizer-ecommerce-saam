using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.Services.Ms11.Contracts;

public sealed class LegacyMultiFileUploadRequestDto
{
        [JsonPropertyName("file")]
        public List<string> File { get; set; } = new();
}
