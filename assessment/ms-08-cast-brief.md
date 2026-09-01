# CAST Scout Brief: Tax (MS-08)

**Analysis timestamp:** 2026-09-01T15:32:46.810+04:00  
**Service:** MS-08 Tax  
**CAST application:** `Shopizer-Backend`  
**Analysis mode:** Hybrid — live CAST transaction/data-graph scope plus direct source extraction  
**Local path mapping:** `§{main_sources}§` → `initial-source/shopizer-3.2.7/`

## Scope

MS-08 owns tax-class and tax-rate administration, tax-basis selection, tax-rate lookup, tax aggregation, and tax calculation results.

The service consumes product/order amounts, customer and store address context, and shipping/handling inputs. It returns tax items and tax totals. It must not write order totals owned by MS-04 or MS-05.

Tax provider integrations are optional in the target architecture, but no external tax-provider component was identified in the available CAST-origin graph records.

## CAST Query Status and Limitations

Live CAST Imaging is available for the `Shopizer-Backend` application. The Tax transaction query returned 12 transaction records and the Tax data-graph query returned 8 data graphs. Full transaction graphs were retrieved for representative Tax-class and Tax-rate administration paths; calculation callers were resolved through `TaxService` usage.

The local graph remains partial for source vectors and source-to-table edges, so source semantics and complete data ownership still require direct source reading.

## Application Inventory

| Metric | Value |
|---|---:|
| CAST application | `Shopizer-Backend` |
| CAST LOC | 94,528 |
| CAST elements | 16,269 |
| CAST interactions | 72,033 |
| Local backend source root | `initial-source/shopizer-3.2.7/` |
| Pricing/tax/shipping segment components | 101 |
| Backend Tax-name/path subset | 24 |
| Backend Tax business-layer components | 6 |
| Backend Tax infrastructure/context components | 18 |
| Existing P1 Tax component marked extracted | 1 (`TaxServiceImpl`) |
| Live Tax transaction records returned | 12 |
| Live Tax data graphs returned | 8 |
| Source-to-table access edges in local graph | 0 |

## Entry Points and Transaction Evidence

No live CAST transaction records were available in the local graph. The following components are the effective Tax entry points identified from the CAST-origin component inventory and Phase 1 evidence.

| Entry component | CAST ID | Local path | Available evidence |
|---|---:|---|---|
| `TaxServiceImpl` | `13426` | `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/tax/TaxServiceImpl.java` | Tax calculation and aggregation; P1 reference `calculateTax:108-318` |
| `TaxFacadeImpl` | `20996` | `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/facade/tax/TaxFacadeImpl.java` | API orchestration, mapping, validation, and exception translation |
| `TaxClassApi` | `29900` | `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/api/v1/tax/TaxClassApi.java` | Tax-class administration API |
| `TaxRatesApi` | `29770` | `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/api/v1/tax/TaxRatesApi.java` | Tax-rate administration API |
| `TaxClassServiceImpl` | `13397` | `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/tax/TaxClassServiceImpl.java` | Tax-class persistence and store scoping |
| `TaxRateServiceImpl` | `13411` | `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/tax/TaxRateServiceImpl.java` | Tax-rate persistence, geographic references, and store scoping |

### Live transaction query result

The live query `name:contains:tax` returned the following Tax administration transactions:

| Transaction ID | Operation | Reduced | Full |
|---:|---|---:|---:|
| `244002` | GET `api/v1/private/tax/class/` | 13 | 85 |
| `243999` | POST `api/v1/private/tax/class/` | 14 | 97 |
| `244000` | GET `api/v1/private/tax/class/unique/` | 13 | 49 |
| `244004` | DELETE `api/v1/private/tax/class/{}/` | 1 | 59 |
| `244003` | GET `api/v1/private/tax/class/{}/` | 13 | 78 |
| `244001` | PUT `api/v1/private/tax/class/{}/` | 1 | 87 |
| `244156` | POST `api/v1/private/tax/rate/` | 31 | 189 |
| `244238` | GET `api/v1/private/tax/rate/unique/` | 19 | 66 |
| `244242` | DELETE `api/v1/private/tax/rate/{}/` | 19 | 67 |
| `244241` | GET `api/v1/private/tax/rate/{}/` | 19 | 139 |
| `244239` | PUT `api/v1/private/tax/rate/{}/` | 31 | 186 |
| `244240` | GET `api/v1/private/tax/rates/` | 18 | 150 |

