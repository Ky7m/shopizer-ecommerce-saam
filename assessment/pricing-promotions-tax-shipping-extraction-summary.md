# Pricing, Promotions, Tax, and Shipping - Extraction Summary

## Segment Profile

- Scope: product pricing, promotions, tax classes/rates, shipping origins/quotes/options, packaging, and Drools shipping rules.
- Modules: `sm-core`, `sm-core-model`, `sm-shop`.
- Business rules extracted: 36.
- Discovery: direct source read; high confidence for current implementation, medium/low for intended behavior where TODOs or defects are present.

## Call Graph

```text
ProductPriceUtils -> product/variant availability -> primary/additional prices
Cart/order total -> item prices -> order-total processors -> shipping -> tax -> grand total
ShippingService -> origin/country/module selection -> preprocessors/provider
  -> option selection -> quote persistence
TaxService -> configured address basis -> tax class/rate queries -> tax items
ShippingDecisionPreProcessor -> package facts -> KIE ShippingDecision.drl
```

## Business Rules

| ID | Rule | Source reference |
|---|---|---|
| BR-PRC-001 | Default-selected variant availability precedes product availability; wildcard region and usable price required. | `ProductPriceUtils.calculateFinalPrice:550-584` |
| BR-PRC-002 | Default price is primary; other prices become additional/order-total lines. | `ProductPriceUtils:584-612`; `OrderServiceImpl.caculateOrder:217-274` |
| BR-PRC-003 | Special price activates for valid open/date windows. | `ProductPriceUtils.finalPrice:651-704` |
| BR-PRC-004 | Discount percentage uses special/original amount and integer conversion. | `ProductPriceUtils.discountPrice:706-720` |
| BR-PRC-005 | Positive selected attribute prices increase final/original/discounted values depending on overload. | `ProductPriceUtils.getFinalPrice:92-177` |
| BR-PRC-006 | Customer-specific pricing delegates to ordinary product pricing; customer ignored. | `PricingServiceImpl:44-58` |
| BR-PRC-007 | Direct variant pricing returns null and may require fallback. | `PricingServiceImpl:116-120` |
| BR-PRC-008 | Active order-total processor list contains only promo processor; manufacturer/shipping processor disabled. | `ProcessorsConfiguration:45-51` |
| BR-PRC-009 | Nonblank promo code creates KIE session, inserts date/code, fires rules, and returns quantity-scaled reduction. | `PromoCodeCalculatorModule:63-116` |
| BR-PRC-010 | Visible `Test1234` promo gives 10% before `31-Oct-2025`; appears expired at analysis time. | `PromoCoupon.drl:10-16` |
| BR-PRC-011 | Promo reductions are positive and subtracted during subtotal assembly. | `PromoCodeCalculatorModule:96-109`; `OrderServiceImpl:285-301` |
| BR-PRC-012 | Manufacturer/shipping-code discount processor is inactive. | `ProcessorsConfiguration:45-51`; `ManufacturerShippingCodeOrderTotalModuleImpl:86-101` |
| BR-PRC-013 | Grand total sequence is items, additional prices, variations, shipping, handling, tax. | `OrderServiceImpl.caculateOrder:217-394` |
| BR-PRC-014 | Tax address basis is shipping, billing, or store address; default is shipping. | `TaxServiceImpl.calculateTax:108-139` |
| BR-PRC-015 | Different-province tax can be suppressed, returning null. | `TaxServiceImpl.calculateTax:141-165` |
| BR-PRC-016 | Different-country tax can replace customer address with store address. | `TaxServiceImpl.calculateTax:167-173` |
| BR-PRC-017 | Amounts aggregate by tax class; missing class uses default. | `TaxServiceImpl.calculateTax:181-210` |
| BR-PRC-018 | Positive shipping/handling are currently added to default tax class; configured conditional logic is commented. | `TaxServiceImpl.calculateTax:213-244` |
| BR-PRC-019 | Piggyback rates compound on running taxed amount; others use original class amount. | `TaxServiceImpl.calculateTax:265-287` |
| BR-PRC-020 | Same tax codes are intended to consolidate but aggregate is not written back. | `TaxServiceImpl.calculateTax:299-318` |
| BR-PRC-021 | Visible tax-rate repository queries do not explicitly constrain tax class. | `TaxServiceImpl:253-261`; `TaxRateRepository:53-57` |
| BR-PRC-022 | Configured active shipping origin is used; otherwise store address is synthesized. | `ShippingServiceImpl:getShippingQuote:399-414`; `ShippingFacadeImpl:getShippingOrigin:120-171` |
| BR-PRC-023 | National shipping requires same country; international requires supported destination. | `ShippingServiceImpl:getShippingQuote:426-453` |
| BR-PRC-024 | First active non-pre/postprocessor shipping module becomes primary provider. | `ShippingServiceImpl:getShippingQuote:457-486` |
| BR-PRC-025 | Preprocessors may replace the selected shipping module. | `ShippingServiceImpl:522-551`; `ShippingDecisionPreProcessorImpl:53-164` |
| BR-PRC-026 | Free shipping threshold is exclusive (`orderTotal > threshold`). | `ShippingServiceImpl:getShippingQuote:496-520` |
| BR-PRC-027 | Shipping selection supports highest, least, or all options; comparisons use long values. | `ShippingServiceImpl:getShippingQuote:570-627` |
| BR-PRC-028 | Final shipping options persist as quotes with cart, destination, module, price, handling, and dates. | `ShippingServiceImpl:681-748`; `ShippingQuoteServiceImpl:18-30` |
| BR-PRC-029 | Packaging mode is BOX or ITEM; default is ITEM. | `ShippingServiceImpl:getPackagesDetails:871-892`; `ShippingConfiguration.java:19-24` |
| BR-PRC-030 | Virtual products excluded; missing weight/dimensions default; quantity expands package entries. | `DefaultPackagingImpl:getItemPackagesDetails:313-399` |
| BR-PRC-031 | Box fit requires remaining volume × .75 and remaining weight to cover item. | `DefaultPackagingImpl:getBoxPackagesDetails:213-252` |
| BR-PRC-032 | Generated package weights may use current-loop box weight for every package. | `DefaultPackagingImpl:getBoxPackagesDetails:294-310` |
| BR-PRC-033 | Distance pricing requires distance, rejects >150 km, and uses hard-coded per-km rates. | `PriceByDistanceShippingQuoteRules:59-132` |
| BR-PRC-034 | Google preprocessing geocodes origin/destination and stores Distance Matrix kilometers. | `GoogleDistanceMatrixPreProcessor` implementation |
| BR-PRC-035 | Shipping decision aggregates package facts and executes `ShippingDecision.drl`. | `ShippingDecisionPreProcessorImpl:53-164`; `ShippingDecision.drl:7-23` |
| BR-PRC-036 | Price-by-distance Drools rules use hard-coded distance bands; overlapping rules lack salience. | `PriceByDistance.drl:7-19`; `PriceByDistance2.drl:7-26` |

