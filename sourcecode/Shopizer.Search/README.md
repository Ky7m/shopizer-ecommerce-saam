# Shopizer Search (MS-03)

This service implements the provider-neutral Search API on ASP.NET Core 10. Its local
adapter uses the six owned PostgreSQL projection tables in the `search` schema; it does
not read or write another service's schema.

## Running

Run from `sourcecode/` with .NET Aspire:

```text
dotnet run --project Shopizer.AppHost
```

The service is registered as `search`, uses the `searchdb` database and RabbitMQ, and
is exposed on port 8103 by the AppHost. Required context headers on every request are
`x-tenant-id`, `x-store-id`, and `x-correlation-id`. Rebuild additionally requires a
Bearer administrator token and `idempotency-key`.

The API base path is `/api/v1`. The frozen OpenAPI contract and DTO source are:
`spec/microservices/ms-03/04-api-contract.yaml` and `spec/microservices/ms-03/08-dtos/`.
Search and autocomplete are public after context validation. Rebuild accepts roles
`SUPERADMIN`, `ADMIN`, `ADMIN_CATALOGUE`, and `ADMIN_RETAIL`.

Configuration is under `Search`: `Enabled`, `NoIndex`, `ProviderAvailable`, `Provider`,
`DefaultLocale`, and `Locales`. `local-postgresql` is a concrete provider-neutral
adapter, not a shell response. Set `Enabled=false` or `NoIndex=true` to expose the
specified disabled outcome.

## Container

The Docker build context is `sourcecode/`:

```text
docker build -f Shopizer.Search/Dockerfile .
```

The image listens on port 8080. Schema creation and additive migrations run at startup.
