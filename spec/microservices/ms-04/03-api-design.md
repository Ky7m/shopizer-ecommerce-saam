# Cart and Checkout — API Design

**Service ID:** MS-04  
**Base path:** `/api/v1`  
**Field naming:** camelCase  
**Path naming:** kebab-case  
**Currency representation:** ISO-4217 code plus decimal string amounts  
**Required headers on every operation:**

| Header | Required | Purpose |
|---|---:|---|
| `x-tenant-id` | yes | Tenant isolation |
| `x-store-id` | yes | Store isolation within tenant |
| `x-correlation-id` | yes | Distributed tracing |
| `Authorization` | authenticated operations | Customer or administrative authentication |
| `idempotency-key` | checkout and payment-sensitive mutations | Replay-safe submission |

## Endpoint catalogue

| # | Method | Endpoint | Success | Driving rules | Ownership |
|---:|---|---|---:|---|---|
| 1 | POST | `/cart` | 201 | BR-SC-CRE-001, BR-SC-SEL-002, BR-SC-ATR-003, BR-SC-MRG-004 | MS-04 |
| 2 | GET | `/cart/{code}` | 200 | BR-SC-HYD-006, BR-SC-PRO-011 | MS-04 |
| 3 | PUT | `/cart/{code}` | 200 | BR-SC-SEL-002, BR-SC-UPD-005, BR-SC-PRO-011 | MS-04 |
| 4 | POST | `/cart/{code}/multi` | 200 | BR-SC-SEL-002, BR-SC-UPD-005, BR-SC-ATR-003 | MS-04 |
| 5 | POST | `/cart/{code}/promo/{promoCode}` | 200 | BR-SC-PRO-011 | MS-04/MS-07 boundary |
| 6 | DELETE | `/cart/{code}/product/{sku}` | 200 or 204 | BR-SC-UPD-005 | MS-04 |
| 7 | GET | `/auth/customer/cart` | 200 | BR-CO-AUT-012, BR-SC-HYD-006, BR-SC-MRG-007 | MS-04/MS-01 boundary |
| 8 | GET | `/auth/customer/{id}/cart` | 200 | BR-CO-AUT-012 | Compatibility endpoint; deprecated |
| 9 | POST | `/customers/{id}/cart` | 410 | No target business operation | Deprecated and unsupported |
| 10 | POST | `/auth/cart/{code}/checkout` | 202 | BR-CO-AUT-012, BR-CO-SNP-014, BR-CO-TOT-015, BR-CO-IDM-017, BR-CO-ORC-019 | MS-04 |
| 11 | POST | `/cart/{code}/checkout` | 202 | BR-CO-CUS-013, BR-CO-SNP-014, BR-CO-TOT-015, BR-CO-IDM-017, BR-CO-ORC-019 | MS-04 |
| 12 | GET | `/auth/cart/{code}/shipping` | 200 | BR-CO-AUT-012, BR-SC-SHP-008, BR-SC-SHP-009 | MS-04/MS-09 |
| 13 | POST | `/cart/{code}/shipping` | 200 | BR-SC-SHP-008, BR-SC-SHP-009 | MS-04/MS-09 |
| 14 | GET | `/auth/cart/{id}/total` | 200 | BR-CO-AUT-012, BR-SC-TOT-010 | MS-04/MS-07/MS-08/MS-09 |
| 15 | GET | `/cart/{code}/total` | 200 | BR-SC-TOT-010, BR-SC-PRO-011 | MS-04/MS-07/MS-08/MS-09 |
| 16 | POST | `/auth/cart/{code}/payment/init` | 202 | BR-CO-AUT-012, BR-CO-PAY-016 | MS-04/MS-06 |
| 17 | POST | `/cart/{code}/payment/init` | 202 | BR-CO-PAY-016 | MS-04/MS-06 |

## Request and response contracts

### Cart item

`AddCartItemRequest`

```json
{
  "product": "SKU-RED-42",
  "quantity": 2,
  "attributes": [
    {"id": 701}
  ],
  "promoCode": "Test1234"
}
```

Rules:

