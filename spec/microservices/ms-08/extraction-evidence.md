# MS-08 Tax Extraction Evidence

**Extraction date:** 2026-09-01  
**Service:** MS-08 Tax  
**CAST application:** `Shopizer-Backend`  
**Analysis mode:** Hybrid  
**Local CAST root mapping:** `§{main_sources}§` → `initial-source/shopizer-3.2.7/`

## CAST scope

### Application inventory

| Metric | Value |
|---|---:|
| CAST LOC | 94,528 |
| CAST elements | 16,269 |
| CAST interactions | 72,033 |
| Pricing/tax/shipping segment components | 101 |
| Backend Tax-name/path subset | 24 |
| Backend Tax business-layer components | 6 |
| Backend Tax infrastructure/context components | 18 |
| Existing P1 Tax component marked extracted | 1 |
| Live Tax transaction records | 12 |
| Live Tax data graphs | 8 |
| Local source-to-table edges | 0 |

### CAST transaction scope

| Transaction ID | Operation | Reduced nodes | Full nodes |
|---:|---|---:|---:|
| 244002 | GET `api/v1/private/tax/class/` | 13 | 85 |
| 243999 | POST `api/v1/private/tax/class/` | 14 | 97 |
| 244000 | GET `api/v1/private/tax/class/unique/` | 13 | 49 |
| 244004 | DELETE `api/v1/private/tax/class/{}/` | 1 | 59 |
| 244003 | GET `api/v1/private/tax/class/{}/` | 13 | 78 |
| 244001 | PUT `api/v1/private/tax/class/{}/` | 1 | 87 |
| 244156 | POST `api/v1/private/tax/rate/` | 31 | 189 |
| 244238 | GET `api/v1/private/tax/rate/unique/` | 19 | 66 |
| 244242 | DELETE `api/v1/private/tax/rate/{}/` | 19 | 67 |
| 244241 | GET `api/v1/private/tax/rate/{}/` | 19 | 139 |
| 244239 | PUT `api/v1/private/tax/rate/{}/` | 31 | 186 |
| 244240 | GET `api/v1/private/tax/rates/` | 18 | 150 |

`TaxService` is used by 21 transactions, including cart/checkout total and product-price flows. No dedicated live calculation transaction detail was available, so calculation evidence uses CAST object `13426` plus direct caller search.

### CAST data-graph scope

| Data graph ID | Start entity/operation | Size |
|---:|---|---:|
| 243910 | `api/v1/private/tax/class/` | 1590 |
| 243915 | `api/v1/private/tax/class/` | 1650 |
| 243952 | `api/v1/private/tax/class/` | 1637 |
| 243954 | `api/v1/private/tax/class/` | 1652 |
| 243962 | `api/v1/private/tax/class/` | 1598 |
| 243924 | `api/v1/private/tax/rate/` | 423 |
| 243919 | `salesmanager.tax_class` | 587 |
| 243926 | `salesmanager.tax_rate_description` | 1 |

### CAST component scope

| Component | CAST ID | CAST control | Outcomes | Writes | Errors |
|---|---:|---:|---:|---:|---:|
| `TaxFacadeImpl` | 20996 | 81 | 35 | 3 | 77 |
| `TaxServiceImpl` | 13426 | 52 | 10 | 0 | 4 |
| `TaxClassApi` | 29900 | 3 | 9 | 2 | 0 |
| `TaxRatesApi` | 29770 | 3 | 9 | 2 | 0 |
| `TaxClassServiceImpl` | 13397 | 3 | 6 | 4 | 1 |
| `TaxRateServiceImpl` | 13411 | 3 | 7 | 4 | 1 |
| **Total** |  | **145** | **76** | **15** | **83** |

CAST did not provide populated cyclomatic fields or complete source-to-table vectors. Direct source reading was therefore required for semantic vectors and ownership.

## Mandatory CAST-identified files directly read

All six CAST-identified business files were directly read.

| File | LOC | Sections read | Evidence |
|---|---:|---:|---|
| `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/facade/tax/TaxFacadeImpl.java` | 354 | 1 | Lines 1-354: tax-class/rate validation, ownership checks, mapping, CRUD orchestration, exception translation |
| `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/tax/TaxServiceImpl.java` | 302 | 1 | Lines 1-302: configuration, jurisdiction basis, province/country policy, grouping, shipping, rate application, consolidation |
| `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/api/v1/tax/TaxClassApi.java` | 122 | 1 | Lines 1-122: tax-class REST routes, request parameters, body DTO, facade delegation |
| `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/api/v1/tax/TaxRatesApi.java` | 122 | 1 | Lines 1-122: tax-rate REST routes, request parameters, body DTO, facade delegation |
| `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/tax/TaxClassServiceImpl.java` | 78 | 1 | Lines 1-78: store listing, code lookup, ownership-scoped existence, delete, save/update |
| `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/tax/TaxRateServiceImpl.java` | 84 | 1 | Lines 1-84: store/language listing, code/id lookup, geographic lookup delegation, delete, save/update |

