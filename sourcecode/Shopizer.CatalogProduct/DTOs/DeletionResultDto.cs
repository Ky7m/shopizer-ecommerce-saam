using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.CatalogProduct.DTOs;

public sealed class DeletionResultDto
{
        [JsonPropertyName("id")]
        [Required]
        public string Id { get; set; }

        [JsonPropertyName("status")]
        [Required]
        public string Status { get; set; }

        [JsonPropertyName("dependentsRemoved")]
        public int? DependentsRemoved { get; set; }

        [JsonPropertyName("projectionEventPublished")]
        public bool? ProjectionEventPublished { get; set; }
}
