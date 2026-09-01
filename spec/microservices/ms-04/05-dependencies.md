# Dependencies: Cart and Checkout

**Service ID:** MS-04

## Services Consumed

### Customer and Identity (MS-01) (sync REST)

#### Call: `getCurrentCustomer`
- **Triggered by:** BR-CO-AUT-012
- **Method:** GET
- **Path:** `/customers/me`
- **Headers:** `x-tenant-id`, `x-store-id`, `x-correlation-id` required; `Authorization` is declared optional in the provider contract but is required by this business operation.
- **Request body:** none
- **Success response:** `200`, `#/components/schemas/Customer`
- **Response shape:** `Customer` with required `id`, `storeId`, `loginName`, `emailAddress`, `status`, `defaultLanguageCode`, `reviewAverage`, and `reviewCount`; address snapshots are `billing` and `delivery`.
- **Error handling:**
  | Status | Meaning | Action |
  |---|---|---|
  | 401 | Principal cannot be authenticated | Reject checkout |
  | 404 | Customer context is unavailable | Reject checkout; do not expose the cart |
  | 503 | Identity unavailable | Retry, then fail checkout without mutation |
- **Resilience:** 10s timeout; 3 retries at 2s/4s/8s; circuit opens after 5 failures and half-opens after 30s; no anonymous fallback for an authenticated operation.

#### Call: `registerCustomer` (conditional)
- **Triggered by:** BR-CO-CUS-013 when anonymous checkout includes credentials
- **Method:** POST
- **Path:** `/customer-auth/registrations`
- **Headers:** `x-tenant-id`, `x-store-id`, `x-correlation-id`; `Authorization` is not required for registration.
- **Request body:** `#/components/schemas/CreateCustomerRequest` with required `emailAddress`, `password`, and `billing`; optional `firstName`, `lastName`, `gender`, `language`, `provider`, `delivery`, and `attributes`.
- **Success response:** `201`, `#/components/schemas/AuthenticationResponse`
- **Error handling:** `409` duplicate registration -> reject checkout; `422` invalid customer data -> return validation failure; `503` unavailable -> retry then leave checkout unsubmitted.
- **Resilience:** 10s timeout; no automatic retry after a provider may have accepted a non-idempotent registration; circuit opens after 5 failures and half-opens after 30s.

### Catalog and Product (MS-02) (sync REST)

#### Call: `getProductBySku`
- **Triggered by:** BR-SC-SEL-002, BR-SC-ATR-003, BR-SC-HYD-006, BR-CO-SNP-014
- **Method:** GET
- **Path:** `/products/sku/{sku}`
- **Headers:** `x-tenant-id`, `x-store-id`, `x-correlation-id`
- **Request body:** none; required path `sku`
- **Success response:** `200`, `#/components/schemas/Product`
- **Error handling:** `404` -> reject the line as unavailable; `503` -> retry then reject the operation without writing a cart snapshot.
- **Resilience:** 10s timeout; 3 retries at 2s/4s/8s; circuit opens after 5 failures and half-opens after 30s; no stale product fallback.

#### Call: `getProductAvailability`
- **Triggered by:** BR-SC-SEL-002
- **Method:** GET
- **Path:** `/products/{productId}/availability`
- **Headers:** `x-tenant-id`, `x-store-id`, `x-correlation-id`
- **Request body:** none; required path `productId` is a UUID
- **Success response:** `200`, `#/components/schemas/AvailabilityListResponse` (`items` array)
- **Error handling:** `404` -> reject the line; `503` is not declared by the provider and must not be invented as an HTTP success.
- **Resilience:** 10s timeout; 3 retries only for transport failure; no availability fallback.

#### Call: `createInventoryReservation`
- **Triggered by:** BR-ORD-012 and BR-CO-ORC-019
- **Method:** POST
- **Path:** `/products/{productId}/reservations`
- **Headers:** `x-tenant-id`, `x-store-id`, `x-correlation-id`, `Idempotency-Key`
- **Request body:** `#/components/schemas/CreateReservationRequest`, required `reservationKey`, `quantity`, and `expiresAt`; optional `variantId`, `availabilityId`, and `regionCode`.
- **Success response:** `201`, `#/components/schemas/InventoryReservation`
- **Error handling:** `401` reject authorization; `404` reject missing product; `409` surface insufficient/already-reserved inventory; `422` reject invalid reservation request.
- **Resilience:** 10s timeout; do not retry without the same `Idempotency-Key`; circuit opens after 5 failures and half-opens after 30s; fallback is checkout failure/compensation, never local inventory mutation.

### Pricing and Promotions (MS-07) (sync REST)

