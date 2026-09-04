using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.CustomerIdentity.DTOs;

public sealed class CustomerPasswordChangeRequestDto
{
    [JsonPropertyName("currentPassword")]
    [Required]
    public string CurrentPassword { get; set; }

    [JsonPropertyName("newPassword")]
    [Required]
    [MinLength(8)]
    public string NewPassword { get; set; }

    [JsonPropertyName("repeatPassword")]
    [Required]
    [MinLength(8)]
    public string RepeatPassword { get; set; }
}
