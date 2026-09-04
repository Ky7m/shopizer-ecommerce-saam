# Shopizer Pricing and Promotions (MS-07)

MS-07 owns tenant/store-scoped price lists and price entries, calculates effective
product and variant prices, and evaluates promotion-code reductions. Product,
variant, availability, customer, cart, order, tax, and shipping entities remain
owned by their respective services; catalog identifiers are opaque here.

## Run

From `sourcecode/`, run the Aspire host:

```bash
dotnet run --project Shopizer.AppHost
```

The service uses the Aspire `shopizerDb` PostgreSQL connection and the `rabbitmq`
resource. `Data/SchemaInitializer.cs` creates the `pricing_promotions` schema
idempotently and applies additive migrations on startup. No ORM is used.

## HTTP contract

- Base path: `/api/v1`
- Local endpoint: `http://localhost:8107`
- Contract: `spec/microservices/ms-07/04-api-contract.yaml`
- JSON fields and query parameters use camelCase; URL resources use kebab-case.
- Every operation requires `x-tenant-id`, `x-store-id`, and `x-correlation-id`.
- Price administration and processor inspection require an administrator bearer
  token with `ADMIN`, `SUPERADMIN`, `PRICING_ADMIN`, or `STORE_ADMIN`.
- Calculation endpoints return computed values only; shipping, handling, tax, and
  grand-total calculation are downstream responsibilities.

Price mutations write a transactional `event_outbox` row before the RabbitMQ
`PriceChanged.v1` publish attempt. RabbitMQ failures are logged and remain
recoverable from the outbox.

## Container

Build with `sourcecode/` as the Docker build context:

```bash
docker build -f Shopizer.PricingPromotions/Dockerfile .
```

The image listens on port 8080.
