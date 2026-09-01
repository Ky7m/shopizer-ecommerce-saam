# Phase 4: Specification Generation — Task Tracker

## Status: IN_PROGRESS

## Summary
| Metric | Value |
|--------|-------|
| Total tasks | 12 |
| Completed | 1 |
| In progress | 1 |
| Blocked | 0 |
| Started | 2026-09-01T12:52:17+04:00 |
| Last updated | 2026-09-01T13:10:18+04:00 |

## Tasks

### Backend Service Specifications — Provider-First Order

| # | Task | Status | Jira | Assignee | Notes |
|---|------|--------|------|----------|-------|
| 1 | Generate Customer and Identity specification (MS-01) | DONE | — | Agent | 2026-09-01T13:10:18+04:00 — 51 rules, 14 tables, 39 endpoint methods, OpenAPI 3.1 contract |
| 2 | Generate Merchant and Store Administration specification (MS-10) | PENDING | — | Agent | Provider for tenant and store scope |
| 3 | Generate Catalog and Product specification (MS-02) | PENDING | — | Agent | Depends on MS-10 store scope |
| 4 | Generate Search specification (MS-03) | PENDING | — | Agent | Consumes catalog and content events |
| 5 | Generate Pricing and Promotions specification (MS-07) | PENDING | — | Agent | Depends on MS-10 store scope |
| 6 | Generate Tax specification (MS-08) | PENDING | — | Agent | Calculation provider |
| 7 | Generate Shipping specification (MS-09) | PENDING | — | Agent | Calculation provider and carrier integration boundary |
| 8 | Generate Cart and Checkout specification (MS-04) | PENDING | — | Agent | Consumes customer, catalog, pricing, tax, and shipping contracts |
| 9 | Generate Payments specification (MS-06) | PENDING | — | Agent | Provider/external boundary for order payment state |
| 10 | Generate Order Management specification (MS-05) | PENDING | — | Agent | Consumes checkout and payment events |
| 11 | Generate Content and Configuration specification (MS-11) | PENDING | — | Agent | Depends on store scope; publishes content/configuration events |
| 12 | Generate Platform Integrations specification (MS-12) | PENDING | — | Agent | Consumes business events and invokes external adapters |

### Stage 1.5 — Cross-Service Compilation

| Deliverable | Status | Notes |
|---|---|---|
| Generate all 05-dependencies.md files | PENDING | After all provider contracts exist |
| Reconcile synchronous consumer-provider contracts | PENDING | Produce spec/shared/cross-service-contracts.md |
| Compile event schemas and shared conventions | PENDING | Human reconciliation required for convention normalization |
| Pin shared dependency versions | PENDING | Target stack currently confirmed as .NET 10 |

### Stage 1.6–1.8 — Shared Compilation

| Deliverable | Status | Notes |
|---|---|---|
| Compile per-service and cross-service workflows | PENDING | After dependency contracts are stable |
| Compile entity lifecycle and invariants | PENDING | Validate closed state machines |
| Compile shared extensibility model | PENDING | No shared engine currently identified; verify during extraction |

### Stage 2 — Frontend Specification

| Deliverable | Status | Notes |
|---|---|---|
| Generate frontend specifications or document skip | PENDING | UI exists; requires human asset-reuse and access-pattern decisions |
