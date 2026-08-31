# Cart, Checkout, Orders, and Payments - Extraction Summary

## Segment Profile

- System: Shopizer 3.2.7; Hybrid mode with direct source extraction.
- Modules: `sm-shop`, `sm-core`, `sm-core-model`, `sm-core-modules`.
- Primary entities: `SHOPPING_CART`, `SHOPPING_CART_ITEM`, `SHOPPING_CART_ATTR_ITEM`, `ORDERS`, `ORDER_PRODUCT`, `ORDER_PRODUCT_ATTRIBUTE`, `ORDER_PRODUCT_PRICE`, `ORDER_PRODUCT_DOWNLOAD`, `ORDER_STATUS_HISTORY`, `ORDER_TOTAL`, `SM_TRANSACTION`.
- Business rules extracted: 19.
- Confidence: High 65%, Medium 25%, Low 10%.
- No scheduled batch job was found in this segment.
- Checkout/payment orchestration has no evident enclosing `@Transactional`; persistence commonly uses `saveAndFlush`.

## Call Graph

### Cart add/update

```text
POST /api/v1/cart -> ShoppingCartApi.addToCart
  -> ShoppingCartFacadeImpl.addToCart/createCartItem
  -> ProductService/ProductAttributeService/PricingService
  -> ShoppingCartService.save -> JpaRepository.saveAndFlush
  -> ShoppingCartCalculationService -> OrderService.caculateShoppingCart
  -> tax/shipping/order-total processors -> response mapper

PUT /api/v1/cart/{code} -> modifyCart
  -> merchant-scoped cart lookup -> refresh product/attributes/prices
  -> quantity update or delete at zero -> promo -> save/reload/recalculate
```

Evidence: `initial-source/sm-shop/.../ShoppingCartApi.java:80-116`,
`initial-source/sm-shop/.../ShoppingCartFacadeImpl.java:756-966`,
`initial-source/sm-core/.../ShoppingCartServiceImpl.java:231-359`,
`initial-source/sm-core/.../ShoppingCartCalculationServiceImpl.java:66-79`.

### Checkout/payment

```text
POST /api/v1/auth/cart/{code}/checkout (or anonymous checkout)
  -> customer/cart lookup -> OrderFacadeImpl.processOrder
  -> reload cart -> OrderProductPopulator
  -> shipping summary -> OrderService.caculateOrderTotal
  -> promo processors -> shipping -> tax -> grand total validation
  -> PaymentService provider selection/call -> TransactionService
  -> order/history/transaction persistence -> inventory decrement
  -> cart completion -> notification/confirmation
```

Evidence: `initial-source/sm-shop/.../OrderApi.java:348-477`,
`initial-source/sm-shop/.../OrderFacadeImpl.java:1196-1359`,
`initial-source/sm-core/.../OrderServiceImpl.java:127-397`,
`initial-source/sm-core/.../PaymentServiceImpl.java:300-399`.

### Administrative payment paths

Capture and refund facade implementations exist (`OrderFacadeImpl.java:1383-1430`,
`PaymentServiceImpl.java:405-568`), but the corresponding `OrderPaymentApi` endpoints currently return `null`
(`OrderPaymentApi.java:292-351`). Payment initialization uses `PaymentService.initTransaction`
(`OrderPaymentApi.java:88-180`; `PaymentServiceImpl.java:739-784`).

## Business Rules

### BR-ORD-001: Cart creation generates a merchant-scoped client-visible code

Create a cart, assign merchant/customer context, and generate a UUID-derived code when none is supplied.

Source: `initial-source/sm-shop/.../ShoppingCartFacadeImpl.java:createCartModel:441-456`,
`uniqueShoppingCartCode:1080-1083`.

### BR-ORD-002: Cart products must belong to the merchant and be sellable

Resolve by SKU and reject missing, cross-merchant, unavailable, future-availability, or unconfigured-inventory products.

Source: `initial-source/sm-shop/.../ShoppingCartFacadeImpl.java:createCartItem:254-344`,
`initial-source/sm-core/.../ShoppingCartServiceImpl.java:getShoppingCartItems:440-490`.

### BR-ORD-003: Duplicate non-attribute items increment quantity

Same-SKU items with no attributes merge by increasing quantity; the duplicate path excludes virtual products.

Source: `initial-source/sm-shop/.../ShoppingCartFacadeImpl.java:115-188,830-870`.

### BR-ORD-004: Quantity zero removes an item and its attributes

Single and multi-item cart updates delete attribute rows first, then item rows, when quantity is zero.

