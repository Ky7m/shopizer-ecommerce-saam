# CAST Scout Brief: Catalog and Product (MS-02)

**Analysis mode:** Hybrid (CAST structure + direct source read)  
**CAST application:** `Shopizer-Backend`  
**Local root mapping:** `§{main_sources}§` → `initial-source/shopizer-3.2.7/`

## Entry Points

CAST identified the following catalog, product, category, variant, inventory, media, and price
transactions. The price-read transactions are the largest graph hotspots and must not be reduced
to CRUD extraction.

| Transaction | CAST ID | Reduced / full objects | Complexity signal |
|---|---:|---:|---|
| Product storefront read | 244082 | 42 / 1310 | Localized and regional eligibility |
| Product friendly-URL read | 243984 | 42 / 1310 | Storefront predicate chain |
| Product create | 244111 | 71 / 802 | Aggregate persistence and media |
| Product update | 244113 | 71 / 809 | Aggregate merge and dependent writes |
| Product delete | 244115–244116 | 40 / 243 | Cascade and relationship cleanup |
| Product SKU uniqueness | 243985 | 23 / 44 | Store-scoped identity |
| Product/category attach | 243986 | 38 / 1083 | Association and scope validation |
| Product/category detach | 244117 | 38 / 1079 | Association cleanup |
| Category listing/read | 244026, 244029 | 14–24 / 137–228 | Localized hierarchy reads |
| Category create | 244031 | 38 / 393 | Hierarchy initialization |
| Category update | 244009 | 38 / 393 | Hierarchy-preserving mutation |
| Category move | 244007 | 34 / 304 | Recursive lineage/depth update |
| Category delete | 244008 | 80 / 670 | Subtree and product effects |
| Category uniqueness | 244028 | 14 / 45 | Store-scoped identity |
| Variant create/update | 244268–244269 | 46–48 / 469–484 | Variant aggregate and inventory |
| Variant read | 244271–244272 | 24–40 / 78–780 | Variant projection and uniqueness |
| Variant SKU uniqueness | 244270 | 37 / 990 | Parent-product scope |
| Inventory create/update | 244155, 244157 | 32–46 / 299–753 | Availability and price association |
| Inventory read/delete | 244158–244160 | 23–40 / 64–629 | Regional availability lifecycle |
| Product price reads | 244172–244174 | 137 / 3009–3014 | Highest-risk pricing graph |
| Product price create/update/delete | 244169–244171, 244175 | 27–40 / 69–230 | Price persistence boundary |
| Product image create/delete | 244147–244151 | 18–38 / 47–360 | Provider, metadata, and cleanup |
| Product option/value lifecycle | 244120–244133 | 13–29 / 33–193 | Option/value definitions |
| Product attribute lifecycle | 244134–244139 | 24–35 / 82–406 | Product-level selections |
| Option price calculation | 244281 | 1 / 201 | Variation and option adjustment |

## Source Files to Read

These files are the primary business-logic candidates. Read complete files, using the Java legacy
multi-pass protocol for files over 500 lines.

| Local path | CAST evidence / inclusion reason |
|---|---|
| `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/api/v1/product/ProductApi.java` | Product create, update, delete, storefront reads, SKU uniqueness, and category associations |
| `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/api/v1/category/CategoryApi.java` | Category reads, mutations, uniqueness, visibility, and hierarchy operations |
| `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/api/v2/product/ProductVariantApi.java` | Variant CRUD, authorization, and parent-product scope |
| `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/api/v2/product/ProductVariationApi.java` | Selected option/value variation and price calculation |
| `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/api/v1/product/ProductInventoryApi.java` | Availability and inventory administration boundary |
| `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/api/v1/product/ProductImageApi.java` | Product media upload, update, and deletion paths |
| `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/api/v1/product/ProductPriceApi.java` | Product and availability price administration |
| `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/api/v1/product/ProductAttributeOptionApi.java` | Product option, option-value, and attribute lifecycle |
| `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/catalog/product/ProductServiceImpl.java` | Product persistence, availability precondition, media handling, deletion, and listing |
| `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/catalog/category/CategoryServiceImpl.java` | Category creation, recursive moves, subtree deletion, and lineage/depth |
| `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/repositories/catalog/product/ProductRepositoryImpl.java` | Storefront predicates, localized reads, friendly URLs, count/fetch behavior |
| `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/repositories/catalog/product/price/ProductPriceRepository.java` | Product-price store predicates and query grouping |
| `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/catalog/product/price/ProductPriceServiceImpl.java` | Price persistence, descriptions, and availability association |
| `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/catalog/pricing/PricingServiceImpl.java` | Variant-price delegation and fallback behavior |
| `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/utils/ProductPriceUtils.java` | Default/variant price selection, regional availability, special windows, and option adjustments |
| `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/catalog/inventory/ProductInventoryServiceImpl.java` | Wildcard-region availability and inventory selection |
| `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/mapper/catalog/product/PersistableProductMapper.java` | Reference resolution, descriptions, variants, availability, and media mapping |
| `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/populator/catalog/ReadableProductPopulator.java` | Localized output and display-only versus selectable product properties |
| `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/modules/cms/product/ProductFileManagerImpl.java` | Media validation, provider calls, resizing, and storage behavior |
| `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/catalog/product/image/ProductImageServiceImpl.java` | Image metadata persistence and media event publication |
| `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/configuration/events/products/listeners/IndexProductEventListener.java` | Product, variant, attribute, and image event dispatch |
| `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/order/OrderServiceImpl.java` | Legacy inventory decrement path and catalog reservation dependency |
| `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/controller/order/facade/OrderFacadeImpl.java` | Inventory validation during checkout; read for boundary behavior |
| `initial-source/shopizer-3.2.7/sm-core-model/src/main/java/com/salesmanager/core/model/catalog/product/Product.java` | Product identity, visibility, availability, and merchant scope |
| `initial-source/shopizer-3.2.7/sm-core-model/src/main/java/com/salesmanager/core/model/catalog/product/variant/ProductVariant.java` | Variant identity, SKU, default selection, and parent relationship |
| `initial-source/shopizer-3.2.7/sm-core-model/src/main/java/com/salesmanager/core/model/catalog/category/Category.java` | Category identity, parent, lineage, depth, and store scope |
| `initial-source/shopizer-3.2.7/sm-core-model/src/main/java/com/salesmanager/core/model/catalog/catalog/Catalog.java` | Catalog identity and merchant scope |
| `initial-source/shopizer-3.2.7/sm-core-model/src/main/java/com/salesmanager/core/model/catalog/product/attribute/ProductOption.java` | Option identity and selectable-property structure |
| `initial-source/shopizer-3.2.7/sm-core-model/src/main/java/com/salesmanager/core/model/catalog/product/attribute/ProductOptionValue.java` | Option-value identity, display, and image data |
| `initial-source/shopizer-3.2.7/sm-core-model/src/main/java/com/salesmanager/core/model/catalog/product/variation/ProductVariation.java` | Variation identity and option/value combinations |

