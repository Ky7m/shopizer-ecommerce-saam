# MS-06 Payments — CAST Scout Brief

**Engagement:** Shopizer 3.2.7  
**Service:** MS-06 Payments  
**Phase:** Phase 4 CAST Scout  
**Analysis timestamp:** 2026-09-01T16:50:24+04:00  
**CAST application:** `Shopizer-Backend`  
**CAST delivery:** `Onboarding-202511171247`  
**Analysis mode:** Hybrid — CAST structural evidence plus targeted source inspection  
**Local source root:** `initial-source/shopizer-3.2.7/`  
**Target service port:** `8106`  
**Target schema:** `payments`

> This is a CAST discovery brief only. It does not define an MS-06 service specification, OpenAPI contract, implementation, or test suite.

---

## 1. Scope and Ownership

MS-06 owns payment intent and transaction state, payment-provider dispatch, authorization, capture, refund, provider callbacks, provider references, and payment-related audit history.

The service must preserve payment semantics without preserving the legacy monolith's cross-service writes.

### MS-06 owns

- Payment intent lifecycle.
- Authorization and authorization-plus-capture processing.
- Capture of previously authorized payments.
- Full and partial refunds.
- Provider transaction and correlation identifiers.
- Provider callback/webhook records.
- Provider failure and decline outcomes.
- Payment transaction history.
- Payment-operation idempotency.
- Payment-provider adapter execution.
- Payment-specific audit records.

### MS-06 does not own

- Order lifecycle or order status transitions — MS-05.
- Order totals and order-total rows — MS-05.
- Cart or checkout state — MS-04.
- Customer identity or addresses — MS-01.
- Merchant/store lifecycle — MS-10.
- Module and merchant configuration ownership — MS-11.
- Product, inventory, pricing, tax, or shipping facts — MS-02, MS-07, MS-08, and MS-09 respectively.

The legacy code directly changes order status and order totals from payment methods. That behavior is a boundary violation to be replaced by authenticated payment events consumed by MS-05.

---

## 2. CAST Evidence Status and Limitations

The repository contains recorded CAST results for the same `Shopizer-Backend` delivery, including the payment-initiation and order-payment transaction records. The current tool surface did not expose a live CAST Imaging MCP client for issuing a new transaction query during this session. Consequently:

- Existing CAST transaction IDs and graph sizes below are taken from the recorded CAST brief `assessment/ms-04-cast-brief.md`.
- Payment source-component CAST IDs and `srcControlFlow` values were confirmed from the local SAAM graph's CAST-provenance records.
- No payment-module transaction IDs or payment-specific merchant/module data-graph IDs were present in the preserved assessment artifacts.
- Missing IDs are explicitly marked rather than inferred.

### CAST application inventory

| Metric | Value |
|---|---:|
| Application | `Shopizer-Backend` |
| CAST LOC | 94,528 |
| CAST elements | 16,269 |
| CAST interactions | 72,033 |
| Recorded backend delivery | `Onboarding-202511171247` |
| CAST root mapping | `§{main_sources}§` → `initial-source/` |
| Local resolved root | `initial-source/shopizer-3.2.7/` |

### Required live query set

The following transaction filters are required for the next live CAST refresh:

```text
name:contains:payment
name:contains:authorize
name:contains:capture
name:contains:refund
name:contains:transaction
name:contains:checkout-payment
name:contains:payment-module
```

The refresh must retrieve:

1. Reduced and full transaction records.
2. Full call-graph nodes and links for checkout payment, payment initialization, capture, refund, and authorization.
3. Payment, order, merchant, and module data graphs.
4. Complexity-ranked objects ordered by `srcControlFlow`.
5. Inward and outward object details for `PaymentServiceImpl`, `TransactionServiceImpl`, and each provider adapter.
6. Unreachable/dead-code candidates.
7. CAST source paths translated through the `§{main_sources}§` mapping.

---

## 3. Transaction Inventory

### 3.1 Checkout and payment-initialization transactions

| CAST ID | Method | Endpoint | Full graph | Link count | MS-06 disposition |
|---:|---|---|---:|---:|---|
| `244089` | POST | `/api/v1/auth/cart/{code}/checkout/` | 3,245 | 8,112 | Critical cross-service payment flow |
| `244090` | POST | `/api/v1/cart/{code}/checkout/` | 3,262 | 8,173 | Critical cross-service payment flow |
| `244094` | POST | `/api/v1/auth/cart/{code}/payment/init/` | 643 | 1,335 | Critical MS-06 payment-initiation boundary |
| `244093` | POST | `/api/v1/cart/{code}/payment/init/` | 616 | 1,288 | Critical MS-06 payment-initiation boundary |

The checkout graphs include cart, product, customer, tax, module configuration, merchant configuration, order, payment, storage, and notification components. MS-06 must extract only the payment portion and replace direct monolith orchestration with service events.

### 3.2 Administrative payment transactions

| CAST ID | Method | Endpoint | Full graph | Link count | Disposition |
|---:|---|---|---:|---:|---|
| `244097` | GET | `/api/v1/private/orders/payment/capturable/` | 580 | Not preserved | MS-06/MS-05 boundary; deep read |
| `244098` | POST | `/api/v1/private/orders/{id}/capture/` | 9 | Not preserved | Endpoint is currently a stub; target capability remains required |
| `244099` | POST | `/api/v1/private/orders/{id}/refund/` | 9 | Not preserved | Endpoint is currently a stub; target capability remains required |
| `244100` | POST | `/api/v1/private/orders/{id}/authorize/` | 9 | Not preserved | Endpoint is currently a stub; target capability remains required |
| `244095` | GET | `/api/v1/private/orders/{id}/payment/nextTransaction/` | 333 | Not preserved | Transaction-state inspection; deep read |
| `244096` | GET | `/api/v1/private/orders/{id}/payment/transactions/` | 407 | Not preserved | Transaction-history read; deep read |

The nine-object capture, refund, and authorize graphs reflect the controller methods returning `null`; they do not represent the complete intended payment behavior. The implemented service and provider paths must be read separately.

### 3.3 Payment-module configuration transactions

The source contains the following payment-module endpoints:

| CAST ID | Method | Endpoint | Source evidence | Disposition |
|---:|---|---|---|---|
| Not preserved | GET | `/api/v1/private/modules/payment` | `PaymentApi.paymentModules` | Configuration boundary; live CAST ID required |
| Not preserved | POST | `/api/v1/private/modules/payment` | `PaymentApi.configure` | Configuration write; live CAST ID required |
| Not preserved | GET | `/api/v1/private/modules/payment/{code}` | `PaymentApi.paymentModule` | Configuration read; live CAST ID required |

No transaction IDs for these endpoints were preserved in the available artifacts. They must be queried live before the Phase 4 extraction is considered CAST-complete.

### 3.4 Transaction keyword coverage

| Requested concept | Recorded evidence | Status |
|---|---|---|
| Payment | Payment initialization, checkout, `PaymentApi`, `PaymentServiceImpl`, provider adapters | Covered structurally |
| Authorize | Transaction `244100`; checkout authorization path; provider `authorize` methods | Covered, but administrative endpoint is stubbed |
| Capture | Transaction `244098`; `processCapturePayment`; provider `capture` methods | Covered, but administrative endpoint is stubbed |
| Refund | Transaction `244099`; `processRefund`; provider `refund` methods | Covered, but administrative endpoint is stubbed |
| Transaction | Transactions `244095`, `244096`, `244097`; `SM_TRANSACTION` data graph | Covered |
| Checkout-payment | Checkout transactions `244089`, `244090`; payment-init transactions `244093`, `244094` | Covered as checkout/payment paths |
| Payment-module | Source endpoints in `PaymentApi`; module map in Spring configuration | Live transaction IDs still required |

---

## 4. Full Call-Graph Findings

### 4.1 Authenticated payment initialization — CAST `244094`

```text
POST /api/v1/auth/cart/{code}/payment/init
  -> OrderPaymentApi.init
  -> request principal lookup
  -> CustomerService.getByNick
  -> ShoppingCartService.getByCode
  -> authenticated customer/cart ownership check
  -> PersistablePaymentPopulator.populate
  -> PricingService.getAmount
  -> PaymentServiceImpl.initTransaction
  -> getPaymentModulesConfigured
  -> MerchantConfigurationService
  -> decrypt PAYMENT_MODULES configuration
  -> ConfigurationModulesLoader.loadIntegrationConfigurations
  -> configured PaymentModule lookup
  -> PaymentService.getPaymentMethodByCode
  -> provider.initTransaction
  -> TransactionServiceImpl.save/create
  -> ReadableTransactionPopulator.populate
  -> response
```

Important behaviors:

- The authenticated path checks the principal's customer identity and cart ownership.
- The payment request is converted from strings to `PaymentType`, `TransactionType`, and `BigDecimal`.
- Provider configuration is decrypted before module selection.
- The returned transaction may contain a provider client token, payment intent ID, or PayPal token.
- The legacy implementation does not expose an explicit idempotency key.
- Provider initialization may create remote state before an order exists.

### 4.2 Anonymous payment initialization — CAST `244093`

```text
POST /api/v1/cart/{code}/payment/init
  -> OrderPaymentApi.init
  -> ShoppingCartService.getByCode
  -> PersistablePaymentPopulator.populate
  -> PaymentServiceImpl.initTransaction(null, payment, merchantStore)
  -> getPaymentModulesConfigured
  -> decrypt merchant payment configuration
  -> provider lookup
  -> provider.initTransaction
  -> TransactionServiceImpl.save
  -> ReadableTransactionPopulator.populate
  -> response
```

Important risks:

- The anonymous endpoint checks the cart code and merchant context but does not perform an authenticated customer ownership check.
- The target service must require tenant/store context and bind the payment intent to the checkout session or order submission context.
- A payment intent created anonymously must not be reusable across stores, customers, or checkout sessions.

### 4.3 Authenticated and anonymous checkout — CAST `244089` and `244090`

```text
POST /api/v1/{auth?}/cart/{code}/checkout
  -> OrderApi.checkout
  -> customer resolution or anonymous customer population
  -> ShoppingCartService.getByCode / getById
  -> OrderFacadeImpl.processOrder
  -> cart reload and product re-resolution
  -> PersistableOrderApiPopulator
  -> OrderProductPopulator for each cart line
  -> shipping quote lookup
  -> OrderService.caculateOrderTotal
  -> tax, shipping, promotion, and total processors
  -> submitted amount comparison
  -> PaymentServiceImpl.processPayment
  -> configured provider authorize or authorizeAndCapture
  -> TransactionServiceImpl.create
  -> OrderService order persistence
  -> order status/history mutation
  -> inventory decrement
  -> cart completion
  -> notification/download email
```

Legacy coupling that MS-06 must not reproduce:

- Payment is called directly from checkout/order processing.
- Payment transaction persistence and order persistence are not enclosed in one atomic boundary.
- The legacy payment method can directly promote an order to `PROCESSED`.
- Provider calls can occur before all order-side writes complete.
- Retry of the checkout request can repeat the provider operation.
- The submitted amount is calculated outside the payment service and is not cryptographically or transactionally bound to a payment intent.

Target interaction:

```text
MS-04 -> OrderSubmitted
MS-05 -> PaymentRequested
MS-06 -> provider authorization/capture
MS-06 -> PaymentAuthorized / PaymentCaptured / PaymentFailed
MS-05 -> order lifecycle transition
```

### 4.4 Capturable-order listing — CAST `244097`

```text
GET /api/v1/private/orders/payment/capturable
  -> OrderPaymentApi.listCapturableOrders
  -> default date range if omitted
  -> OrderFacade.getCapturableOrderList
  -> order/transaction retrieval
  -> transaction filtering
  -> readable order list
```

Default date behavior:

- Missing `startDate`: current date minus one day.
- Missing `endDate`: current timestamp.
- Explicit dates are converted at the system default time zone.
- The endpoint is marked `202 Accepted` in the legacy controller.

The target should make the time zone, inclusivity, provider status, and pagination semantics explicit. A date sweep must not be treated as a reliable payment reconciliation mechanism without provider-state verification.

### 4.5 Intended capture path — CAST `244098` plus source call graph

The endpoint currently returns `null`, but the intended source path is:

```text
POST /api/v1/private/orders/{id}/capture
  -> intended OrderFacade.captureOrder
  -> PaymentServiceImpl.processCapturePayment
  -> configured merchant payment module lookup
  -> TransactionServiceImpl.getCapturableTransaction
  -> provider.capture
  -> TransactionServiceImpl.create
  -> OrderService.addOrderStatusHistory(PROCESSED)
  -> Order.status = PROCESSED
  -> OrderService.saveOrUpdate
  -> ReadableTransaction response
```

