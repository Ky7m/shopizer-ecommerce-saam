# Phase 3 Microservice Gap Analysis

## Scope and outcome

This convergence pass reconciles the seven Phase 1 extraction summaries with the approved
12-service Phase 2 architecture. The source inventory contains 180 unique business rules:
31 catalog, 28 customer, 24 integration, 27 merchant, 19 order, 36 pricing, and 15 UI
interaction rules. Every rule has exactly one target service assignment.

The five ownership decisions raised by convergence were approved on 2026-09-01 and are
recorded in `assessment/assessment_agenda.md`. No top-down extraction gap remains: all 12
design-named flows have at least one backing extracted BR-ID.

## Complete feature and rule matrix

The ranges below are inclusive. Each range expands to an individual BR-ID, and ranges do not
overlap. This is the complete source-to-target assignment ledger for all 180 extracted rules.

| Target service | Service ID | Assigned source features/rules | Count | Status |
|---|---|---|---:|---|
| Customer and Identity | MS-01 | `BR-CUS-001..028`, `BR-UI-001..002` | 30 | Mapped |
| Catalog and Product | MS-02 | `BR-CAT-001..019`, `BR-CAT-025..031`, `BR-ORD-012`, `BR-EXT-019..020`, `BR-UI-003..006` | 33 | Mapped |
| Search | MS-03 | `BR-CAT-020..024`, `BR-EXT-023..024` | 7 | Mapped |
| Cart and Checkout | MS-04 | `BR-ORD-001..008`, `BR-ORD-010..011`, `BR-UI-010`, `BR-UI-012`, `BR-UI-014` | 13 | Mapped |
| Order Management | MS-05 | `BR-ORD-013`, `BR-ORD-018`, `BR-UI-009` | 3 | Mapped |
| Payments | MS-06 | `BR-ORD-014..017`, `BR-ORD-019`, `BR-EXT-001..009`, `BR-UI-015` | 15 | Mapped |
| Pricing and Promotions | MS-07 | `BR-ORD-009`, `BR-PRC-001..013`, `BR-UI-011`, `BR-UI-013` | 16 | Mapped |
| Tax | MS-08 | `BR-PRC-014..021` | 8 | Mapped |
| Shipping | MS-09 | `BR-PRC-022..036`, `BR-EXT-010..016`, `BR-EXT-018`, `BR-UI-008` | 24 | Mapped |
| Merchant and Store Administration | MS-10 | `BR-MER-001..012`, `BR-UI-007` | 13 | Mapped |
| Content and Configuration | MS-11 | `BR-MER-013..022`, `BR-MER-027`, `BR-EXT-021` | 12 | Mapped |
| Platform Integrations | MS-12 | `BR-MER-023..026`, `BR-EXT-017`, `BR-EXT-022` | 6 | Mapped |
| **Total** |  |  | **180** | **100% assigned** |

### Assignment rationale

- Customer identity, users, groups, permissions, addresses, and reset flows remain in MS-01.
- Product, category, variant, availability, image, and catalog rules remain in MS-02.
  The approved inventory decision makes MS-02 the owner of atomic availability reservation and
  decrement; checkout and order services consume that contract.
- Search indexing and localized search documents are derived in MS-03.
- Cart mutation, quote orchestration, snapshot freezing, and order submission remain in MS-04.
- Order lifecycle, immutable order snapshots, status history, and download entitlements belong
  to MS-05. MS-05 consumes authenticated payment events and alone changes order state.
- Provider state, callbacks, authorization, capture, and refund belong to MS-06.
- Pricing, promotions, coupons, tax, and shipping calculations are separated into MS-07, MS-08,
  and MS-09 respectively; none writes checkout or order totals.
- Merchant/store lifecycle belongs to MS-10. Content and merchant/module configuration belong to
  MS-11. External delivery and adapter execution belong to MS-12.

## Top-down flow coverage

Every operation named in `modernization/shopizer-sequence-diagrams.md` has at least one
bottom-up rule. Therefore, there are no zero-backing extraction gaps to route back to Phase 1.

