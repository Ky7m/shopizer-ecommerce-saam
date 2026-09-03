# MS-04 Implementation Audit

## Decisions made during implementation

| Date | Decision | Rationale |
|---|---|---|
| 2026-09-03 | Use Npgsql ADO.NET with one MS-04 schema initializer and repository | Keeps ownership, constraints, and tenant filters visible without an ORM. |
| 2026-09-03 | Use service-discovered typed REST clients with a context propagation handler | Provider facts remain owned by MS-01/MS-02/MS-07/MS-08/MS-09 and receive inbound scope. |
| 2026-09-03 | Persist checkout snapshots, idempotency response, cart completion, and outbox in one serializable transaction | Makes `OrderSubmitted.v1` replay-safe and durable before publication. |
| 2026-09-03 | Product and tenant/store identifiers are stored using the target database's legacy-compatible scalar representation | The domain DDL specifies UUID tenant/store values while the frozen contract test values are non-UUID strings; this is recorded for review rather than silently rejecting valid contract headers. |
| 2026-09-03 | Migrated the existing Cart Checkout suite without renaming tests or changing DisplayName values | The existing names/display names are preserved exactly per implementation review instruction; inheritance was removed and request execution is now local to the test class. |

## Specification boundaries requiring review

| Area | Boundary | Resolution |
|---|---|---|
| Workflow artifact | `spec/microservices/ms-04/07-workflows.md` is absent | Implemented from business rules, domain model, API design, contract, and dependencies; no workflow was invented. |
| Product identity | MS-02 publishes UUID product IDs while the MS-04 target DDL maps `product_id` to BIGINT | The implementation uses a deterministic numeric representation for the local legacy-compatible column; provider UUID remains the source identity. This requires Phase 4a/operator confirmation. |
| Tenant/store type | Target DDL says UUID, global contract test values include `tenant-001` and `store-001` | MS-04 columns accept scoped strings so required headers are not discarded. A forward migration to UUID is unsafe until the contract test values are reconciled. |
| Shipping quote identity | MS-09 response has no required quote ID field while MS-04 requires a quote reference | The service retains a generated local quote reference for the returned summary; provider contract reconciliation is required before treating it as a provider-owned ID. |
| Payment initialization | MS-06 owns payment intent state and no MS-04 payment table exists | MS-04 calls `POST /payment-intents` and returns its opaque provider reference without persisting provider state. |
| Existing integration-test claims | The frozen suite contained placeholder success/error claims and legacy path assumptions | Test method names and DisplayName values were preserved exactly; all 54 calls now dispatch to explicit contract scenarios with aligned traits/comments. Positive provider-backed scenarios remain blocked by downstream scaffolds rather than being represented as false passes. |

## Validation record

| Check | Result |
|---|---|
| DTO copy | 32 files copied from `spec/microservices/ms-04/08-dtos/`; implementation DTOs remain verbatim |
| BR annotations | 20 rule IDs are annotated on reachable service methods; mechanical count to be rerun after final edits |
| Stub/TODO scan | No `NotImplementedException` or TODO introduced |
| Solution build | PASS — `dotnet build sourcecode/Shopizer.slnx --no-restore` |
| Aspire integration suite | PARTIALLY BLOCKED BY PROVIDERS — configured MTP run executed all 54 tests with 21 passed, 33 failed, and 0 skipped; remaining failures are provider-backed happy paths receiving `CHECKOUT_UNAVAILABLE` from scaffold-only MS-06/MS-07/MS-08/MS-09 services. |
| Container build | PASS — Docker build completed with `mcr.microsoft.com/dotnet/sdk:10.0` and `aspnet:10.0`. |
