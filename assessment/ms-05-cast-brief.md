# MS-05 Order Management — CAST Scout Brief

**Phase:** 4 CAST Scout  
**CAST application:** `Shopizer-Backend`  
**Analysis mode:** Hybrid — live CAST structure plus targeted direct-source extraction by the downstream extractor  
**CAST delivery:** `Onboarding-202511171247`  
**Source root mapping:** `§{main_sources}§/` → `initial-source/shopizer-3.2.7/`  
**Target service:** MS-05 Order Management  
**Target schema:** `order_management`  
**Target port:** `8105`  
**Scout scope:** Order creation/submission, order retrieval, status/history, payment-status coordination, capture/refund boundaries, fulfillment/shipping-status discovery, cancellation discovery, data ownership, hidden-engine detection  
**Output constraint:** This brief is the only requested artifact. No MS-05 service specification or tests are included.

The Scout queried live CAST transactions, full transaction graphs, data graphs, object details, complexity-ranked objects, source-file paths, and caller/callee metadata. Per the CAST Scout role, source files were not directly read; the file classifications below are extraction instructions based on CAST and Phase 1 evidence.

---

## 1. CAST Transaction Inventory

### 1.1 Order creation and submission

| CAST ID | Operation | Reduced objects | Full objects | Role |
|---:|---|---:|---:|---|
| `244089` | `POST /api/v1/auth/cart/{cart}/checkout/` | 168 | 3,245 | Authenticated checkout submission |
| `244090` | `POST /api/v1/cart/{cart}/checkout/` | 167 | 3,262 | Anonymous checkout submission |
| `244093` | `POST /api/v1/cart/{cart}/payment/init/` | 40 | 616 | Anonymous payment initialization; primarily MS-06/MS-04 boundary |
| `244094` | `POST /api/v1/auth/cart/{cart}/payment/init/` | 47 | 643 | Authenticated payment initialization; primarily MS-06/MS-04 boundary |

The two checkout transactions are the critical order-creation paths. Their full graphs include cart hydration, product and variant resolution, order-product snapshot population, order-total calculation, tax, shipping, payment processing, transaction persistence, order/history persistence, inventory interaction, cart completion, email/notification, and storage integrations.

The graph sizes are far above a CRUD baseline and confirm that checkout is an orchestration hotspot rather than a simple order insert.

### 1.2 Order retrieval and administration

| CAST ID | Operation | Reduced objects | Full objects | Role |
|---:|---|---:|---:|---|
| `244005` | `GET /api/v1/auth/orders/` | 43 | 1,437 | Authenticated customer order list |
| `244088` | `GET /api/v1/auth/orders/{order}/` | 47 | 1,436 | Authenticated customer order detail |
| `244006` | `GET /api/v1/private/orders/` | 44 | 773 | Administrative order list |
| `244086` | `GET /api/v1/private/orders/customers/{customer}/` | 43 | 1,437 | Administrative customer-order list |
| `244087` | `GET /api/v1/private/orders/{order}/` | 69 | 1,585 | Administrative order detail |
| `244091` | `ANY /api/v1/private/orders/{order}/customer/` | 46 | 416 | Administrative order-customer update/read path |

The order-detail graph reaches the order aggregate, product snapshots, totals, history, downloads, customer data, transaction data, repository joins, and response populators. Broad fetch paths are therefore potential row-multiplication and latency risks.

### 1.3 Status and status history

| CAST ID | Operation | Reduced objects | Full objects | Role |
|---:|---|---:|---:|---|
| `244092` | `PUT /api/v1/private/orders/{order}/status/` | 41 | 353 | Administrative order-status update |
| `244103` | `GET /api/v1/private/orders/{order}/history/` | 40 | 333 | Status-history retrieval |
| `244104` | `POST /api/v1/private/orders/{order}/history/` | 40 | 336 | Status-history append |

The status-update transaction reaches `OrderFacadeImpl.updateOrderStatus`, `OrderServiceImpl.addOrderStatusHistory`, `OrderStatus`, `Order`, and the `orders` data graph. CAST identifies the update operation as a small endpoint wrapper, but the business significance is high because the legacy implementation accepts enum values without enforcing a legal transition matrix.

### 1.4 Payment-status and transaction administration

| CAST ID | Operation | Reduced objects | Full objects | Role |
|---:|---|---:|---:|---|
| `244095` | `GET /api/v1/private/orders/{order}/payment/nextTransaction/` | 45 | 333 | Determine next transaction action |
| `244096` | `GET /api/v1/private/orders/{order}/payment/transactions/` | 44 | 407 | List order payment transactions |
| `244097` | `GET /api/v1/private/orders/payment/capturable/` | 28 | 580 | Find capturable orders |
| `244100` | `POST /api/v1/private/orders/{order}/authorize/` | 1 | 9 | Authorization endpoint; stub-like surface |
| `244098` | `POST /api/v1/private/orders/{order}/capture/` | 1 | 9 | Capture endpoint; stub-like surface |
| `244099` | `POST /api/v1/private/orders/{order}/refund/` | 1 | 9 | Refund endpoint; stub-like surface |

