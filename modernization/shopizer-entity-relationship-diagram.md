# Shopizer 3.2.7 Target Entity Relationship Diagrams

Each diagram is scoped to one service-owned PostgreSQL schema. `tenant_id` and `store_id` are
included on tenant-scoped records. Cross-service identifiers such as `customer_id`,
`product_id`, and `order_id` are opaque values without foreign-key constraints.

## MS-01 Customer and Identity

```mermaid
erDiagram
    CUSTOMER ||--o{ CUSTOMER_ADDRESS : has
    CUSTOMER ||--o{ CUSTOMER_ROLE : assigned
    ROLE ||--o{ CUSTOMER_ROLE : grants
    CUSTOMER {
        uuid customer_id PK
        uuid tenant_id
        string email
        string status
        datetime created_at
    }
    CUSTOMER_ADDRESS {
        uuid address_id PK
        uuid customer_id FK
        string type
        string line1
        string city
        string country_code
    }
    ROLE {
        uuid role_id PK
        string name
    }
    CUSTOMER_ROLE {
        uuid customer_id FK
        uuid role_id FK
    }
```

## MS-02 Catalog and Product

```mermaid
erDiagram
    CATEGORY ||--o{ CATEGORY_PRODUCT : contains
    PRODUCT ||--o{ CATEGORY_PRODUCT : classified
    PRODUCT ||--o{ PRODUCT_VARIANT : offers
    PRODUCT ||--o{ PRODUCT_ATTRIBUTE : describes
    ATTRIBUTE ||--o{ PRODUCT_ATTRIBUTE : values
    PRODUCT {
        uuid product_id PK
        uuid tenant_id
        string sku
        string name
        string status
    }
    CATEGORY {
        uuid category_id PK
        uuid tenant_id
        uuid parent_category_id
        string name
    }
    CATEGORY_PRODUCT {
        uuid category_id FK
        uuid product_id FK
    }
    PRODUCT_VARIANT {
        uuid variant_id PK
        uuid product_id FK
        string sku
        json attributes
    }
    ATTRIBUTE {
        uuid attribute_id PK
        string code
        string value_type
    }
    PRODUCT_ATTRIBUTE {
        uuid product_id FK
        uuid attribute_id FK
        string value
    }
```

## MS-03 Search

```mermaid
erDiagram
    SEARCH_INDEX ||--o{ SEARCH_DOCUMENT : contains
    SEARCH_INDEX ||--o{ INDEX_BUILD : records
    SEARCH_INDEX {
        uuid index_id PK
        uuid tenant_id
        string entity_type
        string status
    }
    SEARCH_DOCUMENT {
        uuid document_id PK
        uuid index_id FK
        string source_id
        string version
        json searchable_payload
        datetime indexed_at
    }
    INDEX_BUILD {
        uuid build_id PK
        uuid index_id FK
        string status
        datetime started_at
        datetime completed_at
    }
```

## MS-04 Cart and Checkout

```mermaid
erDiagram
    CART ||--o{ CART_LINE : contains
    CART ||--o| CHECKOUT_SESSION : starts
    CHECKOUT_SESSION ||--o{ CHECKOUT_SNAPSHOT : records
    CART {
        uuid cart_id PK
        uuid tenant_id
        uuid customer_id
        string status
        datetime updated_at
    }
    CART_LINE {
        uuid cart_line_id PK
        uuid cart_id FK
        string product_id
        string sku
        int quantity
        decimal unit_price_snapshot
    }
    CHECKOUT_SESSION {
        uuid checkout_id PK
        uuid cart_id FK
        string status
        string idempotency_key
        datetime expires_at
    }
    CHECKOUT_SNAPSHOT {
        uuid snapshot_id PK
        uuid checkout_id FK
        json customer_snapshot
        json totals_snapshot
        json shipping_snapshot
        datetime created_at
    }
```

## MS-05 Order Management

```mermaid
erDiagram
    ORDER ||--o{ ORDER_LINE : contains
    ORDER ||--o{ ORDER_STATUS_HISTORY : transitions
    ORDER ||--o| INVOICE : produces
    ORDER {
        uuid order_id PK
        uuid tenant_id
        uuid customer_id
        string order_number
        string status
        decimal grand_total
        datetime submitted_at
    }
    ORDER_LINE {
        uuid order_line_id PK
        uuid order_id FK
        string product_id
        string sku
        int quantity
        decimal line_total
    }
    ORDER_STATUS_HISTORY {
        uuid history_id PK
        uuid order_id FK
        string from_status
        string to_status
        string reason
        datetime changed_at
    }
    INVOICE {
        uuid invoice_id PK
        uuid order_id FK
        string invoice_number
        string document_reference
        datetime issued_at
    }
```

## MS-06 Payments

```mermaid
erDiagram
    PAYMENT_INTENT ||--o{ PAYMENT_TRANSACTION : records
    PAYMENT_INTENT ||--o{ PAYMENT_CALLBACK : receives
    PAYMENT_INTENT {
        uuid payment_intent_id PK
        uuid tenant_id
        string order_id
        decimal amount
        string currency
        string status
    }
    PAYMENT_TRANSACTION {
        uuid transaction_id PK
        uuid payment_intent_id FK
        string provider
        string provider_reference
        string type
        decimal amount
        string status
    }
    PAYMENT_CALLBACK {
        uuid callback_id PK
        uuid payment_intent_id FK
        string provider_event_id
        string payload_hash
        string processing_status
    }
```

