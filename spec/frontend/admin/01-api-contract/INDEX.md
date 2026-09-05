# Admin BFF API Contract Index

## Access pattern

**Selected pattern: dedicated Admin BFF edge.** The browser calls one environment-configured
base URL, `${ADMIN_BFF_BASE_URL}`, using only the frontend paths beginning
`/api/admin/v1/`. The BFF routes to provider operations; provider service URLs, ports, and
discovery names are not frontend configuration.

The right-hand backend path in the tables below is the exact path and method verified in the
provider `04-api-contract.yaml`. It is not a browser call path. A row is absent when the
published provider contract has no operation for the legacy capability.

## Common request rules

| Concern | Rule |
|---|---|
| Authentication | MS-01 `POST /admin-auth/login` returns `AuthenticationResponse`; BFF session/token handling must use `accessToken`, `tokenType`, `expiresAt`, and `subjectId` exactly as published. |
| Authorization | BFF and provider enforce authorization; UI visibility is advisory and cannot replace a 403 response. |
| Tenant | Forward `x-tenant-id` on every provider operation. Malformed or missing context is surfaced as 400/401 according to the provider contract. |
| Store | Forward `x-store-id` for store-scoped calls. The selected store is maintained by `TenantStoreContext`; changing it invalidates store-scoped caches and reloads the current route. |
| Correlation | Forward or generate `x-correlation-id`. Include the correlation value in an error support affordance. |
| Request format | JSON with camelCase field names exactly matching the provider schema. File operations use the content type required by the provider contract; the BFF owns multipart translation if needed. |
| Errors | Render provider 401 as session expiry/login, 403 as an authorization state, 404 as missing entity, 409 as a conflict/reload choice, 422 as field errors, and 500/503 as retryable failure. Never show a provider stack trace. |
| Pagination | Preserve provider response envelopes and query names; map table page state to the provider's documented parameters. Do not invent `items`, `total`, or page fields when a provider schema does not contain them. |

## Contract binding and gap resolution

Each domain file contains: browser-facing path, provider service ID, exact provider path/
method, provider request/response schema refs, and screen field bindings. `ms-01` through
`ms-12` are provider IDs only; the BFF hides their URLs. `CONTRACT GAP` means there is no
matching operation in any published `04-api-contract.yaml`; implementation must render the
deferred screen and not issue a guessed request. A BFF route may be added only after a
provider contract row is added and this specification is updated.

## Domain index

| File | Covered frontend capabilities | Providers |
|---|---|---|
| [identity-and-context.md](identity-and-context.md) | Login, reset, profile, users, store context | MS-01, MS-10 |
| [catalogue.md](catalogue.md) | Products, categories, variants, media, prices | MS-02, MS-07 |
| [commerce.md](commerce.md) | Customers, orders, invoice/payment operations | MS-01, MS-05 |
| [content.md](content.md) | Pages, boxes, files, images, gallery, configuration, payment modules | MS-06, MS-11, MS-12 |
| [shipping-tax.md](shipping-tax.md) | Shipping configuration, origin, packages/modules/expedition, tax | MS-08, MS-09 |
| [parity-gaps.md](parity-gaps.md) | Explicitly deferred routed capabilities | None |

## Shared schema/error behavior

Use the exact schema references listed in each domain file and the provider's
`ErrorResponse`/status-specific error schemas. The frontend does not normalize field casing.
For an empty valid list, render the empty state from HTTP 200 and the provider response
envelope. For loading, use skeleton rows/cards without changing route or query state.
