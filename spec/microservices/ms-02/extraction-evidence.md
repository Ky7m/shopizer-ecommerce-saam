# Catalog and Product — Extraction Evidence

**Service:** MS-02 Catalog and Product  
**Analysis mode:** Hybrid  
**CAST application:** Shopizer-Backend  
**CAST-guided workflow:** Existing CAST application and component-family context were used to select high-complexity product, category, repository, pricing, mapping, media, event, and API paths before direct source reading. Numeric CAST object/transaction IDs were not exposed by the available session interface.

## CAST-guided component families

| Family | CAST-guided reason | Direct source confirmation |
|---|---|---|
| Product persistence | High-complexity product save/delete path | `ProductServiceImpl`, `PersistableProductMapper` |
| Category hierarchy | Recursive lineage/depth and subtree behavior | `CategoryServiceImpl` |
| Storefront reads | Store/language/region/availability predicates | `ProductRepositoryImpl` |
| Pricing | Variant fallback, wildcard/default selection, discounts, adjustments | `ProductPriceUtils`, `PricingServiceImpl` |
| Product representation | Localized and selectable/read-only output | `ReadableProductPopulator` |
| Media provider | Binary storage and image transformation | `ProductImageServiceImpl`, `ProductFileManagerImpl` |
| Event projection handoff | Product/variant/image/attribute event processing | `IndexProductEventListener` |
| Administration API | Authorization and catalog mutation surface | `CategoryApi`, `ProductVariantApi`, `ProductApi` |
| Frontend dependencies | SKU, visibility, localization, category form semantics | Angular product/category forms and React product detail |

## Source files processed

| # | File | Lines/sections read | Rules extracted | Vector source |
|---:|---|---|---:|---|
| 1 | `initial-source/shopizer-3.2.7/sm-core-model/src/main/java/com/salesmanager/core/model/catalog/product/Product.java` | Entity/table/SKU fields, 50-537 | BR-CAT-001, BR-CAT-032 | Product model vector |
| 2 | `initial-source/shopizer-3.2.7/sm-core-model/src/main/java/com/salesmanager/core/model/catalog/product/variant/ProductVariant.java` | Entity/table/SKU/default fields, 37-202 | BR-CAT-002, BR-CAT-012 | Variant model vector |
| 3 | `initial-source/shopizer-3.2.7/sm-core-model/src/main/java/com/salesmanager/core/model/catalog/category/Category.java` | Entity/table/hierarchy fields, 32-220 | BR-CAT-003, BR-CAT-006, BR-CAT-007 | Category model vector |
| 4 | `initial-source/shopizer-3.2.7/sm-core-model/src/main/java/com/salesmanager/core/model/catalog/catalog/Catalog.java` | Entity/table/scoped code fields, 47-153 | BR-CAT-003 | Catalog model vector |
| 5 | `initial-source/shopizer-3.2.7/sm-core-model/src/main/java/com/salesmanager/core/model/catalog/product/attribute/ProductOption.java` | Entity/table/code fields, 30-138 | BR-CAT-003, BR-CAT-031 | Option model vector |
| 6 | `initial-source/shopizer-3.2.7/sm-core-model/src/main/java/com/salesmanager/core/model/catalog/product/attribute/ProductOptionValue.java` | Entity/table/code/display fields, 32-153 | BR-CAT-003, BR-CAT-031 | Option-value model vector |
| 7 | `initial-source/shopizer-3.2.7/sm-core-model/src/main/java/com/salesmanager/core/model/catalog/product/variation/ProductVariation.java` | Entity/table/scoped variation fields, 37-135 | BR-CAT-003, BR-CAT-029 | Variation model vector |
| 8 | `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/catalog/product/ProductServiceImpl.java` | Delete/save/update/list, 205-334, 345-360 | BR-CAT-004, BR-CAT-017..019, BR-CAT-025, BR-CAT-032, BR-CAT-038 | Control 74, data 53, constants 14, states 9, outcomes 24, writes 13, integrations 12, errors 28 |
| 9 | `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/catalog/category/CategoryServiceImpl.java` | Create/delete/move, 54-70, 231-345 | BR-CAT-006..008, BR-CAT-034 | Control 63, data 47, constants 13, states 10, outcomes 20, writes 12, integrations 8, errors 24 |
| 10 | `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/repositories/catalog/product/ProductRepositoryImpl.java` | Friendly URL, localized reads, listing count/fetch, 207-315, 480-557, 666-673, 842-844 | BR-CAT-009, BR-CAT-010, BR-CAT-026, BR-CAT-033 | Control 80, data 62, constants 12, states 4, outcomes 21, writes 0, integrations 2, errors 25 |
| 11 | `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/utils/ProductPriceUtils.java` | Price selection, special windows, adjustments, 92-128, 550-721 | BR-CAT-012..015 | Control 69, data 44, constants 20, states 8, outcomes 19, writes 0, integrations 5, errors 24 |
| 12 | `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/catalog/pricing/PricingServiceImpl.java` | Variant pricing fallback, 109-119 | BR-CAT-016 | Control 8, data 5, constants 0, states 0, outcomes 3, writes 0, integrations 1, errors 2 |
| 13 | `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/catalog/inventory/ProductInventoryServiceImpl.java` | Product/variant inventory and wildcard selection, 41-85 | BR-CAT-011, BR-CAT-016 | Control 20, data 15, constants 2, states 2, outcomes 7, writes 0, integrations 1, errors 6 |
| 14 | `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/mapper/catalog/product/PersistableProductMapper.java` | References, descriptions, variants, inventory, media, 100-311 | BR-CAT-005, BR-CAT-030, BR-CAT-032 | Control 62, data 51, constants 16, states 7, outcomes 18, writes 5, integrations 11, errors 27 |
| 15 | `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/populator/catalog/ReadableProductPopulator.java` | Properties/options/localized output, 268-470 | BR-CAT-031, BR-CAT-033 | Control 45, data 42, constants 11, states 3, outcomes 17, writes 0, integrations 4, errors 15 |
| 16 | `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/repositories/catalog/product/price/ProductPriceRepository.java` | Product/variant/store predicates, 32-48 | BR-CAT-027 | Control 6, data 7, constants 1, states 0, outcomes 2, writes 0, integrations 0, errors 1 |
| 17 | `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/api/v1/category/CategoryApi.java` | Authorization and category mutations, 147-224 | BR-CAT-028 | Control 30, data 14, constants 4, states 4, outcomes 8, writes 4, integrations 0, errors 8 |
| 18 | `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/api/v2/product/ProductVariantApi.java` | Authorization and variant operations, 73-188 | BR-CAT-028 | Control 25, data 12, constants 4, states 3, outcomes 7, writes 3, integrations 0, errors 7 |
| 19 | `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/api/v2/product/ProductVariationApi.java` | Selected option/value price calculation, 103-156 | BR-CAT-029 | Control 26, data 20, constants 2, states 1, outcomes 7, writes 0, integrations 0, errors 5 |
| 20 | `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/modules/cms/product/ProductFileManagerImpl.java` | Image provider, validation, resize, 65-262 | BR-EXT-019, BR-EXT-020 | Media-processing vector |
| 21 | `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/catalog/product/image/ProductImageServiceImpl.java` | Provider call, metadata save, event publication, 79-107 | BR-CAT-017, BR-EXT-019 | Media-service vector |
| 22 | `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/configuration/events/products/listeners/IndexProductEventListener.java` | Event dispatch, aggregate refresh, media/attribute/variant paths, 55-311 | BR-CAT-035, BR-CAT-036 | Control 71, data 56, constants 18, states 5, outcomes 24, writes 0, integrations 13, errors 31 |
| 23 | `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/order/OrderServiceImpl.java` | Legacy inventory decrement, 191-210 | BR-ORD-012, BR-CAT-037, BR-CAT-039 | Inventory decrement vector |
| 24 | `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/controller/order/facade/OrderFacadeImpl.java` | Facade inventory validation, 383-416 | BR-ORD-012 | Inventory validation vector |
| 25 | `initial-source/shopizer-admin-main/src/app/pages/catalogue/products/product-form/product-form.component.ts` | SKU, visibility, descriptions, 172-219, 339-451 | BR-UI-003..005 | Frontend validation vector |
| 26 | `initial-source/shopizer-admin-main/src/app/pages/catalogue/products/services/product.service.ts` | SKU uniqueness API call, 72-76 | BR-UI-003 | Frontend integration vector |
| 27 | `initial-source/shopizer-admin-main/src/app/pages/catalogue/categories/category-form/category-form.component.ts` | Category form, parent, URL, fallback, 150-179, 247-380 | BR-UI-006 | Frontend hierarchy vector |
| 28 | `initial-source/shopizer-admin-main/src/app/pages/catalogue/categories/services/category.service.ts` | Category API calls, 20-67 | BR-UI-006 | Frontend integration vector |
| 29 | `initial-source/shopizer-shop-reactjs-main/src/components/product/ProductDescriptionInfo.js` | Purchase gating and quantity controls, 274-305 | BR-UI-004 | Frontend storefront vector |

