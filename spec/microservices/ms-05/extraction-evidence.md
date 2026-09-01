# MS-05 Order Management — Extraction Evidence

**Analysis mode:** Hybrid  
**CAST application:** `Shopizer-Backend`  
**CAST delivery:** `Onboarding-202511171247`  
**Source root:** `initial-source/shopizer-3.2.7/`  
**Target service:** MS-05 Order Management

## Source Files Processed

| # | File | Lines read | Sections/read purpose | Rules extracted | Vectors |
|---:|---|---:|---|---:|---|
| 1 | `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/api/v1/order/OrderApi.java` | 1-520 | All declarations, list/detail, authenticated and anonymous checkout, customer update, status update | 4 | ✅ |
| 2 | `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/api/v1/order/OrderPaymentApi.java` | 1-362 | Payment initialization boundary, next action, transactions, capturable list, stub commands | 4 | ✅ |
| 3 | `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/api/v1/order/OrderStatusHistoryApi.java` | 1-76 | History list and append routes | 2 | ✅ |
| 4 | `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/api/v1/order/OrderTotalApi.java` | 1-224 | Cart-total boundary and customer/store authorization | 0 — MS-04/MS-07/MS-08 boundary | ✅ |
| 5 | `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/api/v1/order/OrderShippingApi.java` | 1-290 | Shipping quote boundary only | 0 — MS-09 boundary | ✅ |
| 6 | `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/controller/order/facade/OrderFacade.java` | 1-205 | Active facade contract and operation inventory | 0 — interface evidence | ✅ |
| 7 | `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/controller/order/facade/OrderFacadeImpl.java` | 1-1648, multi-pass | Initialization, totals, process order, validation, retrieval, payment action, history, status | 8 | ✅ |
| 8 | `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/order/OrderService.java` | 1-130 | Service contract, order processing, totals, capturable orders | 0 — interface evidence | ✅ |
| 9 | `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/order/OrderServiceImpl.java` | 1-680, multi-pass | Payment/order persistence, inventory interaction, total construction, history, capturable discovery | 6 | ✅ |
| 10 | `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/repositories/order/OrderRepository.java` | 1-22 | Detail fetch joins and store filtering | 1 | ✅ |
| 11 | `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/repositories/order/OrderRepositoryCustom.java` | 1-14 | Repository list contract | 0 | ✅ |
| 12 | `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/repositories/order/OrderRepositoryImpl.java` | 1-268 | Store/customer/status/email/phone filters, ordering, pagination, fetch joins | 1 | ✅ |
| 13 | `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/repositories/order/OrderStatusHistoryRepository.java` | 1-15 | History query and newest-first ordering | 1 | ✅ |
| 14 | `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/order/orderstatushistory/OrderStatusHistoryService.java` | 1-10 | History service contract | 0 | ✅ |
| 15 | `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/order/orderstatushistory/OrderStatusHistoryServiceImpl.java` | 1-20 | History retrieval delegation | 0 | ✅ |
| 16 | `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/order/ordertotal/OrderTotalService.java` | 1-19 | Order-total postprocessor boundary | 1 | ✅ |
| 17 | `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/order/ordertotal/OrderTotalServiceImpl.java` | 1-74 | Configured postprocessor fan-out; product-specific variations | 1 | ✅ |
| 18 | `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/populator/order/OrderProductPopulator.java` | 1-191 | Product/store validation, line snapshot, attributes, prices, digital entitlement | 2 | ✅ |
| 19 | `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/populator/order/PersistableOrderProductPopulator.java` | 1-167 | API line snapshot, product/store validation, attributes, digital entitlement | 1 | ✅ |
| 20 | `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/populator/order/PersistableOrderPopulator.java` | 1-236 | Customer/store/currency/status/history/order totals mapping | 1 | ✅ |
| 21 | `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/populator/order/PersistableOrderApiPopulator.java` | 1-221 | API customer/address/currency/status/channel mapping | 1 | ✅ |
| 22 | `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/populator/order/ReadableOrderPopulator.java` | 1-213 | Order response fields, address snapshot, total mapping | 1 | ✅ |
| 23 | `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/populator/order/ReadableOrderProductPopulator.java` | 1-162 | Line response, subtotal formula, attributes, catalog display boundary | 1 | ✅ |
| 24 | `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/populator/order/ReadableOrderProductDownloadPopulator.java` | 1-39 | Download response mapping | 1 | ✅ |
| 25 | `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/populator/order/ReadableOrderStatusHistoryPopulator.java` | searched; absent | Filename search and repository grep; no alternate implementation found | 0 — not found | N/A |
| 26 | `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/order/orderproduct/OrderProductDownloadService.java` | 1-18 | Download lookup contract | 0 | ✅ |
| 27 | `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/order/orderproduct/OrderProductDownloadServiceImpl.java` | 1-38 | Download lookup delegation | 0 | ✅ |
| 28 | `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/repositories/order/orderproduct/OrderProductDownloadRepository.java` | 1-18 | Download and order fetch predicates | 1 | ✅ |
| 29 | `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/modules/order/InvoiceModule.java` | 1-14 | Invoice interface | 1 | ✅ |
| 30 | `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/modules/order/ODSInvoiceModule.java` | 1-387, multi-pass | Active not-implemented method and commented spreadsheet/PDF implementation | 1 | ✅ |
| 31 | `initial-source/shopizer-3.2.7/sm-core/src/main/resources/spring/shopizer-core-modules.xml` | 1-142 | Payment/shipping module maps, invoice bean, email wiring | 1 | ✅ |
| 32 | `initial-source/shopizer-3.2.7/sm-core-model/src/main/java/com/salesmanager/core/model/order/Order.java` | 1-409, multi-pass | Table, columns, embedded addresses, relationships, fields | 3 | ✅ |
| 33 | `initial-source/shopizer-3.2.7/sm-core-model/src/main/java/com/salesmanager/core/model/order/OrderTotal.java` | 1-157 | Table and total columns | 1 | ✅ |
| 34 | `initial-source/shopizer-3.2.7/sm-core-model/src/main/java/com/salesmanager/core/model/order/OrderTotalType.java` | 1-7 | Total-type enum | 1 | ✅ |
| 35 | `initial-source/shopizer-3.2.7/sm-core-model/src/main/java/com/salesmanager/core/model/order/OrderValueType.java` | 1-7 | Total value-type enum | 0 | ✅ |
| 36 | `initial-source/shopizer-3.2.7/sm-core-model/src/main/java/com/salesmanager/core/model/order/OrderSummary.java` | 1-54 | Summary products, shipping summary, promotion code boundary | 0 — upstream calculation boundary | ✅ |
| 37 | `initial-source/shopizer-3.2.7/sm-core-model/src/main/java/com/salesmanager/core/model/order/orderproduct/OrderProduct.java` | 1-140 | Purchased line table and relationships | 1 | ✅ |
| 38 | `initial-source/shopizer-3.2.7/sm-core-model/src/main/java/com/salesmanager/core/model/order/orderproduct/OrderProductAttribute.java` | 1-134 | Purchased attribute snapshot fields | 1 | ✅ |
| 39 | `initial-source/shopizer-3.2.7/sm-core-model/src/main/java/com/salesmanager/core/model/order/orderproduct/OrderProductPrice.java` | 1-140 | Purchased price snapshot fields | 1 | ✅ |
| 40 | `initial-source/shopizer-3.2.7/sm-core-model/src/main/java/com/salesmanager/core/model/order/orderproduct/OrderProductDownload.java` | 1-92 | Download entitlement fields and default 31-day duration | 1 | ✅ |
| 41 | `initial-source/shopizer-3.2.7/sm-core-model/src/main/java/com/salesmanager/core/model/order/orderstatus/OrderStatus.java` | 1-21 | Declared order states | 1 | ✅ |
| 42 | `initial-source/shopizer-3.2.7/sm-core-model/src/main/java/com/salesmanager/core/model/order/orderstatus/OrderStatusHistory.java` | 1-109 | History table and audit fields | 1 | ✅ |
| 43 | `initial-source/shopizer-3.2.7/sm-core-model/src/main/java/com/salesmanager/core/model/order/attributes/OrderAttribute.java` | 1-81 | Order attribute table and fields | 1 | ✅ |
| 44 | `initial-source/shopizer-3.2.7/sm-core-model/src/main/java/com/salesmanager/core/model/order/orderaccount/OrderAccount.java` | 1-106 | Conditional recurring-account ownership assessment | 0 — conditional migration | ✅ |
| 45 | `initial-source/shopizer-3.2.7/sm-core-model/src/main/java/com/salesmanager/core/model/order/orderaccount/OrderAccountProduct.java` | 1-166 | Conditional recurring-account product assessment | 0 — conditional migration | ✅ |
| 46 | `initial-source/shopizer-3.2.7/sm-core-model/src/main/java/com/salesmanager/core/model/payments/Transaction.java` | 1-187 | Payment table boundary and fields; no target ownership | 1 | ✅ |
| 47 | `initial-source/shopizer-3.2.7/sm-core-model/src/main/java/com/salesmanager/core/model/payments/TransactionType.java` | 1-7 | Payment transaction types and next-action evidence | 1 | ✅ |
| 48 | `initial-source/shopizer-admin-main/src/app/pages/orders/order-details/order-details.component.ts` | 1-314 | Admin order detail interactions, status history, transactions, capture, refund, snapshot update | 1 | ✅ |
| 49 | `initial-source/shopizer-admin-main/src/app/pages/orders/services/orders.service.ts` | 1-58 | Admin client endpoint coverage | 1 | ✅ |

