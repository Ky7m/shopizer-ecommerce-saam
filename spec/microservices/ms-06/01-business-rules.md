
# Payments — Business Rules

**Version:** 1.0  
**Date:** 2026-09-01  
**Status:** In Progress — pending BA review  
**Service ID:** MS-06  
**Discovery mode:** Hybrid — CAST structural evidence plus direct Java source reading

## Scope and Ownership

MS-06 owns payment intents, provider dispatch, authorization, capture, refunds, provider references, callbacks, payment-operation idempotency, transaction history, and payment-specific audit records.

MS-06 does not own order lifecycle, order totals, carts, checkout calculation, customer identity, merchant configuration ownership, pricing, tax, shipping, or generic external-integration infrastructure.

Order state and order-total mutations observed in the legacy source are recorded as boundary violations. The target design publishes authenticated payment events for MS-05 rather than directly changing order state.

## Rule Inventory

| BR-ID | Name | Intent | Provenance |
|---|---|---|---|
| BR-ORD-014 | Store-eligible payment methods | Routing | P1 re-extraction |
| BR-ORD-015 | Transaction persistence and order association | State Transition | P1 re-extraction |
| BR-ORD-016 | Capture requires prior authorization | State Transition | P1 re-extraction |
| BR-ORD-017 | Refund amount limit | Validation | P1 re-extraction |
| BR-ORD-019 | Conditional card validation | Validation | P1 re-extraction |
| BR-EXT-001 | Configuration-driven provider dispatch | Routing | P1 re-extraction |
| BR-EXT-002 | Capture event boundary | State Transition | P1 re-extraction |
| BR-EXT-003 | Cumulative refundable balance | Validation | P1 re-extraction |
| BR-EXT-004 | Stripe classic credential and token validation | Validation | P1 re-extraction |
| BR-EXT-005 | Stripe PaymentIntent manual capture | State Transition | P1 re-extraction |
| BR-EXT-006 | Braintree environment and nonce selection | Routing | P1 re-extraction |
| BR-EXT-007 | PayPal Express credentials and token flow | Routing | P1 re-extraction |
| BR-EXT-008 | Beanstream form transaction and response validation | Integration | P1 re-extraction |
| BR-EXT-009 | Money-order local settlement | State Transition | P1 re-extraction |
| BR-UI-015 | Canonical client token and authoritative amount | Validation | P1 re-extraction |
| BR-PA-020 | Immutable payment amount and currency binding | Compliance | P4 net-new |
| BR-PA-021 | Chronological transaction-state selection | State Transition | P4 net-new |
| BR-PA-022 | Payment-operation idempotency | Compliance | P4 net-new |
| BR-PA-023 | Callback correlation and verification boundary | Validation | P4 net-new |

---

### BR-ORD-014: Store-Eligible Payment Methods

**Source Reference:**  
`initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/payments/PaymentServiceImpl.java`: `getPaymentMethods()` lines 82-96; `getPaymentMethodByCode()` lines 146-159

**Discovery Method:** Hybrid — CAST transaction paths `244093`, `244094` plus Direct Source Read  
**CAST Reference:** `PaymentServiceImpl`, CAST ID `12989`, `srcControlFlow=102`

**Statement:** A payment method is available to a store only when its configured geographic region list contains the store’s country or the wildcard region. A method not eligible for the store must not be selectable or dispatched.

**Intent:** Routing
**Classification:** Core
**Weight:** Critical

**Logic:**
- Read the registered payment-module metadata.
- For each module, include it when `module.regionsSet` contains `store.country.isoCode` or `"*"`.
- Resolve a requested method by exact module code.
- Return no method when the code is not present in the eligible set.

**Data Dependencies:**
- Reads: `merchant_store.country.iso_code`, payment-module metadata, `integration_module.code`, `integration_module.regions`
- Writes: none

**Side Effects:**
- Calls the module registry and store-context services.
- No order, payment, or provider transaction is written.

**Semantic Preservation:**

| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 2 | 2 | OK |
| Data-flow | 4 | 4 | OK |
| Constants | 1 | 1 | OK (`*`) |
| State transitions | 0 | 0 | OK |
| Outcomes | 2 | 2 | OK |
| Data writes | 0 | 0 | OK |
| Integrations | 1 | 1 | OK |
| Error paths | 0 | 1 | GAP — target rejects ineligible method explicitly |

**Preservation:** FLAGGED (error paths)

**Concrete Example:**
- API Input: `GET /api/v1/payment-methods?countryCode=CA`
- Success: `200 {"items":[{"code":"stripe","active":true},{"code":"beanstream","active":true}]}`
- Error Input: `POST /api/v1/payment-intents {"paymentMethodCode":"paypal-express-checkout","amount":"49.99","currency":"CAD","checkoutSessionId":"chk_102"}`
- Error Output: `422 {"error":"PAYMENT_METHOD_UNAVAILABLE","message":"Payment method is not available for store country CA","statusCode":422}`

---

### BR-ORD-015: Transaction Persistence and Order Association

**Source Reference:**  
`initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/payments/PaymentServiceImpl.java`: `processPayment()` lines 300-398; `initTransaction()` lines 739-782  
`initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/payments/TransactionServiceImpl.java`: `create()` lines 37-49  
`initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/order/OrderServiceImpl.java`: payment association lines 147-186

**Discovery Method:** Hybrid — CAST checkout paths `244089`, `244090`, `244093`, `244094` plus Direct Source Read  
**CAST Reference:** `PaymentServiceImpl` CAST ID `12989`; `TransactionServiceImpl` CAST ID `13115`

**Statement:** Each provider attempt must produce a durable payment transaction record containing its amount, operation type, provider reference, and result. The transaction may be associated with an order only after the order aggregate exists; payment persistence and order persistence must not be assumed to be one atomic cross-service transaction.

**Intent:** State Transition
**Classification:** Core
**Weight:** High

**Logic:**
- Provider initialization creates a transaction before an order exists.
- Authorization or authorization-plus-capture calls the provider and then invokes `transactionService.create(transaction)`.
- Checkout later assigns `transaction.order = order` and creates or updates the transaction.
- `TransactionServiceImpl.create()` serializes `transactionDetails` into `details` before persistence.
- Target replacement uses an outbox event and a payment aggregate rather than direct writes to MS-05.

**Data Dependencies:**
- Reads: `payment.amount`, `payment.transactionType`, `payment.paymentType`, `transaction.transactionDetails`
- Writes: legacy `SM_TRANSACTION.TRANSACTION_ID`, `ORDER_ID`, `AMOUNT`, `TRANSACTION_DATE`, `TRANSACTION_TYPE`, `PAYMENT_TYPE`, `DETAILS`

**Side Effects:**
- Calls the provider adapter.
- Target publishes `PaymentAuthorized`, `PaymentCaptured`, or `PaymentFailed`.
- No direct target write to order tables.

