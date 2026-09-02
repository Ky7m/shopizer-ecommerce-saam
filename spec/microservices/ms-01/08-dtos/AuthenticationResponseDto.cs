using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.Services.Ms01.Contracts;

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
