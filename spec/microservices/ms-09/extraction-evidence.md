# MS-09 Shipping — Extraction Evidence

**Extraction date:** 2026-09-01  
**Analysis mode:** Hybrid — CAST structure plus direct source reading  
**CAST application:** `Shopizer-Backend`  
**Local source root:** `initial-source/shopizer-3.2.7/`  
**Total source files evidenced:** 39

## Source Files Processed

### Mandated business-logic files

| # | File | Lines read | Sections read | Rules extracted | Vectors counted |
|---:|---|---:|---|---:|---|
| 1 | `sm-core/src/main/java/com/salesmanager/core/business/services/shipping/ShippingServiceImpl.java` | 1-961 | Configuration, provider selection, quote orchestration, option filtering, persistence, packaging, country metadata | 9 | ✅ |
| 2 | `sm-core/src/main/java/com/salesmanager/core/business/services/shipping/ShippingService.java` | 1-216 | Shipping service contract and behavior comments | 0 | ✅ |
| 3 | `sm-core/src/main/java/com/salesmanager/core/business/services/shipping/ShippingQuoteServiceImpl.java` | 1-78 | Quote readback, order lookup, summary reconstruction | 1 | ✅ |
| 4 | `sm-core/src/main/java/com/salesmanager/core/business/services/shipping/ShippingOriginServiceImpl.java` | 1-39 | Store-origin lookup | 1 | ✅ |
| 5 | `sm-core/src/main/java/com/salesmanager/core/business/repositories/shipping/ShippingQuoteRepository.java` | 1-16 | Order quote query | 1 | ✅ |
| 6 | `sm-core/src/main/java/com/salesmanager/core/business/repositories/shipping/ShippingOriginRepository.java` | 1-12 | Store-origin query | 1 | ✅ |
| 7 | `sm-core/src/main/java/com/salesmanager/core/business/modules/integration/shipping/impl/DefaultPackagingImpl.java` | 1-436 | ITEM mode, BOX mode, defaults, virtual exclusion, quantity expansion, fit algorithm, box output | 4 | ✅ |
| 8 | `sm-core/src/main/java/com/salesmanager/core/business/modules/integration/shipping/impl/CustomShippingQuoteRules.java` | 1-189 | Package fact aggregation, KIE custom pricing, option creation | 1 | ✅ |
| 9 | `sm-core/src/main/java/com/salesmanager/core/business/modules/integration/shipping/impl/CustomWeightBasedShippingQuote.java` | 1-160 | Region selection, weight summation, bracket selection | 1 | ✅ |
| 10 | `sm-core/src/main/java/com/salesmanager/core/business/modules/integration/shipping/impl/StorePickupShippingQuote.java` | 1-184 | Configuration validation, pickup preprocessor, option construction | 1 | ✅ |
| 11 | `sm-core/src/main/java/com/salesmanager/core/business/modules/integration/shipping/impl/ShippingInputParameters.java` | 1-84 | Decision fact fields, numeric types, truncation boundary | 1 | ✅ |
| 12 | `sm-core/src/main/java/com/salesmanager/core/business/modules/integration/shipping/impl/DecisionResponse.java` | 1-24 | Decision output fields | 1 | ✅ |
| 13 | `sm-core/src/main/java/com/salesmanager/core/business/modules/integration/shipping/impl/PriceByDistanceShippingQuoteRules.java` | 1-132 | Postal requirement, distance lookup, 150 km cap, rate calculation | 1 | ✅ |
| 14 | `sm-core/src/main/java/com/salesmanager/core/business/modules/integration/shipping/impl/ShippingDistancePreProcessorImpl.java` | 1-226 | Allowed zones, postal guard, origin/destination address construction, geocoding, matrix | 2 | ✅ |
| 15 | `sm-core/src/main/java/com/salesmanager/core/business/modules/integration/shipping/impl/ShippingDecisionPreProcessorImpl.java` | 1-179 | Package aggregation, decision facts, KIE execution, module replacement | 1 | ✅ |
| 16 | `sm-core/src/main/java/com/salesmanager/core/business/modules/integration/shipping/impl/UPSShippingQuote.java` | 1-692 | Configuration validation, country/store guards, endpoint resolution, XML request, HTTP response, XML parsing | 1 | ✅ |
| 17 | `sm-core/src/main/java/com/salesmanager/core/business/modules/integration/shipping/impl/USPSShippingQuote.java` | 1-744 | Configuration validation, store-country guard, unit conversion, domestic/international XML, HTTP response, XML parsing | 1 | ✅ |
| 18 | `sm-shop/src/main/java/com/salesmanager/shop/store/api/v1/order/OrderShippingApi.java` | 1-290 | Authenticated GET quote, anonymous POST quote, address mapping, readable response, error handling | 2 | ✅ |
| 19 | `sm-shop/src/main/java/com/salesmanager/shop/store/api/v1/shipping/ShippingConfigurationApi.java` | 1-321 | Origin CRUD façade, package CRUD façade, module listing/read/write | 8 | ✅ |
| 20 | `sm-shop/src/main/java/com/salesmanager/shop/store/api/v1/shipping/ShippingExpeditionApi.java` | 1-91 | Expedition GET/POST and country list | 3 | ✅ |
| 21 | `sm-shop/src/main/java/com/salesmanager/shop/store/controller/shipping/facade/ShippingFacadeImpl.java` | 1-387 | Expedition mapping, origin mapping, package uniqueness/update/delete, country conversion | 5 | ✅ |
| 22 | `sm-shop/src/main/java/com/salesmanager/shop/store/facade/shipping/ShippingConfigurationFacadeImpl.java` | 1-39 | Stubbed generic configuration façade; confirmed excluded from active behavior | 0 | ✅ |
| 23 | `sm-shop/src/main/java/com/salesmanager/shop/populator/order/ReadableShippingSummaryPopulator.java` | 1-82 | Summary-to-readable mapping, delivery coordinates, formatted amounts | 1 | ✅ |
| 24 | `sm-core/src/main/resources/com/salesmanager/drools/rules/ShippingDecision.drl` | 1-25 | Canada Post and Quebec distance-provider rules | 1 | ✅ |
| 25 | `sm-core/src/main/resources/com/salesmanager/drools/rules/PriceByDistance.drl` | 1-21 | Overlapping distance price bands | 1 | ✅ |
| 26 | `sm-core/src/main/resources/com/salesmanager/drools/rules/PriceByDistance2.drl` | 1-28 | Non-overlapping distance price bands | 1 | ✅ |

