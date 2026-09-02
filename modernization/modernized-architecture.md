# Shopizer 3.2.7 Modernized Target Architecture

## Architecture intent

The target system is a tenant-aware headless commerce platform. The architecture separates
catalog truth, customer identity, checkout coordination, order lifecycle, payment state,
commercial calculation, fulfillment quotes, merchant administration, content, search, and
platform adapters. Each service owns its data and publishes stable domain events through
RabbitMQ.

## Confirmed technology stack

| Layer | Decision | Status |
|---|---|---|
| Language | C# (.NET 10+) | Human confirmed; preliminary until Phase 4b |
| Framework | ASP.NET Core | Human confirmed; preliminary until Phase 4b |
| Database | PostgreSQL | Human confirmed; one logical schema/database ownership per service |
| Messaging | RabbitMQ | Human confirmed; asynchronous integration backbone |
| Cache | Redis | Human confirmed; cache and short-lived coordination data only |
| Local orchestration | .NET Aspire AppHost + Docker | Phase 5 human confirmed |
| Production hosting | Deferred | To be selected after implementation evidence |
| Frontend | Blazor Web App with Interactive Auto | Phase 5 human confirmed for separate administration and storefront applications |
| CI/CD | GitHub Actions | Human confirmed |
| Authentication | Central OIDC provider with OAuth2 access tokens | Architecture decision; provider remains deployable/configurable |
| Observability | OpenTelemetry, structured logs, metrics, distributed traces | Architecture decision |

Phase 4b may reconcile technology choices against migration evidence, operational constraints,
and cost. It must not silently replace the human-confirmed stack.

## Communication and integration

1. Blazor storefront and administration clients use an API gateway/BFF over HTTPS. The gateway
   validates tokens, resolves the merchant/store context, applies rate limits, and routes
   requests to service APIs. The two applications remain separate deployable frontends.
2. Synchronous REST is used for short-lived queries and commands where the caller needs an
   immediate result: catalog reads, cart mutations, quote requests, and administrative CRUD.
3. RabbitMQ carries domain events and long-running integration work. Events use versioned
   envelopes containing `eventId`, `eventType`, `eventVersion`, `occurredAt`, `tenantId`,
   `correlationId`, and a typed payload.
4. Services use the transactional outbox pattern when a database change and event publication
   must be coordinated. Consumers use an inbox/idempotency store and acknowledge messages only
   after successful processing.
5. Retries use bounded exponential backoff. Poison messages move to a dead-letter exchange
   with an operator-visible reason and correlation ID.

## Data ownership and consistency

- Each service owns its PostgreSQL schema and migrations. A service may expose read APIs but
  never shares tables or writes another service's schema.
- Cross-service references are opaque IDs or immutable snapshots; they are not foreign keys.
- Redis is non-authoritative. Losing Redis must not lose orders, payments, or customer data.
- Catalog, price, tax, shipping, and customer data are copied into projections where low-latency
  reads require it. Projections are refreshed by events and expose freshness metadata.
- Checkout stores the price, tax, shipping, and product snapshots used for order submission.
  The order service owns the resulting order snapshot and lifecycle after acceptance.
- Payment authorization and order state are coordinated by a saga. No distributed transaction
  spans checkout, order, and payment services.

## Security and tenancy

- A centralized OIDC provider issues OAuth2 access tokens. APIs validate issuer, audience,
  expiry, scopes, and tenant claims locally.
- `tenantId` and `storeId` are required in authenticated commands and are propagated into
  events. Services verify that resource ownership matches the authenticated context.
- Secrets, provider keys, and signing material are supplied through local development secrets or
  the eventual production platform's secret store; they are never committed to the repository.
- Payment services keep provider references and tokenized identifiers only. Raw card data is
  outside the platform boundary.
- Administrative operations require explicit scopes and are audited with actor, tenant, action,
  target, correlation ID, and outcome.

## Observability

- All services emit structured JSON logs with correlation, causation, tenant, service, and
  operation identifiers. Sensitive fields are redacted at the logging boundary.
