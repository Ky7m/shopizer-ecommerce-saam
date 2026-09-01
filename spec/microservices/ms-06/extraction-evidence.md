
# MS-06 Payments — Extraction Evidence

**Session date:** 2026-09-01  
**Analysis mode:** Hybrid  
**Local source root:** `initial-source/shopizer-3.2.7/`  
**CAST application:** `Shopizer-Backend`  
**CAST delivery:** `Onboarding-202511171247`  
**CAST root mapping:** `§{main_sources}§` → `initial-source/`  
**Target service:** MS-06 Payments

## Reading Method

Each mandatory source file was read in numbered multi-pass ranges. Large provider and service files were read in sequential ranges covering the complete file. The evidence below records exact file line ranges and the business behavior reviewed.

## Source Files Processed

| # | File | Lines Read | Sections / behavior reviewed | Rules | Vectors |
|---:|---|---:|---|---:|---|
| 1 | `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/payments/PaymentServiceImpl.java` | 1-784 | Provider method filtering; configuration decryption; module validation; authorization; authorization-plus-capture; capture; refund; card validation; initialization | 8 | CAST `12989`, `srcControlFlow=102`; all dimensions counted per rule |
| 2 | `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/payments/TransactionServiceImpl.java` | 1-208 | Serialization; transaction listing; lexicographic last-transaction selection; capturable selection; refundable selection; date listing | 2 | CAST `13115`, `srcControlFlow=24` |
| 3 | `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/modules/integration/payment/impl/StripePayment.java` | 1-576 | Configuration keys; token validation; charge authorization; capture; sale; refund; exception mapping | 1 | CAST `11679`, `srcControlFlow=43` |
| 4 | `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/modules/integration/payment/impl/Stripe3Payment.java` | 1-621 | PaymentIntent initialization; manual capture; retrieval; refund; provider statuses; amount conversion; exception mapping | 1 | CAST `11710`, `srcControlFlow=47` |
| 5 | `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/modules/integration/payment/impl/BraintreePayment.java` | 1-469 | Credential validation; sandbox/production selection; client token; nonce; sale; settlement; refund; validation errors | 1 | CAST `11668`, `srcControlFlow=39` |
| 6 | `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/modules/integration/payment/impl/PayPalExpressCheckoutPayment.java` | 1-670 | Credentials; unimplemented SPI initialization; Express initialization; line/tax/shipping totals; return/cancel URLs; authorization/sale; capture; refund | 1 | CAST `11688`, `srcControlFlow=44` |
| 7 | `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/modules/integration/payment/impl/BeanStreamPayment.java` | 1-738 | Form construction; environment selection; HTTP transport; response decoding; approval validation; capture; refund; credential validation; logging | 1 | CAST `11652`, `srcControlFlow=61` |
| 8 | `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/modules/integration/payment/impl/MoneyOrderPayment.java` | 1-113 | Address configuration; local authorization-plus-capture; unsupported capture/refund paths | 1 | CAST `11678`, `srcControlFlow=3` |
| 9 | `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/api/v1/order/OrderPaymentApi.java` | 1-362 | Anonymous/authenticated initialization; ownership checks; transaction history; next transaction; capturable listing; stubbed admin operations | 3 | CAST `29357`, `srcControlFlow=30`; transactions `244093`, `244094`, `244095`, `244096`, `244097`, `244098`, `244099`, `244100` |
| 10 | `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/api/v1/payment/PaymentApi.java` | 1-205 | Payment-module listing; configuration write; provider lookup; configuration projection | 1 | CAST `30425`, `srcControlFlow=11` |
| 11 | `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/repositories/payments/TransactionRepository.java` | 1-22 | Find by order; date query; fetch joins; absence of explicit ordering | supporting | CAST data graph `243929` |
| 12 | `initial-source/shopizer-3.2.7/sm-core-model/src/main/java/com/salesmanager/core/model/payments/Transaction.java` | 1-187 | Entity mapping; amount; date; transaction type; payment type; details serialization; order relationship | supporting | CAST `17360`, `srcControlFlow=4` |
| 13 | `initial-source/shopizer-3.2.7/sm-core-model/src/main/java/com/salesmanager/core/model/payments/TransactionType.java` | 1-7 | Legacy transaction vocabulary: INIT, AUTHORIZE, CAPTURE, AUTHORIZECAPTURE, REFUND, OK | supporting | no independent rule |
| 14 | `initial-source/shopizer-3.2.7/sm-core-model/src/main/java/com/salesmanager/core/model/payments/Payment.java` | 1-65 | Payment type; transaction type default; module name; currency; amount; metadata | supporting | source data-flow evidence |
| 15 | `initial-source/shopizer-3.2.7/sm-core-model/src/main/java/com/salesmanager/core/model/payments/CreditCardPayment.java` | 1-53 | Card number; validation number; expiry; owner; card type fields | supporting | source data-flow evidence |
| 16 | `initial-source/shopizer-3.2.7/sm-core-model/src/main/java/com/salesmanager/core/model/payments/PaymentType.java` | 1-28 | Payment method vocabulary | supporting | source data-flow evidence |
| 17 | `initial-source/shopizer-3.2.7/sm-core-modules/src/main/java/com/salesmanager/core/modules/integration/payment/model/PaymentModule.java` | 1-54 | Provider SPI operations: validation, init, authorize, capture, authorize-and-capture, refund | supporting | SPI integration vector |
| 18 | `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/populator/order/transaction/PersistablePaymentPopulator.java` | 1-67 | Request amount conversion; module; payment type; transaction type; payment token metadata | 1 | CAST `19820`, `srcControlFlow=2` |
| 19 | `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/populator/order/transaction/ReadableTransactionPopulator.java` | 1-83 | Response amount/date formatting; transaction fields; optional order ID | supporting | CAST `19828`, `srcControlFlow=7` |
| 20 | `initial-source/shopizer-3.2.7/sm-shop-model/src/main/java/com/salesmanager/shop/model/order/transaction/TransactionEntity.java` | 1-50 | API-visible transaction ID; order ID; details; date; amount | supporting | response data-flow evidence |
| 21 | `initial-source/shopizer-3.2.7/sm-core/src/main/resources/spring/shopizer-core-modules.xml` | 1-141 | Registered provider map and bean registrations: Beanstream, PayPal Express, Money Order, Stripe, Stripe3, Braintree | 1 | provider registry evidence |
| 22 | `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/order/OrderServiceImpl.java` | 1-680 | Payment-first order processing; transaction association; order status writes; capturable-order scan and filters | boundary evidence | CAST data graph `243908` |
| 23 | `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/controller/order/facade/OrderFacadeImpl.java` | 1-1648 | Checkout payment construction; payment token mapping; capture facade; transaction history; next-transaction mapping | boundary evidence | checkout CAST paths `244089`, `244090` |

