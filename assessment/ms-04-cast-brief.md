# MS-04 Cart and Checkout — CAST Scout Brief

**Engagement:** Shopizer 3.2.7  
**Service:** MS-04 Cart and Checkout  
**CAST application:** `Shopizer-Backend`  
**CAST delivery:** `Onboarding-202511171247`  
**Analysis mode:** Hybrid — CAST structural discovery plus targeted source review  
**Brief date:** 2026-09-01  
**Local source root:** `initial-source/shopizer-3.2.7/`

> This is a CAST discovery brief only. It does not define the MS-04 service specification, OpenAPI contract, implementation, or tests.

---

## 1. CAST Query Coverage

Live CAST Imaging was queried for:

- Cart creation, retrieval, add, update, multi-update, delete, and promotion flows.
- Authenticated and anonymous checkout.
- Authenticated and anonymous order-total calculation.
- Authenticated and anonymous shipping selection.
- Authenticated and anonymous payment initialization.
- Full transaction call graphs for all critical flows.
- Cart, order, customer, product, product-price, tax, shipping-quote, and transaction data graphs.
- Complexity-ranked objects within the critical transaction graphs.
- CAST source-file paths, translated to the local `initial-source/shopizer-3.2.7/` root.
- Duplicate facade reachability and endpoint relationships.

CAST reports the backend application as a Java/Spring/Hibernate/SQL application with 218 transactions in the primary transaction profile. The full checkout graph is highly coupled and includes product, customer, tax, shipping, payment, storage, messaging/email, and order persistence concerns.

---

## 2. Transaction Inventory

### 2.1 Cart and checkout transaction surface

| CAST ID | HTTP operation | Endpoint | Full graph size | MS-04 disposition |
|---:|---|---|---:|---|
| 244217 | GET | `/api/v1/auth/customer/cart/` | 1,129 | Read |
| 244216 | GET | `/api/v1/auth/customer/{id}/cart/` | 1,122 | Read |
| 244210 | POST | `/api/v1/cart/` | 1,093 | Deep read |
| 244214 | GET | `/api/v1/cart/{code}/` | 1,049 | Deep read |
| 244211 | PUT | `/api/v1/cart/{code}/` | 1,116 | Deep read |
| 244213 | POST | `/api/v1/cart/{code}/multi/` | 1,109 | Deep read |
| 244218 | DELETE | `/api/v1/cart/{code}/product/{id}/` | 1,072 | Deep read |
| 244212 | POST | `/api/v1/cart/{code}/promo/{code}/` | 1,052 | Read for boundary compatibility |
| 244215 | POST | `/api/v1/customers/{id}/cart/` | 24 | Context only |
| 244089 | POST | `/api/v1/auth/cart/{code}/checkout/` | 3,245 | Critical deep read |
| 244090 | POST | `/api/v1/cart/{code}/checkout/` | 3,262 | Critical deep read |
| 244094 | POST | `/api/v1/auth/cart/{code}/payment/init/` | 643 | Critical boundary read |
| 244093 | POST | `/api/v1/cart/{code}/payment/init/` | 616 | Critical boundary read |
| 244101 | GET | `/api/v1/auth/cart/{code}/shipping/` | 1,202 | Critical boundary read |
| 244102 | POST | `/api/v1/cart/{code}/shipping/` | 1,192 | Critical boundary read |
| 244105 | GET | `/api/v1/auth/cart/{code}/total/` | 662 | Critical deep read |
| 244106 | GET | `/api/v1/cart/{code}/total/` | 705 | Critical deep read |

### 2.2 Related order and payment transactions

These transactions are visible in the same CAST domain surface but are not MS-04-owned capabilities.

| CAST ID | Endpoint | Full graph size | Target owner |
|---:|---|---:|---|
| 244097 | `/api/v1/private/orders/payment/capturable/` | 580 | MS-06 / MS-05 boundary |
| 244098 | `/api/v1/private/orders/{id}/capture/` | 9 | MS-06 |
| 244099 | `/api/v1/private/orders/{id}/refund/` | 9 | MS-06 |
| 244100 | `/api/v1/private/orders/{id}/authorize/` | 9 | MS-06 |
| 244095 | `/api/v1/private/orders/{id}/payment/nextTransaction/` | 333 | MS-06 |
| 244096 | `/api/v1/private/orders/{id}/payment/transactions/` | 407 | MS-06 |

The capture, refund, and authorize endpoints have minimal CAST graphs and source implementations that return `null`. The capability remains required in the target architecture by the approved decision register; it must not be treated as an MS-04 implementation responsibility.

---

## 3. Full Call-Graph Results

All graphs below were requested with `isfullcallgraph=true`.