Capture guard:

- An authorization transaction must be present.
- A later capture or refund stops the legacy scan.
- Provider authorization identifiers are read from `Transaction.details`.

Target correction:

- Capture must be authorized against the payment intent and order/payment aggregate, not only an arbitrary order ID.
- Capture must be idempotent.
- Capture amount must be explicit and provider-verified.
- Capture must not directly change MS-05 order status; it publishes an authenticated event.

### 4.6 Intended refund path — CAST `244099` plus source call graph

The endpoint currently returns `null`, but the intended service path is:

```text
POST /api/v1/private/orders/{id}/refund
  -> intended order/payment facade
  -> PaymentServiceImpl.processRefund
  -> validate order, customer, store, amount, and order total
  -> configured provider lookup
  -> TransactionServiceImpl.getRefundableTransaction
  -> provider.refund
  -> TransactionServiceImpl.create
  -> append REFUND order-total line
  -> reduce order total
  -> set order status REFUNDED
  -> append order status history
  -> OrderService.saveOrUpdate
  -> ReadableTransaction response
```

Legacy refund behavior is unsafe for direct preservation:

- It compares `doubleValue()` rather than using exact monetary arithmetic.
- It rejects only a refund greater than the current order total, not necessarily the cumulative refunded amount.
- It mutates MS-05 order totals and order state.
- The Stripe implementations appear to contain transaction-type and amount inconsistencies.
- Provider refund identifiers and original transaction identifiers are not consistently preserved.

### 4.7 Transaction history and next-transaction paths

```text
GET /api/v1/private/orders/{id}/payment/transactions
  -> authorization check
  -> OrderFacade.listTransactions
  -> Order lookup
  -> TransactionServiceImpl.listTransactions(order)
  -> TransactionRepository.findByOrder
  -> deserialize DETAILS JSON
  -> ReadableTransactionPopulator
```

```text
GET /api/v1/private/orders/{id}/payment/nextTransaction
  -> authorization check
  -> OrderFacade.nextTransaction
  -> TransactionServiceImpl.lastTransaction
  -> TransactionRepository.findByOrder
  -> transaction-type map
  -> selected transaction
  -> JSON response
```

`lastTransaction` does not order by transaction timestamp. It groups transactions by transaction-type name and takes the last lexicographic key from a `TreeMap`. This is not a valid payment-state transition algorithm.

---

## 5. Payment and Order Data Graphs

### 5.1 Recorded data graphs

| CAST graph ID | Root | Graph size | MS-06 relevance |
|---:|---|---:|---|
| `243929` | `sm_transaction` | 22 | Primary legacy payment transaction persistence |
| `243908` | `orders` | 62 | Order context, totals, history, and transaction relationship |
| `243909` | `order_product` | 10 | Order context used by payment/checkout processing |

### 5.2 Data graphs visible through checkout/payment paths

The recorded checkout graphs expose:

```text
sm_transaction
orders
order_product
order_product_attribute
order_product_price
order_product_download
order_status_history
order_total
merchant_store
merchant_configuration
module_configuration
customer
shopping_cart
product
product_availability
product_price
tax_class
tax_rate
shipping_quote
```

The following payment-specific data-graph queries must be rerun live and their graph IDs recorded:

```text
name:contains:sm_transaction
name:contains:transaction
name:contains:merchant_configuration
name:contains:module_configuration
name:contains:payment
```

### 5.3 Legacy `SM_TRANSACTION` persistence

Source mapping:

| Column | Source mapping |
|---|---|
| `TRANSACTION_ID` | `Transaction.id` |
| `ORDER_ID` | `Transaction.order` foreign-key relationship |
| `AMOUNT` | `Transaction.amount` |
| `TRANSACTION_DATE` | `Transaction.transactionDate` |
| `TRANSACTION_TYPE` | `Transaction.transactionType` |
| `PAYMENT_TYPE` | `Transaction.paymentType` |
| `DETAILS` | Serialized `transactionDetails` map |
| Audit columns | Embedded `AuditSection` and inherited audit mapping |

`TransactionRepository` provides:

- `findByOrder(orderId)`.
- `findByDates(startDate, endDate)` with an order fetch graph including order attributes, products, totals, and history.

The date query's broad joins may multiply rows and should not be used as an unexamined high-volume reconciliation design.

### 5.4 Target persistence candidates

These are modeling candidates for later Phase 4 domain work, not generated schema:

- `payment_intent`
- `payment_transaction`
- `payment_operation`
- `payment_refund`
- `payment_provider_reference`
- `payment_callback`
- `payment_idempotency_record`
- `payment_provider_attempt`
- `payment_outbox`
- `payment_inbox`
- `provider_credential_reference`

A target payment transaction must preserve the provider's opaque identifiers and normalized status without storing PAN, CVV, or raw credential material.

---

## 6. Complexity-Ranked Objects

### 6.1 CAST source-component ranking

The local CAST-provenance records provide the following `srcControlFlow` values. These are component-level control-flow vectors, not a substitute for a fresh live method-level complexity query.

