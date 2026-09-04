# Shopizer Content and Configuration (MS-11)

This service owns tenant/store-scoped CMS content, localized descriptions, provider-backed
content-file metadata, merchant configuration projections, and integration-module metadata.
It does not execute payment/shipping providers or store file bytes in PostgreSQL.

## Running

From `sourcecode/`, run `dotnet run --project Shopizer.ContentConfiguration`. Aspire supplies
the `shopizerDb` PostgreSQL resource and `rabbitmq`; the service listens on port `8111` when
started by the AppHost. The database schema is initialized idempotently with Npgsql ADO.NET.
Local provider bytes are stored below `ContentConfiguration:StorageRoot` (the application
content-storage directory by default).

All requests require `x-tenant-id` and `x-correlation-id`; store-scoped requests also require
`x-store-id`. Private operations require a bearer administrator token. Local development may
use the externally issued JWT from Customer Identity. Provider selection is explicit through
`ContentConfiguration:CmsMethod` (`default`, `httpd`, `aws`, or `gcp`) and never falls back.

The contract base path is `/api/v1`. The authoritative contract is
`spec/microservices/ms-11/04-api-contract.yaml`; DTOs under `DTOs/` are copied verbatim from
`spec/microservices/ms-11/08-dtos/`.

## Container

Build with `docker build -f Shopizer.ContentConfiguration/Dockerfile .` from `sourcecode/`.
The image exposes port `8080`; Aspire maps the service's development endpoint to `8111`.

## Boundaries

Configuration mutations persist encrypted integration values and publish only configuration
references. Provider validation/execution belongs outside MS-11. The graph service node was
unavailable during this implementation; BR traceability is retained in source annotations and
the implementation audit.
