using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net;
using System.Net.Sockets;
using Npgsql;

namespace Shopizer.IntegrationTests.Fixtures;

public sealed class AspireHostFixture : IAsyncLifetime
{
    private DistributedApplication? _application;
    private TcpListener? _platformProviderListener;
    private CancellationTokenSource? _platformProviderCancellation;
    private Task? _platformProviderLoop;
    private string? _platformProviderUri;

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
    public string TestTenantAdminAccessToken { get; private set; } = null!;
    public string TaxAdminAccessToken { get; private set; } = null!;
    public string PricingAdminAccessToken { get; private set; } = null!;
    public string ContentAdminAccessToken { get; private set; } = null!;
    public string BasicAdminAccessToken { get; private set; } = null!;
    public string CustomerAccessToken { get; private set; } = null!;
    public string CartCheckoutCustomerAccessToken { get; private set; } = null!;

    public async ValueTask InitializeAsync()
    {
        var builder = await DistributedApplicationTestingBuilder.CreateAsync<Projects.Shopizer_AppHost>(
            [
                "DcpPublisher:RandomizePorts=true"
            ]);

        _application = await builder.BuildAsync();
        await _application.StartAsync();
        StartPlatformProviderStub();
        await EnsureTestAdministratorAsync();
        await EnsureTestCustomersAsync();

        var clients = new Dictionary<string, (Action<HttpClient> Assign, string Tenant, string Store, string Correlation, bool Authorization, string? Language, string? IdempotencyKey)>
        {
            ["customer-identity"] = (client => CustomerIdentityClient = client, "tenant-demo", "default", "00000000-0000-0000-0000-000000000001", false, null, null),
            ["catalog-product"] = (client => CatalogProductClient = client, "test-tenant-001", "test-store-001", "11111111-1111-4111-8111-111111111111", false, null, "phase4c-test"),
            ["search"] = (client => SearchClient = client, "tenant-demo", "default", "corr-ms03-0001", false, null, null),
            ["cart-checkout"] = (client => CartCheckoutClient = client, "test-tenant-001", "test-store-001", "00000000-0000-0000-0000-000000000001", false, null, null),
            ["order-management"] = (client => OrderManagementClient = client, "tenant-demo", "default", "corr-ms05-001", true, null, null),
            ["payments"] = (client => PaymentsClient = client, "test-tenant-001", "test-store-001", "corr-ms06-001", true, null, "phase4c-payment"),
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

        await EnsureTestCatalogAsync();
        await EnsureTestPricingAsync();
        await EnsureCartCheckoutTaxAsync();
        await EnsureCartCheckoutCustomerAsync();
        await EnsureTestContentConfigurationAsync();
        await EnsureTaxComprehensiveDataAsync();
        AdminAccessToken = await LoginAsync("phase4c-test", "Phase4c!Password2026", true);
        BasicAdminAccessToken = await LoginAsync("phase4c-basic", "Phase4c!Password2026", true);
        CustomerAccessToken = await LoginAsync("phase4c-test", "Phase4c!Password2026", false);
        var cartCustomerClient = _application.CreateHttpClient("customer-identity");
        ConfigureClient(cartCustomerClient, "test-tenant-001", "test-store-001", "00000000-0000-0000-0000-000000000001", false, null, null);
        CartCheckoutCustomerAccessToken = await LoginAsync(cartCustomerClient, "phase4c-cart-test", "Phase4c!Password2026", false);
        TestTenantAdminAccessToken = await LoginForContextAsync("test-tenant-001", "test-store-001");
        TaxAdminAccessToken = await LoginForContextAsync("tenant-001", "store-001");
        PricingAdminAccessToken = await LoginForContextAsync("2e6d7b63-5b1d-4f8a-8e12-8cf43c9f2001", "store-us-east");
        ContentAdminAccessToken = await LoginForContextAsync(
            "00000000-0000-0000-0000-000000000001", "00000000-0000-0000-0000-000000000002");
        await EnsureCartCheckoutShippingAsync();
    }

    private async Task EnsureTestContentConfigurationAsync()
    {
        var connectionString = await _application!.GetConnectionStringAsync("shopizerDb")
            ?? throw new InvalidOperationException("The shopizer database connection string is unavailable.");

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            """
            INSERT INTO content_configuration.content
                (content_id, tenant_id, store_id, code, content_type, content_position,
                 link_to_menu, product_group, sort_order, visible, modified_by)
            VALUES
                ('00000000-0000-0000-0000-000000000001'::uuid,
                 '00000000-0000-0000-0000-000000000001'::uuid,
                 '00000000-0000-0000-0000-000000000002'::uuid,
                 'phase4c-page', 'PAGE', NULL, true, NULL, 1, true, 'fixture'),
                ('00000000-0000-0000-0000-000000000002'::uuid,
                 '00000000-0000-0000-0000-000000000001'::uuid,
                 '00000000-0000-0000-0000-000000000002'::uuid,
                 'phase4c-box', 'BOX', 'LEFT', false, NULL, 2, true, 'fixture')
            ON CONFLICT (content_id) DO UPDATE SET
                tenant_id = EXCLUDED.tenant_id, store_id = EXCLUDED.store_id,
                code = EXCLUDED.code, content_type = EXCLUDED.content_type,
                content_position = EXCLUDED.content_position, link_to_menu = EXCLUDED.link_to_menu,
                sort_order = EXCLUDED.sort_order, visible = EXCLUDED.visible, modified_by = EXCLUDED.modified_by;

            INSERT INTO content_configuration.content_description
                (content_id, language_code, name, title, description, friendly_url,
                 meta_keywords, meta_title, meta_description)
            VALUES
                ('00000000-0000-0000-0000-000000000001'::uuid, 'en',
                 'Phase 4c page', 'Phase 4c page', 'A seeded page',
                 'phase4c-page', 'phase4c', 'Phase 4c', 'Seeded page'),
                ('00000000-0000-0000-0000-000000000002'::uuid, 'en',
                 'Phase 4c box', 'Phase 4c box', 'A seeded box',
                 'phase4c-box', 'phase4c', 'Phase 4c', 'Seeded box')
            ON CONFLICT (content_id, language_code) DO UPDATE SET
                name = EXCLUDED.name, title = EXCLUDED.title, description = EXCLUDED.description,
                friendly_url = EXCLUDED.friendly_url, meta_keywords = EXCLUDED.meta_keywords,
                meta_title = EXCLUDED.meta_title, meta_description = EXCLUDED.meta_description;

            INSERT INTO content_configuration.merchant_configuration
                (tenant_id, store_id, config_key, configuration_type, active, value, modified_by)
            VALUES
                ('00000000-0000-0000-0000-000000000001'::uuid,
                 '00000000-0000-0000-0000-000000000002'::uuid,
                 'CONFIG', 'CONFIG', false,
                 '{"displayCustomerSection":true,"displayContactUs":true,"displayPagesMenu":true,"allowPurchaseItems":true}',
                 'fixture'),
                ('00000000-0000-0000-0000-000000000001'::uuid,
                 '00000000-0000-0000-0000-000000000002'::uuid,
                 'facebook_page_url', 'SOCIAL', false,
                 'https://example.com/phase4c', 'fixture')
            ON CONFLICT (tenant_id, store_id, config_key) DO UPDATE SET
                configuration_type = EXCLUDED.configuration_type, active = EXCLUDED.active,
                value = EXCLUDED.value, modified_by = EXCLUDED.modified_by;

            INSERT INTO content_configuration.module_configuration
                (module_configuration_id, module_family, code, module_type, image, custom_module,
                 regions, configuration, details)
            VALUES
                ('00000000-0000-0000-0000-000000000011'::uuid, 'PAYMENT', 'phase4c-payment',
                 'payment', 'phase4c-payment.png', false, '["*"]'::jsonb,
                 '[{"env":"TEST","scheme":"https","host":"test.example","port":"443","uri":"https://test.example","config1":"test-url","config2":"test-token"},{"env":"PROD","scheme":"https","host":"prod.example","port":"443","uri":"https://prod.example","config1":"prod-url","config2":"prod-token"}]'::jsonb,
                 '{"configurable":true,"requiredKeys":["secretKey","publishableKey"]}'::jsonb),
                ('00000000-0000-0000-0000-000000000012'::uuid, 'SHIPPING', 'phase4c-shipping',
                 'shipping', 'phase4c-shipping.png', false, '["*"]'::jsonb,
                 '[{"env":"TEST","scheme":"https","host":"shipping.example","port":"443","uri":"https://shipping.example","config1":"shipping-url","config2":"shipping-token"}]'::jsonb,
                 '{"configurable":true,"requiredKeys":["account"]}'::jsonb)
            ON CONFLICT (code) DO UPDATE SET
                module_family = EXCLUDED.module_family, module_type = EXCLUDED.module_type,
                image = EXCLUDED.image, regions = EXCLUDED.regions,
                configuration = EXCLUDED.configuration, details = EXCLUDED.details;
            """,
            connection);
        await command.ExecuteNonQueryAsync();
    }

