# Order Management — API Design

**Service:** MS-05 Order Management  
**Base path:** `/api/v1`  
**Port:** `8105`

## Global Request Context

Every operation requires:

- `Authorization: Bearer <token>`
- `x-tenant-id`
- `x-store-id`
- `x-correlation-id`

Every mutating operation additionally requires `Idempotency-Key`.

## Endpoint Index

| # | Method | Endpoint | Purpose | Driving BR-IDs |
|---:|---|---|---|---|
| 1 | GET | `/orders` | Administrative paginated order list | BR-OR-AUTH-001, BR-OR-ADM-001, BR-OR-READ-001 |
| 2 | GET | `/orders/{orderId}` | Administrative order detail | BR-OR-AUTH-001, BR-OR-ADM-001, BR-OR-READ-001 |
| 3 | GET | `/me/orders` | Authenticated customer order list | BR-OR-AUTH-002, BR-OR-READ-001 |
| 4 | GET | `/me/orders/{orderId}` | Authenticated customer order detail | BR-OR-AUTH-002, BR-OR-READ-001 |
| 5 | GET | `/customers/{customerId}/orders` | Administrative customer-order list | BR-OR-AUTH-001, BR-OR-ADM-001 |
| 6 | GET | `/orders/{orderId}/history` | Read lifecycle history | BR-OR-LIFE-002 |
| 7 | POST | `/orders/{orderId}/history` | Append administrative history | BR-OR-LIFE-001, BR-OR-LIFE-002 |
| 8 | PUT | `/orders/{orderId}/status` | Perform legal status transition | BR-OR-LIFE-001, BR-OR-LIFE-002 |
| 9 | PATCH | `/orders/{orderId}/customer-snapshot` | Correct order snapshot | BR-OR-ADM-002 |
| 10 | GET | `/orders/{orderId}/payment/next-action` | Determine next payment action | BR-OR-PAY-003 |
| 11 | GET | `/orders/{orderId}/payment-transactions` | Read payment outcome projection | BR-OR-PAY-001, BR-OR-UI-001 |
| 12 | GET | `/orders/capturable` | Find authorized but uncaptured orders | BR-OR-PAY-004 |
| 13 | POST | `/orders/{orderId}/capture` | Request payment capture through MS-06 | BR-OR-PAY-001, BR-OR-UI-001 |
| 14 | POST | `/orders/{orderId}/refund` | Request payment refund through MS-06 | BR-OR-REF-001, BR-OR-UI-001 |
| 15 | POST | `/orders/{orderId}/cancel` | Start cancellation compensation | BR-OR-CAN-001 |
| 16 | POST | `/orders/{orderId}/fulfillment` | Request fulfillment | BR-OR-FUL-001 |
| 17 | GET | `/orders/{orderId}/fulfillment` | Read fulfillment state | BR-OR-FUL-001 |
| 18 | GET | `/orders/{orderId}/invoice` | Request/read invoice artifact boundary | BR-OR-INV-001 |

Cart checkout, cart totals, shipping quotes, and payment initialization are deliberately not exposed by MS-05.

## 1. GET `/orders`

- **Authorization:** administrative order group.
- **Query:** `page` default `1`; `pageSize` default `20`, maximum `100`; optional `status`, `customerName`, `email`, `phone`, `orderId`.
- **Success:** `200 OrderListResponse`.
- **Errors:** `401`, `403`, `422`, `500`.
- **Driven by:** BR-OR-AUTH-001, BR-OR-ADM-001, BR-OR-READ-001.
- **CRUD classification:** Read projection with business authorization and tenant/store filtering.

## 2. GET `/orders/{orderId}`

- **Authorization:** administrative order group.
- **Success:** `200 Order`.
- **Errors:** `401`, `403`, `404`, `500`.
- **Driven by:** BR-OR-AUTH-001, BR-OR-ADM-001, BR-OR-READ-001.

## 3. GET `/me/orders`

- **Authorization:** authenticated customer.
- **Query:** `page`, `pageSize`, optional `status`.
- **Success:** `200 OrderListResponse`.
- **Errors:** `401`, `422`, `500`.
- **Driven by:** BR-OR-AUTH-002, BR-OR-READ-001.

