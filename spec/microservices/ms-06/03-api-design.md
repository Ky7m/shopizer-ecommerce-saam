
# Payments — API Design

**Version:** 1.0  
**Service ID:** MS-06  
**Base path:** `/api/v1`  
**Field naming:** camelCase  
**Path naming:** kebab-case

## Cross-Cutting Request Context

Every operation requires:

- `x-tenant-id`
- `x-store-id`
- `x-correlation-id`
- `Authorization`

Payment-mutating operations additionally require `Idempotency-Key`, represented as `idempotencyKey` in request bodies for contract portability and repeated in the HTTP header where supported.

## Ownership Boundary

- Payment-method configuration metadata is owned by MS-11.
- MS-06 exposes a provider-facing configuration boundary for validation and dispatch.
- MS-06 does not persist merchant configuration keys or decrypted credentials.
- Order lifecycle and totals remain owned by MS-05.
- Checkout amount snapshots originate in MS-04/MS-05.
- Generic secrets, egress, and callback transport may be supplied by MS-12.

## Endpoint Inventory

| # | Method | Path | Purpose | Success |
|---:|---|---|---|---|
| 1 | GET | `/payment-methods` | List store-eligible payment methods | 200 |
| 2 | GET | `/payment-methods/{code}` | Read provider configuration projection | 200 |
| 3 | PUT | `/payment-methods/{code}/configuration` | Validate and submit provider configuration to MS-11 | 200 |
| 4 | POST | `/payment-intents` | Create payment intent or provider initialization | 201 |
| 5 | GET | `/payment-intents/{paymentIntentId}` | Read payment intent | 200 |
| 6 | POST | `/payment-intents/{paymentIntentId}/authorize` | Authorize payment | 201 |
| 7 | POST | `/payment-intents/{paymentIntentId}/capture` | Capture authorized payment | 201 |
| 8 | POST | `/payment-intents/{paymentIntentId}/refunds` | Full or partial refund | 201 |
| 9 | GET | `/payment-intents/{paymentIntentId}/transactions` | Chronological transaction history | 200 |
| 10 | GET | `/payment-operations/{paymentOperationId}` | Read operation and replay status | 200 |
| 11 | POST | `/callbacks/{providerCode}` | Receive provider callback through adapter | 202 |
| 12 | GET | `/reconciliation/capturable` | List intents requiring capture/reconciliation | 200 |

## Endpoint Definitions

### GET `/api/v1/payment-methods`

- **Driven by:** BR-ORD-014, BR-EXT-001
- **Authorization:** authenticated tenant/store administrator or checkout service
- **Query parameters:** `page`, `pageSize`
- **Response:** `PaymentMethodListResponse`
- **Errors:** 401, 403, 500

### GET `/api/v1/payment-methods/{code}`

- **Driven by:** BR-ORD-014, BR-EXT-001
- **Path parameter:** `code`
- **Response:** `PaymentMethod`
- **Errors:** 401, 404, 500

### PUT `/api/v1/payment-methods/{code}/configuration`

- **Driven by:** BR-EXT-001 and provider-specific configuration rules
- **Ownership:** MS-06 validates the provider contract; MS-11 persists the configuration.
- **Request:** provider code, active flag, default selection, environment, secret reference, public configuration
- **Response:** `PaymentMethod`
- **Errors:** 400, 401, 404, 422, 502

### POST `/api/v1/payment-intents`

- **Driven by:** BR-ORD-014, BR-EXT-001, BR-EXT-005, BR-EXT-007, BR-EXT-009, BR-UI-015, BR-PA-020, BR-PA-022
- **Request:** `CreatePaymentIntentRequest`
- **Rules:**
  - Amount must be positive.
  - Currency must be ISO-4217 uppercase.
  - Provider code must be eligible, registered, and active.
  - Amount and currency are frozen on creation.
  - `checkoutSessionId` is required.
  - `paymentToken` is optional for providers that require later customer action.
- **Response:** `201 PaymentIntent`
- **Errors:** 400, 401, 404, 409, 422, 502

### GET `/api/v1/payment-intents/{paymentIntentId}`

- **Driven by:** CRUD/read behavior plus BR-PA-021
- **Response:** `PaymentIntent`
- **Errors:** 401, 404, 500

### POST `/api/v1/payment-intents/{paymentIntentId}/authorize`

