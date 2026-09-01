
**Service:** MS-07 Pricing and Promotions  
**Version:** 1.0  
**Analysis mode:** Hybrid — CAST-guided transaction scope plus direct Java source read  
**Boundary:** MS-07 owns product and variant price selection, special-price windows, attribute price adjustments, promotion-code evaluation, and pricing processor registration. Product/catalog persistence remains an MS-02 dependency. MS-07 returns calculated pricing and promotion reductions; it does not write checkout or order totals, calculate tax, calculate shipping, or own payment/order lifecycle state.

## Pricing and Product Price Selection

### BR-PRC-001: Default-selected variant availability takes precedence over product availability

**Source Reference:** `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/utils/ProductPriceUtils.java:550-610`  
**Discovery Method:** Hybrid — CAST transaction `244173` (`GET /api/v1/private/product/{productId}/price/`) and direct source read  
**CAST Reference:** Transaction `244173`; full pricing call graph with 137 reduced objects and 3,009 full-graph objects; data graph `243922` identifies `salesmanager.product_price` as the primary price entity.

**Semantic Preservation:**

| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 7 | 7 | OK |
| Data-flow | 8 | 8 | OK |
| Constants | 1 | 1 | OK (`*` wildcard region) |
| State transitions | 0 | 0 | OK |
| Outcomes | 3 | 4 | GAP |
| Data writes | 0 | 0 | OK |
| Integrations | 0 | 0 | OK |
| Error paths | 4 | 4 | OK |

**Preservation:** FLAGGED — the target contract distinguishes validation, unavailable-price, and successful-price outcomes instead of exposing only the legacy exception/null behavior.

**Statement:** When a product has a selected default variant with at least one availability record containing prices, the pricing service must calculate the product price from that variant availability before considering product-level availability. If the selected variant has no usable priced availability, the service must fall back to the product’s availability records. Only availability records for the wildcard region `*` participate in this calculation. A missing usable price is an unavailable-price result, not a zero-priced product.

**Intent:** Routing; Calculation; Validation

**Logic:**
```text
IF product has one or more variants:
    selectedVariant = first variant where defaultSelection = true
    IF selectedVariant exists:
        candidateAvailabilities = selectedVariant.availabilities
        candidateAvailabilities = retain only availabilities with at least one price
IF candidateAvailabilities is empty:
    candidateAvailabilities = product.availabilities
    candidateAvailabilities = retain only availabilities with at least one price
FOR EACH candidate availability:
    IF region is non-empty AND region = '*':
        evaluate every price on that availability
        default price becomes the primary result
        non-default prices become additional prices
IF no primary or additional price was found:
    return pricing-unavailable error
```

**Data Dependencies:** Product variant default-selection flag, variant availability records, product availability records, availability region, associated price records, default-price flag, base amount, and store context.

**Side Effects:** None. The calculation produces an in-memory pricing result and does not mutate catalog or availability persistence.

**Concrete Example:**
- **Input:** `GET /api/v1/pricing/products/SKU-BLUE-MUG/price` with product `SKU-BLUE-MUG`, default variant `SKU-BLUE-MUG-LARGE`, variant availability region `*`, and default price `18.00`.
- **Success:** `200 {"sku":"SKU-BLUE-MUG","selectedVariantSku":"SKU-BLUE-MUG-LARGE","finalPrice":18.00,"currency":"USD","availabilitySource":"variant"}`
- **Error Input:** The selected variant has only an availability for region `EU`, the product has no priced wildcard availability, and the request is made for store `store-us`.
- **Error Output:** `404 {"error":"PRICE_UNAVAILABLE","message":"No usable wildcard-region price is available for product SKU-BLUE-MUG","statusCode":404}`

### BR-PRC-002: Default price is primary and non-default prices are additional price lines

**Source Reference:** `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/utils/ProductPriceUtils.java:578-603,614-647`; `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/order/OrderServiceImpl.java:235-274`  
**Discovery Method:** Hybrid — CAST transactions `244173`, `244172`, and `244174` plus direct source read  
**CAST Reference:** Product price retrieval, single-price retrieval, and price-collection transactions; data graph `243922`.

**Semantic Preservation:**

| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 8 | 8 | OK |
| Data-flow | 10 | 10 | OK |
| Constants | 2 | 2 | OK |
| State transitions | 0 | 1 | GAP |
| Outcomes | 4 | 4 | OK |
| Data writes | 0 | 0 | OK |
| Integrations | 0 | 0 | OK |
| Error paths | 2 | 3 | GAP |

**Preservation:** FLAGGED — the target explicitly represents primary and additional price roles and defines malformed multiple-default data as a validation concern.