## MS-07 Pricing and Promotions

```mermaid
erDiagram
    PRICE_LIST ||--o{ PRICE : contains
    PROMOTION ||--o{ PROMOTION_RULE : defines
    COUPON ||--o{ COUPON_REDEMPTION : used
    PRICE_LIST {
        uuid price_list_id PK
        uuid tenant_id
        string name
        string currency
        string status
    }
    PRICE {
        uuid price_id PK
        uuid price_list_id FK
        string product_id
        decimal amount
        datetime valid_from
        datetime valid_to
    }
    PROMOTION {
        uuid promotion_id PK
        uuid tenant_id
        string name
        string status
        datetime valid_from
        datetime valid_to
    }
    PROMOTION_RULE {
        uuid rule_id PK
        uuid promotion_id FK
        string rule_type
        json parameters
    }
    COUPON {
        uuid coupon_id PK
        uuid promotion_id
        string code
        int redemption_limit
    }
    COUPON_REDEMPTION {
        uuid redemption_id PK
        uuid coupon_id FK
        string order_id
        datetime redeemed_at
    }
```

## MS-08 Tax

```mermaid
erDiagram
    TAX_PROFILE ||--o{ TAX_RATE : defines
    TAX_PROFILE ||--o{ TAX_QUOTE : produces
    TAX_PROFILE {
        uuid tax_profile_id PK
        uuid tenant_id
        string name
        string status
    }
    TAX_RATE {
        uuid tax_rate_id PK
        uuid tax_profile_id FK
        string jurisdiction_code
        decimal rate
        datetime valid_from
        datetime valid_to
    }
    TAX_QUOTE {
        uuid tax_quote_id PK
        uuid tax_profile_id FK
        string checkout_id
        decimal taxable_amount
        decimal tax_amount
        datetime calculated_at
    }
```

## MS-09 Shipping

```mermaid
erDiagram
    SHIPPING_ZONE ||--o{ SHIPPING_ZONE_COUNTRY : includes
    SHIPPING_METHOD ||--o{ SHIPPING_RATE : offers
    SHIPPING_METHOD ||--o{ SHIPMENT_QUOTE : quotes
    SHIPPING_ZONE {
        uuid zone_id PK
        uuid tenant_id
        string name
    }
    SHIPPING_ZONE_COUNTRY {
        uuid zone_id FK
        string country_code
    }
    SHIPPING_METHOD {
        uuid method_id PK
        uuid tenant_id
        string name
        string carrier
        string status
    }
    SHIPPING_RATE {
        uuid rate_id PK
        uuid method_id FK
        uuid zone_id
        decimal amount
        int max_days
    }
    SHIPMENT_QUOTE {
        uuid quote_id PK
        uuid method_id FK
        string checkout_id
        decimal amount
        datetime expires_at
    }
```

## MS-10 Merchant and Store Administration

```mermaid
erDiagram
    MERCHANT ||--o{ STORE : owns
    STORE ||--o{ STORE_CHANNEL : exposes
    MERCHANT ||--o{ MERCHANT_USER : authorizes
    MERCHANT {
        uuid merchant_id PK
        string legal_name
        string status
        datetime created_at
    }
    STORE {
        uuid store_id PK
        uuid merchant_id FK
        string code
        string name
        string default_currency
        string status
    }
    STORE_CHANNEL {
        uuid channel_id PK
        uuid store_id FK
        string channel_type
        string base_url
        string status
    }
    MERCHANT_USER {
        uuid merchant_id FK
        string customer_id
        string role
    }
```

## MS-11 Content and Configuration

```mermaid
erDiagram
    CONTENT_ITEM ||--o{ CONTENT_VERSION : versions
    CONTENT_ITEM ||--o{ CONTENT_PUBLICATION : publishes
    STORE ||--o{ CONFIGURATION_ENTRY : configures
    CONTENT_ITEM {
        uuid content_id PK
        uuid tenant_id
        uuid store_id
        string key
        string content_type
        string status
    }
    CONTENT_VERSION {
        uuid version_id PK
        uuid content_id FK
        int version_number
        string locale
        json body
        string status
    }
    CONTENT_PUBLICATION {
        uuid publication_id PK
        uuid content_id FK
        int version_number
        datetime published_at
    }
    STORE {
        uuid store_id PK
        uuid tenant_id
        string external_reference
    }
    CONFIGURATION_ENTRY {
        uuid entry_id PK
        uuid store_id FK
        string key
        string value
        string value_type
    }
```

`STORE.external_reference` in this diagram is an opaque reference to MS-10. It is not a
cross-service foreign key; the simplified local entity is resolved through an API or event.

## MS-12 Platform Integrations

```mermaid
erDiagram
    INTEGRATION_ENDPOINT ||--o{ DELIVERY_ATTEMPT : receives
    EMAIL_MESSAGE ||--o{ DELIVERY_ATTEMPT : delivers
    INTEGRATION_ENDPOINT {
        uuid endpoint_id PK
        uuid tenant_id
        string integration_type
        string provider
        string status
    }
    EMAIL_MESSAGE {
        uuid message_id PK
        uuid tenant_id
        string template_key
        string recipient_reference
        string status
    }
    DELIVERY_ATTEMPT {
        uuid attempt_id PK
        uuid endpoint_id FK
        uuid message_id
        string correlation_id
        int attempt_number
        string status
        datetime attempted_at
    }
```

