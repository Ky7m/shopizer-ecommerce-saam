# Content and Configuration — Domain Model

**Version:** 1.0  
**Date:** 2026-09-01  
**Status:** 🟡 In Progress — Phase 4 extraction  
**Service ID:** MS-11  
**Database:** PostgreSQL 16  
**Schema:** `content_configuration`

## Domain Boundary and Ownership

MS-11 owns merchant-scoped CMS content, localized content descriptions, provider-backed content-file metadata, merchant configuration records, integration-module metadata, and module-discovery cache policy.

The following are explicit reference boundaries and are not duplicated or owned by MS-11:

- `store_id` and `tenant_id` are opaque references resolved through MS-10 and the platform tenancy boundary. No foreign key is created to MS-10.
- `language_code` is resolved through shared reference data. No language table is created in this schema.
- `product_group` is an opaque catalog reference. Product-group ownership remains outside MS-11.
- Payment and shipping provider execution remains outside MS-11. MS-11 may invoke provider discovery and validation before saving configuration state.
- CMS file bytes remain in the selected provider. PostgreSQL stores metadata and provider-operation state only.
- `config.displayShipping` is a platform property, not merchant configuration state.
- `merchant_configuration` records containing `PAYMENT_MODULES` or `SHIPPING_MODULES` hold encrypted configuration state. They do not own payment, shipping, or provider-execution records.

## Legacy-to-Target Mapping

| Target table | Legacy table/model | Ownership and mapping |
|---|---|---|
| `content_configuration.content` | `CONTENT`, `com.salesmanager.core.model.content.Content` | Merchant-scoped CMS page, box, or section |
| `content_configuration.content_description` | `CONTENT_DESCRIPTION`, `Description`, `ContentDescription` | One localized description per content item and language |
| `content_configuration.content_file` | No legacy relational table; `ContentFile`, `InputContentFile`, `OutputContentFile` | Target metadata/state registry for external provider objects. File bytes are not stored in PostgreSQL |
| `content_configuration.merchant_configuration` | `MERCHANT_CONFIGURATION`, `MerchantConfiguration` | Merchant-scoped key/value configuration, including serialized or encrypted payloads |
| `content_configuration.module_configuration` | `MODULE_CONFIGURATION`, `IntegrationModule` | Global integration-module metadata and serialized environment definitions |
| Module discovery cache | `CacheUtils`, key `INTEGRATION_M<module>` | Application/cache infrastructure; no relational cache table |

## Core Entities

### Executable PostgreSQL DDL

