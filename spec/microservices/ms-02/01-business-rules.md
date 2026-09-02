# Catalog and Product Specification — Business Rules

**Service:** MS-02 Catalog and Product  
**Version:** 1.0  
**Date:** 2026-09-01  
**Status:** 🟡 Phase 4 complete; BA validation pending  
**Analysis mode:** Hybrid — CAST-guided component selection plus direct Java/TypeScript/JavaScript source read  
**CAST application:** Shopizer-Backend  
**Boundary:** MS-02 owns products, categories, variants, availability, media, and atomic inventory reservation/decrement. Store scope is opaque and validated through MS-10. Search read/index ownership remains with MS-03.

## Rule conventions

- Statements describe business meaning and do not use legacy table, column, variable, or method names.
- Logic records implementation evidence and is not the target design.
- `Source` and `Spec` values in preservation tables are local counts for the rule's source and target behavior.
- `OK` means the target preserves the observed behavior; `FLAGGED` means the legacy behavior is retained as an explicit review or compatibility decision; `GAP` means the target intentionally closes a legacy defect or fills a target-architecture gap.
- CAST references identify the CAST-guided component/transaction family. Numeric CAST IDs were not exposed in this session.

### BR-CAT-001: Product SKU is unique within a merchant store

**Source Reference:** `initial-source/shopizer-3.2.7/sm-core-model/src/main/java/com/salesmanager/core/model/catalog/product/Product.java:50-53,190-191`  
**Discovery Method:** Hybrid (CAST-guided Product persistence target + direct source read)  
**CAST Reference:** `Shopizer-Backend / Product persistence transaction / Product.java`; numeric CAST object ID unavailable locally  
**Semantic Preservation:**

| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 2 | 2 | OK |
| Data-flow | 3 | 3 | OK |
| Constants | 1 | 1 | OK |
| State transitions | 0 | 0 | OK |
| Outcomes | 2 | 2 | OK |
| Data writes | 1 | 1 | OK |
| Integrations | 0 | 0 | OK |
| Error paths | 1 | 2 | OK |

**Preservation:** OK  
**Statement:** A product SKU must be unique within its owning store; two products in the same store cannot be created or updated to the same SKU. The same SKU may exist in another store.  
**Intent:** Validation  
**Classification:** Core
**Weight:** Critical
**Logic:** Before product creation or update, query the product identity scope for the candidate SKU and store. Reject a create when any matching product exists; on update, ignore the current product identity. Enforce the invariant again with a database unique constraint.  
**Data:** `product.store_id`, `product.sku`, `product.id`; opaque store validation from MS-10.  
**Side Effects:** No product write occurs on conflict; emit `ProductChanged.v1` only after a successful mutation.  
**Concrete Example:**
- Input: `POST /api/v1/catalog/products` with `{"storeId":"store-eu","sku":"TSHIRT-BLUE-M","visible":true,"canBePurchased":true,"dateAvailable":"2026-09-01","descriptions":[{"languageCode":"en","name":"Blue T-Shirt","friendlyUrl":"blue-t-shirt"}],"availabilities":[{"regionCode":"*","quantity":25}]}`
- Success: `201` with `{"id":"11111111-1111-4111-8111-111111111111","sku":"TSHIRT-BLUE-M"}`
- Error Input: same request while `TSHIRT-BLUE-M` already belongs to `store-eu`
- Error Output: `409 {"error":"PRODUCT_SKU_CONFLICT","message":"SKU 'TSHIRT-BLUE-M' already exists in store 'store-eu'","statusCode":409}`

### BR-CAT-002: Variant SKU is unique within its parent product

**Source Reference:** `initial-source/shopizer-3.2.7/sm-core-model/src/main/java/com/salesmanager/core/model/catalog/product/variant/ProductVariant.java:37-44,77-93`  
**Discovery Method:** Hybrid (CAST-guided variant-management target + direct source read)  
**CAST Reference:** `Shopizer-Backend / Product variant management transaction / ProductVariant.java`; numeric CAST object ID unavailable locally  
**Semantic Preservation:**

| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 2 | 2 | OK |
| Data-flow | 3 | 3 | OK |
| Constants | 1 | 1 | OK |
| State transitions | 1 | 1 | OK |
| Outcomes | 2 | 2 | OK |
| Data writes | 1 | 1 | OK |
| Integrations | 0 | 0 | OK |
| Error paths | 1 | 2 | OK |

**Preservation:** OK  
**Statement:** A variant SKU must be unique among the variants of one parent product.  
**Intent:** Validation  
**Classification:** Core
**Weight:** Critical
**Logic:** Query variants by `product_id` and candidate `sku`; exclude the current variant during update; reject duplicates.  
**Data:** `product_variant.product_id`, `product_variant.sku`, `product_variant.id`.  
**Side Effects:** No variant write on conflict; successful changes publish `ProductChanged.v1`.  
**Concrete Example:**
- Input: `POST /api/v1/catalog/products/11111111-1111-4111-8111-111111111111/variants` with `{"sku":"TSHIRT-BLUE-M","code":"blue-m","defaultSelection":true,"availability":{"regionCode":"*","quantity":10}}`
- Success: `201 {"id":"22222222-2222-4222-8222-222222222222","sku":"TSHIRT-BLUE-M"}`
- Error Input: another variant of the same product already uses `TSHIRT-BLUE-M`
- Error Output: `409 {"error":"VARIANT_SKU_CONFLICT","message":"Variant SKU already exists for this product","statusCode":409}`

### BR-CAT-003: Catalog identity codes are merchant-scoped

**Source Reference:** `initial-source/shopizer-3.2.7/sm-core-model/src/main/java/com/salesmanager/core/model/catalog/category/Category.java:32-40,89-90`; `initial-source/shopizer-3.2.7/sm-core-model/src/main/java/com/salesmanager/core/model/catalog/catalog/Catalog.java:47-50,75-87`; `initial-source/shopizer-3.2.7/sm-core-model/src/main/java/com/salesmanager/core/model/catalog/product/attribute/ProductOption.java:30-37,59-68`; `initial-source/shopizer-3.2.7/sm-core-model/src/main/java/com/salesmanager/core/model/catalog/product/attribute/ProductOptionValue.java:32-38,78-80`; `initial-source/shopizer-3.2.7/sm-core-model/src/main/java/com/salesmanager/core/model/catalog/product/variation/ProductVariation.java:37-46,59-72`  
**Discovery Method:** Hybrid (CAST-guided catalog model target + direct source read)  
**CAST Reference:** `Shopizer-Backend / Catalog administration transactions / catalog identity entities`; numeric IDs unavailable locally  
**Semantic Preservation:**

| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 4 | 4 | OK |
| Data-flow | 8 | 8 | OK |
| Constants | 3 | 3 | OK |
| State transitions | 0 | 0 | OK |
| Outcomes | 3 | 3 | OK |
| Data writes | 5 | 5 | OK |
| Integrations | 0 | 0 | OK |
| Error paths | 2 | 3 | OK |

**Preservation:** OK  
**Statement:** Category, catalog, product-option, option-value, and product-variation codes are unique within the owning merchant/store scope and may be reused by a different merchant/store.  
**Intent:** Validation  
**Classification:** Core
**Weight:** High
**Logic:** Resolve the owning store before checking a code. Apply uniqueness by `(store_id, code)` for each aggregate type.  
**Data:** `category.store_id/code`, `catalog.store_id/code`, `product_option.store_id/code`, `product_option_value.store_id/code`, `product_variation.store_id/code`.  
**Side Effects:** Conflicting aggregate is not persisted.  
**Concrete Example:**
- Input: `POST /api/v1/catalog/categories` with `{"storeId":"store-eu","code":"summer","parentId":null,"descriptions":[{"languageCode":"en","name":"Summer","friendlyUrl":"summer"}]}`
- Success: `201` when `summer` is unused in `store-eu`, even if `store-us` uses it.
- Error Input: `summer` already exists in `store-eu`
- Error Output: `409 {"error":"CATEGORY_CODE_CONFLICT","message":"Category code is already used in this store","statusCode":409}`

### BR-CAT-004: A product must have availability before persistence

**Source Reference:** `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/catalog/product/ProductServiceImpl.java:259-263`  
**Discovery Method:** Hybrid (CAST-guided ProductServiceImpl target + direct source read)  
**CAST Reference:** `Shopizer-Backend / Product create-update transaction / ProductServiceImpl.saveOrUpdate`; numeric ID unavailable locally  
**Semantic Preservation:**

| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 3 | 3 | OK |
| Data-flow | 3 | 3 | OK |
| Constants | 0 | 0 | OK |
| State transitions | 1 | 1 | OK |
| Outcomes | 2 | 2 | OK |
| Data writes | 1 | 1 | OK |
| Integrations | 0 | 0 | OK |
| Error paths | 2 | 2 | OK |

