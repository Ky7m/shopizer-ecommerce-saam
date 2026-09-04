using Npgsql;

namespace Shopizer.Tax.Data;

public sealed class SchemaInitializer(NpgsqlDataSource dataSource, ILogger<SchemaInitializer> logger)
{
    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await EnsurePgCryptoAsync(connection, cancellationToken);

        await using (var schema = new NpgsqlCommand(SchemaSql, connection))
        {
            await schema.ExecuteNonQueryAsync(cancellationToken);
        }

        await using (var migration = new NpgsqlCommand(MigrationSql, connection))
        {
            await migration.ExecuteNonQueryAsync(cancellationToken);
        }

        logger.LogInformation("Tax PostgreSQL schema is ready.");
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
        CREATE SCHEMA IF NOT EXISTS tax_schema;

        CREATE TABLE IF NOT EXISTS tax_schema.tax_classes (
            id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
            tenant_id varchar(100) NOT NULL,
            store_id varchar(100) NOT NULL,
            code varchar(10) NOT NULL,
            title varchar(32) NOT NULL,
            created_at timestamptz NOT NULL DEFAULT now(),
            updated_at timestamptz NOT NULL DEFAULT now(),
            created_by uuid NULL,
            correlation_id varchar(255) NULL,
            CONSTRAINT tax_classes_code_not_blank CHECK (length(btrim(code)) BETWEEN 1 AND 10),
            CONSTRAINT tax_classes_title_not_blank CHECK (length(btrim(title)) BETWEEN 1 AND 32),
            CONSTRAINT tax_classes_tenant_store_code_uk UNIQUE (tenant_id, store_id, code)
        );
        CREATE INDEX IF NOT EXISTS tax_classes_store_idx
            ON tax_schema.tax_classes (tenant_id, store_id);

        CREATE TABLE IF NOT EXISTS tax_schema.tax_rates (
            id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
            tenant_id varchar(100) NOT NULL,
            store_id varchar(100) NOT NULL,
            tax_class_id uuid NOT NULL REFERENCES tax_schema.tax_classes (id) ON DELETE RESTRICT,
            code varchar(100) NOT NULL,
            rate_percent numeric(7,4) NOT NULL,
            priority integer NOT NULL DEFAULT 0,
            piggyback boolean NOT NULL DEFAULT false,
            country_code varchar(3) NOT NULL,
            zone_code varchar(100) NULL,
            state_province varchar(100) NULL,
            parent_rate_id uuid NULL REFERENCES tax_schema.tax_rates (id) ON DELETE CASCADE,
            created_at timestamptz NOT NULL DEFAULT now(),
            updated_at timestamptz NOT NULL DEFAULT now(),
            created_by uuid NULL,
            correlation_id varchar(255) NULL,
            CONSTRAINT tax_rates_code_not_blank CHECK (length(btrim(code)) BETWEEN 1 AND 100),
            CONSTRAINT tax_rates_percent_range CHECK (rate_percent >= 0 AND rate_percent <= 100),
            CONSTRAINT tax_rates_priority_nonnegative CHECK (priority >= 0),
            CONSTRAINT tax_rates_country_not_blank CHECK (length(btrim(country_code)) BETWEEN 2 AND 3),
            CONSTRAINT tax_rates_tenant_store_code_uk UNIQUE (tenant_id, store_id, code)
        );
        CREATE INDEX IF NOT EXISTS tax_rates_lookup_idx
            ON tax_schema.tax_rates (tenant_id, store_id, country_code, zone_code, state_province, tax_class_id, priority);
        CREATE INDEX IF NOT EXISTS tax_rates_class_idx
            ON tax_schema.tax_rates (tenant_id, store_id, tax_class_id);

        CREATE TABLE IF NOT EXISTS tax_schema.tax_rate_descriptions (
            id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
            tax_rate_id uuid NOT NULL REFERENCES tax_schema.tax_rates (id) ON DELETE CASCADE,
            language_code varchar(10) NOT NULL,
            name varchar(255) NOT NULL,
            title varchar(255) NULL,
            description text NULL,
            created_at timestamptz NOT NULL DEFAULT now(),
            updated_at timestamptz NOT NULL DEFAULT now(),
            created_by uuid NULL,
            correlation_id varchar(255) NULL,
            CONSTRAINT tax_rate_descriptions_name_not_blank CHECK (length(btrim(name)) > 0),
            CONSTRAINT tax_rate_descriptions_rate_language_uk UNIQUE (tax_rate_id, language_code)
        );
        CREATE INDEX IF NOT EXISTS tax_rate_descriptions_language_idx
            ON tax_schema.tax_rate_descriptions (language_code);