**Statement:** For each usable wildcard-region availability, the price marked as default is the primary product price. Prices that are not marked as default are returned as additional price lines. If no default price exists but at least one additional price exists, the first additional price is used as the fallback primary result. Additional prices marked as one-time charges contribute to the item subtotal when order totals are assembled; other price types remain separate from the one-time subtotal.

**Intent:** Calculation; Routing

**Logic:**
```text
FOR EACH eligible availability:
    FOR EACH price:
        calculated = calculate price and special-price state
        IF price.defaultPrice = true:
            primary = calculated
        ELSE:
            append calculated to additionalPrices
IF primary exists:
    primary.additionalPrices = additionalPrices
ELSE IF additionalPrices is not empty:
    primary = additionalPrices[0]
IF primary does not exist:
    return pricing-unavailable error

During subtotal assembly:
    FOR EACH additional price:
        aggregate by price.code
        IF price.type = ONE_TIME:
            add price.finalPrice to the one-time subtotal
```

**Data Dependencies:** Price code, default-price flag, price type, base amount, calculated final amount, eligible availability, product SKU, cart quantity, and order summary.

**Side Effects:** The target pricing response contains an explicit primary price and additional price collection. MS-07 does not persist order-total lines; MS-04 or MS-05 owns any order snapshot.

**Concrete Example:**
- **Input:** `GET /api/v1/pricing/products/SKU-LAPTOP-15/price` with wildcard availability containing `base = 900.00` marked default and `setup = 50.00` marked non-default with type `OneTime`.
- **Success:** `200 {"sku":"SKU-LAPTOP-15","finalPrice":900.00,"additionalPrices":[{"code":"setup","finalPrice":50.00,"priceType":"OneTime"}]}`
- **Error Input:** A price request contains an availability with no default price and no non-default price.
- **Error Output:** `404 {"error":"PRICE_UNAVAILABLE","message":"No usable price exists for availability","statusCode":404}`

### BR-PRC-003: Special prices apply only during valid open or bounded windows

**Source Reference:** `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/utils/ProductPriceUtils.java:510-530,651-704`  
**Discovery Method:** Hybrid — CAST transactions `244173`, `244172`, and `244174` plus direct source read  
**CAST Reference:** Product price retrieval and price collection transactions.

**Semantic Preservation:**

| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 9 | 9 | OK |
| Data-flow | 7 | 7 | OK |
| Constants | 1 | 1 | OK |
| State transitions | 0 | 1 | GAP |
| Outcomes | 4 | 4 | OK |
| Data writes | 0 | 0 | OK |
| Integrations | 0 | 0 | OK |
| Error paths | 3 | 4 | GAP |

**Preservation:** FLAGGED — the target exposes the effective-price state explicitly and rejects invalid windows rather than silently returning an ambiguous discount state.

**Statement:** A special amount replaces the base amount only when the special-price window is active at evaluation time. A bounded window is active when its start date is before the evaluation time and its end date is after the evaluation time. A window with no start date is active until its end date, provided the end date is still in the future. When both dates are absent, a positive special amount is treated as active. A future start date, an expired end date, or a non-positive special amount does not activate a discount.

**Intent:** Calculation; Validation

**Logic:**
```text
today = current date/time
finalAmount = baseAmount
discountActive = false

IF startDate != null OR endDate != null:
    IF startDate != null:
        IF startDate < today AND endDate != null AND endDate > today:
            discountActive = true
            finalAmount = specialAmount
            discountEndDate = endDate
    IF discountActive = false AND startDate = null AND endDate != null:
        IF endDate > today:
            discountActive = true
            finalAmount = specialAmount
            discountEndDate = endDate
ELSE:
    IF specialAmount != null AND specialAmount > 0:
        discountActive = true
        finalAmount = specialAmount
        discountEndDate = endDate

IF discountActive = false:
    finalAmount = baseAmount
```

**Data Dependencies:** Base amount, special amount, optional special start date, optional special end date, evaluation timestamp, and default-price designation.

**Side Effects:** The calculated response records whether a discount is active, the effective amount, original amount, discounted amount when applicable, and discount end date.

**Concrete Example:**
- **Input:** `GET /api/v1/pricing/products/SKU-COFFEE-1KG/price` at `2026-09-01T12:00:00Z`, base amount `24.00`, special amount `18.00`, start `2026-08-01`, end `2026-09-30`.
- **Success:** `200 {"sku":"SKU-COFFEE-1KG","originalPrice":24.00,"finalPrice":18.00,"discounted":true,"discountEndDate":"2026-09-30"}`
- **Error Input:** Base amount `24.00`, special amount `18.00`, start `2026-10-01`, end `2026-10-31`, evaluated on `2026-09-01`.
- **Error Output:** `422 {"error":"SPECIAL_PRICE_NOT_ACTIVE","message":"The special price window is not active at the requested evaluation time","statusCode":422}`