| Flow | CAST ID | Nodes | Links | Important table endpoints |
|---|---:|---:|---:|---|
| Cart add | 244210 | 1,093 | 2,430 | `shopping_cart`, product, customer, merchant/configuration, tax lookup |
| Cart update | 244211 | 1,116 | 2,502 | `shopping_cart`, `shopping_cart_item`, `shopping_cart_attr_item`, product, customer |
| Authenticated checkout | 244089 | 3,245 | 8,112 | cart, product, customer, tax, module configuration, order, payment, storage |
| Anonymous checkout | 244090 | 3,262 | 8,173 | cart, product, customer, tax, module configuration, order, payment, storage |
| Authenticated shipping | 244101 | 1,202 | 2,783 | cart, product, customer, tax/configuration, shipping origin |
| Anonymous shipping | 244102 | 1,192 | 2,778 | cart, product, customer, tax/configuration, shipping origin |
| Authenticated totals | 244105 | 662 | 1,382 | product, customer, tax, merchant/configuration |
| Anonymous totals | 244106 | 705 | 1,533 | product, customer, tax, merchant/configuration |
| Authenticated payment initialization | 244094 | 643 | 1,335 | cart, product, customer, module configuration, merchant configuration |
| Anonymous payment initialization | 244093 | 616 | 1,288 | cart, product, customer, module configuration, merchant configuration |

### 3.1 Critical call paths

#### Cart add/update

```text
ShoppingCartApi
  -> ShoppingCartFacadeImpl.addToCart / addItemsToShoppingCart
  -> createCartModel / createCartItem(s)
  -> product and attribute lookup
  -> PricingService / ProductPriceUtils
  -> ShoppingCartService.saveOrUpdate
  -> cart reload and hydration
  -> OrderService.caculateShoppingCart
  -> order-total, tax, and shipping-related processors
  -> readable cart response
```

#### Checkout

```text
OrderApi.checkout
  -> customer resolution or anonymous customer population
  -> ShoppingCartService.getByCode / getById
  -> OrderFacadeImpl.processOrder
  -> PersistableOrderApiPopulator
  -> OrderProductPopulator for every cart line
  -> shipping quote lookup
  -> OrderService.caculateOrderTotal
  -> tax calculation
  -> payment population and provider processing
  -> OrderService.processOrder
  -> order and order-total persistence
  -> transaction persistence
  -> legacy inventory decrement
  -> cart.orderId update
  -> asynchronous confirmation/download email
```

#### Shipping selection

```text
OrderShippingApi.shipping
  -> cart lookup
  -> customer/address or anonymous delivery construction
  -> OrderFacadeImpl.getShippingQuote
  -> ShoppingCartService.createShippingProduct
  -> ShippingServiceImpl.getShippingQuote
  -> packaging and shipping preprocessors
  -> shipping module/provider selection
  -> ShippingQuote persistence
  -> localized shipping-option response
```

#### Order totals

```text
OrderTotalApi
  -> cart lookup
  -> optional shipping-quote lookup
  -> OrderSummary construction
  -> OrderService.caculateOrderTotal
  -> ProductPriceUtils
  -> configured order-total variations
  -> shipping and handling
  -> TaxServiceImpl.calculateTax
  -> grand-total assembly
```

#### Payment initialization

```text
OrderPaymentApi.init
  -> cart lookup and authenticated ownership check
  -> PersistablePaymentPopulator
  -> PaymentServiceImpl.initTransaction
  -> merchant payment configuration/decryption
  -> configured payment provider lookup
  -> transaction response population
```

---

## 4. Complexity-Ranked CAST Objects

CAST complexity results are sorted in decreasing cyclomatic complexity within each full transaction graph.

### 4.1 Checkout-ranked objects

| Cyclomatic | CAST object | Likely local source | Disposition |
|---:|---|---|---|
| 64 | `merge` | Customer/product mapper dependency; resolve during source read | Context/dependency |
| 46 | `populate` | Order/customer/product population path | Deep read if source path resolves |
| 42 | `calculateTax` | `sm-core/.../TaxServiceImpl.java` | MS-08 dependency; read contract only |
| 25 | `sendTransaction` | Payment provider path | MS-06 dependency; skip deep extraction |
| 24 | `processOrder` | `sm-shop/.../OrderFacadeImpl.java` | Must deep-read |
| 23 | `populate` | Order-product/payment population path | Resolve and read if MS-04-owned |
| 22 | `validateCreditCardNumber` | `sm-core/.../PaymentServiceImpl.java` | MS-06 dependency; skip deep extraction |
| 22 | `caculateOrder` | `sm-core/.../OrderServiceImpl.java` | Must deep-read |
| 21 | `sendOrderEmail` | Email/integration path | Context only |
| 20 | `addProductImage` | Catalog/media dependency | Exclude from MS-04 extraction |
| 19 | `process` | Payment/order processing path | Resolve ownership; likely MS-06/MS-05 |
| 19 | `processPayment` | Payment provider path | MS-06 dependency |
| 17 | `saveOrUpdate` | Cart/order persistence path | Read when cart-owned |
| 15 | `getPopulatedItem` | Cart hydration | Must deep-read |
| 15 | `finalPrice` | `ProductPriceUtils.java` | MS-07 dependency; read calculation contract |
| 13 | `calculateFinalPrice` | `ProductPriceUtils.java` | MS-07 dependency; read calculation contract |

### 4.2 Cart graph-ranked objects