- `product` is an MS-02 SKU.
- `quantity` must be positive for add and may be zero only for update/removal.
- Attribute identifiers are validated against the product.
- `promoCode` is stored by MS-04 and evaluated by MS-07.

### Cart response

```json
{
  "cart": {
    "id": "cart-1001",
    "code": "a91b8c7d2e",
    "status": "Open",
    "customerId": "cust-1001",
    "items": [
      {
        "id": "line-1",
        "sku": "SKU-RED-42",
        "productId": 4201,
        "variantId": null,
        "quantity": 2,
        "unitPrice": "19.99",
        "subTotal": "39.98",
        "attributes": [
          {"id": 701}
        ]
      }
    ],
    "subTotal": "39.98",
    "total": "45.98",
    "currency": "CAD",
    "promoCode": "Test1234"
  }
}
```

### Checkout request

Authenticated:

```json
{
  "currency": "CAD",
  "shippingQuoteId": "quote-701",
  "payment": {
    "amount": "56.48",
    "paymentModule": "stripe",
    "paymentType": "CREDITCARD",
    "transactionType": "AUTHORIZECAPTURE",
    "paymentToken": "tok_test_123"
  },
  "comments": "Leave at reception",
  "customerAgreement": true
}
```

Anonymous:

```json
{
  "currency": "CAD",
  "customer": {
    "email": "ada@example.test",
    "firstName": "Ada",
    "lastName": "Lovelace",
    "billing": {
      "address": "1 Main St",
      "city": "Montreal",
      "countryCode": "CA",
      "postalCode": "H2Y 1C6",
      "phone": "5145550100"
    }
  },
  "shippingQuoteId": "quote-701",
  "payment": {
    "amount": "56.48",
    "paymentModule": "stripe",
    "paymentType": "CREDITCARD",
    "transactionType": "AUTHORIZECAPTURE",
    "paymentToken": "tok_test_123"
  },
  "customerAgreement": true
}
```

### Shipping request

```json
{
  "postalCode": "H2Y 1C6",
  "countryCode": "CA"
}
```

### Shipping response

```json
{
  "quoteId": "quote-701",
  "expiresAt": "2026-09-01T17:00:00Z",
  "shippingRequired": true,
  "delivery": {
    "postalCode": "H2Y 1C6",
    "countryCode": "CA"
  },
  "options": [
    {
      "code": "canadapost",
      "name": "Canada Post",
      "price": "12.00",
      "currency": "CAD"
    }
  ]
}
```

### Total response

```json
{
  "cartCode": "a91b8c7d2e",
  "currency": "CAD",
  "subTotal": "39.98",
  "discountTotal": "4.00",
  "shipping": "12.00",
  "handling": "2.00",
  "tax": "6.50",
  "grandTotal": "56.48",
  "quoteVersion": 3,
  "components": [
    {"code": "order.total.subtotal", "amount": "35.98"},
    {"code": "order.total.shipping", "amount": "12.00"},
    {"code": "order.total.handling", "amount": "2.00"},
    {"code": "order.total.tax", "amount": "6.50"},
    {"code": "order.total.total", "amount": "56.48"}
  ]
}
```

### Payment initialization response

```json
{
  "submissionId": "sub-9001",
  "paymentState": "Pending",
  "providerReference": "payref-701",
  "amount": "56.48",
  "currency": "CAD"
}
```

MS-04 does not expose provider transaction internals. Provider transaction state is owned by MS-06.

## Operation details

### POST `/api/v1/cart`

- Creates an anonymous cart when no cart exists.
- Validates SKU, store ownership, sellability, inventory configuration, availability date, and attributes.
- Merges an existing physical attribute-free line by quantity.
- Returns `201 CartEnvelope`.

### GET `/api/v1/cart/{code}`

- Resolves the cart within tenant/store scope.
- Rehydrates product and attribute facts.
- Recalculates line prices and subtotals.
- May mark and remove an obsolete cart.
- Returns `200 CartEnvelope`.

### PUT `/api/v1/cart/{code}`

