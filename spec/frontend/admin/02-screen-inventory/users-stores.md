# Screen Inventory: Users and Stores

## Shared layout

Lists retain the legacy heading, search/filter row, table, paginator, and create action.
Details retain the right-side sub-navigation where the legacy `RightSidemenuComponent` is used.
Create/edit forms retain grouped identity, contact, address, language, and configuration
sections; responsive layouts stack groups and keep save/cancel actions reachable.

| Screen | Data on load | URL state | Actions | Capability |
|---|---|---|---|---|
| Profile | `GET users/me` | none | view profile; navigate change password | contract-backed |
| Change password | current user context | return route | submit new password | contract-backed administrator operation |
| User list | paged `AdministratorListResponse` | search/page/filter | create, view, edit, enable/disable, delete | contract-backed |
| Create/edit user | `Administrator`, groups only if provider schema supports them | `{id}` for edit | validate username, save, cancel | contract-backed with role binding gap |
| Store home/detail | `Store` and `Branding` where applicable | selected store code | edit store, branding, landing links | contract-backed except marketing/landing mutations |
| Stores list | `StoreListResponse` | search/page | create, open, delete | contract-backed |
| Create store | `CreateStoreRequest`; supported languages via MS-10 only | form state | uniqueness check, save, cancel | contract-backed; shared lookups gap |
| Store branding | `Branding` | `{code}` | edit, upload/delete logo | contract-backed |
| Retailer | no complete provider schema | none | legacy retailer form | deferred |
| Retailer list | merchant/child store reads where applicable | search/page | view retailer stores | partially contract-backed/read-only |
| Retailer stores | `StoreListResponse` from merchant/child reads | merchant code | open store | read contract-backed; mutation gap |

## State visuals

Lists show skeleton rows, empty response panels, accessible sort/filter status, and paginator
position. Forms keep dirty-state protection on navigation. 401 redirects through the auth
provider after preserving a safe return route; 403 leaves the route visible with a permission
panel; 409 retains submitted values and identifies the conflicting code/username; 422 maps
provider field errors next to the exact field; 500/503 provides retry without duplicate
submission.
