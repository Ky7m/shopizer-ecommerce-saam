# Shopizer 3.2.7 — Modernization

## System Overview

| Attribute | Value |
|-----------|-------|
| System | Shopizer 3.2.7 |
| Business Domain | E-commerce / headless commerce |
| Legacy Stack | Java 11, Maven, Spring Boot, Spring Security, Hibernate/JPA, Drools |
| Codebase Size | Approximately 308,769 textual LOC; 2,345 source files; 8 application/build units |
| Analysis Mode | Hybrid, CAST verified for all three in-scope applications |
| Target Stack | C#/.NET 10+, ASP.NET Core, PostgreSQL, RabbitMQ, Redis, Docker, Azure Container Apps |
| Started | 2026-08-31 |

## Current Status

| Phase | Status | Completed |
|-------|--------|-----------|
| Phase 0: Onboarding | ✅ Complete | 2026-08-31 |
| Phase 1: Bottom-Up | ✅ Complete | 2026-08-31 |
| Phase 2: Top-Down | ✅ Complete | 2026-08-31 |
| Phase 3: Convergence | ✅ Complete | 2026-09-01 |
| Phase 4: Specification | — | — |
| Phase 4a: Rule Validation | ✅ Complete | 2026-09-02 |
| Phase 4b: Roadmap | ✅ Complete | 2026-09-02 |
| Phase 4c: Test Suites | — | — |
| Phase 5: Implementation | IN_PROGRESS — all 12 backend services implemented; runtime validation pending | 2026-09-04 |

## Phase 5 Service Implementation Status

| Service | Status | Notes |
|---|---|---|
| MS-01 Customer and Identity | IN_PROGRESS | Existing reference implementation; follow-up validation and lifecycle work remain. |
| MS-02 Catalog and Product | IN_REVIEW | Existing implementation with 41 BR-IDs. |
| MS-03 Search | IN_REVIEW | Existing implementation with 10 BR-IDs. |
| MS-04 Cart and Checkout | IN_REVIEW | Existing implementation with 20 BR-IDs; provider-backed runtime paths remain dependent on downstream services. |
| MS-05 Order Management | COMPLETE | Existing implementation with 23 BR-IDs and passing focused validation. |
| MS-06 Payments | IN_REVIEW | 19 rules implemented with raw Npgsql persistence, provider boundaries, outbox events, and AppHost wiring. Aspire integration validation is blocked by the repository test-platform/runtime setup. |
| MS-07 Pricing and Promotions | BLOCKED | 13 rules implemented with price selection, promotion evaluation, persistence, and outbox events. Aspire integration validation is blocked by the repository test-platform/runtime setup. |
| MS-08 Tax | IN_REVIEW | 20 rules, 15 contract endpoints, raw Npgsql persistence, auth/tenancy middleware, and persisted calculation quotes implemented. Aspire integration validation is blocked by the repository test-platform/runtime setup. |
| MS-09 Shipping | BLOCKED | 24 rules implemented with quote calculation, packaging, provider selection, persistence, and outbox events. Aspire integration validation is blocked by the repository test-platform/runtime setup. |
| MS-10 Merchant and Store Administration | BLOCKED | 21 rules implemented with store lifecycle, branding, signup verification, persistence, and outbox events. Aspire integration validation is blocked by the repository test-platform/runtime setup. |
| MS-11 Content and Configuration | BLOCKED | 39 rules implemented with content/file/configuration operations, persistence, and outbox events. Aspire integration validation is blocked by the repository test-platform/runtime setup. |
| MS-12 Platform Integrations | BLOCKED | 23 rules implemented with adapter, delivery, storage, email, geolocation, and event operations. Aspire integration validation is blocked by the repository test-platform/runtime setup. |

## Phase 1 Completion Summary

The eight planned bottom-up segments were analyzed using the Hybrid workflow: CAST structural
inventory plus direct source extraction. The phase produced 180 business rules, identified 38
integration points, and recorded graph effective-confidence results of 49 high and 131 medium
rules. The full graph inventory contains 1,138 source components, including 473 business-layer
components; 61 business components currently have extracted-rule coverage.

## Phase 2 Completion Summary

The top-down architecture defined 12 target services across the confirmed nine business
capabilities. The human-approved preliminary stack is C# (.NET 10+), ASP.NET Core, PostgreSQL,
RabbitMQ, Redis, Docker with Azure Container Apps, and GitHub Actions. Architecture decisions
cover REST plus RabbitMQ event integration, database ownership per service, OIDC authentication,
OpenTelemetry observability, tenant propagation, and saga-based checkout/order/payment
coordination. Phase 4b remains responsible for evidence-based reconciliation.

Phase 2 artifacts are under `modernization/`: target architecture, service composition, Mermaid
ERDs, Mermaid process flows, implementation roadmap, and risk analysis. The graph now contains
12 service nodes, 14 direct `CALLS` edges, and 13 transitive dependency edges.

## Phase 3 Completion Summary

