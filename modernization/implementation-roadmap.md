# Implementation Roadmap

## Summary

- Total services: 12.
- Active rules: 303.
- Final provisional average automatibility: 86.6%.
- Lowest score: MS-10 Merchant and Store Administration at 83.7%.
- Type distribution: 10 Type A, 2 Type B, 0 Type C.
- Recommended execution model: Model B, Transform + GitHub Copilot, with human review of
  payments, tax, shipping, and merchant lifecycle assumptions.

## Timeline comparison by execution model

| Model | Total duration | Parallelism | Best for |
|---|---|---|---|
| A: GitHub Copilot | 61 service-days, about 12 weeks sequential | One service at a time | Complex services and maximum interactive control |
| B: Transform + GitHub Copilot | 41 service-days, about 8 weeks sequential or 4-5 weeks on two tracks | Two tracks | Balanced control and migration speed |
| C: ATX Batch + AI-DLC | 1-2 hours batch plus 6-8 days wiring, about 2 weeks elapsed with dependency gates | All services in batch; wiring in dependency waves | Maximum velocity after inferred assumptions are accepted |

Model C is not the primary recommendation until the Mode A assumptions for MS-06, MS-08,
MS-09, and MS-10 have received implementation-level review.

## Per-service estimates

| Service | Score | Rules | Model A | Model B | Model C |
|---|---:|---:|---:|---:|---|
| MS-01 Customer and Identity | 86.4% | 51 | 9 days | 6 days | Batch + 3 days wiring |
| MS-02 Catalog and Product | 89.6% | 41 | 7 days | 5 days | Batch + 4 days wiring |
| MS-03 Search | 86.2% | 10 | 2 days | 2 days | Batch + 1 day wiring |
| MS-04 Cart and Checkout | 90.7% | 20 | 3 days | 2 days | Batch + 3 days wiring |
| MS-05 Order Management | 86.1% | 23 | 4 days | 3 days | Batch + 4 days wiring |
| MS-06 Payments | 87.8% | 19 | 6 days | 4 days | Batch + 4 days wiring |
| MS-07 Pricing and Promotions | 86.9% | 12 | 3 days | 2 days | Batch + 2 days wiring |
| MS-08 Tax | 87.2% | 20 | 4 days | 3 days | Batch + 2 days wiring |
| MS-09 Shipping | 84.5% | 24 | 5 days | 4 days | Batch + 3 days wiring |
| MS-10 Merchant and Store Administration | 83.7% | 21 | 5 days | 4 days | Batch + 3 days wiring |
| MS-11 Content and Configuration | 85.0% | 39 | 7 days | 4 days | Batch + 3 days wiring |
| MS-12 Platform Integrations | 85.6% | 23 | 6 days | 4 days | Batch + 3 days wiring |
| **Total sequential** |  | **303** | **61 days** | **41 days** | — |

The Model C service wiring estimates overlap in dependency waves; they are not additive. The
critical path is approximately 8 working days after the batch stage.

## Recommended model

**Recommended: Model B.** The engagement has enough specification depth for Transform to
accelerate CRUD and contract scaffolding, while two Type B services and twelve explicitly
inferred Mode A assumptions still justify human review. Model C is the alternative when the
team accepts those assumptions as generation inputs and commits to a short, intensive wiring
window.

## Critical path

`MS-10 Merchant and Store Administration -> MS-02 Catalog and Product -> MS-04 Cart and
Checkout -> MS-05 Order Management -> MS-06 Payments`

MS-07, MS-08, and MS-09 can proceed in parallel before MS-04 integration. MS-03 follows MS-02
and MS-11 projections. MS-12 follows the order/payment event contracts.

## Phase plan

### Wave 1: Foundation services

| Service | Score | Rules | Est. duration | Dependencies |
|---|---:|---:|---:|---|
| MS-01 Customer and Identity | 86.4% | 51 | 6 days Model B | OIDC configuration |
| MS-10 Merchant and Store Administration | 83.7% | 21 | 4 days Model B | MS-01 operator identity |

### Wave 2: Core catalog and calculation services

| Service | Score | Rules | Est. duration | Dependencies |
|---|---:|---:|---:|---|
| MS-02 Catalog and Product | 89.6% | 41 | 5 days Model B | MS-10 store scope |
| MS-07 Pricing and Promotions | 86.9% | 12 | 2 days Model B | MS-02 references, MS-10 scope |
| MS-08 Tax | 87.2% | 20 | 3 days Model B | Address/tax inputs |
| MS-09 Shipping | 84.5% | 24 | 4 days Model B | MS-02 product facts, MS-10 scope, MS-12 adapter |
| MS-11 Content and Configuration | 85.0% | 39 | 4 days Model B | MS-10 store scope |

### Wave 3: Checkout and derived projections

| Service | Score | Rules | Est. duration | Dependencies |
|---|---:|---:|---:|---|
| MS-04 Cart and Checkout | 90.7% | 20 | 2 days Model B | MS-01, MS-02, MS-07, MS-08, MS-09 |
| MS-03 Search | 86.2% | 10 | 2 days Model B | MS-02 and MS-11 events |

### Wave 4: Lifecycle and integrations

| Service | Score | Rules | Est. duration | Dependencies |
|---|---:|---:|---:|---|
| MS-05 Order Management | 86.1% | 23 | 3 days Model B | MS-04 order submission |
| MS-06 Payments | 87.8% | 19 | 4 days Model B | MS-05 payment events, provider credentials |
| MS-12 Platform Integrations | 85.6% | 23 | 4 days Model B | MS-05/MS-06/MS-11 events |

## Parallel execution plan

- Track 1: MS-01 -> MS-10 -> MS-02 -> MS-04 -> MS-05 -> MS-06.
- Track 2: MS-11 in parallel with MS-02; then MS-03 and MS-12 as their event contracts become
  available.
- Calculation track: MS-07, MS-08, and MS-09 in parallel after their upstream contracts are
  stable.
- Sequential bottleneck: checkout submission, order lifecycle, and payment saga integration.

## Risk-adjusted schedule

| Risk | Impact on timeline | Mitigation |
|---|---|---|
| Inferred payment callback and refund rules | +2 days | Implement provider contract tests and review cumulative refund locking before MS-06 completion |
| External tax/carrier provider availability | +2-4 days | Use deterministic adapters in test mode and retain provider references in audit records |
| Cross-service event binding gaps | +2 days | Freeze shared event schemas before Wave 3 and validate outbox/inbox behavior |
| Inventory reservation concurrency | +1-2 days | Complete MS-02 reservation contract and run concurrent reservation tests |
| Tenant and store scope mismatch | +1 day | Enforce shared middleware and repository filters from `spec/shared/infrastructure-patterns.md` |
| Legacy defect discovery during implementation | Variable | Escalate to Phase 6 deviation flow; do not silently change approved rules |

## Remaining automatibility gaps

| Service | Score | Gap description | Impact if unresolved |
|---|---:|---|---|
| MS-06 | 87.8% | Callback freshness/provider verification and cumulative refund assumptions are inferred | Incorrect provider state or refund over-application |
| MS-08 | 87.2% | Provider authority and fallback policy are inferred | Inconsistent tax results during provider failure |
| MS-09 | 84.5% | Unit normalization and carrier index assumptions are inferred | Non-reproducible quotes or slower lookups |
| MS-10 | 83.7% | Signup expiry and child-store deletion policy are inferred | Lifecycle behavior may require a specification amendment |

These are documented risks, not threshold blockers. All services exceed the 75% implementation
minimum.
