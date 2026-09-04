using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.CustomerIdentity.DTOs;

public sealed class NewsletterSubscriptionRequestDto
{
    [JsonPropertyName("email")]
    [Required]
    [EmailAddress]
    public string Email { get; set; }

    [JsonPropertyName("firstName")]
    public string? FirstName { get; set; }

    [JsonPropertyName("lastName")]
    public string? LastName { get; set; }
}
