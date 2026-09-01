# MS-04 Cart and Checkout — Extraction Evidence

**Engagement:** Shopizer 3.2.7  
**Service:** MS-04 Cart and Checkout  
**CAST application:** `Shopizer-Backend`  
**CAST delivery:** `Onboarding-202511171247`  
**Analysis mode:** Hybrid  
**Local root:** `initial-source/shopizer-3.2.7/`

## Source files processed

All mandatory business-logic files from `assessment/ms-04-cast-brief.md` were located and read in full. Files over 500 LOC were read in multiple sections according to the Java legacy source-reading protocol.

| # | File | Total lines | Sections read | Sections read summary | Rules extracted | Vectors |
|---:|---|---:|---|---|---:|---|
| 1 | `sm-shop/src/main/java/com/salesmanager/shop/store/api/v1/shoppingCart/ShoppingCartApi.java` | 303 | 1-303 | Controller mappings for add, update, multi-update, retrieval, customer cart, deletion, and promotion | 4 | ✅ |
| 2 | `sm-shop/src/main/java/com/salesmanager/shop/store/controller/shoppingCart/facade/ShoppingCartFacadeImpl.java` | 1164 | 1-1000; 1001-1164 | 1-1000: cart creation, SKU/attribute validation, merge, update, deletion, hydration calls; 1001-1164: customer cart, code lookup, promotion, order association | 8 | ✅ |
| 3 | `sm-core/src/main/java/com/salesmanager/core/business/services/shoppingcart/ShoppingCartServiceImpl.java` | 513 | 1-500; 501-513 | 1-500: customer/store cart retrieval, hydration, price refresh, orphan cleanup, shipping filtering, cart merge; 501-513: cart-line attribute deletion | 6 | ✅ |
| 4 | `sm-core/src/main/java/com/salesmanager/core/business/services/shoppingcart/ShoppingCartCalculationServiceImpl.java` | 121 | 1-121 | Calculation preconditions, delegation to order total service, cart persistence refresh | 1 | ✅ |
| 5 | `sm-shop/src/main/java/com/salesmanager/shop/store/api/v1/order/OrderApi.java` | 520 | 1-500; 501-520 | 1-500: order reads, authenticated and anonymous checkout, customer/password handling; 501-520: administrative status endpoint boundary | 3 | ✅ |
| 6 | `sm-shop/src/main/java/com/salesmanager/shop/store/controller/order/facade/OrderFacadeImpl.java` | 1648 | 1-1000; 1001-1648 | 1-1000: order initialization, legacy order mapping, validation, shipping preparation; 1001-1648: shipping summary, checkout process, amount comparison, cart completion, notification, payment administration boundary | 8 | ✅ |
| 7 | `sm-shop/src/main/java/com/salesmanager/shop/populator/order/PersistableOrderApiPopulator.java` | 221 | 1-221 | Currency, customer, address, order attributes, API channel, initial order status, payment module mapping | 2 | ✅ |
| 8 | `sm-shop/src/main/java/com/salesmanager/shop/populator/order/OrderProductPopulator.java` | 191 | 1-191 | Product re-resolution, merchant check, digital download metadata, line price snapshot, attribute snapshot | 2 | ✅ |
| 9 | `sm-core/src/main/java/com/salesmanager/core/business/services/order/OrderServiceImpl.java` | 680 | 1-500; 501-680 | 1-500: payment-first persistence sequence, inventory decrement, total formula, promo expiry; 501-680: cart total overload, download detection, capturable-order query | 6 | ✅ |
| 10 | `sm-core/src/main/java/com/salesmanager/core/business/services/order/ordertotal/OrderTotalServiceImpl.java` | 74 | 1-74 | Configured processor list, product lookup, per-line variation fan-out | 1 | ✅ |
| 11 | `sm-shop/src/main/java/com/salesmanager/shop/store/api/v1/order/OrderTotalApi.java` | 224 | 1-224 | Authenticated and public total endpoints, optional quote lookup, cart/customer checks, total response mapping | 2 | ✅ |
| 12 | `sm-shop/src/main/java/com/salesmanager/shop/store/api/v1/order/OrderShippingApi.java` | 290 | 1-290 | Authenticated shipping lookup, anonymous postal/country construction, quote response and localized options | 2 | ✅ |
| 13 | `sm-core/src/main/java/com/salesmanager/core/business/services/shipping/ShippingQuoteServiceImpl.java` | 78 | 1-78 | Quote lookup and conversion to shipping summary, handling and tax-on-shipping flags | 1 | ✅ |
| 14 | `sm-shop/src/main/java/com/salesmanager/shop/store/api/v1/order/OrderPaymentApi.java` | 362 | 1-362 | Public/authenticated payment initialization, cart/customer ownership checks, provider response; capture/refund/authorize stubs recorded as exclusions | 1 | ✅ |
| 15 | `sm-shop/src/main/java/com/salesmanager/shop/populator/order/transaction/PersistablePaymentPopulator.java` | 67 | 1-67 | Payment amount normalization, module/type/transaction mapping, payment-token metadata | 1 | ✅ |

