# Shared Convention Reconciliation

**Status:** RECONCILED — human approval received 2026-09-01
**Protocol:** The approved proposal has been applied. Common forms live in `spec/shared/`;
module-specific forms remain in the service contract.

## Applied conventions

| Concern | Reconciled form | Decision |
|---|---|---|
| Tenant header | Required `x-tenant-id` | Shared parameter referenced by contracts; service-owned UUID/opaque constraints remain where required. |
| Store header | `x-store-id` on store-scoped operations | MS-11 compatibility optionality is retained; MS-10 uses the path `storeCode` in addition to shared context. |
| Correlation header | Required `x-correlation-id` for internal/authenticated operations and event metadata | Shared required parameter is referenced by contracts. |
| Authorization | `Authorization: Bearer <OAuth2 access token>` on protected operations | Shared bearer security and required/optional parameter forms are documented in `auth-config.md`; public and callback exceptions remain operation-specific. |
| Tenant/store fields | `tenantId`, `storeId`, and domain-specific `storeCode` | No opaque `storeId` was renamed. |
| Currency and money | No shared currency or Money DTO | The proposal did not approve a breaking currency rename or wire-level money normalization; existing service-specific forms are preserved. |
| Pagination | Shared one-based `page`/`pageSize` and `PaginationInfo` | Compatible contracts reference shared components. |
| MS-03 pagination | Search `count`/`start` and offset/limit result form | Preserved as a legitimate search-provider divergence and documented in the contract. |
| MS-11 pagination | Legacy `page`, `count`, `number`, `totalPages`, `recordsTotal`, `recordsFiltered` | Preserved for the CMS compatibility surface; new projections may use shared pagination. |
| List envelopes | `{items, pagination}` with shared pagination metadata | Resource item schemas remain service-owned; bare arrays retain named service response schemas. |
| Errors | Shared `ErrorResponse` | Service-specific status meanings remain; MS-11 legacy operation semantics are not replaced. |
| Idempotency | HTTP `Idempotency-Key`; event `eventId` | Requiredness remains operation-specific, but spelling/casing is fixed. |
| Event versioning | Routing name `.vN` where versioned plus integer `eventVersion` | All compiled event schemas compose shared event metadata. |

## Applied approval decisions

The approval signal authorized reconciliation as proposed. The derived artifacts are
`spec/shared/common-schemas.yaml` and `spec/shared/auth-config.md`. The MS-10 event name is
canonicalized to `StoreCreated`; the sequence-only `StoreConfigured` alias is marked
`DEAD/UNCONSUMED`. The typed MS-09 -> MS-12 adapter event and MS-11 publication/configuration
payloads are compiled in `spec/shared/event-schemas/`.