The `TaxService` object is used by 21 transactions, including cart/checkout totals and product-price flows. This confirms that the tax calculation engine is a cross-cutting provider behind more than the administration endpoints.

## Component-Centered Call-Graph Results

Because transaction details were unavailable, component-centered `SOURCE_CALLS` traversals were used as a fallback. These are not substitutes for transaction-specific CAST call graphs.

| Entry component | Direct callees | Reachable nodes within five hops | Interpretation |
|---|---:|---:|---|
| `TaxFacadeImpl` | 18 | 95 | Broad facade/model/exception graph; requires transaction filtering |
| `TaxServiceImpl` | 20 | 99 | Tax calculation references order, cart, address, shipping, merchant, and tax models |
| `TaxClassApi` | 6 | 20 | Administration DTO, store, language, and entity graph |
| `TaxRatesApi` | 6 | 31 | Administration DTO, store, language, geographic, and tax-model graph |
| `TaxClassServiceImpl` | 3 | 79 | Store, tax-class, and shared model graph; broad transitive model leakage |
| `TaxRateServiceImpl` | 7 | 79 | Country, zone, language, store, tax-class, and tax-rate graph |

### Direct Tax Calculation Dependencies

`TaxServiceImpl` directly references:

- `MerchantStore`
- `MerchantConfiguration`
- `Customer`
- `Billing`
- `Delivery`
- `OrderSummary`
- `ShoppingCartItem`
- `ShippingSummary`
- `ShippingConfiguration`
- `TaxConfiguration`
- `TaxClass`
- `TaxRate`
- `TaxItem`
- `Country`
- `Zone`
- `Language`
- `ServiceException`

This confirms that tax calculation is not an isolated tax-rate CRUD operation.

## Complexity-Sorted Business-Logic Candidates

The graph did not contain a populated CAST cyclomatic-complexity field. The following provisional ranking uses `srcControlFlow`, supplemented by outcomes, writes, and error-path counts.

| Rank | Component | CAST ID | Local path | Control | Outcomes | Writes | Errors | Classification |
|---:|---|---:|---|---:|---:|---:|---:|---|
| 1 | `TaxFacadeImpl` | `20996` | `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/facade/tax/TaxFacadeImpl.java` | 81 | 35 | 3 | 77 | Business logic — mandatory deep read |
| 2 | `TaxServiceImpl` | `13426` | `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/tax/TaxServiceImpl.java` | 52 | 10 | 0 | 4 | Business logic — mandatory P4 re-read |
| 3 | `TaxClassApi` | `29900` | `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/api/v1/tax/TaxClassApi.java` | 3 | 9 | 2 | 0 | Business/API boundary — read |
| 4 | `TaxRatesApi` | `29770` | `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/api/v1/tax/TaxRatesApi.java` | 3 | 9 | 2 | 0 | Business/API boundary — read |
| 5 | `TaxClassServiceImpl` | `13397` | `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/tax/TaxClassServiceImpl.java` | 3 | 6 | 4 | 1 | Business logic — read |
| 6 | `TaxRateServiceImpl` | `13411` | `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/tax/TaxRateServiceImpl.java` | 3 | 7 | 4 | 1 | Business logic — read |

### Provisional Tax Business Vector Totals

| Vector | Total |
|---|---:|
| Business components | 6 |
| Control-flow | 145 |
| Data-flow | 6 |
| Constants | 3 |
| State transitions | 0 |
| Outcomes | 76 |
| Data writes | 15 |
| Integrations | 0 |
| Error paths | 83 |

The zero integration vector is not proof that integrations are absent. It reflects the current partial/provisional graph inventory.

## Source Files to Read

### CAST-identified business logic — mandatory

