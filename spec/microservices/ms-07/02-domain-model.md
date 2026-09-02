
**Service ID:** MS-07  
**Database schema:** `pricing_promotions`  
**Ownership:** Product and variant price selection, price windows, price adjustments, promotion definitions, coupon-code evaluation, and promotion processor registration. Product, variant, and availability facts remain owned by MS-02 and are referenced by opaque identifiers.

## Domain boundary

MS-07 owns commercial price and promotion data used to calculate a deterministic pricing result. It does not own product, variant, inventory, customer, tax, shipping, cart, or order entities.

The following references are intentionally not foreign keys because they belong to other service schemas:

- `product_sku` and `variant_sku` reference catalog identities owned by MS-02.
- `availability_id` references an MS-02 availability identity.
- `customer_id`, when supplied to a pricing request, is an opaque context value and does not alter the extracted pricing result.
- Cart and order identifiers belong to MS-04 and MS-05 and are not persisted by the pricing calculation.

## Owned entities

| Entity | Purpose | Source or target basis |
|---|---|---|
| `price_list` | Tenant/store/currency scope for commercial price entries | Required by BR-PRC-001, BR-PRC-002, and the MS-10 store boundary |
| `price_entry` | Base, additional, and special-window product or variant price | Maps to `PRODUCT_PRICE` and BR-PRC-001 through BR-PRC-005 |
| `price_entry_description` | Localized descriptions associated with a price entry | Maps to the `ProductPrice.descriptions` relationship read by the price repository |
| `promotion` | Promotion definition and discount-rate rule metadata | Required by BR-PRC-009 and BR-PRC-010 |
| `coupon` | Store-scoped promotion code and eligibility window | Required by BR-PRC-009 through BR-PRC-012 |

The extracted legacy source contains no persisted coupon-redemption reservation implementation. Coupon redemption reservation is therefore not modeled as an active MS-07 entity in this Phase 4 package; it requires a separately approved rule and contract before persistence is introduced.

## PostgreSQL DDL

