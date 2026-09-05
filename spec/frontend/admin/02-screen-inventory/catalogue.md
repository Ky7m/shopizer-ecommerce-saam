# Screen Inventory: Catalogue

## Product and category screens

| Screen family | Layout and data on load | URL state/actions | Capability |
|---|---|---|---|
| Products list | heading, search/filter, table of `Product` fields, paginator, create action | query search/page; open product, visibility action, product ordering | contract-backed |
| Product create/detail | grouped product form; descriptions, SKU, visibility/purchase settings, images/categories, inventory/prices tabs | `{id}` and child tab; save/cancel, media upload, association | contract-backed for published fields; options/properties/discount deferred |
| Product ordering | sortable product table/order controls | query ordering state | contract-backed only where ordering maps to list query; otherwise gap |
| Images | gallery/list and upload/delete | `{id}/images`; upload/remove | contract-backed |
| Category list | table/search and create action | query search/page; detail | contract-backed |
| Category create/detail | code, name/description, friendly URL, parent, sort/visibility fields where exact schemas support them | `{id}`; save/delete | contract-backed |
| Category hierarchy | tree/hierarchy with move action | selected category/parent | contract-backed move |
| Category association | product/category association list | product/category IDs | contract-backed |
| Inventory | availability rows, store context, quantity/status fields from `Availability` | product/inventory IDs | contract-backed availability only |
| Prices | price list/form under inventory/product | product SKU, availability ID, price ID | contract-backed MS-07 price operations |

## Deferred catalogue screens

Brands, catalogues, product groups, product types, options/values/sets, legacy option
variations, product attributes, product properties, and product discount are routable but show
the explicit contract-gap state from `01-api-contract/parity-gaps.md`. Do not render mock rows
or issue old `/v1/private/...` calls.

## Responsive behavior

Tables become cards with the primary identifier and status first on narrow screens; row actions
move to an accessible action menu. Product detail tabs become a select/accordion while
retaining the tab order. Upload drop zones have a keyboard file picker equivalent. Hierarchy
move controls include a non-drag alternative.
