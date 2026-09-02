using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.Services.Ms12.Contracts;

public sealed class EmailMessageDto
{
        [JsonPropertyName("messageId")]
        [Required]
        public string MessageId { get; set; }

        [JsonPropertyName("operationId")]
        [Required]
        public string OperationId { get; set; }

        [JsonPropertyName("endpointId")]
        [Required]
        public string EndpointId { get; set; }

        [JsonPropertyName("idempotencyKey")]
        [Required]
        public string IdempotencyKey { get; set; }

        [JsonPropertyName("templateKey")]
        [Required]
        public string TemplateKey { get; set; }

        [JsonPropertyName("locale")]
        [Required]
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
        public string Subject { get; set; }

        [JsonPropertyName("status")]
        public EmailMessageStatusDto Status { get; set; }

        [JsonPropertyName("orderReference")]
        public string? OrderReference { get; set; }

        [JsonPropertyName("queuedAt")]
        [Required]
        public string QueuedAt { get; set; }

        [JsonPropertyName("sentAt")]
        public string? SentAt { get; set; }
}