## Supporting model files processed

| # | File | Total lines | Sections read | Evidence captured |
|---:|---|---:|---|---|
| 16 | `sm-core-model/src/main/java/com/salesmanager/core/model/shoppingcart/ShoppingCart.java` | 191 | 1-191 | `SHOPPING_CART`, cart code, merchant, customer, order, IP, promotion, transient obsolete marker |
| 17 | `sm-core-model/src/main/java/com/salesmanager/core/model/shoppingcart/ShoppingCartItem.java` | 251 | 1-251 | `SHOPPING_CART_ITEM`, quantity, product ID, SKU, variant, transient price/subtotal/final-price/product fields |
| 18 | `sm-core-model/src/main/java/com/salesmanager/core/model/shoppingcart/ShoppingCartAttributeItem.java` | 120 | 1-120 | Attribute ID and cart-line relationship |
| 19 | `sm-shop-model/src/main/java/com/salesmanager/shop/model/shoppingcart/ShoppingCartData.java` | 90 | 1-90 | API cart code, quantity, totals, order ID, totals list, unavailable lines |
| 20 | `sm-shop-model/src/main/java/com/salesmanager/shop/model/shoppingcart/ShoppingCartEntity.java` | 14 | 1-14 | Base API entity context; no independent business rule |
| 21 | `sm-core-model/src/main/java/com/salesmanager/core/model/order/OrderTotalSummary.java` | 56 | 1-56 | Subtotal, total, tax total, total-line list |
| 22 | `sm-core-model/src/main/java/com/salesmanager/core/model/order/OrderTotal.java` | 156 | 1-156 | `ORDER_TOTAL`, code, title, text, value, module, type, sort order |
| 23 | `sm-core-model/src/main/java/com/salesmanager/core/model/shipping/ShippingSummary.java` | 86 | 1-86 | Shipping amount, handling, module, option, free-shipping, tax-on-shipping flags |
| 24 | `sm-core-model/src/main/java/com/salesmanager/core/model/shipping/ShippingQuote.java` | 138 | 1-138 | Quote return codes, options, free-shipping threshold, handling, tax, selected option, warnings |
| 25 | `sm-core-model/src/main/java/com/salesmanager/core/model/payments/Payment.java` | 65 | 1-65 | Payment type, transaction type, module, currency, amount, metadata |
| 26 | `sm-core-model/src/main/java/com/salesmanager/core/model/payments/Transaction.java` | 187 | 1-187 | `SM_TRANSACTION`, order reference, amount, date, transaction type, payment type, details |
| 27 | `sm-core-model/src/main/java/com/salesmanager/core/model/order/orderproduct/OrderProduct.java` | 140 | 1-140 | `ORDER_PRODUCT`, SKU, name, quantity, one-time charge, order relation, price/attribute/download relations |
| 28 | `sm-core-model/src/main/java/com/salesmanager/core/model/order/orderproduct/OrderProductAttribute.java` | 134 | 1-134 | `ORDER_PRODUCT_ATTRIBUTE`, attribute price/free/weight, option and value IDs/names |
| 29 | `sm-core-model/src/main/java/com/salesmanager/core/model/order/orderproduct/OrderProductPrice.java` | 140 | 1-140 | `ORDER_PRODUCT_PRICE`, price code/value/special values/date range/default flag/name |
| 30 | `sm-core-model/src/main/java/com/salesmanager/core/model/order/orderproduct/OrderProductDownload.java` | 91 | 1-91 | `ORDER_PRODUCT_DOWNLOAD`, filename, max days, download count |
| 31 | `sm-shop/src/main/java/com/salesmanager/shop/store/api/v1/order/OrderTotalApi.java` | 224 | 1-224 | Re-read as a supporting boundary for total DTO shape and quote input |
| 32 | `sm-shop/src/main/java/com/salesmanager/shop/store/api/v1/order/OrderShippingApi.java` | 290 | 1-290 | Re-read as a supporting boundary for address and shipping option shape |
| 33 | `sm-core/src/main/java/com/salesmanager/core/business/services/shipping/ShippingQuoteServiceImpl.java` | 78 | 1-78 | Re-read as a supporting boundary for quote persistence semantics |
| 34 | `sm-shop/src/main/java/com/salesmanager/shop/store/api/v1/order/OrderPaymentApi.java` | 362 | 1-362 | Re-read as a supporting boundary for authenticated/public payment-init behavior |