| Cyclomatic | CAST object | Local source | Disposition |
|---:|---|---|---|
| 24 | `processOrder` | `sm-shop/.../OrderFacadeImpl.java` | Must deep-read |
| 21 | `populate` | Order/cart population path | Resolve and deep-read if MS-04-owned |
| 14 | `shipping` | `sm-shop/.../OrderShippingApi.java` | Deep-read boundary behavior |
| 13 | `addItemsToShoppingCart` | `ShoppingCartFacadeImpl.java` | Must deep-read |
| 13 | `modifyCart` | `ShoppingCartFacadeImpl.java` | Must deep-read |
| 12 | `shipping` | `OrderShippingApi.java` | Deep-read boundary behavior |
| 12 | `getShoppingCartData` | `ShoppingCartFacadeImpl.java` | Read cart response behavior |
| 10 | `mergeCart` | `ShoppingCartServiceImpl.java` | Read merge semantics |
| 10 | `checkout` | `OrderApi.java` | Must deep-read |
| 9 | `init` | `OrderPaymentApi.java` | Boundary read |
| 8 | `readableShoppingCart` | `ShoppingCartFacadeImpl.java` | Read response mapping |
| 8 | `modifyCartMulti` | `ShoppingCartFacadeImpl.java` | Must deep-read |
| 7 | `calculateTotal` | `OrderTotalApi.java` | Must deep-read |
| 6 | `getShoppingCart` | `ShoppingCartServiceImpl.java` | Read ownership and lifecycle |
| 6 | `calculateOrderTotal` | `OrderFacadeImpl.java` | Must deep-read |

### 4.3 Shipping and totals complexity

| Flow | Highest-ranked objects |
|---|---|
| Shipping | `getShippingQuote` 69; `getShippingQuotes` 35 and 34; `getBoxPackagesDetails` 32; `getItemPackagesDetails` 18; `prePostProcessShippingQuotes` 17 |
| Totals | `calculateTax` 42; `caculateOrder` 22; `finalPrice` 15; `calculateFinalPrice` 13; `calculateTotal` 7 |
| Payment init | `getIntegrationModules` 17; `getPopulatedItem` 15; `finalPrice` 15; `calculateFinalPrice` 13; `init` 9; `initTransaction` 5 |

The highest-complexity objects are not all MS-04-owned. Tax, shipping, pricing, provider calls, email, catalog media, and customer population must not be copied into the Cart and Checkout boundary.

---

## 5. CAST Data-Graph Inventory

### 5.1 Cart and checkout data graphs

| CAST graph ID | Start entity | Graph size | Relevant tables |
|---:|---|---:|---|
| 243932 | `shopping_cart` | 111 | cart, product, customer, merchant, tax/configuration |
| 243920 | `shopping_cart_item` | 20 | `shopping_cart_item`, `shopping_cart_attr_item` |
| 243908 | `orders` | 62 | `orders`, order products, totals, history, downloads, transactions |
| 243909 | `order_product` | 10 | order-product snapshot tables |
| 243929 | `sm_transaction` | 22 | `sm_transaction` |
| 243922 | `product_price` | 31 | `product_price`, `product_price_description` |
| 243933 | `shipping_quote` | 4 | `shipping_quote` |

### 5.2 Customer and product data graphs used by MS-04 flows

| CAST graph ID | Start entity | Graph size | MS-04 relevance |
|---:|---|---:|---|
| 243914 | `customer` | 649 | Resolve authenticated customer and address context |
| 243912 | `product` | 527 | SKU/product/variant hydration and sellability |
| 243945 | `product_availability` | 65 | Availability and price lookup |
| 243922 | `product_price` | 31 | Price resolution dependency |
| 243919 | `tax_class` | 587 | Tax calculation dependency |
| 243926 | `tax_rate_description` | 1 | Tax-rate description lookup; no usable standalone `tax_rate` graph surfaced in this query |

CAST data-graph queries also returned endpoint-rooted graphs when a filter matched an endpoint or method name. The table-rooted graphs above are the relevant persistence evidence.

### 5.3 Checkout data-graph table set

The checkout graphs expose the following legacy tables:

```text
shopping_cart
shopping_cart_item
shopping_cart_attr_item
product
product_variation
product_variant_group
product_variant
product_type
product_option
product_option_value
product_attribute
product_image
product_digital
customer
customer_option
customer_option_value
customer_option_value / description tables
merchant_store
merchant_configuration
module_configuration
manufacturer
language
geozone
zone
country
currency
tax_class
tax_rate
content
```

The order/payment portions of the full checkout graph additionally expose:

```text
orders
order_product
order_product_attribute
order_product_price
order_product_download
order_status_history
order_total
sm_transaction
```

---

## 6. Source Files to Read

All paths below are resolved to the local source root.

### 6.1 MS-04 business-logic files — mandatory deep read

