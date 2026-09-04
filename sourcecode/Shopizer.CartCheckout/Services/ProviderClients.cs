using System.Net;
using System.Text.Json;
using Shopizer.CartCheckout.DTOs;
using Shopizer.CartCheckout.Models;

namespace Shopizer.CartCheckout.Services;

public sealed class ContextPropagationHandler(IHttpContextAccessor accessor) : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        if (accessor.HttpContext is { } http)
        {
            var context = RequestContext.From(http);
            request.Headers.TryAddWithoutValidation("x-tenant-id", context.TenantId);
            request.Headers.TryAddWithoutValidation("x-store-id", context.StoreId);
            request.Headers.TryAddWithoutValidation("x-correlation-id", context.CorrelationId);
            if (http.Request.Headers.Authorization.Count > 0)
                request.Headers.TryAddWithoutValidation("Authorization", http.Request.Headers.Authorization.ToString());
        }
        return base.SendAsync(request, ct);
    }
}

internal static class ProviderResponse
{
    public static async Task<JsonDocument> Read(HttpResponseMessage response, string operation, CancellationToken ct)
    {
        if (response.IsSuccessStatusCode) return await response.Content.ReadFromJsonAsync<JsonDocument>(cancellationToken: ct)
            ?? throw new DomainException("CHECKOUT_UNAVAILABLE", $"{operation} returned an empty response", 503);
        var status = (int)response.StatusCode;
        if (status == 404)
        {
            var code = operation.Contains("Customer lookup", StringComparison.OrdinalIgnoreCase)
                ? "AUTHENTICATION_REQUIRED"
                : "CHECKOUT_UNAVAILABLE";
            var responseStatus = code == "AUTHENTICATION_REQUIRED" ? 401 : 503;
            throw new DomainException(code, $"{operation} could not resolve the required downstream resource", responseStatus);
        }
        if (status is 400 or 422) throw new DomainException("INVALID_REQUEST", $"{operation} rejected the request", status);
        if (status == 409) throw new DomainException("CUSTOMER_ALREADY_REGISTERED", $"{operation} conflicts with an existing resource", 409);
        if (status == 401) throw new DomainException("AUTHENTICATION_REQUIRED", $"{operation} could not authenticate the request", 401);
        throw new DomainException("CHECKOUT_UNAVAILABLE", $"{operation} is unavailable", 503);
    }
    public static long Id(JsonElement root, string property)
    {
        var value = JsonHelpers.String(root, property) ?? throw new DomainException("PRODUCT_NOT_SELLABLE", "The provider did not return a product identifier", 422);
        return DtoMapper.OpaqueNumericId(value);
    }
}

public sealed class CustomerClient(HttpClient client)
{
    public async Task<CustomerFact> CurrentAsync(CancellationToken ct)
    {
        using var response = await client.GetAsync("/api/v1/customers/me", ct);
        using var document = await ProviderResponse.Read(response, "Customer lookup", ct);
        var root = document.RootElement;
        var id = JsonHelpers.String(root, "id") ?? throw new DomainException("AUTHENTICATION_REQUIRED", "Customer context is unavailable", 401);
        return new CustomerFact(DtoMapper.OpaqueNumericId(id), ReadAddress(root, "billing"), ReadAddress(root, "delivery"));
    }

    public async Task<CustomerFact> RegisterAsync(AnonymousCustomerDto customer, string? password, RequestContext context, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(password)) return new CustomerFact(DtoMapper.OpaqueNumericId(customer.Email), customer.Billing, null);
        var body = new
        {
            emailAddress = customer.Email,
            password,
            firstName = customer.FirstName,
            lastName = customer.LastName,
            billing = customer.Billing,
            delivery = (object?)null
        };
        using var response = await client.PostAsJsonAsync("/api/v1/customer-auth/registrations", body, cancellationToken: ct);
        using var document = await ProviderResponse.Read(response, "Customer registration", ct);
        var root = document.RootElement;
        var id = JsonHelpers.String(root, "subjectId") ?? throw new DomainException("CHECKOUT_UNAVAILABLE", "Customer registration returned no subject", 503);
        return new CustomerFact(DtoMapper.OpaqueNumericId(id), customer.Billing, null);
    }

    private static AddressDto? ReadAddress(JsonElement root, string property)
    {
        if (!root.TryGetProperty(property, out var value) || value.ValueKind == JsonValueKind.Null) return null;
        return new AddressDto
        {
            FirstName = JsonHelpers.String(value, "firstName") ?? "",
            LastName = JsonHelpers.String(value, "lastName") ?? "",
            Company = JsonHelpers.String(value, "company") ?? JsonHelpers.String(value, "companyName"),
            Address = JsonHelpers.String(value, "address") ?? JsonHelpers.String(value, "streetAddress") ?? "",
            City = JsonHelpers.String(value, "city") ?? "",
            StateProvince = JsonHelpers.String(value, "stateProvince"),
            CountryCode = JsonHelpers.String(value, "countryCode") ?? "",
            PostalCode = JsonHelpers.String(value, "postalCode") ?? "",
            Phone = JsonHelpers.String(value, "phone") ?? JsonHelpers.String(value, "telephone")
        };
    }
}