## Hidden calculation and configuration files processed

| File | Total lines | Sections read | Findings |
|---|---:|---|---|
| `sm-core/src/main/java/com/salesmanager/core/business/configuration/ProcessorsConfiguration.java` | 55 | 1-55 | Only `PromoCodeCalculatorModule` is active; manufacturer shipping processor is commented out |
| `sm-core/src/main/java/com/salesmanager/core/business/modules/order/total/PromoCodeCalculatorModule.java` | 118 | 1-118 | Reads promo code, invokes Drools, obtains product price, computes discount allocation |
| `sm-core/src/main/resources/com/salesmanager/drools/rules/PromoCoupon.drl` | 16 | 1-16 | `Test1234`, 10% discount, date before `31-Oct-2025` |
| `sm-core/src/main/resources/com/salesmanager/drools/rules/ShippingDecision.drl` | 25 | 1-25 | Canada routing by weight, size, and Quebec province |
| `sm-core/src/main/resources/com/salesmanager/drools/rules/PriceByDistance.drl` | 21 | 1-21 | Distance tiers at 530 and 3550 with prices 75 and 140 |
| `sm-core/src/main/resources/com/salesmanager/drools/rules/PriceByDistance2.drl` | 28 | 1-28 | Distance tiers at 40, 80, and 2550 with prices 75, 120, and 140 |

## Source-reference line evidence

| Rule family | Exact source ranges |
|---|---|
| Cart code and creation | `ShoppingCartFacadeImpl.java:440-456`, `:756-779`, `:1080-1082` |
| Product/store/availability validation | `ShoppingCartFacadeImpl.java:254-342`, `:346-429`; `ShoppingCartServiceImpl.java:440-489` |
| Attribute validation and orphan cleanup | `ShoppingCartFacadeImpl.java:323-340`, `:408-425`; `ShoppingCartServiceImpl.java:309-353` |
| Duplicate merge | `ShoppingCartFacadeImpl.java:150-171`, `:830-868`; `ShoppingCartServiceImpl.java:390-438` |
| Quantity update and removal | `ShoppingCartFacadeImpl.java:877-927`, `:968-1022`; `ShoppingCartServiceImpl.java:491-511` |
| Cart hydration and pricing | `ShoppingCartServiceImpl.java:230-359`; `ShoppingCartCalculationServiceImpl.java:65-119` |
| Shipping-product filtering | `ShoppingCartServiceImpl.java:361-383`; `OrderFacadeImpl.java:1151-1189` |
| Shipping address fallback | `OrderFacadeImpl.java:1151-1189`; `OrderShippingApi.java:94-167`, `:207-279` |
| Total formula | `OrderServiceImpl.java:217-394`; `OrderTotalApi.java:120-144`, `:190-213` |
| Promotion expiry | `ShoppingCartFacadeImpl.java:1107-1132`, `:1150-1160`; `OrderServiceImpl.java:430-461` |
| Authenticated ownership | `OrderApi.java:343-392`; `OrderShippingApi.java:94-120`; `OrderTotalApi.java:91-118`; `OrderPaymentApi.java:128-169` |
| Anonymous checkout customer | `OrderApi.java:402-471`; `PersistableOrderApiPopulator.java:93-112` |
| Checkout snapshot | `OrderFacadeImpl.java:1208-1328`; `OrderProductPopulator.java:60-153` |
| Amount equality | `OrderFacadeImpl.java:1261-1294`; `PersistablePaymentPopulator.java:25-50` |
| Payment handoff | `OrderPaymentApi.java:88-181`; `PaymentServiceImpl.java:299-393`; `PaymentServiceImpl.java:739-777` |
| Legacy orchestration | `OrderFacadeImpl.java:1196-1359`; `OrderServiceImpl.java:127-214` |
| Notification boundary | `OrderFacadeImpl.java:1361-1380` |
| Explicit target idempotency | No legacy implementation found; source search covered the inspected Java/configuration tree; target rule BR-CO-IDM-017 |
| Explicit target checkout lifecycle | `ShoppingCartServiceImpl.java:230-264`; `OrderFacadeImpl.java:1327-1336`; no dedicated legacy checkout state found |
| Payment administrative exclusions | `OrderPaymentApi.java:292-361`; capture/refund/authorize methods return `null` or contain commented implementation |

