# Shopizer 3.2.7 Service Composition

## Service catalog

| Service ID | Name | Port | PostgreSQL schema | Priority | Phase |
|---|---|---:|---|---:|---|
| MS-01 | Customer and Identity | 8101 | customer_identity | 1 | 1 |
| MS-02 | Catalog and Product | 8102 | catalog_product | 1 | 1 |
| MS-03 | Search | 8103 | search | 3 | 3 |
| MS-04 | Cart and Checkout | 8104 | cart_checkout | 1 | 2 |
| MS-05 | Order Management | 8105 | order_management | 1 | 2 |
| MS-06 | Payments | 8106 | payments | 1 | 2 |
| MS-07 | Pricing and Promotions | 8107 | pricing_promotions | 2 | 2 |
| MS-08 | Tax | 8108 | tax | 2 | 2 |
| MS-09 | Shipping | 8109 | shipping | 2 | 2 |
| MS-10 | Merchant and Store Administration | 8110 | merchant_store | 1 | 1 |
| MS-11 | Content and Configuration | 8111 | content_config | 2 | 3 |
| MS-12 | Platform Integrations | 8112 | platform_integrations | 3 | 3 |

## Responsibilities and ownership

| Service | Aggregate roots | Owned data | Key dependencies |
|---|---|---|---|
| MS-01 | Customer, User, Address, PermissionGroup | Accounts, credentials metadata, addresses, roles, sessions, consent | OIDC provider; publishes `CustomerRegistered` |
| MS-02 | Product, Category, Manufacturer, AttributeSet | Product facts, category tree, variants, catalog status | MS-10 for store scope; publishes `ProductChanged` |
| MS-03 | SearchIndex, SearchDocument | Search documents, index metadata, rebuild jobs | Consumes MS-02 and MS-11 events |
| MS-04 | Cart, CheckoutSession | Cart lines, checkout state, submitted snapshots, idempotency keys | MS-01, MS-02, MS-07, MS-08, MS-09; publishes `OrderSubmitted` |
| MS-05 | Order, FulfillmentOrder, Invoice | Order snapshots, lifecycle state, totals, invoice references | Consumes MS-04 and MS-06 events; publishes order lifecycle events |
| MS-06 | PaymentIntent, PaymentTransaction, Refund | Provider references, authorization/capture/refund states, callback records | External payment providers; publishes payment events |
| MS-07 | PriceList, Promotion, Coupon | Prices, promotion definitions, coupon redemption reservations | MS-10 store scope; publishes promotion changes |
| MS-08 | TaxProfile, TaxRate, TaxQuote | Jurisdictions, rates, tax rules, quote audit records | External tax provider optional; publishes rate changes |
| MS-09 | ShippingMethod, ShippingZone, ShipmentQuote | Origins, zones, methods, rates, delivery estimates | Carrier adapters through MS-12 |
| MS-10 | Merchant, Store, StoreChannel | Tenant/store identity, store settings, merchant lifecycle | MS-01 for operator identity; publishes `StoreConfigured` |
| MS-11 | ContentItem, ContentPublication, ConfigurationEntry | Versioned content, publication state, storefront configuration | MS-10 store scope; publishes `ContentPublished` |
| MS-12 | IntegrationEndpoint, DeliveryAttempt, EmailMessage | Adapter configuration references, delivery state, retries, templates metadata | Consumes business events; external email/files/maps/carrier adapters |

## Dependency rules

| Caller | Provider or consumer | Protocol | Purpose |
|---|---|---|---|
| MS-02 | MS-10 | REST | Validate store/tenant ownership on catalog administration |
| MS-03 | MS-02 | Event | Build search projections from product changes |
| MS-04 | MS-01 | REST | Resolve authenticated customer and address snapshot |
| MS-04 | MS-02 | REST | Validate product and availability snapshot |
| MS-04 | MS-07 | REST | Calculate prices and promotions |
| MS-04 | MS-08 | REST | Calculate taxes |
| MS-04 | MS-09 | REST | Calculate shipping options |
| MS-04 | MS-05 | Event | Submit an immutable order snapshot |
| MS-05 | MS-06 | Event | Request and receive payment state |
| MS-06 | External providers | REST/webhook | Authorize, capture, refund, and receive callbacks |
| MS-05 | MS-12 | Event | Request invoice, email, and fulfillment integration delivery |
| MS-11 | MS-03 | Event | Reindex published content |

Events are asynchronous dependencies and are represented as `PUBLISHES`/`CONSUMES` relationships
in the knowledge graph. REST dependencies are limited to short-lived queries and validation.

## Boundary invariants

- MS-01 is the only service that changes customer identity and permission data.
- MS-02 is the only source of truth for product and category facts.
- MS-04 owns cart mutation and checkout submission; it cannot transition an order.
- MS-05 owns order lifecycle transitions and immutable order snapshots.
- MS-06 owns payment provider state; it never changes order state directly.
- MS-07, MS-08, and MS-09 return calculation results but do not write order totals.
- MS-10 owns tenant and store lifecycle; all other services reference tenant/store IDs.
- MS-03 and MS-12 are derived/supporting contexts and must tolerate replayed events.
- No service uses a cross-service foreign key or writes another service's schema.