| Local path | CAST/P1 reason |
|---|---|
| `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/api/v1/shoppingCart/ShoppingCartApi.java` | Cart entry points and request semantics |
| `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/controller/shoppingCart/facade/ShoppingCartFacadeImpl.java` | Highest cart mutation logic; CAST objects `30430`, `30045`, `30046`, `30062`, `30063` |
| `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/shoppingcart/ShoppingCartServiceImpl.java` | Cart hydration, obsolete-item cleanup, merchant lookup, persistence |
| `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/shoppingcart/ShoppingCartCalculationServiceImpl.java` | Cart calculation delegation |
| `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/api/v1/order/OrderApi.java` | Authenticated and anonymous checkout entry points |
| `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/controller/order/facade/OrderFacadeImpl.java` | Checkout orchestration; CAST `processOrder` complexity 24 |
| `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/populator/order/PersistableOrderApiPopulator.java` | Submitted checkout-to-domain mapping |
| `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/populator/order/OrderProductPopulator.java` | Immutable order-line/product/attribute/price/download snapshot construction |
| `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/order/OrderServiceImpl.java` | Total calculation and legacy checkout persistence; CAST `caculateOrder` complexity 22 |
| `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/order/ordertotal/OrderTotalServiceImpl.java` | Configured total-processor fan-out |
| `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/api/v1/order/OrderTotalApi.java` | Total endpoint and optional shipping-quote input |
| `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/api/v1/order/OrderShippingApi.java` | Shipping-selection endpoint and cart/customer checks |
| `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/shipping/ShippingQuoteServiceImpl.java` | Quote retrieval/persistence boundary |
| `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/api/v1/order/OrderPaymentApi.java` | Payment-initiation boundary and authentication behavior |
| `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/populator/order/transaction/PersistablePaymentPopulator.java` | Payment input normalization |
| `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/order/OrderServiceImpl.java` | Repeated intentionally because it is both total calculator and checkout persistence/orchestration participant |

### 6.2 Supporting model files — read for ownership and snapshot semantics

| Local path | Purpose |
|---|---|
| `initial-source/shopizer-3.2.7/sm-core-model/src/main/java/com/salesmanager/core/model/shoppingcart/ShoppingCart.java` | Cart aggregate fields and lifecycle markers |
| `initial-source/shopizer-3.2.7/sm-core-model/src/main/java/com/salesmanager/core/model/shoppingcart/ShoppingCartItem.java` | Cart-line quantity, price, subtotal, product references |
| `initial-source/shopizer-3.2.7/sm-core-model/src/main/java/com/salesmanager/core/model/shoppingcart/ShoppingCartAttributeItem.java` | Selected attribute ownership and orphan cleanup |
| `initial-source/shopizer-3.2.7/sm-shop-model/src/main/java/com/salesmanager/shop/model/shoppingcart/ShoppingCartData.java` | API cart DTO and storefront payload shape |
| `initial-source/shopizer-3.2.7/sm-shop-model/src/main/java/com/salesmanager/shop/model/shoppingcart/ShoppingCartEntity.java` | API/persistence mapping context |
| `initial-source/shopizer-3.2.7/sm-core-model/src/main/java/com/salesmanager/core/model/order/OrderTotalSummary.java` | Legacy calculated-total result structure |
| `initial-source/shopizer-3.2.7/sm-core-model/src/main/java/com/salesmanager/core/model/order/OrderTotal.java` | Legacy total-line representation |
| `initial-source/shopizer-3.2.7/sm-core-model/src/main/java/com/salesmanager/core/model/shipping/ShippingSummary.java` | Shipping allocation consumed by totals |
| `initial-source/shopizer-3.2.7/sm-core-model/src/main/java/com/salesmanager/core/model/shipping/ShippingQuote.java` | Legacy persisted quote reference |
| `initial-source/shopizer-3.2.7/sm-core-model/src/main/java/com/salesmanager/core/model/payments/Payment.java` | Payment request model used during checkout |
| `initial-source/shopizer-3.2.7/sm-core-model/src/main/java/com/salesmanager/core/model/payments/Transaction.java` | Payment transaction reference and state |
| `initial-source/shopizer-3.2.7/sm-core-model/src/main/java/com/salesmanager/core/model/order/orderproduct/OrderProduct.java` | Legacy submitted line snapshot |
| `initial-source/shopizer-3.2.7/sm-core-model/src/main/java/com/salesmanager/core/model/order/orderproduct/OrderProductAttribute.java` | Snapshot attribute data |
| `initial-source/shopizer-3.2.7/sm-core-model/src/main/java/com/salesmanager/core/model/order/orderproduct/OrderProductPrice.java` | Snapshot price data |
| `initial-source/shopizer-3.2.7/sm-core-model/src/main/java/com/salesmanager/core/model/order/orderproduct/OrderProductDownload.java` | Digital product snapshot and download metadata |

### 6.3 Rule/configuration files — read to detect hidden calculation engines

| Local path | Reason |
|---|---|
| `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/configuration/ProcessorsConfiguration.java` | Determines active order-total processors |
| `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/modules/order/total/PromoCodeCalculatorModule.java` | Promotion calculation module invoked from totals |
| `initial-source/shopizer-3.2.7/sm-core/src/main/resources/com/salesmanager/drools/rules/PromoCoupon.drl` | Coupon rule behavior |
| `initial-source/shopizer-3.2.7/sm-core/src/main/resources/com/salesmanager/drools/rules/ShippingDecision.drl` | Shipping decision rule behavior |
| `initial-source/shopizer-3.2.7/sm-core/src/main/resources/com/salesmanager/drools/rules/PriceByDistance.drl` | Shipping-price rule dependency |
| `initial-source/shopizer-3.2.7/sm-core/src/main/resources/com/salesmanager/drools/rules/PriceByDistance2.drl` | Additional shipping-price rule dependency |

---

## 7. Source Files to Skip or Treat as Context Only

### 7.1 Cross-service calculation implementations

These are required for dependency understanding but are not MS-04-owned business logic.

