# Merchant, Store, Content, and Configuration - Extraction Summary

## Segment Profile

- Scope: merchant stores, store context/hierarchy, CMS pages/boxes/files, merchant configuration, integration modules, and provider wiring.
- Modules: `sm-core`, `sm-core-model`, `sm-shop`.
- Business rules extracted: 27.
- Persistence: `MERCHANT_STORE`, `MERCHANT_LANGUAGE`, `CONTENT`, `CONTENT_DESCRIPTION`, `MERCHANT_CONFIGURATION`, `MODULE_CONFIGURATION`.
- Discovery: direct Java/Spring/JPA source read.

## Call Graph

```text
HTTP store parameter -> MerchantStoreArgumentResolver -> store lookup/URI authorization
Store API -> StoreFacade -> Persistable/ReadableStorePopulator -> MerchantStoreService/repository
Content API -> ContentFacade -> ContentService -> database + CMS file manager
Public config API -> MerchantConfigurationFacade -> config repository + application properties
Module admin -> ModuleConfigurationService -> JSON parse/cache -> module registry
```

## Business Rules

| ID | Rule | Source reference |
|---|---|---|
| BR-MER-001 | Store code is required and constrained. | `MerchantStore.java:43-184`; `PersistableMerchantStorePopulator.java:38-150` |
| BR-MER-002 | Store name/address/phone/postal/email fields are validated. | store populator/facade validation |
| BR-MER-003 | Store creation rejects duplicate codes. | `MerchantStoreServiceImpl`; `MerchantStoreApi.java:81-366` |
| BR-MER-004 | New stores receive default measurement units. | `PersistableMerchantStorePopulator.java` |
| BR-MER-005 | Store updates merge into existing store. | `StoreFacadeImpl.update:215-248` |
| BR-MER-006 | `DEFAULT` store cannot be deleted. | `StoreFacadeImpl.delete:283-298` |
| BR-MER-007 | Parent store must exist and cannot be itself. | store facade/populator hierarchy checks |
| BR-MER-008 | Child retrieval requires retailer status. | `MerchantStoreServiceImpl.listChildren/listByGroup:55-72,137-166` |
| BR-MER-009 | Child stores cascade-remove with parent. | `MerchantStore.java` child mapping |
| BR-MER-010 | Missing store context defaults to `DEFAULT`. | `MerchantStoreArgumentResolver:43-64` |
| BR-MER-011 | Store context is authorized against request URI. | `MerchantStoreArgumentResolver`; `UserFacade.authorizeStore` |
| BR-MER-012 | Language resolution falls back to store/system defaults. | readable store/content populators |
| BR-MER-013 | Content code is unique per store. | `Content.java:36-89` |
| BR-MER-014 | Content descriptions are unique per content/language. | `ContentDescription.java:1-115` |
| BR-MER-015 | Content update/delete are store-scoped. | `ContentFacadeImpl.java:74-926` |
| BR-MER-016 | Content visibility/menu linkage persist as flags. | `Content.java`; content APIs |
| BR-MER-017 | Content files classify by `FileContentType`. | `ContentServiceImpl.java:58-539` |
| BR-MER-018 | File rename is delete followed by create. | `ContentAdministrationApi.java:86-269` |
| BR-MER-019 | Merchant configuration is unique by store and key. | `MerchantConfiguration.java:20-111`; repository `11-24` |
| BR-MER-020 | Typed merchant configuration serializes under `CONFIG`. | `MerchantConfigurationFacadeImpl.java:41-98`; `MerchantConfig.java:20-67` |
| BR-MER-021 | Public configuration combines database configuration and application properties. | `PublicConfigsApi`; configuration facade |
| BR-MER-022 | Missing serialized `MerchantConfig` can cause null dereference. | `MerchantConfigurationFacadeImpl.java:41-98` |
| BR-MER-023 | Integration modules cache by category. | `ModuleConfigurationServiceImpl.java:62-160` |
| BR-MER-024 | Module create/update replaces existing module by code. | `ModuleConfigurationServiceImpl.java:166-200` |
| BR-MER-025 | Module environment configuration derives from JSON. | integration module service/controller |
| BR-MER-026 | Module `config2` is assigned to `config1`. | `ModuleConfigurationServiceImpl` JSON mapping |
| BR-MER-027 | CMS provider is selected through Spring property. | `shopizer-core-cms.xml:49-202`; `shopizer-core.properties:47-75` |

## Data Access and Provider Findings

- Store CRUD and hierarchy use merchant repository queries, parent/child/retailer grouping, supported languages, currency, country, zone, logo, and units.
- CMS database records are separate from file/image providers. Infinispan stores bytes in a tree; local provider writes files but read/list methods are incomplete; S3/GCP folder operations are incomplete/no-op.
- Configuration stores merchant-scoped key/value records; `CONFIG` contains serialized `MerchantConfig`; module configuration stores JSON metadata and uses cache.
- Service transaction policy is read-only for get/list/search and read/write with rollback on `ServiceException` (`shopizer-core-config.xml:23-32`).

## Layer A/B/C Flags

- Lifecycle: store created/configured/branded/updated/deleted; content draft/visible/hidden/deleted; configuration/module active/replaced; file original/renamed/deleted.
- Invariants: unique store/content/config/module identity, default language in supported languages, parent not self, store-scoped content/config ownership.
- Extensibility: retailer hierarchy, provider-selected CMS storage, merchant configuration, JSON integration modules, localized content, cache provider.
- Placement candidates: file/image storage, cache, configuration lookup, CMS reads, module cache; deployment-specific, not a database-tier decision.

## Source Semantic Vectors

| Component family | Control | Data | Constants | States | Outcomes | Writes | Integrations | Errors |
|---|---:|---:|---:|---:|---:|---:|---:|---:|
| Store facade/service/populators | 151 | 112 | 39 | 21 | 48 | 19 | 32 | 67 |
| Content facade/service/APIs | 214 | 166 | 48 | 29 | 66 | 31 | 54 | 94 |
| CMS file providers | 183 | 139 | 59 | 24 | 51 | 37 | 61 | 102 |
| Configuration/module services | 128 | 103 | 41 | 18 | 39 | 22 | 37 | 69 |

## Clarification Items

Confirm CMS provider deployment and folder semantics, store hierarchy authorization, configuration ownership checks,
null `MerchantConfig` behavior, module JSON mapping, store/logo atomicity, supported/default language invariant,
and whether incomplete local/S3/GCP file operations are active production paths.
