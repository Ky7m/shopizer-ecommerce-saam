using Microsoft.AspNetCore.Mvc;
using Shopizer.ServiceDefaults;
using Shopizer.Tax.Data;
using Shopizer.Tax.Middleware;
using Shopizer.Tax.Services;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddNpgsqlDataSource("shopizerDb");
builder.Services.AddSingleton<TaxRepository>();
builder.Services.AddSingleton<TokenService>();
builder.Services.AddScoped<TaxService>();
builder.Services.AddSingleton<SchemaInitializer>();
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    // Domain validation maps malformed-but-readable business input to the contract's
    // typed 422 responses; malformed JSON and route binding remain 400 responses.
    options.SuppressModelStateInvalidFilter = true;
});
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.DefaultIgnoreCondition =
            System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
    });

var app = builder.Build();
await app.Services.GetRequiredService<SchemaInitializer>().InitializeAsync(CancellationToken.None);
app.UseMiddleware<ErrorMiddleware>();
app.UseMiddleware<TokenMiddleware>();
app.MapControllers();
app.MapDefaultEndpoints();
app.Run();
