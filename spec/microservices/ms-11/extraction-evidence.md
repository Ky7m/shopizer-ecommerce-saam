# MS-11 Content and Configuration — Extraction Evidence

## Extraction metadata

| Field | Value |
|---|---|
| Service | MS-11 — Content and Configuration |
| Analysis mode | Hybrid CAST transaction/data-graph analysis followed by direct source extraction |
| CAST application | `Shopizer-Backend` |
| CAST delivery | `Onboarding-202511171247` |
| CAST root mapping | `§{main_sources}§` → `initial-source/shopizer-3.2.7/` |
| Assessment brief | `assessment/ms-11-cast-brief.md` |
| Business-rules source | `spec/microservices/ms-11/01-business-rules.md` |
| Existing source files with direct rule evidence | 38 |
| Aggregate lines across existing source files | 9,134 |
| Missing referenced paths | 5 |
| Extractor deep-read coverage note | 11 files declared; the enumerated deep-read set is 11 when the provider-neutral `StaticContentFileManagerImpl.java` is included |
| Rules extracted | **41** |
| Rule ranges | `BR-MER-013`–`BR-MER-028` (16), `BR-CF-001`–`BR-CF-015` (15), `BR-EXT-021`–`BR-EXT-030` (10) |
| Semantic-preservation records | 41/41 |
| Vector dimensions per rule | Control-flow, data-flow, constants, state transitions, outcomes, data writes, integrations, and error paths |
| Vector status | **Complete for all 41 rules; six intentional target-policy GAPs are recorded in the rule preservation tables** |

## Source evidence table

The table includes source paths that have direct evidence references in the current MS-11 business-rules document. Coverage ranges are the extractor’s full or multi-pass business-logic ranges, or the targeted ranges explicitly cited by the extracted rules. No source read is inferred for the missing paths listed separately below.

