using Npgsql;

namespace Shopizer.Payments.Data;

public sealed class SchemaInitializer(NpgsqlDataSource dataSource, ILogger<SchemaInitializer> logger)
{
    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await EnsurePgCryptoAsync(connection, cancellationToken);
        await using (var schema = new NpgsqlCommand(SchemaSql, connection))
            await schema.ExecuteNonQueryAsync(cancellationToken);
        await using (var migration = new NpgsqlCommand(MigrationSql, connection))
            await migration.ExecuteNonQueryAsync(cancellationToken);
        logger.LogInformation("Payments PostgreSQL schema is ready.");
    }

    private static async Task EnsurePgCryptoAsync(NpgsqlConnection connection, CancellationToken cancellationToken)
    {
        try
        {
            await using var command = new NpgsqlCommand("CREATE EXTENSION IF NOT EXISTS pgcrypto", connection);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation && ex.ConstraintName == "pg_extension_name_index")
        {
        }
    }

    private const string SchemaSql = """
        CREATE SCHEMA IF NOT EXISTS payments;

        CREATE TABLE IF NOT EXISTS payments.payment_intent (
          payment_intent_id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
          tenant_id varchar(100) NOT NULL, store_id varchar(100) NOT NULL,
          checkout_session_id varchar(100) NOT NULL, order_id varchar(100),
          provider_code varchar(64) NOT NULL, provider_config_version bigint NOT NULL,
          amount numeric(19,4) NOT NULL CHECK (amount > 0), currency_code char(3) NOT NULL,
          status varchar(32) NOT NULL, authorized_amount numeric(19,4) NOT NULL DEFAULT 0 CHECK (authorized_amount >= 0),
          captured_amount numeric(19,4) NOT NULL DEFAULT 0 CHECK (captured_amount >= 0),
          client_secret_reference varchar(255), created_at timestamptz NOT NULL DEFAULT now(),
          updated_at timestamptz NOT NULL DEFAULT now(), created_by varchar(100), correlation_id varchar(100),
          CONSTRAINT payment_intent_currency_ck CHECK (currency_code ~ '^[A-Z]{3}$'),
          CONSTRAINT payment_intent_status_ck CHECK (status IN
            ('Created','RequiresAction','Authorized','CapturePending','Captured','PartiallyRefunded',
             'Refunded','Failed','Cancelled','Expired','PendingManualSettlement','ReconciliationRequired'))
        );

        CREATE TABLE IF NOT EXISTS payments.payment_operation (
          payment_operation_id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
          payment_intent_id uuid NOT NULL REFERENCES payments.payment_intent(payment_intent_id),
          tenant_id varchar(100) NOT NULL, store_id varchar(100) NOT NULL,
          operation_type varchar(24) NOT NULL, status varchar(32) NOT NULL,
          requested_amount numeric(19,4) NOT NULL CHECK (requested_amount > 0),
          currency_code char(3) NOT NULL, idempotency_key varchar(255) NOT NULL,
          request_fingerprint char(64) NOT NULL, provider_attempt_id uuid,
          provider_reference varchar(255), failure_code varchar(80), failure_message varchar(500),
          created_at timestamptz NOT NULL DEFAULT now(), completed_at timestamptz, correlation_id varchar(100),
          CONSTRAINT payment_operation_type_ck CHECK (operation_type IN ('Initialize','Authorize','Capture','Refund')),
          CONSTRAINT payment_operation_status_ck CHECK (status IN ('Requested','InProgress','Succeeded','Failed','ReconciliationRequired')),
          CONSTRAINT payment_operation_currency_ck CHECK (currency_code ~ '^[A-Z]{3}$')
        );
        CREATE UNIQUE INDEX IF NOT EXISTS payment_operation_idempotency_uq
          ON payments.payment_operation(tenant_id, store_id, payment_intent_id, operation_type, idempotency_key);

        CREATE TABLE IF NOT EXISTS payments.payment_transaction (
          payment_transaction_id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
          payment_intent_id uuid NOT NULL REFERENCES payments.payment_intent(payment_intent_id),
          payment_operation_id uuid REFERENCES payments.payment_operation(payment_operation_id),
          tenant_id varchar(100) NOT NULL, store_id varchar(100) NOT NULL,
          operation_type varchar(24) NOT NULL, status varchar(32) NOT NULL,
          amount numeric(19,4) NOT NULL CHECK (amount > 0), currency_code char(3) NOT NULL,
          provider_code varchar(64) NOT NULL, provider_reference varchar(255), provider_status varchar(100),
          provider_correlation_id varchar(255), provider_details jsonb NOT NULL DEFAULT '{}'::jsonb,
          occurred_at timestamptz NOT NULL DEFAULT now(), sequence_no bigint NOT NULL,
          created_at timestamptz NOT NULL DEFAULT now(), correlation_id varchar(100),
          CONSTRAINT payment_transaction_type_ck CHECK (operation_type IN ('Initialize','Authorize','Capture','Refund')),
          CONSTRAINT payment_transaction_status_ck CHECK (status IN ('Succeeded','Failed','Pending','ReconciliationRequired')),
          CONSTRAINT payment_transaction_currency_ck CHECK (currency_code ~ '^[A-Z]{3}$'),
          CONSTRAINT payment_transaction_sequence_uq UNIQUE(payment_intent_id, sequence_no)
        );

        CREATE TABLE IF NOT EXISTS payments.payment_refund (
          payment_refund_id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
          payment_intent_id uuid NOT NULL REFERENCES payments.payment_intent(payment_intent_id),
          payment_operation_id uuid NOT NULL REFERENCES payments.payment_operation(payment_operation_id),
          tenant_id varchar(100) NOT NULL, store_id varchar(100) NOT NULL,
          amount numeric(19,4) NOT NULL CHECK (amount > 0), currency_code char(3) NOT NULL,
          status varchar(24) NOT NULL, provider_reference varchar(255),
          requested_at timestamptz NOT NULL DEFAULT now(), completed_at timestamptz, correlation_id varchar(100),
          CONSTRAINT payment_refund_status_ck CHECK (status IN ('Reserved','Succeeded','Failed','Released')),
          CONSTRAINT payment_refund_currency_ck CHECK (currency_code ~ '^[A-Z]{3}$')
        );

        CREATE TABLE IF NOT EXISTS payments.payment_provider_reference (
          payment_provider_reference_id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
          payment_intent_id uuid NOT NULL REFERENCES payments.payment_intent(payment_intent_id),
          payment_transaction_id uuid REFERENCES payments.payment_transaction(payment_transaction_id),
          tenant_id varchar(100) NOT NULL, store_id varchar(100) NOT NULL,
          provider_code varchar(64) NOT NULL, reference_type varchar(32) NOT NULL,
          provider_reference varchar(255) NOT NULL, is_current boolean NOT NULL DEFAULT true,
          created_at timestamptz NOT NULL DEFAULT now(), correlation_id varchar(100)
        );

        CREATE TABLE IF NOT EXISTS payments.payment_callback (
          payment_callback_id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
          tenant_id varchar(100), store_id varchar(100), provider_code varchar(64) NOT NULL,
          provider_event_id varchar(255), provider_reference varchar(255), payment_intent_id uuid REFERENCES payments.payment_intent(payment_intent_id),
          verification_status varchar(24) NOT NULL, processing_status varchar(24) NOT NULL,
          payload_hash char(64) NOT NULL, protected_payload jsonb, received_at timestamptz NOT NULL DEFAULT now(),
          processed_at timestamptz, correlation_id varchar(100),
          CONSTRAINT callback_verification_ck CHECK (verification_status IN ('Unverified','Verified','Rejected','Duplicate')),
          CONSTRAINT callback_processing_ck CHECK (processing_status IN ('Received','Applied','Ignored','Failed'))
        );
        CREATE UNIQUE INDEX IF NOT EXISTS payment_callback_provider_event_uq
          ON payments.payment_callback(provider_code, provider_event_id) WHERE provider_event_id IS NOT NULL;

        CREATE TABLE IF NOT EXISTS payments.payment_idempotency (
          payment_idempotency_id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
          tenant_id varchar(100) NOT NULL, store_id varchar(100) NOT NULL, payment_intent_id uuid,
          operation_type varchar(24) NOT NULL, idempotency_key varchar(255) NOT NULL,
          request_fingerprint char(64) NOT NULL, payment_operation_id uuid REFERENCES payments.payment_operation(payment_operation_id),
          replay_status varchar(24) NOT NULL, response_snapshot jsonb, created_at timestamptz NOT NULL DEFAULT now(),
          expires_at timestamptz NOT NULL,
          CONSTRAINT payment_idempotency_scope_uq UNIQUE(tenant_id,store_id,payment_intent_id,operation_type,idempotency_key)
        );

        CREATE TABLE IF NOT EXISTS payments.payment_outbox (
          payment_outbox_id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
          tenant_id varchar(100) NOT NULL, store_id varchar(100) NOT NULL,
          aggregate_type varchar(40) NOT NULL, aggregate_id uuid NOT NULL, event_type varchar(80) NOT NULL,
          event_version integer NOT NULL DEFAULT 1, payload jsonb NOT NULL,
          publish_status varchar(24) NOT NULL DEFAULT 'Pending', occurred_at timestamptz NOT NULL DEFAULT now(),
          published_at timestamptz, correlation_id varchar(100),
          CONSTRAINT payment_outbox_status_ck CHECK (publish_status IN ('Pending','Published','Failed'))
        );

        CREATE TABLE IF NOT EXISTS payments.payment_method_configuration (
          code varchar(64) NOT NULL, tenant_id varchar(100) NOT NULL, store_id varchar(100) NOT NULL,
          provider_code varchar(64) NOT NULL, active boolean NOT NULL, default_selected boolean NOT NULL,
          configurable boolean NOT NULL, environment varchar(16) NOT NULL, configuration_version bigint NOT NULL,
          secret_reference varchar(255) NOT NULL, public_configuration jsonb NOT NULL DEFAULT '{}'::jsonb,
          regions text[] NOT NULL DEFAULT ARRAY['*'], updated_at timestamptz NOT NULL DEFAULT now(),
          PRIMARY KEY(tenant_id, store_id, code)
        );
        INSERT INTO payments.payment_method_configuration
          (tenant_id,store_id,code,provider_code,active,default_selected,configurable,environment,configuration_version,secret_reference,regions)
        VALUES
          ('test-tenant-001','test-store-001','stripe','stripe',true,true,true,'Test',1,'development',ARRAY['*']),
          ('test-tenant-001','test-store-001','stripe3','stripe3',true,false,true,'Test',1,'development',ARRAY['*']),
          ('test-tenant-001','test-store-001','braintree','braintree',true,false,true,'Test',1,'development',ARRAY['*']),
          ('test-tenant-001','test-store-001','paypal-express-checkout','paypal-express-checkout',true,false,true,'Test',1,'development',ARRAY['*']),
          ('test-tenant-001','test-store-001','beanstream','beanstream',true,false,true,'Test',1,'development',ARRAY['*']),
          ('test-tenant-001','test-store-001','moneyorder','moneyorder',true,false,true,'Test',1,'development',ARRAY['*'])
        ON CONFLICT (tenant_id,store_id,code) DO NOTHING;

        CREATE INDEX IF NOT EXISTS payment_intent_store_status_idx ON payments.payment_intent(tenant_id,store_id,status,created_at);
        CREATE INDEX IF NOT EXISTS payment_transaction_intent_sequence_idx ON payments.payment_transaction(payment_intent_id,sequence_no,occurred_at);
        CREATE INDEX IF NOT EXISTS payment_refund_intent_status_idx ON payments.payment_refund(payment_intent_id,status);
        CREATE INDEX IF NOT EXISTS payment_outbox_pending_idx ON payments.payment_outbox(publish_status,occurred_at);
        """;

    private const string MigrationSql = """
        ALTER TABLE payments.payment_intent ADD COLUMN IF NOT EXISTS correlation_id varchar(100);
        ALTER TABLE payments.payment_operation ADD COLUMN IF NOT EXISTS correlation_id varchar(100);
        ALTER TABLE payments.payment_transaction ADD COLUMN IF NOT EXISTS correlation_id varchar(100);
        ALTER TABLE payments.payment_refund ADD COLUMN IF NOT EXISTS correlation_id varchar(100);
        ALTER TABLE payments.payment_callback ADD COLUMN IF NOT EXISTS protected_payload jsonb;
        ALTER TABLE payments.payment_outbox ADD COLUMN IF NOT EXISTS correlation_id varchar(100);

        CREATE OR REPLACE FUNCTION payments.enforce_refund_balance()
        RETURNS trigger LANGUAGE plpgsql AS $$
        DECLARE captured_total numeric(19,4); refunded_total numeric(19,4); reserved_total numeric(19,4);
        BEGIN
          SELECT captured_amount INTO captured_total FROM payments.payment_intent
           WHERE payment_intent_id = NEW.payment_intent_id FOR UPDATE;
          SELECT COALESCE(SUM(amount),0) INTO refunded_total FROM payments.payment_refund
           WHERE payment_intent_id = NEW.payment_intent_id AND status = 'Succeeded'
             AND payment_refund_id <> COALESCE(NEW.payment_refund_id, gen_random_uuid());
          SELECT COALESCE(SUM(amount),0) INTO reserved_total FROM payments.payment_refund
           WHERE payment_intent_id = NEW.payment_intent_id AND status = 'Reserved'
             AND payment_refund_id <> COALESCE(NEW.payment_refund_id, gen_random_uuid());
          IF NEW.status IN ('Reserved','Succeeded') AND refunded_total + reserved_total + NEW.amount > captured_total THEN
            RAISE EXCEPTION USING ERRCODE = '23514', MESSAGE = 'Refund amount exceeds captured remaining balance';
          END IF;
          RETURN NEW;
        END $$;
        DROP TRIGGER IF EXISTS payment_refund_balance_guard ON payments.payment_refund;
        CREATE TRIGGER payment_refund_balance_guard BEFORE INSERT OR UPDATE OF amount,status
          ON payments.payment_refund FOR EACH ROW EXECUTE FUNCTION payments.enforce_refund_balance();
        """;
}
