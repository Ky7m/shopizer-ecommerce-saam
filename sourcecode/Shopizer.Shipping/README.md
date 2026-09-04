# Shopizer Shipping (MS-09)

MS-09 owns tenant/store-scoped shipping origins and immutable quote snapshots. It
implements destination policy, packaging facts, option selection, free-shipping
evaluation, and configuration projections. Carrier and Maps calls are represented
by the `ShippingAdapterExecutionRequested.v1` outbox event for MS-12.

## Run locally

```bash
dotnet run --project sourcecode/Shopizer.Shipping
```

Aspire supplies `ConnectionStrings__shopizerDb` and RabbitMQ. PostgreSQL is
initialized with the `shipping` schema using Npgsql ADO.NET; no ORM is used.
Required request headers are `x-tenant-id` and `x-store-id`; clients should also
send `x-correlation-id`. The API base path is `/api/v1` and the health endpoints
are supplied by `Shopizer.ServiceDefaults`.

Administrative endpoints require a bearer token with one of `SUPERADMIN`, `ADMIN`,
`SHIPPING`, or `ADMIN_RETAIL`. `Shipping:JwtSecret` must match the identity token
issuer in a deployed environment and must be explicitly configured outside
Development.

## Docker

Build from the `sourcecode/` directory:

```bash
docker build -f Shopizer.Shipping/Dockerfile .
```

The frozen contract is `spec/microservices/ms-09/04-api-contract.yaml`; the
verbatim DTO binding is in `Shopizer.Shipping/DTOs/`.