public sealed class CatalogClient(HttpClient client)
{
    public async Task<ProductFact?> ProductAsync(string sku, CancellationToken ct)
    {
        using var response = await client.GetAsync($"/api/v1/products/sku/{Uri.EscapeDataString(sku)}", ct);
        if (response.StatusCode == HttpStatusCode.NotFound) return null;
        using var document = await ProviderResponse.Read(response, "Product lookup", ct);
        var root = document.RootElement;
        var id = JsonHelpers.String(root, "id") ?? "";
        var descriptions = root.TryGetProperty("descriptions", out var values) && values.ValueKind == JsonValueKind.Array ? values.EnumerateArray().ToList() : [];
        var name = descriptions.Count == 0 ? sku : JsonHelpers.String(descriptions[0], "name") ?? sku;
        var attributes = new HashSet<long>();
        if (root.TryGetProperty("options", out var options) && options.ValueKind == JsonValueKind.Array)
            foreach (var option in options.EnumerateArray())
                if (option.TryGetProperty("values", out var optionValues) && optionValues.ValueKind == JsonValueKind.Array)
                    foreach (var value in optionValues.EnumerateArray())
                        if (JsonHelpers.String(value, "id") is { } attributeId) attributes.Add(DtoMapper.OpaqueNumericId(attributeId));
        DateTimeOffset? availableAt = null;
        if (DateTimeOffset.TryParse(JsonHelpers.String(root, "dateAvailable"), out var parsed)) availableAt = parsed;
        decimal? price = root.TryGetProperty("price", out var priceRoot) ? JsonHelpers.Decimal(priceRoot, "finalAmount", JsonHelpers.Decimal(priceRoot, "amount", 0)) : null;
        return new ProductFact(id, DtoMapper.OpaqueNumericId(id), JsonHelpers.String(root, "storeId") ?? "", JsonHelpers.String(root, "sku") ?? sku,
            JsonHelpers.Bool(root, "available"), JsonHelpers.Bool(root, "canBePurchased"), availableAt,
            JsonHelpers.Bool(root, "productVirtual"), JsonHelpers.Bool(root, "productShippable", true), name,
            root.TryGetProperty("price", out var p) ? JsonHelpers.String(p, "currencyCode") ?? "" : "", price, attributes);
    }

    public async Task EnsureAvailableAsync(ProductFact product, CancellationToken ct)
    {
        if (!Guid.TryParse(product.Id, out var id))
            throw new DomainException("PRODUCT_NOT_SELLABLE", $"Product {product.Sku} has no valid availability identity", 422);
        using var response = await client.GetAsync($"/api/v1/products/{id}/availability", ct);
        using var document = await ProviderResponse.Read(response, "Product availability", ct);
        var items = document.RootElement.TryGetProperty("items", out var value) && value.ValueKind == JsonValueKind.Array ? value.EnumerateArray() : [];
        if (!items.Any(item => JsonHelpers.Bool(item, "active") && (JsonHelpers.Decimal(item, "sellableQuantity", JsonHelpers.Decimal(item, "quantity")) > 0)))
            throw new DomainException("PRODUCT_NOT_SELLABLE", $"Product {product.Sku} has no available inventory", 422);
    }
}

public sealed class PricingClient(HttpClient client)
{
    public async Task<PriceFact> PriceAsync(string sku, IEnumerable<long> attributes, string currency, CancellationToken ct)
    {
        var body = new
        {
            attributes = attributes.Select(x => new
            {
                attributeId = x.ToString(),
                valueId = x.ToString(),
                priceAdjustment = 0m
            }).ToArray(),
            evaluationAt = DateTimeOffset.UtcNow
        };
        using var response = await client.PostAsJsonAsync($"/api/v1/pricing/products/{Uri.EscapeDataString(sku)}/quote", body, cancellationToken: ct);
        using var document = await ProviderResponse.Read(response, "Pricing quote", ct);
        var root = document.RootElement;
        var amount = JsonHelpers.Decimal(root, "finalPrice", JsonHelpers.Decimal(root, "finalAmount", -1));
        if (amount < 0) throw new DomainException("CHECKOUT_UNAVAILABLE", "Pricing did not return a final amount", 503);
        return new PriceFact(amount, JsonHelpers.String(root, "currency") ?? JsonHelpers.String(root, "currencyCode") ?? currency, JsonHelpers.String(root, "quoteVersion"));
    }

    public async Task<(decimal Discount, string? Version)> PromotionAsync(string code, IEnumerable<CartLine> items, string currency, CancellationToken ct)
    {
        var body = new
        {
            promoCode = code,
            items = items.Select(x => new
            {
                productSku = x.Sku,
                quantity = x.Quantity,
                attributes = x.Attributes.Select(id => new { attributeId = id.ToString(), valueId = id.ToString(), priceAdjustment = 0m })
            }),
            evaluationAt = DateTimeOffset.UtcNow
        };
        using var response = await client.PostAsJsonAsync("/api/v1/pricing/promotions/evaluate", body, cancellationToken: ct);
        using var document = await ProviderResponse.Read(response, "Promotion evaluation", ct);
        var root = document.RootElement;
        if (!JsonHelpers.Bool(root, "matched")) return (0, JsonHelpers.String(root, "version"));
        return (JsonHelpers.Decimal(root, "reduction"), JsonHelpers.String(root, "version"));
    }

