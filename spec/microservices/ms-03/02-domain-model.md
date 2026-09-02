# Search Service — Domain Model

**Service ID:** MS-03  
**Database schema:** `search`  
**Ownership:** Search projections, localized fields, provider metadata, query profiles, and rebuild state. Product/catalog data remains an opaque MS-02 reference.

## Owned entities

| Entity | Purpose |
|---|---|
| `search_index` | Store-scoped provider configuration and operational state |
| `search_document` | Product/store/locale projection identity and lifecycle |
| `search_document_locale` | Localized searchable fields |
| `search_document_inventory` | Projected product and variant stock/price entries |
| `search_query_profile` | Provider-neutral locale mappings and limits |
| `search_rebuild_job` | Asynchronous rebuild request, progress, and outcome |

## PostgreSQL DDL

```sql
CREATE EXTENSION IF NOT EXISTS pgcrypto;
CREATE SCHEMA IF NOT EXISTS search;

CREATE TABLE search.search_index (
    search_index_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID NOT NULL,
    store_id VARCHAR(120) NOT NULL,
    provider_name VARCHAR(120) NOT NULL,
    configured_locales TEXT[] NOT NULL,
    configuration_version BIGINT NOT NULL DEFAULT 1,
    state VARCHAR(20) NOT NULL DEFAULT 'Configured',
    last_success_at TIMESTAMPTZ,
    last_failure_at TIMESTAMPTZ,
    last_failure_code VARCHAR(80),
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT uq_search_index_tenant_store UNIQUE (tenant_id, store_id),
    CONSTRAINT ck_search_index_store CHECK (length(trim(store_id)) > 0),
    CONSTRAINT ck_search_index_provider CHECK (length(trim(provider_name)) > 0),
    CONSTRAINT ck_search_index_locales CHECK (cardinality(configured_locales) > 0),
    CONSTRAINT ck_search_index_state CHECK (state IN ('Configured','Building','Ready','Degraded','Disabled'))
);

CREATE TABLE search.search_document (
    document_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    search_index_id UUID NOT NULL REFERENCES search.search_index(search_index_id),
    tenant_id UUID NOT NULL,
    store_id VARCHAR(120) NOT NULL,
    product_id BIGINT NOT NULL,
    locale VARCHAR(16) NOT NULL,
    provider_document_key VARCHAR(300) NOT NULL,
    state VARCHAR(20) NOT NULL DEFAULT 'Active',
    source_version BIGINT,
    indexed_at TIMESTAMPTZ,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT uq_search_document_identity UNIQUE (search_index_id, product_id, locale),
    CONSTRAINT uq_search_document_provider_key UNIQUE (search_index_id, provider_document_key),
    CONSTRAINT ck_search_document_store CHECK (length(trim(store_id)) > 0),
    CONSTRAINT ck_search_document_locale CHECK (length(trim(locale)) > 0),
    CONSTRAINT ck_search_document_state CHECK (state IN ('Active','Removed'))
);

CREATE TABLE search.search_document_locale (
    document_id UUID PRIMARY KEY REFERENCES search.search_document(document_id) ON DELETE CASCADE,
    name TEXT NOT NULL,
    description TEXT,
    product_link TEXT,
    brand_name TEXT,
    category_name TEXT,
    attributes JSONB NOT NULL DEFAULT '{}'::jsonb,
    image_url TEXT,
    review_average NUMERIC(5,2),
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT ck_search_locale_name CHECK (length(trim(name)) > 0),
    CONSTRAINT ck_search_locale_attributes CHECK (jsonb_typeof(attributes) = 'object'),
    CONSTRAINT ck_search_locale_review CHECK (review_average IS NULL OR review_average BETWEEN 0 AND 5)
);

CREATE TABLE search.search_document_inventory (
    inventory_entry_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    document_id UUID NOT NULL REFERENCES search.search_document(document_id) ON DELETE CASCADE,
    sku VARCHAR(160) NOT NULL,
    variant_sku VARCHAR(160),
    quantity NUMERIC(19,4) NOT NULL,
    price NUMERIC(19,4) NOT NULL,
    discounted_price NUMERIC(19,4),
    option_values JSONB NOT NULL DEFAULT '{}'::jsonb,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT ck_search_inventory_sku CHECK (length(trim(sku)) > 0),
    CONSTRAINT ck_search_inventory_quantity CHECK (quantity >= 0),
    CONSTRAINT ck_search_inventory_price CHECK (price >= 0),
    CONSTRAINT ck_search_inventory_discount CHECK (discounted_price IS NULL OR discounted_price >= 0),
    CONSTRAINT ck_search_inventory_options CHECK (jsonb_typeof(option_values) = 'object')
);

CREATE TABLE search.search_query_profile (
    query_profile_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    search_index_id UUID NOT NULL REFERENCES search.search_index(search_index_id),
    locale VARCHAR(16) NOT NULL,
    provider_query_name VARCHAR(160) NOT NULL,
    product_mapping_version VARCHAR(80) NOT NULL,
    keyword_mapping_version VARCHAR(80) NOT NULL,
    settings JSONB NOT NULL DEFAULT '{}'::jsonb,
    product_mapping JSONB NOT NULL DEFAULT '{}'::jsonb,
    keyword_mapping JSONB NOT NULL DEFAULT '{}'::jsonb,
    default_result_limit INTEGER NOT NULL DEFAULT 100,
    autocomplete_limit INTEGER NOT NULL DEFAULT 15,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT uq_search_query_profile_locale UNIQUE (search_index_id, locale),
    CONSTRAINT ck_search_query_profile_locale CHECK (length(trim(locale)) > 0),
    CONSTRAINT ck_search_query_profile_default_limit CHECK (default_result_limit BETWEEN 1 AND 100),
    CONSTRAINT ck_search_query_profile_autocomplete_limit CHECK (autocomplete_limit BETWEEN 1 AND 15),
    CONSTRAINT ck_search_query_profile_settings CHECK (jsonb_typeof(settings) = 'object'),
    CONSTRAINT ck_search_query_profile_product_mapping CHECK (jsonb_typeof(product_mapping) = 'object'),
    CONSTRAINT ck_search_query_profile_keyword_mapping CHECK (jsonb_typeof(keyword_mapping) = 'object')
);

CREATE TABLE search.search_rebuild_job (
    rebuild_job_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    search_index_id UUID NOT NULL REFERENCES search.search_index(search_index_id),
    tenant_id UUID NOT NULL,
    store_id VARCHAR(120) NOT NULL,
    requested_by VARCHAR(200) NOT NULL,
    idempotency_key VARCHAR(200) NOT NULL,
    state VARCHAR(20) NOT NULL DEFAULT 'Requested',
    requested_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    started_at TIMESTAMPTZ,
    completed_at TIMESTAMPTZ,
    indexed_document_count BIGINT NOT NULL DEFAULT 0,
    failed_document_count BIGINT NOT NULL DEFAULT 0,
    error_code VARCHAR(80),
    error_message TEXT,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT uq_search_rebuild_idempotency UNIQUE (search_index_id, idempotency_key),
    CONSTRAINT ck_search_rebuild_store CHECK (length(trim(store_id)) > 0),
    CONSTRAINT ck_search_rebuild_state CHECK (state IN ('Requested','Running','Succeeded','Failed','Cancelled')),
    CONSTRAINT ck_search_rebuild_counts CHECK (indexed_document_count >= 0 AND failed_document_count >= 0),
    CONSTRAINT ck_search_rebuild_dates CHECK (completed_at IS NULL OR started_at IS NULL OR completed_at >= started_at)
);

CREATE INDEX ix_search_document_product ON search.search_document (tenant_id, store_id, product_id);
CREATE INDEX ix_search_document_state ON search.search_document (search_index_id, state);
CREATE INDEX ix_search_document_locale ON search.search_document (search_index_id, locale);
CREATE INDEX ix_search_inventory_document ON search.search_document_inventory (document_id);
CREATE INDEX ix_search_rebuild_state ON search.search_rebuild_job (search_index_id, state, requested_at);
```

