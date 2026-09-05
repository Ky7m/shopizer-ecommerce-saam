# Shopizer Admin Frontend Specification

## Purpose

Shopizer Admin is the authenticated merchant administration application. It preserves the
legacy Angular administration information architecture and terminology while reimplementing
the visual and runtime layer as a responsive, accessible .NET 10 Blazor Web App. This is a
brownfield specification: routed capability, navigation hierarchy, workflow sequence, screen
layout, and selected Shopizer/provider assets are retained; Angular/Nebular CSS is not copied.

## Source and authority

The primary legacy source is `initial-source/shopizer-admin-main`. Routes are taken from
`src/app/app-routing.module.ts`, `src/app/pages/pages-routing.module.ts`, and each feature
`*-routing.module.ts`; active navigation is taken from `src/app/pages/pages-menu.ts`; data and
actions are taken from the feature components and `services/*.ts`. English terminology is
primarily from `src/assets/i18n/en.json`. Backend names and fields are authoritative only from
the provider `spec/microservices/*/04-api-contract.yaml` files.

The screen inventory includes every routed capability, including routes commented out of the
active menu and routes that are not currently visible for a role. A route being listed here
does not mean that its backend capability is available.

## Target users and roles

| Legacy role or mode | Observed navigation/access meaning | Target binding |
|---|---|---|
| `superadmin` / `isSuperadmin` | Platform-wide administration; marketplace category access | Bind to the authenticated MS-01 administrator claims and the final backend scope/role matrix. |
| `adminretail` / `isAdminRetail` | Retail/catalogue and store administration | Backend role/scope binding is an open contract decision. |
| `admin` / `isAdmin` | General administrator; user/store/catalogue access | Backend role/scope binding is an open contract decision. |
| `admincatalogue` / `isAdminCatalogue` | Catalogue maintenance | Backend role/scope binding is an open contract decision. |
| `adminstore` / `isAdminStore` | Store configuration | Backend role/scope binding is an open contract decision. |
| `adminorder` / `isAdminOrder` | Order administration | Backend role/scope binding is an open contract decision. |
| `admincontent` / `isAdminContent` | Content administration | Backend role/scope binding is an open contract decision. |
| `customer` / `isCustomer` | Legacy role flag; not an admin menu grant | Must not grant admin routes unless an explicit provider binding is approved. |
| `canAccessToOrder` | Legacy order-menu predicate | Replace with a documented MS-05 permission/scope; current contracts declare bearer auth but no admin scope names. |
| `MARKETPLACE` mode | Categories are visible to superadmin only | Preserve as a deployment/context policy, not as a client-only authorization decision. |
| `STANDARD`/store mode | Categories may be visible to retail/admin roles | Preserve mode-aware navigation after server authorization. |

## Target stack

| Layer | Decision |
|---|---|
| Host | .NET 10 Blazor Web App, project `sourcecode/Shopizer.Admin/Shopizer.Admin` |
| Client | `Shopizer.Admin.Client`, Interactive Auto |
| Rendering | Interactive Server and Interactive WebAssembly render modes as established by `Components/Routes.razor` and `Program.cs` |
| Edge | Dedicated Admin BFF; the browser never calls a provider service directly |
| Routing | Blazor route components with a shared authenticated admin layout |
| State | Scoped authentication, tenant/store context, route/query state, and per-domain feature state; no browser role flag is authoritative |
| Styling | New scoped/global CSS and semantic components; responsive CSS, not copied Nebular/Angular styles |
| Test target | Playwright E2E against the live Aspire host and BFF |

## BFF and request context

The exact deployed BFF base URL is an environment value named `ADMIN_BFF_BASE_URL`; no
deployment hostname is selected by this specification. Browser calls use only the
frontend-facing `/api/admin/v1/...` paths documented in `01-api-contract/`.

MS-01 authentication is the identity authority. Protected calls carry the bearer access token
through the BFF. Every provider call receives `x-tenant-id`; store-scoped operations also
receive `x-store-id`. The BFF validates the session and context, forwards the context to the
provider, and must not allow a client-selected tenant/store to escape the authenticated
administrator's permitted context. `x-correlation-id` is propagated or generated according to
`spec/shared/auth-config.md` and `spec/shared/infrastructure-patterns.md`.

## Brownfield preservation policy

Preserve the legacy left navigation and hierarchy: Home, User management, Store management,
Inventory management, Content management, Shipping management, Payment, Tax management,
Customer management, and Order management. Preserve labels such as “Store information”,
“Expedition”, “Packaging”, “Tax Classes”, “Orders”, “File Manager”, “Capture”, and “Refund”.
Preserve list -> detail -> edit/create sequences, route parameters, search/pagination intent,
confirmation before destructive actions, and the existing information grouping. Modernize
keyboard use, focus, validation feedback, mobile navigation, table behavior, loading states,
and error recovery.

## Out of scope

- Implementing Blazor pages, BFF endpoints, generated API clients, or tests.
- Adding backend behavior or changing a provider contract.
- Designing storefront routes or storefront workflows.
- Treating legacy localStorage roles, direct provider URLs, or Angular implementation details as
  target architecture.
- Making a Figma file or claiming one exists; no Figma reference was found.

## Known backend parity gaps

The published contracts were checked before this specification was written. The following
routed capabilities are deferred because no matching provider CRUD/read contract exists:
dashboard metrics/cache management, brands, product types, options and option values/sets,
variations, product groups, product attributes/properties, catalogues, shipping rules,
promotion CRUD, customer option-management/set-credentials behavior, and shared country/zone/
currency/measurement/system-language/group lookups used by forms. Retailer create/edit behavior
also lacks a complete provider contract, although store and merchant/child-store reads exist.

The contracts do provide operations for authentication/users, stores and branding, products,
categories, variants, product media, prices, customers, orders and order history/payment
commands, content pages/boxes/files/images/folders/configuration, shipping configuration/
origin/packaging/modules/expedition, payment-module configuration, and tax classes/rates.
The exact bindings and limitations are in `01-api-contract/`.

Missing backend workflow documents are a separate gap. No `07-workflows.md` or
`spec/07-cross-service-workflows.md` is present; flow IDs marked `GAP-WF-ADMIN-*` are
traceability placeholders, not completed backend workflows.

## Open decisions / contract gaps

1. Publish the admin scope/role matrix for the legacy role predicates and bind it to provider
   authorization before implementation.
2. Produce provider contracts for the deferred capabilities, or approve their removal, before
   enabling their mutations.
3. Publish backend workflow documents for authentication/context switching, product/category
   editing, order operations, content publishing, and store configuration.
4. Decide whether shared lookups become an MS-10/MS-11 contract or a BFF-owned read model.