The transaction graph for `244095` includes `sm_transaction`, `orders`, `nextTransaction`, `lastTransaction`, and transaction-type values including `CAPTURE`, `AUTHORIZECAPTURE`, and `REFUND`.

The capture/refund/authorize endpoint graphs contain only nine objects and nine links each. These endpoints must not be classified as dead code: Phase 1 confirms facade and payment-service implementations exist, and the approved target architecture explicitly requires capture and refund capability. Treat them as incomplete legacy surfaces requiring target replacement.

### 1.5 Fulfillment, shipping-status, and cancellation discovery

No live backend transaction matched:

- `cancel`
- `cancellation`
- `fulfill`
- `fulfillment`
- `shipment`
- `shipping-status`
- `deliver`
- `callback`
- `webhook`
- `event` as an order operation
- `publish` as an order operation

Shipping-related transactions found by CAST are cart quote/configuration operations, not fulfillment-status operations:

| CAST ID | Operation | Reduced objects | Full objects | Classification |
|---:|---|---:|---:|---|
| `244101` | `GET /api/v1/auth/cart/{cart}/shipping/` | 72 | 1,202 | MS-09 quote boundary; not fulfillment |
| `244102` | `POST /api/v1/cart/{cart}/shipping/` | 66 | 1,192 | MS-09 selection/quote boundary; not fulfillment |

The `CANCELED` and `DELIVERED` order-status enum values exist, but no dedicated cancellation or fulfillment orchestration transaction was found. These are target capability gaps or status-only behaviors, not evidence of an existing legacy fulfillment engine.

---

## 2. Full Call-Graph Evidence

Full call graphs were requested for the critical order and administration flows.

| Transaction | Full graph result | Key graph facts |
|---|---|---|
| `244089` authenticated checkout | 3,245 nodes / 8,112 links | `shopping_cart`, product/variant data, order tables, payment providers, tax, shipping, totals, email/storage, transaction persistence |
| `244090` anonymous checkout | 3,262 nodes / 8,173 links | Same orchestration as authenticated checkout with anonymous customer/address branches |
| `244087` administrative order detail | 1,585 nodes / 3,219 links | `orders`, order products/totals/history/downloads, repository joins, order populators |
| `244092` status update | 353 nodes / 702 links | `updateOrderStatus`, `addOrderStatusHistory`, `OrderStatus`, `orders` |
| `244095` next payment transaction | 333 nodes / 679 links | `sm_transaction`, `nextTransaction`, `lastTransaction`, capture/refund transaction types |
| `244096` payment transaction list | 407 nodes / 836 links | `sm_transaction`, transaction repository, transaction populators, order lookup |
| `244097` capturable orders | 580 nodes / 990 links | `sm_transaction`, `orders`, `order_total`, capturable-order filtering |
| `244100` authorize | 9 nodes / 9 links | Endpoint and DTO surface only; no substantive implementation path |
| `244098` capture | 9 nodes / 9 links | Endpoint and DTO surface only; no substantive implementation path |
| `244099` refund | 9 nodes / 9 links | Endpoint and DTO surface only; no substantive implementation path |

### Named critical nodes observed

The checkout graphs include:

- `OrderFacadeImpl.processOrder`
- `OrderFacadeImpl.processOrderModel`
- `OrderServiceImpl.caculateOrder`
- `PaymentServiceImpl.processPayment`
- `TaxServiceImpl.calculateTax`
- `ShippingDecisionPreProcessorImpl.process`
- `ShoppingCartServiceImpl.getPopulatedItem`
- `OrderProductPopulator`
- `OrderTotalService`
- `sendOrderEmail`
- payment provider classes and transaction methods
- order, order-product, order-total, history, download, cart, and product tables

The status graph includes:

- `OrderFacadeImpl.updateOrderStatus`
- `OrderServiceImpl.addOrderStatusHistory`
- `OrderStatus`
- `OrderStatusHistory`
- `Order`

The transaction graphs include:

- `OrderFacadeImpl.nextTransaction`
- `OrderServiceImpl.getCapturableOrders`
- `TransactionServiceImpl.getRefundableTransaction`
- `PaymentServiceImpl.processRefund`
- `TransactionServiceImpl.lastTransaction`
- `TransactionServiceImpl.listTransactions`

---

## 3. Complexity-Ranked Objects and Source Paths

CAST complexity values below are object-level cyclomatic complexity and code-line metrics.

