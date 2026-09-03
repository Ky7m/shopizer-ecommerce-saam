# Implementation Audit: Search (MS-03)

## Decisions made during implementation

| Date | Decision | Rationale |
|---|---|---|
| 2026-09-03 | Copied all twelve frozen C# DTOs verbatim before implementation. | The API contract skill makes `08-dtos` the concrete API binding. |
| 2026-09-03 | Used raw Npgsql ADO.NET and kept all six specified tables in `search`. | Search owns projections and must not access MS-02 tables or use an ORM. |
| 2026-09-03 | Implemented `local-postgresql` as the provider-neutral adapter. | No external provider runtime or contract is available; returning fabricated provider responses would violate fidelity. |
| 2026-09-03 | Persisted rebuild and operational events in `search.event_outbox` before RabbitMQ delivery. | Rebuild effects and terminal failures must survive broker downtime. |
| 2026-09-03 | Rebuild replays the durable local projection because no approved MS-02 HTTP projection endpoint exists. | The external product projection consumer boundary remains event-driven and is exposed as a testable application service. |
| 2026-09-03 | Kept the empty frozen `RebuildStatusDto` and supplied a serializer converter. | The generated DTO is immutable for Phase 5, while the contract requires a string status enum. |

## Specification boundaries requiring review

| Area | Boundary | Resolution |
|---|---|---|
| Graph context | `graph_implementation_context(ms-03)` returned service not found. | Proceeded from local approved specifications; orchestration reconciliation is pending after code lands. |
| Workflow sequencing | `spec/microservices/ms-03/07-workflows.md` is absent. | Sequencing follows `03-api-design.md`, `02-domain-model.md`, `05-dependencies.md`, and each rule's Logic/Side Effects. |
| Product projection input | No approved MS-02 HTTP endpoint or concrete receiver topology is available. | `SearchService.HandleProductChangedAsync` is a real, retrying application boundary; no invented cross-service endpoint is called. |
| Token issuer | Search has no approved token-introspection endpoint and MS-01 development secrets are process-local. | Non-rejecting middleware validates JWT shape, audience, expiry and context in development; a configured `Search:JwtSecret` enables signature validation. |

## Validation record

| Check | Result | Notes |
|---|---|---|
| Frozen DTO byte equality | PASS | Twelve files copied from `spec/microservices/ms-03/08-dtos/`. |
| Compiles | PASS | `dotnet build sourcecode/Shopizer.slnx` completed successfully. |
| Comprehensive suite | PASS | Native .NET 10 MTP filtered suite: 26 passed, 0 failed, 0 skipped; original Search method names and DisplayName values preserved. |
| Container build | PASS | `docker build -f Shopizer.Search/Dockerfile -t shopizer-search:phase5 .` from `sourcecode/` completed successfully. |
| Service starts | PASS | Aspire fixture started the Search service and its health endpoint returned 200. |
