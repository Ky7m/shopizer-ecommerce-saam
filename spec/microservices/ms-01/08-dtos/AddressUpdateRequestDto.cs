using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.Services.Ms01.Contracts;

public sealed class AddressUpdateRequestDto
{
        [JsonPropertyName("billing")]
        public AddressDto? Billing { get; set; }

        [JsonPropertyName("delivery")]
        public AddressDto? Delivery { get; set; }
}