    private async Task EnsureTestAdministratorAsync()
    {
        var connectionString = await _application!.GetConnectionStringAsync("shopizerDb")
            ?? throw new InvalidOperationException("The shopizer database connection string is unavailable.");

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
        await EnsureContextAdministratorsAsync(connection, encodedPassword);
    }

    private static async Task EnsureContextAdministratorsAsync(NpgsqlConnection connection, string encodedPassword)
    {
        await using var command = new NpgsqlCommand(
            """
            INSERT INTO customer_identity.administrator_accounts
                (tenant_id, store_id, user_name, email_address, password_hash, first_name, last_name, is_active, default_language_code)
            VALUES
                ('test-tenant-001', 'test-store-001', 'phase4c-test', 'phase4c-test@test.example', @password, 'phase4c', 'test', true, 'en'),
                ('tenant-001', 'store-001', 'phase4c-test', 'phase4c-test@tax.example', @password, 'phase4c', 'test', true, 'en'),
                ('2e6d7b63-5b1d-4f8a-8e12-8cf43c9f2001', 'store-us-east', 'phase4c-test', 'phase4c-test@pricing.example', @password, 'phase4c', 'test', true, 'en'),
                ('00000000-0000-0000-0000-000000000001', '00000000-0000-0000-0000-000000000002', 'phase4c-test', 'phase4c-test@content.example', @password, 'phase4c', 'test', true, 'en')
            ON CONFLICT (tenant_id, store_id, user_name)
            DO UPDATE SET password_hash = EXCLUDED.password_hash, is_active = true, last_password_reset_at = NULL;

            INSERT INTO customer_identity.administrator_group_memberships(administrator_id, group_id)
            SELECT a.id, g.id
            FROM customer_identity.administrator_accounts a
            JOIN customer_identity.permission_groups g ON g.name = 'ADMIN'
            WHERE a.user_name = 'phase4c-test'
              AND (a.tenant_id, a.store_id) IN (
                  ('test-tenant-001', 'test-store-001'),
                  ('tenant-001', 'store-001'),
                  ('2e6d7b63-5b1d-4f8a-8e12-8cf43c9f2001', 'store-us-east'),
                  ('00000000-0000-0000-0000-000000000001', '00000000-0000-0000-0000-000000000002'))
            ON CONFLICT DO NOTHING;
            """,
            connection);
        command.Parameters.AddWithValue("password", encodedPassword);
        await command.ExecuteNonQueryAsync();
    }

    private async Task EnsureTestCustomersAsync()
    {
        var connectionString = await _application!.GetConnectionStringAsync("shopizerDb")
            ?? throw new InvalidOperationException("The shopizer database connection string is unavailable.");

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

    private async Task EnsureCartCheckoutTaxAsync()
    {
        var connectionString = await _application!.GetConnectionStringAsync("shopizerDb")
            ?? throw new InvalidOperationException("The shopizer database connection string is unavailable.");

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            """
            INSERT INTO tax_schema.tax_classes (id, tenant_id, store_id, code, title)
            VALUES ('00000000-0000-0000-0000-000000000021',
                    'test-tenant-001', 'test-store-001', 'DEFAULT', 'Default')
            ON CONFLICT (tenant_id, store_id, code)
            DO UPDATE SET title = EXCLUDED.title, updated_at = current_timestamp;

            INSERT INTO tax_schema.tax_configurations
                (id, tenant_id, store_id, tax_basis, collect_tax_if_different_province, different_country_behavior)
            VALUES ('00000000-0000-0000-0000-000000000021',
                    'test-tenant-001', 'test-store-001', 'BillingAddress', true, 'UseCustomerJurisdiction')
            ON CONFLICT (tenant_id, store_id)
            DO UPDATE SET tax_basis = EXCLUDED.tax_basis,
                          collect_tax_if_different_province = EXCLUDED.collect_tax_if_different_province,
                          different_country_behavior = EXCLUDED.different_country_behavior,
                          updated_at = current_timestamp;
            """,
            connection);
        await command.ExecuteNonQueryAsync();
    }

