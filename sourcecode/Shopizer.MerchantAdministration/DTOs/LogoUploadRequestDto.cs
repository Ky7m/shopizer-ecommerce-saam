using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.MerchantAdministration.DTOs;

public sealed class LogoUploadRequestDto
{
    [JsonPropertyName("file")]
    [Required]
    public string File { get; set; }
}