**Semantic Preservation:**

| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 6 | 5 | GAP — target removes cross-service write branches |
| Data-flow | 8 | 8 | OK |
| Constants | 1 | 1 | OK |
| State transitions | 4 | 4 | OK |
| Outcomes | 4 | 4 | OK |
| Data writes | 5 | 4 | GAP — order write intentionally moved to MS-05 |
| Integrations | 3 | 3 | OK |
| Error paths | 2 | 3 | OK — target adds persistence-recovery outcome |

**Preservation:** FLAGGED (control-flow, data writes)

**Concrete Example:**
- API Input: `POST /api/v1/payment-intents/pi_100/authorize {"amount":"49.99","currency":"USD","paymentToken":"tok_visa","idempotencyKey":"auth-pi_100-1"}`
- Success: `201 {"operationId":"op_100","paymentIntentId":"pi_100","status":"Authorized","providerReference":"ch_100"}`
- Error Input: Same request when provider authorization succeeds but local commit is unavailable
- Error Output: `202 {"operationId":"op_100","paymentIntentId":"pi_100","status":"ReconciliationRequired","error":"LOCAL_COMMIT_UNCONFIRMED"}`

---

### BR-ORD-016: Capture Requires Prior Authorization

**Source Reference:**  
`initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/payments/PaymentServiceImpl.java`: `processCapturePayment()` lines 405-472  
`initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/payments/TransactionServiceImpl.java`: `getCapturableTransaction()` lines 118-146

**Discovery Method:** Hybrid — CAST transaction `244098` plus Direct Source Read  
**CAST Reference:** `OrderPaymentApi` CAST ID `29357`; endpoint graph `244098` is stub-sized

**Statement:** A capture may be requested only for a payment intent with a successful prior authorization and no completed capture, refund, or terminal failure. The capture amount must be positive, currency-compatible, and no greater than the authorized remaining balance.

**Intent:** State Transition
**Classification:** Core
**Weight:** High

**Logic:**
- Load provider configuration for the order’s payment module.
- Find transactions for the order.
- Select an authorization transaction while scanning history.
- Stop the legacy scan when a capture or refund transaction is encountered.
- Invoke the provider capture operation using the stored provider authorization reference.
- Persist a capture transaction.
- Target publishes `PaymentCaptured`; MS-05 decides the corresponding order transition.

**Data Dependencies:**
- Reads: `payment_transaction.payment_intent_id`, `payment_transaction.operation_type`, `payment_transaction.provider_reference`, `payment_intent.authorized_amount`, `payment_intent.currency`
- Writes: `payment_transaction` capture record, `payment_operation`

**Side Effects:**
- Calls the provider capture endpoint.
- Publishes `PaymentCaptured` after the local payment commit.
- Must not set order status directly.

**Semantic Preservation:**

| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 8 | 8 | OK |
| Data-flow | 7 | 7 | OK |
| Constants | 0 | 1 | GAP — positive amount introduced as target guard |
| State transitions | 5 | 6 | OK — explicit target states |
| Outcomes | 3 | 4 | OK |
| Data writes | 4 | 3 | GAP — order history write removed |
| Integrations | 2 | 2 | OK |
| Error paths | 3 | 4 | OK |

**Preservation:** FLAGGED (constants, data writes)

**Concrete Example:**
- API Input: `POST /api/v1/payment-intents/pi_200/capture {"amount":"49.99","currency":"USD","idempotencyKey":"cap-pi_200-1"}`
- Success: `201 {"operationId":"op_200","paymentIntentId":"pi_200","status":"Captured","amount":"49.99","currency":"USD"}`
- Error Input: Capture for `pi_200` with no authorization
- Error Output: `409 {"error":"CAPTURE_NOT_ALLOWED","message":"Payment intent has no capturable authorization","statusCode":409}`

---

### BR-ORD-017: Refund Amount Limit

**Source Reference:**  
`initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/payments/PaymentServiceImpl.java`: `processRefund()` lines 474-569

**Discovery Method:** Hybrid — CAST transaction `244099` plus Direct Source Read  
**CAST Reference:** `PaymentServiceImpl` CAST ID `12989`; endpoint graph `244099` is stub-sized

**Statement:** A refund must be greater than zero and no greater than the payment’s captured amount remaining after all previously successful refunds. Refund validation uses exact currency arithmetic rather than binary floating-point comparison.

**Intent:** Validation
**Classification:** Core
**Weight:** Critical

**Logic:**
- The legacy method compares the requested amount to `order.getTotal().doubleValue()`.
- It determines `partial` by comparing the requested amount with the current order total.
- It selects a refundable transaction.
- It calls the provider refund method.
- It creates a refund transaction and mutates order-total rows and order status.
- Target computes `capturedAmount - SUM(successfulRefunds)` in minor units or fixed-precision decimal.
- Concurrent refund requests lock the payment intent and enforce the balance invariant.

**Data Dependencies:**
- Reads: `payment_intent.captured_amount`, `payment_refund.amount`, `payment_refund.status`, `payment_intent.currency`
- Writes: `payment_refund`, `payment_operation`, `payment_transaction`

**Side Effects:**
- Calls the provider refund operation.
- Publishes `PaymentRefunded` or `PaymentRefundFailed`.
- Does not write MS-05 order totals or order status.

**Semantic Preservation:**

| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 7 | 8 | OK — target adds cumulative guard |
| Data-flow | 6 | 7 | GAP — prior refunds are target data |
| Constants | 0 | 2 | GAP — zero and remaining-balance guards |
| State transitions | 3 | 4 | OK |
| Outcomes | 3 | 4 | OK |
| Data writes | 6 | 4 | GAP — order writes moved to event consumer |
| Integrations | 2 | 2 | OK |
| Error paths | 2 | 4 | OK |

**Preservation:** FLAGGED (data-flow, constants, data writes)

**Concrete Example:**
- API Input: `POST /api/v1/payment-intents/pi_300/refunds {"amount":"25.00","currency":"USD","idempotencyKey":"refund-pi_300-1"}`
- Success: `201 {"refundId":"rf_300","paymentIntentId":"pi_300","status":"Succeeded","amount":"25.00","currency":"USD"}`
- Error Input: Captured amount `100.00`, prior successful refunds `80.00`, requested refund `25.00`
- Error Output: `422 {"error":"REFUND_EXCEEDS_REMAINING_BALANCE","message":"Only 20.00 USD remains refundable","statusCode":422}`

---

### BR-ORD-019: Conditional Card Validation

**Source Reference:**  
`initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/payments/PaymentServiceImpl.java`: `processPayment()` lines 300-398; `validateCreditCard()` lines 571-597; `validateCreditCardDate()` lines 598-618; `validateCreditCardNumber()` lines 619-671; `luhnValidate()` lines 672-698

**Discovery Method:** Direct Source Read with CAST hotspot `validateCreditCardNumber`, recorded complexity `22`

