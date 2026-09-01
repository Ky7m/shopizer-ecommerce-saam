# Catalog and Product — API Design

**Base path:** `/api/v1/catalog`  
**Port:** `8102`  
**JSON naming:** camelCase  
**URL naming:** kebab-case  
**Required request context:** `x-tenant-id`, `x-store-id`, `x-correlation-id`  
**Authentication:** bearer JWT for mutations and administrative reads; storefront reads require tenant/store context.

## Product endpoints

### POST `/products`
Creates a product and its descriptions, categories, variants, availability, options, and media metadata.

- Auth: catalog administrator
- Response: `201 Product`
- Rules: BR-CAT-001, BR-CAT-003..005, BR-CAT-017, BR-CAT-028, BR-CAT-032

### GET `/products`
Returns a paginated product listing.

- Auth: storefront or administrator
- Query: `page`, `pageSize`, `languageCode`, `countryCode`, `categoryId`, `sku`, `name`, `manufacturerCode`, `available`
- Response: `200 ProductListResponse`
- Rules: BR-CAT-009, BR-CAT-025, BR-CAT-026

### GET `/products/{productId}`
Returns a product in the requested language and region.

- Auth: storefront or administrator
- Response: `200 Product`
- Rules: BR-CAT-009, BR-CAT-031, BR-CAT-033

### GET `/products/slug/{friendlyUrl}`
Returns an eligible product by localized friendly URL.

- Auth: storefront or administrator
- Response: `200 Product`
- Rules: BR-CAT-010, BR-CAT-033

### GET `/products/sku/{sku}`
Returns one product by store-scoped SKU.

- Auth: storefront or administrator
- Response: `200 Product`
- Rules: BR-CAT-001, BR-CAT-009

### GET `/products/uniqueness`
Checks product SKU uniqueness in the current store.

- Auth: catalog administrator
- Response: `200 ExistsResponse`
- Rules: BR-CAT-001, BR-UI-003

### PUT `/products/{productId}`
Updates product metadata and aggregate associations.

- Auth: catalog administrator
- Response: `200 Product`
- Rules: BR-CAT-001, BR-CAT-003..005, BR-CAT-017, BR-CAT-018, BR-CAT-028, BR-CAT-032

### PATCH `/products/{productId}/visibility`
Updates visibility and purchase eligibility.

- Auth: catalog administrator
- Response: `200 Product`
- Rules: BR-CAT-009, BR-CAT-032, BR-UI-004

### DELETE `/products/{productId}`
Deletes a product aggregate and dependent records.

- Auth: catalog administrator
- Response: `200 DeletionResult`
- Rules: BR-CAT-019, BR-CAT-038

### POST `/products/{productId}/categories/{categoryId}`
Attaches a product to a store-scoped category.

- Auth: catalog administrator
- Response: `200 Product`
- Rules: BR-CAT-005, BR-CAT-028

### DELETE `/products/{productId}/categories/{categoryId}`
Detaches a product from a category.

- Auth: catalog administrator
- Response: `200 Product`
- Rules: BR-CAT-008, BR-CAT-019

## Category endpoints

### POST `/categories`
Creates a category and materializes lineage/depth.

- Auth: catalog administrator
- Response: `201 Category`
- Rules: BR-CAT-003, BR-CAT-006, BR-CAT-028, BR-UI-006

### GET `/categories`
Returns root or filtered category hierarchy.

- Auth: storefront or administrator
- Query: `page`, `pageSize`, `languageCode`, `name`, `visible`, `featured`
- Response: `200 CategoryListResponse`
- Rules: BR-CAT-003, BR-UI-006

### GET `/categories/{categoryId}`
Returns one category and its children.

- Auth: storefront or administrator
- Response: `200 Category`
- Rules: BR-CAT-003, BR-CAT-006, BR-UI-006

### GET `/categories/slug/{friendlyUrl}`
Returns a category by localized friendly URL.

- Auth: storefront or administrator
- Response: `200 Category`
- Rules: BR-UI-006

### GET `/categories/uniqueness`
Checks category code uniqueness.

- Auth: catalog administrator
- Response: `200 ExistsResponse`
- Rules: BR-CAT-003, BR-UI-006

### PUT `/categories/{categoryId}`
Updates category metadata.

- Auth: catalog administrator
- Response: `200 Category`
- Rules: BR-CAT-003, BR-CAT-006, BR-UI-006

### PATCH `/categories/{categoryId}/visibility`
Updates category visibility.

- Auth: catalog administrator
- Response: `200 Category`
- Rules: BR-UI-006

### PUT `/categories/{categoryId}/move/{parentId}`
Moves a category and recursively recalculates descendants.

- Auth: catalog administrator
- Response: `200 Category`
- Rules: BR-CAT-007, BR-CAT-034