No mandatory source file exceeded 500 LOC. Therefore, the required single-pass full-read protocol applied; no multi-pass file was required.

## Mandatory P1-confirmed repository/service/facade contract files directly read

| File | LOC | Sections read | Evidence |
|---|---:|---:|---|
| `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/repositories/tax/TaxClassRepository.java` | 22 | 1 | Lines 1-22: store, global-code, and store/code JPQL queries |
| `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/repositories/tax/TaxRateRepository.java` | 34 | 1 | Lines 1-34: store, language, code, ID, zone, and province queries; missing tax-class predicate confirmed |
| `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/tax/TaxService.java` | 47 | 1 | Lines 1-47: configuration and calculation service contract |
| `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/tax/TaxClassService.java` | 24 | 1 | Lines 1-24: tax-class service contract |
| `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/tax/TaxRateService.java` | 39 | 1 | Lines 1-39: tax-rate service and geographic lookup contract |
| `initial-source/shopizer-3.2.7/sm-shop-model/src/main/java/com/salesmanager/shop/store/controller/tax/facade/TaxFacade.java` | 36 | 1 | Lines 1-36: tax-class/rate facade operation contract |

The CAST brief listed the facade contract under `sm-shop/src/main/...`; that path did not exist. The actual declaration was resolved and read at `sm-shop-model/src/main/.../TaxFacade.java`.

## Relevant model, entity, DTO, and mapper files read

These files were read only to resolve exact fields, persistence mappings, serialization, and API shapes.

| File | LOC | Sections read | Use |
|---|---:|---:|---|
| `sm-core-model/.../tax/taxclass/TaxClass.java` | 121 | 1 | `TAX_CLASS` fields, unique constraint, store relation |
| `sm-core-model/.../tax/taxrate/TaxRate.java` | 251 | 1 | `TAX_RATE` fields, precision, geographic fields, parent, compound flag |
| `sm-core-model/.../tax/taxrate/TaxRateDescription.java` | 57 | 1 | `TAX_RATE_DESCRIPTION` relation and language uniqueness |
| `sm-core-model/.../tax/TaxConfiguration.java` | 53 | 1 | Basis enum and serialization omission |
| `sm-core-model/.../tax/TaxItem.java` | 32 | 1 | Result label, item price, and rate reference |
| `sm-core-model/.../tax/TaxBasisCalculation.java` | 7 | 1 | `STOREADDRESS`, `SHIPPINGADDRESS`, `BILLINGADDRESS` values |
| `sm-shop-model/.../tax/TaxClassEntity.java` | 36 | 1 | API class fields and code size validation |
| `sm-shop-model/.../tax/TaxRateEntity.java` | 26 | 1 | API rate ID, priority, and code |
| `sm-shop-model/.../tax/PersistableTaxClass.java` | 10 | 1 | Request inheritance |
| `sm-shop-model/.../tax/PersistableTaxRate.java` | 57 | 1 | Request rate, store, zone, country, class, descriptions |
| `sm-shop-model/.../tax/ReadableTaxClass.java` | 10 | 1 | Response inheritance |
| `sm-shop-model/.../tax/ReadableTaxRate.java` | 55 | 1 | Response fields and localized description |
| `sm-shop-model/.../tax/ReadableTaxRateDescription.java` | 12 | 1 | Localized response fields |
| `sm-shop-model/.../tax/ReadableTaxRateFull.java` | 22 | 1 | Full-description response shape |
| `sm-shop-model/.../tax/TaxRateDescription.java` | 12 | 1 | Description request shape |
| `sm-shop-model/.../entity/Entity.java` | 24 | 1 | Legacy numeric ID wrapper |
| `sm-shop-model/.../catalog/NamedEntity.java` | 67 | 1 | Localized name/description inheritance |
| `sm-core-model/.../order/OrderTotalItem.java` | 27 | 1 | Tax item amount and item-code inheritance |
| `sm-shop/.../mapper/tax/PersistableTaxClassMapper.java` | 42 | 1 | Request-to-domain class mapping |
| `sm-shop/.../mapper/tax/PersistableTaxRateMapper.java` | 121 | 1 | Request-to-domain rate mapping and state/province defect |
| `sm-shop/.../mapper/tax/ReadableTaxClassMapper.java` | 34 | 1 | Domain-to-response class mapping |
| `sm-shop/.../mapper/tax/ReadableTaxRateMapper.java` | 72 | 1 | Domain-to-response rate mapping and nullable-zone defect |

## Direct caller evidence

