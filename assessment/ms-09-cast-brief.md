# MS-09 Shipping — CAST Scout Brief

**Phase:** 4 CAST Scout  
**CAST application:** `Shopizer-Backend`  
**Analysis mode:** Hybrid (live CAST scope plus direct source extraction)  
**Source root:** `initial-source/shopizer-3.2.7/`

## CAST transaction scope

Shipping discovery matched these transactions:

| CAST ID | Transaction | Full graph objects | Scope |
|---:|---|---:|---|
| 244101 | GET `/api/v1/auth/cart/{cart}/shipping/` | 1,202 | Customer quote calculation |
| 244102 | POST `/api/v1/cart/{cart}/shipping/` | 1,192 | Shipping selection/update |
| 244036 | GET `/api/v1/private/configurations/shipping/` | 9 | Shipping configuration |
| 244204 | GET `/api/v1/private/modules/shipping/` | 200 | Provider/module listing |
| 244205 | GET `/api/v1/private/modules/shipping/{module}/` | 197 | Provider configuration read |
| 244206 | POST `/api/v1/private/modules/shipping/` | 284 | Provider configuration write |
| 244197 | GET `/api/v1/private/shipping/origin/` | 336 | Origin read |
| 244198 | POST `/api/v1/private/shipping/origin/` | 353 | Origin write |
| 244199 | GET `/api/v1/private/shipping/packages/` | 365 | Package collection |
| 244200 | GET `/api/v1/private/shipping/package/{package}/` | 369 | Package read |
| 244201 | POST `/api/v1/private/shipping/package/` | 452 | Package create |
| 244202 | PUT `/api/v1/private/shipping/package/{package}/` | 451 | Package update |
| 244203 | DELETE `/api/v1/private/shipping/package/{package}/` | 423 | Package delete |
| 244207 | GET `/api/v1/private/shipping/expedition/` | 347 | Expedition configuration |
| 244209 | POST `/api/v1/private/shipping/expedition/` | 432 | Expedition configuration write |
| 244208 | GET `/api/v1/shipping/country/` | 3,020 | Supported-country flow |

The critical full call graphs for transactions 244101 and 244102 reach shipping
orchestration, origin lookup, store/country validation, module discovery, packaging,
provider quote generation, pre/post processors, option selection, and quote persistence.
Transaction 244208 additionally reaches shared country/reference and merchant configuration
paths.

## Source files to read

Read these business-logic sources in full, using multi-pass reads for files over 500 lines:

### Orchestration and persistence

- `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/shipping/ShippingServiceImpl.java`
- `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/shipping/ShippingService.java`
- `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/shipping/ShippingQuoteServiceImpl.java`
- `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/shipping/ShippingOriginServiceImpl.java`
- `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/repositories/shipping/ShippingQuoteRepository.java`
- `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/repositories/shipping/ShippingOriginRepository.java`

### Packaging and quote engines

- `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/modules/integration/shipping/impl/DefaultPackagingImpl.java`
- `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/modules/integration/shipping/impl/CustomShippingQuoteRules.java`
- `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/modules/integration/shipping/impl/CustomWeightBasedShippingQuote.java`
- `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/modules/integration/shipping/impl/StorePickupShippingQuote.java`
- `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/modules/integration/shipping/impl/ShippingInputParameters.java`
- `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/modules/integration/shipping/impl/DecisionResponse.java`
- `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/modules/integration/shipping/impl/PriceByDistanceShippingQuoteRules.java`
- `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/modules/integration/shipping/impl/ShippingDistancePreProcessorImpl.java`
- `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/modules/integration/shipping/impl/ShippingDecisionPreProcessorImpl.java`
- `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/modules/integration/shipping/impl/UPSShippingQuote.java`
- `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/modules/integration/shipping/impl/USPSShippingQuote.java`

### API and mapping

- `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/api/v1/order/OrderShippingApi.java`
- `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/api/v1/shipping/ShippingConfigurationApi.java`
- `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/api/v1/shipping/ShippingExpeditionApi.java`
- `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/controller/shipping/facade/ShippingFacadeImpl.java`
- `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/facade/shipping/ShippingConfigurationFacadeImpl.java`
- `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/populator/order/ReadableShippingSummaryPopulator.java`

### Hidden business rules

- `initial-source/shopizer-3.2.7/sm-core/src/main/resources/com/salesmanager/drools/rules/ShippingDecision.drl`
- `initial-source/shopizer-3.2.7/sm-core/src/main/resources/com/salesmanager/drools/rules/PriceByDistance.drl`
- `initial-source/shopizer-3.2.7/sm-core/src/main/resources/com/salesmanager/drools/rules/PriceByDistance2.drl`

