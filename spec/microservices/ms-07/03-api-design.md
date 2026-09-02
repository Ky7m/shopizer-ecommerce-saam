
**Service ID:** MS-07  
**Port:** `8107`  
**Base path:** `/api/v1`  
**JSON naming:** camelCase  
**Path naming:** kebab-case  
**Required context headers on every operation:** `x-tenant-id`, `x-store-id`, `x-correlation-id`  
**Required authentication:** `Authorization: Bearer <token>` on private administration and processor operations

## API boundary

MS-07 exposes two API groups:

1. **Price administration** — source-backed CRUD operations for product and availability price records.
2. **Pricing and promotion calculation** — target service operations that expose the pricing engine behind product presentation and order-total processing.

MS-07 returns calculated prices, additional price lines, promotion reductions, and pricing metadata. It does not calculate or persist tax, shipping, handling, cart totals, order totals, or payment amounts.

## Endpoint inventory

| # | Method | Path | Purpose | Source/target | Driving rules |
|---:|---|---|---|---|---|
| 1 | POST | `/private/products/{sku}/availabilities/{availabilityId}/prices` | Create a price associated with a product availability | Source-backed target mapping | BR-PRC-001, BR-PRC-002 |
| 2 | POST | `/private/products/{sku}/prices` | Create a product price | Source-backed target mapping | BR-PRC-001, BR-PRC-002 |
| 3 | PUT | `/private/products/{sku}/availabilities/{availabilityId}/prices/{priceId}` | Update an availability price | Source-backed target mapping | BR-PRC-002, BR-PRC-003 |
| 4 | GET | `/private/products/{sku}/prices/{priceId}` | Retrieve one product price | Source-backed target mapping | BR-PRC-002, BR-PRC-003, BR-PRC-004 |
| 5 | GET | `/private/products/{sku}/availabilities/{availabilityId}/prices` | List prices for one availability | Source-backed target mapping | BR-PRC-001, BR-PRC-002 |
| 6 | GET | `/private/products/{sku}/prices` | List all prices for a product | Source-backed target mapping | BR-PRC-001, BR-PRC-002 |
| 7 | DELETE | `/private/products/{sku}/prices/{priceId}` | Delete one product price | Source-backed target mapping | BR-PRC-002 |
| 8 | GET | `/pricing/products/{sku}/price` | Calculate the current product price | Target calculation contract | BR-PRC-001 through BR-PRC-004, BR-PRC-006 |
| 9 | POST | `/pricing/products/{sku}/quote` | Calculate a product price with selected attributes and optional customer context | Target calculation contract | BR-PRC-005, BR-PRC-006 |
| 10 | POST | `/pricing/variants/{variantSku}/quote` | Calculate a direct variant price with explicit fallback behavior | Target calculation contract | BR-PRC-001, BR-PRC-007 |
| 11 | POST | `/pricing/promotions/evaluate` | Evaluate a promotion code for one or more priced items | Target calculation contract | BR-PRC-008 through BR-PRC-012 |
| 12 | GET | `/private/pricing/processors` | Return active and inactive pricing processors | Target operational contract | BR-PRC-008, BR-PRC-012 |
| 13 | POST | `/pricing/quotes` | Calculate merchandise pricing and promotion reductions for checkout consumers | Target cross-service contract | BR-PRC-009, BR-PRC-011, BR-PRC-013 |

**API operation count:** 13  
**Unique API path templates:** 10  
**Source-backed administration operations:** 7  
**Target calculation and operational operations:** 6

## Global request context

Every operation requires:

| Header | Required | Description | Example |
|---|---:|---|---|
| `x-tenant-id` | Yes | Tenant isolation context | `2e6d7b63-5b1d-4f8a-8e12-8cf43c9f2001` |
| `x-store-id` | Yes | Store isolation context owned by MS-10 | `store-us-east` |
| `x-correlation-id` | Yes | Distributed tracing identifier | `corr-20260901-000184` |

Private operations additionally require:

| Header | Required | Description | Example |
|---|---:|---|---|
| `Authorization` | Yes | Authenticated operator token | `Bearer eyJ...` |

Promotion and quote operations may additionally accept:

| Header | Required | Description | Example |
|---|---:|---|---|
| `x-evaluation-at` | No | Explicit evaluation timestamp for deterministic replay/testing; defaults to service clock | `2025-10-30T23:59:59Z` |

## Price administration

### POST `/api/v1/private/products/{sku}/availabilities/{availabilityId}/prices`

Creates a price associated with an existing MS-02 product availability.

**Legacy source mapping:** `ProductPriceApi.save:52-71`  
**Legacy source path:** `/api/v1/private/product/{sku}/inventory/{inventoryId}/price`  
**Target path correction:** The target uses plural kebab-case resource names and treats `availabilityId` as an opaque MS-02 reference.

**Authorization:** Requires an authenticated operator with price-management permission for the tenant/store.

**Path parameters:**

| Parameter | Type | Required | Description |
|---|---|---:|---|
| `sku` | string | Yes | Product SKU |
| `availabilityId` | integer | Yes | Opaque MS-02 availability identifier |

**Request body:**
```json
{
  "code": "base",
  "amount": 129.99,
  "priceType": "OneTime",
  "defaultPrice": true,
  "specialStartDate": "2026-09-01",
  "specialEndDate": "2026-09-30",
  "specialAmount": 109.99,
  "productIdentifierId": 88021
}
```

**Validation:**

- `code` is required and may contain only letters, digits, and underscores.
- `amount` must be greater than or equal to `0`.
- `priceType` must be `OneTime` or `Monthly`.
- `specialAmount`, when supplied, must be greater than or equal to `0`.
- `specialStartDate` cannot be after `specialEndDate`.
- Only one default price may be active for the same price-list/product/variant/availability identity.
- The product and availability must resolve within the current tenant/store boundary.

**Responses:**

| Status | Schema | Meaning |
|---:|---|---|
| `201` | `PriceCreatedResponse` | Price created |
| `400` | `ErrorResponse` | Malformed path, context, or JSON |
| `401` | `ErrorResponse` | Authentication required |
| `403` | `ErrorResponse` | Operator lacks price-management permission |
| `404` | `ErrorResponse` | Product or availability does not exist |
| `409` | `ErrorResponse` | Duplicate default price or conflicting price identity |
| `422` | `ErrorResponse` | Invalid amount, code, type, or date window |
| `500` | `ErrorResponse` | Persistence failure |

**Success response:**
```json
{
  "id": "1a40db36-2e7a-4f5f-99e0-5241c5f1e5a1",
  "legacyPriceId": 88055,
  "productSku": "SKU-COFFEE-1KG",
  "availabilityId": 88020
}
```

### POST `/api/v1/private/products/{sku}/prices`

Creates a product price without an availability path parameter. The request must identify or resolve the product’s target availability according to the MS-02 reference contract.

**Legacy source mapping:** `ProductPriceApi.save:73-90`  
**Legacy source path:** `/api/v1/private/product/{sku}/price`

**Request body:**
```json
{
  "availabilityId": 88020,
  "code": "base",
  "amount": 24.99,
  "priceType": "OneTime",
  "defaultPrice": true,
  "specialStartDate": null,
  "specialEndDate": null,
  "specialAmount": null,
  "productIdentifierId": null
}
```

**Responses:** `201`, `400`, `401`, `403`, `404`, `409`, `422`, and `500`, using the same response schemas as the availability-scoped create operation.

**Success response:**
```json
{
  "id": "c12f576c-6cf7-43e0-8bfd-ecaa2631f5a4",
  "legacyPriceId": 88056,
  "productSku": "SKU-COFFEE-1KG",
  "availabilityId": 88020
}
```

### PUT `/api/v1/private/products/{sku}/availabilities/{availabilityId}/prices/{priceId}`

Updates an existing price and associates it with the supplied product availability.