## 4. GET `/me/orders/{orderId}`

- **Authorization:** authenticated customer owning the order.
- **Success:** `200 CustomerOrder`.
- **Errors:** `401`, `404`, `500`.
- **Driven by:** BR-OR-AUTH-002, BR-OR-READ-001.

## 5. GET `/customers/{customerId}/orders`

- **Authorization:** administrative order group.
- **Query:** `page`, `pageSize`.
- **Success:** `200 OrderListResponse`.
- **Errors:** `401`, `403`, `404`, `422`, `500`.
- **Driven by:** BR-OR-AUTH-001, BR-OR-ADM-001, BR-OR-READ-001.

## 6. GET `/orders/{orderId}/history`

- **Authorization:** administrative order group or owning customer.
- **Success:** `200 OrderHistoryResponse`, newest first.
- **Errors:** `401`, `403`, `404`, `500`.
- **Driven by:** BR-OR-LIFE-002.

## 7. POST `/orders/{orderId}/history`

- **Authorization:** administrative order group.
- **Request:** `status`, optional `comments`, `source`.
- **Success:** `201 OrderHistoryEntry`.
- **Errors:** `401`, `403`, `404`, `409`, `422`, `500`.
- **Driven by:** BR-OR-LIFE-001, BR-OR-LIFE-002.
- **Idempotency:** required.

## 8. PUT `/orders/{orderId}/status`

- **Authorization:** administrative order group.
- **Request:** `status`, optional `reason`.
- **Success:** `200 Order`.
- **Errors:** `401`, `403`, `404`, `409`, `422`, `500`.
- **Driven by:** BR-OR-LIFE-001, BR-OR-LIFE-002.
- **Idempotency:** required.

## 9. PATCH `/orders/{orderId}/customer-snapshot`

- **Authorization:** administrative order group.
- **Request:** email and billing/delivery snapshot.
- **Success:** `200 Order`.
- **Errors:** `401`, `403`, `404`, `409`, `422`, `500`.
- **Driven by:** BR-OR-ADM-002.
- **Idempotency:** required.

## 10. GET `/orders/{orderId}/payment/next-action`

- **Authorization:** administrative order group.
- **Success:** `200 NextPaymentAction`.
- **Errors:** `401`, `403`, `404`, `500`.
- **Driven by:** BR-OR-PAY-003.

## 11. GET `/orders/{orderId}/payment-transactions`

- **Authorization:** administrative order group.
- **Success:** `200 PaymentTransactionListResponse`.
- **Errors:** `401`, `403`, `404`, `500`.
- **Driven by:** BR-OR-PAY-001, BR-OR-UI-001.
- **Ownership:** Read projection from MS-06; MS-05 does not write provider transactions.

## 12. GET `/orders/capturable`

- **Authorization:** administrative order group.
- **Query:** optional ISO `startDate`, `endDate`, `page`, `pageSize`.
- **Success:** `200 OrderListResponse`.
- **Errors:** `401`, `403`, `422`, `503`, `500`.
- **Driven by:** BR-OR-PAY-004.

## 13. POST `/orders/{orderId}/capture`

- **Authorization:** administrative order group.
- **Request:** `amount`, `currency`, optional `paymentReference`.
- **Success:** `202 PaymentCommandResponse`.
- **Errors:** `401`, `403`, `404`, `409`, `422`, `502`, `503`.
- **Driven by:** BR-OR-PAY-001, BR-OR-UI-001.
- **Ownership:** MS-06 executes capture; MS-05 applies the resulting event.
- **Idempotency:** required.

## 14. POST `/orders/{orderId}/refund`

- **Authorization:** administrative order group.
- **Request:** `amount`, `currency`, `reason`.
- **Success:** `202 RefundCommandResponse`.
- **Errors:** `401`, `403`, `404`, `409`, `422`, `502`, `503`.
- **Driven by:** BR-OR-REF-001, BR-OR-UI-001.
- **Ownership:** MS-06 executes provider refund; MS-05 reconciles the outcome.
- **Idempotency:** required.

## 15. POST `/orders/{orderId}/cancel`

