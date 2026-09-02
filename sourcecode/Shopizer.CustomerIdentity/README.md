# Shopizer Customer and Identity (MS-01)

ASP.NET Core/.NET 10 service for tenant- and store-scoped customers,
administrators, credentials, addresses, reviews, newsletter consent and external
identity links.

## Run locally

```bash
dotnet run --project sourcecode/Shopizer.CustomerIdentity
```

When `ConnectionStrings__customeridentitydb` (Aspire), `DATABASE_URL`, or
`ConnectionStrings:customeridentitydb` is present, PostgreSQL is the primary
store and the service initializes the `customer_identity` schema. With no database
configuration, the explicitly logged development fallback store is selected.

Required request context is `x-tenant-id`, `x-store-id`, and
`x-correlation-id`. The contract base path is `/api/v1`; health remains provided
by `Shopizer.ServiceDefaults`.

## Docker

Build from the `sourcecode/` directory so the project reference to
`Shopizer.ServiceDefaults` remains available:

```bash
docker build -f Shopizer.CustomerIdentity/Dockerfile .
```

The frozen contract and copied DTO binding are in
`spec/microservices/ms-01/04-api-contract.yaml` and
`Shopizer.CustomerIdentity/DTOs/`.
