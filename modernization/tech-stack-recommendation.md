# Tech Stack Recommendation

## Engagement context

- Preliminary stack: C#/.NET 10+, ASP.NET Core, PostgreSQL, RabbitMQ, Redis, Docker, Azure
  Container Apps, GitHub Actions.
- Services in scope: 12.
- Frontend scope: Angular administration UI and React storefront UI, to be reimplemented as
  separate Blazor applications.
- Final provisional average automatibility: 86.6%.
- Team profile: Phase 0 established a .NET-centered target; no approved polyglot operating model
  is recorded.

## Recommendation

The Phase 4b evidence supports retaining the human-confirmed application and data stack for
every service. Phase 5 review changes local orchestration to .NET Aspire AppHost with Docker and
defers production hosting. The service profiles remain primarily relational, REST/event oriented,
and compatible with shared ASP.NET Core infrastructure.

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

## Phase 5 human adjustment

| Concern | Phase 4b decision | Phase 5 decision | Rationale |
|---|---|---|---|
| Local orchestration | Docker + Azure Container Apps | .NET Aspire AppHost + Docker | Reproducible local service/dependency graph and better developer feedback |
| Production hosting | Azure Container Apps | Deferred | Select after implementation evidence, operational constraints, and deployment testing |
| Frontend framework | Angular administration and React storefront | Blazor Web App with Interactive Auto | Standardizes the target frontend on the confirmed .NET platform while preserving both application boundaries |

The change affects local startup, service discovery, and developer documentation. It does not
change APIs, service boundaries, database ownership, messaging, or business-rule behavior.
Aspire reference: https://aspire.dev/reference/overview/

## Frontend target

The Angular administration UI and React storefront UI remain separate user-facing applications,
but both are reimplemented with Blazor Web App using Interactive Auto. Interactive Auto allows
server interactivity initially and progressively uses WebAssembly where the client bundle and
runtime support it. The existing screen inventory, navigation, terminology, and workflows remain
the brownfield compatibility baseline; visual styling, responsiveness, accessibility, and client
performance are modernized.

| Application | Legacy implementation | Target implementation | Status |
|---|---|---|---|
| Administration | Angular 11 SPA | Blazor Web App, Interactive Auto | Target selected; specification pending |
| Storefront | React 16.6 SPA | Blazor Web App, Interactive Auto | Target selected; specification pending |

Frontend specifications will be created separately under `spec/frontend/` for each application.
They must bind browser-facing API paths to the frozen backend contracts through the approved
gateway/BFF access pattern.

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
- Serverless appetite: not selected for the initial implementation because the services need
  relational ownership, messaging, and consistent runtime conventions.
- Team growth: constrain the initial implementation to current .NET expertise.
- Production hosting: intentionally deferred; Docker/OCI images remain portable for a later
  platform decision.

**Phase 4b decision:** Accepted the application and data stack under Mode A.

**Phase 5 adjustment:** Adopted .NET Aspire + Docker for local orchestration and deferred
production hosting.