- OpenTelemetry traces cover gateway requests, REST calls, RabbitMQ publish/consume spans,
  database operations, and external provider calls.
- Metrics include request rate/latency/errors, queue depth, dead-letter count, outbox age,
  projection lag, checkout conversion, payment failure rate, and quote latency.
- Alerts are based on customer impact and recovery signals: elevated checkout failures,
  payment callback lag, stale search projections, growing dead letters, and database saturation.

## Deployment and operations

- The implementation solution follows the Microsoft Aspire Shop shape: one `.AppHost` project
  owns the distributed application graph, one `.ServiceDefaults` class library centralizes
  cross-cutting defaults, and each backend service is an independently runnable ASP.NET Core
  project.
- The administration and storefront applications are separate Blazor Web App projects using
  Interactive Auto. Database model and migration code stays with the owning service; a separate
  database-manager project is introduced only when initialization or migration orchestration
  needs its own lifecycle.
- Each service is packaged as a Docker/OCI image and runs locally through a .NET Aspire AppHost.
- Aspire provisions the local service graph and containerized PostgreSQL, RabbitMQ, and Redis
  dependencies for development and test orchestration.
- GitHub Actions runs build, unit/contract/integration tests, image scanning, and migration checks.
  Production deployment and hosting promotion are deferred until a hosting target is selected.
- PostgreSQL migrations are forward-only and run as a controlled release step. Backups,
  point-in-time recovery, and restore drills are mandatory for transactional services.
- The eventual production platform must provide independent service revisions, managed or highly
  available PostgreSQL/RabbitMQ equivalents, secret injection, and rollback support.

## Architecture decisions

| ADR | Decision | Rationale |
|---|---|---|
| ADR-001 | Database ownership per service | Prevent shared-table coupling and make service boundaries enforceable. |
| ADR-002 | REST for request/response, RabbitMQ for events | Keeps user-facing latency predictable while allowing independent consumers. |
| ADR-003 | Checkout/order/payment saga | Preserves service autonomy while making partial failure and compensation explicit. |
| ADR-004 | OIDC plus gateway and local token validation | Centralizes identity without requiring every request to traverse the gateway. |
| ADR-005 | Outbox/inbox and idempotent consumers | Prevents lost events and duplicate effects during retries. |
| ADR-006 | Tenant context in tokens and event envelopes | Makes tenant isolation explicit across synchronous and asynchronous paths. |
| ADR-007 | .NET Aspire AppHost with Docker for local orchestration; production hosting deferred | Keeps local development reproducible while preserving portable container images until production constraints are selected. |
| ADR-008 | Aspire Shop-aligned solution layout | Keeps orchestration, service defaults, independently deployable services, database lifecycle code, and the two Blazor frontends explicit and discoverable. |

## Logical topology

```mermaid
flowchart LR
    Storefront[Blazor Storefront] --> Gateway[API Gateway / BFF]
    Admin[Blazor Administration] --> Gateway
    Gateway --> Identity[MS-01 Customer and Identity]
    Gateway --> Catalog[MS-02 Catalog and Product]
    Gateway --> Search[MS-03 Search]
    Gateway --> Cart[MS-04 Cart and Checkout]
    Gateway --> Orders[MS-05 Order Management]
    Gateway --> Merchant[MS-10 Merchant and Store Administration]
    Gateway --> Content[MS-11 Content and Configuration]
    Cart -->|REST quotes| Pricing[MS-07 Pricing and Promotions]
    Cart -->|REST quotes| Tax[MS-08 Tax]
    Cart -->|REST quotes| Shipping[MS-09 Shipping]
    Cart -->|OrderSubmitted.v1 event| Orders
    Orders -->|PaymentRequested event| Payments[MS-06 Payments]
    Catalog -->|ProductChanged event| Search
    Orders -->|Integration events| Integrations[MS-12 Platform Integrations]
    Payments -->|Payment events| Orders
    Rabbit[RabbitMQ] --- Cart
    Rabbit --- Orders
    Rabbit --- Payments
    Rabbit --- Search
    Rabbit --- Integrations
    Redis[(Redis)] --- Gateway
    Redis --- Catalog
```
