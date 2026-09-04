using System.Text.Json.Serialization;

namespace Shopizer.MerchantAdministration.DTOs;

public sealed class EntityExistsResponseDto
{
    [JsonPropertyName("exists")]
    public bool Exists { get; set; }
}
