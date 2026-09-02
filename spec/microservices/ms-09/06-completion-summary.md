# Shipping Specification — Completion Summary

**Version**: 1.0  
**Date**: 2026-09-01  
**Status**: 🟢 100% COMPLETE  
**Service ID**: MS-09  
**Service Name**: Shipping  
**Port**: 8109  
**Database Schema**: `shipping`  
**Priority**: 2  
**Implementation Phase**: Months 4–6

## Completion Metrics

| Metric | Count | Status |
|---|---:|---|
| Business rules | 24 | Complete |
| `BR-PRC-*` rules | 15 | Complete |
| `BR-EXT-*` rules assigned to MS-09 | 8 | Complete |
| `BR-UI-*` rules assigned to MS-09 | 1 | Complete |
| Physical PostgreSQL tables owned | 2 | Complete |
| Legacy-owned table candidates | 2 | Reconciled |
| API operations | 16 | Contract/design match |
| CAST transactions in brief | 16 | Reviewed |
| Mandated business-logic source files | 26 | Read in full |
| Supporting model/API/UI source files | 13 | Read in full |
| Total source files evidenced | 39 | Complete |
| Events observed in legacy source | 0 | No legacy event publisher found |
| Target events recommended | 2 | Optional target integration |

## Source-Derived Decomposition

The 24-rule count is a decomposition outcome, not a target-yield match:

- 10 source behavior seams from the shipping orchestration, packaging, distance, decision, and
  provider paths were decomposed into `BR-PRC-022..036`.
- 8 integration-boundary seams were reconciled as `BR-EXT-010..016` and `BR-EXT-018`.
- 1 administration serialization seam was retained as `BR-UI-008`.
- 5 source/model/API seams were consolidated into the existing rule descriptions rather than
  inflated into additional identifiers.

The deep read produced the following net-new findings beyond the Phase 1 grouping:

1. The `ALL` option mode still records the least-priced option as selected and compares prices
   using `longValue`, which truncates decimal precision.
2. Box output weight uses the last-created local box variable for every returned package, rather
   than the loop variable for each package.
3. The distance minimum of one unit is assigned after the total price is calculated, so it has
   no effect on prices below one kilometre.
4. Free shipping returns before quote persistence, creating a reproducibility gap for checkout.
5. The two provider validation implementations overwrite earlier validation errors instead of
   accumulating all invalid fields.
6. `PriceByDistance.drl` contains overlapping rules without explicit salience or precedence.

## Legacy Components Replaced

| Component | Source evidence | Target placement |
|---|---|---|
| `ShippingServiceImpl` | 961 lines; orchestration and policy | MS-09 application tier |
| `ShippingQuoteServiceImpl` | 78 lines; quote readback | MS-09 application/repository tier |
| `ShippingOriginServiceImpl` | 39 lines; origin readback | MS-09 application/repository tier |
| `DefaultPackagingImpl` | 436 lines; item/box packaging engine | MS-09 application tier |
| `ShippingDecisionPreProcessorImpl` | 179 lines; KIE decision bridge | MS-09 policy tier |
| `ShippingDistancePreProcessorImpl` | 226 lines; distance preparation | MS-09 policy + MS-12 Maps adapter |
| `UPSShippingQuote` | 692 lines; carrier request/response adapter | MS-09 policy contract + MS-12 adapter |
| `USPSShippingQuote` | 744 lines; carrier request/response adapter | MS-09 policy contract + MS-12 adapter |
| `ShippingConfigurationApi` | 321 lines; administration façade | MS-09 compatibility API over MS-11 |
| `OrderShippingApi` | 290 lines; quote API façade | MS-09 API |
| `ShippingExpeditionApi` | 91 lines; expedition/country API | MS-09 compatibility API over MS-11 |
| `ShippingFacadeImpl` | 387 lines; origin/package/expedition façade | MS-09 application façade |
| `ShippingDecision.drl` | 25 lines | MS-09 policy engine |
| `PriceByDistance.drl` | 21 lines | MS-09 policy engine |
| `PriceByDistance2.drl` | 28 lines | MS-09 policy engine |

## Ownership and Boundary Decisions

### MS-09 owns

- Effective origin resolution.
- National/international destination eligibility.
- Free-shipping threshold policy.
- Packaging mode and package-fact construction.
- Provider-independent provider selection.
- Option filtering and selection.
- Quote snapshot persistence.
- Shipping and handling facts supplied to MS-08.
- Quote and method snapshots supplied to MS-04/MS-05.

### MS-09 consumes

- Product identity, availability, virtual status, weight, and dimensions from MS-02.
- Validated customer address and customer/cart context from MS-01/MS-04.
- Store and tenant identity from MS-10.
- Shipping configuration projections from MS-11.

### MS-09 delegates to MS-12

- UPS and USPS credentials.
- Carrier endpoint selection and HTTP/XML protocol.
- Carrier retries, timeout, circuit breaking, and response normalization.
- Google geocoding and Distance Matrix calls.
- Maps API keys and external integration telemetry.

