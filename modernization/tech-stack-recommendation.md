# Tech Stack Recommendation

## Engagement context

- Preliminary stack: C#/.NET 10+, ASP.NET Core, PostgreSQL, RabbitMQ, Redis, Docker, Azure
  Container Apps, GitHub Actions.
- Services in scope: 12.
- Final provisional average automatibility: 86.6%.
- Team profile: Phase 0 established a .NET-centered target; no approved polyglot operating model
  is recorded.

## Recommendation

The Phase 4b evidence supports retaining the human-confirmed stack for every service. The
service profiles are primarily relational, REST/event oriented, and compatible with shared
ASP.NET Core infrastructure. No service has evidence strong enough to justify a separate
runtime or database.

| Service | Language | Framework | Database | Events | Decision |
|---|---|---|---|---|---|
| MS-01 Customer and Identity | C#/.NET 10+ | ASP.NET Core | PostgreSQL | RabbitMQ + outbox | Accept |
| MS-02 Catalog and Product | C#/.NET 10+ | ASP.NET Core | PostgreSQL | RabbitMQ + outbox | Accept |
| MS-03 Search | C#/.NET 10+ | ASP.NET Core | PostgreSQL | RabbitMQ consumer/outbox | Accept |
| MS-04 Cart and Checkout | C#/.NET 10+ | ASP.NET Core | PostgreSQL | RabbitMQ + outbox/inbox | Accept |
| MS-05 Order Management | C#/.NET 10+ | ASP.NET Core | PostgreSQL | RabbitMQ + outbox/inbox | Accept |
| MS-06 Payments | C#/.NET 10+ | ASP.NET Core | PostgreSQL | RabbitMQ + provider webhooks | Accept |
| MS-07 Pricing and Promotions | C#/.NET 10+ | ASP.NET Core | PostgreSQL | RabbitMQ + outbox | Accept |
| MS-08 Tax | C#/.NET 10+ | ASP.NET Core | PostgreSQL | RabbitMQ optional; REST primary | Accept |
| MS-09 Shipping | C#/.NET 10+ | ASP.NET Core | PostgreSQL | RabbitMQ + adapter events | Accept |
| MS-10 Merchant and Store Administration | C#/.NET 10+ | ASP.NET Core | PostgreSQL | RabbitMQ + outbox | Accept |
| MS-11 Content and Configuration | C#/.NET 10+ | ASP.NET Core | PostgreSQL | RabbitMQ + outbox | Accept |
| MS-12 Platform Integrations | C#/.NET 10+ | ASP.NET Core | PostgreSQL | RabbitMQ consumer/outbox | Accept |

## Architecture impact

No stack change is required. `modernization/modernized-architecture.md` remains aligned with
the recommendation; `modernization/architecture.md` records the Phase 4b confirmation. The
shared infrastructure patterns document makes health, error, tenancy, startup, logging, and
messaging conventions mandatory across services.

## Constraints considered

- Team expertise: 40% — strongly favors the confirmed .NET stack.
- Service complexity profile: 30% — calculation-heavy services benefit from a strongly typed
  common runtime.
- ATX/Transform compatibility: 15% — shared .NET generation and contract tooling reduce wiring
  variation.
- Operational consistency: 15% — one deployment, observability, and security model reduces
  cross-service support cost.

## Global assumptions carried forward

- Polyglot tolerance: low; operate one primary stack unless later evidence proves a service
  cannot meet its requirements.
- Serverless appetite: not selected for the initial migration because the services need
  relational ownership, messaging, and consistent runtime conventions.
- Team growth: constrain the initial implementation to current .NET expertise.

**Phase 4b decision:** Accept all recommendations under Mode A; no architecture override was
required.