| # | Source file | Lines | Full or multi-pass coverage | One-line purpose | Rules extracted | Vector status |
|---:|---|---:|---|---|---|---|
| 1 | `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/facade/content/ContentFacadeImpl.java` | 930 | Multi-pass: 75–258; 292–396; 414–520; 519–690; 693–768; 770–838; 840–930 | Content conversion, localized-description handling, content persistence, reads, deletion, file operations, rename, and download orchestration. | `BR-MER-013`–`BR-MER-021`, `BR-MER-022`, `BR-MER-025`, `BR-MER-027`, `BR-MER-028` | ✅ 8/8 dimensions for mapped rules |
| 2 | `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/repositories/content/ContentRepository.java` | 45 | Targeted: 8–18; 20–22; 32–34; 38–44 | Content lookup predicates for code, type, language, and merchant scope. | `BR-MER-013`, `BR-MER-016` | ✅ 8/8 |
| 3 | `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/api/v1/content/ContentApi.java` | 520 | Multi-pass: 86–191; 193–323; 322–379; 392–520 | Public and private content endpoints, mutation routes, upload routes, folder route, and deprecated operations. | `BR-MER-014`, `BR-MER-018`, `BR-MER-019`, `BR-MER-021`, `BR-MER-023`, `BR-MER-027` | ✅ 8/8 |
| 4 | `initial-source/shopizer-3.2.7/sm-core-model/src/main/java/com/salesmanager/core/model/content/ContentDescription.java` | 104 | Targeted: 20–76 | Persistent localized content-description fields and content/language relationships. | `BR-MER-015` | ✅ 8/8 |
| 5 | `initial-source/shopizer-3.2.7/sm-core-model/src/main/java/com/salesmanager/core/model/common/description/Description.java` | 113 | Targeted: 24–91 | Shared localized description fields inherited by content DTOs and entities. | `BR-MER-015` | ✅ 8/8 |
| 6 | `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/repositories/content/ContentRepositoryImpl.java` | 109 | Targeted: 42–76 | Merchant-scoped friendly-URL and visibility query behavior. | `BR-MER-017`, `BR-MER-018` | ✅ 8/8 |
| 7 | `initial-source/shopizer-3.2.7/sm-core-model/src/main/java/com/salesmanager/core/model/content/Content.java` | 196 | Targeted: 65–72; 130–151 | Content visibility, menu-linkage, type, code, and merchant metadata. | `BR-MER-018` | ✅ 8/8 |
| 8 | `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/repositories/content/PageContentRepository.java` | 26 | Targeted: 9–22 | Page-specific language, type, ordering, and pagination query definitions. | `BR-MER-019` | ✅ 8/8 |
| 9 | `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/content/ContentServiceImpl.java` | 542 | Two-pass: 58–275; 296–542 | Content persistence delegation, provider file routing, file deletion, folder operations, rename, file listing, and paginated reads. | `BR-MER-019`, `BR-MER-021`, `BR-MER-022`, `BR-MER-024`–`BR-MER-026`, `BR-MER-028`, `BR-EXT-029`, `BR-EXT-030` | ✅ 8/8 |
| 10 | `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/api/v1/content/ContentAdministrationApi.java` | 363 | Multi-pass: 83–162; 164–249; 261–363 | Administrative file listing, folder, upload, download, rename, removal, and response conversion. | `BR-MER-023`, `BR-MER-025`, `BR-MER-027`, `BR-MER-028`, `BR-EXT-030` | ✅ 8/8 |
| 11 | `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/modules/cms/content/ContentAssetsManager.java` | 83 | Targeted: 31–60 | Provider-neutral content-file namespace and asset-manager contract. | `BR-MER-024`, `BR-EXT-022` | ✅ 8/8 |
| 12 | `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/modules/cms/content/infinispan/CmsStaticContentFileManagerImpl.java` | 477 | Multi-pass: 108–233; 237–318; 329–470; 472–477 | Infinispan-backed file retrieval, storage, deletion, listing, folder behavior, and namespace construction. | `BR-MER-024`, `BR-MER-026`, `BR-EXT-023`, `BR-EXT-029` | ✅ 8/8 |
| 13 | `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/modules/cms/content/local/CmsStaticContentFileManagerImpl.java` | 484 | Multi-pass: 97–179; 180–368; 370–390; 399–480 | Local filesystem upload, retrieval, deletion, listing, folder behavior, and merchant path construction. | `BR-MER-026`, `BR-EXT-023` | ✅ 8/8 |
| 14 | `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/repositories/system/MerchantConfigurationRepository.java` | 25 | Targeted: 10–23 | Merchant- and configuration-key-based persistence queries. | `BR-CF-001` | ✅ 8/8 |
| 15 | `initial-source/shopizer-3.2.7/sm-core-model/src/main/java/com/salesmanager/core/model/system/MerchantConfiguration.java` | 139 | Targeted: 35–88 | `merchant_configuration` table mapping, merchant scope, key, type, value, and active state. | `BR-CF-001` | ✅ 8/8 |
| 16 | `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/system/MerchantConfigurationServiceImpl.java` | 112 | Targeted: 30–108 | Merchant configuration lookup, save/update behavior, typed configuration retrieval, and missing-state handling. | `BR-CF-001`, `BR-CF-002`, `BR-CF-015` | ✅ 8/8 |
| 17 | `initial-source/shopizer-3.2.7/sm-core-model/src/main/java/com/salesmanager/core/model/system/MerchantConfig.java` | 174 | Targeted: 18–70 | Typed merchant configuration fields and JSON serialization. | `BR-CF-002` | ✅ 8/8 |
| 18 | `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/controller/system/MerchantConfigurationFacadeImpl.java` | 101 | Targeted: 3–6; 37–91 | Public configuration projection, social-key lookups, property defaults, and configuration-value access. | `BR-CF-003`–`BR-CF-005`, `BR-CF-015` | ✅ 8/8 |
| 19 | `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/api/v1/system/PublicConfigsApi.java` | 51 | Full: 1–51 | Public configuration endpoint and response projection. | `BR-CF-003`, `BR-CF-004`, `BR-CF-015` | ✅ 8/8 |
| 20 | `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/payments/PaymentServiceImpl.java` | 784 | Targeted: 82–96; 162–250 | Payment module discovery, configured-module reads, decryption, provider validation, and configuration persistence boundary. | `BR-CF-006`, `BR-CF-007`, `BR-CF-012`, `BR-CF-013`, `BR-EXT-024`, `BR-EXT-025`, `BR-EXT-027` | ✅ 8/8 |
| 21 | `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/shipping/ShippingServiceImpl.java` | 962 | Targeted: 221–275 | Shipping module discovery, validation, and configuration persistence boundary. | `BR-CF-006`, `BR-CF-012`, `BR-CF-013`, `BR-EXT-025` | ✅ 8/8 |
| 22 | `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/reference/loader/ConfigurationModulesLoader.java` | 105 | Targeted: 50–103 | JSON configuration parsing, typed integration options, and the options-field defect. | `BR-CF-006`, `BR-CF-007`, `BR-EXT-027` | ✅ 8/8 |
| 23 | `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/modules/utils/EncryptionImpl.java` | 107 | Targeted: 23–61 | Encryption and decryption boundary for persisted payment and shipping configuration. | `BR-CF-006` | ✅ 8/8 |
| 24 | `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/api/v1/payment/PaymentApi.java` | 205 | Targeted: 83–124; 134–202 | Payment module configuration and detail endpoints, including provider-validation orchestration and redaction boundary. | `BR-CF-007`, `BR-CF-013`, `BR-EXT-025`, `BR-EXT-027` | ✅ 8/8 |
| 25 | `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/reference/loader/IntegrationModulesLoader.java` | 190 | Full: 1–190 | Reference integration-module loading, JSON mapping, regions, details, and TEST/PROD environment metadata. | `BR-CF-008`, `BR-CF-009`, `BR-EXT-028` | ✅ 8/8 |
| 26 | `initial-source/shopizer-3.2.7/sm-core-model/src/main/java/com/salesmanager/core/model/system/IntegrationModule.java` | 221 | Targeted: 24–83 | Persisted and transient integration-module metadata, configuration, regions, and environment fields. | `BR-CF-008` | ✅ 8/8 |
| 27 | `initial-source/shopizer-3.2.7/sm-core-model/src/main/java/com/salesmanager/core/model/system/ModuleConfig.java` | 56 | Targeted: 3–48 | Module environment configuration and TEST/PROD endpoint representation. | `BR-CF-008` | ✅ 8/8 |
| 28 | `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/system/ModuleConfigurationServiceImpl.java` | 187 | Full: 1–187 | Module discovery, cache hydration, payment-starter append behavior, lookup, and replacement. | `BR-CF-009`, `BR-CF-010`, `BR-CF-011`, `BR-EXT-024`, `BR-EXT-026` | ✅ 8/8 |
| 29 | `initial-source/shopizer-3.2.7/sm-core/src/main/resources/reference/integrationmodules.json` | 121 | Full: 1–121 | Reference payment and shipping module definitions, regions, details, and environment endpoints. | `BR-CF-009`, `BR-CF-012`, `BR-EXT-028` | ✅ 8/8 |
| 30 | `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/api/v0/system/SystemRESTController.java` | 156 | Targeted: 35–68 | Legacy module replacement endpoint and JSON hand-off. | `BR-CF-010` | ✅ 8/8 |
| 31 | `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/repositories/system/ModuleConfigurationRepository.java` | 16 | Targeted: 10–14 | Module-configuration lookup by module code. | `BR-CF-011` | ✅ 8/8 |
| 32 | `initial-source/shopizer-3.2.7/sm-core/src/main/resources/spring/shopizer-core-modules.xml` | 142 | Targeted: 1–61 | Payment and shipping module bean/map configuration. | `BR-CF-011` | ✅ 8/8 |
| 33 | `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/api/v1/shipping/ShippingConfigurationApi.java` | 321 | Targeted: 203–305 | Shipping module configuration, detail, and validation endpoints. | `BR-CF-013`, `BR-CF-014`, `BR-EXT-025`, `BR-EXT-027` | ✅ 8/8 |
| 34 | `initial-source/shopizer-3.2.7/sm-shop-model/src/main/java/com/salesmanager/shop/model/system/IntegrationModuleSummaryEntity.java` | 50 | Full: 1–59 | Module summary response fields distinguishing configured, active, image, and configurable state. | `BR-CF-014`, `BR-EXT-024` | ✅ 8/8 |
| 35 | `initial-source/shopizer-3.2.7/sm-core/src/main/resources/spring/shopizer-core-cms.xml` | 213 | Targeted: 20–26; 33–85; 91–161 | CMS provider selection property and provider bean wiring. | `BR-EXT-021` | ✅ 8/8 |
| 36 | `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/modules/cms/content/StaticContentFileManagerImpl.java` | 158 | Targeted: 33–85 | Provider-neutral delegation layer for content-file operations. | `BR-EXT-021` | ✅ 8/8 |
| 37 | `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/modules/cms/content/aws/S3StaticContentAssetsManagerImpl.java` | 322 | Two-pass: 59–153; 155–224; 234–322 | S3 object retrieval, listing, upload, deletion, bucket access, and unsupported folder operations. | `BR-EXT-022`, `BR-EXT-023`, `BR-EXT-029`, `BR-EXT-030` | ✅ 8/8 |
| 38 | `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/modules/cms/content/gcp/GCPStaticContentAssetsManagerImpl.java` | 224 | Full: 1–224 | GCP object retrieval, listing, upload, deletion, and unsupported folder operations. | `BR-EXT-022`, `BR-EXT-023`, `BR-EXT-030` | ✅ 8/8 |

