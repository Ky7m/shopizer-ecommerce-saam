using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Npgsql;

namespace Shopizer.IntegrationTests;

public sealed class AspireHostFixture : IAsyncLifetime
{
    private DistributedApplication? _application;

    public HttpClient CustomerIdentityClient { get; private set; } = null!;
    public HttpClient CatalogProductClient { get; private set; } = null!;
    public HttpClient SearchClient { get; private set; } = null!;
    public HttpClient CartCheckoutClient { get; private set; } = null!;
    public HttpClient OrderManagementClient { get; private set; } = null!;
    public HttpClient PaymentsClient { get; private set; } = null!;
    public HttpClient PricingPromotionsClient { get; private set; } = null!;
    public HttpClient TaxClient { get; private set; } = null!;
    public HttpClient ShippingClient { get; private set; } = null!;
    public HttpClient MerchantAdministrationClient { get; private set; } = null!;
    public HttpClient ContentConfigurationClient { get; private set; } = null!;
    public HttpClient PlatformIntegrationsClient { get; private set; } = null!;
    public string AdminAccessToken { get; private set; } = null!;
    public string BasicAdminAccessToken { get; private set; } = null!;
    public string CustomerAccessToken { get; private set; } = null!;

    public async ValueTask InitializeAsync()
    {
        var builder = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.Shopizer_AppHost>();

        _application = await builder.BuildAsync();
        await _application.StartAsync();
        await EnsureTestAdministratorAsync();
        await EnsureTestCustomersAsync();

        var clients = new Dictionary<string, (Action<HttpClient> Assign, string Tenant, string Store, string Correlation, bool Authorization, string? Language, string? IdempotencyKey)>
        {
            ["customer-identity"] = (client => CustomerIdentityClient = client, "tenant-demo", "default", "00000000-0000-0000-0000-000000000001", false, null, null),
            ["catalog-product"] = (client => CatalogProductClient = client, "test-tenant-001", "test-store-001", "11111111-1111-4111-8111-111111111111", false, null, null),
            ["search"] = (client => SearchClient = client, "test-tenant-001", "test-store-001", "corr-ms03-0001", false, null, null),
            ["cart-checkout"] = (client => CartCheckoutClient = client, "tenant-001", "store-001", "00000000-0000-0000-0000-000000000001", false, null, null),
            ["order-management"] = (client => OrderManagementClient = client, "tenant-a", "store-12", "corr-ms05-001", true, null, null),
            ["payments"] = (client => PaymentsClient = client, "test-tenant-001", "test-store-001", "corr-ms06-001", true, null, null),
            ["pricing-promotions"] = (client => PricingPromotionsClient = client, "2e6d7b63-5b1d-4f8a-8e12-8cf43c9f2001", "store-us-east", "corr-20260901-000184", false, null, null),
            ["tax"] = (client => TaxClient = client, "tenant-001", "store-001", "corr-001", true, null, null),
            ["shipping"] = (client => ShippingClient = client, "test-tenant-001", "test-store-001", "corr-ms09-001", false, null, null),
            ["merchant-administration"] = (client => MerchantAdministrationClient = client, "test-tenant-001", "test-store-001", "corr-ms10-0001", false, null, null),
            ["content-configuration"] = (client => ContentConfigurationClient = client, "00000000-0000-0000-0000-000000000001", "00000000-0000-0000-0000-000000000002", "00000000-0000-0000-0000-000000000003", true, "en", "ms11-test-idempotency-001"),
            ["platform-integrations"] = (client => PlatformIntegrationsClient = client, "test-tenant-001", "test-store-001", "corr-ms12-0001", true, null, null)
        };

        foreach (var (resourceName, configuration) in clients)
        {
            await _application.ResourceNotifications.WaitForResourceHealthyAsync(resourceName);
            var client = _application.CreateHttpClient(resourceName);
            ConfigureClient(client, configuration.Tenant, configuration.Store, configuration.Correlation, configuration.Authorization, configuration.Language, configuration.IdempotencyKey);
            using var healthResponse = await client.GetAsync("/health");
            Assert.Equal(HttpStatusCode.OK, healthResponse.StatusCode);
            configuration.Assign(client);
        }

        AdminAccessToken = await LoginAsync("phase4c-test", "Phase4c!Password2026", true);
        BasicAdminAccessToken = await LoginAsync("phase4c-basic", "Phase4c!Password2026", true);
        CustomerAccessToken = await LoginAsync("phase4c-test", "Phase4c!Password2026", false);
    }