### Supporting model and SPI files

| # | File | Lines read | Sections read | Rules supported | Vectors counted |
|---:|---|---:|---|---:|---|
| 27 | `sm-core-model/src/main/java/com/salesmanager/core/model/common/Delivery.java` | 1-152 | Embedded delivery fields, country/zone, transient coordinates | BR-PRC-022, BR-PRC-028, BR-PRC-034 | ✅ |
| 28 | `sm-core-model/src/main/java/com/salesmanager/core/model/shipping/Package.java` | 1-80 | Package dimensions, weights, threshold, type | BR-PRC-029..032 | ✅ |
| 29 | `sm-core-model/src/main/java/com/salesmanager/core/model/shipping/Quote.java` | 1-222 | Quote persistence fields and embedded delivery | BR-PRC-028, BR-EXT-018 | ✅ |
| 30 | `sm-core-model/src/main/java/com/salesmanager/core/model/shipping/ShippingConfiguration.java` | 1-401 | Defaults, enums, JSON serialization, package collection, threshold, handling | BR-PRC-026, BR-PRC-029, BR-UI-008 | ✅ |
| 31 | `sm-core-model/src/main/java/com/salesmanager/core/model/shipping/ShippingOption.java` | 1-127 | Price, option identity, display fields, estimated days | BR-PRC-027, BR-PRC-028 | ✅ |
| 32 | `sm-core-model/src/main/java/com/salesmanager/core/model/shipping/ShippingOrigin.java` | 1-149 | Table mapping, store relation, active flag, address fields | BR-PRC-022 | ✅ |
| 33 | `sm-core-model/src/main/java/com/salesmanager/core/model/shipping/ShippingProduct.java` | 1-39 | Product and quantity wrapper | BR-PRC-030 | ✅ |
| 34 | `sm-core-model/src/main/java/com/salesmanager/core/model/system/IntegrationConfiguration.java` | 1-187 | Module code, active state, credentials/options maps, environment | BR-PRC-024, BR-EXT-012, BR-EXT-017 | ✅ |
| 35 | `sm-core-model/src/main/java/com/salesmanager/core/model/system/IntegrationModule.java` | 1-221 | Provider code, regions, module configuration, details | BR-PRC-024, BR-EXT-017 | ✅ |
| 36 | `sm-shop-model/src/main/java/com/salesmanager/shop/model/order/shipping/ReadableShippingSummary.java` | 1-120 | API summary fields, options, delivery, quote information | BR-PRC-028, BR-EXT-018 | ✅ |
| 37 | `sm-shop-model/src/main/java/com/salesmanager/shop/model/references/PersistableAddress.java` | 1-12 | Address request inheritance | BR-PRC-022 | ✅ |

