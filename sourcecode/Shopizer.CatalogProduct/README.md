# Shopizer Catalog and Product (MS-02)

This service owns products, categories, variants, regional availability, prices, media
metadata, and atomic inventory reservations. The API base path is
`/api/v1` and the Aspire endpoint is `catalog-product` (port `8102`).

Run it through the Aspire host:

```bash
dotnet run --project sourcecode/Shopizer.AppHost
```

The service expects the Aspire resources `catalogproductdb` (PostgreSQL) and `rabbitmq`.
Persistence uses raw Npgsql commands; there is no ORM. Startup applies idempotent DDL and
additive migrations in `Data/SchemaInitializer.cs`.

Every request must carry `x-tenant-id`, `x-store-id`, and `x-correlation-id`. Mutation
endpoints require a bearer administrator principal with catalog-management permission.
Events are written to `catalog_product.event_outbox` in the mutation transaction and then
published to RabbitMQ's durable `domain-events` topic exchange.

The contract is the authority for names, paths, payloads, and status codes:
`spec/microservices/ms-02/04-api-contract.yaml`. DTOs in `DTOs/` are copied verbatim from
`spec/microservices/ms-02/08-dtos/`.

For a container build, use `sourcecode/` as the build context:

```bash
docker build -f Shopizer.CatalogProduct/Dockerfile sourcecode
```
