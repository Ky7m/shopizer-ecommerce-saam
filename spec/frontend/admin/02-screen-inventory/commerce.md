# Screen Inventory: Payment, Tax, Customers, Orders

## Payment and tax

Payment methods retains the provider-module list and configure detail flow. Tax retains
separate “Tax Classes” and “Tax Rate” list/add flows, list pagination, delete confirmation,
and exact `TaxClass`/`TaxRate` fields. Store context is required for store-scoped reads.

## Customers

Customer list preserves search/table/paginator and opens add/detail form. Address fields and
customer identity bind exact MS-01 schemas. `set-credentials` remains routable but deferred
because no published customer credential operation exists.

## Orders

Order list preserves store filter, order rows, and detail navigation. Detail preserves:

- billing and shipping information;
- order date, customer contact, status and payment mode;
- line items with item, quantity, price and total;
- totals;
- status history/comment editor;
- transactions;
- capture, refund, cancel, and invoice actions where exact MS-05 operations apply.

Route state carries the selected order identifier through detail and returns to the list query
after save. Detail loads the order, history, next payment action, and transactions as separate
states so one failed auxiliary call does not erase the primary order. 409 payment/order
conflicts disable the affected command and retain server detail.
