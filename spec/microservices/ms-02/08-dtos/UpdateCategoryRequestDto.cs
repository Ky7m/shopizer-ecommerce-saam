using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.Services.Ms02.Contracts;

public sealed class UpdateCategoryRequestDto
{
        [JsonPropertyName("code")]
        public string? Code { get; set; }

        [JsonPropertyName("parentId")]
        public string? ParentId { get; set; }

        [JsonPropertyName("visible")]
        public bool? Visible { get; set; }

        [JsonPropertyName("featured")]
        public bool? Featured { get; set; }

        [JsonPropertyName("sortOrder")]
        public int? SortOrder { get; set; }

        [JsonPropertyName("descriptions")]
        public List<CategoryDescriptionDto>? Descriptions { get; set; } = new();
}
