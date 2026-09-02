# Implementation Audit: Customer and Identity

## Decisions made during implementation

| Date | Decision | Rationale |
|---|---|---|
| 2026-09-02 | Preserved the existing ASP.NET Core/Aspire project and copied all 34 DTOs before implementation into root-level `DTOs/`. | Setup and API-contract steering files make the scaffold and DTOs frozen inputs. |
| 2026-09-02 | Used Npgsql ADO.NET with an explicit schema initializer instead of adding an ORM. | Keeps PostgreSQL primary, makes all 14 tables and constraints visible, and avoids cross-service database access. |
| 2026-09-02 | Added tenant context to owned rows and an additive startup migration for password-reset cutoffs. | JWT pre-reset invalidation and cross-tenant isolation require durable context not present in the original legacy table shape. |
| 2026-09-02 | Persisted `CustomerRegistered` in an outbox and attempted RabbitMQ publication after commit. | The dependency contract requires durable at-least-once delivery, while RabbitMQ may be temporarily unavailable. |
| 2026-09-02 | Kept newsletter PUT at 501 and implemented DELETE as a persisted unsubscribe. | BR-CUS-028 explicitly requires a deliberate capability decision and the API contract allows both compatibility outcomes. |

## Specification boundaries requiring review

| Area | Boundary | Resolution |
|---|---|---|
| OIDC | No provider endpoint or token exchange contract exists. | Validate service-issued JWTs locally; no invented external call. |
| Reset email | No approved MS-12 payload exists. | Persist reset state and log only non-sensitive queue intent; provider wiring remains an integration boundary. |
| Store hierarchy | No MS-10 CALL edge exists. | Enforce supplied opaque store scope; do not query another service's database. |

## Validation record

The project compiles with the copied DTOs and the implementation. Full Aspire
integration validation remains pending because it requires external PostgreSQL and
RabbitMQ resources; no validation test was modified.
