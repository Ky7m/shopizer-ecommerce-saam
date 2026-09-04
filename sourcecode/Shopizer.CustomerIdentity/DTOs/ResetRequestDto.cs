using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.CustomerIdentity.DTOs;

public sealed class ResetRequestDto
{
    [JsonPropertyName("username")]
    [Required]
    public string Username { get; set; }

    [JsonPropertyName("returnUrl")]
    [Required]
    public string ReturnUrl { get; set; }
}