**Statement:** Card-number validation is applied only when the card-validation feature is enabled. When applied, the supplied card number must contain only permitted separators, match the selected card-type length and prefix rules, pass expiry validation, and pass the Luhn checksum.

**Intent:** Validation
**Classification:** Active
**Weight:** Medium

**Logic:**
- If `coreConfiguration.getProperty("VALIDATE_CREDIT_CARD")` equals `"true"`, validate the card.
- Parse expiration month and year as integers.
- Reject blank card number.
- Reject characters outside digits, whitespace, period, and hyphen.
- Remove whitespace, periods, and hyphens.
- Reject an expiration year before the current year or the current month already elapsed.
- Apply type-specific rules:
  - MasterCard: length 16 and prefix 51–55.
  - Visa: length 13 or 16 and prefix 4.
  - Amex: length 15 and prefix 34 or 37.
  - Diners: length 14 with the source prefix conditions.
  - Discover: length 16 and prefix 6011.
- Apply Luhn checksum.

**Data Dependencies:**
- Reads: payment feature configuration, `credit_card.number`, `credit_card.expiration_month`, `credit_card.expiration_year`, `credit_card.type`
- Writes: none

**Side Effects:**
- Rejects the payment before provider dispatch.
- Target must prefer tokenized card data and must not persist PAN or CVV.

**Semantic Preservation:**

| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 15 | 15 | OK |
| Data-flow | 5 | 5 | OK |
| Constants | 17 | 17 | OK |
| State transitions | 0 | 0 | OK |
| Outcomes | 6 | 6 | OK |
| Data writes | 0 | 0 | OK |
| Integrations | 0 | 0 | OK |
| Error paths | 8 | 8 | OK |

**Preservation:** OK

**Concrete Example:**
- API Input: `POST /api/v1/payment-intents {"paymentMethodCode":"stripe","amount":"19.95","currency":"USD","card":{"type":"Visa","number":"4242-4242-4242-4242","expirationMonth":12,"expirationYear":2028},"checkoutSessionId":"chk_401"}`
- Success: `201 {"paymentIntentId":"pi_401","status":"Created","amount":"19.95","currency":"USD"}`
- Error Input: Same request with `expirationMonth=1` and `expirationYear=2024`
- Error Output: `422 {"error":"CARD_EXPIRED","message":"Card expiration date is not valid","statusCode":422}`

---

### BR-EXT-001: Configuration-Driven Provider Dispatch

**Source Reference:**  
`initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/payments/PaymentServiceImpl.java`: `getPaymentModulesConfigured()` lines 185-207; `processPayment()` lines 300-398; `initTransaction()` lines 699-782  
`initial-source/shopizer-3.2.7/sm-core/src/main/resources/spring/shopizer-core-modules.xml`: lines 47-61, 82-101

**Discovery Method:** Hybrid — CAST provider component records plus Direct Source Read  
**CAST Reference:** `PaymentServiceImpl` CAST ID `12989`; provider registry resource

**Statement:** Provider execution is selected by the store’s active payment configuration and the registered provider code. A missing, inactive, invalid, or unregistered provider configuration prevents the payment operation.

**Intent:** Routing
**Classification:** Core
**Weight:** High

**Logic:**
- Read the encrypted store payment-module configuration.
- Decrypt and deserialize the configuration map.
- Resolve `configuration = modules.get(payment.getModuleName())`.
- Reject missing configuration or inactive configuration.
- Read the configured transaction mode; default to `AUTHORIZECAPTURE` when absent or invalid.
- Resolve the registered `PaymentModule`.
- Dispatch to `authorize`, `authorizeAndCapture`, or `initTransaction`.
- Target pins the selected configuration version to the payment intent.

**Data Dependencies:**
- Reads: `merchant_configuration.payment_modules`, `integration_configuration.module_code`, `integration_configuration.active`, `integration_configuration.environment`, `integration_configuration.integration_keys.transaction`
- Writes: target `payment_intent.provider_config_version`, `payment_operation.provider_code`

**Side Effects:**
- Calls MS-11 configuration/secret reference service.
- Calls one provider adapter.
- Never stores decrypted credentials in payment transactions.

**Semantic Preservation:**

| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 11 | 12 | OK |
| Data-flow | 8 | 8 | OK |
| Constants | 2 | 2 | OK |
| State transitions | 3 | 4 | OK |
| Outcomes | 5 | 6 | OK |
| Data writes | 3 | 3 | OK |
| Integrations | 4 | 4 | OK |
| Error paths | 5 | 6 | OK |

**Preservation:** OK

**Concrete Example:**
- API Input: `POST /api/v1/payment-intents {"paymentMethodCode":"stripe3","amount":"75.00","currency":"EUR","checkoutSessionId":"chk_501"}`
- Success: `201 {"paymentIntentId":"pi_501","providerCode":"stripe3","providerConfigVersion":12,"status":"Created"}`
- Error Input: Same request when `stripe3` is configured but inactive
- Error Output: `409 {"error":"PAYMENT_METHOD_INACTIVE","message":"The selected payment method is inactive for this store","statusCode":409}`

---

### BR-EXT-002: Capture Event Boundary

**Source Reference:**  
`initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/payments/PaymentServiceImpl.java`: `processCapturePayment()` lines 405-472  
`initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/controller/order/facade/OrderFacadeImpl.java`: `captureOrder()` lines 1416-1429

**Discovery Method:** Hybrid — CAST transaction `244098` plus Direct Source Read

**Statement:** A successful capture changes payment state and emits an authenticated payment event; it does not directly change the order’s lifecycle state. MS-05 remains the sole owner of order transitions.

**Intent:** State Transition
**Classification:** Core
**Weight:** High

**Logic:**
- Legacy code calls `orderService.addOrderStatusHistory()` and sets `order.status = PROCESSED`.
- Target removes those writes from MS-06.
- After a provider-confirmed capture is durably committed, publish `PaymentCaptured` with payment intent ID, order ID, amount, currency, provider reference, and event ID.
- MS-05 consumes the event idempotently and applies its own order transition.

**Data Dependencies:**
- Reads: `payment_intent.order_id`, `payment_intent.currency`, capture operation, authorization provider reference
- Writes: `payment_transaction`, `payment_operation`, `payment_outbox`

**Side Effects:**
- Publishes `PaymentCaptured`.
- Does not write `orders`, `order_total`, or `order_status_history`.

**Semantic Preservation:**

| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 6 | 5 | GAP — direct order mutation removed |
| Data-flow | 6 | 6 | OK |
| Constants | 1 | 1 | OK |
| State transitions | 4 | 4 | OK |
| Outcomes | 3 | 4 | OK |
| Data writes | 5 | 3 | GAP — cross-service writes removed |
| Integrations | 2 | 3 | OK — event added |
| Error paths | 2 | 3 | OK |