| Object | CAST ID | Complexity | Code lines | Local source path | Classification |
|---|---:|---:|---:|---|---|
| `TaxServiceImpl.calculateTax` | `29441` | 42 | 148 | `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/tax/TaxServiceImpl.java` | Context-only; MS-08 |
| `OrderFacadeImpl.processOrderModel` | `30007` | 29 | 161 | `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/controller/order/facade/OrderFacadeImpl.java` | Mandatory order-boundary read |
| `OrderFacadeImpl.processOrder` | `30017` | 24 | 106 | same `OrderFacadeImpl.java` path | Mandatory order-boundary read |
| `OrderServiceImpl.caculateOrder` | `29389` | 22 | 131 | `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/order/OrderServiceImpl.java` | Mandatory total/snapshot read |
| `PaymentServiceImpl.processPayment` | `29399` | 19 | 72 | `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/payments/PaymentServiceImpl.java` | Context-only; MS-06 |
| `ShippingDecisionPreProcessorImpl.process` | `11911` | 17 | 83 | `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/modules/integration/shipping/impl/ShippingDecisionPreProcessorImpl.java` | Context-only; MS-09 |
| `TransactionServiceImpl.getRefundableTransaction` | `13122` | 17 | 48 | `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/payments/TransactionServiceImpl.java` | Boundary read; refund outcome affects MS-05 |
| `ShoppingCartServiceImpl.getPopulatedItem` | `29433` | 15 | 50 | `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/shoppingcart/ShoppingCartServiceImpl.java` | Context-only; MS-04 |
| `ProductPriceUtils.finalPrice` | `13559` | 15 | 45 | `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/utils/ProductPriceUtils.java` | Context-only; pricing boundary |
| `ModuleConfigurationServiceImpl.getIntegrationModules` | `13381` | 17 | 72 | `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/system/ModuleConfigurationServiceImpl.java` | Context-only; MS-11/MS-12 |
| `OrderServiceImpl.getCapturableOrders` | `29392` | 12 | 44 | `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/order/OrderServiceImpl.java` | Mandatory payment-boundary read |
| `OrderFacadeImpl.nextTransaction` | `30027` | 8 | 23 | `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/controller/order/facade/OrderFacadeImpl.java` | Mandatory transaction-state read |
| `PaymentServiceImpl.processRefund` | `29401` | 9 | 65 | `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/payments/PaymentServiceImpl.java` | Context-only implementation; MS-06 owns provider state |
| `OrderFacadeImpl.updateOrderStatus` | `30030` | 4 | 19 | `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/controller/order/facade/OrderFacadeImpl.java` | Mandatory state-machine read |
| `OrderServiceImpl.addOrderStatusHistory` | `12943` | 1 | 6 | `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/order/OrderServiceImpl.java` | Mandatory audit/history read |

The checkout complexity list also contains high-complexity methods from customer, catalog, image, payment-provider, and shipping code. Those are dependencies of the checkout transaction, not automatically MS-05-owned logic.

---

## 4. Source Files to Read

These files are business-logic or order-boundary candidates. The extractor must read them in full where practical and use multi-pass reading for large files.

### 4.1 API entry points and active order facade

- `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/api/v1/order/OrderApi.java`
- `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/api/v1/order/OrderPaymentApi.java`
- `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/api/v1/order/OrderStatusHistoryApi.java`
- `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/api/v1/order/OrderTotalApi.java`
- `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/api/v1/order/OrderShippingApi.java` — read only for checkout boundary; shipping ownership remains MS-09
- `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/controller/order/facade/OrderFacade.java`
- `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/controller/order/facade/OrderFacadeImpl.java`

### 4.2 Order lifecycle, repository, totals, and history

- `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/order/OrderService.java`
- `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/order/OrderServiceImpl.java`
- `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/repositories/order/OrderRepository.java`
- `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/repositories/order/OrderRepositoryCustom.java`
- `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/repositories/order/OrderRepositoryImpl.java`
- `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/repositories/order/OrderStatusHistoryRepository.java`
- `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/order/orderstatushistory/OrderStatusHistoryService.java`
- `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/order/orderstatushistory/OrderStatusHistoryServiceImpl.java`
- `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/order/ordertotal/OrderTotalService.java`
- `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/order/ordertotal/OrderTotalServiceImpl.java`

`OrderTotalServiceImpl` must be read for boundary determination because the checkout graph demonstrates configured postprocessor fan-out. Do not duplicate promotion, tax, or shipping rules already assigned to MS-07, MS-08, or MS-09.

### 4.3 Order-product snapshots and downloads

- `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/populator/order/OrderProductPopulator.java`
- `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/populator/order/PersistableOrderProductPopulator.java`
- `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/populator/order/PersistableOrderPopulator.java`
- `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/populator/order/PersistableOrderApiPopulator.java`
- `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/populator/order/ReadableOrderPopulator.java`
- `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/populator/order/ReadableOrderProductPopulator.java`
- `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/populator/order/ReadableOrderProductDownloadPopulator.java`
- `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/populator/order/ReadableOrderStatusHistoryPopulator.java` if present
- `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/order/orderproduct/OrderProductDownloadService.java`
- `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/order/orderproduct/OrderProductDownloadServiceImpl.java`
- `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/repositories/order/orderproduct/OrderProductDownloadRepository.java`

### 4.4 Invoice and order integration boundary

- `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/modules/order/InvoiceModule.java`
- `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/modules/order/ODSInvoiceModule.java`
- `initial-source/shopizer-3.2.7/sm-core/src/main/resources/spring/shopizer-core-modules.xml`

Read these to determine whether the target order aggregate stores an invoice reference, creates an invoice artifact, or emits a downstream invoice-delivery request. Do not assume a separate invoice table: no dedicated invoice table was identified in the CAST order data graph.

### 4.5 Persistence and domain models