## Historical path-correction and search record

The following repository-relative paths are referenced by the current rules or extraction trail but do not exist at those exact locations. They are not counted as successfully read source files.

| Historical path | Search result | Status |
|---|---|---|
| `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/model/content/common/ContentDescription.java` | Repository search found `initial-source/shopizer-3.2.7/sm-shop-model/src/main/java/com/salesmanager/shop/model/content/common/ContentDescription.java`. | Corrected in the rule package. |
| `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/model/content/page/ReadableContentPage.java` | Repository search found `initial-source/shopizer-3.2.7/sm-shop-model/src/main/java/com/salesmanager/shop/model/content/page/ReadableContentPage.java`. | Corrected in the rule package. |
| `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/model/system/Configs.java` | Repository search found `initial-source/shopizer-3.2.7/sm-shop-model/src/main/java/com/salesmanager/shop/model/system/Configs.java`. | Corrected in the rule package. |
| `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/api/v1/system/PublicConfigsApi.java` | Repository search found `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/api/v1/system/PublicConfigsApi.java`. | Corrected in the rule package. |
| `initial-source/com/salesmanager/shop/store/api/v1/payment/PaymentApi.java` | Repository search found `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/api/v1/payment/PaymentApi.java`. | Corrected in the rule package. |

No unresolved source path remains in the current rule package. No `<LISA>` library/JDK path is treated as a local source read. Provider examples named only by class filename, including `StripePayment.java` and `USPSShippingQuote.java`, are retained as boundary context and are not counted as independently read MS-11 source files because no repository-relative extractor path was recorded for them.