- Replaces the requested line quantity.
- Quantity zero deletes selected attributes before deleting the line.
- Recalculates the cart.
- Returns `200 CartEnvelope`.

### POST `/api/v1/cart/{code}/multi`

- Applies only the submitted item changes.
- Items not in the request remain unchanged.
- Quantity zero removes an existing line.
- Returns `200 CartEnvelope`.

### POST `/api/v1/cart/{code}/promo/{promoCode}`

- Stores promotion code and timestamp.
- Recalculates the cart.
- Returns `200 CartEnvelope`.

### DELETE `/api/v1/cart/{code}/product/{sku}`

- Removes a matching SKU line and selected attributes.
- Query `body=true` returns the remaining cart with `200`.
- Query `body=false` returns `204`.

### Authenticated cart reads

- Resolve the customer from the principal.
- Enforce customer, tenant, and store scope.
- The legacy customer-ID endpoint is retained only for compatibility and remains deprecated.

### Checkout

- Requires `idempotency-key`.
- Rehydrates cart and provider facts.
- Resolves shipping quote if supplied.
- Recalculates totals.
- Compares submitted amount with server amount.
- Persists immutable snapshot and outbox event.
- Returns `202 SubmissionAccepted`.
- Does not create an MS-05 order directly.

### Shipping

- Digital-only carts return `shippingRequired=false`.
- Physical carts use delivery address, billing fallback, or anonymous postal/country input.
- Provider selection and carrier calculation remain MS-09/MS-12 responsibilities.

### Totals

- Optional `quote` query parameter references an MS-09 quote.
- Authenticated form enforces customer ownership.
- Provider calculation remains MS-07/MS-08/MS-09-owned.

### Payment initialization

- Authenticated form enforces customer ownership.
- Public form is limited to payment handoff and does not authorize access to another customer's cart.
- Active method configuration is checked.
- Provider state remains MS-06-owned.

## Error model

| Error code | HTTP | Meaning |
|---|---:|---|
| `INVALID_REQUEST` | 400 | Malformed request |
| `AUTHENTICATION_REQUIRED` | 401 | Authenticated operation lacks valid principal |
| `CART_SCOPE_MISMATCH` | 403 | Tenant/store/customer scope violation |
| `CART_NOT_FOUND` | 404 | Cart unavailable in requested scope |
| `QUOTE_NOT_FOUND` | 404 | Quote unavailable in requested scope |
| `PRODUCT_NOT_SELLABLE` | 422 | Product unavailable, future-dated, or inventory not configured |
| `ATTRIBUTE_PRODUCT_MISMATCH` | 422 | Attribute does not belong to SKU |
| `INVALID_QUANTITY` | 422 | Invalid quantity |
| `SHIPPING_ADDRESS_INVALID` | 422 | Missing or unsupported shipping address |
| `SHIPPING_NOT_REQUIRED` | 422 | Shipping supplied for digital-only cart |
| `PROMOTION_EXPIRED` | 422 | Promotion no longer eligible |
| `PAYMENT_METHOD_INACTIVE` | 422 | Payment method is not active |
| `AMOUNT_MISMATCH` | 409 | Submitted amount differs from calculated total |
| `QUOTE_STALE` | 409 | Cart or provider quote changed |
| `IDEMPOTENCY_KEY_REUSED` | 409 | Same key used with a different request |
| `CHECKOUT_TERMINAL` | 409 | Terminal checkout session reused |
| `OWNERSHIP_VIOLATION` | 403 | MS-04 attempted to write another service's state |
| `CHECKOUT_UNAVAILABLE` | 503 | Durable local submission or required provider unavailable |

## Explicitly excluded endpoints

The following legacy endpoints are not MS-04 target capabilities:

- `/private/orders/{id}/capture`
- `/private/orders/{id}/refund`
- `/private/orders/{id}/authorize`
- `/private/orders/payment/capturable`
- `/private/orders/{id}/payment/transactions`
- `/private/orders/{id}/payment/nextTransaction`
- Order status update endpoints

They belong to MS-05/MS-06. The legacy capture, refund, and authorize methods in `OrderPaymentApi.java:292-361` are stubbed or return `null`.
