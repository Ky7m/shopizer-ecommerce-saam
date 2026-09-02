using Npgsql;

namespace Shopizer.CustomerIdentity.Data;

public sealed class SchemaInitializer(NpgsqlDataSource dataSource, ILogger<SchemaInitializer> logger)
{
    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(SchemaSql, connection);
        await command.ExecuteNonQueryAsync(cancellationToken);
        await using var migration = new NpgsqlCommand(MigrationSql, connection);
        await migration.ExecuteNonQueryAsync(cancellationToken);
        logger.LogInformation("Customer identity PostgreSQL schema is ready.");
    }

    private const string SchemaSql = """
        CREATE SCHEMA IF NOT EXISTS customer_identity;
        CREATE EXTENSION IF NOT EXISTS pgcrypto;
        DO $$ BEGIN
          CREATE TYPE customer_identity.customer_status AS ENUM ('Active','Suspended','Deleted');
        EXCEPTION WHEN duplicate_object THEN NULL; END $$;
        DO $$ BEGIN
          CREATE TYPE customer_identity.address_type AS ENUM ('Billing','Delivery');
        EXCEPTION WHEN duplicate_object THEN NULL; END $$;
        DO $$ BEGIN
          CREATE TYPE customer_identity.review_status AS ENUM ('Pending','Published','Rejected','Deleted');
        EXCEPTION WHEN duplicate_object THEN NULL; END $$;
        DO $$ BEGIN
          CREATE TYPE customer_identity.reset_subject_type AS ENUM ('Customer','Administrator');
        EXCEPTION WHEN duplicate_object THEN NULL; END $$;
        DO $$ BEGIN
          CREATE TYPE customer_identity.subscription_status AS ENUM ('Subscribed','Unsubscribed');
        EXCEPTION WHEN duplicate_object THEN NULL; END $$;
        CREATE TABLE IF NOT EXISTS customer_identity.customer_accounts (
          id uuid PRIMARY KEY DEFAULT gen_random_uuid(), tenant_id varchar(100) NOT NULL DEFAULT 'default',
          store_id varchar(100) NOT NULL, login_name varchar(96) NOT NULL, email_address varchar(96) NOT NULL,
          password_hash varchar(255) NOT NULL, gender varchar(16) NOT NULL DEFAULT 'M', date_of_birth date,
          company_name varchar(100), provider varchar(80), status customer_identity.customer_status NOT NULL DEFAULT 'Active',
          default_language_code varchar(10) NOT NULL, review_average numeric(4,2) NOT NULL DEFAULT 0 CHECK (review_average BETWEEN 0 AND 5),
          review_count integer NOT NULL DEFAULT 0 CHECK (review_count >= 0), anonymous boolean NOT NULL DEFAULT false,
          created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NOT NULL DEFAULT now(),
          created_by varchar(100), correlation_id uuid, last_password_reset_at timestamptz,
          CONSTRAINT uq_customer_store_login UNIQUE (tenant_id, store_id, login_name),
          CONSTRAINT uq_customer_store_email UNIQUE (tenant_id, store_id, email_address));
        CREATE INDEX IF NOT EXISTS ix_customer_store_email ON customer_identity.customer_accounts (tenant_id,store_id,email_address);
        CREATE INDEX IF NOT EXISTS ix_customer_status ON customer_identity.customer_accounts (tenant_id,store_id,status);
        CREATE TABLE IF NOT EXISTS customer_identity.customer_addresses (
          id uuid PRIMARY KEY DEFAULT gen_random_uuid(), customer_id uuid NOT NULL REFERENCES customer_identity.customer_accounts(id) ON DELETE CASCADE,
          address_type customer_identity.address_type NOT NULL, first_name varchar(64) NOT NULL, last_name varchar(64) NOT NULL,
          company_name varchar(100), street_address varchar(256) NOT NULL, city varchar(100) NOT NULL, postal_code varchar(20) NOT NULL,
          state_province varchar(100), telephone varchar(32), country_code varchar(10) NOT NULL, zone_code varchar(20),
          latitude varchar(100), longitude varchar(100), created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NOT NULL DEFAULT now(),
          CONSTRAINT uq_customer_address_type UNIQUE (customer_id,address_type));
        CREATE INDEX IF NOT EXISTS ix_customer_address_country ON customer_identity.customer_addresses(country_code,zone_code);
        CREATE TABLE IF NOT EXISTS customer_identity.customer_options (
          id uuid PRIMARY KEY DEFAULT gen_random_uuid(), store_id varchar(100) NOT NULL, code varchar(100) NOT NULL,
          option_type varchar(10) NOT NULL, sort_order integer NOT NULL DEFAULT 0, is_active boolean NOT NULL DEFAULT true,
          is_public boolean NOT NULL DEFAULT false, created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NOT NULL DEFAULT now(),
          CONSTRAINT uq_customer_option_store_code UNIQUE(store_id,code), CONSTRAINT ck_customer_option_code CHECK(code ~ '^[A-Za-z0-9_]+$'));
        CREATE INDEX IF NOT EXISTS ix_customer_option_store ON customer_identity.customer_options(store_id,sort_order);
        CREATE TABLE IF NOT EXISTS customer_identity.customer_option_values (
          id uuid PRIMARY KEY DEFAULT gen_random_uuid(), option_id uuid NOT NULL REFERENCES customer_identity.customer_options(id) ON DELETE CASCADE,
          store_id varchar(100) NOT NULL, code varchar(100) NOT NULL, image_url varchar(512), sort_order integer NOT NULL DEFAULT 0,
          created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NOT NULL DEFAULT now(),
          CONSTRAINT uq_customer_option_value_store_code UNIQUE(store_id,code), CONSTRAINT uq_customer_option_value_option_code UNIQUE(option_id,code),
          CONSTRAINT ck_customer_option_value_code CHECK(code ~ '^[A-Za-z0-9_]+$'));
        CREATE TABLE IF NOT EXISTS customer_identity.customer_attributes (
          id uuid PRIMARY KEY DEFAULT gen_random_uuid(), customer_id uuid NOT NULL REFERENCES customer_identity.customer_accounts(id) ON DELETE CASCADE,
          option_id uuid NOT NULL REFERENCES customer_identity.customer_options(id) ON DELETE RESTRICT,
          option_value_id uuid NOT NULL REFERENCES customer_identity.customer_option_values(id) ON DELETE RESTRICT, text_value text,
          created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NOT NULL DEFAULT now(),
          CONSTRAINT uq_customer_attribute_option UNIQUE(customer_id,option_id));
        CREATE INDEX IF NOT EXISTS ix_customer_attributes_customer ON customer_identity.customer_attributes(customer_id);
        CREATE TABLE IF NOT EXISTS customer_identity.customer_reviews (
          id uuid PRIMARY KEY DEFAULT gen_random_uuid(), reviewer_customer_id uuid NOT NULL REFERENCES customer_identity.customer_accounts(id) ON DELETE CASCADE,
          reviewed_customer_id uuid NOT NULL REFERENCES customer_identity.customer_accounts(id) ON DELETE CASCADE,
          rating numeric(3,1) NOT NULL CHECK(rating BETWEEN 1 AND 5), review_text text, review_date timestamptz NOT NULL DEFAULT now(),
          status customer_identity.review_status NOT NULL DEFAULT 'Pending', read_count bigint NOT NULL DEFAULT 0 CHECK(read_count >= 0),
          created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NOT NULL DEFAULT now(),
          CONSTRAINT uq_customer_review_pair UNIQUE(reviewer_customer_id,reviewed_customer_id),
          CONSTRAINT ck_customer_review_not_self CHECK(reviewer_customer_id <> reviewed_customer_id));
        CREATE INDEX IF NOT EXISTS ix_customer_reviews_target ON customer_identity.customer_reviews(reviewed_customer_id,status);
        CREATE TABLE IF NOT EXISTS customer_identity.newsletter_subscriptions (
          id uuid PRIMARY KEY DEFAULT gen_random_uuid(), tenant_id varchar(100) NOT NULL DEFAULT 'default', store_id varchar(100) NOT NULL,
          campaign_code varchar(50) NOT NULL, email_address varchar(320) NOT NULL, first_name varchar(64), last_name varchar(64),
          status customer_identity.subscription_status NOT NULL DEFAULT 'Subscribed', subscribed_at timestamptz NOT NULL DEFAULT now(),
          unsubscribed_at timestamptz, created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NOT NULL DEFAULT now(),
          CONSTRAINT uq_newsletter_store_campaign_email UNIQUE(tenant_id,store_id,campaign_code,email_address));
        CREATE INDEX IF NOT EXISTS ix_newsletter_email ON customer_identity.newsletter_subscriptions(tenant_id,store_id,email_address);
        CREATE TABLE IF NOT EXISTS customer_identity.permission_groups (
          id uuid PRIMARY KEY DEFAULT gen_random_uuid(), name varchar(100) NOT NULL UNIQUE, group_type varchar(30) NOT NULL,
          created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NOT NULL DEFAULT now());
        CREATE TABLE IF NOT EXISTS customer_identity.permissions (
          id uuid PRIMARY KEY DEFAULT gen_random_uuid(), name varchar(150) NOT NULL UNIQUE,
          created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NOT NULL DEFAULT now());
        CREATE TABLE IF NOT EXISTS customer_identity.group_permissions (
          group_id uuid NOT NULL REFERENCES customer_identity.permission_groups(id) ON DELETE CASCADE,
          permission_id uuid NOT NULL REFERENCES customer_identity.permissions(id) ON DELETE CASCADE, PRIMARY KEY(group_id,permission_id));
        CREATE TABLE IF NOT EXISTS customer_identity.administrator_accounts (
          id uuid PRIMARY KEY DEFAULT gen_random_uuid(), tenant_id varchar(100) NOT NULL DEFAULT 'default', store_id varchar(100) NOT NULL,
          user_name varchar(100) NOT NULL, email_address varchar(320) NOT NULL, password_hash varchar(255) NOT NULL,
          first_name varchar(100), last_name varchar(100), is_active boolean NOT NULL DEFAULT true, default_language_code varchar(10),
          question_one text, question_two text, question_three text, answer_one text, answer_two text, answer_three text,
          last_access_at timestamptz, login_at timestamptz, created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NOT NULL DEFAULT now(),
          created_by varchar(100), correlation_id uuid, last_password_reset_at timestamptz,
          CONSTRAINT uq_admin_store_username UNIQUE(tenant_id,store_id,user_name));
        CREATE INDEX IF NOT EXISTS ix_admin_store_email ON customer_identity.administrator_accounts(tenant_id,store_id,email_address);
        CREATE INDEX IF NOT EXISTS ix_admin_active ON customer_identity.administrator_accounts(tenant_id,store_id,is_active);
        CREATE TABLE IF NOT EXISTS customer_identity.administrator_group_memberships (
          administrator_id uuid NOT NULL REFERENCES customer_identity.administrator_accounts(id) ON DELETE CASCADE,
          group_id uuid NOT NULL REFERENCES customer_identity.permission_groups(id) ON DELETE RESTRICT, PRIMARY KEY(administrator_id,group_id));
        CREATE TABLE IF NOT EXISTS customer_identity.credential_reset_tokens (
          id uuid PRIMARY KEY DEFAULT gen_random_uuid(), subject_type customer_identity.reset_subject_type NOT NULL,
          customer_id uuid REFERENCES customer_identity.customer_accounts(id) ON DELETE CASCADE,
          administrator_id uuid REFERENCES customer_identity.administrator_accounts(id) ON DELETE CASCADE,
          token_hash varchar(255) NOT NULL UNIQUE, tenant_id varchar(100) NOT NULL DEFAULT 'default', store_id varchar(100) NOT NULL, expires_at timestamptz NOT NULL, consumed_at timestamptz,
          created_at timestamptz NOT NULL DEFAULT now(),
          CONSTRAINT ck_reset_one_subject CHECK((subject_type='Customer' AND customer_id IS NOT NULL AND administrator_id IS NULL) OR
            (subject_type='Administrator' AND administrator_id IS NOT NULL AND customer_id IS NULL)));
        CREATE INDEX IF NOT EXISTS ix_reset_expiry ON customer_identity.credential_reset_tokens(expires_at,consumed_at);
        CREATE TABLE IF NOT EXISTS customer_identity.external_identity_connections (
          user_id varchar(100) NOT NULL, provider_id varchar(100) NOT NULL, provider_user_id varchar(255) NOT NULL,
          access_token text, refresh_token text, secret text, display_name varchar(255), profile_url varchar(512), image_url varchar(512),
          expires_at timestamptz, rank integer NOT NULL DEFAULT 0, created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NOT NULL DEFAULT now(),
          PRIMARY KEY(user_id,provider_id,provider_user_id));
        CREATE TABLE IF NOT EXISTS customer_identity.event_outbox (
          id uuid PRIMARY KEY, event_type varchar(150) NOT NULL, tenant_id varchar(100) NOT NULL, store_id varchar(100) NOT NULL,
          correlation_id varchar(255) NOT NULL, payload jsonb NOT NULL, occurred_at timestamptz NOT NULL, published_at timestamptz);
        CREATE TABLE IF NOT EXISTS customer_identity.email_outbox (
          id uuid PRIMARY KEY, tenant_id varchar(100) NOT NULL, store_id varchar(100) NOT NULL, recipient varchar(320) NOT NULL,
          template varchar(100) NOT NULL, payload jsonb NOT NULL, created_at timestamptz NOT NULL DEFAULT now(), sent_at timestamptz);
        """;

    private const string MigrationSql = """
        ALTER TABLE customer_identity.customer_accounts ADD COLUMN IF NOT EXISTS tenant_id varchar(100) NOT NULL DEFAULT 'default';
        ALTER TABLE customer_identity.customer_accounts ADD COLUMN IF NOT EXISTS last_password_reset_at timestamptz;
        ALTER TABLE customer_identity.administrator_accounts ADD COLUMN IF NOT EXISTS tenant_id varchar(100) NOT NULL DEFAULT 'default';
        ALTER TABLE customer_identity.administrator_accounts ADD COLUMN IF NOT EXISTS last_password_reset_at timestamptz;
        ALTER TABLE customer_identity.newsletter_subscriptions ADD COLUMN IF NOT EXISTS tenant_id varchar(100) NOT NULL DEFAULT 'default';
        ALTER TABLE customer_identity.credential_reset_tokens ADD COLUMN IF NOT EXISTS store_id varchar(100) NOT NULL DEFAULT 'default';
        ALTER TABLE customer_identity.credential_reset_tokens ADD COLUMN IF NOT EXISTS tenant_id varchar(100) NOT NULL DEFAULT 'default';
        """;
}