## Context-Only / Skip Unless Referenced

| Path family | Reason |
|---|---|
| `sm-core/src/main/java/com/salesmanager/core/business/services/search/*` and search provider adapters | MS-03 owns search reads and projections; MS-02 supplies versioned catalog events |
| `sm-core/src/main/java/com/salesmanager/core/business/services/tax/*` | MS-08 ownership; read only to understand catalog availability inputs |
| `sm-core/src/main/java/com/salesmanager/core/business/services/shipping/*` | MS-09 ownership; read only to resolve product dimensions, weight, and virtual-product facts |
| `sm-core/src/main/java/com/salesmanager/core/business/modules/order/total/*` | MS-07/MS-08/MS-09 calculation ownership; inspect only where product pricing is an explicit dependency |
| Product review APIs and review services | Include only when product deletion or ownership rules require the relationship; no standalone MS-02 target endpoint is defined |
| Angular administration and React storefront components | Frontend behavior is evidence for UI rules; do not duplicate frontend implementation in the backend extraction |
| Hibernate/JPA framework classes and generated metadata | Infrastructure unless they change scope, cascade, uniqueness, or transaction behavior |

## Owned Data Candidates

Products, localized product descriptions, categories and category lineage, product variants,
availability records, product media metadata, options/option values, variations, product-category
associations, and inventory reservation/decrement state. Product price records and commercial
promotion behavior require coordination with MS-07; catalog display and variation-price behavior
must not be confused with ownership of the pricing engine.

Exact table names and columns must be derived from JPA annotations, repository queries, and the
approved MS-02 domain model. Do not infer ownership from a CAST data graph alone.

## Cross-Service Dependencies

| Dependency | Evidence / purpose |
|---|---|
| MS-10 Merchant and Store Administration | Store/tenant scope validation for product, category, variant, and inventory mutations |
| MS-03 Search | Consumes product, category, availability, and media change events; MS-02 does not own search reads |
| MS-04 Cart and Checkout | Consumes product facts, availability, price/display data, and inventory reservation results |
| MS-05 Order Management | Consumes inventory reservation and release/compensation events |
| MS-07 Pricing and Promotions | Owns commercial price/promotion policy; coordinate catalog display-price and variation-price boundaries |
| MS-08 Tax | Consumes product, availability, and taxable catalog facts |
| MS-09 Shipping | Consumes product weight, dimensions, virtual status, and availability facts |
| MS-12 Platform Integrations | Provider-neutral media storage and image-processing boundary |

## Existing P1 Rules Requiring P4 Upgrade

Re-extract all assigned rules rather than copying summaries:

- `BR-CAT-001..019` — product, category, variant, availability, media, and deletion behavior
- `BR-CAT-025..039` — pagination, query consistency, catalog pricing, event projections, and inventory reservation
- `BR-ORD-012` — inventory availability validation and decrement/reservation behavior
- `BR-EXT-019..020` — media-provider and catalog integration behavior
- `BR-UI-003..006` — SKU, visibility, localization, and category administration interactions

Pay particular attention to category subtree deletion, variant-price fallback, special-price
windows, negative option adjustments, image-event filtering, product event aggregate reload,
availability null paths, and the replacement of legacy inventory decrement with an atomic,
idempotent reservation state machine.

## Hidden-Engine Check

The CAST surface is substantially larger than a CRUD baseline. Product price retrieval has
3,009–3,014 full-graph objects, storefront product reads have 1,310, category deletion has 670,
inventory creation has 753, and product/category association flows exceed 1,079 objects.

The residual contains several engines: merchant-scoped identity validation, localized and
regional storefront eligibility, category lineage/depth recursion, variant and option price
selection, special-price date handling, media-provider orchestration, event-driven projection
handoff, and inventory reservation/decrement. Treat these as deep business logic and preserve
their cross-service boundaries rather than generating thin entity CRUD.

## Dead Code

No dead-code determination is made in this brief. The extractor must not exclude product,
category, variant, media, pricing, or inventory components solely because an endpoint has a small
reduced graph or because a provider is configured indirectly. Exclude only components that CAST
and source evidence identify as unreachable, audit-only, or generic infrastructure.