## High-Value Source References

| Behavior | Exact evidence |
|---|---|
| Initial `ORDERED` state | `OrderFacadeImpl.java:173-195`; `OrderServiceImpl.java:149-163` |
| Snapshot construction | `OrderFacadeImpl.java:345-465`; `PersistableOrderApiPopulator.java:93-146` |
| Product line snapshot | `OrderProductPopulator.java:68-145`; `PersistableOrderProductPopulator.java:65-140` |
| Digital entitlement | `OrderProductPopulator.java:78-87`; `PersistableOrderProductPopulator.java:75-84` |
| Accepted total comparison | `OrderFacadeImpl.java:1261-1306` |
| Total-component construction | `OrderServiceImpl.java:217-394` |
| Payment persistence boundary | `OrderServiceImpl.java:146-188` |
| Capturable-order algorithm | `OrderServiceImpl.java:600-676` |
| Next action algorithm | `OrderFacadeImpl.java:1554-1585` |
| Store filtering | `OrderRepository.java:10-20`; `OrderRepositoryImpl.java:152-239` |
| Customer visibility | `OrderApi.java:292-330`; `OrderRepositoryImpl.java:70-74` |
| Status update defect | `OrderFacadeImpl.java:1624-1647` |
| History append | `OrderServiceImpl.java:109-114`; `OrderFacadeImpl.java:1458-1484` |
| Admin UI operations | `order-details.component.ts:196-290`; `orders.service.ts:37-56` |
| Invoice incompleteness | `InvoiceModule.java:10-12`; `ODSInvoiceModule.java:45-50`; `shopizer-core-modules.xml:103-106` |

