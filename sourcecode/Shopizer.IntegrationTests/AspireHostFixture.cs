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
            ["catalog-product"] = (client => CatalogProductClient = client, "test-tenant-001", "test-store-001", "11111111-1111-4111-8111-111111111111", false, null, "phase4c-test"),
            ["search"] = (client => SearchClient = client, "tenant-demo", "default", "corr-ms03-0001", false, null, null),
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

        await EnsureTestCatalogAsync();
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

    private async Task EnsureTestCatalogAsync(bool includeReservation = true)
    {
        const string tenant = "test-tenant-001";
        const string store = "test-store-001";
        const string resourceId = "00000000-0000-0000-0000-000000000001";
        const string moveParentId = "00000000-0000-0000-0000-000000000002";
        const string secondAvailabilityId = "00000000-0000-0000-0000-000000000002";
        const string reservationPayload = """{"reservationKey":"phase4c-test","variantId":"00000000-0000-0000-0000-000000000001","availabilityId":"00000000-0000-0000-0000-000000000001","regionCode":"phase4c-test","quantity":1,"expiresAt":"2026-09-02T00:00:00Z"}""";

        await CleanupCatalogTestDataAsync();

        var connectionString = await _application!.GetConnectionStringAsync("catalogproductdb")
            ?? throw new InvalidOperationException("The catalog product database connection string is unavailable.");

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
                 now() - interval '1 day', 'phase4c-test', 'phase4c-test', 'phase4c-test', true, true, true, 1)
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

    public Task PrepareCatalogRequestAsync(HttpMethod method, string path, int expectedStatus)
    {
        if (expectedStatus == (int)HttpStatusCode.Unauthorized)
        {
            return Task.CompletedTask;
        }

        var includeReservation = path.StartsWith("/api/v1/reservations/", StringComparison.Ordinal);
        return EnsureTestCatalogAsync(includeReservation);
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

        var connectionString = await _application!.GetConnectionStringAsync("catalogproductdb")
            ?? throw new InvalidOperationException("The catalog product database connection string is unavailable.");

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

    public async Task<NpgsqlConnection> OpenDatabaseAsync(string resourceName)
    {
        var connectionString = await _application!.GetConnectionStringAsync(resourceName)
            ?? throw new InvalidOperationException($"No connection string for {resourceName}.");
        var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        return connection;
    }
}

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class ShopizerAspireCollection : ICollectionFixture<AspireHostFixture>
{
    public const string Name = "Shopizer Aspire";
}