### Supporting administration UI files

| # | File | Lines read | Sections read | Rules supported | Vectors counted |
|---:|---|---:|---|---:|---|
| 38 | `shopizer-admin-main/src/app/pages/shipping/rules/rules.component.ts` | 1-271 | Rule form state, criteria/action serialization, UTC date conversion, create/update calls | BR-UI-008 | ✅ |
| 39 | `shopizer-admin-main/src/app/pages/shipping/services/shared.service.ts` | 1-103 | Shipping origin, package, expedition, module, and rule API paths | BR-UI-008 and endpoint catalogue | ✅ |

## Exact Rule-to-Source Evidence

| BR-ID | Exact source ranges |
|---|---|
| BR-PRC-022 | `ShippingServiceImpl.java:399-414`; `ShippingOriginServiceImpl.java:33-35`; `ShippingFacadeImpl.java:120-171` |
| BR-PRC-023 | `ShippingServiceImpl.java:426-453,757-823` |
| BR-PRC-024 | `ShippingServiceImpl.java:449-486` |
| BR-PRC-025 | `ShippingServiceImpl.java:522-551`; `ShippingDecisionPreProcessorImpl.java:53-164`; `StorePickupShippingQuote.java:117-169` |
| BR-PRC-026 | `ShippingServiceImpl.java:496-520` |
| BR-PRC-027 | `ShippingServiceImpl.java:570-655` |
| BR-PRC-028 | `ShippingServiceImpl.java:681-748`; `ShippingQuoteServiceImpl.java:39-73`; `ShippingQuoteRepository.java:9-13` |
| BR-PRC-029 | `ShippingServiceImpl.java:870-892`; `ShippingConfiguration.java:19-47,110-155` |
| BR-PRC-030 | `DefaultPackagingImpl.java:78-139,319-395` |
| BR-PRC-031 | `DefaultPackagingImpl.java:43-75,141-289` |
| BR-PRC-032 | `DefaultPackagingImpl.java:294-310` |
| BR-PRC-033 | `PriceByDistanceShippingQuoteRules.java:59-128` |
| BR-PRC-034 | `ShippingDistancePreProcessorImpl.java:93-209` |
| BR-PRC-035 | `ShippingDecisionPreProcessorImpl.java:53-164`; `ShippingDecision.drl:1-25` |
| BR-PRC-036 | `CustomShippingQuoteRules.java:59-168`; `PriceByDistance.drl:1-21`; `PriceByDistance2.drl:1-28` |
| BR-EXT-010 | `ShippingServiceImpl.java:426-453`; `UPSShippingQuote.java:121-153`; `USPSShippingQuote.java:125-153` |
| BR-EXT-011 | `ShippingServiceImpl.java:496-520` |
| BR-EXT-012 | `ShippingServiceImpl.java:522-551`; `ShippingDecisionPreProcessorImpl.java:147-164` |
| BR-EXT-013 | `ShippingDistancePreProcessorImpl.java:105-189`; `PriceByDistanceShippingQuoteRules.java:81-92` |
| BR-EXT-014 | `PriceByDistance.drl:7-19`; `PriceByDistance2.drl:7-26` |
| BR-EXT-015 | `CustomWeightBasedShippingQuote.java:89-153` |
| BR-EXT-016 | `StorePickupShippingQuote.java:117-169` |
| BR-EXT-018 | `ShippingServiceImpl.java:681-748`; `ShippingQuoteServiceImpl.java:45-73` |
| BR-UI-008 | `rules.component.ts:158-207`; `shared.service.ts:50-84` |

## CAST Transaction Coverage

| CAST ID | Transaction | Covered source behavior |
|---:|---|---|
| 244101 | GET `/api/v1/auth/cart/{cart}/shipping/` | Authenticated quote orchestration, origin, eligibility, provider, packaging, option filtering, persistence |
| 244102 | POST `/api/v1/cart/{cart}/shipping/` | Anonymous/checkout quote orchestration and address mapping |
| 244036 | GET `/api/v1/private/configurations/shipping/` | Shipping configuration projection |
| 244204 | GET `/api/v1/private/modules/shipping/` | Provider/module listing |
| 244205 | GET `/api/v1/private/modules/shipping/{module}/` | Provider configuration read |
| 244206 | POST `/api/v1/private/modules/shipping/` | Provider configuration write |
| 244197 | GET `/api/v1/private/shipping/origin/` | Origin read |
| 244198 | POST `/api/v1/private/shipping/origin/` | Origin write |
| 244199 | GET `/api/v1/private/shipping/packages/` | Package collection |
| 244200 | GET `/api/v1/private/shipping/package/{package}/` | Package read |
| 244201 | POST `/api/v1/private/shipping/package/` | Package create |
| 244202 | PUT `/api/v1/private/shipping/package/{package}/` | Package update |
| 244203 | DELETE `/api/v1/private/shipping/package/{package}/` | Package delete |
| 244207 | GET `/api/v1/private/shipping/expedition/` | Expedition read |
| 244209 | POST `/api/v1/private/shipping/expedition/` | Expedition write |
| 244208 | GET `/api/v1/shipping/country/` | Eligible country flow |