### BR-PRC-004: Discount percentage is the truncated percentage reduction from original to special amount

**Source Reference:** `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/utils/ProductPriceUtils.java:699-720`  
**Discovery Method:** Hybrid — CAST transactions `244173`, `244172`, and `244174` plus direct source read  
**CAST Reference:** Product price retrieval, single-price retrieval, and price collection transactions.

**Semantic Preservation:**

| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 3 | 3 | OK |
| Data-flow | 4 | 4 | OK |
| Constants | 1 | 1 | OK |
| State transitions | 0 | 0 | OK |
| Outcomes | 1 | 2 | GAP |
| Data writes | 0 | 0 | OK |
| Integrations | 0 | 0 | OK |
| Error paths | 0 | 2 | GAP |

**Preservation:** FLAGGED — the target makes zero-denominator and invalid special-amount handling explicit; the legacy arithmetic can otherwise produce an undefined percentage.

**Statement:** When a special price is active, the displayed discount percentage is calculated as `100 - (special amount / original amount × 100)` and converted to an integer by truncating the fractional part toward zero. The original amount remains the comparison basis, and the special amount is returned as the discounted amount. A percentage cannot be calculated from a zero original amount.

**Intent:** Calculation; Validation

**Logic:**
```text
IF discountActive:
    ratio = specialAmount / originalAmount
    rawPercent = 100 - (ratio * 100)
    discountPercent = integer conversion of rawPercent
    discountedPrice = specialAmount
ELSE:
    discountPercent = 0
    discountedPrice = null

IF originalAmount = 0 AND discountActive:
    return invalid-discount-definition error
```

**Data Dependencies:** Original/base amount, special amount, active discount flag, calculated final amount, and discount metadata.

**Side Effects:** None beyond the calculated response.

**Concrete Example:**
- **Input:** `GET /api/v1/pricing/products/SKU-JACKET-RED/price` with original amount `80.00` and active special amount `60.00`.
- **Success:** `200 {"originalPrice":80.00,"discountedPrice":60.00,"finalPrice":60.00,"discounted":true,"discountPercent":25}`
- **Error Input:** An active discount is configured with original amount `0.00` and special amount `5.00`.
- **Error Output:** `422 {"error":"INVALID_DISCOUNT_BASE","message":"An active discount requires a non-zero original amount","statusCode":422}`

### BR-PRC-005: Positive selected attribute prices are additive

**Source Reference:** `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/utils/ProductPriceUtils.java:92-127,141-175`  
**Discovery Method:** Hybrid — CAST transaction `244173` plus direct source read  
**CAST Reference:** Product price retrieval transaction `244173`; product-price call graph objects include final-price and attribute-variation paths.

**Semantic Preservation:**

| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 10 | 10 | OK |
| Data-flow | 8 | 8 | OK |
| Constants | 1 | 1 | OK |
| State transitions | 0 | 0 | OK |
| Outcomes | 4 | 4 | OK |
| Data writes | 0 | 0 | OK |
| Integrations | 0 | 0 | OK |
| Error paths | 2 | 3 | GAP |

**Preservation:** FLAGGED — the target makes negative, null, and unknown attribute adjustments explicit; the legacy implementation silently ignores non-positive adjustments.

**Statement:** Selected product attributes increase the calculated price only when their individual price adjustments are greater than zero. For a request that supplies an attribute list, all positive adjustments in that list are summed and added to the final and original prices; if the item is discounted, the discounted-price value is also increased by the same sum. For a product-level calculation without an explicit list, only positive adjustments belonging to default attributes are included. Null or non-positive adjustments do not change the price.

**Intent:** Calculation; Validation

**Logic:**
```text
base = calculateFinalPrice(product)
attributeAdjustment = 0

IF explicit attributes were supplied:
    FOR EACH attribute:
        IF attribute.price != null AND attribute.price > 0:
            attributeAdjustment += attribute.price
ELSE:
    FOR EACH product attribute:
        IF attribute.default = true
           AND attribute.price != null
           AND attribute.price > 0:
            attributeAdjustment += attribute.price

IF attributeAdjustment > 0:
    finalPrice += attributeAdjustment
    originalPrice += attributeAdjustment
    IF discountedPrice != null AND explicit attributes were supplied:
        discountedPrice += attributeAdjustment
```