| Rank | CAST ID | Object | `srcControlFlow` | Local path | Disposition |
|---:|---:|---|---:|---|---|
| 1 | `12989` | `PaymentServiceImpl` | 102 | `sm-core/.../services/payments/PaymentServiceImpl.java` | Mandatory deep read |
| 2 | `11652` | `BeanStreamPayment` | 61 | `sm-core/.../payment/impl/BeanStreamPayment.java` | Mandatory deep read |
| 3 | `11710` | `Stripe3Payment` | 47 | `sm-core/.../payment/impl/Stripe3Payment.java` | Mandatory deep read |
| 4 | `11688` | `PayPalExpressCheckoutPayment` | 44 | `sm-core/.../payment/impl/PayPalExpressCheckoutPayment.java` | Mandatory deep read |
| 5 | `11679` | `StripePayment` | 43 | `sm-core/.../payment/impl/StripePayment.java` | Mandatory deep read |
| 6 | `11668` | `BraintreePayment` | 39 | `sm-core/.../payment/impl/BraintreePayment.java` | Mandatory deep read |
| 7 | `29357` | `OrderPaymentApi` | 30 | `sm-shop/.../api/v1/order/OrderPaymentApi.java` | Boundary and dead-path read |
| 8 | `13115` | `TransactionServiceImpl` | 24 | `sm-core/.../services/payments/TransactionServiceImpl.java` | Mandatory deep read |
| 9 | `11699` | `PayPalRestPayment` | 15 | `sm-core/.../payment/impl/PayPalRestPayment.java` | Reachability/dead-code review |
| 10 | `30425` | `PaymentApi` | 11 | `sm-shop/.../api/v1/payment/PaymentApi.java` | Configuration boundary read |
| 11 | `17360` | `Transaction` | 4 | `sm-core-model/.../payments/Transaction.java` | Persistence/model read |
| 12 | `11678` | `MoneyOrderPayment` | 3 | `sm-core/.../payment/impl/MoneyOrderPayment.java` | Read despite low complexity |
| 13 | `19828` | `PersistableTransactionPopulator` | 7 | `sm-shop/.../transaction/PersistableTransactionPopulator.java` | DTO mapping read |
| 14 | `19820` | `PersistablePaymentPopulator` | 2 | `sm-shop/.../transaction/PersistablePaymentPopulator.java` | DTO mapping read |

### 6.2 Recorded method hotspots

The recorded checkout CAST profile identifies these payment-related method hotspots:

| Method | Recorded complexity | Reason |
|---|---:|---|
| `sendTransaction` | 25 | Provider call and response parsing |
| `validateCreditCardNumber` | 22 | Card-format and Luhn validation |
| `process` | 19 | Payment/order processing path |
| `processPayment` | 19 | Provider dispatch and transaction creation |
| `getIntegrationModules` | 17 | Module/configuration resolution |
| `init` | 9 | Payment initialization controller path |
| `initTransaction` | 5 | Provider initialization dispatch |

The provider classes have high source semantic vectors even where method-level complexity is not preserved. They must not be treated as simple adapters until each authorization, capture, refund, amount-conversion, response, and exception path has been read.

---

## 7. Source Files to Read

### 7.1 Mandatory business-logic files

| Priority | Local path | CAST/P1 reason |
|---:|---|---|
| 1 | `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/payments/PaymentServiceImpl.java` | Provider selection, configuration decryption, authorization/capture/refund orchestration, card validation, order-side mutations |
| 2 | `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/payments/TransactionServiceImpl.java` | Transaction serialization, capturable/refundable selection, history ordering, date sweeps |
| 3 | `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/modules/integration/payment/impl/StripePayment.java` | Stripe classic token, authorization, capture, refund, amount conversion, error paths |
| 4 | `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/modules/integration/payment/impl/Stripe3Payment.java` | PaymentIntent/manual capture flow, intent status, refund behavior, amount/currency handling |
| 5 | `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/modules/integration/payment/impl/BraintreePayment.java` | Sandbox/production selection, nonce, sale, settlement, refund, validation errors |
| 6 | `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/modules/integration/payment/impl/PayPalExpressCheckoutPayment.java` | Express token lifecycle, authorization/sale, capture, refund, redirect URLs, credential use |
| 7 | `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/modules/integration/payment/impl/BeanStreamPayment.java` | Form-encoded backend transaction construction, response parsing, credential handling, logging |
| 8 | `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/modules/integration/payment/impl/MoneyOrderPayment.java` | Local authorize-and-capture behavior and no-provider external call semantics |
| 9 | `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/api/v1/order/OrderPaymentApi.java` | Payment initialization, ownership checks, admin payment endpoints, stubbed operations |
| 10 | `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/api/v1/payment/PaymentApi.java` | Payment-module listing, configuration writes, module validation boundary |
| 11 | `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/repositories/payments/TransactionRepository.java` | Transaction data access and broad order fetch graph |
| 12 | `initial-source/shopizer-3.2.7/sm-core-model/src/main/java/com/salesmanager/core/model/payments/Transaction.java` | `SM_TRANSACTION` mapping and serialized provider details |
| 13 | `initial-source/shopizer-3.2.7/sm-core-model/src/main/java/com/salesmanager/core/model/payments/TransactionType.java` | Legacy transaction state vocabulary |
| 14 | `initial-source/shopizer-3.2.7/sm-core-model/src/main/java/com/salesmanager/core/model/payments/Payment.java` | Payment request fields and defaults |
| 15 | `initial-source/shopizer-3.2.7/sm-core-model/src/main/java/com/salesmanager/core/model/payments/CreditCardPayment.java` | Conditional card validation fields |
| 16 | `initial-source/shopizer-3.2.7/sm-core-model/src/main/java/com/salesmanager/core/model/payments/PaymentType.java` | Payment method vocabulary |
| 17 | `initial-source/shopizer-3.2.7/sm-core-modules/src/main/java/com/salesmanager/core/modules/integration/payment/model/PaymentModule.java` | Provider SPI contract |
| 18 | `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/populator/order/transaction/PersistablePaymentPopulator.java` | API input conversion, token metadata mapping |
| 19 | `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/populator/order/transaction/ReadableTransactionPopulator.java` | Transaction response mapping |
| 20 | `initial-source/shopizer-3.2.7/sm-shop-model/src/main/java/com/salesmanager/shop/model/order/transaction/TransactionEntity.java` | API-visible transaction fields |
| 21 | `initial-source/shopizer-3.2.7/sm-core/src/main/resources/spring/shopizer-core-modules.xml` | Provider registration and active provider map |
| 22 | `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/order/OrderServiceImpl.java` | Context-only read of payment invocation and transaction association |
| 23 | `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/controller/order/facade/OrderFacadeImpl.java` | Context-only read of capture, transaction-list, next-transaction, and checkout coupling |

### 7.2 Supporting files to inspect as needed

```text
initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/payments/PaymentService.java
initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/payments/TransactionService.java
initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/system/MerchantConfigurationService.java
initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/system/ModuleConfigurationService.java
initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/reference/loader/ConfigurationModulesLoader.java
initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/constants/Constants.java
initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/utils/CoreConfiguration.java
initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/modules/utils/EncryptionImpl.java
initial-source/shopizer-3.2.7/sm-core-model/src/main/java/com/salesmanager/core/model/system/IntegrationConfiguration.java
initial-source/shopizer-3.2.7/sm-core-model/src/main/java/com/salesmanager/core/model/system/IntegrationModule.java
initial-source/shopizer-3.2.7/sm-shop-model/src/main/java/com/salesmanager/shop/model/system/IntegrationModuleConfiguration.java
initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/facade/payment/PaymentConfigurationFacadeImpl.java
```

