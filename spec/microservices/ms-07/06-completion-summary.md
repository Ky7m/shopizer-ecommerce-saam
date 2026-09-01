# MS-07 Pricing and Promotions — Completion Summary

## Service overview

- **Service ID:** MS-07
- **Service:** Pricing and Promotions
- **Port:** `8107`
- **Database schema:** `pricing_promotions`
- **Priority:** 2
- **Implementation phase:** Revenue path, months 6–10
- **Analysis mode:** Hybrid — CAST-guided transaction scope plus direct Java source read
- **Boundary:** Product/variant price selection, special-price windows, attribute adjustments, promotion-code evaluation, and processor registration
- **Excluded ownership:** Product/catalog persistence outside price references, customer identity, tax, shipping, cart, order lifecycle, and payment state

## Decomposition outcome

The specification contains **13 BR-PRC rules**, corresponding to the **13 Phase 1 pricing/promotion behaviors** identified in the MS-07 CAST brief. The count is a decomposition outcome from the source behavior seams, not a target yield.

The deep read confirmed several implementation-critical findings that are preserved in the rules and contract:

- A selected default variant is attempted before product-level pricing, with wildcard-region filtering and fallback behavior.
- Non-default prices are returned as additional lines and can contribute to the one-time merchandise subtotal.
- Special-price activation has separate bounded, open-start, and no-date branches.
- Discount percentage calculation truncates the computed percentage and is unsafe when the original amount is zero.
- Customer arguments are accepted but ignored by the current pricing implementation.
- The direct variant pricing facade returns `null` despite utility-level variant pricing logic.
- The visible `Test1234` promotion is date-bounded to before 31 October 2025 and is expired at the analysis date.
- Promotion reductions are returned as positive values and subtracted by the consuming total assembler.
- The manufacturer/shipping-code processor is not registered.
- The legacy product-price `GET` operation incorrectly declares a request body; the target contract removes that body.
- Store isolation is made explicit in the target API and persistence boundary rather than relying on the legacy query predicate grouping.

## Artifact counts

| Artifact | Count |
|---|---:|
| Business rules | 13 |
| Owned tables | 5 |
| PostgreSQL indexes | 7 |
| Unique API path templates | 10 |
| API operations/methods | 13 |
| OpenAPI schemas | 25 |
| Published target event types | 2 |
| Consumed event types | 0 |
| Source files read | 16 |
| CAST transactions covered | 7 |
| CAST data graphs covered | 1 |

## CAST transaction coverage

| Transaction ID | Entry point | Coverage |
|---|---|---|
| `244173` | `GET /api/v1/private/product/{productId}/price/` | Product price retrieval and full pricing call graph |
| `244172` | `GET /api/v1/private/product/{productId}/price/{priceId}/` | Single price retrieval |
| `244174` | `GET /api/v1/private/product/{productId}/prices/` | Product price collection |
| `244170` | `POST /api/v1/private/product/{productId}/price/` | Product price creation |
| `244171` | `PUT /api/v1/private/product/{productId}/price/{priceId}/` | Product price update |
| `244169` | `POST /api/v1/private/product/{productId}/inventory/{availabilityId}/price/` | Availability price creation |
| `244175` | `DELETE /api/v1/private/product/{productId}/price/{priceId}/` | Product price deletion |

CAST data graph `243922` identifies `salesmanager.product_price` as the primary legacy price entity. The principal pricing transaction contains a reduced graph of 137 objects and a full graph of 3,009–3,014 objects.

## Semantic preservation

| Rule family | Source dimensions requiring target clarification | Target treatment |
|---|---|---|
| BR-PRC-001 | Outcomes | Explicit `PRICE_UNAVAILABLE` response replaces ambiguous legacy failure/null outcomes |
| BR-PRC-002 | State transitions, error paths | Primary/additional price roles and conflicting identity errors are explicit |
| BR-PRC-003 | State transitions, error paths | Active, future, and expired special-price states are explicit |
| BR-PRC-004 | Outcomes, error paths | Zero original amount is rejected before percentage calculation |
| BR-PRC-005 | Error paths | Negative attribute adjustments are rejected instead of silently ignored |
| BR-PRC-006 | Error paths | Customer context is explicitly accepted but does not apply customer pricing |
| BR-PRC-007 | Control-flow, data-flow, state transitions, outcomes, integrations, errors | Direct variant and parent-product fallback are explicit modes |
| BR-PRC-008 | Control-flow, data-flow, constants, state, outcomes, integrations | Processor registry exposes active and inactive processors |
| BR-PRC-009 | Control-flow, state, outcomes, errors | Rule-session failure and unmatched-code outcomes are typed |
| BR-PRC-010 | Control-flow, state, outcomes, writes, errors | Expired `Test1234` rule is preserved as source evidence and exposed as expired |
| BR-PRC-011 | Control-flow, data-flow, state, outcomes, writes, errors | MS-07 returns a positive reduction; the consumer owns subtotal mutation |
| BR-PRC-012 | State, outcomes, errors | Unregistered manufacturer/shipping processor is explicit |
| BR-PRC-013 | Control-flow, state, writes, errors | Pricing boundary ends before shipping, handling, tax, and grand-total ownership |

