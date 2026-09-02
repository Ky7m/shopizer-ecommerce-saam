using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.CustomerIdentity.DTOs;

public sealed class ExternalIdentityConnectionDto
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

        [JsonPropertyName("displayName")]
        public string? DisplayName { get; set; }

        [JsonPropertyName("profileUrl")]
        public string? ProfileUrl { get; set; }

        [JsonPropertyName("imageUrl")]
        public string? ImageUrl { get; set; }
}
