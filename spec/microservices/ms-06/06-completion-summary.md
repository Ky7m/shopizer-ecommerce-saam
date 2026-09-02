
# MS-06 Payments — Completion Summary

**Version:** 1.0  
**Date:** 2026-09-01  
**Status:** 🟡 In Progress — pending BA review and Phase 4b placement decisions  
**Service ID:** MS-06  
**Port:** 8106  
**Database schema:** `payments`  
**Analysis mode:** Hybrid  
**Target stack assumption:** .NET 10 — non-binding until Phase 4b confirmation

## Decomposition Outcome

This extraction produced **19 business rules** from **23 mandatory source files**, decomposed along **19 behavioral seams**:

- 15 assigned Phase 1 rules re-extracted at Phase 4 depth.
- 4 net-new findings discovered during direct source review:
  1. Transaction history uses lexicographic operation-type ordering rather than chronological ordering.
  2. No legacy idempotency mechanism exists for payment-mutating operations.
  3. Payment amount and currency are not explicitly bound to an immutable payment intent.
  4. No confirmed generic callback verification path exists in the analyzed application.

The count is a decomposition outcome, not a target match. Several Phase 1 rules remain intentionally distinct because they describe different seams: provider eligibility, configuration dispatch, capture ownership, refund accounting, provider-specific behavior, and cross-service event boundaries.

## Artifact Counts

| Artifact | Count |
|---|---:|
| Business rules | 19 |
| Domain tables | 8 |
| Database logic objects | 2 |
| Entity lifecycle models | 3 |
| Data invariants | 7 |
| API operation methods | 12 |
| Published event types | 5 |
| Consumed event types | 3 |
| Mandatory source files read | 23 |
| Registered active provider adapters | 6 |
| Unregistered provider adapters | 1 (`PayPalRestPayment`) |
| Legacy admin payment stubs | 3 |
| Legacy idempotency mechanisms found | 0 |
| Confirmed legacy callback endpoints | 0 |

## Source Components Reviewed

| Component | CAST ID | Complexity | Rules |
|---|---:|---:|---:|
| `PaymentServiceImpl` | 12989 | 102 | 8 |
| `BeanStreamPayment` | 11652 | 61 | 1 |
| `Stripe3Payment` | 11710 | 47 | 1 |
| `PayPalExpressCheckoutPayment` | 11688 | 44 | 1 |
| `StripePayment` | 11679 | 43 | 1 |
| `BraintreePayment` | 11668 | 39 | 1 |
| `OrderPaymentApi` | 29357 | 30 | 3 |
| `TransactionServiceImpl` | 13115 | 24 | 2 |
| `PaymentApi` | 30425 | 11 | 1 |
| `Transaction` | 17360 | 4 | supporting model evidence |
| `MoneyOrderPayment` | 11678 | 3 | 1 |
| `PersistablePaymentPopulator` | 19828 | 2 | 1 |
| `ReadableTransactionPopulator` | 19820 | 7 | supporting response evidence |
| Payment model and SPI files | — | — | supporting evidence |
| Provider registry resource | — | — | provider registration evidence |
| Order service/facade | — | — | boundary evidence |

## Endpoint Coverage

| Endpoint | Method | Status | Driving BR-IDs |
|---|---|---|---|
| `/api/v1/payment-methods` | GET | COVERED | BR-ORD-014, BR-EXT-001 |
| `/api/v1/payment-methods/{code}` | GET | COVERED | BR-ORD-014, BR-EXT-001 |
| `/api/v1/payment-methods/{code}/configuration` | PUT | COVERED | BR-EXT-001, provider-specific BRs |
| `/api/v1/payment-intents` | POST | COVERED | BR-ORD-014, BR-EXT-001, BR-EXT-005, BR-EXT-007, BR-EXT-009, BR-UI-015, BR-PA-020, BR-PA-022 |
| `/api/v1/payment-intents/{paymentIntentId}` | GET | CRUD/read behavior | BR-PA-021 |
| `/api/v1/payment-intents/{paymentIntentId}/authorize` | POST | COVERED | BR-ORD-015, BR-EXT-004 through BR-EXT-008, BR-UI-015, BR-PA-020, BR-PA-022 |
| `/api/v1/payment-intents/{paymentIntentId}/capture` | POST | COVERED | BR-ORD-016, BR-EXT-002, BR-EXT-005 through BR-EXT-008, BR-PA-020, BR-PA-022 |
| `/api/v1/payment-intents/{paymentIntentId}/refunds` | POST | COVERED | BR-ORD-017, BR-EXT-003 through BR-EXT-008, BR-PA-020, BR-PA-022 |
| `/api/v1/payment-intents/{paymentIntentId}/transactions` | GET | COVERED | BR-PA-021 |
| `/api/v1/payment-operations/{paymentOperationId}` | GET | COVERED | BR-ORD-015, BR-PA-022 |
| `/api/v1/callbacks/{providerCode}` | POST | COVERED with unresolved legacy provenance | BR-PA-023 |
| `/api/v1/reconciliation/capturable` | GET | COVERED | BR-ORD-016, BR-PA-021 |