| Design flow | Backing BR-IDs | Coverage |
|---|---|---|
| Browse catalog | `BR-CAT-009..015`, `BR-UI-004` | Covered |
| Search products | `BR-CAT-020..024`, `BR-EXT-023..024` | Covered |
| Customer registration and login | `BR-CUS-003`, `BR-CUS-006..010`, `BR-CUS-024..027` | Covered |
| Update cart | `BR-ORD-001..005`, `BR-UI-010`, `BR-UI-012` | Covered |
| Checkout quote | `BR-ORD-006..010`, `BR-PRC-001..036`, `BR-UI-014` | Covered |
| Submit order | `BR-ORD-010..015` | Covered |
| Payment authorization and callback | `BR-ORD-014..017`, `BR-EXT-001..009` | Covered |
| Apply promotion | `BR-ORD-007`, `BR-ORD-009`, `BR-PRC-009..011`, `BR-UI-013` | Covered |
| Tax and shipping quote | `BR-PRC-014..036`, `BR-EXT-010..018` | Covered |
| Merchant/store setup | `BR-MER-001..012`, `BR-UI-007` | Covered |
| Publish content | `BR-MER-013..018`, `BR-EXT-021` | Covered |
| Catalog indexing | `BR-CAT-020..022`, `BR-EXT-023..024` | Covered |

## Gaps and resolutions

### Source-to-target gaps

No source feature is unmapped after the approved decisions. The following risks were identified
during convergence and resolved as explicit Phase 4 contract/model work rather than dropped:

| Gap/risk | Severity | Resolution |
|---|---|---|
| Inventory ownership and atomic decrement were embedded in checkout/order code and absent as a target aggregate | Critical | MS-02 owns availability and reservation/decrement. Define an idempotent reservation API/event and concurrency behavior in Phase 4. |
| Payment callbacks and order transitions crossed the legacy payment/order boundary | Critical | MS-06 publishes authenticated payment events; MS-05 owns order transitions and compensation rules. |
| Digital download records had no target customer entitlement flow | Critical | MS-05 owns entitlements; MS-12 delivers notifications/files. Define entitlement retrieval and expiry/count behavior in Phase 4. |
| Administrative capture/refund APIs were stubbed at the legacy API surface | Critical | Capability is required in the target. MS-06 exposes explicit capture/refund contracts with cumulative partial-refund, provider validation, and idempotency rules. |
| Module configuration and integration adapter concerns overlapped | Important | MS-11 owns merchant/module configuration; MS-12 owns adapter execution and delivery attempts. |
| Legacy defects and incomplete provider paths could be reproduced accidentally | Important | Preserve business intent, not defects. Carry catalog predicate, image-event, variant fallback, payment provider, tax, shipping, and storage findings into Phase 4a review. |

### Target-to-source gaps

These are intentional target capabilities and not losses from legacy:

- OIDC and gateway/BFF token validation with tenant propagation.
- RabbitMQ versioned events, transactional outbox, inbox/idempotency, retries, and dead letters.
- Independent PostgreSQL schema ownership and projection freshness metadata.
- Checkout/order/payment saga compensation and immutable snapshots.
- OpenTelemetry traces, operational metrics, audit records, canary deployment, and rollback.
- Explicit download entitlement retrieval and integration delivery contracts.

### Boundary violations and split contracts

| Legacy split | Target contract | Transaction rule |
|---|---|---|
| Product availability used by cart/order | MS-02 reservation/availability API or event | MS-02 atomically changes availability; callers retain only opaque reservation IDs. |
| Payment provider state used to advance orders | MS-06 payment events consumed by MS-05 | No distributed transaction; outbox/inbox and idempotent state transitions. |
| Download persistence and notification | MS-05 entitlement event consumed by MS-12 | MS-05 is authoritative for entitlement; MS-12 is authoritative for delivery attempt state. |
| Merchant module configuration and adapter execution | MS-11 configuration API/events consumed by MS-12 | Configuration writes stay in MS-11; adapter delivery/retry writes stay in MS-12. |

