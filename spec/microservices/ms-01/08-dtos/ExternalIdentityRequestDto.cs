using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.Services.Ms01.Contracts;

public sealed class ExternalIdentityRequestDto
{
        [JsonPropertyName("userId")]
        [Required]
        public string UserId { get; set; }

        [JsonPropertyName("providerId")]
        [Required]
        public string ProviderId { get; set; }

        [JsonPropertyName("providerUserId")]
        [Required]
        public string ProviderUserId { get; set; }

        [JsonPropertyName("accessToken")]
        public string? AccessToken { get; set; }

        [JsonPropertyName("refreshToken")]
        public string? RefreshToken { get; set; }

        [JsonPropertyName("profileUrl")]
        public string? ProfileUrl { get; set; }
}
