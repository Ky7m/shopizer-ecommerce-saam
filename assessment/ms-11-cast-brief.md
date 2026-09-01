# CAST Scout Brief — MS-11 Content and Configuration

**Analysis mode:** Hybrid  
**CAST application:** `Shopizer-Backend`  
**CAST delivery:** `Onboarding-202511171247`  
**CAST root mapping:** `§{main_sources}§` -> `initial-source/shopizer-3.2.7/`

## Scope

MS-11 owns localized CMS pages and content boxes, content visibility and menu
linkage, content files and folders, merchant-scoped configuration, public
configuration projection, and integration-module configuration metadata/cache
state. Store lifecycle remains MS-10. Payment/shipping provider execution
remains MS-12, while MS-11 owns configuration state and invokes provider
validation/discovery boundaries.

## CAST queries executed

- Application inventory and transactions filtered by `content`, `configuration`,
  `config`, `module`, and `store`.
- Full transaction node/link graphs, complexity details, and complexity-sorted
  Java-method objects.
- Object inward/outward call graphs, data graphs, data-graph details, source
  resolution, and database explorer.
- Transaction reachability checks for dead-code candidates.

## Entry points and complexity

### Content and CMS operations

| CAST ID | Operation | Size reduced/full | Complexity max/sum |
|---:|---|---:|---:|
| 244047 | GET `api/v1/content/boxes/` | 13 / 132 | 3 / 44 |
| 244062 | GET `api/v1/content/boxes/{}/` | 15 / 121 | 5 / 45 |
| 244063 | DELETE `api/v1/content/folder/` | 1 / 6 | 1 / 1 |
| 244064 | GET `api/v1/content/images/` | 9 / 252 | 9 / 99 |
| 244040 | GET `api/v1/content/images/download/` | 1 / 17 | 3 / 6 |
| 244045 | GET `api/v1/content/pages/` | 13 / 134 | 3 / 44 |
| 244050 | GET `api/v1/content/pages/name/{}/` | 1 / 87 | 3 / 36 |
| 244049 | GET `api/v1/content/pages/{}/` | 15 / 116 | 4 / 42 |
| 244046 | GET `api/v1/content/summary/` | 1 / 9 | 1 / 1 |
| 244069 | DELETE `api/v1/private/content/` | 11 / 152 | 4 / 58 |
| 244059 | GET `api/v1/private/content/any/{}/` | 13 / 114 | 6 / 52 |
| 244051 | POST `api/v1/private/content/box/` | 18 / 141 | 6 / 60 |
| 244056 | DELETE `api/v1/private/content/box/{}/` | 14 / 63 | 5 / 21 |
| 244058 | PUT `api/v1/private/content/box/{}/` | 18 / 142 | 6 / 62 |
| 244052 | GET `api/v1/private/content/box/{}/exists/` | 13 / 36 | 1 / 5 |
| 244048 | GET `api/v1/private/content/boxes/` | 13 / 132 | 3 / 44 |
| 244061 | GET `api/v1/private/content/boxes/{}/` | 15 / 121 | 5 / 45 |
| 244038 | GET `api/v1/private/content/folder/` | 9 / 270 | 9 / 104 |
| 244039 | POST `api/v1/private/content/images/add/` | 14 / 230 | 7 / 102 |
| 244043 | DELETE `api/v1/private/content/images/remove/` | 11 / 154 | 4 / 60 |
| 244042 | POST `api/v1/private/content/images/rename/` | 23 / 236 | 5 / 101 |
| 244037 | GET `api/v1/private/content/list/` | 9 / 270 | 9 / 112 |
| 244054 | POST `api/v1/private/content/page/` | 18 / 145 | 6 / 61 |
| 244055 | DELETE `api/v1/private/content/page/{}/` | 14 / 63 | 5 / 21 |
| 244057 | PUT `api/v1/private/content/page/{}/` | 18 / 146 | 6 / 63 |
| 244053 | GET `api/v1/private/content/page/{}/exists/` | 13 / 36 | 1 / 5 |
| 244068 | DELETE `api/v1/private/content/{}/` | 14 / 62 | 5 / 21 |
| 244067 | PUT `api/v1/private/content/{}/` | 1 / 11 | 1 / 2 |
| 244060 | GET `api/v1/private/contents/any/` | 13 / 118 | 3 / 42 |

### Configuration and module operations

