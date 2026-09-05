# BFF Contract: Catalogue

## Contract-backed product and category operations

| Frontend BFF method/path | Provider | Exact provider method/path | Request -> response | Main UI fields |
|---|---|---|---|---|
| GET `/api/admin/v1/products` | MS-02 | GET `/products` | query -> `ProductListResponse` | exact `ProductListResponse` envelope; `Product` rows |
| POST `/api/admin/v1/products` | MS-02 | POST `/products` | `CreateProductRequest` -> `Product` | `sku`, descriptions, visibility/purchase fields only if present in schema |
| GET `/api/admin/v1/products/{productId}` | MS-02 | GET `/products/{productId}` | path -> `Product` | product detail |
| PUT `/api/admin/v1/products/{productId}` | MS-02 | PUT `/products/{productId}` | `UpdateProductRequest` -> `Product` | product form |
| DELETE `/api/admin/v1/products/{productId}` | MS-02 | DELETE `/products/{productId}` | path -> `DeletionResult` | product list/detail |
| PATCH `/api/admin/v1/products/{productId}/visibility` | MS-02 | PATCH `/products/{productId}/visibility` | `UpdateVisibilityRequest` -> `Product` | availability button |
| POST `/api/admin/v1/products/{productId}/categories/{categoryId}` | MS-02 | POST `/products/{productId}/categories/{categoryId}` | path -> `Product` | category association |
| DELETE `/api/admin/v1/products/{productId}/categories/{categoryId}` | MS-02 | DELETE `/products/{productId}/categories/{categoryId}` | path -> `Product` | category association |
| GET `/api/admin/v1/products/{productId}/availability` | MS-02 | GET `/products/{productId}/availability` | path -> `AvailabilityListResponse` | inventory |
| PUT `/api/admin/v1/products/{productId}/availability` | MS-02 | PUT `/products/{productId}/availability` | `ReplaceAvailabilityRequest` -> `AvailabilityListResponse` | inventory |
| POST `/api/admin/v1/products/{productId}/media` | MS-02 | POST `/products/{productId}/media` | `ProductMediaUploadRequest` -> `ProductMedia` | product images |
| DELETE `/api/admin/v1/products/{productId}/media/{mediaId}` | MS-02 | DELETE `/products/{productId}/media/{mediaId}` | path -> `DeletionResult` | product images |
| GET `/api/admin/v1/categories` | MS-02 | GET `/categories` | query -> `CategoryListResponse` | category rows |
| POST `/api/admin/v1/categories` | MS-02 | POST `/categories` | `CreateCategoryRequest` -> `Category` | category form |
| GET `/api/admin/v1/categories/{categoryId}` | MS-02 | GET `/categories/{categoryId}` | path -> `Category` | category detail |
| PUT `/api/admin/v1/categories/{categoryId}` | MS-02 | PUT `/categories/{categoryId}` | `UpdateCategoryRequest` -> `Category` | category form |
| DELETE `/api/admin/v1/categories/{categoryId}` | MS-02 | DELETE `/categories/{categoryId}` | path -> `CategoryDeletionResult` | category list/detail |
| PATCH `/api/admin/v1/categories/{categoryId}/visibility` | MS-02 | PATCH `/categories/{categoryId}/visibility` | `UpdateVisibilityRequest` -> `Category` | category visibility |
| PUT `/api/admin/v1/categories/{categoryId}/move/{parentId}` | MS-02 | PUT `/categories/{categoryId}/move/{parentId}` | path -> `Category` | hierarchy |
| GET `/api/admin/v1/categories/{categoryId}/products` | MS-02 | GET `/categories/{categoryId}/products` | query -> `ProductListResponse` | category products |
| GET `/api/admin/v1/products/{productId}/variants` | MS-02 | GET `/products/{productId}/variants` | query -> `ProductVariantListResponse` | variations |
| POST `/api/admin/v1/products/{productId}/variants` | MS-02 | POST `/products/{productId}/variants` | `CreateVariantRequest` -> `ProductVariant` | variation form |
| GET `/api/admin/v1/products/{productId}/variants/{variantId}` | MS-02 | GET `/products/{productId}/variants/{variantId}` | path -> `ProductVariant` | variation detail |
| PUT `/api/admin/v1/products/{productId}/variants/{variantId}` | MS-02 | PUT `/products/{productId}/variants/{variantId}` | `UpdateVariantRequest` -> `ProductVariant` | variation form |
| DELETE `/api/admin/v1/products/{productId}/variants/{variantId}` | MS-02 | DELETE `/products/{productId}/variants/{variantId}` | path -> `DeletionResult` | variation list |
| GET `/api/admin/v1/products/{sku}/prices` | MS-07 | GET `/private/products/{sku}/prices` | query -> `PriceListResponse` | prices list |
| POST `/api/admin/v1/products/{sku}/prices` | MS-07 | POST `/private/products/{sku}/prices` | `ProductPriceCreateRequest` -> `PriceCreatedResponse` | price form |
| GET `/api/admin/v1/products/{sku}/prices/{priceId}` | MS-07 | GET `/private/products/{sku}/prices/{priceId}` | path -> `Price` | price form |
| DELETE `/api/admin/v1/products/{sku}/prices/{priceId}` | MS-07 | DELETE `/private/products/{sku}/prices/{priceId}` | path -> `DeletePriceResponse` | prices list |
| PUT `/api/admin/v1/products/{sku}/availabilities/{availabilityId}/prices/{priceId}` | MS-07 | PUT `/private/products/{sku}/availabilities/{availabilityId}/prices/{priceId}` | `PriceUpdateRequest` -> `Price` | price form |

The frontend binds exact provider fields from `Product`, `Category`, `ProductVariant`,
`ProductMedia`, `Availability`, and `Price`. Product forms preserve legacy groups for SKU,
localized descriptions, visibility/display/purchasability, quantity, images, categories,
options, properties, inventory, and prices, but controls are rendered only for fields present
in the provider request schema.

## Loading, pagination, and errors

List pages keep search/page state in query parameters and send only provider-supported query
parameters. Show skeleton rows, 200 empty-state panels, provider 422 field errors, 409
uniqueness/conflict recovery, and retryable 500/503 states. Mutating a product/category
invalidates the relevant list and detail cache. Media and price mutations show an operation
progress state and retain the selected product route.

## Contract gaps

There is no published MS-02 operation for brands/manufacturers, product types, options,
option values, option sets, variations under the legacy option model, product groups,
product attributes/properties, or catalogues. The routed screens remain in the inventory as
deferred screens and must render a contract-gap explanation, not issue legacy paths or fake
CRUD calls. The published variant operations are the only variation binding.

The legacy product “option price” call has no corresponding admin operation in the published
contracts; do not use the storefront pricing operations as an admin substitute without an
approved workflow and authorization decision.