The OpenAPI contract contains the same **12 operation methods**.

## Provider Coverage

| Provider | Active registration | Authorization | Capture | Refund | Initialization | Key finding |
|---|---|---|---|---|---|---|
| Stripe classic | Yes | Charge with `capture=false` | Charge capture | Charge refund | Token path | Credential/token validation present |
| Stripe 3 | Yes | PaymentIntent retrieval | Manual capture | PaymentIntent refund | PaymentIntent creation | Amount/status/reference inconsistencies require correction |
| Braintree | Yes | Sale/nonce | Submit settlement | Transaction refund | Client token | Sandbox/production selection explicit |
| PayPal Express | Yes | Express checkout | DoCapture | RefundTransaction | SetExpressCheckout | Direct SPI init is unimplemented; helper exists |
| Beanstream | Yes | Form POST | PAC request | R request | Unimplemented | Response approval field required |
| Money Order | Yes | Local | Unsupported | Unsupported | Unimplemented | Local authorization-plus-capture only |
| PayPal REST | No | Unresolved | Unresolved | Unresolved | Unresolved | Excluded from active provider set |

## Boundary Decisions

- MS-06 owns payment state and provider operation records.
- MS-05 alone owns order status and order-total transitions.
- MS-04/MS-05 provide immutable amount and currency snapshots.
- MS-11 owns merchant/module configuration persistence.
- MS-12 may supply secret storage, generic outbound transport, callback ingress, and telemetry.
- No cross-service foreign keys are present in the target DDL.
- No provider credentials, PAN, CVV, raw secret, or unrestricted provider payload is persisted in payment transaction records.

## Failure and Recovery Model

| Failure | Target behavior |
|---|---|
| Missing/inactive provider configuration | Reject before provider call |
| Missing provider token | 422 validation response |
| Provider decline | 402 response and `PaymentFailed` event where applicable |
| Invalid provider response | 502 and `ReconciliationRequired` when outcome is ambiguous |
| Provider success/local persistence failure | Persist operation as `ReconciliationRequired`; do not report definitive success |
| Duplicate command | Return original idempotent result |
| Conflicting idempotency key | 409 |
| Refund exceeds cumulative balance | 422/409; database trigger also rejects |
| Unverified callback | Store callback as rejected; no payment-state transition |
| Duplicate callback | Store as duplicate; no repeated transition |
| Invalid transaction history | Mark reconciliation required; do not infer state from operation-name sorting |

## Automatibility Assessment

| Dimension | Score | Rationale |
|---|---:|---|
| Statement clarity | 86% | Rules use domain statements and explicit constraints |
| Algorithm completeness | 78% | Refund, idempotency, state reduction, and provider dispatch are explicit; provider callback algorithms remain adapter-specific |
| Data-model readiness | 88% | Executable DDL, invariants, lifecycle models, and indexes provided |
| Edge-case coverage | 82% | Includes retries, duplicate callbacks, cumulative refunds, stale amounts, provider/local divergence, and unsupported providers |
| Overall preliminary automatibility | 83% | Provisional until BA review and target-provider contracts are confirmed |

## Outstanding BA Decisions

1. Whether Money Order should become `Captured` immediately or remain `PendingManualSettlement`.
2. Whether partial capture is permitted for each provider.
3. Provider-specific callback verification and event-status mappings.
4. Provider timeout and retry limits.
5. Whether card validation remains in MS-06 when all production card data is tokenized.
6. PayPal Express redirect lifetime and token expiry policy.
7. Whether PayPal REST is obsolete or should be separately reintroduced.
8. Retention and redaction policy for protected callback payloads.
9. Exact MS-05 event-consumer behavior for `PaymentCaptured` and `PaymentRefunded`.
10. Secret-reference format and configuration-version lifecycle owned by MS-11/MS-12.

## Deferred Deliverables

The user-requested package intentionally contains six files. Cross-service `05-dependencies.md`, `07-workflows.md`, shared event schemas, BA review, graph import, telemetry, and Phase 4b placement decisions remain pending.

## Phase 4a BA disposition

Mode A agent defaults were approved on 2026-09-02. 19 rules remain active after 0 approved obsolete-rule removal(s). Retained rules carry explicit Classification and Weight metadata; no rules were deferred, merged, or simplified without BA-specific guidance.