```sql
CREATE EXTENSION IF NOT EXISTS pgcrypto;

CREATE SCHEMA IF NOT EXISTS content_configuration;

CREATE TABLE IF NOT EXISTS content_configuration.content (
    content_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID NOT NULL,
    store_id UUID NOT NULL,
    code VARCHAR(100) NOT NULL,
    content_type VARCHAR(10) NOT NULL,
    content_position VARCHAR(10),
    link_to_menu BOOLEAN NOT NULL DEFAULT FALSE,
    product_group TEXT,
    sort_order INTEGER NOT NULL DEFAULT 0,
    visible BOOLEAN NOT NULL DEFAULT FALSE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    modified_by VARCHAR(60),

    CONSTRAINT ck_content_code_nonempty
        CHECK (btrim(code) <> ''),
    CONSTRAINT ck_content_type
        CHECK (content_type IN ('BOX', 'PAGE', 'SECTION')),
    CONSTRAINT ck_content_position
        CHECK (content_position IS NULL OR content_position IN ('LEFT', 'RIGHT')),
    CONSTRAINT uq_content_tenant_store_code
        UNIQUE (tenant_id, store_id, code)
);

COMMENT ON COLUMN content_configuration.content.content_id IS
    'Maps to CONTENT.CONTENT_ID.';
COMMENT ON COLUMN content_configuration.content.tenant_id IS
    'Audit/multi-tenancy standard; validated by the platform tenancy boundary.';
COMMENT ON COLUMN content_configuration.content.store_id IS
    'Maps to CONTENT.MERCHANT_ID; opaque MS-10 store reference. No cross-service FK.';
COMMENT ON COLUMN content_configuration.content.code IS
    'Maps to CONTENT.CODE; BR-MER-013 requires uniqueness within a merchant store.';
COMMENT ON COLUMN content_configuration.content.content_type IS
    'Maps to CONTENT.CONTENT_TYPE; BR-MER-014 assigns PAGE or BOX from the operation.';
COMMENT ON COLUMN content_configuration.content.content_position IS
    'Maps to CONTENT.CONTENT_POSITION; legacy values are LEFT and RIGHT.';
COMMENT ON COLUMN content_configuration.content.link_to_menu IS
    'Maps to CONTENT.LINK_TO_MENU; BR-MER-018 keeps menu linkage independent of visibility.';
COMMENT ON COLUMN content_configuration.content.product_group IS
    'Maps to CONTENT.PRODUCT_GROUP; opaque catalog/product-group reference.';
COMMENT ON COLUMN content_configuration.content.sort_order IS
    'Maps to CONTENT.SORT_ORDER; BR-MER-019 orders merchant/type lists ascending.';
COMMENT ON COLUMN content_configuration.content.visible IS
    'Maps to CONTENT.VISIBLE; BR-MER-017 permits public friendly-URL lookup only when true.';
COMMENT ON COLUMN content_configuration.content.created_at IS
    'Maps to CONTENT.DATE_CREATED; audit/multi-tenancy standard.';
COMMENT ON COLUMN content_configuration.content.updated_at IS
    'Maps to CONTENT.DATE_MODIFIED; audit/multi-tenancy standard.';
COMMENT ON COLUMN content_configuration.content.modified_by IS
    'Maps to CONTENT.UPDT_ID.';

CREATE INDEX IF NOT EXISTS ix_content_tenant_store_type_sort
    ON content_configuration.content (tenant_id, store_id, content_type, sort_order, content_id);

CREATE INDEX IF NOT EXISTS ix_content_tenant_store_visibility
    ON content_configuration.content (tenant_id, store_id, visible);

CREATE INDEX IF NOT EXISTS ix_content_tenant_store_menu
    ON content_configuration.content (tenant_id, store_id, link_to_menu);


CREATE TABLE IF NOT EXISTS content_configuration.content_description (
    description_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    content_id UUID NOT NULL,
    language_code VARCHAR(10) NOT NULL,
    name VARCHAR(120) NOT NULL,
    title VARCHAR(100),
    description TEXT,
    friendly_url VARCHAR(120),
    meta_keywords TEXT,
    meta_title VARCHAR(100),
    meta_description TEXT,
    created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    modified_by VARCHAR(60),

    CONSTRAINT fk_content_description_content
        FOREIGN KEY (content_id)
        REFERENCES content_configuration.content (content_id)
        ON DELETE CASCADE,
    CONSTRAINT ck_content_description_name_nonempty
        CHECK (btrim(name) <> ''),
    CONSTRAINT ck_content_description_language_nonempty
        CHECK (btrim(language_code) <> ''),
    CONSTRAINT uq_content_description_content_language
        UNIQUE (content_id, language_code)
);

COMMENT ON COLUMN content_configuration.content_description.description_id IS
    'Maps to CONTENT_DESCRIPTION.DESCRIPTION_ID.';
COMMENT ON COLUMN content_configuration.content_description.content_id IS
    'Maps to CONTENT_DESCRIPTION.CONTENT_ID; local FK to content.content_id.';
COMMENT ON COLUMN content_configuration.content_description.language_code IS
    'Maps to CONTENT_DESCRIPTION.LANGUAGE_ID through the shared language reference boundary.';
COMMENT ON COLUMN content_configuration.content_description.name IS
    'Maps to Description.NAME; required localized display name.';
COMMENT ON COLUMN content_configuration.content_description.title IS
    'Maps to Description.TITLE.';
COMMENT ON COLUMN content_configuration.content_description.description IS
    'Maps to Description.DESCRIPTION; localized CMS body.';
COMMENT ON COLUMN content_configuration.content_description.friendly_url IS
    'Maps to CONTENT_DESCRIPTION.SEF_URL; used by BR-MER-017 public friendly-URL lookup.';
COMMENT ON COLUMN content_configuration.content_description.meta_keywords IS
    'Maps to CONTENT_DESCRIPTION.META_KEYWORDS.';
COMMENT ON COLUMN content_configuration.content_description.meta_title IS
    'Maps to CONTENT_DESCRIPTION.META_TITLE.';
COMMENT ON COLUMN content_configuration.content_description.meta_description IS
    'Maps to CONTENT_DESCRIPTION.META_DESCRIPTION.';
COMMENT ON COLUMN content_configuration.content_description.created_at IS
    'Maps to CONTENT_DESCRIPTION.DATE_CREATED; audit/multi-tenancy standard.';
COMMENT ON COLUMN content_configuration.content_description.updated_at IS
    'Maps to CONTENT_DESCRIPTION.DATE_MODIFIED; audit/multi-tenancy standard.';
COMMENT ON COLUMN content_configuration.content_description.modified_by IS
    'Maps to CONTENT_DESCRIPTION.UPDT_ID.';

CREATE INDEX IF NOT EXISTS ix_content_description_language
    ON content_configuration.content_description (language_code);

CREATE INDEX IF NOT EXISTS ix_content_description_friendly_url
    ON content_configuration.content_description (friendly_url);

CREATE INDEX IF NOT EXISTS ix_content_description_content_friendly_url
    ON content_configuration.content_description (content_id, friendly_url);


CREATE TABLE IF NOT EXISTS content_configuration.content_file (
    content_file_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID NOT NULL,
    store_id UUID NOT NULL,
    file_name TEXT NOT NULL,
    mime_type VARCHAR(255),
    file_content_type VARCHAR(30) NOT NULL,
    folder_path TEXT NOT NULL DEFAULT '/',
    provider_name VARCHAR(20) NOT NULL,
    provider_key TEXT NOT NULL,
    state VARCHAR(20) NOT NULL DEFAULT 'PENDING',
    created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT now(),

    CONSTRAINT ck_content_file_name
        CHECK (
            btrim(file_name) <> ''
            AND position('/' IN file_name) = 0
            AND position(chr(92) IN file_name) = 0
            AND position('..' IN file_name) = 0
        ),
    CONSTRAINT ck_content_file_type
        CHECK (
            file_content_type IN (
                'STATIC_FILE',
                'IMAGE',
                'LOGO',
                'PRODUCT',
                'PRODUCTLG',
                'PROPERTY',
                'VARIANT',
                'MANUFACTURER',
                'PRODUCT_DIGITAL',
                'API_IMAGE',
                'API_FILE'
            )
        ),
    CONSTRAINT ck_content_file_folder_path
        CHECK (folder_path ~ '^/$|^(/[A-Za-z0-9_-]+)+$'),
    CONSTRAINT ck_content_file_provider
        CHECK (provider_name IN ('default', 'httpd', 'aws', 'gcp')),
    CONSTRAINT ck_content_file_state
        CHECK (state IN ('PENDING', 'AVAILABLE', 'RENAME_PENDING', 'DELETED')),
    CONSTRAINT ck_content_file_provider_key_nonempty
        CHECK (btrim(provider_key) <> '')
);

COMMENT ON COLUMN content_configuration.content_file.content_file_id IS
    'Target metadata identity required by BR-EXT-023 and BR-EXT-030; ContentFile has no legacy relational identifier.';
COMMENT ON COLUMN content_configuration.content_file.tenant_id IS
    'Audit/multi-tenancy standard; validated by the platform tenancy boundary.';
COMMENT ON COLUMN content_configuration.content_file.store_id IS
    'Maps to the merchant-store code scope used by InputContentFile and provider APIs; opaque MS-10 reference.';
COMMENT ON COLUMN content_configuration.content_file.file_name IS
    'Maps to ContentFile.fileName and InputContentFile.fileName; BR-MER-023 and BR-EXT-030 protect the provider namespace.';
COMMENT ON COLUMN content_configuration.content_file.mime_type IS
    'Maps to ContentFile.mimeType; BR-MER-022 and BR-EXT-029 require MIME metadata preservation.';
COMMENT ON COLUMN content_configuration.content_file.file_content_type IS
    'Maps to StaticContentFile.fileContentType and FileContentType.';
COMMENT ON COLUMN content_configuration.content_file.folder_path IS
    'Maps to InputContentFile.path and ContentService folder operations; BR-MER-026 defines Linux-style path validation.';
COMMENT ON COLUMN content_configuration.content_file.provider_name IS
    'BR-EXT-021 target provider binding from config.cms.method: default, httpd, aws, or gcp.';
COMMENT ON COLUMN content_configuration.content_file.provider_key IS
    'BR-EXT-022 canonical provider object key used consistently for upload, retrieval, listing, rename, and deletion.';
COMMENT ON COLUMN content_configuration.content_file.state IS
    'Target metadata/state required by BR-MER-025, BR-EXT-023, and BR-EXT-030; legacy providers do not persist this state.';
COMMENT ON COLUMN content_configuration.content_file.created_at IS
    'Audit/multi-tenancy standard.';
COMMENT ON COLUMN content_configuration.content_file.updated_at IS
    'Audit/multi-tenancy standard.';

CREATE UNIQUE INDEX IF NOT EXISTS uq_content_file_active_provider_key
    ON content_configuration.content_file (tenant_id, store_id, provider_key)
    WHERE state <> 'DELETED';

CREATE UNIQUE INDEX IF NOT EXISTS uq_content_file_active_namespace_name
    ON content_configuration.content_file (
        tenant_id,
        store_id,
        file_content_type,
        folder_path,
        file_name
    )
    WHERE state <> 'DELETED';

CREATE INDEX IF NOT EXISTS ix_content_file_store_type_state
    ON content_configuration.content_file (
        tenant_id,
        store_id,
        file_content_type,
        state
    );

CREATE INDEX IF NOT EXISTS ix_content_file_store_folder
    ON content_configuration.content_file (
        tenant_id,
        store_id,
        folder_path
    );


CREATE TABLE IF NOT EXISTS content_configuration.merchant_configuration (
    merchant_configuration_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID NOT NULL,
    store_id UUID NOT NULL,
    config_key VARCHAR(255) NOT NULL,
    configuration_type VARCHAR(20) NOT NULL DEFAULT 'INTEGRATION',
    active BOOLEAN NOT NULL DEFAULT FALSE,
    value TEXT,
    created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    modified_by VARCHAR(60),

    CONSTRAINT ck_merchant_configuration_key_nonempty
        CHECK (btrim(config_key) <> ''),
    CONSTRAINT ck_merchant_configuration_type
        CHECK (configuration_type IN ('INTEGRATION', 'SHOP', 'CONFIG', 'SOCIAL')),
    CONSTRAINT uq_merchant_configuration_store_key
        UNIQUE (tenant_id, store_id, config_key)
);

COMMENT ON COLUMN content_configuration.merchant_configuration.merchant_configuration_id IS
    'Maps to MERCHANT_CONFIGURATION.MERCHANT_CONFIG_ID.';
COMMENT ON COLUMN content_configuration.merchant_configuration.tenant_id IS
    'Audit/multi-tenancy standard; validated by the platform tenancy boundary.';
COMMENT ON COLUMN content_configuration.merchant_configuration.store_id IS
    'Maps to MERCHANT_CONFIGURATION.MERCHANT_ID; opaque MS-10 store reference.';
COMMENT ON COLUMN content_configuration.merchant_configuration.config_key IS
    'Maps to MERCHANT_CONFIGURATION.CONFIG_KEY; BR-CF-001 defines store/key identity.';
COMMENT ON COLUMN content_configuration.merchant_configuration.configuration_type IS
    'Maps to MERCHANT_CONFIGURATION.TYPE and MerchantConfigurationType.';
COMMENT ON COLUMN content_configuration.merchant_configuration.active IS
    'Maps to MERCHANT_CONFIGURATION.ACTIVE; module summaries distinguish configured from active under BR-CF-014.';
COMMENT ON COLUMN content_configuration.merchant_configuration.value IS
    'Maps to MERCHANT_CONFIGURATION.VALUE. CONFIG values contain MerchantConfig JSON; integration values contain encrypted JSON.';
COMMENT ON COLUMN content_configuration.merchant_configuration.created_at IS
    'Maps to MERCHANT_CONFIGURATION.DATE_CREATED; audit/multi-tenancy standard.';
COMMENT ON COLUMN content_configuration.merchant_configuration.updated_at IS
    'Maps to MERCHANT_CONFIGURATION.DATE_MODIFIED; audit/multi-tenancy standard.';
COMMENT ON COLUMN content_configuration.merchant_configuration.modified_by IS
    'Maps to MERCHANT_CONFIGURATION.UPDT_ID.';

CREATE INDEX IF NOT EXISTS ix_merchant_configuration_store_type
    ON content_configuration.merchant_configuration (
        tenant_id,
        store_id,
        configuration_type
    );

CREATE INDEX IF NOT EXISTS ix_merchant_configuration_store_active
    ON content_configuration.merchant_configuration (
        tenant_id,
        store_id,
        active
    );


CREATE TABLE IF NOT EXISTS content_configuration.module_configuration (
    module_configuration_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    module_family VARCHAR(30) NOT NULL,
    code VARCHAR(100) NOT NULL,
    regions JSONB,
    configuration JSONB,
    details JSONB,
    module_type VARCHAR(100),
    image VARCHAR(255),
    custom_module BOOLEAN NOT NULL DEFAULT FALSE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    modified_by VARCHAR(60),

    CONSTRAINT ck_module_family_nonempty
        CHECK (btrim(module_family) <> ''),
    CONSTRAINT ck_module_code_nonempty
        CHECK (btrim(code) <> ''),
    CONSTRAINT ck_module_regions_array
        CHECK (regions IS NULL OR jsonb_typeof(regions) = 'array'),
    CONSTRAINT ck_module_configuration_array
        CHECK (
            configuration IS NULL
            OR jsonb_typeof(configuration) = 'array'
        ),
    CONSTRAINT ck_module_details_object
        CHECK (
            details IS NULL
            OR jsonb_typeof(details) = 'object'
        ),
    CONSTRAINT uq_module_configuration_code
        UNIQUE (code)
);

COMMENT ON COLUMN content_configuration.module_configuration.module_configuration_id IS
    'Maps to MODULE_CONFIGURATION.MODULE_CONF_ID.';
COMMENT ON COLUMN content_configuration.module_configuration.module_family IS
    'Maps to MODULE_CONFIGURATION.MODULE; reference definitions use PAYMENT and SHIPPING.';
COMMENT ON COLUMN content_configuration.module_configuration.code IS
    'Maps to MODULE_CONFIGURATION.CODE; BR-CF-010 replaces metadata by code, not family.';
COMMENT ON COLUMN content_configuration.module_configuration.regions IS
    'Maps to MODULE_CONFIGURATION.REGIONS; JSON array of country codes or wildcard *.';
COMMENT ON COLUMN content_configuration.module_configuration.configuration IS
    'Maps to MODULE_CONFIGURATION.CONFIGURATION; JSON array of environment definitions.';
COMMENT ON COLUMN content_configuration.module_configuration.details IS
    'Maps to MODULE_CONFIGURATION.DETAILS; JSON object of provider/display detail values.';
COMMENT ON COLUMN content_configuration.module_configuration.module_type IS
    'Maps to MODULE_CONFIGURATION.TYPE.';
COMMENT ON COLUMN content_configuration.module_configuration.image IS
    'Maps to MODULE_CONFIGURATION.IMAGE.';
COMMENT ON COLUMN content_configuration.module_configuration.custom_module IS
    'Maps to MODULE_CONFIGURATION.CUSTOM_IND; BR-CF-008 preserves boolean/string-to-boolean loading.';
COMMENT ON COLUMN content_configuration.module_configuration.created_at IS
    'Maps to MODULE_CONFIGURATION.DATE_CREATED; audit/multi-tenancy standard.';
COMMENT ON COLUMN content_configuration.module_configuration.updated_at IS
    'Maps to MODULE_CONFIGURATION.DATE_MODIFIED; audit/multi-tenancy standard.';
COMMENT ON COLUMN content_configuration.module_configuration.modified_by IS
    'Maps to MODULE_CONFIGURATION.UPDT_ID.';

CREATE INDEX IF NOT EXISTS ix_module_configuration_family
    ON content_configuration.module_configuration (module_family);

CREATE INDEX IF NOT EXISTS ix_module_configuration_family_code
    ON content_configuration.module_configuration (module_family, code);
```