**Preservation:** OK  
**Statement:** A product cannot be created or updated without at least one availability record describing where and how much stock can be sold.  
**Intent:** Validation  
**Classification:** Core
**Weight:** High
**Logic:** Reject a null or empty availability collection before persistence. The target additionally requires a positive quantity or an explicit unlimited-stock policy.  
**Data:** `product_availability.product_id`, `region_code`, `quantity`, `active`.  
**Side Effects:** Product transaction is rolled back; no media processing occurs.  
**Concrete Example:**
- Input: `POST /api/v1/catalog/products` with `{"storeId":"store-eu","sku":"MUG-001","descriptions":[{"languageCode":"en","name":"Mug","friendlyUrl":"mug"}],"availabilities":[]}`
- Success: A product with `{"regionCode":"*","quantity":100}` returns `201`.
- Error Output: `422 {"error":"AVAILABILITY_REQUIRED","message":"At least one product availability is required","statusCode":422}`

### BR-CAT-005: Product references must resolve within the current store

**Source Reference:** `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/mapper/catalog/product/PersistableProductMapper.java:126-144,210-232`  
**Discovery Method:** Hybrid (CAST-guided PersistableProductMapper target + direct source read)  
**CAST Reference:** `Shopizer-Backend / Product request mapping transaction / PersistableProductMapper.merge`; numeric ID unavailable locally  
**Semantic Preservation:**

| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 8 | 8 | OK |
| Data-flow | 10 | 10 | OK |
| Constants | 1 | 1 | OK |
| State transitions | 1 | 1 | OK |
| Outcomes | 5 | 5 | OK |
| Data writes | 4 | 4 | OK |
| Integrations | 2 | 2 | OK |
| Error paths | 4 | 5 | OK |

**Preservation:** OK  
**Statement:** Manufacturer, product-type, category, and language references supplied for a product must exist and belong to the product's current store or declared shared reference scope.  
**Intent:** Validation  
**Classification:** Core
**Weight:** High
**Logic:** Resolve each reference using the request store. Reject missing manufacturer/type/category/language references and reject a category belonging to another store.  
**Data:** `product.store_id`, `category.store_id`, `manufacturer.store_id`, `product_type.store_id`, `language.code`.  
**Side Effects:** No product write when any reference fails.  
**Concrete Example:**
- Input: product request with `{"manufacturerCode":"acme","categories":[{"code":"electronics"}],"descriptions":[{"languageCode":"en","name":"Camera","friendlyUrl":"camera"}]}`
- Success: `201` when all references resolve in `store-eu`.
- Error Output: `422 {"error":"CATEGORY_SCOPE_INVALID","message":"Category 'electronics' is not owned by store 'store-eu'","statusCode":422}`

### BR-CAT-006: Category creation materializes lineage and depth

**Source Reference:** `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/catalog/category/CategoryServiceImpl.java:54-70`  
**Discovery Method:** Hybrid (CAST-guided CategoryServiceImpl target + direct source read)  
**CAST Reference:** `Shopizer-Backend / Category creation transaction / CategoryServiceImpl.create`; numeric ID unavailable locally  
**Semantic Preservation:**

| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 4 | 4 | OK |
| Data-flow | 5 | 5 | OK |
| Constants | 1 | 1 | OK |
| State transitions | 2 | 2 | OK |
| Outcomes | 3 | 3 | OK |
| Data writes | 2 | 2 | OK |
| Integrations | 0 | 0 | OK |
| Error paths | 1 | 2 | OK |

**Preservation:** OK  
**Statement:** A new category receives a hierarchy path and depth derived from its validated parent; a root category has depth zero.  
**Intent:** Calculation  
**Classification:** Active
**Weight:** Medium
**Logic:** Persist the category identity, obtain the parent if supplied, set `depth = parent.depth + 1` and `lineage = parent.lineage + category.id`, or set root lineage to `/{id}/` and depth to `0`.  
**Data:** `category.id`, `parent_id`, `depth`, `lineage`, `store_id`.  
**Side Effects:** Category is written twice in the legacy flow; the target should calculate lineage before one atomic insert. Publish `CategoryChanged.v1`.  
**Concrete Example:**
- Input: `POST /api/v1/catalog/categories` with `{"code":"laptops","parentId":"33333333-3333-4333-8333-333333333333","descriptions":[{"languageCode":"en","name":"Laptops","friendlyUrl":"laptops"}]}`
- Success: response contains `depth:2` and a lineage ending in the new category ID.
- Error Output: `422 {"error":"PARENT_CATEGORY_NOT_FOUND","message":"Parent category does not exist in this store","statusCode":422}`

### BR-CAT-007: Category moves recalculate descendants

**Source Reference:** `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/catalog/category/CategoryServiceImpl.java:299-345`  
**Discovery Method:** Hybrid (CAST-guided category hierarchy transaction + direct source read)  
**CAST Reference:** `Shopizer-Backend / Category move transaction / CategoryServiceImpl.addChild`; numeric ID unavailable locally  
**Semantic Preservation:**

| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 9 | 10 | FLAGGED |
| Data-flow | 8 | 9 | FLAGGED |
| Constants | 1 | 1 | OK |
| State transitions | 3 | 3 | OK |
| Outcomes | 4 | 4 | OK |
| Data writes | 5 | 5 | OK |
| Integrations | 0 | 0 | OK |
| Error paths | 3 | 5 | GAP |

**Preservation:** FLAGGED  
**Statement:** Moving a category changes its parent, depth, and lineage, and the same recalculation must be applied to every descendant in the moved subtree. A category cannot be moved beneath itself or one of its descendants.  
**Intent:** State Transition  
**Classification:** Core
**Weight:** High
**Logic:** Resolve the new parent in the same store; set the moved category's parent, depth, and lineage; recursively update descendants. The target adds cycle detection and performs the subtree change atomically.  
**Data:** `category.parent_id`, `depth`, `lineage`, `store_id`.  
**Side Effects:** Descendant rows are updated; publish one `CategoryChanged.v1` event with subtree version.  
**Concrete Example:**
- Input: `PUT /api/v1/catalog/categories/44444444-4444-4444-8444-444444444444/move/33333333-3333-4333-8333-333333333333`
- Success: moved category and all descendants return updated lineage/depth.
- Error Output: `409 {"error":"CATEGORY_CYCLE","message":"A category cannot be moved below itself or its descendants","statusCode":409}`

### BR-CAT-008: Category deletion applies subtree product policy

**Source Reference:** `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/catalog/category/CategoryServiceImpl.java:231-285`  
**Discovery Method:** Hybrid (CAST-guided category deletion transaction + direct source read)  
**CAST Reference:** `Shopizer-Backend / Category deletion transaction / CategoryServiceImpl.delete`; numeric ID unavailable locally  
**Semantic Preservation:**

| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 12 | 13 | FLAGGED |
| Data-flow | 11 | 12 | FLAGGED |
| Constants | 1 | 1 | OK |
| State transitions | 4 | 4 | OK |
| Outcomes | 5 | 5 | OK |
| Data writes | 8 | 8 | OK |
| Integrations | 0 | 1 | GAP |
| Error paths | 3 | 5 | GAP |

**Preservation:** FLAGGED  
**Statement:** Deleting a category removes the category subtree. A product assigned to another category is detached from the deleted subtree; a product with no remaining category is deleted according to the approved catalog policy.  
**Intent:** State Transition  
**Classification:** Core
**Weight:** High
**Logic:** Build the subtree from lineage, process descendants deepest-first, load affected products, remove deleted category links, delete products with no remaining category, then delete categories. The target performs the operation atomically and emits product/category change events.  
**Data:** `category.lineage`, `product_category`, `product.categories`, `product.id`.  
**Side Effects:** Category links, products, media, and derived projections may be deleted or updated.  
**Concrete Example:**
- Input: `DELETE /api/v1/catalog/categories/44444444-4444-4444-8444-444444444444?orphanProductPolicy=Delete`
- Success: `200 {"deletedCategoryCount":3,"detachedProductCount":4,"deletedProductCount":1}`
- Error Output: `409 {"error":"CATEGORY_DELETE_BLOCKED","message":"Deletion would orphan protected products","statusCode":409}`

### BR-CAT-009: Storefront listings require product and regional eligibility

**Source Reference:** `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/repositories/catalog/product/ProductRepositoryImpl.java:281-315,480-557`  
**Discovery Method:** Hybrid (CAST-guided storefront listing transaction + direct source read)  
**CAST Reference:** `Shopizer-Backend / Storefront product listing transaction / ProductRepositoryImpl`; numeric ID unavailable locally  
**Semantic Preservation:**

| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 12 | 12 | OK |
| Data-flow | 12 | 12 | OK |
| Constants | 2 | 2 | OK |
| State transitions | 1 | 1 | OK |
| Outcomes | 4 | 4 | OK |
| Data writes | 0 | 0 | OK |
| Integrations | 1 | 1 | OK |
| Error paths | 3 | 4 | OK |

**Preservation:** OK  
**Statement:** A storefront listing includes only products belonging to the requested store, active for sale, effective by date, translated into the requested language, and available for the requested region or the wildcard region.  
**Intent:** Validation  
**Classification:** Core
**Weight:** High
**Logic:** Filter by store, category scope, active product flag, `dateAvailable <= now`, language description, and availability region in `[* , requestedCountry]`.  
**Data:** `product.store_id`, `available`, `date_available`, `product_description.language_code`, `product_availability.region_code`, `product_category`.  
**Side Effects:** Read-only; no catalog writes.  
**Concrete Example:**
- Input: `GET /api/v1/catalog/products?storeId=store-eu&languageCode=en&countryCode=DE&categoryId=55555555-5555-4555-8555-555555555555`
- Success: only active products with `*` or `DE` availability and an effective English description are returned.
- Error Output: `404 {"error":"NO_ELIGIBLE_PRODUCTS","message":"No products satisfy the requested storefront criteria","statusCode":404}`