        CREATE TABLE IF NOT EXISTS tax_schema.tax_configurations (
            id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
            tenant_id varchar(100) NOT NULL,
            store_id varchar(100) NOT NULL,
            tax_basis varchar(32) NOT NULL DEFAULT 'ShippingAddress',
            collect_tax_if_different_province boolean NOT NULL DEFAULT true,
            different_country_behavior varchar(32) NOT NULL DEFAULT 'UseCustomerJurisdiction',
            created_at timestamptz NOT NULL DEFAULT now(),
            updated_at timestamptz NOT NULL DEFAULT now(),
            created_by uuid NULL,
            correlation_id varchar(255) NULL,
            CONSTRAINT tax_configurations_basis_ck
                CHECK (tax_basis IN ('StoreAddress', 'ShippingAddress', 'BillingAddress')),
            CONSTRAINT tax_configurations_country_behavior_ck
                CHECK (different_country_behavior IN
                    ('UseCustomerJurisdiction', 'UseStoreJurisdiction', 'NoTax')),
            CONSTRAINT tax_configurations_tenant_store_uk UNIQUE (tenant_id, store_id)
        );

        CREATE TABLE IF NOT EXISTS tax_schema.tax_quotes (
            id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
            tenant_id varchar(100) NOT NULL,
            store_id varchar(100) NOT NULL,
            idempotency_key varchar(128) NULL,
            currency_code varchar(3) NOT NULL,
            status varchar(16) NOT NULL DEFAULT 'Calculated',
            customer_id uuid NULL,
            order_id uuid NULL,
            jurisdiction_country_code varchar(3) NULL,
            jurisdiction_zone_code varchar(100) NULL,
            jurisdiction_state_province varchar(100) NULL,
            taxable_amount numeric(19,4) NOT NULL DEFAULT 0,
            total_tax_amount numeric(19,4) NOT NULL DEFAULT 0,
            calculated_at timestamptz NOT NULL DEFAULT now(),
            correlation_id varchar(255) NULL,
            CONSTRAINT tax_quotes_status_ck CHECK (status IN ('Calculated', 'Failed')),
            CONSTRAINT tax_quotes_amounts_nonnegative_ck CHECK (taxable_amount >= 0 AND total_tax_amount >= 0),
            CONSTRAINT tax_quotes_currency_ck CHECK (currency_code ~ '^[A-Z]{3}$'),
            CONSTRAINT tax_quotes_idempotency_uk UNIQUE (tenant_id, store_id, idempotency_key)
        );
        CREATE INDEX IF NOT EXISTS tax_quotes_store_created_idx
            ON tax_schema.tax_quotes (tenant_id, store_id, calculated_at DESC);
        CREATE INDEX IF NOT EXISTS tax_quote_scope_created_idx
            ON tax_schema.tax_quotes (tenant_id, store_id, calculated_at);

        CREATE TABLE IF NOT EXISTS tax_schema.tax_quote_items (
            id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
            tax_quote_id uuid NOT NULL REFERENCES tax_schema.tax_quotes (id) ON DELETE CASCADE,
            tax_class_id uuid NULL REFERENCES tax_schema.tax_classes (id) ON DELETE SET NULL,
            tax_code varchar(100) NOT NULL,
            label varchar(255) NOT NULL,
            rate_percent numeric(7,4) NOT NULL,
            taxable_amount numeric(19,4) NOT NULL DEFAULT 0,
            tax_amount numeric(19,4) NOT NULL DEFAULT 0,
            piggyback boolean NOT NULL DEFAULT false,
            priority integer NOT NULL DEFAULT 0,
            CONSTRAINT tax_quote_items_rate_ck CHECK (rate_percent >= 0 AND rate_percent <= 100),
            CONSTRAINT tax_quote_items_amounts_ck CHECK (taxable_amount >= 0 AND tax_amount >= 0),
            CONSTRAINT tax_quote_items_priority_ck CHECK (priority >= 0)
        );
        CREATE INDEX IF NOT EXISTS tax_quote_items_quote_idx
            ON tax_schema.tax_quote_items (tax_quote_id, priority);
        """;

    private const string MigrationSql = """
        ALTER TABLE tax_schema.tax_classes ADD COLUMN IF NOT EXISTS tenant_id varchar(100) NOT NULL DEFAULT 'default';
        ALTER TABLE tax_schema.tax_classes ADD COLUMN IF NOT EXISTS store_id varchar(100) NOT NULL DEFAULT 'default';
        ALTER TABLE tax_schema.tax_classes ADD COLUMN IF NOT EXISTS correlation_id varchar(255);
        ALTER TABLE tax_schema.tax_rates ADD COLUMN IF NOT EXISTS state_province varchar(100);
        ALTER TABLE tax_schema.tax_rates ADD COLUMN IF NOT EXISTS correlation_id varchar(255);
        ALTER TABLE tax_schema.tax_rate_descriptions ADD COLUMN IF NOT EXISTS correlation_id varchar(255);
        ALTER TABLE tax_schema.tax_configurations ADD COLUMN IF NOT EXISTS different_country_behavior varchar(32) NOT NULL DEFAULT 'UseCustomerJurisdiction';
        ALTER TABLE tax_schema.tax_configurations ADD COLUMN IF NOT EXISTS correlation_id varchar(255);
        ALTER TABLE tax_schema.tax_quotes ADD COLUMN IF NOT EXISTS idempotency_key varchar(128);
        ALTER TABLE tax_schema.tax_quotes ADD COLUMN IF NOT EXISTS correlation_id varchar(255);
        """;
}
