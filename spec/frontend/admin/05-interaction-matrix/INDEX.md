# Admin Interaction Matrix

All actions below are subject to server authorization and current tenant/store context.
Interactive controls expose disabled/pending semantics and accessible status announcements.
The domain files contain the complete domain action mapping.

| Domain file | Coverage |
|---|---|
| [identity-stores.md](identity-stores.md) | Auth, users, stores, context |
| [catalogue.md](catalogue.md) | Products, categories, inventory, media, prices, deferred catalogue |
| [commerce-content.md](commerce-content.md) | Customers, orders, content, files, gallery |
| [shipping-tax-payment.md](shipping-tax-payment.md) | Shipping, tax, payment modules, deferred rules/promotion |

## Legacy role to backend binding

The legacy role names below are preserved for navigation terminology. Published provider
contracts declare bearer authentication but do not publish administrator scope/role names, so
the right column is an explicit binding required before implementation. UI guards may hide
controls but the provider/BFF remains authoritative.

| Legacy predicate/name | Target permission binding | Used by |
|---|---|---|
| `IsSuperadmin` / `isSuperadmin` | `TBD: MS-01 administrator scope/role` | marketplace categories, all platform/store actions |
| `IsAdmin` / `isAdmin` | `TBD: MS-01 administrator scope/role` | users, stores, catalogue |
| `IsAdminRetail` / `isAdminRetail` | `TBD: MS-01 administrator scope/role` | products, orders, categories, stores |
| `IsAdminCatalogue` / `isAdminCatalogue` | `TBD: MS-01 administrator scope/role` | catalogue |
| `IsAdminStore` / `isAdminStore` | `TBD: MS-10/MS-09 store scope` | store/shipping configuration |
| `IsAdminOrder` / `isAdminOrder` | `TBD: MS-05 order scope` | orders |
| `IsAdminContent` / `isAdminContent` | `TBD: MS-11 content scope` | content/files |
| `IsOrderManagementVisible` / `canAccessToOrder` | `TBD: MS-05 order scope` | order menu |
| `IsCustomer` / `isCustomer` | no admin grant by default | legacy flag only |

## Cross-cutting interaction states

| Trigger | Feedback | Required behavior |
|---|---|---|
| initial load | skeleton | preserve heading and route, announce loading |
| successful mutation | inline confirmation/toast + updated data | invalidate/reload exact domain state |
| 400/422 | field/summary errors | preserve draft and focus first invalid field |
| 401 | session-expired panel/redirect | one refresh attempt only if approved, then login |
| 403 | forbidden state | keep route, hide/disable unauthorized mutation, explain permission |
| 404 | not-found state | preserve parent navigation and safe back action |
| 409 | conflict dialog/panel | retain draft, offer reload/compare; never overwrite silently |
| 500/503 | retry state | retain route/context, prevent duplicate submit |
| contract gap | deferred panel | no BFF call, no fake empty state, link to parent |