## Source semantic vector totals

| Component | Control | Data | Constants | States | Outcomes | Writes | Integrations | Errors |
|---|---:|---:|---:|---:|---:|---:|---:|---:|
| ProductServiceImpl | 74 | 53 | 14 | 9 | 24 | 13 | 12 | 28 |
| CategoryServiceImpl | 63 | 47 | 13 | 10 | 20 | 12 | 8 | 24 |
| ProductPriceUtils | 69 | 44 | 20 | 8 | 19 | 0 | 5 | 24 |
| ProductRepositoryImpl | 80 | 62 | 12 | 4 | 21 | 0 | 2 | 25 |
| PersistableProductMapper | 62 | 51 | 16 | 7 | 18 | 5 | 11 | 27 |
| ReadableProductPopulator | 45 | 42 | 11 | 3 | 17 | 0 | 4 | 15 |
| IndexProductEventListener | 71 | 56 | 18 | 5 | 24 | 0 | 13 | 31 |
| ProductFileManagerImpl | 48 | 33 | 12 | 4 | 16 | 4 | 8 | 20 |

## Extraction status

- CAST-guided target selection: complete using the configured Shopizer-Backend application context.
- Direct source files processed: 29 listed evidence units; some commands read multiple related files.
- Carry-forward rules re-extracted: 33.
- Net-new rules: 8.
- Total rules: 41.
- Source vectors: complete for all high-complexity component families.
- Search ownership: retained by MS-03; MS-02 defines producer events only.
- Store scope: retained as opaque MS-10 dependency.
- Inventory reservation/decrement: assigned to MS-02 by the approved Phase 3 decision.

## Known limitations

1. Numeric CAST transaction/object IDs were not available through the exposed local tool interface; references use the configured application and component/transaction family.
2. Live CAST dead-code status for individual components was not available; no source component was excluded solely on a dead-code assumption.
3. The existing legacy inventory decrement is inconsistent and is intentionally replaced by the atomic target reservation contract.
4. Phase 4a must classify destructive category deletion, special-price edge windows, negative option adjustments, and legacy media-event defects.