| Local path | Treatment | Reason |
|---|---|---|
| `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/catalog/pricing/PricingServiceImpl.java` | Context only | MS-07 owns price/promotion calculation |
| `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/utils/ProductPriceUtils.java` | Read calculation contract; do not re-own | High-complexity pricing logic belongs to MS-07 |
| `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/catalog/product/ProductServiceImpl.java` | Context only | MS-02 owns product facts and availability |
| `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/catalog/product/attribute/ProductAttributeServiceImpl.java` | Context only | MS-02 owns product attributes |
| `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/tax/TaxServiceImpl.java` | Read request/response dependency only | MS-08 owns tax calculation |
| `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/shipping/ShippingServiceImpl.java` | Read boundary and quote semantics only | MS-09 owns shipping |
| `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/modules/integration/shipping/impl/DefaultPackagingImpl.java` | Context only | Packaging belongs to MS-09 |
| `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/modules/integration/shipping/impl/ShippingDecisionPreProcessorImpl.java` | Context only | Shipping rules belong to MS-09/MS-12 |
| `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/payments/PaymentServiceImpl.java` | Context only; do not extract MS-04 rules | MS-06 owns provider state and payment processing |
| `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/payments/TransactionServiceImpl.java` | Context only | MS-06 owns transaction lifecycle |
| `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/api/v1/order/OrderPaymentApi.java` | Read payment-initiation boundary; skip capture/refund extraction | Capture/refund/authorize belong to MS-06 |

### 7.2 Interfaces, DTO-only classes, and framework plumbing

Read only when required to resolve a signature or payload:

```text
initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/shoppingcart/ShoppingCartService.java
initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/order/OrderService.java
initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/order/ordertotal/OrderTotalService.java
initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/payments/PaymentService.java
initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/shipping/ShippingService.java
initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/tax/TaxService.java
initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/controller/order/facade/OrderFacade.java
initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/controller/shoppingCart/facade/ShoppingCartFacade.java
```

Skip generic getters, setters, DTO mappers, repository base classes, logging helpers, cache wrappers, locale formatters, and framework constructors as independent business-rule sources.

### 7.3 Frontend files

The frontend extraction already records the relevant behavior in `BR-UI-010`, `BR-UI-012`, and `BR-UI-014`. Do not perform a second frontend extraction for this brief.

Use these only for compatibility verification if required:

```text
initial-source/shopizer-shop-reactjs-main/src/redux/actions/cartActions.js
initial-source/shopizer-shop-reactjs-main/src/redux/reducers/cartReducer.js
initial-source/shopizer-shop-reactjs-main/src/components/product/ProductDescriptionInfo.js
initial-source/shopizer-shop-reactjs-main/src/pages/other/Checkout.js
```

---

## 8. Target Data Ownership

### 8.1 MS-04-owned legacy tables

The legacy tables that are directly associated with cart mutation and cart-line state are:

| Table | CAST evidence | Target ownership |
|---|---|---|
| `salesmanager.shopping_cart` | Data graph `243932`; cart transactions | MS-04 |
| `salesmanager.shopping_cart_item` | Data graph `243920`; cart update/delete graphs | MS-04 |
| `salesmanager.shopping_cart_attr_item` | Data graph `243920`; attribute cleanup during hydration | MS-04 |

No static DDL was found; table names are derived from Hibernate/CAST mappings.

### 8.2 MS-04 target-only persistence

The target architecture requires explicit persistence for concepts not represented as dedicated legacy tables:

| Logical target data | Ownership | Purpose |
|---|---|---|
| `checkout_session` | MS-04 | Explicit checkout lifecycle and expiry |
| `checkout_line_snapshot` | MS-04 | Frozen product, quantity, price, and attribute values |
| `checkout_total_snapshot` | MS-04 | Server-calculated subtotal, discounts, tax, shipping, handling, and grand total |
| `checkout_submission` | MS-04 | Durable submission record and state |
| `checkout_idempotency_key` | MS-04 | Request deduplication and replay-safe response |
| `cart_quote_reference` | MS-04 | Expiring references to pricing, tax, and shipping results |
| MS-04 outbox/inbox records | Platform pattern | Durable `OrderSubmitted` publication and consumer deduplication |

These are target modeling candidates, not legacy CAST tables.

### 8.3 Legacy tables touched by checkout but not owned by MS-04

The following tables appear in the CAST checkout graphs but must not be assigned to MS-04:

| Legacy table | Target owner | Boundary note |
|---|---|---|
| `orders` | MS-05 | MS-04 publishes an immutable order snapshot; MS-05 creates the order |
| `order_product` | MS-05 | Order-line snapshot |
| `order_product_attribute` | MS-05 | Order-line attribute snapshot |
| `order_product_price` | MS-05 | Order-line price snapshot |
| `order_product_download` | MS-05 | Entitlement state |
| `order_status_history` | MS-05 | Order lifecycle history |
| `order_total` | MS-05 | Persisted order totals |
| `sm_transaction` | MS-06 | Provider transaction state |
| `product` | MS-02 | Product facts |
| `product_availability` | MS-02 | Atomic availability reservation/decrement |
| `product_price` | MS-07 | Price source |
| `customer` | MS-01 | Customer identity and address context |
| `tax_class`, `tax_rate` | MS-08 | Tax calculation |
| `shipping_quote` | MS-09 | Shipping quote persistence |
| `merchant_store`, `merchant_configuration`, `module_configuration` | MS-10/MS-11 | Store and configuration ownership |