## Structured Configuration Payloads

### `CONFIG` Merchant Configuration

When `configuration_type = 'CONFIG'` and `config_key = 'CONFIG'`, `value` contains the serialized `MerchantConfig` object.

The target payload preserves the legacy fields and defaults:

```json
{
  "displayCustomerSection": false,
  "displayContactUs": false,
  "displayStoreAddress": false,
  "displayAddToCartOnFeaturedItems": false,
  "displayCustomerAgreement": false,
  "displayPagesMenu": true,
  "allowPurchaseItems": true,
  "displaySearchBox": true,
  "testMode": false,
  "debugMode": false,
  "useDefaultSearchConfig": {
    "en": true
  },
  "defaultSearchConfigPath": {
    "en": "search/default-en.json"
  }
}
```

`useDefaultSearchConfig` is a language-code-to-boolean map.  
`defaultSearchConfigPath` is a language-code-to-path map.  
Null boolean values and blank search paths are omitted during serialization, as required by BR-CF-002.

`testMode`, `debugMode`, and search configuration paths are internal fields and must not be projected through the public configuration response under BR-CF-003.

### Payment and Shipping Integration Configuration

For `PAYMENT_MODULES` and `SHIPPING_MODULES`, `value` contains encrypted JSON at rest. The plaintext structure before encryption is an array of module configurations:

```json
[
  {
    "moduleCode": "stripe",
    "active": true,
    "defaultSelected": true,
    "environment": "TEST",
    "integrationKeys": {
      "secretKey": "provider-secret",
      "publishableKey": "pk_test_123"
    },
    "integrationOptions": {
      "captureMode": [
        "automatic"
      ]
    }
  }
]
```

The following rules apply:

- Reads decrypt before parsing.
- Writes validate against the selected provider, serialize, encrypt, and then persist.
- `integrationOptions` must be parsed independently of `integrationKeys`.
- Secret values must be masked or omitted from API responses.
- The configuration record is merchant-scoped and must not reference provider-execution tables.
- `environment` is descriptive configuration state; provider execution remains outside MS-11.

### Module Environment Configuration

`module_configuration.configuration` is a JSON array whose entries have the fields represented by `ModuleConfig`:

```json
[
  {
    "env": "TEST",
    "scheme": "https",
    "host": "gateway.test",
    "port": "443",
    "uri": "/rate",
    "config1": "client-id",
    "config2": "tenant-id"
  },
  {
    "env": "PROD",
    "scheme": "https",
    "host": "gateway.example",
    "port": "443",
    "uri": "/rate",
    "config1": "client-id",
    "config2": "tenant-id"
  }
]
```