**Preservation:** FLAGGED (control-flow, data writes)

**Concrete Example:**
- API Input: `POST /api/v1/payment-intents/pi_600/capture {"amount":"100.00","currency":"USD","idempotencyKey":"cap-pi_600-1"}`
- Success: `201 {"operationId":"op_600","status":"Captured"}` and event `PaymentCaptured` is published.
- Error Input: Provider returns a declined capture response
- Error Output: `402 {"error":"PAYMENT_CAPTURE_DECLINED","message":"The provider declined the capture","statusCode":402}`; no `PaymentCaptured` event is published.

---

### BR-EXT-003: Cumulative Refundable Balance

**Source Reference:**  
`initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/payments/PaymentServiceImpl.java`: `processRefund()` lines 474-569  
`initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/payments/TransactionServiceImpl.java`: `getRefundableTransaction()` lines 148-200

**Discovery Method:** Hybrid — CAST transaction `244099` plus Direct Source Read

**Statement:** The sum of successful full and partial refunds for a payment must never exceed the amount successfully captured for that payment. A refund operation must reserve its amount before contacting the provider.

**Intent:** Validation
**Classification:** Core
**Weight:** Critical

**Logic:**
- Legacy selection stores the latest `AUTHORIZECAPTURE` or `CAPTURE` transaction and the latest refund by transaction date.
- It does not calculate a cumulative refund balance.
- Target locks the payment intent.
- Compute `remaining = capturedAmount - successfulRefundAmount - reservedRefundAmount`.
- Reject when requested refund exceeds `remaining`.
- Reserve the amount, call the provider, then mark the reservation succeeded or released.

**Data Dependencies:**
- Reads: `payment_transaction.operation_type`, `payment_transaction.amount`, `payment_refund.status`, `payment_refund.amount`
- Writes: `payment_refund.status`, `payment_operation.status`

**Side Effects:**
- Provider refund call.
- Publishes `PaymentRefunded` only once for a successful operation.

**Semantic Preservation:**

| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 9 | 10 | OK |
| Data-flow | 8 | 9 | GAP — reservation balance added |
| Constants | 0 | 2 | GAP |
| State transitions | 4 | 5 | OK |
| Outcomes | 3 | 5 | OK |
| Data writes | 3 | 3 | OK |
| Integrations | 1 | 2 | OK |
| Error paths | 2 | 4 | OK |

**Preservation:** FLAGGED (data-flow, constants)

**Concrete Example:**
- API Input: `POST /api/v1/payment-intents/pi_601/refunds {"amount":"20.00","currency":"USD","idempotencyKey":"refund-pi_601-2"}`
- Success: `201 {"refundId":"rf_601","status":"Succeeded","remainingRefundableAmount":"30.00"}`
- Error Input: Captured `50.00`, successful refunds `35.00`, pending refund reservations `10.00`, requested `10.00`
- Error Output: `409 {"error":"REFUND_BALANCE_RESERVED","message":"Only 5.00 USD remains available for refund","statusCode":409}`

---

### BR-EXT-004: Stripe Classic Credential and Token Validation

**Source Reference:**  
`initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/modules/integration/payment/impl/StripePayment.java`: `validateModuleConfiguration()` lines 50-87; `authorize()` lines 108-187; `authorizeAndCapture()` lines 252-331; `refund()` lines 333-397

**Discovery Method:** Direct Source Read  
**CAST Reference:** `StripePayment` CAST ID `11679`, `srcControlFlow=43`

**Statement:** Stripe classic operations require configured secret credentials and a client payment token. Missing credentials or tokens prevent gateway access; provider declines and validation failures are translated into stable payment error categories.

**Intent:** Validation
**Classification:** Core
**Weight:** Critical

**Logic:**
- Require `secretKey` and `publishableKey` during module validation.
- For authorization require payment metadata and `stripe_token`.
- For authorization-plus-capture accept `stripe_token`, then `paymentToken` as fallback.
- Convert the amount to a provider minor-unit string by removing the decimal separator.
- Call the provider with `capture=false` for authorization or `capture=true` for sale.
- Map `card_declined` to payment-declined.
- Map invalid number, expiry, CVC, or incorrect number/CVC to validation.
- Map other provider exceptions to transaction failure.

**Data Dependencies:**
- Reads: provider configuration keys, payment metadata token, store currency, payment amount
- Writes: `payment_transaction.provider_reference`, `provider_status`, `provider_operation_id`

**Side Effects:**
- Calls Stripe classic charge and refund APIs.
- Logs provider errors; target must redact tokens and credentials.

**Semantic Preservation:**

| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 14 | 14 | OK |
| Data-flow | 6 | 6 | OK |
| Constants | 8 | 8 | OK |
| State transitions | 3 | 3 | OK |
| Outcomes | 7 | 7 | OK |
| Data writes | 3 | 3 | OK |
| Integrations | 4 | 4 | OK |
| Error paths | 10 | 10 | OK |

**Preservation:** OK

**Concrete Example:**
- API Input: `POST /api/v1/payment-intents/pi_700/authorize {"amount":"12.50","currency":"USD","paymentToken":"tok_visa","idempotencyKey":"auth-pi_700-1"}`
- Success: `201 {"operationId":"op_700","providerCode":"stripe","status":"Authorized","providerReference":"ch_700"}`
- Error Input: Same request with `"paymentToken":""`
- Error Output: `422 {"error":"PAYMENT_TOKEN_REQUIRED","message":"A payment token is required for Stripe authorization","statusCode":422}`

---

### BR-EXT-005: Stripe PaymentIntent Manual Capture

**Source Reference:**  
`initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/modules/integration/payment/impl/Stripe3Payment.java`: `initTransaction()` lines 52-106; `authorize()` lines 145-205; `capture()` lines 207-276; `authorizeAndCapture()` lines 278-374; `refund()` lines 376-442

**Discovery Method:** Direct Source Read  
**CAST Reference:** `Stripe3Payment` CAST ID `11710`, `srcControlFlow=47`

**Statement:** Stripe 3 initialization creates a PaymentIntent using the store currency and amount in minor units with manual capture. Authorization retrieves the client-supplied PaymentIntent and capture acts on that intent; the target must validate status, amount, currency, and provider reference before transitioning payment state.

**Intent:** State Transition
**Classification:** Core
**Weight:** Critical

**Logic:**
- Require `secretKey` and `publishableKey`.
- Convert the decimal amount by removing the decimal separator.
- Create a PaymentIntent with currency, minor-unit amount, and manual capture.
- Store intent ID and client secret in the initialization result.
- For authorization retrieve the `stripe_token` PaymentIntent.
- For capture retrieve `TRNORDERNUMBER`, set amount to capture, and call `paymentIntent.capture()`.
- For authorization-plus-capture the source retrieves the token and calls capture.
- For refund retrieve the stored provider intent and create a refund.
- The source records refund output with `TransactionType.CAPTURE`; target normalizes it to `Refunded` or `PartiallyRefunded` based on operation state.