### BR-CAT-010: Friendly URL reads apply the same storefront filters

**Source Reference:** `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/repositories/catalog/product/ProductRepositoryImpl.java:207-267`  
**Discovery Method:** Hybrid (CAST-guided friendly-URL transaction + direct source read)  
**CAST Reference:** `Shopizer-Backend / Friendly URL product read / ProductRepositoryImpl.getByFriendlyUrl`; numeric ID unavailable locally  
**Semantic Preservation:**

| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 7 | 7 | OK |
| Data-flow | 9 | 9 | OK |
| Constants | 2 | 2 | OK |
| State transitions | 1 | 1 | OK |
| Outcomes | 3 | 3 | OK |
| Data writes | 0 | 0 | OK |
| Integrations | 0 | 0 | OK |
| Error paths | 2 | 3 | OK |

**Preservation:** OK  
**Statement:** A friendly URL identifies a product only when the slug, store, effective date, active status, and requested region all match.  
**Intent:** Validation  
**Classification:** Core
**Weight:** High
**Logic:** Query localized product descriptions by slug, join eligible availability rows, restrict product store and active/date predicates, and return one product or not found.  
**Data:** `product_description.friendly_url`, `product.store_id`, `available`, `date_available`, `product_availability.region_code`.  
**Side Effects:** Read-only.  
**Concrete Example:**
- Input: `GET /api/v1/catalog/products/slug/blue-t-shirt?storeId=store-eu&languageCode=en&countryCode=DE`
- Success: `200` product detail.
- Error Output: `404 {"error":"PRODUCT_NOT_FOUND","message":"No eligible product matches friendly URL 'blue-t-shirt'","statusCode":404}`

### BR-CAT-011: Wildcard region is the preferred default availability

**Source Reference:** `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/catalog/inventory/ProductInventoryServiceImpl.java:62-73`  
**Discovery Method:** Hybrid (CAST-guided inventory read target + direct source read)  
**CAST Reference:** `Shopizer-Backend / Product inventory transaction / ProductInventoryServiceImpl.defaultAvailability`; numeric ID unavailable locally  
**Semantic Preservation:**

| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 4 | 4 | OK |
| Data-flow | 4 | 4 | OK |
| Constants | 1 | 1 | OK |
| State transitions | 0 | 0 | OK |
| Outcomes | 2 | 2 | OK |
| Data writes | 0 | 0 | OK |
| Integrations | 0 | 0 | OK |
| Error paths | 1 | 2 | OK |

**Preservation:** OK  
**Statement:** When an inventory view needs one default availability, the wildcard region is selected over country-specific rows; if no wildcard exists, the target must apply an explicit fallback policy rather than selecting arbitrarily.  
**Intent:** Routing  
**Classification:** Active
**Weight:** Low
**Logic:** Iterate availability rows, prefer `region_code='*'`, otherwise use the configured deterministic fallback.  
**Data:** `product_availability.region_code`, `quantity`, `active`.  
**Side Effects:** Read-only for inventory lookup.  
**Concrete Example:**
- Input: product has `* = 20` and `DE = 5`; `GET /availability`
- Success: response uses quantity `20` as the default.
- Error Output: `409 {"error":"DEFAULT_AVAILABILITY_UNRESOLVED","message":"No wildcard or configured regional availability exists","statusCode":409}`

### BR-CAT-012: Default-selected variant supplies price when usable

**Source Reference:** `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/utils/ProductPriceUtils.java:550-576`  
**Discovery Method:** Hybrid (CAST-guided pricing calculation target + direct source read)  
**CAST Reference:** `Shopizer-Backend / Product price calculation transaction / ProductPriceUtils.calculateFinalPrice(Product)`; numeric ID unavailable locally  
**Semantic Preservation:**

| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 6 | 6 | OK |
| Data-flow | 7 | 7 | OK |
| Constants | 1 | 1 | OK |
| State transitions | 0 | 0 | OK |
| Outcomes | 3 | 3 | OK |
| Data writes | 0 | 0 | OK |
| Integrations | 1 | 1 | OK |
| Error paths | 2 | 3 | OK |

**Preservation:** OK  
**Statement:** If a product has a default-selected variant with a usable priced availability, that variant supplies the product's displayed price; otherwise pricing falls back to the parent product's eligible availability.  
**Intent:** Routing  
**Classification:** Core
**Weight:** High
**Logic:** Find the default-selected variant, retain its availabilities containing prices, and use them if non-empty; otherwise use parent product availabilities.  
**Data:** `product_variant.default_selection`, `product_variant.availability_id`, `product_availability`, `product_price`.  
**Side Effects:** Read-only calculation.  
**Concrete Example:**
- Input: product `TSHIRT-001`, default variant `TSHIRT-001-BLUE-M` priced at `24.00`, parent price `29.00`.
- Success: `200 {"sku":"TSHIRT-001","finalPrice":24.00,"priceSource":"defaultVariant"}`
- Error Output: `422 {"error":"PRICE_UNAVAILABLE","message":"Neither the selected variant nor the parent product has a usable price","statusCode":422}`

### BR-CAT-013: Wildcard and default prices are evaluated first

**Source Reference:** `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/utils/ProductPriceUtils.java:578-610`  
**Discovery Method:** Hybrid (CAST-guided price selection target + direct source read)  
**CAST Reference:** `Shopizer-Backend / Product price selection / ProductPriceUtils.calculateFinalPrice`; numeric ID unavailable locally  
**Semantic Preservation:**

| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 7 | 7 | OK |
| Data-flow | 7 | 7 | OK |
| Constants | 2 | 2 | OK |
| State transitions | 0 | 0 | OK |
| Outcomes | 3 | 3 | OK |
| Data writes | 0 | 0 | OK |
| Integrations | 0 | 0 | OK |
| Error paths | 2 | 3 | OK |

**Preservation:** OK  
**Statement:** Price resolution considers prices attached to wildcard availability first and selects the explicitly default price before additional prices.  
**Intent:** Routing  
**Classification:** Core
**Weight:** High
**Logic:** Filter eligible availabilities to the wildcard region, convert each price, retain the default price as primary, and expose other prices as additional choices.  
**Data:** `product_availability.region_code`, `product_price.default_price`, `amount`, `currency_code`.  
**Side Effects:** Read-only.  
**Concrete Example:**
- Input: wildcard prices `19.00` default and `17.00` non-default, plus country price `18.00`.
- Success: `200 {"finalPrice":19.00,"additionalPrices":[17.00]}`
- Error Output: `422 {"error":"WILDCARD_PRICE_REQUIRED","message":"No eligible wildcard availability contains a price","statusCode":422}`

### BR-CAT-014: Special prices require a valid active window

**Source Reference:** `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/utils/ProductPriceUtils.java:651-704`  
**Discovery Method:** Hybrid (CAST-guided discount calculation target + direct source read)  
**CAST Reference:** `Shopizer-Backend / Price discount calculation / ProductPriceUtils.finalPrice`; numeric ID unavailable locally  
**Semantic Preservation:**

| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 9 | 9 | OK |
| Data-flow | 7 | 7 | OK |
| Constants | 2 | 2 | OK |
| State transitions | 1 | 1 | OK |
| Outcomes | 4 | 4 | OK |
| Data writes | 0 | 0 | OK |
| Integrations | 0 | 0 | OK |
| Error paths | 3 | 4 | OK |

**Preservation:** OK  
**Statement:** A special price is used only while its configured start/end window is active; an open-ended end date is not silently inferred unless the target policy explicitly allows it.  
**Intent:** Validation  
**Classification:** Core
**Weight:** High
**Logic:** If start and end dates exist, require `start < now < end`; when only an end date exists, apply the legacy active-until-end behavior. Calculate discount metadata from the selected special amount.  
**Data:** `product_price.special_amount`, `special_start_at`, `special_end_at`, `amount`.  
**Side Effects:** Read-only.  
**Concrete Example:**
- Input: base `100.00`, special `80.00`, start `2026-08-01`, end `2026-09-30`, request date `2026-09-01`.
- Success: `200 {"originalPrice":100.00,"finalPrice":80.00,"discounted":true}`
- Error Output: `200 {"originalPrice":100.00,"finalPrice":100.00,"discounted":false}` when requested on `2026-10-01`.

### BR-CAT-015: Positive selected attribute prices are additive

**Source Reference:** `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/utils/ProductPriceUtils.java:92-128`  
**Discovery Method:** Hybrid (CAST-guided option-price calculation target + direct source read)  
**CAST Reference:** `Shopizer-Backend / Selected option price calculation / ProductPriceUtils.getFinalPrice`; numeric ID unavailable locally  
**Semantic Preservation:**

| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 5 | 5 | OK |
| Data-flow | 6 | 6 | OK |
| Constants | 1 | 1 | OK |
| State transitions | 0 | 0 | OK |
| Outcomes | 3 | 3 | OK |
| Data writes | 0 | 0 | OK |
| Integrations | 0 | 0 | OK |
| Error paths | 2 | 3 | OK |

**Preservation:** OK  
**Statement:** Each selected option value with a positive price adjustment increases the final, original, and discounted price by that adjustment. Zero and negative adjustments do not increase the price.  
**Intent:** Calculation  
**Classification:** Core
**Weight:** High
**Logic:** Start with the selected base price; for each selected attribute whose adjustment is greater than zero, add it to the final/original/discounted amounts.  
**Data:** `product_attribute.product_option_id`, `product_option_value_id`, `price_adjustment`, selected option identifiers.  
**Side Effects:** Read-only calculation.  
**Concrete Example:**
- Input: base `50.00`, selected `engraving +5.00`, `gift-wrap +2.50`.
- Success: `200 {"originalPrice":57.50,"finalPrice":57.50,"selectedAdjustments":7.50}`
- Error Output: `422 {"error":"OPTION_SELECTION_INVALID","message":"Selected option value does not belong to this product","statusCode":422}`

### BR-CAT-016: Variant-specific pricing falls back to parent pricing

**Source Reference:** `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/catalog/pricing/PricingServiceImpl.java:109-119`; `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/catalog/inventory/ProductInventoryServiceImpl.java:41-59`  
**Discovery Method:** Hybrid (CAST-guided pricing/inventory target + direct source read)  
**CAST Reference:** `Shopizer-Backend / Variant inventory pricing transaction / PricingServiceImpl.calculateProductPrice(ProductVariant)`; numeric ID unavailable locally  
**Semantic Preservation:**

| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 4 | 4 | FLAGGED |
| Data-flow | 5 | 5 | OK |
| Constants | 0 | 0 | OK |
| State transitions | 0 | 0 | OK |
| Outcomes | 3 | 3 | FLAGGED |
| Data writes | 0 | 0 | OK |
| Integrations | 1 | 1 | OK |
| Error paths | 2 | 3 | FLAGGED |

**Preservation:** FLAGGED  
**Statement:** If a variant has no usable variant-specific price, the product's eligible parent price is returned; the response identifies that fallback so clients do not mistake it for a variant price.  
**Intent:** Routing  
**Classification:** Active
**Weight:** Medium
**Logic:** The legacy variant pricing method returns null; the inventory service then calculates the parent product price. The target preserves fallback but emits `priceSource=parentProduct`.  
**Data:** variant availability/price rows and parent product price rows.  
**Side Effects:** Read-only.  
**Concrete Example:**
- Input: variant `TSHIRT-001-BLUE-M` has availability but no price; parent has `29.00`.
- Success: `200 {"sku":"TSHIRT-001-BLUE-M","finalPrice":29.00,"priceSource":"parentProduct"}`
- Error Output: `422 {"error":"PRICE_UNAVAILABLE","message":"No price exists for the variant or parent product","statusCode":422}`

### BR-CAT-017: Product media is persisted independently from the product row

**Source Reference:** `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/catalog/product/ProductServiceImpl.java:259-325`  
**Discovery Method:** Hybrid (CAST-guided product/media transaction + direct source read)  
**CAST Reference:** `Shopizer-Backend / Product media persistence transaction / ProductServiceImpl.saveOrUpdate`; numeric ID unavailable locally  
**Semantic Preservation:**

| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 9 | 9 | OK |
| Data-flow | 9 | 9 | OK |
| Constants | 2 | 2 | OK |
| State transitions | 2 | 2 | OK |
| Outcomes | 4 | 4 | OK |
| Data writes | 4 | 4 | OK |
| Integrations | 3 | 3 | OK |
| Error paths | 3 | 4 | OK |

**Preservation:** OK  
**Statement:** Product metadata and media metadata are separate durable records; binary media is written through the provider boundary and linked to the product media record.  
**Intent:** Routing  
**Classification:** Core
**Weight:** High
**Logic:** Persist the product first, send new binary images to the image service/provider, save existing media metadata separately, and remove omitted media after reconciliation.  
**Data:** `product.id`, `product_image.product_id`, `file_name`, `original_uri`, provider object key.  
**Side Effects:** Product and media records change; object storage receives or deletes binary content; publish `MediaChanged.v1`.  
**Concrete Example:**
- Input: `POST /api/v1/catalog/products/{id}/media` multipart file `blue-shirt.jpg`.
- Success: `201 {"id":"66666666-6666-4666-8666-666666666666","fileName":"blue-shirt.jpg","status":"Ready"}`
- Error Output: `503 {"error":"MEDIA_PROVIDER_UNAVAILABLE","message":"Media content could not be stored","statusCode":503}`

### BR-CAT-018: Legacy image persistence failures are non-fatal after product persistence

**Source Reference:** `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/catalog/product/ProductServiceImpl.java:281-332`  
**Discovery Method:** Hybrid (CAST-guided product save transaction + direct source read)  
**CAST Reference:** `Shopizer-Backend / Product save and media exception path / ProductServiceImpl.saveOrUpdate`; numeric ID unavailable locally  
**Semantic Preservation:**

| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 6 | 6 | FLAGGED |
| Data-flow | 5 | 5 | FLAGGED |
| Constants | 1 | 1 | OK |
| State transitions | 2 | 2 | FLAGGED |
| Outcomes | 3 | 3 | FLAGGED |
| Data writes | 3 | 3 | FLAGGED |
| Integrations | 2 | 2 | OK |
| Error paths | 4 | 4 | FLAGGED |

**Preservation:** FLAGGED  
**Statement:** Product metadata may remain saved when media persistence fails, but the target records the media failure, marks the media operation incomplete, and exposes the partial-success condition instead of silently swallowing it.  
**Intent:** Routing  
**Classification:** Core
**Weight:** High
**Logic:** Legacy catches media exceptions, logs them, and returns the product. Target preserves product success but records an outbox/media failure and returns `mediaStatus=Pending` or `Failed`.  
**Data:** `product`, `product_image.media_status`, outbox/delivery-attempt record.  
**Side Effects:** Product write succeeds; media retry event is emitted; failure is observable.  
**Concrete Example:**
- Input: product update with valid SKU but object-storage timeout during image upload.
- Success: `202 {"productId":"11111111-1111-4111-8111-111111111111","mediaStatus":"Pending","retryable":true}`
- Error Output: `503` only when the product transaction itself fails; media failure alone must not erase the product.

### BR-CAT-019: Product deletion removes dependent catalog records first

**Source Reference:** `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/catalog/product/ProductServiceImpl.java:215-245`  
**Discovery Method:** Hybrid (CAST-guided product deletion transaction + direct source read)  
**CAST Reference:** `Shopizer-Backend / Product deletion transaction / ProductServiceImpl.delete`; numeric ID unavailable locally  
**Semantic Preservation:**

| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 8 | 8 | OK |
| Data-flow | 9 | 9 | OK |
| Constants | 0 | 0 | OK |
| State transitions | 2 | 2 | OK |
| Outcomes | 5 | 5 | OK |
| Data writes | 8 | 8 | OK |
| Integrations | 2 | 2 | OK |
| Error paths | 3 | 4 | OK |

**Preservation:** OK  
**Statement:** Deleting a product removes or detaches its media, reviews, relationships, categories, variants, availability, and prices before removing the product itself.  
**Intent:** State Transition  
**Classification:** Core
**Weight:** High
**Logic:** Validate product/store, reload attached entity, remove media through the provider, delete reviews and relationships, clear category associations, then delete the product aggregate.  
**Data:** product and dependent tables: `product_image`, `product_review`, `product_relationship`, `product_category`, `product_variant`, `product_availability`, `product_price`.  
**Side Effects:** Object storage deletion, dependent row deletion, `ProductChanged.v1` tombstone.  
**Concrete Example:**
- Input: `DELETE /api/v1/catalog/products/11111111-1111-4111-8111-111111111111`
- Success: `200 {"productId":"11111111-1111-4111-8111-111111111111","status":"Deleted","dependentsRemoved":7}`
- Error Output: `409 {"error":"PRODUCT_DELETE_BLOCKED","message":"Product has a protected downstream dependency","statusCode":409}`

### BR-CAT-025: Product listing page size must remain caller-controlled

**Source Reference:** `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/catalog/product/ProductServiceImpl.java:345-360`  
**Discovery Method:** Hybrid (CAST-guided listing transaction + direct source read)  
**CAST Reference:** `Shopizer-Backend / Product administration listing / ProductServiceImpl.listByStore`; numeric ID unavailable locally  
**Semantic Preservation:**

| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 4 | 4 | FLAGGED |
| Data-flow | 4 | 4 | FLAGGED |
| Constants | 1 | 1 | OK |
| State transitions | 0 | 0 | OK |
| Outcomes | 3 | 3 | FLAGGED |
| Data writes | 0 | 0 | OK |
| Integrations | 0 | 0 | OK |
| Error paths | 1 | 2 | GAP |