    private async Task EnsureTestAdministratorAsync()
    {
        var connectionString = await _application!.GetConnectionStringAsync("customeridentitydb")
            ?? throw new InvalidOperationException("The customer identity database connection string is unavailable.");

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();

        const string username = "phase4c-test";
        const string password = "Phase4c!Password2026";
        var salt = RandomNumberGenerator.GetBytes(16);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, 120_000, HashAlgorithmName.SHA256, 32);
        var encodedPassword = $"PBKDF2-SHA256$120000${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";

        await using var command = new NpgsqlCommand(
            """
            INSERT INTO customer_identity.administrator_accounts
                (tenant_id, store_id, user_name, email_address, password_hash, first_name, last_name, is_active, default_language_code)
            VALUES
                (@tenant, @store, @username, @email, @password, @first, @last, true, @language)
            ON CONFLICT (tenant_id, store_id, user_name)
            DO UPDATE SET
                email_address = EXCLUDED.email_address,
                password_hash = EXCLUDED.password_hash,
                first_name = EXCLUDED.first_name,
                last_name = EXCLUDED.last_name,
                is_active = true,
                default_language_code = EXCLUDED.default_language_code,
                last_password_reset_at = NULL
            """,
            connection);

        command.Parameters.AddWithValue("tenant", "tenant-demo");
        command.Parameters.AddWithValue("store", "default");
        command.Parameters.AddWithValue("username", username);
        command.Parameters.AddWithValue("email", "phase4c@example.com");
        command.Parameters.AddWithValue("password", encodedPassword);
        command.Parameters.AddWithValue("first", "phase4c-test");
        command.Parameters.AddWithValue("last", "phase4c-test");
        command.Parameters.AddWithValue("language", "en");
        await command.ExecuteNonQueryAsync();

        await using var groupCommand = new NpgsqlCommand(
            """
            INSERT INTO customer_identity.permission_groups(name, group_type)
            VALUES ('ADMIN', 'Administrator'), ('BASIC', 'Administrator')
            ON CONFLICT (name) DO NOTHING;
            INSERT INTO customer_identity.administrator_group_memberships(administrator_id, group_id)
            SELECT a.id, g.id
            FROM customer_identity.administrator_accounts a
            JOIN customer_identity.permission_groups g ON g.name = CASE a.user_name
                WHEN 'phase4c-test' THEN 'ADMIN'
                ELSE 'BASIC'
            END
            WHERE a.tenant_id = 'tenant-demo' AND a.store_id = 'default'
              AND a.user_name IN ('phase4c-test', 'phase4c-basic')
            ON CONFLICT DO NOTHING;
            """,
            connection);
        await groupCommand.ExecuteNonQueryAsync();

        await using var basicAdminCommand = new NpgsqlCommand(
            """
            INSERT INTO customer_identity.administrator_accounts
                (tenant_id, store_id, user_name, email_address, password_hash, first_name, last_name, is_active, default_language_code)
            VALUES
                (@tenant, @store, 'phase4c-basic', 'phase4c-basic@example.com', @password, 'phase4c', 'basic', true, 'en')
            ON CONFLICT (tenant_id, store_id, user_name)
            DO UPDATE SET password_hash = EXCLUDED.password_hash, is_active = true, last_password_reset_at = NULL
            """,
            connection);
        basicAdminCommand.Parameters.AddWithValue("tenant", "tenant-demo");
        basicAdminCommand.Parameters.AddWithValue("store", "default");
        basicAdminCommand.Parameters.AddWithValue("password", encodedPassword);
        await basicAdminCommand.ExecuteNonQueryAsync();

        await using var basicGroupCommand = new NpgsqlCommand(
            """
            INSERT INTO customer_identity.administrator_group_memberships(administrator_id, group_id)
            SELECT a.id, g.id
            FROM customer_identity.administrator_accounts a
            JOIN customer_identity.permission_groups g ON g.name = 'BASIC'
            WHERE a.tenant_id = 'tenant-demo' AND a.store_id = 'default' AND a.user_name = 'phase4c-basic'
            ON CONFLICT DO NOTHING;
            """,
            connection);
        await basicGroupCommand.ExecuteNonQueryAsync();

