# MS-11 Content and Configuration — Implementation Audit

## Decisions made during implementation

| Date | Decision | Rationale |
|---|---|---|
| 2026-09-04 | Use Npgsql ADO.NET with an idempotent schema initializer and additive migration SQL. | This is the authoritative .NET reference pattern and keeps ownership/constraints visible. |
| 2026-09-04 | Use the configured local filesystem as the executable default provider. | It gives real upload, retrieval, listing, rename, folder, and deletion behavior without inventing an AWS/GCP dependency; unsupported configured capabilities return explicit errors. |
| 2026-09-04 | Keep the copied MS-11 DTO files unchanged and use anonymous projection objects where generated marker DTOs do not contain contract properties. | The DTO copy is a frozen Phase 4 binding and must not be regenerated or improved during Phase 5. |
| 2026-09-04 | Persist event envelopes in `event_outbox` before attempting RabbitMQ delivery. | Durable state must survive broker outages and published events must not contain provider credentials. |

## Specification boundaries requiring review

| Area | Boundary | Resolution |
|---|---|---|
| Provider validation | MS-12 provider-validation endpoint is not available in the current AppHost graph. | Module state is validated for availability and payload shape locally; no payment charge or shipping quote is executed. Add the MS-12 client when that boundary is provisioned. |
| Graph context | No MS-11 service node was available when the graph context lookup was attempted. | Implemented from the authoritative rules, domain model, API design, contract, and DTOs; annotations remain harvestable by the orchestrator. |
| Generated DTOs | Several Phase 4 marker DTOs have no properties although the OpenAPI schemas are arrays/enums or allOf shapes. | Files were copied verbatim as required; HTTP responses use exact contract-shaped projections and multipart/query values use contract names. |
| Shared database | The current Aspire fixture uses the shared `shopizerDb` resource. | MS-11 uses only `content_configuration` and never reads another service schema. |

## Validation record

| Check | Status | Result |
|---|---|---|
| DTO hashes match `spec/microservices/ms-11/08-dtos/` | PASS | All copied files matched byte-for-byte. |
| MS-11 project build | PASS | `dotnet build Shopizer.ContentConfiguration.csproj --no-restore` succeeded with 0 errors; copied generated DTOs produce nullable-initialization warnings. |
| Solution build | PASS | `dotnet build sourcecode/Shopizer.slnx --no-restore` succeeded with 0 errors; 79 warnings originate from copied nullable DTO initializers. |
| Container build | FAIL/BLOCKED | Correct sourcecode-context build reached publish but the clean SDK image reported missing `Microsoft.Extensions.Telemetry.Abstractions 10.9.0` after restore. |
| Aspire integration suite | BLOCKED | MTP test launch started Aspire resources but produced no test outcome after the runtime wait; it was terminated and no pass is claimed. |
| Graph reconciliation | BLOCKED | MS-11 graph service node was unavailable during context lookup. |