## CAST transaction evidence

| CAST ID | Operation | Graph size | Use in extraction |
|---:|---|---:|---|
| 244210 | POST `/api/v1/cart/` | 1093 objects | Cart creation and add flow |
| 244214 | GET `/api/v1/cart/{code}/` | 1049 objects | Cart retrieval and hydration |
| 244211 | PUT `/api/v1/cart/{code}/` | 1116 objects | Cart update |
| 244213 | POST `/api/v1/cart/{code}/multi/` | 1109 objects | Multi-line update |
| 244212 | POST `/api/v1/cart/{code}/promo/{code}/` | 1052 objects | Promotion association |
| 244089 | POST `/api/v1/auth/cart/{code}/checkout/` | 3245 objects | Authenticated checkout |
| 244090 | POST `/api/v1/cart/{code}/checkout/` | 3262 objects | Anonymous checkout |
| 244101 | GET `/api/v1/auth/cart/{code}/shipping/` | 1202 objects | Authenticated shipping |
| 244102 | POST `/api/v1/cart/{code}/shipping/` | 1192 objects | Anonymous shipping |
| 244105 | GET `/api/v1/auth/cart/{code}/total/` | 662 objects | Authenticated totals |
| 244106 | GET `/api/v1/cart/{code}/total/` | 705 objects | Anonymous totals |
| 244094 | POST `/api/v1/auth/cart/{code}/payment/init/` | 643 objects | Authenticated payment initialization |
| 244093 | POST `/api/v1/cart/{code}/payment/init/` | 616 objects | Anonymous payment initialization |
| 244217 | GET `/api/v1/auth/customer/cart/` | 1129 objects | Authenticated cart retrieval |
| 244216 | GET `/api/v1/auth/customer/{id}/cart/` | 1122 objects | Deprecated customer-ID cart retrieval |

## CAST data-graph evidence

| CAST graph | Entity | Use |
|---:|---|---|
| 243932 | `shopping_cart` | Cart ownership and cart aggregate |
| 243920 | `shopping_cart_item` | Cart line and attribute persistence |
| 243908 | `orders` | Boundary-only order graph; assigned to MS-05 |
| 243909 | `order_product` | Boundary-only immutable order-line graph; assigned to MS-05 |
| 243929 | `sm_transaction` | Boundary-only payment transaction graph; assigned to MS-06 |
| 243922 | `product_price` | MS-07 pricing dependency |
| 243933 | `shipping_quote` | MS-09 shipping dependency |
| 243914 | `customer` | MS-01 identity/address dependency |
| 243912 | `product` | MS-02 product dependency |
| 243945 | `product_availability` | MS-02 availability dependency |
| 243919 | `tax_class` | MS-08 tax dependency |

## Source semantic vectors

Vectors below are the full direct-read vectors recorded for the primary CAST components. Counts include infrastructure behavior while the rule preservation tables classify business-relevant portions.