No preservation table was omitted from `01-business-rules.md`; each of the 13 rules contains all eight dimensions.

## Endpoint coverage

| API operation group | Methods | Status | Driving rules |
|---|---:|---|---|
| Price administration | 7 | COVERED | BR-PRC-001, BR-PRC-002, BR-PRC-003 |
| Product price calculation | 2 | COVERED | BR-PRC-001 through BR-PRC-006 |
| Variant price calculation | 1 | COVERED | BR-PRC-001, BR-PRC-007 |
| Promotion evaluation | 1 | COVERED | BR-PRC-008 through BR-PRC-012 |
| Processor registry | 1 | COVERED | BR-PRC-008, BR-PRC-012 |
| Checkout pricing quote | 1 | COVERED | BR-PRC-002, BR-PRC-009, BR-PRC-011, BR-PRC-013 |

## Domain-model coverage

- `price_list` provides tenant/store/currency scope.
- `price_entry` preserves the legacy product-price fields and special-price behavior.
- `price_entry_description` preserves the localized description association.
- `promotion` stores target promotion rule metadata and discount rates.
- `coupon` stores tenant/store-scoped promotion codes and validity windows.
- All cross-service product, variant, and availability references remain opaque; no cross-service foreign keys are created.
- All non-infrastructure columns in the DDL include a legacy source mapping or BR-ID justification.
- Database constraints enforce amount, code, price-type, date-window, discount-rate, and tenant/store integrity.
- No database functions, procedures, views, or triggers are required; pricing and promotion orchestration remains in the application/rule-engine boundary.

## Events and dependencies

### Published

| Event | Trigger | Consumers |
|---|---|---|
| `PriceChanged.v1` | Price entry created, updated, or deleted | MS-04 Cart and Checkout; MS-05 Order Management; MS-03 Search for display projection |
| `PromotionChanged.v1` | Promotion or coupon definition changes | MS-04 Cart and Checkout; MS-05 Order Management |

The listed legacy source files do not publish equivalent events. Target event publication requires an outbox implementation.

### Dependencies

- **MS-02:** Resolve product, variant, availability, and attribute references.
- **MS-10:** Establish and validate tenant/store scope.
- **MS-04:** Consume merchandise pricing and promotion quotes.
- **MS-05:** Preserve calculated price and promotion values in immutable order snapshots.
- **MS-08/MS-09:** Calculate tax and shipping independently; MS-07 must not read their tables.

## Known risks and decisions forwarded to Phase 4a/4b

- Confirm whether product and variant price records remain physically owned by MS-02 or are migrated into the MS-07 price-entry schema; the target boundary currently treats MS-07 as the price-selection owner and MS-02 as the catalog-reference owner.
- Confirm whether `Test1234` is retained as an expired migration record or removed as obsolete campaign data.
- Confirm the final monetary rounding policy at the MS-07/consumer boundary.
- Confirm whether customer-specific pricing is required in a later rule set.
- Confirm the approved direct-variant fallback policy.
- Define promotion redemption reservation and idempotency rules before adding reservation persistence.
- Confirm whether the target promotion engine remains Drools-compatible or uses a neutral rule adapter.
- Confirm authorization roles for private price administration and processor inspection.

## Readiness

- `01-business-rules.md` contains 13 uniquely identified rules with source references, logic, data dependencies, side effects, concrete success/error examples, and eight-dimensional preservation tables.
- `02-domain-model.md` contains executable PostgreSQL DDL, constraints, indexes, invariants, and cross-service ownership boundaries.
- `03-api-design.md` defines 13 operations across 10 unique path templates.
- `04-api-contract.yaml` defines the matching OpenAPI 3.1 schemas, paths, operations, headers, status codes, and error responses.
- `extraction-evidence.md` records all source files read, CAST scope, source sections, and rule coverage.
- Phase 4a classification and Phase 4b automatibility scoring remain pending.
