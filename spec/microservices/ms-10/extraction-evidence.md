# Merchant and Store Administration — Extraction Evidence

**Analysis mode:** Hybrid  
**CAST application:** `Shopizer-Backend`  
**CAST brief:** `assessment/ms-10-cast-brief.md`  
**Deep-read scope:** 17 CAST-listed business-logic files.

## CAST-bounded source files processed

| # | File | Reason / sections read | Rules |
|---:|---|---|---|
| 1 | `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/api/v1/store/MerchantStoreApi.java` | Store, hierarchy, marketing, language, signup, and uniqueness entry points | BR-MER-010..012, BR-UI-007, BR-MSA-BRD-001, BR-MSA-LST-001 |
| 2 | `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/controller/store/facade/StoreFacadeImpl.java` | Create, update, delete, uniqueness, validation orchestration | BR-MER-001..007, BR-MSA-VAL-001..003 |
| 3 | `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/merchant/MerchantStoreServiceImpl.java` | Persistence, retailer hierarchy, child lookup, pagination | BR-MER-008, BR-MER-012, BR-MSA-READ-001, BR-MSA-AUTH-001 |
| 4 | `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/merchant/MerchantStoreService.java` | Store service contract and operation surface | BR-MER-008..009 |
| 5 | `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/repositories/merchant/MerchantRepository.java` | Store lookup and uniqueness query contract | BR-MER-003 |
| 6 | `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/repositories/merchant/PageableMerchantRepository.java` | Collection filtering, ordering, and pagination contract | BR-MSA-READ-001 |
| 7 | `initial-source/shopizer-3.2.7/sm-core-model/src/main/java/com/salesmanager/core/model/merchant/MerchantStore.java` | Store fields, retailer/parent relationship, validation annotations | BR-MER-001..002, BR-MER-006..007, BR-MER-012 |
| 8 | `initial-source/shopizer-3.2.7/sm-core-model/src/main/java/com/salesmanager/core/model/merchant/MerchantStoreCriteria.java` | Store search criteria and filtering semantics | BR-MSA-READ-001, BR-MSA-LST-001 |
| 9 | `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/populator/store/PersistableMerchantStorePopulator.java` | Request mapping, defaults, hierarchy, language, and address validation | BR-MER-001..004, BR-MER-007, BR-MER-012, BR-MSA-VAL-003, BR-MSA-LANG-001 |
| 10 | `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/populator/store/ReadableMerchantStorePopulator.java` | Response mapping and store-context behavior | BR-MER-010, BR-MER-012, BR-MSA-BRD-001 |
| 11 | `initial-source/shopizer-3.2.7/sm-shop-model/src/main/java/com/salesmanager/shop/model/store/MerchantStoreEntity.java` | Persistence mapping and store-owned fields | BR-MER-009, BR-MSA-BRD-001, BR-MSA-LANG-001 |
| 12 | `initial-source/shopizer-3.2.7/sm-shop-model/src/main/java/com/salesmanager/shop/model/store/PersistableMerchantStore.java` | Store administration request shape | BR-MER-001..005, BR-MSA-VAL-002 |
| 13 | `initial-source/shopizer-3.2.7/sm-shop-model/src/main/java/com/salesmanager/shop/model/store/ReadableMerchantStore.java` | Store response shape | BR-MER-010, BR-MER-012, BR-MSA-BRD-001 |
| 14 | `initial-source/shopizer-3.2.7/sm-shop-model/src/main/java/com/salesmanager/shop/model/store/ReadableMerchantStoreList.java` | Collection response and pagination shape | BR-MSA-READ-001, BR-MSA-LST-001 |
| 15 | `initial-source/shopizer-3.2.7/sm-shop-model/src/main/java/com/salesmanager/shop/model/store/MerchantConfigEntity.java` | Boundary classification only; configuration is MS-11-owned | BR-MSA-BRD-001 |
| 16 | `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/application/config/MerchantStoreArgumentResolver.java` | Default-store resolution and URI/store authorization | BR-MER-010..011, BR-UI-007 |
| 17 | `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/facade/user/UserFacadeImpl.java` | Administrator store hierarchy authorization | BR-MER-011, BR-MSA-AUTH-001 |

## CAST transaction evidence

The service brief bounded extraction to transactions `244219..244234`, `244084`, and `244085`, including store creation, update, deletion, collection, hierarchy, language, marketing/logo, uniqueness, public/private reads, and signup verification. Create and update paths contained more than 3,000 full call-graph objects each, so their facade, populator, service, repository, model, and resolver paths were treated as mandatory business-logic scope.

## Context-only classification

CMS content APIs, merchant/module configuration implementations, generic reference-data services, file-provider internals, and generic framework/audit helpers were not assigned independent MS-10 rules. Their reachable role is captured as an explicit dependency or boundary gap.

## Extraction status

- CAST-listed files processed: **17/17**
- Rules extracted: **21**
- Carry-forward rules: **13**
- Net-new rules: **8**
- Preservation tables: **21/21**, eight dimensions each
- Source-reference existence: **verified — all 36 references resolve to existing files**
