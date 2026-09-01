# MS-07 Pricing and Promotions — CAST-Guided Phase 4 Brief

## Scope

- **Service:** MS-07 Pricing and Promotions
- **CAST application:** `Shopizer-Backend`
- **Analysis mode:** Hybrid — CAST transaction/data-graph scope followed by direct Java source reading
- **Ownership:** Product and variant price selection, special-price windows, attribute price adjustments, promotion-code evaluation, and pricing processor registration
- **Inputs:** Product availability, regional price records, variant selections, product attributes, promotion codes, order quantity, currency/store context
- **Outputs:** Final product prices, discount metadata, promotion reductions, and price-management responses
- **Out of scope:** Tax calculation (MS-08), shipping quotes and packaging (MS-09), catalog/product persistence ownership (MS-02), and payment/order lifecycle ownership (MS-06/MS-05)

## CAST entry points and data scope

| Transaction ID | Entry point | Reduced/full size | CAST evidence |
|---|---|---:|---|
| `244173` | `GET /api/v1/private/product/{productId}/price/` | 137 / 3009 | Product price retrieval and full pricing call graph |
| `244172` | `GET /api/v1/private/product/{productId}/price/{priceId}/` | 137 / 3014 | Single price retrieval |
| `244174` | `GET /api/v1/private/product/{productId}/prices/` | 137 / 3009 | Product price collection |
| `244170` | `POST /api/v1/private/product/{productId}/price/` | 40 / 230 | Product price creation |
| `244171` | `PUT /api/v1/private/product/{productId}/price/{priceId}/` | 40 / 229 | Product price update |
| `244169` | `POST /api/v1/private/product/{productId}/inventory/{availabilityId}/price/` | 40 / 230 | Availability price creation |
| `244175` | `DELETE /api/v1/private/product/{productId}/price/{priceId}/` | 27 / 69 | Product price deletion |

CAST data graph `243922` identifies `salesmanager.product_price` as the primary price entity. The highest-complexity transaction objects include `populate` methods, `calculateFinalPrice`, `finalPrice`, `calculateProductPiceVariation`, and price retrieval/merge paths.

## Source files to read

| Priority | Local path | Why it is in scope |
|---:|---|---|
| 1 | `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/utils/ProductPriceUtils.java` | Final-price selection, regional availability, special-price dates, discount percentage, and attribute-price overloads; 721 LOC requires multi-pass reading |
| 2 | `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/catalog/product/price/ProductPriceServiceImpl.java` | Price CRUD, descriptions, availability association, and repository behavior |
| 3 | `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/catalog/pricing/PricingServiceImpl.java` | Customer/variant pricing delegation and fallback behavior |
| 4 | `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/api/v1/product/ProductPriceApi.java` | Administrative product-price endpoint contract and authorization/context |
| 5 | `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/facade/product/ProductPriceFacadeImpl.java` | API-to-service mapping, price DTO conversion, and error behavior |
| 6 | `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/modules/order/total/PromoCodeCalculatorModule.java` | Promotion-code KIE session, reduction calculation, quantity scaling, and invalid-code behavior |
| 7 | `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/configuration/ProcessorsConfiguration.java` | Active/inactive order-total processor registration |
| 8 | `initial-source/shopizer-3.2.7/sm-core/src/main/resources/com/salesmanager/drools/rules/PromoCoupon.drl` | Promotion-code conditions, date window, and percentage constants |
| 9 | `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/repositories/catalog/product/price/ProductPriceRepository.java` | Price entity query and persistence semantics |
| 10 | `initial-source/shopizer-3.2.7/sm-core-model/src/main/java/com/salesmanager/core/model/catalog/product/price/ProductPrice.java` | Core price fields and lifecycle data |
| 11 | `initial-source/shopizer-3.2.7/sm-core-model/src/main/java/com/salesmanager/core/model/catalog/product/price/FinalPrice.java` | Calculated-price, discount, and date representation |
| 12 | `initial-source/shopizer-3.2.7/sm-shop-model/src/main/java/com/salesmanager/shop/model/catalog/product/ProductPriceRequest.java` | Write request fields and API validation surface |
| 13 | `initial-source/shopizer-3.2.7/sm-shop-model/src/main/java/com/salesmanager/shop/model/catalog/product/ProductPriceEntity.java` | Read/write price DTO shape and serialized fields |
| 14 | `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/order/OrderServiceImpl.java` | Price and promotion contribution to order-total sequencing; read only the pricing-relevant calculation sections |

## Source files to skip

| Path/family | Reason |
|---|---|
| `TaxServiceImpl.java`, tax repositories, and tax models | MS-08 ownership; pricing extractor may record only the declared boundary |
| `ShippingServiceImpl.java`, shipping providers, packaging, and shipping Drools files | MS-09 ownership |
| Product/category persistence implementation except price repository | MS-02 owns catalog mutations |
| Payment, order-state, and checkout controllers | MS-06/MS-05/MS-04 ownership |
| Hibernate/JPA framework classes and generated metadata | Infrastructure rather than pricing behavior |

## Data and integration signals

- Price selection depends on availability region, default-price designation, validity dates, and variant/attribute adjustments.
- Product price administration has seven CAST transactions with a 3,014-object full graph hotspot.
- Promotion evaluation uses a KIE/Drools boundary and is wired as the active promotion order-total processor.
- Pricing is consumed by product presentation and order-total calculation; monetary semantics must be reconciled with MS-04.
- Store/customer-specific pricing is present in the source boundary; the deep read must confirm whether customer identity is intentionally ignored or is a preservation defect.

## Phase 1 rules requiring Phase 4 deep extraction

- `BR-PRC-001` through `BR-PRC-007` — product/variant availability, default/additional prices, special windows, discount percentage, attribute adjustments, customer pricing delegation, and direct variant fallback.
- `BR-PRC-008` through `BR-PRC-013` — promotion processor activation, promo-code KIE evaluation, visible coupon behavior, disabled manufacturer/shipping processor, and order-total sequencing where pricing participates.

## Hidden-engine check

This is not CRUD-only. The primary price retrieval transaction has **3,009–3,014 full-graph objects** and a reduced graph of **137 objects**, while pricing utility complexity includes final-price selection, special-price date windows, regional availability, discount math, and attribute variation. The residual is a pricing engine behind the product-price API, with a separate promotion rules engine behind the order-total processor. Both require deep extraction; the API CRUD surface is not an adequate scope.

## Cross-service dependencies

- **MS-02:** consumes product and availability references; MS-07 does not own catalog entities outside pricing records.
- **MS-04:** provides calculated item/promotion totals to cart and checkout flows through a declared contract.
- **MS-05:** may consume persisted price/promotion snapshots for order consistency.
- **MS-08/MS-09:** tax and shipping are downstream total components and must not be implemented as hidden MS-07 table reads.
- **MS-12:** external promotion/provider adapters, if confirmed by the source read, remain behind a neutral boundary.

## CAST limitations

CAST exposes the product-price transaction and data-graph scope clearly, but promotion behavior is represented partly through component discovery and Drools resources rather than a distinct named transaction. The extractor must read `PromoCodeCalculatorModule.java` and `PromoCoupon.drl` directly and must not invent undocumented promotion providers or customer-pricing semantics.
