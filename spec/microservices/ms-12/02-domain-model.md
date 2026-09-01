# Platform Integrations — Domain Model

**Version:** 2.0  
**Date:** 2026-09-01  
**Service ID:** MS-12  
**PostgreSQL schema:** `platform_integrations`  
**Declared table count:** 5

MS-12 stores only provider execution projections and delivery reliability state. Merchant and
module configuration remains owned by MS-11; order, shipping, payment, and product/media records
are opaque references and are not copied into this schema.

## Core Entities

The following is executable PostgreSQL DDL. Tables are base migrations (order `0`), followed by
functions (orders `10`–`16`) and triggers (orders `30`–`36`).

```sql
CREATE SCHEMA IF NOT EXISTS platform_integrations;

CREATE TABLE IF NOT EXISTS platform_integrations.integration_endpoint (
    endpoint_id UUID PRIMARY KEY,
    tenant_id UUID NOT NULL,
    store_id UUID NOT NULL,
    integration_type VARCHAR(32) NOT NULL,
    provider VARCHAR(64) NOT NULL,
    code VARCHAR(128) NOT NULL,
    environment VARCHAR(32) NOT NULL,
    status VARCHAR(16) NOT NULL DEFAULT 'ACTIVE',
    configuration_ref VARCHAR(512) NOT NULL,
    endpoint_uri VARCHAR(1024),
    capabilities JSONB NOT NULL DEFAULT '{}'::jsonb,
    supplemental_configuration JSONB NOT NULL DEFAULT '{}'::jsonb,
    timeout_ms INTEGER NOT NULL DEFAULT 10000,
    max_attempts INTEGER NOT NULL DEFAULT 3,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT ck_integration_endpoint_type
        CHECK (integration_type IN ('EMAIL', 'SHIPPING', 'MAPS', 'STORAGE', 'ADAPTER')),
    CONSTRAINT ck_integration_endpoint_status
        CHECK (status IN ('ACTIVE', 'DISABLED', 'RETIRED')),
    CONSTRAINT ck_integration_endpoint_timeout
        CHECK (timeout_ms BETWEEN 100 AND 120000),
    CONSTRAINT ck_integration_endpoint_attempts
        CHECK (max_attempts BETWEEN 1 AND 10)
);

CREATE TABLE IF NOT EXISTS platform_integrations.delivery_idempotency (
    operation_id UUID PRIMARY KEY,
    tenant_id UUID NOT NULL,
    store_id UUID NOT NULL,
    operation_type VARCHAR(32) NOT NULL,
    idempotency_key VARCHAR(256) NOT NULL,
    request_hash VARCHAR(128) NOT NULL,
    item_count INTEGER NOT NULL DEFAULT 1,
    status VARCHAR(20) NOT NULL DEFAULT 'RECEIVED',
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT uq_delivery_idempotency_key
        UNIQUE (tenant_id, idempotency_key),
    CONSTRAINT ck_delivery_idempotency_type
        CHECK (operation_type IN ('EMAIL', 'STORAGE_UPLOAD', 'STORAGE_BATCH_UPLOAD')),
    CONSTRAINT ck_delivery_idempotency_items
        CHECK (item_count > 0),
    CONSTRAINT ck_delivery_idempotency_status
        CHECK (status IN ('RECEIVED', 'QUEUED', 'IN_PROGRESS', 'SUCCEEDED', 'FAILED', 'DEAD_LETTERED'))
);

CREATE TABLE IF NOT EXISTS platform_integrations.email_message (
    message_id UUID PRIMARY KEY,
    operation_id UUID NOT NULL,
    endpoint_id UUID NOT NULL,
    tenant_id UUID NOT NULL,
    store_id UUID NOT NULL,
    idempotency_key VARCHAR(256) NOT NULL,
    template_key VARCHAR(256) NOT NULL,
    locale VARCHAR(16) NOT NULL,
    recipient_email VARCHAR(320) NOT NULL,
    sender_email VARCHAR(320) NOT NULL,
    sender_name VARCHAR(256),
    subject VARCHAR(998) NOT NULL,
    token_payload JSONB NOT NULL DEFAULT '{}'::jsonb,
    status VARCHAR(20) NOT NULL DEFAULT 'QUEUED',
    order_reference VARCHAR(128),
    queued_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    sent_at TIMESTAMPTZ,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT fk_email_message_operation
        FOREIGN KEY (operation_id)
        REFERENCES platform_integrations.delivery_idempotency(operation_id),
    CONSTRAINT fk_email_message_endpoint
        FOREIGN KEY (endpoint_id)
        REFERENCES platform_integrations.integration_endpoint(endpoint_id),
    CONSTRAINT ck_email_message_status
        CHECK (status IN ('QUEUED', 'RENDERED', 'SUCCEEDED', 'FAILED', 'DEAD_LETTERED')),
    CONSTRAINT ck_email_message_recipient
        CHECK (recipient_email ~* '^[^[:space:]@]+@[^[:space:]@]+[.][^[:space:]@]+$'),
    CONSTRAINT uq_email_message_operation
        UNIQUE (operation_id)
);

CREATE TABLE IF NOT EXISTS platform_integrations.outbox_event (
    event_id UUID PRIMARY KEY,
    operation_id UUID NOT NULL,
    tenant_id UUID NOT NULL,
    store_id UUID NOT NULL,
    event_type VARCHAR(128) NOT NULL,
    aggregate_type VARCHAR(64) NOT NULL,
    aggregate_id UUID NOT NULL,
    payload JSONB NOT NULL,
    status VARCHAR(16) NOT NULL DEFAULT 'PENDING',
    available_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    published_at TIMESTAMPTZ,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT fk_outbox_event_operation
        FOREIGN KEY (operation_id)
        REFERENCES platform_integrations.delivery_idempotency(operation_id),
    CONSTRAINT ck_outbox_event_status
        CHECK (status IN ('PENDING', 'PUBLISHED', 'FAILED')),
    CONSTRAINT ck_outbox_event_published_at
        CHECK (status <> 'PUBLISHED' OR published_at IS NOT NULL),
    CONSTRAINT uq_outbox_event_operation_type
        UNIQUE (operation_id, event_type)
);

CREATE TABLE IF NOT EXISTS platform_integrations.delivery_attempt (
    attempt_id UUID PRIMARY KEY,
    operation_id UUID NOT NULL,
    endpoint_id UUID NOT NULL,
    message_id UUID,
    tenant_id UUID NOT NULL,
    store_id UUID NOT NULL,
    operation_item_key VARCHAR(512) NOT NULL,
    attempt_number INTEGER NOT NULL,
    status VARCHAR(20) NOT NULL DEFAULT 'PENDING',
    provider_request_ref VARCHAR(256),
    provider_outcome_code VARCHAR(128),
    provider_error_code VARCHAR(128),
    provider_error_message TEXT,
    request_payload JSONB NOT NULL DEFAULT '{}'::jsonb,
    response_summary JSONB,
    next_attempt_at TIMESTAMPTZ,
    attempted_at TIMESTAMPTZ,
    completed_at TIMESTAMPTZ,
    replay_of_attempt_id UUID,
    outbox_event_id UUID,
    dead_lettered_at TIMESTAMPTZ,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT fk_delivery_attempt_operation
        FOREIGN KEY (operation_id)
        REFERENCES platform_integrations.delivery_idempotency(operation_id),
    CONSTRAINT fk_delivery_attempt_endpoint
        FOREIGN KEY (endpoint_id)
        REFERENCES platform_integrations.integration_endpoint(endpoint_id),
    CONSTRAINT fk_delivery_attempt_message
        FOREIGN KEY (message_id)
        REFERENCES platform_integrations.email_message(message_id),
    CONSTRAINT fk_delivery_attempt_replay
        FOREIGN KEY (replay_of_attempt_id)
        REFERENCES platform_integrations.delivery_attempt(attempt_id),
    CONSTRAINT fk_delivery_attempt_outbox
        FOREIGN KEY (outbox_event_id)
        REFERENCES platform_integrations.outbox_event(event_id),
    CONSTRAINT ck_delivery_attempt_number
        CHECK (attempt_number BETWEEN 1 AND 10),
    CONSTRAINT ck_delivery_attempt_status
        CHECK (status IN ('PENDING', 'STARTED', 'SUCCEEDED', 'FAILED', 'DEAD_LETTERED')),
    CONSTRAINT ck_delivery_attempt_dead_letter_error
        CHECK (status <> 'DEAD_LETTERED'
            OR provider_error_code IS NOT NULL
            OR provider_error_message IS NOT NULL),
    CONSTRAINT uq_delivery_attempt_item_number
        UNIQUE (operation_id, operation_item_key, attempt_number)
);

COMMENT ON TABLE platform_integrations.integration_endpoint IS
    'Target projection of an adapter endpoint; configuration secrets remain owned by MS-11.';
COMMENT ON COLUMN platform_integrations.integration_endpoint.endpoint_id IS
    'BR-INT-MS12-002: stable endpoint projection identity.';
COMMENT ON COLUMN platform_integrations.integration_endpoint.tenant_id IS
    'Audit/multi-tenancy standard.';
COMMENT ON COLUMN platform_integrations.integration_endpoint.store_id IS
    'Audit/store isolation standard.';
COMMENT ON COLUMN platform_integrations.integration_endpoint.integration_type IS
    'BR-INT-MS12-001: category-scoped adapter projection.';
COMMENT ON COLUMN platform_integrations.integration_endpoint.provider IS
    'BR-INT-MS12-003 and BR-INT-MS12-018: selected provider family.';
COMMENT ON COLUMN platform_integrations.integration_endpoint.code IS
    'Maps to IntegrationModule.code; BR-INT-MS12-002.';
COMMENT ON COLUMN platform_integrations.integration_endpoint.environment IS
    'BR-INT-MS12-003: environment-specific projection.';
COMMENT ON COLUMN platform_integrations.integration_endpoint.status IS
    'BR-INT-MS12-002: active replacement lifecycle.';
COMMENT ON COLUMN platform_integrations.integration_endpoint.configuration_ref IS
    'BR-INT-MS12-003: opaque MS-11 configuration reference.';
COMMENT ON COLUMN platform_integrations.integration_endpoint.endpoint_uri IS
    'BR-INT-MS12-003: resolved non-secret provider URI.';
COMMENT ON COLUMN platform_integrations.integration_endpoint.capabilities IS
    'BR-INT-MS12-020: provider capability declaration.';
COMMENT ON COLUMN platform_integrations.integration_endpoint.supplemental_configuration IS
    'BR-INT-MS12-004: independent supplemental settings.';
COMMENT ON COLUMN platform_integrations.integration_endpoint.timeout_ms IS
    'BR-INT-MS12-022: provider timeout policy.';
COMMENT ON COLUMN platform_integrations.integration_endpoint.max_attempts IS
    'BR-INT-MS12-022: bounded delivery attempt policy.';
COMMENT ON COLUMN platform_integrations.integration_endpoint.created_at IS
    'Audit/multi-tenancy standard.';
COMMENT ON COLUMN platform_integrations.integration_endpoint.updated_at IS
    'Audit/multi-tenancy standard.';

COMMENT ON TABLE platform_integrations.delivery_idempotency IS
    'Target operation identity linking a caller key and immutable request hash to attempts.';
COMMENT ON COLUMN platform_integrations.delivery_idempotency.operation_id IS
    'BR-INT-MS12-021: durable operation identity.';
COMMENT ON COLUMN platform_integrations.delivery_idempotency.tenant_id IS
    'Audit/multi-tenancy standard.';
COMMENT ON COLUMN platform_integrations.delivery_idempotency.store_id IS
    'Audit/store isolation standard.';
COMMENT ON COLUMN platform_integrations.delivery_idempotency.operation_type IS
    'BR-INT-MS12-021: email, single upload, or batch upload operation kind.';
COMMENT ON COLUMN platform_integrations.delivery_idempotency.idempotency_key IS
    'BR-INT-MS12-021: caller-provided logical operation key.';
COMMENT ON COLUMN platform_integrations.delivery_idempotency.request_hash IS
    'BR-INT-MS12-021 and INV-INT-002: immutable request identity.';
COMMENT ON COLUMN platform_integrations.delivery_idempotency.item_count IS
    'BR-INT-MS12-021: number of durable operation items, including batch items.';
COMMENT ON COLUMN platform_integrations.delivery_idempotency.status IS
    'BR-INT-MS12-022 and BR-INT-MS12-023: operation lifecycle.';
COMMENT ON COLUMN platform_integrations.delivery_idempotency.created_at IS
    'Audit/multi-tenancy standard.';
COMMENT ON COLUMN platform_integrations.delivery_idempotency.updated_at IS
    'Audit/multi-tenancy standard.';

COMMENT ON TABLE platform_integrations.email_message IS
    'Target durable email projection; no legacy email-message table was found.';
COMMENT ON COLUMN platform_integrations.email_message.message_id IS
    'BR-INT-MS12-014: durable message identity.';
COMMENT ON COLUMN platform_integrations.email_message.operation_id IS
    'BR-INT-MS12-021: operation association.';
COMMENT ON COLUMN platform_integrations.email_message.endpoint_id IS
    'BR-INT-MS12-014: selected sender endpoint.';
COMMENT ON COLUMN platform_integrations.email_message.tenant_id IS
    'Audit/multi-tenancy standard.';
COMMENT ON COLUMN platform_integrations.email_message.store_id IS
    'Audit/store isolation standard.';
COMMENT ON COLUMN platform_integrations.email_message.idempotency_key IS
    'BR-INT-MS12-021: caller operation key.';
COMMENT ON COLUMN platform_integrations.email_message.template_key IS
    'BR-INT-MS12-015 through BR-INT-MS12-017: selected template.';
COMMENT ON COLUMN platform_integrations.email_message.locale IS
    'BR-INT-MS12-016 and BR-INT-MS12-017: rendering locale.';
COMMENT ON COLUMN platform_integrations.email_message.recipient_email IS
    'BR-INT-MS12-016 and BR-INT-MS12-017: resolved recipient.';
COMMENT ON COLUMN platform_integrations.email_message.sender_email IS
    'BR-INT-MS12-014 and BR-INT-MS12-015: sender address.';
COMMENT ON COLUMN platform_integrations.email_message.sender_name IS
    'BR-INT-MS12-015: sender display name.';
COMMENT ON COLUMN platform_integrations.email_message.subject IS
    'BR-INT-MS12-015 through BR-INT-MS12-017: message subject.';
COMMENT ON COLUMN platform_integrations.email_message.token_payload IS
    'BR-INT-MS12-015 through BR-INT-MS12-017: template payload.';
COMMENT ON COLUMN platform_integrations.email_message.status IS
    'BR-INT-MS12-022 and BR-INT-MS12-023: email delivery lifecycle.';
COMMENT ON COLUMN platform_integrations.email_message.order_reference IS
    'BR-INT-MS12-016: opaque MS-05 order reference.';
COMMENT ON COLUMN platform_integrations.email_message.queued_at IS
    'BR-INT-MS12-023: queue timing.';
COMMENT ON COLUMN platform_integrations.email_message.sent_at IS
    'BR-INT-MS12-022: provider success timing.';
COMMENT ON COLUMN platform_integrations.email_message.created_at IS
    'Audit/multi-tenancy standard.';
COMMENT ON COLUMN platform_integrations.email_message.updated_at IS
    'Audit/multi-tenancy standard.';

COMMENT ON TABLE platform_integrations.outbox_event IS
    'Target durable post-commit event publication record.';
COMMENT ON COLUMN platform_integrations.outbox_event.event_id IS
    'BR-INT-MS12-023: publication identity.';
COMMENT ON COLUMN platform_integrations.outbox_event.operation_id IS
    'BR-INT-MS12-023: operation association.';
COMMENT ON COLUMN platform_integrations.outbox_event.tenant_id IS
    'Audit/multi-tenancy standard.';
COMMENT ON COLUMN platform_integrations.outbox_event.store_id IS
    'Audit/store isolation standard.';
COMMENT ON COLUMN platform_integrations.outbox_event.event_type IS
    'BR-INT-MS12-023: queued or dead-letter event type.';
COMMENT ON COLUMN platform_integrations.outbox_event.aggregate_type IS
    'BR-INT-MS12-023: target aggregate category.';
COMMENT ON COLUMN platform_integrations.outbox_event.aggregate_id IS
    'BR-INT-MS12-023: target aggregate identity.';
COMMENT ON COLUMN platform_integrations.outbox_event.payload IS
    'BR-INT-MS12-023: redacted event payload.';
COMMENT ON COLUMN platform_integrations.outbox_event.status IS
    'BR-INT-MS12-023: publication lifecycle.';
COMMENT ON COLUMN platform_integrations.outbox_event.available_at IS
    'BR-INT-MS12-023: next publication time.';
COMMENT ON COLUMN platform_integrations.outbox_event.published_at IS
    'BR-INT-MS12-023: successful publication time.';
COMMENT ON COLUMN platform_integrations.outbox_event.created_at IS
    'Audit/multi-tenancy standard.';
COMMENT ON COLUMN platform_integrations.outbox_event.updated_at IS
    'Audit/multi-tenancy standard.';

COMMENT ON TABLE platform_integrations.delivery_attempt IS
    'Target durable provider attempt, retry, replay, and dead-letter record.';
COMMENT ON COLUMN platform_integrations.delivery_attempt.attempt_id IS
    'BR-INT-MS12-022: attempt identity.';
COMMENT ON COLUMN platform_integrations.delivery_attempt.operation_id IS
    'BR-INT-MS12-021: durable operation association.';
COMMENT ON COLUMN platform_integrations.delivery_attempt.endpoint_id IS
    'BR-INT-MS12-022: endpoint invoked.';
COMMENT ON COLUMN platform_integrations.delivery_attempt.message_id IS
    'BR-INT-MS12-023: optional email association.';
COMMENT ON COLUMN platform_integrations.delivery_attempt.tenant_id IS
    'Audit/multi-tenancy standard.';
COMMENT ON COLUMN platform_integrations.delivery_attempt.store_id IS
    'Audit/store isolation standard.';
COMMENT ON COLUMN platform_integrations.delivery_attempt.operation_item_key IS
    'BR-INT-MS12-021: file name or logical item within an operation.';
COMMENT ON COLUMN platform_integrations.delivery_attempt.attempt_number IS
    'BR-INT-MS12-022: bounded retry sequence.';
COMMENT ON COLUMN platform_integrations.delivery_attempt.status IS
    'BR-INT-MS12-022 and BR-INT-MS12-023: attempt lifecycle.';
COMMENT ON COLUMN platform_integrations.delivery_attempt.provider_request_ref IS
    'BR-INT-MS12-009, BR-INT-MS12-011, and BR-INT-MS12-015: provider correlation.';
COMMENT ON COLUMN platform_integrations.delivery_attempt.provider_outcome_code IS
    'BR-INT-MS12-009, BR-INT-MS12-011, and BR-INT-MS12-015: normalized success outcome.';
COMMENT ON COLUMN platform_integrations.delivery_attempt.provider_error_code IS
    'BR-INT-MS12-009, BR-INT-MS12-011, and BR-INT-MS12-022: normalized failure code.';
COMMENT ON COLUMN platform_integrations.delivery_attempt.provider_error_message IS
    'BR-INT-MS12-009, BR-INT-MS12-011, and BR-INT-MS12-022: provider failure detail.';
COMMENT ON COLUMN platform_integrations.delivery_attempt.request_payload IS
    'BR-INT-MS12-022: replayable redacted request.';
COMMENT ON COLUMN platform_integrations.delivery_attempt.response_summary IS
    'BR-INT-MS12-009, BR-INT-MS12-011, and BR-INT-MS12-023: normalized response.';
COMMENT ON COLUMN platform_integrations.delivery_attempt.next_attempt_at IS
    'BR-INT-MS12-022: retry schedule.';
COMMENT ON COLUMN platform_integrations.delivery_attempt.attempted_at IS
    'Audit/delivery timing.';
COMMENT ON COLUMN platform_integrations.delivery_attempt.completed_at IS
    'Audit/delivery timing.';
COMMENT ON COLUMN platform_integrations.delivery_attempt.replay_of_attempt_id IS
    'BR-INT-MS12-023: replay lineage.';
COMMENT ON COLUMN platform_integrations.delivery_attempt.outbox_event_id IS
    'BR-INT-MS12-023: publication association.';
COMMENT ON COLUMN platform_integrations.delivery_attempt.dead_lettered_at IS
    'BR-INT-MS12-023: terminal dead-letter timing.';
COMMENT ON COLUMN platform_integrations.delivery_attempt.created_at IS
    'Audit/multi-tenancy standard.';
COMMENT ON COLUMN platform_integrations.delivery_attempt.updated_at IS
    'Audit/multi-tenancy standard.';

CREATE INDEX IF NOT EXISTS ix_integration_endpoint_lookup
    ON platform_integrations.integration_endpoint
       (tenant_id, store_id, integration_type, environment, status);

CREATE UNIQUE INDEX IF NOT EXISTS uq_integration_endpoint_active
    ON platform_integrations.integration_endpoint
       (tenant_id, store_id, integration_type, code, environment)
    WHERE status = 'ACTIVE';

CREATE INDEX IF NOT EXISTS ix_delivery_operation_status
    ON platform_integrations.delivery_idempotency
       (tenant_id, store_id, status, created_at);

CREATE INDEX IF NOT EXISTS ix_email_message_status
    ON platform_integrations.email_message
       (tenant_id, store_id, status, queued_at);

CREATE INDEX IF NOT EXISTS ix_delivery_attempt_retry
    ON platform_integrations.delivery_attempt
       (tenant_id, status, next_attempt_at);

CREATE INDEX IF NOT EXISTS ix_delivery_attempt_operation
    ON platform_integrations.delivery_attempt
       (operation_id, operation_item_key, attempt_number);

CREATE INDEX IF NOT EXISTS ix_outbox_event_pending
    ON platform_integrations.outbox_event
       (status, available_at);

CREATE OR REPLACE FUNCTION platform_integrations.prevent_request_hash_change()
RETURNS trigger
LANGUAGE plpgsql
AS $$
BEGIN
    IF OLD.request_hash IS DISTINCT FROM NEW.request_hash THEN
        RAISE EXCEPTION 'request_hash is immutable for an idempotency key';
    END IF;
    RETURN NEW;
END;
$$;

CREATE OR REPLACE FUNCTION platform_integrations.validate_delivery_attempt()
RETURNS trigger
LANGUAGE plpgsql
AS $$
DECLARE
    operation_tenant UUID;
    operation_store UUID;
    endpoint_attempts INTEGER;
BEGIN
    SELECT tenant_id, store_id
      INTO operation_tenant, operation_store
      FROM platform_integrations.delivery_idempotency
     WHERE operation_id = NEW.operation_id;
    SELECT max_attempts
      INTO endpoint_attempts
      FROM platform_integrations.integration_endpoint
     WHERE endpoint_id = NEW.endpoint_id
       AND tenant_id = NEW.tenant_id
       AND store_id = NEW.store_id;
    IF operation_tenant IS NULL
       OR operation_tenant <> NEW.tenant_id
       OR operation_store <> NEW.store_id THEN
        RAISE EXCEPTION 'delivery attempt tenant or store does not match its operation';
    END IF;
    IF endpoint_attempts IS NULL OR NEW.attempt_number > endpoint_attempts THEN
        RAISE EXCEPTION 'attempt_number exceeds endpoint retry policy';
    END IF;
    RETURN NEW;
END;
$$;

CREATE OR REPLACE FUNCTION platform_integrations.prevent_successful_replay()
RETURNS trigger
LANGUAGE plpgsql
AS $$
DECLARE
    original_status VARCHAR(20);
BEGIN
    IF NEW.replay_of_attempt_id IS NOT NULL THEN
        SELECT status
          INTO original_status
          FROM platform_integrations.delivery_attempt
         WHERE attempt_id = NEW.replay_of_attempt_id;
        IF original_status IS NULL
           OR original_status NOT IN ('FAILED', 'DEAD_LETTERED') THEN
            RAISE EXCEPTION 'only failed or dead-lettered attempts can be replayed';
        END IF;
    END IF;
    RETURN NEW;
END;
$$;

CREATE OR REPLACE FUNCTION platform_integrations.enforce_dead_letter_error()
RETURNS trigger
LANGUAGE plpgsql
AS $$
BEGIN
    IF NEW.status = 'DEAD_LETTERED'
       AND NEW.provider_error_code IS NULL
       AND NEW.provider_error_message IS NULL THEN
        RAISE EXCEPTION 'dead-lettered attempts require provider error details';
    END IF;
    RETURN NEW;
END;
$$;

CREATE OR REPLACE FUNCTION platform_integrations.enforce_active_endpoint_uniqueness()
RETURNS trigger
LANGUAGE plpgsql
AS $$
BEGIN
    IF NEW.status = 'ACTIVE'
       AND EXISTS (
           SELECT 1
             FROM platform_integrations.integration_endpoint existing
            WHERE existing.tenant_id = NEW.tenant_id
              AND existing.store_id = NEW.store_id
              AND existing.integration_type = NEW.integration_type
              AND existing.code = NEW.code
              AND existing.environment = NEW.environment
              AND existing.status = 'ACTIVE'
              AND existing.endpoint_id <> NEW.endpoint_id
       ) THEN
        RAISE EXCEPTION 'only one active endpoint projection is allowed for a tenant/store/category/code/environment';
    END IF;
    RETURN NEW;
END;
$$;

CREATE OR REPLACE FUNCTION platform_integrations.validate_email_message_references()
RETURNS trigger
LANGUAGE plpgsql
AS $$
BEGIN
    IF NOT EXISTS (
        SELECT 1
          FROM platform_integrations.delivery_idempotency operation
         WHERE operation.operation_id = NEW.operation_id
    ) OR NOT EXISTS (
        SELECT 1
          FROM platform_integrations.integration_endpoint endpoint
         WHERE endpoint.endpoint_id = NEW.endpoint_id
    ) THEN
        RAISE EXCEPTION 'email message references must resolve to MS-12 operation and endpoint records';
    END IF;
    RETURN NEW;
END;
$$;

CREATE OR REPLACE FUNCTION platform_integrations.validate_outbox_published_timestamp()
RETURNS trigger
LANGUAGE plpgsql
AS $$
BEGIN
    IF NEW.status = 'PUBLISHED' AND NEW.published_at IS NULL THEN
        RAISE EXCEPTION 'published outbox events require published_at';
    END IF;
    RETURN NEW;
END;
$$;

CREATE TRIGGER trg_delivery_idempotency_hash_immutable
BEFORE UPDATE ON platform_integrations.delivery_idempotency
FOR EACH ROW
EXECUTE FUNCTION platform_integrations.prevent_request_hash_change();

CREATE TRIGGER trg_delivery_attempt_policy
BEFORE INSERT OR UPDATE ON platform_integrations.delivery_attempt
FOR EACH ROW
EXECUTE FUNCTION platform_integrations.validate_delivery_attempt();

CREATE TRIGGER trg_delivery_attempt_replay_guard
BEFORE INSERT OR UPDATE ON platform_integrations.delivery_attempt
FOR EACH ROW
EXECUTE FUNCTION platform_integrations.prevent_successful_replay();

CREATE TRIGGER trg_delivery_attempt_dead_letter_error
BEFORE INSERT OR UPDATE ON platform_integrations.delivery_attempt
FOR EACH ROW
EXECUTE FUNCTION platform_integrations.enforce_dead_letter_error();

CREATE TRIGGER trg_integration_endpoint_active_unique
BEFORE INSERT OR UPDATE ON platform_integrations.integration_endpoint
FOR EACH ROW
EXECUTE FUNCTION platform_integrations.enforce_active_endpoint_uniqueness();

CREATE TRIGGER trg_email_message_references
BEFORE INSERT OR UPDATE ON platform_integrations.email_message
FOR EACH ROW
EXECUTE FUNCTION platform_integrations.validate_email_message_references();

CREATE TRIGGER trg_outbox_event_published_timestamp
BEFORE INSERT OR UPDATE ON platform_integrations.outbox_event
FOR EACH ROW
EXECUTE FUNCTION platform_integrations.validate_outbox_published_timestamp();
```

