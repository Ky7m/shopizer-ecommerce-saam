# MS-03 Search — Completion Summary

## Service overview

- **Service ID:** MS-03
- **Analysis mode:** Hybrid — CAST transaction bounds plus direct Java source extraction
- **Priority:** Supporting search capability
- **Owned schema:** `search`
- **Target port:** `8103`

## Decomposition outcome

The specification contains **10 rules** derived from the search transaction paths and source behavior: **7 source behaviors carried forward from Phase 1 and 3 net-new findings** from the deep read. The net-new findings are the confirmed component self-comparison/image-deletion defects, the provider-independent rebuild lifecycle and idempotency requirement, and the need to enforce pagination at the service boundary because the legacy provider path does not reliably apply `count` and `start`.

The extraction covers the indexing engine rather than treating the module as CRUD. The administrative rebuild transaction is asynchronous, event-driven projection maintenance is separate from storefront querying, and provider configuration is an explicit adapter boundary.

## Artifact counts

| Artifact | Count |
|---|---:|
| Business rules | 10 |
| Owned tables | 6 |
| Indexes | 5 |
| API paths | 3 |
| API operations | 3 |
| OpenAPI schemas | 10 |
| Published event types | 2 |
| Consumed event types | 2 |

## Semantic preservation

| Source component | Flagged dimensions | Status | Notes |
|---|---|---|---|
| `SearchServiceImpl.java` | state transitions, outcomes, data writes | FLAGGED → target-preserved | Durable search-index and projection state is explicit in the target model |
| `SearchFacadeImpl.java` | outcomes, error paths | FLAGGED → target-preserved | Typed HTTP outcomes and bounded pagination are explicit |
| `IndexProductEventListener.java` | data writes, error paths | FLAGGED → target-preserved | Projection mutation and retry/dead-letter behavior are explicit |
| `SearchToolsApi.java` | state transitions, outcomes | FLAGGED → target-preserved | Rebuild identity and asynchronous `202` contract are explicit |
| `SearchProductRequest.java` | control-flow, constants, outcomes, error paths | FLAGGED → resolved | Service-level validation closes legacy provider pagination gaps |

One rule (`BR-CAT-021`) preserves all eight dimensions without a gap. Remaining differences are intentional target-system additions or corrections documented in the rule-level preservation tables.

## Endpoint coverage

| Endpoint | Method | Status | Driving rules |
|---|---|---|---|
| `/api/v1/search` | POST | COVERED | BR-CAT-020, BR-CAT-034 |
| `/api/v1/search/autocomplete` | POST | COVERED | BR-CAT-020, BR-CAT-024 |
| `/api/v1/private/system/search/index` | POST | COVERED | BR-CAT-020, BR-CAT-032, BR-EXT-023, BR-EXT-024 |

## State and integrity coverage

- Search index lifecycle: `Configured → Building → Ready/Degraded/Disabled`.
- Search document lifecycle: `Active → Removed`.
- Rebuild lifecycle: `Requested → Running → Succeeded/Failed/Cancelled`.
- Nine data invariants are documented, including tenant/store isolation, projection identity, bounded limits, non-negative quantities/prices, and rebuild timestamp ordering.
- No database functions, procedures, views, or triggers are required; executable PostgreSQL constraints enforce integrity.

## Placement candidates

| Candidate | Legacy tier | Data-volume signal | Set-vs-row | Call frequency | App-tier risk | Default |
|---|---|---|---|---|---|---|
| Store-wide search rebuild | Application orchestration plus external provider | All searchable products for a store; CAST full transaction size 536 | Product iteration with provider writes | Administrative on-demand/batch | Loading the full catalog into memory or issuing unbounded synchronous requests can cause latency and memory blow-up | app-tier |
| Localized product projection | Application service plus provider adapter | One document per product/store/locale, with variant inventory | Per-product projection write | Product/event driven | Rebuilding in a database loop would not preserve provider semantics and can create round-trip storms | app-tier |

## Known decisions forwarded to Phase 4a/4b

- Confirm whether disabled storefront search returns `503` or an empty result.
- Confirm whether image deletion requires immediate refresh or may rely on an ordered product refresh.
- Set provider retry count and degraded-index policy.
- Confirm the final maximum search result limit.
- Confirm whether category facets remain disabled.
- Confirm exact ownership of provider execution between MS-03 and MS-12.

## Readiness

- Business rules, domain model, API design, OpenAPI contract, completion summary, and extraction evidence are present.
- API contract uses OpenAPI 3.1 and names all response schemas.
- Source references use resolved local paths; the external `SearchModule` implementation is intentionally not fabricated.

## Phase 4a BA disposition

Mode A agent defaults were approved on 2026-09-02. 10 rules remain active after 0 approved obsolete-rule removal(s). Retained rules carry explicit Classification and Weight metadata; no rules were deferred, merged, or simplified without BA-specific guidance.