| Priority | Component | CAST ID | Local path | Reason |
|---:|---|---:|---|---|
| 1 | `TaxFacadeImpl` | `20996` | `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/facade/tax/TaxFacadeImpl.java` | Highest provisional control-flow and error-path signals; facade validation, authorization, mapping, and calculation orchestration |
| 2 | `TaxServiceImpl` | `13426` | `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/tax/TaxServiceImpl.java` | Core tax-basis selection, address fallback, tax-class aggregation, rate application, shipping/handling treatment, and tax-item production |
| 3 | `TaxClassApi` | `29900` | `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/api/v1/tax/TaxClassApi.java` | Tax-class API operations, authorization, validation, and response/error behavior |
| 4 | `TaxRatesApi` | `29770` | `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/api/v1/tax/TaxRatesApi.java` | Tax-rate API operations, geographic inputs, validation, and response/error behavior |
| 5 | `TaxClassServiceImpl` | `13397` | `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/tax/TaxClassServiceImpl.java` | Tax-class persistence and store ownership |
| 6 | `TaxRateServiceImpl` | `13411` | `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/tax/TaxRateServiceImpl.java` | Tax-rate persistence and country/zone/language associations |

### P1-confirmed candidates missing from the partial CAST component inventory

These files are required to close the data-access and service-contract gaps. No CAST IDs were available for them in the current graph.

| Priority | Local path | Reason |
|---:|---|---|
| 7 | `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/repositories/tax/TaxClassRepository.java` | Verify tax-class queries, store scoping, and CRUD semantics |
| 8 | `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/repositories/tax/TaxRateRepository.java` | Verify geographic/rate queries and the missing tax-class constraint identified by `BR-PRC-021` |
| 9 | `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/tax/TaxService.java` | Confirm calculation service contract |
| 10 | `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/tax/TaxClassService.java` | Confirm tax-class service contract |
| 11 | `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/tax/TaxRateService.java` | Confirm tax-rate service contract |
| 12 | `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/controller/tax/facade/TaxFacade.java` | Confirm facade contract and operation ownership |

## Source Files to Skip or Treat as Context Only

These files are not independent business-rule sources. They may be consulted only when required to resolve DTO, persistence, or serialization semantics.

### DTOs, entities, models, and mappers

| Local path/family | Reason for exclusion from independent BR extraction |
|---|---|
| `initial-source/shopizer-3.2.7/sm-core-model/src/main/java/com/salesmanager/core/model/tax/TaxClass.java` | Domain data structure; no independent workflow expected |
| `initial-source/shopizer-3.2.7/sm-core-model/src/main/java/com/salesmanager/core/model/tax/taxrate/TaxRate.java` | Rate data structure; inspect fields only when resolving rate semantics |
| `initial-source/shopizer-3.2.7/sm-core-model/src/main/java/com/salesmanager/core/model/tax/taxrate/TaxRateDescription.java` | Description/value object |
| `initial-source/shopizer-3.2.7/sm-core-model/src/main/java/com/salesmanager/core/model/tax/TaxConfiguration.java` | Configuration value object; calculation behavior belongs to `TaxServiceImpl` |
| `initial-source/shopizer-3.2.7/sm-core-model/src/main/java/com/salesmanager/core/model/tax/TaxItem.java` | Calculation result/value object; no persistence ownership indicated |
| `initial-source/shopizer-3.2.7/sm-shop-model/src/main/java/com/salesmanager/shop/model/tax/TaxClassEntity.java` | Persistence mapping |
| `initial-source/shopizer-3.2.7/sm-shop-model/src/main/java/com/salesmanager/shop/model/tax/TaxRateEntity.java` | Persistence mapping |
| `initial-source/shopizer-3.2.7/sm-shop-model/src/main/java/com/salesmanager/shop/model/tax/PersistableTaxClass.java` | API request DTO |
| `initial-source/shopizer-3.2.7/sm-shop-model/src/main/java/com/salesmanager/shop/model/tax/PersistableTaxRate.java` | API request DTO |
| `initial-source/shopizer-3.2.7/sm-shop-model/src/main/java/com/salesmanager/shop/model/tax/ReadableTaxClass.java` | API response DTO |
| `initial-source/shopizer-3.2.7/sm-shop-model/src/main/java/com/salesmanager/shop/model/tax/ReadableTaxRate.java` | API response DTO |
| `initial-source/shopizer-3.2.7/sm-shop-model/src/main/java/com/salesmanager/shop/model/tax/ReadableTaxRateDescription.java` | API response DTO |
| `initial-source/shopizer-3.2.7/sm-shop-model/src/main/java/com/salesmanager/shop/model/tax/ReadableTaxRateFull.java` | API response DTO |
| `initial-source/shopizer-3.2.7/sm-shop-model/src/main/java/com/salesmanager/shop/model/tax/TaxRateDescription.java` | API value object |
| `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/mapper/tax/PersistableTaxClassMapper.java` | Mapping infrastructure |
| `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/mapper/tax/PersistableTaxRateMapper.java` | Mapping infrastructure |
| `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/mapper/tax/ReadableTaxClassMapper.java` | Mapping infrastructure |
| `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/mapper/tax/ReadableTaxRateMapper.java` | Mapping infrastructure |

