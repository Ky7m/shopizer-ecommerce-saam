# Interaction Matrix: Customers, Orders, Content, Files

| Area/action | Trigger | BFF call/navigation | Feedback | Gating/entity state |
|---|---|---|---|---|
| Customer list | load/search/page | GET `/api/admin/v1/customers` | skeleton/empty/page | customer admin binding |
| Customer add/edit | submit | POST/PUT `/api/admin/v1/customers...` | field errors/conflict | customer exists on edit |
| Customer address/delete | submit/confirm | PATCH address/DELETE customer | confirmation/error | customer exists |
| Customer credentials/options | open/submit | no call | contract-gap state | deferred |
| Order list | load/filter/page | GET `/api/admin/v1/orders` | skeleton/empty/page | order scope |
| Order detail | select | GET order, history, next-action, transactions | independent loading/error | order exists |
| Append history | valid comment/status | POST history | pending, 409 retain detail | workflow binding required |
| Snapshot edit | save | PATCH customer snapshot | pending/422/409 | order mutable state |
| Capture/refund/cancel | confirm | POST command path | pending/idempotency/conflict | provider order/payment state |
| Invoice | click | GET invoice | download/loading/error | order exists |
| Page/box save/delete | form/confirm | POST/PUT/DELETE content paths | dirty guard, 409 code | content scope |
| File upload/download/rename/delete | picker/confirm | content file paths | progress, status, retry | content scope |
| Image gallery select | click image/cancel | GET image manager path; return to caller | loading/empty/keyboard selection | content scope |
| Promotion | form action | no call | gap state | deferred |