The target parser must preserve `config1` and `config2` as separate fields. Duplicate environment entries are rejected at the application boundary rather than silently overwriting the earlier entry in a map.

## Provider-Backed File Model

### File Metadata

The legacy `ContentFile`, `InputContentFile`, and `OutputContentFile` classes are transport objects. The legacy CMS path does not write a relational file table:

- Infinispan stores bytes under a merchant/type/name node.
- The local provider stores files under a merchant/type/name directory.
- S3 and GCP store objects under provider keys.
- MIME type is supplied or guessed from the file name.
- File retrieval returns bytes outside the relational database.

`content_file` is therefore a target metadata/state registry, not a legacy byte-storage table. It exists to make provider identity, MIME preservation, rename recovery, deletion state, and idempotency explicit under BR-MER-025, BR-EXT-023, BR-EXT-029, and BR-EXT-030.

The canonical target provider key is:

```text
files/<merchant-store-code>/<file-content-type>/<folder-path>/<file-name>
```

For root-level files, the folder component is omitted or represented by `/`. The target must use one deterministic key strategy for all provider operations. The legacy provider implementations differ in whether they include the type segment for `IMAGE` and `STATIC_FILE`; the target canonical key must retain the content-type namespace so equal names cannot collide across types.

The `store_id` column remains the ownership boundary. The store code used in `provider_key` is resolved through MS-10 and is not duplicated as an ownership column.