## CAST Evidence Register

| Evidence | Reference |
|---|---|
| Authenticated checkout | Transaction `244089`, 3,245 full objects |
| Anonymous checkout | Transaction `244090`, 3,262 full objects |
| Administrative detail | Transaction `244087`, 1,585 full objects |
| Status update | Transaction `244092`, 353 full objects |
| Next payment action | Transaction `244095`, 333 full objects |
| Payment transaction list | Transaction `244096`, 407 full objects |
| Capturable orders | Transaction `244097`, 580 full objects |
| Capture stub | Transaction `244098`, 9 objects |
| Refund stub | Transaction `244099`, 9 objects |
| Authorize stub | Transaction `244100`, 9 objects |
| Consolidated order data graph | Graph `243908`, 62 nodes / 245 links |
| Order-product data graph | Graph `243909`, 10 nodes / 18 links |
| Payment transaction graph | Graph `243929`, 22 nodes / 82 links |
| Checkout/cart boundary | Graph `243932`, 111 nodes / 638 links |
| Order process model | CAST object `30007`, complexity 29 |
| Order process | CAST object `30017`, complexity 24 |
| Order total calculation | CAST object `29389`, complexity 22 |
| Capturable-order discovery | CAST object `29392`, complexity 12 |
| Next transaction | CAST object `30027`, complexity 8 |
| Status update | CAST object `30030`, complexity 4 |

## Source Semantic Vectors

Counts are direct-source semantic counts for the primary business components. Counts include infrastructure branches where the source-reading guide requires them.

