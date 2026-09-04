using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.CustomerIdentity.DTOs;

public sealed class AuthenticationResponseDto
{
    [JsonPropertyName("subjectId")]
    [Required]
    public string SubjectId { get; set; }

    [JsonPropertyName("accessToken")]
    [Required]
    public string AccessToken { get; set; }

    [JsonPropertyName("tokenType")]
    [Required]
    public string TokenType { get; set; }

    [JsonPropertyName("expiresAt")]
    public string? ExpiresAt { get; set; }
}