- **Driven by:** BR-ORD-015, BR-EXT-004, BR-EXT-005, BR-EXT-006, BR-EXT-007, BR-EXT-008, BR-UI-015, BR-PA-020, BR-PA-022
- **Request:** `AuthorizePaymentRequest`
- **Rules:** payment intent must be in `Created` or `RequiresAction`; amount/currency must match; provider token must satisfy adapter requirements; duplicate idempotency key returns the original operation.
- **Response:** `201 PaymentOperation`
- **Errors:** 400, 401, 404, 409, 422, 402, 502

### POST `/api/v1/payment-intents/{paymentIntentId}/capture`

- **Driven by:** BR-ORD-016, BR-EXT-002, BR-EXT-005, BR-EXT-006, BR-EXT-007, BR-EXT-008, BR-PA-020, BR-PA-022
- **Request:** `CapturePaymentRequest`
- **Rules:** prior authorization required; capture amount cannot exceed remaining authorized balance; duplicate request is replayed; no direct order mutation.
- **Response:** `201 PaymentOperation`
- **Errors:** 400, 401, 404, 409, 402, 502

### POST `/api/v1/payment-intents/{paymentIntentId}/refunds`

- **Driven by:** BR-ORD-017, BR-EXT-003, BR-EXT-004, BR-EXT-005, BR-EXT-006, BR-EXT-007, BR-EXT-008, BR-PA-020, BR-PA-022
- **Request:** `RefundPaymentRequest`
- **Rules:** captured balance required; cumulative refund invariant enforced; exact decimal arithmetic; provider refund reference persisted; order totals are not changed by MS-06.
- **Response:** `201 Refund`
- **Errors:** 400, 401, 404, 409, 422, 402, 502

### GET `/api/v1/payment-intents/{paymentIntentId}/transactions`

- **Driven by:** BR-PA-021
- **Query parameters:** `page`, `pageSize`
- **Response:** `PaymentTransactionListResponse`
- **Ordering:** `sequenceNo ASC, occurredAt ASC`
- **Errors:** 401, 404, 500

### GET `/api/v1/payment-operations/{paymentOperationId}`

- **Driven by:** BR-ORD-015 and BR-PA-022
- **Response:** `PaymentOperation`
- **Errors:** 401, 404, 500

### POST `/api/v1/callbacks/{providerCode}`

- **Driven by:** BR-PA-023
- **Authentication:** provider-specific verification performed by the adapter; normal user JWT is not required.
- **Request:** provider event ID, provider reference, event type, provider payload, signature headers
- **Response:** `202 CallbackReceipt`
- **Rules:** callback is stored before processing; unverified or duplicate callbacks do not change payment state.
- **Errors:** 400, 401, 404, 409, 422

### GET `/api/v1/reconciliation/capturable`

- **Driven by:** BR-ORD-016, BR-PA-021
- **Query parameters:** `from`, `to`, `page`, `pageSize`
- **Semantics:** explicit UTC date-time bounds; no legacy default timezone behavior.
- **Response:** `CapturablePaymentListResponse`
- **Errors:** 400, 401, 500

## Events Published

| Event | Trigger | Consumer |
|---|---|---|
| `PaymentAuthorized.v1` | Successful authorization | MS-05 |
| `PaymentCaptured.v1` | Successful capture | MS-05 |
| `PaymentRefunded.v1` | Successful refund | MS-05 |
| `PaymentFailed.v1` | Definitive provider or validation failure | MS-05, operations |
| `PaymentReconciliationRequired.v1` | Provider/local outcome divergence | MS-05, MS-12 |

## Events Consumed

| Event | Source | Action |
|---|---|---|
| `OrderSubmitted.v1` | MS-04 | Create or bind immutable amount/currency payment context |
| `PaymentRequested.v1` | MS-05 | Begin authorization or capture workflow using the order snapshot |
| `ConfigurationReferenceChanged` | MS-11 | Refresh future-operation configuration; never mutate existing intent configuration |

## Resilience

- Provider timeout: 10 seconds by default, provider-specific override allowed only after BA approval.
- Retry: no automatic retry for non-idempotent provider calls unless the adapter provides a provider-safe request key.
- Circuit breaker: per provider and store, opened after five failures in one minute with 50% half-open sampling.
- Callback processing: at-least-once delivery with inbox deduplication.
- Outbox publishing: at-least-once delivery with event ID deduplication by consumers.
