using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.Services.Ms12.Contracts;

public sealed class FileBatchResponseDto
{
        [JsonPropertyName("operationId")]
        [Required]
        public string OperationId { get; set; }

        [JsonPropertyName("items")]
        public List<UploadedFileAssetDto> Items { get; set; } = new();

        [JsonPropertyName("acceptedCount")]
        [Range(0, double.MaxValue)]
        public int AcceptedCount { get; set; }

        [JsonPropertyName("failedCount")]
        [Range(0, double.MaxValue)]
        public int FailedCount { get; set; }
}