---

## 8. Source Files to Skip or Treat as Context Only

| File or family | Treatment | Reason |
|---|---|---|
| `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/modules/integration/payment/impl/PayPalRestPayment.java` | Reachability review only | Not registered in `paymentModules`; reported implementation gaps and null-returning paths |
| `OrderFacadeImpl.processOrder` outside the payment invocation region | Context only | MS-04/MS-05 checkout and order ownership |
| `OrderServiceImpl.caculateOrder` and total processors | Context only | MS-04/MS-07/MS-08/MS-09 calculation ownership |
| `ProductPriceUtils.java` | Skip deep extraction | MS-07 owns amount and price calculation; read only for monetary contract |
| Tax and shipping service implementations | Skip deep extraction | MS-08 and MS-09 ownership |
| Cart services and cart repositories | Skip deep extraction | MS-04 ownership |
| Customer services and customer repositories | Skip deep extraction | MS-01 ownership |
| Merchant-store lifecycle services | Skip deep extraction | MS-10 ownership |
| CMS, file, email, and search providers | Skip | MS-11/MS-12 ownership; only record payment-flow dependency |
| Generic repository base classes | Skip | Persistence infrastructure |
| Generic audit, logging, cache, and framework classes | Skip | No independent payment rule unless a concrete security or consistency behavior is found |
| DTO getters/setters and entity boilerplate | Do not extract independently | Data shape only |
| Frontend payment components | Context only | `BR-UI-015` is already extracted; use only to verify token and amount compatibility |
| Existing tests | Skip for this brief | No test generation or test artifact work is requested |

Do not mark older facades or low-complexity repository methods dead solely because they are not obvious from one endpoint. Confirm reachability through CAST callers and Spring wiring before exclusion.

---

## 9. Data Ownership

### 9.1 Owned legacy table

| Legacy table | Evidence | CRUD scope | Target ownership |
|---|---|---|---|
| `SM_TRANSACTION` | CAST data graph `243929`; `Transaction` entity; `TransactionServiceImpl` | Create, read by order, read by date; association to order | MS-06 |

`SM_TRANSACTION` is the only confirmed payment-owned legacy table.

### 9.2 Referenced but not owned

| Legacy table | Target owner | MS-06 use |
|---|---|---|
| `ORDERS` | MS-05 | Order/payment correlation and order amount context |
| `ORDER_TOTAL` | MS-05 | Legacy refund behavior only; target MS-06 publishes refund event |
| `ORDER_STATUS_HISTORY` | MS-05 | Legacy payment-triggered status history; target MS-05 consumes payment events |
| `ORDER_PRODUCT` | MS-05 | Provider line-item context during authorization |
| `MERCHANT_STORE` | MS-10 | Store and tenant context |
| `MERCHANT_CONFIGURATION` | MS-11 | Legacy encrypted payment-module configuration |
| `MODULE_CONFIGURATION` | MS-11 | Available payment module metadata |
| `CUSTOMER` | MS-01 | Customer context for authenticated payment operations |
| `SHOPPING_CART` | MS-04 | Payment initialization ownership check and checkout context |
| `PRODUCT`, `PRODUCT_AVAILABILITY`, `PRODUCT_PRICE` | MS-02/MS-07 | Amount and line-item context; no direct MS-06 writes |
| `SM_SEQUENCER` | Shared infrastructure | Legacy identifier generation; not a payment domain table |

No cross-service foreign keys or direct writes to another service's schema are permitted in the target design.

---

## 10. Cross-Service Dependencies

| Dependency | Legacy evidence | Target boundary |
|---|---|---|
| MS-04 Cart and Checkout | Payment initialization and checkout invoke payment logic directly | MS-04 freezes checkout amount and publishes `OrderSubmitted`; no direct provider call from MS-04 |
| MS-05 Order Management | Checkout creates orders and payment can mutate order status/totals | MS-05 publishes `PaymentRequested` and consumes authenticated payment events; MS-05 alone changes order state |
| MS-01 Customer and Identity | Authenticated payment initialization resolves customer by principal | Consume authenticated customer/store context; do not read customer tables |
| MS-10 Merchant and Store Administration | All payment configuration and provider operations are store-scoped | Require tenant/store context on every operation |
| MS-11 Content and Configuration | Payment module metadata and encrypted merchant configuration are read through configuration services | Consume versioned provider configuration or credential references; MS-06 does not own configuration tables |
| MS-07 Pricing and Promotions | Payment amount comes from checkout recalculation and pricing services | Accept an immutable amount/currency snapshot from MS-04/MS-05; do not recalculate product prices inside MS-06 |
| MS-08 Tax | Tax contributes to the amount sent to payment | Treat the submitted total as an upstream signed/versioned snapshot |
| MS-09 Shipping | Shipping contributes to the amount sent to payment | Do not query shipping tables directly |
| MS-12 Platform Integrations | Common external egress, secrets, telemetry, and delivery infrastructure may be shared | Domain-specific payment adapters remain under MS-06; generic secret/egress platform services may be supplied by MS-12 |
| External providers | Stripe, Stripe 3, Braintree, PayPal Express, Beanstream | Provider-specific adapters, timeout, retry, response normalization, and callback verification remain behind MS-06 |

### Dependency invariants

- MS-06 never changes order status directly.
- MS-06 never writes order totals.
- MS-06 never writes inventory.
- Payment events must be authenticated, versioned, and idempotently consumed by MS-05.
- Provider callbacks must be correlated to an internal payment intent and provider reference.
- Amount and currency must be verified against the payment intent before authorization, capture, or refund.
- Provider credentials must be represented by secret references, not persisted in payment transactions.

---

## 11. Phase 1 Rules Requiring Deep Extraction

All 15 rules assigned to MS-06 require Phase 4 re-extraction.

