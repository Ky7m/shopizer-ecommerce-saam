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
| Phase 1: Bottom-Up | Approved to start | — |
| Phase 2: Top-Down | Approved to start | — |
| Phase 3: Convergence | — | — |
| Phase 4: Specification | — | — |
| Phase 4a: Rule Validation | — | — |
| Phase 4b: Roadmap | — | — |
| Phase 4c: Test Suites | — | — |
| Phase 5: Implementation | — | — |

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
