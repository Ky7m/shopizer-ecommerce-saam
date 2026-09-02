# Order Management — Business Rules

**Service:** MS-05 Order Management  
**Version:** 1.0  
**Date:** 2026-09-01  
**Analysis mode:** Hybrid — CAST structure plus direct source read  
**Target schema:** `order_management`  
**Target port:** `8105`

## Scope and Boundary

MS-05 owns the order aggregate, immutable purchased-item and monetary snapshots, order lifecycle, order history, customer-visible order retrieval, administrative order operations, fulfillment-order coordination, and application of authenticated payment outcomes.

The following remain outside MS-05:

- Cart and checkout submission: MS-04.
- Payment-provider state and payment transaction ownership: MS-06.
- Product, inventory, availability, and catalog state: MS-02.
- Pricing and promotion calculation: MS-07.
- Tax calculation: MS-08.
- Shipping quotes and carrier execution: MS-09/MS-12.
- Email, invoice delivery, file storage, and external delivery: MS-12.

## Business Rule Index

| BR-ID | Name | Intent | Classification |
|---|---|---|---|
| BR-OR-SUB-001 | Initial order state and history | State Transition | Core |
| BR-OR-SUB-002 | Customer, store, and address snapshot | Compliance | Core |
| BR-OR-SUB-003 | Purchased-line snapshot | Calculation | Core |
| BR-OR-SUB-004 | Accepted monetary snapshot | Calculation | Core |
| BR-OR-PAY-001 | Payment handoff boundary | Routing | Core |
| BR-OR-DIG-001 | Digital entitlement creation | Routing | Core |
| BR-OR-FAIL-001 | Submission failure and compensation | Routing | Core |
| BR-OR-LIFE-001 | Legal order-state progression | State Transition | Core |
| BR-OR-LIFE-002 | Append-only lifecycle history | Compliance | Core |
| BR-OR-PAY-002 | Payment-status reconciliation | State Transition | Core |
| BR-OR-PAY-003 | Next payment action | Routing | Core |
| BR-OR-PAY-004 | Capturable-order discovery | Routing | Core |
| BR-OR-REF-001 | Refund balance reconciliation | Calculation | Core |
| BR-OR-CAN-001 | Cancellation orchestration | State Transition | Edge Case / target capability |
| BR-OR-FUL-001 | Fulfillment request and shipment status | State Transition | Edge Case / target capability |
| BR-OR-AUTH-001 | Tenant and store isolation | Authorization | Core |
| BR-OR-AUTH-002 | Customer order visibility | Authorization | Core |
| BR-OR-ADM-001 | Administrative order authorization | Authorization | Core |
| BR-OR-ADM-002 | Customer snapshot correction | Compliance | Core |
| BR-OR-READ-001 | Order read projection | Routing | Core |
| BR-OR-RES-001 | Idempotent submission and event application | Compliance | Target requirement |
| BR-OR-INV-001 | Invoice generation boundary | Routing | Edge Case |
| BR-OR-UI-001 | Administrative order-detail coverage | Routing | Core; re-extracts BR-UI-009 |

---

### BR-OR-SUB-001: Initial order state and history

**Source Reference:** `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/controller/order/facade/OrderFacadeImpl.java` : `initializeOrder()` lines 173-195; `processOrderModel()` lines 345-381  
**Cross-Reference:** `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/order/OrderServiceImpl.java` : `process()` lines 149-163  
**Discovery Method:** Direct Source Read + CAST Imaging (Hybrid)  
**CAST Reference:** Transactions `244089`, `244090`; objects `30007`, `30017`

**Statement:** Every newly accepted order starts in `ORDERED`. The order must have an initial lifecycle-history entry dated at acceptance time. A submission comment may be recorded as an additional history entry without changing the order state.

**Intent:** State Transition
**Classification:** Core
**Weight:** Critical

**Logic:**
```pseudocode
orderStatus = ORDERED
order.setOrderStatus(orderStatus)

if order.orderHistory is null OR order.orderHistory is empty OR order.status is null:
    if order.status is null:
        order.status = ORDERED

    initialHistory = new OrderStatusHistory()
    initialHistory.status = order.status
    initialHistory.dateAdded = currentDate()
    initialHistory.order = order
    order.orderHistory.add(initialHistory)

if order.comments is not blank:
    commentHistory = new OrderStatusHistory()
    commentHistory.status = ORDERED
    commentHistory.dateAdded = currentDate()
    commentHistory.comments = order.comments
    commentHistory.order = modelOrder
    modelOrder.orderHistory.add(commentHistory)
```

**Data Dependencies:**
- Reads: `ORDERS.ORDER_STATUS`, `ORDER_STATUS_HISTORY.DATE_ADDED`, submitted order comments
- Writes: `ORDERS.ORDER_STATUS`, `ORDER_STATUS_HISTORY.ORDER_ID`, `ORDER_STATUS_HISTORY.STATUS`, `ORDER_STATUS_HISTORY.DATE_ADDED`, `ORDER_STATUS_HISTORY.COMMENTS`

**Side Effects:**
- Publishes target `OrderAccepted` through the transactional outbox.
- Does not call MS-06 directly for provider state.

**Semantic Preservation:**

| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 4 | 4 | OK |
| Data-flow | 5 | 5 | OK |
| Constants | 1 | 1 | OK (`ORDERED`) |
| State transitions | 1 | 1 | OK |
| Outcomes | 2 | 2 | OK |
| Data writes | 5 | 5 | OK |
| Integrations | 0 | 1 | GAP — target outbox is new |
| Error paths | 0 | 1 | GAP — target transaction failure is explicit |

**Preservation:** FLAGGED (integrations, error paths)

**Concrete Example:**
- API Input: `POST /api/v1/internal/order-submissions`
  ```json
  {
    "submissionId": "sub-10001",
    "storeId": 12,
    "customerId": 481,
    "comments": "Leave package at reception",
    "lines": [{"sku": "CAM-100", "quantity": 1, "unitPrice": 249.99}],
    "total": 249.99,
    "currency": "USD"
  }
  ```
- Success: `201`
  ```json
  {"orderId": 70001, "status": "ORDERED", "historyCreated": true}
  ```
- Error Input:
  ```json
  {"submissionId": "sub-10001", "storeId": 12, "lines": [], "total": 0}
  ```
- Error Output: `422`
  ```json
  {"error": "ORDER_LINES_REQUIRED", "message": "An order must contain at least one purchased line.", "statusCode": 422}
  ```

---

### BR-OR-SUB-002: Customer, store, and address snapshot

**Source Reference:** `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/controller/order/facade/OrderFacadeImpl.java` : `processOrderModel()` lines 350-365, 446-450, 567-578  
**Cross-Reference:** `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/populator/order/PersistableOrderApiPopulator.java` : `populate()` lines 93-146  
**Discovery Method:** Direct Source Read + CAST Imaging (Hybrid)  
**CAST Reference:** Transaction `244090`; data graph `243908`

**Statement:** An order preserves the customer identity, store identity, email address, billing address, delivery address, currency, payment method, shipping method, customer agreement, and address-confirmation values that applied when the order was accepted. Later customer-profile changes must not rewrite the accepted order snapshot.

**Intent:** Compliance
**Classification:** Core
**Weight:** Critical

**Logic:**
```pseudocode
modelOrder.billing = customer.billing
modelOrder.delivery = customer.delivery
modelOrder.customerEmailAddress = customer.emailAddress
modelOrder.customerId = customer.id
modelOrder.currency = currency
modelOrder.merchant = store
modelOrder.paymentModuleCode = order.paymentModule
modelOrder.paymentType = PaymentType.valueOf(order.paymentMethodType)
modelOrder.shippingModuleCode = order.shippingModule
modelOrder.customerAgreement = order.customerAgreed
modelOrder.confirmedAddress = true for API submission
modelOrder.locale = LocaleUtils.getLocale(store)
```

**Data Dependencies:**
- Reads: `ORDERS.CUSTOMER_ID`, `ORDERS.CUSTOMER_EMAIL_ADDRESS`, embedded billing fields, embedded delivery fields, `CURRENCY_ID`, `MERCHANTID`, `PAYMENT_MODULE_CODE`, `PAYMENT_TYPE`, `SHIPPING_MODULE_CODE`, `CUSTOMER_AGREED`, `CONFIRMED_ADDRESS`, `LOCALE`
- Writes: the same order snapshot fields in the target order aggregate

**Side Effects:**
- No write to the customer master record.
- Customer and store ownership are resolved through MS-01/MS-10 context.

**Semantic Preservation:**

| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 5 | 4 | GAP — API and web paths differ |
| Data-flow | 15 | 15 | OK |
| Constants | 2 | 2 | OK |
| State transitions | 0 | 0 | OK |
| Outcomes | 3 | 3 | OK |
| Data writes | 15 | 15 | OK |
| Integrations | 4 | 2 | GAP — target ownership is boundary-based |
| Error paths | 5 | 5 | OK |

