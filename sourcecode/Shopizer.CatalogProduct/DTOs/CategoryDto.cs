using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.CatalogProduct.DTOs;

public sealed class CategoryDto
{
    [JsonPropertyName("id")]
    [Required]
    public string Id { get; set; }

    [JsonPropertyName("storeId")]
    [Required]
    public string StoreId { get; set; }

    [JsonPropertyName("code")]
    [Required]
    public string Code { get; set; }

    [JsonPropertyName("parentId")]
    public string? ParentId { get; set; }

    [JsonPropertyName("status")]
    [Required]
    public string Status { get; set; }

    [JsonPropertyName("visible")]
    public bool Visible { get; set; }

    [JsonPropertyName("featured")]
    public bool? Featured { get; set; }

    [JsonPropertyName("sortOrder")]
    public int? SortOrder { get; set; }

    [JsonPropertyName("depth")]
    [Range(0, double.MaxValue)]
    public int Depth { get; set; }

    [JsonPropertyName("lineage")]
    [Required]
    public string Lineage { get; set; }

    [JsonPropertyName("descriptions")]
    public List<CategoryDescriptionDto> Descriptions { get; set; } = new();

    [JsonPropertyName("children")]
    public List<CategoryReferenceDto>? Children { get; set; } = new();
}