    private async Task EnsureTaxComprehensiveDataAsync()
    {
        var connectionString = await _application!.GetConnectionStringAsync("shopizerDb")
            ?? throw new InvalidOperationException("The shopizer database connection string is unavailable.");

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            """
            INSERT INTO tax_schema.tax_classes (id, tenant_id, store_id, code, title)
            VALUES ('00000000-0000-0000-0000-000000000001', 'tenant-001', 'store-001', 'DEFAULT', 'Default')
            ON CONFLICT (id) DO UPDATE SET tenant_id = EXCLUDED.tenant_id,
                                           store_id = EXCLUDED.store_id,
                                           code = EXCLUDED.code,
                                           title = EXCLUDED.title,
                                           updated_at = current_timestamp;

            INSERT INTO tax_schema.tax_rates
                (id, tenant_id, store_id, tax_class_id, code, rate_percent, priority, piggyback,
                 country_code, zone_code, state_province)
            VALUES ('00000000-0000-0000-0000-000000000001',
                    'tenant-001', 'store-001',
                    '00000000-0000-0000-0000-000000000001',
                    'tax-base', 10.5, 1, true, 'CA', 'QC', 'QC')
            ON CONFLICT (id) DO UPDATE SET tenant_id = EXCLUDED.tenant_id,
                                           store_id = EXCLUDED.store_id,
                                           tax_class_id = EXCLUDED.tax_class_id,
                                           code = EXCLUDED.code,
                                           rate_percent = EXCLUDED.rate_percent,
                                           priority = EXCLUDED.priority,
                                           piggyback = EXCLUDED.piggyback,
                                           country_code = EXCLUDED.country_code,
                                           zone_code = EXCLUDED.zone_code,
                                           state_province = EXCLUDED.state_province,
                                           updated_at = current_timestamp;

            INSERT INTO tax_schema.tax_rate_descriptions
                (id, tax_rate_id, language_code, name, title, description)
            VALUES ('00000000-0000-0000-0000-000000000002',
                    '00000000-0000-0000-0000-000000000001',
                    'en', 'Tax base', 'Tax base', 'Tax base')
            ON CONFLICT (tax_rate_id, language_code)
            DO UPDATE SET name = EXCLUDED.name, title = EXCLUDED.title,
                          description = EXCLUDED.description, updated_at = current_timestamp;

            INSERT INTO tax_schema.tax_configurations
                (id, tenant_id, store_id, tax_basis, collect_tax_if_different_province,
                 different_country_behavior)
            VALUES ('00000000-0000-0000-0000-000000000003',
                    'tenant-001', 'store-001', 'ShippingAddress', true, 'UseCustomerJurisdiction')
            ON CONFLICT (tenant_id, store_id)
            DO UPDATE SET tax_basis = EXCLUDED.tax_basis,
                          collect_tax_if_different_province = EXCLUDED.collect_tax_if_different_province,
                          different_country_behavior = EXCLUDED.different_country_behavior,
                          updated_at = current_timestamp;
            """,
            connection);
        await command.ExecuteNonQueryAsync();
    }

    public async Task PrepareTaxRequestAsync(HttpMethod method, string path, int expectedStatus)
    {
        if (expectedStatus is (int)HttpStatusCode.BadRequest or (int)HttpStatusCode.Unauthorized)
        {
            return;
        }

        await EnsureTaxComprehensiveDataAsync();
        if (method == HttpMethod.Delete &&
            path.StartsWith("/api/v1/tax-classes/", StringComparison.Ordinal))
        {
            await using var connection = new NpgsqlConnection(
                await _application!.GetConnectionStringAsync("shopizerDb")
                ?? throw new InvalidOperationException("The shopizer database connection string is unavailable."));
            await connection.OpenAsync();
            await using var command = new NpgsqlCommand(
                "DELETE FROM tax_schema.tax_rates WHERE tax_class_id = '00000000-0000-0000-0000-000000000001'",
                connection);
            await command.ExecuteNonQueryAsync();
        }
    }

    private async Task EnsureCartCheckoutShippingAsync()
    {
        using var client = _application!.CreateHttpClient("shipping");
        ConfigureClient(client, "test-tenant-001", "test-store-001", "corr-ms09-cart-checkout", false, null, null);

        using var origin = new HttpRequestMessage(HttpMethod.Post, "/api/v1/private/shipping/origin")
        {
            Content = JsonContent.Create(new
            {
                address = "1 Main Street",
                city = "Montreal",
                postalCode = "H2Y 1C6",
                state = "QC",
                countryCode = "CA",
                active = true
            })
        };
        origin.Headers.Authorization = new AuthenticationHeaderValue("Bearer", TestTenantAdminAccessToken);
        using var originResponse = await client.SendAsync(origin);
        originResponse.EnsureSuccessStatusCode();

        using var module = new HttpRequestMessage(HttpMethod.Post, "/api/v1/private/modules/shipping")
        {
            Content = JsonContent.Create(new
            {
                moduleCode = "usps",
                active = true,
                defaultSelected = true,
                environment = "Test",
                integrationKeys = new Dictionary<string, object?>
                {
                    ["productVirtual"] = "false",
                    ["productWeight"] = 1m,
                    ["price"] = 5m
                },
                integrationOptions = new Dictionary<string, object?>()
            })
        };
        module.Headers.Authorization = new AuthenticationHeaderValue("Bearer", TestTenantAdminAccessToken);
        using var moduleResponse = await client.SendAsync(module);
        moduleResponse.EnsureSuccessStatusCode();
    }

    public async Task PrepareShippingRequestAsync(HttpMethod method, string path, int expectedStatus)
    {
        if (expectedStatus is (int)HttpStatusCode.BadRequest or (int)HttpStatusCode.Unauthorized ||
            !path.Contains("/private/shipping/package", StringComparison.Ordinal))
        {
            return;
        }

        using var client = _application!.CreateHttpClient("shipping");
        ConfigureClient(client, "test-tenant-001", "test-store-001", "corr-ms09-test", false, null, null);
        using var package = new HttpRequestMessage(HttpMethod.Post, "/api/v1/private/shipping/package")
        {
            Content = JsonContent.Create(new
            {
                code = "phase4c-test",
                shippingWidth = 10.5m,
                shippingHeight = 10.5m,
                shippingLength = 10.5m,
                shippingWeight = 10.5m,
                shippingMaxWeight = 10.5m,
                treshold = 1,
                type = "Item",
                defaultPackaging = true
            })
        };
        package.Headers.Authorization = new AuthenticationHeaderValue("Bearer", TestTenantAdminAccessToken);
        using var response = await client.SendAsync(package);
        response.EnsureSuccessStatusCode();
    }

    public async Task ResetCartCheckoutDataAsync()
    {
        var connectionString = await _application!.GetConnectionStringAsync("shopizerDb")
            ?? throw new InvalidOperationException("The shopizer database connection string is unavailable.");

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            """
            DELETE FROM cart_checkout_schema.ms04_inbox;
            DELETE FROM cart_checkout_schema.ms04_outbox;
            DELETE FROM cart_checkout_schema.cart_quote_reference;
            DELETE FROM cart_checkout_schema.checkout_idempotency_key;
            DELETE FROM cart_checkout_schema.checkout_submission;
            DELETE FROM cart_checkout_schema.checkout_total_snapshot;
            DELETE FROM cart_checkout_schema.checkout_line_snapshot;
            DELETE FROM cart_checkout_schema.checkout_session;
            DELETE FROM cart_checkout_schema.shopping_cart_attr_item;
            DELETE FROM cart_checkout_schema.shopping_cart_item;
            DELETE FROM cart_checkout_schema.shopping_cart;
            """,
            connection);
        await command.ExecuteNonQueryAsync();
    }

    private async Task EnsureCartCheckoutCustomerAsync()
    {
        var connectionString = await _application!.GetConnectionStringAsync("shopizerDb")
            ?? throw new InvalidOperationException("The shopizer database connection string is unavailable.");

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        var salt = RandomNumberGenerator.GetBytes(16);
        var hash = Rfc2898DeriveBytes.Pbkdf2("Phase4c!Password2026", salt, 120_000, HashAlgorithmName.SHA256, 32);
        var encodedPassword = $"PBKDF2-SHA256$120000${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";

        await using var command = new NpgsqlCommand(
            """
            INSERT INTO customer_identity.customer_accounts
                (id, tenant_id, store_id, login_name, email_address, password_hash, gender, company_name, provider, status, default_language_code)
            VALUES
                ('00000000-0000-0000-0000-000000000003', 'test-tenant-001', 'test-store-001',
                 'phase4c-cart-test', 'phase4c-cart@example.test', @password, 'M', 'phase4c',
                 'phase4c', 'Active', 'en')
            ON CONFLICT (id) DO UPDATE SET
                tenant_id = EXCLUDED.tenant_id, store_id = EXCLUDED.store_id,
                login_name = EXCLUDED.login_name, email_address = EXCLUDED.email_address,
                password_hash = EXCLUDED.password_hash, status = 'Active',
                default_language_code = 'en', last_password_reset_at = NULL;

            INSERT INTO customer_identity.customer_addresses
                (customer_id, address_type, first_name, last_name, street_address, city, postal_code, country_code, zone_code)
            VALUES
                ('00000000-0000-0000-0000-000000000003', 'Billing', 'Ada', 'Lovelace', '1 Main St', 'Montreal', 'H2Y 1C6', 'CA', NULL),
                ('00000000-0000-0000-0000-000000000003', 'Delivery', 'Ada', 'Lovelace', '1 Main St', 'Montreal', 'H2Y 1C6', 'CA', NULL)
            ON CONFLICT (customer_id, address_type) DO UPDATE SET
                first_name = EXCLUDED.first_name, last_name = EXCLUDED.last_name,
                street_address = EXCLUDED.street_address, city = EXCLUDED.city,
                postal_code = EXCLUDED.postal_code, country_code = EXCLUDED.country_code,
                zone_code = EXCLUDED.zone_code;
            """,
            connection);
        command.Parameters.AddWithValue("password", encodedPassword);
        await command.ExecuteNonQueryAsync();
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
        var connectionString = await _application!.GetConnectionStringAsync("shopizerDb")
            ?? throw new InvalidOperationException("The shopizer database connection string is unavailable.");

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

    private async Task EnsureTestCatalogAsync(bool includeReservation = true)
    {
        const string tenant = "test-tenant-001";
        const string store = "test-store-001";
        const string resourceId = "00000000-0000-0000-0000-000000000001";
        const string moveParentId = "00000000-0000-0000-0000-000000000002";
        const string secondAvailabilityId = "00000000-0000-0000-0000-000000000002";
        const string reservationPayload = """{"reservationKey":"phase4c-test","variantId":"00000000-0000-0000-0000-000000000001","availabilityId":"00000000-0000-0000-0000-000000000001","regionCode":"phase4c-test","quantity":1,"expiresAt":"2026-09-02T00:00:00Z"}""";

        await CleanupCatalogTestDataAsync();

        var connectionString = await _application!.GetConnectionStringAsync("shopizerDb")
            ?? throw new InvalidOperationException("The shopizer database connection string is unavailable.");

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        await using var command = new NpgsqlCommand(
            """
            INSERT INTO catalog_product.category
                (id, tenant_id, store_id, code, sort_order, status, visible, featured, depth, lineage)
            VALUES
                (@resource_id, @tenant, @store, 'phase4c-parent', 0, 'Active', true, true, 0, '/' || @resource_id || '/')
            ON CONFLICT (id) DO UPDATE SET
                tenant_id = EXCLUDED.tenant_id,
                store_id = EXCLUDED.store_id,
                code = EXCLUDED.code,
                parent_id = NULL,
                sort_order = EXCLUDED.sort_order,
                status = EXCLUDED.status,
                visible = EXCLUDED.visible,
                featured = EXCLUDED.featured,
                depth = EXCLUDED.depth,
                lineage = EXCLUDED.lineage;

            INSERT INTO catalog_product.category_description
                (category_id, language_code, name, friendly_url, description, title, meta_description)
            VALUES
                (@resource_id, 'en', 'phase4c-parent', 'phase4c-value', 'phase4c-test', 'phase4c-test', 'phase4c-test')
            ON CONFLICT (category_id, language_code) DO UPDATE SET
                name = EXCLUDED.name,
                friendly_url = EXCLUDED.friendly_url,
                description = EXCLUDED.description,
                title = EXCLUDED.title,
                meta_description = EXCLUDED.meta_description;

            INSERT INTO catalog_product.category
                (id, tenant_id, store_id, code, sort_order, status, visible, featured, depth, lineage)
            VALUES
                (@move_parent_id, @tenant, @store, 'phase4c-move-parent', 1, 'Active', true, false, 0, '/' || @move_parent_id || '/')
            ON CONFLICT (id) DO UPDATE SET
                tenant_id = EXCLUDED.tenant_id,
                store_id = EXCLUDED.store_id,
                code = EXCLUDED.code,
                parent_id = NULL,
                sort_order = EXCLUDED.sort_order,
                status = EXCLUDED.status,
                visible = EXCLUDED.visible,
                featured = EXCLUDED.featured,
                depth = EXCLUDED.depth,
                lineage = EXCLUDED.lineage;

            INSERT INTO catalog_product.category_description
                (category_id, language_code, name, friendly_url, description, title, meta_description)
            VALUES
                (@move_parent_id, 'en', 'phase4c-move-parent', 'phase4c-move-parent', 'phase4c-test', 'phase4c-test', 'phase4c-test')
            ON CONFLICT (category_id, language_code) DO UPDATE SET
                name = EXCLUDED.name,
                friendly_url = EXCLUDED.friendly_url,
                description = EXCLUDED.description,
                title = EXCLUDED.title,
                meta_description = EXCLUDED.meta_description;

            INSERT INTO catalog_product.product
                (id, tenant_id, store_id, sku, ref_sku, status, visible, available, can_be_purchased,
                 date_available, manufacturer_code, product_type_code, tax_class_code, product_virtual,
                 product_shippable, product_free, sort_order)
            VALUES
                (@resource_id, @tenant, @store, 'phase4c-sku', 'phase4c-ref-sku', 'Active', true, true, true,
                 now() - interval '1 day', 'phase4c-test', 'phase4c-test', 'phase4c-test', false, true, true, 1)
            ON CONFLICT (id) DO UPDATE SET
                tenant_id = EXCLUDED.tenant_id,
                store_id = EXCLUDED.store_id,
                sku = EXCLUDED.sku,
                ref_sku = EXCLUDED.ref_sku,
                status = EXCLUDED.status,
                visible = EXCLUDED.visible,
                available = EXCLUDED.available,
                can_be_purchased = EXCLUDED.can_be_purchased,
                date_available = EXCLUDED.date_available,
                manufacturer_code = EXCLUDED.manufacturer_code,
                product_type_code = EXCLUDED.product_type_code,
                tax_class_code = EXCLUDED.tax_class_code,
                product_virtual = EXCLUDED.product_virtual,
                product_shippable = EXCLUDED.product_shippable,
                product_free = EXCLUDED.product_free,
                sort_order = EXCLUDED.sort_order,
                version = 0;

            INSERT INTO catalog_product.product_description
                (product_id, language_code, name, friendly_url, description, highlights, title, keywords, meta_description)
            VALUES
                (@resource_id, 'en', 'phase4c-product', 'phase4c-value', 'phase4c-test', 'phase4c-test',
                 'phase4c-test', 'phase4c-test', 'phase4c-test')
            ON CONFLICT (product_id, language_code) DO UPDATE SET
                name = EXCLUDED.name,
                friendly_url = EXCLUDED.friendly_url,
                description = EXCLUDED.description,
                highlights = EXCLUDED.highlights,
                title = EXCLUDED.title,
                keywords = EXCLUDED.keywords,
                meta_description = EXCLUDED.meta_description;

            INSERT INTO catalog_product.product_variant
                (id, product_id, store_id, sku, code, status, available, default_selection, date_available, sort_order)
            VALUES
                (@resource_id, @resource_id, @store, 'phase4c-sku', 'phase4c-value', 'Active', true, true,
                 now() - interval '1 day', 0)
            ON CONFLICT (id) DO UPDATE SET
                product_id = EXCLUDED.product_id,
                store_id = EXCLUDED.store_id,
                sku = EXCLUDED.sku,
                code = EXCLUDED.code,
                status = EXCLUDED.status,
                available = EXCLUDED.available,
                default_selection = EXCLUDED.default_selection,
                date_available = EXCLUDED.date_available,
                sort_order = EXCLUDED.sort_order;

            INSERT INTO catalog_product.product_availability
                (id, variant_id, store_id, region_code, quantity, reserved_quantity, active)
            VALUES
                (@resource_id, @resource_id, @store, 'phase4c-test', 10, 0, true)
            ON CONFLICT (id) DO UPDATE SET
                product_id = NULL,
                variant_id = EXCLUDED.variant_id,
                store_id = EXCLUDED.store_id,
                region_code = EXCLUDED.region_code,
                quantity = EXCLUDED.quantity,
                reserved_quantity = 0,
                active = EXCLUDED.active;

            INSERT INTO catalog_product.product_availability
                (id, product_id, store_id, region_code, quantity, reserved_quantity, active)
            VALUES
                (@second_availability_id, @resource_id, @store, '*', 10, 0, true)
            ON CONFLICT (id) DO UPDATE SET
                product_id = EXCLUDED.product_id,
                variant_id = NULL,
                store_id = EXCLUDED.store_id,
                region_code = EXCLUDED.region_code,
                quantity = EXCLUDED.quantity,
                reserved_quantity = 0,
                active = EXCLUDED.active;

            INSERT INTO catalog_product.product_price
                (id, availability_id, store_id, currency_code, amount, price_type, default_price)
            VALUES
                (@resource_id, @resource_id, @store, 'USD', 19.99, 'OneTime', true)
            ON CONFLICT (id) DO UPDATE SET
                availability_id = EXCLUDED.availability_id,
                store_id = EXCLUDED.store_id,
                currency_code = EXCLUDED.currency_code,
                amount = EXCLUDED.amount,
                price_type = EXCLUDED.price_type,
                default_price = EXCLUDED.default_price;

            INSERT INTO catalog_product.product_option
                (id, store_id, code, option_type, display_only, sort_order)
            VALUES
                (@resource_id, @store, 'phase4c-option', 'TEXT', false, 0)
            ON CONFLICT (id) DO UPDATE SET
                store_id = EXCLUDED.store_id,
                code = EXCLUDED.code,
                option_type = EXCLUDED.option_type,
                display_only = EXCLUDED.display_only,
                sort_order = EXCLUDED.sort_order;

            INSERT INTO catalog_product.product_option_value
                (id, option_id, store_id, code, display_only, sort_order)
            VALUES
                (@resource_id, @resource_id, @store, 'phase4c-value', false, 0)
            ON CONFLICT (id) DO UPDATE SET
                option_id = EXCLUDED.option_id,
                store_id = EXCLUDED.store_id,
                code = EXCLUDED.code,
                display_only = EXCLUDED.display_only,
                sort_order = EXCLUDED.sort_order;

            INSERT INTO catalog_product.product_attribute
                (id, product_id, option_id, option_value_id, display_only, price_adjustment, default_selection)
            VALUES
                (@resource_id, @resource_id, @resource_id, @resource_id, false, 2.50, true)
            ON CONFLICT (id) DO UPDATE SET
                product_id = EXCLUDED.product_id,
                option_id = EXCLUDED.option_id,
                option_value_id = EXCLUDED.option_value_id,
                display_only = EXCLUDED.display_only,
                price_adjustment = EXCLUDED.price_adjustment,
                default_selection = EXCLUDED.default_selection;

            INSERT INTO catalog_product.product_image
                (id, product_id, image_type, file_name, original_uri, external_url, default_image, media_status)
            VALUES
                (@resource_id, @resource_id, 'ExternalUrl', 'phase4c-image.jpg',
                 'https://example.com/phase4c-image.jpg', 'https://example.com/phase4c-image.jpg', true, 'Ready')
            ON CONFLICT (id) DO UPDATE SET
                product_id = EXCLUDED.product_id,
                image_type = EXCLUDED.image_type,
                file_name = EXCLUDED.file_name,
                original_uri = EXCLUDED.original_uri,
                external_url = EXCLUDED.external_url,
                default_image = EXCLUDED.default_image,
                media_status = EXCLUDED.media_status;

            INSERT INTO catalog_product.inventory_reservation
                (id, tenant_id, store_id, product_id, variant_id, availability_id, reservation_key,
                 request_hash, quantity, state, expires_at)
            VALUES
                (@resource_id, @tenant, @store, @resource_id, @resource_id, @resource_id, 'phase4c-fixture',
                 upper(encode(digest(@reservation_payload, 'sha256'), 'hex')), 1, 'Held', now() + interval '1 day')
            ON CONFLICT (id) DO UPDATE SET
                tenant_id = EXCLUDED.tenant_id,
                store_id = EXCLUDED.store_id,
                product_id = EXCLUDED.product_id,
                variant_id = EXCLUDED.variant_id,
                availability_id = EXCLUDED.availability_id,
                reservation_key = EXCLUDED.reservation_key,
                request_hash = EXCLUDED.request_hash,
                quantity = EXCLUDED.quantity,
                state = 'Held',
                expires_at = EXCLUDED.expires_at,
                committed_at = NULL,
                released_at = NULL;
            """,
            connection,
            transaction);
        command.Parameters.AddWithValue("tenant", tenant);
        command.Parameters.AddWithValue("store", store);
        command.Parameters.AddWithValue("resource_id", Guid.Parse(resourceId));
        command.Parameters.AddWithValue("move_parent_id", Guid.Parse(moveParentId));
        command.Parameters.AddWithValue("second_availability_id", Guid.Parse(secondAvailabilityId));
        command.Parameters.AddWithValue("reservation_payload", reservationPayload);
        await command.ExecuteNonQueryAsync();
        await transaction.CommitAsync();

        if (!includeReservation)
        {
            await using var reservationCleanup = new NpgsqlCommand(
                """
                DELETE FROM catalog_product.inventory_reservation
                WHERE tenant_id = @tenant AND store_id = @store;
                """,
                connection);
            reservationCleanup.Parameters.AddWithValue("tenant", tenant);
            reservationCleanup.Parameters.AddWithValue("store", store);
            await reservationCleanup.ExecuteNonQueryAsync();
        }
    }

    private async Task EnsureTestPricingAsync()
    {
        var connectionString = await _application!.GetConnectionStringAsync("shopizerDb")
            ?? throw new InvalidOperationException("The shopizer database connection string is unavailable.");

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            """
            INSERT INTO pricing_promotions.price_list
                (price_list_id, tenant_id, store_id, name, currency_code, is_active)
            VALUES
                ('00000000-0000-0000-0000-000000000010',
                 '2e6d7b63-5b1d-4f8a-8e12-8cf43c9f2001', 'store-us-east', 'DEFAULT', 'USD', true)
            ON CONFLICT (tenant_id, store_id, currency_code, name)
            DO UPDATE SET is_active = true, updated_at = current_timestamp;

            INSERT INTO pricing_promotions.price_entry
                (price_entry_id, price_list_id, product_sku, variant_sku, availability_id,
                 code, amount, price_type, is_default, product_identifier_id)
            SELECT
                '00000000-0000-0000-0000-000000000001', price_list_id, 'phase4c-sku', NULL, 1,
                'seed', 19.99, 'OneTime', false, 1
            FROM pricing_promotions.price_list
            WHERE tenant_id = '2e6d7b63-5b1d-4f8a-8e12-8cf43c9f2001'
              AND store_id = 'store-us-east' AND currency_code = 'USD' AND name = 'DEFAULT'
            ON CONFLICT (price_entry_id)
            DO UPDATE SET
                price_list_id = EXCLUDED.price_list_id, product_sku = EXCLUDED.product_sku,
                variant_sku = EXCLUDED.variant_sku, availability_id = EXCLUDED.availability_id,
                code = EXCLUDED.code, amount = EXCLUDED.amount, price_type = EXCLUDED.price_type,
                is_default = EXCLUDED.is_default, product_identifier_id = EXCLUDED.product_identifier_id;

            INSERT INTO pricing_promotions.price_entry
                (price_entry_id, price_list_id, product_sku, variant_sku, availability_id,
                 code, amount, price_type, is_default, product_identifier_id)
            SELECT
                '00000000-0000-0000-0000-000000000002', price_list_id, 'phase4c-sku', 'phase4c-sku', 1,
                'variant', 21.99, 'OneTime', true, 1
            FROM pricing_promotions.price_list
            WHERE tenant_id = '2e6d7b63-5b1d-4f8a-8e12-8cf43c9f2001'
              AND store_id = 'store-us-east' AND currency_code = 'USD' AND name = 'DEFAULT'
            ON CONFLICT (price_entry_id)
            DO UPDATE SET
                price_list_id = EXCLUDED.price_list_id, product_sku = EXCLUDED.product_sku,
                variant_sku = EXCLUDED.variant_sku, availability_id = EXCLUDED.availability_id,
                code = EXCLUDED.code, amount = EXCLUDED.amount, price_type = EXCLUDED.price_type,
                is_default = EXCLUDED.is_default, product_identifier_id = EXCLUDED.product_identifier_id;

            INSERT INTO pricing_promotions.price_list
                (price_list_id, tenant_id, store_id, name, currency_code, is_active)
            VALUES
                ('00000000-0000-0000-0000-000000000011',
                 'test-tenant-001', 'test-store-001', 'DEFAULT', 'USD', true)
            ON CONFLICT (tenant_id, store_id, currency_code, name)
            DO UPDATE SET is_active = true, updated_at = current_timestamp;

            INSERT INTO pricing_promotions.price_entry
                (price_entry_id, price_list_id, product_sku, variant_sku, availability_id,
                 code, amount, price_type, is_default, product_identifier_id)
            SELECT
                '00000000-0000-0000-0000-000000000011', price_list_id, 'phase4c-sku', NULL, 1,
                'seed', 19.99, 'OneTime', false, 1
            FROM pricing_promotions.price_list
            WHERE tenant_id = 'test-tenant-001'
              AND store_id = 'test-store-001' AND currency_code = 'USD' AND name = 'DEFAULT'
            ON CONFLICT (price_entry_id)
            DO UPDATE SET
                price_list_id = EXCLUDED.price_list_id, product_sku = EXCLUDED.product_sku,
                variant_sku = EXCLUDED.variant_sku, availability_id = EXCLUDED.availability_id,
                code = EXCLUDED.code, amount = EXCLUDED.amount, price_type = EXCLUDED.price_type,
                is_default = EXCLUDED.is_default, product_identifier_id = EXCLUDED.product_identifier_id;

            INSERT INTO pricing_promotions.price_entry
                (price_entry_id, price_list_id, product_sku, variant_sku, availability_id,
                 code, amount, price_type, is_default, product_identifier_id)
            SELECT
                '00000000-0000-0000-0000-000000000012', price_list_id, 'phase4c-sku', 'phase4c-sku', 1,
                'variant', 21.99, 'OneTime', true, 1
            FROM pricing_promotions.price_list
            WHERE tenant_id = 'test-tenant-001'
              AND store_id = 'test-store-001' AND currency_code = 'USD' AND name = 'DEFAULT'
            ON CONFLICT (price_entry_id)
            DO UPDATE SET
                price_list_id = EXCLUDED.price_list_id, product_sku = EXCLUDED.product_sku,
                variant_sku = EXCLUDED.variant_sku, availability_id = EXCLUDED.availability_id,
                code = EXCLUDED.code, amount = EXCLUDED.amount, price_type = EXCLUDED.price_type,
                is_default = EXCLUDED.is_default, product_identifier_id = EXCLUDED.product_identifier_id;

            INSERT INTO pricing_promotions.promotion
                (promotion_id, tenant_id, store_id, name, rule_key, discount_rate, is_enabled)
            VALUES
                ('00000000-0000-0000-0000-000000000003',
                 '2e6d7b63-5b1d-4f8a-8e12-8cf43c9f2001', 'store-us-east',
                 'Phase 4c promotion', 'phase4c-test', 0.10, true)
            ON CONFLICT (tenant_id, store_id, rule_key)
            DO UPDATE SET discount_rate = EXCLUDED.discount_rate, is_enabled = true;

            INSERT INTO pricing_promotions.coupon
                (coupon_id, promotion_id, tenant_id, store_id, code, is_enabled)
            VALUES
                ('00000000-0000-0000-0000-000000000004',
                 '00000000-0000-0000-0000-000000000003',
                 '2e6d7b63-5b1d-4f8a-8e12-8cf43c9f2001', 'store-us-east',
                 'phase4c-test', true)
            ON CONFLICT (tenant_id, store_id, code)
            DO UPDATE SET promotion_id = EXCLUDED.promotion_id, is_enabled = true;
            """,
            connection);
        await command.ExecuteNonQueryAsync();
    }

    public Task PrepareCatalogRequestAsync(HttpMethod method, string path, int expectedStatus)
    {
        if (expectedStatus == (int)HttpStatusCode.Unauthorized)
        {
            return Task.CompletedTask;
        }

        var includeReservation = path.StartsWith("/api/v1/reservations/", StringComparison.Ordinal);
        return EnsureTestCatalogAsync(includeReservation);
    }

    public Task PreparePricingRequestAsync(HttpMethod method, string path, int expectedStatus)
    {
        if (expectedStatus == (int)HttpStatusCode.Unauthorized)
        {
            return Task.CompletedTask;
        }

        return EnsureTestPricingAsync();
    }

    public async Task DisableCartPricingAsync()
    {
        await using var connection = await OpenDatabaseAsync("shopizerDb");
        await using var command = new NpgsqlCommand(
            """
            DELETE FROM pricing_promotions.price_entry
            WHERE price_list_id IN (
                SELECT price_list_id
                FROM pricing_promotions.price_list
                WHERE tenant_id = 'test-tenant-001' AND store_id = 'test-store-001'
            );
            """,
            connection);
        await command.ExecuteNonQueryAsync();
    }

    public Task RestoreCartPricingAsync() => EnsureTestPricingAsync();

    public Task PreparePaymentRequestAsync(HttpMethod method, string path, int expectedStatus)
    {
        if (expectedStatus == (int)HttpStatusCode.Unauthorized)
        {
            return Task.CompletedTask;
        }

        return EnsureTestPaymentsAsync(path);
    }

    public async Task PreparePlatformRequestAsync(HttpMethod method, string path, int expectedStatus)
    {
        if (expectedStatus == (int)HttpStatusCode.Unauthorized ||
            expectedStatus == (int)HttpStatusCode.BadRequest)
        {
            return;
        }

        if (path.Contains("/delivery-attempts/", StringComparison.Ordinal))
        {
            await EnsurePlatformDeliveryAttemptAsync();
        }

        if (path.StartsWith("/api/v1/carrier-quotes/", StringComparison.Ordinal))
        {
            await EnsurePlatformCarrierEndpointsAsync();
        }

        if (path.StartsWith("/api/v1/files", StringComparison.Ordinal) &&
            (method == HttpMethod.Get || method == HttpMethod.Delete))
        {
            if (path.Contains("/folders", StringComparison.Ordinal))
            {
                await EnsurePlatformFolderAsync();
            }
            else
            {
                await EnsurePlatformFileAsync();
            }
        }
    }

    private async Task EnsurePlatformFileAsync()
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/files")
        {
            Content = JsonContent.Create(new
            {
                storeCode = "test-store-001",
                contentType = "Image",
                folderPath = "phase4c-files",
                fileName = "phase4c-file",
                mimeType = "text/plain",
                contentBase64 = "cGhhc2U0Yy10ZXN0",
                idempotencyKey = $"platform-fixture-{Guid.NewGuid():N}"
            })
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", TestTenantAdminAccessToken);
        using var response = await PlatformIntegrationsClient.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.Created, body);
    }

    private async Task EnsurePlatformFolderAsync()
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/files/folders")
        {
            Content = JsonContent.Create(new
            {
                storeCode = "test-store-001",
                provider = "Local",
                folderPath = "phase4c-folders",
                folderName = "phase4c-folder"
            })
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", TestTenantAdminAccessToken);
        using var response = await PlatformIntegrationsClient.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.Created, body);
    }

    private async Task EnsurePlatformDeliveryAttemptAsync()
    {
        await using var connection = await OpenDatabaseAsync("shopizerDb");
        await using var command = new NpgsqlCommand(
            """
            INSERT INTO platform_integrations.integration_endpoint
                (endpoint_id, tenant_id, store_id, integration_type, provider, code, environment,
                 status, configuration_ref, endpoint_uri, capabilities, supplemental_configuration,
                 timeout_ms, max_attempts)
            VALUES
                ('00000000-0000-0000-0000-000000000010', 'test-tenant-001', 'test-store-001',
                 'EMAIL', 'Local', 'fixture-email', 'PROD', 'ACTIVE', 'fixture://email', NULL,
                 '{}'::jsonb, '{}'::jsonb, 10000, 3)
            ON CONFLICT (endpoint_id) DO UPDATE SET status = 'ACTIVE';

            INSERT INTO platform_integrations.delivery_idempotency
                (operation_id, tenant_id, store_id, operation_type, idempotency_key, request_hash,
                 item_count, status)
            VALUES
                ('00000000-0000-0000-0000-000000000010', 'test-tenant-001', 'test-store-001',
                 'EMAIL', 'fixture-delivery', repeat('0', 64), 1, 'FAILED')
            ON CONFLICT (operation_id) DO UPDATE SET status = 'FAILED';

            INSERT INTO platform_integrations.delivery_attempt
                (attempt_id, operation_id, endpoint_id, message_id, tenant_id, store_id,
                 operation_item_key, attempt_number, status, provider_error_code, request_payload)
            VALUES
                ('00000000-0000-0000-0000-000000000001',
                 '00000000-0000-0000-0000-000000000010',
                 '00000000-0000-0000-0000-000000000010',
                 NULL, 'test-tenant-001', 'test-store-001', 'fixture', 1, 'FAILED',
                 'FIXTURE_FAILURE', '{}'::jsonb)
            ON CONFLICT (attempt_id) DO UPDATE SET status = 'FAILED', provider_error_code = 'FIXTURE_FAILURE';
            """,
            connection);
        await command.ExecuteNonQueryAsync();
    }

    private void StartPlatformProviderStub()
    {
        _platformProviderListener = new TcpListener(IPAddress.Loopback, 0);
        _platformProviderListener.Start();
        var port = ((IPEndPoint)_platformProviderListener.LocalEndpoint).Port;
        _platformProviderUri = $"http://127.0.0.1:{port}/quote";
        _platformProviderCancellation = new CancellationTokenSource();
        _platformProviderLoop = Task.Run(() => ServePlatformProviderAsync(_platformProviderCancellation.Token));
    }

    private async Task ServePlatformProviderAsync(CancellationToken cancellationToken)
    {
        const string body = "<?xml version=\"1.0\"?><Response><RatedShipment><Service><Code>STANDARD</Code></Service><TotalCharges><MonetaryValue>10.00</MonetaryValue></TotalCharges><GuaranteedDaysToDelivery>3</GuaranteedDaysToDelivery></RatedShipment><Postage><MailService>STANDARD</MailService><Rate>10.00</Rate></Postage></Response>";
        var bytes = Encoding.UTF8.GetBytes(body);
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                using var client = await _platformProviderListener!.AcceptTcpClientAsync(cancellationToken);
                await using var stream = client.GetStream();
                var buffer = new byte[4096];
                _ = await stream.ReadAsync(buffer, cancellationToken);
                var header = Encoding.ASCII.GetBytes($"HTTP/1.1 200 OK\r\nContent-Type: application/xml\r\nContent-Length: {bytes.Length}\r\nConnection: close\r\n\r\n");
                await stream.WriteAsync(header, cancellationToken);
                await stream.WriteAsync(bytes, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { }
            catch (SocketException) when (cancellationToken.IsCancellationRequested) { }
        }
    }

    private async Task EnsurePlatformCarrierEndpointsAsync()
    {
        await using var connection = await OpenDatabaseAsync("shopizerDb");
        await using var command = new NpgsqlCommand(
            """
            INSERT INTO platform_integrations.integration_endpoint
                (endpoint_id, tenant_id, store_id, integration_type, provider, code, environment,
                 status, configuration_ref, endpoint_uri, capabilities, supplemental_configuration,
                 timeout_ms, max_attempts)
            VALUES
                ('00000000-0000-0000-0000-000000000020', 'test-tenant-001', 'test-store-001',
                 'SHIPPING', 'UPS', 'ups', 'phase4c-test', 'ACTIVE', 'fixture://ups', @uri,
                 '{}'::jsonb, '{}'::jsonb, 10000, 3),
                ('00000000-0000-0000-0000-000000000021', 'test-tenant-001', 'test-store-001',
                 'SHIPPING', 'USPS', 'usps', 'phase4c-test', 'ACTIVE', 'fixture://usps', @uri,
                 '{}'::jsonb, '{}'::jsonb, 10000, 3)
            ON CONFLICT (endpoint_id) DO UPDATE SET endpoint_uri = EXCLUDED.endpoint_uri, status = 'ACTIVE';
            """,
            connection);
        command.Parameters.AddWithValue("uri", _platformProviderUri ?? throw new InvalidOperationException("Provider stub is not running."));
        await command.ExecuteNonQueryAsync();
    }

    private async Task EnsureTestPaymentsAsync(string path)
    {
        const string tenant = "test-tenant-001";
        const string store = "test-store-001";
        const string intentId = "00000000-0000-0000-0000-000000000001";
        const string operationId = "00000000-0000-0000-0000-000000000001";
        const string authorizationOperationId = "00000000-0000-0000-0000-000000000002";
        const string captureOperationId = "00000000-0000-0000-0000-000000000003";
        const string authorizationTransactionId = "00000000-0000-0000-0000-000000000011";
        const string captureTransactionId = "00000000-0000-0000-0000-000000000012";
        var isAuthorization = path.EndsWith("/authorize", StringComparison.Ordinal);
        var isCapture = path.EndsWith("/capture", StringComparison.Ordinal);
        var isRefund = path.EndsWith("/refunds", StringComparison.Ordinal);
        var status = isAuthorization ? "Created" : isCapture ? "Authorized" : isRefund ? "Captured" : "Created";
        var authorizedAmount = isCapture || isRefund ? 10m : 0m;
        var capturedAmount = isRefund ? 10m : 0m;

        await using var connection = await OpenDatabaseAsync("shopizerDb");
        await using var transaction = await connection.BeginTransactionAsync();
        await using (var cleanup = new NpgsqlCommand(
            """
            DELETE FROM payments.payment_callback WHERE tenant_id = @tenant AND store_id = @store;
            DELETE FROM payments.payment_refund WHERE tenant_id = @tenant AND store_id = @store;
            DELETE FROM payments.payment_provider_reference WHERE tenant_id = @tenant AND store_id = @store;
            DELETE FROM payments.payment_transaction WHERE tenant_id = @tenant AND store_id = @store;
            DELETE FROM payments.payment_idempotency WHERE tenant_id = @tenant AND store_id = @store;
            DELETE FROM payments.payment_operation WHERE tenant_id = @tenant AND store_id = @store;
            DELETE FROM payments.payment_intent WHERE tenant_id = @tenant AND store_id = @store;
            """, connection, transaction))
        {
            cleanup.Parameters.AddWithValue("tenant", tenant);
            cleanup.Parameters.AddWithValue("store", store);
            await cleanup.ExecuteNonQueryAsync();
        }

        await using (var seed = new NpgsqlCommand(
            """
            INSERT INTO payments.payment_intent
                (payment_intent_id, tenant_id, store_id, checkout_session_id, order_id, provider_code,
                 provider_config_version, amount, currency_code, status, authorized_amount, captured_amount,
                 created_by, correlation_id)
            VALUES (@intent, @tenant, @store, @intent, @intent, 'stripe', 1, 10.00, 'USD', @status,
                    @authorized, @captured, 'phase4c-test', 'corr-ms06-001');

            INSERT INTO payments.payment_operation
                (payment_operation_id, payment_intent_id, tenant_id, store_id, operation_type, status,
                 requested_amount, currency_code, idempotency_key, request_fingerprint, provider_attempt_id,
                 provider_reference, correlation_id)
            VALUES (@operation, @intent, @tenant, @store, 'Initialize', 'Succeeded', 10.00, 'USD',
                    'phase4c-seed', repeat('0', 64), @operation, 'stripe_seed', 'corr-ms06-001');

            INSERT INTO payments.payment_operation
                (payment_operation_id, payment_intent_id, tenant_id, store_id, operation_type, status,
                 requested_amount, currency_code, idempotency_key, request_fingerprint, provider_attempt_id,
                 provider_reference, correlation_id)
            VALUES (@authorizationOperation, @intent, @tenant, @store, 'Authorize', 'Succeeded', 10.00, 'USD',
                    'phase4c-seed-authorize', repeat('1', 64), @authorizationOperation, 'stripe_seed', 'corr-ms06-001')
            ON CONFLICT DO NOTHING;

            INSERT INTO payments.payment_operation
                (payment_operation_id, payment_intent_id, tenant_id, store_id, operation_type, status,
                 requested_amount, currency_code, idempotency_key, request_fingerprint, provider_attempt_id,
                 provider_reference, correlation_id)
            VALUES (@captureOperation, @intent, @tenant, @store, 'Capture', 'Succeeded', 10.00, 'USD',
                    'phase4c-seed-capture', repeat('2', 64), @captureOperation, 'stripe_seed', 'corr-ms06-001')
            ON CONFLICT DO NOTHING;

            INSERT INTO payments.payment_provider_reference
                (payment_intent_id, tenant_id, store_id, provider_code, reference_type, provider_reference)
            VALUES (@intent, @tenant, @store, 'stripe', 'Callback', 'phase4c-test');

            INSERT INTO payments.payment_transaction
                (payment_transaction_id, payment_intent_id, payment_operation_id, tenant_id, store_id,
                 operation_type, status, amount, currency_code, provider_code, provider_reference, provider_status,
                 sequence_no, correlation_id)
            SELECT @authorizationTransaction, @intent, @authorizationOperation, @tenant, @store,
                   'Authorize', 'Succeeded', 10.00, 'USD', 'stripe', 'stripe_seed', 'approved', 1, 'corr-ms06-001'
            WHERE @status IN ('Authorized', 'Captured');

            INSERT INTO payments.payment_transaction
                (payment_transaction_id, payment_intent_id, payment_operation_id, tenant_id, store_id,
                 operation_type, status, amount, currency_code, provider_code, provider_reference, provider_status,
                 sequence_no, correlation_id)
            SELECT @captureTransaction, @intent, @captureOperation, @tenant, @store,
                   'Capture', 'Succeeded', 10.00, 'USD', 'stripe', 'stripe_seed', 'captured', 2, 'corr-ms06-001'
            WHERE @status = 'Captured';
            """, connection, transaction))
        {
            seed.Parameters.AddWithValue("intent", Guid.Parse(intentId));
            seed.Parameters.AddWithValue("operation", Guid.Parse(operationId));
            seed.Parameters.AddWithValue("authorizationOperation", Guid.Parse(authorizationOperationId));
            seed.Parameters.AddWithValue("captureOperation", Guid.Parse(captureOperationId));
            seed.Parameters.AddWithValue("authorizationTransaction", Guid.Parse(authorizationTransactionId));
            seed.Parameters.AddWithValue("captureTransaction", Guid.Parse(captureTransactionId));
            seed.Parameters.AddWithValue("tenant", tenant);
            seed.Parameters.AddWithValue("store", store);
            seed.Parameters.AddWithValue("status", status);
            seed.Parameters.AddWithValue("authorized", authorizedAmount);
            seed.Parameters.AddWithValue("captured", capturedAmount);
            await seed.ExecuteNonQueryAsync();
        }

        await transaction.CommitAsync();
    }

    private async Task<string> LoginAsync(string username, string password, bool administrator)
        => await LoginAsync(CustomerIdentityClient, username, password, administrator);

    private async Task<string> LoginForContextAsync(string tenant, string store)
    {
        var client = _application!.CreateHttpClient("customer-identity");
        ConfigureClient(client, tenant, store, $"corr-auth-{Guid.NewGuid():N}", false, null, null);
        return await LoginAsync(client, "phase4c-test", "Phase4c!Password2026", true);
    }

    private static async Task<string> LoginAsync(HttpClient client, string username, string password, bool administrator)
    {
        using var response = await client.PostAsync(
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
        _platformProviderCancellation?.Cancel();
        _platformProviderListener?.Stop();
        if (_platformProviderLoop is not null)
        {
            try { await _platformProviderLoop; }
            catch (OperationCanceledException) { }
        }
        if (_application is not null)
        {
            try
            {
                await CleanupCatalogTestDataAsync();
                await CleanupTestDataAsync();
            }
            finally
            {
                await _application.DisposeAsync();
            }
        }
    }

    private async Task CleanupCatalogTestDataAsync()
    {
        const string tenant = "test-tenant-001";
        const string store = "test-store-001";

        var connectionString = await _application!.GetConnectionStringAsync("shopizerDb")
            ?? throw new InvalidOperationException("The shopizer database connection string is unavailable.");

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        await using var command = new NpgsqlCommand(
            """
            DELETE FROM catalog_product.event_outbox
            WHERE tenant_id = @tenant AND store_id = @store;

            DELETE FROM catalog_product.inventory_reservation
            WHERE tenant_id = @tenant AND store_id = @store;

            DELETE FROM catalog_product.product_image
            WHERE product_id IN (
                SELECT id FROM catalog_product.product
                WHERE tenant_id = @tenant AND store_id = @store
            );

            DELETE FROM catalog_product.product_relationship
            WHERE product_id IN (
                SELECT id FROM catalog_product.product
                WHERE tenant_id = @tenant AND store_id = @store
            )
               OR related_product_id IN (
                SELECT id FROM catalog_product.product
                WHERE tenant_id = @tenant AND store_id = @store
            );

            DELETE FROM catalog_product.product_category
            WHERE product_id IN (
                SELECT id FROM catalog_product.product
                WHERE tenant_id = @tenant AND store_id = @store
            )
               OR category_id IN (
                SELECT id FROM catalog_product.category
                WHERE tenant_id = @tenant AND store_id = @store
            );

            DELETE FROM catalog_product.product_attribute
            WHERE product_id IN (
                SELECT id FROM catalog_product.product
                WHERE tenant_id = @tenant AND store_id = @store
            );

            DELETE FROM catalog_product.product_price
            WHERE store_id = @store;

            DELETE FROM catalog_product.product_availability
            WHERE store_id = @store;

            DELETE FROM catalog_product.product_variant
            WHERE store_id = @store;

            DELETE FROM catalog_product.product_description
            WHERE product_id IN (
                SELECT id FROM catalog_product.product
                WHERE tenant_id = @tenant AND store_id = @store
            );

            DELETE FROM catalog_product.category_description
            WHERE category_id IN (
                SELECT id FROM catalog_product.category
                WHERE tenant_id = @tenant AND store_id = @store
            );

            DELETE FROM catalog_product.product_option_value
            WHERE store_id = @store;

            DELETE FROM catalog_product.product_variation
            WHERE store_id = @store;

            DELETE FROM catalog_product.product_option
            WHERE store_id = @store;

            DELETE FROM catalog_product.product
            WHERE tenant_id = @tenant AND store_id = @store;

            DELETE FROM catalog_product.category
            WHERE id IN (
                SELECT id
                FROM catalog_product.category
                WHERE tenant_id = @tenant AND store_id = @store
                ORDER BY depth DESC, id DESC
            );
            """,
            connection,
            transaction);
        command.Parameters.AddWithValue("tenant", tenant);
        command.Parameters.AddWithValue("store", store);
        await command.ExecuteNonQueryAsync();
        await transaction.CommitAsync();
    }

    private async Task CleanupTestDataAsync()
    {
        var connectionString = await _application!.GetConnectionStringAsync("shopizerDb")
            ?? throw new InvalidOperationException("The shopizer database connection string is unavailable.");

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

            DELETE FROM customer_identity.customer_addresses
            WHERE customer_id = '00000000-0000-0000-0000-000000000003'::uuid;

            DELETE FROM customer_identity.customer_accounts
            WHERE id = '00000000-0000-0000-0000-000000000003'::uuid
              AND tenant_id = 'test-tenant-001' AND store_id = 'test-store-001';

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

    public async Task<NpgsqlConnection> OpenDatabaseAsync(string resourceName)
    {
        var connectionString = await _application!.GetConnectionStringAsync(resourceName)
            ?? throw new InvalidOperationException($"No connection string for {resourceName}.");
        var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        return connection;
    }
}