**Data Dependencies:** Product base price, original price, discounted price, product attributes, default-attribute flag, selected attribute identifiers, and attribute price adjustments.

**Side Effects:** None. The service returns a calculated price and does not update product attributes.

**Concrete Example:**
- **Input:** `POST /api/v1/pricing/products/SKU-DESK/quote` with `{"attributes":[{"attributeId":"finish","valueId":"walnut","priceAdjustment":35.00},{"attributeId":"drawer","valueId":"none","priceAdjustment":0.00}]}`
- **Success:** If the base price is `250.00`, response is `200 {"finalPrice":285.00,"originalPrice":285.00,"attributeAdjustment":35.00}`
- **Error Input:** The request supplies an attribute adjustment of `-20.00` for `finish`.
- **Error Output:** `422 {"error":"INVALID_ATTRIBUTE_ADJUSTMENT","message":"Attribute price adjustments must be zero or positive","statusCode":422}`

## Customer, Variant, and Formatting Delegation

### BR-PRC-006: Customer-specific pricing delegates to ordinary product pricing

**Source Reference:** `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/catalog/pricing/PricingServiceImpl.java:38-58`  
**Discovery Method:** Direct Source Read  
**CAST Reference:** Pricing call graph attached to transaction `244173`.

**Semantic Preservation:**

| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 2 | 2 | OK |
| Data-flow | 3 | 3 | OK |
| Constants | 0 | 0 | OK |
| State transitions | 0 | 0 | OK |
| Outcomes | 2 | 2 | OK |
| Data writes | 0 | 0 | OK |
| Integrations | 1 | 1 | OK |
| Error paths | 0 | 1 | GAP |

**Preservation:** FLAGGED — the source accepts a customer argument but does not use it. The target explicitly documents that customer identity does not affect the result until a separate approved customer-pricing rule exists.

**Statement:** A product-price request that includes a customer identity must produce the same product price as the equivalent request without customer identity. The current pricing implementation does not apply customer groups, customer accounts, or customer-specific price lists. Customer-specific pricing is therefore not inferred or invented by MS-07; adding it requires a new approved business rule and contract version.

**Intent:** Calculation; Routing

**Logic:**
```text
calculateProductPrice(product, customer):
    ignore customer for price selection
    return calculateProductPrice(product)

calculateProductPrice(product, attributes, customer):
    ignore customer for price selection
    return calculateProductPrice(product, attributes)
```

**Data Dependencies:** Product, product availability, price records, selected attributes, optional customer identifier, and store context.

**Side Effects:** None. No customer record is read or updated by the pricing calculation.

**Concrete Example:**
- **Input:** `POST /api/v1/pricing/products/SKU-SHOE-42/quote` with `{"customerId":"customer-1007","attributes":[]}`.
- **Success:** For a public product price of `79.00`, response is `200 {"sku":"SKU-SHOE-42","customerId":"customer-1007","finalPrice":79.00,"pricingBasis":"standard"}`
- **Error Input:** A caller supplies an unrecognized customer identifier and expects a customer-specific discount.
- **Error Output:** `200 {"sku":"SKU-SHOE-42","finalPrice":79.00,"pricingBasis":"standard","customerPricingApplied":false}`

### BR-PRC-007: Direct variant pricing is not implemented by the pricing-service facade

**Source Reference:** `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/catalog/pricing/PricingServiceImpl.java:109-119`; variant calculation support exists in `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/utils/ProductPriceUtils.java:180-223`  
**Discovery Method:** Direct Source Read  
**CAST Reference:** Pricing call graph attached to transaction `244173`.

**Semantic Preservation:**

| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 1 | 2 | GAP |
| Data-flow | 2 | 3 | GAP |
| Constants | 0 | 0 | OK |
| State transitions | 0 | 1 | GAP |
| Outcomes | 1 | 2 | GAP |
| Data writes | 0 | 0 | OK |
| Integrations | 0 | 1 | GAP |
| Error paths | 0 | 2 | GAP |

**Preservation:** FLAGGED — the utility contains direct variant selection logic, but the service method exposed for direct variant pricing returns `null`. The target contract removes the ambiguous null outcome and requires an explicit variant-to-product fallback policy.

**Statement:** A direct variant-price request must not return an untyped null result. The legacy pricing-service facade does not delegate direct variant requests to the variant-capable price utility and therefore provides no reliable direct-variant result. In the target contract, a variant request must either calculate from the variant’s usable wildcard-region availability or explicitly fall back to the parent product according to the caller-selected fallback policy. If neither is permitted or available, the service returns `VARIANT_PRICE_UNAVAILABLE`.