**Legacy source mapping:** `ProductPriceApi.edit:92-113`  
**Legacy source path:** `/api/v1/private/product/{sku}/inventory/{inventoryId}/price/{priceId}`

**Path parameters:**

| Parameter | Type | Required | Description |
|---|---|---:|---|
| `sku` | string | Yes | Product SKU |
| `availabilityId` | integer | Yes | Opaque MS-02 availability identifier |
| `priceId` | UUID | Yes | Target price identifier |

**Request body:**
```json
{
  "code": "base",
  "amount": 129.99,
  "priceType": "OneTime",
  "defaultPrice": true,
  "specialStartDate": "2026-09-01",
  "specialEndDate": "2026-09-30",
  "specialAmount": 109.99,
  "productIdentifierId": 88021
}
```

**Responses:**

| Status | Schema | Meaning |
|---:|---|---|
| `200` | `Price` | Price updated |
| `400` | `ErrorResponse` | Malformed request |
| `401` | `ErrorResponse` | Authentication required |
| `403` | `ErrorResponse` | Operator lacks permission |
| `404` | `ErrorResponse` | Product, availability, or price not found |
| `409` | `ErrorResponse` | Default-price or identity conflict |
| `422` | `ErrorResponse` | Invalid price data |
| `500` | `ErrorResponse` | Persistence failure |

### GET `/api/v1/private/products/{sku}/prices/{priceId}`

Retrieves one price for a product in the current store context.

**Legacy source mapping:** `ProductPriceApi.get:115-133`; `ProductPriceFacadeImpl.get:132-148`  
**Legacy source path:** `/api/v1/private/product/{sku}/price/{priceId}`

**Note:** The legacy method declares a request body on a `GET`. The target operation does not accept a request body; `sku`, `priceId`, and global context headers are sufficient.

**Responses:**

| Status | Schema | Meaning |
|---:|---|---|
| `200` | `Price` | Price retrieved |
| `400` | `ErrorResponse` | Malformed identifier or context |
| `401` | `ErrorResponse` | Authentication required |
| `403` | `ErrorResponse` | Operator lacks permission |
| `404` | `ErrorResponse` | Price does not belong to the product/store or does not exist |
| `500` | `ErrorResponse` | Read or conversion failure |

### GET `/api/v1/private/products/{sku}/availabilities/{availabilityId}/prices`

Lists prices associated with one product availability.

**Legacy source mapping:** `ProductPriceApi.list:135-149`; `ProductPriceFacadeImpl.list:68-86`  
**Legacy source path:** `/api/v1/private/product/{sku}/inventory/{inventoryId}/price`

**Responses:**

| Status | Schema | Meaning |
|---:|---|---|
| `200` | `PriceListResponse` | Price collection; may be empty |
| `400` | `ErrorResponse` | Malformed identifier or context |
| `401` | `ErrorResponse` | Authentication required |
| `403` | `ErrorResponse` | Operator lacks permission |
| `404` | `ErrorResponse` | Product or availability does not exist |
| `500` | `ErrorResponse` | Read or conversion failure |

**Success response:**
```json
{
  "items": [
    {
      "id": "1a40db36-2e7a-4f5f-99e0-5241c5f1e5a1",
      "productSku": "SKU-COFFEE-1KG",
      "availabilityId": 88020,
      "code": "base",
      "amount": 24.99,
      "priceType": "OneTime",
      "defaultPrice": true,
      "discounted": false,
      "price": 24.99,
      "discountedPrice": null,
      "discountPercent": 0,
      "specialStartDate": null,
      "specialEndDate": null,
      "specialAmount": null
    }
  ]
}
```

### GET `/api/v1/private/products/{sku}/prices`

Lists all prices associated with a product in the current store context.

**Legacy source mapping:** `ProductPriceApi.list:152-165`; `ProductPriceFacadeImpl.list:88-104`  
**Legacy source path:** `/api/v1/private/product/{sku}/prices`

**Responses:** `200 PriceListResponse`, `400 ErrorResponse`, `401 ErrorResponse`, `403 ErrorResponse`, `404 ErrorResponse`, and `500 ErrorResponse`.