Source: `initial-source/sm-shop/.../ShoppingCartFacadeImpl.java:877-1024`.

### BR-ORD-005: Cart hydration recalculates prices and marks obsolete items

Cart reads reload products/attributes, calculate current prices and subtotals, remove orphaned attributes, and mark missing-product/empty carts obsolete.

Source: `initial-source/sm-core/.../ShoppingCartServiceImpl.java:231-359`.

### BR-ORD-006: Virtual or non-shippable products do not produce shipping input

Shipping input includes only `!productVirtual && productShipeable`; no physical items means no shipping quote.

Source: `initial-source/sm-core/.../ShoppingCartServiceImpl.java:362-383`,
`initial-source/sm-shop/.../OrderFacadeImpl.java:623-661,975-1004`.

### BR-ORD-007: Cart promotions are short-lived and may be cleared during calculation

Promo code and date are stored on the cart; calculation accepts a date before tomorrow and clears an expired code.

Source: `initial-source/sm-shop/.../ShoppingCartFacadeImpl.java:1150-1164`,
`initial-source/sm-core/.../OrderServiceImpl.java:430-477`.

Concern: calendar-date expiry and null handling differ between total calculation paths.

### BR-ORD-008: Totals combine items, variations, shipping, handling, tax, and grand total

Base subtotal is item price times quantity; configured variations, shipping, handling, and tax are added to derive grand total.

Source: `initial-source/sm-core/.../OrderServiceImpl.java:caculateOrder:217-397`.

Concern: some `BigDecimal.setScale(2, HALF_UP)` calls do not assign the returned value.

### BR-ORD-009: Configured order-total processors calculate promotion/variation lines

Every configured postprocessor runs for each cart item; non-null variations become order-total lines. `PromoCodeCalculatorModule` is registered by `ProcessorsConfiguration`.

Source: `initial-source/sm-core/.../OrderTotalServiceImpl.java:37-72`,
`ProcessorsConfiguration.java:31-51`.

### BR-ORD-010: Submitted checkout amount must equal server recalculation

Checkout recalculates the cart total and rejects a submitted payment amount that differs.

Source: `initial-source/sm-shop/.../OrderFacadeImpl.java:1196-1359` (comparison approximately `1270-1294`).

### BR-ORD-011: Checkout creates an order snapshot from current cart state

Each SKU is resolved again; cart lines become order products with attribute/price snapshots, totals, addresses, currency, locale, payment/shipping modules, and cart code.

Source: `initial-source/sm-shop/.../OrderFacadeImpl.java:345-565`,
`OrderProductPopulator.java:60-154`.

### BR-ORD-012: Facade inventory validation and core decrement behavior diverge

The facade rejects insufficient `ALL_REGIONS` availability, while the core decrement path logs mismatches rather than consistently rejecting. The core lookup uses `orderProduct.getId()`, which may not be the catalog product ID.

Source: `initial-source/sm-shop/.../OrderFacadeImpl.java:383-416`,
`initial-source/sm-core/.../OrderServiceImpl.java:192-214`.

### BR-ORD-013: New orders receive `ORDERED`; some payments promote to `PROCESSED`

Initial order history is `ORDERED`. `AUTHORIZECAPTURE` promotes orders to `PROCESSED` except Money Order.

Source: `initial-source/sm-core/.../OrderServiceImpl.java:127-214`,
`PaymentServiceImpl.java:300-399`.

### BR-ORD-014: Payment provider selection is configuration-driven

Merchant configuration selects active module and transaction mode, resolves a registered provider, and calls authorize, authorize-and-capture, or initialization.

Source: `initial-source/sm-core/.../PaymentServiceImpl.java:300-399`,
`initial-source/sm-core/src/main/resources/spring/shopizer-core-modules.xml:45-88`.

Providers observed: Beanstream, PayPal Express, Money Order, Stripe, Stripe 3, and Braintree.

### BR-ORD-015: Provider transactions persist before and after order association

Payment processing creates a provider transaction before order persistence; order processing then associates and persists the transaction again.

Source: `initial-source/sm-core/.../PaymentServiceImpl.java:357-386`,
`OrderServiceImpl.java:127-181`,
`TransactionServiceImpl.java:38-50`.

Concern: no visible atomic boundary spans provider call, order, transaction, inventory, cart completion, and notification.

### BR-ORD-016: Capture requires prior authorization and sets `PROCESSED`

Find an authorization without later capture/refund, capture through the provider, persist the capture transaction, append history, and set order status.

