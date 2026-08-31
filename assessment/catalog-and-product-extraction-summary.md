# Catalog and Product - Extraction Summary

## Segment Profile

- Scope: products, variants, categories, catalogues, options, attributes, inventory, pricing, search, and catalog administration.
- Modules: `sm-core`, `sm-core-model`, `sm-shop`.
- Business rules extracted: 31.
- Primary entities: product, product description, availability, price, variant, category, catalog, option/value, image, review, product relationship.
- Discovery: direct source read; confidence high for constraints and calculations, medium/low for identified defect candidates.

## Call Graphs

```text
Product API -> product facade/mapper -> ProductServiceImpl
  -> ProductRepository/category/option/inventory/image services -> persistence

Category API -> CategoryServiceImpl.create/addChild/delete
  -> lineage/depth recursion -> product-category association changes

Storefront product/friendly URL -> ProductRepositoryImpl
  -> merchant/language/region/availability filters -> price/inventory/populator

Product/variant/attribute/image event -> IndexProductEventListener
  -> SearchServiceImpl.index/document -> configured search module
```

## Business Rules

| ID | Rule | Source reference | Confidence |
|---|---|---|---|
| BR-CAT-001 | Product SKU is unique within merchant. | `sm-core-model/.../Product.java:52-53` | High |
| BR-CAT-002 | Variant SKU is unique within parent product. | `.../ProductVariant.java:39-44` | High |
| BR-CAT-003 | Category/catalog/option/value/variation codes are merchant-scoped. | `Category.java:34-37`; `Catalog.java:49-50`; `ProductOption.java:31-34`; `ProductOptionValue.java:33-36`; `ProductVariation.java:39-40` | High |
| BR-CAT-004 | Product requires at least one availability before persistence. | `ProductServiceImpl.saveOrUpdate:259-263` | High |
| BR-CAT-005 | Product manufacturer/type/category references must resolve to current merchant. | `PersistableProductMapper.merge:126-144,210-232` | High |
| BR-CAT-006 | Category create materializes lineage and depth from parent. | `CategoryServiceImpl.create:54-70` | High |
| BR-CAT-007 | Category moves recursively recalculate descendant lineage/depth. | `CategoryServiceImpl.addChild:299-340` | High |
| BR-CAT-008 | Category deletion may delete/detach products in subtree. | `CategoryServiceImpl.delete:231-285` | High |
| BR-CAT-009 | Storefront listings require active/available product, date eligibility, merchant/language, and region availability. | `ProductRepositoryImpl.getProductForLocale/getProductsListForLocale:281-315,480-557` | High |
| BR-CAT-010 | Friendly URL retrieval applies merchant, language, region, availability, and date filters. | `ProductRepositoryImpl.getByFriendlyUrl:207-267` | High |
| BR-CAT-011 | Wildcard region `*` is preferred default availability. | `ProductInventoryServiceImpl.defaultAvailability:62-73` | High |
| BR-CAT-012 | Default-selected variant supplies price when it has usable pricing; otherwise parent product is used. | `ProductPriceUtils.calculateFinalPrice:550-576` | High |
| BR-CAT-013 | Pricing considers wildcard availability and default price first, then additional prices. | `ProductPriceUtils.calculateFinalPrice:578-610` | High |
| BR-CAT-014 | Special price is active for valid open/date windows. | `ProductPriceUtils.finalPrice:651-704` | High |
| BR-CAT-015 | Positive selected attribute prices are added to final/original/discounted prices. | `ProductPriceUtils.getFinalPrice:92-128` | High |
| BR-CAT-016 | Variant-specific pricing returns null and falls back to parent pricing. | `PricingServiceImpl.calculateProductPrice(ProductVariant):109-119`; `ProductInventoryServiceImpl:41-59` | Low |
| BR-CAT-017 | Product images persist separately from product transaction. | `ProductServiceImpl.saveOrUpdate:264-325` | High |
| BR-CAT-018 | Image persistence exceptions are logged/swallowed after product persistence. | `ProductServiceImpl.saveOrUpdate:281-332` | High |
| BR-CAT-019 | Product deletion removes images, reviews, relationships, categories, then product. | `ProductServiceImpl.delete:215-245` | High |
| BR-CAT-020 | Search indexing/search is bypassed when no-index, `INDEX_PRODUCTS=false`, or module absent. | `SearchServiceImpl:122-128,378-417` | High |
| BR-CAT-021 | Search creates one localized document per product description/language. | `SearchServiceImpl:122-249` | High |
| BR-CAT-022 | Product/variant/attribute/image events refresh search index. | `IndexProductEventListener:55-311` | Medium |
| BR-CAT-023 | Image/attribute event filters compare an ID to itself, always false. | `IndexProductEventListener:217-230,244-311` | Low defect candidate |
| BR-CAT-024 | Autocomplete returns at most 15 suggestions; category facets are effectively disabled. | `SearchFacadeImpl.autocompleteRequest:62-64,176-203` | High |
| BR-CAT-025 | Product listing pagination page-size assignment is immediately overwritten. | `ProductServiceImpl.listByStore:345-360` | Medium |
| BR-CAT-026 | Listing count and fetch availability predicates differ, with grouping concerns. | `ProductRepositoryImpl.listByStore:666-673,842-844` | Medium |
| BR-CAT-027 | Product-price merchant predicate has operator-precedence risk. | `ProductPriceRepository.java:32-48` | Medium |
| BR-CAT-028 | Catalog/product mutations require privileged groups. | `CategoryApi.java:147-224`; `ProductVariantApi.java:73-113` | High |
| BR-CAT-029 | Selected option/value pairs drive variation-specific price calculation. | `ProductVariationApi.calculateVariant:103-156` | High |
| BR-CAT-030 | Variant-derived default inventory may null-dereference without wildcard availability. | `PersistableProductMapper.merge:248-266` | Low |
| BR-CAT-031 | Readable output separates display-only properties from selectable options. | `ReadableProductPopulator.populate:268-470` | High |