## Database Logic Objects

The table uses the fixed positional order required by the importer:
`Name | Kind | Implements | Enforces Invariant | Migration Order | Binding | Placement`.

| Name | Kind | Implements | Enforces Invariant | Migration Order | Binding | Placement |
|---|---|---|---|---:|---|---|
| `prevent_request_hash_change` | function |  | `INV-INT-002` | 10 | Trigger function called by `trg_delivery_idempotency_hash_immutable`; no application call | mandatory-db-integrity |
| `validate_delivery_attempt` | function |  | `INV-INT-003` | 11 | Trigger function called by `trg_delivery_attempt_policy`; no application call | mandatory-db-integrity |
| `prevent_successful_replay` | function |  | `INV-INT-004` | 12 | Trigger function called by `trg_delivery_attempt_replay_guard`; no application call | mandatory-db-integrity |
| `enforce_dead_letter_error` | function |  | `INV-INT-006` | 13 | Trigger function called by `trg_delivery_attempt_dead_letter_error`; no application call | mandatory-db-integrity |
| `enforce_active_endpoint_uniqueness` | function |  | `INV-INT-001` | 14 | Trigger function called by `trg_integration_endpoint_active_unique`; no application call | mandatory-db-integrity |
| `validate_email_message_references` | function |  | `INV-INT-005` | 15 | Trigger function called by `trg_email_message_references`; no application call | mandatory-db-integrity |
| `validate_outbox_published_timestamp` | function |  | `INV-INT-007` | 16 | Trigger function called by `trg_outbox_event_published_timestamp`; no application call | mandatory-db-integrity |
| `trg_delivery_idempotency_hash_immutable` | trigger |  | `INV-INT-002` | 30 | Fires before update on `delivery_idempotency`; no application call | mandatory-db-integrity |
| `trg_delivery_attempt_policy` | trigger |  | `INV-INT-003` | 31 | Fires before insert/update on `delivery_attempt`; no application call | mandatory-db-integrity |
| `trg_delivery_attempt_replay_guard` | trigger |  | `INV-INT-004` | 32 | Fires before insert/update on `delivery_attempt`; no application call | mandatory-db-integrity |
| `trg_delivery_attempt_dead_letter_error` | trigger |  | `INV-INT-006` | 33 | Fires before insert/update on `delivery_attempt`; no application call | mandatory-db-integrity |
| `trg_integration_endpoint_active_unique` | trigger |  | `INV-INT-001` | 34 | Fires before insert/update on `integration_endpoint`; no application call | mandatory-db-integrity |
| `trg_email_message_references` | trigger |  | `INV-INT-005` | 35 | Fires before insert/update on `email_message`; no application call | mandatory-db-integrity |
| `trg_outbox_event_published_timestamp` | trigger |  | `INV-INT-007` | 36 | Fires before insert/update on `outbox_event`; no application call | mandatory-db-integrity |