**Data Dependencies:**
- Reads: provider keys, `payment.amount`, `payment.currency`, PaymentIntent ID, PaymentIntent status
- Writes: `payment_transaction.provider_reference`, `provider_status`, `client_secret`

**Side Effects:**
- Calls Stripe PaymentIntent create, retrieve, capture, and refund APIs.
- No external Stripe webhook behavior is assumed beyond a target callback adapter contract.

**Semantic Preservation:**

| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 18 | 18 | OK |
| Data-flow | 8 | 9 | GAP — explicit amount/currency verification |
| Constants | 6 | 7 | GAP |
| State transitions | 6 | 7 | OK |
| Outcomes | 6 | 7 | OK |
| Data writes | 4 | 4 | OK |
| Integrations | 5 | 5 | OK |
| Error paths | 8 | 9 | OK |

**Preservation:** FLAGGED (data-flow, constants)

**Concrete Example:**
- API Input: `POST /api/v1/payment-intents {"paymentMethodCode":"stripe3","amount":"42.75","currency":"USD","checkoutSessionId":"chk_701"}`
- Success: `201 {"paymentIntentId":"pi_701","providerCode":"stripe3","status":"Created","clientSecret":"set-by-provider"}`
- Error Input: `POST /api/v1/payment-intents/pi_701/capture {"amount":"45.75","currency":"USD","idempotencyKey":"cap-pi_701-1"}`
- Error Output: `409 {"error":"PAYMENT_AMOUNT_MISMATCH","message":"Capture amount does not match the authorized payment balance","statusCode":409}`

---

### BR-EXT-006: Braintree Environment and Nonce Selection

**Source Reference:**  
`initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/modules/integration/payment/impl/BraintreePayment.java`: `validateModuleConfiguration()` lines 30-78; `initTransaction()` lines 80-117; `authorize()` lines 119-210; `capture()` lines 212-295; `authorizeAndCapture()` lines 297-386; `refund()` lines 387-469

**Discovery Method:** Direct Source Read  
**CAST Reference:** `BraintreePayment` CAST ID `11668`, `srcControlFlow=39`

**Statement:** Braintree uses sandbox credentials when the configured environment is test and production credentials otherwise. Authorization and sale operations require a client payment nonce, and provider validation failures or missing transaction identifiers prevent success.

**Intent:** Routing
**Classification:** Active
**Weight:** Medium

**Logic:**
- Require merchant ID, public key, private key, and tokenization key during configuration validation.
- Select `Environment.SANDBOX` when environment equals `"TEST"`; otherwise select production.
- Generate a client token during initialization.
- Read `paymentToken` as the payment nonce.
- Submit a sale for authorization or authorization-plus-capture.
- Submit a settlement using the stored authorization reference.
- Refund using the stored transaction reference and amount.
- On unsuccessful result, concatenate provider validation errors into an integration failure.
- Reject a successful provider result that contains no transaction ID.

**Data Dependencies:**
- Reads: provider credentials, environment, payment nonce, amount, order currency
- Writes: payment transaction provider reference and provider status

**Side Effects:**
- Calls Braintree client-token, sale, settlement, and refund APIs.
- Target stores only a secret reference, never raw credentials.

**Semantic Preservation:**

| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 19 | 19 | OK |
| Data-flow | 7 | 7 | OK |
| Constants | 5 | 5 | OK |
| State transitions | 5 | 5 | OK |
| Outcomes | 6 | 6 | OK |
| Data writes | 4 | 4 | OK |
| Integrations | 5 | 5 | OK |
| Error paths | 9 | 9 | OK |

**Preservation:** OK

**Concrete Example:**
- API Input: `POST /api/v1/payment-intents/pi_800/authorize {"amount":"30.00","currency":"USD","paymentToken":"fake-valid-nonce","idempotencyKey":"auth-pi_800-1"}`
- Success: `201 {"operationId":"op_800","providerCode":"braintree","status":"Authorized","providerReference":"bt_800"}`
- Error Input: Same request with no `paymentToken`
- Error Output: `422 {"error":"PAYMENT_NONCE_REQUIRED","message":"A Braintree payment nonce is required","statusCode":422}`

---

### BR-EXT-007: PayPal Express Credentials and Token Flow

**Source Reference:**  
`initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/modules/integration/payment/impl/PayPalExpressCheckoutPayment.java`: `validateModuleConfiguration()` lines 72-111; `initTransaction()` lines 113-120; `authorize()` lines 122-135; `initPaypalTransaction()` lines 149-325; `authorizeAndCapture()` lines 327-339; `refund()` lines 341-433; `processTransaction()` lines 434-558; `capture()` lines 559-670

**Discovery Method:** Direct Source Read  
**CAST Reference:** `PayPalExpressCheckoutPayment` CAST ID `11688`, `srcControlFlow=44`

**Statement:** PayPal Express requires configured API credentials and an Express Checkout token. Initialization creates a redirect transaction from the immutable checkout amount, currency, line items, tax, shipping, and return/cancel URLs; completion retrieves payer details and commits either authorization or sale according to configured transaction mode.

**Intent:** Routing
**Classification:** Core
**Weight:** Critical

**Logic:**
- Require `api`, `username`, and `signature`.
- The direct SPI `initTransaction()` throws “not implemented”; the separate `initPaypalTransaction()` constructs the Express request.
- Select `SALE` for authorization-plus-capture configuration; otherwise select `AUTHORIZATION`.
- Populate line items, shipping, handling, tax, item total, order total, currency, return URL, and cancel URL.
- Select sandbox unless environment equals production.
- Call SetExpressCheckout; require acknowledgement `"Success"`; return token and correlation ID.
- Completion calls GetExpressCheckoutDetails, requires successful acknowledgement, obtains payer ID, and calls DoExpressCheckoutPayment.
- Require successful commit acknowledgement and store transaction ID, token, payer ID, and correlation ID.
- Capture uses the stored authorization ID and order total.
- Refund requires the stored transaction ID and returns the provider refund transaction ID.

**Data Dependencies:**
- Reads: provider credentials, environment, checkout line items, summary subtotal, summary total, shipping, handling, tax, payment currency, PayPal token
- Writes: payment transaction provider token, payer reference, transaction reference, correlation ID

**Side Effects:**
- Calls PayPal SetExpressCheckout, GetExpressCheckoutDetails, DoExpressCheckoutPayment, capture, and refund operations.
- The target does not assume behavior for the unregistered PayPal REST adapter.

**Semantic Preservation:**

| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 25 | 25 | OK |
| Data-flow | 18 | 18 | OK |
| Constants | 11 | 11 | OK |
| State transitions | 6 | 6 | OK |
| Outcomes | 7 | 7 | OK |
| Data writes | 5 | 5 | OK |
| Integrations | 6 | 6 | OK |
| Error paths | 9 | 10 | GAP — target adds token-expiry outcome |