```sql
CREATE EXTENSION IF NOT EXISTS pgcrypto;

CREATE SCHEMA IF NOT EXISTS pricing_promotions;

CREATE TABLE pricing_promotions.price_list (
    price_list_id UUID PRIMARY KEY DEFAULT gen_random_uuid(), -- Required by BR-PRC-001 to group prices into a selectable commercial scope
    tenant_id UUID NOT NULL, -- Audit/multi-tenancy standard
    store_id VARCHAR(120) NOT NULL, -- Required by BR-PRC-001 and MS-10 store scope
    name VARCHAR(200) NOT NULL, -- Required by BR-PRC-002 to identify a price collection
    currency_code CHAR(3) NOT NULL, -- Required by BR-PRC-013 because monetary results require currency context
    is_active BOOLEAN NOT NULL DEFAULT TRUE, -- Required by BR-PRC-001 to select an active commercial price scope
    created_by VARCHAR(200), -- Audit/multi-tenancy standard
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP, -- Audit/multi-tenancy standard
    updated_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP, -- Audit/multi-tenancy standard
    CONSTRAINT ck_price_list_store_not_blank
        CHECK (length(trim(store_id)) > 0),
    CONSTRAINT ck_price_list_name_not_blank
        CHECK (length(trim(name)) > 0),
    CONSTRAINT ck_price_list_currency
        CHECK (currency_code ~ '^[A-Z]{3}$'),
    CONSTRAINT uq_price_list_tenant_store_currency
        UNIQUE (tenant_id, store_id, currency_code, name)
);

CREATE TABLE pricing_promotions.price_entry (
    price_entry_id UUID PRIMARY KEY DEFAULT gen_random_uuid(), -- Maps to PRODUCT_PRICE.PRODUCT_PRICE_ID
    price_list_id UUID NOT NULL REFERENCES pricing_promotions.price_list(price_list_id) ON DELETE CASCADE, -- Required by BR-PRC-001 and BR-PRC-002 for price-list ownership
    legacy_price_id BIGINT, -- Maps to PRODUCT_PRICE.PRODUCT_PRICE_ID for migration traceability
    product_sku VARCHAR(160) NOT NULL, -- Maps to the product SKU used by ProductPriceRepository queries and BR-PRC-001
    variant_sku VARCHAR(160), -- Maps to the variant SKU used by ProductPriceRepository queries and BR-PRC-007
    availability_id BIGINT, -- Maps to PRODUCT_AVAILABILITY.PRODUCT_AVAIL_ID; MS-02 owns the referenced availability
    code VARCHAR(80) NOT NULL DEFAULT 'base', -- Maps to PRODUCT_PRICE.PRODUCT_PRICE_CODE and BR-PRC-002
    amount NUMERIC(19,4) NOT NULL DEFAULT 0, -- Maps to PRODUCT_PRICE.PRODUCT_PRICE_AMOUNT and BR-PRC-003
    price_type VARCHAR(20) NOT NULL DEFAULT 'OneTime', -- Maps to PRODUCT_PRICE.PRODUCT_PRICE_TYPE and BR-PRC-002
    is_default BOOLEAN NOT NULL DEFAULT FALSE, -- Maps to PRODUCT_PRICE.DEFAULT_PRICE and BR-PRC-002
    special_start_date DATE, -- Maps to PRODUCT_PRICE.PRODUCT_PRICE_SPECIAL_ST_DATE and BR-PRC-003
    special_end_date DATE, -- Maps to PRODUCT_PRICE.PRODUCT_PRICE_SPECIAL_END_DATE and BR-PRC-003
    special_amount NUMERIC(19,4), -- Maps to PRODUCT_PRICE.PRODUCT_PRICE_SPECIAL_AMOUNT and BR-PRC-003
    product_identifier_id BIGINT, -- Maps to PRODUCT_PRICE.PRODUCT_IDENTIFIER_ID
    created_by VARCHAR(200), -- Audit/multi-tenancy standard
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP, -- Audit/multi-tenancy standard
    updated_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP, -- Audit/multi-tenancy standard
    CONSTRAINT ck_price_entry_product_sku
        CHECK (length(trim(product_sku)) > 0),
    CONSTRAINT ck_price_entry_variant_sku
        CHECK (variant_sku IS NULL OR length(trim(variant_sku)) > 0),
    CONSTRAINT ck_price_entry_code
        CHECK (code ~ '^[A-Za-z0-9_]+$'),
    CONSTRAINT ck_price_entry_amount
        CHECK (amount >= 0),
    CONSTRAINT ck_price_entry_special_amount
        CHECK (special_amount IS NULL OR special_amount >= 0),
    CONSTRAINT ck_price_entry_type
        CHECK (price_type IN ('OneTime', 'Monthly')),
    CONSTRAINT ck_price_entry_special_dates
        CHECK (
            special_start_date IS NULL
            OR special_end_date IS NULL
            OR special_start_date <= special_end_date
        ),
    CONSTRAINT uq_price_entry_legacy_id
        UNIQUE (legacy_price_id)
);

CREATE TABLE pricing_promotions.price_entry_description (
    price_entry_description_id UUID PRIMARY KEY DEFAULT gen_random_uuid(), -- Required by the ProductPrice.descriptions relationship
    price_entry_id UUID NOT NULL REFERENCES pricing_promotions.price_entry(price_entry_id) ON DELETE CASCADE, -- Maps to the ProductPrice-to-description association
    language_code VARCHAR(16) NOT NULL, -- Required by localized price administration and price read conversion
    description TEXT, -- Maps to the localized price description associated with ProductPrice
    created_by VARCHAR(200), -- Audit/multi-tenancy standard
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP, -- Audit/multi-tenancy standard
    updated_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP, -- Audit/multi-tenancy standard
    CONSTRAINT ck_price_entry_description_language
        CHECK (length(trim(language_code)) > 0),
    CONSTRAINT uq_price_entry_description_language
        UNIQUE (price_entry_id, language_code)
);

CREATE TABLE pricing_promotions.promotion (
    promotion_id UUID PRIMARY KEY DEFAULT gen_random_uuid(), -- Required by BR-PRC-009 to identify a promotion definition
    tenant_id UUID NOT NULL, -- Audit/multi-tenancy standard
    store_id VARCHAR(120) NOT NULL, -- Required by BR-PRC-009 and MS-10 store scope
    name VARCHAR(200) NOT NULL, -- Required by BR-PRC-009 to identify a promotion definition
    rule_key VARCHAR(160) NOT NULL, -- Maps to the rule identity used by the promotion rule boundary, including Bam0520
    discount_rate NUMERIC(9,6) NOT NULL, -- Maps to the discount value assigned to OrderTotalResponse by BR-PRC-009 and BR-PRC-010
    valid_from DATE, -- Required by BR-PRC-010 for promotion validity windows
    valid_until DATE, -- Required by BR-PRC-010 for promotion validity windows
    is_enabled BOOLEAN NOT NULL DEFAULT TRUE, -- Required by BR-PRC-008 through BR-PRC-012 for processor eligibility
    created_by VARCHAR(200), -- Audit/multi-tenancy standard
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP, -- Audit/multi-tenancy standard
    updated_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP, -- Audit/multi-tenancy standard
    CONSTRAINT ck_promotion_store_not_blank
        CHECK (length(trim(store_id)) > 0),
    CONSTRAINT ck_promotion_name_not_blank
        CHECK (length(trim(name)) > 0),
    CONSTRAINT ck_promotion_rule_key_not_blank
        CHECK (length(trim(rule_key)) > 0),
    CONSTRAINT ck_promotion_discount_rate
        CHECK (discount_rate >= 0 AND discount_rate <= 1),
    CONSTRAINT ck_promotion_valid_dates
        CHECK (
            valid_from IS NULL
            OR valid_until IS NULL
            OR valid_from <= valid_until
        ),
    CONSTRAINT uq_promotion_rule_scope
        UNIQUE (tenant_id, store_id, rule_key)
);

CREATE TABLE pricing_promotions.coupon (
    coupon_id UUID PRIMARY KEY DEFAULT gen_random_uuid(), -- Required by BR-PRC-009 to identify a coupon code
    promotion_id UUID NOT NULL REFERENCES pricing_promotions.promotion(promotion_id) ON DELETE CASCADE, -- Required by BR-PRC-009 to connect a code to a promotion
    tenant_id UUID NOT NULL, -- Audit/multi-tenancy standard
    store_id VARCHAR(120) NOT NULL, -- Required by BR-PRC-009 and MS-10 store scope
    code VARCHAR(160) NOT NULL, -- Maps to OrderSummary.promoCode and BR-PRC-009/BR-PRC-010
    valid_from DATE, -- Required by BR-PRC-010 for code-specific validity
    valid_until DATE, -- Required by BR-PRC-010 for code-specific validity
    is_enabled BOOLEAN NOT NULL DEFAULT TRUE, -- Required by BR-PRC-008 through BR-PRC-012 for eligibility
    created_by VARCHAR(200), -- Audit/multi-tenancy standard
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP, -- Audit/multi-tenancy standard
    updated_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP, -- Audit/multi-tenancy standard
    CONSTRAINT ck_coupon_store_not_blank
        CHECK (length(trim(store_id)) > 0),
    CONSTRAINT ck_coupon_code_not_blank
        CHECK (length(trim(code)) > 0),
    CONSTRAINT ck_coupon_valid_dates
        CHECK (
            valid_from IS NULL
            OR valid_until IS NULL
            OR valid_from <= valid_until
        ),
    CONSTRAINT uq_coupon_code_scope
        UNIQUE (tenant_id, store_id, code),
    CONSTRAINT uq_coupon_promotion_code
        UNIQUE (promotion_id, code)
);

CREATE INDEX ix_price_entry_product
    ON pricing_promotions.price_entry (price_list_id, product_sku);

CREATE INDEX ix_price_entry_variant
    ON pricing_promotions.price_entry (price_list_id, variant_sku)
    WHERE variant_sku IS NOT NULL;

CREATE INDEX ix_price_entry_availability
    ON pricing_promotions.price_entry (availability_id)
    WHERE availability_id IS NOT NULL;

CREATE INDEX ix_price_entry_active_window
    ON pricing_promotions.price_entry (special_start_date, special_end_date)
    WHERE special_amount IS NOT NULL;

CREATE INDEX ix_price_entry_description_language
    ON pricing_promotions.price_entry_description (price_entry_id, language_code);

CREATE INDEX ix_promotion_enabled_window
    ON pricing_promotions.promotion (tenant_id, store_id, is_enabled, valid_from, valid_until);

CREATE INDEX ix_coupon_enabled_window
    ON pricing_promotions.coupon (tenant_id, store_id, is_enabled, valid_from, valid_until);
```