## Entity State Model

### `integration_endpoint` lifecycle

- **States:** `ACTIVE` (initial), `DISABLED`, `RETIRED` (terminal)
- **Transitions:**

| From | To | Trigger | Guard |
|---|---|---|---|
| ACTIVE | DISABLED | BR-INT-MS12-002 | An administrator disables the endpoint |
| DISABLED | ACTIVE | BR-INT-MS12-002 | A valid endpoint projection is reactivated |
| ACTIVE | RETIRED | BR-INT-MS12-002 | A replacement retires the active projection |
| DISABLED | RETIRED | BR-INT-MS12-002 | The disabled endpoint is permanently withdrawn |

### `delivery_idempotency` lifecycle

- **States:** `RECEIVED` (initial), `QUEUED`, `IN_PROGRESS`, `SUCCEEDED` (terminal), `FAILED`, `DEAD_LETTERED` (terminal)
- **Transitions:**

| From | To | Trigger | Guard |
|---|---|---|---|
| RECEIVED | QUEUED | BR-INT-MS12-023 | Initial attempt and outbox event are committed |
| QUEUED | IN_PROGRESS | BR-INT-MS12-022 | A delivery worker claims an attempt |
| IN_PROGRESS | SUCCEEDED | BR-INT-MS12-022 | All operation items complete successfully |
| IN_PROGRESS | FAILED | BR-INT-MS12-022 | A retryable item failure remains within budget |
| IN_PROGRESS | DEAD_LETTERED | BR-INT-MS12-023 | A terminal failure or exhausted budget remains |
| FAILED | QUEUED | BR-INT-MS12-022 | Another attempt is allowed for a retryable item |

