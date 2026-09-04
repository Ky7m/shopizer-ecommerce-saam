using Npgsql;

namespace Shopizer.PlatformIntegrations.Data;

public sealed class SchemaInitializer(NpgsqlDataSource dataSource, ILogger<SchemaInitializer> logger)
{
    public async Task InitializeAsync(CancellationToken ct)
    {
        await using var connection = await dataSource.OpenConnectionAsync(ct);
        await using var schema = new NpgsqlCommand(SchemaSql, connection);
        await schema.ExecuteNonQueryAsync(ct);
        await using var migration = new NpgsqlCommand(MigrationSql, connection);
        await migration.ExecuteNonQueryAsync(ct);
        logger.LogInformation("Platform integrations PostgreSQL schema is ready.");
    }

    private const string SchemaSql = """
        CREATE SCHEMA IF NOT EXISTS platform_integrations;
        CREATE TABLE IF NOT EXISTS platform_integrations.integration_endpoint (
          endpoint_id uuid PRIMARY KEY, tenant_id varchar(256) NOT NULL, store_id varchar(256) NOT NULL,
          integration_type varchar(32) NOT NULL, provider varchar(64) NOT NULL, code varchar(128) NOT NULL,
          environment varchar(32) NOT NULL, status varchar(16) NOT NULL DEFAULT 'ACTIVE',
          configuration_ref varchar(512) NOT NULL, endpoint_uri varchar(1024),
          capabilities jsonb NOT NULL DEFAULT '{}'::jsonb,
          supplemental_configuration jsonb NOT NULL DEFAULT '{}'::jsonb,
          timeout_ms integer NOT NULL DEFAULT 10000, max_attempts integer NOT NULL DEFAULT 3,
          created_at timestamptz NOT NULL DEFAULT current_timestamp,
          updated_at timestamptz NOT NULL DEFAULT current_timestamp,
          CONSTRAINT ck_endpoint_type CHECK (integration_type IN ('EMAIL','SHIPPING','MAPS','STORAGE','ADAPTER')),
          CONSTRAINT ck_endpoint_status CHECK (status IN ('ACTIVE','DISABLED','RETIRED')),
          CONSTRAINT ck_endpoint_timeout CHECK (timeout_ms BETWEEN 100 AND 120000),
          CONSTRAINT ck_endpoint_attempts CHECK (max_attempts BETWEEN 1 AND 10));
        CREATE TABLE IF NOT EXISTS platform_integrations.delivery_idempotency (
          operation_id uuid PRIMARY KEY, tenant_id varchar(256) NOT NULL, store_id varchar(256) NOT NULL,
          operation_type varchar(32) NOT NULL, idempotency_key varchar(256) NOT NULL,
          request_hash varchar(128) NOT NULL, item_count integer NOT NULL DEFAULT 1,
          status varchar(20) NOT NULL DEFAULT 'RECEIVED',
          created_at timestamptz NOT NULL DEFAULT current_timestamp,
          updated_at timestamptz NOT NULL DEFAULT current_timestamp,
          CONSTRAINT uq_delivery_key UNIQUE (tenant_id,idempotency_key),
          CONSTRAINT ck_delivery_type CHECK (operation_type IN ('EMAIL','STORAGE_UPLOAD','STORAGE_BATCH_UPLOAD')),
          CONSTRAINT ck_delivery_items CHECK (item_count > 0),
          CONSTRAINT ck_delivery_status CHECK (status IN ('RECEIVED','QUEUED','IN_PROGRESS','SUCCEEDED','FAILED','DEAD_LETTERED')));
        CREATE TABLE IF NOT EXISTS platform_integrations.email_message (
          message_id uuid PRIMARY KEY, operation_id uuid NOT NULL REFERENCES platform_integrations.delivery_idempotency(operation_id),
          endpoint_id uuid NOT NULL REFERENCES platform_integrations.integration_endpoint(endpoint_id),
          tenant_id varchar(256) NOT NULL, store_id varchar(256) NOT NULL,
          idempotency_key varchar(256) NOT NULL, template_key varchar(256) NOT NULL, locale varchar(16) NOT NULL,
          recipient_email varchar(320) NOT NULL, sender_email varchar(320) NOT NULL, sender_name varchar(256),
          subject varchar(998) NOT NULL, token_payload jsonb NOT NULL DEFAULT '{}'::jsonb,
          status varchar(20) NOT NULL DEFAULT 'QUEUED', order_reference varchar(128),
          queued_at timestamptz NOT NULL DEFAULT current_timestamp, sent_at timestamptz,
          created_at timestamptz NOT NULL DEFAULT current_timestamp, updated_at timestamptz NOT NULL DEFAULT current_timestamp,
          CONSTRAINT ck_email_status CHECK (status IN ('QUEUED','RENDERED','SUCCEEDED','FAILED','DEAD_LETTERED')),
          CONSTRAINT ck_email_recipient CHECK (recipient_email ~* '^[^[:space:]@]+@[^[:space:]@]+[.][^[:space:]@]+$'),
          CONSTRAINT uq_email_operation UNIQUE (operation_id));
        CREATE TABLE IF NOT EXISTS platform_integrations.outbox_event (
          event_id uuid PRIMARY KEY, operation_id uuid NOT NULL REFERENCES platform_integrations.delivery_idempotency(operation_id),
          tenant_id varchar(256) NOT NULL, store_id varchar(256) NOT NULL, event_type varchar(128) NOT NULL,
          aggregate_type varchar(64) NOT NULL, aggregate_id uuid NOT NULL, payload jsonb NOT NULL,
          status varchar(16) NOT NULL DEFAULT 'PENDING', available_at timestamptz NOT NULL DEFAULT current_timestamp,
          published_at timestamptz, created_at timestamptz NOT NULL DEFAULT current_timestamp,
          updated_at timestamptz NOT NULL DEFAULT current_timestamp,
          CONSTRAINT ck_outbox_status CHECK (status IN ('PENDING','PUBLISHED','FAILED')),
          CONSTRAINT ck_outbox_published CHECK (status <> 'PUBLISHED' OR published_at IS NOT NULL),
          CONSTRAINT uq_outbox_operation_type UNIQUE (operation_id,event_type));
        CREATE TABLE IF NOT EXISTS platform_integrations.delivery_attempt (
          attempt_id uuid PRIMARY KEY, operation_id uuid NOT NULL REFERENCES platform_integrations.delivery_idempotency(operation_id),
          endpoint_id uuid NOT NULL REFERENCES platform_integrations.integration_endpoint(endpoint_id),
          message_id uuid REFERENCES platform_integrations.email_message(message_id),
          tenant_id varchar(256) NOT NULL, store_id varchar(256) NOT NULL,
          operation_item_key varchar(512) NOT NULL, attempt_number integer NOT NULL,
          status varchar(20) NOT NULL DEFAULT 'PENDING', provider_request_ref varchar(256),
          provider_outcome_code varchar(128), provider_error_code varchar(128), provider_error_message text,
          request_payload jsonb NOT NULL DEFAULT '{}'::jsonb, response_summary jsonb,
          next_attempt_at timestamptz, attempted_at timestamptz, completed_at timestamptz,
          replay_of_attempt_id uuid REFERENCES platform_integrations.delivery_attempt(attempt_id),
          outbox_event_id uuid REFERENCES platform_integrations.outbox_event(event_id), dead_lettered_at timestamptz,
          created_at timestamptz NOT NULL DEFAULT current_timestamp, updated_at timestamptz NOT NULL DEFAULT current_timestamp,
          CONSTRAINT ck_attempt_number CHECK (attempt_number BETWEEN 1 AND 10),
          CONSTRAINT ck_attempt_status CHECK (status IN ('PENDING','STARTED','SUCCEEDED','FAILED','DEAD_LETTERED')),
          CONSTRAINT ck_dead_letter_error CHECK (status <> 'DEAD_LETTERED' OR provider_error_code IS NOT NULL OR provider_error_message IS NOT NULL),
          CONSTRAINT uq_attempt_item_number UNIQUE (operation_id,operation_item_key,attempt_number));
        CREATE INDEX IF NOT EXISTS ix_endpoint_lookup ON platform_integrations.integration_endpoint(tenant_id,store_id,integration_type,environment,status);
        CREATE UNIQUE INDEX IF NOT EXISTS uq_endpoint_active ON platform_integrations.integration_endpoint(tenant_id,store_id,integration_type,code,environment) WHERE status='ACTIVE';
        CREATE INDEX IF NOT EXISTS ix_delivery_status ON platform_integrations.delivery_idempotency(tenant_id,store_id,status,created_at);
        CREATE INDEX IF NOT EXISTS ix_attempt_operation ON platform_integrations.delivery_attempt(operation_id,operation_item_key,attempt_number);
        CREATE INDEX IF NOT EXISTS ix_outbox_pending ON platform_integrations.outbox_event(status,available_at);
        CREATE OR REPLACE FUNCTION platform_integrations.prevent_request_hash_change() RETURNS trigger LANGUAGE plpgsql AS $fn$
        BEGIN IF OLD.request_hash IS DISTINCT FROM NEW.request_hash THEN RAISE EXCEPTION 'request_hash is immutable for an idempotency key'; END IF; RETURN NEW; END $fn$;
        CREATE OR REPLACE FUNCTION platform_integrations.validate_delivery_attempt() RETURNS trigger LANGUAGE plpgsql AS $fn$
        DECLARE ot varchar(256); os varchar(256); max_attempts integer;
        BEGIN SELECT tenant_id,store_id INTO ot,os FROM platform_integrations.delivery_idempotency WHERE operation_id=NEW.operation_id;
          SELECT e.max_attempts INTO max_attempts FROM platform_integrations.integration_endpoint e WHERE e.endpoint_id=NEW.endpoint_id AND e.tenant_id=NEW.tenant_id AND e.store_id=NEW.store_id;
          IF ot IS NULL OR ot<>NEW.tenant_id OR os<>NEW.store_id OR max_attempts IS NULL OR NEW.attempt_number>max_attempts THEN RAISE EXCEPTION 'delivery attempt violates tenant or retry policy'; END IF; RETURN NEW; END $fn$;
        CREATE OR REPLACE FUNCTION platform_integrations.prevent_successful_replay() RETURNS trigger LANGUAGE plpgsql AS $fn$
        DECLARE original_status varchar(20);
        BEGIN IF NEW.replay_of_attempt_id IS NOT NULL THEN SELECT status INTO original_status FROM platform_integrations.delivery_attempt WHERE attempt_id=NEW.replay_of_attempt_id;
          IF original_status IS NULL OR original_status NOT IN ('FAILED','DEAD_LETTERED') THEN RAISE EXCEPTION 'only failed or dead-lettered attempts can be replayed'; END IF; END IF; RETURN NEW; END $fn$;
        CREATE OR REPLACE FUNCTION platform_integrations.enforce_dead_letter_error() RETURNS trigger LANGUAGE plpgsql AS $fn$
        BEGIN IF NEW.status='DEAD_LETTERED' AND NEW.provider_error_code IS NULL AND NEW.provider_error_message IS NULL THEN RAISE EXCEPTION 'dead-lettered attempts require provider error details'; END IF; RETURN NEW; END $fn$;
        CREATE OR REPLACE FUNCTION platform_integrations.validate_outbox_published_timestamp() RETURNS trigger LANGUAGE plpgsql AS $fn$
        BEGIN IF NEW.status='PUBLISHED' AND NEW.published_at IS NULL THEN RAISE EXCEPTION 'published outbox events require published_at'; END IF; RETURN NEW; END $fn$;
        DO $fn$ BEGIN
          IF NOT EXISTS (SELECT 1 FROM pg_trigger WHERE tgname='trg_delivery_hash_immutable') THEN
            CREATE TRIGGER trg_delivery_hash_immutable BEFORE UPDATE ON platform_integrations.delivery_idempotency FOR EACH ROW EXECUTE FUNCTION platform_integrations.prevent_request_hash_change();
          END IF;
          IF NOT EXISTS (SELECT 1 FROM pg_trigger WHERE tgname='trg_attempt_policy') THEN
            CREATE TRIGGER trg_attempt_policy BEFORE INSERT OR UPDATE ON platform_integrations.delivery_attempt FOR EACH ROW EXECUTE FUNCTION platform_integrations.validate_delivery_attempt();
          END IF;
          IF NOT EXISTS (SELECT 1 FROM pg_trigger WHERE tgname='trg_attempt_replay') THEN
            CREATE TRIGGER trg_attempt_replay BEFORE INSERT OR UPDATE ON platform_integrations.delivery_attempt FOR EACH ROW EXECUTE FUNCTION platform_integrations.prevent_successful_replay();
          END IF;
          IF NOT EXISTS (SELECT 1 FROM pg_trigger WHERE tgname='trg_attempt_dead_letter') THEN
            CREATE TRIGGER trg_attempt_dead_letter BEFORE INSERT OR UPDATE ON platform_integrations.delivery_attempt FOR EACH ROW EXECUTE FUNCTION platform_integrations.enforce_dead_letter_error();
          END IF;
          IF NOT EXISTS (SELECT 1 FROM pg_trigger WHERE tgname='trg_outbox_timestamp') THEN
            CREATE TRIGGER trg_outbox_timestamp BEFORE INSERT OR UPDATE ON platform_integrations.outbox_event FOR EACH ROW EXECUTE FUNCTION platform_integrations.validate_outbox_published_timestamp();
          END IF;
        END $fn$;
        """;

    private const string MigrationSql = """
        ALTER TABLE platform_integrations.integration_endpoint ADD COLUMN IF NOT EXISTS supplemental_configuration jsonb NOT NULL DEFAULT '{}'::jsonb;
        ALTER TABLE platform_integrations.delivery_idempotency ADD COLUMN IF NOT EXISTS completed_at timestamptz;
        ALTER TABLE platform_integrations.delivery_attempt ADD COLUMN IF NOT EXISTS request_payload jsonb NOT NULL DEFAULT '{}'::jsonb;
        ALTER TABLE platform_integrations.delivery_attempt ADD COLUMN IF NOT EXISTS response_summary jsonb;
        ALTER TABLE platform_integrations.delivery_attempt ADD COLUMN IF NOT EXISTS replay_of_attempt_id uuid;
        ALTER TABLE platform_integrations.delivery_attempt ADD COLUMN IF NOT EXISTS outbox_event_id uuid;
        """;
}