**Preservation:** FLAGGED (error paths)

**Concrete Example:**
- API Input: `POST /api/v1/payment-intents {"paymentMethodCode":"paypal-express-checkout","amount":"89.00","currency":"USD","checkoutSessionId":"chk_900"}`
- Success: `201 {"paymentIntentId":"pi_900","status":"RequiresAction","redirectUrl":"provided-by-paypal-adapter","providerToken":"redacted"}`
- Error Input: `POST /api/v1/payment-intents/pi_900/authorize {"paymentToken":"","payerId":"","amount":"89.00","currency":"USD","idempotencyKey":"auth-pi_900-1"}`
- Error Output: `422 {"error":"PAYPAL_TOKEN_REQUIRED","message":"A PayPal Express token and payer reference are required","statusCode":422}`

---

### BR-EXT-008: Beanstream Form Transaction and Response Validation

**Source Reference:**  
`initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/modules/integration/payment/impl/BeanStreamPayment.java`: `authorize()` lines 67-77; `capture()` lines 79-127; `authorizeAndCapture()` lines 129-142; `refund()` lines 144-239; `sendTransaction()` lines 240-459; `processTransaction()` lines 460-649; `parseResponse()` lines 650-688; `validateModuleConfiguration()` lines 689-738

**Discovery Method:** Direct Source Read  
**CAST Reference:** `BeanStreamPayment` CAST ID `11652`, `srcControlFlow=61`

**Statement:** Beanstream operations select the configured test or production endpoint, submit form-encoded payment data, require an approval field in the response, and reject declined or structurally invalid responses. Sensitive payment data must never appear in diagnostic logs.

**Intent:** Integration
**Classification:** Active
**Weight:** Medium

**Logic:**
- Require merchant ID, username, and password.
- Select module endpoint `TEST` or `PROD` according to environment.
- Build form parameters for purchase, preauthorization, capture, or refund.
- Format amount using the store’s payment formatting utility.
- Submit `application/x-www-form-urlencoded` over HTTP POST.
- Read the response code and body.
- Decode key-value response fields to uppercase keys.
- Require `TRNAPPROVED`.
- If approval equals `"0"`, log a masked diagnostic entry and raise payment-declined.
- Build transaction details from transaction ID, approval, order number, and message text.
- Target uses a provider client with timeout, redaction, and allowlisted response fields.

**Data Dependencies:**
- Reads: provider credentials, endpoint configuration, card token/card fields, customer billing context, amount, currency
- Writes: payment transaction provider reference and provider response status

**Side Effects:**
- Calls the configured Beanstream endpoint.
- Legacy logging includes a request construction path; target must exclude PAN, CVV, password, and credential values.

**Semantic Preservation:**

| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 22 | 22 | OK |
| Data-flow | 16 | 16 | OK |
| Constants | 12 | 12 | OK |
| State transitions | 5 | 5 | OK |
| Outcomes | 6 | 6 | OK |
| Data writes | 3 | 3 | OK |
| Integrations | 5 | 5 | OK |
| Error paths | 10 | 11 | GAP — explicit timeout/redaction handling |

**Preservation:** FLAGGED (error paths)

**Concrete Example:**
- API Input: `POST /api/v1/payment-intents/pi_1000/authorize {"amount":"77.01","currency":"CAD","paymentToken":"tok_beanstream_1000","idempotencyKey":"auth-pi_1000-1"}`
- Success: `201 {"operationId":"op_1000","status":"Authorized","providerReference":"bs_1000"}`
- Error Input: Same request when provider response lacks its approval field
- Error Output: `502 {"error":"PROVIDER_RESPONSE_INVALID","message":"Beanstream response did not contain an approval result","statusCode":502}`

---

### BR-EXT-009: Money-Order Local Settlement

**Source Reference:**  
`initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/modules/integration/payment/impl/MoneyOrderPayment.java`: `validateModuleConfiguration()` lines 26-47; `authorizeAndCapture()` lines 76-94; `refund()` lines 96-102; `capture()` lines 104-111

**Discovery Method:** Direct Source Read  
**CAST Reference:** `MoneyOrderPayment` CAST ID `11678`, `srcControlFlow=3`

**Statement:** Money-order payment creates a local authorization-and-capture transaction without contacting an external payment provider. A configured remittance address is required, while separate capture and refund operations are unsupported by the legacy adapter.

**Intent:** State Transition
**Classification:** Core
**Weight:** High

**Logic:**
- Require integration key `address`.
- `initTransaction()` and `authorize()` return no transaction.
- `authorizeAndCapture()` creates a transaction with amount, current date, `AUTHORIZECAPTURE`, and `MONEYORDER`.
- `refund()` raises transaction-not-supported.
- `capture()` returns no transaction.
- Target represents manual settlement as `Captured` only if BA approves immediate local recognition; otherwise it uses `PendingManualSettlement`.

**Data Dependencies:**
- Reads: money-order configuration address, payment amount
- Writes: local payment transaction

**Side Effects:**
- No external provider call.
- Publishes either `PaymentCaptured` or `PaymentPendingManualSettlement` according to approved target policy.

**Semantic Preservation:**

| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 3 | 4 | GAP — target makes policy explicit |
| Data-flow | 2 | 2 | OK |
| Constants | 2 | 3 | GAP |
| State transitions | 2 | 3 | OK |
| Outcomes | 3 | 4 | OK |
| Data writes | 1 | 1 | OK |
| Integrations | 0 | 0 | OK |
| Error paths | 2 | 3 | OK |

**Preservation:** FLAGGED (control-flow, constants)

**Concrete Example:**
- API Input: `POST /api/v1/payment-intents {"paymentMethodCode":"moneyorder","amount":"125.00","currency":"USD","checkoutSessionId":"chk_1100"}`
- Success: `201 {"paymentIntentId":"pi_1100","status":"PendingManualSettlement","amount":"125.00","currency":"USD"}`
- Error Input: Configuration without a remittance address
- Error Output: `422 {"error":"PAYMENT_CONFIGURATION_INVALID","message":"Money-order remittance address is required","statusCode":422}`

---

### BR-UI-015: Canonical Client Token and Authoritative Amount

**Source Reference:**  
`initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/populator/order/transaction/PersistablePaymentPopulator.java`: lines 20-52  
`initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/controller/order/facade/OrderFacadeImpl.java`: lines 457-552  
Recorded UI evidence: `initial-source/shopizer-shop-reactjs-main/src/pages/other/Checkout.js`: lines 198-225, 508-625

**Discovery Method:** Hybrid — CAST checkout paths plus Direct Source Read

**Statement:** A payment request must contain one canonical provider token field and must use the server-issued amount and currency snapshot associated with the checkout session. Client-provided display amounts are not authoritative.

