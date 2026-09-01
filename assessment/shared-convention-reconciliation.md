# Shared Convention Reconciliation

**Status:** GAP — human reconciliation required  
**Protocol:** This is a working proposal only. No service contract has been edited or normalized.
The governing principle is **common to `spec/shared/`, module-specific to the service spec**.

## Tenant and store scoping

| Concern | Current reality | Proposed common form | Recommendation | Status |
|---|---|---|---|---|
| Tenant header | Most contracts use required `x-tenant-id`; MS-10 does not define global headers; MS-03 has no `x-global-headers` extension although operation parameters exist | Required `x-tenant-id` on every service operation and event | Normalize MS-10/MS-03 declarations after human approval; preserve opaque string vs UUID constraints per service until approved | GAP |
| Store header | Most contracts use `x-store-id`; MS-11 marks it optional globally and requires it only for scoped operations; MS-10 omits it | `x-store-id` on store-scoped operations, with global declaration documenting operation-level exceptions | Keep MS-11's operation-specific optionality if confirmed as legitimate; add explicit rationale | GAP |
| Correlation header | Usually `x-correlation-id`; MS-09 marks it optional; MS-03 operation parameters require it | Required `x-correlation-id` for internal calls and event metadata | Normalize requiredness except where an unauthenticated/public endpoint is deliberately approved | GAP |
| Authorization | Required in MS-05/MS-06/MS-08/MS-12; optional in MS-01; absent from MS-10; MS-11 optional globally | `Authorization: Bearer ...` on protected operations, not necessarily public operations | Keep operation-level security; add a shared `auth-config.md` only after approval | GAP |

## Naming and scalar forms

| Concern | Current reality | Proposed common form | Recommendation | Status |
|---|---|---|---|---|
| Tenant/store identifier field | `tenantId` and `storeId` dominate response/event payloads; MS-10 uses `code` for store code and MS-04 uses `cartCode` | `tenantId`, `storeId`, and domain-specific `storeCode` only where a human-facing code is required | Keep `storeCode` in MS-10 path; do not rename opaque `storeId` | RECONCILED |
| Currency | MS-07 uses `currency`; MS-08 uses `currencyCode`; MS-04 uses `currency`; MS-05 uses `currency` | `currencyCode` for a standalone ISO code; preserve `currency` where the existing aggregate contract is already fixed | Human decision required before any cross-service DTO normalization | GAP |
| Monetary values | Numeric `number` appears in MS-07/MS-08/MS-09; MS-04 examples use decimal strings; MS-05 examples mix numeric values | Decimal string for wire-level money, with explicit precision constraints | Recommend common decimal-string form, but do not alter frozen drafts | GAP |

## Pagination

| Service(s) | Current form | Proposed common form | Recommendation | Status |
|---|---|---|---|---|
| MS-01, MS-02, MS-07, MS-09, MS-10, MS-12 | `page`/`pageSize` or service-specific pagination | `page` (1-based) and `pageSize`, with a named pagination object | Normalize only after confirming zero/one-based behavior | GAP |
| MS-03 | Search request uses `count`/`start`; response is search-specific | Keep `count`/`start` as a legitimate search-provider concern; expose shared pagination only at a gateway projection if needed | Keep — legitimately service-specific | RECONCILED |
| MS-11 | Content responses use `page`, `count`, `number`, `totalPages`, `recordsTotal`, `recordsFiltered` | Shared `items` + `pagination` envelope for new cross-service APIs | Keep legacy-compatible MS-11 form until human approves a breaking contract change | GAP |

## List envelopes

| Current form | Services | Proposed common form | Recommendation | Status |
|---|---|---|---|---|
| `{items, pagination}` | MS-01/MS-02/MS-07 and several target contracts | `{items, pagination}` with `page`, `pageSize`, `totalItems`, `totalPages` | Normalize compatible services to this shared form | GAP |
| Search-specific result object | MS-03 | Search result schema with ranked documents and freshness | Keep — legitimately service-specific | RECONCILED |
| `{items, page, count, number, totalPages, recordsTotal, recordsFiltered}` | MS-11 content API design | Shared envelope only for new endpoints; preserve existing CMS compatibility surface | Keep pending human decision | GAP |
| Bare arrays/inline response objects | MS-08 lists, MS-09 module/country lists, MS-10 some lists | Named response schemas | Recommend named schemas before DTO generation | GAP |

## Error shapes and status handling

Most services declare `error`, `message`, `statusCode`, and `timestamp`, but MS-11 also documents
legacy `{success, error, preventRetry}` responses, MS-09 uses `502` for adapter failure, and
MS-10 has inline responses without a global error component. The proposed common form is
`ErrorResponse`; legacy-compatible aliases must remain explicitly service-specific until approved.

**Recommendation:** promote the common error schema and an `auth-config.md` only after human
approval. Do not silently replace MS-11 upload semantics or service-specific `502`, `409`, and
`422` distinctions.

## Additional repeated concerns

- **Idempotency:** `Idempotency-Key` spelling/casing and event `eventId`/business keys are not
  uniformly declared. Recommend `Idempotency-Key` for HTTP commands and `eventId` plus a
  domain-specific business key for events.
- **Versioning:** event versions are written as `.v1`, `eventVersion: 1`, or omitted. Recommend
  both a routing name with `.vN` and an integer `eventVersion`.
- **Base URLs:** services use `/api/v1`, `/api/v1/catalog`, and `/api/v1/integrations`; preserve
  service-specific deployment routing, but gateway routes should be documented separately.
- **Security declarations:** some operations use `security`, others only header parameters.
  Recommend one shared security declaration plus explicit public-operation exceptions.

## Human decisions required

1. Approve or reject the proposed header, pagination, error, money, and idempotency conventions.
2. Decide whether MS-11 legacy envelopes and MS-03 search pagination remain legitimate divergences.
3. Resolve MS-10 `StoreConfigured` versus `StoreCreated` event naming.
4. Provide the missing MS-09 -> MS-12 internal event contract and the MS-11 publication/configuration event payloads.