**Preservation:** FLAGGED (control-flow, integrations)

**Concrete Example:**
- API Input: `POST /api/v1/internal/order-submissions`
  ```json
  {
    "submissionId": "sub-10002",
    "customerId": 481,
    "storeId": 12,
    "customerEmail": "ana@example.com",
    "billingAddress": {"firstName": "Ana", "lastName": "Silva", "address": "10 Main St", "city": "Austin", "country": "US", "postalCode": "78701"},
    "deliveryAddress": {"firstName": "Ana", "lastName": "Silva", "address": "10 Main St", "city": "Austin", "country": "US", "postalCode": "78701"},
    "currency": "USD",
    "paymentMethod": "CREDITCARD"
  }
  ```
- Success: `201`
  ```json
  {"orderId": 70002, "customerId": 481, "billingAddress":{"city":"Austin"}, "status":"ORDERED"}
  ```
- Error Input:
  ```json
  {"submissionId":"sub-10002","customerId":481,"storeId":99,"currency":"USD"}
  ```
- Error Output: `403`
  ```json
  {"error":"STORE_ACCESS_DENIED","message":"The submission does not belong to the requested store.","statusCode":403}
  ```

---

### BR-OR-SUB-003: Purchased-line snapshot

**Source Reference:** `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/populator/order/OrderProductPopulator.java` : `populate()` lines 68-115  
**Cross-Reference:** `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/populator/order/PersistableOrderProductPopulator.java` : `populate()` lines 65-112  
**Discovery Method:** Direct Source Read + CAST Imaging (Hybrid)  
**CAST Reference:** Object `OrderProductPopulator`; data graph `243909`

**Statement:** Each accepted order line stores the purchased SKU, product name, quantity, one-time charge, selected attributes, attribute prices, selected price records, and any digital-file metadata as a historical snapshot. The order detail must remain readable even if the catalog later changes.

**Intent:** Calculation
**Classification:** Core
**Weight:** Critical

**Logic:**
```pseudocode
modelProduct = productService.getBySku(source.sku, store, language)
if modelProduct is null:
    raise ConversionException("Cannot get product with sku ...")
if modelProduct.merchantStore.id != store.id:
    raise ConversionException("Invalid product with sku ...")

target.oneTimeCharge = source.itemPrice
target.productName = source.product.descriptions.first.name
target.productQuantity = source.quantity
target.sku = source.product.sku

finalPrice = source.finalPrice
if finalPrice is null:
    raise ConversionException("Object final price not populated")

target.prices.add(orderProductPrice(finalPrice))
for each additionalPrice in finalPrice.additionalPrices:
    target.prices.add(orderProductPrice(additionalPrice))

for each selectedAttribute in source.attributes:
    attr = productAttributeService.getById(selectedAttribute.productAttributeId)
    if attr is null:
        raise ConversionException("Attribute does not exist")
    if attr.product.merchantStore.id != store.id:
        raise ConversionException("Attribute invalid for this store")
    snapshot attribute names, option identifiers, price, weight, and free flag
```

**Data Dependencies:**
- Reads: product SKU/name, `ORDER_PRODUCT.PRODUCT_SKU`, `ORDER_PRODUCT.PRODUCT_NAME`, `ORDER_PRODUCT.PRODUCT_QUANTITY`, `ORDER_PRODUCT.ONETIME_CHARGE`, `ORDER_PRODUCT_ATTRIBUTE.PRODUCT_ATTRIBUTE_PRICE`, `PRODUCT_ATTRIBUTE_IS_FREE`, `PRODUCT_ATTRIBUTE_WEIGHT`, `PRODUCT_OPTION_ID`, `PRODUCT_OPTION_VALUE_ID`, `PRODUCT_ATTRIBUTE_NAME`, `PRODUCT_ATTRIBUTE_VAL_NAME`, `ORDER_PRODUCT_PRICE.PRODUCT_PRICE_CODE`, `PRODUCT_PRICE`, `PRODUCT_PRICE_SPECIAL`, special-date fields, `DEFAULT_PRICE`, `PRODUCT_PRICE_NAME`
- Writes: `ORDER_PRODUCT`, `ORDER_PRODUCT_ATTRIBUTE`, `ORDER_PRODUCT_PRICE`

**Side Effects:**
- Reads MS-02 catalog and product-attribute data.
- No catalog mutation.

**Semantic Preservation:**

| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 14 | 14 | OK |
| Data-flow | 24 | 24 | OK |
| Constants | 0 | 0 | OK |
| State transitions | 0 | 0 | OK |
| Outcomes | 7 | 7 | OK |
| Data writes | 22 | 22 | OK |
| Integrations | 3 | 1 | GAP — inventory/catalog boundary consolidated |
| Error paths | 6 | 6 | OK |

**Preservation:** FLAGGED (integrations)

**Concrete Example:**
- API Input:
  ```json
  {"submissionId":"sub-10003","lines":[{"sku":"SHOE-42","productName":"Trail Shoe","quantity":2,"unitPrice":79.50,"attributes":[{"optionId":8,"valueId":22,"name":"Size","value":"42","price":0}]}]}
  ```
- Success: `201`
  ```json
  {"orderId":70003,"lines":[{"sku":"SHOE-42","productName":"Trail Shoe","quantity":2,"unitPrice":79.50,"attributes":[{"name":"Size","value":"42","price":0}]}]}
  ```
- Error Input:
  ```json
  {"submissionId":"sub-10003","lines":[{"sku":"SHOE-42","quantity":2,"unitPrice":79.50,"attributes":[{"optionId":999,"valueId":22}]}]}
  ```
- Error Output: `422`
  ```json
  {"error":"ATTRIBUTE_INVALID","message":"The selected product attribute is invalid for this store.","statusCode":422}
  ```

---

### BR-OR-SUB-004: Accepted monetary snapshot

**Source Reference:** `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/controller/order/facade/OrderFacadeImpl.java` : `processOrder(v1 PersistableOrder)` lines 1261-1306; `setOrderTotals()` lines 300-314  
**Cross-Reference:** `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/order/OrderServiceImpl.java` : `caculateOrder()` lines 217-394  
**Discovery Method:** Direct Source Read + CAST Imaging (Hybrid)  
**CAST Reference:** Object `29389`; data graph `243908`

**Statement:** MS-05 stores the validated total supplied by the checkout boundary and its component lines as immutable order facts. MS-05 does not recalculate pricing, promotions, tax, or shipping after acceptance.

**Intent:** Calculation
**Classification:** Core
**Weight:** Critical

**Logic:**
```pseudocode
calculatedAmount = orderTotalSummary.total
submittedAmount = productPriceUtils.getAmount(order.payment.amount)

if calculatedAmount.compareTo(submittedAmount) != 0:
    raise ConversionException(
      "Payment.amount does not match calculated total")

modelOrder.total = calculatedAmount

for each total in orderTotalSummary.totals:
    total.order = modelOrder
    modelOrder.orderTotal.add(total)
```

**Data Dependencies:**
- Reads: `ORDERS.ORDER_TOTAL`, `ORDER_TOTAL.CODE`, `TITLE`, `TEXT`, `VALUE`, `MODULE`, `ORDER_VALUE_TYPE`, `ORDER_TOTAL_TYPE`, `SORT_ORDER`
- Writes: `ORDERS.ORDER_TOTAL`, `ORDER_TOTAL`

**Side Effects:**
- Consumes validated pricing/tax/shipping snapshot from MS-04/MS-07/MS-08/MS-09.
- Refund reconciliation may later add credit/refund facts; original totals are not overwritten.

**Semantic Preservation:**

| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 8 | 8 | OK |
| Data-flow | 14 | 14 | OK |
| Constants | 2 | 2 | OK |
| State transitions | 0 | 0 | OK |
| Outcomes | 3 | 3 | OK |
| Data writes | 11 | 11 | OK |
| Integrations | 5 | 4 | GAP — target consumes snapshots |
| Error paths | 3 | 3 | OK |

**Preservation:** FLAGGED (integrations)

**Concrete Example:**
- API Input:
  ```json
  {"submissionId":"sub-10004","currency":"USD","total":129.50,"totals":[{"code":"subtotal","type":"SUBTOTAL","value":119.50},{"code":"tax","type":"TAX","value":10.00}]}
  ```
- Success: `201`
  ```json
  {"orderId":70004,"total":129.50,"currency":"USD","totals":[{"code":"subtotal","value":119.50},{"code":"tax","value":10.00}]}
  ```
- Error Input:
  ```json
  {"submissionId":"sub-10004","currency":"USD","total":130.50,"calculatedTotal":129.50}
  ```
- Error Output: `422`
  ```json
  {"error":"TOTAL_MISMATCH","message":"Submitted total 130.50 does not match accepted total 129.50.","statusCode":422}
  ```

---

### BR-OR-PAY-001: Payment handoff boundary

