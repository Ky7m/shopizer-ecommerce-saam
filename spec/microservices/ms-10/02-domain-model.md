# Merchant and Store Administration — Domain Model

**Service ID:** MS-10  
**Schema:** `merchant_store`  
**Database:** PostgreSQL 16  
**Ownership:** MS-10 owns store identity, hierarchy, supported-language links, defaults, and branding metadata. Country, language, currency, units, CMS content, and file bytes are external/shared references.

## Core Entities

```sql
CREATE EXTENSION IF NOT EXISTS pgcrypto;
CREATE SCHEMA IF NOT EXISTS merchant_store;

CREATE TABLE merchant_store.stores (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  tenant_id VARCHAR(100) NOT NULL,
  code VARCHAR(100) NOT NULL,
  name VARCHAR(150) NOT NULL,
  email_address VARCHAR(320) NOT NULL,
  phone VARCHAR(50) NOT NULL,
  street_address VARCHAR(256),
  city VARCHAR(100) NOT NULL,
  postal_code VARCHAR(30) NOT NULL,
  country_code VARCHAR(10) NOT NULL,
  state_province VARCHAR(100),
  zone_code VARCHAR(30),
  retailer BOOLEAN NOT NULL DEFAULT FALSE,
  parent_store_id UUID REFERENCES merchant_store.stores(id) ON DELETE RESTRICT,
  default_language_code VARCHAR(10) NOT NULL,
  currency_code VARCHAR(10) NOT NULL,
  dimension_unit VARCHAR(20) NOT NULL,
  weight_unit VARCHAR(20) NOT NULL,
  template_code VARCHAR(100),
  logo_uri VARCHAR(1024),
  status VARCHAR(20) NOT NULL DEFAULT 'Active'
    CHECK (status IN ('Active', 'Suspended', 'Deleted')),
  created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
  updated_at TIMESTAMPTZ NOT NULL DEFAULT now(),
  CONSTRAINT uq_store_tenant_code UNIQUE (tenant_id, code),
  CONSTRAINT ck_store_not_self_parent CHECK (parent_store_id IS NULL OR parent_store_id <> id)
);

CREATE INDEX ix_store_tenant_name ON merchant_store.stores (tenant_id, name);
CREATE INDEX ix_store_parent ON merchant_store.stores (parent_store_id);
CREATE INDEX ix_store_retailer ON merchant_store.stores (tenant_id, retailer);

CREATE TABLE merchant_store.store_languages (
  store_id UUID NOT NULL REFERENCES merchant_store.stores(id) ON DELETE CASCADE,
  language_code VARCHAR(10) NOT NULL,
  PRIMARY KEY (store_id, language_code)
);

CREATE INDEX ix_store_languages_language ON merchant_store.store_languages (language_code);
```

`country_code`, `zone_code`, `currency_code`, language codes, and measurement units are opaque references resolved through shared contracts. CMS records and logo bytes are not duplicated here; `logo_uri` is only the provider reference.

## Entity State Model

#### Store lifecycle
| State | Type |
|---|---|
| Active | initial |
| Suspended | — |
| Deleted | terminal |

| From | To | Trigger (BR-ID) | Guard |
|---|---|---|---|
| Active | Suspended | BR-MSA-VAL-002 | An authorized identity-management operation suspends the store |
| Suspended | Active | BR-MSA-VAL-002 | An authorized identity-management operation reactivates the store |
| Active | Deleted | BR-MER-006 | Store is not the protected default store |
| Suspended | Deleted | BR-MER-006 | Store is not the protected default store |

## Data Invariants

| Invariant ID | Statement | Entity | Kind | Tier |
|---|---|---|---|---|
| INV-MER-001 | Store code is unique within a tenant. | stores | uniqueness | db |
| INV-MER-002 | A store cannot be its own parent. | stores | referential | db |
| INV-MER-003 | A default language must appear in the store’s supported-language set. | stores/store_languages | cross-entity | both |
| INV-MER-004 | A child store references an existing parent and is never orphaned by parent deletion. | stores | referential | db |
| INV-MER-005 | Store status is one of Active, Suspended, or Deleted. | stores | lifecycle | db |

## Boundary and provenance notes

- `stores.tenant_id` is owned by the platform tenancy boundary; it is not a foreign key to another service.
- `country_code`, `zone_code`, `currency_code`, language codes, `dimension_unit`, and `weight_unit` map from legacy reference entities and are resolved through shared/reference APIs.
- `template_code` and `logo_uri` preserve store branding metadata. CMS content and binary file storage remain MS-11/provider concerns.
- `parent_store_id` is an internal MS-10 relationship; no cross-service foreign key is created.
