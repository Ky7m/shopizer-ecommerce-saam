var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres")
    .WithPgAdmin()
    .WithLifetime(ContainerLifetime.Persistent);

var rabbitmq = builder.AddRabbitMQ("rabbitmq")
    .WithManagementPlugin();

var redis = builder.AddRedis("redis")
    .WithDataVolume()
    .WithRedisCommander();

var customerIdentityDb = postgres.AddDatabase("customeridentitydb");
var catalogProductDb = postgres.AddDatabase("catalogproductdb");
var searchDb = postgres.AddDatabase("searchdb");
var cartCheckoutDb = postgres.AddDatabase("cartcheckoutdb");
var orderManagementDb = postgres.AddDatabase("ordermanagementdb");
var paymentsDb = postgres.AddDatabase("paymentsdb");
var pricingPromotionsDb = postgres.AddDatabase("pricingpromotionsdb");
var taxDb = postgres.AddDatabase("taxdb");
var shippingDb = postgres.AddDatabase("shippingdb");
var merchantAdministrationDb = postgres.AddDatabase("merchantadministrationdb");
var contentConfigurationDb = postgres.AddDatabase("contentconfigurationdb");
var platformIntegrationsDb = postgres.AddDatabase("platformintegrationsdb");

var customerIdentity = builder.AddProject<Projects.Shopizer_CustomerIdentity>("customer-identity")
    .WithReference(customerIdentityDb)
    .WithReference(rabbitmq)
    .WithExternalHttpEndpoints()
    .WithHttpEndpoint(port: 8101, name: "http")
    .WithHttpHealthCheck("/health");
var catalogProduct = builder.AddProject<Projects.Shopizer_CatalogProduct>("catalog-product")
    .WithReference(catalogProductDb)
    .WithReference(rabbitmq)
    .WithExternalHttpEndpoints()
    .WithHttpEndpoint(port: 8102, name: "http")
    .WithHttpHealthCheck("/health");
var search = builder.AddProject<Projects.Shopizer_Search>("search")
    .WithReference(searchDb)
    .WithReference(rabbitmq)
    .WithExternalHttpEndpoints()
    .WithHttpEndpoint(port: 8103, name: "http")
    .WithHttpHealthCheck("/health");
var cartCheckout = builder.AddProject<Projects.Shopizer_CartCheckout>("cart-checkout")
    .WithReference(cartCheckoutDb)
    .WithReference(rabbitmq)
    .WithReference(redis)
    .WithExternalHttpEndpoints()
    .WithHttpEndpoint(port: 8104, name: "http")
    .WithHttpHealthCheck("/health");
var orderManagement = builder.AddProject<Projects.Shopizer_OrderManagement>("order-management")
    .WithReference(orderManagementDb)
    .WithReference(rabbitmq)
    .WithExternalHttpEndpoints()
    .WithHttpEndpoint(port: 8105, name: "http")
    .WithHttpHealthCheck("/health");
var payments = builder.AddProject<Projects.Shopizer_Payments>("payments")
    .WithReference(paymentsDb)
    .WithReference(rabbitmq)
    .WithExternalHttpEndpoints()
    .WithHttpEndpoint(port: 8106, name: "http")
    .WithHttpHealthCheck("/health");
var pricingPromotions = builder.AddProject<Projects.Shopizer_PricingPromotions>("pricing-promotions")
    .WithReference(pricingPromotionsDb)
    .WithReference(rabbitmq)
    .WithExternalHttpEndpoints()
    .WithHttpEndpoint(port: 8107, name: "http")
    .WithHttpHealthCheck("/health");
var tax = builder.AddProject<Projects.Shopizer_Tax>("tax")
    .WithReference(taxDb)
    .WithExternalHttpEndpoints()
    .WithHttpEndpoint(port: 8108, name: "http")
    .WithHttpHealthCheck("/health");
var shipping = builder.AddProject<Projects.Shopizer_Shipping>("shipping")
    .WithReference(shippingDb)
    .WithReference(rabbitmq)
    .WithExternalHttpEndpoints()
    .WithHttpEndpoint(port: 8109, name: "http")
    .WithHttpHealthCheck("/health");
var merchantAdministration = builder.AddProject<Projects.Shopizer_MerchantAdministration>("merchant-administration")
    .WithReference(merchantAdministrationDb)
    .WithReference(rabbitmq)
    .WithExternalHttpEndpoints()
    .WithHttpEndpoint(port: 8110, name: "http")
    .WithHttpHealthCheck("/health");
var contentConfiguration = builder.AddProject<Projects.Shopizer_ContentConfiguration>("content-configuration")
    .WithReference(contentConfigurationDb)
    .WithReference(rabbitmq)
    .WithExternalHttpEndpoints()
    .WithHttpEndpoint(port: 8111, name: "http")
    .WithHttpHealthCheck("/health");
var platformIntegrations = builder.AddProject<Projects.Shopizer_PlatformIntegrations>("platform-integrations")
    .WithReference(platformIntegrationsDb)
    .WithReference(rabbitmq)
    .WithExternalHttpEndpoints()
    .WithHttpEndpoint(port: 8112, name: "http")
    .WithHttpHealthCheck("/health");

builder.AddProject<Projects.Shopizer_Admin>("admin")
    .WithExternalHttpEndpoints()
    .WithReference(customerIdentity)
    .WithReference(catalogProduct)
    .WithReference(search)
    .WithReference(cartCheckout)
    .WithReference(orderManagement)
    .WithReference(merchantAdministration)
    .WithReference(contentConfiguration)
    .WaitFor(customerIdentity);

builder.AddProject<Projects.Shopizer_Storefront>("storefront")
    .WithExternalHttpEndpoints()
    .WithReference(customerIdentity)
    .WithReference(catalogProduct)
    .WithReference(search)
    .WithReference(cartCheckout)
    .WithReference(orderManagement)
    .WithReference(contentConfiguration)
    .WaitFor(catalogProduct);

builder.Build().Run();
