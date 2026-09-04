using Npgsql;

namespace Shopizer.PricingPromotions.Data;

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
        logger.LogInformation("Pricing and promotions PostgreSQL schema is ready.");
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
        CREATE SCHEMA IF NOT EXISTS pricing_promotions;

        CREATE TABLE IF NOT EXISTS pricing_promotions.price_list (
          price_list_id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
          tenant_id varchar(200) NOT NULL, store_id varchar(120) NOT NULL,
          name varchar(200) NOT NULL, currency_code char(3) NOT NULL,
          is_active boolean NOT NULL DEFAULT true, created_by varchar(200),
          created_at timestamptz NOT NULL DEFAULT current_timestamp,
          updated_at timestamptz NOT NULL DEFAULT current_timestamp,
          CONSTRAINT ck_price_list_store_not_blank CHECK (length(trim(store_id)) > 0),
          CONSTRAINT ck_price_list_name_not_blank CHECK (length(trim(name)) > 0),
          CONSTRAINT ck_price_list_currency CHECK (currency_code ~ '^[A-Z]{3}$'),
          CONSTRAINT uq_price_list_tenant_store_currency UNIQUE (tenant_id, store_id, currency_code, name)
        );

        CREATE TABLE IF NOT EXISTS pricing_promotions.price_entry (
          price_entry_id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
          price_list_id uuid NOT NULL REFERENCES pricing_promotions.price_list(price_list_id) ON DELETE CASCADE,
          legacy_price_id bigint, product_sku varchar(160) NOT NULL, variant_sku varchar(160),
          availability_id bigint, code varchar(80) NOT NULL DEFAULT 'base',
          amount numeric(19,4) NOT NULL DEFAULT 0, price_type varchar(20) NOT NULL DEFAULT 'OneTime',
          is_default boolean NOT NULL DEFAULT false, special_start_date date, special_end_date date,
          special_amount numeric(19,4), product_identifier_id bigint, created_by varchar(200),
          created_at timestamptz NOT NULL DEFAULT current_timestamp,
          updated_at timestamptz NOT NULL DEFAULT current_timestamp,
          CONSTRAINT ck_price_entry_product_sku CHECK (length(trim(product_sku)) > 0),
          CONSTRAINT ck_price_entry_variant_sku CHECK (variant_sku IS NULL OR length(trim(variant_sku)) > 0),
          CONSTRAINT ck_price_entry_code CHECK (code ~ '^[A-Za-z0-9_]+$'),
          CONSTRAINT ck_price_entry_amount CHECK (amount >= 0),
          CONSTRAINT ck_price_entry_special_amount CHECK (special_amount IS NULL OR special_amount >= 0),
          CONSTRAINT ck_price_entry_type CHECK (price_type IN ('OneTime', 'Monthly')),
          CONSTRAINT ck_price_entry_special_dates CHECK (
            special_start_date IS NULL OR special_end_date IS NULL OR special_start_date <= special_end_date),
          CONSTRAINT uq_price_entry_legacy_id UNIQUE (legacy_price_id)
        );

        CREATE TABLE IF NOT EXISTS pricing_promotions.price_entry_description (
          price_entry_description_id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
          price_entry_id uuid NOT NULL REFERENCES pricing_promotions.price_entry(price_entry_id) ON DELETE CASCADE,
          language_code varchar(16) NOT NULL, description text, created_by varchar(200),
          created_at timestamptz NOT NULL DEFAULT current_timestamp,
          updated_at timestamptz NOT NULL DEFAULT current_timestamp,
          CONSTRAINT ck_price_entry_description_language CHECK (length(trim(language_code)) > 0),
          CONSTRAINT uq_price_entry_description_language UNIQUE (price_entry_id, language_code)
        );

        CREATE TABLE IF NOT EXISTS pricing_promotions.promotion (
          promotion_id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
          tenant_id varchar(200) NOT NULL, store_id varchar(120) NOT NULL,
          name varchar(200) NOT NULL, rule_key varchar(160) NOT NULL,
          discount_rate numeric(9,6) NOT NULL, valid_from date, valid_until date,
          is_enabled boolean NOT NULL DEFAULT true, created_by varchar(200),
          created_at timestamptz NOT NULL DEFAULT current_timestamp,
          updated_at timestamptz NOT NULL DEFAULT current_timestamp,
          CONSTRAINT ck_promotion_store_not_blank CHECK (length(trim(store_id)) > 0),
          CONSTRAINT ck_promotion_name_not_blank CHECK (length(trim(name)) > 0),
          CONSTRAINT ck_promotion_rule_key_not_blank CHECK (length(trim(rule_key)) > 0),
          CONSTRAINT ck_promotion_discount_rate CHECK (discount_rate >= 0 AND discount_rate <= 1),
          CONSTRAINT ck_promotion_valid_dates CHECK (
            valid_from IS NULL OR valid_until IS NULL OR valid_from <= valid_until),
          CONSTRAINT uq_promotion_rule_scope UNIQUE (tenant_id, store_id, rule_key)
        );

        CREATE TABLE IF NOT EXISTS pricing_promotions.coupon (
          coupon_id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
          promotion_id uuid NOT NULL REFERENCES pricing_promotions.promotion(promotion_id) ON DELETE CASCADE,
          tenant_id varchar(200) NOT NULL, store_id varchar(120) NOT NULL,
          code varchar(160) NOT NULL, valid_from date, valid_until date,
          is_enabled boolean NOT NULL DEFAULT true, created_by varchar(200),
          created_at timestamptz NOT NULL DEFAULT current_timestamp,
          updated_at timestamptz NOT NULL DEFAULT current_timestamp,
          CONSTRAINT ck_coupon_store_not_blank CHECK (length(trim(store_id)) > 0),
          CONSTRAINT ck_coupon_code_not_blank CHECK (length(trim(code)) > 0),
          CONSTRAINT ck_coupon_valid_dates CHECK (
            valid_from IS NULL OR valid_until IS NULL OR valid_from <= valid_until),
          CONSTRAINT uq_coupon_code_scope UNIQUE (tenant_id, store_id, code),
          CONSTRAINT uq_coupon_promotion_code UNIQUE (promotion_id, code)
        );

        CREATE TABLE IF NOT EXISTS pricing_promotions.event_outbox (
          id uuid PRIMARY KEY, event_type varchar(150) NOT NULL, tenant_id varchar(200) NOT NULL,
          store_id varchar(120) NOT NULL, correlation_id varchar(255) NOT NULL,
          payload jsonb NOT NULL, occurred_at timestamptz NOT NULL, published_at timestamptz
        );

        CREATE INDEX IF NOT EXISTS ix_price_entry_product
          ON pricing_promotions.price_entry (price_list_id, product_sku);
        CREATE INDEX IF NOT EXISTS ix_price_entry_variant
          ON pricing_promotions.price_entry (price_list_id, variant_sku) WHERE variant_sku IS NOT NULL;
        CREATE INDEX IF NOT EXISTS ix_price_entry_availability
          ON pricing_promotions.price_entry (availability_id) WHERE availability_id IS NOT NULL;
        CREATE INDEX IF NOT EXISTS ix_price_entry_active_window
          ON pricing_promotions.price_entry (special_start_date, special_end_date)
          WHERE special_amount IS NOT NULL;
        CREATE INDEX IF NOT EXISTS ix_price_entry_description_language
          ON pricing_promotions.price_entry_description (price_entry_id, language_code);
        CREATE INDEX IF NOT EXISTS ix_promotion_enabled_window
          ON pricing_promotions.promotion (tenant_id, store_id, is_enabled, valid_from, valid_until);
        CREATE INDEX IF NOT EXISTS ix_coupon_enabled_window
          ON pricing_promotions.coupon (tenant_id, store_id, is_enabled, valid_from, valid_until);
        """;

    // Additive, idempotent changes protect persistent Aspire databases from schema drift.
    private const string MigrationSql = """
        ALTER TABLE pricing_promotions.price_list ADD COLUMN IF NOT EXISTS is_active boolean NOT NULL DEFAULT true;
        ALTER TABLE pricing_promotions.price_entry ADD COLUMN IF NOT EXISTS product_identifier_id bigint;
        ALTER TABLE pricing_promotions.price_entry ADD COLUMN IF NOT EXISTS special_amount numeric(19,4);
        ALTER TABLE pricing_promotions.promotion ADD COLUMN IF NOT EXISTS is_enabled boolean NOT NULL DEFAULT true;
        ALTER TABLE pricing_promotions.event_outbox ADD COLUMN IF NOT EXISTS published_at timestamptz;
        """;
}
