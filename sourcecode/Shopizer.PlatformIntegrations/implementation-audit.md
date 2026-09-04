# MS-12 Implementation Audit

## Decisions made during implementation

| Date | Decision | Rationale |
|---|---|---|
| 2026-09-04 | Use Npgsql ADO.NET and five explicit reliability tables. | Matches the authoritative domain model and prevents hidden cross-service persistence. |
| 2026-09-04 | Keep credentials in an in-process runtime cache only. | The contract accepts credentials for provider resolution but MS-12 must not persist secret material. |
| 2026-09-04 | Implement Local storage completely and return `501 STORAGE_OPERATION_UNSUPPORTED` for provider capabilities not present. | Preserves source provider semantics without claiming an unavailable S3/GCP/Infinispan SDK. |
| 2026-09-04 | Preserve the generated DTO files verbatim and add marker converters at composition time. | The generated enum DTOs are marker classes; converters bind their contract scalar values without changing the frozen files. |

## Specification boundaries requiring review

| Area | Boundary | Resolution |
|---|---|---|
| Graph context | No MS-12 service node was available from the requested graph lookup. | Implemented against the frozen specifications and recorded the limitation in tracking. |
| GeoLite | The repository contains no GeoLite2 database asset. | Validate IP syntax and return the contract's unresolved result until the deployment asset is supplied. |
| External providers | UPS, USPS and Maps require configured endpoint URIs and live provider responses. | Calls are real HTTP/XML calls; absent or rejected providers return contract-shaped 502/503 errors. |
| Workflows | `07-workflows.md` is absent. | Used the explicit event and state-machine sections in the business rules/domain model. |

## Validation record

| Check | Result | Notes |
|---|---|---|
| Targeted MS-12 build | PASS | `dotnet build sourcecode/Shopizer.PlatformIntegrations/Shopizer.PlatformIntegrations.csproj --no-restore`; 0 errors, copied DTO nullable warnings only. |
| Full solution build | PASS | `dotnet build sourcecode/Shopizer.slnx --no-restore`; 0 errors, generated DTO nullable warnings. |
| Integration suite | BLOCKED | `dotnet test` fails before test execution because Microsoft.Testing.Platform rejects the VSTest target on .NET 10. |
| Container build | NOT RUN | Docker daemon availability not established in this session. |
| Validation runner | BLOCKED | `./validation/run-and-reconcile.sh ms-12 stage4_final` entered the MTP test step and was stopped after no result; no pass is claimed. |
| Graph annotation reconcile | PASS/PARTIAL | `detect_br_ids.py --all` found all 23 MS-12 rule IDs and projected 29 implementation edges; no MS-12 service node exists for service-level status/context. |
