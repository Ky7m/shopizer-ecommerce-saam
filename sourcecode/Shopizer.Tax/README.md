# Shopizer Tax (MS-08)

MS-08 owns tenant/store-scoped tax classes, tax rates and localized descriptions,
tax policy configuration, and persisted synchronous tax quotes. It does not read or
write MS-01, MS-02, MS-04, MS-05, MS-09, or MS-10 tables.

Run locally from the repository root:

```bash
dotnet run --project sourcecode/Shopizer.Tax
```

The service uses the Aspire PostgreSQL connection named `shopizerDb` and creates its
objects in the `tax_schema` PostgreSQL schema. Persistence is raw Npgsql ADO.NET;
there is no ORM. Under Aspire the resource is registered by
`sourcecode/Shopizer.AppHost/AppHost.cs` as `tax`, with the contract port `8008`.

All requests require `x-tenant-id`, `x-store-id`, `x-correlation-id`, and a valid
Bearer token. Administrator identity is required for tax-class, tax-rate, and
configuration mutations. Tax reads and calculations accept an authenticated
customer or administrator identity. The signing secret must be shared with the
identity service in deployed environments through `Tax:JwtSecret` (or the
`CustomerIdentity:JwtSecret` configuration key).

The API is rooted at `/api/v1`; its frozen naming and response contract is
`spec/microservices/ms-08/04-api-contract.yaml`. The authoritative business rules
are in `spec/microservices/ms-08/01-business-rules.md`.

The Docker build context is `sourcecode/`:

```bash
docker build -f Shopizer.Tax/Dockerfile sourcecode
```
