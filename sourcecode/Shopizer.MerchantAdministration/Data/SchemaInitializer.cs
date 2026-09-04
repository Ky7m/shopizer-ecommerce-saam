using Npgsql;

namespace Shopizer.MerchantAdministration.Data;

public sealed class SchemaInitializer(NpgsqlDataSource dataSource, ILogger<SchemaInitializer> logger)
{
    public async Task InitializeAsync(CancellationToken ct)
    {
        await using var connection = await dataSource.OpenConnectionAsync(ct);
        await EnsurePgCryptoAsync(connection, ct);
        await using (var command = new NpgsqlCommand(SchemaSql, connection)) await command.ExecuteNonQueryAsync(ct);
        await using (var migration = new NpgsqlCommand(MigrationSql, connection)) await migration.ExecuteNonQueryAsync(ct);
        logger.LogInformation("Merchant administration PostgreSQL schema is ready.");
    }

    private static async Task EnsurePgCryptoAsync(NpgsqlConnection connection, CancellationToken ct)
    {
        try
        {
            await using var command = new NpgsqlCommand("CREATE EXTENSION IF NOT EXISTS pgcrypto", connection);
            await command.ExecuteNonQueryAsync(ct);
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation && ex.ConstraintName == "pg_extension_name_index")
        {
        }
    }

    private const string SchemaSql = """
        CREATE SCHEMA IF NOT EXISTS merchant_store;
        CREATE TABLE IF NOT EXISTS merchant_store.stores (
          id uuid PRIMARY KEY DEFAULT gen_random_uuid(), tenant_id varchar(100) NOT NULL, code varchar(100) NOT NULL,
          name varchar(150) NOT NULL, email_address varchar(320) NOT NULL, phone varchar(50) NOT NULL,
          street_address varchar(256), city varchar(100) NOT NULL, postal_code varchar(30) NOT NULL, country_code varchar(10) NOT NULL,
          state_province varchar(100), zone_code varchar(30), retailer boolean NOT NULL DEFAULT false,
          parent_store_id uuid REFERENCES merchant_store.stores(id) ON DELETE RESTRICT,
          default_language_code varchar(10) NOT NULL, currency_code varchar(10) NOT NULL,
          dimension_unit varchar(20) NOT NULL, weight_unit varchar(20) NOT NULL, template_code varchar(100), logo_uri varchar(1024),
          status varchar(20) NOT NULL DEFAULT 'Active' CHECK (status IN ('Active','Suspended','Deleted')),
          created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NOT NULL DEFAULT now(),
          CONSTRAINT uq_store_tenant_code UNIQUE (tenant_id, code), CONSTRAINT ck_store_not_self_parent CHECK (parent_store_id IS NULL OR parent_store_id <> id));
        CREATE INDEX IF NOT EXISTS ix_store_tenant_name ON merchant_store.stores (tenant_id, name);
        CREATE INDEX IF NOT EXISTS ix_store_parent ON merchant_store.stores (parent_store_id);
        CREATE INDEX IF NOT EXISTS ix_store_retailer ON merchant_store.stores (tenant_id, retailer);
        CREATE INDEX IF NOT EXISTS merchant_store_parent_status_idx ON merchant_store.stores (tenant_id, parent_store_id, status);
        CREATE UNIQUE INDEX IF NOT EXISTS uq_store_tenant_code_ci ON merchant_store.stores (tenant_id, lower(code));
        CREATE TABLE IF NOT EXISTS merchant_store.store_languages (
          store_id uuid NOT NULL REFERENCES merchant_store.stores(id) ON DELETE CASCADE, language_code varchar(10) NOT NULL,
          PRIMARY KEY (store_id, language_code));
        CREATE INDEX IF NOT EXISTS ix_store_languages_language ON merchant_store.store_languages (language_code);
        CREATE TABLE IF NOT EXISTS merchant_store.event_outbox (
          id uuid PRIMARY KEY, event_type varchar(150) NOT NULL, tenant_id varchar(100) NOT NULL, store_id varchar(100) NOT NULL,
          correlation_id varchar(255) NOT NULL, payload jsonb NOT NULL, occurred_at timestamptz NOT NULL, published_at timestamptz);
        CREATE TABLE IF NOT EXISTS merchant_store.store_signups (
          id uuid PRIMARY KEY, tenant_id varchar(100) NOT NULL, code varchar(100) NOT NULL, payload jsonb NOT NULL,
          token_hash varchar(128) NOT NULL UNIQUE, expires_at timestamptz NOT NULL, consumed_at timestamptz,
          created_at timestamptz NOT NULL DEFAULT now());
        CREATE INDEX IF NOT EXISTS ix_store_signups_lookup ON merchant_store.store_signups (tenant_id, code, expires_at, consumed_at);
        """;

    private const string MigrationSql = """
        ALTER TABLE merchant_store.stores ADD COLUMN IF NOT EXISTS template_code varchar(100);
        ALTER TABLE merchant_store.stores ADD COLUMN IF NOT EXISTS logo_uri varchar(1024);
        ALTER TABLE merchant_store.stores ADD COLUMN IF NOT EXISTS status varchar(20) NOT NULL DEFAULT 'Active';
        ALTER TABLE merchant_store.event_outbox ADD COLUMN IF NOT EXISTS published_at timestamptz;
        """;
}
