# Automatibility Scores — Phase 4b Initial Assessment

**Assessment date:** 2026-09-02  
**Calibration version:** 7 (`.github/saam-calibration.yaml`)  
**Assessment status:** Provisional — initial scoring before improvement iteration  
**Scope:** 12 services, 303 active Phase 4a rules

## Scoring method

The composite score is the weighted average of statement clarity (30%), algorithm completeness
(25%), integration definition (15%), data model readiness (15%), and edge-case coverage (15%).
Scores are based on the Phase 4 rule statements, logic blocks, examples, API contracts, domain
models, shared event schemas, and the Phase 4a decision register. A score is not an
implementation or test-pass prediction.

## Initial service scores

| Service | ID | Active rules | Statement clarity | Algorithm completeness | Integration definition | Data model readiness | Edge-case coverage | Composite | Type |
|---|---|---:|---:|---:|---:|---:|---:|---:|---|
| Customer and Identity | MS-01 | 51 | 88% | 82% | 82% | 95% | 86% | **86.4%** | A |
| Catalog and Product | MS-02 | 41 | 90% | 84% | 88% | 96% | 88% | **89.6%** | A |
| Search | MS-03 | 10 | 88% | 78% | 86% | 94% | 64% | **82.5%** | B |
| Cart and Checkout | MS-04 | 20 | 91% | 86% | 92% | 94% | 90% | **90.7%** | A |
| Order Management | MS-05 | 23 | 88% | 80% | 88% | 95% | 88% | **86.1%** | A |
| Payments | MS-06 | 19 | 86% | 78% | 84% | 92% | 82% | **83.3%** | B |
| Pricing and Promotions | MS-07 | 12 | 88% | 86% | 80% | 90% | 78% | **84.2%** | B |
| Tax | MS-08 | 20 | 83% | 91% | 70% | 94% | 68% | **81.1%** | B |
| Shipping | MS-09 | 24 | 84% | 79% | 82% | 66% | 80% | **78.5%** | B |
| Merchant and Store Administration | MS-10 | 21 | 82% | 76% | 80% | 68% | 70% | **75.4%** | B |
| Content and Configuration | MS-11 | 39 | 87% | 82% | 90% | 84% | 86% | **85.0%** | A |
| Platform Integrations | MS-12 | 23 | 88% | 80% | 92% | 86% | 84% | **85.6%** | A |
| **Average** |  | **303** | **87.8%** | **81.8%** | **84.5%** | **88.2%** | **81.2%** | **84.0%** |  |

## Interpretation

- **Type A (>=85%):** 6 services; suitable for agent-led implementation with review of
  critical sections.
- **Type B (70-84%):** 6 services; implementation should include human review of calculations,
  external boundaries, or rejection paths.
- **Type C (<70%):** none.
- All services clear the calibrated 75% minimum for implementation, but six services have
  dimensions below 80% and require targeted improvement or an explicitly accepted risk.
- No service is below the 60% mandatory-improvement threshold.

## Primary blockers

| Priority | Services | Dimension | Evidence-based gap |
|---:|---|---|---|
| 1 | MS-03, MS-08, MS-10 | Edge-case coverage | Search failure/no-result behavior, tax-provider and quote failures, and store lifecycle rejection matrices are thinner than the happy paths. |
| 2 | MS-09, MS-10 | Data model readiness | Shipping has only origin/quote core tables in its executable DDL; merchant administration has limited schema coverage for the wider lifecycle and language model. |
| 3 | MS-06, MS-08, MS-09, MS-10 | Algorithm completeness | Provider verification/refunds, provider fallback, shipping normalization, and store hierarchy/lifecycle rules need more explicit step recipes. |
| 4 | MS-08 | Integration definition | Optional external tax-provider behavior and the synchronous fallback boundary need a concrete request/response and failure contract. |

## Scoring caveats

- The score is a specification-quality heuristic and does not account for test-environment
  dependency isolation, database startup wiring, or message-bus mocking.
- The malformed or provisional BR-ID naming seen in portions of MS-01 is a traceability risk
  for Phase 5 even where the rule content is implementable; the API contract remains the naming
  authority.
- Scores remain provisional until the selected improvement mode is completed and the roadmap is
  finalized.

  ## Iteration 1 — Mode A recalculation

  Mode A recommendations were reconciled into the six affected services and annotated in their
  rule, domain-model, API-design, and API-contract documents. No legacy source behavior was
  silently asserted as validated; all additions remain marked as inferred.

  | Service | ID | Score before | Score after | Main affected dimensions | Remaining gaps |
  |---|---|---:|---:|---|---|
  | Customer and Identity | MS-01 | 86.4% | 86.4% | None | Inferred assumptions not applicable |
  | Catalog and Product | MS-02 | 89.6% | 89.6% | None | None |
  | Search | MS-03 | 82.5% | **86.2%** | Edge cases 64% → 82%; algorithm 78% → 82% | Retry count and no-result semantics are agent-inferred |
  | Cart and Checkout | MS-04 | 90.7% | 90.7% | None | None |
  | Order Management | MS-05 | 86.1% | 86.1% | None | None |
  | Payments | MS-06 | 83.3% | **87.8%** | Algorithm 78% → 86%; integration 84% → 90%; edge cases 82% → 88% | Callback freshness and provider verification are agent-inferred |
  | Pricing and Promotions | MS-07 | 84.2% | **86.9%** | Integration 80% → 84%; edge cases 78% → 86% | Promotion precedence is agent-inferred |
  | Tax | MS-08 | 81.1% | **87.2%** | Algorithm 91% → 93%; integration 70% → 84%; edge cases 68% → 82% | Provider authority and fallback policy are agent-inferred |
  | Shipping | MS-09 | 78.5% | **84.5%** | Algorithm 79% → 86%; data model 66% → 82%; edge cases 80% → 86% | Unit normalization and carrier indexes are agent-inferred |
  | Merchant and Store Administration | MS-10 | 75.4% | **83.7%** | Statement 82% → 84%; algorithm 76% → 84%; data model 68% → 82%; edge cases 70% → 86% | Signup expiry and child deletion policy are agent-inferred |
  | Content and Configuration | MS-11 | 85.0% | 85.0% | None | None |
  | Platform Integrations | MS-12 | 85.6% | 85.6% | None | None |
  | **Average** |  | **84.0%** | **86.6%** |  |  |

  ### Iteration outcome

  - 10 services are Type A (>=85%) and 2 are Type B (70–84%); no Type C services remain.
  - All services exceed the 75% implementation threshold.
  - No further improvement iteration is required for score threshold compliance.
  - Mode A inferred assumptions remain implementation risks and must be surfaced during Phase 5
    review; domain validation can replace them later without changing the architecture.