## Entity State Model

### Search index lifecycle

States: `Configured (initial)`, `Building`, `Ready`, `Degraded`, `Disabled (terminal)`.

| From | To | Trigger | Guard |
|---|---|---|---|
| Configured | Building | BR-CAT-032 | Valid rebuild accepted while indexing is enabled |
| Configured | Disabled | BR-EXT-023 | Deployment disables indexing |
| Building | Ready | BR-CAT-032 | Rebuild completes successfully |
| Building | Degraded | BR-EXT-024 | Provider failure leaves a usable prior projection |
| Building | Disabled | BR-EXT-023 | Operator retires the index while disabled |
| Ready | Building | BR-CAT-032 | Valid rebuild accepted |
| Ready | Degraded | BR-EXT-024 | Provider health failure |
| Degraded | Building | BR-CAT-032 | Retry/rebuild accepted |
| Degraded | Disabled | BR-EXT-023 | Operator retires the index |

### Search document lifecycle

States: `Active (initial)`, `Removed (terminal)`.

| From | To | Trigger | Guard |
|---|---|---|---|
| Active | Removed | BR-CAT-032 | Replacement or product deletion removes the prior document |
| Active | Removed | BR-CAT-023 | Product deletion event is accepted |

### Rebuild job lifecycle

States: `Requested (initial)`, `Running`, `Succeeded (terminal)`, `Failed (terminal)`, `Cancelled (terminal)`.

