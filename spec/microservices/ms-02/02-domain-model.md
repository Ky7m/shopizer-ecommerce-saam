# Catalog and Product — Domain Model

**Service:** MS-02  
**PostgreSQL schema:** `catalog_product`  
**Database ownership:** MS-02 only  
**Store scope:** `tenant_id` and `store_id` are opaque values validated through MS-10; no cross-service foreign keys.

## Core entities

Products, categories, variants, availability, prices, options, variations, attributes, media, relationships, and inventory reservations are owned by MS-02. Manufacturer, product type, tax class, language, tenant, and store references are opaque dependency identifiers.

## PostgreSQL DDL

```sql
CREATE EXTENSION IF NOT EXISTS pgcrypto;

CREATE SCHEMA IF NOT EXISTS catalog_product;

CREATE TABLE catalog_product.product (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(), -- BR-CAT-001; legacy PRODUCT.PRODUCT_ID
    tenant_id text NOT NULL, -- Audit/multi-tenancy standard
    store_id text NOT NULL, -- Maps to PRODUCT.MERCHANT_ID; opaque MS-10 scope
    sku text NOT NULL, -- BR-CAT-001; maps to PRODUCT.SKU
    ref_sku text, -- Maps to PRODUCT.REF_SKU
    status text NOT NULL DEFAULT 'Draft', -- BR-CAT-032; target lifecycle
    visible boolean NOT NULL DEFAULT false, -- BR-UI-004; maps to PRODUCT.AVAILABLE/UI visible
    available boolean NOT NULL DEFAULT false, -- BR-CAT-009; storefront eligibility
    can_be_purchased boolean NOT NULL DEFAULT true, -- BR-UI-004
    date_available timestamptz NOT NULL DEFAULT now(), -- BR-CAT-032; maps to PRODUCT.DATE_AVAILABLE
    manufacturer_code text, -- BR-CAT-005; opaque MS-02 reference
    product_type_code text, -- BR-CAT-005; opaque MS-02 reference
    tax_class_code text, -- Maps to PRODUCT.TAX_CLASS_ID; opaque MS-08 reference
    product_virtual boolean NOT NULL DEFAULT false, -- Maps to PRODUCT.PRODUCT_VIRTUAL
    product_shippable boolean NOT NULL DEFAULT false, -- Maps to PRODUCT.PRODUCT_SHIP
    product_free boolean NOT NULL DEFAULT false, -- Maps to PRODUCT.PRODUCT_FREE
    length numeric(18,6), -- Maps to PRODUCT.PRODUCT_LENGTH
    width numeric(18,6), -- Maps to PRODUCT.PRODUCT_WIDTH
    height numeric(18,6), -- Maps to PRODUCT.PRODUCT_HEIGHT
    weight numeric(18,6), -- Maps to PRODUCT.PRODUCT_WEIGHT
    review_average numeric(18,6), -- Maps to PRODUCT.REVIEW_AVG
    review_count integer NOT NULL DEFAULT 0, -- Maps to PRODUCT.REVIEW_COUNT
    sort_order integer NOT NULL DEFAULT 0, -- Maps to PRODUCT.SORT_ORDER
    version bigint NOT NULL DEFAULT 0, -- Infrastructure optimistic concurrency
    created_at timestamptz NOT NULL DEFAULT now(), -- Audit/multi-tenancy standard
    updated_at timestamptz NOT NULL DEFAULT now(), -- Audit/multi-tenancy standard
    UNIQUE (store_id, sku)
);

CREATE TABLE catalog_product.product_description (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    product_id uuid NOT NULL REFERENCES catalog_product.product(id) ON DELETE CASCADE, -- Maps to PRODUCT_DESCRIPTION.PRODUCT_ID
    language_code text NOT NULL, -- BR-UI-005; maps to language reference
    name text NOT NULL, -- BR-UI-005
    friendly_url text NOT NULL, -- BR-CAT-010; maps to PRODUCT_DESCRIPTION.SE_URL
    description text,
    highlights text,
    title text,
    keywords text,
    meta_description text,
    created_at timestamptz NOT NULL DEFAULT now(), -- Audit/multi-tenancy standard
    updated_at timestamptz NOT NULL DEFAULT now(), -- Audit/multi-tenancy standard
    UNIQUE (product_id, language_code),
    UNIQUE (product_id, language_code, friendly_url)
);

CREATE TABLE catalog_product.category (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id text NOT NULL,
    store_id text NOT NULL, -- Maps to CATEGORY.MERCHANT_ID
    code text NOT NULL, -- BR-CAT-003; maps to CATEGORY.CODE
    parent_id uuid REFERENCES catalog_product.category(id) ON DELETE RESTRICT, -- BR-CAT-006/007
    category_image_uri text, -- Maps to CATEGORY.CATEGORY_IMAGE
    sort_order integer NOT NULL DEFAULT 0, -- Maps to CATEGORY.SORT_ORDER
    status text NOT NULL DEFAULT 'Draft', -- Target lifecycle
    visible boolean NOT NULL DEFAULT false, -- Maps to CATEGORY.VISIBLE
    featured boolean NOT NULL DEFAULT false, -- Maps to CATEGORY.FEATURED
    depth integer NOT NULL DEFAULT 0, -- BR-CAT-006; maps to CATEGORY.DEPTH
    lineage text NOT NULL, -- BR-CAT-006/007; maps to CATEGORY.LINEAGE
    created_at timestamptz NOT NULL DEFAULT now(), -- Audit/multi-tenancy standard
    updated_at timestamptz NOT NULL DEFAULT now(), -- Audit/multi-tenancy standard
    UNIQUE (store_id, code),
    CHECK (depth >= 0)
);

CREATE TABLE catalog_product.category_description (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    category_id uuid NOT NULL REFERENCES catalog_product.category(id) ON DELETE CASCADE,
    language_code text NOT NULL,
    name text NOT NULL, -- BR-UI-006
    friendly_url text NOT NULL, -- BR-UI-006
    description text,
    title text,
    meta_description text,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    UNIQUE (category_id, language_code)
);

CREATE TABLE catalog_product.product_category (
    product_id uuid NOT NULL REFERENCES catalog_product.product(id) ON DELETE CASCADE,
    category_id uuid NOT NULL REFERENCES catalog_product.category(id) ON DELETE CASCADE,
    created_at timestamptz NOT NULL DEFAULT now(), -- BR-CAT-008/019
    PRIMARY KEY (product_id, category_id)
);

CREATE TABLE catalog_product.product_variant (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    product_id uuid NOT NULL REFERENCES catalog_product.product(id) ON DELETE CASCADE,
    store_id text NOT NULL,
    sku text NOT NULL, -- BR-CAT-002; maps to PRODUCT_VARIANT.SKU
    code text, -- Maps to PRODUCT_VARIANT.CODE
    status text NOT NULL DEFAULT 'Draft',
    available boolean NOT NULL DEFAULT false,
    default_selection boolean NOT NULL DEFAULT false, -- BR-CAT-012
    date_available timestamptz NOT NULL DEFAULT now(),
    sort_order integer NOT NULL DEFAULT 0,
    variation_id uuid, -- BR-CAT-029; opaque variation reference
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    UNIQUE (product_id, sku)
);

CREATE TABLE catalog_product.product_availability (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    product_id uuid REFERENCES catalog_product.product(id) ON DELETE CASCADE,
    variant_id uuid REFERENCES catalog_product.product_variant(id) ON DELETE CASCADE,
    store_id text NOT NULL,
    region_code text NOT NULL, -- BR-CAT-009/011; '*' is wildcard
    quantity integer NOT NULL DEFAULT 0, -- BR-ORD-012
    reserved_quantity integer NOT NULL DEFAULT 0, -- BR-ORD-012
    active boolean NOT NULL DEFAULT true,
    version bigint NOT NULL DEFAULT 0,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    CHECK ((product_id IS NOT NULL) <> (variant_id IS NOT NULL)),
    CHECK (quantity >= 0),
    CHECK (reserved_quantity >= 0 AND reserved_quantity <= quantity)
);

CREATE TABLE catalog_product.product_price (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    availability_id uuid NOT NULL REFERENCES catalog_product.product_availability(id) ON DELETE CASCADE,
    store_id text NOT NULL,
    currency_code text NOT NULL, -- Maps to merchant currency reference
    amount numeric(19,4) NOT NULL, -- Maps to PRODUCT_PRICE.PRODUCT_PRICE_AMOUNT
    price_type text NOT NULL DEFAULT 'OneTime',
    default_price boolean NOT NULL DEFAULT false, -- BR-CAT-013
    special_amount numeric(19,4), -- BR-CAT-014
    special_start_at timestamptz,
    special_end_at timestamptz,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    CHECK (amount >= 0),
    CHECK (special_amount IS NULL OR special_amount >= 0),
    CHECK (special_end_at IS NULL OR special_start_at IS NULL OR special_end_at > special_start_at)
);

CREATE TABLE catalog_product.product_option (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    store_id text NOT NULL,
    code text NOT NULL, -- BR-CAT-003; maps to PRODUCT_OPTION.PRODUCT_OPTION_CODE
    option_type text NOT NULL,
    display_only boolean NOT NULL DEFAULT false,
    sort_order integer NOT NULL DEFAULT 0,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    UNIQUE (store_id, code)
);

CREATE TABLE catalog_product.product_option_value (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    option_id uuid NOT NULL REFERENCES catalog_product.product_option(id) ON DELETE CASCADE,
    store_id text NOT NULL,
    code text NOT NULL, -- BR-CAT-003
    display_only boolean NOT NULL DEFAULT false,
    image_uri text,
    sort_order integer NOT NULL DEFAULT 0,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    UNIQUE (option_id, code)
);

CREATE TABLE catalog_product.product_variation (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    store_id text NOT NULL,
    option_id uuid NOT NULL REFERENCES catalog_product.product_option(id) ON DELETE RESTRICT,
    option_value_id uuid NOT NULL REFERENCES catalog_product.product_option_value(id) ON DELETE RESTRICT,
    code text NOT NULL, -- BR-CAT-003
    default_variation boolean NOT NULL DEFAULT false,
    sort_order integer NOT NULL DEFAULT 0,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    UNIQUE (store_id, code)
);

CREATE TABLE catalog_product.product_attribute (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    product_id uuid NOT NULL REFERENCES catalog_product.product(id) ON DELETE CASCADE,
    option_id uuid NOT NULL REFERENCES catalog_product.product_option(id) ON DELETE RESTRICT,
    option_value_id uuid NOT NULL REFERENCES catalog_product.product_option_value(id) ON DELETE RESTRICT,
    display_only boolean NOT NULL DEFAULT false, -- BR-CAT-031
    price_adjustment numeric(19,4) NOT NULL DEFAULT 0, -- BR-CAT-015/029
    default_selection boolean NOT NULL DEFAULT false,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE catalog_product.product_image (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    product_id uuid NOT NULL REFERENCES catalog_product.product(id) ON DELETE CASCADE,
    variant_id uuid REFERENCES catalog_product.product_variant(id) ON DELETE CASCADE,
    image_type text NOT NULL, -- BR-EXT-019
    file_name text NOT NULL,
    original_uri text,
    transformed_uri text,
    provider_key text,
    external_url text,
    default_image boolean NOT NULL DEFAULT false,
    media_status text NOT NULL DEFAULT 'Pending',
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE catalog_product.product_relationship (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    product_id uuid NOT NULL REFERENCES catalog_product.product(id) ON DELETE CASCADE,
    related_product_id uuid NOT NULL REFERENCES catalog_product.product(id) ON DELETE RESTRICT,
    relationship_type text NOT NULL,
    sort_order integer NOT NULL DEFAULT 0,
    created_at timestamptz NOT NULL DEFAULT now(),
    UNIQUE (product_id, related_product_id, relationship_type)
);

CREATE TABLE catalog_product.inventory_reservation (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id text NOT NULL,
    store_id text NOT NULL,
    product_id uuid REFERENCES catalog_product.product(id) ON DELETE RESTRICT,
    variant_id uuid REFERENCES catalog_product.product_variant(id) ON DELETE RESTRICT,
    availability_id uuid NOT NULL REFERENCES catalog_product.product_availability(id) ON DELETE RESTRICT,
    reservation_key text NOT NULL, -- BR-CAT-037
    request_hash text NOT NULL,
    quantity integer NOT NULL, -- BR-ORD-012
    state text NOT NULL DEFAULT 'Held', -- BR-CAT-039
    expires_at timestamptz NOT NULL,
    committed_at timestamptz,
    released_at timestamptz,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    CHECK (quantity > 0),
    CHECK (state IN ('Held','Committed','Released','Expired')),
    UNIQUE (store_id, reservation_key)
);

CREATE INDEX ix_product_store_visibility
    ON catalog_product.product (store_id, visible, available, date_available);

CREATE INDEX ix_product_description_slug
    ON catalog_product.product_description (friendly_url, language_code);

CREATE INDEX ix_category_store_lineage
    ON catalog_product.category (store_id, lineage);

CREATE INDEX ix_availability_product_region
    ON catalog_product.product_availability (product_id, region_code, active);

CREATE INDEX ix_availability_variant_region
    ON catalog_product.product_availability (variant_id, region_code, active);

CREATE INDEX ix_reservation_expiry
    ON catalog_product.inventory_reservation (state, expires_at);

CREATE UNIQUE INDEX ux_default_variant_per_product
    ON catalog_product.product_variant (product_id)
    WHERE default_selection = true;
```

