# MS-07 Pricing and Promotions — Extraction Evidence

## Extraction method

- **Engagement:** Shopizer 3.2.7
- **Service:** MS-07 Pricing and Promotions
- **Application:** `Shopizer-Backend`
- **Analysis mode:** Hybrid — CAST transaction/data-graph scope followed by direct Java source reading
- **Source root:** `initial-source/`
- **CAST path mapping:** `Shopizer-Backend` → `initial-source/shopizer-3.2.7/`
- **Rules extracted:** 13
- **Rule range:** `BR-PRC-001` through `BR-PRC-013`
- **CAST transactions covered:** 7
- **CAST data graphs covered:** 1
- **Source vectors:** Counted for every extracted rule across control-flow, data-flow, constants, state transitions, outcomes, data writes, integrations, and error paths

## CAST scope

| Transaction ID | CAST entry point | Reduced graph | Full graph | Use in extraction |
|---:|---|---:|---:|---|
| `244173` | `GET /api/v1/private/product/{productId}/price/` | 137 | 3009 | Primary product-price calculation and full pricing call graph |
| `244172` | `GET /api/v1/private/product/{productId}/price/{priceId}/` | 137 | 3014 | Single price retrieval |
| `244174` | `GET /api/v1/private/product/{productId}/prices/` | 137 | 3009 | Product price collection |
| `244170` | `POST /api/v1/private/product/{productId}/price/` | 40 | 230 | Product price creation |
| `244171` | `PUT /api/v1/private/product/{productId}/price/{priceId}/` | 40 | 229 | Product price update |
| `244169` | `POST /api/v1/private/product/{productId}/inventory/{availabilityId}/price/` | 40 | 230 | Availability price creation |
| `244175` | `DELETE /api/v1/private/product/{productId}/price/{priceId}/` | 27 | 69 | Product price deletion |

| CAST data graph | Primary entity | Use in extraction |
|---:|---|---|
| `243922` | `salesmanager.product_price` | Confirmed the primary legacy price entity and its availability relationship |

## Source files processed

| # | File | Lines | Sections read | Primary evidence | Vectors counted |
|---:|---|---:|---:|---|---|
| 1 | `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/utils/ProductPriceUtils.java` | 721 | 1-240: product, variant, and availability final-price overloads; 242-414: monetary formatting; 416-493: amount parsing; 495-548: quantity and discount helpers; 550-721: availability selection, special-price calculation, and discount percentage | BR-PRC-001, BR-PRC-002, BR-PRC-003, BR-PRC-004, BR-PRC-005, BR-PRC-007 | ✅ |
| 2 | `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/catalog/product/price/ProductPriceServiceImpl.java` | 78 | 18-28: service construction; 30-47: description and save/update behavior; 49-56: delete behavior; 58-74: product and availability queries | Price administration persistence and repository delegation | ✅ |
| 3 | `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/catalog/pricing/PricingServiceImpl.java` | 123 | 29-63: product, customer, attribute, and quantity delegation; 65-107: amount formatting/parsing delegation; 109-119: availability and direct variant methods | BR-PRC-006, BR-PRC-007 | ✅ |
| 4 | `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/api/v1/product/ProductPriceApi.java` | 183 | 39-50: controller and route base; 52-90: create operations; 92-113: update operation; 115-165: get/list operations; 167-181: delete operation | Seven source-backed price administration operations | ✅ |
| 5 | `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/facade/product/ProductPriceFacadeImpl.java` | 150 | 28-65: mapping and save/update; 68-104: list operations; 107-123: delete and not-found behavior; 125-148: response population and get behavior | Price DTO conversion, store validation, and error behavior | ✅ |
| 6 | `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/modules/order/total/PromoCodeCalculatorModule.java` | 118 | 26-60: processor identity; 62-116: validation, rule-session creation, promotion evaluation, reduction calculation, and result construction | BR-PRC-009, BR-PRC-011 | ✅ |
| 7 | `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/configuration/ProcessorsConfiguration.java` | 55 | 34-53: order-total processor registration | BR-PRC-008, BR-PRC-012 | ✅ |
| 8 | `initial-source/shopizer-3.2.7/sm-core/src/main/resources/com/salesmanager/drools/rules/PromoCoupon.drl` | 16 | 1-16: global response, `Bam0520` rule, `Test1234` code, date condition, and `0.10` discount | BR-PRC-010 | ✅ |
| 9 | `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/repositories/catalog/product/price/ProductPriceRepository.java` | 50 | 10-15: entity fetch; 32-48: product, price, and availability query predicates | Product/store/availability query semantics and store-isolation risk | ✅ |
| 10 | `initial-source/shopizer-3.2.7/sm-core-model/src/main/java/com/salesmanager/core/model/catalog/product/price/ProductPrice.java` | 168 | 32-80: entity, table, identifier, price, type, default, special-window, availability, and identifier fields; 85-165: accessors | Price DDL and DTO field mapping | ✅ |
| 11 | `initial-source/shopizer-3.2.7/sm-core-model/src/main/java/com/salesmanager/core/model/catalog/product/price/FinalPrice.java` | 125 | 14-32: calculated-price fields; 34-123: additional prices, discount metadata, and formatted values | Calculated response shape and computed-field provenance | ✅ |
| 12 | `initial-source/shopizer-3.2.7/sm-shop-model/src/main/java/com/salesmanager/shop/model/catalog/product/ProductPriceRequest.java` | 34 | 9-33: selected options and SKU request fields | Product/variant price-request context | ✅ |
| 13 | `initial-source/shopizer-3.2.7/sm-shop-model/src/main/java/com/salesmanager/shop/model/catalog/product/ProductPriceEntity.java` | 79 | 13-25: read/write price DTO fields; 27-72: discount, dates, default flag, code, and price accessors | Price administration response field mapping | ✅ |
| 14 | `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/order/OrderServiceImpl.java` | 680 | 217-274: item and additional-price subtotal; 276-311: promotion/variation subtraction and subtotal line; 314-350: shipping and handling sequencing; 353-379: tax sequencing; 381-394: grand total | BR-PRC-002, BR-PRC-011, BR-PRC-013 | ✅ |
| 15 | `initial-source/shopizer-3.2.7/sm-core-model/src/main/java/com/salesmanager/core/model/catalog/product/price/ProductPriceType.java` | 7 | 1-7: `ONE_TIME` and `MONTHLY` enum values | Confirmed target `OneTime` and `Monthly` price types | ✅ |
| 16 | `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/modules/order/total/ManufacturerShippingCodeOrderTotalModuleImpl.java` | 151 | 80-107: commented rule-session path, discount calculation, and order-total result | Confirmed inactive manufacturer/shipping-code discount behavior for BR-PRC-012 | ✅ |