| Component | Control-flow | Data-flow | Constants | State transitions | Outcomes | Data writes | Integrations | Error paths |
|---|---:|---:|---:|---:|---:|---:|---:|---:|
| `OrderFacadeImpl.processOrderModel` | 29 | 34 | 5 | 2 | 9 | 18 | 8 | 7 |
| `OrderFacadeImpl.processOrder` | 24 | 31 | 4 | 1 | 8 | 15 | 9 | 8 |
| `OrderServiceImpl.process` | 18 | 29 | 3 | 2 | 7 | 17 | 7 | 6 |
| `OrderServiceImpl.caculateOrder` | 22 | 32 | 9 | 0 | 6 | 12 | 5 | 4 |
| `OrderServiceImpl.getCapturableOrders` | 12 | 9 | 4 | 0 | 3 | 0 | 1 | 1 |
| `OrderFacadeImpl.nextTransaction` | 8 | 5 | 5 | 0 | 5 | 0 | 1 | 1 |
| `OrderFacadeImpl.updateOrderStatus` | 4 | 6 | 2 | 1 | 3 | 2 | 0 | 1 |
| `OrderProductPopulator.populate` | 14 | 24 | 1 | 0 | 7 | 12 | 3 | 6 |
| `PersistableOrderApiPopulator.populate` | 11 | 21 | 3 | 1 | 6 | 10 | 4 | 5 |
| `OrderRepositoryImpl.listOrders` | 21 | 25 | 4 | 0 | 6 | 0 | 1 | 3 |
| `ReadableOrderPopulator.populate` | 13 | 31 | 5 | 0 | 4 | 0 | 3 | 2 |
| `ODSInvoiceModule.createInvoice` active path | 0 | 0 | 0 | 0 | 1 | 0 | 0 | 1 |

## Excluded or Context-Only Components

| Component | Treatment | Evidence |
|---|---|---|
| `PaymentServiceImpl.java` | Context only; MS-06 owns provider behavior | Brief lines 258-275 |
| `TransactionServiceImpl.java` | Context only; MS-06 owns payment transactions | Brief lines 258-275, 363-385 |
| Payment provider adapters | Excluded from MS-05 | Brief lines 299-313 |
| Tax implementation | Excluded; MS-08 | Brief lines 153-165 |
| Shipping integration implementations | Excluded; MS-09 | Brief lines 258-275 |
| Shopping-cart services | Excluded; MS-04 | Brief lines 258-275 |
| `ProductPriceUtils.java` | Excluded; MS-07/catalog | Brief lines 153-165 |
| Duplicate `store/facade/order/OrderFacadeImpl.java` | Not read; active path is controller facade | Brief lines 287-295 |
| `PayPalRestPayment.java` | Excluded dead-code candidate for MS-06 | Brief lines 299-313 |
| Product event publisher | Excluded; no order event behavior | Brief lines 700-714 |
| `FilesController` | Context only for entitlement boundary | Brief lines 718-725 |
| Scheduled jobs | None found | Brief lines 725-730 |

## Hidden-Engine Findings

The module is not CRUD-only:

- CAST checkout graphs contain 3,245–3,262 full objects.
- Administrative detail contains 1,585 full objects.
- Order totals invoke configurable postprocessors.
- Payment status and capturable discovery contain branching algorithms.
- Status values form an implicit lifecycle without legal transition enforcement.
- Digital entitlement creation is embedded in line-snapshot creation.
- No legacy cancellation, fulfillment, or order-event engine was found; these are target capabilities and are explicitly marked unresolved.

## Missing-File Search Record

**Missing file:**  
`initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/populator/order/ReadableOrderStatusHistoryPopulator.java`

**Search performed:**

1. Exact path existence check.
2. Filename search for `ReadableOrderStatusHistoryPopulator`.
3. Repository grep for class name and `ReadableOrderStatusHistory`.
4. Directory inspection of `sm-shop/.../populator/order`.

**Result:** No alternate implementation found. History response mapping is instead performed inline by `OrderFacadeImpl.mapToReadbleOrderStatusHistory()` lines 1447-1455.

## Extraction Status

- Mandatory source files listed: 49
- Existing mandatory source files read: 48
- Missing mandatory source file: 1, explicitly recorded above
- Primary business components with vectors: 12
- Rules extracted: 23
- Every BR-ID has an 8-dimensional preservation table.
- Every BR-ID has a concrete success and error example.
- Every source-derived BR-ID has an exact source reference.