MS-04 must not use cross-service foreign keys or write any of these schemas in the target architecture.

---

## 9. Cross-Service Dependencies

| Dependency | Legacy CAST/source evidence | Target interaction |
|---|---|---|
| MS-01 Customer and Identity | Authenticated checkout resolves `Customer` by principal; cart ownership uses customer ID | REST customer/address snapshot lookup; tenant/customer context |
| MS-02 Catalog and Product | Cart hydration resolves SKU/product/attributes/availability; checkout re-resolves cart products | REST product snapshot and availability/reservation contract |
| MS-07 Pricing and Promotions | `PricingService`, `ProductPriceUtils`, order-total processors, promotion module | REST price/promotion calculation returning allocation and expiry |
| MS-08 Tax | `TaxServiceImpl.calculateTax` is a high-complexity checkout dependency | REST tax quote with jurisdiction, lines, totals, and expiry |
| MS-09 Shipping | `ShippingServiceImpl.getShippingQuote`, packaging, shipping modules | REST shipping options and selected-quote validation |
| MS-05 Order Management | Legacy checkout directly persists order tables and updates cart `orderId` | Publish durable `OrderSubmitted`; MS-05 creates order and owns lifecycle |
| MS-06 Payments | Legacy checkout invokes `PaymentService`; payment init invokes provider selection | Payment flow should be mediated through MS-05 `PaymentRequested` / MS-06 events; no direct legacy-style state mutation |
| MS-10 Merchant and Store Administration | Every graph carries `merchant_store` and store context | Tenant/store ID propagated in request and event metadata |
| MS-11 Content and Configuration | Merchant/module configuration participates in payment, tax, shipping, and processor selection | Configuration read APIs/events; no MS-04 configuration writes |
| MS-12 Platform Integrations | CAST checkout graph includes S3/GCP/email/provider-related nodes | Event-driven delivery/adapters; not part of cart domain ownership |

### Boundary constraints

- MS-04 owns cart mutation, quote orchestration, checkout freezing, and submission publication.
- MS-04 cannot transition an order.
- MS-04 cannot write product availability directly.
- MS-04 cannot write payment provider state.
- MS-07, MS-08, and MS-09 return calculation results; they do not write checkout/order totals.
- MS-05 consumes the submitted snapshot and owns the order aggregate.
- MS-06 owns provider state and publishes authenticated payment events.
- No distributed transaction is assumed across MS-04, MS-05, MS-06, MS-07, MS-08, or MS-09.

---

## 10. P1 Rules Requiring P4 Deep Extraction

### 10.1 MS-04-owned rules

| Rule | P1 statement | P4 extraction focus |
|---|---|---|
| `BR-ORD-001` | Cart creation generates a merchant-scoped client-visible code | Code generation, merchant scope, anonymous/authenticated identity, collision behavior |
| `BR-ORD-002` | Cart products must belong to the merchant and be sellable | SKU lookup, merchant predicate, availability, future dates, inventory configuration |
| `BR-ORD-003` | Duplicate non-attribute items increment quantity | Merge key, virtual-product exception, attribute distinction, quantity overflow |
| `BR-ORD-004` | Quantity zero removes an item and its attributes | Delete ordering, cascade/orphan behavior, multi-item update semantics |
| `BR-ORD-005` | Cart hydration recalculates prices and marks obsolete items | Product rehydration, orphan attributes, unavailable products, empty-cart lifecycle |
| `BR-ORD-006` | Virtual/non-shippable products do not produce shipping input | Mixed physical/virtual cart behavior and no-shipping quote behavior |
| `BR-ORD-007` | Cart promotions are short-lived and may be cleared during calculation | Calendar-date expiry, null promo date, mutation during read/calculation |
| `BR-ORD-008` | Totals combine items, variations, shipping, handling, tax, and grand total | Ordering, rounding, additional prices, tax/handling inclusion, precision |
| `BR-ORD-010` | Submitted checkout amount must equal server recalculation | Canonical amount representation, currency, rounding, stale quote behavior |
| `BR-ORD-011` | Checkout creates an order snapshot from current cart state | Snapshot boundary, re-resolution, selected quote, addresses, currency, locale, freeze point |
| `BR-UI-010` | Storefront cart identity is merchant-specific | Cookie/cart-code scope, tenant propagation, anonymous-to-authenticated merge |
| `BR-UI-012` | Add-to-cart creates or updates a backend cart | Empty versus existing cart, SKU/quantity/options payload compatibility |
| `BR-UI-014` | Checkout obtains shipping options and recalculates totals | Shipping-address input, selected quote, quote freshness, total refresh sequence |

### 10.2 Boundary carry-forward rules

These rules are not MS-04-owned but must be read or reconciled during MS-04 extraction:

| Rule | Owner | MS-04 relevance |
|---|---|---|
| `BR-ORD-009` | MS-07 | Active order-total processor behavior affects cart totals |
| `BR-ORD-012` | MS-02 | Inventory validation/decrement conflict is a checkout acceptance gate |
| `BR-ORD-013` | MS-05 | Initial order status is created outside MS-04 in the target |
| `BR-ORD-014..017` | MS-06/MS-05 | Legacy checkout/payment coupling must be replaced by events |
| `BR-ORD-018` | MS-05/MS-12 | Digital-product snapshot and delivery consequences |
| `BR-PRC-001..013` | MS-07 | Product and promotion pricing inputs |
| `BR-PRC-014..021` | MS-08 | Tax quote semantics |
| `BR-PRC-022..036` | MS-09 | Shipping quote and selection semantics |