## Exact High-Value Ranges

| Behavior | Source range |
|---|---|
| Store-region payment filtering | `PaymentServiceImpl.java:82-96` |
| Accepted active payment methods | `PaymentServiceImpl.java:99-130` |
| Provider code lookup | `PaymentServiceImpl.java:146-159` |
| Configuration decryption/loading | `PaymentServiceImpl.java:185-207` |
| Configuration validation and encrypted persistence | `PaymentServiceImpl.java:209-253` |
| Configuration removal | `PaymentServiceImpl.java:255-298` |
| Provider dispatch and transaction-mode selection | `PaymentServiceImpl.java:300-398` |
| Capture orchestration | `PaymentServiceImpl.java:405-472` |
| Refund orchestration and direct order mutation | `PaymentServiceImpl.java:474-569` |
| Card validation | `PaymentServiceImpl.java:571-698` |
| Order-bound initialization | `PaymentServiceImpl.java:699-738` |
| Anonymous initialization | `PaymentServiceImpl.java:739-782` |
| Transaction serialization | `TransactionServiceImpl.java:37-49` |
| Transaction listing and JSON detail parsing | `TransactionServiceImpl.java:51-69` |
| Legacy transaction ordering defect | `TransactionServiceImpl.java:86-116` |
| Capturable transaction selection | `TransactionServiceImpl.java:118-146` |
| Refundable transaction selection | `TransactionServiceImpl.java:148-200` |
| Date-based transaction listing | `TransactionServiceImpl.java:202-207` |
| Stripe classic provider validation | `StripePayment.java:50-87` |
| Stripe classic authorization | `StripePayment.java:108-187` |
| Stripe classic capture | `StripePayment.java:189-250` |
| Stripe classic authorization-plus-capture | `StripePayment.java:252-331` |
| Stripe classic refund and error normalization | `StripePayment.java:333-576` |
| Stripe3 initialization | `Stripe3Payment.java:52-107` |
| Stripe3 provider validation | `Stripe3Payment.java:109-143` |
| Stripe3 authorization | `Stripe3Payment.java:145-206` |
| Stripe3 capture | `Stripe3Payment.java:207-277` |
| Stripe3 authorization-plus-capture | `Stripe3Payment.java:278-375` |
| Stripe3 refund | `Stripe3Payment.java:376-442` |
| Braintree provider validation | `BraintreePayment.java:30-79` |
| Braintree initialization | `BraintreePayment.java:80-117` |
| Braintree authorization | `BraintreePayment.java:119-210` |
| Braintree capture | `BraintreePayment.java:212-296` |
| Braintree authorization-plus-capture | `BraintreePayment.java:297-386` |
| Braintree refund | `BraintreePayment.java:387-469` |
| PayPal Express provider validation | `PayPalExpressCheckoutPayment.java:72-111` |
| PayPal Express unimplemented SPI initialization | `PayPalExpressCheckoutPayment.java:113-120` |
| PayPal Express authorization entry | `PayPalExpressCheckoutPayment.java:122-135` |
| PayPal Express initialization helper | `PayPalExpressCheckoutPayment.java:149-325` |
| PayPal Express authorization-plus-capture | `PayPalExpressCheckoutPayment.java:327-339` |
| PayPal Express refund | `PayPalExpressCheckoutPayment.java:341-433` |
| PayPal Express completion flow | `PayPalExpressCheckoutPayment.java:434-558` |
| PayPal Express capture | `PayPalExpressCheckoutPayment.java:559-670` |
| Beanstream authorization/capture/refund entry points | `BeanStreamPayment.java:67-239` |
| Beanstream HTTP request/response handling | `BeanStreamPayment.java:240-459` |
| Beanstream form construction | `BeanStreamPayment.java:460-649` |
| Beanstream response parsing | `BeanStreamPayment.java:650-688` |
| Beanstream configuration validation | `BeanStreamPayment.java:689-738` |
| Money-order local behavior | `MoneyOrderPayment.java:26-113` |
| Payment SPI | `PaymentModule.java:1-54` |
| Provider registry | `shopizer-core-modules.xml:47-61`, `82-101` |
| Anonymous payment initialization endpoint | `OrderPaymentApi.java:88-118` |
| Authenticated payment initialization and cart ownership | `OrderPaymentApi.java:120-181` |
| Transaction next-state endpoint | `OrderPaymentApi.java:183-202` |
| Transaction history endpoint | `OrderPaymentApi.java:204-223` |
| Capturable listing and default date range | `OrderPaymentApi.java:225-281` |
| Capture/refund/authorize stubs | `OrderPaymentApi.java:283-361` |
| Payment-module list/configuration/details | `PaymentApi.java:55-182` |
| Legacy transaction persistence mapping | `Transaction.java:35-187` |
| Request payment conversion | `PersistablePaymentPopulator.java:20-52` |
| Response transaction conversion | `ReadableTransactionPopulator.java:23-52` |
| Payment-first order processing | `OrderServiceImpl.java:127-186` |
| Capturable-order scan | `OrderServiceImpl.java:603-680` |
| Checkout payment construction | `OrderFacadeImpl.java:457-552` |
| Capture facade | `OrderFacadeImpl.java:1416-1429` |
| Next transaction mapping | `OrderFacadeImpl.java:1555-1590` |
| Transaction history facade | `OrderFacadeImpl.java:1595-1625` |

