using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.Services.Ms05.Contracts;

public sealed class ErrorResponseDto
{
        [JsonPropertyName("error")]
        [Required]
        public string Error { get; set; }

        [JsonPropertyName("message")]
        [Required]
        public string Message { get; set; }

        [JsonPropertyName("statusCode")]
        public int StatusCode { get; set; }

        [JsonPropertyName("timestamp")]
        [Required]
        public string Timestamp { get; set; }

        [JsonPropertyName("correlationId")]
        public string? CorrelationId { get; set; }

        [JsonPropertyName("details")]
        public object? Details { get; set; }
}