| BR-ID | Phase 1 rule | Deep-extraction focus |
|---|---|---|
| `BR-ORD-014` | Payment provider selection is configuration-driven | Active module filtering, transaction mode, module lookup, missing/inactive configuration, provider fallback |
| `BR-ORD-015` | Provider transactions persist before and after order association | Transaction creation timing, order association, failure windows, duplicate persistence, atomicity gap |
| `BR-ORD-016` | Capture requires prior authorization and sets `PROCESSED` | Capturable-state definition, provider authorization reference, capture amount, duplicate capture, order-event boundary |
| `BR-ORD-017` | Refund cannot exceed current order total | Exact monetary arithmetic, cumulative refund balance, refundable transaction selection, partial/full semantics, provider refund reference |
| `BR-ORD-019` | Card validation is conditional and duplicated | Feature flag, required fields, date rules, card-type rules, Luhn validation, tokenization boundary, sensitive-data handling |
| `BR-EXT-001` | Payment provider dispatch is merchant-configuration driven | Module registry, merchant configuration decryption, configuration versioning, tenant/store isolation |
| `BR-EXT-002` | Capture requires a capturable prior transaction and transitions order | Authorization/capture state machine and replacement of direct order mutation with event publication |
| `BR-EXT-003` | Refund cannot exceed current order total | Cumulative refund invariant and provider-side validation |
| `BR-EXT-004` | Stripe classic validates credentials and payment tokens before gateway calls | `secretKey`, `publishableKey`, Stripe token, amount conversion, error mapping |
| `BR-EXT-005` | Stripe 3 uses PaymentIntent/manual-capture flow | PaymentIntent creation/retrieval, manual capture, intent status, currency, minor-unit conversion, refund type |
| `BR-EXT-006` | Braintree selects sandbox/production from environment configuration | `merchant_id`, `public_key`, `private_key`, nonce, environment, sale/settlement/refund outcomes |
| `BR-EXT-007` | PayPal Express requires token/payer/environment credentials | API credentials, token and payer flow, authorization versus sale, capture/refund identifiers |
| `BR-EXT-008` | Beanstream builds form-encoded backend transactions and parses approval fields | Merchant credentials, request encoding, response approval fields, transport failures, sensitive logging |
| `BR-EXT-009` | Money Order supports local authorize-and-capture only | No external provider call, local transaction semantics, order-event behavior |
| `BR-UI-015` | Checkout tokenizes Stripe payment and submits capture data | Frontend token shape, amount/currency consistency, `stripe_token` versus `paymentToken`, possible token-variable defect |

### P1 findings needing BA confirmation

- Whether `AUTHORIZECAPTURE` means a single provider sale or two distinct internal state transitions.
- Whether a payment intent may be retried with a changed amount or currency.
- Whether partial refunds are cumulative against the original authorized/captured amount.
- Whether a successful provider response is sufficient for `CAPTURED`, or whether asynchronous settlement/callback confirmation is required.
- Whether Money Order should publish `PaymentCaptured` immediately or remain pending manual verification.
- Whether card validation remains in MS-06 when all card data is tokenized by the frontend.
- Whether provider credentials are migrated to a vault and rotated independently of module configuration.
- Whether PayPal Express initialization is an active requirement; the direct `initTransaction` implementation is not implemented while `initPaypalTransaction` exists.
- Whether PayPal REST is intentionally obsolete or an unregistered future provider.
- Whether provider callback/webhook flows exist outside the analyzed backend application.

---

## 12. Provider and Credential Boundaries

### 12.1 Registered provider map

`shopizer-core-modules.xml` registers:

| Module code | Provider | Registered |
|---|---|---|
| `beanstream` | `BeanStreamPayment` | Yes |
| `paypal-express-checkout` | `PayPalExpressCheckoutPayment` | Yes |
| `moneyorder` | `MoneyOrderPayment` | Yes |
| `stripe` | `StripePayment` | Yes |
| `stripe3` | `Stripe3Payment` | Yes |
| `braintree` | `BraintreePayment` | Yes |
| PayPal REST | `PayPalRestPayment` | No evidence of registration |

### 12.2 Credential and configuration keys

| Provider | Configuration keys observed | Runtime payment data |
|---|---|---|
| Beanstream | `merchantid`, `username`, `password` | Card/order fields and provider transaction data |
| Braintree | `merchant_id`, `public_key`, `private_key`, `tokenization_key` | `paymentToken` nonce |
| PayPal Express | `api`, `username`, `signature`, environment | PayPal token/payer data |
| Stripe classic | `secretKey`, `publishableKey` | `stripe_token` or `paymentToken` |
| Stripe 3 | `secretKey`, `publishableKey` | PaymentIntent ID/token |
| Money Order | `address` | No external provider token |
| PayPal REST | `client`, `secret` | Implementation/reachability unresolved |

### 12.3 Required target boundary

- Store provider configuration in MS-11 or a secret-management platform.
- Persist only a credential reference/version in MS-06.
- Never persist API secrets, private keys, card numbers, CVV, or raw provider credential payloads in `payment_transaction`.
- Treat provider response details as allowlisted structured fields, not unrestricted raw JSON.
- Redact provider tokens and payment identifiers in logs unless the identifier is explicitly approved for correlation.
- Ensure provider configuration changes cannot alter the interpretation of an already-created payment intent.
- Bind provider configuration version to the payment intent or operation.
- Use provider-specific clients behind a neutral MS-06 adapter interface.
- Normalize provider status into internal states while retaining the raw provider status in a controlled diagnostic field where permitted.

### Security risks requiring deep read

- Beanstream constructs and logs request strings; verify that credentials and card data are not written to logs.
- Stripe and Braintree provider identifiers are stored in `Transaction.details`.
- `PaymentServiceImpl` decrypts merchant configuration in application memory.
- `PersistablePaymentPopulator` places the incoming token in a generic metadata map.
- The target must not accept arbitrary client-supplied transaction status, provider reference, or amount as authoritative.

---

## 13. Hidden-Engine Check

### 13.1 Payment state engine

**Finding: hidden state engine present.**

The legacy state vocabulary is:

```text
INIT
AUTHORIZE
AUTHORIZECAPTURE
CAPTURE
REFUND
OK
```

Payment state is not represented by one explicit aggregate state. It is reconstructed from transaction rows and provider details.

Observed issues:

- `lastTransaction` chooses by transaction-type name rather than transaction timestamp.
- `getCapturableTransaction` scans for an authorization and stops at capture/refund, but does not enforce amount or provider status.
- `getRefundableTransaction` selects authorization-plus-capture or capture transactions and tracks refunds inconsistently.
- No explicit transition matrix exists.
- No concurrency version or row lock is evident.
- No idempotency key is persisted.
- `OK` has no clear transition semantics in the examined model.
- Provider response status is stored in loosely typed detail maps.

