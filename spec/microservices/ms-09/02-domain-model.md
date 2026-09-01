# Shipping Specification — Domain Model

**Version**: 1.0  
**Date**: 2026-09-01  
**Service ID**: MS-09  
**PostgreSQL schema**: `shipping`

## Domain Boundary

MS-09 owns shipping origins and persisted quote snapshots. Shipping methods, provider
configuration, carrier credentials, package configuration, and merchant configuration are
configuration or adapter concerns owned by MS-11/MS-12. Product facts are consumed from MS-02.

No cross-service foreign keys are used. `tenant_id`, `store_id`, `cart_id`, `customer_id`, and
`order_id` are opaque identifiers resolved by the owning service.

## Legacy-to-Target Mapping

| Target table | Legacy evidence | Ownership |
|---|---|---|
| `shipping.shipping_origin` | `salesmanager.shiping_origin` / `SHIPING_ORIGIN` | MS-09 |
| `shipping.shipping_quote` | `salesmanager.shipping_quote` / `SHIPPING_QUOTE` | MS-09 |
| Package definitions | Embedded JSON in merchant shipping configuration | MS-11 configuration projection |
| Provider/module configuration | `MODULE_CONFIGURATION` and `MERCHANT_CONFIGURATION` | MS-11 |
| Product/package source facts | Product and order/cart models | MS-02 and MS-04 |

## Core Entities

The following is executable PostgreSQL DDL. Every non-infrastructure column has either a
legacy mapping or a BR-ID justification in the comments.