Read the order entity/model classes needed to establish real legacy columns and relationships:

- `initial-source/shopizer-3.2.7/sm-core-model/src/main/java/com/salesmanager/core/model/order/Order.java`
- `initial-source/shopizer-3.2.7/sm-core-model/src/main/java/com/salesmanager/core/model/order/OrderTotal.java`
- `initial-source/shopizer-3.2.7/sm-core-model/src/main/java/com/salesmanager/core/model/order/OrderTotalType.java`
- `initial-source/shopizer-3.2.7/sm-core-model/src/main/java/com/salesmanager/core/model/order/OrderValueType.java`
- `initial-source/shopizer-3.2.7/sm-core-model/src/main/java/com/salesmanager/core/model/order/OrderSummary.java`
- `initial-source/shopizer-3.2.7/sm-core-model/src/main/java/com/salesmanager/core/model/order/orderproduct/OrderProduct.java`
- `initial-source/shopizer-3.2.7/sm-core-model/src/main/java/com/salesmanager/core/model/order/orderproduct/OrderProductAttribute.java`
- `initial-source/shopizer-3.2.7/sm-core-model/src/main/java/com/salesmanager/core/model/order/orderproduct/OrderProductPrice.java`
- `initial-source/shopizer-3.2.7/sm-core-model/src/main/java/com/salesmanager/core/model/order/orderproduct/OrderProductDownload.java`
- `initial-source/shopizer-3.2.7/sm-core-model/src/main/java/com/salesmanager/core/model/order/orderstatus/OrderStatus.java`
- `initial-source/shopizer-3.2.7/sm-core-model/src/main/java/com/salesmanager/core/model/order/orderstatus/OrderStatusHistory.java`
- `initial-source/shopizer-3.2.7/sm-core-model/src/main/java/com/salesmanager/core/model/order/attributes/OrderAttribute.java`
- `initial-source/shopizer-3.2.7/sm-core-model/src/main/java/com/salesmanager/core/model/order/orderaccount/OrderAccount.java`
- `initial-source/shopizer-3.2.7/sm-core-model/src/main/java/com/salesmanager/core/model/order/orderaccount/OrderAccountProduct.java`
- `initial-source/shopizer-3.2.7/sm-core-model/src/main/java/com/salesmanager/core/model/payments/Transaction.java`
- `initial-source/shopizer-3.2.7/sm-core-model/src/main/java/com/salesmanager/core/model/payments/TransactionType.java`

### 4.6 Administration UI evidence for the assigned UI rule

- `initial-source/shopizer-admin-main/src/app/pages/orders/order-details/order-details.component.ts`
- `initial-source/shopizer-admin-main/src/app/pages/orders/services/orders.service.ts`

These support `BR-UI-009`: order-detail administration exposes status history, customer/address updates, transaction details, capture, and refund. Read for UI/API coverage only; do not create a frontend specification in this task.

---

## 5. Source Files to Skip or Treat as Context Only

### 5.1 Other service-owned business logic

Do not extract MS-05 rules from these areas:

- `sm-core/.../business/services/payments/PaymentServiceImpl.java` — MS-06 provider/payment state
- `sm-core/.../business/services/payments/TransactionServiceImpl.java` — MS-06 transaction ownership; read only for MS-05 payment-event boundary
- `sm-core/.../business/modules/integration/payment/impl/*.java` — MS-06 provider adapters
- `sm-core/.../business/services/tax/TaxServiceImpl.java` — MS-08
- `sm-core/.../business/modules/integration/shipping/impl/*.java` — MS-09
- `sm-core/.../business/services/shoppingcart/*.java` — MS-04
- `sm-core/.../business/utils/ProductPriceUtils.java` — MS-07/catalog pricing context
- `sm-core/.../business/modules/order/total/PromoCodeCalculatorModule.java` — MS-07
- product, category, variant, inventory, and availability services — MS-02
- merchant/store/module configuration services — MS-10/MS-11
- adapter delivery, email, file-storage, and carrier clients — MS-12

### 5.2 Mapping and framework-only code

Skip for business-rule extraction unless a referenced mapper contains snapshot semantics:

- generated getters/setters, constructors, enum accessors, and DTO-only classes
- generic Spring/JPA repository implementations without domain conditionals
- generic response and exception mappers
- framework `ApplicationEvent` classes unrelated to order behavior
- image conversion, catalog-image, and unrelated product mapper code
- broad shared utility classes with high fan-in and no order-specific decision logic

### 5.3 Duplicate facade path

CAST source discovery returned:

`initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/facade/order/OrderFacadeImpl.java`

The active order transactions resolve to:

`initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/controller/order/facade/OrderFacadeImpl.java`

Treat the `store/facade/order/OrderFacadeImpl.java` path as a duplicate/stale candidate. Confirm reachability before reading it; do not extract duplicate rules from both implementations.

### 5.4 Payment-provider implementation details

The following files were identified by CAST but belong to MS-06 and should not be deep-read for MS-05 rules:

- `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/modules/integration/payment/impl/BeanStreamPayment.java`
- `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/modules/integration/payment/impl/BraintreePayment.java`
- `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/modules/integration/payment/impl/MoneyOrderPayment.java`
- `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/modules/integration/payment/impl/StripePayment.java`
- `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/modules/integration/payment/impl/Stripe3Payment.java`
- `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/modules/integration/payment/impl/PayPalExpressCheckoutPayment.java`
- `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/modules/integration/payment/impl/PayPalRestPayment.java`

`PayPalRestPayment.java` is a dead-code exclusion candidate based on the Phase 1 finding that it is not registered and its methods return `null`. Keep this as an exclusion candidate rather than silently deleting it from the broader MS-06 assessment.

---

## 6. CAST Data-Graph Evidence

### 6.1 Consolidated order graph

**CAST data graph:** `243908`

The graph contains:

| CAST table | Legacy logical table |
|---|---|
| `orders` | `ORDERS` |
| `order_product` | `ORDER_PRODUCT` |
| `order_product_attribute` | `ORDER_PRODUCT_ATTRIBUTE` |
| `order_product_price` | `ORDER_PRODUCT_PRICE` |
| `order_product_download` | `ORDER_PRODUCT_DOWNLOAD` |
| `order_status_history` | `ORDER_STATUS_HISTORY` |
| `order_total` | `ORDER_TOTAL` |
| `order_attribute` | `ORDER_ATTRIBUTE` |
| `order_account` | `ORDER_ACCOUNT` |
| `order_account_product` | `ORDER_ACCOUNT_PRODUCT` |
| `sm_transaction` | `SM_TRANSACTION` |

Graph size: 62 nodes and 245 links.

The graph includes six JPA `Select` operations and the order retrieval, history, status, payment-transaction, and capturable-order transactions. The graph is useful for reachability and ownership, but the observed JPA operations are SELECT-oriented; write behavior must be verified from the recommended source files.

### 6.2 Order-product graph

**CAST data graph:** `243909`

Graph size: 10 nodes and 18 links.

Tables:

- `order_product`
- `order_product_attribute`
- `order_product_price`
- `order_product_download`
- `order_account_product`

Primary methods:

- `findByOrderId`
- `findOne`
- `getByOrderId`

This graph confirms that order-product snapshots and download/account-product records are tightly coupled to the order aggregate.

### 6.3 Transaction graph

**CAST data graph:** `243929`

Graph size: 22 nodes and 82 links.

Table:

- `sm_transaction`

Primary methods:

- `listTransactions`
- `nextTransaction`
- `lastTransaction`
- `getCapturableTransaction`
- `getRefundableTransaction`
- `processCapturePayment`
- `processRefund`
- `findByOrder`
- `findByDates`

The target must not preserve direct MS-05 writes to `sm_transaction`. Payment transaction state belongs to MS-06; MS-05 consumes authenticated payment outcomes.

### 6.4 Status graph interpretation

No standalone `order_status_history` data graph was returned. Status and history are represented inside consolidated graph `243908`, with the status endpoint and history endpoints connected to `orders` and `order_status_history`.

This is consistent with the Phase 1 finding that order status and status history are part of the order lifecycle aggregate rather than an independent service.

### 6.5 Cart graph boundary

**CAST data graph:** `243932`

Graph size: 111 nodes and 638 links.

Tables:

- `shopping_cart`
- `shopping_cart_item`
- `shopping_cart_attr_item`

The graph contains both checkout transactions and cart/shipping/total operations. It confirms that legacy checkout crosses the cart boundary. In the target, MS-04 owns cart and checkout submission; MS-05 receives an immutable submitted-order snapshot and does not mutate cart tables.

---

## 7. Target Table Ownership

### 7.1 MS-05-owned order data

The target MS-05 schema should own the order lifecycle and immutable order snapshot data represented by:

| Legacy table | Target ownership | Notes |
|---|---|---|
| `orders` | MS-05 | Aggregate root, identity, customer/store snapshot, status, totals, addresses, payment/shipping references |
| `order_product` | MS-05 | Immutable purchased-product snapshot |
| `order_product_attribute` | MS-05 | Purchased variant/attribute snapshot |
| `order_product_price` | MS-05 | Price snapshot and line-price facts |
| `order_attribute` | MS-05 | Order-level attributes |
| `order_total` | MS-05 | Persisted immutable total components and refund balance representation |
| `order_status_history` | MS-05 | Append-only lifecycle history |
| `order_product_download` | MS-05 | Download entitlement metadata and access state |
| `order_account` | MS-05, conditional | Read and classify before migration; likely recurring/account-order context |
| `order_account_product` | MS-05, conditional | Read and classify before migration; may require a later subscription/payment ownership decision |

### 7.2 Not owned by MS-05

| Legacy table/area | Target owner | MS-05 interaction |
|---|---|---|
| `sm_transaction` | MS-06 | Consume payment authorization/capture/refund outcomes; no direct writes |
| `shopping_cart` | MS-04 | Receive submitted immutable snapshot; no cart mutation |
| `shopping_cart_item` | MS-04 | No direct access in target |
| `shopping_cart_attr_item` | MS-04 | No direct access in target |
| product and availability tables | MS-02 | Consume product and reservation identifiers; no catalog writes |
| tax tables/quotes | MS-08 | Consume tax result snapshot |
| shipping quote/configuration tables | MS-09 | Consume shipping selection/rate snapshot |
| merchant/module configuration | MS-10/MS-11 | Consume validated store/tenant context |
| delivery attempts, email, external files | MS-12 | Consume entitlement, invoice, and lifecycle events |

