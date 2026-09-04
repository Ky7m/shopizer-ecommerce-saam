# MS-09 Shipping Implementation Audit

## Decisions made during implementation

| Date | Decision | Rationale |
|---|---|---|
| 2026-09-04 | Use Npgsql ADO.NET and idempotent raw DDL | Required by the .NET reference implementation and MS-09 domain model. |
| 2026-09-04 | Store configuration as tenant/store projections | MS-11 owns the source configuration; MS-09 needs a durable normalized projection for the compatibility façade. |
| 2026-09-04 | Persist eligible free-shipping snapshots but do not emit an adapter request | Checkout reproducibility requires a snapshot, while BR-EXT-011 explicitly bypasses providers. |
| 2026-09-04 | Use exact decimal option comparisons | BR-PRC-027 explicitly replaces the legacy truncated-integer defect. |
| 2026-09-04 | Emit adapter requests through a transactionally written outbox | MS-12 owns carrier/Maps protocols; MS-09 must not fabricate external responses. |

## Specification boundaries requiring review

| Area | Boundary | Resolution |
|---|---|---|
| Cart/product facts | MS-04/MS-02 are the owners and no cross-service graph node was available in this session | The contract has no product-line request shape; the implementation consumes normalized projection keys when supplied and applies the specified missing-fact defaults. |
| MS-12 synchronous response | Carrier and Maps protocols are explicitly outside MS-09 | A configured normalized price projection is supported; otherwise the provider failure is returned and the durable adapter request remains available. |
| Graph context | The requested graph implementation-context lookup found no MS-09 service node | Specs and frozen contract were used as the source of truth; graph reconciliation remains an orchestrator follow-up. |
| DTO marker response types | Several generated response DTOs are marker classes despite populated OpenAPI schemas | DTO source files were copied verbatim as required. Contract-shaped internal response models are used without changing the generated binding. |
| Tenant identifiers | Domain DDL uses UUID scope columns while fixture headers include opaque strings | Opaque header values are deterministically projected to UUIDs at the persistence boundary; API context remains the original header value. |

## Validation record

| Check | Status | Result |
|---|---|---|
| DTO copy/diff | PASS | All files in `spec/microservices/ms-09/08-dtos/` are identical to `Shopizer.Shipping/DTOs/`. |
| Targeted build | PASS | `dotnet build sourcecode/Shopizer.Shipping/Shopizer.Shipping.csproj --no-restore`. |
| Solution build | NOT_RUN | Pending after the service implementation changes. |
| Container build | PASS | `cd sourcecode && docker build -f Shopizer.Shipping/Dockerfile -t shopizer-shipping:validation .` |
| Integration suite | NOT_RUN/BLOCKED | Runtime suite requires the known .NET 10 MTP/Aspire container environment. Existing generated suite is legacy shape-only coverage and was not used as implementation input. |
| Graph reconciliation | PARTIAL | `detect_br_ids.py --all` detected 24 MS-09 IDs and 24 implementation edges; no MS-09 service node was available for service-level context/status. |