## Rule-to-source coverage

| Rule ID | Rule seam | Direct source evidence | CAST evidence |
|---|---|---|---|
| BR-PRC-001 | Default-selected variant availability precedes product availability; wildcard region is required | `ProductPriceUtils.java:550-610` | Transaction `244173`, data graph `243922` |
| BR-PRC-002 | Default price is primary; non-default prices are additional and may affect subtotal | `ProductPriceUtils.java:578-603,614-647`; `OrderServiceImpl.java:235-274` | Transactions `244173`, `244172`, `244174` |
| BR-PRC-003 | Special-price activation uses bounded, open-start, and no-date branches | `ProductPriceUtils.java:510-530,651-704` | Transactions `244173`, `244172`, `244174` |
| BR-PRC-004 | Discount percentage uses original/special amounts and integer truncation | `ProductPriceUtils.java:699-720` | Transactions `244173`, `244172`, `244174` |
| BR-PRC-005 | Positive selected attribute adjustments are additive | `ProductPriceUtils.java:92-127,141-175` | Transaction `244173` |
| BR-PRC-006 | Customer-aware methods delegate to ordinary product pricing and ignore customer identity | `PricingServiceImpl.java:38-58` | Pricing call graph attached to `244173` |
| BR-PRC-007 | Direct variant pricing facade returns `null`; utility-level variant calculation exists separately | `PricingServiceImpl.java:109-119`; `ProductPriceUtils.java:180-223` | Pricing call graph attached to `244173` |
| BR-PRC-008 | Only the promotion processor is registered | `ProcessorsConfiguration.java:34-53` | Pricing/order-total processor path |
| BR-PRC-009 | Non-blank promotion code creates a rule session and scales reduction by quantity | `PromoCodeCalculatorModule.java:62-116` | Promotion processor path; no distinct CAST promotion transaction |
| BR-PRC-010 | `Test1234` receives 10% only before `31-Oct-2025` | `PromoCoupon.drl:1-16` | Rule resource reached through promotion processor |
| BR-PRC-011 | Positive promotion reductions are subtracted during subtotal assembly | `PromoCodeCalculatorModule.java:85-109`; `OrderServiceImpl.java:283-301` | Promotion/order-total path |
| BR-PRC-012 | Manufacturer/shipping-code processor is inactive | `ProcessorsConfiguration.java:45-51`; `ManufacturerShippingCodeOrderTotalModuleImpl.java:80-107` | Processor registration path |
| BR-PRC-013 | Pricing participates before shipping, handling, tax, and grand-total assembly | `OrderServiceImpl.java:217-394` | Transaction `244173` full pricing call graph |