**Intent:** Routing; Calculation; Validation

**Logic:**
```text
IF variantPriceMode = DIRECT:
    require variant, parent product, and variant availability context
    select wildcard-region variant availability
    calculate primary and additional prices
    IF no usable variant price:
        return VARIANT_PRICE_UNAVAILABLE
IF variantPriceMode = FALLBACK_TO_PRODUCT:
    attempt direct variant calculation
    IF no usable variant price:
        calculate parent product price
```

**Data Dependencies:** Variant identifier/SKU, parent product identifier/SKU, variant availability, wildcard region, price records, and fallback mode.

**Side Effects:** None. The target records the pricing basis (`variant` or `parentProductFallback`) in the response.

**Concrete Example:**
- **Input:** `POST /api/v1/pricing/variants/SKU-SHIRT-BLUE-L/quote` with `{"fallbackMode":"ParentProduct"}`
- **Success:** Variant has no usable price, parent product has wildcard base price `39.00`; response is `200 {"variantSku":"SKU-SHIRT-BLUE-L","finalPrice":39.00,"pricingBasis":"parentProductFallback"}`
- **Error Input:** Direct mode is requested for `SKU-SHIRT-BLUE-L`, and neither the variant nor its parent has a usable wildcard-region price.
- **Error Output:** `404 {"error":"VARIANT_PRICE_UNAVAILABLE","message":"No usable variant price is available and parent-product fallback is disabled","statusCode":404}`

### BR-PRC-008: Only the promotion processor is active in the order-total processor registry

**Source Reference:** `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/configuration/ProcessorsConfiguration.java:34-53`  
**Discovery Method:** Direct Source Read  
**CAST Reference:** Pricing/order-total call graph attached to transaction `244173`.

**Semantic Preservation:**

| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 2 | 3 | GAP |
| Data-flow | 3 | 4 | GAP |
| Constants | 1 | 2 | GAP |
| State transitions | 0 | 1 | GAP |
| Outcomes | 1 | 2 | GAP |
| Data writes | 0 | 0 | OK |
| Integrations | 0 | 1 | GAP |
| Error paths | 0 | 1 | GAP |

**Preservation:** FLAGGED — the target makes processor activation and inactive processor reasons observable through configuration rather than relying on a commented registration line.

**Statement:** The active order-total postprocessor registry contains the promotion-code processor. The legacy manufacturer/shipping-code discount processor is not registered and must not affect pricing. MS-07 must expose processor activation as configuration or health metadata so that a deployment cannot silently assume that an inactive processor is calculating discounts.

**Intent:** Routing; Configuration

**Logic:**
```text
processors = empty ordered list
do not register ManufacturerShippingCode processor
register PromoCode processor
return processors
```

**Data Dependencies:** Processor registry, promotion processor configuration, processor code/name, deployment configuration, and service health state.

**Side Effects:** The active processor list is created at application configuration time. No manufacturer/shipping-code discount line is generated by the active registry.

**Concrete Example:**
- **Input:** `GET /api/v1/private/pricing/processors` with valid tenant/store context.
- **Success:** `200 {"processors":[{"code":"PROMO_CODE","active":true}],"inactive":[{"code":"MANUFACTURER_SHIPPING_CODE","reason":"NOT_REGISTERED"}]}`
- **Error Input:** Deployment configuration attempts to activate the manufacturer/shipping-code processor without an approved implementation.
- **Error Output:** `409 {"error":"PROCESSOR_NOT_SUPPORTED","message":"MANUFACTURER_SHIPPING_CODE is not an active MS-07 processor","statusCode":409}`

## Promotion-Code Evaluation

### BR-PRC-009: A non-blank promotion code is evaluated through the promotion rule session and scaled by quantity

**Source Reference:** `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/modules/order/total/PromoCodeCalculatorModule.java:62-116`  
**Discovery Method:** Direct Source Read  
**CAST Reference:** Promotion processor path attached to pricing/order-total transaction scope; promotion behavior is not exposed as a distinct CAST transaction.

**Semantic Preservation:**

| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 9 | 10 | GAP |
| Data-flow | 10 | 10 | OK |
| Constants | 5 | 5 | OK |
| State transitions | 0 | 1 | GAP |
| Outcomes | 5 | 6 | GAP |
| Data writes | 0 | 0 | OK |
| Integrations | 2 | 2 | OK |
| Error paths | 3 | 5 | GAP |

**Preservation:** FLAGGED — the target defines explicit invalid-code and rule-engine-failure outcomes and makes the promotion evaluation time part of the request context.

