using System.Text.Json.Serialization;

namespace Shopizer.MerchantAdministration.DTOs;

public sealed class SignupVerificationResponseDto
{
    [JsonPropertyName("verified")]
    public bool Verified { get; set; }
}
