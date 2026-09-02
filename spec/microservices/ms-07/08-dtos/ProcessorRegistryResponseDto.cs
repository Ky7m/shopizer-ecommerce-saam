using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.Services.Ms07.Contracts;

public sealed class ProcessorRegistryResponseDto
{
        [JsonPropertyName("processors")]
        public List<ProcessorDto> Processors { get; set; } = new();

        [JsonPropertyName("inactive")]
        public List<InactiveProcessorDto> Inactive { get; set; } = new();
}