**Source Reference:** `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/order/OrderServiceImpl.java` : `process()` lines 127-188  
**Cross-Reference:** `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/controller/order/facade/OrderFacadeImpl.java` : `processOrderModel()` lines 457-555  
**Discovery Method:** Direct Source Read + CAST Imaging (Hybrid)  
**CAST Reference:** Transactions `244089`, `244090`; object `29399`

**Statement:** Payment authorization, capture, refund, and provider-specific transaction state belong to MS-06. MS-05 records only the authenticated payment outcome and correlates it to the order; it must not own provider credentials or write the payment provider ledger.

**Intent:** Routing
**Classification:** Core
**Weight:** Critical

**Logic:**
```pseudocode
processTransaction = paymentService.processPayment(customer, store, payment, items, order)

if processTransaction exists:
    processTransaction.order = order
    if processTransaction.id is empty:
        transactionService.create(processTransaction)
    else:
        transactionService.update(processTransaction)

if caller supplied transaction exists:
    transaction.order = order
    if transaction.id is empty:
        transactionService.create(transaction)
    else:
        transactionService.update(transaction)
```

**Data Dependencies:**
- Reads: `SM_TRANSACTION.TRANSACTION_TYPE`, `TRANSACTION_DATE`, `AMOUNT`, `ORDER_ID`, payment module and payment type
- Writes in legacy: `SM_TRANSACTION`
- Target writes: payment outcome projection and correlation identifiers only

**Side Effects:**
- Calls MS-06 payment command/event boundary.
- No direct target write to `SM_TRANSACTION`.

**Semantic Preservation:**

| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 9 | 8 | GAP |
| Data-flow | 9 | 8 | GAP — provider table excluded by boundary |
| Constants | 0 | 0 | OK |
| State transitions | 2 | 2 | OK |
| Outcomes | 4 | 4 | OK |
| Data writes | 6 | 3 | GAP — ownership intentionally changed |
| Integrations | 3 | 3 | OK |
| Error paths | 2 | 3 | GAP — asynchronous failure added |

**Preservation:** FLAGGED (control-flow, data-flow, data writes, error paths)

**Concrete Example:**
- API Input: `POST /api/v1/orders/70005/capture`
  ```json
  {"amount":249.99,"currency":"USD","paymentReference":"pay-8841"}
  ```
- Success: `202`
  ```json
  {"orderId":70005,"paymentAction":"CAPTURE","status":"PROCESSING","correlationId":"corr-5"}
  ```
- Error Input:
  ```json
  {"amount":300.00,"currency":"USD","paymentReference":"pay-8841"}
  ```
- Error Output: `422`
  ```json
  {"error":"PAYMENT_AMOUNT_INVALID","message":"Capture amount exceeds the order's refundable or capturable balance.","statusCode":422}
  ```

---

### BR-OR-DIG-001: Digital entitlement creation

**Source Reference:** `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/populator/order/OrderProductPopulator.java` : `populate()` lines 78-87  
**Cross-Reference:** `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/populator/order/PersistableOrderProductPopulator.java` : `populate()` lines 75-84; `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/order/orderproduct/OrderProductDownloadServiceImpl.java` : lines 18-35  
**Discovery Method:** Direct Source Read + CAST Imaging (Hybrid)  
**CAST Reference:** Object `OrderProductPopulator`; data graph `243909`

**Statement:** When an accepted purchased product has a digital file, the order receives one download entitlement containing the file name, an initial download count of zero, and the configured entitlement duration. The entitlement is tied to the purchased order line, not to the mutable product catalog.

**Intent:** Routing
**Classification:** Active
**Weight:** Medium

**Logic:**
```pseudocode
digitalProduct = digitalProductService.getByProduct(store, modelProduct)

if digitalProduct exists:
    entitlement = new OrderProductDownload()
    entitlement.orderProductFilename = digitalProduct.productFileName
    entitlement.orderProduct = target
    entitlement.downloadCount = 0
    entitlement.maxdays = ApplicationConstants.MAX_DOWNLOAD_DAYS
    target.downloads.add(entitlement)
```

**Data Dependencies:**
- Reads: digital-product file name and configured maximum download days
- Writes: `ORDER_PRODUCT_DOWNLOAD.ORDER_PRODUCT_ID`, `ORDER_PRODUCT_FILENAME`, `DOWNLOAD_COUNT`, `DOWNLOAD_MAXDAYS`

**Side Effects:**
- Publishes `DownloadEntitlementGranted`.
- MS-12 may deliver download instructions.
- Customer download authorization is enforced by MS-05.

**Semantic Preservation:**

| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 3 | 3 | OK |
| Data-flow | 7 | 7 | OK |
| Constants | 1 | 1 | OK (`MAX_DOWNLOAD_DAYS`) |
| State transitions | 0 | 1 | GAP — target entitlement access state |
| Outcomes | 3 | 3 | OK |
| Data writes | 5 | 5 | OK |
| Integrations | 1 | 2 | GAP — target event is new |
| Error paths | 1 | 2 | GAP |

**Preservation:** FLAGGED (state transitions, integrations, error paths)

**Concrete Example:**
- API Input:
  ```json
  {"orderId":70006,"lines":[{"sku":"EBOOK-JAVA","quantity":1,"digitalFileName":"java-guide.pdf"}]}
  ```
- Success: `201`
  ```json
  {"orderId":70006,"downloads":[{"fileName":"java-guide.pdf","downloadCount":0,"downloadExpiryDays":31,"accessState":"AVAILABLE"}]}
  ```
- Error Input:
  ```json
  {"orderId":70006,"lines":[{"sku":"EBOOK-JAVA","quantity":1,"digitalFileName":""}]}
  ```
- Error Output: `422`
  ```json
  {"error":"DIGITAL_FILE_INVALID","message":"A digital entitlement requires a file name.","statusCode":422}
  ```

---

### BR-OR-FAIL-001: Submission failure and compensation

**Source Reference:** `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/order/OrderServiceImpl.java` : `process()` lines 165-214  
**Cross-Reference:** `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/api/v1/order/OrderApi.java` : checkout handlers lines 343-391 and 402-469  
**Discovery Method:** Direct Source Read + CAST Imaging (Hybrid)  
**CAST Reference:** Transactions `244089`, `244090`

**Statement:** A submission must not remain partially accepted when an order-line, inventory-reservation, payment, or persistence step fails. The target uses a saga: order acceptance is durable only when local order writes succeed, and every completed external reservation or payment action has an explicit compensating action or retry state.

**Intent:** Routing
**Classification:** Active
**Weight:** Medium

**Logic:**
```pseudocode
process payment
initialize order history
create customer when required
create order
persist payment transactions
for each orderProduct:
    resolve product
    if product missing:
        raise INVENTORY_MISMATCH
    if availability < requested quantity:
        legacy logs mismatch but continues
    decrement availability
    update product

legacy controller converts exceptions to 503 or runtime errors
target:
    commit local order transaction atomically
    publish outbox event
    compensate or mark FAILED for external steps that cannot complete
```

**Data Dependencies:**
- Reads: order, order lines, payment outcome, product availability
- Writes: order aggregate; no direct target write to MS-02 availability
- Legacy writes: product availability and payment transaction records

**Side Effects:**
- Target publishes `OrderProcessingFailed` or `OrderCompensationRequired`.
- MS-02 reservation release is a declared dependency, not a direct table update.

**Semantic Preservation:**

| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 14 | 13 | GAP |
| Data-flow | 11 | 9 | GAP — foreign writes removed |
| Constants | 1 | 1 | OK |
| State transitions | 4 | 5 | GAP — explicit failure state added |
| Outcomes | 6 | 6 | OK |
| Data writes | 10 | 7 | GAP — saga boundary |
| Integrations | 5 | 6 | GAP — compensation events added |
| Error paths | 5 | 7 | GAP — failure matrix expanded |

**Preservation:** FLAGGED (multiple dimensions; target compensation is required)

**Concrete Example:**
- API Input:
  ```json
  {"submissionId":"sub-10007","lines":[{"sku":"LAPTOP-15","quantity":1}],"reservationId":"res-81","paymentIntentId":"pi-81"}
  ```
- Success: `201`
  ```json
  {"orderId":70007,"status":"ORDERED","processingState":"ACCEPTED"}
  ```
- Error Input:
  ```json
  {"submissionId":"sub-10007","lines":[{"sku":"LAPTOP-15","quantity":2}],"reservationId":"res-81","paymentIntentId":"pi-81"}
  ```
- Error Output: `409`
  ```json
  {"error":"ORDER_COMPENSATION_REQUIRED","message":"The order could not be accepted because the requested reservation is unavailable.","statusCode":409}
  ```

---

### BR-OR-LIFE-001: Legal order-state progression

**Source Reference:** `initial-source/shopizer-3.2.7/sm-core-model/src/main/java/com/salesmanager/core/model/order/orderstatus/OrderStatus.java` : lines 3-10  
**Cross-Reference:** `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/controller/order/facade/OrderFacadeImpl.java` : `updateOrderStatus()` lines 1624-1647; `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/order/OrderServiceImpl.java` : `process()` lines 149-163  
**Discovery Method:** Direct Source Read + CAST Imaging (Hybrid)  
**CAST Reference:** Transaction `244092`; object `30030`

