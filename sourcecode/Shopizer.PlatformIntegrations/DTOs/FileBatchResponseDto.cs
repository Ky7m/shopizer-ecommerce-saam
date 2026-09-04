using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.PlatformIntegrations.DTOs;

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
