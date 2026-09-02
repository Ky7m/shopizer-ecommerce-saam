# Shopizer 3.2.7 — Modernization

## System Overview

| Attribute | Value |
|-----------|-------|
| System | Shopizer 3.2.7 |
| Business Domain | E-commerce / headless commerce |
| Legacy Stack | Java 11, Maven, Spring Boot, Spring Security, Hibernate/JPA, Drools |
| Codebase Size | Approximately 308,769 textual LOC; 2,345 source files; 8 application/build units |
| Analysis Mode | Hybrid, CAST verified for all three in-scope applications |
| Target Stack (assumed) | .NET 10; non-binding until Phase 4b |
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
| Phase 4b: Roadmap | — | — |
| Phase 4c: Test Suites | — | — |
| Phase 5: Implementation | — | — |

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
