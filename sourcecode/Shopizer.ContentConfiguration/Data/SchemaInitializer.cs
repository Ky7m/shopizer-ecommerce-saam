using Npgsql;

namespace Shopizer.ContentConfiguration.Data;

public sealed class SchemaInitializer(NpgsqlDataSource dataSource, ILogger<SchemaInitializer> logger)
{
    public async Task InitializeAsync(CancellationToken ct)
    {
        await using var connection = await dataSource.OpenConnectionAsync(ct);
        await EnsurePgCryptoAsync(connection, ct);
        await using var schema = new NpgsqlCommand(SchemaSql, connection);
        await schema.ExecuteNonQueryAsync(ct);
        await using var migration = new NpgsqlCommand(MigrationSql, connection);
        await migration.ExecuteNonQueryAsync(ct);
        logger.LogInformation("Content and configuration PostgreSQL schema is ready.");
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
        CREATE SCHEMA IF NOT EXISTS content_configuration;
        CREATE TABLE IF NOT EXISTS content_configuration.content (
          content_id uuid PRIMARY KEY DEFAULT gen_random_uuid(), tenant_id uuid NOT NULL, store_id uuid NOT NULL,
          code varchar(100) NOT NULL, content_type varchar(10) NOT NULL, content_position varchar(10),
          link_to_menu boolean NOT NULL DEFAULT false, product_group text, sort_order integer NOT NULL DEFAULT 0,
          visible boolean NOT NULL DEFAULT false, created_at timestamptz NOT NULL DEFAULT now(),
          updated_at timestamptz NOT NULL DEFAULT now(), modified_by varchar(60),
          CONSTRAINT ck_content_code_nonempty CHECK (btrim(code) <> ''),
          CONSTRAINT ck_content_type CHECK (content_type IN ('BOX','PAGE','SECTION')),
          CONSTRAINT ck_content_position CHECK (content_position IS NULL OR content_position IN ('LEFT','RIGHT')),
          CONSTRAINT uq_content_tenant_store_code UNIQUE (tenant_id,store_id,code));
        CREATE INDEX IF NOT EXISTS ix_content_tenant_store_type_sort ON content_configuration.content(tenant_id,store_id,content_type,sort_order,content_id);
        CREATE INDEX IF NOT EXISTS ix_content_tenant_store_visibility ON content_configuration.content(tenant_id,store_id,visible);
        CREATE INDEX IF NOT EXISTS ix_content_tenant_store_menu ON content_configuration.content(tenant_id,store_id,link_to_menu);
        CREATE TABLE IF NOT EXISTS content_configuration.content_description (
          description_id uuid PRIMARY KEY DEFAULT gen_random_uuid(), content_id uuid NOT NULL,
          language_code varchar(10) NOT NULL, name varchar(120) NOT NULL, title varchar(100), description text,
          friendly_url varchar(120), meta_keywords text, meta_title varchar(100), meta_description text,
          created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NOT NULL DEFAULT now(), modified_by varchar(60),
          CONSTRAINT fk_content_description_content FOREIGN KEY(content_id) REFERENCES content_configuration.content(content_id) ON DELETE CASCADE,
          CONSTRAINT ck_content_description_name_nonempty CHECK(btrim(name) <> ''),
          CONSTRAINT ck_content_description_language_nonempty CHECK(btrim(language_code) <> ''),
          CONSTRAINT uq_content_description_content_language UNIQUE(content_id,language_code));
        CREATE INDEX IF NOT EXISTS ix_content_description_language ON content_configuration.content_description(language_code);
        CREATE INDEX IF NOT EXISTS ix_content_description_friendly_url ON content_configuration.content_description(friendly_url);
        CREATE TABLE IF NOT EXISTS content_configuration.content_file (
          content_file_id uuid PRIMARY KEY DEFAULT gen_random_uuid(), tenant_id uuid NOT NULL, store_id uuid NOT NULL,
          file_name text NOT NULL, mime_type varchar(255), file_content_type varchar(30) NOT NULL, folder_path text NOT NULL DEFAULT '/',
          provider_name varchar(20) NOT NULL, provider_key text NOT NULL, state varchar(20) NOT NULL DEFAULT 'PENDING',
          created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NOT NULL DEFAULT now(),
          CONSTRAINT ck_content_file_name CHECK(btrim(file_name) <> '' AND position('/' IN file_name)=0 AND position(chr(92) IN file_name)=0 AND position('..' IN file_name)=0),
          CONSTRAINT ck_content_file_type CHECK(file_content_type IN('STATIC_FILE','IMAGE','LOGO','PRODUCT','PRODUCTLG','PROPERTY','VARIANT','MANUFACTURER','PRODUCT_DIGITAL','API_IMAGE','API_FILE')),
          CONSTRAINT ck_content_file_folder_path CHECK(folder_path ~ '^/$|^(/[A-Za-z0-9_-]+)+$'),
          CONSTRAINT ck_content_file_provider CHECK(provider_name IN('default','httpd','aws','gcp')),
          CONSTRAINT ck_content_file_state CHECK(state IN('PENDING','AVAILABLE','RENAME_PENDING','DELETED')),
          CONSTRAINT ck_content_file_provider_key_nonempty CHECK(btrim(provider_key) <> ''));
        CREATE UNIQUE INDEX IF NOT EXISTS uq_content_file_active_provider_key ON content_configuration.content_file(tenant_id,store_id,provider_key) WHERE state <> 'DELETED';
        CREATE UNIQUE INDEX IF NOT EXISTS uq_content_file_active_namespace_name ON content_configuration.content_file(tenant_id,store_id,file_content_type,folder_path,file_name) WHERE state <> 'DELETED';
        CREATE INDEX IF NOT EXISTS ix_content_file_store_type_state ON content_configuration.content_file(tenant_id,store_id,file_content_type,state);
        CREATE INDEX IF NOT EXISTS ix_content_file_store_folder ON content_configuration.content_file(tenant_id,store_id,folder_path);
        CREATE TABLE IF NOT EXISTS content_configuration.merchant_configuration (
          merchant_configuration_id uuid PRIMARY KEY DEFAULT gen_random_uuid(), tenant_id uuid NOT NULL, store_id uuid NOT NULL,
          config_key varchar(255) NOT NULL, configuration_type varchar(20) NOT NULL DEFAULT 'INTEGRATION',
          active boolean NOT NULL DEFAULT false, value text, created_at timestamptz NOT NULL DEFAULT now(),
          updated_at timestamptz NOT NULL DEFAULT now(), modified_by varchar(60),
          CONSTRAINT ck_merchant_configuration_key_nonempty CHECK(btrim(config_key) <> ''),
          CONSTRAINT ck_merchant_configuration_type CHECK(configuration_type IN('INTEGRATION','SHOP','CONFIG','SOCIAL')),
          CONSTRAINT uq_merchant_configuration_store_key UNIQUE(tenant_id,store_id,config_key));
        CREATE INDEX IF NOT EXISTS ix_merchant_configuration_store_type ON content_configuration.merchant_configuration(tenant_id,store_id,configuration_type);
        CREATE TABLE IF NOT EXISTS content_configuration.module_configuration (
          module_configuration_id uuid PRIMARY KEY DEFAULT gen_random_uuid(), module_family varchar(30) NOT NULL,
          code varchar(100) NOT NULL, regions jsonb, configuration jsonb, details jsonb, module_type varchar(100),
          image varchar(255), custom_module boolean NOT NULL DEFAULT false, created_at timestamptz NOT NULL DEFAULT now(),
          updated_at timestamptz NOT NULL DEFAULT now(), modified_by varchar(60),
          CONSTRAINT ck_module_family_nonempty CHECK(btrim(module_family) <> ''),
          CONSTRAINT ck_module_code_nonempty CHECK(btrim(code) <> ''),
          CONSTRAINT ck_module_regions_array CHECK(regions IS NULL OR jsonb_typeof(regions)='array'),
          CONSTRAINT ck_module_configuration_array CHECK(configuration IS NULL OR jsonb_typeof(configuration)='array'),
          CONSTRAINT ck_module_details_object CHECK(details IS NULL OR jsonb_typeof(details)='object'),
          CONSTRAINT uq_module_configuration_code UNIQUE(code));
        CREATE INDEX IF NOT EXISTS ix_module_configuration_family ON content_configuration.module_configuration(module_family);
        CREATE INDEX IF NOT EXISTS ix_module_configuration_family_code ON content_configuration.module_configuration(module_family,code);
        CREATE TABLE IF NOT EXISTS content_configuration.event_outbox (
          id uuid PRIMARY KEY, event_type varchar(150) NOT NULL, tenant_id uuid NOT NULL, store_id uuid,
          correlation_id varchar(255) NOT NULL, payload jsonb NOT NULL, occurred_at timestamptz NOT NULL, published_at timestamptz);
        """;

    private const string MigrationSql = """
        ALTER TABLE content_configuration.content ADD COLUMN IF NOT EXISTS tenant_id uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
        ALTER TABLE content_configuration.content ADD COLUMN IF NOT EXISTS modified_by varchar(60);
        ALTER TABLE content_configuration.content_description ADD COLUMN IF NOT EXISTS meta_keywords text;
        ALTER TABLE content_configuration.content_description ADD COLUMN IF NOT EXISTS meta_title varchar(100);
        ALTER TABLE content_configuration.content_description ADD COLUMN IF NOT EXISTS meta_description text;
        ALTER TABLE content_configuration.content_file ADD COLUMN IF NOT EXISTS state varchar(20) NOT NULL DEFAULT 'AVAILABLE';
        ALTER TABLE content_configuration.merchant_configuration ADD COLUMN IF NOT EXISTS active boolean NOT NULL DEFAULT false;
        ALTER TABLE content_configuration.module_configuration ADD COLUMN IF NOT EXISTS configuration jsonb;
        """;
}