**Preservation:** FLAGGED  
**Statement:** A product listing uses the requested page size, bounded by the API maximum; a page-size value must not be overwritten by the page number.  
**Intent:** Calculation  
**Classification:** Core
**Weight:** High
**Logic:** Legacy assigns `page` to page size and immediately replaces it with `count`. Target accepts zero-based `page` and `pageSize`, validates bounds, and applies them once.  
**Data:** request `page/pageSize`, listing query offset/limit.  
**Side Effects:** Read-only.  
**Concrete Example:**
- Input: `GET /api/v1/catalog/products?page=2&pageSize=25`
- Success: response pagination is `{"page":2,"pageSize":25}`.
- Error Output: `422 {"error":"PAGE_SIZE_INVALID","message":"pageSize must be between 1 and 200","statusCode":422}`

### BR-CAT-026: Listing count and fetch predicates must be identical

**Source Reference:** `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/repositories/catalog/product/ProductRepositoryImpl.java:666-673,842-844`  
**Discovery Method:** Hybrid (CAST-guided product listing query target + direct source read)  
**CAST Reference:** `Shopizer-Backend / Product count and fetch transactions / ProductRepositoryImpl.listByStore`; numeric ID unavailable locally  
**Semantic Preservation:**

| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 8 | 9 | GAP |
| Data-flow | 10 | 10 | OK |
| Constants | 1 | 1 | OK |
| State transitions | 0 | 0 | OK |
| Outcomes | 3 | 4 | GAP |
| Data writes | 0 | 0 | OK |
| Integrations | 0 | 0 | OK |
| Error paths | 3 | 4 | GAP |

**Preservation:** GAP  
**Statement:** The total count and returned product page must apply the same store, category, region, availability, date, language, and grouping predicates so pagination totals describe the actual result set.  
**Intent:** Calculation  
**Classification:** Active
**Weight:** Medium
**Logic:** Legacy count and fetch queries differ and the fetch query contains an unresolved merchant parameter expression. Target builds one predicate specification and reuses it for count and fetch, with distinct product IDs before pagination.  
**Data:** product, availability, category, description, store, query criteria.  
**Side Effects:** Read-only; mismatch telemetry is emitted if detected.  
**Concrete Example:**
- Input: `GET /api/v1/catalog/products?categoryId=55555555-5555-4555-8555-555555555555&available=true&page=0&pageSize=20`
- Success: `totalItems` equals the number of distinct eligible products, not joined availability rows.
- Error Output: `500 {"error":"LISTING_QUERY_INVALID","message":"Catalog listing predicates could not be compiled consistently","statusCode":500}`

### BR-CAT-027: Merchant predicates must be explicitly grouped

**Source Reference:** `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/repositories/catalog/product/price/ProductPriceRepository.java:32-48`  
**Discovery Method:** Hybrid (CAST-guided price repository target + direct source read)  
**CAST Reference:** `Shopizer-Backend / Product price lookup / ProductPriceRepository`; numeric ID unavailable locally  
**Semantic Preservation:**

| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 3 | 4 | GAP |
| Data-flow | 7 | 7 | OK |
| Constants | 1 | 1 | OK |
| State transitions | 0 | 0 | OK |
| Outcomes | 2 | 3 | GAP |
| Data writes | 0 | 0 | OK |
| Integrations | 0 | 0 | OK |
| Error paths | 1 | 3 | GAP |

**Preservation:** GAP  
**Statement:** A price lookup matches a product or variant SKU only within the requested store; store scope applies to both sides of the product/variant alternative.  
**Intent:** Validation  
**Classification:** Core
**Weight:** High
**Logic:** Replace the legacy `productSku = ? OR variantSku = ? AND merchant = ?` precedence with `(productSku = ? OR variantSku = ?) AND storeId = ?`.  
**Data:** product/variant SKU, `product_price.availability_id`, `product_availability.store_id`.  
**Side Effects:** Read-only; cross-store leakage is prevented and audited.  
**Concrete Example:**
- Input: lookup SKU `SHOE-RED-42` for `store-eu`.
- Success: returns the EU price only.
- Error Output: `404 {"error":"PRICE_NOT_FOUND","message":"No price for SKU 'SHOE-RED-42' exists in the requested store","statusCode":404}`

### BR-CAT-028: Catalog mutations require privileged groups

**Source Reference:** `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/api/v1/category/CategoryApi.java:147-224`; `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/api/v2/product/ProductVariantApi.java:73-113`  
**Discovery Method:** Hybrid (CAST-guided administration API target + direct source read)  
**CAST Reference:** `Shopizer-Backend / Catalog administration mutation transactions / CategoryApi and ProductVariantApi`; numeric IDs unavailable locally  
**Semantic Preservation:**

| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 8 | 8 | OK |
| Data-flow | 5 | 5 | OK |
| Constants | 4 | 4 | OK |
| State transitions | 2 | 2 | OK |
| Outcomes | 3 | 3 | OK |
| Data writes | 2 | 2 | OK |
| Integrations | 0 | 1 | OK |
| Error paths | 3 | 4 | OK |

**Preservation:** OK  
**Statement:** Product, category, variant, availability, and media mutations require an authenticated principal with catalog-management permission in the target store.  
**Intent:** Authorization  
**Classification:** Core
**Weight:** High
**Logic:** Require an authenticated user and membership in one of the approved administrative groups; additionally verify store scope through MS-10.  
**Data:** identity claims, permission groups, `store_id`, resource owner.  
**Side Effects:** Unauthorized attempts are audited; no mutation occurs.  
**Concrete Example:**
- Input: `PUT /api/v1/catalog/products/{id}` with bearer principal having `catalog-admin`.
- Success: `200` updated product.
- Error Output: `403 {"error":"CATALOG_PERMISSION_REQUIRED","message":"Catalog management permission is required","statusCode":403}`

### BR-CAT-029: Selected option/value pairs determine variation pricing

**Source Reference:** `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/api/v2/product/ProductVariationApi.java:103-156`  
**Discovery Method:** Hybrid (CAST-guided variation-price API target + direct source read)  
**CAST Reference:** `Shopizer-Backend / Product variation price transaction / ProductVariationApi.calculateVariant`; numeric ID unavailable locally  
**Semantic Preservation:**

| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 9 | 9 | OK |
| Data-flow | 9 | 9 | OK |
| Constants | 1 | 1 | OK |
| State transitions | 0 | 0 | OK |
| Outcomes | 4 | 4 | OK |
| Data writes | 0 | 0 | OK |
| Integrations | 0 | 0 | OK |
| Error paths | 3 | 4 | OK |

**Preservation:** OK  
**Statement:** A variation-price request is calculated from the option/value pairs selected by the shopper, and only pairs belonging to the requested product contribute to the price.  
**Intent:** Calculation  
**Classification:** Active
**Weight:** Medium
**Logic:** Load the product, match each requested option/value pair against product attributes, collect matching attributes, and calculate the resulting price.  
**Data:** product attributes, option IDs, option-value IDs, adjustment amounts.  
**Side Effects:** Read-only.  
**Concrete Example:**
- Input: `POST /api/v1/catalog/products/{id}/options/price` with `{"selections":[{"optionId":"77777777-7777-4777-8777-777777777777","valueId":"88888888-8888-4888-8888-888888888888"}]}`
- Success: `200 {"finalPrice":57.50,"matchedSelections":1}`
- Error Output: `422 {"error":"OPTION_VALUE_NOT_ALLOWED","message":"The selected option value is not available for this product","statusCode":422}`

### BR-CAT-030: Missing wildcard availability must not cause a null dereference

**Source Reference:** `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/mapper/catalog/product/PersistableProductMapper.java:248-266`  
**Discovery Method:** Hybrid (CAST-guided product mapping target + direct source read)  
**CAST Reference:** `Shopizer-Backend / Product variant inventory derivation / PersistableProductMapper`; numeric ID unavailable locally  
**Semantic Preservation:**

| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 5 | 6 | GAP |
| Data-flow | 5 | 6 | OK |
| Constants | 1 | 1 | OK |
| State transitions | 1 | 1 | OK |
| Outcomes | 3 | 4 | GAP |
| Data writes | 2 | 2 | OK |
| Integrations | 0 | 0 | OK |
| Error paths | 2 | 4 | GAP |

**Preservation:** GAP  
**Statement:** When product inventory is derived from a variant, the target must require or explicitly select a valid availability; absence of a wildcard row must produce a validation error, never an unhandled null failure.  
**Intent:** Validation  
**Classification:** Active
**Weight:** Medium
**Logic:** Legacy calls `.get()` on the first wildcard match. Target searches variant and parent availability deterministically and returns `AVAILABILITY_REQUIRED` when none qualifies.  
**Data:** variant availability region, parent availability region, quantity.  
**Side Effects:** No product write when inventory cannot be derived.  
**Concrete Example:**
- Input: product has variants but every variant has only `DE` availability and no parent inventory.
- Success: product saves when a valid `*` availability is supplied.
- Error Output: `422 {"error":"DEFAULT_AVAILABILITY_REQUIRED","message":"A wildcard availability is required when deriving product inventory from variants","statusCode":422}`

### BR-CAT-031: Read output separates properties from selectable options

**Source Reference:** `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/populator/catalog/ReadableProductPopulator.java:268-470`  
**Discovery Method:** Hybrid (CAST-guided product representation target + direct source read)  
**CAST Reference:** `Shopizer-Backend / Readable product population transaction / ReadableProductPopulator`; numeric ID unavailable locally  
**Semantic Preservation:**

| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 13 | 13 | OK |
| Data-flow | 14 | 14 | OK |
| Constants | 2 | 2 | OK |
| State transitions | 1 | 1 | OK |
| Outcomes | 5 | 5 | OK |
| Data writes | 0 | 0 | OK |
| Integrations | 2 | 2 | OK |
| Error paths | 4 | 5 | OK |

**Preservation:** OK  
**Statement:** Display-only product attributes are returned as properties, while shopper-selectable attributes are returned as options with selectable values, prices, images, and localized descriptions.  
**Intent:** Routing  
**Classification:** Active
**Weight:** Medium
**Logic:** Partition attributes by display-only flag; create property output for read-only attributes and grouped option output for selectable attributes; apply requested-language descriptions with deterministic fallback.  
**Data:** product attributes, option/value codes, display-only flag, price adjustment, image reference, localized descriptions.  
**Side Effects:** Read-only.  
**Concrete Example:**
- Input: product has `material=Cotton` display-only and `color=Blue +2.00` selectable.
- Success: `200 {"properties":[{"code":"material","value":"Cotton"}],"options":[{"code":"color","values":[{"code":"blue","price":2.00}]}]}`
- Error Output: `422 {"error":"OPTION_DESCRIPTION_MISSING","message":"Selectable option value has no usable localized description","statusCode":422}`

### BR-ORD-012: Inventory acceptance uses an MS-02 atomic reservation contract

**Source Reference:** `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/controller/order/facade/OrderFacadeImpl.java:383-416`; `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/order/OrderServiceImpl.java:191-210`  
**Discovery Method:** Hybrid (CAST-guided order inventory call path + direct source read)  
**CAST Reference:** `Shopizer-Backend / Order submission inventory validation and decrement path`; numeric CAST IDs unavailable locally  
**Semantic Preservation:**

| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 10 | 12 | GAP |
| Data-flow | 10 | 12 | GAP |
| Constants | 2 | 2 | OK |
| State transitions | 3 | 5 | GAP |
| Outcomes | 4 | 5 | GAP |
| Data writes | 5 | 5 | GAP |
| Integrations | 0 | 2 | GAP |
| Error paths | 4 | 6 | GAP |

**Preservation:** GAP  
**Statement:** MS-02 is the sole owner of inventory availability. A reservation atomically verifies available quantity, decreases sellable quantity, is idempotent by caller key, and can later be committed or released; cart and order services never mutate inventory directly.  
**Intent:** State Transition  
**Classification:** Core
**Weight:** Critical
**Logic:** The legacy facade checks quantity, while the order service decrements rows and logs insufficient stock instead of consistently rejecting. The target replaces both paths with `POST /inventory-reservations`, using a conditional update/transaction and idempotency key.  
**Data:** `product_availability.quantity`, `reserved_quantity`, `inventory_reservation.idempotency_key`, product/variant identity.  
**Side Effects:** Atomic availability update; `InventoryReservationChanged.v1` event.  
**Concrete Example:**
- Input: `POST /api/v1/catalog/products/11111111-1111-4111-8111-111111111111/reservations` with `{"reservationKey":"checkout-abc-1","quantity":3,"regionCode":"DE","expiresAt":"2026-09-01T14:00:00Z"}`
- Success: `201 {"reservationId":"99999999-9999-4999-8999-999999999999","state":"Held","quantity":3,"remainingQuantity":7}`
- Error Output: `409 {"error":"INSUFFICIENT_AVAILABILITY","message":"Only 2 units are available for reservation","statusCode":409}`

### BR-EXT-019: Product image binary content is provider-separated from metadata

**Source Reference:** `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/catalog/product/image/ProductImageServiceImpl.java:79-107`; `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/catalog/product/ProductServiceImpl.java:283-299`  
**Discovery Method:** Hybrid (CAST-guided product-media provider path + direct source read)  
**CAST Reference:** `Shopizer-Backend / Product image provider transaction / ProductImageServiceImpl`; numeric ID unavailable locally  
**Semantic Preservation:**

| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 6 | 6 | OK |
| Data-flow | 7 | 7 | OK |
| Constants | 2 | 2 | OK |
| State transitions | 2 | 2 | OK |
| Outcomes | 3 | 3 | OK |
| Data writes | 3 | 3 | OK |
| Integrations | 3 | 3 | OK |
| Error paths | 3 | 4 | OK |

**Preservation:** OK  
**Statement:** Product image bytes are stored through a provider-neutral media boundary, while MS-02 persists only product-media metadata and provider references.  
**Intent:** Routing  
**Classification:** Core
**Weight:** High
**Logic:** Validate image content, call the configured product file manager, then save the media metadata and publish an image event.  
**Data:** product image name/type, provider URI/key, product ID, image status.  
**Side Effects:** Object storage write and metadata write.  
**Concrete Example:**
- Input: `POST /api/v1/catalog/products/{id}/media` with `imageType=Binary` and `file=shoe.jpg`.
- Success: `201 {"status":"Ready","providerKey":"catalog/store-eu/products/..."}`
- Error Output: `503 {"error":"MEDIA_WRITE_FAILED","message":"The configured media provider rejected the image","statusCode":503}`

### BR-EXT-020: Image processing creates configured representations

**Source Reference:** `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/modules/cms/product/ProductFileManagerImpl.java:65-102,128-215,252-262`; `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/utils/ProductImageSizeUtils.java:23-147`  
**Discovery Method:** Hybrid (CAST-guided image-processing target + direct source read)  
**CAST Reference:** `Shopizer-Backend / Product image transformation transaction / ProductFileManagerImpl`; numeric ID unavailable locally  
**Semantic Preservation:**

| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 14 | 14 | OK |
| Data-flow | 12 | 12 | OK |
| Constants | 6 | 6 | OK |
| State transitions | 2 | 2 | OK |
| Outcomes | 5 | 5 | OK |
| Data writes | 4 | 4 | OK |
| Integrations | 3 | 3 | OK |
| Error paths | 5 | 6 | OK |

**Preservation:** OK  
**Statement:** When configured image dimensions are valid, the uploaded product image is validated and transformed into the configured catalog representation while preserving aspect ratio; invalid configuration or unreadable content fails the media operation.  
**Intent:** Calculation  
**Classification:** Active
**Weight:** Medium
**Logic:** Read bytes, decode an image, resolve configured width/height, reject non-positive dimensions, optionally crop, resize when source dimensions exceed the target, write the transformed representation, and upload it.  
**Data:** image bytes, file name/content type, configured width/height, product/image ID.  
**Side Effects:** Original and transformed provider objects are written; temporary files are deleted.  
**Concrete Example:**
- Input: `POST /media` with a `2400x1600` JPEG and configured maximum `1200x1200`.
- Success: `201 {"originalUri":"...","largeUri":"...","width":1200,"height":800}`
- Error Output: `422 {"error":"IMAGE_INVALID","message":"Uploaded content is not a readable image","statusCode":422}`

### BR-UI-003: Administration validates SKU syntax and uniqueness

**Source Reference:** `initial-source/shopizer-admin-main/src/app/pages/catalogue/products/product-form/product-form.component.ts:172-183,339-345`; `initial-source/shopizer-admin-main/src/app/pages/catalogue/products/services/product.service.ts:72-76`  
**Discovery Method:** Hybrid (CAST-guided Shopizer-WebAdmin dependency path + direct source read)  
**CAST Reference:** `Shopizer-WebAdmin → Shopizer-Backend / product form SKU validation`; numeric CAST IDs unavailable locally  
**Semantic Preservation:**

| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 5 | 5 | OK |
| Data-flow | 5 | 5 | OK |
| Constants | 2 | 2 | OK |
| State transitions | 1 | 1 | OK |
| Outcomes | 3 | 3 | OK |
| Data writes | 0 | 0 | OK |
| Integrations | 1 | 1 | OK |
| Error paths | 2 | 3 | OK |

**Preservation:** OK  
**Statement:** Catalog administrators must provide a non-empty alphanumeric SKU, and the form must reject it when another product in the same store already uses it.  
**Intent:** Validation  
**Classification:** Core
**Weight:** Critical
**Logic:** Apply required and alphanumeric client validation; call the uniqueness endpoint on change; server validation remains authoritative.  
**Data:** request SKU, store scope, product identity.  
**Side Effects:** No save on invalid or duplicate SKU.  
**Concrete Example:**
- Input: form SKU `CAMERA-01`.
- Success: form enables save when the server reports `exists=false`.
- Error Output: `422 {"error":"SKU_FORMAT_INVALID","message":"SKU must contain only permitted alphanumeric characters","statusCode":422}`

### BR-UI-004: Storefront purchase controls require all availability flags

**Source Reference:** `initial-source/shopizer-admin-main/src/app/pages/catalogue/products/product-form/product-form.component.ts:172-183`; `initial-source/shopizer-shop-reactjs-main/src/components/product/ProductDescriptionInfo.js:274-305`  
**Discovery Method:** Hybrid (CAST-guided frontend dependency path + direct source read)  
**CAST Reference:** `Shopizer-WebFrontEnd → Shopizer-Backend / Product detail and add-to-cart path`; numeric CAST IDs unavailable locally  
**Semantic Preservation:**

| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 6 | 6 | OK |
| Data-flow | 6 | 6 | OK |
| Constants | 2 | 2 | OK |
| State transitions | 2 | 2 | OK |
| Outcomes | 3 | 3 | OK |
| Data writes | 0 | 0 | OK |
| Integrations | 1 | 1 | OK |
| Error paths | 2 | 3 | OK |

**Preservation:** OK  
**Statement:** A product is purchasable only when it is available, visible, explicitly purchasable, and has quantity greater than zero.  
**Intent:** Validation  
**Classification:** Core
**Weight:** High
**Logic:** The storefront add-to-cart control is enabled only when `available && canBePurchased && visible && quantity > 0`; the target API repeats the check server-side.  
**Data:** product availability state, visibility, purchase flag, available quantity.  
**Side Effects:** Add-to-cart is rejected when any condition fails.  
**Concrete Example:**
- Input: product has `available=true`, `visible=true`, `canBePurchased=true`, `quantity=4`.
- Success: `POST /api/v1/cart/lines` may use the product reservation flow.
- Error Output: `409 {"error":"PRODUCT_NOT_PURCHASABLE","message":"Product is not currently purchasable","statusCode":409}` when `quantity=0`.

### BR-UI-005: Localized descriptions use first non-empty fallback

**Source Reference:** `initial-source/shopizer-admin-main/src/app/pages/catalogue/products/product-form/product-form.component.ts:204-219,395-451`  
**Discovery Method:** Hybrid (CAST-guided Shopizer-WebAdmin dependency path + direct source read)  
**CAST Reference:** `Shopizer-WebAdmin → Shopizer-Backend / Product localization form path`; numeric CAST IDs unavailable locally  
**Semantic Preservation:**

| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 8 | 8 | OK |
| Data-flow | 8 | 8 | OK |
| Constants | 1 | 1 | OK |
| State transitions | 1 | 1 | OK |
| Outcomes | 4 | 4 | OK |
| Data writes | 2 | 2 | OK |
| Integrations | 0 | 0 | OK |
| Error paths | 2 | 3 | OK |

**Preservation:** OK  
**Statement:** Product descriptions are collected per supported language; when a localized field is empty, the first non-empty value for that field is copied as a fallback, while name and friendly URL remain required.  
**Intent:** Calculation  
**Classification:** Active
**Weight:** Medium
**Logic:** Iterate descriptions in form order, retain the first non-empty value for each fallback field, require name/friendly URL/SKU/manufacturer, then fill empty localized values before submission.  
**Data:** description language, name, friendly URL, title, keywords, metadata.  
**Side Effects:** Product descriptions are written for each supported language.  
**Concrete Example:**
- Input: English name `Shoes`, French name empty, English slug `running-shoes`, French slug empty.
- Success: French description receives fallback name/slug and product saves.
- Error Output: `422 {"error":"DESCRIPTION_REQUIRED","message":"At least one description requires a name and friendly URL","statusCode":422}`

### BR-UI-006: Category code, URL, parent, and hierarchy are managed together

**Source Reference:** `initial-source/shopizer-admin-main/src/app/pages/catalogue/categories/category-form/category-form.component.ts:150-179,247-380`; `initial-source/shopizer-admin-main/src/app/pages/catalogue/categories/services/category.service.ts:20-67`  
**Discovery Method:** Hybrid (CAST-guided Shopizer-WebAdmin dependency path + direct source read)  
**CAST Reference:** `Shopizer-WebAdmin → Shopizer-Backend / Category administration path`; numeric CAST IDs unavailable locally  
**Semantic Preservation:**

| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 11 | 11 | OK |
| Data-flow | 10 | 10 | OK |
| Constants | 2 | 2 | OK |
| State transitions | 3 | 3 | OK |
| Outcomes | 5 | 5 | OK |
| Data writes | 3 | 3 | OK |
| Integrations | 1 | 1 | OK |
| Error paths | 3 | 4 | OK |

**Preservation:** OK  
**Statement:** Category administration validates the category code, localized name, friendly URL, sort order, and selected parent as one hierarchy mutation; the selected store scope cannot be changed by a non-superadministrator.  
**Intent:** Validation  
**Classification:** Active
**Weight:** Medium
**Logic:** Validate form fields, resolve the selected parent to an ID/code pair, check code uniqueness, fill localized fallbacks, and submit a create/update operation.  
**Data:** category code, parent ID, store scope, sort order, localized descriptions.  
**Side Effects:** Category or subtree changes; `CategoryChanged.v1` event.  
**Concrete Example:**
- Input: `POST /api/v1/catalog/categories` with `{"code":"running-shoes","parentId":"33333333-3333-4333-8333-333333333333","sortOrder":10,"descriptions":[{"languageCode":"en","name":"Running Shoes","friendlyUrl":"running-shoes"}]}`
- Success: `201` category with materialized depth/lineage.
- Error Output: `422 {"error":"CATEGORY_FORM_INVALID","message":"Category code, parent, name, and friendly URL must be valid","statusCode":422}`

### BR-CAT-032: Product visibility maps to availability, but explicit availability dates must be preserved

**Source Reference:** `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/mapper/catalog/product/PersistableProductMapper.java:100-106,145-151`  
**Discovery Method:** Hybrid (CAST-guided request-mapping target + direct source read)  
**CAST Reference:** `Shopizer-Backend / Persistable product mapping / PersistableProductMapper.merge`; numeric ID unavailable locally  
**Semantic Preservation:**

| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 5 | 6 | GAP |
| Data-flow | 5 | 6 | GAP |
| Constants | 2 | 2 | FLAGGED |
| State transitions | 2 | 2 | OK |
| Outcomes | 3 | 4 | GAP |
| Data writes | 2 | 2 | OK |
| Integrations | 0 | 0 | OK |
| Error paths | 2 | 3 | GAP |

**Preservation:** GAP  
**Statement:** A product's visibility controls whether it can appear in storefront reads, but updating visibility must not silently replace a caller-supplied effective date with the current timestamp.  
**Intent:** State Transition  
**Classification:** Core
**Weight:** High
**Logic:** Legacy sets availability from visibility and initially resets the date to `new Date()`, then parses a supplied date if present. Target preserves a supplied date and uses current time only when no date is provided.  
**Data:** product visibility, `date_available`, request date.  
**Side Effects:** Product status/date update and `ProductChanged.v1`.  
**Concrete Example:**
- Input: update `{"visible":true,"dateAvailable":"2026-10-15"}` on `2026-09-01`.
- Success: product becomes visible on `2026-10-15`, not immediately.
- Error Output: `422 {"error":"DATE_INVALID","message":"dateAvailable must be a valid ISO date","statusCode":422}`

### BR-CAT-033: Locale selection must be deterministic after a product read

**Source Reference:** `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/catalog/product/ProductServiceImpl.java:145-160`; `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/utils/CatalogServiceHelper.java:1-73`  
**Discovery Method:** Hybrid (CAST-guided localized product-read target + direct source read)  
**CAST Reference:** `Shopizer-Backend / Localized product detail transaction / ProductServiceImpl.getProductForLocale`; numeric ID unavailable locally  
**Semantic Preservation:**

| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 5 | 5 | OK |
| Data-flow | 6 | 6 | OK |
| Constants | 1 | 1 | OK |
| State transitions | 1 | 1 | OK |
| Outcomes | 3 | 3 | OK |
| Data writes | 0 | 0 | OK |
| Integrations | 0 | 0 | OK |
| Error paths | 2 | 3 | OK |

**Preservation:** OK  
**Statement:** A localized product read selects the requested region and language before building the response, so price, availability, descriptions, options, and media represent one deterministic storefront context.  
**Intent:** Routing  
**Classification:** Core
**Weight:** High
**Logic:** Load an eligible product, set the selected availability for the locale, then select the requested language before population.  
**Data:** product availability region, language ID/code, localized descriptions/prices.  
**Side Effects:** Read-only.  
**Concrete Example:**
- Input: `GET /api/v1/catalog/products/11111111-1111-4111-8111-111111111111?languageCode=fr&countryCode=FR`
- Success: response contains French descriptions and FR-or-wildcard availability.
- Error Output: `404 {"error":"LOCALIZED_PRODUCT_NOT_FOUND","message":"No product is eligible for language 'fr' and country 'FR'","statusCode":404}`

### BR-CAT-034: Category moves require cross-store and cycle validation

**Source Reference:** `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/catalog/category/CategoryServiceImpl.java:299-325`  
**Discovery Method:** Hybrid (CAST-guided category move target + direct source read)  
**CAST Reference:** `Shopizer-Backend / Category move hierarchy transaction / CategoryServiceImpl.addChild`; numeric ID unavailable locally  
**Semantic Preservation:**

| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 5 | 7 | GAP |
| Data-flow | 6 | 7 | GAP |
| Constants | 1 | 1 | OK |
| State transitions | 2 | 3 | GAP |
| Outcomes | 3 | 4 | GAP |
| Data writes | 2 | 2 | OK |
| Integrations | 0 | 1 | GAP |
| Error paths | 2 | 4 | GAP |