| CAST ID | Operation | Size reduced/full | Complexity max/sum |
|---:|---|---:|---:|
| 244237 | GET `api/v1/config/` | 17 / 107 | 4 / 43 |
| 244035 | GET `api/v1/private/configurations/payment/` | 1 / 9 | 1 / 1 |
| 244034 | POST `api/v1/private/configurations/payment/` | 1 / 6 | 1 / 1 |
| 244036 | GET `api/v1/private/configurations/shipping/` | 1 / 9 | 1 / 1 |
| 244107 | GET `api/v1/private/modules/payment/` | 20 / 205 | 17 / 95 |
| 244108 | POST `api/v1/private/modules/payment/` | 22 / 284 | 17 / 215 |
| 244109 | GET `api/v1/private/modules/payment/{}/` | 22 / 211 | 17 / 105 |
| 244204 | GET `api/v1/private/modules/shipping/` | 20 / 200 | 17 / 91 |
| 244206 | POST `api/v1/private/modules/shipping/` | 22 / 284 | 17 / 186 |
| 244205 | GET `api/v1/private/modules/shipping/{}/` | 21 / 197 | 17 / 95 |
| 244013 | POST `services/private/system/module/` | 7 / 113 | 17 / 52 |

### Critical full call graphs

| Transaction | Full nodes | Links | Evidence |
|---:|---:|---:|---|
| 244057 | 146 | 235 | localized descriptions, content persistence, language/store reads |
| 244042 | 236 | 585 | file rename, provider-neutral manager, local/Infinispan/S3/GCP paths |
| 244108 | 284 | 679 | JSON configuration, validation, encryption, cache, payment discovery |
| 244206 | 284 | 621 | JSON configuration, validation, encryption, cache, shipping discovery |
| 244013 | 113 | 158 | module replacement, JSON mapping, loader, `config1`/`config2` handling |

## Source files to read

### Content API, facade, services, and repositories

- `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/api/v1/content/ContentApi.java`
- `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/api/v1/content/ContentAdministrationApi.java`
- `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/facade/content/ContentFacadeImpl.java`
- `initial-source/shopizer-3.2.7/sm-shop-model/src/main/java/com/salesmanager/shop/store/controller/content/facade/ContentFacade.java`
- `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/content/ContentService.java`
- `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/content/ContentServiceImpl.java`
- `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/repositories/content/ContentRepository.java`
- `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/repositories/content/ContentRepositoryCustom.java`
- `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/repositories/content/ContentRepositoryImpl.java`
- `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/repositories/content/PageContentRepository.java`

### Content domain and DTOs

- `initial-source/shopizer-3.2.7/sm-core-model/src/main/java/com/salesmanager/core/model/content/Content.java`
- `initial-source/shopizer-3.2.7/sm-core-model/src/main/java/com/salesmanager/core/model/content/ContentDescription.java`
- `initial-source/shopizer-3.2.7/sm-core-model/src/main/java/com/salesmanager/core/model/content/ContentFile.java`
- `initial-source/shopizer-3.2.7/sm-core-model/src/main/java/com/salesmanager/core/model/content/ContentPosition.java`
- `initial-source/shopizer-3.2.7/sm-core-model/src/main/java/com/salesmanager/core/model/content/ContentType.java`
- `initial-source/shopizer-3.2.7/sm-core-model/src/main/java/com/salesmanager/core/model/content/FileContentType.java`
- `initial-source/shopizer-3.2.7/sm-shop-model/src/main/java/com/salesmanager/shop/model/content/Content.java`
- `initial-source/shopizer-3.2.7/sm-shop-model/src/main/java/com/salesmanager/shop/model/content/PersistableContent.java`
- `initial-source/shopizer-3.2.7/sm-shop-model/src/main/java/com/salesmanager/shop/model/content/ReadableContent.java`
- `initial-source/shopizer-3.2.7/sm-shop-model/src/main/java/com/salesmanager/shop/model/content/common/Content.java`
- `initial-source/shopizer-3.2.7/sm-shop-model/src/main/java/com/salesmanager/shop/model/content/common/ContentDescription.java`
- `initial-source/shopizer-3.2.7/sm-shop-model/src/main/java/com/salesmanager/shop/model/content/page/ContentPage.java`
- `initial-source/shopizer-3.2.7/sm-shop-model/src/main/java/com/salesmanager/shop/model/content/page/PersistableContentPage.java`
- `initial-source/shopizer-3.2.7/sm-shop-model/src/main/java/com/salesmanager/shop/model/content/page/ReadableContentPage.java`
- `initial-source/shopizer-3.2.7/sm-shop-model/src/main/java/com/salesmanager/shop/model/content/box/ContentBox.java`
- `initial-source/shopizer-3.2.7/sm-shop-model/src/main/java/com/salesmanager/shop/model/content/box/PersistableContentBox.java`
- `initial-source/shopizer-3.2.7/sm-shop-model/src/main/java/com/salesmanager/shop/model/content/box/ReadableContentBox.java`