## Entity State Model

The extracted legacy pricing records, promotions, and coupons do not contain an explicit persisted lifecycle/status field. Their effective state is derived from `is_active`/`is_enabled` and date-window predicates. No closed state machine is introduced for those entities because doing so would invent lifecycle transitions not present in the source.

## Data Invariants

| Invariant ID | Statement | Entity | Kind | Tier |
|---|---|---|---|---|
| INV-PRC-001 | Every price entry belongs to exactly one price list | `price_entry` | referential | db |
| INV-PRC-002 | A price entry has a non-negative base amount | `price_entry` | constraint | db |
| INV-PRC-003 | A special amount, when present, is non-negative | `price_entry` | constraint | db |
| INV-PRC-004 | A bounded special-price window cannot end before it starts | `price_entry` | cross-field | db |
| INV-PRC-005 | A price code contains only letters, digits, and underscores | `price_entry` | constraint | db |
| INV-PRC-006 | A price type is either `OneTime` or `Monthly` | `price_entry` | constraint | db |
| INV-PRC-007 | A promotion discount rate is between zero and one inclusive | `promotion` | constraint | db |
| INV-PRC-008 | A bounded promotion window cannot end before it starts | `promotion` | cross-field | db |
| INV-PRC-009 | A coupon code is unique within a tenant and store | `coupon` | identity | both |
| INV-PRC-010 | A promotion rule key is unique within a tenant and store | `promotion` | identity | both |
| INV-PRC-011 | Price and promotion data is isolated by tenant and store | all owned entities | cross-entity | both |
| INV-PRC-012 | Product, variant, and availability references remain opaque MS-02 identifiers and are not cross-service foreign keys | `price_entry` | cross-entity | both |
| INV-PRC-013 | A localized price-entry description is unique per price entry and language | `price_entry_description` | identity | db |