---

## 11. Placement Candidates

Default placement remains the application/domain tier unless a target boundary requires otherwise.

| Concern | Candidate placement | Recommendation |
|---|---|---|
| Cart mutation and line invariants | MS-04 application/domain tier | Keep in MS-04 aggregate/application services |
| Cart ownership and tenant predicates | MS-04 repository plus gateway/request context | Enforce both application authorization and database uniqueness/index constraints |
| Cart total orchestration | MS-04 application tier | Orchestrate MS-07/MS-08/MS-09; do not duplicate their algorithms |
| Price/promotion calculation | MS-07 application tier | MS-04 consumes an allocation result |
| Tax calculation | MS-08 application tier | MS-04 consumes a tax quote |
| Shipping packaging/provider calls | MS-09 and MS-12 | Keep provider calls outside MS-04 |
| Checkout quote snapshot | MS-04 database/application transaction | Persist an expiring, versioned snapshot |
| Idempotency | MS-04 database plus application middleware | Unique key on tenant/customer/cart/operation scope; replay original response |
| Order submission event | MS-04 outbox | Publish after local checkout freeze/submission transaction |
| Order creation | MS-05 | Consume `OrderSubmitted`; no direct MS-04 order-table writes |
| Payment initiation | MS-05/MS-06 event workflow | Do not preserve direct provider invocation inside MS-04 |
| Inventory reservation | MS-02 | Atomic reservation/decrement owned by MS-02; MS-04 retains opaque reservation IDs |
| Order status transition | MS-05 | Never implement in MS-04 |
| Persistence joins/order fetch graphs | MS-05 read model/query tier | Do not reproduce legacy broad joins in MS-04 |
| Batch/scheduled job | None found | No CAST or source evidence of a required batch process |
| Stored procedure/database decision engine | None found | No stored procedure evidence; application tier remains default |

---

## 12. Dead-Code and Exclusion Register

### 12.1 Confirmed non-executable or unimplemented paths

| Path/object | Evidence | Treatment |
|---|---|---|
| `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/api/v1/order/OrderPaymentApi.java` capture endpoint | CAST transaction `244098`, size 9; method returns `null` | Exclude from MS-04 extraction; preserve as MS-06 target capability gap |
| Same `OrderPaymentApi.java` refund endpoint | CAST transaction `244099`, size 9; method returns `null` | Exclude from MS-04 extraction; MS-06 owns required target capability |
| Same `OrderPaymentApi.java` authorize endpoint | CAST transaction `244100`, size 9; method returns `null` | Exclude from MS-04 extraction; MS-06 owns required target capability |
| Commented-out checkout/payment code | Source comments only | Do not treat as executable behavior |
| `PaymentServiceImpl` TODO/null provider paths | Payment-provider concern and target MS-06 ownership | Do not extract as MS-04 rules |
| Inactive manufacturer/shipping-code order-total module | `ProcessorsConfiguration` indicates it is not active | Do not assign to MS-04; retain as MS-07 clarification |
| PayPal REST implementation paths returning `null` | P1 extraction identified provider registration/implementation gap | Exclude from MS-04; MS-06 provider decision |

### 12.2 Not classified as dead

The following must not be discarded solely because they are small, duplicated, or low-complexity:

- `store/facade/order/OrderFacadeImpl.java`
- `store/facade/shoppingCart/ShoppingCartFacadeImpl.java`
- Interface methods and versioned facade methods
- Legacy v0 DTO/populator paths
- Repository methods with low cyclomatic complexity
- Customer/product/tax/shipping lookup methods
- Getter/setter and entity methods participating in persistence mappings

CAST exposed both controller-facade and older facade classes, including Spring Bean relationships. The older facade paths are treated as compatibility/context paths until endpoint or caller analysis proves they are unreachable.

The queried CAST APIs did not provide a direct `unreachable` filter result for this brief. No component is therefore marked dead solely from zero visible callers.

---

## 13. Hidden-Engine Check

### 13.1 Cart totals

**Finding: hidden calculation engine present.**

Totals are not calculated in one controller. The behavior is distributed across:

```text
ShoppingCartFacadeImpl
  -> ShoppingCartServiceImpl
  -> ShoppingCartCalculationServiceImpl
  -> OrderServiceImpl.caculateShoppingCart
  -> OrderServiceImpl.caculateOrder
  -> OrderTotalServiceImpl
  -> configured order-total processors
  -> PromoCodeCalculatorModule / Drools
  -> ProductPriceUtils
  -> ShippingService
  -> TaxServiceImpl
```

Evidence:

- `caculateOrder` has CAST cyclomatic complexity 22.
- `calculateTax` has CAST cyclomatic complexity 42.
- `getShippingQuote` has CAST cyclomatic complexity 69.
- `PromoCodeCalculatorModule` and `PromoCoupon.drl` provide rule-driven discount behavior.
- `ProcessorsConfiguration` controls which total processors execute.
- Legacy scale calls include `BigDecimal.setScale(2, HALF_UP)` calls whose returned value is not always assigned.
- Total calculation has multiple paths with different promotion/date/null handling.