No separate invoice table was observed in the CAST order graph. Invoice generation/reference handling must be resolved from `InvoiceModule`, `ODSInvoiceModule`, and the target event boundary.

---

## 8. Cross-Service Dependencies

### 8.1 Legacy dependencies observed in checkout

The full checkout graphs directly reach:

- MS-04-like cart and checkout components
- MS-01-like customer and address components
- MS-02-like product, option, variant, and availability components
- MS-07-like pricing and promotion processors
- MS-08-like tax calculation
- MS-09-like shipping calculation
- MS-06-like payment provider and transaction components
- email, file-storage, and external integration components

These legacy calls must not be copied as a distributed transaction into MS-05.

### 8.2 Target inbound dependencies

| Source | Boundary | Purpose |
|---|---|---|
| MS-04 | `OrderSubmitted` event | Submit immutable cart/customer/product/price/tax/shipping/payment-intent snapshot |
| MS-06 | Authenticated payment events | Advance or compensate order lifecycle based on provider-owned payment state |
| MS-02 | Reservation/decrement outcome, where required | Release or compensate availability on cancellation/refund failure paths |
| MS-01 | Validated customer identity context | Customer identity and address references/snapshots; no identity writes |
| MS-10 | Store/tenant context | Tenant isolation and store ownership |

### 8.3 Target outbound dependencies

| Consumer | Candidate boundary | Purpose |
|---|---|---|
| MS-12 | Order lifecycle event | Invoice, email, file, and external-delivery integration |
| MS-12 | Download entitlement event | Deliver digital-product access or notification |
| MS-09/MS-12 | Fulfillment request/status boundary | Request fulfillment and receive shipment progress; no legacy implementation found |
| MS-06 | Payment command/event boundary | Payment capture/refund outcome remains provider-owned |
| MS-02 | Inventory release/reservation boundary | Compensate reservation on cancellation or failed order processing |

MS-05 must own the order transition decision. MS-06 must not directly update order status.

---

## 9. Existing P1 Rules Requiring P4 Deep Extraction

Phase 3 assigns three existing rules to MS-05:

| Rule | P1 meaning | P4 treatment |
|---|---|---|
| `BR-ORD-013` | New orders receive `ORDERED`; some payment modes promote to `PROCESSED` | Re-extract from checkout, order persistence, history, and payment-outcome coupling. Resolve legal transitions and payment gating. |
| `BR-ORD-018` | Digital products create download records and trigger notification | Re-extract entitlement creation, availability, expiry/count metadata, access authorization, and MS-12 delivery boundary. |
| `BR-UI-009` | Administration order details support lifecycle and payment operations | Reconcile UI actions with live backend status/history/customer/transaction/capture/refund surfaces. |

### Boundary rules requiring targeted read but not duplicate ownership

| Rule | Assigned target | MS-05 extraction treatment |
|---|---|---|
| `BR-ORD-010` | MS-04 | Read for immutable submitted-total boundary; do not duplicate checkout validation |
| `BR-ORD-011` | MS-04/MS-05 | Read for order snapshot schema and ownership handoff |
| `BR-ORD-012` | MS-02 | Read for reservation/decrement compensation; MS-05 must not write availability |
| `BR-ORD-014` | MS-06 | Read for payment-provider outcome contract |
| `BR-ORD-015` | MS-06/MS-05 | Read for legacy non-atomic persistence and saga replacement |
| `BR-ORD-016` | MS-06/MS-05 | Read for capture outcome and order-state transition |
| `BR-ORD-017` | MS-06/MS-05 | Read for refund outcome, cumulative balance, and order-state transition |
| `BR-ORD-019` | MS-06 | Read only for payment validation boundary |

P4 must preserve ownership assignments from the gap analysis and must not turn payment-provider behavior, tax, shipping, pricing, or inventory behavior into MS-05 rules.

---

## 10. State Machine and Event Boundaries

### 10.1 Legacy order states

Phase 1 identifies these order statuses:

- `ORDERED`
- `PROCESSED`
- `DELIVERED`
- `REFUNDED`
- `CANCELED`

Observed legacy progression:

1. New order receives `ORDERED` and an initial status-history record.
2. `AUTHORIZECAPTURE` may promote the order to `PROCESSED`, except for Money Order behavior.
3. Capture can append history and set `PROCESSED`.
4. Administrative status update accepts enum values without a legal transition matrix.
5. Refund processing marks the order refunded and mutates total information in legacy code.
6. `DELIVERED` and `CANCELED` exist as values, but no dedicated fulfillment or cancellation orchestration was found.

### 10.2 Legacy transaction states

The transaction model includes:

- `INIT`
- `AUTHORIZE`
- `AUTHORIZECAPTURE`
- `CAPTURE`
- `REFUND`
- `OK`