## Entity state models

### Product lifecycle

- **States:** Draft (initial), Active, Unavailable, Deleted (terminal)
- **Transitions:**

| From | To | Trigger | Guard |
|---|---|---|---|
| Draft | Active | BR-CAT-004 | Product has valid description, store scope, and availability |
| Active | Unavailable | BR-UI-004 | Product is hidden, unavailable, not purchasable, or has no sellable quantity |
| Unavailable | Active | BR-UI-004 | Product is visible, purchasable, date-eligible, and has sellable quantity |
| Draft | Deleted | BR-CAT-019 | Aggregate cleanup succeeds |
| Active | Deleted | BR-CAT-019 | Aggregate cleanup succeeds |
| Unavailable | Deleted | BR-CAT-019 | Aggregate cleanup succeeds |

`Deleted` is terminal.

### Category lifecycle

- **States:** Draft (initial), Active, Hidden, Deleted (terminal)
- **Transitions:**

| From | To | Trigger | Guard |
|---|---|---|---|
| Draft | Active | BR-CAT-006 | Valid store, code, description, parent, lineage, and depth |
| Active | Hidden | BR-UI-006 | Authorized administrator sets visibility false |
| Hidden | Active | BR-UI-006 | Authorized administrator sets visibility true |
| Draft | Deleted | BR-CAT-008 | Subtree deletion policy succeeds |
| Active | Deleted | BR-CAT-008 | Subtree deletion policy succeeds |
| Hidden | Deleted | BR-CAT-008 | Subtree deletion policy succeeds |