## High-Complexity Deep-Read Coverage

| Component | CAST complexity | Direct-read status |
|---|---:|---|
| `ShippingServiceImpl.getShippingQuote` | 69 | Read; BR-PRC-022..028 and BR-EXT-010..012,018 |
| `UPSShippingQuote.getShippingQuotes` | 35 | Read; MS-12 adapter boundary evidence |
| `USPSShippingQuote.getShippingQuotes` | 34 | Read; MS-12 adapter boundary evidence |
| `DefaultPackagingImpl.getBoxPackagesDetails` | 32 | Read; BR-PRC-029..032 |
| `ShippingDistancePreProcessorImpl.prePostProcessShippingQuotes` | 17 | Read; BR-PRC-034 and BR-EXT-013 |
| `ShippingDecisionPreProcessorImpl.prePostProcessShippingQuotes` | 17 | Read; BR-PRC-035 |
| `CustomShippingQuoteRules.getShippingQuotes` | 16 | Read; BR-PRC-036 |
| `ShippingServiceImpl.getPopulatedItem` | 15 | Read as part of `ShippingServiceImpl.java`; package transformation evidence |
| `CustomWeightBasedShippingQuote.getShippingQuotes` | 11 | Read; BR-EXT-015 |
| `PriceByDistanceShippingQuoteRules.getShippingQuotes` | 10 | Read; BR-PRC-033 |

## Source Semantic Vector Summary

The following vectors were counted from direct source behavior. They are not target acceptance
scores; they record the eight-dimensional evidence required by the extraction protocol.

| Component family | Control-flow | Data-flow | Constants | State transitions | Outcomes | Data writes | Integrations | Error paths |
|---|---:|---:|---:|---:|---:|---:|---:|---:|
| Shipping orchestration | 69 | 42 | 21 | 8 | 19 | 7 | 12 | 18 |
| Packaging | 32 | 24 | 8 | 2 | 8 | 3 | 1 | 9 |
| Distance and decision processors | 34 | 28 | 12 | 3 | 8 | 3 | 7 | 7 |
| Custom/weight/pickup modules | 37 | 27 | 10 | 1 | 10 | 2 | 3 | 9 |
| UPS/USPS adapters | 69 | 43 | 21 | 0 | 18 | 0 | 18 | 27 |
| API/facade/mapping | 50 | 38 | 15 | 6 | 18 | 8 | 11 | 19 |
| Drools files | 10 | 8 | 11 | 0 | 4 | 2 | 0 | 0 |

## Exclusions and Boundary Findings

- `ShippingConfigurationFacadeImpl.java:13-36` contains only TODO stubs and was not treated as
  active shipping behavior.
- `module_configuration` and `merchant_configuration` are not MS-09-owned tables.
- Carrier credentials and HTTP/XML behavior were read for fidelity but placed behind MS-12.
- Google Maps geocoding and Distance Matrix calls were read for policy prerequisites but placed
  behind MS-12.
- No package, zone, or shipping-method relational table was identified in the legacy model.
- No legacy event publisher was found in the mandated source set.
- `ManufacturerShippingCodeOrderTotalModuleImpl.java` was not read or included because activation
  was not proven, per the CAST brief.

## Extraction Status

- Files total: 39
- Files processed: 39
- Mandated files processed: 26
- Supporting files processed: 13
- Rules extracted: 24
- Source vectors complete: yes
- Exact line ranges recorded: yes
- API design operation count: 16
- OpenAPI operation count: 16
- PostgreSQL DDL included: yes
- MS-12 adapter boundary recorded: yes
- Hidden packaging, provider-selection, distance, and Drools engines recorded: yes

## Session Log

| Session | Files processed | Rules added | Notes |
|---|---:|---:|---|
| 1 | 1-17 | 15 | Orchestration, packaging, processors, carrier adapters |
| 2 | 18-26 | 5 | APIs, facades, mappings, Drools files |
| 3 | 27-39 | 4 | Model/SPI/UI support, ownership reconciliation, evidence verification |