**Extraction consequence:** MS-04 must orchestrate versioned calculation responses and persist a canonical quote snapshot. It must not reimplement pricing, promotion, tax, or shipping algorithms inside the cart service.

### 13.2 Checkout state

**Finding: implicit legacy state exists, but no explicit checkout state machine was found.**

Observed state markers include:

- `ShoppingCart.orderId` used to associate a completed cart with an order.
- `ShoppingCart.obsolete` used for empty/unavailable/invalid cart cleanup.
- `OrderStatus.ORDERED` created during legacy order processing.
- Payment processing may directly promote an order to `PROCESSED`.
- Cart completion is represented by mutation/association rather than an explicit checkout-session lifecycle.

No dedicated legacy checkout-session aggregate, checkout-state table, legal transition matrix, or explicit freeze state was found in the CAST graphs or source search.

**Extraction consequence:** target MS-04 requires explicit states such as open, quoted, frozen, submitted, expired, and failed. The target must not infer checkout state from `cart.orderId`.

### 13.3 Idempotency

**Finding: no legacy idempotency mechanism found.**

Evidence:

- No `idempot` or equivalent idempotency-key implementation was found in the inspected Java source/configuration search.
- No idempotency data graph or dedicated idempotency table was returned.
- Checkout endpoints accept no visible idempotency key in the legacy request path.
- The legacy flow can perform provider calls, order persistence, inventory decrement, cart completion, and notification without a single enclosing transaction.

**Extraction consequence:** idempotency is a target capability, not a preserved legacy rule. MS-04 must require a caller-supplied idempotency key for submission and payment-sensitive operations, persist it with a unique constraint, and replay the original result for duplicate requests.

### 13.4 Orchestration

**Finding: legacy orchestration is hidden in `OrderFacadeImpl.processOrder` and `OrderServiceImpl.process`.**

The legacy path directly coordinates:

- Customer lookup/creation.
- Cart reload.
- Product and attribute re-resolution.
- Order-line snapshot creation.
- Shipping-quote lookup.
- Total recalculation and submitted-amount comparison.
- Payment-provider invocation.
- Order and order-total persistence.
- Transaction persistence.
- Inventory decrement.
- Cart completion by setting `orderId`.
- Confirmation and download email dispatch.

No enclosing transaction was found across the full checkout operation. Cart service methods have local `@Transactional` annotations, but they do not cover provider calls, order creation, inventory mutation, cart completion, and notification as one atomic unit.

**Extraction consequence:** target orchestration must be split into:

1. MS-04 local cart/checkout transaction.
2. Durable outbox publication of `OrderSubmitted`.
3. MS-05 order creation and lifecycle ownership.
4. MS-05 publication of `PaymentRequested`.
5. MS-06 payment provider state and authenticated payment events.
6. MS-02 inventory reservation/decrement.
7. Explicit compensation and retry behavior.

---

## 14. Key Risks to Carry into Phase 4a

1. Merchant scoping is inconsistent in some legacy cart access paths.
2. Authenticated cart endpoints perform customer checks in controllers, while public paths may rely only on cart code/store lookup.
3. Cart hydration mutates persistence during a read by deleting orphan attributes and marking carts obsolete.
4. Cart and checkout totals use multiple calculation paths with possible rounding differences.
5. Submitted amount comparison depends on string-to-decimal normalization and current recalculation.
6. Legacy checkout performs payment before order persistence and lacks a visible atomic boundary.
7. Legacy inventory decrement can log insufficient inventory instead of consistently rejecting checkout.
8. The legacy decrement path may resolve the wrong product identifier from an order-product object.
9. Cart completion is represented by `orderId`, not an explicit state transition.
10. Shipping quote validity, selection, and reuse rules require explicit expiry/version semantics.
11. Digital product download records are created during order processing, but a customer download route was not identified.
12. Payment-initiation, authorization, capture, and refund responsibilities cross the legacy MS-04/MS-06 boundary.
13. Customer-to-cart merge behavior requires explicit authenticated/anonymous semantics.
14. No legacy idempotency mechanism was found; retry behavior must be designed rather than inferred.
15. Provider, tax, shipping, promotion, and email logic must remain outside MS-04 ownership.

---

## 15. Scout Exit Checklist

- [x] Live CAST application `Shopizer-Backend` queried.
- [x] Cart transactions queried.
- [x] Checkout transactions queried.
- [x] Order-total transactions queried.
- [x] Shipping-selection transactions queried.
- [x] Payment-initiation transactions queried.
- [x] Full call graphs retrieved for critical authenticated and anonymous flows.
- [x] Cart/order/customer/product/price/tax/shipping data graphs queried.
- [x] Complexity-ranked objects retrieved.
- [x] CAST source paths resolved to `initial-source/shopizer-3.2.7/`.
- [x] MS-04-owned legacy tables identified.
- [x] Cross-service dependencies identified.
- [x] P1 rules requiring deep extraction identified.
- [x] Placement candidates identified.
- [x] Dead/unimplemented exclusions recorded.
- [x] Hidden-engine check completed for totals, checkout state, idempotency, and orchestration.
- [x] No MS-04 service specification, API contract, implementation, or test artifact produced.