Phase 1 reports that `lastTransaction` ordering is by transaction type rather than timestamp. This must be treated as a migration risk, not a target behavior.

### 10.3 Required target boundary decisions

MS-05 must define, during extraction and BA review:

- legal order-state transitions and terminal states
- whether `PROCESSED` requires authorization, capture, or either
- how payment failure, timeout, reversal, and unknown states affect order state
- whether cancellation is allowed before authorization, after authorization, or after capture
- how cancellation releases inventory reservations
- how refund completion affects order status
- cumulative partial-refund balance
- how fulfillment status relates to `PROCESSED`, `DELIVERED`, and `CANCELED`
- append-only history and actor/source attribution
- idempotent processing of duplicate `OrderSubmitted` and payment events

### 10.4 Candidate target event boundaries

These are target boundary candidates, not observed legacy events:

**Consumed by MS-05**

- `OrderSubmitted` from MS-04
- `PaymentAuthorized` from MS-06
- `PaymentCaptured` from MS-06
- `PaymentFailed` from MS-06
- `PaymentRefunded` from MS-06
- `PaymentVoided` or equivalent compensation event from MS-06
- fulfillment/shipment updates from MS-09 or MS-12

**Published by MS-05**

- order accepted/created
- order status changed
- fulfillment requested
- order canceled
- refund outcome applied to order
- download entitlement granted
- invoice generation/delivery requested

All target events require an outbox/inbox and idempotency strategy. No equivalent order-event publication was discovered in the live legacy application.

---

## 11. Placement Candidates

The default placement is application tier. Each candidate requires P4b evidence for data volume, set-vs-row behavior, call frequency, app-tier risk, and final placement.

| Candidate | Evidence | Default |
|---|---|---|
| Order retrieval and broad fetch joins | Administrative detail graph has 1,585 full objects and reaches order products, totals, history, downloads, attributes, and prices | App-tier query service with explicit projections; avoid loading the complete aggregate for lists |
| Order total snapshot assembly | `OrderServiceImpl.caculateOrder`, CAST complexity 22, 131 lines; checkout graph includes pricing, tax, shipping, and processor fan-out | App tier; calculation remains upstream, MS-05 stores validated snapshot |
| Status transition and history append | `updateOrderStatus`, `addOrderStatusHistory`, and `OrderStatus` graph | App tier with local transaction and append-only history |
| Capturable-order discovery | `getCapturableOrders`, complexity 12; `sm_transaction`, `orders`, and `order_total` graph | App tier/read model; payment state remains MS-06 |
| Refund-balance reconciliation | `processRefund`, `getRefundableTransaction`, `order_total`, and `sm_transaction` coupling | MS-06 payment operation plus MS-05 idempotent order projection |
| Download-entitlement creation | `OrderProductPopulator` and `OrderProductDownloadServiceImpl` | App tier with local order transaction and event publication |
| Fulfillment orchestration | No legacy fulfillment engine or transaction found; target capability is architectural | App tier saga/orchestration; carrier execution belongs MS-12 |
| Event publication | Legacy order flows publish no order events; target requires outbox | App tier transactional outbox |

No stored procedure, database trigger, scheduled batch, or set-based order-processing engine was identified in the Phase 1 inventory. No batch job was found for this segment.

---

## 12. Hidden-Engine Check

### 12.1 CRUD baseline

The consolidated order graph contains 11 order-related tables. A rough CRUD baseline of 5–8 components per table would be approximately 55–88 components. The checkout call graphs contain 3,245–3,262 full objects, and the administrative order-detail graph contains 1,585 full objects.

These totals include shared dependencies and framework objects, so they are not a direct order-component count. Nevertheless, the magnitude and the named high-complexity objects clearly indicate that MS-05 is not CRUD-only.

### 12.2 Order totals and progression

**Hidden engine suspected: yes.**

Evidence:

- `OrderServiceImpl.caculateOrder`: complexity 22
- `TaxServiceImpl.calculateTax`: complexity 42, outside MS-05 but on the critical checkout path
- `ProductPriceUtils.finalPrice`: complexity 15, outside MS-05
- configured order-total processor fan-out
- tax, shipping, variation, promotion, and grand-total interactions
- amount validation before order submission
- incomplete `BigDecimal.setScale` assignments reported in Phase 1

Action:

- Deep-read order snapshot and persisted-total behavior.
- Keep price, promotion, tax, and shipping calculation ownership in MS-07/MS-08/MS-09.
- Treat MS-05 totals as an immutable accepted snapshot plus refund balance, not as a duplicate recalculation engine.

### 12.3 Status progression

**Hidden lifecycle engine suspected: yes.**

Evidence:

- `ORDERED`, `PROCESSED`, `DELIVERED`, `REFUNDED`, and `CANCELED` enum values
- status-history append and retrieval transactions
- `updateOrderStatus` accepts enum values without a visible transition matrix
- payment operations can change order status
- status history and payment state are joined in the order data graph

Action:

- Extract a complete state-transition matrix.
- Classify legacy unrestricted status updates as a defect/obsolete behavior where appropriate.
- Require guards, actor/source, idempotency, terminal-state handling, and audit history in the target.

