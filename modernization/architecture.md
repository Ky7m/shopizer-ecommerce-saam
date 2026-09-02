# Phase 4b Architecture Confirmation

Phase 4b confirms the human-selected target architecture without changing service boundaries
or introducing a polyglot runtime.

## Confirmed technology stack

| Concern | Confirmed choice |
|---|---|
| Language/runtime | C# on .NET 10+ |
| Web framework | ASP.NET Core |
| Database | PostgreSQL, one owned schema per service |
| Messaging | RabbitMQ with versioned events |
| Cache | Redis, non-authoritative |
| Local orchestration | .NET Aspire AppHost with Docker |
| Production hosting | Deferred; container target remains portable |
| Frontend | Blazor Web App with Interactive Auto for administration and storefront |
| CI/CD | GitHub Actions |
| Authentication | Central OIDC provider with local OAuth2 token validation |
| Observability | OpenTelemetry and structured logging |

## Phase 4b evidence

- 303 active rules across 12 services.
- Final provisional average automatibility: 86.6%.
- Ten Type A services and two Type B services; no Type C services.
- All services exceed the 75% implementation threshold.
- No Layer C DB-placement candidate met the evidence threshold; application-tier placement is
  retained.

## Reconciliation

This document is the Phase 4b confirmation companion to
`modernization/modernized-architecture.md`. The latter remains the detailed target topology,
while this document records the evidence-based decision and its implementation implications.
All services must follow `spec/shared/infrastructure-patterns.md`.

## Phase 5 solution structure

Implementation follows the Microsoft Aspire Shop sample structure at
`https://github.com/microsoft/aspire-samples/tree/main/samples/aspire-shop`:

- `Shopizer.AppHost` owns the local distributed application graph and dependency ordering.
- `Shopizer.ServiceDefaults` contains shared health, service discovery, resilience, and
  OpenTelemetry defaults.
- Each of the 12 backend services is an independent ASP.NET Core project with its own data
  access, migrations, API endpoints, and container boundary.
- `Shopizer.Admin` and `Shopizer.Storefront` are separate Blazor Web App projects using
  Interactive Auto.
- Database model/migration code remains beside its owning service; a separate database manager is
  added only for lifecycle operations that must be independently orchestrated.
