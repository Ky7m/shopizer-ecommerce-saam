# MS-08 Tax Domain Model

**Version:** 1.0  
**Database:** PostgreSQL  
**Schema:** `tax_schema`  
**Ownership:** MS-08 only

## Domain boundaries

MS-08 owns tax classes, tax rates, localized tax-rate descriptions, tax configuration, and calculation quotes/results. `tenant_id`, `store_id`, `product_id`, `order_id`, `customer_id`, and address reference values are boundary identifiers or snapshots. They intentionally have no foreign keys to other services.

Internal foreign keys are limited to MS-08-owned tables.

## Executable PostgreSQL DDL

```sql
CREATE EXTENSION IF NOT EXISTS pgcrypto;

CREATE SCHEMA IF NOT EXISTS tax_schema;

CREATE TABLE IF NOT EXISTS tax_schema.tax_classes (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID NOT NULL,
    store_id UUID NOT NULL,
    code VARCHAR(10) NOT NULL,
    title VARCHAR(32) NOT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    created_by UUID NULL,
    correlation_id UUID NULL,

    CONSTRAINT tax_classes_code_not_blank
        CHECK (length(btrim(code)) BETWEEN 1 AND 10),
    CONSTRAINT tax_classes_title_not_blank
        CHECK (length(btrim(title)) BETWEEN 1 AND 32),
    CONSTRAINT tax_classes_tenant_store_code_uk
        UNIQUE (tenant_id, store_id, code)
);

COMMENT ON COLUMN tax_schema.tax_classes.id
    IS 'BR-TAX-CLS-001/002/003: tax-class identity';
COMMENT ON COLUMN tax_schema.tax_classes.tenant_id
    IS 'Audit/multi-tenancy standard';
COMMENT ON COLUMN tax_schema.tax_classes.store_id
    IS 'BR-TAX-CLS-001/002/003: store ownership scope';
COMMENT ON COLUMN tax_schema.tax_classes.code
    IS 'Maps to TAX_CLASS.TAX_CLASS_CODE';
COMMENT ON COLUMN tax_schema.tax_classes.title
    IS 'Maps to TAX_CLASS.TAX_CLASS_TITLE';
COMMENT ON COLUMN tax_schema.tax_classes.created_at
    IS 'Audit/multi-tenancy standard';
COMMENT ON COLUMN tax_schema.tax_classes.updated_at
    IS 'Audit/multi-tenancy standard';
COMMENT ON COLUMN tax_schema.tax_classes.created_by
    IS 'Audit/multi-tenancy standard';
COMMENT ON COLUMN tax_schema.tax_classes.correlation_id
    IS 'Audit/multi-tenancy standard';

CREATE INDEX IF NOT EXISTS tax_classes_store_idx
    ON tax_schema.tax_classes (tenant_id, store_id);

CREATE TABLE IF NOT EXISTS tax_schema.tax_rates (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID NOT NULL,
    store_id UUID NOT NULL,
    tax_class_id UUID NOT NULL,
    code VARCHAR(100) NOT NULL,
    rate_percent NUMERIC(7,4) NOT NULL,
    priority INTEGER NOT NULL DEFAULT 0,
    piggyback BOOLEAN NOT NULL DEFAULT FALSE,
    country_code VARCHAR(3) NOT NULL,
    zone_code VARCHAR(100) NULL,
    state_province VARCHAR(100) NULL,
    parent_rate_id UUID NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    created_by UUID NULL,
    correlation_id UUID NULL,

    CONSTRAINT tax_rates_tax_class_fk
        FOREIGN KEY (tax_class_id)
        REFERENCES tax_schema.tax_classes (id)
        ON DELETE RESTRICT,

    CONSTRAINT tax_rates_parent_fk
        FOREIGN KEY (parent_rate_id)
        REFERENCES tax_schema.tax_rates (id)
        ON DELETE CASCADE,

    CONSTRAINT tax_rates_code_not_blank
        CHECK (length(btrim(code)) BETWEEN 1 AND 100),
    CONSTRAINT tax_rates_percent_range
        CHECK (rate_percent >= 0 AND rate_percent <= 100),
    CONSTRAINT tax_rates_priority_nonnegative
        CHECK (priority >= 0),
    CONSTRAINT tax_rates_country_not_blank
        CHECK (length(btrim(country_code)) BETWEEN 2 AND 3),
    CONSTRAINT tax_rates_tenant_store_code_uk
        UNIQUE (tenant_id, store_id, code)
);

COMMENT ON COLUMN tax_schema.tax_rates.id
    IS 'BR-TAX-RAT-001/002/004: tax-rate identity';
COMMENT ON COLUMN tax_schema.tax_rates.tenant_id
    IS 'Audit/multi-tenancy standard';
COMMENT ON COLUMN tax_schema.tax_rates.store_id
    IS 'BR-TAX-RAT-001/002/003/004/005: store ownership scope';
COMMENT ON COLUMN tax_schema.tax_rates.tax_class_id
    IS 'Maps to TAX_RATE.TAX_CLASS_ID; BR-TAX-CAL-008';
COMMENT ON COLUMN tax_schema.tax_rates.code
    IS 'Maps to TAX_RATE.TAX_CODE';
COMMENT ON COLUMN tax_schema.tax_rates.rate_percent
    IS 'Maps to TAX_RATE.TAX_RATE; BR-TAX-CAL-009';
COMMENT ON COLUMN tax_schema.tax_rates.priority
    IS 'Maps to TAX_RATE.TAX_PRIORITY; BR-TAX-CAL-008/009';
COMMENT ON COLUMN tax_schema.tax_rates.piggyback
    IS 'Maps to TAX_RATE.PIGGYBACK; BR-TAX-CAL-009';
COMMENT ON COLUMN tax_schema.tax_rates.country_code
    IS 'Maps to TAX_RATE.COUNTRY_ID through reference-data code; BR-TAX-CAL-008';
COMMENT ON COLUMN tax_schema.tax_rates.zone_code
    IS 'Maps to TAX_RATE.ZONE_ID through reference-data code; BR-TAX-CAL-008';
COMMENT ON COLUMN tax_schema.tax_rates.state_province
    IS 'Maps to TAX_RATE.STORE_STATE_PROV; corrected mapper behavior';
COMMENT ON COLUMN tax_schema.tax_rates.parent_rate_id
    IS 'Maps to TAX_RATE.PARENT_ID';
COMMENT ON COLUMN tax_schema.tax_rates.created_at
    IS 'Audit/multi-tenancy standard';
COMMENT ON COLUMN tax_schema.tax_rates.updated_at
    IS 'Audit/multi-tenancy standard';
COMMENT ON COLUMN tax_schema.tax_rates.created_by
    IS 'Audit/multi-tenancy standard';
COMMENT ON COLUMN tax_schema.tax_rates.correlation_id
    IS 'Audit/multi-tenancy standard';

CREATE INDEX IF NOT EXISTS tax_rates_lookup_idx
    ON tax_schema.tax_rates (
        tenant_id,
        store_id,
        country_code,
        zone_code,
        state_province,
        tax_class_id,
        priority
    );

CREATE INDEX IF NOT EXISTS tax_rates_class_idx
    ON tax_schema.tax_rates (tenant_id, store_id, tax_class_id);

CREATE TABLE IF NOT EXISTS tax_schema.tax_rate_descriptions (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tax_rate_id UUID NOT NULL,
    language_code VARCHAR(10) NOT NULL,
    name VARCHAR(255) NOT NULL,
    title VARCHAR(255) NULL,
    description TEXT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    created_by UUID NULL,
    correlation_id UUID NULL,

    CONSTRAINT tax_rate_descriptions_rate_fk
        FOREIGN KEY (tax_rate_id)
        REFERENCES tax_schema.tax_rates (id)
        ON DELETE CASCADE,

    CONSTRAINT tax_rate_descriptions_name_not_blank
        CHECK (length(btrim(name)) > 0),
    CONSTRAINT tax_rate_descriptions_rate_language_uk
        UNIQUE (tax_rate_id, language_code)
);

COMMENT ON COLUMN tax_schema.tax_rate_descriptions.id
    IS 'Maps to TAX_RATE_DESCRIPTION identifier';
COMMENT ON COLUMN tax_schema.tax_rate_descriptions.tax_rate_id
    IS 'Maps to TAX_RATE_DESCRIPTION.TAX_RATE_ID';
COMMENT ON COLUMN tax_schema.tax_rate_descriptions.language_code
    IS 'Maps to TAX_RATE_DESCRIPTION.LANGUAGE_ID through reference-data code';
COMMENT ON COLUMN tax_schema.tax_rate_descriptions.name
    IS 'Maps to Description.name; BR-TAX-CAL-009';
COMMENT ON COLUMN tax_schema.tax_rate_descriptions.title
    IS 'Maps to Description.title';
COMMENT ON COLUMN tax_schema.tax_rate_descriptions.description
    IS 'Maps to Description.description';
COMMENT ON COLUMN tax_schema.tax_rate_descriptions.created_at
    IS 'Audit/multi-tenancy standard';
COMMENT ON COLUMN tax_schema.tax_rate_descriptions.updated_at
    IS 'Audit/multi-tenancy standard';
COMMENT ON COLUMN tax_schema.tax_rate_descriptions.created_by
    IS 'Audit/multi-tenancy standard';
COMMENT ON COLUMN tax_schema.tax_rate_descriptions.correlation_id
    IS 'Audit/multi-tenancy standard';

CREATE INDEX IF NOT EXISTS tax_rate_descriptions_language_idx
    ON tax_schema.tax_rate_descriptions (language_code);

CREATE TABLE IF NOT EXISTS tax_schema.tax_configurations (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID NOT NULL,
    store_id UUID NOT NULL,
    tax_basis VARCHAR(32) NOT NULL DEFAULT 'ShippingAddress',
    collect_tax_if_different_province BOOLEAN NOT NULL DEFAULT TRUE,
    different_country_behavior VARCHAR(32) NOT NULL DEFAULT 'UseCustomerJurisdiction',
    created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    created_by UUID NULL,
    correlation_id UUID NULL,

    CONSTRAINT tax_configurations_basis_ck
        CHECK (tax_basis IN ('StoreAddress', 'ShippingAddress', 'BillingAddress')),
    CONSTRAINT tax_configurations_country_behavior_ck
        CHECK (different_country_behavior IN
            ('UseCustomerJurisdiction', 'UseStoreJurisdiction', 'NoTax')),
    CONSTRAINT tax_configurations_tenant_store_uk
        UNIQUE (tenant_id, store_id)
);

COMMENT ON COLUMN tax_schema.tax_configurations.id
    IS 'BR-TAX-CFG-001/002: configuration identity';
COMMENT ON COLUMN tax_schema.tax_configurations.tenant_id
    IS 'Audit/multi-tenancy standard';
COMMENT ON COLUMN tax_schema.tax_configurations.store_id
    IS 'BR-TAX-CFG-001/002: store configuration scope';
COMMENT ON COLUMN tax_schema.tax_configurations.tax_basis
    IS 'Maps to TaxConfiguration.taxBasisCalculation; BR-TAX-CAL-002';
COMMENT ON COLUMN tax_schema.tax_configurations.collect_tax_if_different_province
    IS 'Maps to TaxConfiguration.collectTaxIfDifferentProvinceOfStoreCountry; BR-TAX-CAL-003';
COMMENT ON COLUMN tax_schema.tax_configurations.different_country_behavior
    IS 'BR-TAX-CAL-004: explicit replacement for ambiguous legacy boolean';
COMMENT ON COLUMN tax_schema.tax_configurations.created_at
    IS 'Audit/multi-tenancy standard';
COMMENT ON COLUMN tax_schema.tax_configurations.updated_at
    IS 'Audit/multi-tenancy standard';
COMMENT ON COLUMN tax_schema.tax_configurations.created_by
    IS 'Audit/multi-tenancy standard';
COMMENT ON COLUMN tax_schema.tax_configurations.correlation_id
    IS 'Audit/multi-tenancy standard';

CREATE TABLE IF NOT EXISTS tax_schema.tax_quotes (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID NOT NULL,
    store_id UUID NOT NULL,
    idempotency_key VARCHAR(128) NULL,
    currency_code VARCHAR(3) NOT NULL,
    status VARCHAR(16) NOT NULL DEFAULT 'Calculated',
    customer_id UUID NULL,
    order_id UUID NULL,
    jurisdiction_country_code VARCHAR(3) NULL,
    jurisdiction_zone_code VARCHAR(100) NULL,
    jurisdiction_state_province VARCHAR(100) NULL,
    taxable_amount NUMERIC(19,4) NOT NULL DEFAULT 0,
    total_tax_amount NUMERIC(19,4) NOT NULL DEFAULT 0,
    calculated_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    correlation_id UUID NULL,

    CONSTRAINT tax_quotes_status_ck
        CHECK (status IN ('Calculated', 'Failed')),
    CONSTRAINT tax_quotes_amounts_nonnegative_ck
        CHECK (taxable_amount >= 0 AND total_tax_amount >= 0),
    CONSTRAINT tax_quotes_currency_ck
        CHECK (currency_code ~ '^[A-Z]{3}$'),
    CONSTRAINT tax_quotes_idempotency_uk
        UNIQUE (tenant_id, store_id, idempotency_key)
);

COMMENT ON COLUMN tax_schema.tax_quotes.id
    IS 'BR-TAX-CAL-001 through BR-TAX-CAL-010: calculation result identity';
COMMENT ON COLUMN tax_schema.tax_quotes.tenant_id
    IS 'Audit/multi-tenancy standard';
COMMENT ON COLUMN tax_schema.tax_quotes.store_id
    IS 'BR-TAX-CAL-002/003/004: calculation store scope';
COMMENT ON COLUMN tax_schema.tax_quotes.idempotency_key
    IS 'BR-TAX-CAL-010: replay-safe calculation request key';
COMMENT ON COLUMN tax_schema.tax_quotes.currency_code
    IS 'BR-TAX-CAL-001: calculation monetary context';
COMMENT ON COLUMN tax_schema.tax_quotes.status
    IS 'BR-TAX-CAL-001: calculation result status';
COMMENT ON COLUMN tax_schema.tax_quotes.customer_id
    IS 'Boundary identifier; owned by MS-01; no cross-service foreign key';
COMMENT ON COLUMN tax_schema.tax_quotes.order_id
    IS 'Boundary identifier; owned by MS-04/MS-05; no cross-service foreign key';
COMMENT ON COLUMN tax_schema.tax_quotes.jurisdiction_country_code
    IS 'BR-TAX-CAL-002/004/005';
COMMENT ON COLUMN tax_schema.tax_quotes.jurisdiction_zone_code
    IS 'BR-TAX-CAL-002/003/005';
COMMENT ON COLUMN tax_schema.tax_quotes.jurisdiction_state_province
    IS 'BR-TAX-CAL-002/003/005';
COMMENT ON COLUMN tax_schema.tax_quotes.taxable_amount
    IS 'BR-TAX-CAL-006/007: computed sum of taxable inputs';
COMMENT ON COLUMN tax_schema.tax_quotes.total_tax_amount
    IS 'BR-TAX-CAL-009/010: computed sum of tax items';
COMMENT ON COLUMN tax_schema.tax_quotes.calculated_at
    IS 'Audit/multi-tenancy standard';
COMMENT ON COLUMN tax_schema.tax_quotes.correlation_id
    IS 'Audit/multi-tenancy standard';

CREATE INDEX IF NOT EXISTS tax_quotes_store_created_idx
    ON tax_schema.tax_quotes (tenant_id, store_id, calculated_at DESC);

CREATE TABLE IF NOT EXISTS tax_schema.tax_quote_items (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tax_quote_id UUID NOT NULL,
    tax_class_id UUID NULL,
    tax_code VARCHAR(100) NOT NULL,
    label VARCHAR(255) NOT NULL,
    rate_percent NUMERIC(7,4) NOT NULL,
    taxable_amount NUMERIC(19,4) NOT NULL DEFAULT 0,
    tax_amount NUMERIC(19,4) NOT NULL DEFAULT 0,
    piggyback BOOLEAN NOT NULL DEFAULT FALSE,
    priority INTEGER NOT NULL DEFAULT 0,

    CONSTRAINT tax_quote_items_quote_fk
        FOREIGN KEY (tax_quote_id)
        REFERENCES tax_schema.tax_quotes (id)
        ON DELETE CASCADE,
    CONSTRAINT tax_quote_items_class_fk
        FOREIGN KEY (tax_class_id)
        REFERENCES tax_schema.tax_classes (id)
        ON DELETE SET NULL,
    CONSTRAINT tax_quote_items_rate_ck
        CHECK (rate_percent >= 0 AND rate_percent <= 100),
    CONSTRAINT tax_quote_items_amounts_ck
        CHECK (taxable_amount >= 0 AND tax_amount >= 0),
    CONSTRAINT tax_quote_items_priority_ck
        CHECK (priority >= 0)
);

COMMENT ON COLUMN tax_schema.tax_quote_items.id
    IS 'BR-TAX-CAL-009/010: tax-item identity';
COMMENT ON COLUMN tax_schema.tax_quote_items.tax_quote_id
    IS 'BR-TAX-CAL-010: owning calculation result';
COMMENT ON COLUMN tax_schema.tax_quote_items.tax_class_id
    IS 'BR-TAX-CAL-006/008: resolved tax class';
COMMENT ON COLUMN tax_schema.tax_quote_items.tax_code
    IS 'Maps to TaxItem.taxRate.code; BR-TAX-CAL-010';
COMMENT ON COLUMN tax_schema.tax_quote_items.label
    IS 'Maps to TaxItem.label; BR-TAX-CAL-009';
COMMENT ON COLUMN tax_schema.tax_quote_items.rate_percent
    IS 'Maps to TaxRate.taxRate; BR-TAX-CAL-009';
COMMENT ON COLUMN tax_schema.tax_quote_items.taxable_amount
    IS 'BR-TAX-CAL-006/007/009: calculation provenance';
COMMENT ON COLUMN tax_schema.tax_quote_items.tax_amount
    IS 'Maps to TaxItem.itemPrice; BR-TAX-CAL-009/010';
COMMENT ON COLUMN tax_schema.tax_quote_items.piggyback
    IS 'Maps to TaxRate.piggyback; BR-TAX-CAL-009';
COMMENT ON COLUMN tax_schema.tax_quote_items.priority
    IS 'Maps to TaxRate.taxPriority; BR-TAX-CAL-008/009';

CREATE INDEX IF NOT EXISTS tax_quote_items_quote_idx
    ON tax_schema.tax_quote_items (tax_quote_id, priority);
