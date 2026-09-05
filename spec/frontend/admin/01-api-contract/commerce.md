# BFF Contract: Customers and Orders

## Customers

| Frontend BFF method/path | Provider | Exact provider method/path | Request -> response |
|---|---|---|---|
| GET `/api/admin/v1/customers` | MS-01 | GET `/customers` | query -> `CustomerListResponse` |
| POST `/api/admin/v1/customers` | MS-01 | POST `/customers` | `CreateCustomerRequest` -> `Customer` |
| GET `/api/admin/v1/customers/{customerId}` | MS-01 | GET `/customers/{customerId}` | path -> `Customer` |
| PUT `/api/admin/v1/customers/{customerId}` | MS-01 | PUT `/customers/{customerId}` | `UpdateCustomerRequest` -> `Customer` |
| DELETE `/api/admin/v1/customers/{customerId}` | MS-01 | DELETE `/customers/{customerId}` | path -> 204 |
| PATCH `/api/admin/v1/customers/{customerId}/address` | MS-01 | PATCH `/customers/{customerId}/address` | `AddressUpdateRequest` -> 204 |

Customer list/detail/form fields bind exact `Customer`, `Address`, `CreateCustomerRequest`, and
`UpdateCustomerRequest` fields. Country/zone selector data is a `CONTRACT GAP` because the
legacy shared lookup operations have no published provider equivalent; a BFF may not call a
legacy lookup path.

## Orders

| Frontend BFF method/path | Provider | Exact provider method/path | Request -> response |
|---|---|---|---|
| GET `/api/admin/v1/orders` | MS-05 | GET `/orders` | query -> `OrderListResponse` |
| GET `/api/admin/v1/orders/{orderId}` | MS-05 | GET `/orders/{orderId}` | path -> `Order` |
| GET `/api/admin/v1/orders/{orderId}/history` | MS-05 | GET `/orders/{orderId}/history` | path/query -> `OrderHistoryResponse` |
| POST `/api/admin/v1/orders/{orderId}/history` | MS-05 | POST `/orders/{orderId}/history` | `AppendHistoryRequest` -> `OrderHistoryEntry` |
| PATCH `/api/admin/v1/orders/{orderId}/customer-snapshot` | MS-05 | PATCH `/orders/{orderId}/customer-snapshot` | `CustomerSnapshotUpdateRequest` -> `Order` |
| GET `/api/admin/v1/orders/{orderId}/payment/next-action` | MS-05 | GET `/orders/{orderId}/payment/next-action` | path -> `NextPaymentAction` |
| GET `/api/admin/v1/orders/{orderId}/payment-transactions` | MS-05 | GET `/orders/{orderId}/payment-transactions` | query -> `PaymentTransactionListResponse` |
| POST `/api/admin/v1/orders/{orderId}/capture` | MS-05 | POST `/orders/{orderId}/capture` | `PaymentCommandRequest` -> `PaymentCommandResponse` |
| POST `/api/admin/v1/orders/{orderId}/refund` | MS-05 | POST `/orders/{orderId}/refund` | `RefundRequest` -> `RefundCommandResponse` |
| POST `/api/admin/v1/orders/{orderId}/cancel` | MS-05 | POST `/orders/{orderId}/cancel` | `CancelOrderRequest` -> `CancellationResponse` |
| GET `/api/admin/v1/orders/{orderId}/invoice` | MS-05 | GET `/orders/{orderId}/invoice` | path -> `InvoiceResponse` |

Order lists bind exact `OrderListResponse`/`Order` fields; detail preserves billing information,
shipping information, line item `OrderLine` fields, totals, status, history, and transactions.
The legacy “Update status” action uses the published history command only when its request
semantics match `AppendHistoryRequest`. A status transition is not invented from the legacy
button; use `PUT /orders/{orderId}/status` only after the workflow binding is approved.

Pagination follows the exact `PaginationInfo` nested in each provider response. Loading and
empty states are table-local. Refund/capture/cancel require confirmation, disable controls
while pending, and surface 409 payment/order state conflicts without clearing detail state.

## Open decisions / gaps

- Legacy customer “set credentials” and customer option screens have no matching operation.
- The legacy order workflow has no provider `07-workflows.md`; status, capture, refund, cancel,
  invoice, and history sequencing require `GAP-WF-ADMIN-003`.
- Legacy country and zone calls are not provider-contract backed.
