using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.Services.Ms12.Contracts;

public sealed class QueueEmailRequestDto
{
        [JsonPropertyName("idempotencyKey")]
        [Required]
        [StringLength(256, MinimumLength = 1)]
        public string IdempotencyKey { get; set; }

        [JsonPropertyName("templateKey")]
        [Required]
        [MinLength(1)]
        public string TemplateKey { get; set; }

        [JsonPropertyName("locale")]
        [Required]
        [MinLength(2)]
        public string Locale { get; set; }

        [JsonPropertyName("recipientEmail")]
        [Required]
        [EmailAddress]
        public string RecipientEmail { get; set; }

        [JsonPropertyName("senderEmail")]
        [Required]
        [EmailAddress]
        public string SenderEmail { get; set; }

        [JsonPropertyName("senderName")]
        public string? SenderName { get; set; }

        [JsonPropertyName("subject")]
        [Required]
        [MinLength(1)]
        public string Subject { get; set; }

        [JsonPropertyName("tokenPayload")]
        public Dictionary<string, object?> TokenPayload { get; set; } = new();

        [JsonPropertyName("orderReference")]
        public string? OrderReference { get; set; }
}