### Configuration and module logic

- `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/api/v1/system/PublicConfigsApi.java`
- `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/controller/system/MerchantConfigurationFacade.java`
- `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/controller/system/MerchantConfigurationFacadeImpl.java`
- `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/api/v1/configurations/ConfigurationsApi.java`
- `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/api/v1/payment/PaymentApi.java`
- `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/api/v1/shipping/ShippingConfigurationApi.java`
- `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/api/v0/system/SystemRESTController.java`
- `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/system/MerchantConfigurationService.java`
- `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/system/MerchantConfigurationServiceImpl.java`
- `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/system/ModuleConfigurationService.java`
- `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/system/ModuleConfigurationServiceImpl.java`
- `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/repositories/system/MerchantConfigurationRepository.java`
- `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/repositories/system/ModuleConfigurationRepository.java`
- `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/reference/loader/IntegrationModulesLoader.java`
- `initial-source/shopizer-3.2.7/sm-core-model/src/main/java/com/salesmanager/core/model/system/MerchantConfiguration.java`
- `initial-source/shopizer-3.2.7/sm-core-model/src/main/java/com/salesmanager/core/model/system/MerchantConfigurationType.java`
- `initial-source/shopizer-3.2.7/sm-core-model/src/main/java/com/salesmanager/core/model/system/MerchantConfig.java`
- `initial-source/shopizer-3.2.7/sm-core-model/src/main/java/com/salesmanager/core/model/system/IntegrationConfiguration.java`
- `initial-source/shopizer-3.2.7/sm-core-model/src/main/java/com/salesmanager/core/model/system/IntegrationModule.java`
- `initial-source/shopizer-3.2.7/sm-core-model/src/main/java/com/salesmanager/core/model/system/ModuleConfig.java`
- `initial-source/shopizer-3.2.7/sm-shop-model/src/main/java/com/salesmanager/shop/model/system/IntegrationModuleConfiguration.java`
- `initial-source/shopizer-3.2.7/sm-shop-model/src/main/java/com/salesmanager/shop/model/system/IntegrationModuleEntity.java`
- `initial-source/shopizer-3.2.7/sm-shop-model/src/main/java/com/salesmanager/shop/model/system/IntegrationModuleSummaryEntity.java`
- `initial-source/shopizer-3.2.7/sm-core/src/main/resources/reference/integrationmodules.json`
- `initial-source/shopizer-3.2.7/sm-core/src/main/resources/spring/shopizer-core-modules.xml`

### Provider-neutral content-file logic

- `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/modules/cms/content/ContentAssetsManager.java`
- `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/modules/cms/content/StaticContentFileManager.java`
- `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/modules/cms/content/StaticContentFileManagerImpl.java`
- `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/modules/cms/content/ContentImageGet.java`
- `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/modules/cms/content/ContentImageRemove.java`
- `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/modules/cms/impl/StaticContentCacheManagerImpl.java`

## Source files to skip or treat as context-only

- CMS provider adapters under `.../cms/content/aws/`, `.../gcp/`, `.../local/`,
  and `.../infinispan/`: infrastructure; inspect only for provider selection and
  failure semantics.
- Product media adapters under `.../cms/product/`: catalog/platform boundary.
- `CacheApi.java`, merchant logging services, and global system configuration:
  administration or cross-cutting infrastructure.
- Payment and shipping provider implementations: MS-06/MS-09/MS-12 ownership;
  inspect only for configuration validation contracts.
- `FilesController.java`: shared static-file serving surface.
- `MerchantStore.java` and `StoreFacadeImpl.java`: MS-10-owned lifecycle.
- CAST `<LISA>` JDK/library objects: external artifacts with no local source.

## Owned tables

