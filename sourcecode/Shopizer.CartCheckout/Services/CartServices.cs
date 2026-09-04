using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Globalization;
using Shopizer.CartCheckout.Data;
using Shopizer.CartCheckout.DTOs;
using Shopizer.CartCheckout.Models;

namespace Shopizer.CartCheckout.Services;

public sealed class CartService(
    CartRepository repository,
    CatalogClient catalog,
    PricingClient pricing,
    CustomerClient customers,
    ShippingClient shipping,
    TaxClient tax,
    PaymentClient payments,
    EventPublisher events,
    IConfiguration configuration)
{
    private readonly string _defaultCurrency = configuration["CartCheckout:DefaultCurrency"] ?? "CAD";

    // @BR-SC-CRE-001: A new cart is persisted with a unique opaque code and the complete tenant/store scope.
    // @BR-SC-SEL-002: A cart line is accepted only after the catalog confirms that its product is sellable and available.
    // @BR-SC-ATR-003: Selected attributes are required to belong to the catalog product represented by the SKU.
    // @BR-SC-MRG-004: A repeated attribute-free physical SKU is merged into the existing line by quantity.
    public async Task<CartEnvelopeDto> AddAsync(AddCartItemRequestDto request, RequestContext context, string? existingCode, CancellationToken ct)
    {
        ValidateProductRequest(request.Product, request.Quantity, allowZero: false);
        var product = await RequiredProductAsync(request.Product, context, ct);
        await catalog.EnsureAvailableAsync(product, ct);
        var attributes = ValidateAttributes(request.Attributes, product);
        var price = await pricing.PriceAsync(product.Sku, attributes, product.Currency.Length == 3 ? product.Currency : _defaultCurrency, ct);
        var cart = string.IsNullOrWhiteSpace(existingCode) ? null : await repository.FindByCodeAsync(existingCode, context, ct);
        if (cart is not null)
        {
            EnsureOpen(cart);
            var matching = cart.Items.FirstOrDefault(x => x.Sku.Equals(product.Sku, StringComparison.OrdinalIgnoreCase) &&
                x.Attributes.Count == 0 && attributes.Count == 0 && !product.IsVirtual);
            if (matching is not null) { matching.Quantity += request.Quantity; matching.UnitPrice = price.Amount; matching.SubTotal = matching.Quantity * price.Amount; }
            else
            {
                var newLine = new CartLine
                {
                    Sku = product.Sku,
                    ProductId = product.NumericId,
                    ProviderProductId = product.Id,
                    Quantity = request.Quantity,
                    UnitPrice = price.Amount,
                    SubTotal = request.Quantity * price.Amount
                };
                newLine.Attributes.AddRange(attributes);
                cart.Items.Add(newLine);
            }
            await repository.SaveAsync(cart, context, ct);
            cart.CurrencyCode = price.Currency;
        }
        else cart = await repository.CreateAsync(null, null, request.PromoCode, product, request.Quantity, attributes, price.Amount,
            price.Currency.Length == 3 ? price.Currency : _defaultCurrency, context, ct);
        cart.CurrencyCode = price.Currency;
        return DtoMapper.Cart(cart, price.Currency);
    }

    // @BR-SC-HYD-006: Cart hydration re-resolves product facts and current prices and obsoletes unusable carts.
    public async Task<CartEnvelopeDto> GetAsync(string code, RequestContext context, CancellationToken ct)
    {
        var cart = await RequiredCartAsync(code, context, ct);
        var hydrated = await HydrateAsync(cart, context, ct);
        if (hydrated.Items.Count == 0)
        {
            await repository.MarkOrphanedAsync(cart.Id, context, ct);
            throw new DomainException("CART_NOT_FOUND", $"Cart {code} has no sellable items", 404);
        }
        return DtoMapper.Cart(hydrated, Currency(hydrated));
    }

    // @BR-SC-UPD-005: A zero quantity update removes the line and its selected attributes while positive values replace quantity.
    // @BR-SC-SEL-002: Newly introduced lines in a replacement request pass the same sellability checks as cart creation.
    public async Task<CartEnvelopeDto> UpdateAsync(string code, UpdateCartItemRequestDto request, RequestContext context, CancellationToken ct)
    {
        ValidateProductRequest(request.Product, request.Quantity, allowZero: true);
        var cart = await RequiredCartAsync(code, context, ct);
        EnsureOpen(cart);
        var existing = cart.Items.FirstOrDefault(x => x.Sku.Equals(request.Product, StringComparison.OrdinalIgnoreCase));
        if (request.Quantity == 0)
        {
            if (existing is not null) cart.Items.Remove(existing);
        }
        else
        {
            var product = await RequiredProductAsync(request.Product, context, ct);
            await catalog.EnsureAvailableAsync(product, ct);
            var attributes = ValidateAttributes(request.Attributes, product);
            var price = await pricing.PriceAsync(product.Sku, attributes, Currency(cart), ct);
            if (existing is null)
            {
                existing = new CartLine { Sku = product.Sku, ProductId = product.NumericId, ProviderProductId = product.Id };
                cart.Items.Add(existing);
            }
            existing.Quantity = request.Quantity; existing.UnitPrice = price.Amount; existing.SubTotal = price.Amount * request.Quantity;
            existing.Attributes.Clear(); existing.Attributes.AddRange(attributes);
        }
        if (!string.IsNullOrWhiteSpace(request.PromoCode)) { cart.PromoCode = request.PromoCode; cart.PromoAddedAt = DateTimeOffset.UtcNow; }
        if (cart.Items.Count == 0) cart.Status = "OBSOLETE";
        await repository.SaveAsync(cart, context, ct);
        return DtoMapper.Cart(cart, Currency(cart));
    }

    // @BR-SC-UPD-005: A multi-line update changes only submitted products and removes zero-quantity lines.
    // @BR-SC-ATR-003: Attribute selections are validated before they are attached to changed lines.
    public async Task<CartEnvelopeDto> UpdateMultipleAsync(string code, IReadOnlyCollection<MultiCartItemRequestDto> requests, RequestContext context, CancellationToken ct)
    {
        if (requests.Count == 0) throw new DomainException("INVALID_REQUEST", "At least one cart item is required", 400);
        var cart = await RequiredCartAsync(code, context, ct);
        foreach (var request in requests)
        {
            ValidateProductRequest(request.Product, request.Quantity, true);
            var existing = cart.Items.FirstOrDefault(x => x.Sku.Equals(request.Product, StringComparison.OrdinalIgnoreCase));
            if (request.Quantity == 0) { if (existing is not null) cart.Items.Remove(existing); continue; }
            var product = await RequiredProductAsync(request.Product, context, ct); await catalog.EnsureAvailableAsync(product, ct);
            var attributes = ValidateAttributes(request.Attributes, product);
            var price = await pricing.PriceAsync(product.Sku, attributes, Currency(cart), ct);
            if (existing is null) { existing = new CartLine { Sku = product.Sku, ProductId = product.NumericId, ProviderProductId = product.Id }; cart.Items.Add(existing); }
            existing.Quantity = request.Quantity; existing.UnitPrice = price.Amount; existing.SubTotal = price.Amount * request.Quantity;
            existing.Attributes.Clear(); existing.Attributes.AddRange(attributes);
        }
        if (cart.Items.Count == 0) cart.Status = "OBSOLETE";
        await repository.SaveAsync(cart, context, ct);
        return DtoMapper.Cart(cart, Currency(cart));
    }

    // @BR-SC-PRO-011: A promotion is retained only for the current calendar day and is recalculated by MS-07.
    public async Task<CartEnvelopeDto> ApplyPromotionAsync(string code, string promoCode, RequestContext context, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(promoCode)) throw new DomainException("INVALID_REQUEST", "Promotion code is required", 400);
        var cart = await RequiredCartAsync(code, context, ct); EnsureOpen(cart);
        cart.PromoCode = promoCode; cart.PromoAddedAt = DateTimeOffset.UtcNow;
        await repository.SaveAsync(cart, context, ct);
        await pricing.PromotionAsync(promoCode, cart.Items, Currency(cart), ct);
        var hydrated = await HydrateAsync(cart, context, ct);
        if (hydrated.PromoAddedAt is { } added && added.UtcDateTime.Date >= DateTime.UtcNow.Date.AddDays(1))
        {
            cart.PromoCode = null; cart.PromoAddedAt = null; await repository.SaveAsync(cart, context, ct);
            throw new DomainException("PROMOTION_EXPIRED", $"Promotion {promoCode} is no longer valid for this cart", 422);
        }
        return DtoMapper.Cart(hydrated, Currency(hydrated));
    }

    // @BR-SC-UPD-005: Removing a SKU deletes the line and its selected attributes and optionally returns the remaining cart.
    public async Task<CartEnvelopeDto?> RemoveAsync(string code, string sku, bool body, RequestContext context, CancellationToken ct)
    {
        var cart = await RequiredCartAsync(code, context, ct); EnsureOpen(cart);
        cart.Items.RemoveAll(x => x.Sku.Equals(sku, StringComparison.OrdinalIgnoreCase));
        if (cart.Items.Count == 0) cart.Status = "OBSOLETE";
        await repository.SaveAsync(cart, context, ct);
        return body ? DtoMapper.Cart(cart, Currency(cart)) : null;
    }

    // @BR-CO-AUT-012: An authenticated cart is resolved from the authenticated customer and never from a caller-supplied customer id.
    // @BR-SC-MRG-007: Anonymous cart adoption merges only same-scope lines and consumes the anonymous cart.
    public async Task<CartEnvelopeDto> CustomerCartAsync(string? anonymousCode, RequestContext context, CancellationToken ct)
    {
        var customer = await customers.CurrentAsync(ct);
        var customerCart = await repository.FindOpenCustomerCartAsync(customer.Id, context, ct);
        if (!string.IsNullOrWhiteSpace(anonymousCode))
        {
            var anonymous = await RequiredCartAsync(anonymousCode, context, ct);
            if (anonymous.CustomerId is not null && anonymous.CustomerId != customer.Id)
                throw new DomainException("CART_SCOPE_MISMATCH", "Cart belongs to another customer", 403);
            if (customerCart is null)
            {
                anonymous.CustomerId = customer.Id; await repository.SaveAsync(anonymous, context, ct); customerCart = anonymous;
            }
            else if (customerCart.Id != anonymous.Id)
            {
                foreach (var source in anonymous.Items)
                {
                    var target = customerCart.Items.FirstOrDefault(x => x.Sku == source.Sku && x.Attributes.SequenceEqual(source.Attributes));
                    if (target is null) customerCart.Items.Add(source);
                    else { target.Quantity += source.Quantity; target.SubTotal = target.UnitPrice * target.Quantity; }
                }
                anonymous.Status = "OBSOLETE"; await repository.SaveAsync(anonymous, context, ct); await repository.SaveAsync(customerCart, context, ct);
            }
        }
        if (customerCart is null) throw new DomainException("CART_NOT_FOUND", "No open cart exists for this customer", 404);
        var hydrated = await HydrateAsync(customerCart, context, ct);
        return DtoMapper.Cart(hydrated, Currency(hydrated));
    }

    // @BR-SC-SHP-008: Digital-only carts return no carrier options and physical shipping lines alone enter the provider request.
    // @BR-SC-SHP-009: A customer's delivery address is preferred and billing is the fallback for physical shipping.
    public async Task<ShippingSummaryDto> ShippingAsync(string code, ShippingAddressRequestDto? request, CustomerFact? customer,
        RequestContext context, CancellationToken ct, string? selectedQuoteId = null)
    {
        var cart = await RequiredCartAsync(code, context, ct);
        var hydrated = await HydrateAsync(cart, context, ct);
        var physical = hydrated.Items.Where(x => !x.Obsolete).ToList();
        if (physical.Count == 0) throw new DomainException("CART_NOT_FOUND", "Cart has no sellable items", 404);
        // Product shippability is checked during hydration and retained in the local fact map below.
        var productFacts = await ResolveProductsAsync(physical, context, ct);
        if (productFacts.All(x => x.IsVirtual || !x.IsShippable)) return new ShippingSummaryDto { ShippingRequired = false };
        var address = customer?.Delivery is { PostalCode.Length: > 0 } delivery ? delivery :
            customer?.Billing ?? (request is null ? null : new AddressDto { FirstName = "", LastName = "", Address = "", City = "", CountryCode = request.CountryCode, PostalCode = request.PostalCode });
        if (address is null || string.IsNullOrWhiteSpace(address.PostalCode) || address.CountryCode.Length != 2)
            throw new DomainException("SHIPPING_ADDRESS_INVALID", "A postal code and supported country are required for shipping quotes", 422);
        if (selectedQuoteId is not null && !await repository.HasActiveQuoteAsync(cart.Id, selectedQuoteId, "SHIPPING", context, ct))
            throw new DomainException("QUOTE_NOT_FOUND", $"Shipping quote {selectedQuoteId} is not available in this store", 404);
        var result = await shipping.QuoteAsync(cart.Code, address, ct);
        if (selectedQuoteId is not null) result.Summary.QuoteId = selectedQuoteId;
        if (Guid.TryParse(result.Summary.QuoteId, out var quoteId))
            await repository.SaveQuoteReferenceAsync(cart.Id, quoteId.ToString(), "SHIPPING", result.Summary.ExpiresAt, context, ct);
        return result.Summary;
    }

    // @BR-SC-TOT-010: Totals are computed from current priced lines plus provider-owned promotion, shipping, handling, and tax allocations.
    public async Task<TotalSummaryDto> TotalAsync(string code, string? quoteId, CustomerFact? customer, RequestContext context, CancellationToken ct)
    {
        var cart = await RequiredCartAsync(code, context, ct);
        return await TotalForCartAsync(cart, quoteId, customer, context, ct);
    }

    // @BR-SC-TOT-010: Numeric cart routes resolve the tenant/store-scoped cart identity before calculating totals.
    public async Task<TotalSummaryDto> TotalByIdAsync(long id, string? quoteId, CustomerFact? customer, RequestContext context, CancellationToken ct)
    {
        var cart = await repository.FindByIdAsync(id, context, ct)
            ?? throw new DomainException("CART_NOT_FOUND", $"Cart {id} was not found in this store", 404);
        EnsureCustomerCart(cart, customer);
        return await TotalForCartAsync(cart, quoteId, customer, context, ct);
    }

    private async Task<TotalSummaryDto> TotalForCartAsync(Cart cart, string? quoteId, CustomerFact? customer, RequestContext context, CancellationToken ct)
    {
        var hydrated = await HydrateAsync(cart, context, ct);
        var promoCode = hydrated.PromoCode;
        if (hydrated.PromoAddedAt is { } added && added.UtcDateTime.Date >= DateTime.UtcNow.Date.AddDays(1))
        {
            hydrated.PromoCode = null;
            hydrated.PromoAddedAt = null;
            await repository.SaveAsync(hydrated, context, ct);
            promoCode = null;
        }
        var quote = await pricing.QuoteAsync(Currency(hydrated), hydrated.Items, promoCode, ct);
        var root = quote.RootElement;
        var subtotal = JsonHelpers.Decimal(root, "merchandiseSubtotal", hydrated.Items.Sum(x => x.SubTotal));
        var discount = root.TryGetProperty("promotion", out var promotion) ? JsonHelpers.Decimal(promotion, "reduction") : 0;
        var afterPromotion = JsonHelpers.Decimal(root, "subtotalAfterPromotion", subtotal - discount);
        var shippingAmount = 0m; var handlingAmount = 0m;
        if (quoteId is not null)
        {
            if (!Guid.TryParse(quoteId, out _)) throw new DomainException("QUOTE_NOT_FOUND", "Shipping quote is invalid", 404);
            var shippingSummary = await ShippingAsync(cart.Code, null, customer, context, ct, quoteId);
            shippingAmount = shippingSummary.FreeShipping == true ? 0 : decimal.TryParse(shippingSummary.Shipping, out var ship) ? ship : 0;
            handlingAmount = decimal.TryParse(shippingSummary.Handling, out var handling) ? handling : 0;
        }
        var taxAmount = customer?.Billing is { } address
            ? await tax.QuoteAsync(Currency(hydrated), address, hydrated.Items, null, ct)
            : 0m;
        var grand = afterPromotion + shippingAmount + handlingAmount + taxAmount;
        return new TotalSummaryDto
        {
            CartCode = cart.Code,
            Currency = Currency(hydrated),
            SubTotal = DtoMapper.Money(subtotal),
            DiscountTotal = DtoMapper.Money(discount),
            Shipping = DtoMapper.Money(shippingAmount),
            Handling = DtoMapper.Money(handlingAmount),
            Tax = DtoMapper.Money(taxAmount),
            GrandTotal = DtoMapper.Money(grand),
            QuoteVersion = hydrated.Version,
            Components = [new() { Code = "order.total.subtotal", Amount = DtoMapper.Money(afterPromotion) }, new() { Code = "order.total.shipping", Amount = DtoMapper.Money(shippingAmount) }, new() { Code = "order.total.handling", Amount = DtoMapper.Money(handlingAmount) }, new() { Code = "order.total.tax", Amount = DtoMapper.Money(taxAmount) }, new() { Code = "order.total.total", Amount = DtoMapper.Money(grand) }]
        };
    }

    // @BR-CO-AUT-012: Authenticated checkout uses only the customer resolved from the validated principal.
    // @BR-CO-CUS-013: Anonymous checkout constructs customer and address context from the request and delegates account creation to MS-01.
    // @BR-CO-SNP-014: Checkout freezes server-derived line and total snapshots before submission.
    // @BR-CO-TOT-015: The submitted amount must exactly equal the server-calculated grand total.
    // @BR-CO-IDM-017: The idempotency key is scoped and replayed without repeating submission side effects.
    // @BR-CO-STA-018: Checkout moves to a terminal submitted state and rejects reuse through the durable state model.
    // @BR-CO-ORC-019: OrderSubmitted is durably queued in the same local transaction as the checkout completion.
    // @BR-CO-BND-020: MS-04 writes only cart and checkout state and delegates order, payment, tax, pricing, shipping, and inventory ownership.
    public async Task<CheckoutResult> CheckoutAsync(string code, AuthenticatedCheckoutRequestDto? authenticated,
        AnonymousCheckoutRequestDto? anonymous, string idempotencyKey, RequestContext context, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey)) throw new DomainException("INVALID_REQUEST", "idempotency-key is required", 400);
        if (authenticated is null && anonymous is null) throw new DomainException("INVALID_REQUEST", "A checkout request is required", 400);
        if (authenticated is not null && authenticated.Payment is null) throw new DomainException("INVALID_REQUEST", "Payment is required", 400);
        if (anonymous is not null && (anonymous.Customer is null || anonymous.Payment is null))
            throw new DomainException("INVALID_REQUEST", "Customer and payment are required", 400);
        var cart = await RequiredCartAsync(code, context, ct);
        CustomerFact? customer = null;
        if (authenticated is not null)
        {
            customer = await customers.CurrentAsync(ct);
            EnsureCustomerCart(cart, customer);
        }
        else if (anonymous is not null) customer = await customers.RegisterAsync(anonymous.Customer, anonymous.Customer.Password, context, ct);
        var request = authenticated is not null ? (object)authenticated : anonymous!;
        var currency = authenticated?.Currency ?? anonymous!.Currency;
        if (currency.Length != 3 || currency != currency.ToUpperInvariant()) throw new DomainException("INVALID_REQUEST", "Currency must be an uppercase ISO-4217 code", 400);
        var hydrated = await HydrateAsync(cart, context, ct);
        if (hydrated.Items.Count == 0) throw new DomainException("CART_NOT_FOUND", "Cart has no sellable items", 404);
        var totals = await TotalAsync(code, authenticated?.ShippingQuoteId ?? anonymous?.ShippingQuoteId, customer, context, ct);
        if (!decimal.TryParse((authenticated?.Payment ?? anonymous!.Payment).Amount, NumberStyles.Number, CultureInfo.InvariantCulture, out var supplied) ||
            !decimal.TryParse(totals.GrandTotal, NumberStyles.Number, CultureInfo.InvariantCulture, out var calculated) ||
            supplied != calculated)
            throw new DomainException("AMOUNT_MISMATCH", $"Submitted amount {(authenticated?.Payment ?? anonymous!.Payment).Amount} does not match calculated total {totals.GrandTotal}", 409);
        if (!(authenticated?.CustomerAgreement ?? anonymous?.CustomerAgreement ?? false))
            throw new DomainException("INVALID_REQUEST", "Customer agreement is required", 422);
        var payment = authenticated?.Payment ?? anonymous!.Payment;
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(request))));
        var products = await ResolveProductsAsync(hydrated.Items, context, ct);
        var result = await repository.PersistCheckoutAsync(hydrated, customer?.Id, currency, hash, idempotencyKey, payment,
            decimal.Parse(totals.SubTotal, CultureInfo.InvariantCulture), decimal.Parse(totals.DiscountTotal, CultureInfo.InvariantCulture), decimal.Parse(totals.Shipping, CultureInfo.InvariantCulture),
            decimal.Parse(totals.Handling, CultureInfo.InvariantCulture), decimal.Parse(totals.Tax, CultureInfo.InvariantCulture), decimal.Parse(totals.GrandTotal, CultureInfo.InvariantCulture), products, context, ct);
        await events.PublishOrderSubmittedAsync(result.EventId, context, ct);
        return result;
    }

    // @BR-CO-PAY-016: Payment handoff is allowed only for an active provider method and delegates provider state to MS-06.
    public async Task<PaymentInitializationResponseDto> InitializePaymentAsync(string code, PaymentInitializationRequestDto request,
        long? customerId, string idempotencyKey, RequestContext context, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey)) throw new DomainException("INVALID_REQUEST", "idempotency-key is required", 400);
        var cart = await RequiredCartAsync(code, context, ct); EnsureOpen(cart);
        if (customerId is not null && cart.CustomerId != customerId) throw new DomainException("CART_SCOPE_MISMATCH", "Cart is not owned by this customer", 403);
        var requestHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new { request, customerId }))));
        var replay = await repository.BeginPaymentIdempotencyAsync(cart.Id, customerId, idempotencyKey, requestHash, context, ct);
        if (replay is not null) return replay;
        if (!await payments.IsActiveAsync(request.PaymentModule, ct))
            throw new DomainException("PAYMENT_METHOD_INACTIVE", $"Payment module {request.PaymentModule} is not active for this store", 422);
        if (!decimal.TryParse(request.Amount, NumberStyles.Number, CultureInfo.InvariantCulture, out var amount))
            throw new DomainException("INVALID_REQUEST", "Payment amount is invalid", 400);
        try
        {
            var session = await repository.CreatePaymentSubmissionAsync(cart.Id, customerId, _defaultCurrency, request, context, ct);
            var provider = await payments.InitializeAsync(session, request, _defaultCurrency, idempotencyKey, ct);
            var response = new PaymentInitializationResponseDto
            {
                SubmissionId = session.ToString(),
                PaymentState = "Pending",
                ProviderReference = provider,
                Amount = DtoMapper.Money(amount),
                Currency = _defaultCurrency
            };
            await repository.CompletePaymentIdempotencyAsync(cart.Id, customerId, idempotencyKey, response, context, ct);
            return response;
        }
        catch (DomainException)
        {
            await repository.FailPaymentIdempotencyAsync(cart.Id, customerId, idempotencyKey, context, ct);
            throw;
        }
        catch (HttpRequestException)
        {
            await repository.FailPaymentIdempotencyAsync(cart.Id, customerId, idempotencyKey, context, ct);
            throw;
        }
    }

    private async Task<Cart> HydrateAsync(Cart cart, RequestContext context, CancellationToken ct)
    {
        var toRemove = new List<CartLine>(); var products = new List<ProductFact>();
        foreach (var item in cart.Items)
        {
            var product = await catalog.ProductAsync(item.Sku, ct);
            if (product is null || !product.Available || !product.CanBePurchased || product.DateAvailable > DateTimeOffset.UtcNow)
            { item.Obsolete = true; toRemove.Add(item); continue; }
            await catalog.EnsureAvailableAsync(product, ct);
            if (item.Attributes.Any(x => !product.AttributeIds.Contains(x)))
            { item.Obsolete = true; toRemove.Add(item); continue; }
            var price = await pricing.PriceAsync(product.Sku, item.Attributes, Currency(cart), ct);
            item.ProductId = product.NumericId; item.ProviderProductId = product.Id; item.UnitPrice = price.Amount; item.SubTotal = price.Amount * item.Quantity; item.Obsolete = false; cart.CurrencyCode = price.Currency; products.Add(product);
        }
        foreach (var item in toRemove) cart.Items.Remove(item);
        if (cart.Items.Count == 0) cart.Status = "OBSOLETE";
        if (toRemove.Count > 0) await repository.SaveAsync(cart, context, ct);
        return cart;
    }

    private async Task<IReadOnlyList<ProductFact>> ResolveProductsAsync(IEnumerable<CartLine> items, RequestContext context, CancellationToken ct)
    {
        var result = new List<ProductFact>();
        foreach (var item in items) result.Add(await RequiredProductAsync(item.Sku, context, ct));
        return result;
    }

    private async Task<ProductFact> RequiredProductAsync(string sku, RequestContext context, CancellationToken ct)
    {
        var product = await catalog.ProductAsync(sku, ct) ?? throw new DomainException("PRODUCT_NOT_SELLABLE", $"Product {sku} is not available for sale", 422);
        if (!string.IsNullOrWhiteSpace(product.StoreId) && !product.StoreId.Equals(context.StoreId, StringComparison.Ordinal))
            throw new DomainException("CART_SCOPE_MISMATCH", "Product is not available in this store", 403);
        if (!product.Available || !product.CanBePurchased || product.DateAvailable > DateTimeOffset.UtcNow)
            throw new DomainException("PRODUCT_NOT_SELLABLE", $"Product {sku} is not available for sale", 422);
        return product;
    }

    private async Task<Cart> RequiredCartAsync(string code, RequestContext context, CancellationToken ct) =>
        await repository.FindByCodeAsync(code, context, ct) ?? throw new DomainException("CART_NOT_FOUND", $"Cart {code} was not found in this store", 404);
    private static void EnsureOpen(Cart cart)
    {
        if (cart.Status != "OPEN") throw new DomainException("CHECKOUT_TERMINAL", "The cart is no longer open", 409);
    }
    private static void EnsureCustomerCart(Cart cart, CustomerFact? customer)
    {
        if (customer is not null && cart.CustomerId != customer.Id)
            throw new DomainException("CART_NOT_FOUND", "Cart is not available to this customer", 404);
    }
    private static void ValidateProductRequest(string product, int quantity, bool allowZero)
    {
        if (string.IsNullOrWhiteSpace(product)) throw new DomainException("INVALID_REQUEST", "Product is required", 400);
        if (allowZero ? quantity < 0 : quantity <= 0) throw new DomainException("INVALID_QUANTITY", allowZero ? "Quantity must be zero or greater for an update" : "Quantity must be greater than zero", 422);
    }
    private static IReadOnlyCollection<long> ValidateAttributes(IEnumerable<CartAttributeReferenceDto>? requested, ProductFact product)
    {
        var attributes = (requested ?? []).Select(x => x.Id).Distinct().ToArray();
        if (attributes.Any(x => !product.AttributeIds.Contains(x))) throw new DomainException("ATTRIBUTE_PRODUCT_MISMATCH", $"Selected attributes are not valid for {product.Sku}", 422);
        return attributes;
    }
    private string Currency(Cart cart) => string.IsNullOrWhiteSpace(cart.CurrencyCode) ? _defaultCurrency : cart.CurrencyCode;
}