### DELETE `/api/v1/private/products/{sku}/prices/{priceId}`

Deletes one product price after verifying that the price belongs to the supplied product and current store.

**Legacy source mapping:** `ProductPriceApi.delete:167-181`; `ProductPriceFacadeImpl.delete:107-123`  
**Legacy source path:** `/api/v1/private/product/{sku}/price/{priceId}`

**Responses:**

| Status | Schema | Meaning |
|---:|---|---|
| `204` | No content | Price deleted |
| `400` | `ErrorResponse` | Malformed identifier or context |
| `401` | `ErrorResponse` | Authentication required |
| `403` | `ErrorResponse` | Operator lacks permission |
| `404` | `ErrorResponse` | Price does not exist for the product/store |
| `409` | `ErrorResponse` | Price cannot be deleted because it is referenced by an active rule/snapshot |
| `500` | `ErrorResponse` | Delete failure |

## Price calculation

### GET `/api/v1/pricing/products/{sku}/price`

Calculates the current primary price for a product.

**Driving rules:** BR-PRC-001, BR-PRC-002, BR-PRC-003, BR-PRC-004, BR-PRC-006

**Query parameters:**

| Parameter | Type | Required | Default | Description |
|---|---|---:|---|---|
| `evaluationAt` | date-time | No | Service clock | Timestamp used for special-price evaluation |
| `includeAdditionalPrices` | boolean | No | `true` | Include non-default price lines |

**Processing:**

1. Resolve the product in the current tenant/store context.
2. Select usable availability from the default-selected variant when available.
3. Fall back to product availability when the selected variant has no usable priced availability.
4. Restrict selection to wildcard-region availability `*`.
5. Select the default price as primary.
6. Return additional prices separately.
7. Apply the active special-price window and discount metadata.

**Responses:**

| Status | Schema | Meaning |
|---:|---|---|
| `200` | `ProductPriceCalculationResponse` | Price calculated |
| `400` | `ErrorResponse` | Malformed SKU, context, or timestamp |
| `404` | `ErrorResponse` | No usable wildcard-region price |
| `422` | `ErrorResponse` | Invalid price window or discount definition |
| `503` | `ErrorResponse` | MS-02 catalog reference unavailable |
| `500` | `ErrorResponse` | Unexpected calculation failure |

**Success response:**
```json
{
  "productSku": "SKU-BLUE-MUG",
  "selectedVariantSku": "SKU-BLUE-MUG-LARGE",
  "availabilitySource": "variant",
  "currency": "USD",
  "originalPrice": 20.00,
  "finalPrice": 18.00,
  "discounted": true,
  "discountedPrice": 18.00,
  "discountPercent": 10,
  "discountEndDate": "2026-09-30",
  "additionalPrices": []
}
```

### POST `/api/v1/pricing/products/{sku}/quote`

Calculates a product price using explicitly selected attributes and optional customer context.

**Driving rules:** BR-PRC-005 and BR-PRC-006

**Request body:**
```json
{
  "customerId": "customer-1007",
  "attributes": [
    {
      "attributeId": "finish",
      "valueId": "walnut",
      "priceAdjustment": 35.00
    }
  ],
  "evaluationAt": "2026-09-01T12:00:00Z"
}
```

**Processing:**

- Positive selected attribute adjustments are summed.
- Null and zero adjustments do not change the result.
- Negative adjustments are rejected.
- Customer identity is accepted as context but does not alter the extracted standard price.
- The response identifies whether the price came from standard pricing or parent-product fallback.

**Responses:**

| Status | Schema | Meaning |
|---:|---|---|
| `200` | `ProductPriceCalculationResponse` | Price calculated |
| `400` | `ErrorResponse` | Malformed request |
| `404` | `ErrorResponse` | Product price unavailable |
| `422` | `ErrorResponse` | Invalid attribute selection or adjustment |
| `503` | `ErrorResponse` | Catalog dependency unavailable |
| `500` | `ErrorResponse` | Unexpected calculation failure |

