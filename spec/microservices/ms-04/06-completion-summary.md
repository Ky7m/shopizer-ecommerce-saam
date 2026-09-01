# MS-04 Cart and Checkout — Completion Summary

**Date:** 2026-09-01  
**Service ID:** MS-04  
**Status:** 🟡 In Progress — architect-complete, pending BA sign-off and Phase 4b placement decisions  
**Analysis mode:** Hybrid — CAST plus direct source reading

## Verified artifact counts

| Artifact | Actual count |
|---|---:|
| Business rules in `01-business-rules.md` | 20 |
| Domain-model tables in executable DDL | 10 |
| API operations in `03-api-design.md` | 17 |
| OpenAPI path operations in `04-api-contract.yaml` | 17 |
| Published target events | 1 |
| Consumed target events | 2 |
| Mandatory business-logic source files read | 15 |
| Supporting model/configuration source files read | 19 |
| Total unique source files recorded in extraction evidence | 34 |

The OpenAPI operation count matches the API-design operation count: **17 / 17**.

## Scope completed

- Cart creation with anonymous and customer context.
- Store and tenant scope.
- SKU/product sellability checks.
- Product-attribute association checks.
- Duplicate physical-line merge behavior.
- Single-line and multi-line update behavior.
- Quantity-zero deletion ordering.
- Cart hydration, repricing, subtotal calculation, and obsolete-cart handling.
- Anonymous-to-authenticated cart merge boundary.
- Digital-only and physical-cart shipping behavior.
- Billing fallback for missing delivery postal code.
- Shipping quote reference and expiry model.
- Promotion timestamp and expiry behavior.
- Total orchestration across price allocations, variations, shipping, handling, tax, and grand total.
- Server-side submitted-amount validation.
- Immutable checkout line and total snapshots.
- Authenticated cart ownership.
- Anonymous customer construction boundary.
- Payment-method configuration handoff.
- Explicit checkout lifecycle.
- Durable idempotency.
- Local transaction plus outbox orchestration.
- Explicit downstream service boundaries.

## Endpoint coverage

| Endpoint | Method | Status | Driving BR-IDs |
|---|---|---|---|
| `/api/v1/cart` | POST | COVERED | BR-SC-CRE-001, BR-SC-SEL-002, BR-SC-ATR-003, BR-SC-MRG-004 |
| `/api/v1/cart/{code}` | GET | COVERED | BR-SC-HYD-006, BR-SC-PRO-011 |
| `/api/v1/cart/{code}` | PUT | COVERED | BR-SC-SEL-002, BR-SC-UPD-005 |
| `/api/v1/cart/{code}/multi` | POST | COVERED | BR-SC-SEL-002, BR-SC-UPD-005, BR-SC-ATR-003 |
| `/api/v1/cart/{code}/promo/{promoCode}` | POST | COVERED | BR-SC-PRO-011 |
| `/api/v1/cart/{code}/product/{sku}` | DELETE | COVERED | BR-SC-UPD-005 |
| `/api/v1/auth/customer/cart` | GET | COVERED | BR-CO-AUT-012, BR-SC-HYD-006, BR-SC-MRG-007 |
| `/api/v1/auth/customer/{id}/cart` | GET | COVERED; deprecated | BR-CO-AUT-012 |
| `/api/v1/customers/{id}/cart` | POST | COVERED; explicit unsupported compatibility endpoint | — |
| `/api/v1/auth/cart/{code}/checkout` | POST | COVERED | BR-CO-AUT-012, BR-CO-SNP-014, BR-CO-TOT-015, BR-CO-IDM-017, BR-CO-ORC-019 |
| `/api/v1/cart/{code}/checkout` | POST | COVERED | BR-CO-CUS-013, BR-CO-SNP-014, BR-CO-TOT-015, BR-CO-IDM-017, BR-CO-ORC-019 |
| `/api/v1/auth/cart/{code}/shipping` | GET | COVERED | BR-CO-AUT-012, BR-SC-SHP-008, BR-SC-SHP-009 |
| `/api/v1/cart/{code}/shipping` | POST | COVERED | BR-SC-SHP-008, BR-SC-SHP-009 |
| `/api/v1/auth/cart/{id}/total` | GET | COVERED | BR-CO-AUT-012, BR-SC-TOT-010 |
| `/api/v1/cart/{code}/total` | GET | COVERED | BR-SC-TOT-010, BR-SC-PRO-011 |
| `/api/v1/auth/cart/{code}/payment/init` | POST | COVERED | BR-CO-AUT-012, BR-CO-PAY-016 |
| `/api/v1/cart/{code}/payment/init` | POST | COVERED | BR-CO-PAY-016 |

