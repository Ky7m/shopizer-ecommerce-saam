using System.Text.Json.Serialization;

namespace Shopizer.Services.Ms02.Contracts;

public sealed class UpdateCategoryVisibilityRequestDto
{
        [JsonPropertyName("visible")]
        public bool Visible { get; set; }
}