## Default-price selection constraint

The source selects the default price in application logic and does not define a database uniqueness constraint for multiple default prices. The target application must reject or deterministically resolve multiple default entries before calculation. A partial unique index may be introduced only after the product/variant/availability identity contract with MS-02 is finalized; the current DDL preserves the legacy data shape without creating a cross-service identity assumption.

## Source-to-target mapping

| Source concept | Target entity/field | Mapping |
|---|---|---|
| `PRODUCT_PRICE.PRODUCT_PRICE_ID` | `price_entry.legacy_price_id` | Preserved for migration traceability; target primary key is service-owned UUID |
| `PRODUCT_PRICE.PRODUCT_PRICE_CODE` | `price_entry.code` | Price code, default `base` |
| `PRODUCT_PRICE.PRODUCT_PRICE_AMOUNT` | `price_entry.amount` | Base/original amount |
| `PRODUCT_PRICE.PRODUCT_PRICE_TYPE` | `price_entry.price_type` | `ONE_TIME` becomes `OneTime`; `MONTHLY` becomes `Monthly` |
| `PRODUCT_PRICE.DEFAULT_PRICE` | `price_entry.is_default` | Primary-price designation |
| `PRODUCT_PRICE.PRODUCT_PRICE_SPECIAL_ST_DATE` | `price_entry.special_start_date` | Special-price start date |
| `PRODUCT_PRICE.PRODUCT_PRICE_SPECIAL_END_DATE` | `price_entry.special_end_date` | Special-price end date |
| `PRODUCT_PRICE.PRODUCT_PRICE_SPECIAL_AMOUNT` | `price_entry.special_amount` | Special/effective amount |
| `PRODUCT_PRICE.PRODUCT_AVAIL_ID` | `price_entry.availability_id` | Opaque MS-02 availability reference |
| `PRODUCT_PRICE.PRODUCT_IDENTIFIER_ID` | `price_entry.product_identifier_id` | Optional source identifier |
| `ProductPrice.descriptions` | `price_entry_description` | Localized description association |
| `OrderSummary.promoCode` | `coupon.code` / promotion evaluation input | Promotion-code evaluation context |
| `OrderTotalResponse.discount` | `promotion.discount_rate` / calculated response | Positive fractional discount rate |
| `PromoCoupon.drl` rule name | `promotion.rule_key` | Rule-boundary identity, including `Bam0520` |
| Store code | `price_list.store_id`, `promotion.store_id`, `coupon.store_id` | Store isolation reference; MS-10 remains authoritative |

## Persistence and ownership constraints

- No table in this schema has a foreign key into `catalog_product`, `customer_identity`, `cart_checkout`, `order_management`, `tax`, or `shipping`.
- Product, variant, and availability existence must be validated through MS-02 contracts.
- MS-07 does not persist cart subtotals, order totals, shipping totals, tax totals, or payment values.
- `FinalPrice` is a calculated response model, not a persisted entity.
- Discount percentage, effective price, additional prices, and promotion reductions are calculated values and must not be treated as independently authoritative stored totals.
- The source has no customer-specific pricing rule. `customer_id` is therefore not stored in price-entry selection data.
- The source has no persisted promotion-redemption reservation implementation. Reservation tables and redemption state transitions remain out of scope pending an approved business rule.

## Phase 4b inferred data clarifications

- `[Inferred in Phase 4b — Mode A]` Promotion evaluation orders candidates by explicit
  exclusivity and priority fields; equal-priority candidates use stable promotion ID order.
- `[Inferred in Phase 4b — Mode A]` Coupon reservation identity is the pair
  `(store_id, checkout_idempotency_key)` and is unique for the lifetime of a checkout.

CREATE INDEX IF NOT EXISTS pricing_promotion_scope_priority_idx
    ON pricing_promotions.promotion (store_id, enabled, priority DESC);
CREATE INDEX IF NOT EXISTS pricing_coupon_scope_code_idx
    ON pricing_promotions.coupon (store_id, code);
```

[Turn 3]
[Message]
Return artifact 3 now: complete contents of spec/microservices/ms-07/03-api-design.md only. Use one fenced markdown block; if too large, split into numbered chunks and stop after first chunk.