Model/SPI context may be read as needed:
`sm-core-model/.../model/shipping/`, `sm-core-modules/.../modules/integration/shipping/model/`,
and `sm-shop-model/.../model/shipping/`.

## Complexity and hidden-engine signals

CAST ranked the following objects as mandatory deep-read candidates:

| Object | Cyclomatic complexity | Why |
|---|---:|---|
| `ShippingServiceImpl.getShippingQuote` | 69 | Main orchestration, provider selection, threshold and option policy |
| `UPSShippingQuote.getShippingQuotes` | 35 | Carrier quote and failure behavior |
| `USPSShippingQuote.getShippingQuotes` | 34 | Carrier quote and failure behavior |
| `DefaultPackagingImpl.getBoxPackagesDetails` | 32 | Box fit and package construction |
| `ShippingDistancePreProcessorImpl.prePostProcessShippingQuotes` | 17 | Geocoding and distance preparation |
| `ShippingDecisionPreProcessorImpl.prePostProcessShippingQuotes` | 17 | KIE/Drools decision execution |
| `CustomShippingQuoteRules.getShippingQuotes` | 16 | Region/item custom rates |
| `ShippingServiceImpl.getPopulatedItem` | 15 | Product/package transformation |
| `CustomWeightBasedShippingQuote.getShippingQuotes` | 11 | Weight-band rate engine |
| `PriceByDistanceShippingQuoteRules.getShippingQuotes` | 10 | Distance eligibility and rates |

MS-09 is not CRUD-only. The extractor must preserve the packaging engine (ITEM/BOX
mode, virtual-product exclusion, defaults, quantity expansion, box volume factor `.75`,
weight aggregation), provider registry and replacement ordering, free-shipping threshold,
option selection, distance cap/rates, and Drools consequences/rule ordering.

## Data ownership and boundaries

Confirmed MS-09 legacy table candidates:

- `salesmanager.shipping_quote` (CAST table 369, graph 243933): quote persistence/readback.
- `salesmanager.shiping_origin` (CAST table 370, graph 243934): configured origin persistence.

Do not create MS-09 ownership for `module_configuration`, `merchant_configuration`, or
`order_product`; these are shared or owned by MS-11/MS-10, MS-02, and checkout/order services.
CAST's quote/origin graphs were SELECT-oriented, so verify INSERT/UPDATE paths directly in the
repositories and services. No package, zone, or shipping-method table was identified; packaging
is primarily in-memory and provider configuration is shared/configuration data.

Required target boundaries:

- MS-09 consumes product identity, availability, weight, dimensions, and virtual status from MS-02.
- MS-09 consumes validated customer address from MS-01/MS-04 and store context from MS-10/MS-11.
- MS-09 supplies shipping and handling facts to MS-08 for tax calculation.
- MS-09 supplies quote/method snapshots to checkout/order services and does not transition orders.
- Carrier clients, Google geocoding/distance calls, credentials, retries, and response normalization
  belong behind MS-12; provider-independent policy remains in MS-09.

## P1 rules requiring deep extraction

Re-extract and reconcile `BR-PRC-022` through `BR-PRC-036` from the Phase 1 summary:

- configured origin precedence and store fallback
- national versus international destination eligibility
- first active provider selection and no-provider behavior
- pre/postprocessor ordering and provider replacement
- strict free-shipping threshold comparison
- highest/least/all option selection and numeric precision
- quote persistence fields and lifecycle
- BOX versus ITEM packaging and defaults
- virtual-product exclusion, missing dimensions/weight, and quantity expansion
- `.75` box-fit volume factor and package-weight behavior
- 150 km distance cap and per-kilometer rates
- Google geocoding and distance-matrix preprocessing
- package fact aggregation and `ShippingDecision.drl`
- overlapping `PriceByDistance` Drools rules and salience/firing order

Keep pricing (`BR-PRC-008`, `BR-PRC-012`, `BR-PRC-013`) and tax (`BR-PRC-014`–`021`)
out of MS-09 unless direct source evidence changes ownership. Do not classify dynamically
configured UPS/USPS providers as dead without caller/configuration evidence. Exclude
`ManufacturerShippingCodeOrderTotalModuleImpl.java` from active behavior unless activation
is proven.

## Placement candidates

For `DefaultPackagingImpl`, distance pricing, Drools decisions, and provider selection,
record legacy tier, data volume, set-vs-row behavior, call frequency, app-tier risk, and
default app-tier placement in the completion summary. The likely target is app-tier policy
with MS-12 adapters, but P4b must make the final placement decision.