Convergence assigned all 180 extracted business rules to exactly one of the 12 target services:
180 `ASSIGNED_TO` edges, zero orphaned rules, and zero multiply assigned rules. All 12 top-down
process flows have backing extracted rules, so no extraction gaps were identified. Boundary
validation passed for cohesion, coupling, data ownership, and transaction scope, with explicit
ownership decisions for inventory (MS-02), order/payment transitions and download entitlements
(MS-05), payment state and provider operations (MS-06), and merchant/module configuration (MS-11).
Administrative capture and refund remains a required target capability.

Phase 3 artifacts are under `assessment/` and `validation/`: the complete feature/rule matrix,
gap analysis, comprehensive validation summary, updated decision register, convergence tracker,
and `.saam/telemetry/phase3-convergence.yaml`. Phase 4 specification generation is not activated;
the operator will start it manually.

## Phase 4a Completion Summary

Phase 4a used Mode A agent defaults, approved by the operator on 2026-09-02. The 306 specification rules were reconciled to 303 active rules and 3 obsolete rules were moved to service appendices (0.98% scope reduction). Retained rules are classified as Core or Active and carry Critical, High, Medium, or Low impact weights. 86 active rules have Critical weight. No rules were deferred, merged, or simplified. The 23 unresolved Stage 1.5 event bindings remain explicit review items for downstream planning.

## Phase 4b Completion Summary

Phase 4b scored all 12 services across statement clarity, algorithm completeness, integration
definition, data-model readiness, and edge-case coverage using calibration version 7. Mode A
recommendations raised the average provisional automatibility from 84.0% to 86.6%; 10 services
are Type A, 2 are Type B, and none are Type C. All services exceed the 75% implementation
threshold. Model B (Transform plus GitHub Copilot) is recommended at approximately 4–5 weeks
using two parallel tracks; Model C is an alternative at approximately 2 weeks elapsed after
dependency wiring. The phase also confirmed the .NET stack, documented team composition,
recorded no qualifying Layer C placement candidates, and added shared infrastructure patterns.
The 12 Mode A resolutions remain explicitly inferred risks for implementation review.

## Analysis Scope

The loaded application sources are under `initial-source/shopizer-3.2.7/` (backend),
`initial-source/shopizer-admin-main/` (Angular administration), and
`initial-source/shopizer-shop-reactjs-main/` (React storefront). Supplemental architecture, API,
model, build, and configuration documentation is under `initial-source/documentation-master/`.
All source is treated as read-only reference material.

CAST Imaging is configured and verified for `Shopizer-Backend`, `Shopizer-WebAdmin`, and
`Shopizer-WebFrontEnd`. CAST reports 94,528 backend LOC, 82,284 admin LOC, and 29,251 storefront
LOC. The admin and storefront have 178 and 199 analyzed dependencies into the backend,
respectively. Direct source evidence remains required for detailed business-rule semantics.

## Segmentation

| Segment | Description | Components / Files |
|---------|-------------|-------------------|
| Catalog and product | Catalogs, products, categories, variants, attributes, manufacturers, and search-facing models | 342 |
| Customer, user, and security | Customer, user, permission, group, credential, address, and authentication concerns | 178 |
| Cart, checkout, orders, and payments | Cart, checkout, order, totals, transaction, download, and payment flows | 150 |
| Merchant, store, content, and configuration | Merchant stores, CMS/content, store context, system configuration, and administration | 216 |
| Pricing, promotions, tax, and shipping | Tax, shipping, order totals, and promotion-related logic | 71 |
| External integrations and platform extensions | Payment/shipping SPI, search, email, files/images, maps, and shared extensions | 210 |
| Web administration frontend | Angular administration application and backend API client usage | 721 files |
| Storefront frontend | React storefront application and backend API client usage | 420 files |

## Key Decisions and Assumptions

- Analysis mode: Hybrid, with CAST structural evidence and direct source business-rule extraction.
- Assumed target stack: .NET 10; Phase 4b has final authority.
- The eight-segment split is confirmed for initial analysis and may be refined by CAST evidence.
- Database objects are primarily represented by JPA/Hibernate entities; the `SALESMANAGER` schema is
  updated through Hibernate rather than a complete checked-in DDL script.

## Directory Structure

```text
├── .github/
│   ├── skills/saam-*/
│   └── skills/saam-application-context/
├── .saam/telemetry/
├── assessment/
├── graph-mcp/
├── initial-source/
│   ├── shopizer-3.2.7/
│   ├── shopizer-admin-main/
│   ├── shopizer-shop-reactjs-main/
│   └── documentation-master/
├── inventory/
├── tracking/
└── validation/
```

## Artifacts

- `inventory/INDEX.md` — source inventory, counts, conventions, CAST status, and segmentation.
- `assessment/assessment_agenda.md` — human decision register.
- `tracking/phase0-onboarding.md` — Phase 0 task status.
- `.saam/telemetry/` — engagement and Phase 0 telemetry.
- `.github/skills/saam-application-context/SKILL.md` — auto-loaded application context for source work.

## Stakeholders

Known participation currently consists of the operator and the SAAM agent. Additional business
stakeholders and subject-matter experts are not yet identified.