### 12.4 Fulfillment and shipping-status orchestration

**Legacy hidden fulfillment engine not found.**

Evidence:

- no `fulfill`, `fulfillment`, `shipment`, or shipment-status transaction
- shipping transactions are cart quote/selection paths only
- `DELIVERED` exists only as an order status value in the observed model/UI evidence
- target architecture explicitly introduces `FulfillmentOrder`, but no legacy aggregate or fulfillment workflow was found

Action:

- Do not claim that legacy fulfillment behavior was extracted.
- Read order status/history and shipping-summary paths for the boundary only.
- Treat fulfillment request/status handling as a target capability requiring MS-09/MS-12 coordination and BA confirmation.

### 12.5 Cancellation and refund orchestration

**Refund engine exists; cancellation orchestration does not.**

Refund evidence:

- `PaymentServiceImpl.processRefund`: complexity 9
- `TransactionServiceImpl.getRefundableTransaction`: complexity 17
- `sm_transaction`, `orders`, and `order_total` are connected
- refund endpoint exists as a nine-node stub surface
- Phase 1 reports refund-total mutation, provider validation concerns, partial-refund accumulation uncertainty, and provider transaction-type inconsistencies

Cancellation evidence:

- `CANCELED` enum value exists
- no cancel-specific transaction or endpoint was found
- no inventory-release or payment-void orchestration was found under cancellation

Action:

- Keep capture/refund capability in scope.
- MS-06 owns provider refund/capture state.
- MS-05 owns order-state application, refund balance, cancellation guards, and compensating events.
- Require explicit handling for partial refunds, duplicate refund events, refund-after-cancellation, and inventory release.

### 12.6 Event publication

**Legacy order event engine not found.**

Evidence:

- CAST found Spring `ApplicationEventPublisher`, `AsynchronousEventsConfiguration`, `PublishProductAspect`, and product event listeners.
- Product event source path:
  `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/configuration/events/products/PublishProductAspect.java`
- No order-specific event publisher/listener, order callback, webhook, or order event transaction was found.
- Checkout reaches direct email/notification logic, including `sendOrderEmail`, rather than an order-event boundary.

Action:

- Do not infer that legacy order events exist.
- Treat `OrderSubmitted`, lifecycle events, entitlement events, invoice requests, and fulfillment requests as new target boundaries.
- Use transactional outbox/inbox, replay-safe consumers, event versioning, and dead-letter handling in the target architecture.

---

## 13. Dead-Code and Exclusion Register

| Candidate | Evidence | Treatment |
|---|---|---|
| `PayPalRestPayment.java` | CAST component exists but Phase 1 reports it is not registered and methods return `null` | Exclude from MS-05; retain as MS-06 inactive-provider evidence pending final reachability check |
| Duplicate `store/facade/order/OrderFacadeImpl.java` | CAST active order transactions resolve to `store/controller/order/facade/OrderFacadeImpl.java` | Do not read unless reachability confirms active callers |
| Capture/authorize/refund endpoint wrappers | Full graph contains only nine objects, but target capability is approved and facade/payment implementations exist | Not dead; classify as incomplete/stubbed legacy surface |
| Product event publisher/listeners | CAST-reachable product event infrastructure, no order behavior | Exclude from MS-05; use only as evidence that order events are absent |
| Generic getters, constructors, DTOs, and framework classes | High graph presence but no independent business decision | Context only; no BR extraction |
| `FilesController` administrator download handling | Existing administrative file route; no customer entitlement route found | Read only for MS-05/MS-12 entitlement boundary; do not treat as complete customer download behavior |
| Scheduled/batch order jobs | Phase 1 found no scheduled or Quartz jobs in the segment | No batch extraction required |

No component should be excluded solely because it has low cyclomatic complexity when it participates in order persistence, status history, entitlement creation, or payment-state application.

---

## 14. Extractor Handoff Priorities

1. Read the active `OrderFacadeImpl`, `OrderServiceImpl`, order APIs, status-history services, order-product populators, download service, repository, and order models.
2. Re-extract `BR-ORD-013`, `BR-ORD-018`, and `BR-UI-009`.
3. Use `BR-ORD-010` through `BR-ORD-017` as explicit cross-service boundary evidence without duplicating MS-04/MS-06/MS-02 ownership.
4. Establish the real legacy order-table columns from entity annotations; do not infer DDL from endpoint DTOs.
5. Resolve whether `order_account` and `order_account_product` belong in MS-05 or require a subscription/payment split.
6. Define the legal order-state and payment-gated transition model from source evidence and BA decisions.
7. Treat cancellation and fulfillment as missing legacy implementations requiring target capability decisions.
8. Treat event publication as a new target mechanism, not a preserved legacy behavior.
9. Preserve all CAST references in downstream rule evidence:
   - checkout transactions `244089` and `244090`
   - order detail transaction `244087`
   - status transaction `244092`
   - transaction flows `244095`–`244097`
   - order data graph `243908`
   - order-product data graph `243909`
   - transaction data graph `243929`
10. Do not produce MS-05 API contracts, domain specifications, test artifacts, or implementation code in the Scout phase.
