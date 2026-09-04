using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.ContentConfiguration.DTOs;

public sealed class ImageUploadRequestDto
{
    [JsonPropertyName("qqfile")]
    [Required]
    public string Qqfile { get; set; }

    [JsonPropertyName("qquuid")]
    [Required]
    [MinLength(1)]
    public string Qquuid { get; set; }

    [JsonPropertyName("qqfilename")]
    [Required]
    [MinLength(1)]
    [RegularExpression(@"^(?!.*\.\.)[^/\\]+$")]
    public string Qqfilename { get; set; }

    [JsonPropertyName("qqtotalfilesize")]
    public long? Qqtotalfilesize { get; set; }

    [JsonPropertyName("parentPath")]
    [RegularExpression(@"^/$|^(?!.*\.\.)(/[A-Za-z0-9_-]+)+$")]
    public string? ParentPath { get; set; }

    [JsonPropertyName("qqpartindex")]
    public int? Qqpartindex { get; set; }

    [JsonPropertyName("qqtotalparts")]
    public int? Qqtotalparts { get; set; }
}
