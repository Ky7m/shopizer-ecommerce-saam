# Implementation Team Composition

## Operating model

The recommended Model B execution uses a shared platform lane and two service implementation
lanes. The team remains .NET-focused and uses the shared infrastructure patterns as the
cross-service contract.

## Roles and allocation

| Role | Allocation | Primary responsibility | Phase 4b/5 touchpoints |
|---|---:|---|---|
| SAAM lead architect | 1.0 | Own roadmap, traceability, gates, and cross-service decisions | All waves; resolves deviations |
| Domain/service architect | 1.0 | Validate service boundaries, invariants, and event choreography | MS-02, MS-04, MS-05, MS-06 |
| .NET implementation engineers | 3.0 | Generate and wire service APIs, domain logic, persistence, and messaging | Two parallel service tracks |
| Platform/infrastructure engineer | 1.0 | Shared runtime, PostgreSQL, RabbitMQ, Redis, containers, CI/CD, test-mode wiring | Foundation and every wave |
| QA/test engineer | 1.0 | Phase 4c suites, contract tests, integration tests, and BR traceability | Starts before Wave 1 implementation |
| Payments/tax/shipping SME | 0.5 shared | Review Mode A assumptions and provider behavior | MS-06, MS-08, MS-09 |
| Product/merchant SME | 0.25 shared | Review promotion, store lifecycle, and content semantics | MS-07, MS-10, MS-11 |

## Service ownership allocation

| Track | Services | Lead | Review focus |
|---|---|---|---|
| Foundation and identity | MS-01, MS-10 | Engineer A + domain architect | Tenant isolation, roles, store lifecycle |
| Catalog and commerce | MS-02, MS-04, MS-07 | Engineer B + domain architect | Inventory reservation, quotes, promotion precedence |
| Transactional lifecycle | MS-05, MS-06 | Engineer C + payments SME | Saga, callbacks, refunds, idempotency |
| Supporting projections | MS-03, MS-08, MS-09, MS-11, MS-12 | Engineers A/B/C rotating + platform engineer | Event replay, provider adapters, delivery retries |

## Allocation by phase

| Phase | Architect | Engineers | Platform | QA | SMEs |
|---|---:|---:|---:|---:|---:|
| 4b roadmap | 1.0 | 0.25 | 0.25 | 0.25 | 0.25 |
| 4c test generation | 1.0 | 0.5 | 0.5 | 1.0 | 0.5 |
| 5 Wave 1 | 1.0 | 2.0 | 1.0 | 1.0 | 0.25 |
| 5 Waves 2-3 | 1.0 | 3.0 | 1.0 | 1.0 | 0.5 |
| 5 Wave 4 | 1.0 | 3.0 | 1.0 | 1.0 | 0.75 |
| 6 continuous evolution | 0.5 | 1.5 | 0.5 | 0.5 | 0.25 |

## Required review checkpoints

- Before MS-02 implementation: approve reservation concurrency and idempotency behavior.
- Before MS-04/MS-05 integration: freeze `OrderSubmitted.v1` and saga state transitions.
- Before MS-06 implementation: review provider verification, callback freshness, and refund
  balance assumptions.
- Before MS-08/MS-09 implementation: confirm provider fallback, unit normalization, and
  deterministic test adapters.
- Before production deployment: verify tenant filters, outbox/inbox replay, health/readiness,
  and migration rollback procedures.