## CAST Transaction Evidence

| CAST ID | Flow | Size | Disposition |
|---:|---|---:|---|
| 244089 | Authenticated checkout | 3,245 nodes / 8,112 links | Payment slice only; MS-04/MS-05 own checkout/order |
| 244090 | Anonymous checkout | 3,262 nodes / 8,173 links | Payment slice only; cart/order ownership excluded |
| 244094 | Authenticated payment initialization | 643 nodes / 1,335 links | MS-06 payment initialization evidence |
| 244093 | Anonymous payment initialization | 616 nodes / 1,288 links | MS-06 payment initialization evidence |
| 244097 | Capturable orders | 580 nodes | Date-sweep and transaction filtering evidence |
| 244098 | Capture endpoint | 9 nodes | Stub endpoint; target capability retained |
| 244099 | Refund endpoint | 9 nodes | Stub endpoint; target capability retained |
| 244100 | Authorize endpoint | 9 nodes | Stub endpoint; target capability retained |
| 244095 | Next transaction | 333 nodes | Legacy state-selection evidence |
| 244096 | Transaction history | 407 nodes | Legacy transaction listing evidence |

## Data Graph Evidence

| CAST graph | Root | Relevance |
|---:|---|---|
| 243929 | `sm_transaction` | Primary legacy payment transaction persistence |
| 243908 | `orders` | Referenced order context; not owned by MS-06 |
| 243909 | `order_product` | Referenced checkout/provider context; not owned by MS-06 |

## Provider Registration Evidence

Registered in `shopizer-core-modules.xml`:

- `beanstream`
- `paypal-express-checkout`
- `moneyorder`
- `stripe`
- `stripe3`
- `braintree`

Not registered in the preserved map:

- `PayPalRestPayment`

## Full-Read Status

- Mandatory source files: **23**
- Mandatory source files fully ranged: **23**
- Mandatory provider resources fully ranged: **1**
- Source files marked context-only by the brief: excluded from independent rule extraction
- Existing tests read: **0**, as required
- New tests created: **0**
- New dependencies created: **0**
- New workflows created: **0**
- New unrelated files created: **0**

## Evidence Limitations

1. Dedicated live CAST IDs for payment-module configuration endpoints were not preserved in the brief.
2. Dedicated payment/module data-graph IDs were not preserved; provider registry and source evidence were used.
3. No confirmed generic callback endpoint or callback signature verifier was found in the mandatory source set.
4. No legacy idempotency field or idempotency table was found in the inspected payment request, service, entity, repository, or controller paths.
5. PayPal REST was not treated as active behavior because it is not registered in the preserved provider map.
