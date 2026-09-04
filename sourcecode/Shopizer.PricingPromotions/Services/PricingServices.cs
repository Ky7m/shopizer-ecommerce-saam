using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Shopizer.PricingPromotions.Data;
using Shopizer.PricingPromotions.DTOs;
using Shopizer.PricingPromotions.Models;

namespace Shopizer.PricingPromotions.Services;

public sealed record PricingToken(Guid SubjectId, string Kind, string Login, string TenantId, string StoreId,
    DateTimeOffset ExpiresAt, IReadOnlyList<string> Roles);

public sealed class TokenService(IConfiguration configuration, IHostEnvironment environment)
{
    private readonly byte[] _secret = CreateSecret(configuration, environment);

    private static byte[] CreateSecret(IConfiguration configuration, IHostEnvironment environment)
    {
        var configured = configuration["PricingPromotions:JwtSecret"];
        if (!string.IsNullOrWhiteSpace(configured)) return Encoding.UTF8.GetBytes(configured);
        if (!environment.IsDevelopment())
            throw new InvalidOperationException("PricingPromotions:JwtSecret must be configured outside Development.");
        return RandomNumberGenerator.GetBytes(64);
    }

    public Task<PricingToken?> ValidateAsync(string raw, RequestContext context, CancellationToken ct)
    {
        try
        {
            var pieces = raw.Split('.');
            if (pieces.Length != 3) return Task.FromResult<PricingToken?>(null);
            using var hmac = new HMACSHA512(_secret);
            var expected = hmac.ComputeHash(Encoding.UTF8.GetBytes($"{pieces[0]}.{pieces[1]}"));
            if (!CryptographicOperations.FixedTimeEquals(expected, FromBase64Url(pieces[2])))
                return Task.FromResult<PricingToken?>(null);
            using var document = JsonDocument.Parse(Encoding.UTF8.GetString(FromBase64Url(pieces[1])));
            var root = document.RootElement;
            if (!root.TryGetProperty("aud", out var audience) || audience.GetString() != "api")
                return Task.FromResult<PricingToken?>(null);
            var subject = Guid.Parse(root.GetProperty("sub").GetString()!);
            var tenant = root.GetProperty("tenantId").GetString()!;
            var store = root.GetProperty("storeId").GetString()!;
            var expiry = DateTimeOffset.FromUnixTimeSeconds(root.GetProperty("exp").GetInt64());
            if (!tenant.Equals(context.TenantId, StringComparison.Ordinal) ||
                !store.Equals(context.StoreId, StringComparison.Ordinal) ||
                expiry <= DateTimeOffset.UtcNow)
                return Task.FromResult<PricingToken?>(null);
            var roles = root.TryGetProperty("roles", out var roleJson)
                ? roleJson.EnumerateArray().Select(x => x.GetString() ?? "").ToArray()
                : Array.Empty<string>();
            return Task.FromResult<PricingToken?>(new PricingToken(subject,
                root.GetProperty("kind").GetString()!, root.GetProperty("name").GetString()!,
                tenant, store, expiry, roles));
        }
        catch (FormatException) { return Task.FromResult<PricingToken?>(null); }
        catch (JsonException) { return Task.FromResult<PricingToken?>(null); }
        catch (KeyNotFoundException) { return Task.FromResult<PricingToken?>(null); }
        catch (CryptographicException) { return Task.FromResult<PricingToken?>(null); }
    }

    private static byte[] FromBase64Url(string value) =>
        Convert.FromBase64String(value.Replace('-', '+').Replace('_', '/') +
                                 new string('=', (4 - value.Length % 4) % 4));
}