### Provider Selection

`provider_name` corresponds to `config.cms.method`:

| `provider_name` | Legacy implementation | Capability note |
|---|---|---|
| `default` | Infinispan content provider | Byte storage and file lookup supported; missing file returns null |
| `httpd` | Local content provider | Upload and removal implemented; retrieval/listing are not implemented in the inspected source |
| `aws` | S3 content provider | Upload, retrieval, listing, and removal implemented; folder operations are incomplete |
| `gcp` | GCP content provider | Upload, retrieval, listing, and removal implemented; folder operations are incomplete |

There is no automatic fallback between providers. An unavailable or unsupported selected provider must produce an explicit capability or provider failure.

### Folder Semantics

Folder paths are validated by the application using Linux-style path syntax:

- `/` is valid.
- Nested segments contain only letters, digits, underscores, and hyphens.
- Folder enumeration and deletion are provider-dependent and incomplete in the legacy implementation.
- No relational `content_folder` table is created because no legacy relational folder model exists.
- A target folder operation must not return success when the selected provider performed no operation.

## Entity State Model

### Content lifecycle

`content.visible` represents the persisted content publication flag. There is no legacy status column; the following is the target closed logical state model.

| State | Representation |
|---|---|
| Hidden | `visible = false`; (initial) state for a newly created entity because the legacy Java field defaults to `false` |
| Visible | `visible = true` |
| Deleted | Row removed; terminal state and therefore not queryable |

| From | To | Trigger | Guard |
|---|---|---|---|
| Hidden | Visible | BR-MER-018 | Content mutation sets visibility to true |
| Visible | Hidden | BR-MER-018 | Content mutation sets visibility to false |
| Hidden | Deleted | BR-MER-021 | Delete is scoped to the owning store |
| Visible | Deleted | BR-MER-021 | Delete is scoped to the owning store |

`link_to_menu` is not a lifecycle state and must not be used as a substitute for `visible`.

### Merchant configuration lifecycle

This lifecycle applies to configuration records for which `active` is meaningful, especially payment and shipping module configuration.