## Source vector coverage

All 13 rules contain eight-dimensional preservation tables.

| Dimension | Coverage status | Notes |
|---|---|---|
| Control-flow | Complete | Counts include source branches, guards, loops, and rule conditions |
| Data-flow | Complete | Counts include distinct domain fields and relationships read or written |
| Constants | Complete | Counts include codes, dates, rates, enum values, resource paths, and sort/order constants |
| State transitions | Complete | Explicit target states are counted where the target makes implicit calculation/evaluation states observable |
| Outcomes | Complete | Counts include source returns, calculated results, null outcomes, and target response outcomes |
| Data writes | Complete | In-memory result construction is distinguished from durable database writes |
| Integrations | Complete | KIE/Drools, pricing delegation, catalog references, and order-total consumers are identified |
| Error paths | Complete | Validation, unavailable-price, not-found, invalid-window, inactive-processor, and dependency failures are identified |

## Extraction status

- **Files in CAST brief:** 14
- **Brief-listed files processed:** 14
- **Supplementary source files processed:** 2
- **Total source files processed:** 16
- **Rules extracted:** 13
- **Source vectors complete:** yes
- **All BR-PRC-001 through BR-PRC-013 present:** yes
- **Product-price CAST transactions covered:** 7 of 7
- **Primary CAST full-graph hotspot read through targeted source files:** yes
- **Promotion rule resource read directly:** yes
- **Executable PostgreSQL domain model produced:** yes
- **OpenAPI 3.1 contract produced:** yes

## Excluded and boundary evidence

The following source families were not processed for MS-07 because they belong to other services or were explicitly outside the CAST brief:

| Excluded path/family | Reason |
|---|---|
| `TaxServiceImpl.java`, tax repositories, and tax models | MS-08 owns tax calculation |
| `ShippingServiceImpl.java`, shipping providers, packaging, and shipping Drools files | MS-09 owns shipping quotes and packaging |
| Product/category persistence other than price access | MS-02 owns catalog facts and mutations |
| Payment, order-state, and checkout controllers | MS-06/MS-05/MS-04 ownership |
| Hibernate/JPA framework classes and generated metadata | Infrastructure rather than pricing behavior |
| Frontend React and Angular files | UI consumers; not required for the MS-07 service-owned calculation extraction |
| External or absent KIE provider implementation | The source exposes the KIE boundary but does not provide a separate provider implementation; no undocumented provider was fabricated |

## Not-found and unresolved evidence

No required CAST-listed source file was missing. No local implementation of a separate promotion-provider service was found or assumed. Promotion behavior is represented by `PromoCodeCalculatorModule.java` and `PromoCoupon.drl`.

The following behaviors remain source limitations rather than missing-file findings:

- Customer-specific pricing is not implemented; customer arguments are ignored.
- Direct variant pricing through `PricingServiceImpl` returns `null`.
- The visible `Test1234` promotion rule is expired at the analysis date `2026-09-01`.
- Promotion redemption reservation persistence is not present in the read source.
- Legacy `GET` price retrieval accepts a request body even though the target API removes it.
- Monetary scale calls in the order-total source do not assign the returned `BigDecimal` value; the target contract requires an explicit currency-rounding decision.

## Session log

| Session | Files processed | Rules added | Notes |
|---:|---:|---:|---|
| 1 | 1-8 | BR-PRC-001 through BR-PRC-012 | Read the pricing utility, service/facade/API surface, promotion module, processor registry, and promotion rule |
| 2 | 9-14 | BR-PRC-002, BR-PRC-011, BR-PRC-013 | Read repository/entity/DTO sources and pricing-relevant order-total assembly |
| 3 | 15-16 | BR-PRC-004, BR-PRC-007, BR-PRC-012 | Confirmed price-type enum and inactive manufacturer/shipping-code processor evidence; completed cross-reference reconciliation |

## Evidence conclusion

The CAST scope is not CRUD-only. The primary price retrieval path contains a 3,009–3,014-object full graph and reaches pricing utility logic for availability selection, special-price windows, discount arithmetic, attribute adjustments, and additional price aggregation. The promotion path adds a separate KIE/Drools evaluation boundary and order-total integration. The 13-rule MS-07 package therefore covers both the administrative price surface and the hidden pricing/promotion engines behind it.
