using Npgsql;

namespace Shopizer.Search.Data;

public sealed class SchemaInitializer(NpgsqlDataSource dataSource, ILogger<SchemaInitializer> logger)
{
    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await EnsurePgCryptoAsync(connection, cancellationToken);
        await using (var command = new NpgsqlCommand(SchemaSql, connection))
        {
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        await using (var migration = new NpgsqlCommand(MigrationSql, connection))
        {
            await migration.ExecuteNonQueryAsync(cancellationToken);
        }

        logger.LogInformation("Search PostgreSQL schema is ready.");
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

    public const string SchemaSql = """
        CREATE SCHEMA IF NOT EXISTS search;

        CREATE TABLE IF NOT EXISTS search.search_index (
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

        CREATE TABLE IF NOT EXISTS search.search_document (
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

        CREATE TABLE IF NOT EXISTS search.search_document_locale (
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

        CREATE TABLE IF NOT EXISTS search.search_document_inventory (
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

        CREATE TABLE IF NOT EXISTS search.search_query_profile (
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

        CREATE TABLE IF NOT EXISTS search.search_rebuild_job (
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

        CREATE TABLE IF NOT EXISTS search.event_outbox (
            id UUID PRIMARY KEY,
            event_type VARCHAR(150) NOT NULL,
            tenant_id VARCHAR(200) NOT NULL,
            store_id VARCHAR(120) NOT NULL,
            correlation_id VARCHAR(255) NOT NULL,
            payload JSONB NOT NULL,
            occurred_at TIMESTAMPTZ NOT NULL,
            published_at TIMESTAMPTZ
        );

        CREATE INDEX IF NOT EXISTS ix_search_document_product
            ON search.search_document (tenant_id, store_id, product_id);
        CREATE INDEX IF NOT EXISTS ix_search_document_state
            ON search.search_document (search_index_id, state);
        CREATE INDEX IF NOT EXISTS ix_search_document_locale
            ON search.search_document (search_index_id, locale);
        CREATE INDEX IF NOT EXISTS ix_search_inventory_document
            ON search.search_document_inventory (document_id);
        CREATE INDEX IF NOT EXISTS ix_search_rebuild_state
            ON search.search_rebuild_job (search_index_id, state, requested_at);
        """;

    public const string MigrationSql = """
        ALTER TABLE search.search_index ADD COLUMN IF NOT EXISTS last_failure_code VARCHAR(80);
        ALTER TABLE search.search_document ADD COLUMN IF NOT EXISTS source_version BIGINT;
        ALTER TABLE search.search_document ADD COLUMN IF NOT EXISTS indexed_at TIMESTAMPTZ;
        ALTER TABLE search.search_rebuild_job ADD COLUMN IF NOT EXISTS error_message TEXT;
        ALTER TABLE search.search_rebuild_job ADD COLUMN IF NOT EXISTS failed_document_count BIGINT NOT NULL DEFAULT 0;
        CREATE INDEX IF NOT EXISTS search_document_scope_updated_idx
            ON search.search_document (tenant_id, store_id, updated_at);
        CREATE INDEX IF NOT EXISTS search_rebuild_job_scope_status_idx
            ON search.search_rebuild_job (tenant_id, store_id, state, created_at);
        """;
}