| State | Representation |
|---|---|
| Inactive | `active = false`; (initial) state |
| Active | `active = true` |
| Deleted | Row removed; terminal state |

| From | To | Trigger | Guard |
|---|---|---|---|
| Inactive | Active | BR-CF-006 / BR-CF-014 | Provider validation succeeds before persistence |
| Active | Inactive | BR-CF-006 / BR-CF-014 | Configuration update explicitly disables the module |
| Inactive | Deleted | BR-CF-001 | Store-scoped configuration deletion |
| Active | Deleted | BR-CF-001 | Store-scoped configuration deletion |

A configuration record's existence means `configured = true`; it does not imply `active = true`.

### Module metadata lifecycle

Module metadata is globally persisted and has no status column.

| State | Representation |
|---|---|
| Registered | A row exists for a module code; (initial) state |
| Replaced | The prior row is deleted and a new definition with the same code is inserted |
| Removed | No row exists for the former definition; terminal state |

| From | To | Trigger | Guard |
|---|---|---|---|
| Registered | Replaced | BR-CF-010 | Replacement is identified by the same module code |
| Registered | Removed | BR-CF-010 | Existing module code is selected for removal |
| Replaced | Registered | BR-CF-010 | Replacement insert succeeds |

After replacement, the cache entry `INTEGRATION_M<module_family>` must be invalidated under BR-EXT-026.

### Provider-backed file lifecycle

| State | Meaning |
|---|---|
| `PENDING` | Metadata exists while the provider write is in progress; (initial) state |
| `AVAILABLE` | Provider object is confirmed available |
| `RENAME_PENDING` | Original object has been read and a replacement name is being written |
| `DELETED` | Metadata tombstone; provider object is absent or deletion has been confirmed; terminal state |

| From | To | Trigger | Guard |
|---|---|---|---|
| PENDING | AVAILABLE | BR-EXT-023 | Selected provider confirms upload |
| PENDING | DELETED | BR-EXT-023 / BR-EXT-030 | Upload fails and metadata is rolled back or tombstoned |
| AVAILABLE | RENAME_PENDING | BR-MER-025 | Original file exists and rename request is valid |
| RENAME_PENDING | AVAILABLE | BR-MER-025 / BR-EXT-029 | New provider key is written with original MIME and type metadata |
| RENAME_PENDING | AVAILABLE | BR-MER-025 | Rename failure restores or retains the original object |
| AVAILABLE | DELETED | BR-EXT-030 | Scoped, idempotent deletion succeeds |
`DELETED` is terminal for the file metadata lifecycle. A later upload creates a new metadata row beginning in `PENDING`; it does not transition the deleted row. A failed legacy remove-then-create rename must never be reported as successful.

## Data Invariants