        await using var superGroupCommand = new NpgsqlCommand(
            """
            INSERT INTO customer_identity.permission_groups(name, group_type)
            VALUES ('SUPERADMIN', 'Administrator')
            ON CONFLICT (name) DO NOTHING;
            INSERT INTO customer_identity.administrator_group_memberships(administrator_id, group_id)
            SELECT a.id, g.id
            FROM customer_identity.administrator_accounts a
            JOIN customer_identity.permission_groups g ON g.name = 'SUPERADMIN'
            WHERE a.tenant_id = 'tenant-demo' AND a.store_id = 'default' AND a.user_name = 'phase4c-test'
            ON CONFLICT DO NOTHING;
            """,
            connection);
        await superGroupCommand.ExecuteNonQueryAsync();
    }

    private async Task EnsureTestCustomersAsync()
    {
        var connectionString = await _application!.GetConnectionStringAsync("customeridentitydb")
            ?? throw new InvalidOperationException("The customer identity database connection string is unavailable.");

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();

        var salt = RandomNumberGenerator.GetBytes(16);
        var hash = Rfc2898DeriveBytes.Pbkdf2("Phase4c!Password2026", salt, 120_000, HashAlgorithmName.SHA256, 32);
        var encodedPassword = $"PBKDF2-SHA256$120000${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";

        await using var command = new NpgsqlCommand(
            """
            INSERT INTO customer_identity.customer_options(id, store_id, code, option_type, is_public)
            VALUES ('00000000-0000-0000-0000-000000000001', 'default', 'PHASE4C_OPTION', 'TEXT', true)
            ON CONFLICT (id) DO NOTHING;
            INSERT INTO customer_identity.customer_option_values(id, option_id, store_id, code)
            VALUES ('00000000-0000-0000-0000-000000000001', '00000000-0000-0000-0000-000000000001', 'default', 'PHASE4C_VALUE')
            ON CONFLICT (id) DO NOTHING;

            INSERT INTO customer_identity.customer_accounts
                (id, tenant_id, store_id, login_name, email_address, password_hash, gender, company_name, provider, status, default_language_code)
            VALUES
                ('00000000-0000-0000-0000-000000000001', 'tenant-demo', 'default', 'phase4c-target', 'phase4c@example.com', @password, 'M', 'phase4c', 'phase4c', 'Active', 'en'),
                ('00000000-0000-0000-0000-000000000002', 'tenant-demo', 'default', 'phase4c-test', 'phase4c-login@example.com', @password, 'M', 'phase4c', 'phase4c', 'Active', 'en')
            ON CONFLICT (id) DO UPDATE SET
                password_hash = EXCLUDED.password_hash, status = 'Active',
                email_address = EXCLUDED.email_address, default_language_code = 'en',
                last_password_reset_at = NULL;

            INSERT INTO customer_identity.customer_addresses
                (customer_id, address_type, first_name, last_name, street_address, city, postal_code, country_code, zone_code)
            VALUES
                ('00000000-0000-0000-0000-000000000001', 'Billing', 'phase4c', 'target', '1 Main Street', 'Seattle', '98101', 'US', 'WA'),
                ('00000000-0000-0000-0000-000000000001', 'Delivery', 'phase4c', 'target', '1 Main Street', 'Seattle', '98101', 'US', 'WA'),
                ('00000000-0000-0000-0000-000000000002', 'Billing', 'phase4c', 'test', '1 Main Street', 'Seattle', '98101', 'US', 'WA'),
                ('00000000-0000-0000-0000-000000000002', 'Delivery', 'phase4c', 'test', '1 Main Street', 'Seattle', '98101', 'US', 'WA')
            ON CONFLICT (customer_id, address_type) DO UPDATE SET
                first_name = EXCLUDED.first_name, last_name = EXCLUDED.last_name,
                street_address = EXCLUDED.street_address, city = EXCLUDED.city,
                postal_code = EXCLUDED.postal_code, country_code = EXCLUDED.country_code,
                zone_code = EXCLUDED.zone_code;

            INSERT INTO customer_identity.customer_attributes(customer_id, option_id, option_value_id, text_value)
            VALUES
                ('00000000-0000-0000-0000-000000000001', '00000000-0000-0000-0000-000000000001', '00000000-0000-0000-0000-000000000001', 'phase4c-test'),
                ('00000000-0000-0000-0000-000000000002', '00000000-0000-0000-0000-000000000001', '00000000-0000-0000-0000-000000000001', 'phase4c-test')
            ON CONFLICT (customer_id, option_id) DO UPDATE SET text_value = EXCLUDED.text_value;
            """,
            connection);
        command.Parameters.AddWithValue("password", encodedPassword);
        await command.ExecuteNonQueryAsync();
    }

    public async Task EnsureTestCustomerAsync()
    {
        await EnsureTestCustomersAsync();
    }

    public async Task EnsureAuthenticatedTestCustomerAsync()
    {
        await EnsureTestCustomersAsync();
        CustomerAccessToken = await LoginAsync("phase4c-test", "Phase4c!Password2026", false);
    }

    public async Task EnsureAuthenticatedTestAdministratorAsync()
    {
        await EnsureTestAdministratorAsync();
        AdminAccessToken = await LoginAsync("phase4c-test", "Phase4c!Password2026", true);
    }

    public async Task EnsureAuthenticatedBasicAdministratorAsync()
    {
        await EnsureTestAdministratorAsync();
        BasicAdminAccessToken = await LoginAsync("phase4c-basic", "Phase4c!Password2026", true);
    }

    public async Task EnsureTestResetTokenAsync(string token, bool administrator)
    {
        var connectionString = await _application!.GetConnectionStringAsync("customeridentitydb")
            ?? throw new InvalidOperationException("The customer identity database connection string is unavailable.");

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

        await using var command = new NpgsqlCommand(
            """
            DELETE FROM customer_identity.credential_reset_tokens WHERE token_hash = @hash;
            INSERT INTO customer_identity.credential_reset_tokens
                (subject_type, customer_id, administrator_id, token_hash, tenant_id, store_id, expires_at)
            VALUES
                (@type::customer_identity.reset_subject_type,
                 CASE WHEN @administrator THEN NULL ELSE '00000000-0000-0000-0000-000000000002'::uuid END,
                 CASE WHEN @administrator THEN (SELECT id FROM customer_identity.administrator_accounts WHERE tenant_id = 'tenant-demo' AND store_id = 'default' AND user_name = 'phase4c-test') ELSE NULL END,
                 @hash, 'tenant-demo', 'default', now() + interval '2 days');
            """,
            connection);
        command.Parameters.AddWithValue("hash", hash);
        command.Parameters.AddWithValue("type", administrator ? "Administrator" : "Customer");
        command.Parameters.AddWithValue("administrator", administrator);
        await command.ExecuteNonQueryAsync();
    }

    private async Task<string> LoginAsync(string username, string password, bool administrator)
    {
        using var response = await CustomerIdentityClient.PostAsync(
            administrator ? "/api/v1/admin-auth/login" : "/api/v1/customer-auth/login",
            new StringContent(
                JsonSerializer.Serialize(new { username, password }),
                Encoding.UTF8,
                "application/json"));
        response.EnsureSuccessStatusCode();
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return document.RootElement.GetProperty("accessToken").GetString()
            ?? throw new InvalidOperationException("The authentication response did not contain an access token.");
    }

    public async ValueTask DisposeAsync()
    {
        if (_application is not null)
        {
            try
            {
                await CleanupTestDataAsync();
            }
            finally
            {
                await _application.DisposeAsync();
            }
        }
    }

    private async Task CleanupTestDataAsync()
    {
        var connectionString = await _application!.GetConnectionStringAsync("customeridentitydb")
            ?? throw new InvalidOperationException("The customer identity database connection string is unavailable.");

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        await using var command = new NpgsqlCommand(
            """
            DELETE FROM customer_identity.external_identity_connections
            WHERE user_id IN (
                SELECT id::text
                FROM customer_identity.customer_accounts
                WHERE tenant_id = 'tenant-demo' AND store_id = 'default'
                  AND (id IN (
                        '00000000-0000-0000-0000-000000000001'::uuid,
                        '00000000-0000-0000-0000-000000000002'::uuid)
                       OR email_address LIKE 'phase4c-%@example.com'
                       OR email_address = 'phase4c@example.com')
                UNION
                SELECT id::text
                FROM customer_identity.administrator_accounts
                WHERE tenant_id = 'tenant-demo' AND store_id = 'default'
                  AND (user_name LIKE 'phase4c-%' OR email_address LIKE 'phase4c-%@example.com')
            )
               OR (user_id = '00000000-0000-0000-0000-000000000001'
                   AND provider_id = '00000000-0000-0000-0000-000000000001');

            DELETE FROM customer_identity.customer_reviews
            WHERE reviewer_customer_id IN (
                    SELECT id
                    FROM customer_identity.customer_accounts
                    WHERE tenant_id = 'tenant-demo' AND store_id = 'default'
                      AND (id IN (
                            '00000000-0000-0000-0000-000000000001'::uuid,
                            '00000000-0000-0000-0000-000000000002'::uuid)
                           OR email_address LIKE 'phase4c-%@example.com'
                           OR email_address = 'phase4c@example.com')
                )
               OR reviewed_customer_id IN (
                    SELECT id
                    FROM customer_identity.customer_accounts
                    WHERE tenant_id = 'tenant-demo' AND store_id = 'default'
                      AND (id IN (
                            '00000000-0000-0000-0000-000000000001'::uuid,
                            '00000000-0000-0000-0000-000000000002'::uuid)
                           OR email_address LIKE 'phase4c-%@example.com'
                           OR email_address = 'phase4c@example.com')
                );

            DELETE FROM customer_identity.customer_attributes
            WHERE option_id = '00000000-0000-0000-0000-000000000001'::uuid;

            DELETE FROM customer_identity.credential_reset_tokens
            WHERE tenant_id = 'tenant-demo' AND store_id = 'default';

            DELETE FROM customer_identity.event_outbox
            WHERE tenant_id = 'tenant-demo' AND store_id = 'default'
              AND event_type = 'CustomerRegistered'
              AND payload->>'loginName' LIKE 'phase4c-%@example.com';

            DELETE FROM customer_identity.email_outbox
            WHERE tenant_id = 'tenant-demo' AND store_id = 'default'
              AND payload->>'returnUrl' = 'https://example.com/phase4c';

            DELETE FROM customer_identity.newsletter_subscriptions
            WHERE tenant_id = 'tenant-demo' AND store_id = 'default'
              AND campaign_code = 'NEWSLETTER'
              AND email_address = 'phase4c@example.com';

            DELETE FROM customer_identity.customer_accounts
            WHERE tenant_id = 'tenant-demo' AND store_id = 'default'
              AND (id IN (
                    '00000000-0000-0000-0000-000000000001'::uuid,
                    '00000000-0000-0000-0000-000000000002'::uuid)
                   OR email_address LIKE 'phase4c-%@example.com'
                   OR email_address = 'phase4c@example.com');

            DELETE FROM customer_identity.customer_option_values
            WHERE id = '00000000-0000-0000-0000-000000000001'::uuid
              AND store_id = 'default';

            DELETE FROM customer_identity.customer_options
            WHERE id = '00000000-0000-0000-0000-000000000001'::uuid
              AND store_id = 'default';

            DELETE FROM customer_identity.administrator_group_memberships
            WHERE administrator_id IN (
                SELECT id
                FROM customer_identity.administrator_accounts
                WHERE tenant_id = 'tenant-demo' AND store_id = 'default'
                  AND (user_name LIKE 'phase4c-%' OR email_address LIKE 'phase4c-%@example.com')
            );

            DELETE FROM customer_identity.administrator_accounts
            WHERE tenant_id = 'tenant-demo' AND store_id = 'default'
              AND (user_name LIKE 'phase4c-%' OR email_address LIKE 'phase4c-%@example.com');

            DELETE FROM customer_identity.permission_groups
            WHERE name IN ('ADMIN', 'BASIC', 'SUPERADMIN')
              AND NOT EXISTS (
                  SELECT 1
                  FROM customer_identity.administrator_group_memberships membership
                  WHERE membership.group_id = permission_groups.id
              )
              AND NOT EXISTS (
                  SELECT 1
                  FROM customer_identity.group_permissions permission
                  WHERE permission.group_id = permission_groups.id
              );
            """,
            connection,
            transaction);

        await command.ExecuteNonQueryAsync();
        await transaction.CommitAsync();
    }

    private static void ConfigureClient(HttpClient client, string tenant, string store, string correlation, bool authorization, string? language, string? idempotencyKey)
    {
        client.DefaultRequestHeaders.Add("x-tenant-id", tenant);
        client.DefaultRequestHeaders.Add("x-store-id", store);
        client.DefaultRequestHeaders.Add("x-correlation-id", correlation);
        if (language is not null)
        {
            client.DefaultRequestHeaders.Add("x-language", language);
        }

        if (idempotencyKey is not null)
        {
            client.DefaultRequestHeaders.Add("Idempotency-Key", idempotencyKey);
        }
    }
}

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class ShopizerAspireCollection : ICollectionFixture<AspireHostFixture>
{
    public const string Name = "Shopizer Aspire";
}
