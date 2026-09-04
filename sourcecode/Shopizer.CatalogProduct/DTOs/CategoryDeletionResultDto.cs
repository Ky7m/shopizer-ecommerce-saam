using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.CatalogProduct.DTOs;

public sealed class CategoryDeletionResultDto
{
    [JsonPropertyName("categoryId")]
    [Required]
    public string CategoryId { get; set; }

    [JsonPropertyName("status")]
    [Required]
    public string Status { get; set; }

    [JsonPropertyName("deletedCategoryCount")]
    [Range(1, double.MaxValue)]
    public int DeletedCategoryCount { get; set; }

    [JsonPropertyName("detachedProductCount")]
    public int? DetachedProductCount { get; set; }

    [JsonPropertyName("deletedProductCount")]
    public int? DeletedProductCount { get; set; }
}