**Statement:** When an order summary contains a non-blank promotion code, MS-07 evaluates the code against the active promotion rules using the current evaluation timestamp. If a rule returns a discount rate, MS-07 calculates the reduction from the calculated product final price multiplied by the discount rate and the cart-item quantity. A blank or whitespace-only code produces no promotion reduction. A code for which no rule matches produces an explicit invalid-or-ineligible result rather than a discount line.

**Intent:** Calculation; Validation; Routing

**Logic:**
```text
require orderSummary and store
IF orderSummary.promoCode is blank:
    return no-promotion result

session = create promotion rule session using PromoCoupon rules
response = empty promotion response
input.promoCode = orderSummary.promoCode
input.evaluationDate = current date/time
insert input into session
set response as global "total"
fire all rules

IF response.discount is not null:
    productPrice = calculateProductPrice(product)
    reduction = productPrice.finalPrice
                * response.discount
                * shoppingCartItem.quantity
    return promotion order-total variation with:
        code = discount title
        type = subtotal
        text = submitted promo code
        value = positive reduction
ELSE:
    return no-promotion result
```

**Data Dependencies:** Order summary promotion code, store context, product, calculated final price, cart-item quantity, promotion rule set, evaluation timestamp, discount rate, and promotion response.

**Side Effects:** The target may create an auditable promotion evaluation record or reservation, but it must not mutate the cart or order total. The returned reduction is positive and is applied by the consuming total assembler.

**Concrete Example:**
- **Input:** `POST /api/v1/pricing/promotions/evaluate` with `{"promoCode":"Test1234","items":[{"sku":"SKU-MUG-BLUE","quantity":2}],"evaluationDate":"2025-10-01T12:00:00Z"}`
- **Success:** If the item final price is `20.00` and the matching discount rate is `0.10`, response is `200 {"promoCode":"Test1234","matched":true,"discountRate":0.10,"reduction":4.00,"quantity":2}`
- **Error Input:** `POST /api/v1/pricing/promotions/evaluate` with `{"promoCode":"   ","items":[{"sku":"SKU-MUG-BLUE","quantity":2}]}`
- **Error Output:** `200 {"promoCode":"","matched":false,"reduction":0.00,"reason":"PROMO_CODE_BLANK"}`

### BR-PRC-010: The extracted promotion rule grants ten percent for `Test1234` before 31 October 2025

**Source Reference:** `initial-source/shopizer-3.2.7/sm-core/src/main/resources/com/salesmanager/drools/rules/PromoCoupon.drl:1-16`  
**Discovery Method:** Direct Source Read  
**CAST Reference:** Promotion rule resource reached through `PromoCodeCalculatorModule`; no distinct promotion transaction is present in CAST.

**Semantic Preservation:**

| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 3 | 4 | GAP |
| Data-flow | 2 | 2 | OK |
| Constants | 3 | 3 | OK |
| State transitions | 0 | 1 | GAP |
| Outcomes | 2 | 3 | GAP |
| Data writes | 1 | 0 | GAP |
| Integrations | 0 | 0 | OK |
| Error paths | 1 | 3 | GAP |

**Preservation:** FLAGGED — the source rule’s visible date-bounded behavior is preserved, while the target exposes the expired state and avoids treating a global rule response as durable promotion state.

**Statement:** The extracted rule named `Bam0520` matches promotion code `Test1234` only when the evaluation date is earlier than `31 October 2025`, and assigns a discount rate of `10%`. Because the current analysis date is `2026-09-01`, this extracted rule is expired and must not produce a discount for a current-time evaluation. The code and date are preserved as source evidence, not as an assertion that the campaign remains commercially active.

**Intent:** Calculation; Validation; Compliance

**Logic:**
```text
IF input.promoCode = 'Test1234'
   AND input.evaluationDate < 2025-10-31:
    response.discount = 0.10
ELSE:
    response.discount remains null
```

**Data Dependencies:** Promotion code, evaluation timestamp, rule identifier, discount rate, and promotion evaluation response.

**Side Effects:** The rule sets the in-memory promotion response used by the promotion processor. It does not create a coupon record, redemption, or durable campaign mutation.

**Concrete Example:**
- **Input:** `POST /api/v1/pricing/promotions/evaluate` with `{"promoCode":"Test1234","items":[{"sku":"SKU-MUG-BLUE","quantity":1}],"evaluationDate":"2025-10-30T23:59:59Z"}`
- **Success:** `200 {"promoCode":"Test1234","matched":true,"discountRate":0.10,"reduction":2.00}`
- **Error Input:** The same request with `evaluationDate:"2026-09-01T12:00:00Z"`.
- **Error Output:** `422 {"error":"PROMOTION_EXPIRED","message":"Promotion code Test1234 has no active rule at the requested evaluation time","statusCode":422}`

