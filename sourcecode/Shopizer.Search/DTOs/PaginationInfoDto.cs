using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.Services.Ms03.Contracts;

public sealed class PaginationInfoDto
{
        [JsonPropertyName("offset")]
        [Range(0, double.MaxValue)]
        public int Offset { get; set; }

        [JsonPropertyName("limit")]
        [Range(1, 100)]
        public int Limit { get; set; }

        [JsonPropertyName("totalItems")]
        [Range(0, double.MaxValue)]
        public int TotalItems { get; set; }

        [JsonPropertyName("totalPages")]
        [Range(0, double.MaxValue)]
        public int TotalPages { get; set; }
}
