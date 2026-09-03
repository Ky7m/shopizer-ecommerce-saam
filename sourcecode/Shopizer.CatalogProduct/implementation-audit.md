# MS-02 implementation audit

## Decisions made during implementation

| Date | Decision | Rationale |
|---|---|---|
| 2026-09-03 | Use raw Npgsql with one catalog schema and explicit transactions. | The domain model owns the DDL and the reference implementation forbids an ORM. |
| 2026-09-03 | Use a transactional outbox before RabbitMQ delivery. | Preserves at-least-once event delivery when the broker is unavailable. |
| 2026-09-03 | Keep all tenant/store predicates in repository SQL. | Prevents cross-tenant and cross-store reads and writes. |
| 2026-09-03 | Keep media metadata separate from product rows. | Binary/provider ownership is outside the catalog database boundary. |

## Specification boundaries requiring review

| Area | Boundary | Resolution |
|---|---|---|
| Store validation | MS-10 owns opaque store validity. | MS-02 persists and filters the supplied tenant/store context; synchronous MS-10 wiring remains an integration deployment concern because the current MS-10 project is still a scaffold. |
| Binary media provider | Provider contract is external to MS-02. | Metadata and provider-neutral URIs are persisted; RabbitMQ projection events are emitted for every media mutation. |
| Pricing writes | The MS-02 contract exposes calculation but no price administration endpoint. | Price resolution is implemented for records present in the owned schema; no cross-service schema is queried. |

## Validation record

| Check | Result | Notes |
|---|---|---|
| DTO byte equality | PASS | Every file under `spec/microservices/ms-02/08-dtos/` was copied unchanged. |
| Targeted build | PASS | `dotnet build sourcecode/Shopizer.CatalogProduct/Shopizer.CatalogProduct.csproj --no-restore`. |
| Solution build | PASS | `dotnet build sourcecode/Shopizer.slnx --no-restore` completed with zero errors. |
| Container build | PASS | Container image build completed successfully. |
| Aspire integration suite | PASS | Native MTP ran all 111 tests with 111 passed, 0 failed, and 0 skipped after the approved `/api/v1` route alignment and fixture isolation updates. |

## Test failure analysis

| Area | Root cause | Fix applied |
|---|---|---|
| Resource prerequisites | Generated scenarios referenced placeholder catalog IDs that were not present in the test database. | Added deterministic MS-02 seed data and per-request catalog reset in `AspireHostFixture`. |
| Mutation collisions | Generated create scenarios reused codes, SKUs, and slugs across independent facts. | Added unique payload normalization in `ComprehensiveTestBase` while preserving service uniqueness validation. |
| Negative-resource cases | Error scenarios reused resources created by success scenarios, making expected `404` outcomes order-dependent. | Normalized negative paths to intentionally missing IDs, SKUs, and slugs. |
