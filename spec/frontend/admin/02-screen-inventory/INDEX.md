# Admin Screen Inventory Index

## Route and visibility rules

The route column is the target route shape based on the legacy route. The legacy source route
column is exact where Angular used a literal route. `Active` means an item appears in
`pages/pages-menu.ts` for at least one role; `Hidden` means it is routed but absent from the
active menu, role-filtered, commented out of the menu, or only reachable from an action. Access
predicates are the legacy names and are not a replacement for provider authorization.

| Domain file | Routes |
|---|---|
| [auth-dashboard.md](auth-dashboard.md) | Authentication, Home, Gallery, Error |
| [users-stores.md](users-stores.md) | Users and Store management |
| [catalogue.md](catalogue.md) | All catalogue routes |
| [content-shipping.md](content-shipping.md) | Content and shipping |
| [commerce.md](commerce.md) | Payment, tax, customers, orders |

## Complete routed capability map

| Domain | Target route | Access predicate / legacy role | Parent | Legacy visibility | Capability |
|---|---|---|---|---|---|
| Auth | `/auth` | anonymous | root | active entry target | contract-backed |
| Auth | `/auth/register` | anonymous | Auth | hidden action | contract-backed store signup where contract fields bind |
| Auth | `/auth/forgot-password` | anonymous | Auth | hidden action | contract-backed |
| Auth | `/user/{id}/reset/{token}` | anonymous | root | hidden/direct | contract-backed reset binding |
| Error | `/errorPage` | public error | root | hidden | static |
| Error | `/pages/error-500` | authenticated shell | Pages | hidden | static |
| Gallery | `/gallery` | authenticated | root/dialog | hidden | contract-backed through content image manager |
| Home | `/pages/home` | authenticated | Pages | active | partially contract-backed; metrics deferred |
| Users | `/pages/user-management/profile` | authenticated | User management | active | contract-backed |
| Users | `/pages/user-management/change-password` | authenticated | User management | hidden action | contract-backed for administrator |
| Users | `/pages/user-management/create-user` | `IsAdmin` | User management | active, role-filtered | contract-backed |
| Users | `/pages/user-management/users` | `IsAdmin` | User management | active, role-filtered | contract-backed |
| Users | `/pages/user-management/user/{id}` | `IsAdmin` | User management | hidden/detail | contract-backed |
| Stores | `/pages/store-management/store` | superadmin/admin/adminretail/adminstore | Store management | active | contract-backed |
| Stores | `/pages/store-management/create-store` | superadmin/adminretail | Store management | active, role-filtered | contract-backed |
| Stores | `/pages/store-management/stores-list` | `IsAdmin` | Store management | active, role-filtered | contract-backed |
| Stores | `/pages/store-management/store-landing/{code}` | authenticated store context | Store management | hidden | deferred mutation surface |
| Stores | `/pages/store-management/store/{code}` | authenticated store context | Store management | hidden/detail | contract-backed read/update |
| Stores | `/pages/store-management/store-branding/{code}` | authenticated store context | Store management | hidden/detail | contract-backed |
| Stores | `/pages/store-management/retailer` | superadmin/adminretail intent | Store management | hidden | deferred |
| Stores | `/pages/store-management/retailer-list` | superadmin/adminretail intent | Store management | hidden | deferred/read gap |
| Stores | `/pages/store-management/retailer-stores` | superadmin/adminretail intent | Store management | hidden | read contract-backed; mutations deferred |
| Catalogue | `/pages/catalogue/categories/categories-list` | category predicate | Catalogue | active, role-filtered | contract-backed |
| Catalogue | `/pages/catalogue/categories/create-category` | superadmin/admin/adminretail/admincatalogue | Categories | active, role-filtered | contract-backed |
| Catalogue | `/pages/catalogue/categories/categories-hierarchy` | superadmin/admin/adminretail/admincatalogue | Categories | active, role-filtered | contract-backed |
| Catalogue | `/pages/catalogue/categories/category/{id}` | category predicate | Categories | hidden/detail | contract-backed |
| Catalogue | `/pages/catalogue/products/products-list` | adminretail | Products | active, role-filtered | contract-backed |
| Catalogue | `/pages/catalogue/products/create-product` | catalogue roles | Products | hidden/action | contract-backed |
| Catalogue | `/pages/catalogue/products/product-ordering` | adminretail | Products | active | contract-backed only if ordering uses provider list fields |
| Catalogue | `/pages/catalogue/products/product/{id}/default` | catalogue roles | Product | hidden/detail tab | contract-backed |
| Catalogue | `/pages/catalogue/products/product/{id}/images` | catalogue roles | Product | hidden/detail tab | contract-backed |
| Catalogue | `/pages/catalogue/products/product/{id}/category` | catalogue roles | Product | hidden/detail tab | contract-backed |
| Catalogue | `/pages/catalogue/products/product/{id}/options` | catalogue roles | Product | hidden/detail tab | deferred |
| Catalogue | `/pages/catalogue/products/product/{id}/properties` | catalogue roles | Product | hidden/detail tab | deferred |
| Catalogue | `/pages/catalogue/products/product/{id}/discount` | catalogue roles | Product | hidden/detail tab | deferred/promotion gap |
| Catalogue | `/pages/catalogue/products/{productId}/category-association` | catalogue roles | Product | hidden action | contract-backed |
| Catalogue | `/pages/catalogue/products/association` | catalogue roles | Product | hidden action | contract-backed only for category association |
| Catalogue | `/pages/catalogue/products/{productId}/product-attributes` | catalogue roles | Product | hidden | deferred |
| Catalogue | `/pages/catalogue/products/{productId}/inventory-list` | catalogue roles | Product | hidden | contract-backed availability |
| Catalogue | `/pages/catalogue/products/{productId}/inventory/{inventoryId}` | catalogue roles | Inventory | hidden | contract-backed only availability/price binding |
| Catalogue | `/pages/catalogue/products/{productId}/inventory-creation` | catalogue roles | Inventory | hidden | contract-backed only availability replacement |
| Catalogue | `/pages/catalogue/products/{productId}/inventory/{inventoryId}/create-price` | catalogue roles | Price | hidden | contract-backed |
| Catalogue | `/pages/catalogue/products/{productId}/inventory/{inventoryId}/price/{priceId}` | catalogue roles | Price | hidden | contract-backed |
| Catalogue | `/pages/catalogue/brands/brands-list` | catalogue roles | Brands | active, role-filtered | deferred |
| Catalogue | `/pages/catalogue/brands/create-brand` | catalogue roles | Brands | active, role-filtered | deferred |
| Catalogue | `/pages/catalogue/brands/brand/{id}` | catalogue roles | Brands | hidden/detail | deferred |
| Catalogue | `/pages/catalogue/catalogues/catalogues-list` | catalogue roles | Catalogues | hidden/commented menu | deferred |
| Catalogue | `/pages/catalogue/catalogues/create-catalogue` | catalogue roles | Catalogues | hidden/commented menu | deferred |
| Catalogue | `/pages/catalogue/catalogues/catalogue/{catalogId}` | catalogue roles | Catalogues | hidden | deferred |
| Catalogue | `/pages/catalogue/catalogues/{catalogId}/catalogues-products` | catalogue roles | Catalogues | hidden | deferred |
| Catalogue | `/pages/catalogue/products-groups/groups-list` | catalogue roles | Product groups | active, role-filtered | deferred |
| Catalogue | `/pages/catalogue/products-groups/products-groups-list` | catalogue roles | Product groups | hidden | deferred |
| Catalogue | `/pages/catalogue/products-groups/create-products-group` | catalogue roles | Product groups | hidden | deferred |
| Catalogue | `/pages/catalogue/products-groups/create-products-group/{code}` | catalogue roles | Product groups | hidden | deferred |
| Catalogue | `/pages/catalogue/options/options-list` | catalogue roles | Options | active, role-filtered | deferred |
| Catalogue | `/pages/catalogue/options/create-option` | catalogue roles | Options | hidden | deferred |
| Catalogue | `/pages/catalogue/options/option/{optionId}` | catalogue roles | Options | hidden | deferred |
| Catalogue | `/pages/catalogue/options/options-values-list` | catalogue roles | Options | active, role-filtered | deferred |
| Catalogue | `/pages/catalogue/options/create-option-value` | catalogue roles | Options | hidden | deferred |
| Catalogue | `/pages/catalogue/options/option-value/{optionValueId}` | catalogue roles | Options | hidden | deferred |
| Catalogue | `/pages/catalogue/options/options-set-list` | catalogue roles | Options | active, role-filtered | deferred |
| Catalogue | `/pages/catalogue/options/option-set` | catalogue roles | Options | hidden | deferred |
| Catalogue | `/pages/catalogue/options/option-set/{optionId}` | catalogue roles | Options | hidden | deferred |
| Catalogue | `/pages/catalogue/options/variations/list` | catalogue roles | Options | active, role-filtered | deferred unless product variant route is used |
| Catalogue | `/pages/catalogue/options/variations/add` | catalogue roles | Options | hidden | deferred unless product variant route is used |
| Catalogue | `/pages/catalogue/types/types-list` | catalogue roles | Product types | active, role-filtered | deferred |
| Catalogue | `/pages/catalogue/types/create-type` | catalogue roles | Product types | hidden | deferred |
| Catalogue | `/pages/catalogue/types/type/{id}` | catalogue roles | Product types | hidden | deferred |
| Content | `/pages/content/pages/list` | authenticated/content intent | Content | active | contract-backed |
| Content | `/pages/content/pages/add` | authenticated/content intent | Content pages | hidden action | contract-backed |
| Content | `/pages/content/pages/add/{code}` | authenticated/content intent | Content pages | hidden action | contract-backed |
| Content | `/pages/content/boxes/list` | authenticated/content intent | Content | active | contract-backed |
| Content | `/pages/content/boxes/add` | authenticated/content intent | Content boxes | hidden action | contract-backed |
| Content | `/pages/content/boxes/add/{code}` | authenticated/content intent | Content boxes | hidden action | contract-backed |
| Content | `/pages/content/images/list` | authenticated/content intent | Content | active | contract-backed |
| Content | `/pages/content/files/list` | authenticated/content intent | Content | hidden/commented menu | contract-backed |
| Content | `/pages/content/promotion` | authenticated/content intent | Content | hidden/commented menu | deferred |
| Shipping | `/pages/shipping/config` | authenticated/store context | Shipping | active | contract-backed |
| Shipping | `/pages/shipping/methods` | authenticated/store context | Shipping | active | contract-backed |
| Shipping | `/pages/shipping/methods-configure/{id}` | authenticated/store context | Methods | hidden action | contract-backed |
| Shipping | `/pages/shipping/origin` | authenticated/store context | Shipping | active | contract-backed |
| Shipping | `/pages/shipping/packaging` | authenticated/store context | Shipping | active | contract-backed |
| Shipping | `/pages/shipping/packaging/add` | authenticated/store context | Packaging | hidden action | contract-backed |
| Shipping | `/pages/shipping/rules` | authenticated/store context | Shipping | hidden/commented menu | deferred |
| Shipping | `/pages/shipping/rules/add` | authenticated/store context | Shipping rules | hidden | deferred |
| Payment | `/pages/payment/methods` | authenticated/store context | Payment | active | contract-backed |
| Payment | `/pages/payment/configure/{id}` | authenticated/store context | Payment | hidden action | contract-backed |
| Tax | `/pages/tax-management/classes-list` | authenticated/store context | Tax | active | contract-backed |
| Tax | `/pages/tax-management/classes-add` | authenticated/store context | Tax | hidden action | contract-backed |
| Tax | `/pages/tax-management/rate-list` | authenticated/store context | Tax | active | contract-backed |
| Tax | `/pages/tax-management/rate-add` | authenticated/store context | Tax | hidden action | contract-backed |
| Customers | `/pages/customer/list` | authenticated | Customer management | active | contract-backed |
| Customers | `/pages/customer/add` | authenticated | Customer management | hidden action | contract-backed |
| Customers | `/pages/customer/set-credentials` | authenticated | Customer management | hidden | deferred |
| Orders | `/pages/orders/order-list` | `IsOrderManagementVisible` intent | Orders | active, role-filtered | contract-backed |
| Orders | `/pages/orders/order-details` | `IsOrderManagementVisible` intent | Orders | hidden/detail | contract-backed |

Commented-out Angular menu entries and commented-out route declarations are recorded as
historical evidence where useful but are not counted as currently routed capabilities. The
explicit routes above are the implementation inventory.