### BR-PRC-011: Promotion reductions are positive values subtracted during subtotal assembly

**Source Reference:** `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/modules/order/total/PromoCodeCalculatorModule.java:85-109`; `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/order/OrderServiceImpl.java:283-301`  
**Discovery Method:** Direct Source Read  
**CAST Reference:** Promotion processor and order-total assembly paths attached to pricing transaction scope.

**Semantic Preservation:**

| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 6 | 7 | GAP |
| Data-flow | 7 | 8 | GAP |
| Constants | 4 | 4 | OK |
| State transitions | 1 | 2 | GAP |
| Outcomes | 4 | 5 | GAP |
| Data writes | 1 | 0 | GAP |
| Integrations | 1 | 1 | OK |
| Error paths | 1 | 2 | GAP |

**Preservation:** FLAGGED — the target separates a positive reduction returned by MS-07 from the consuming subtotal mutation owned by the order/cart calculation service.

**Statement:** MS-07 returns a promotion reduction as a positive monetary amount. The consuming subtotal assembler subtracts that reduction from the pre-promotion subtotal. The promotion amount is calculated from the item’s effective final price, the matched discount rate, and the item quantity. MS-07 must never return the reduction as a negative amount and must not directly write the consumer’s subtotal or grand total.

**Intent:** Calculation; Routing

**Logic:**
```text
reduction = effectiveItemFinalPrice
            * matchedDiscountRate
            * itemQuantity
require reduction >= 0
return reduction as a positive value

consumer subtotal:
    adjustedSubtotal = prePromotionSubtotal - reduction
```

**Data Dependencies:** Effective product final price, matched discount rate, item quantity, pre-promotion subtotal, promotion code, order-total variation, and currency.

**Side Effects:** MS-07 returns promotion metadata and a positive reduction. MS-04 or MS-05 applies the reduction to its own subtotal calculation and order snapshot.

**Concrete Example:**
- **Input:** `POST /api/v1/pricing/promotions/evaluate` with `{"promoCode":"WELCOME10","items":[{"sku":"SKU-BAG-TRAVEL","quantity":3,"finalPrice":40.00}]}`
- **Success:** `200 {"promoCode":"WELCOME10","reduction":12.00,"reductionSign":"positive","application":"subtract-from-subtotal"}`; a consuming subtotal of `120.00` becomes `108.00`.
- **Error Input:** A consumer submits a promotion reduction of `-12.00` to the pricing result reconciliation endpoint.
- **Error Output:** `422 {"error":"NEGATIVE_PROMOTION_REDUCTION","message":"Promotion reductions returned by MS-07 must be positive","statusCode":422}`

### BR-PRC-012: Manufacturer and shipping-code discounts remain inactive

**Source Reference:** `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/configuration/ProcessorsConfiguration.java:45-51`; inactive implementation reference identified in `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/modules/order/total/ManufacturerShippingCodeOrderTotalModuleImpl.java:86-101`  
**Discovery Method:** Direct Source Read  
**CAST Reference:** Processor registration path attached to pricing/order-total transaction scope.

**Semantic Preservation:**

| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 2 | 2 | OK |
| Data-flow | 3 | 3 | OK |
| Constants | 1 | 1 | OK |
| State transitions | 0 | 1 | GAP |
| Outcomes | 1 | 2 | GAP |
| Data writes | 0 | 0 | OK |
| Integrations | 0 | 0 | OK |
| Error paths | 0 | 1 | GAP |

**Preservation:** FLAGGED — the target records the inactive capability explicitly instead of allowing an unregistered implementation to be discovered accidentally.

**Statement:** Manufacturer-code and shipping-code discount behavior is not active in the extracted implementation because its processor is commented out of the registered processor list. A manufacturer or shipping code must therefore not reduce a product price or subtotal in MS-07. If the capability is required in the future, it must be introduced as a separately approved processor with its own eligibility, rate, and API contract.

**Intent:** Routing; Validation; Configuration

**Logic:**
```text
activeProcessors = [PromoCodeProcessor]
manufacturerShippingCodeProcessor is not present
IF a manufacturer/shipping code is supplied:
    do not calculate a reduction from it
    return unsupported-inactive-processor outcome
```

**Data Dependencies:** Active processor registry, supplied code type, manufacturer/shipping code, product price, and promotion evaluation context.

**Side Effects:** None for the inactive code. No discount line is returned and no subtotal is changed.

