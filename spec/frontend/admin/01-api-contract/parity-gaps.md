# Admin Contract Gaps and Deferred Screens

These are explicit routed capabilities from the legacy source for which no exact operation was
found in the published `spec/microservices/*/04-api-contract.yaml`. No frontend BFF path is
reserved for them, and no legacy path is allowed to leak into the new client.

| Gap ID | Routed capability | Legacy route/source evidence | Why deferred | Required before implementation |
|---|---|---|---|---|
| GAP-API-ADMIN-001 | Dashboard metrics/cache | `pages/home/home.component.*`; chart code is commented and cache button disabled | No dashboard metrics or cache contract | Decide read model, schema, and authorization |
| GAP-API-ADMIN-002 | Brands | `catalogue/brands/*`, `brand.service.ts` | No manufacturer/brand operation | Publish brand CRUD contract |
| GAP-API-ADMIN-003 | Product types/options/sets/values | `catalogue/types/*`, `catalogue/options/*` | No matching MS-02 operations | Publish option/type aggregate contract |
| GAP-API-ADMIN-004 | Product groups/attributes/properties/catalogues | corresponding catalogue routes/services | No matching MS-02 operations | Publish aggregate and association contracts |
| GAP-API-ADMIN-005 | Shipping rules | `shipping/rules/*`, `shared.service.ts` | No MS-09 rule/criteria/action operation | Publish rule model, CRUD, and evaluation workflow |
| GAP-API-ADMIN-006 | Promotion CRUD | `content/promotion/*` | MS-07 exposes promotion evaluation, not admin CRUD | Publish promotion management contract |
| GAP-API-ADMIN-007 | Shared lookups | `config.service.ts` calls countries, zones, currency, measures, languages, groups | No published equivalent in consumed contracts | Assign an owning provider or BFF read model |
| GAP-API-ADMIN-008 | Customer credentials/options | `customers/set-credentials`, `options`, `manageoptions` | No matching admin customer operation | Publish credential and option-management contract |
| GAP-API-ADMIN-009 | Retailer mutations/marketing landing page | store-management retailer and landing components | Store reads exist; mutation surface is incomplete | Publish retailer/store marketing contract |

Deferred screens still appear in the route and screen inventory so the modernization has zero
orphan legacy features. Their loading state is a contract-gap panel with source route,
capability name, and a non-mutating “Back” action. It is not an API error and must not be
represented as a successful empty list.