**Intent:** Validation
**Classification:** Core
**Weight:** Critical

**Logic:**
- The payment populator converts the request amount and maps `paymentToken` into payment metadata.
- Checkout constructs payment data from `paymentToken`, payment module, payment type, amount, and currency.
- Stripe paths accept either `stripe_token` or `paymentToken`, creating an ambiguity.
- Target accepts `paymentToken` only, stores a token fingerprint, and resolves authoritative amount/currency from the payment intent.
- Reject missing, conflicting, or mismatched token fields.

**Data Dependencies:**
- Reads: `checkout_session.amount_snapshot`, `checkout_session.currency`, request payment token, payment method code
- Writes: token fingerprint and payment intent linkage

**Side Effects:**
- Calls provider adapter with the canonical token.
- Does not trust a client amount for authorization.

**Semantic Preservation:**

| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 7 | 8 | OK |
| Data-flow | 7 | 8 | GAP — target snapshot |
| Constants | 0 | 1 | GAP |
| State transitions | 1 | 2 | OK |
| Outcomes | 3 | 4 | OK |
| Data writes | 2 | 2 | OK |
| Integrations | 2 | 2 | OK |
| Error paths | 3 | 5 | OK |

**Preservation:** FLAGGED (data-flow, constants)

**Concrete Example:**
- API Input: `POST /api/v1/payment-intents/pi_1200/authorize {"paymentToken":"tok_1200","amount":"999.00","currency":"USD","idempotencyKey":"auth-pi_1200-1"}`
- Success: `201 {"operationId":"op_1200","status":"Authorized","amount":"49.99","currency":"USD"}` when the server snapshot is `49.99 USD`.
- Error Input: Same request where the server snapshot is `49.99 USD` and provider token fields contain both `paymentToken` and `stripeToken`
- Error Output: `422 {"error":"AMBIGUOUS_PAYMENT_TOKEN","message":"Exactly one canonical paymentToken is permitted","statusCode":422}`

---

### BR-PA-020: Immutable Payment Amount and Currency Binding

**Source Reference:**  
`initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/payments/PaymentServiceImpl.java`: `processPayment()` lines 300-398; `initTransaction()` lines 699-782  
`initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/controller/order/facade/OrderFacadeImpl.java`: lines 457-489

**Discovery Method:** Direct Source Read — P4 net-new finding from comparing checkout amount construction with provider dispatch

**Statement:** Once a payment intent is created, its amount and currency are immutable. Authorization, capture, and refund operations must use compatible amounts and the same currency, and a stale checkout snapshot must be rejected rather than silently recalculated by MS-06.

**Intent:** Compliance
**Classification:** Core
**Weight:** Critical

**Logic:**
- Legacy payment processing obtains amount from `order.getTotal()`.
- Initialization can obtain amount from the request or order context.
- Target stores `amount`, `currency`, and upstream snapshot version on creation.
- Authorization verifies provider amount and currency against the intent.
- Capture cannot exceed authorized remaining amount.
- Refund cannot exceed captured remaining balance.
- MS-06 does not recalculate product, tax, shipping, or promotion values.

**Data Dependencies:**
- Reads: `payment_intent.amount`, `payment_intent.currency`, `payment_intent.amount_snapshot_version`, operation request
- Writes: payment intent and operation records

**Side Effects:**
- Publishes `PaymentFailed` for stale or mismatched snapshots.
- Calls no pricing, tax, or shipping calculation service.

**Semantic Preservation:**

| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 5 | 7 | OK |
| Data-flow | 5 | 7 | GAP |
| Constants | 0 | 2 | GAP |
| State transitions | 2 | 3 | OK |
| Outcomes | 2 | 4 | OK |
| Data writes | 2 | 2 | OK |
| Integrations | 1 | 0 | GAP — target removes recalculation dependency |
| Error paths | 2 | 4 | OK |

**Preservation:** FLAGGED (data-flow, constants, integrations)

**Concrete Example:**
- API Input: `POST /api/v1/payment-intents/pi_1300/authorize {"amount":"40.00","currency":"EUR","idempotencyKey":"auth-pi_1300-1"}`
- Success: `201 {"operationId":"op_1300","status":"Authorized","amount":"40.00","currency":"EUR"}`
- Error Input: Intent amount `40.00 EUR`, request amount `40.00 USD`
- Error Output: `409 {"error":"PAYMENT_CURRENCY_MISMATCH","message":"Operation currency must match payment intent currency","statusCode":409}`

---

### BR-PA-021: Chronological Transaction-State Selection

**Source Reference:**  
`initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/payments/TransactionServiceImpl.java`: `lastTransaction()` lines 86-116; `getCapturableTransaction()` lines 118-146; `getRefundableTransaction()` lines 148-200  
`initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/repositories/payments/TransactionRepository.java`: lines 13-21

**Discovery Method:** Direct Source Read — P4 net-new algorithm finding

**Statement:** Payment state must be derived from operation sequence and provider-confirmed timestamps, not from lexicographic ordering of operation-type names. A transaction history query must produce deterministic chronological results.

**Intent:** State Transition
**Classification:** Core
**Weight:** High

**Logic:**
- Legacy `lastTransaction()` collects transactions into a `TreeMap` keyed by transaction-type name and returns `map.lastEntry()`.
- The source contains a TODO indicating ordering by date was not implemented.
- `findByOrder()` has no explicit ordering clause.
- Target orders by committed sequence or transaction timestamp plus ID.
- State reduction processes every operation in sequence and rejects impossible transitions.
- Capturable selection requires an authorized operation with no later successful capture/refund.
- Refundable selection uses the captured balance, not the last matching row.

**Data Dependencies:**
- Reads: `payment_transaction.operation_type`, `payment_transaction.status`, `payment_transaction.occurred_at`, `payment_transaction.sequence_no`
- Writes: none during read; reconciliation record on invalid history

**Side Effects:**
- May create `ReconciliationRequired` state for contradictory history.
- No provider call for ordinary history reads.

**Semantic Preservation:**

| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 7 | 8 | OK |
| Data-flow | 5 | 6 | GAP |
| Constants | 0 | 1 | GAP |
| State transitions | 5 | 7 | GAP — explicit reduction |
| Outcomes | 3 | 4 | OK |
| Data writes | 0 | 1 | GAP |
| Integrations | 0 | 0 | OK |
| Error paths | 1 | 3 | OK |

**Preservation:** FLAGGED (data-flow, constants, state transitions, data writes)

**Concrete Example:**
- API Input: `GET /api/v1/payment-intents/pi_1400/transactions`
- Success: `200 {"items":[{"operationType":"Authorized","occurredAt":"2026-09-01T10:00:00Z"},{"operationType":"Captured","occurredAt":"2026-09-01T10:02:00Z"}]}`
- Error Input: History containing `Refunded` before `Captured`
- Error Output: `409 {"error":"PAYMENT_HISTORY_INVALID","message":"Payment operation sequence cannot transition from Refunded to Captured","statusCode":409}`

