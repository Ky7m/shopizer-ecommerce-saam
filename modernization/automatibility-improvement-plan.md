# Automatibility Improvement Plan — Phase 4b Initial

**Assessment date:** 2026-09-02  
**Calibration version:** 7  
**Mode:** Pending human selection (Mode A agent recommendations or Mode B working sessions)

The initial assessment found no service below the mandatory 60% improvement threshold. The
items below target dimensions below 80% and are ordered by expected impact on implementation
reliability. Each item has one goal, one expected artifact, and a recommendation that can be
applied in Mode A. Recommendations are not domain-validated.

## Working sessions required

| ID | Service(s) | Gap | Session goal | Expected output | Agent recommendation |
|---|---|---|---|---|---|
| WS-01 | MS-06 Payments | Algorithm: provider amount/status verification and cumulative partial refunds | Walk through authorization, capture, callback, full refund, and two partial-refund scenarios. | Provider verification pseudocode, refund-balance formula, and 2–3 explicit BR-ID examples. | Add `capturedAmount - refundedAmount` as the refundable balance; reject refunds above balance; require provider amount/currency/status agreement before applying a callback. `[Agent-inferred — not validated by SME]` |
| WS-02 | MS-08 Tax | Integration and edge cases: provider failure, unsupported jurisdiction, and fallback | Trace three quotes: supported jurisdiction, provider timeout, and missing tax class. | Provider request/response schema plus a failure/fallback matrix. | Use a synchronous provider adapter with bounded timeout; return a typed provider error when fallback is not configured; never persist an unpriced quote. `[Agent-inferred — not validated by SME]` |
| WS-03 | MS-09 Shipping | Algorithm/data: package normalization, distance/rate calculation, and quote persistence | Walk through one domestic, one international, and one virtual-only cart. | Formula variables, unit conversions, and required quote/origin fields. | Normalize dimensions to centimeters and weight to grams before rate selection; persist the normalized request and selected method on every quote. `[Agent-inferred — not validated by SME]` |
| WS-04 | MS-10 Merchant and Store Administration | Lifecycle/edge cases: parent stores, retailer status, delete behavior | Trace create, child listing, parent deactivation, language update, and delete attempts. | Store state-transition matrix and rejection examples. | Permit child stores only under an active retailer; reject deleting a store with active children or dependent configuration; make language updates idempotent. `[Agent-inferred — not validated by SME]` |

## Information requests

| ID | Service(s) | Gap | Specific question | Expected response | Who can answer | Agent recommendation |
|---|---|---|---|---|---|---|
| IR-01 | MS-03 Search | Edge cases: search request validation and empty results | What are the exact response/status requirements for blank queries, unsupported locale, no matches, and malformed pagination? | Status/error table with 4–6 examples. | Product/API owner | Return `200` with an empty result page for valid no-match queries; return `422` for malformed query or pagination input. `[Agent-inferred — not validated by SME]` |
| IR-02 | MS-03 Search | Integration: replay and rebuild terminal behavior | After how many projection retries does a rebuild/index event become terminal, and which event payload is emitted? | Retry count, backoff, terminal event payload. | Platform/operations owner | Use three bounded retries with exponential backoff and publish `SearchIndexingFailed.v1` containing aggregate ID, source version, and correlation ID. `[Agent-inferred — not validated by SME]` |
| IR-03 | MS-06 Payments | Algorithm: provider callback freshness | What callback age/skew is accepted before a callback is treated as stale? | Duration and timezone/clock rule. | Payments/provider owner | Reject callbacks older than 15 minutes unless an explicit reconciliation flow accepts them. `[Agent-inferred — not validated by SME]` |
| IR-04 | MS-07 Pricing and Promotions | Edge cases: promotion precedence and coupon collisions | When multiple promotions apply, what is the exact precedence and can a coupon be reserved more than once for one checkout? | Ordered precedence list and duplicate-reservation examples. | Commercial rules owner | Apply exclusive promotion before stackable promotions, then reserve a coupon once per checkout idempotency key. `[Agent-inferred — not validated by SME]` |
| IR-05 | MS-08 Tax | Integration: provider contract and fallback ownership | Is an external tax provider authoritative for all jurisdictions, and what response fields are mandatory? | OpenAPI/event schema or provider sample JSON. | Tax/platform owner | Define provider request with destination, tax class, taxable lines, and currency; require rate, amount, jurisdiction, and provider reference in the response. `[Agent-inferred — not validated by SME]` |
| IR-06 | MS-09 Shipping | Data model: required carrier and package indexes | Which quote lookups dominate production traffic and which carrier fields must be searchable? | Query patterns and index list. | DBA/operations owner | Add indexes for `(tenant_id, store_id, created_at)`, quote idempotency, origin scope, and method availability. `[Agent-inferred — not validated by SME]` |
| IR-07 | MS-10 Merchant and Store Administration | Data model: store configuration ownership | Which branding, currency, measurement, and language fields are authoritative in MS-10 versus MS-11? | Field ownership matrix. | Architecture/product owner | Keep tenant/store identity, lifecycle, language, currency, and measurement fields in MS-10; keep versioned content/module configuration in MS-11. `[Agent-inferred — not validated by SME]` |
| IR-08 | MS-10 Merchant and Store Administration | Edge cases: signup-token lifecycle | What is the exact signup-token expiry and replay behavior? | Duration, consumption rule, and error examples. | Identity/merchant owner | Use a single-use, store-bound token with explicit expiry and `410` after expiry or consumption. `[Agent-inferred — not validated by SME]` |

## Planned iteration

| Iteration | Scope | Exit condition |
|---|---|---|
| 1 | Resolve selected working sessions and information requests, or apply approved Mode A recommendations. | Re-score affected dimensions; document score deltas and remaining risks. |
| 2 | Optional targeted pass for any service still below 75% or any dimension below 70%. | Scores stabilize or the human accepts documented residual risks. |

## Human decision required

**Mode A — Apply agent recommendations:** reconcile the recommendations above into the affected
specifications, annotate each change with `[Inferred in Phase 4b — Mode A]`, and recalculate scores.

**Mode B — Real workshops and information requests:** the human supplies artifacts from the
working sessions and requests; the architect updates specifications and recalculates scores.

The placement review is separate from this improvement plan. Current Phase 3 evidence identifies
no mandatory DB-tier placement; all logic remains app-tier unless the architect chooses otherwise
after reviewing performance evidence.

## Final iteration state

**Iteration 1 completed in Mode A on 2026-09-02.** All 12 planned gap areas were reconciled as
agent-inferred clarifications. The four working-session items and eight information requests
remain recorded as unvalidated assumptions rather than outstanding implementation blockers.

| State | Count |
|---|---:|
| Items resolved by Mode A | 12 |
| Items remaining as documented risks | 12 |
| Services below 75% | 0 |
| Services below 70% | 0 |

The next human intervention for these items is domain validation during implementation or a
future Mode B refinement. Phase 4b can proceed to roadmap finalization.