| Caller | Reference |
|---|---|
| Order total assembly | `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/order/OrderServiceImpl.java:354` — `taxService.calculateTax(summary, customer, store, language)` |
| Tax calculation service | `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/tax/TaxService.java:22-45` |
| Tax configuration | `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/tax/TaxServiceImpl.java:54-89` |

`OrderServiceImpl` was not independently extracted because it is outside MS-08 ownership; it was used only as a boundary/caller reference from the P1 summary and direct symbol search.

## Source semantic vectors

The CAST-origin vector summary was:

| Component family | Control | Data | Constants | States | Outcomes | Writes | Integrations | Errors |
|---|---:|---:|---:|---:|---:|---:|---:|---:|
| Tax services/repositories | 103 | 82 | 19 | 15 | 31 | 4 | 9 | 38 |
| Tax facade/API | 42 | 22 | 3 | 0 | 45 | 6 | 0 | 45 |
| **CAST/provisional subtotal** | **145** | **104** | **22** | **15** | **76** | **10** | **9** | **83** |

The brief's provisional Tax business vector reported `Data-flow=6`, `Writes=15`, and `Integrations=0` because the local graph was incomplete and source-to-table edges were absent. Direct source reading supersedes those incomplete dimensions for the specification.

### Direct-read semantic vector interpretation

| Dimension | Direct-read result |
|---|---|
| Control-flow | All calculation, ownership, geographic, lookup, and error branches recorded; infrastructure-only null guards were not promoted to independent rules |
| Data-flow | Tax class, rate, description, configuration, address snapshot, item amount, quantity, shipping, handling, and language fields recorded |
| Constants | `TAX_CONFIG`, `DEFAULT`, three tax-basis enum values, two legacy policy defaults, scale 2, `HALF_UP`, and rate/priority constraints recorded |
| State transitions | Tax class/rate present/deleted and quote calculated/failed target lifecycles modeled |
| Outcomes | Created, listed, retrieved, updated, deleted, exists true/false, no-tax, empty, calculated, duplicate, not-found, unauthorized, and validation outcomes recorded |
| Data writes | Class, rate, description, configuration, quote, and quote-item writes modeled; order/cart writes excluded |
| Integrations | Merchant configuration, country/zone/language resolution, and cross-service snapshot boundaries documented; no provider inferred |
| Error paths | Duplicate, ownership, missing resource, invalid configuration, invalid geography, invalid amounts, and malformed input outcomes documented |

## Source defects and ambiguity register

| Finding | Evidence | Target disposition |
|---|---|---|
| Configuration booleans omitted during serialization | `TaxConfiguration.java:13-23` | Persist explicit target fields |
| Tax-class parameter ignored in rate queries | `TaxRateServiceImpl.java:50-58`; `TaxRateRepository.java:27-31` | Add `tax_class_id` predicate |
| Zone copied into state/province | `PersistableTaxRateMapper.java` | Map fields independently |
| Rate existence returns not-found for absence | `TaxFacadeImpl.java:316-328,195-218` | Return boolean false |
| Default class absent from class map during shipping tax | `TaxServiceImpl.java:203-218` | Insert default class into both maps |
| Nullable zone can cause response failure | `ReadableTaxRateMapper.java` | Make `zoneCode` nullable |
| Same-code aggregate not assigned back | `TaxServiceImpl.java:271-284` | Mutate retained item or aggregate explicitly |
| Different-country boolean is ambiguous | `TaxServiceImpl.java:160-166` | Replace with explicit behavior enum |
| List pagination arguments ignored | API lines 95-99 and facade lines 117-121/340-344 | Target API applies page/pageSize contract |
| Legacy API has no calculation endpoint | `TaxService.java:43-45` only exposes internal service | Target adds synchronous `/tax-calculations` contract |

## Exclusions

- Angular tax-management UI files were excluded as frontend scope.
- Generic exceptions and audit listeners were excluded except for externally visible error mapping.
- Pricing, product availability, promotion, shipping provider, packaging, and Drools shipping files were excluded as MS-02, MS-07, MS-09, or MS-12 scope.
- Customer/address mutation was excluded as MS-01 scope.
- Cart, checkout, order lifecycle, and grand-total persistence were excluded as MS-04/MS-05 scope.
- No external tax provider was identified by CAST and none was fabricated.
- No dead-code exclusion was asserted: the brief reported no reachable/unreachable result and no zero-caller query result.
- No tests or dependency files were generated.

## Extraction result

- Mandatory CAST business files read: 6/6
- Mandatory P1 contract files read: 6/6
- Relevant field/model files read: yes
- Files over 500 LOC: 0
- Business rules: 20
- Domain tables: 6
- API endpoints: 15
- OpenAPI contract: generated
- Cross-service foreign keys: 0
- Unresolved business preservation dimensions: 0
- BA review: pending
- Graph ingestion: pending
