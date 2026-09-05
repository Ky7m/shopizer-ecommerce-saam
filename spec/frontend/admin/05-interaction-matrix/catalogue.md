# Interaction Matrix: Catalogue

| Area/action | Trigger | BFF call/navigation | Feedback | Gating/entity state |
|---|---|---|---|---|
| Product list search/page | toolbar/page | GET `/api/admin/v1/products` | skeleton/empty/pagination | catalogue scope |
| Create product | button | navigate create | form | catalogue scope |
| Save product | valid submit | POST/PUT `/api/admin/v1/products...` | pending, 409 SKU, 422 fields | SKU/tenant context |
| Product visibility | switch | PATCH `/api/admin/v1/products/{productId}/visibility` | pending switch/revert on error | product exists |
| Product media upload/delete | file/confirm | POST/DELETE `/api/admin/v1/products/{productId}/media...` | progress/gallery/error | product exists |
| Category association | add/remove | POST/DELETE product/category association | update association | both entities exist |
| Availability | load/save inventory | GET/PUT `/api/admin/v1/products/{productId}/availability` | skeleton/save/error | product exists |
| Category list/search | toolbar/page | GET `/api/admin/v1/categories` | skeleton/empty/pagination | category scope |
| Category save/delete | form/confirm | POST/PUT/DELETE `/api/admin/v1/categories...` | conflict/validation/confirmation | parent exists for move |
| Category visibility | switch | PATCH category visibility | pending/revert | category exists |
| Category move | choose parent/confirm | PUT category move | tree pending; conflict | cannot choose invalid parent |
| Variant list/form | open/add/save/delete | `/api/admin/v1/products/{productId}/variants...` | pending/409 SKU | product exists |
| Price list/form | open/add/update/delete | MS-07 price BFF paths | pending/conflict | product SKU and availability exist |
| Brand/type/option/group/catalogue/property | any action | no call | contract-gap state | deferred |
| Discount/promotion | open/save | no call | contract-gap state | deferred |
