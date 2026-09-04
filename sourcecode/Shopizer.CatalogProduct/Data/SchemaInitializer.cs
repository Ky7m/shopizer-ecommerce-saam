using Npgsql;

namespace Shopizer.CatalogProduct.Data;

public sealed class SchemaInitializer(NpgsqlDataSource dataSource, ILogger<SchemaInitializer> logger)
{
    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
                await EnsurePgCryptoAsync(connection, cancellationToken);
                await using (var schema = new NpgsqlCommand(SchemaSql, connection))
                    await schema.ExecuteNonQueryAsync(cancellationToken);
                await using (var migration = new NpgsqlCommand(MigrationSql, connection))
                    await migration.ExecuteNonQueryAsync(cancellationToken);
                logger.LogInformation("Catalog-product PostgreSQL schema is ready.");
                return;
            }
            catch (NpgsqlException) when (attempt < 15 && !cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
            }
        }
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
        CREATE SCHEMA IF NOT EXISTS catalog_product;
        CREATE TABLE IF NOT EXISTS catalog_product.product (
          id uuid PRIMARY KEY DEFAULT gen_random_uuid(), tenant_id text NOT NULL, store_id text NOT NULL,
          sku text NOT NULL, ref_sku text, status text NOT NULL DEFAULT 'Draft', visible boolean NOT NULL DEFAULT false,
          available boolean NOT NULL DEFAULT false, can_be_purchased boolean NOT NULL DEFAULT true,
          date_available timestamptz NOT NULL DEFAULT now(), manufacturer_code text, product_type_code text,
          tax_class_code text, product_virtual boolean NOT NULL DEFAULT false, product_shippable boolean NOT NULL DEFAULT false,
          product_free boolean NOT NULL DEFAULT false, length numeric(18,6), width numeric(18,6), height numeric(18,6),
          weight numeric(18,6), review_average numeric(18,6), review_count integer NOT NULL DEFAULT 0 CHECK(review_count >= 0),
          sort_order integer NOT NULL DEFAULT 0, version bigint NOT NULL DEFAULT 0,
          created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NOT NULL DEFAULT now(),
          UNIQUE(store_id, sku));
        CREATE TABLE IF NOT EXISTS catalog_product.product_description (
          id uuid PRIMARY KEY DEFAULT gen_random_uuid(), product_id uuid NOT NULL REFERENCES catalog_product.product(id) ON DELETE CASCADE,
          language_code text NOT NULL, name text NOT NULL, friendly_url text NOT NULL, description text, highlights text,
          title text, keywords text, meta_description text, created_at timestamptz NOT NULL DEFAULT now(),
          updated_at timestamptz NOT NULL DEFAULT now(), UNIQUE(product_id, language_code));
        CREATE TABLE IF NOT EXISTS catalog_product.category (
          id uuid PRIMARY KEY DEFAULT gen_random_uuid(), tenant_id text NOT NULL, store_id text NOT NULL, code text NOT NULL,
          parent_id uuid REFERENCES catalog_product.category(id) ON DELETE RESTRICT, category_image_uri text,
          sort_order integer NOT NULL DEFAULT 0, status text NOT NULL DEFAULT 'Draft', visible boolean NOT NULL DEFAULT false,
          featured boolean NOT NULL DEFAULT false, depth integer NOT NULL DEFAULT 0 CHECK(depth >= 0), lineage text NOT NULL,
          created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NOT NULL DEFAULT now(), UNIQUE(store_id, code));
        CREATE TABLE IF NOT EXISTS catalog_product.category_description (
          id uuid PRIMARY KEY DEFAULT gen_random_uuid(), category_id uuid NOT NULL REFERENCES catalog_product.category(id) ON DELETE CASCADE,
          language_code text NOT NULL, name text NOT NULL, friendly_url text NOT NULL, description text, title text,
          meta_description text, created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NOT NULL DEFAULT now(),
          UNIQUE(category_id, language_code));
        CREATE TABLE IF NOT EXISTS catalog_product.product_category (
          product_id uuid NOT NULL REFERENCES catalog_product.product(id) ON DELETE CASCADE,
          category_id uuid NOT NULL REFERENCES catalog_product.category(id) ON DELETE CASCADE,
          created_at timestamptz NOT NULL DEFAULT now(), PRIMARY KEY(product_id, category_id));
        CREATE TABLE IF NOT EXISTS catalog_product.product_variant (
          id uuid PRIMARY KEY DEFAULT gen_random_uuid(), product_id uuid NOT NULL REFERENCES catalog_product.product(id) ON DELETE CASCADE,
          store_id text NOT NULL, sku text NOT NULL, code text, status text NOT NULL DEFAULT 'Draft',
          available boolean NOT NULL DEFAULT false, default_selection boolean NOT NULL DEFAULT false,
          date_available timestamptz NOT NULL DEFAULT now(), sort_order integer NOT NULL DEFAULT 0, variation_id uuid,
          created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NOT NULL DEFAULT now(), UNIQUE(product_id, sku));
        CREATE TABLE IF NOT EXISTS catalog_product.product_availability (
          id uuid PRIMARY KEY DEFAULT gen_random_uuid(), product_id uuid REFERENCES catalog_product.product(id) ON DELETE CASCADE,
          variant_id uuid REFERENCES catalog_product.product_variant(id) ON DELETE CASCADE, store_id text NOT NULL,
          region_code text NOT NULL, quantity integer NOT NULL DEFAULT 0, reserved_quantity integer NOT NULL DEFAULT 0,
          active boolean NOT NULL DEFAULT true, version bigint NOT NULL DEFAULT 0,
          created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NOT NULL DEFAULT now(),
          CHECK((product_id IS NOT NULL) <> (variant_id IS NOT NULL)), CHECK(quantity >= 0),
          CHECK(reserved_quantity >= 0 AND reserved_quantity <= quantity));
        CREATE TABLE IF NOT EXISTS catalog_product.product_price (
          id uuid PRIMARY KEY DEFAULT gen_random_uuid(), availability_id uuid NOT NULL REFERENCES catalog_product.product_availability(id) ON DELETE CASCADE,
          store_id text NOT NULL, currency_code text NOT NULL, amount numeric(19,4) NOT NULL, price_type text NOT NULL DEFAULT 'OneTime',
          default_price boolean NOT NULL DEFAULT false, special_amount numeric(19,4), special_start_at timestamptz,
          special_end_at timestamptz, created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NOT NULL DEFAULT now(),
          CHECK(amount >= 0), CHECK(special_amount IS NULL OR special_amount >= 0),
          CHECK(special_end_at IS NULL OR special_start_at IS NULL OR special_end_at > special_start_at));
        CREATE TABLE IF NOT EXISTS catalog_product.product_option (
          id uuid PRIMARY KEY DEFAULT gen_random_uuid(), store_id text NOT NULL, code text NOT NULL, option_type text NOT NULL,
          display_only boolean NOT NULL DEFAULT false, sort_order integer NOT NULL DEFAULT 0,
          created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NOT NULL DEFAULT now(), UNIQUE(store_id, code));
        CREATE TABLE IF NOT EXISTS catalog_product.product_option_value (
          id uuid PRIMARY KEY DEFAULT gen_random_uuid(), option_id uuid NOT NULL REFERENCES catalog_product.product_option(id) ON DELETE CASCADE,
          store_id text NOT NULL, code text NOT NULL, display_only boolean NOT NULL DEFAULT false, image_uri text,
          sort_order integer NOT NULL DEFAULT 0, created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NOT NULL DEFAULT now(),
          UNIQUE(option_id, code));
        CREATE TABLE IF NOT EXISTS catalog_product.product_variation (
          id uuid PRIMARY KEY DEFAULT gen_random_uuid(), store_id text NOT NULL,
          option_id uuid NOT NULL REFERENCES catalog_product.product_option(id) ON DELETE RESTRICT,
          option_value_id uuid NOT NULL REFERENCES catalog_product.product_option_value(id) ON DELETE RESTRICT,
          code text NOT NULL, default_variation boolean NOT NULL DEFAULT false, sort_order integer NOT NULL DEFAULT 0,
          created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NOT NULL DEFAULT now(), UNIQUE(store_id, code));
        CREATE TABLE IF NOT EXISTS catalog_product.product_attribute (
          id uuid PRIMARY KEY DEFAULT gen_random_uuid(), product_id uuid NOT NULL REFERENCES catalog_product.product(id) ON DELETE CASCADE,
          option_id uuid NOT NULL REFERENCES catalog_product.product_option(id) ON DELETE RESTRICT,
          option_value_id uuid NOT NULL REFERENCES catalog_product.product_option_value(id) ON DELETE RESTRICT,
          display_only boolean NOT NULL DEFAULT false, price_adjustment numeric(19,4) NOT NULL DEFAULT 0,
          default_selection boolean NOT NULL DEFAULT false, created_at timestamptz NOT NULL DEFAULT now(),
          updated_at timestamptz NOT NULL DEFAULT now());
        CREATE TABLE IF NOT EXISTS catalog_product.product_image (
          id uuid PRIMARY KEY DEFAULT gen_random_uuid(), product_id uuid NOT NULL REFERENCES catalog_product.product(id) ON DELETE CASCADE,
          variant_id uuid REFERENCES catalog_product.product_variant(id) ON DELETE CASCADE, image_type text NOT NULL,
          file_name text NOT NULL, original_uri text, transformed_uri text, provider_key text, external_url text,
          default_image boolean NOT NULL DEFAULT false, media_status text NOT NULL DEFAULT 'Pending',
          created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NOT NULL DEFAULT now());
        CREATE TABLE IF NOT EXISTS catalog_product.product_relationship (
          id uuid PRIMARY KEY DEFAULT gen_random_uuid(), product_id uuid NOT NULL REFERENCES catalog_product.product(id) ON DELETE CASCADE,
          related_product_id uuid NOT NULL REFERENCES catalog_product.product(id) ON DELETE RESTRICT, relationship_type text NOT NULL,
          sort_order integer NOT NULL DEFAULT 0, created_at timestamptz NOT NULL DEFAULT now(),
          UNIQUE(product_id, related_product_id, relationship_type));
        CREATE TABLE IF NOT EXISTS catalog_product.inventory_reservation (
          id uuid PRIMARY KEY DEFAULT gen_random_uuid(), tenant_id text NOT NULL, store_id text NOT NULL,
          product_id uuid REFERENCES catalog_product.product(id) ON DELETE RESTRICT,
          variant_id uuid REFERENCES catalog_product.product_variant(id) ON DELETE RESTRICT,
          availability_id uuid NOT NULL REFERENCES catalog_product.product_availability(id) ON DELETE RESTRICT,
          reservation_key text NOT NULL, request_hash text NOT NULL, quantity integer NOT NULL CHECK(quantity > 0),
          state text NOT NULL DEFAULT 'Held' CHECK(state IN ('Held','Committed','Released','Expired')),
          expires_at timestamptz NOT NULL, committed_at timestamptz, released_at timestamptz,
          created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NOT NULL DEFAULT now(),
          UNIQUE(store_id, reservation_key));
        CREATE TABLE IF NOT EXISTS catalog_product.event_outbox (
          id uuid PRIMARY KEY, event_type text NOT NULL, tenant_id text NOT NULL, store_id text NOT NULL,
          correlation_id text NOT NULL, payload jsonb NOT NULL, occurred_at timestamptz NOT NULL, published_at timestamptz);
        CREATE INDEX IF NOT EXISTS ix_product_store_visibility ON catalog_product.product(store_id, visible, available, date_available);
        CREATE INDEX IF NOT EXISTS ix_product_description_slug ON catalog_product.product_description(friendly_url, language_code);
        CREATE INDEX IF NOT EXISTS ix_category_store_lineage ON catalog_product.category(store_id, lineage);
        CREATE INDEX IF NOT EXISTS ix_availability_product_region ON catalog_product.product_availability(product_id, region_code, active);
        CREATE INDEX IF NOT EXISTS ix_availability_variant_region ON catalog_product.product_availability(variant_id, region_code, active);
        CREATE INDEX IF NOT EXISTS ix_reservation_expiry ON catalog_product.inventory_reservation(state, expires_at);
        CREATE UNIQUE INDEX IF NOT EXISTS ux_default_variant_per_product ON catalog_product.product_variant(product_id) WHERE default_selection = true;
        """;

    private const string MigrationSql = """
        ALTER TABLE catalog_product.product ADD COLUMN IF NOT EXISTS version bigint NOT NULL DEFAULT 0;
        ALTER TABLE catalog_product.product ADD COLUMN IF NOT EXISTS can_be_purchased boolean NOT NULL DEFAULT true;
        ALTER TABLE catalog_product.product_variant ADD COLUMN IF NOT EXISTS variation_id uuid;
        ALTER TABLE catalog_product.product_image ADD COLUMN IF NOT EXISTS media_status text NOT NULL DEFAULT 'Pending';
        ALTER TABLE catalog_product.event_outbox ADD COLUMN IF NOT EXISTS published_at timestamptz;
        """;
}
