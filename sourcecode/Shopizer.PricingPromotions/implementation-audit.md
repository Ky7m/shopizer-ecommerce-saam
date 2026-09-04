# MS-07 Implementation Audit

## Decisions made during implementation

| Date | Decision | Rationale |
|---|---|---|
| 2026-09-04 | Use raw Npgsql against the Aspire `shopizerDb` connection | Matches the authoritative .NET reference and keeps MS-07 DDL and tenant predicates explicit. |
| 2026-09-04 | Keep product, variant, and availability identifiers opaque | MS-02 owns catalog facts; MS-07 must not create cross-service foreign keys or query another schema. |
| 2026-09-04 | Create the configured `DEFAULT` USD price list on first price mutation | The frozen price request DTOs do not carry a price-list identifier or currency. Currency is configurable and calculation quotes can supply their own output currency. |
| 2026-09-04 | Persist outbox events for price mutations | `PriceChanged.v1` is a target event contract and requires durable mutation-before-publish behavior. |

## Specification boundaries requiring review

| Area | Boundary | Resolution |
|---|---|---|
| Catalog resolution | MS-02 product/variant/availability REST contract is not present in the MS-07 frozen API. | Price entries retain opaque identifiers and calculations use the MS-07-owned scoped entries; a future MS-02 client can be added after its contract is approved. |
| Promotion administration | MS-07 domain DDL owns promotions and coupons, but the frozen API exposes evaluation only. | Evaluation reads enabled, in-window promotion/coupon rows; no uncontracted mutation endpoint was invented. |
| Availability region | The frozen MS-07 DDL contains no region column even though the rules describe wildcard-region selection. | Entries in this service represent the already-resolved wildcard availability; region resolution remains an MS-02 boundary. |
| Inferred exclusivity/priority | Phase 4b mentions fields not present in the frozen DDL. | No fields or behavior were invented; matching is deterministic by promotion UUID. |

## Validation record

| Check | Status | Result |
|---|---|---|
| DTO copy | PASS | All frozen C# DTO files copied verbatim from `spec/microservices/ms-07/08-dtos/`. |
| Target project build | PASS | `dotnet build Shopizer.PricingPromotions.csproj --no-restore`; only nullable warnings from verbatim DTOs. |
| Contract route coverage | PASS | All 13 operations are mapped in `Controllers/PricingController.cs`. |
| Tenant/store query audit | PASS | Price and promotion reads/writes include tenant/store predicates or derive from the scoped price list. |
| Outbox/event wiring | PASS | Price create/update/delete write outbox rows and call `EventPublisher`. |
| Integration suite | BLOCKED | The .NET 10 MTP command started Aspire infrastructure but did not complete; plain `dotnet test` is rejected by the repository's MTP configuration. No test pass is claimed. |
