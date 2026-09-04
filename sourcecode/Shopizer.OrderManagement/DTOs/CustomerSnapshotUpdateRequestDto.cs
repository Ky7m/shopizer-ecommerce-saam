using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shopizer.OrderManagement.DTOs;

public sealed class CustomerSnapshotUpdateRequestDto
{
        [JsonPropertyName("emailAddress")]
        [Required]
        [EmailAddress]
        public string EmailAddress { get; set; }

        [JsonPropertyName("billingAddress")]
        public AddressSnapshotDto BillingAddress { get; set; }

        [JsonPropertyName("deliveryAddress")]
        public AddressSnapshotDto DeliveryAddress { get; set; }
}