    public async Task<JsonDocument> QuoteAsync(string currency, IEnumerable<CartLine> items, string? promoCode, CancellationToken ct)
    {
        var body = new
        {
            currency,
            items = items.Select(x => new
            {
                productSku = x.Sku,
                quantity = x.Quantity,
                attributes = x.Attributes.Select(id => new { attributeId = id.ToString(), valueId = id.ToString(), priceAdjustment = 0m })
            }),
            promoCode,
            evaluationAt = DateTimeOffset.UtcNow
        };
        using var response = await client.PostAsJsonAsync("/api/v1/pricing/quotes", body, cancellationToken: ct);
        return await ProviderResponse.Read(response, "Pricing quote", ct);
    }
}

public sealed class ShippingClient(HttpClient client)
{
    public async Task<ShippingFact> QuoteAsync(string cartCode, AddressDto address, CancellationToken ct)
    {
        using var response = await client.PostAsJsonAsync($"/api/v1/cart/{Uri.EscapeDataString(cartCode)}/shipping", new { countryCode = address.CountryCode, postalCode = address.PostalCode, address = address.Address, city = address.City, state = address.StateProvince }, ct);
        using var document = await ProviderResponse.Read(response, "Shipping quote", ct);
        var root = document.RootElement;
        var options = new List<ShippingOptionDto>();
        if (root.TryGetProperty("shippingOptions", out var raw) && raw.ValueKind == JsonValueKind.Array)
            foreach (var option in raw.EnumerateArray())
                options.Add(new ShippingOptionDto { Code = JsonHelpers.String(option, "optionCode") ?? "", Name = JsonHelpers.String(option, "optionName"), Price = DtoMapper.Money(JsonHelpers.Decimal(option, "optionPrice")), Currency = "CAD" });
        return new ShippingFact(new ShippingSummaryDto
        {
            QuoteId = Guid.NewGuid().ToString(),
            ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(15).ToString("O"),
            ShippingRequired = true,
            Delivery = address,
            Shipping = DtoMapper.Money(JsonHelpers.Decimal(root, "shipping")),
            Handling = DtoMapper.Money(JsonHelpers.Decimal(root, "handling")),
            FreeShipping = JsonHelpers.Bool(root, "freeShipping"),
            TaxOnShipping = JsonHelpers.Bool(root, "taxOnShipping"),
            Options = options
        }, null);
    }
}

public sealed class TaxClient(HttpClient client)
{
    public async Task<decimal> QuoteAsync(string currency, AddressDto address, IEnumerable<CartLine> items, string? idempotencyKey, CancellationToken ct)
    {
        var billingAddress = new
        {
            firstName = address.FirstName,
            lastName = address.LastName,
            streetAddress = address.Address,
            city = address.City,
            stateProvince = address.StateProvince,
            countryCode = address.CountryCode,
            postalCode = address.PostalCode
        };
        var body = new { currencyCode = currency, billingAddress, items = items.Select(x => new { productId = x.ProductId.ToString(), sku = x.Sku, quantity = x.Quantity, unitPrice = x.UnitPrice, taxClassCode = (string?)null }), idempotencyKey };
        using var response = await client.PostAsJsonAsync("/api/v1/tax-calculations", body, ct);
        using var document = await ProviderResponse.Read(response, "Tax calculation", ct);
        return JsonHelpers.Decimal(document.RootElement, "totalTaxAmount", -1) is var tax and >= 0 ? tax : throw new DomainException("CHECKOUT_UNAVAILABLE", "Tax service did not return a total", 503);
    }
}

public sealed class PaymentClient(HttpClient client)
{
    public async Task<bool> IsActiveAsync(string module, CancellationToken ct)
    {
        using var response = await client.GetAsync($"/api/v1/payment-methods/{Uri.EscapeDataString(module)}", ct);
        if (response.StatusCode == HttpStatusCode.NotFound) return false;
        using var document = await ProviderResponse.Read(response, "Payment method lookup", ct);
        return JsonHelpers.Bool(document.RootElement, "active");
    }

    public async Task<string?> InitializeAsync(Guid checkoutSessionId, PaymentInitializationRequestDto request, string currency, string idempotencyKey, CancellationToken ct)
    {
        var body = new { checkoutSessionId = checkoutSessionId.ToString(), paymentMethodCode = request.PaymentModule, amount = request.Amount, currency, paymentToken = request.PaymentToken };
        using var message = new HttpRequestMessage(HttpMethod.Post, "/api/v1/payment-intents")
        {
            Content = JsonContent.Create(body)
        };
        message.Headers.TryAddWithoutValidation("Idempotency-Key", idempotencyKey);
        using var response = await client.SendAsync(message, ct);
        using var document = await ProviderResponse.Read(response, "Payment initialization", ct);
        return JsonHelpers.String(document.RootElement, "paymentIntentId");
    }
}