**Extraction consequence:** MS-06 needs an explicit payment state machine with operation records and provider status correlation. The generator must not infer legal transitions from enum ordering or transaction-type sorting.

### 13.2 Authorization semantics

**Finding: provider authorization is not semantically uniform.**

- `AUTHORIZE` may create an authorization-only provider operation.
- `AUTHORIZECAPTURE` may perform an immediate sale.
- Stripe 3 initialization creates a PaymentIntent with manual capture and records it as `AUTHORIZE`.
- Stripe 3 authorization retrieves an intent but does not visibly verify amount, currency, or successful authorization status before creating the local transaction.
- Braintree's `authorize` method calls a sale request without an explicit authorization-only flag in the observed code.
- PayPal Express chooses sale versus authorization from configured transaction mode.
- Money Order locally creates `AUTHORIZECAPTURE`.

Phase 4 must distinguish:

```text
intent created
authorization requested
authorized
capture requested
captured
authorization failed
capture failed
cancelled/expired
refund requested
partially refunded
refunded
provider reconciliation required
```

The internal state must not be named solely from the legacy `TransactionType`.

### 13.3 Capture semantics

**Finding: capture is a separate provider operation but an incomplete API capability.**

- The administrative capture endpoint returns `null`.
- The service-level capture implementation exists.
- Capture uses a prior authorization transaction and provider reference.
- Braintree submits the authorization for settlement using the order total.
- PayPal Express captures using provider transaction data.
- Stripe classic and Stripe 3 use provider-specific capture behavior.
- Capture amount is not consistently explicit or independently validated.
- The legacy path directly writes `PROCESSED` to the order.

Required extraction points:

- Full versus partial capture.
- Whether multiple captures are legal.
- Capture expiration.
- Provider settlement versus immediate capture confirmation.
- Amount and currency equality with the original intent.
- Duplicate capture response handling.
- Event publication timing.

### 13.4 Refund semantics

**Finding: refund is an incomplete, non-idempotent state engine.**

Observed behavior:

- Refund amount is compared to the current order total using `doubleValue()`.
- A refundable transaction is selected from transaction history.
- A provider refund is requested.
- A local `REFUND` transaction is created.
- The order total is reduced.
- A refund order-total row and `REFUNDED` order status are written.

Risks:

- Cumulative partial refunds may exceed the original captured amount.
- A repeated request can issue a second provider refund.
- Stripe classic and Stripe 3 appear to contain transaction-type or provider-reference inconsistencies.
- Refund state and order state are coupled in one legacy service.
- Provider success may be returned before asynchronous refund settlement.

Required extraction points:

- Refundable balance = captured amount minus successful prior refunds.
- Exact decimal arithmetic in minor units or fixed-precision decimal.
- Full and partial refund semantics.
- Refund idempotency key.
- Provider refund status and callback/reconciliation handling.
- Compensation when provider succeeds but local persistence fails.
- No direct MS-05 order-total mutation by MS-06.

### 13.5 Idempotency check

**Finding: no legacy idempotency mechanism found.**

Evidence:

- No idempotency-key field or dedicated idempotency table was found in the inspected source.
- Payment initialization accepts no visible idempotency key.
- Checkout and payment provider calls are not enclosed in one atomic transaction.
- Transaction details are used as provider references but not as request deduplication records.
- Capture and refund operations can be retried without a clear replay contract.

Target requirement:

- Require an idempotency key for payment-intent creation, authorization, capture, refund, and callback ingestion.
- Scope keys by tenant, store, operation type, and aggregate.
- Store request fingerprint, provider operation reference, normalized result, and replay status.
- Reject the same key with a different request fingerprint.
- Return the original result for a true duplicate.
- Use an inbox record for provider callbacks and a unique provider-event identifier where available.

### 13.6 External-provider orchestration

**Finding: external-provider orchestration is the primary hidden engine.**

The provider layer contains:

- Provider registry and configuration-driven routing.
- Environment selection.
- Credential validation and decryption.
- Token/nonce/payment-intent resolution.
- Amount conversion to provider minor units or formatted decimal strings.
- Remote authorization, sale, capture, and refund calls.
- Provider response parsing.
- Local transaction construction.
- Error and decline normalization.
- Provider identifier storage.
- Incomplete callback/reconciliation semantics.

The target orchestration must separate:

1. Internal payment command.
2. Provider attempt record.
3. Remote provider call.
4. Provider response normalization.
5. Local transaction commit.
6. Event publication.
7. Retry/reconciliation.

No provider call should occur inside a distributed database transaction spanning MS-04, MS-05, or MS-06.

---

## 14. Placement Candidates

Default placement is application/domain tier. No legacy stored procedure or payment batch job was identified.

| Candidate | Legacy evidence | Recommended initial placement | Phase 4b decision needed |
|---|---|---|---|
| Payment state transition guards | `TransactionServiceImpl` reconstructs state from rows | MS-06 application/domain layer plus database uniqueness/concurrency constraints | Yes |
| Authorization/capture/refund orchestration | `PaymentServiceImpl` provider dispatch | MS-06 application tier | Yes |
| Provider adapters | Stripe, Braintree, PayPal, Beanstream classes | MS-06 provider-adapter boundary; common egress may use MS-12 | Yes |
| Monetary validation and refund balance | `processRefund`, provider amount conversion | MS-06 application tier using fixed-precision decimal/minor units | Yes |
| Transaction history listing | `findByOrder`, `findByDates` with broad joins | Application query/read model; optional DB view only if volume evidence supports it | Yes |
| Capturable-order date sweep | `listCapturableOrders`, `findByDates` | Application query or reconciliation worker | Yes |
| Idempotency uniqueness | No legacy equivalent | Mandatory database uniqueness plus application replay logic | Yes |
| Outbox/inbox | No legacy equivalent | Standard platform persistence pattern | Yes |
| Credential encryption/decryption | Legacy merchant configuration decryption | Secret-management boundary; no payment-domain stored procedure | Yes |
| Callback signature validation | No confirmed legacy callback endpoint | MS-06 application/edge adapter | Yes |
| Provider reconciliation | No scheduled job found | Explicit worker only if provider requirements justify it | Yes |