| Invariant ID | Statement | Entity | Kind | Tier | DDL or enforcement binding |
|---|---|---|---|---|---|
| INV-MER-001 | A merchant store cannot contain two CMS items with the same content code, regardless of whether the items are pages, boxes, or sections. | `content` | uniqueness | db | `uq_content_tenant_store_code`; BR-MER-013 |
| INV-MER-002 | A content item can have at most one localized description for a language. | `content_description` | uniqueness | db | `uq_content_description_content_language`; BR-MER-015 |
| INV-MER-003 | Every content description belongs to an existing content item and is deleted with its parent. | `content_description` | referential | db | `fk_content_description_content`; BR-MER-021 |
| INV-MER-004 | Content type is one of `BOX`, `PAGE`, or `SECTION`; page and box operations assign their own type rather than accepting a client-supplied type. | `content` | lifecycle/validation | both | `ck_content_type` plus application operation binding; BR-MER-014 |
| INV-MER-005 | Visibility and menu linkage are independent policies. | `content` | policy | app | BR-MER-018 |
| INV-MER-006 | Public friendly-URL lookup returns only content whose visibility is true and whose description belongs to the requested store. | `content`, `content_description` | cross-entity | app | Query predicate required by BR-MER-017 |
| INV-MER-007 | Content lists are scoped by tenant, store, and content type and ordered by ascending sort order. | `content` | query policy | app | `ix_content_tenant_store_type_sort`; BR-MER-019 |
| INV-CF-001 | A store has at most one merchant configuration record for a configuration key. | `merchant_configuration` | uniqueness | db | `uq_merchant_configuration_store_key`; BR-CF-001 |
| INV-CF-002 | Merchant configuration type is one of `INTEGRATION`, `SHOP`, `CONFIG`, or `SOCIAL`. | `merchant_configuration` | domain constraint | db | `ck_merchant_configuration_type` |
| INV-CF-003 | A `CONFIG` payload contains the typed `MerchantConfig` fields and language-keyed search maps; missing configuration has an explicit target absence policy. | `merchant_configuration` | payload | app | BR-CF-002 and BR-CF-015 |
| INV-CF-004 | Payment and shipping module configuration is encrypted at rest and decrypted only inside the protected configuration boundary. | `merchant_configuration` | compliance | app | BR-CF-006 and BR-EXT-027 |
| INV-CF-005 | Integration configuration parsing loads `integrationOptions` independently of `integrationKeys`. | `merchant_configuration` | payload | app | BR-CF-007 |
| INV-CF-006 | Saving payment or shipping configuration requires selected-provider validation before the encrypted value is written. | `merchant_configuration` | cross-service | app | BR-CF-013 and BR-EXT-025 |
| INV-MOD-001 | An integration-module code identifies one global metadata definition. | `module_configuration` | uniqueness | db | `uq_module_configuration_code`; BR-CF-010 |
| INV-MOD-002 | Module regions are a JSON array and module details are a JSON object when present. | `module_configuration` | payload | both | `ck_module_regions_array`, `ck_module_details_object`; BR-CF-008 |
| INV-MOD-003 | Module environment configuration is a JSON array; each environment entry has a distinct `env` value and preserves `config1` separately from `config2`. | `module_configuration` | payload | app | `ck_module_configuration_array`; BR-CF-009 and BR-EXT-028 |
| INV-MOD-004 | Replacing module metadata invalidates the cache for the affected module family before subsequent discovery. | `module_configuration`, cache | consistency | app | BR-EXT-026 |
| INV-FILE-001 | Active provider metadata is unique by tenant, store, and provider key. | `content_file` | uniqueness | db | `uq_content_file_active_provider_key`; BR-EXT-022 |
| INV-FILE-002 | Active files are unique by tenant, store, file-content type, folder path, and file name. | `content_file` | uniqueness | db | `uq_content_file_active_namespace_name`; BR-MER-024 |
| INV-FILE-003 | File names cannot introduce path separators or traversal segments into a provider namespace. | `content_file` | security/integrity | both | `ck_content_file_name`; BR-MER-023 and BR-EXT-030 |
| INV-FILE-004 | A provider-backed file retains its file-content type and MIME metadata across rename. | `content_file` | cross-operation | app | BR-MER-025 and BR-EXT-029 |
| INV-FILE-005 | A missing provider object is represented as not found, not as an empty successful file. | `content_file` | provider consistency | app | BR-EXT-023 |
| INV-FILE-006 | Provider selection is explicit and operations do not silently fall back to another provider. | `content_file` | routing | app | `ck_content_file_provider`; BR-EXT-021 |
| INV-FILE-007 | File deletion is store-scoped and idempotency behavior is explicit. | `content_file` | authorization | both | Store scope and partial active indexes; BR-EXT-030 |

## Cache and Projection Semantics

Module discovery uses an application cache rather than a relational table.

- Cache key: `INTEGRATION_M` concatenated with the requested module family, for example `INTEGRATION_MPAYMENT` or `INTEGRATION_MSHIPPING`.
- Cache value: hydrated `IntegrationModule` metadata, including parsed regions, details, and environment configuration.
- On cache miss:
  1. Read `module_configuration` by `module_family`.
  2. Parse `regions`, `details`, and `configuration`.
  3. Preserve `config1` and `config2` independently.
  4. Append runtime payment-starter metadata when the requested family is payment.
  5. Cache the resulting list.
- On module replacement, invalidate the affected family cache before returning success.
- A cached module's existence does not make a merchant module configured or active. Those values come from the merchant's decrypted integration configuration under BR-CF-014.
- Cache failures must produce an explicit discovery failure rather than an empty successful module list.

## Persistence and Transaction Notes

- Content page and box mutations persist the content row and localized descriptions as one unit.
- Content deletion cascades to local descriptions through `fk_content_description_content`.
- Omitted descriptions in an update request must be removed explicitly by the application to implement BR-MER-015 replacement semantics; the database cascade applies only when the parent content row is deleted.
- Merchant configuration updates are store-scoped and must use the unique `(tenant_id, store_id, config_key)` identity.
- Module replacement is logically a delete-and-create operation by `code`; cache invalidation is part of the same application operation.
- Provider object writes are not part of the PostgreSQL transaction. File metadata state must be reconciled with provider success or failure.
- No payment charge, shipping quote, CMS provider execution, or provider-owned operational table is persisted by MS-11.