`Deleted` is terminal.

### Variant lifecycle

- **States:** Draft (initial), Active, Unavailable, Deleted (terminal)
- **Transitions:**

| From | To | Trigger | Guard |
|---|---|---|---|
| Draft | Active | BR-CAT-002 | Unique SKU and valid parent product |
| Active | Unavailable | BR-UI-004 | Variant is unavailable or has no sellable quantity |
| Unavailable | Active | BR-UI-004 | Variant has valid sellable availability |
| Draft | Deleted | BR-CAT-019 | Parent aggregate cleanup succeeds |
| Active | Deleted | BR-CAT-019 | Parent aggregate cleanup succeeds |
| Unavailable | Deleted | BR-CAT-019 | Parent aggregate cleanup succeeds |

`Deleted` is terminal.

### Inventory reservation lifecycle

- **States:** Held (initial), Committed (terminal), Released (terminal), Expired (terminal)
- **Transitions:**

| From | To | Trigger | Guard |
|---|---|---|---|
| Held | Committed | BR-CAT-039 | Reservation exists, is not expired, and caller is authorized |
| Held | Released | BR-CAT-039 | Reservation exists and caller is authorized |
| Held | Expired | BR-CAT-039 | Expiry time has passed and reservation is not committed |
| Committed | Committed | BR-CAT-037 | Idempotent retry with same request hash |
| Released | Released | BR-CAT-037 | Idempotent retry with same request hash |