**Statement:** The order lifecycle is closed: `ORDERED` is the initial state; `PROCESSED`, `DELIVERED`, `REFUNDED`, and `CANCELED` are the only declared states. The target rejects arbitrary state changes and rejects transitions out of terminal states.

**Intent:** State Transition
**Classification:** Core
**Weight:** Critical

**Logic:**
```pseudocode
allowedTransitions = {
  ORDERED:   {PROCESSED, CANCELED},
  PROCESSED: {DELIVERED, REFUNDED, CANCELED},
  DELIVERED: {REFUNDED},
  REFUNDED:  {},
  CANCELED:  {}
}

if requestedStatus not in allowedTransitions[currentStatus]:
    reject ORDER_STATUS_TRANSITION_INVALID

if currentStatus in {REFUNDED, CANCELED}:
    reject ORDER_TERMINAL

order.status = requestedStatus
append history
```

The legacy `updateOrderStatus()` only checks whether the requested status differs from the current status and therefore accepts invalid transitions. That behavior is classified as an obsolete defect.

**Data Dependencies:**
- Reads: `ORDERS.ORDER_STATUS`
- Writes: `ORDERS.ORDER_STATUS`, `ORDER_STATUS_HISTORY.STATUS`

**Side Effects:**
- Publishes `OrderStatusChanged`.
- A transition to `CANCELED` or `REFUNDED` starts compensation/reconciliation workflows.

**Semantic Preservation:**

| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 3 | 7 | GAP — target closes the machine |
| Data-flow | 3 | 3 | OK |
| Constants | 5 | 5 | OK |
| State transitions | 5 | 7 | GAP — guards and terminal behavior added |
| Outcomes | 3 | 5 | GAP |
| Data writes | 2 | 2 | OK |
| Integrations | 0 | 1 | GAP |
| Error paths | 1 | 4 | GAP |

**Preservation:** FLAGGED; legacy unrestricted update is obsolete

**Concrete Example:**
- API Input: `PUT /api/v1/orders/70008/status`
  ```json
  {"status":"PROCESSED","reason":"Payment captured"}
  ```
- Success: `200`
  ```json
  {"orderId":70008,"previousStatus":"ORDERED","status":"PROCESSED"}
  ```
- Error Input:
  ```json
  {"status":"ORDERED","reason":"Reopen order"}
  ```
  when current state is `DELIVERED`.
- Error Output: `409`
  ```json
  {"error":"ORDER_STATUS_TRANSITION_INVALID","message":"DELIVERED cannot transition to ORDERED.","statusCode":409}
  ```

---

### BR-OR-LIFE-002: Append-only lifecycle history

**Source Reference:** `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/order/OrderServiceImpl.java` : `addOrderStatusHistory()` lines 109-114  
**Cross-Reference:** `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/controller/order/facade/OrderFacadeImpl.java` : `getReadableOrderHistory()` lines 1432-1455 and `createOrderStatus()` lines 1458-1484  
**Discovery Method:** Direct Source Read + CAST Imaging (Hybrid)  
**CAST Reference:** Transactions `244092`, `244103`, `244104`

**Statement:** Every accepted lifecycle transition creates an append-only history record containing the resulting status, timestamp, actor/source, and optional comment. Existing history records cannot be overwritten or deleted through the order API.

**Intent:** Compliance
**Classification:** Core
**Weight:** Critical

**Logic:**
```pseudocode
history = new OrderStatusHistory()
history.order = order
history.status = newStatus
history.dateAdded = currentDate()
history.comments = suppliedComment
history.actor = authenticatedActor
history.source = transitionSource

order.orderHistory.add(history)
orderRepository.save(order)
```

**Data Dependencies:**
- Reads: `ORDER_STATUS_HISTORY`
- Writes: `ORDER_STATUS_HISTORY.STATUS`, `DATE_ADDED`, `COMMENTS`, `ORDER_ID`, actor/source target fields

**Side Effects:**
- Publishes `OrderStatusChanged`.
- History is returned newest-first for administrative retrieval.

**Semantic Preservation:**

| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 2 | 3 | GAP |
| Data-flow | 6 | 7 | GAP — actor/source added |
| Constants | 0 | 0 | OK |
| State transitions | 1 | 1 | OK |
| Outcomes | 2 | 3 | GAP |
| Data writes | 5 | 6 | GAP |
| Integrations | 0 | 1 | GAP |
| Error paths | 1 | 2 | GAP |

**Preservation:** FLAGGED

**Concrete Example:**
- API Input: `POST /api/v1/orders/70009/history`
  ```json
  {"status":"PROCESSED","comments":"Captured by operator OP-17","source":"ADMIN"}
  ```
- Success: `201`
  ```json
  {"historyId":90001,"orderId":70009,"status":"PROCESSED","source":"ADMIN"}
  ```
- Error Input:
  ```json
  {"status":"ORDERED","comments":"Delete previous history"}
  ```
- Error Output: `409`
  ```json
  {"error":"HISTORY_IMMUTABLE","message":"Existing order history cannot be edited or deleted.","statusCode":409}
  ```

---

### BR-OR-PAY-002: Payment-status reconciliation

**Source Reference:** `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/controller/order/facade/OrderFacadeImpl.java` : `updateOrderStatus()` lines 1624-1647; `captureOrder()` lines 1415-1427  
**Cross-Reference:** `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/order/OrderServiceImpl.java` : `process()` lines 146-188  
**Discovery Method:** Direct Source Read + CAST Imaging (Hybrid)  
**CAST Reference:** Transactions `244095`–`244097`; data graph `243929`

**Statement:** MS-05 applies only authenticated payment outcomes from MS-06. A successful capture may move an eligible `ORDERED` order to `PROCESSED`; a payment failure does not advance the order; a duplicate or stale payment event must not reverse a later order state.

**Intent:** State Transition
**Classification:** Core
**Weight:** Critical

**Logic:**
```pseudocode
on PaymentCaptured(event):
    load order by tenantId, storeId, orderId
    if event.eventId already processed:
        return existing result

    if order.status == ORDERED:
        transition order to PROCESSED
        append history with source PAYMENT_CAPTURED
    else if order.status in {PROCESSED, DELIVERED}:
        record duplicate/no-op
    else:
        reject payment outcome as incompatible with terminal state

on PaymentFailed(event):
    record payment failure projection
    do not transition ORDERED to PROCESSED
```

The legacy code couples provider processing and order updates; target behavior separates ownership and makes event application replay-safe.

**Data Dependencies:**
- Reads: order state, payment outcome event, payment reference
- Writes: order state/history, payment-status projection, inbox record

**Side Effects:**
- Publishes `OrderStatusChanged` or `OrderPaymentFailed`.
- Does not write MS-06 transaction tables.

**Semantic Preservation:**

| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 4 | 7 | GAP |
| Data-flow | 8 | 8 | OK |
| Constants | 4 | 4 | OK |
| State transitions | 3 | 4 | GAP |
| Outcomes | 4 | 5 | GAP |
| Data writes | 5 | 6 | GAP |
| Integrations | 2 | 3 | GAP |
| Error paths | 2 | 5 | GAP |

**Preservation:** FLAGGED

**Concrete Example:**
- Event Input: `PaymentCaptured`
  ```json
  {"eventId":"evt-cap-81","orderId":70010,"paymentReference":"pay-81","amount":249.99,"currency":"USD"}
  ```
- Success: `200`
  ```json
  {"orderId":70010,"status":"PROCESSED","paymentStatus":"CAPTURED","eventApplied":true}
  ```
- Error Input:
  ```json
  {"eventId":"evt-cap-81","orderId":70010,"paymentReference":"pay-81","amount":249.99,"currency":"USD"}
  ```
  after the same event was already applied.
- Error Output: `200`
  ```json
  {"orderId":70010,"status":"PROCESSED","eventApplied":false,"duplicate":true}
  ```

---

### BR-OR-PAY-003: Next payment action

**Source Reference:** `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/controller/order/facade/OrderFacadeImpl.java` : `nextTransaction()` lines 1554-1585  
**Cross-Reference:** `initial-source/shopizer-3.2.7/sm-core-model/src/main/java/com/salesmanager/core/model/payments/TransactionType.java` : lines 3-6  
**Discovery Method:** Direct Source Read + CAST Imaging (Hybrid)  
**CAST Reference:** Transaction `244095`; object `30027`

**Statement:** The administrative payment-action projection returns `CAPTURE` after authorization, `REFUND` after capture or authorize-and-capture, and `OK` after refund or when no supported next action exists. The target derives this from timestamp-ordered payment outcomes supplied by MS-06, not from transaction-type ordering.

**Intent:** Routing
**Classification:** Core
**Weight:** Critical

