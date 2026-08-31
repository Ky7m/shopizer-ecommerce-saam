---
title: Application Context
inclusion: fileMatch
fileMatchPattern: 'initial-source/**'
authors: SAAM Phase 0 (auto-generated)
---

# Application Context

Shopizer 3.2.7 is a three-application e-commerce engagement. The backend source is under
`initial-source/shopizer-3.2.7/` and is organized into six Maven modules: `sm-core-model`,
`sm-core-modules`, `sm-core`, `sm-shop-model`, and `sm-shop`, plus the root build. The in-scope
frontend applications are `initial-source/shopizer-admin-main/` (Angular administration) and
`initial-source/shopizer-shop-reactjs-main/` (React storefront).

The observed stack is Java 11, Spring Boot 2.5.12, Spring Security, Hibernate/JPA, Drools, and
Swagger 2. The application exposes REST APIs for catalog, cart, checkout, orders, customers,
merchants, users, content, shipping, tax, and payment concerns. Integrations include payment and
shipping providers, search, email, maps, object/file storage, and extension modules.

Source naming conventions include `com.salesmanager...` packages, `*Service`/`*ServiceImpl`
service pairs, `*Repository`/`*Impl` persistence classes, versioned API models (`v0`/`v1`), and
`Readable*`/`Persistable*`/`*Entity` model roles. The database schema convention is `SALESMANAGER`.

Phase 0 proposes eight analysis segments: six backend domain segments — catalog/product;
customer/user/security; cart/checkout/orders/payments; merchant/store/content/configuration;
pricing/promotions/tax/shipping; and external integrations/platform extensions — plus the web
administration frontend and storefront frontend.

The assumed modernization target is .NET 10 and remains subject to evidence-based confirmation in
Phase 4b. CAST Imaging has been verified for `Shopizer-Backend`, `Shopizer-WebAdmin`, and
`Shopizer-WebFrontEnd`; direct source reading remains required for detailed business-rule semantics.
