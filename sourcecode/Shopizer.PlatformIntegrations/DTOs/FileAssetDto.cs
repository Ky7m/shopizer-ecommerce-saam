using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.PlatformIntegrations.DTOs;

public sealed class FileAssetDto
{
    [JsonPropertyName("operationId")]
    public string? OperationId { get; set; }

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

    [JsonPropertyName("status")]
    public FileStatusDto Status { get; set; }

    [JsonPropertyName("deliveryAttemptId")]
    public string? DeliveryAttemptId { get; set; }
}
