using System.Net;

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

    public async ValueTask InitializeAsync()
    {
        var builder = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.Shopizer_AppHost>();

        _application = await builder.BuildAsync();
        await _application.StartAsync();

        var clients = new Dictionary<string, (Action<HttpClient> Assign, string Tenant, string Store, string Correlation, bool Authorization, string? Language, string? IdempotencyKey)>
        {
            ["customer-identity"] = (client => CustomerIdentityClient = client, "tenant-demo", "default", "00000000-0000-0000-0000-000000000001", true, null, null),
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
    }

    public async ValueTask DisposeAsync()
    {
        if (_application is not null)
        {
            await _application.DisposeAsync();
        }
    }

    private static void ConfigureClient(HttpClient client, string tenant, string store, string correlation, bool authorization, string? language, string? idempotencyKey)
    {
        if (authorization)
        {
            client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", "******");
        }

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