### Other exclusions

- Generic audit listeners, audit sections, schema constants, and persistence framework classes.
- Generic exception classes; record their externally visible mapping only through `TaxFacadeImpl`.
- Angular administration components under `initial-source/shopizer-admin-main/app/pages/tax-management/`; these are frontend scope, not MS-08 backend extraction scope.
- Pricing and product-price components owned by MS-07/MS-02.
- Shipping providers, packaging logic, shipping Drools rules, and quote providers owned by MS-09/MS-12.
- Customer identity mutation and address ownership logic owned by MS-01.
- Order lifecycle and order-total persistence owned by MS-04/MS-05.

## Data Entities and Table Ownership

### Legacy data entities

| Legacy entity/table | Evidence | MS-08 status |
|---|---|---|
| `TAX_CLASS` | SourceTable graph record; evidence `BR-PRC-014-021` | Owned by MS-08 |
| `TAX_RATE` | SourceTable graph record; evidence `BR-PRC-014-021` | Owned by MS-08 |
| `TaxItem` | Core model referenced by `TaxServiceImpl` | In-memory calculation/result entity; no legacy table evidence |
| `TaxConfiguration` | Core model referenced by `TaxServiceImpl` | Configuration input; persistence ownership requires confirmation |
| `MerchantConfiguration` | Referenced by `TaxServiceImpl` | Owned by merchant/configuration context; consume through a service contract |
| `MerchantStore` | Referenced by Tax APIs and services | Owned by MS-10; do not write from MS-08 |
| `Customer`, `Billing`, `Delivery` | Referenced by `TaxServiceImpl` | Owned by MS-01/order or checkout contexts; consume snapshots |
| `ShoppingCartItem`, `OrderSummary` | Referenced by `TaxServiceImpl` | Owned by MS-04/MS-05; consume calculation inputs |
| `ShippingSummary`, `ShippingConfiguration` | Referenced by `TaxServiceImpl` | Owned by MS-09/configuration context; consume shipping results/configuration |

### Target conceptual entities from service composition

The target architecture identifies the following MS-08 aggregates:

- `TaxProfile`
- `TaxRate`
- `TaxQuote`
- Jurisdiction and tax-rule concepts

`TaxQuote` and tax audit records were not represented as legacy `SourceTable` nodes. Their persistence and replay/idempotency requirements require Phase 4 extraction and Phase 4b confirmation.

### Data-access graph result

Live CAST returned tax-related data graphs including:

| Data graph ID | Start entity/operation | Size |
|---:|---|---:|
| `243910` | `api/v1/private/tax/class/` | 1590 |
| `243915` | `api/v1/private/tax/class/` | 1650 |
| `243952` | `api/v1/private/tax/class/` | 1637 |
| `243954` | `api/v1/private/tax/class/` | 1652 |
| `243962` | `api/v1/private/tax/class/` | 1598 |
| `243924` | `api/v1/private/tax/rate/` | 423 |
| `243919` | `salesmanager.tax_class` | 587 |
| `243926` | `salesmanager.tax_rate_description` | 1 |

The following still must be confirmed through direct source and detailed data-graph inspection:

- CRUD access of `TaxClassServiceImpl` to `TAX_CLASS`
- CRUD access of `TaxRateServiceImpl` to `TAX_RATE`
- Read access of `TaxServiceImpl` to tax classes/rates
- Whether tax calculation reads merchant configuration directly or through a service
- Whether any order, cart, product, or shipping tables are accessed directly
- Whether a tax quote/audit record is persisted

The MS-08 boundary invariant remains: no direct cross-service table writes and no cross-service foreign keys.

## Cross-Service Dependencies

