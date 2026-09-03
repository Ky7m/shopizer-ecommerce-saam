using Shopizer.CartCheckout.Data;
using Shopizer.CartCheckout.Middleware;
using Shopizer.CartCheckout.Services;
using Shopizer.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddNpgsqlDataSource("shopizerDb");
builder.AddRabbitMQClient("rabbitmq");
builder.Services.AddHttpContextAccessor();
builder.Services.AddTransient<ContextPropagationHandler>();
builder.Services.AddHttpClient<CustomerClient>(client => client.BaseAddress = new Uri("http://customer-identity"))
    .AddHttpMessageHandler<ContextPropagationHandler>();
builder.Services.AddHttpClient<CatalogClient>(client => client.BaseAddress = new Uri("http://catalog-product"))
    .AddHttpMessageHandler<ContextPropagationHandler>();
builder.Services.AddHttpClient<PricingClient>(client => client.BaseAddress = new Uri("http://pricing-promotions"))
    .AddHttpMessageHandler<ContextPropagationHandler>();
builder.Services.AddHttpClient<TaxClient>(client => client.BaseAddress = new Uri("http://tax"))
    .AddHttpMessageHandler<ContextPropagationHandler>();
builder.Services.AddHttpClient<ShippingClient>(client => client.BaseAddress = new Uri("http://shipping"))
    .AddHttpMessageHandler<ContextPropagationHandler>();
builder.Services.AddHttpClient<PaymentClient>(client => client.BaseAddress = new Uri("http://payments"))
    .AddHttpMessageHandler<ContextPropagationHandler>();
builder.Services.AddSingleton<CartRepository>();
builder.Services.AddSingleton<SchemaInitializer>();
builder.Services.AddSingleton<EventPublisher>();
builder.Services.AddHostedService<OutboxDispatcher>();
builder.Services.AddScoped<CartService>();
builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(options => options.SuppressModelStateInvalidFilter = true)
    .AddMvcOptions(options => options.Filters.Add<ModelStateExceptionFilter>())
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
    });

var app = builder.Build();

await app.Services.GetRequiredService<SchemaInitializer>().InitializeAsync(CancellationToken.None);
app.UseMiddleware<ErrorMiddleware>();
app.UseMiddleware<TokenMiddleware>();
app.MapControllers();
app.MapDefaultEndpoints();

app.Run();