## Rules extracted

### Content and CMS rules

| Rule ID | Rule |
|---|---|
| `BR-MER-013` | Content codes are unique within a merchant store. |
| `BR-MER-014` | Page and box operations assign their content type from the operation. |
| `BR-MER-015` | Localized descriptions are upserted by language code during content mutation. |
| `BR-MER-016` | Language-specific and all-language reads use different projections. |
| `BR-MER-017` | Public friendly-URL lookup exposes only visible content. |
| `BR-MER-018` | Visibility and menu linkage are independent content policies. |
| `BR-MER-019` | Content lists are merchant-scoped, type-scoped, ordered, and paginated. |
| `BR-MER-020` | Localized content projection preserves domain fields but applies endpoint-specific formatting. |
| `BR-MER-021` | Content deletion is restricted to the owning merchant store. |
| `BR-MER-022` | Uploaded files are classified by the MIME-type major component. |
| `BR-MER-023` | File-manager image uploads validate the submitted filename before storage. |
| `BR-MER-024` | Content files are isolated by merchant and file-content type. |
| `BR-MER-025` | File rename is a read-remove-recreate sequence and is not atomic. |
| `BR-MER-026` | Folder paths use Linux-style directory syntax, but folder enumeration and deletion are incomplete. |
| `BR-MER-027` | Legacy download and folder controller operations contain explicit nonfunctional behavior. |
| `BR-MER-028` | Image listings are store-scoped and expose generated static-image paths. |

