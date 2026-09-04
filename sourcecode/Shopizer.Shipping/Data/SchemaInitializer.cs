using Npgsql;

namespace Shopizer.Shipping.Data;

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
        logger.LogInformation("Shipping PostgreSQL schema is ready.");
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
        CREATE SCHEMA IF NOT EXISTS shipping;

        CREATE TABLE IF NOT EXISTS shipping.shipping_origin (
            id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
            tenant_id UUID NOT NULL, store_id UUID NOT NULL,
            address VARCHAR(256) NOT NULL, city VARCHAR(100) NOT NULL,
            postal_code VARCHAR(20) NOT NULL, state VARCHAR(100),
            country_code CHAR(2), zone_code VARCHAR(32),
            active BOOLEAN NOT NULL DEFAULT FALSE,
            created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
            updated_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
            CONSTRAINT shipping_origin_country_code_ck
                CHECK (country_code IS NULL OR country_code ~ '^[A-Z]{2}$')
        );
        CREATE UNIQUE INDEX IF NOT EXISTS shipping_origin_one_active_per_store_uq
            ON shipping.shipping_origin (tenant_id, store_id) WHERE active = TRUE;
        CREATE INDEX IF NOT EXISTS shipping_origin_store_ix
            ON shipping.shipping_origin (tenant_id, store_id);

        CREATE TABLE IF NOT EXISTS shipping.shipping_quote (
            id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
            tenant_id UUID NOT NULL, store_id UUID NOT NULL,
            cart_id UUID, customer_id UUID, order_id UUID,
            provider_code VARCHAR(100) NOT NULL,
            option_code VARCHAR(100), option_name VARCHAR(255),
            option_delivery_at TIMESTAMPTZ, option_shipping_at TIMESTAMPTZ,
            quoted_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
            estimated_number_of_days INTEGER,
            price NUMERIC(19,4) NOT NULL DEFAULT 0,
            handling NUMERIC(19,4) NOT NULL DEFAULT 0,
            free_shipping BOOLEAN NOT NULL DEFAULT FALSE,
            ip_address INET,
            delivery_first_name VARCHAR(64), delivery_last_name VARCHAR(64),
            delivery_company VARCHAR(100), delivery_address VARCHAR(256),
            delivery_city VARCHAR(100), delivery_postal_code VARCHAR(20),
            delivery_state VARCHAR(100), delivery_telephone VARCHAR(32),
            delivery_country_code CHAR(2), delivery_zone_code VARCHAR(32),
            delivery_latitude NUMERIC(12,8), delivery_longitude NUMERIC(12,8),
            idempotency_key VARCHAR(128),
            calculation_audit JSONB NOT NULL DEFAULT '{}'::jsonb,
            created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
            updated_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
            CONSTRAINT shipping_quote_price_ck CHECK (price >= 0),
            CONSTRAINT shipping_quote_handling_ck CHECK (handling >= 0),
            CONSTRAINT shipping_quote_days_ck
                CHECK (estimated_number_of_days IS NULL OR estimated_number_of_days >= 0),
            CONSTRAINT shipping_quote_country_code_ck
                CHECK (delivery_country_code IS NULL OR delivery_country_code ~ '^[A-Z]{2}$')
        );
        CREATE INDEX IF NOT EXISTS shipping_quote_cart_ix
            ON shipping.shipping_quote (tenant_id, store_id, cart_id, quoted_at DESC);
        CREATE INDEX IF NOT EXISTS shipping_quote_order_ix
            ON shipping.shipping_quote (tenant_id, store_id, order_id, quoted_at DESC);
        CREATE INDEX IF NOT EXISTS shipping_quote_provider_ix
            ON shipping.shipping_quote (tenant_id, store_id, provider_code, quoted_at DESC);
        CREATE UNIQUE INDEX IF NOT EXISTS shipping_quote_idempotency_uq
            ON shipping.shipping_quote (tenant_id, store_id, idempotency_key)
            WHERE idempotency_key IS NOT NULL;

        CREATE TABLE IF NOT EXISTS shipping.shipping_configuration_projection (
            tenant_id UUID NOT NULL, store_id UUID NOT NULL,
            shipping_type VARCHAR(20) NOT NULL DEFAULT 'National',
            shipping_basis_type VARCHAR(20) NOT NULL DEFAULT 'Shipping',
            shipping_option_price_type VARCHAR(20) NOT NULL DEFAULT 'All',
            shipping_package_type VARCHAR(20) NOT NULL DEFAULT 'Item',
            shipping_description VARCHAR(30), free_shipping_type VARCHAR(20),
            box_width INTEGER, box_height INTEGER, box_length INTEGER,
            box_weight NUMERIC(19,4), max_weight NUMERIC(19,4),
            free_shipping_enabled BOOLEAN NOT NULL DEFAULT FALSE,
            order_total_free_shipping NUMERIC(19,4), handling_fees NUMERIC(19,4),
            tax_on_shipping BOOLEAN NOT NULL DEFAULT FALSE,
            updated_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
            PRIMARY KEY (tenant_id, store_id),
            CONSTRAINT shipping_config_nonnegative_ck CHECK (
                (box_width IS NULL OR box_width >= 0) AND
                (box_height IS NULL OR box_height >= 0) AND
                (box_length IS NULL OR box_length >= 0) AND
                (box_weight IS NULL OR box_weight >= 0) AND
                (max_weight IS NULL OR max_weight >= 0) AND
                (handling_fees IS NULL OR handling_fees >= 0))
        );

        CREATE TABLE IF NOT EXISTS shipping.shipping_module_projection (
            tenant_id UUID NOT NULL, store_id UUID NOT NULL, module_code VARCHAR(100) NOT NULL,
            active BOOLEAN NOT NULL DEFAULT FALSE, default_selected BOOLEAN NOT NULL DEFAULT FALSE,
            environment VARCHAR(20) NOT NULL DEFAULT 'Test',
            integration_keys JSONB NOT NULL DEFAULT '{}'::jsonb,
            integration_options JSONB NOT NULL DEFAULT '{}'::jsonb,
            updated_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
            PRIMARY KEY (tenant_id, store_id, module_code)
        );
        CREATE TABLE IF NOT EXISTS shipping.shipping_package_projection (
            id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
            tenant_id UUID NOT NULL, store_id UUID NOT NULL, code VARCHAR(100) NOT NULL,
            shipping_width NUMERIC(19,4) NOT NULL, shipping_height NUMERIC(19,4) NOT NULL,
            shipping_length NUMERIC(19,4) NOT NULL, shipping_weight NUMERIC(19,4) NOT NULL,
            shipping_max_weight NUMERIC(19,4) NOT NULL, treshold INTEGER,
            type VARCHAR(10) NOT NULL, default_packaging BOOLEAN,
            CONSTRAINT shipping_package_scope_code_uq UNIQUE (tenant_id, store_id, code),
            CONSTRAINT shipping_package_nonnegative_ck CHECK (
                shipping_width >= 0 AND shipping_height >= 0 AND shipping_length >= 0 AND
                shipping_weight >= 0 AND shipping_max_weight >= 0)
        );
        CREATE TABLE IF NOT EXISTS shipping.shipping_expedition_projection (
            tenant_id UUID NOT NULL, store_id UUID NOT NULL,
            international_shipping BOOLEAN NOT NULL DEFAULT FALSE,
            tax_on_shipping BOOLEAN NOT NULL DEFAULT FALSE,
            ship_to_country JSONB NOT NULL DEFAULT '[]'::jsonb,
            updated_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
            PRIMARY KEY (tenant_id, store_id)
        );
        CREATE TABLE IF NOT EXISTS shipping.event_outbox (
            id UUID PRIMARY KEY, event_type VARCHAR(150) NOT NULL,
            tenant_id UUID NOT NULL, store_id UUID NOT NULL, correlation_id VARCHAR(255) NOT NULL,
            payload JSONB NOT NULL, occurred_at TIMESTAMPTZ NOT NULL, published_at TIMESTAMPTZ
        );
        """;

    private const string MigrationSql = """
        ALTER TABLE shipping.shipping_quote ADD COLUMN IF NOT EXISTS idempotency_key VARCHAR(128);
        ALTER TABLE shipping.shipping_quote ADD COLUMN IF NOT EXISTS calculation_audit JSONB NOT NULL DEFAULT '{}'::jsonb;
        ALTER TABLE shipping.shipping_quote ADD COLUMN IF NOT EXISTS delivery_latitude NUMERIC(12,8);
        ALTER TABLE shipping.shipping_quote ADD COLUMN IF NOT EXISTS delivery_longitude NUMERIC(12,8);
        ALTER TABLE shipping.shipping_quote ADD COLUMN IF NOT EXISTS updated_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP;
        ALTER TABLE shipping.shipping_origin ADD COLUMN IF NOT EXISTS updated_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP;
        """;
}