**Preservation:** GAP  
**Statement:** A category may move only to a parent in the same store and outside its own descendant subtree.  
**Intent:** Validation  
**Classification:** Core
**Weight:** High
**Logic:** Legacy resolves the parent and rewrites lineage but does not explicitly guard cycles or store mismatch. Target checks store equality and traverses ancestors before mutation.  
**Data:** category IDs, parent IDs, store IDs, lineage.  
**Side Effects:** No write on invalid move; rejected attempts are audited.  
**Concrete Example:**
- Input: move category in `store-eu` beneath parent in `store-us`.
- Success: move under a valid `store-eu` parent.
- Error Output: `422 {"error":"PARENT_STORE_MISMATCH","message":"Parent category belongs to another store","statusCode":422}`

### BR-CAT-035: Media deletion must invalidate the derived product projection

**Source Reference:** `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/configuration/events/products/listeners/IndexProductEventListener.java:232-259`  
**Discovery Method:** Hybrid (CAST-guided product event listener target + direct source read)  
**CAST Reference:** `Shopizer-Backend / Product image deletion event path / IndexProductEventListener.deleteProductImage`; numeric ID unavailable locally  
**Semantic Preservation:**

| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 3 | 4 | GAP |
| Data-flow | 4 | 5 | GAP |
| Constants | 1 | 1 | OK |
| State transitions | 1 | 2 | GAP |
| Outcomes | 2 | 3 | GAP |
| Data writes | 0 | 1 | GAP |
| Integrations | 1 | 2 | GAP |
| Error paths | 1 | 3 | GAP |

**Preservation:** GAP  
**Statement:** Removing product media must publish a product projection change so consumers can remove stale media references; search projection ownership remains with MS-03.  
**Intent:** Routing  
**Classification:** Core
**Weight:** High
**Logic:** Legacy returns without reindexing after image deletion. Target emits `MediaChanged.v1`/`ProductChanged.v1`; MS-03 consumes it and refreshes its derived document.  
**Data:** product ID, media ID, event version.  
**Side Effects:** Event publication; no direct search write by MS-02.  
**Concrete Example:**
- Input: `DELETE /api/v1/catalog/products/{id}/media/{mediaId}`
- Success: `200 {"mediaId":"66666666-6666-4666-8666-666666666666","status":"Deleted","projectionEventPublished":true}`
- Error Output: `503 {"error":"EVENT_PUBLISH_FAILED","message":"Media deletion committed but projection event could not be published","statusCode":503}`

### BR-CAT-036: Product events must refresh the complete aggregate before publishing

**Source Reference:** `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/configuration/events/products/listeners/IndexProductEventListener.java:98-130,148-225,261-309`  
**Discovery Method:** Hybrid (CAST-guided product event transaction + direct source read)  
**CAST Reference:** `Shopizer-Backend / Product, variant, image, and attribute event paths / IndexProductEventListener`; numeric IDs unavailable locally  
**Semantic Preservation:**

| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 12 | 12 | OK |
| Data-flow | 13 | 13 | OK |
| Constants | 2 | 2 | OK |
| State transitions | 3 | 3 | OK |
| Outcomes | 5 | 5 | OK |
| Data writes | 1 | 1 | OK |
| Integrations | 3 | 3 | OK |
| Error paths | 4 | 5 | OK |

**Preservation:** OK  
**Statement:** A product, variant, attribute, or media change publishes a complete, current product aggregate to downstream projection consumers rather than a partial stale object.  
**Intent:** Routing  
**Classification:** Core
**Weight:** High
**Logic:** Reload the product by ID and store before processing the event; apply the changed child to the refreshed aggregate; publish a versioned change event.  
**Data:** product ID/store, variants, attributes, images, aggregate version.  
**Side Effects:** `ProductChanged.v1` event; MS-03 updates its projection.  
**Concrete Example:**
- Input: save variant `TSHIRT-001-BLUE-M`.
- Success: event contains the current product plus all variants and media.
- Error Output: `409 {"error":"AGGREGATE_VERSION_CONFLICT","message":"Product changed while the event was being assembled","statusCode":409}`

### BR-CAT-037: Inventory reservation keys are idempotent

**Source Reference:** `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/order/OrderServiceImpl.java:191-210`; target requirement from `assessment/microservice-gap-analysis.md:80-81`  
**Discovery Method:** Hybrid (CAST-guided order decrement path + direct source read + approved target boundary decision)  
**CAST Reference:** `Shopizer-Backend / Order decrement call path / OrderServiceImpl`; numeric ID unavailable locally  
**Semantic Preservation:**

| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 5 | 7 | GAP |
| Data-flow | 6 | 8 | GAP |
| Constants | 1 | 1 | OK |
| State transitions | 2 | 4 | GAP |
| Outcomes | 3 | 5 | GAP |
| Data writes | 3 | 4 | GAP |
| Integrations | 0 | 2 | GAP |
| Error paths | 2 | 4 | GAP |

**Preservation:** GAP  
**Statement:** Repeating an inventory reservation request with the same caller-provided key returns the original reservation and must not reserve or decrement stock twice.  
**Intent:** Validation  
**Classification:** Core
**Weight:** Critical
**Logic:** Store `(store_id, reservation_key)` uniquely; on retry, return the existing reservation if request attributes match, otherwise return an idempotency conflict.  
**Data:** reservation key, product/variant ID, quantity, state, request hash.  
**Side Effects:** At most one availability decrement and one reservation event.  
**Concrete Example:**
- Input: submit key `checkout-abc-1` twice for quantity `3`.
- Success: both responses return the same reservation ID and remaining quantity.
- Error Output: `409 {"error":"IDEMPOTENCY_KEY_REUSED","message":"Reservation key was previously used with different contents","statusCode":409}`

### BR-CAT-038: Product deletion must not leave catalog references

**Source Reference:** `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/catalog/product/ProductServiceImpl.java:220-242`  
**Discovery Method:** Hybrid (CAST-guided product deletion target + direct source read)  
**CAST Reference:** `Shopizer-Backend / Product dependent-cleanup transaction / ProductServiceImpl.delete`; numeric ID unavailable locally  
**Semantic Preservation:**

| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 7 | 7 | OK |
| Data-flow | 8 | 8 | OK |
| Constants | 0 | 0 | OK |
| State transitions | 2 | 2 | OK |
| Outcomes | 4 | 4 | OK |
| Data writes | 7 | 7 | OK |
| Integrations | 2 | 2 | OK |
| Error paths | 3 | 4 | OK |

**Preservation:** OK  
**Statement:** Product deletion is complete only when no category, relationship, review, media, variant, availability, price, or reservation record can still reference the deleted product.  
**Intent:** Compliance  
**Classification:** Core
**Weight:** Critical
**Logic:** Execute aggregate cleanup in one transaction with foreign-key constraints or explicit ordered deletion; reject/compensate if any dependent cleanup fails.  
**Data:** all product-dependent tables and active reservations.  
**Side Effects:** dependent records and provider objects are removed; tombstone event is emitted.  
**Concrete Example:**
- Input: delete product with two variants, three media records, and one held reservation.
- Success: `200 {"status":"Deleted","activeReservationState":"Released","orphanReferences":0}`
- Error Output: `409 {"error":"ACTIVE_RESERVATION_EXISTS","message":"Product cannot be deleted while an active reservation exists","statusCode":409}`

### BR-CAT-039: Reservation commit and release are mutually exclusive terminal outcomes

**Source Reference:** `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/order/OrderServiceImpl.java:191-210`; target boundary decision in `assessment/microservice-gap-analysis.md:80-81,102-103`  
**Discovery Method:** Hybrid (CAST-guided inventory call path + direct source read + approved target contract)  
**CAST Reference:** `Shopizer-Backend / Order inventory decrement path`; numeric ID unavailable locally  
**Semantic Preservation:**

| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 2 | 5 | GAP |
| Data-flow | 4 | 6 | GAP |
| Constants | 0 | 1 | GAP |
| State transitions | 1 | 4 | GAP |
| Outcomes | 2 | 4 | GAP |
| Data writes | 2 | 3 | GAP |
| Integrations | 0 | 1 | GAP |
| Error paths | 1 | 3 | GAP |

**Preservation:** GAP  
**Statement:** A held reservation may be committed once or released once; after either terminal outcome, subsequent commit/release requests are idempotent and cannot change stock again.  
**Intent:** State Transition  
**Classification:** Core
**Weight:** Critical
**Logic:** Reservation states are `Held`, `Committed`, `Released`, and `Expired`. Commit changes `Held→Committed`; release changes `Held→Released`; terminal states reject the opposite transition without another stock mutation.  
**Data:** reservation state, quantity, expiry, product availability, commit/release timestamps.  
**Side Effects:** Commit finalizes decrement; release restores sellable quantity; events are published.  
**Concrete Example:**
- Input: `POST /api/v1/catalog/reservations/99999999-9999-4999-8999-999999999999/commit`
- Success: `200 {"state":"Committed","quantity":3}`
- Error Output: `409 {"error":"RESERVATION_TERMINAL","message":"Reservation is already Released and cannot be committed","statusCode":409}`