### `email_message` lifecycle

- **States:** `QUEUED` (initial), `RENDERED`, `SUCCEEDED` (terminal), `FAILED`, `DEAD_LETTERED` (terminal)
- **Transitions:**

| From | To | Trigger | Guard |
|---|---|---|---|
| QUEUED | RENDERED | BR-INT-MS12-015 | Template rendering succeeds |
| QUEUED | FAILED | BR-INT-MS12-015 | Template or sender preparation fails |
| RENDERED | SUCCEEDED | BR-INT-MS12-022 | Provider accepts the message |
| RENDERED | FAILED | BR-INT-MS12-022 | Provider failure is retryable |
| FAILED | DEAD_LETTERED | BR-INT-MS12-023 | Retry budget is exhausted |

### `delivery_attempt` lifecycle

- **States:** `PENDING` (initial), `STARTED`, `SUCCEEDED` (terminal), `FAILED` (terminal), `DEAD_LETTERED` (terminal)
- **Transitions:**

| From | To | Trigger | Guard |
|---|---|---|---|
| PENDING | STARTED | BR-INT-MS12-022 | Worker claims the attempt |
| STARTED | SUCCEEDED | BR-INT-MS12-022 | Provider returns success |
| STARTED | FAILED | BR-INT-MS12-022 | Provider failure is retryable and another attempt is allowed |
| STARTED | DEAD_LETTERED | BR-INT-MS12-023 | Failure is terminal or no attempts remain |

