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
| Runtime | Docker on Azure Container Apps |
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