**Logic:**
```pseudocode
last = paymentProjection.latestOutcomeByTimestamp(orderId)

if last.type == AUTHORIZE:
    nextAction = CAPTURE
else if last.type in {AUTHORIZECAPTURE, CAPTURE}:
    nextAction = REFUND
else if last.type == REFUND:
    nextAction = OK
else:
    nextAction = OK
```

The legacy `lastTransaction` behavior is identified by the brief as ordering by transaction type rather than timestamp; this is a migration risk and is not preserved.

**Data Dependencies:**
- Reads: payment outcome type, payment outcome timestamp, order identifier
- Writes: none; read projection only

**Side Effects:** None.

**Semantic Preservation:**

| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 5 | 5 | OK |
| Data-flow | 5 | 5 | OK |
| Constants | 5 | 5 | OK |
| State transitions | 0 | 0 | OK |
| Outcomes | 5 | 5 | OK |
| Data writes | 0 | 0 | OK |
| Integrations | 1 | 1 | OK |
| Error paths | 1 | 2 | GAP |

**Preservation:** FLAGGED (error paths)

**Concrete Example:**
- API Input: `GET /api/v1/orders/70011/payment/next-action`
- Success: `200`
  ```json
  {"orderId":70011,"nextAction":"REFUND","lastPaymentAction":"CAPTURE"}
  ```
- Error Input: order ID belongs to another store.
- Error Output: `404`
  ```json
  {"error":"ORDER_NOT_FOUND","message":"Order 70011 was not found in this store.","statusCode":404}
  ```

---

### BR-OR-PAY-004: Capturable-order discovery

**Source Reference:** `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/order/OrderServiceImpl.java` : `getCapturableOrders()` lines 600-676  
**Cross-Reference:** `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/controller/order/facade/OrderFacadeImpl.java` : `getCapturableOrderList()` lines 1382-1413  
**Discovery Method:** Direct Source Read + CAST Imaging (Hybrid)  
**CAST Reference:** Transaction `244097`; object `29392`

**Statement:** An order is capturable only when it has an authorization outcome within the requested date range and has no capture, authorize-and-capture, or refund outcome in that outcome set.

**Intent:** Routing
**Classification:** Core
**Weight:** High

**Logic:**
```pseudocode
transactions = paymentReadModel.list(startDate, endDate)
preAuthorizedOrders = {}
transactionsByOrder = {}

for trx in transactions:
    if trx.type == AUTHORIZE:
        preAuthorizedOrders[trx.orderId] = trx.order
    transactionsByOrder[trx.orderId].append(trx)

for orderId, outcomes in transactionsByOrder:
    capturable = true
    for outcome in outcomes:
        if outcome.type in {CAPTURE, AUTHORIZECAPTURE, REFUND}:
            capturable = false
    if capturable:
        result.add(preAuthorizedOrders[orderId])
```

**Data Dependencies:**
- Reads: payment outcomes, order ID, outcome type, outcome date
- Writes: none

**Side Effects:**
- Calls MS-06 read projection.
- Capture command is sent to MS-06 only after administrative confirmation.

**Semantic Preservation:**

| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 12 | 12 | OK |
| Data-flow | 9 | 9 | OK |
| Constants | 4 | 4 | OK |
| State transitions | 0 | 0 | OK |
| Outcomes | 3 | 3 | OK |
| Data writes | 0 | 0 | OK |
| Integrations | 1 | 1 | OK |
| Error paths | 1 | 2 | GAP |

**Preservation:** FLAGGED (error paths)

**Concrete Example:**
- API Input: `GET /api/v1/orders/capturable?startDate=2026-08-31&endDate=2026-09-01`
- Success: `200`
  ```json
  {"items":[{"orderId":70012,"status":"ORDERED","paymentStatus":"AUTHORIZED"}],"pagination":{"page":1,"pageSize":20,"totalItems":1,"totalPages":1}}
  ```
- Error Input: `GET /api/v1/orders/capturable?startDate=2026-09-02&endDate=2026-09-01`
- Error Output: `422`
  ```json
  {"error":"DATE_RANGE_INVALID","message":"startDate must not be after endDate.","statusCode":422}
  ```

---

### BR-OR-REF-001: Refund balance reconciliation

**Source Reference:** `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/payments/TransactionServiceImpl.java` : `getRefundableTransaction()` — CAST complexity evidence; `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/api/v1/order/OrderPaymentApi.java` : refund endpoint lines 335-343  
**Cross-Reference:** `assessment/ms-05-cast-brief.md` : lines 673-696  
**Discovery Method:** Direct Source Read + CAST Imaging (Hybrid)  
**CAST Reference:** Transactions `244099`, `244096`; data graph `243929`

**Statement:** A refund cannot exceed the captured amount less refunds already accepted. Partial refunds accumulate against a durable refund balance, and duplicate refund outcomes do not reduce the remaining balance twice. MS-06 owns provider execution; MS-05 owns the order-facing reconciliation.

**Intent:** Calculation
**Classification:** Core
**Weight:** Critical

**Logic:**
```pseudocode
captured = paymentProjection.totalCaptured(orderId)
refunded = orderRefundProjection.totalApplied(orderId)
remaining = captured - refunded

if requestedAmount <= 0 OR requestedAmount > remaining:
    reject REFUND_AMOUNT_INVALID

create pending refund application with idempotencyKey
on PaymentRefunded(event):
    if inbox contains event.eventId:
        return existing result
    if event.amount > currentRemaining:
        mark REFUND_RECONCILIATION_FAILED
    else:
        refundApplied += event.amount
        if refundApplied == captured:
            transition order to REFUNDED
```

The legacy refund endpoint is a stub and the brief reports uncertainty around partial-refund accumulation; this target rule is therefore a required replacement decision, not a claim that the legacy behavior is complete.

**Data Dependencies:**
- Reads: captured payment total, applied refund total, order total
- Writes: refund applications, payment-status projection, order status/history

**Side Effects:**
- Sends refund command to MS-06.
- Publishes `OrderRefundApplied` or `RefundReconciliationFailed`.

**Semantic Preservation:**

| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 2 | 8 | GAP |
| Data-flow | 5 | 8 | GAP |
| Constants | 0 | 1 | GAP |
| State transitions | 1 | 2 | GAP |
| Outcomes | 2 | 5 | GAP |
| Data writes | 2 | 6 | GAP |
| Integrations | 2 | 3 | GAP |
| Error paths | 2 | 5 | GAP |

**Preservation:** UNRESOLVED — legacy endpoint is incomplete and partial-refund semantics require BA confirmation.

**Concrete Example:**
- API Input: `POST /api/v1/orders/70013/refund`
  ```json
  {"amount":40.00,"currency":"USD","idempotencyKey":"refund-70013-1","reason":"Damaged item"}
  ```
- Success: `202`
  ```json
  {"orderId":70013,"refundId":"rfd-1","amount":40.00,"remainingRefundable":109.99,"status":"PROCESSED"}
  ```
- Error Input:
  ```json
  {"amount":200.00,"currency":"USD","idempotencyKey":"refund-70013-2"}
  ```
- Error Output: `422`
  ```json
  {"error":"REFUND_AMOUNT_INVALID","message":"Refund amount 200.00 exceeds remaining refundable balance 149.99.","statusCode":422}
  ```

---

### BR-OR-CAN-001: Cancellation orchestration

**Source Reference:** N/A — no legacy cancellation implementation was found; CAST negative finding is recorded in `assessment/ms-05-cast-brief.md` : lines 71-94 and 673-696  
**Discovery Method:** CAST Imaging boundary evidence; no legacy cancellation implementation found  
**CAST Reference:** No live cancellation transaction; `CANCELED` enum is present in `OrderStatus.java` lines 3-10

**Statement:** An order may be canceled only before a terminal state and only when the requested cancellation is compatible with payment and fulfillment state. Cancellation must release any MS-02 reservation, request payment void/refund through MS-06 when required, transition the order to `CANCELED`, append history, and publish a compensating event.

**Intent:** State Transition
**Classification:** Core
**Weight:** High

**Logic:**
```pseudocode
if order.status in {DELIVERED, REFUNDED, CANCELED}:
    reject ORDER_CANNOT_BE_CANCELED

if fulfillment.status in {SHIPPED, DELIVERED}:
    reject FULFILLMENT_ALREADY_STARTED

if paymentStatus == AUTHORIZED:
    send PaymentVoidRequested to MS-06
if paymentStatus == CAPTURED:
    send RefundRequested to MS-06

send InventoryReservationReleaseRequested to MS-02
transition order ORDERED or PROCESSED -> CANCELED
append history
publish OrderCanceled
```

**Data Dependencies:**
- Reads: order state, payment projection, fulfillment state, reservation reference
- Writes: order state/history, cancellation record, compensation records

**Side Effects:**
- MS-06 payment void/refund command.
- MS-02 reservation release command.
- `OrderCanceled` event.

**Semantic Preservation:**

| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 0 | 8 | GAP |
| Data-flow | 0 | 7 | GAP |
| Constants | 1 | 5 | GAP |
| State transitions | 1 | 3 | GAP |
| Outcomes | 1 | 5 | GAP |
| Data writes | 0 | 6 | GAP |
| Integrations | 0 | 4 | GAP |
| Error paths | 1 | 5 | GAP |

**Preservation:** UNRESOLVED — no legacy cancellation orchestration exists.

**Concrete Example:**
- API Input: `POST /api/v1/orders/70014/cancel`
  ```json
  {"reason":"Customer changed delivery address","idempotencyKey":"cancel-70014-1"}
  ```
- Success: `202`
  ```json
  {"orderId":70014,"status":"CANCELED","compensationState":"PENDING"}
  ```
- Error Input: same request after order status is `DELIVERED`.
- Error Output: `409`
  ```json
  {"error":"ORDER_CANNOT_BE_CANCELED","message":"A delivered order cannot be canceled.","statusCode":409}
  ```

---

### BR-OR-FUL-001: Fulfillment request and shipment status

**Source Reference:** N/A — no legacy fulfillment implementation was found; CAST negative finding is recorded in `assessment/ms-05-cast-brief.md` : lines 71-94 and 656-671  
**Discovery Method:** CAST negative finding plus target-boundary definition  
**CAST Reference:** No live fulfillment, shipment, delivery, or shipment-status transaction

**Statement:** After an order is payment-ready and contains physical items, MS-05 may publish a fulfillment request containing the immutable delivery snapshot and purchased lines. Shipment updates are accepted from MS-09/MS-12 and may move fulfillment to `REQUESTED`, `IN_PROGRESS`, `SHIPPED`, `DELIVERED`, or `CANCELED`; carrier execution remains outside MS-05.

**Intent:** State Transition
**Classification:** Core
**Weight:** High

**Logic:**
```pseudocode
if order.status not in {PROCESSED}:
    reject FULFILLMENT_ORDER_NOT_READY

if order has no physical lines:
    do not create physical fulfillment request

create fulfillmentOrder(status=REQUESTED)
publish FulfillmentRequested(orderId, lines, deliverySnapshot)

on ShipmentStatusUpdated:
    validate monotonic fulfillment transition
    persist fulfillment status/history
    if status == DELIVERED:
        transition order PROCESSED -> DELIVERED
```

**Data Dependencies:**
- Reads: order status, purchased lines, delivery snapshot, physical/digital classification
- Writes: `FULFILLMENT_ORDER`, `FULFILLMENT_STATUS_HISTORY`, optionally `ORDERS.ORDER_STATUS`

**Side Effects:**
- Publishes `FulfillmentRequested`.
- Consumes shipment updates from MS-09/MS-12.
- No direct carrier call.

**Semantic Preservation:**

| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 0 | 6 | GAP |
| Data-flow | 0 | 7 | GAP |
| Constants | 2 | 5 | GAP |
| State transitions | 1 | 7 | GAP |
| Outcomes | 1 | 5 | GAP |
| Data writes | 0 | 6 | GAP |
| Integrations | 0 | 4 | GAP |
| Error paths | 1 | 4 | GAP |

**Preservation:** UNRESOLVED — legacy fulfillment engine was not found.

**Concrete Example:**
- API Input: `POST /api/v1/orders/70015/fulfillment`
  ```json
  {"idempotencyKey":"fulfill-70015-1"}
  ```
- Success: `202`
  ```json
  {"orderId":70015,"fulfillmentId":"fo-70015","status":"REQUESTED"}
  ```
- Error Input: order status is `ORDERED`.
- Error Output: `409`
  ```json
  {"error":"FULFILLMENT_ORDER_NOT_READY","message":"Fulfillment requires an order in PROCESSED state.","statusCode":409}
  ```

---

### BR-OR-AUTH-001: Tenant and store isolation

**Source Reference:** `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/repositories/order/OrderRepository.java` : lines 10-20; `OrderRepositoryImpl.java` : `listOrders()` lines 152-239  
**Cross-Reference:** `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/api/v1/order/OrderApi.java` : authorization calls lines 245-249, 273-278  
**Discovery Method:** Direct Source Read + CAST Imaging (Hybrid)  
**CAST Reference:** Data graph `243908`

**Statement:** Every order query and mutation is scoped to both the authenticated tenant and store. An order identifier from another tenant or store is indistinguishable from not found to unauthorized callers.

**Intent:** Authorization
**Classification:** Core
**Weight:** Critical

**Logic:**
```pseudocode
authorize request tenantId
authorize request storeId
query orders where tenant_id = request.tenantId
  and store_id = request.storeId
  and order_id = requestedOrderId
if no row:
    return ORDER_NOT_FOUND
```

**Data Dependencies:**
- Reads: `ORDERS.MERCHANTID`, target tenant/store context
- Writes: all MS-05 tables carry tenant/store ownership through the aggregate

**Side Effects:** None.

**Semantic Preservation:**

| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 5 | 5 | OK |
| Data-flow | 4 | 5 | GAP |
| Constants | 4 | 4 | OK |
| State transitions | 0 | 0 | OK |
| Outcomes | 3 | 3 | OK |
| Data writes | 0 | 0 | OK |
| Integrations | 1 | 1 | OK |
| Error paths | 2 | 3 | GAP |

**Preservation:** FLAGGED (data-flow, error paths)

**Concrete Example:**
- API Input: `GET /api/v1/orders/70016` with `x-tenant-id: tenant-a`, `x-store-id: store-12`
- Success: `200`
  ```json
  {"orderId":70016,"storeId":12,"status":"ORDERED"}
  ```
- Error Input: same order ID with `x-store-id: store-99`.
- Error Output: `404`
  ```json
  {"error":"ORDER_NOT_FOUND","message":"Order 70016 was not found.","statusCode":404}
  ```

---

### BR-OR-AUTH-002: Customer order visibility

**Source Reference:** `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/api/v1/order/OrderApi.java` : authenticated list lines 165-207; authenticated detail lines 292-330  
**Cross-Reference:** `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/repositories/order/OrderRepositoryImpl.java` : customer filtering lines 70-74  
**Discovery Method:** Direct Source Read + CAST Imaging (Hybrid)  
**CAST Reference:** Transactions `244005`, `244088`

**Statement:** An authenticated customer may list and retrieve only orders associated with that customer and current store. Administrative order access is not implied by customer authentication.

**Intent:** Authorization
**Classification:** Core
**Weight:** High

**Logic:**
```pseudocode
principal = request.userPrincipal
customer = customerService.getByNick(principal.name)
if customer is null:
    return 401

list orders where customerId = customer.id and storeId = request.storeId

for detail:
    if order.customerId != customer.id:
        return 404
```

**Data Dependencies:**
- Reads: `ORDERS.CUSTOMER_ID`, `ORDERS.MERCHANTID`
- Writes: none

**Side Effects:** None.

**Semantic Preservation:**

| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 7 | 7 | OK |
| Data-flow | 5 | 5 | OK |
| Constants | 0 | 0 | OK |
| State transitions | 0 | 0 | OK |
| Outcomes | 4 | 4 | OK |
| Data writes | 0 | 0 | OK |
| Integrations | 1 | 1 | OK |
| Error paths | 3 | 3 | OK |

**Preservation:** OK

**Concrete Example:**
- API Input: `GET /api/v1/me/orders` as customer `481`
- Success: `200`
  ```json
  {"items":[{"orderId":70017,"customerId":481,"status":"PROCESSED"}],"pagination":{"page":1,"pageSize":20,"totalItems":1,"totalPages":1}}
  ```
- Error Input: `GET /api/v1/me/orders/70018` where order `70018` belongs to customer `482`.
- Error Output: `404`
  ```json
  {"error":"ORDER_NOT_FOUND","message":"Order 70018 was not found.","statusCode":404}
  ```

---

### BR-OR-ADM-001: Administrative order authorization

**Source Reference:** `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/api/v1/order/OrderApi.java` : lines 245-249, 273-278, 485-488, 506-508  
**Cross-Reference:** `initial-source/shopizer-admin-main/src/app/pages/orders/order-details/order-details.component.ts` : operations lines 196-255, 266-290  
**Discovery Method:** Direct Source Read + CAST Imaging (Hybrid)  
**CAST Reference:** Transactions `244006`, `244087`, `244092`

**Statement:** Administrative order listing, detail, status, history, customer-snapshot, capture, and refund operations require an authenticated principal in an allowed order-administration group and a matching tenant/store context.

**Intent:** Authorization
**Classification:** Core
**Weight:** High

**Logic:**
```pseudocode
allowedGroups = {SUPERADMIN, ADMIN, ADMIN_ORDER, ADMIN_RETAIL}
user = authorizationUtils.authenticatedUser()
authorizationUtils.authorizeUser(user, allowedGroups, merchantStore)
```

