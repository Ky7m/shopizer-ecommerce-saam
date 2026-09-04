# Implementation Audit: Payments

## Decisions made during implementation

| Date | Decision | Rationale |
|---|---|---|
| 2026-09-04 | Copied all frozen MS-06 DTOs verbatim before implementation. | The API contract and DTO directory are the binding authority. |
| 2026-09-04 | Used raw Npgsql with the `payments` schema, additive migrations, a refund trigger, and an outbox. | The reference standard forbids ORMs and requires database-tier invariant enforcement. |
| 2026-09-04 | Provider adapters use secret references and deterministic local provider-boundary results. | MS-12/MS-11 own secret transport and external egress; MS-06 must not persist credentials or PAN/CVV. |
| 2026-09-04 | Order state is never written by this service. | MS-05 remains the sole owner of order lifecycle transitions; payment outcomes are events. |

## Specification boundaries requiring review

| Area | Boundary | Resolution |
|---|---|---|
| Store country | The frozen API has no country header/query parameter. | Configuration projections default to wildcard eligibility; a future MS-11 store-country projection can replace this without changing payment state. |
| Provider transport | The repository does not provide real Stripe/Braintree/PayPal/Beanstream credentials. | Adapter validation, timeout boundary, response normalization, redaction, and durable provider references are implemented; external egress remains an integration deployment concern. |
| Checkout snapshot | MS-06 does not own checkout totals. | The supplied amount/currency are bound immutably at intent creation and revalidated for every operation. |

## Validation record

| Check | Status | Result |
|---|---|---|
| DTO byte equality | PASS | All 18 files copied from `spec/microservices/ms-06/08-dtos/`. |
| Targeted service build | PASS | `dotnet build ...Shopizer.Payments.csproj --no-restore` completed after adding Aspire Npgsql/RabbitMQ dependencies. |
| Container build | PASS | `docker build -f sourcecode/Shopizer.Payments/Dockerfile sourcecode` passed. |
| Aspire runtime suite | BLOCKED | Native .NET 10 targeted test invocation started but produced no test result after the Aspire startup wait and was stopped; no pass is claimed. |
| Contract structural review | PASS | Paths, operation IDs, response status codes, and DTO property names were reviewed against the frozen contract. |
