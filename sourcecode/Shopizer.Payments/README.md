# Shopizer Payments (MS-06)

ASP.NET Core/.NET 10 Payments service implementing payment-method eligibility,
immutable payment intents, provider authorization/capture/refund boundaries,
transaction history, callbacks, reconciliation, idempotency, and transactional
payment events.

## Run locally

```bash
dotnet run --project sourcecode/Shopizer.Payments
```

Aspire supplies the `shopizerDb` PostgreSQL connection and `rabbitmq` connection.
The service initializes the `payments` schema using raw Npgsql and does not use an
ORM. Every request except the provider callback requires `x-tenant-id`,
`x-store-id`, `x-correlation-id`, and a valid administrator/service bearer token.
Mutating payment requests also require `Idempotency-Key`.

The API base path is `/api/v1`. The frozen contract is
`spec/microservices/ms-06/04-api-contract.yaml`; the service DTOs are copied
verbatim from `spec/microservices/ms-06/08-dtos/`.

## Provider boundary

Provider credentials are represented only by secret references. The local
configuration projection is updated by the configuration endpoint; decrypted
credentials and PAN/CVV are never persisted or logged. Provider outcomes are
recorded in `payment_transaction`, and an outbox row is committed before the
RabbitMQ `domain-events` publication attempt.

## Docker

Build from `sourcecode/` so the ServiceDefaults project reference is available:

```bash
docker build -f Shopizer.Payments/Dockerfile .
```