**Data Dependencies:**
- Reads: authenticated principal, group membership, store context
- Writes: only after authorization succeeds

**Side Effects:** None.

**Semantic Preservation:**

| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 3 | 3 | OK |
| Data-flow | 4 | 4 | OK |
| Constants | 4 | 4 | OK |
| State transitions | 0 | 0 | OK |
| Outcomes | 2 | 2 | OK |
| Data writes | 0 | 0 | OK |
| Integrations | 1 | 1 | OK |
| Error paths | 2 | 3 | GAP |

**Preservation:** FLAGGED (error paths)

**Concrete Example:**
- API Input: `PUT /api/v1/orders/70019/status`
  ```json
  {"status":"PROCESSED","reason":"Manual review complete"}
  ```
  with group `ADMIN_ORDER`.
- Success: `200`
  ```json
  {"orderId":70019,"status":"PROCESSED"}
  ```
- Error Input: same request from group `CUSTOMER`.
- Error Output: `403`
  ```json
  {"error":"FORBIDDEN","message":"Order administration permission is required.","statusCode":403}
  ```

---

### BR-OR-ADM-002: Customer snapshot correction

**Source Reference:** `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/controller/order/facade/OrderFacadeImpl.java` : `updateOrderCustomre()` lines 1486-1510; address conversion lines 1512-1552  
**Cross-Reference:** `initial-source/shopizer-admin-main/src/app/pages/orders/order-details/order-details.component.ts` : `updateOrder()` lines 218-255  
**Discovery Method:** Direct Source Read + CAST Imaging (Hybrid)  
**CAST Reference:** Transaction `244091`

**Statement:** An authorized administrator may correct the order's stored email, billing-address snapshot, and delivery-address snapshot without modifying the customer master record.

**Intent:** Compliance
**Classification:** Core
**Weight:** Critical

**Logic:**
```pseudocode
modelOrder = orderService.getOrder(orderId, store)
if modelOrder is null:
    raise ORDER_NOT_FOUND

modelOrder.customerEmailAddress = customer.emailAddress
modelOrder.billing = convertBilling(customer.billing)
modelOrder.delivery = convertDelivery(customer.delivery)
orderService.saveOrUpdate(modelOrder)
```

**Data Dependencies:**
- Reads: `ORDERS.CUSTOMER_EMAIL_ADDRESS`, embedded billing/delivery fields
- Writes: those order snapshot fields only

**Side Effects:**
- Does not call customer update.
- Publishes `OrderSnapshotCorrected`.

**Semantic Preservation:**

| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 5 | 5 | OK |
| Data-flow | 12 | 12 | OK |
| Constants | 0 | 0 | OK |
| State transitions | 0 | 0 | OK |
| Outcomes | 3 | 3 | OK |
| Data writes | 7 | 7 | OK |
| Integrations | 2 | 1 | GAP |
| Error paths | 2 | 3 | GAP |

**Preservation:** FLAGGED (integrations, error paths)

**Concrete Example:**
- API Input: `PATCH /api/v1/orders/70020/customer-snapshot`
  ```json
  {
    "emailAddress":"ana.updated@example.com",
    "billing":{"firstName":"Ana","lastName":"Silva","address":"20 Main St","city":"Austin","country":"US","postalCode":"78701","phone":"+15125550101"},
    "delivery":{"firstName":"Ana","lastName":"Silva","address":"20 Main St","city":"Austin","country":"US","postalCode":"78701","phone":"+15125550101"}
  }
  ```
- Success: `200`
  ```json
  {"orderId":70020,"emailAddress":"ana.updated@example.com","snapshotUpdated":true}
  ```
- Error Input:
  ```json
  {"emailAddress":"not-an-email","billing":{"city":"Austin"}}
  ```
- Error Output: `422`
  ```json
  {"error":"ADDRESS_INVALID","message":"Billing and delivery snapshots require country and postalCode.","statusCode":422}
  ```

---

### BR-OR-READ-001: Order read projection

**Source Reference:** `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/repositories/order/OrderRepository.java` : lines 10-20; `OrderRepositoryImpl.java` : lines 44-130 and 152-260  
**Cross-Reference:** `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/controller/order/facade/OrderFacadeImpl.java` : `getReadableOrder()` lines 1106-1148; `ReadableOrderPopulator.java` lines 49-194  
**Discovery Method:** Direct Source Read + CAST Imaging (Hybrid)  
**CAST Reference:** Transactions `244005`, `244006`, `244087`, `244088`; data graph `243908`

**Statement:** Order lists use store-scoped filters and pagination, while order detail returns the accepted order snapshot, purchased lines, totals, attributes, history, and download entitlements. List reads must use projections rather than loading every child collection into memory.

**Intent:** Routing
**Classification:** Core
**Weight:** High

**Logic:**
```pseudocode
criteria.storeId = request.storeId
criteria.page = request.page
criteria.pageSize = request.pageSize
apply optional customerName, email, id, phone, status filters
order by orderId DESC unless ascending explicitly requested
count matching orders
fetch projected order rows
detail fetches:
    billing, delivery, lines, line attributes, line prices,
    totals, history, downloads
```

**Data Dependencies:**
- Reads: all MS-05 order tables and embedded snapshots
- Writes: none

**Side Effects:** None.

**Semantic Preservation:**

| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 21 | 20 | GAP |
| Data-flow | 25 | 25 | OK |
| Constants | 4 | 4 | OK |
| State transitions | 0 | 0 | OK |
| Outcomes | 6 | 6 | OK |
| Data writes | 0 | 0 | OK |
| Integrations | 3 | 1 | GAP |
| Error paths | 3 | 4 | GAP |

**Preservation:** FLAGGED (control-flow, integrations, error paths)

**Concrete Example:**
- API Input: `GET /api/v1/orders?page=1&pageSize=20&status=PROCESSED&email=ana@example.com`
- Success: `200`
  ```json
  {"items":[{"orderId":70021,"status":"PROCESSED","emailAddress":"ana@example.com","total":129.50}],"pagination":{"page":1,"pageSize":20,"totalItems":1,"totalPages":1}}
  ```
- Error Input: `GET /api/v1/orders?page=0&pageSize=5000`
- Error Output: `422`
  ```json
  {"error":"PAGINATION_INVALID","message":"page must be at least 1 and pageSize must be between 1 and 100.","statusCode":422}
  ```

---

### BR-OR-RES-001: Idempotent submission and event application

**Source Reference:** N/A — target idempotency and event application capability; no equivalent legacy implementation was found. CAST evidence is recorded in `assessment/ms-05-cast-brief.md` : lines 548-587  
**Discovery Method:** Target architecture requirement derived from CAST boundary findings  
**CAST Reference:** Transactions `244089`, `244090`; no legacy idempotency mechanism found

**Statement:** Repeating the same order submission or payment/fulfillment event must produce the original result without creating a second order, second entitlement, second history transition, or duplicate compensation.

**Intent:** Compliance
**Classification:** Core
**Weight:** Critical

**Logic:**
```pseudocode
require idempotencyKey for every mutating command
if inbox/outbox contains same (tenantId, idempotencyKey):
    return stored result

begin local transaction
create inbox record
perform command exactly once
write resulting state and outbox messages
store response
commit
```

**Data Dependencies:**
- Reads: `ORDER_INBOX.IDEMPOTENCY_KEY`, submission identifier, event identifier
- Writes: inbox, outbox, unique order submission key, resulting aggregate

**Side Effects:**
- Transactional outbox publication.
- Replay-safe consumers.

**Semantic Preservation:**

| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 0 | 5 | GAP |
| Data-flow | 0 | 5 | GAP |
| Constants | 0 | 2 | GAP |
| State transitions | 0 | 2 | GAP |
| Outcomes | 0 | 3 | GAP |
| Data writes | 0 | 5 | GAP |
| Integrations | 0 | 3 | GAP |
| Error paths | 0 | 3 | GAP |

**Preservation:** UNRESOLVED — no equivalent legacy mechanism was found.

**Concrete Example:**
- API Input: two identical requests to `POST /api/v1/orders/70022/refund` with `Idempotency-Key: refund-70022-1`.
- Success: first request `202`, second request `200`
  ```json
  {"refundId":"rfd-22","amount":25.00,"duplicate":true}
  ```
- Error Input: reuse the same key with amount `30.00`.
- Error Output: `409`
  ```json
  {"error":"IDEMPOTENCY_KEY_REUSED","message":"The idempotency key was already used with a different request.","statusCode":409}
  ```

---

### BR-OR-INV-001: Invoice generation boundary

**Source Reference:** `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/modules/order/InvoiceModule.java` : lines 10-12; `ODSInvoiceModule.java` : lines 21-50  
**Cross-Reference:** `initial-source/shopizer-3.2.7/sm-core/src/main/resources/spring/shopizer-core-modules.xml` : lines 103-106  
**Discovery Method:** Direct Source Read + CAST Imaging (Hybrid)  
**CAST Reference:** No invoice table in data graph `243908`

