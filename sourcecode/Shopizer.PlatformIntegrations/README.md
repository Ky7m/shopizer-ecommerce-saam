# Shopizer Platform Integrations (MS-12)

MS-12 owns tenant/store-scoped adapter projections, provider execution boundaries,
provider-neutral local storage, email queue state, transactional outbox records,
delivery attempts, replay and dead-letter lineage. Merchant configuration remains
owned by MS-11; order and shipping quote persistence remain owned by MS-05/MS-09.

## Run locally

```bash
dotnet run --project sourcecode/Shopizer.PlatformIntegrations
```

Aspire supplies `shopizerDb` (the shared PostgreSQL resource) and `rabbitmq`.
The service initializes the `platform_integrations` schema with Npgsql ADO.NET;
no ORM is used. `Storage:RootPath` may select the local provider root. Provider
endpoint URIs and credentials are supplied through an adapter refresh; credentials
are retained only in process memory for provider execution and never persisted.

Every request requires `x-tenant-id`, `x-store-id`, `x-correlation-id`, and a
Bearer authorization token. The contract base path is
`/api/v1`; the allocated Aspire port is `8112`.

## Docker

Build from `sourcecode/`:

```bash
docker build -f Shopizer.PlatformIntegrations/Dockerfile .
```

The frozen API contract and verbatim DTO binding are in
`spec/microservices/ms-12/04-api-contract.yaml` and
`spec/microservices/ms-12/08-dtos/`.