## Data Access and Integrations

| Area | Behavior |
|---|---|
| Pricing | Product price/availability/attribute and variant repositories; default/additional/special prices. |
| Tax | Tax classes/rates, merchant configuration, customer/store addresses, tax item aggregation. |
| Shipping | Shipping origin/configuration, supported countries, modules/processors, quotes, packaging boxes/items. |
| Rules | KIE/Drools promo and shipping rule files. |
| External | Google geocoding/distance, carrier modules, store pickup, configured shipping providers. |

## Layer A/B/C Flags

- Lifecycle/invariants: price availability, special-price window, tax class/rate applicability, shipping quote selection, package fit.
- Extensibility: order-total processors, promo/shipping Drools, shipping module registry, packaging mode, merchant tax/shipping configuration.
- Placement candidates: price/tax aggregation, package calculation, carrier calls, distance lookup, quote persistence; default app/integration tier pending P4b.

## Source Semantic Vectors

| Component family | Control | Data | Constants | States | Outcomes | Writes | Integrations | Errors |
|---|---:|---:|---:|---:|---:|---:|---:|---:|
| Pricing/product price | 146 | 91 | 45 | 16 | 49 | 0 | 12 | 48 |
| Tax services/repositories | 103 | 82 | 19 | 15 | 31 | 4 | 9 | 38 |
| Shipping services/providers | 291 | 164 | 87 | 24 | 73 | 8 | 46 | 151 |
| Packaging/rules | 117 | 76 | 35 | 11 | 32 | 3 | 15 | 61 |

## Clarification Items

Confirm customer-price requirements, variant pricing fallback, promo expiry, tax-on-shipping policy,
tax-code aggregation, tax-class query filtering, free-shipping quote persistence, monetary comparison/rounding,
box-weight defect, distance-rule ownership, and active carrier configuration.