Do not place authorization, capture, refund, provider calls, or payment-state transitions in database procedures by default. The operations are provider-bound, conditional, and failure-sensitive.

---

## 15. Dead-Code and Exclusion Register

### 15.1 Confirmed unimplemented or inactive paths

| Component/path | Evidence | Treatment |
|---|---|---|
| `OrderPaymentApi.capturePayment` | CAST transaction `244098`; method returns `null`; implementation is commented out | Exclude as executable legacy behavior; retain as required target capability |
| `OrderPaymentApi.refundPayment` | CAST transaction `244099`; method returns `null` | Exclude as executable legacy behavior; retain as required target capability |
| `OrderPaymentApi.authorizePayment` | CAST transaction `244100`; method returns `null` | Exclude as executable legacy behavior; retain as required target capability |
| Commented capture implementation in `OrderPaymentApi` | Source comments only | Do not extract as active behavior |
| `PayPalRestPayment` registration | No entry in `paymentModules` map | Exclude from active provider set pending live CAST caller/configuration evidence |
| `PayPalRestPayment.initTransaction` and other null/unimplemented paths | P1 summary identifies implementation gaps | Treat as dormant provider candidate, not active preserved behavior |
| PayPal Express direct `initTransaction` | Source throws `IntegrationException("Not imlemented")`; separate `initPaypalTransaction` exists | Record as unresolved provider capability, do not assume active initialization behavior |
| Inactive generic payment modules | Configuration-dependent | Do not classify a provider as dead solely because it is not active for one store |

### 15.2 Do not classify as dead without CAST caller evidence

- `PaymentModule` interface methods.
- `TransactionService` repository methods.
- Legacy and versioned DTO/populator classes.
- Older order facade paths.
- Provider classes selected through configuration rather than direct static calls.
- `PaymentConfigurationFacadeImpl`.
- Low-complexity transaction entity and repository methods.
- Provider callback or redirect helper methods.
- Configuration modules loaded dynamically.

The preserved CAST artifacts do not include a reliable payment-specific unreachable-component report. No active provider should be removed solely from zero visible callers.

---

## 16. Payment UI Compatibility Evidence

Assigned rule: `BR-UI-015`.

Source:

```text
initial-source/shopizer-shop-reactjs-main/src/pages/other/Checkout.js:198-225
initial-source/shopizer-shop-reactjs-main/src/pages/other/Checkout.js:508-625
```

Recorded behavior:

- Checkout tokenizes Stripe payment data.
- Checkout submits currency, shipping quote, payment module, token, and amount.
- The UI may pass `result.token.id` to `onPayment`, while a later path reads `result.token`.
- The target must define one canonical token field and reject missing or ambiguous token shapes.
- The client amount is informational; MS-04/MS-05 must provide the authoritative amount snapshot.
- MS-06 must verify the payment intent amount and currency before provider authorization or capture.

---

## 17. Extraction Priorities

### Priority 1 — Must be resolved before rule extraction

1. Provider registry and active module configuration.
2. Authorization versus authorization-plus-capture semantics per provider.
3. PaymentIntent/manual-capture semantics for Stripe 3.
4. Provider amount and currency verification.
5. Capturable transaction selection.
6. Refundable transaction selection and cumulative refund balance.
7. Transaction ordering and state-transition rules.
8. Provider identifiers stored in `DETAILS`.
9. Credential storage and logging exposure.
10. Missing idempotency and retry behavior.
11. Callback/webhook presence and authentication.
12. Direct order status and total mutations.

### Priority 2 — Required for boundary validation

1. Payment initialization ownership for anonymous and authenticated carts.
2. Payment amount source and stale checkout handling.
3. Store and tenant binding.
4. Provider configuration versioning.
5. Order/payment event contract assumptions.
6. Failure windows between provider success and local persistence.
7. Reconciliation and operational recovery.
8. Money Order settlement semantics.
9. PayPal Express initialization path.
10. PayPal REST reachability.

### Priority 3 — Context only

1. Product line-item hydration.
2. Tax and shipping calculation.
3. Order snapshot construction.
4. Cart completion.
5. Email and download notification.
6. Inventory decrement.
7. Order listing and unrelated order filters.

---

## 18. Scout Exit Assessment

| Check | Result |
|---|---|
| CAST application identified | Pass — `Shopizer-Backend` |
| Local CAST root mapping resolved | Pass — `§{main_sources}§` → `initial-source/` |
| Checkout payment transactions identified | Pass — `244089`, `244090` |
| Payment initialization transactions identified | Pass — `244093`, `244094` |
| Authorization/capture/refund transactions identified | Pass — `244098`, `244099`, `244100`; all endpoint stubs |
| Transaction history transactions identified | Pass — `244095`, `244096`, `244097` |
| Payment-module source endpoints identified | Pass; transaction IDs not preserved |
| Critical full call graphs available | Partial — recorded for checkout/payment-init; admin operation graphs are stub-sized |
| Payment/order data graphs identified | Pass — `243929`, `243908`, `243909` |
| Merchant/module data graphs identified | Partial — table names visible through checkout graphs; dedicated graph IDs require live rerun |
| Complexity-ranked payment components identified | Pass — CAST IDs and `srcControlFlow` recorded |
| Source paths resolved | Pass |
| `SM_TRANSACTION` ownership identified | Pass — MS-06 |
| Cross-service dependencies identified | Pass |
| P1 payment rules identified | Pass — 15 assigned rules |
| Provider/credential boundaries identified | Pass |
| Placement candidates identified | Pass |
| Dead/unimplemented exclusions identified | Pass |
| Hidden payment-state engine checked | Pass |
| Authorization/capture/refund semantics checked | Pass with unresolved provider differences |
| Idempotency checked | Pass — no legacy mechanism found |
| External-provider orchestration checked | Pass |
| New MS-06 specification artifacts generated | No |
| Tests generated | No |

**Overall scout confidence:** `0.78`

**Confidence limitation:** The payment source-component CAST scope is strong, but dedicated live transaction records for payment-module endpoints and dedicated merchant/module data-graph identifiers must be refreshed before final Phase 4 extraction. The largest business risks are cumulative refund accounting, provider amount/status verification, PayPal Express initialization, Stripe 3 semantics, callback handling, and the absence of legacy idempotency.