| Table | CAST ID | Important columns | Evidence |
|---|---:|---|---|
| `content` | 437 | `content_id`, `code`, `content_position`, `content_type`, `link_to_menu`, `product_group`, `sort_order`, `visible`, `merchant_id`, audit timestamps | unique content/store metadata and merchant FK |
| `content_description` | 436 | `description_id`, `description`, `name`, `title`, SEO fields, `language_id`, `content_id`, audit timestamps | unique content/language metadata and language/content FKs |
| `merchant_configuration` | 414 | `merchant_config_id`, `active`, `config_key`, `type`, `value`, `merchant_id`, audit timestamps | merchant-scoped key/value configuration |
| `module_configuration` | 410 | `module_conf_id`, `code`, `details`, `configuration`, `custom_ind`, `image`, `module`, `regions`, `type`, audit timestamps | module configuration record |

Referenced but not owned: `merchant_store` (MS-10), `language`, `country`,
`currency`, and `geozone` (shared/reference), and `system_configuration`
(global/platform).

## Cross-service dependencies

| Direction | Dependency | Boundary |
|---|---|---|
| MS-11 -> MS-10 | store/tenant scope | persist opaque store IDs; do not write MS-10 tables |
| MS-11 -> shared reference data | localized content | language and jurisdiction lookup |
| MS-11 -> MS-12 | CMS storage providers | S3, GCP, local, and cache provider adapter boundary |
| MS-11 -> MS-12 | payment/shipping module validation | configuration flow invokes provider validation/discovery |
| MS-11 -> MS-03 | published-content reindexing | verify event/API contract during extraction |

## P1 rules requiring Phase 4 upgrade

`BR-MER-013` through `BR-MER-027` and `BR-EXT-021` require full source
re-extraction. Focus areas are store-scoped uniqueness, localized-description
fallback, visibility versus publication, file classification and rename
atomicity, merchant configuration serialization/encryption, public
configuration precedence, null configuration defects, module cache invalidation,
replacement-by-code, JSON/environment handling, `config2` compatibility, CMS
provider selection, and the MS-11/MS-12 content-file boundary.

## Fan-in/fan-out findings

- `buildDescriptions` (CAST 30117): fan-in 2, fan-out 34.
- `updateContentPage` (30129): fan-in 1, fan-out 20.
- `renameFile` (30127): fan-in 1, fan-out 15.
- `getIntegrationModules` (13381): fan-in 2, fan-out 61.
- `validateModuleConfiguration` (11669): complexity 13, provider-owned.
- `loadModule` (13191): complexity 17, fan-out 46.
- `createOrUpdateModule` (13382): fan-out 18.
- `getMerchantConfig` (13363): typed configuration deserialization.
- `toJSONString` (16220): shared serialization engine.
- `loadIntegrationConfigurations` (13186): fan-in 6, fan-out 24.

## Dead-code exclusions

CAST object `ModulesApi` (29894) had zero transactions in
`transactions_using_object` and is excluded as an unreachable candidate.
Provider managers are reachable infrastructure, not dead code: Infinispan
(11304) is used by 18 transactions, local (11325) by 15, S3 (11268) has seven
callers, and GCP (11290) has four. No broader application-wide dead-code claim
is made.

## Hidden-engine check

MS-11 is not CRUD-only. Content file operations have 230-270 full-graph
objects; page and box mutations invoke localized-description construction,
store/language resolution, conversion, and persistence orchestration. File
rename traverses multiple provider implementations. Payment/shipping module
mutations have 284 full-graph objects and maximum complexity 17, while module
discovery has fan-out 61 and loader fan-out 46. The hidden engine is
configuration/module discovery and provider validation shared by payment and
shipping contexts. MS-11 owns configuration state and content policy; MS-12
owns reusable adapter/provider execution.

## Source-resolution notes

- CAST `§{main_sources}§` resolves to `initial-source/shopizer-3.2.7/`.
- CAST responses sometimes concatenate multiple source paths with literal
  newlines; split and verify each path locally.
- `<LISA>` paths are analyzed libraries/JDK artifacts and must not be fabricated
  as local business source.
- Truncated CAST transaction start-point names should be resolved through the
  source-file list above.

## Scope summary

Thirty content/CMS transactions and eleven configuration/module transactions
cover four MS-11-owned tables, localized content, provider-backed file storage,
merchant-scoped configuration, module JSON/cache behavior, and the MS-12
integration boundary.