#### Call: `calculatePricingQuote`
- **Triggered by:** BR-SC-TOT-010, BR-CO-SNP-014, BR-CO-TOT-015
- **Method:** POST
- **Path:** `/pricing/quotes`
- **Headers:** `x-tenant-id`, `x-store-id`, `x-correlation-id`
- **Request body:** `#/components/schemas/PricingQuoteRequest` with required `currency` and `items`; optional `promoCode` and `evaluationAt`.
- **Success response:** `200`, `#/components/schemas/PricingQuoteResponse`
- **Response shape:** required `currency`, `items`, `additionalPriceLines`, `merchandiseSubtotal`, `promotion`, `subtotalAfterPromotion`, `downstreamComponents`, and `grandTotalOwnedBy`.
- **Error handling:** `400/422` reject invalid quote; `404` reject an unknown pricing reference; `409` reject conflicting quote state; `503` retry then fail quote; `500` fail without persisting an authoritative total.
- **Resilience:** 10s timeout; 3 retries at 2s/4s/8s for safe quote requests; circuit opens after 5 failures and half-opens after 30s; no stale price fallback.

#### Call: `evaluatePromotion`
- **Triggered by:** BR-SC-PRO-011
- **Method:** POST
- **Path:** `/pricing/promotions/evaluate`
- **Headers:** `x-tenant-id`, `x-store-id`, `x-correlation-id`
- **Request body:** `#/components/schemas/PromotionEvaluationRequest` with required `promoCode` and `items`; optional `evaluationAt`.
- **Success response:** `200`, `#/components/schemas/PromotionEvaluationResponse`
- **Error handling:** `400/422` reject malformed promotion input; `404` return a non-match; `409` preserve provider conflict; `503` retry then fail calculation.
- **Resilience:** 10s timeout; 3 retries at 2s/4s/8s; circuit opens after 5 failures and half-opens after 30s; no local discount fallback.

### Tax (MS-08) (sync REST)

#### Call: `calculateTax`
- **Triggered by:** BR-SC-TOT-010 and BR-CO-SNP-014
- **Method:** POST
- **Path:** `/tax-calculations`
- **Headers:** `x-tenant-id`, `x-store-id`, `x-correlation-id`, `Authorization`
- **Request body:** `#/components/schemas/CalculateTaxRequest` with required `currencyCode`, `billingAddress`, and `items`; optional customer/order/shipping/language/idempotency fields as defined by the provider schema.
- **Success response:** `200`, `#/components/schemas/TaxCalculationResponse`
- **Response shape:** required `quoteId`, `currencyCode`, `jurisdiction`, `taxableAmount`, `totalTaxAmount`, and `taxItems`.
- **Error handling:** `400/422` reject the quote; `401` fail authentication; `500` fail checkout without a locally invented tax value.
- **Resilience:** 10s timeout; 3 retries at 2s/4s/8s only when the same `idempotencyKey` is sent; circuit opens after 5 failures and half-opens after 30s; no zero-tax fallback.

### Shipping (MS-09) (sync REST)

#### Call: `calculateCartShipping`
- **Triggered by:** BR-SC-SHP-008 and BR-SC-SHP-009
- **Method:** POST
- **Path:** `/cart/{cart}/shipping`
- **Headers:** `x-tenant-id`, `x-store-id`, optional `x-correlation-id`, optional `Authorization`; optional query `lang` defaults to `en`.
- **Request body:** `#/components/schemas/ShippingAddressRequest` with required `countryCode` and `postalCode`; optional `address`, `city`, `state`, and `zoneCode`.
- **Success response:** `200`, `#/components/schemas/ShippingSummary`
- **Response shape:** required `shipping`, `handling`, `freeShipping`, `taxOnShipping`, and `shippingOptions`.
- **Error handling:** `400/422` reject destination; `404` reject missing cart; `502` surface adapter failure; `500` fail quote without a fabricated option.
- **Resilience:** 10s timeout at the MS-04/MS-09 boundary; 3 retries at 2s/4s/8s for a safe quote request; circuit opens after 5 failures and half-opens after 30s; no stale carrier fallback.

## Events Published

### `OrderSubmitted.v1`
- **Triggered by:** BR-CO-ORC-019
- **Channel:** RabbitMQ domain-events exchange; routing key `OrderSubmitted.v1`
- **Schema:** `spec/shared/event-schemas/order-submitted-v1.yaml`
- **Guarantees:** local transactional outbox, at-least-once delivery
- **Ordering:** by checkout submission/order correlation

## Events Consumed

### `PaymentAuthorized.v1`, `PaymentCaptured.v1`, `PaymentFailed.v1`, `PaymentRefunded.v1`, `PaymentVoided.v1`
- **Triggered by:** BR-CO-ORC-019 and downstream payment reconciliation
- **Channel:** RabbitMQ domain-events exchange; source MS-06
- **Schemas:** corresponding files under `spec/shared/event-schemas/`
- **Action:** update checkout submission/downstream status; never mutate MS-06 state.
- **Idempotency:** inbox uniqueness on `eventId`.

### `InventoryReservationChanged.v1`
- **Triggered by:** BR-ORD-012
- **Channel:** RabbitMQ domain-events exchange; source MS-02
- **Schema:** `spec/shared/event-schemas/inventory-reservation-changed-v1.yaml`
- **Action:** reconcile reservation state for the checkout submission.
- **Idempotency:** inbox uniqueness on `eventId` and reservation ID.

## Integration reconciliation

The rules identify all five synchronous providers and the order/inventory event boundaries. The
graph supplies the five REST edges and one event edge to MS-05. Payment and inventory events are
also declared by the service rules; they are represented above even where the Phase 2 graph edge
is directional at the aggregate boundary.
