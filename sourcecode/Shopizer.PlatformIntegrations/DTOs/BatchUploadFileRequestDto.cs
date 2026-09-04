using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.PlatformIntegrations.DTOs;

public sealed class BatchUploadFileRequestDto
{
    [JsonPropertyName("storeCode")]
    [Required]
    [MinLength(1)]
    public string StoreCode { get; set; }

    [JsonPropertyName("folderPath")]
    public string? FolderPath { get; set; }

    [JsonPropertyName("idempotencyKey")]
    [Required]
    [StringLength(256, MinimumLength = 1)]
    public string IdempotencyKey { get; set; }

    [JsonPropertyName("files")]
    public List<UploadFileItemDto> Files { get; set; } = new();
}
