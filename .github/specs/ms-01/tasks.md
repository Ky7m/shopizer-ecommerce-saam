# Tasks: Customer and Identity (MS-01)

## Task 1: Project scaffolding
- **Status:** IN_REVIEW
- **SAAM Spec:** `spec/microservices/ms-01/02-domain-model.md`
- **Tracking:** `tracking/phase5-implementation/ms-01.md#task-1`
- **Deliverables:** Aspire project preserved; ServiceDefaults preserved; PostgreSQL,
  RabbitMQ, configuration and explicit schema initialization wired.

## Task 2: Domain and repository layer
- **Status:** IN_REVIEW
- **SAAM Spec:** `spec/microservices/ms-01/02-domain-model.md`
- **Tracking:** `tracking/phase5-implementation/ms-01.md#task-2`
- **BR-IDs:** —
- **Deliverables:** All 14 specified tables/enums/constraints/indexes, tenant/store
  predicates, account/address/attribute/admin/review/newsletter/reset/external
  repositories, and all 34 verbatim DTOs in the root-level `DTOs/` folder.

## Task 3: Customer and identity business logic
- **Status:** IN_REVIEW
- **SAAM Spec:** `spec/microservices/ms-01/01-business-rules.md`
- **Tracking:** `tracking/phase5-implementation/ms-01.md#task-3`
- **BR-IDs:** BR-CUS-001..020, BR-CUS-NN-001..009, BR-CUS-NN-010..020,
  BR-CUS-NN-021
- **Deliverables:** Registration, profile/address/attribute isolation, password
  policy and reset lifecycle, JWT security, administrator authorization and
  enablement, and external identity composite-key behavior.

## Task 4: Reviews, consent and API controllers
- **Status:** IN_REVIEW
- **SAAM Spec:** `spec/microservices/ms-01/01-business-rules.md` and
  `spec/microservices/ms-01/04-api-contract.yaml`
- **Tracking:** `tracking/phase5-implementation/ms-01.md#task-4`
- **BR-IDs:** BR-CUS-021..028, BR-UI-001..002
- **Deliverables:** Review uniqueness/range/aggregate recomputation, newsletter
  idempotent upsert and deliberate 501/real unsubscribe behavior, canonical
  review routing, all contract endpoints and error handling.

## Task 5: Integration, unit tests and validation reconciliation
- **Status:** IN_PROGRESS
- **SAAM Spec:** `spec/microservices/ms-01/05-dependencies.md`
- **Tracking:** `tracking/phase5-implementation/ms-01.md#task-5`
- **BR-IDs:** ALL
- **Deliverables:** RabbitMQ/outbox round-trip verification, unit/integration test
  execution, compilation, service startup and validation reconciliation. Human
  review is required before any task can become DONE.