| Dependency | Direction | Evidence/status | Required boundary |
|---|---|---|---|
| Customer identity and address snapshot | MS-04/MS-08 and MS-01/MS-08 | `Customer`, `Billing`, and `Delivery` referenced by `TaxServiceImpl`; service composition declares MS-01 ownership | Consume customer/address data through MS-01 or checkout snapshot contracts |
| Product tax classification | MS-02 → MS-04 → MS-08 | Product/cart item tax inputs are implied by P1 calculation flow; direct service edge not available | MS-08 consumes product tax-class identifiers, not product tables |
| Cart and checkout totals | MS-04 → MS-08 | Service composition explicitly declares MS-04 → MS-08 REST calculation dependency | MS-08 returns tax results; MS-04 owns cart and checkout totals |
| Shipping and handling inputs | MS-09/MS-04 → MS-08 | `ShippingSummary` and `ShippingConfiguration` referenced by `TaxServiceImpl`; `BR-PRC-018` covers shipping/handling taxation | Consume shipping amount/configuration; do not write shipping data |
| Order total assembly | MS-08 → MS-04/MS-05 | `BR-PRC-013` and `BR-ORD-008` establish tax as a downstream total component | Return tax result; order/cart context owns grand-total persistence |
| Store and merchant configuration | MS-10 → MS-08 | `MerchantStore` and `MerchantConfiguration` references observed | Resolve store scope and tax configuration through declared contracts |
| External tax provider | MS-08 → MS-12/external provider | Optional in service composition; no provider component or integration vector observed | Keep provider adapter behind MS-12 or an explicit MS-08 port; do not infer implementation |
| Language/country/zone reference data | Shared reference data → MS-08 | Direct references from tax administration and rate service | Use reference-data contracts; no cross-service table reads |

## Phase 1 Rules Requiring Phase 4 Deep Extraction

All eight tax-specific P1 rules require source re-reading at Phase 4 depth.

| Rule ID | P1 source reference | P4 extraction focus |
|---|---|---|
| `BR-PRC-014` | `TaxServiceImpl.calculateTax:108-139` | Exact precedence and fallback among shipping, billing, and store address bases |
| `BR-PRC-015` | `TaxServiceImpl.calculateTax:141-165` | Different-province behavior, null/no-tax outcome, configuration gates, and error semantics |
| `BR-PRC-016` | `TaxServiceImpl.calculateTax:167-173` | Different-country behavior and store-address substitution conditions |
| `BR-PRC-017` | `TaxServiceImpl.calculateTax:181-210` | Tax-class grouping, default-class assignment, missing-class handling, and monetary precision |
| `BR-PRC-018` | `TaxServiceImpl.calculateTax:213-244` | Shipping/handling inclusion, positive-value conditions, and commented-out configuration behavior |
| `BR-PRC-019` | `TaxServiceImpl.calculateTax:265-287` | Piggyback versus non-piggyback rate calculation base and compounding order |
| `BR-PRC-020` | `TaxServiceImpl.calculateTax:299-318` | Same-code consolidation intent versus actual aggregate mutation behavior |
| `BR-PRC-021` | `TaxServiceImpl:253-261`; `TaxRateRepository:53-57` | Whether rate queries are constrained by tax class, country, zone, store, and geographic specificity |

### Boundary rule requiring coordination

| Rule ID | Source reference | Ownership note |
|---|---|---|
| `BR-PRC-013` | `OrderServiceImpl.caculateOrder:217-394` | Reconfirm tax placement in total assembly with MS-04/MS-05; MS-08 supplies tax calculation but does not own grand-total assembly |

The P1 summary contains 36 rules across pricing, promotions, tax, and shipping. Only `BR-PRC-014..021` are directly assigned to MS-08; `BR-PRC-013` is a required cross-service boundary rule.

## Dead Code and Exclusion Check

No dead-code exclusion can be asserted from the available graph.

- The `unreachable` property was not present on the source-component records.
- No transaction reachability results were available.
- No zero-caller CAST query was available in this session.
- Therefore, verified dead-code exclusions: **0**.
- `TaxServiceImpl` is marked `extracted=true` in the graph, but this means it was covered during P1; it is not evidence of dead code and does not remove the P4 deep-read requirement.
- DTOs, mappers, frontend components, exceptions, and framework classes are scope exclusions based on classification, not dead-code determinations.

The live CAST Scout must query unreachable/zero-caller components before finalizing exclusions:

```text
mcp_imaging_objects(
  application="Shopizer-Backend",
  filters="unreachable:true",
  sort="cyclomaticComplexity",
  order="desc"
)
