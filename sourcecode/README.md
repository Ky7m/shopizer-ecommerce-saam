# Shopizer Implementation Solution

The Phase 5 implementation follows the structure of Microsoft's
[Aspire Shop sample](https://github.com/microsoft/aspire-samples/tree/main/samples/aspire-shop),
adapted to Shopizer's 12 service boundaries and two frontend applications.

## Solution layout

```text
Shopizer.slnx
Shopizer.AppHost/                 # Aspire distributed application graph
Shopizer.ServiceDefaults/         # Health, discovery, resilience, telemetry defaults
Shopizer.Admin/                   # Blazor Web App host + Interactive Auto client
  Shopizer.Admin/
  Shopizer.Admin.Client/
Shopizer.Storefront/              # Blazor Web App host + Interactive Auto client
  Shopizer.Storefront/
  Shopizer.Storefront.Client/
Shopizer.<ServiceName>/           # One ASP.NET Core project per backend service
```

Backend projects:

```text
Shopizer.CustomerIdentity
Shopizer.CatalogProduct
Shopizer.Search
Shopizer.CartCheckout
Shopizer.OrderManagement
Shopizer.Payments
Shopizer.PricingPromotions
Shopizer.Tax
Shopizer.Shipping
Shopizer.MerchantAdministration
Shopizer.ContentConfiguration
Shopizer.PlatformIntegrations
```

Validation projects remain outside service source directories:

```text
validation/aspire-integration/Shopizer.AspireIntegrationTests.csproj
```

## Structural rules

- `Shopizer.AppHost` is the only project that defines the local distributed resource graph.
- AppHost references are added only for dependencies declared in the service's
  `05-dependencies.md`; a PostgreSQL database resource is created per service, while RabbitMQ
  and Redis references are service-specific.
- Every executable project references `Shopizer.ServiceDefaults` and calls
  `AddServiceDefaults()` during startup.
- Backend services own their entities, repositories, migrations, API endpoints, and event
  handlers. Cross-service database access is forbidden.
- DTOs are copied from the matching `spec/microservices/<service>/08-dtos/` directory without
  renaming.
- API paths, status codes, and response fields come from the matching
  `04-api-contract.yaml`.
- `Shopizer.Admin` and `Shopizer.Storefront` call backend APIs through the documented gateway/BFF
  edge and preserve the legacy screen and workflow inventory.
- Database-manager projects may be added beside a service only when initialization or migration
  orchestration requires a separate lifecycle, following the sample's `CatalogDbManager` pattern.
- `Aspire.Hosting.Testing` suites validate the complete AppHost resource graph; SAAM shell suites
  remain the contract and business-rule acceptance authority under `validation/<service>/`.