### POST `/api/v1/pricing/variants/{variantSku}/quote`

Calculates a direct variant price with explicit fallback behavior.

**Driving rules:** BR-PRC-001 and BR-PRC-007

**Request body:**
```json
{
  "parentProductSku": "SKU-SHIRT",
  "fallbackMode": "ParentProduct",
  "evaluationAt": "2026-09-01T12:00:00Z"
}
```

**Allowed `fallbackMode` values:**

| Value | Meaning |
|---|---|
| `DirectOnly` | Return an error when the variant has no usable price |
| `ParentProduct` | Use the parent product price when direct variant pricing is unavailable |

**Responses:**

| Status | Schema | Meaning |
|---:|---|---|
| `200` | `ProductPriceCalculationResponse` | Variant price calculated |
| `400` | `ErrorResponse` | Malformed request |
| `404` | `ErrorResponse` | Variant or permitted parent fallback price unavailable |
| `422` | `ErrorResponse` | Invalid fallback mode |
| `503` | `ErrorResponse` | Catalog dependency unavailable |
| `500` | `ErrorResponse` | Unexpected calculation failure |

## Promotion evaluation

### POST `/api/v1/pricing/promotions/evaluate`

Evaluates a promotion code against one or more priced items.

**Driving rules:** BR-PRC-008 through BR-PRC-012

**Request body:**
```json
{
  "promoCode": "WELCOME10",
  "items": [
    {
      "productSku": "SKU-BAG-TRAVEL",
      "variantSku": null,
      "quantity": 3,
      "attributes": []
    }
  ],
  "evaluationAt": "2026-09-01T12:00:00Z"
}
```

**Processing:**

1. If `promoCode` is blank or whitespace-only, return `matched:false` and `reduction:0.00`.
2. Resolve each item’s effective final price through MS-07 pricing.
3. Evaluate the code against active promotion rules.
4. Return the matched discount rate and a positive reduction.
5. Do not mutate the cart or order.
6. Do not apply manufacturer/shipping-code discounts because that processor is inactive.

**Reduction formula:**

```text
reduction =
    effectiveItemFinalPrice
    × matchedDiscountRate
    × item.quantity
```

For multiple items, the item reductions are summed. Monetary rounding is performed by the consumer’s currency policy at the final monetary boundary; MS-07 returns decimal monetary values with four fractional digits internally and two fractional digits in the public response.

**Responses:**

| Status | Schema | Meaning |
|---:|---|---|
| `200` | `PromotionEvaluationResponse` | Evaluation completed; code may be matched or unmatched |
| `400` | `ErrorResponse` | Malformed request |
| `404` | `ErrorResponse` | One or more product prices unavailable |
| `409` | `ErrorResponse` | Requested processor is inactive |
| `422` | `ErrorResponse` | Invalid item quantity, attribute adjustment, or evaluation timestamp |
| `503` | `ErrorResponse` | Promotion rule boundary unavailable |
| `500` | `ErrorResponse` | Unexpected evaluation failure |

**Matched response:**
```json
{
  "promoCode": "WELCOME10",
  "matched": true,
  "discountRate": 0.10,
  "reduction": 12.00,
  "currency": "USD",
  "items": [
    {
      "productSku": "SKU-BAG-TRAVEL",
      "quantity": 3,
      "effectiveUnitPrice": 40.00,
      "reduction": 12.00
    }
  ],
  "reason": null
}
```

**Unmatched response:**
```json
{
  "promoCode": "UNKNOWN2026",
  "matched": false,
  "discountRate": null,
  "reduction": 0.00,
  "currency": "USD",
  "items": [],
  "reason": "PROMOTION_NOT_APPLICABLE"
}
```

### GET `/api/v1/private/pricing/processors`

Returns the active promotion processor registry and explicitly reports inactive extracted processors.

**Driving rules:** BR-PRC-008 and BR-PRC-012

**Authorization:** Requires an authenticated operator with pricing-configuration permission.

