const string SharedJwtSecret = "shopizer-development-shared-jwt-secret-change-me";

var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres")
    .WithPgAdmin();

var rabbitmq = builder.AddRabbitMQ("rabbitmq")
    .WithManagementPlugin();

var redis = builder.AddRedis("redis")
    .WithRedisCommander();

var shopizerDb = postgres.AddDatabase("shopizerDb");

var customerIdentity = builder.AddProject<Projects.Shopizer_CustomerIdentity>("customer-identity")
    .WithReference(shopizerDb)
    .WithReference(rabbitmq)
    .WithEnvironment("CustomerIdentity__JwtSecret", SharedJwtSecret)
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WaitFor(shopizerDb)
    .WaitFor(rabbitmq);

var catalogProduct = builder.AddProject<Projects.Shopizer_CatalogProduct>("catalog-product")
    .WithReference(shopizerDb)
    .WithReference(rabbitmq)
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WaitFor(shopizerDb)
    .WaitFor(rabbitmq);

var search = builder.AddProject<Projects.Shopizer_Search>("search")
    .WithReference(shopizerDb)
    .WithReference(rabbitmq)
    .WithEnvironment("Search__JwtSecret", SharedJwtSecret)
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WaitFor(shopizerDb)
    .WaitFor(rabbitmq);

var orderManagement = builder.AddProject<Projects.Shopizer_OrderManagement>("order-management")
    .WithReference(shopizerDb)
    .WithReference(rabbitmq)
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WaitFor(shopizerDb)
    .WaitFor(rabbitmq);

var payments = builder.AddProject<Projects.Shopizer_Payments>("payments")
    .WithReference(shopizerDb)
    .WithReference(rabbitmq)
    .WithEnvironment("Payments__JwtSecret", SharedJwtSecret)
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WaitFor(shopizerDb)
    .WaitFor(rabbitmq);

var pricingPromotions = builder.AddProject<Projects.Shopizer_PricingPromotions>("pricing-promotions")
    .WithReference(shopizerDb)
    .WithReference(rabbitmq)
    .WithEnvironment("PricingPromotions__JwtSecret", SharedJwtSecret)
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WaitFor(shopizerDb)
    .WaitFor(rabbitmq);

var tax = builder.AddProject<Projects.Shopizer_Tax>("tax")
    .WithReference(shopizerDb)
    .WithEnvironment("Tax__JwtSecret", SharedJwtSecret)
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WaitFor(shopizerDb);

var shipping = builder.AddProject<Projects.Shopizer_Shipping>("shipping")
    .WithReference(shopizerDb)
    .WithReference(rabbitmq)
    .WithEnvironment("Shipping__JwtSecret", SharedJwtSecret)
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WaitFor(shopizerDb)
    .WaitFor(rabbitmq);

var cartCheckout = builder.AddProject<Projects.Shopizer_CartCheckout>("cart-checkout")
    .WithReference(shopizerDb)
    .WithReference(rabbitmq)
    .WithReference(redis)
    .WithReference(customerIdentity)
    .WithReference(catalogProduct)
    .WithReference(pricingPromotions)
    .WithReference(tax)
    .WithReference(shipping)
    .WithReference(payments)
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WaitFor(shopizerDb)
    .WaitFor(rabbitmq)
    .WaitFor(redis)
    .WaitFor(customerIdentity)
    .WaitFor(catalogProduct)
    .WaitFor(pricingPromotions)
    .WaitFor(tax)
    .WaitFor(shipping)
    .WaitFor(payments);

var merchantAdministration = builder.AddProject<Projects.Shopizer_MerchantAdministration>("merchant-administration")
    .WithReference(shopizerDb)
    .WithReference(rabbitmq)
    .WithEnvironment("MerchantAdministration__JwtSecret", SharedJwtSecret)
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WaitFor(shopizerDb)
    .WaitFor(rabbitmq);

var contentConfiguration = builder.AddProject<Projects.Shopizer_ContentConfiguration>("content-configuration")
    .WithReference(shopizerDb)
    .WithReference(rabbitmq)
    .WithEnvironment("ContentConfiguration__JwtSecret", SharedJwtSecret)
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WaitFor(shopizerDb)
    .WaitFor(rabbitmq);

var platformIntegrations = builder.AddProject<Projects.Shopizer_PlatformIntegrations>("platform-integrations")
    .WithReference(shopizerDb)
    .WithReference(rabbitmq)
    .WithEnvironment("PlatformIntegrations__JwtSecret", SharedJwtSecret)
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WaitFor(shopizerDb)
    .WaitFor(rabbitmq);

builder.AddProject<Projects.Shopizer_Admin>("admin")
    .WithExternalHttpEndpoints()
    .WithReference(customerIdentity)
    .WithReference(catalogProduct)
    .WithReference(search)
    .WithReference(cartCheckout)
    .WithReference(orderManagement)
    .WithReference(merchantAdministration)
    .WithReference(contentConfiguration)
    .WaitFor(customerIdentity)
    .WaitFor(catalogProduct)
    .WaitFor(search)
    .WaitFor(cartCheckout)
    .WaitFor(orderManagement)
    .WaitFor(merchantAdministration)
    .WaitFor(contentConfiguration);

builder.AddProject<Projects.Shopizer_Storefront>("storefront")
    .WithExternalHttpEndpoints()
    .WithReference(customerIdentity)
    .WithReference(catalogProduct)
    .WithReference(search)
    .WithReference(cartCheckout)
    .WithReference(orderManagement)
    .WithReference(contentConfiguration)
    .WaitFor(customerIdentity)
    .WaitFor(catalogProduct)
    .WaitFor(search)
    .WaitFor(cartCheckout)
    .WaitFor(orderManagement)
    .WaitFor(contentConfiguration);

builder.Build().Run();