Source: `initial-source/sm-core/.../PaymentServiceImpl.java:405-471`,
`TransactionServiceImpl.java:119-146`,
`initial-source/sm-shop/.../OrderFacadeImpl.java:1416-1430`.

Concern: administrative capture endpoint is currently stubbed.

### BR-ORD-017: Refund cannot exceed current order total

Reject excessive refunds, select a refundable transaction, call the provider, persist a refund transaction/total, reduce order total, and mark the order refunded.

Source: `initial-source/sm-core/.../PaymentServiceImpl.java:474-568`.

Concern: partial-refund accumulation and Stripe transaction typing require confirmation.

### BR-ORD-018: Digital products create download records and send notification

Digital order products receive filename, count zero, and maximum-days metadata; checkout sends a download email when downloads exist.

Source: `initial-source/sm-shop/.../OrderProductPopulator.java:60-91`,
`initial-source/sm-core/.../OrderServiceImpl.java:582-597`,
`initial-source/sm-shop/.../OrderFacadeImpl.java:1362-1380`.

Concern: expected customer download route was not found; only administrator file download routes were located.

### BR-ORD-019: Card validation is conditional and duplicated

When `VALIDATE_CREDIT_CARD=true`, facade and payment layers validate required fields, dates, characters, card type, pattern, and Luhn checksum.

Source: `initial-source/sm-shop/.../OrderFacadeImpl.java:707-921`,
`initial-source/sm-core/.../PaymentServiceImpl.java:571-697`.

## Data Access Patterns

| Table/entity | Create | Read | Update | Delete | Main components |
|---|---|---|---|---|---|
| `SHOPPING_CART` | yes | yes | yes | yes | `ShoppingCartFacadeImpl`, `ShoppingCartServiceImpl` |
| `SHOPPING_CART_ITEM` | cascade/direct | fetch-joined | quantity/price | explicit/cascade | cart facade/service |
| `SHOPPING_CART_ATTR_ITEM` | cascade | fetch-joined | reattachment | orphan deletion | cart service |
| `ORDERS` | checkout | repository/custom lists | status/customer | generic only | order service/facade |
| `ORDER_PRODUCT`, `ORDER_PRODUCT_ATTRIBUTE`, `ORDER_PRODUCT_PRICE` | cascade | order fetch graph | snapshot | cascade | order populator |
| `ORDER_PRODUCT_DOWNLOAD` | cascade | repository by order | no active endpoint found | generic only | order populator/download service |
| `ORDER_STATUS_HISTORY` | initial/updates | order fetch graph | aggregate append | cascade | order service/facade |
| `ORDER_TOTAL` | cascade | order fetch graph | refund/total mutation | cascade | order/payment service |
| `SM_TRANSACTION` | payment processing | by order/date | association update | generic repository | payment/transaction service |
| Merchant payment configuration | config service | decrypted lookup | encrypted replacement | module removal | payment service/API |
| Product availability | not owned here | product lookup | checkout decrement | not owned here | cart/order service |

Important write/tenant findings:

- `ShoppingCartServiceImpl.deleteCart` and `ShoppingCartFacadeImpl.getShoppingCartModel(Long, ...)` use inherited unscoped `getById` (`ShoppingCartServiceImpl.java:209-218`; `ShoppingCartFacadeImpl.java:1096-1105`).
- `OrderRepositoryImpl` customer name/phone filters contain ungrouped `OR` clauses that may weaken merchant predicates.
- Broad order fetch joins across products, totals, history, downloads, attributes, and prices can multiply rows.

## Payment Integrations and Rules

Stripe, Stripe 3, Braintree, Beanstream, PayPal Express, and Money Order provider adapters were inspected. PayPal REST is registered nowhere and its methods return `null`. PayPal Express has implemented `initPaypalTransaction` but interface `initTransaction` throws. Stripe refund appears to create a transaction typed as `CAPTURE`. Stripe 3 authorization does not visibly verify amount, currency, or successful authorization state.

Drools evidence:

- `PromoCodeCalculatorModule.java:49-105` and `PromoCoupon.drl:1-17`: `Test1234`, 10% discount, expiry `31-Oct-2025` (expired at analysis time).
- `ShippingDecision.drl`: Canada/weight/size/province routing rules; reconcile with shipping segment.

## Entity Lifecycles and Invariants (Layer A flags)