**Success response:**
```json
{
  "processors": [
    {
      "code": "PROMO_CODE",
      "name": "Promotion code evaluator",
      "active": true
    }
  ],
  "inactive": [
    {
      "code": "MANUFACTURER_SHIPPING_CODE",
      "name": "Manufacturer and shipping-code discount",
      "active": false,
      "reason": "NOT_REGISTERED"
    }
  ]
}
```

**Responses:**

| Status | Schema | Meaning |
|---:|---|---|
| `200` | `ProcessorRegistryResponse` | Registry returned |
| `401` | `ErrorResponse` | Authentication required |
| `403` | `ErrorResponse` | Operator lacks permission |
| `500` | `ErrorResponse` | Registry unavailable |

## Checkout pricing quote

### POST `/api/v1/pricing/quotes`

Calculates the merchandise subtotal, additional one-time price lines, and promotion reductions required by checkout. It does not calculate shipping, handling, tax, or the final grand total.

**Driving rules:** BR-PRC-002, BR-PRC-009, BR-PRC-011, and BR-PRC-013

**Request body:**
```json
{
  "currency": "USD",
  "items": [
    {
      "productSku": "SKU-MUG-BLUE",
      "variantSku": null,
      "quantity": 2,
      "attributes": []
    },
    {
      "productSku": "SKU-DESK",
      "variantSku": null,
      "quantity": 1,
      "attributes": [
        {
          "attributeId": "finish",
          "valueId": "walnut",
          "priceAdjustment": 35.00
        }
      ]
    }
  ],
  "promoCode": "WELCOME10",
  "evaluationAt": "2026-09-01T12:00:00Z"
}
```

**Response calculation order:**

```text
1. Calculate each item effective unit price.
2. Multiply each effective unit price by quantity.
3. Add one-time additional prices.
4. Evaluate active promotion processors.
5. Subtract positive promotion reductions.
6. Return the merchandise subtotal after promotion.
7. Leave shipping, handling, tax, and grand-total calculation to the owning consumers.
```

**Success response:**
```json
{
  "currency": "USD",
  "items": [
    {
      "productSku": "SKU-MUG-BLUE",
      "quantity": 2,
      "unitPrice": 20.00,
      "lineSubtotal": 40.00,
      "additionalPrices": []
    },
    {
      "productSku": "SKU-DESK",
      "quantity": 1,
      "unitPrice": 285.00,
      "lineSubtotal": 285.00,
      "additionalPrices": []
    }
  ],
  "additionalPriceLines": [],
  "merchandiseSubtotal": 325.00,
  "promotion": {
    "promoCode": "WELCOME10",
    "matched": true,
    "reduction": 32.50
  },
  "subtotalAfterPromotion": 292.50,
  "downstreamComponents": [
    "shipping",
    "handling",
    "tax"
  ],
  "grandTotalOwnedBy": "consumer"
}
```

**Responses:**

| Status | Schema | Meaning |
|---:|---|---|
| `200` | `PricingQuoteResponse` | Merchandise pricing calculated |
| `400` | `ErrorResponse` | Malformed request |
| `404` | `ErrorResponse` | Product, variant, availability, or price unavailable |
| `409` | `ErrorResponse` | Inactive processor requested |
| `422` | `ErrorResponse` | Invalid quantity, currency, attribute, or promotion input |
| `503` | `ErrorResponse` | MS-02 or promotion dependency unavailable |
| `500` | `ErrorResponse` | Unexpected calculation failure |

## Error contract

All error responses use the same shape:

```json
{
  "error": "PRICE_UNAVAILABLE",
  "message": "No usable wildcard-region price is available for product SKU-BLUE-MUG",
  "statusCode": 404,
  "timestamp": "2026-09-01T12:00:00Z",
  "correlationId": "corr-20260901-000184",
  "details": []
}
```

Defined MS-07 error codes include:

| Code | HTTP status | Meaning |
|---|---:|---|
| `PRICE_UNAVAILABLE` | `404` | No usable product price exists |
| `VARIANT_PRICE_UNAVAILABLE` | `404` | No direct variant price or permitted fallback exists |
| `PRODUCT_NOT_FOUND` | `404` | Product reference cannot be resolved through MS-02 |
| `AVAILABILITY_NOT_FOUND` | `404` | Availability reference cannot be resolved through MS-02 |
| `PRICE_NOT_FOUND` | `404` | Price does not belong to the requested product/store |
| `INVALID_PRICE_CODE` | `422` | Price code violates the allowed syntax |
| `INVALID_PRICE_AMOUNT` | `422` | Amount is negative or otherwise invalid |
| `INVALID_PRICE_TYPE` | `422` | Price type is not `OneTime` or `Monthly` |
| `INVALID_SPECIAL_PRICE_WINDOW` | `422` | Special-price dates are invalid |
| `SPECIAL_PRICE_NOT_ACTIVE` | `422` | Special price is outside its evaluation window |
| `INVALID_DISCOUNT_BASE` | `422` | Discount percentage cannot be calculated from the original amount |
| `INVALID_ATTRIBUTE_ADJUSTMENT` | `422` | Attribute adjustment is negative or malformed |
| `PROMOTION_EXPIRED` | `422` | Promotion rule or coupon window has expired |
| `PROMOTION_NOT_APPLICABLE` | `200` result | No active rule matches the supplied code |
| `PROMO_CODE_BLANK` | `200` result | Blank promotion code produces no reduction |
| `PROCESSOR_INACTIVE` | `409` | Requested processor is not registered |
| `NEGATIVE_PROMOTION_REDUCTION` | `422` | Promotion reduction is not a positive value |
| `OUT_OF_SCOPE_TOTAL_COMPONENT` | `422` | Request attempts to make MS-07 calculate tax, shipping, handling, or grand total |
| `PRICING_DEPENDENCY_UNAVAILABLE` | `503` | Required catalog or rule dependency is unavailable |

## Events and downstream usage

### Published target events

| Event | Trigger | Consumers |
|---|---|---|
| `PromotionChanged.v1` | Promotion or coupon definition is created, updated, enabled, disabled, or deleted | MS-04 Cart and Checkout; MS-05 Order Management; MS-03 Search only if promotion display is enabled |
| `PriceChanged.v1` | Price entry is created, updated, or deleted | MS-04 Cart and Checkout; MS-05 Order Management; MS-03 Search for display projection refresh |

These events are target architecture contracts. The listed legacy source files do not implement an equivalent event publisher; event publication must therefore be implemented through an outbox in the target service.

### Consumed dependencies

| Dependency | Protocol | Purpose |
|---|---|---|
| MS-02 catalog reference | REST or synchronous query | Resolve product, variant, availability, and attribute references |
| MS-10 store context | REST or validated request context | Validate tenant/store ownership |
| MS-04 checkout | REST consumer | Request merchandise pricing and promotion quote |
| MS-05 order management | Event or snapshot consumer | Preserve calculated price and promotion values in immutable order data |

## Boundary rules

- MS-07 never writes MS-02 product or availability tables.
- MS-07 never writes MS-04 cart totals or MS-05 order totals.
- MS-07 never reads tax or shipping tables directly.
- Customer identity is accepted as context but is ignored by the extracted standard pricing behavior.
- A positive promotion reduction is returned for the consumer to subtract.
- The API does not expose the legacy KIE session or Drools resource path.
- The API does not expose manufacturer/shipping-code discount behavior as active capability.
- Product price administration is store-scoped even when the legacy query predicate did not group all `OR` conditions explicitly.

## Phase 4b inferred pricing clarifications

- `[Inferred in Phase 4b — Mode A]` Promotion evaluation is deterministic: exclusive
  promotions run first, followed by stackable promotions in descending priority and stable ID
  order.
- `[Inferred in Phase 4b — Mode A]` Coupon reservation is idempotent by checkout idempotency
  key; expired, disabled, exhausted, or store-mismatched coupons return `422`.