---

### BR-PA-022: Payment-Operation Idempotency

**Source Reference:**  
`initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/payments/PaymentServiceImpl.java`: `processPayment()` lines 300-398; `processCapturePayment()` lines 405-472; `processRefund()` lines 474-569  
`initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/api/v1/order/OrderPaymentApi.java`: lines 292-360

**Discovery Method:** Direct Source Read — P4 net-new absence finding

**Statement:** Every payment-mutating command must carry an idempotency key scoped to tenant, store, payment intent, and operation type. A repeated request with the same fingerprint returns the original result; reuse of a key with different parameters is rejected.

**Intent:** Compliance
**Classification:** Core
**Weight:** Critical

**Logic:**
- No idempotency-key field or dedicated idempotency table is present in the inspected legacy request, service, entity, or controller paths.
- Legacy provider calls can therefore be repeated by retrying checkout, capture, or refund.
- Target persists the key, request fingerprint, operation ID, provider attempt ID, and normalized result.
- A duplicate matching fingerprint returns the stored result without another provider call.
- A conflicting fingerprint returns `409`.
- Unique constraint prevents concurrent duplicate operations.

**Data Dependencies:**
- Reads: `payment_idempotency.scope`, `idempotency_key`, `request_fingerprint`, `operation_id`
- Writes: `payment_idempotency`, `payment_operation`

**Side Effects:**
- Prevents duplicate provider calls.
- Supports safe client retries.

**Semantic Preservation:**

| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 1 | 5 | GAP — mechanism absent in source |
| Data-flow | 2 | 5 | GAP |
| Constants | 0 | 2 | GAP |
| State transitions | 1 | 3 | GAP |
| Outcomes | 2 | 4 | OK |
| Data writes | 1 | 3 | GAP |
| Integrations | 1 | 1 | OK |
| Error paths | 1 | 3 | OK |

**Preservation:** FLAGGED (legacy absence; target addition)

**Concrete Example:**
- API Input: `POST /api/v1/payment-intents/pi_1500/refunds {"amount":"10.00","currency":"USD","idempotencyKey":"refund-pi_1500-1"}`
- Success: First request `201 {"refundId":"rf_1500","status":"Succeeded"}`; retry returns the same `201` body without a second provider request.
- Error Input: Reuse `refund-pi_1500-1` with amount `11.00`
- Error Output: `409 {"error":"IDEMPOTENCY_KEY_REUSED","message":"The idempotency key was previously used with different parameters","statusCode":409}`

---

### BR-PA-023: Callback Correlation and Verification Boundary

**Source Reference:**  
`initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/api/v1/order/OrderPaymentApi.java`: lines 88-180, 292-360  
`initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/modules/integration/payment/impl/StripePayment.java`: lines 398-575  
`initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/modules/integration/payment/impl/Stripe3Payment.java`: lines 443-621  
`initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/modules/integration/payment/impl/PayPalExpressCheckoutPayment.java`: lines 149-325

**Discovery Method:** Direct Source Read — P4 net-new boundary finding  
**CAST Reference:** Checkout/payment initialization paths `244093`, `244094`; no confirmed callback endpoint in preserved application paths

**Statement:** A provider callback may change payment state only after its provider-specific adapter verifies the callback and correlates it to one payment intent and one provider reference. An unverified, duplicate, unknown, or ambiguous callback is recorded without changing payment state.

**Intent:** Validation
**Classification:** Active
**Weight:** Medium

**Logic:**
- No confirmed generic callback controller or callback signature-verification path was found in the mandatory source set.
- Target receives provider-specific callback payloads through `/callbacks/{provider}`.
- Store the raw payload only in protected callback storage subject to retention and redaction rules.
- Resolve the provider adapter using the path provider code.
- Verify the callback using provider-specific configuration; do not invent a common signature algorithm.
- Require a provider event ID when the provider supplies one; otherwise derive a deterministic fingerprint.
- Correlate provider reference to exactly one payment intent.
- Record duplicate callbacks as `Duplicate`; do not reapply transitions.
- Publish an internal payment event only after successful verification and valid state transition.

**Data Dependencies:**
- Reads: callback provider code, provider event ID, provider reference, payment intent reference
- Writes: `payment_callback`, `payment_transaction`, `payment_outbox`

**Side Effects:**
- May publish `PaymentAuthorized`, `PaymentCaptured`, `PaymentRefunded`, or `PaymentFailed`.
- Does not directly notify or mutate orders.

**Semantic Preservation:**

| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 0 | 8 | GAP — callback path absent in source |
| Data-flow | 1 | 7 | GAP |
| Constants | 0 | 1 | GAP |
| State transitions | 0 | 5 | GAP |
| Outcomes | 1 | 5 | GAP |
| Data writes | 0 | 4 | GAP |
| Integrations | 0 | 2 | GAP |
| Error paths | 0 | 5 | GAP |

**Preservation:** UNRESOLVED — no callback implementation was confirmed in the analyzed application.

**Concrete Example:**
- API Input: `POST /api/v1/callbacks/stripe {"eventId":"evt_1600","providerReference":"pi_1600","eventType":"payment_intent.succeeded","payload":{"status":"succeeded"}}`
- Success: `202 {"callbackId":"cb_1600","status":"Accepted","paymentIntentId":"pi_1600"}`
- Error Input: Same callback with an invalid provider signature
- Error Output: `401 {"error":"CALLBACK_VERIFICATION_FAILED","message":"Provider callback could not be verified","statusCode":401}`

## Explicit Legacy Gaps

- No legacy idempotency mechanism was found.
- Administrative authorize, capture, and refund controller methods are stubs returning `null`.
- PayPal Express direct `initTransaction()` is unimplemented while `initPaypalTransaction()` exists separately.
- PayPal REST is not registered in the preserved provider map.
- Refund logic compares `doubleValue()` and does not enforce cumulative refund balance.
- Transaction “last” selection uses lexicographic transaction-type ordering rather than chronological ordering.
- Legacy payment methods directly mutate order status and totals; MS-06 must replace these writes with events.
- No confirmed generic provider callback endpoint or callback verification implementation was found.

## Phase 4b inferred clarifications

The following assumptions were applied in Mode A and are not validated by a domain expert:

- `[Inferred in Phase 4b — Mode A]` A provider callback is accepted only when signature,
  tenant/store scope, payment intent reference, amount, currency, and provider status agree
  with the locally stored intent.
- `[Inferred in Phase 4b — Mode A]` A callback older than 15 minutes is treated as stale and
  routed to reconciliation instead of changing payment state.
- `[Inferred in Phase 4b — Mode A]` The refundable balance is
  `capturedAmount - SUM(successfulRefunds + reservedRefunds)`; a refund above that balance is
  rejected without a provider call.