**Statement:** An invoice request uses the immutable order snapshot. MS-05 does not claim a durable invoice artifact or invoice table exists in the legacy implementation; it publishes an invoice-generation request to MS-12 and exposes the resulting artifact only after MS-12 confirms availability.

**Intent:** Routing
**Classification:** Active
**Weight:** Medium

**Logic:**
```pseudocode
validate order exists and belongs to tenant/store
require order.products and order.totals
publish InvoiceGenerationRequested {
    orderId,
    orderDate,
    billingSnapshot,
    lines,
    acceptedTotals,
    currency
}
if invoice artifact is available:
    return artifact reference
else:
    return PROCESSING
```

The active `ODSInvoiceModule.createInvoice()` implementation throws `"Not implemented"`; the larger spreadsheet/PDF implementation is commented out.

**Data Dependencies:**
- Reads: order date, billing snapshot, lines, prices, totals, currency
- Writes: invoice request/outbox only; no MS-05 invoice table asserted

**Side Effects:**
- Publishes `InvoiceGenerationRequested`.
- MS-12 owns template rendering, file storage, and delivery.

**Semantic Preservation:**

| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 12 | 6 | GAP |
| Data-flow | 22 | 9 | GAP |
| Constants | 10 | 2 | GAP |
| State transitions | 0 | 1 | GAP |
| Outcomes | 2 | 3 | GAP |
| Data writes | 0 | 2 | GAP |
| Integrations | 1 | 2 | GAP |
| Error paths | 4 | 3 | GAP |

**Preservation:** UNRESOLVED — legacy invoice generation is incomplete.

**Concrete Example:**
- API Input: `GET /api/v1/orders/70023/invoice`
- Success: `202`
  ```json
  {"orderId":70023,"status":"PROCESSING","requestId":"inv-70023"}
  ```
- Error Input: order has no accepted totals.
- Error Output: `422`
  ```json
  {"error":"INVOICE_SNAPSHOT_INCOMPLETE","message":"An invoice requires accepted order totals.","statusCode":422}
  ```

---

### BR-OR-UI-001: Administrative order-detail coverage

**Source Reference:** `initial-source/shopizer-admin-main/src/app/pages/orders/order-details/order-details.component.ts` : lines 80-138, 196-255, 266-312; `orders.service.ts` : lines 18-56  
**Cross-Reference:** `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/api/v1/order/OrderApi.java` : lines 263-280, 473-519; `OrderPaymentApi.java` : lines 183-221, 292-360; `OrderStatusHistoryApi.java` : lines 45-73  
**Discovery Method:** Direct Source Read + CAST Imaging (Hybrid)  
**CAST Reference:** Transactions `244087`, `244092`, `244095`, `244096`, `244098`, `244099`

**Statement:** The administration order-detail experience requires order detail, lifecycle history, payment transactions, next payment action, customer/address correction, capture, refund, and status-update operations. Capture, refund, and authorization are target capabilities even though their legacy endpoint wrappers are stub-like.

**Intent:** Routing
**Classification:** Core
**Weight:** High

**Logic:**
```pseudocode
order detail page loads:
    GET order
    GET history
    GET payment transactions
    GET next payment action

administrator may:
    PATCH customer snapshot
    POST history
    POST capture
    POST refund
    PUT status

capture/refund/authorize wrappers in legacy return null;
target commands must return an explicit accepted, completed, or failed result.
```

**Data Dependencies:**
- Reads: order aggregate, history, payment projection
- Writes: order snapshot, history, lifecycle state, command/outbox records

**Side Effects:**
- Calls MS-06 for capture/refund commands.
- Publishes lifecycle and compensation events.

**Semantic Preservation:**

| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 10 | 9 | GAP |
| Data-flow | 12 | 12 | OK |
| Constants | 5 | 5 | OK |
| State transitions | 3 | 4 | GAP |
| Outcomes | 8 | 8 | OK |
| Data writes | 4 | 6 | GAP |
| Integrations | 5 | 6 | GAP |
| Error paths | 4 | 7 | GAP |

**Preservation:** FLAGGED; stubbed payment operations require replacement.

**Concrete Example:**
- API Input: `GET /api/v1/orders/70024/payment-transactions`
- Success: `200`
  ```json
  {"items":[{"transactionId":"tx-24","action":"CAPTURE","status":"SUCCEEDED","amount":129.50,"occurredAt":"2026-09-01T10:00:00Z"}]}
  ```
- Error Input: `POST /api/v1/orders/70024/capture`
  ```json
  {"amount":129.50,"currency":"USD"}
  ```
  when no authorization exists.
- Error Output: `409`
  ```json
  {"error":"PAYMENT_ACTION_NOT_ALLOWED","message":"The order has no capturable authorization.","statusCode":409}
  ```

## Events Published

| Event | Trigger | Consumers |
|---|---|---|
| `OrderAccepted` | Order snapshot accepted | MS-04, MS-12, analytics |
| `OrderStatusChanged` | Legal lifecycle transition | MS-09/MS-12, MS-12 notifications |
| `DownloadEntitlementGranted` | Digital line snapshot contains a file | MS-12 |
| `OrderCanceled` | Cancellation transition completes | MS-02, MS-06, MS-12 |
| `OrderRefundApplied` | Refund outcome reconciled | MS-06, MS-12 |
| `FulfillmentRequested` | Physical order becomes fulfillment-ready | MS-09/MS-12 |
| `InvoiceGenerationRequested` | Invoice is requested | MS-12 |
| `OrderCompensationRequired` | Submission or downstream step fails | MS-02, MS-06, MS-12 |

All published events use an outbox, include `eventId`, `eventType`, `eventVersion`, `tenantId`, `storeId`, `orderId`, and `occurredAt`, and are delivered at least once.

## Events Consumed

| Event | Source | Action |
|---|---|---|
| `OrderSubmitted` | MS-04 | Create the immutable order aggregate |
| `PaymentAuthorized` | MS-06 | Record payment projection; permit capture |
| `PaymentCaptured` | MS-06 | Reconcile payment and potentially move order to `PROCESSED` |
| `PaymentFailed` | MS-06 | Record failure without advancing order |
| `PaymentRefunded` | MS-06 | Apply cumulative refund and potentially move order to `REFUNDED` |
| `PaymentVoided` | MS-06 | Complete cancellation compensation |
| `ShipmentStatusUpdated` | MS-09/MS-12 | Update fulfillment and potentially order delivery state |
| `InventoryReservationReleased` | MS-02 | Complete cancellation/compensation tracking |

Consumers use `eventId` and an inbox uniqueness constraint for duplicate suppression.

## Known Legacy Defects and Obsolete Behaviors

1. Administrative status update accepts any declared enum value without a legal transition matrix.
2. Capture, refund, and authorize endpoint wrappers return `null`.
3. Legacy order processing can persist some state before a later failure.
4. Legacy inventory mismatch handling logs an error in one path instead of rejecting consistently.
5. Legacy payment transaction ordering is by transaction type rather than timestamp.
6. Invoice generation's active implementation is not implemented.
7. No order-specific event publisher or consumer was found.
8. No cancellation or fulfillment orchestration transaction was found.
9. The legacy customer-facing download route was not found; entitlement creation exists, access delivery is incomplete.

## Placement Candidates

All candidates default to application tier:

| Candidate | Legacy evidence | Volume/set signal | Frequency | App-tier risk |
|---|---|---|---|---|
| Order list/detail projection | `OrderRepositoryImpl.java` lines 44-260; CAST detail graph 1,585 objects | List queries span multiple child collections | Interactive | Loading complete aggregates causes row multiplication and memory pressure |
| Total snapshot persistence | `OrderServiceImpl.java` lines 217-394 | Per-order, row-oriented | Checkout | Recalculation in MS-05 duplicates other service ownership |
| Status/history transition | `OrderFacadeImpl.java` lines 1624-1647 | One aggregate plus history append | Interactive/event-driven | Concurrent transitions can lose history without local transaction |
| Capturable discovery | `OrderServiceImpl.java` lines 600-676 | Date-range scan of payment outcomes | Interactive/admin | In-memory scan becomes latency cliff at high volume |
| Download entitlement | `OrderProductPopulator.java` lines 78-87 | Per purchased line | Acceptance | Duplicate events can create duplicate entitlements |
| Refund reconciliation | CAST transactions `244096`, `244099`; brief lines 673-696 | Per refund event | Interactive/event-driven | Replayed events corrupt remaining balance |
| Fulfillment orchestration | No legacy implementation; brief lines 656-671 | Per physical order | Event-driven | Synchronous carrier calls block order lifecycle |
| Event publication | Brief lines 700-714 | One outbox row per state-changing transaction | Event-driven | Direct publish without local transaction loses events |

## Extraction Status

- Rules extracted: **23**
- Preservation tables: **23**
- Source-specific core rules: **20**
- Target-only or gap-derived rules: **3**
- Legacy names retained only in source references and Logic sections.