| From | To | Trigger | Guard |
|---|---|---|---|
| Requested | Running | BR-CAT-032 | Worker claims an enabled-index job |
| Requested | Cancelled | BR-CAT-032 | Authorized cancellation before claim |
| Running | Succeeded | BR-CAT-032 | All required products process successfully |
| Running | Failed | BR-EXT-024 | Provider failure reaches terminal retry policy |
| Running | Cancelled | BR-CAT-032 | Authorized administrative cancellation |

## Data Invariants

| Invariant ID | Statement | Entity | Kind | Tier |
|---|---|---|---|---|
| INV-SRCH-001 | A product has at most one document per index and locale | search_document | identity | both |
| INV-SRCH-002 | Every active document has one localized field row | search_document_locale | referential | db |
| INV-SRCH-003 | Projected quantity and prices are non-negative | search_document_inventory | constraint | db |
| INV-SRCH-004 | Autocomplete limit remains between 1 and 15 | search_query_profile | constraint | both |
| INV-SRCH-005 | Default result limit remains between 1 and 100 | search_query_profile | constraint | both |
| INV-SRCH-006 | Rebuild counters cannot be negative | search_rebuild_job | constraint | db |
| INV-SRCH-007 | Completion cannot precede start | search_rebuild_job | cross-field | db |
| INV-SRCH-008 | Every projection is isolated by tenant and store | all search entities | cross-entity | both |
| INV-SRCH-009 | Search owns no foreign key into MS-02 data | cross-service boundary | cross-entity | both |

## Database Logic Objects

No views, procedures, functions, or triggers are specified. Integrity is enforced by PostgreSQL constraints; business orchestration remains in the application tier.

## Phase 4b inferred data clarifications

- `[Inferred in Phase 4b — Mode A]` Valid no-match searches are represented by an empty
  response page; no placeholder document is persisted.
- `[Inferred in Phase 4b — Mode A]` Rebuild idempotency is scoped by
  `(search_index_id, idempotency_key)`, and terminal failures retain the last error code and
  source version for replay diagnostics.

CREATE INDEX IF NOT EXISTS search_document_scope_updated_idx
    ON search.search_document (tenant_id, store_id, updated_at);
CREATE INDEX IF NOT EXISTS search_rebuild_job_scope_status_idx
    ON search.search_rebuild_job (tenant_id, store_id, status, created_at);
