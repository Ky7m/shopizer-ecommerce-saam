# Implementation Audit: MS-08 Tax

## Decisions made during implementation

| Timestamp | BR-ID | Decision | Rationale |
|---|---|---|---|
| 2026-09-04T11:29:49+04:00 | — | Copied all 24 files from `spec/microservices/ms-08/08-dtos/` verbatim before adding implementation code. | The DTO directory is the frozen contract binding and must not be regenerated during Phase 5. |
| 2026-09-04T11:29:49+04:00 | BR-TAX-CLS-001..003, BR-TAX-RAT-001..005 | Used raw Npgsql commands and an explicit `SchemaInitializer` with additive migrations. | The reference implementation forbids EF Core, Dapper, and ORM persistence and requires idempotent startup DDL. |
| 2026-09-04T11:29:49+04:00 | BR-TAX-CAL-001..010 | Kept calculations decimal-based, rate-ordered, compound-aware, half-up rounded, persisted as quote and quote-item rows, and replay-safe by tenant/store/idempotency key. | These are named calculation and persistence effects in the business rules and domain model. |
| 2026-09-04T11:29:49+04:00 | — | Stored tenant/store/correlation context as strings in the target schema. | The DDL declares UUID tenant/store values, while the frozen API contract and required test values are `tenant-001`, `store-001`, and `corr-001`. Rejecting those contract values would make the API unusable. This requires operator review before a future UUID migration. |
| 2026-09-04T11:29:49+04:00 | — | Update-rate route accepts the contract JSON and binds it to the copied `CreateTaxRateRequestDto`. | The copied `UpdateTaxRateRequestDto` is an empty marker despite the OpenAPI `allOf` contract requiring the create-rate fields. The DTO was not modified; the contract/DTO generation discrepancy is recorded as a Phase 4 gap. |

## Specification boundaries requiring review

| Area | Boundary | Resolution |
|---|---|---|
| Workflow artifact | `spec/microservices/ms-08/07-workflows.md` is absent. | Implemented from the complete business rules, domain model, API design, contract, and DTOs. No workflow was invented. |
| Graph context | The requested graph service-node lookup was unavailable in this session. | Used the authoritative MS-08 specifications and API contract as the source of truth. Graph reconciliation remains pending when the graph service is available. |
| Store jurisdiction | The MS-08 DDL/configuration DTO has no store country, zone, or province fields, although `StoreAddress` and `UseStoreJurisdiction` are valid policy values. | Store-jurisdiction selection returns the typed `STORE_JURISDICTION_REQUIRED` error until an approved store-address source or contract field is supplied. Cross-service store-table reads were not fabricated. |
| Province policy | `collectTaxIfDifferentProvince` requires a store jurisdiction, but no store-jurisdiction data is owned by MS-08. | The configuration is persisted exactly; enforcement awaits the approved store-address boundary described above. |
| Events | The API design explicitly states that tax publishes and consumes no events. | No RabbitMQ package, publisher, or invented event was added. |

## Validation record

| Check | Result |
|---|---|
| DTO integrity | PASS — 24 implementation DTO files match the 24 authoritative DTO files byte-for-byte. |
| BR annotations | PASS — all 20 MS-08 rules are annotated on reachable service methods; calculation rules are stacked on the reachable calculation method. Two policy rules remain runtime-boundary limited as described above. |
| Stub/TODO scan | PASS for the Tax implementation — no `NotImplementedException`, TODO, hardcoded computed result, or in-memory repository introduced. |
| Tax project build | PASS — `dotnet build sourcecode/Shopizer.Tax/Shopizer.Tax.csproj --no-restore`. |
| Solution build | PASS — `dotnet build sourcecode/Shopizer.slnx --no-restore`. |
| Standard `dotnet test` invocation | BLOCKED — the repository's `global.json` opts into Microsoft.Testing.Platform, and the VSTest invocation reports that VSTest is unsupported on the .NET 10 SDK. |
| Microsoft.Testing.Platform targeted run | BLOCKED — `dotnet test --project ... --filter-class '*TaxComprehensiveTests*'` started but produced no progress beyond startup after repeated waits and was stopped; the Aspire/container runtime did not reach a completed result. |
| Container build | PASS — `docker build -f sourcecode/Shopizer.Tax/Dockerfile -t shopizer-tax:validation sourcecode`; image exposes 8080. |
| Database round-trip | NOT RUN — dependent on the Aspire PostgreSQL runtime and a completed suite. |
| Graph reconciliation | NOT RUN — no graph service node was available for the requested service context. |
| Todo update | BLOCKED — no session todo database or `todos` table is present in the workspace, so the requested SQL update could not be executed without inventing a connection. |