| Component | Control | Data | Constants | States | Outcomes | Writes | Integrations | Errors |
|---|---:|---:|---:|---:|---:|---:|---:|---:|
| `ShoppingCartApi` | 17 | 16 | 5 | 0 | 16 | 0 | 5 | 8 |
| `ShoppingCartFacadeImpl` | 45 | 28 | 15 | 0 | 21 | 4 | 10 | 20 |
| `ShoppingCartServiceImpl` | 42 | 30 | 12 | 7 | 17 | 8 | 8 | 17 |
| `ShoppingCartCalculationServiceImpl` | 6 | 10 | 0 | 0 | 3 | 1 | 1 | 2 |
| `OrderApi` | 27 | 13 | 5 | 4 | 15 | 0 | 4 | 8 |
| `OrderFacadeImpl` | 96 | 70 | 18 | 11 | 30 | 10 | 17 | 35 |
| `OrderServiceImpl` | 62 | 45 | 15 | 12 | 22 | 9 | 7 | 20 |
| `OrderTotalServiceImpl` | 10 | 10 | 0 | 0 | 3 | 0 | 1 | 3 |
| `OrderProductPopulator` | 29 | 25 | 9 | 0 | 10 | 5 | 3 | 12 |
| `OrderPaymentApi` | 24 | 16 | 4 | 0 | 9 | 0 | 4 | 8 |
| `PersistablePaymentPopulator` | 4 | 9 | 2 | 0 | 3 | 1 | 1 | 3 |
| `OrderTotalApi` | 12 | 13 | 2 | 0 | 7 | 0 | 3 | 5 |
| `OrderShippingApi` | 14 | 18 | 3 | 0 | 8 | 0 | 4 | 7 |
| `ShippingQuoteServiceImpl` | 3 | 8 | 0 | 0 | 2 | 0 | 1 | 2 |
| `PromoCodeCalculatorModule` | 10 | 10 | 3 | 0 | 5 | 0 | 1 | 4 |

## Hidden-engine and exclusion findings

- `ProcessorsConfiguration.java:45-51` activates `PromoCodeCalculatorModule`; the manufacturer shipping processor is commented out.
- `PromoCodeCalculatorModule.java:63-115` invokes Drools and pricing services. It is recorded as a pricing/promotion dependency, not MS-04-owned discount logic.
- `PromoCoupon.drl:10-16` contains `Test1234`, 10%, and `31-Oct-2025`.
- `ShippingDecision.drl:9-25` contains Canada routing by weight, size, and province.
- `PriceByDistance.drl:9-21` and `PriceByDistance2.drl:9-28` contain shipping-distance price tiers.
- `OrderPaymentApi.java:292-361` contains capture, refund, and authorize endpoints that return `null`; they are excluded from MS-04.
- No legacy idempotency mechanism was found.
- No explicit legacy checkout-session lifecycle was found.
- No stored procedure, scheduled batch, or database-resident MS-04 decision engine was found.
- The queried CAST APIs did not provide a direct unreachable/dead-code result for this brief; no component was marked dead solely from absent visible callers.

## Extraction status

- Files total in mandatory business-logic list: 15
- Mandatory files processed: 15
- Supporting files processed: 19
- Unique source files recorded: 34
- Business rules extracted: 20
- Source vectors complete: yes
- Exact source line ranges recorded: yes
- CAST transaction references recorded: yes
- Cross-service ownership recorded: yes
- Hidden calculation engines recorded: yes
- Placement evidence recorded: yes
- Idempotency finding recorded: yes
- Checkout lifecycle finding recorded: yes

## Session log

| Session | Files processed | Rules added | Notes |
|---|---|---:|---|
| 1 | Cart API, cart facade, cart service, cart calculation, cart models | 7 | Cart creation, SKU validation, attributes, merge, update, hydration |
| 2 | Checkout API, order facade, order service, order populators | 8 | Checkout identity, customer handling, snapshot, amount comparison, orchestration |
| 3 | Total, shipping, payment boundaries and models | 3 | Shipping, totals, payment handoff |
| 4 | Processor configuration and Drools files | 2 | Hidden engines and ownership boundaries |
| 5 | Target-only lifecycle/idempotency/outbox rules | 0 additional source rules | Target architecture additions documented explicitly |