```sql
CREATE SCHEMA IF NOT EXISTS shipping;

CREATE TABLE IF NOT EXISTS shipping.shipping_origin (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID NOT NULL,
    store_id UUID NOT NULL,
    address VARCHAR(256) NOT NULL,
    city VARCHAR(100) NOT NULL,
    postal_code VARCHAR(20) NOT NULL,
    state VARCHAR(100),
    country_code CHAR(2),
    zone_code VARCHAR(32),
    active BOOLEAN NOT NULL DEFAULT FALSE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT shipping_origin_country_code_ck
        CHECK (country_code IS NULL OR country_code ~ '^[A-Z]{2}$')
);

COMMENT ON COLUMN shipping.shipping_origin.id IS
    'Target identifier; replaces SHIP_ORIGIN_ID.';
COMMENT ON COLUMN shipping.shipping_origin.tenant_id IS
    'Audit/multi-tenancy standard.';
COMMENT ON COLUMN shipping.shipping_origin.store_id IS
    'Maps to SHIPING_ORIGIN.MERCHANT_ID through the store identity boundary.';
COMMENT ON COLUMN shipping.shipping_origin.address IS
    'Maps to SHIPING_ORIGIN.STREET_ADDRESS; required by BR-PRC-022.';
COMMENT ON COLUMN shipping.shipping_origin.city IS
    'Maps to SHIPING_ORIGIN.CITY; required by BR-PRC-022.';
COMMENT ON COLUMN shipping.shipping_origin.postal_code IS
    'Maps to SHIPING_ORIGIN.POSTCODE; required by BR-PRC-022 and BR-PRC-034.';
COMMENT ON COLUMN shipping.shipping_origin.state IS
    'Maps to SHIPING_ORIGIN.STATE; required by BR-PRC-022.';
COMMENT ON COLUMN shipping.shipping_origin.country_code IS
    'Maps to SHIPING_ORIGIN.COUNTRY_ID as an opaque country-code projection; required by BR-PRC-022.';
COMMENT ON COLUMN shipping.shipping_origin.zone_code IS
    'Maps to SHIPING_ORIGIN.ZONE_ID as an opaque zone-code projection; required by BR-PRC-022.';
COMMENT ON COLUMN shipping.shipping_origin.active IS
    'Maps to SHIPING_ORIGIN.ACTIVE; drives BR-PRC-022.';
COMMENT ON COLUMN shipping.shipping_origin.created_at IS
    'Audit/multi-tenancy standard.';
COMMENT ON COLUMN shipping.shipping_origin.updated_at IS
    'Audit/multi-tenancy standard.';

CREATE UNIQUE INDEX IF NOT EXISTS shipping_origin_one_active_per_store_uq
    ON shipping.shipping_origin (tenant_id, store_id)
    WHERE active = TRUE;

CREATE INDEX IF NOT EXISTS shipping_origin_store_ix
    ON shipping.shipping_origin (tenant_id, store_id);


CREATE TABLE IF NOT EXISTS shipping.shipping_quote (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID NOT NULL,
    store_id UUID NOT NULL,
    cart_id UUID,
    customer_id UUID,
    order_id UUID,
    provider_code VARCHAR(100) NOT NULL,
    option_code VARCHAR(100),
    option_name VARCHAR(255),
    option_delivery_at TIMESTAMPTZ,
    option_shipping_at TIMESTAMPTZ,
    quoted_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    estimated_number_of_days INTEGER,
    price NUMERIC(19,4) NOT NULL DEFAULT 0,
    handling NUMERIC(19,4) NOT NULL DEFAULT 0,
    free_shipping BOOLEAN NOT NULL DEFAULT FALSE,
    ip_address INET,
    delivery_first_name VARCHAR(64),
    delivery_last_name VARCHAR(64),
    delivery_company VARCHAR(100),
    delivery_address VARCHAR(256),
    delivery_city VARCHAR(100),
    delivery_postal_code VARCHAR(20),
    delivery_state VARCHAR(100),
    delivery_telephone VARCHAR(32),
    delivery_country_code CHAR(2),
    delivery_zone_code VARCHAR(32),
    delivery_latitude NUMERIC(12,8),
    delivery_longitude NUMERIC(12,8),
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT shipping_quote_price_ck CHECK (price >= 0),
    CONSTRAINT shipping_quote_handling_ck CHECK (handling >= 0),
    CONSTRAINT shipping_quote_days_ck
        CHECK (estimated_number_of_days IS NULL OR estimated_number_of_days >= 0),
    CONSTRAINT shipping_quote_country_code_ck
        CHECK (delivery_country_code IS NULL OR delivery_country_code ~ '^[A-Z]{2}$')
);

COMMENT ON COLUMN shipping.shipping_quote.id IS
    'Target identifier; replaces SHIPPING_QUOTE_ID.';
COMMENT ON COLUMN shipping.shipping_quote.tenant_id IS
    'Audit/multi-tenancy standard.';
COMMENT ON COLUMN shipping.shipping_quote.store_id IS
    'BR-PRC-028 quote isolation by store.';
COMMENT ON COLUMN shipping.shipping_quote.cart_id IS
    'Maps to SHIPPING_QUOTE.CART_ID; required by BR-PRC-028.';
COMMENT ON COLUMN shipping.shipping_quote.customer_id IS
    'Maps to SHIPPING_QUOTE.CUSTOMER_ID; optional customer association.';
COMMENT ON COLUMN shipping.shipping_quote.order_id IS
    'Maps to SHIPPING_QUOTE.ORDER_ID; used by quote readback.';
COMMENT ON COLUMN shipping.shipping_quote.provider_code IS
    'Maps to SHIPPING_QUOTE.MODULE; required by BR-PRC-024 and BR-EXT-012.';
COMMENT ON COLUMN shipping.shipping_quote.option_code IS
    'Maps to SHIPPING_QUOTE.OPTION_CODE; required by BR-PRC-028.';
COMMENT ON COLUMN shipping.shipping_quote.option_name IS
    'Maps to SHIPPING_QUOTE.OPTION_NAME; required by BR-PRC-028.';
COMMENT ON COLUMN shipping.shipping_quote.option_delivery_at IS
    'Maps to SHIPPING_QUOTE.OPTION_DELIVERY_DATE.';
COMMENT ON COLUMN shipping.shipping_quote.option_shipping_at IS
    'Maps to SHIPPING_QUOTE.OPTION_SHIPPING_DATE.';
COMMENT ON COLUMN shipping.shipping_quote.quoted_at IS
    'Maps to SHIPPING_QUOTE.QUOTE_DATE; required by BR-PRC-028.';
COMMENT ON COLUMN shipping.shipping_quote.estimated_number_of_days IS
    'Maps to SHIPPING_QUOTE.SHIPPING_NUMBER_DAYS; required by BR-PRC-028.';
COMMENT ON COLUMN shipping.shipping_quote.price IS
    'Maps to SHIPPING_QUOTE.QUOTE_PRICE; required by BR-PRC-027 and BR-PRC-028.';
COMMENT ON COLUMN shipping.shipping_quote.handling IS
    'Maps to SHIPPING_QUOTE.QUOTE_HANDLING; required by BR-PRC-028.';
COMMENT ON COLUMN shipping.shipping_quote.free_shipping IS
    'Maps to SHIPPING_QUOTE.FREE_SHIPPING; required by BR-PRC-026.';
COMMENT ON COLUMN shipping.shipping_quote.ip_address IS
    'Maps to SHIPPING_QUOTE.IP_ADDRESS.';
COMMENT ON COLUMN shipping.shipping_quote.delivery_first_name IS
    'Maps to embedded DELIVERY_FIRST_NAME.';
COMMENT ON COLUMN shipping.shipping_quote.delivery_last_name IS
    'Maps to embedded DELIVERY_LAST_NAME.';
COMMENT ON COLUMN shipping.shipping_quote.delivery_company IS
    'Maps to embedded DELIVERY_COMPANY.';
COMMENT ON COLUMN shipping.shipping_quote.delivery_address IS
    'Maps to embedded DELIVERY_STREET_ADDRESS; required by BR-PRC-028.';
COMMENT ON COLUMN shipping.shipping_quote.delivery_city IS
    'Maps to embedded DELIVERY_CITY.';
COMMENT ON COLUMN shipping.shipping_quote.delivery_postal_code IS
    'Maps to embedded DELIVERY_POSTCODE; required by BR-PRC-023.';
COMMENT ON COLUMN shipping.shipping_quote.delivery_state IS
    'Maps to embedded DELIVERY_STATE.';
COMMENT ON COLUMN shipping.shipping_quote.delivery_telephone IS
    'Maps to embedded DELIVERY_TELEPHONE.';
COMMENT ON COLUMN shipping.shipping_quote.delivery_country_code IS
    'Maps to embedded DELIVERY_COUNTRY_ID; required by BR-PRC-023.';
COMMENT ON COLUMN shipping.shipping_quote.delivery_zone_code IS
    'Maps to embedded DELIVERY_ZONE_ID; required by BR-PRC-034.';
COMMENT ON COLUMN shipping.shipping_quote.delivery_latitude IS
    'BR-PRC-034 destination geocode snapshot.';
COMMENT ON COLUMN shipping.shipping_quote.delivery_longitude IS
    'BR-PRC-034 destination geocode snapshot.';
COMMENT ON COLUMN shipping.shipping_quote.created_at IS
    'Audit/multi-tenancy standard.';
COMMENT ON COLUMN shipping.shipping_quote.updated_at IS
    'Audit/multi-tenancy standard.';

CREATE INDEX IF NOT EXISTS shipping_quote_cart_ix
    ON shipping.shipping_quote (tenant_id, store_id, cart_id, quoted_at DESC);

CREATE INDEX IF NOT EXISTS shipping_quote_order_ix
    ON shipping.shipping_quote (tenant_id, store_id, order_id, quoted_at DESC);

CREATE INDEX IF NOT EXISTS shipping_quote_provider_ix
    ON shipping.shipping_quote (tenant_id, store_id, provider_code, quoted_at DESC);
```

