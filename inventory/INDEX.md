# Shopizer 3.2.7 Source Inventory

## System Profile

| Attribute | Value |
|-----------|-------|
| Name | Shopizer 3.2.7 |
| Business Domain | E-commerce / headless commerce |
| Stack | Java 11, Maven, Spring Boot 2.5.12, Spring Security, Hibernate/JPA, Drools, Swagger 2 |
| Target Stack (assumed) | .NET 10 |
| LOC | Approximately 308,769 textual LOC across three applications: 129,048 backend, 103,664 Angular admin, and 76,057 React storefront |
| Programs/Modules | 8 application/build units: 6 backend Maven modules plus `shopizer-admin-main` and `shopizer-shop-reactjs-main` |
| Tables/Files | 85 JPA entity classes; schema is generated/updated by Hibernate, with no static table DDL found |
| Analysis Mode | Hybrid — CAST for structure when the CAST application is available; direct source for business-rule extraction |
| Source Root | `initial-source/` |
| Documentation Root | `initial-source/documentation-master/` |
| CAST Status | Verified: `Shopizer-Backend`, `Shopizer-WebAdmin`, and `Shopizer-WebFrontEnd` analyzed in CAST |

## Component Breakdown

| Component Type | Count | Description |
|----------------|-------|-------------|
| Programs | 8 | Six backend Maven modules, one Angular administration application, and one React storefront application |
| Source files | 2,345 | 1,204 backend Java files, 721 Angular-admin files, and 420 React-storefront files; dependency directories excluded |
| Screens/Forms | 2 SPA applications; no JSP files detected | Angular administration and React storefront applications |
| Database Objects | 85 mapped entities | JPA/Hibernate entities; database schema `SALESMANAGER` is managed through Hibernate |
| Batch Jobs | 0 detected | No `@Scheduled` or Quartz job implementation found in the source sweep |
| APIs/Services | 57 controller-bearing files; 370 mapping annotations | REST/Spring MVC API surface, with 63 filename-matched service classes and 71 repository/DAO classes |
| Reports | 3 invoice templates | ODS templates: default, Spanish, and French |
| Rules | 4 Drools files | Pricing, promotion, shipping, and decision rules under `sm-core` resources |
| Templates | 20 FreeMarker email templates | Customer, order, notification, marketing, and account workflows |
| Documentation | 33 Markdown files | Supplemental Shopizer architecture, API, model, build, and configuration documentation |
| CAST analyzed applications | 3 | Backend: 94,528 LOC / 16,269 elements / 72,033 interactions; admin: 82,284 / 3,107 / 4,972; storefront: 29,251 / 592 / 927 |

## Segmentation Strategy

| Segment | Description | Components / Files |
|---------|-------------|-------------------|
| Catalog and product | Catalogs, categories, products, variants, attributes, manufacturers, pricing metadata, and search-facing models | 342 |
| Customer, user, and security | Customers, users, groups, permissions, credentials, addresses, and authentication concerns | 178 |
| Cart, checkout, orders, and payments | Shopping carts, checkout, orders, totals, transactions, downloads, and payment flows | 150 |
| Merchant, store, content, and configuration | Merchant stores, store context, CMS/content, system configuration, and administration | 216 |
| Pricing, promotions, tax, and shipping | Tax classes/rates, shipping quotes/origins, order-total processors, and promotion-related logic | 71 |
| External integrations and platform extensions | Payment/shipping module SPI, search, files/images, email, integration modules, and shared platform utilities | 210 |
| Web administration frontend | Angular 11 administration application consuming backend REST APIs | 721 files; 82,284 CAST LOC |
| Storefront frontend | React 16 storefront application consuming backend REST APIs | 420 files; 29,251 CAST LOC |

Backend counts are a mechanical classification of the 1,167 main Java files. Frontend counts are source-tree file counts. These are starting points for Phase 1; CAST-derived boundaries may refine the split.

## Naming Conventions

| Pattern | Meaning | Example |
|---------|---------|---------|
| `sm-*` Maven modules | Functional/build modules | `sm-core`, `sm-shop-model` |
| `com.salesmanager...` | Java package namespace | `com.salesmanager.core.model.order` |
| `*Service` / `*ServiceImpl` | Service interface and implementation pair | `OrderService` / `OrderServiceImpl` |
| `*Repository`, `*DAO`, `*Impl` | Persistence abstraction and implementation | `ProductRepository`, `ProductRepositoryImpl` |
| `v0` / `v1` | API model version packages | `model.order.v1` |
| `*RESTController` / mapping annotations | HTTP API surface | `@RequestMapping`, `@GetMapping` |
| Uppercase schema/table convention | Database schema naming | `SALESMANAGER` |
| `Readable*`, `Persistable*`, `*Entity` | API read/write model and persistence model roles | `ReadableOrder`, `PersistableOrder`, `OrderEntity` |

## Integrations and Storage

Observed integration families include Stripe, PayPal, Braintree, Elastic, Infinispan, S3, SMTP/email, FedEx, USPS, and Google Maps-related configuration. Supported relational database profiles include H2, MySQL/MariaDB, and PostgreSQL; file-backed repositories and search indexes are also present.

## CAST Path Mapping

| CAST Root | Local Root | Status |
|-----------|------------|--------|
| `Shopizer-Backend` | `initial-source/shopizer-3.2.7/` | Verified CAST application; delivery `Onboarding-202511171247` |
| `Shopizer-WebAdmin` | `initial-source/shopizer-admin-main/` | Verified CAST application; Angular/TypeScript; 82,284 CAST LOC |
| `Shopizer-WebFrontEnd` | `initial-source/shopizer-shop-reactjs-main/` | Verified CAST application; React/JavaScript; 29,251 CAST LOC |

## Scope Notes

- The source tree is read-only reference material for later analysis.
- The uploaded documentation is supplemental evidence and is not counted as application production LOC.
- CAST structural queries are available for all three in-scope applications. `Shopizer-WebAdmin` calls `Shopizer-Backend` through 178 analyzed dependencies; `Shopizer-WebFrontEnd` calls it through 199.
- The assumed .NET 10 target is non-binding and must be confirmed or revised during Phase 4b.