### DELETE `/categories/{categoryId}`
Deletes a category subtree according to orphan-product policy.

- Auth: catalog administrator
- Query: `orphanProductPolicy=Detach|Delete|Reject`
- Response: `200 CategoryDeletionResult`
- Rules: BR-CAT-008

### GET `/categories/{categoryId}/products`
Lists eligible products in a category subtree.

- Auth: storefront or administrator
- Response: `200 ProductListResponse`
- Rules: BR-CAT-009, BR-CAT-026

## Variant and option endpoints

### POST `/products/{productId}/variants`
Creates a product variant.

- Auth: catalog administrator
- Response: `201 ProductVariant`
- Rules: BR-CAT-002, BR-CAT-028

### GET `/products/{productId}/variants`
Lists product variants.

- Auth: storefront or administrator
- Response: `200 ProductVariantListResponse`
- Rules: BR-CAT-002, BR-CAT-031

### GET `/products/{productId}/variants/{variantId}`
Reads one variant.

- Auth: storefront or administrator
- Response: `200 ProductVariant`
- Rules: BR-CAT-002, BR-CAT-012

### PUT `/products/{productId}/variants/{variantId}`
Updates a product variant.

- Auth: catalog administrator
- Response: `200 ProductVariant`
- Rules: BR-CAT-002, BR-CAT-028

### DELETE `/products/{productId}/variants/{variantId}`
Deletes a variant.

- Auth: catalog administrator
- Response: `200 DeletionResult`
- Rules: BR-CAT-019, BR-CAT-038

### GET `/products/{productId}/variants/uniqueness/{sku}`
Checks variant SKU uniqueness under a product.

- Auth: catalog administrator
- Response: `200 ExistsResponse`
- Rules: BR-CAT-002

### POST `/products/{productId}/options/price`
Calculates a price for selected option/value pairs.

- Auth: storefront or administrator
- Response: `200 PriceResponse`
- Rules: BR-CAT-015, BR-CAT-016, BR-CAT-029, BR-CAT-031

## Availability and reservation endpoints

### GET `/products/{productId}/availability`
Returns regional availability.

- Auth: storefront or administrator
- Response: `200 AvailabilityListResponse`
- Rules: BR-CAT-009, BR-CAT-011

### PUT `/products/{productId}/availability`
Replaces product availability.

- Auth: catalog administrator
- Response: `200 AvailabilityListResponse`
- Rules: BR-CAT-004, BR-CAT-011, BR-CAT-028

### POST `/products/{productId}/reservations`
Atomically reserves availability.

- Auth: checkout/order service or authorized administrator
- Header: `Idempotency-Key`
- Response: `201 InventoryReservation`
- Rules: BR-ORD-012, BR-CAT-037

### POST `/reservations/{reservationId}/commit`
Commits a held reservation.

- Auth: checkout/order service
- Response: `200 InventoryReservation`
- Rules: BR-CAT-039

### POST `/reservations/{reservationId}/release`
Releases a held reservation.

- Auth: checkout/order service
- Response: `200 InventoryReservation`
- Rules: BR-CAT-039

## Media endpoints

### POST `/products/{productId}/media`
Uploads binary or external product media.

- Auth: catalog administrator
- Response: `201 ProductMedia`
- Rules: BR-CAT-017, BR-CAT-018, BR-EXT-019, BR-EXT-020

### DELETE `/products/{productId}/media/{mediaId}`
Deletes media and publishes a projection change.

- Auth: catalog administrator
- Response: `200 DeletionResult`
- Rules: BR-CAT-017, BR-CAT-035

## Events published

| Event | Trigger | Consumers |
|---|---|---|
| `ProductChanged.v1` | Product/variant/attribute mutation or deletion | MS-03 Search; MS-04 Cart; MS-07 Pricing |
| `CategoryChanged.v1` | Category create/update/move/delete | MS-03 Search; storefront projections |
| `AvailabilityChanged.v1` | Availability update, reservation, commit, release | MS-04 Cart; MS-05 Order |
| `MediaChanged.v1` | Media create/update/delete | MS-03 Search; MS-12 media delivery |
| `InventoryReservationChanged.v1` | Reservation held/committed/released/expired | MS-04 Cart; MS-05 Order |

Events use an outbox, include `eventId`, `eventType`, `aggregateId`, `aggregateVersion`, `tenantId`, `storeId`, and `occurredAt`.

## Events consumed

MS-02 does not consume search index commands. Store scope is validated synchronously against MS-10. Product and category projection consumers are downstream of MS-02.

## Dependencies

- Upstream: MS-10 store-scope validation; MS-01 operator identity; shared language/reference contracts.
- Downstream: MS-03 Search, MS-04 Cart and Checkout, MS-05 Order Management, MS-07 Pricing.
- External: configured object/media provider.
- No cross-service foreign keys or direct writes to another service schema.