- **Authorization:** authenticated owner or administrative order group.
- **Request:** `reason`.
- **Success:** `202 CancellationResponse`.
- **Errors:** `401`, `403`, `404`, `409`, `422`, `503`.
- **Driven by:** BR-OR-CAN-001.
- **Idempotency:** required.

## 16. POST `/orders/{orderId}/fulfillment`

- **Authorization:** administrative order group or internal orchestration identity.
- **Request:** no body required beyond idempotency key.
- **Success:** `202 FulfillmentResponse`.
- **Errors:** `401`, `403`, `404`, `409`, `422`, `503`.
- **Driven by:** BR-OR-FUL-001.
- **Idempotency:** required.

## 17. GET `/orders/{orderId}/fulfillment`

- **Authorization:** administrative order group or owning customer.
- **Success:** `200 FulfillmentResponse`.
- **Errors:** `401`, `403`, `404`, `500`.
- **Driven by:** BR-OR-FUL-001.

## 18. GET `/orders/{orderId}/invoice`

- **Authorization:** administrative order group or owning customer.
- **Success:** `200 InvoiceResponse` when artifact exists; `202 InvoiceResponse` while MS-12 is generating it.
- **Errors:** `401`, `403`, `404`, `409`, `422`, `503`.
- **Driven by:** BR-OR-INV-001.

## Event Contracts

### Consumed `OrderSubmitted.v1`

```json
{
  "eventId": "evt-submit-70001",
  "eventType": "OrderSubmitted.v1",
  "eventVersion": 1,
  "tenantId": "tenant-a",
  "storeId": "store-12",
  "submissionId": "sub-10001",
  "customerId": 481,
  "currency": "USD",
  "total": 129.50,
  "lines": [
    {
      "sku": "CAM-100",
      "productName": "Camera",
      "quantity": 1,
      "unitPrice": 119.50,
      "attributes": []
    }
  ],
  "occurredAt": "2026-09-01T10:00:00Z"
}

### Published `OrderStatusChanged`

```json
{
  "eventId": "evt-status-70001",
  "eventType": "OrderStatusChanged",
  "eventVersion": 1,
  "tenantId": "tenant-a",
  "storeId": "store-12",
  "orderId": 70001,
  "previousStatus": "ORDERED",
  "status": "PROCESSED",
  "source": "PAYMENT_CAPTURED",
  "occurredAt": "2026-09-01T10:01:00Z"
}
```

## Endpoint Coverage

| Endpoint | Coverage | BR-IDs |
|---|---|---|
| `GET /orders` | COVERED | BR-OR-AUTH-001, BR-OR-ADM-001, BR-OR-READ-001 |
| `GET /orders/{orderId}` | COVERED | BR-OR-AUTH-001, BR-OR-READ-001 |
| `GET /me/orders` | COVERED | BR-OR-AUTH-002 |
| `GET /me/orders/{orderId}` | COVERED | BR-OR-AUTH-002 |
| `GET /customers/{customerId}/orders` | COVERED | BR-OR-AUTH-001 |
| `GET /orders/{orderId}/history` | COVERED | BR-OR-LIFE-002 |
| `POST /orders/{orderId}/history` | COVERED | BR-OR-LIFE-001, BR-OR-LIFE-002 |
| `PUT /orders/{orderId}/status` | COVERED | BR-OR-LIFE-001 |
| `PATCH /orders/{orderId}/customer-snapshot` | COVERED | BR-OR-ADM-002 |
| `GET /orders/{orderId}/payment/next-action` | COVERED | BR-OR-PAY-003 |
| `GET /orders/{orderId}/payment-transactions` | COVERED | BR-OR-PAY-001 |
| `GET /orders/capturable` | COVERED | BR-OR-PAY-004 |
| `POST /orders/{orderId}/capture` | COVERED | BR-OR-PAY-001 |
| `POST /orders/{orderId}/refund` | COVERED | BR-OR-REF-001 |
| `POST /orders/{orderId}/cancel` | COVERED | BR-OR-CAN-001 |
| `POST /orders/{orderId}/fulfillment` | COVERED | BR-OR-FUL-001 |
| `GET /orders/{orderId}/fulfillment` | COVERED | BR-OR-FUL-001 |
| `GET /orders/{orderId}/invoice` | COVERED | BR-OR-INV-001 |
```