### Merchant configuration and module rules

| Rule ID | Rule |
|---|---|
| `BR-CF-001` | Merchant configuration records are keyed by merchant store and configuration key. |
| `BR-CF-002` | Merchant configuration JSON uses typed flags and language-keyed search settings. |
| `BR-CF-003` | Public configuration projects selected merchant flags into the public response. |
| `BR-CF-004` | Public social values are resolved by named merchant configuration keys. |
| `BR-CF-005` | Shipping display is controlled by a platform property and defaults to false. |
| `BR-CF-006` | Payment and shipping module configurations are decrypted before parsing and encrypted before persistence. |
| `BR-CF-007` | Integration configuration parsing has an options-field defect. |
| `BR-CF-008` | Integration-module metadata preserves module, region, detail, and environment configuration. |
| `BR-CF-009` | Environment configuration distinguishes TEST and PROD and exposes a `config2` compatibility defect. |
| `BR-CF-010` | Module replacement is performed by code, not by module family. |
| `BR-CF-011` | Module discovery hydrates cached metadata and appends payment starters. |
| `BR-CF-012` | Provider module availability is filtered by merchant-store country or wildcard region. |
| `BR-CF-013` | Provider configuration must validate against the selected provider before persistence. |
| `BR-CF-014` | Module summary responses distinguish configured from active. |
| `BR-CF-015` | Missing merchant configuration is a distinct state and must not become an implicit public configuration. |

### Extensibility and provider-boundary rules

| Rule ID | Rule |
|---|---|
| `BR-EXT-021` | CMS provider selection is configuration-driven and has no automatic fallback. |
| `BR-EXT-022` | Provider object keys preserve merchant and content-type namespaces. |
| `BR-EXT-023` | Provider capabilities and failure semantics must be explicit. |
| `BR-EXT-024` | Runtime payment starters extend discovered payment metadata. |
| `BR-EXT-025` | Target configuration storage must separate configuration state from provider execution. |
| `BR-EXT-026` | Module-discovery cache requires invalidation after module replacement. |
| `BR-EXT-027` | Public module-detail reads must not expose encrypted merchant configuration. |
| `BR-EXT-028` | Reference module definitions support wildcard and provider-specific environment endpoints. |
| `BR-EXT-029` | File rename must preserve MIME metadata across provider boundaries. |
| `BR-EXT-030` | Provider-backed file deletion must be scoped and idempotency must be explicit. |

## CAST transaction references

### Content and CMS transactions