## CRUD and Integrations

| Area | Behavior |
|---|---|
| Product | CRUD with merchant-scoped SKU, availability, categories, options, variants, images, reviews, relationships, and inventory. |
| Category | CRUD plus recursive hierarchy lineage/depth maintenance and subtree product effects. |
| Pricing | Product/variant/attribute price resolution, special prices, wildcard-region availability, and storefront price DTOs. |
| Search | Event-driven indexing, localized documents, autocomplete, configured search provider. |
| UI | Angular product/category/option/catalog screens; React storefront listing/detail/option-price flows. |

## Layer A/B/C Flags

- Lifecycle: product draft/visible/available/deleted; category nested/moved/deleted; variant and option ownership; index stale/refreshed.
- Invariants: merchant-scoped identities, category lineage/depth, availability before persistence, SKU uniqueness, price selection.
- Extensibility: product options, localized descriptions, configured search module, search event listeners, merchant configuration.
- Placement candidates: price/inventory aggregation, search indexing, listing queries, category subtree deletion; default app tier pending P4b.

## Source Semantic Vectors

| Component | Control | Data | Constants | States | Outcomes | Writes | Integrations | Errors |
|---|---:|---:|---:|---:|---:|---:|---:|---:|
| `ProductServiceImpl` | 74 | 53 | 14 | 9 | 24 | 13 | 12 | 28 |
| `CategoryServiceImpl` | 63 | 47 | 13 | 10 | 20 | 12 | 8 | 24 |
| `ProductPriceUtils` | 69 | 44 | 20 | 8 | 19 | 0 | 5 | 24 |
| `ProductRepositoryImpl` | 80 | 62 | 12 | 4 | 21 | 0 | 2 | 25 |
| `SearchServiceImpl` | 71 | 56 | 18 | 5 | 24 | 0 | 13 | 31 |
| `PersistableProductMapper` | 62 | 51 | 16 | 7 | 18 | 5 | 11 | 27 |
| `ReadableProductPopulator` | 45 | 42 | 11 | 3 | 17 | 0 | 4 | 15 |

## Clarification Items

Confirm destructive category behavior, variant-price fallback, search event filter defect, pagination/count mismatches, merchant predicate grouping, availability null path, index consistency, and product/category localized fallback semantics.
