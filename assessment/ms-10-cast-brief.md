# CAST-Guided Extraction Brief — MS-10 Merchant and Store Administration

**Analysis mode:** Hybrid (CAST transaction bounds + direct Java source read)  
**CAST application:** `Shopizer-Backend`  
**Local path mapping:** `§{main_sources}§` → `initial-source/`  
**Service boundary:** MS-10 owns merchant/store lifecycle, hierarchy, store context, supported languages, branding metadata, and store-scoped authorization. CMS content, file providers, merchant configuration, and integration-module configuration belong to MS-11 and are context-only.

## Entry Points and CAST Transaction Evidence

| Transaction | CAST ID | Reduced objects | Full objects | Entry operation |
|---|---:|---:|---:|---|
| Merchant child stores | 244228 | 48 | 554 | `GET /api/v1/private/merchant/{merchant}/children/` |
| Merchant stores | 244221 | 42 | 534 | `GET /api/v1/private/merchant/{merchant}/stores/` |
| Store creation | 244225 | 145 | 3,082 | `POST /api/v1/private/store/` |
| Store uniqueness | 244233 | 15 | 40 | `GET /api/v1/store/unique/` |
| Store deletion | 244234 | 37 | 284 | `DELETE /api/v1/private/store/{store}/` |
| Store read | 244220 | 39 | 505 | `GET /api/v1/private/store/{store}/` |
| Store update | 244226 | 152 | 3,092 | `PUT /api/v1/private/store/{store}/` |
| Store marketing read | 244227 | 43 | 369 | `GET /api/v1/private/store/{store}/marketing/` |
| Store marketing write | 244229 | 41 | 337 | `POST /api/v1/private/store/{store}/marketing/` |
| Store logo delete | 244231 | 46 | 407 | `DELETE /api/v1/private/store/{store}/marketing/logo/` |
| Store logo write | 244230 | 51 | 462 | `POST /api/v1/private/store/{store}/marketing/logo/` |
| Store collection | 244222 | 47 | 578 | `GET /api/v1/private/stores/` |
| Store names | 244223 | 47 | 579 | `GET /api/v1/private/stores/names/` |
| Store languages | 244224 | 15 | 45 | `GET /api/v1/store/languages/` |
| Store signup | 244084 | 27 | 192 | `POST /api/v1/store/signup/` |
| Store signup verification | 244085 | 1 | 6 | `GET /api/v1/store/{store}/signup/{token}/` |
| Store uniqueness | 244232 | 15 | 40 | `GET /api/v1/store/unique/` |
| Store public read | 244219 | 31 | 377 | `GET /api/v1/store/{store}/` |

The large create/update transactions indicate a hidden validation, hierarchy, branding, and configuration orchestration layer rather than CRUD-only behavior. Deep-read the store facade, populators, resolver, service, repository, and model files before classifying any operation as CRUD.

## Source Files to Read — Business Logic

| File | Local path | CAST evidence / reason |
|---|---|---|
| `MerchantStoreApi.java` | `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/api/v1/store/MerchantStoreApi.java` | Primary store, hierarchy, marketing, language, and uniqueness entry points |
| `StoreFacadeImpl.java` | `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/controller/store/facade/StoreFacadeImpl.java` | Store create/update/delete orchestration and validation |
| `MerchantStoreServiceImpl.java` | `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/merchant/MerchantStoreServiceImpl.java` | Store persistence, retailer hierarchy, child lookup, pagination |
| `MerchantStoreService.java` | `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/merchant/MerchantStoreService.java` | Service contract and operation surface |
| `MerchantRepository.java` | `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/repositories/merchant/MerchantRepository.java` | Store lookup and uniqueness queries |
| `PageableMerchantRepository.java` | `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/repositories/merchant/PageableMerchantRepository.java` | Store listing and pagination query contract |
| `MerchantStore.java` | `initial-source/shopizer-3.2.7/sm-core-model/src/main/java/com/salesmanager/core/model/merchant/MerchantStore.java` | Domain fields, retailer/parent relationships, validation annotations |
| `MerchantStoreCriteria.java` | `initial-source/shopizer-3.2.7/sm-core-model/src/main/java/com/salesmanager/core/model/merchant/MerchantStoreCriteria.java` | Store search criteria and filtering semantics |
| `PersistableMerchantStorePopulator.java` | `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/populator/store/PersistableMerchantStorePopulator.java` | Request-to-domain mapping, defaults, hierarchy, language and address validation |
| `ReadableMerchantStorePopulator.java` | `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/populator/store/ReadableMerchantStorePopulator.java` | Domain-to-response mapping and public/store context behavior |
| `MerchantStoreEntity.java` | `initial-source/shopizer-3.2.7/sm-shop-model/src/main/java/com/salesmanager/shop/model/store/MerchantStoreEntity.java` | Persistence mapping and store-owned fields |
| `PersistableMerchantStore.java` | `initial-source/shopizer-3.2.7/sm-shop-model/src/main/java/com/salesmanager/shop/model/store/PersistableMerchantStore.java` | Store administration request shape |
| `ReadableMerchantStore.java` | `initial-source/shopizer-3.2.7/sm-shop-model/src/main/java/com/salesmanager/shop/model/store/ReadableMerchantStore.java` | Store response shape |
| `ReadableMerchantStoreList.java` | `initial-source/shopizer-3.2.7/sm-shop-model/src/main/java/com/salesmanager/shop/model/store/ReadableMerchantStoreList.java` | Store collection response shape |
| `MerchantConfigEntity.java` | `initial-source/shopizer-3.2.7/sm-shop-model/src/main/java/com/salesmanager/shop/model/store/MerchantConfigEntity.java` | Read only for boundary classification; configuration ownership is MS-11 |
| `MerchantStoreArgumentResolver.java` | `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/application/config/MerchantStoreArgumentResolver.java` | Default-store resolution and URI/store authorization |