## Domain boundary validation

| Service | Cohesion | Coupling | Data ownership | Transaction scope | Result |
|---|---|---|---|---|---|
| MS-01 Customer and Identity | Identity, customer, address, role, and consent concerns align | OIDC provider only plus authenticated context consumers | Owns customer and permission data | Customer/account mutations stay local | Pass |
| MS-02 Catalog and Product | Product, category, variant, availability, and media align | MS-10 store validation; MS-03 consumes events; MS-04 calls availability | Owns catalog and inventory availability | Product and reservation writes are local | Pass with Phase 4 reservation contract |
| MS-03 Search | Derived index and freshness align | Event consumers only | Owns index documents and rebuild state | Index upsert is local/idempotent | Pass |
| MS-04 Cart and Checkout | Cart, quote, snapshot, and submission align | Short-lived calls to MS-01/02/07/08/09; event to MS-05 | Owns carts and checkout snapshots | Cart/checkout mutation local; submission is outbox-backed | Pass |
| MS-05 Order Management | Order lifecycle, invoice, history, and entitlements align | Consumes MS-04/MS-06; publishes lifecycle/integration events | Owns order snapshots and lifecycle | State transitions local and event-driven | Pass with saga contracts |
| MS-06 Payments | Payment intent, transactions, callbacks, and refunds align | External providers; events to MS-05 | Owns provider references and payment state | Provider callback/state mutation local | Pass |
| MS-07 Pricing and Promotions | Price lists, promotions, and coupons align | MS-10 scope validation; called by MS-04 | Owns commercial definitions and reservations | Calculation/reservation local | Pass |
| MS-08 Tax | Jurisdictions, rates, profiles, and quotes align | Optional provider via MS-12 | Owns tax rules and quote audit | Quote calculation local | Pass |
| MS-09 Shipping | Methods, zones, packaging, and quotes align | Carrier adapters via MS-12 | Owns shipping configuration and quotes | Quote calculation/persistence local | Pass |
| MS-10 Merchant and Store Administration | Tenant/store lifecycle aligns | MS-01 operator identity; event consumers | Owns merchant/store identity | Store mutations local | Pass |
| MS-11 Content and Configuration | Content publication and merchant configuration align | MS-10 scope; MS-03 consumes publication events; MS-12 consumes config events | Owns content and configuration entries | Version/publication/config mutations local | Pass |
| MS-12 Platform Integrations | Adapter endpoints, delivery attempts, email, and external calls align | Consumes events; calls external systems | Owns adapter and delivery state | Delivery attempts local with retries | Pass |

No service writes another service's schema, and no cross-service foreign key is introduced.

## Implicit-layer ownership

- Cross-entity invariant: inventory availability gates cart/order acceptance. MS-02 enforces
  atomic reservation/decrement on its write path; MS-04 and MS-05 use reservation state and never
  mutate inventory directly.
- Cross-entity invariant: order status is gated by payment state. MS-05 enforces order
  transitions from authenticated MS-06 events; MS-06 does not change order state directly.
- Cross-entity invariant: download entitlement is gated by order/payment outcome. MS-05 owns the
  entitlement decision and MS-12 only performs delivery.
- Shared extensibility engine: none found at `spec/shared/extensibility-model.md`; no dedicated
  shared engine ownership is required. Individual extension mechanisms follow their owning service.
- Layer C candidates (calculation, provider calls, persistence joins, and inventory concurrency)
  remain application-tier defaults pending Phase 4b placement review.

## Open carry-forward work

The following are not unmapped features; they are specification tasks required to make the
approved target implementable:

1. Define OpenAPI and event contracts for inventory reservations, order/payment saga transitions,
   capture/refund, download entitlements, and module configuration.
2. Define legal order and payment state-transition matrices and idempotency keys.
3. Resolve identified legacy defects through Phase 4a business-rule classification; do not
   silently reproduce defects.
4. Add target entities for inventory reservation and download entitlement during Phase 4 domain
   modeling.
