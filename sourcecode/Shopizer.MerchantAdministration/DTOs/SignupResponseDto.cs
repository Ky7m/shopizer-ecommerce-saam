using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.MerchantAdministration.DTOs;

public sealed class SignupResponseDto
{
    [JsonPropertyName("signupId")]
    [Required]
    public string SignupId { get; set; }

    [JsonPropertyName("status")]
    [Required]
    public string Status { get; set; }
}