## Semantic preservation

| Source component | Flagged dimensions | Status | Notes |
|---|---|---|---|
| `ShoppingCartApi.java` | none | OK | Cart endpoint semantics preserved |
| `ShoppingCartFacadeImpl.java` | none | OK | Cart creation, merge, update, deletion, and promotion paths covered |
| `ShoppingCartServiceImpl.java` | none | OK | Hydration, orphan cleanup, shipping-product filtering, and cart lifecycle covered |
| `ShoppingCartCalculationServiceImpl.java` | none | OK | Delegation and persistence refresh covered |
| `OrderApi.java` | none | OK | Authenticated and anonymous checkout boundaries covered |
| `OrderFacadeImpl.java` | state transitions | FLAGGED → target-resolved | Legacy cart completion was implicit; explicit checkout lifecycle added |
| `OrderServiceImpl.java` | none | OK with ownership split | Total formula and legacy persistence sequence retained; order writes moved to MS-05 |
| `OrderTotalServiceImpl.java` | none | OK with ownership split | Processor fan-out retained as MS-07 dependency |
| `OrderProductPopulator.java` | none | OK with ownership split | Snapshot fields retained without assigning MS-05 tables to MS-04 |
| `OrderPaymentApi.java` | none | OK | Payment initialization retained; capture/refund/authorize excluded |
| `PersistablePaymentPopulator.java` | none | OK | Amount normalization and payment handoff fields covered |
| `PromoCoupon.drl` | constants | FLAGGED → provider-owned | Current rule contains `Test1234`, 10%, and `31-Oct-2025`; MS-07 owns execution |
| `ShippingDecision.drl` | constants | FLAGGED → provider-owned | Canada/weight/size/province routing remains MS-09/MS-12-owned |
| `PriceByDistance.drl` | constants | FLAGGED → provider-owned | Distance tiers remain MS-09-owned |
| `PriceByDistance2.drl` | constants | FLAGGED → provider-owned | Distance tiers remain MS-09-owned |

## Hidden-engine findings

### Cart totals

A hidden calculation engine is present. Total behavior is distributed across:

```text
ShoppingCartFacadeImpl
  -> ShoppingCartServiceImpl
  -> ShoppingCartCalculationServiceImpl
  -> OrderServiceImpl.caculateShoppingCart
  -> OrderServiceImpl.caculateOrder
  -> OrderTotalServiceImpl
  -> ProcessorsConfiguration
  -> PromoCodeCalculatorModule
  -> PromoCoupon.drl
  -> pricing, shipping, and tax services
```

Provider-owned algorithms are not duplicated in MS-04. MS-04 stores versioned quote references and a canonical total snapshot.

### Checkout state

No explicit legacy checkout-session state machine was found. Legacy markers were:

- `ShoppingCart.orderId` for completion association.
- `ShoppingCart.obsolete` for cleanup.
- `OrderStatus.ORDERED` and possible `PROCESSED` promotion outside MS-04.

The target model therefore introduces `Open`, `Quoted`, `Frozen`, `Submitted`, `Failed`, and `Expired`.

### Idempotency

No legacy idempotency implementation was found. No idempotency key, replay record, uniqueness constraint, or idempotency table was found in the inspected source/configuration search. The target model adds `checkout_idempotency_key`.

### Orchestration

Legacy checkout directly coordinated:

- customer resolution;
- cart reload;
- product and attribute re-resolution;
- order-line snapshot creation;
- shipping quote lookup;
- total calculation;
- payment-provider invocation;
- order persistence;
- transaction persistence;
- inventory decrement;
- cart completion;
- email/download notification.

The target split is:

1. MS-04 local cart/checkout transaction.
2. MS-04 durable `OrderSubmitted` outbox event.
3. MS-05 order creation and order lifecycle.
4. MS-05 `PaymentRequested` publication.
5. MS-06 provider transaction state.
6. MS-02 inventory reservation/decrement.
7. MS-12 notification and delivery adapters.

## Placement candidates for Phase 4b

| Candidate | Evidence | Default |
|---|---|---|
| Cart hydration | Per-cart line loop, provider calls, orphan deletion during reads | App/domain |
| Total orchestration | Per-cart aggregation with provider allocations | App/domain |
| Checkout snapshot | New local transaction over cart lines and totals | App + database |
| Idempotency | New unique scoped key and replay response | App middleware + database |
| Outbox | New durable event row before publication | App + database |
| Broad order fetch joins | CAST order graph includes orders, products, totals, history, downloads, attributes, and prices | MS-05 read model |

No stored procedure, scheduled batch, or database-resident decision engine was found for MS-04. No candidate is assigned to the database logic-object table at this phase.

## Cross-service boundary register

| Boundary | MS-04 action | Owning service |
|---|---|---|
| Customer identity and address | Resolve and carry opaque customer/address references | MS-01 |
| Product facts and availability | Validate SKU and consume availability/reservation result | MS-02 |
| Pricing and promotion | Request current allocation and version | MS-07 |
| Tax | Request tax quote and version | MS-08 |
| Shipping | Request options, validate selected quote, store reference | MS-09 |
| Order persistence/status | Publish immutable snapshot; never write order tables | MS-05 |
| Payment provider state | Publish payment handoff; never write transactions | MS-06 |
| Store/configuration | Consume scoped configuration | MS-10/MS-11 |
| Email/download/provider adapters | Consume downstream events | MS-12 |

## Known Phase 4a review items

1. Confirm whether promotion expiry is intended to be calendar-date based or elapsed-time based.
2. Confirm canonical monetary precision and rounding behavior.
3. Confirm whether anonymous public payment initialization may proceed without a customer/cart ownership proof.
4. Confirm checkout-session expiration duration.
5. Confirm retry and compensation semantics for payment and inventory outcomes.
6. Confirm whether `OrderSubmitted` should be emitted before or after inventory reservation.
7. Confirm whether partial payment failure leaves the checkout in `Failed` or allows a new attempt against the same frozen snapshot.
8. Confirm provider-specific payment token retention and redaction policy.
9. Confirm customer cart merge behavior for attribute-bearing duplicate lines.
10. Confirm whether the legacy `Test1234` promotion is obsolete; it was expired at extraction time.
11. Confirm shipping quote TTL and whether provider quote versions are mandatory.
12. Confirm customer download entitlement route in MS-05/MS-12.

## Automation assessment

| Dimension | Before | After | Notes |
|---|---:|---:|---|
| Statement clarity | 45% | 92% | Semantic statements separated from source pseudocode |
| Algorithm completeness | 40% | 88% | Totals, scope, lifecycle, snapshot, and handoff algorithms explicit |
| Data-model readiness | 35% | 93% | Executable PostgreSQL DDL and invariant model provided |
| Edge-case coverage | 30% | 86% | Digital-only carts, stale quotes, duplicate keys, orphan attributes, and terminal states covered |
| Overall automatibility | 38% | 90% | Pending BA decisions and provider contract freeze |

These scores are provisional until independent validation and BA sign-off.

## Completion statement

The MS-04 extraction package contains 20 uniquely identified rules, 10 executable target tables, 17 API operations with matching OpenAPI operations, explicit service ownership boundaries, source line evidence, hidden-engine findings, lifecycle models, invariants, and placement evidence.

The package is ready for Phase 4a business-rule review and Phase 4b implementation-roadmap scoring. It is not marked 100% complete until those reviews are approved.