**Concrete Example:**
- **Input:** `POST /api/v1/pricing/promotions/evaluate` with `{"codeType":"ManufacturerShipping","code":"ACME-FREESHIP","items":[{"sku":"SKU-DRILL-18V","quantity":1}]}`
- **Success:** `200 {"matched":false,"reduction":0.00,"reason":"PROCESSOR_INACTIVE"}`
- **Error Input:** A caller requests that an inactive manufacturer/shipping processor be force-enabled at runtime.
- **Error Output:** `409 {"error":"PROCESSOR_INACTIVE","message":"Manufacturer and shipping-code discounts are not enabled in this deployment","statusCode":409}`

## Order-Total Integration Boundary

### BR-PRC-013: Pricing participates before shipping, handling, and tax in total assembly

**Source Reference:** `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/order/OrderServiceImpl.java:217-394`  
**Discovery Method:** Hybrid — CAST transaction `244173` pricing call graph plus direct source read  
**CAST Reference:** Transaction `244173`; full pricing call graph with 137 reduced objects and 3,009 full-graph objects; order-total assembly path.

**Semantic Preservation:**

| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 17 | 18 | GAP |
| Data-flow | 20 | 20 | OK |
| Constants | 9 | 9 | OK |
| State transitions | 0 | 1 | GAP |
| Outcomes | 8 | 8 | OK |
| Data writes | 3 | 0 | GAP |
| Integrations | 2 | 2 | OK |
| Error paths | 2 | 4 | GAP |

**Preservation:** FLAGGED — the target preserves the pricing position in the sequence but removes MS-07 ownership of order-total mutation, shipping, handling, and tax calculations.

**Statement:** The effective item prices and one-time additional prices form the pre-promotion merchandise subtotal. Applicable promotion reductions are then subtracted from that subtotal. The resulting merchandise subtotal is established before shipping and handling amounts are considered, and tax is evaluated after those downstream components. MS-07 supplies the merchandise and promotion calculation inputs/results but does not calculate shipping, handling, tax, or the final order grand total.

**Intent:** Calculation; Routing

**Logic:**
```text
merchandiseSubtotal = 0
FOR EACH cart item:
    itemSubtotal = item.finalPrice * item.quantity
    merchandiseSubtotal += itemSubtotal
    FOR EACH additional price:
        aggregate by price.code
        IF price.type = ONE_TIME:
            merchandiseSubtotal += price.finalPrice

promotionVariations = evaluate active MS-07 processors
FOR EACH promotion variation:
    merchandiseSubtotal -= variation.reduction

pricingResult = {
    merchandiseSubtotal,
    itemPrices,
    additionalPriceLines,
    promotionLines
}

consumer sequence:
    pricingResult
    -> shipping component
    -> handling component
    -> tax component
    -> grand total
```

**Data Dependencies:** Cart items, item quantity, effective final price, additional price code and type, promotion code, promotion reduction, order summary type, shipping summary, handling amount, tax result, and currency.

**Side Effects:** MS-07 returns a deterministic pricing result and promotion lines. The consumer owns order-total line persistence, shipping/tax calls, final grand-total calculation, and order snapshot storage.

**Concrete Example:**
- **Input:** `POST /api/v1/pricing/quotes` with `{"currency":"USD","items":[{"sku":"SKU-MUG-BLUE","quantity":2,"finalPrice":20.00,"additionalPrices":[{"code":"gift-wrap","priceType":"OneTime","finalPrice":3.00}]}],"promoCode":"WELCOME10"}`
- **Success:** `200 {"merchandiseSubtotal":43.00,"promotionReduction":4.00,"subtotalAfterPromotion":39.00,"nextComponents":["shipping","handling","tax"],"grandTotalOwnedBy":"consumer"}`
- **Error Input:** A request asks MS-07 to calculate tax or shipping from undeclared downstream data.
- **Error Output:** `422 {"error":"OUT_OF_SCOPE_TOTAL_COMPONENT","message":"MS-07 returns pricing and promotion results; shipping and tax must be calculated by their owning services","statusCode":422}`

## Cross-Service Rule References

The following previously assigned rules are preserved as boundary references rather than duplicated MS-07 implementation rules:

- `BR-ORD-009` — configured order-total processors calculate promotion/variation lines. MS-07 provides the active promotion processor and result; order-total orchestration remains with the consuming order/cart context.
- `BR-UI-011` — storefront option selections can change price. MS-07 exposes the attribute-adjusted price calculation consumed by the storefront.
- `BR-UI-013` — storefront coupon input is syntax-restricted and applied server-side. MS-07 evaluates the server-side promotion code; storefront validation and cart-state replacement remain outside this service.
```

[Turn 2]