| CAST ID | Operation | Reduced/full graph size | Complexity max/sum |
|---:|---|---:|---:|
| `244047` | `GET api/v1/content/boxes/` | 13 / 132 | 3 / 44 |
| `244062` | `GET api/v1/content/boxes/{}/` | 15 / 121 | 5 / 45 |
| `244063` | `DELETE api/v1/content/folder/` | 1 / 6 | 1 / 1 |
| `244064` | `GET api/v1/content/images/` | 9 / 252 | 9 / 99 |
| `244040` | `GET api/v1/content/images/download/` | 1 / 17 | 3 / 6 |
| `244045` | `GET api/v1/content/pages/` | 13 / 134 | 3 / 44 |
| `244050` | `GET api/v1/content/pages/name/{}/` | 1 / 87 | 3 / 36 |
| `244049` | `GET api/v1/content/pages/{}/` | 15 / 116 | 4 / 42 |
| `244046` | `GET api/v1/content/summary/` | 1 / 9 | 1 / 1 |
| `244069` | `DELETE api/v1/private/content/` | 11 / 152 | 4 / 58 |
| `244059` | `GET api/v1/private/content/any/{}/` | 13 / 114 | 6 / 52 |
| `244051` | `POST api/v1/private/content/box/` | 18 / 141 | 6 / 60 |
| `244056` | `DELETE api/v1/private/content/box/{}/` | 14 / 63 | 5 / 21 |
| `244058` | `PUT api/v1/private/content/box/{}/` | 18 / 142 | 6 / 62 |
| `244052` | `GET api/v1/private/content/box/{}/exists/` | 13 / 36 | 1 / 5 |
| `244048` | `GET api/v1/private/content/boxes/` | 13 / 132 | 3 / 44 |
| `244061` | `GET api/v1/private/content/boxes/{}/` | 15 / 121 | 5 / 45 |
| `244038` | `GET api/v1/private/content/folder/` | 9 / 270 | 9 / 104 |
| `244039` | `POST api/v1/private/content/images/add/` | 14 / 230 | 7 / 102 |
| `244043` | `DELETE api/v1/private/content/images/remove/` | 11 / 154 | 4 / 60 |
| `244042` | `POST api/v1/private/content/images/rename/` | 23 / 236 | 5 / 101 |
| `244037` | `GET api/v1/private/content/list/` | 9 / 270 | 9 / 112 |
| `244054` | `POST api/v1/private/content/page/` | 18 / 145 | 6 / 61 |
| `244055` | `DELETE api/v1/private/content/page/{}/` | 14 / 63 | 5 / 21 |
| `244057` | `PUT api/v1/private/content/page/{}/` | 18 / 146 | 6 / 63 |
| `244053` | `GET api/v1/private/content/page/{}/exists/` | 13 / 36 | 1 / 5 |
| `244068` | `DELETE api/v1/private/content/{}/` | 14 / 62 | 5 / 21 |
| `244067` | `PUT api/v1/private/content/{}/` | 1 / 11 | 1 / 2 |
| `244060` | `GET api/v1/private/contents/any/` | 13 / 118 | 3 / 42 |

### Configuration and module transactions

| CAST ID | Operation | Reduced/full graph size | Complexity max/sum |
|---:|---|---:|---:|
| `244237` | `GET api/v1/config/` | 17 / 107 | 4 / 43 |
| `244035` | `GET api/v1/private/configurations/payment/` | 1 / 9 | 1 / 1 |
| `244034` | `POST api/v1/private/configurations/payment/` | 1 / 6 | 1 / 1 |
| `244036` | `GET api/v1/private/configurations/shipping/` | 1 / 9 | 1 / 1 |
| `244107` | `GET api/v1/private/modules/payment/` | 20 / 205 | 17 / 95 |
| `244108` | `POST api/v1/private/modules/payment/` | 22 / 284 | 17 / 215 |
| `244109` | `GET api/v1/private/modules/payment/{}/` | 22 / 211 | 17 / 105 |
| `244204` | `GET api/v1/private/modules/shipping/` | 20 / 200 | 17 / 91 |
| `244206` | `POST api/v1/private/modules/shipping/` | 22 / 284 | 17 / 186 |
| `244205` | `GET api/v1/private/modules/shipping/{}/` | 21 / 197 | 17 / 95 |
| `244013` | `POST services/private/system/module/` | 7 / 113 | 17 / 52 |

### Critical full transaction graphs

| Transaction | Full nodes | Links | Evidence captured |
|---:|---:|---:|---|
| `244057` | 146 | 235 | Localized descriptions, content persistence, language and store reads. |
| `244042` | 236 | 585 | File rename, provider-neutral manager, and local/Infinispan/S3/GCP paths. |
| `244108` | 284 | 679 | JSON configuration, validation, encryption, cache, and payment discovery. |
| `244206` | 284 | 621 | JSON configuration, validation, encryption, cache, and shipping discovery. |
| `244013` | 113 | 158 | Module replacement, JSON mapping, loader, and `config1`/`config2` handling. |

