# Shopizer 3.2.7 Target Process Flows

The flows use REST for immediate queries/commands and RabbitMQ events for durable cross-service
coordination. Event handlers are idempotent and carry tenant and correlation context.

## 1. Browse catalog

```mermaid
sequenceDiagram
    participant Client
    participant Gateway
    participant Catalog as MS-02 Catalog
    participant Cache as Redis
    Client->>Gateway: GET products
    Gateway->>Catalog: GET /products?storeId
    Catalog->>Cache: Read product projection
    alt cache hit
        Cache-->>Catalog: Product page
    else cache miss
        Catalog->>Catalog: Query catalog_product
        Catalog->>Cache: Store short-lived page
    end
    Catalog-->>Gateway: Product page
    Gateway-->>Client: 200 Product page
```

## 2. Search products

```mermaid
sequenceDiagram
    participant Client
    participant Gateway
    participant Search as MS-03 Search
    Client->>Gateway: GET /search
    Gateway->>Search: Query index
    Search->>Search: Apply tenant/store filter
    Search-->>Gateway: Ranked documents and freshness
    Gateway-->>Client: 200 Search results
```

## 3. Customer registration and login

```mermaid
sequenceDiagram
    participant Client
    participant Gateway
    participant Identity as MS-01 Customer and Identity
    participant OIDC as OIDC Provider
    Client->>Gateway: POST registration
    Gateway->>Identity: Create customer
    Identity->>Identity: Validate email and tenant
    Identity-->>Gateway: CustomerRegistered
    Gateway-->>Client: 201 Customer
    Client->>OIDC: Authenticate credentials
    OIDC-->>Client: Access token with tenant claims
    Client->>Gateway: Authenticated request
    Gateway->>Identity: Validate customer context
    Identity-->>Gateway: Customer context
```

## 4. Update cart

```mermaid
sequenceDiagram
    participant Client
    participant Gateway
    participant Cart as MS-04 Cart and Checkout
    participant Catalog as MS-02 Catalog
    Client->>Gateway: PUT cart line
    Gateway->>Cart: Add or change line
    Cart->>Catalog: Validate SKU and availability
    Catalog-->>Cart: Product snapshot
    Cart->>Cart: Apply quantity and line invariants
    Cart-->>Gateway: Updated cart
    Gateway-->>Client: 200 Cart
```

## 5. Checkout quote

```mermaid
sequenceDiagram
    participant Client
    participant Cart as MS-04 Cart and Checkout
    participant Pricing as MS-07 Pricing
    participant Tax as MS-08 Tax
    participant Shipping as MS-09 Shipping
    Client->>Cart: POST /checkout/quote
    Cart->>Pricing: Calculate prices and promotions
    Pricing-->>Cart: Price allocation
    Cart->>Tax: Calculate tax
    Tax-->>Cart: Tax quote
    Cart->>Shipping: Calculate shipping options
    Shipping-->>Cart: Shipping quotes
    Cart->>Cart: Store expiring totals snapshot
    Cart-->>Client: Checkout quote
```

## 6. Submit order

```mermaid
sequenceDiagram
    participant Client
    participant Cart as MS-04 Cart and Checkout
    participant Orders as MS-05 Order Management
    participant Payments as MS-06 Payments
    participant MQ as RabbitMQ
    Client->>Cart: POST /checkout/submit with idempotency key
    Cart->>Cart: Validate snapshot and freeze checkout
    Cart-->>MQ: OrderSubmitted.v1
    MQ-->>Orders: OrderSubmitted.v1
    Orders->>Orders: Create order aggregate and status PendingPayment
    Orders-->>MQ: PaymentRequested
    MQ-->>Payments: PaymentRequested
    Payments-->>MQ: PaymentAuthorizationStarted
    Orders-->>Client: 202 Order accepted
```

## 7. Payment authorization and callback

```mermaid
sequenceDiagram
    participant Payments as MS-06 Payments
    participant Provider as Payment Provider
    participant MQ as RabbitMQ
    participant Orders as MS-05 Order Management
    Payments->>Provider: Authorize payment
    Provider-->>Payments: Redirect or provider reference
    Provider-->>Payments: Webhook callback
    Payments->>Payments: Verify signature and deduplicate event
    alt authorized
        Payments-->>MQ: PaymentAuthorized
        MQ-->>Orders: PaymentAuthorized
        Orders->>Orders: Transition PendingPayment to Confirmed
    else rejected
        Payments-->>MQ: PaymentFailed
        MQ-->>Orders: PaymentFailed
        Orders->>Orders: Transition to PaymentFailed
    end
```

## 8. Apply promotion

```mermaid
sequenceDiagram
    participant Cart as MS-04 Cart and Checkout
    participant Pricing as MS-07 Pricing and Promotions
    Cart->>Pricing: Evaluate coupon and cart
    Pricing->>Pricing: Check dates, usage, conditions, and limits
    Pricing-->>Cart: Discount allocation or rejection reason
    Cart->>Cart: Store allocation in totals snapshot
```

## 9. Tax and shipping quote

```mermaid
sequenceDiagram
    participant Cart as MS-04 Cart and Checkout
    participant Tax as MS-08 Tax
    participant Shipping as MS-09 Shipping
    participant Integration as MS-12 Platform Integrations
    Cart->>Tax: Quote taxable lines and destination
    Tax->>Tax: Resolve jurisdiction and rate
    Tax-->>Cart: Tax quote with expiry
    Cart->>Shipping: Quote package and destination
    Shipping->>Integration: Request carrier rate if required
    Integration-->>Shipping: Carrier response
    Shipping-->>Cart: Shipping options with expiry
```

## 10. Merchant/store setup

```mermaid
sequenceDiagram
    participant Admin
    participant Merchant as MS-10 Merchant and Store
    participant Content as MS-11 Content and Configuration
    participant Catalog as MS-02 Catalog
    participant MQ as RabbitMQ
    Admin->>Merchant: Create merchant and store
    Merchant->>Merchant: Enforce unique store code
    Merchant-->>MQ: StoreConfigured
    MQ-->>Content: Initialize default configuration
    MQ-->>Catalog: Initialize store catalog projection
    Merchant-->>Admin: 201 Store
```

## 11. Publish content

```mermaid
sequenceDiagram
    participant Admin
    participant Content as MS-11 Content and Configuration
    participant MQ as RabbitMQ
    participant Search as MS-03 Search
    Admin->>Content: Publish content version
    Content->>Content: Validate locale and publication rules
    Content-->>MQ: ContentPublished
    MQ-->>Search: ContentPublished
    Search->>Search: Upsert search document
    Content-->>Admin: 200 Published
```

## 12. Catalog indexing

```mermaid
sequenceDiagram
    participant Catalog as MS-02 Catalog
    participant MQ as RabbitMQ
    participant Search as MS-03 Search
    Catalog-->>MQ: ProductChanged
    MQ-->>Search: ProductChanged
    Search->>Search: Validate event version and tenant
    Search->>Search: Upsert document idempotently
    Search-->>Search: Record index freshness
```