Retries create a new attempt. Terminal attempts are never reopened.

### `outbox_event` lifecycle

- **States:** `PENDING` (initial), `PUBLISHED` (terminal), `FAILED`
- **Transitions:**

| From | To | Trigger | Guard |
|---|---|---|---|
| PENDING | PUBLISHED | BR-INT-MS12-023 | Broker accepts the event |
| PENDING | FAILED | BR-INT-MS12-023 | Publication fails after its retry policy |
| FAILED | PENDING | BR-INT-MS12-023 | Worker schedules another publication attempt |

## Data Invariants

| Invariant ID | Statement | Entity | Kind | Tier |
|---|---|---|---|---|
| INV-INT-001 | A tenant/store/category/code/environment has at most one active endpoint projection. | integration_endpoint | uniqueness | db |
| INV-INT-002 | An idempotency key has one immutable request hash for its tenant. | delivery_idempotency | cross-row integrity | both |
| INV-INT-003 | An attempt belongs to the same tenant/store as its operation and does not exceed endpoint `max_attempts`. | delivery_attempt | cross-entity integrity | both |
| INV-INT-004 | A replay attempt may reference only a failed or dead-lettered original attempt. | delivery_attempt | lifecycle integrity | both |
| INV-INT-005 | An email message references an endpoint and operation in the MS-12 schema. | email_message | referential integrity | db |
| INV-INT-006 | A dead-lettered attempt retains a provider error code or message. | delivery_attempt | cross-field integrity | both |
| INV-INT-007 | A published outbox event has a publication timestamp. | outbox_event | cross-field integrity | db |

## Boundary and Ownership Decisions

- `configuration_ref` is an opaque MS-11 reference; MS-12 never stores merchant credentials.
- `order_reference` is an opaque MS-05 reference; MS-12 never changes order state.
- Carrier requests return normalized options to MS-09; quote policy and persistence remain there.
- Storage requests operate on provider keys; product/media metadata remains with MS-02.
- `delivery_idempotency`, `email_message`, `delivery_attempt`, and `outbox_event` are target-only reliability tables justified by asynchronous provider delivery and replay.