## CAST method and call-graph references

| CAST method/object | Evidence |
|---|---|
| `30117 buildDescriptions` | Localized-description construction; fan-in 2 and fan-out 34. |
| `30129 updateContentPage` | Content update orchestration; fan-in 1 and fan-out 20. |
| `30127 renameFile` | Provider-neutral rename orchestration; fan-in 1 and fan-out 15. |
| `13381 getIntegrationModules` | Module discovery; fan-in 2 and fan-out 61. |
| `11669 validateModuleConfiguration` | Provider-owned validation boundary; complexity 13. |
| `13191 loadModule` | Reference module loading; complexity 17 and fan-out 46. |
| `13382 createOrUpdateModule` | Module replacement and persistence; fan-out 18. |
| `13363 getMerchantConfig` | Typed merchant-configuration deserialization. |
| `16220 toJSONString` | Shared merchant-configuration serialization engine. |
| `13186 loadIntegrationConfigurations` | Integration configuration parsing; fan-in 6 and fan-out 24. |

## CAST data-graph and database references

### MS-11-owned tables

| Table | CAST object/data-graph ID | Relevant fields | Evidence |
|---|---:|---|---|
| `content` | `437` | `content_id`, `code`, `content_position`, `content_type`, `link_to_menu`, `product_group`, `sort_order`, `visible`, `merchant_id`, audit timestamps | Content/store metadata, merchant foreign-key scope, uniqueness, visibility, type, ordering, and menu-linkage behavior. |
| `content_description` | `436` | `description_id`, `description`, `name`, `title`, SEO fields, `language_id`, `content_id`, audit timestamps | Content/language uniqueness and localized description persistence. |
| `merchant_configuration` | `414` | `merchant_config_id`, `active`, `config_key`, `type`, `value`, `merchant_id`, audit timestamps | Merchant-scoped key/value configuration and active-state behavior. |
| `module_configuration` | `410` | `module_conf_id`, `code`, `details`, `configuration`, `custom_ind`, `image`, `module`, `regions`, `type`, audit timestamps | Integration-module metadata, configuration payload, region availability, and module summary state. |

### Referenced data boundaries

- `merchant_store`: MS-10-owned store/tenant scope; MS-11 persists opaque store identifiers and does not write the MS-10 table.
- `language`: shared reference data used for localized content lookup and mutation.
- `country`, `currency`, and `geozone`: shared/reference data.
- `system_configuration`: global/platform configuration; not an MS-11-owned table.
- Provider object paths: CAST data-graph inspection confirmed merchant and content-type namespaces for local, Infinispan, S3, and GCP storage. The brief does not record separate numeric data-graph IDs for those provider object graphs, so none are fabricated here.

## Hidden-engine findings

MS-11 is not CRUD-only.

- Content file operations contain 230–270 full-graph objects and reach provider-neutral routing, file classification, path construction, storage, listing, deletion, and folder behavior.
- Page and box mutations invoke localized-description construction, language and store resolution, DTO conversion, and persistence orchestration.
- File rename traverses multiple provider implementations and performs a read-remove-recreate sequence rather than an atomic move.
- Payment and shipping module mutations each contain 284 full-graph objects with maximum complexity 17.
- Module discovery has fan-out 61, while reference-module loading has fan-out 46.
- The principal hidden engine is configuration/module discovery and provider validation shared by payment and shipping contexts.
- MS-11 owns configuration state, content policy, metadata, and orchestration boundaries.
- MS-12 owns reusable provider and adapter execution; MS-11 must not perform payment charges, shipping quotes, or provider-owned operational persistence.

## CAST transaction reachability and dead-code findings

- CAST object `ModulesApi` (`29894`) had zero transactions in `transactions_using_object` and is excluded as an unreachable candidate.
- Provider managers are reachable infrastructure, not dead code:
  - Infinispan object `11304`: used by 18 transactions.
  - Local provider object `11325`: used by 15 transactions.
  - S3 provider object `11268`: seven callers.
  - GCP provider object `11290`: four callers.
