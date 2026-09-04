# Implementation Audit: Order Management

## Decisions made during implementation

| Date | Decision | Rationale |
|---|---|---|
| 2026-09-04 | Copied all 33 frozen C# DTOs before adding service code. | The API contract and DTO directory are frozen naming and shape authorities. |
| 2026-09-04 | Used raw Npgsql with an idempotent `order_management` schema and additive startup migrations. | The reference implementation forbids ORMs and requires visible relational integrity controls. |
| 2026-09-04 | Stored checkout facts, payment outcomes, refund applications, inbox records, and outbox records separately. | This preserves immutable snapshots, provider ownership, replay safety, and at-least-once boundaries. |
| 2026-09-04 | Kept provider, carrier, inventory, and invoice artifact execution outside MS-05. | The dependency specification assigns those capabilities to MS-06, MS-09/MS-12, MS-02, and MS-12. |

## Specification boundaries requiring review

| Area | Boundary | Resolution |
|---|---|---|
| Order submission | The public contract intentionally excludes checkout submission. | A private orchestration endpoint accepts the `OrderSubmitted.v1` snapshot for the event adapter. |
| Payment and shipment consumers | Producer services are not all implemented in the current repository. | Internal event boundary and durable inbox/outbox operations are implemented; provider execution remains external. |
| Invoice artifact | No MS-05 invoice table is authorized. | Only an invoice request/projection is stored and MS-12 owns artifact generation. |
| Store identifiers | The domain model uses numeric store IDs while request headers are opaque strings. | Numeric suffixes such as `store-12` are preserved as 12; other opaque values are deterministically scoped. |

## Validation record

| Check | Status | Result |
|---|---|---|
| DTO byte equality | PASS | 33 files copied from `spec/microservices/ms-05/08-dtos/`. |
| Targeted compile | PASS | `dotnet build` passed for the MS-05 service and the integration-test project. |
| Aspire integration suite | NOT_RUN | Parent runs the targeted runtime validation. |
| Container build | NOT_RUN | Parent runtime validation. |
