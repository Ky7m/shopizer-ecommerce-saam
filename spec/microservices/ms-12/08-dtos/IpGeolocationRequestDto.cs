using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.Services.Ms12.Contracts;

public sealed class IpGeolocationRequestDto
{
        [JsonPropertyName("ipAddress")]
        public object IpAddress { get; set; }
}