- No broader application-wide dead-code claim is made.

## Excluded and context-only components

The following components were not assigned independent MS-11 business rules or were treated as boundary/context evidence:

| Component or source family | Treatment |
|---|---|
| CMS provider adapters under `.../cms/content/aws/`, `.../gcp/`, `.../local/`, and `.../infinispan/` | Read only where provider selection, namespace, capability, or failure semantics affect the MS-11 contract; infrastructure execution remains outside the service’s domain rules. |
| Product media adapters under `.../cms/product/` | Catalog/platform boundary; not MS-11-owned content behavior. |
| `CacheApi.java` | Cross-cutting administration/infrastructure. |
| Merchant logging services | Cross-cutting infrastructure. |
| Global system configuration | Platform-level configuration, not merchant content/configuration state. |
| Payment and shipping provider implementations | MS-06/MS-09/MS-12 ownership; only provider-validation/configuration boundaries are retained for MS-11. |
| `FilesController.java` | Shared static-file serving surface. |
| `MerchantStore.java` and `StoreFacadeImpl.java` | MS-10-owned store lifecycle and store administration. |
| CAST `<LISA>` JDK/library objects | External analyzed artifacts; no local business source was fabricated. |
| `ModulesApi` (`29894`) | Unreachable CAST candidate excluded from MS-11 extraction. |
| `merchant_store` and other MS-10 tables | Referenced only for opaque tenant/store scope; no MS-11 write ownership. |
| Payment charge, shipping quote, and provider-operational tables | Explicitly outside MS-11 ownership. |

## Boundary and dependency findings

| Direction | Dependency | Boundary established |
|---|---|---|
| MS-11 → MS-10 | Store/tenant scope | Consume and persist opaque store identifiers; do not write MS-10 lifecycle tables. |
| MS-11 → shared reference data | Localization and jurisdiction | Resolve language and jurisdiction references through shared contracts. |
| MS-11 → MS-12 | CMS storage providers | S3, GCP, local, cache, and provider-adapter execution boundary. |
| MS-11 → MS-12 | Payment/shipping validation | Invoke provider validation/discovery before saving configuration state. |
| MS-11 → MS-03 | Published-content indexing | Verify publication/reindex event or API contract during implementation. |

## Extraction status

- **Rules extracted:** 41.
- **Content rules:** 16, `BR-MER-013` through `BR-MER-028`.
- **Configuration rules:** 15, `BR-CF-001` through `BR-CF-015`.
- **Extensibility/provider rules:** 10, `BR-EXT-021` through `BR-EXT-030`.
- **Semantic-preservation tables:** 41/41.
- **Control-flow vectors:** 41/41.
- **Data-flow vectors:** 41/41.
- **Constants vectors:** 41/41.
- **State-transition vectors:** 41/41.
- **Outcome vectors:** 41/41.
- **Data-write vectors:** 41/41.
- **Integration vectors:** 41/41.
- **Error-path vectors:** 41/41.
- **Existing source files with direct rule evidence:** 38.
- **Aggregate lines across those existing files:** 9,134.
- **Missing referenced paths recorded:** 5.
- **Critical CAST full graphs covered:** transactions `244057`, `244042`, `244108`, `244206`, and `244013`.
- **Owned CAST data-graph/table references covered:** `content` (`437`), `content_description` (`436`), `merchant_configuration` (`414`), and `module_configuration` (`410`).
- **Provider-backed file behavior:** covered for local, Infinispan, S3, and GCP.
- **Hidden-engine conclusion:** configuration/module discovery, provider validation, localized-content orchestration, and provider-backed file operations require explicit target-service behavior; MS-11 is not a CRUD-only service.
- **Source-resolution caveat:** CAST responses that concatenated paths with literal newlines were split and verified where possible; missing and corrected paths remain explicitly recorded rather than silently treated as successful reads.