### MS-09 does not own

- `module_configuration`.
- `merchant_configuration`.
- `order_product`.
- Product catalog or inventory.
- Cart mutation or checkout state.
- Order transitions.
- Carrier delivery-attempt state.
- External provider credentials.

## Placement Review

| Candidate | Legacy tier | Data volume | Set-vs-row behavior | Call frequency | App-tier risk | Default placement |
|---|---|---|---|---|---|---|
| Provider selection | Application service and registry | O(number of configured modules) | Row/configuration scan | Every quote | Configuration-order nondeterminism | MS-09 app policy |
| ITEM packaging | Application service | O(cart lines × quantity) | In-memory per-unit facts | Every quote | Missing defaults and virtual-product predicates | MS-09 app policy |
| BOX packaging | Application service | O(cart units × boxes) | In-memory mutable box set | Every quote | Fit factor, max capacity, weight defect | MS-09 app policy |
| Distance pricing | Application provider/rule class | O(1) per quote | Scalar calculation | Eligible distance quote | Threshold and rounding defects | MS-09 app policy |
| Drools shipping decision | KIE application session | O(package facts + rules) | Fact set | Every quote when enabled | Rule overlap and missing salience | MS-09 app policy engine |
| Quote persistence | JPA repository | O(final options per quote) | One row per final option | Every non-free quote | Duplicate/replay semantics | MS-09 PostgreSQL |
| Maps geocoding/distance | Application integration module | O(2 geocodes + 1 matrix call) | External request/response | Eligible zone quote | API latency and failure | MS-12 adapter |
| UPS/USPS quotation | Application integration module | O(package count) XML nodes | Repeated package rows | Configured provider quote | Credentials, carrier outage, XML errors | MS-12 adapter |
| Package/module configuration | Merchant configuration JSON | O(configuration size) | JSON aggregate | Administrative reads/writes | Shared ownership and schema drift | MS-11 projection |

## Automatibility Assessment

| Dimension | Score | Evidence |
|---|---:|---|
| Statement clarity | 88% | Rules have semantic statements, explicit ownership, and domain examples; a few legacy defects require BA decisions |
| Algorithm completeness | 82% | Packaging, threshold, distance, provider, and option algorithms are executable; carrier behavior is adapter-delegated |
| Data-model readiness | 84% | Two owned tables have executable PostgreSQL DDL, constraints, indexes, and field provenance |
| Edge-case coverage | 76% | Virtual products, missing dimensions, missing postal code, no provider, unsupported countries, distance limits, and provider failures are covered |
| Overall automatibility | 83% | Ready for implementation after BA classification of flagged legacy defects and MS-12 adapter contracts |

## Open BA Decisions

1. Correct the box-weight defect or reproduce the legacy result.
2. Correct `ALL` option selection and decimal comparison behavior.
3. Decide whether free-shipping quote snapshots must be persisted.
4. Define the minimum-distance behavior for distances below one kilometre.
5. Define deterministic precedence for overlapping Drools distance rules.
6. Confirm whether package/module/expedition compatibility APIs remain in MS-09 or are moved
   fully to MS-11.
7. Confirm carrier currency conversion and service filtering requirements in the MS-12 contract.

## API and Contract Validation

- API design operations: **16**
- OpenAPI paths/methods: **16**
- Duplicate operation IDs: **0**
- Dangling schema references: **0**
- Request bodies without required fields: **0**
- Mutating operations without success/error responses: **0**
- Standard error schema: present
- Global tenant/store headers: present on every operation
- PostgreSQL DDL: executable
- Entity state model: closed for the origin lifecycle
- Data invariants: all integrity invariants are `db` or `both`

## Events

No event publisher was found in the mandated legacy source files.

Recommended target events, owned by MS-09:

| Event | Trigger | Consumers |
|---|---|---|
| `ShippingQuoteCalculated.v1` | Persisted quote snapshot created | MS-04, MS-05, MS-08 |
| `ShippingConfigurationChanged.v1` | Origin, expedition, or policy projection changed | MS-11, MS-12, MS-04 |

These are target architecture events, not claims about legacy behavior.

## Completion Statement

MS-09 has a source-derived, implementation-ready Phase 4 specification package. It preserves
the effective-origin fallback, country eligibility, provider registry behavior, preprocessor
replacement, free-shipping threshold, option-selection policy, quote persistence, item/box
packaging, `.75` box-fit factor, distance cap/rates, Maps preprocessing, KIE decisions,
weight brackets, pickup processing, and carrier adapter boundaries. Known legacy defects and
ownership decisions are explicitly flagged for Phase 4a/4b rather than silently reproduced.

## Phase 4a BA disposition

Mode A agent defaults were approved on 2026-09-02. 24 rules remain active after 0 approved obsolete-rule removal(s). Retained rules carry explicit Classification and Weight metadata; no rules were deferred, merged, or simplified without BA-specific guidance.