## Context-Only Files — Do Not Assign MS-10 Rules

| File family | Reason |
|---|---|
| CMS content APIs, content facades, `ContentService*`, file/image providers | MS-11 owns content and configuration; inspect only if store branding calls cross the boundary |
| `MerchantConfig*`, `ModuleConfiguration*`, public configuration APIs | MS-11 ownership; record dependency or handoff, do not duplicate rules |
| Generic country, zone, currency, language services | Shared reference data; use as dependencies unless store-specific behavior is discovered |
| Generic audit, cache, transaction, and framework configuration | Infrastructure unless a concrete store lifecycle decision is found |

## Owned Data Candidates

| Target concern | Legacy evidence |
|---|---|
| Merchant/store identity and code | `MerchantStore`, `MerchantStoreEntity`, `MERCHANT_STORE` |
| Store hierarchy and retailer relationship | parent store, retailer flag, child-store queries |
| Store contact and address metadata | name, email, phone, address, postal/country/zone fields |
| Store defaults | language, currency, measurement/dimension/weight units |
| Store branding metadata | template, marketing metadata, logo references; binary file storage is an MS-11/provider dependency |
| Supported store languages | merchant-language relationship and language lookup |

## Cross-Service Dependencies

| Direction | Dependency | Boundary |
|---|---|---|
| MS-10 → MS-11 | Store branding/content/configuration surfaces | MS-10 may own store metadata while MS-11 owns CMS/configuration records and file providers |
| MS-10 → MS-01 | Administrative authorization context | Store mutation endpoints require administrator identity and store authorization |
| MS-10 → shared reference data | Country, zone, currency, language | Resolve through declared shared/reference contracts, not foreign tables |
| MS-10 → external storage | Logo/file persistence | Provider-specific storage is an integration boundary; do not embed provider internals in store DDL |

## Phase 3 Rules to Re-extract

The assigned carry-forward range is `BR-MER-001..012` plus `BR-UI-007` (13 rules). Re-extract every assigned rule at Phase 4 depth, preserving source evidence and adding concrete examples and eight-dimensional semantic-preservation vectors.

## Hidden-Engine Check

The store surface is not CRUD-only. The create/update transactions contain 145 and 152 reduced-call-graph objects respectively, and the full graphs exceed 3,000 objects. Probe the residual for hierarchy authorization, default-language and unit derivation, store-code uniqueness, default-store protection, retailer child expansion, URI context binding, signup verification, and branding/file-provider orchestration. If provider-selected CMS or module configuration behavior is reachable from store mutation paths, record it as an MS-11 dependency or boundary gap rather than silently absorbing it into MS-10.

## Dead-Code and Scope Notes

No dead-code exclusion is asserted from the transaction query alone. Components with no transaction reachability or only generic infrastructure must be confirmed through CAST object callers before exclusion. `MerchantConfigEntity` and content/file provider components are explicitly context-only pending MS-11 extraction.