| Entity | States | Candidate invariant |
|---|---|---|
| Cart | open, completed, obsolete | completed cart is not reused; merchant ownership is stable |
| Cart item | active, obsolete, removed | positive normal quantity; SKU belongs to cart merchant |
| Order | ORDERED, PROCESSED, DELIVERED, REFUNDED, CANCELED | legal transitions and auditable history |
| Transaction | INIT, AUTHORIZE, AUTHORIZECAPTURE, CAPTURE, REFUND, OK | capture follows authorization; refund follows refundable transaction |
| Order total | subtotal/shipping/handling/tax/refund/total | components sum; refunds stay within balance |
| Inventory | quantity values | no negative quantity; atomic concurrent decrement |
| Download | created/available/expired conceptually | expiry/count enforced at access |

`OrderFacadeImpl.updateOrderStatus:1625-1647` accepts any enum value without a transition matrix. `TransactionServiceImpl.lastTransaction:86-117` orders by transaction type rather than timestamp.

## Extensibility Signals (Layer B flags)

| Component | Mechanism | Variation |
|---|---|---|
| `PaymentServiceImpl` | encrypted merchant configuration/provider map | providers, credentials, modes |
| `OrderTotalServiceImpl` | configured postprocessor collection | total variations |
| `PromoCodeCalculatorModule` | Drools rules | eligibility and discount |
| Cart/product attributes | metadata and selected attributes | variants and attribute pricing |
| Shipping modules | XML processor lists and Drools | carrier/method decisions |

## Placement Candidates (Layer C flags)

Order listing/fetch joins, order-total calculation, per-item postprocessor fan-out, inventory decrement, and transaction date sweeps are placement candidates for P4b review. Default remains application tier; no stored procedure or scheduled job was found.

## Source Semantic Vectors

| Component | Control | Data | Constants | States | Outcomes | Writes | Integrations | Errors |
|---|---:|---:|---:|---:|---:|---:|---:|---:|
| `ShoppingCartApi` | 17 | 16 | 5 | 0 | 16 | 0 | 5 | 8 |
| `ShoppingCartFacadeImpl` | 45 | 28 | 15 | 0 | 21 | 4 | 10 | 20 |
| `ShoppingCartServiceImpl` | 42 | 30 | 12 | 7 | 17 | 8 | 8 | 17 |
| `ShoppingCartCalculationServiceImpl` | 6 | 10 | 0 | 0 | 3 | 1 | 1 | 2 |
| `OrderApi` | 27 | 13 | 5 | 4 | 15 | 0 | 4 | 8 |
| `OrderFacadeImpl` | 96 | 70 | 18 | 11 | 30 | 10 | 17 | 35 |
| `OrderServiceImpl` | 62 | 45 | 15 | 12 | 22 | 9 | 7 | 20 |
| `OrderRepositoryImpl` | 28 | 35 | 4 | 0 | 5 | 0 | 0 | 3 |
| `PaymentServiceImpl` | 72 | 38 | 15 | 10 | 25 | 8 | 10 | 30 |
| `TransactionServiceImpl` | 24 | 12 | 2 | 4 | 7 | 1 | 0 | 7 |
| `OrderTotalServiceImpl` | 10 | 10 | 0 | 0 | 3 | 0 | 1 | 3 |
| `PromoCodeCalculatorModule` | 10 | 10 | 3 | 0 | 5 | 0 | 1 | 4 |
| `OrderProductPopulator` | 29 | 25 | 9 | 0 | 10 | 5 | 3 | 12 |
| `StripePayment` | 52 | 24 | 18 | 4 | 12 | 0 | 5 | 24 |
| `Stripe3Payment` | 49 | 23 | 16 | 5 | 12 | 0 | 5 | 23 |
| `BraintreePayment` | 54 | 26 | 18 | 5 | 13 | 0 | 5 | 27 |
| `BeanStreamPayment` | 61 | 34 | 22 | 5 | 16 | 0 | 6 | 31 |
| `PayPalExpressCheckoutPayment` | 81 | 39 | 22 | 6 | 20 | 0 | 8 | 37 |
| `MoneyOrderPayment` | 12 | 12 | 4 | 1 | 5 | 0 | 0 | 5 |

## Clarification Items

- Confirm stubbed capture/refund/authorize endpoints and PayPal Express initialization.
- Confirm enabled providers, transaction modes, idempotency, retry/reconciliation, and checkout atomicity.
- Confirm inventory product ID lookup and whether mismatch must reject checkout.
- Confirm legal order transitions, cumulative partial refunds, and transaction ordering.
- Confirm expired Drools promo behavior and shipping rule ownership.
- Confirm customer download route, expiry, and count enforcement.
- Confirm public cart/payment-init ownership checks and unscoped cart access.
- Confirm currency precision/rounding and legacy API support.
- Review provider credential storage and sensitive logging.