public sealed class PricingService(
    PricingRepository repository,
    EventPublisher events,
    IConfiguration configuration)
{
    private readonly string _currency = configuration["PricingPromotions:DefaultCurrency"] ?? "USD";

    // @BR-PRC-001: A newly created price is stored in the tenant/store price scope for its opaque catalog availability.
    // @BR-PRC-002: Price creation preserves the default-price designation and price type.
    public async Task<PriceCreatedResponseDto> CreatePriceAsync(
        string sku, long? pathAvailabilityId, AvailabilityPriceCreateRequestDto? availabilityRequest,
        ProductPriceCreateRequestDto? productRequest, RequestContext context, CancellationToken ct)
    {
        var request = (object?)availabilityRequest ?? productRequest
            ?? throw new DomainException("INVALID_REQUEST", "A price request body is required", 400);
        var availabilityId = pathAvailabilityId ?? productRequest!.AvailabilityId;
        var (code, amount, priceType, defaultPrice, start, end, special, identifier) =
            request switch
            {
                AvailabilityPriceCreateRequestDto a => (a.Code, a.Amount, a.PriceType, a.DefaultPrice,
                    a.SpecialStartDate, a.SpecialEndDate, a.SpecialAmount, a.ProductIdentifierId),
                ProductPriceCreateRequestDto p => (p.Code, p.Amount, p.PriceType, p.DefaultPrice,
                    p.SpecialStartDate, p.SpecialEndDate, p.SpecialAmount, p.ProductIdentifierId),
                _ => throw new DomainException("INVALID_REQUEST", "The price request body is invalid", 400)
            };
        ValidatePrice(code, amount, priceType, start, end, special, identifier, availabilityId);
        var price = NewPrice(sku, availabilityId, code, amount, priceType, defaultPrice,
            start, end, special, identifier);
        var created = await repository.CreatePriceAsync(price, context, _currency, ct);
        await events.PublishPriceChangedAsync(created.Price, "Created", created.EventId, context, ct);
        return new PriceCreatedResponseDto
        {
            Id = created.Price.Id.ToString(),
            LegacyPriceId = created.Price.LegacyPriceId,
            ProductSku = created.Price.ProductSku,
            AvailabilityId = created.Price.AvailabilityId!.Value
        };
    }

    // @BR-PRC-002: An updated price remains inside its tenant/store scope and keeps a single deterministic default.
    // @BR-PRC-003: Updated special-price windows are validated before persistence.
    public async Task<PriceDto> UpdatePriceAsync(
        string sku, long availabilityId, Guid priceId, PriceUpdateRequestDto request,
        RequestContext context, CancellationToken ct)
    {
        ValidatePrice(request.Code, request.Amount, request.PriceType, request.SpecialStartDate,
            request.SpecialEndDate, request.SpecialAmount, request.ProductIdentifierId, availabilityId);
        var update = NewPrice(sku, availabilityId, request.Code, request.Amount, request.PriceType,
            request.DefaultPrice, request.SpecialStartDate, request.SpecialEndDate,
            request.SpecialAmount, request.ProductIdentifierId) with
        { Id = priceId };
        var saved = await repository.UpdatePriceAsync(update, sku, context, ct);
        await events.PublishPriceChangedAsync(saved.Price, "Updated", saved.EventId, context, ct);
        return DtoMapper.Price(await CalculateSingleAsync(saved.Price, DateTimeOffset.UtcNow, context, ct));
    }

    // @BR-PRC-002: A product price is read only inside the requested tenant/store and product boundary.
    public async Task<PriceDto> GetPriceAsync(
        string sku, Guid priceId, RequestContext context, CancellationToken ct)
    {
        var price = await repository.FindPriceAsync(priceId, sku, context, ct)
            ?? throw new DomainException("PRICE_NOT_FOUND", "Price was not found for this product and store", 404);
        return DtoMapper.Price(await CalculateSingleAsync(price, DateTimeOffset.UtcNow, context, ct));
    }

    // @BR-PRC-001: Availability price listings are limited to the requested opaque availability and scope.
    // @BR-PRC-002: List results expose calculated primary/additional price state for every stored entry.
    public async Task<PriceListResponseDto> ListPricesAsync(
        string sku, long? availabilityId, RequestContext context, CancellationToken ct)
    {
        var prices = await repository.ListPricesAsync(sku, availabilityId, context, ct);
        var result = new PriceListResponseDto();
        foreach (var price in prices)
            result.Items.Add(DtoMapper.Price(await CalculateSingleAsync(price, DateTimeOffset.UtcNow, context, ct)));
        return result;
    }

    // @BR-PRC-002: Deletion removes the scoped price and publishes the corresponding price change.
    public async Task DeletePriceAsync(string sku, Guid priceId, RequestContext context, CancellationToken ct)
    {
        var deleted = await repository.DeletePriceAsync(priceId, sku, context, ct);
        await events.PublishPriceChangedAsync(new PriceEntry
        {
            Id = priceId,
            ProductSku = sku,
            VariantSku = null,
            Amount = 0,
            PriceType = "OneTime",
            DefaultPrice = false,
            Currency = _currency
        }, "Deleted", deleted.EventId, context, ct);
    }

    // @BR-PRC-001: Product pricing gives a selected default variant first refusal and falls back to product availability data.
    // @BR-PRC-002: Default price is primary while non-default prices remain additional lines.
    // @BR-PRC-003: Active special-price windows determine the effective amount.
    // @BR-PRC-004: Discount metadata is calculated from the original amount using truncated percentage arithmetic.
    // @BR-PRC-006: Customer identity does not alter standard product price selection.
    public async Task<ProductPriceCalculationResponseDto> CalculateProductPriceAsync(
        string sku, string? evaluationAt, bool includeAdditionalPrices, RequestContext context, CancellationToken ct)
    {
        var at = ParseEvaluationAt(evaluationAt);
        var calculated = await CalculateProductCoreAsync(sku, null, [], at, "Standard", context, ct);
        return ToCalculationResponse(calculated, includeAdditionalPrices, false, "Standard");
    }

    // @BR-PRC-005: Positive selected attribute adjustments are summed and applied to the calculated product price.
    // @BR-PRC-006: An optional customer identifier is accepted as context without applying customer-specific pricing.
    public async Task<ProductPriceCalculationResponseDto> QuoteProductPriceAsync(
        string sku, ProductQuoteRequestDto request, RequestContext context, CancellationToken ct)
    {
        var at = ParseEvaluationAt(request.EvaluationAt);
        var calculated = await CalculateProductCoreAsync(sku, null, request.Attributes ?? [], at,
            "Standard", context, ct);
        return ToCalculationResponse(calculated, true, false, "Standard");
    }

    // @BR-PRC-001: Direct variant calculation evaluates the requested variant's usable prices before any fallback.
    // @BR-PRC-007: Variant requests either use direct pricing or explicitly fall back to the parent product.
    public async Task<ProductPriceCalculationResponseDto> QuoteVariantPriceAsync(
        string variantSku, VariantQuoteRequestDto request, RequestContext context, CancellationToken ct)
    {
        if (request.FallbackMode is not ("DirectOnly" or "ParentProduct"))
            throw new DomainException("INVALID_FALLBACK_MODE", "fallbackMode must be DirectOnly or ParentProduct", 422);
        var at = ParseEvaluationAt(request.EvaluationAt);
        var direct = await TryCalculateVariantAsync(variantSku, request.ParentProductSku, at, context, ct);
        if (direct is not null)
            return ToCalculationResponse(direct, true, false, "Variant");
        if (request.FallbackMode == "ParentProduct" && !string.IsNullOrWhiteSpace(request.ParentProductSku))
        {
            var fallback = await CalculateProductCoreAsync(request.ParentProductSku, null, [], at,
                "ParentProductFallback", context, ct);
            return ToCalculationResponse(fallback with { SelectedVariantSku = variantSku },
                true, false, "ParentProductFallback");
        }
        throw new DomainException("VARIANT_PRICE_UNAVAILABLE",
            "No usable variant price is available and parent-product fallback is disabled", 404);
    }

    // @BR-PRC-008: Promotion evaluation uses only the active promotion-code processor.
    // @BR-PRC-009: A matched promotion rate is multiplied by each effective item price and quantity.
    // @BR-PRC-010: Promotion and coupon validity windows are enforced by the scoped rule lookup.
    // @BR-PRC-011: Promotion reductions are returned as positive values for the consumer to subtract.
    // @BR-PRC-012: Manufacturer and shipping-code processing is not registered or applied.
    public async Task<PromotionEvaluationResponseDto> EvaluatePromotionAsync(
        PromotionEvaluationRequestDto request, RequestContext context, CancellationToken ct)
    {
        var at = ParseEvaluationAt(request.EvaluationAt);
        var currency = _currency;
        return await EvaluatePromotionCoreAsync(request.PromoCode, request.Items, currency, at, context, ct);
    }

    // @BR-PRC-008: The operational registry exposes promotion-code processing and the inactive extracted processor.
    // @BR-PRC-012: Manufacturer and shipping-code discounts remain explicitly inactive.
    public Task<ProcessorRegistryResponseDto> GetProcessorsAsync()
    {
        return Task.FromResult(new ProcessorRegistryResponseDto
        {
            Processors =
            [
                new ProcessorDto { Code = "PROMO_CODE", Name = "Promotion code evaluator", Active = true }
            ],
            Inactive =
            [
                new InactiveProcessorDto
                {
                    Code = "MANUFACTURER_SHIPPING_CODE",
                    Name = "Manufacturer and shipping-code discount",
                    Active = false,
                    Reason = "NOT_REGISTERED"
                }
            ]
        });
    }

    // @BR-PRC-002: Merchandise subtotal is assembled from effective item prices and one-time additional lines.
    // @BR-PRC-009: Promotion evaluation occurs after item pricing and before downstream components.
    // @BR-PRC-011: Positive promotion reductions are subtracted from the merchandise subtotal.
    // @BR-PRC-013: Shipping, handling, tax, and grand-total ownership remain downstream.
    public async Task<PricingQuoteResponseDto> CalculateQuoteAsync(
        PricingQuoteRequestDto request, RequestContext context, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Currency) ||
            !request.Currency.Equals(request.Currency.ToUpperInvariant(), StringComparison.Ordinal) ||
            request.Currency.Length != 3)
            throw new DomainException("INVALID_CURRENCY", "currency must be a three-letter uppercase code", 422);
        if (request.Items is null || request.Items.Count == 0)
            throw new DomainException("INVALID_ITEMS", "At least one pricing item is required", 422);
        var at = ParseEvaluationAt(request.EvaluationAt);
        var items = new List<PricedItemDto>();
        var additionalTotals = new Dictionary<(string Code, string Type), decimal>();
        var merchandiseSubtotal = 0m;
        foreach (var item in request.Items)
        {
            ValidateItem(item);
            var calculated = await CalculateProductCoreAsync(item.ProductSku, item.VariantSku,
                item.Attributes ?? [], at, item.VariantSku is null ? "Standard" : "Variant", context, ct);
            var additional = calculated.AdditionalPrices();
            var lineSubtotal = calculated.FinalPrice * item.Quantity;
            merchandiseSubtotal += lineSubtotal;
            foreach (var line in additional)
            {
                if (line.Source.PriceType == "OneTime")
                    merchandiseSubtotal += line.FinalPrice;
                var key = (line.Source.Code, line.Source.PriceType);
                additionalTotals[key] = additionalTotals.GetValueOrDefault(key) + line.FinalPrice;
            }
            items.Add(new PricedItemDto
            {
                ProductSku = item.ProductSku,
                VariantSku = item.VariantSku,
                Quantity = item.Quantity,
                UnitPrice = Round(calculated.FinalPrice),
                LineSubtotal = Round(lineSubtotal),
                AdditionalPrices = additional.Select(x => DtoMapper.Additional(x.Source, x)).ToList()
            });
        }
        var promotion = await EvaluatePromotionFromPricesAsync(request.PromoCode, request.Items, items,
            request.Currency, at, context, ct);
        var subtotal = Round(merchandiseSubtotal);
        var afterPromotion = Math.Max(0, Round(subtotal - promotion.Reduction));
        return new PricingQuoteResponseDto
        {
            Currency = request.Currency,
            Items = items,
            AdditionalPriceLines = additionalTotals.Select(x => new AdditionalPriceLineDto
            {
                Code = x.Key.Code,
                PriceType = x.Key.Type,
                FinalPrice = Round(x.Value)
            }).ToList(),
            MerchandiseSubtotal = subtotal,
            Promotion = new PricingQuotePromotionDto
            {
                PromoCode = request.PromoCode ?? "",
                Matched = promotion.Matched,
                Reduction = promotion.Reduction
            },
            SubtotalAfterPromotion = afterPromotion,
            DownstreamComponents = ["shipping", "handling", "tax"],
            GrandTotalOwnedBy = "consumer"
        };
    }

    // @BR-PRC-003: Special-price windows use strict open/bounded date comparisons and never activate future or expired prices.
    // @BR-PRC-004: Discount percentage uses the original amount and truncates toward zero.
    private static async Task<CalculatedPrice> CalculateSingleAsync(
        PriceEntry price, DateTimeOffset at, RequestContext context, CancellationToken ct)
    {
        var (final, discounted, discountedPrice, percent, endDate) = CalculateEffective(price, at, false);
        await Task.CompletedTask;
        return new CalculatedPrice(price, price.Amount, final, discounted, discountedPrice, percent,
            endDate, "Product", price.VariantSku, 0, []);
    }

    // @BR-PRC-001: The selected variant's price rows are preferred, with product rows as the deterministic fallback.
    // @BR-PRC-002: The default row is selected as primary and all other rows are retained as additional prices.
    private async Task<CalculatedPrice> CalculateProductCoreAsync(
        string sku, string? requestedVariant, IReadOnlyList<PricingAttributeDto> attributes,
        DateTimeOffset at, string basis, RequestContext context, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(sku))
            throw new DomainException("PRODUCT_NOT_FOUND", "Product SKU is required", 404);
        ValidateAttributes(attributes);
        var rows = await repository.ListCalculationPricesAsync(sku, requestedVariant, context, ct);
        if (rows.Count == 0)
            throw new DomainException("PRICE_UNAVAILABLE",
                $"No usable wildcard-region price is available for product {sku}", 404);
        var selectedVariant = requestedVariant;
        if (requestedVariant is null)
        {
            selectedVariant = rows.Where(x => x.VariantSku is not null && x.DefaultPrice)
                .Select(x => x.VariantSku).FirstOrDefault();
            if (selectedVariant is not null)
                rows = rows.Where(x => x.VariantSku == selectedVariant).ToList();
            else
                rows = rows.Where(x => x.VariantSku is null).ToList();
        }
        if (rows.Count == 0)
            throw new DomainException("PRICE_UNAVAILABLE",
                $"No usable wildcard-region price is available for product {sku}", 404);
        var calculatedRows = rows.Select(x => CalculateEffective(x, at)).ToArray();
        var primaryIndex = rows.FindIndex(x => x.DefaultPrice);
        if (primaryIndex < 0) primaryIndex = 0;
        var primaryRow = rows[primaryIndex];
        var primaryValue = calculatedRows[primaryIndex];
        var adjustment = attributes.Sum(x => x.PriceAdjustment > 0 ? x.PriceAdjustment : 0);
        var original = primaryRow.Amount + adjustment;
        var final = primaryValue.Final + adjustment;
        decimal? discounted = primaryValue.DiscountedPrice;
        if (discounted.HasValue) discounted += adjustment;
        var additional = rows
            .Select((row, index) => (row, index))
            .Where(x => x.index != primaryIndex)
            .Select(x =>
            {
                var value = calculatedRows[x.index];
                return new CalculatedPrice(x.row, x.row.Amount, value.Final, value.Discounted,
                    value.DiscountedPrice, value.Percent, value.EndDate,
                    selectedVariant is null ? "Product" : "Variant", selectedVariant, 0, []);
            }).ToList();
        var result = new CalculatedPrice(primaryRow, original, final, primaryValue.Discounted,
            discounted, primaryValue.Percent, primaryValue.EndDate,
            selectedVariant is null ? "Product" : "Variant", selectedVariant, adjustment, additional);
        return result;
    }

    // @BR-PRC-007: Direct variant requests do not silently become null; they return a calculated variant or no result.
    private async Task<CalculatedPrice?> TryCalculateVariantAsync(
        string variantSku, string? parentProductSku, DateTimeOffset at,
        RequestContext context, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(parentProductSku)) return null;
        try
        {
            return await CalculateProductCoreAsync(parentProductSku, variantSku, [], at, "Variant", context, ct);
        }
        catch (DomainException ex) when (ex.Code == "PRICE_UNAVAILABLE")
        {
            return null;
        }
    }

    // @BR-PRC-003: Special amounts are active only for the specified open or bounded window.
    // @BR-PRC-004: Discount percentages are based on the non-adjusted original amount.
    private static (decimal Final, bool Discounted, decimal? DiscountedPrice, int Percent, DateOnly? EndDate)
        CalculateEffective(PriceEntry price, DateTimeOffset at, bool rejectInactive = true)
    {
        var date = DateOnly.FromDateTime(at.UtcDateTime);
        var active = price.SpecialAmount is > 0 &&
                     ((price.SpecialStartDate is null && price.SpecialEndDate is null) ||
                      (price.SpecialStartDate is null && price.SpecialEndDate > date) ||
                      (price.SpecialStartDate < date && price.SpecialEndDate is not null && price.SpecialEndDate > date));
        if (rejectInactive && price.SpecialAmount is > 0 &&
            (price.SpecialStartDate is not null || price.SpecialEndDate is not null) && !active)
            throw new DomainException("SPECIAL_PRICE_NOT_ACTIVE",
                "The special price window is not active at the requested evaluation time", 422);
        if (!active) return (price.Amount, false, null, 0, null);
        if (price.Amount == 0)
            throw new DomainException("INVALID_DISCOUNT_BASE",
                "An active discount requires a non-zero original amount", 422);
        var raw = 100m - price.SpecialAmount!.Value / price.Amount * 100m;
        var percent = Math.Max(0, (int)Math.Truncate(raw));
        return (price.SpecialAmount.Value, true, price.SpecialAmount.Value, percent, price.SpecialEndDate);
    }

    // @BR-PRC-005: Negative attribute adjustments are rejected while zero adjustments remain neutral.
    private static void ValidateAttributes(IEnumerable<PricingAttributeDto> attributes)
    {
        foreach (var attribute in attributes)
        {
            if (string.IsNullOrWhiteSpace(attribute.AttributeId) || string.IsNullOrWhiteSpace(attribute.ValueId))
                throw new DomainException("INVALID_ATTRIBUTE_ADJUSTMENT",
                    "Attribute identifiers are required", 422);
            if (attribute.PriceAdjustment < 0)
                throw new DomainException("INVALID_ATTRIBUTE_ADJUSTMENT",
                    "Attribute price adjustments must be zero or positive", 422);
        }
    }

    private static void ValidateItem(PricingItemRequestDto item)
    {
        if (item is null || string.IsNullOrWhiteSpace(item.ProductSku) || item.Quantity < 1)
            throw new DomainException("INVALID_ITEM", "Each pricing item needs a SKU and positive quantity", 422);
        ValidateAttributes(item.Attributes ?? []);
    }

    // @BR-PRC-002: Price administration rejects malformed price identities before they reach persistence.
    // @BR-PRC-003: Price windows and special amounts are validated at the domain boundary.
    private static void ValidatePrice(string code, decimal amount, string priceType, string? start,
        string? end, decimal? special, long? identifier, long? availabilityId)
    {
        if (availabilityId is not > 0)
            throw new DomainException("AVAILABILITY_NOT_FOUND", "A positive availabilityId is required", 404);
        if (string.IsNullOrWhiteSpace(code) || !System.Text.RegularExpressions.Regex.IsMatch(code, "^[A-Za-z0-9_]+$"))
            throw new DomainException("INVALID_PRICE_CODE", "Price code may contain only letters, digits and underscores", 422);
        if (amount < 0) throw new DomainException("INVALID_PRICE_AMOUNT", "Price amount cannot be negative", 422);
        if (priceType is not ("OneTime" or "Monthly"))
            throw new DomainException("INVALID_PRICE_TYPE", "priceType must be OneTime or Monthly", 422);
        if (special < 0) throw new DomainException("INVALID_PRICE_AMOUNT", "Special amount cannot be negative", 422);
        if (identifier is <= 0) throw new DomainException("INVALID_PRICE_AMOUNT", "productIdentifierId must be positive", 422);
        var startDate = ParseDate(start);
        var endDate = ParseDate(end);
        if (startDate is not null && endDate is not null && startDate > endDate)
            throw new DomainException("INVALID_SPECIAL_PRICE_WINDOW",
                "specialStartDate cannot be after specialEndDate", 422);
    }

    private static PriceEntry NewPrice(string sku, long? availabilityId, string code, decimal amount,
        string priceType, bool defaultPrice, string? start, string? end, decimal? special, long? identifier) =>
        new()
        {
            Id = Guid.NewGuid(),
            ProductSku = sku.Trim(),
            AvailabilityId = availabilityId,
            Code = code.Trim(),
            Amount = amount,
            PriceType = priceType,
            DefaultPrice = defaultPrice,
            SpecialStartDate = ParseDate(start),
            SpecialEndDate = ParseDate(end),
            SpecialAmount = special,
            ProductIdentifierId = identifier
        };

    private async Task<PromotionEvaluationResponseDto> EvaluatePromotionCoreAsync(
        string? code, IReadOnlyList<PricingItemRequestDto>? requests, string currency,
        DateTimeOffset at, RequestContext context, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(code))
            return new PromotionEvaluationResponseDto
            {
                PromoCode = code?.Trim() ?? "",
                Matched = false,
                Reduction = 0,
                Currency = currency,
                Reason = "PROMO_CODE_BLANK"
            };
        if (requests is null || requests.Count == 0)
            throw new DomainException("INVALID_ITEMS", "At least one pricing item is required", 422);
        var priced = new List<PricedItemDto>();
        foreach (var request in requests)
        {
            ValidateItem(request);
            var result = await CalculateProductCoreAsync(request.ProductSku, request.VariantSku,
                request.Attributes ?? [], at, "Standard", context, ct);
            priced.Add(new PricedItemDto
            {
                ProductSku = request.ProductSku,
                VariantSku = request.VariantSku,
                Quantity = request.Quantity,
                UnitPrice = result.FinalPrice,
                LineSubtotal = result.FinalPrice * request.Quantity
            });
        }
        return await EvaluatePromotionFromPricesAsync(code, requests, priced, currency, at, context, ct);
    }

    // @BR-PRC-009: A promotion's discount rate is applied to each priced item and quantity.
    // @BR-PRC-010: Only an enabled, in-window tenant/store promotion can match.
    // @BR-PRC-011: Each item reduction and the total reduction are positive monetary values.
    private async Task<PromotionEvaluationResponseDto> EvaluatePromotionFromPricesAsync(
        string? code, IReadOnlyList<PricingItemRequestDto> requests, IReadOnlyList<PricedItemDto> priced,
        string currency, DateTimeOffset at, RequestContext context, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(code))
            return new PromotionEvaluationResponseDto
            {
                PromoCode = code?.Trim() ?? "",
                Matched = false,
                Reduction = 0,
                Currency = currency,
                Reason = "PROMO_CODE_BLANK"
            };
        var match = await repository.FindPromotionAsync(code.Trim(), context,
            DateOnly.FromDateTime(at.UtcDateTime), ct);
        if (match is null)
            return new PromotionEvaluationResponseDto
            {
                PromoCode = code.Trim(),
                Matched = false,
                Reduction = 0,
                Currency = currency,
                Reason = "PROMOTION_NOT_APPLICABLE"
            };
        var items = priced.Select((item, index) =>
        {
            var reduction = Round(item.UnitPrice * match.Promotion.DiscountRate * item.Quantity);
            return new PromotionItemResultDto
            {
                ProductSku = item.ProductSku,
                VariantSku = item.VariantSku,
                Quantity = item.Quantity,
                EffectiveUnitPrice = Round(item.UnitPrice),
                Reduction = reduction
            };
        }).ToList();
        return new PromotionEvaluationResponseDto
        {
            PromoCode = code.Trim(),
            Matched = true,
            DiscountRate = match.Promotion.DiscountRate,
            Reduction = Round(items.Sum(x => x.Reduction)),
            Currency = currency,
            Items = items
        };
    }

    private static DateTimeOffset ParseEvaluationAt(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return DateTimeOffset.UtcNow;
        if (!DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var result))
            throw new DomainException("INVALID_EVALUATION_TIMESTAMP",
                "evaluationAt must be a valid date-time", 400);
        return result;
    }

    private static DateOnly? ParseDate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        if (!DateOnly.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var result))
            throw new DomainException("INVALID_SPECIAL_PRICE_WINDOW",
                "Price dates must use yyyy-MM-dd", 422);
        return result;
    }

    private static ProductPriceCalculationResponseDto ToCalculationResponse(
        CalculatedPrice value, bool includeAdditionalPrices, bool customerApplied, string basis)
    {
        return new ProductPriceCalculationResponseDto
        {
            ProductSku = value.Source.ProductSku,
            SelectedVariantSku = value.SelectedVariantSku,
            AvailabilitySource = value.AvailabilitySource,
            Currency = value.Source.Currency,
            OriginalPrice = Round(value.OriginalPrice),
            FinalPrice = Round(value.FinalPrice),
            Discounted = value.Discounted,
            DiscountedPrice = value.DiscountedPrice.HasValue ? Round(value.DiscountedPrice.Value) : null,
            DiscountPercent = value.DiscountPercent,
            DiscountEndDate = value.DiscountEndDate?.ToString("yyyy-MM-dd"),
            AttributeAdjustment = Round(value.AttributeAdjustment),
            CustomerPricingApplied = customerApplied,
            PricingBasis = basis,
            AdditionalPrices = includeAdditionalPrices
                ? value.AdditionalPrices().Select(x => DtoMapper.Additional(x.Source, x)).ToList()
                : []
        };
    }

    private static decimal Round(decimal value) => decimal.Round(value, 2, MidpointRounding.AwayFromZero);
}

internal static class CalculatedPriceExtensions
{
    public static IReadOnlyList<CalculatedPrice> AdditionalPrices(this CalculatedPrice value) =>
        value.AdditionalPrices;
}
