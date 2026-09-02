using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.Services.Ms10.Contracts;

public sealed class BrandingRequestDto
{
        [JsonPropertyName("templateCode")]
        public string? TemplateCode { get; set; }

        [JsonPropertyName("logoUri")]
        public string? LogoUri { get; set; }
}
