# Catalog and Product — Phase 4 Completion Summary

**Service:** MS-02 Catalog and Product  
**Port:** 8102  
**Schema:** `catalog_product`  
**Status:** 🟡 Phase 4 complete; Phase 4a BA review and Phase 4b placement review pending.

## Decomposition outcome

The package contains **41 rules**:

- 26 carry-forward catalog rules: `BR-CAT-001..019`, `BR-CAT-025..031`
- 1 carry-forward order/inventory rule: `BR-ORD-012`
- 2 carry-forward integration rules: `BR-EXT-019..020`
- 4 carry-forward UI rules: `BR-UI-003..006`
- 8 net-new rules: `BR-CAT-032..039`

The count is a decomposition outcome from the CAST-guided deep read of product persistence, category hierarchy, storefront predicates, pricing, mapping, image processing, event handling, administration UI, and inventory decrement paths. The net-new findings include:

1. Visibility mapping can overwrite a caller-supplied effective date.
2. Product localization requires deterministic availability/language selection.
3. Category moves need explicit cross-store and cycle guards.
4. Media deletion does not refresh the legacy derived product projection.
5. Product events must reload a complete aggregate before publishing.
6. Inventory reservation idempotency was absent from the legacy decrement path.
7. Product deletion requires an explicit no-orphan-reference invariant.
8. Reservation commit/release needs a closed target state machine.

## Artifact counts

| Artifact | Count |
|---|---:|
| Business-rule IDs | 41 |
| PostgreSQL tables | 15 |
| PostgreSQL indexes/unique indexes | 7 |
| Entity state machines | 4 |
| Data invariants | 12 |
| API path items | 24 |
| API operation methods | 35 |
| OpenAPI schemas | 38 |
| Semantic preservation tables | 41 |
| Source files directly read | 29 |
| CAST-guided component families | 9 |

## Approved boundary

- MS-02 owns product, category, variant, availability, media, and inventory reservation/decrement.
- MS-10 owns merchant/store scope. `tenantId` and `storeId` remain opaque in MS-02.
- MS-03 owns search reads and index projections.
- MS-04 and MS-05 use reservation IDs and events; they do not write MS-02 data directly.
- Media binaries are stored through a provider-neutral boundary; provider execution remains external/MS-12 territory.
- Pricing calculations represented here are catalog display/variation-price behavior. Commercial promotion ownership remains MS-07.

## Endpoint coverage

| Area | Operations | Status |
|---|---:|---|
| Product CRUD and reads | 8 | Covered |
| Product/category association | 2 | Covered |
| Product availability | 2 | Covered |
| Inventory reservations | 3 | Covered |
| Product media | 2 | Covered |
| Category CRUD and hierarchy | 9 | Covered |
| Variants | 7 | Covered |
| Option price calculation | 1 | Covered |

## Semantic preservation status

| Area | Status |
|---|---|
| SKU and store scoping | OK |
| Product availability precondition | OK |
| Category lineage and recursive moves | FLAGGED/GAP — target adds cycle and atomicity guards |
| Storefront eligibility | OK |
| Pricing fallback and special windows | FLAGGED — variant null fallback is made explicit |
| Product media provider behavior | OK with observable partial-failure status |
| Search projection handoff | GAP closed through versioned MS-02 events |
| Legacy inventory decrement | GAP closed through atomic idempotent reservations |
| UI localization fallback | OK |
| Pagination/count query behavior | GAP closed through shared predicate compilation |

## Placement candidates for Phase 4b

| Candidate | Legacy tier | Volume signal | Set-vs-row | Target question |
|---|---|---|---|---|
| Category subtree lineage recalculation | App service recursion | Potentially all descendants in subtree | Row-at-a-time legacy loop | Evaluate app transaction versus database recursive query |
| Category subtree deletion | App service loop | All categories/products in subtree | Row-at-a-time legacy loop | Evaluate set-based database deletion only if integrity evidence supports it |
| Product listing count/fetch | JPQL query | High-frequency storefront reads | Set-based | Consider read projection/materialized view |
| Price aggregation | Application utility | Per product/variant request | Row-oriented | Keep app tier unless profiling shows high-volume batch use |
| Image resize | Application/provider path | Per uploaded image | Per-file | Keep provider boundary; do not place in database |
| Inventory reservation | Order application path in legacy | High-contention stock rows | Atomic set-based update required | Evaluate DB conditional update or serializable transaction |
| Reservation expiry sweep | Target capability | All held reservations | Batch candidate | Phase 4b placement decision required |

## Events

| Event | Producer | Consumer | Delivery |
|---|---|---|---|
| `ProductChanged.v1` | MS-02 | MS-03, MS-04, MS-07 | Transactional outbox |
| `CategoryChanged.v1` | MS-02 | MS-03 | Transactional outbox |
| `AvailabilityChanged.v1` | MS-02 | MS-04, MS-05 | Transactional outbox |
| `MediaChanged.v1` | MS-02 | MS-03, MS-12 | Transactional outbox |
| `InventoryReservationChanged.v1` | MS-02 | MS-04, MS-05 | Transactional outbox |

## Open BA decisions

1. Confirm whether orphan products under category deletion are detached, deleted, or rejected by default.
2. Confirm whether a product without a wildcard availability may be storefront-visible in a country-specific-only configuration.
3. Confirm whether negative option adjustments are supported by MS-07 or remain excluded from MS-02 display pricing.
4. Confirm media size/type/virus-scanning limits.
5. Confirm reservation TTL, maximum quantity, and expiration sweep cadence.
6. Confirm whether product deletion is hard delete or tombstone-first with delayed physical cleanup.
7. Confirm whether catalog administrator roles are exactly the legacy four groups or mapped to MS-01 permissions.
8. Confirm whether friendly URLs must be unique per store/language.

## Readiness

The package is implementation-oriented and contains executable PostgreSQL DDL, closed entity state machines, invariants, 41 fully traced rules, a target API design, and an OpenAPI 3.1 contract. It is ready for validator execution and BA review, but not yet frozen for Phase 5 implementation.