Terminal states have no state-changing outgoing transitions.

## Data invariants

| Invariant ID | Statement | Entity | Kind | Tier |
|---|---|---|---|---|
| INV-CAT-001 | Product SKU is unique within a store | product | referential | both |
| INV-CAT-002 | Variant SKU is unique within a parent product | product_variant | referential | both |
| INV-CAT-003 | Catalog identity codes are unique within store scope | category/option/value/variation | referential | both |
| INV-CAT-004 | A product must have at least one availability before Active state | product | cross-entity | both |
| INV-CAT-005 | Available quantity cannot be negative and reserved quantity cannot exceed quantity | product_availability | constraint | both |
| INV-CAT-006 | Category lineage and depth match the parent hierarchy | category | computed: `lineage = parent.lineage || id`; `depth = parent.depth + 1` | both |
| INV-CAT-007 | Product-category links cannot cross store scope | product_category | cross-entity | both |
| INV-CAT-008 | Reservation quantity is positive and idempotency key is unique per store | inventory_reservation | referential | both |
| INV-CAT-009 | Reservation terminal state cannot transition to another terminal outcome | inventory_reservation | monotonic-status | both |
| INV-CAT-010 | Product final price equals selected base price plus positive selected adjustments | product price response | computed: `finalPrice = basePrice + SUM(positive selected adjustments)` | app |
| INV-CAT-011 | Product media metadata cannot reference a deleted product | product_image | referential | db |
| INV-CAT-012 | Product listing count and fetch predicates use the same eligibility predicate | listing query | cross-field | app |
