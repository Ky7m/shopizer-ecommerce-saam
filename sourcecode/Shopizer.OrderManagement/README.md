# Shopizer Order Management (MS-05)

Order aggregate, immutable purchase snapshots, lifecycle history, payment outcome
projection, cancellation compensation, fulfillment coordination, and invoice
request boundary implemented with ASP.NET Core/.NET 10 and raw Npgsql ADO.NET.

Run locally with:

```bash
dotnet run --project sourcecode/Shopizer.OrderManagement
```

Aspire supplies `ConnectionStrings__shopizerDb` and the RabbitMQ `rabbitmq`
resource. The service creates the `order_management` schema idempotently. Every
request requires `Authorization`, `x-tenant-id`, `x-store-id`, and
`x-correlation-id`; mutating contract operations additionally require
`Idempotency-Key`.

The HTTP contract base is `/api/v1`, on port `8105` under Aspire. The frozen API
contract and DTO binding are `spec/microservices/ms-05/04-api-contract.yaml` and
`spec/microservices/ms-05/08-dtos/`.

Build the container from `sourcecode/`:

```bash
docker build -f Shopizer.OrderManagement/Dockerfile .
```

MS-06, MS-09/MS-12, MS-02, and MS-12 retain ownership of provider execution,
carrier execution, inventory, and invoice artifacts respectively. MS-05 publishes
durable outbox boundaries and never writes another service's schema.