## Entity State Model

`shipping_origin` has a configuration lifecycle. `shipping_quote` is an immutable snapshot and
has no mutable lifecycle in the legacy source.

### Shipping origin lifecycle

- **States:** Inactive (initial), Active, Retired (terminal)
- **Transitions:**

| From | To | Trigger | Guard |
|---|---|---|---|
| Inactive | Active | BR-PRC-022 | Origin has required address, city, postal code, and store scope |
| Active | Inactive | BR-PRC-022 | Store administrator disables the origin |
| Active | Retired | BR-PRC-022 | Store administrator replaces the origin |
| Inactive | Retired | BR-PRC-022 | Store administrator deletes an unused origin |

### Shipping quote lifecycle

Quotes are append-only snapshots. The target may mark a quote as consumed in a downstream
projection, but MS-09 does not transition order or checkout state.

## Data Invariants

| Invariant ID | Statement | Entity | Kind | Tier |
|---|---|---|---|---|
| INV-SHP-001 | A quote price must be non-negative. | shipping_quote | constraint | db |
| INV-SHP-002 | A quote handling amount must be non-negative. | shipping_quote | constraint | db |
| INV-SHP-003 | At most one active origin exists for a store. | shipping_origin | referential | both |
| INV-SHP-004 | A quote destination country must be an uppercase ISO-3166 alpha-2 code when present. | shipping_quote | constraint | db |
| INV-SHP-005 | A calculated distance price is `distanceKm * applicableRate`; the source distance and rate must be retained in calculation audit data. | quote calculation | computed (`price = distanceKm * rate`) | app |
| INV-SHP-006 | An origin belongs to exactly one tenant and store scope. | shipping_origin | referential | both |
| INV-SHP-007 | A persisted quote contains a provider code and quoted timestamp. | shipping_quote | constraint | db |

## Database Logic Objects

No business-rule calculation is placed in the database. The partial unique index
`shipping_origin_one_active_per_store_uq` is a mandatory database integrity object represented
directly in the core DDL. All shipping policy, packaging, provider selection, distance pricing,
and Drools replacement logic remain app-tier by default.

## Persistence Notes

- Legacy `SHIPPING_QUOTE` rows are written once per final option.
- Legacy free-shipping returns before final quote persistence; this is retained as a flagged
  behavior under BR-PRC-026 and BR-EXT-011.
- Package definitions are embedded in shipping configuration JSON and are not assigned a
  standalone MS-09 table.
- Provider/module configuration remains in MS-11. MS-09 consumes a normalized configuration
  projection.
- No foreign key is created to MS-01, MS-02, MS-04, MS-10, MS-11, or MS-12 schemas.
