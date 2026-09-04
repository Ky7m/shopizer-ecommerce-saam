using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.ContentConfiguration.DTOs;

public sealed class ContentFileDto
{
    [JsonPropertyName("id")]
    [Required]
    public string Id { get; set; }

    [JsonPropertyName("tenantId")]
    [Required]
    public string TenantId { get; set; }

    [JsonPropertyName("storeId")]
    [Required]
    public string StoreId { get; set; }

    [JsonPropertyName("fileName")]
    [Required]
    [MinLength(1)]
    [RegularExpression(@"^(?!.*\.\.)[^/\\]+$")]
    public string FileName { get; set; }

    [JsonPropertyName("mimeType")]
    [MaxLength(255)]
    public string? MimeType { get; set; }

    [JsonPropertyName("fileContentType")]
    public FileContentTypeDto FileContentType { get; set; }

    [JsonPropertyName("folderPath")]
    [Required]
    [RegularExpression(@"^/$|^(/[A-Za-z0-9_-]+)+$")]
    public string FolderPath { get; set; }

    [JsonPropertyName("providerName")]
    public ProviderNameDto ProviderName { get; set; }

    [JsonPropertyName("providerKey")]
    [Required]
    [MinLength(1)]
    public string ProviderKey { get; set; }

    [JsonPropertyName("state")]
    public FileStateDto State { get; set; }

    [JsonPropertyName("createdAt")]
    public string? CreatedAt { get; set; }

    [JsonPropertyName("updatedAt")]
    public string? UpdatedAt { get; set; }
}
