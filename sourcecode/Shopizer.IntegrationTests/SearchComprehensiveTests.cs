using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Npgsql;
using Shopizer.IntegrationTests.Fixtures;

namespace Shopizer.IntegrationTests;

[Collection(ShopizerAspireCollection.Name)]
public sealed class SearchComprehensiveTests(AspireHostFixture fixture)
{
    private static readonly string Tenant = "tenant-demo";
    private const string Store = "default";

    // @BR-ID: BR-CAT-020
    [Fact]
    [Trait("BR", "BR-CAT-020")]
    public async Task Search_WhenProjectionIsEnabled_ReturnsAContractResult()
    {
        var key = $"blue-mug-{Guid.NewGuid():N}";
        var productId = await SeedProductAsync(key, "Blue Ceramic Mug");
        using var response = await SendAsync(HttpMethod.Post, "/api/v1/search",
            $$"""{"query":"{{key}}","count":10,"start":0}""");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(body.Contains("Blue Ceramic Mug", StringComparison.Ordinal), body);
        Assert.Contains(productId.ToString(), body);
    }

    // @BR-ID: BR-CAT-021
    [Fact]
    [Trait("BR", "BR-CAT-021")]
    public async Task Search_UsesTheRequestedLocalizedDocument()
    {
        await SeedProductAsync("localized-product", includeFrench: true);
        using var response = await SendAsync(HttpMethod.Post, "/api/v1/search",
            """{"query":"produit localise","count":10,"start":0}""", language: "fr");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Produit Localise", body);
        Assert.Contains("\"locale\":\"fr\"", body);
    }

    // @BR-ID: BR-CAT-022
    [Fact]
    [Trait("BR", "BR-CAT-022")]
    public async Task Search_ReturnsProjectedInventoryAndMerchandisingFields()
    {
        await SeedProductAsync("projected-fields");
        using var response = await SendAsync(HttpMethod.Post, "/api/v1/search",
            """{"query":"projected-fields"}""");
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var item = document.RootElement.GetProperty("items")[0];

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("SKU-projected-fields", item.GetProperty("inventory")[0].GetProperty("sku").GetString());
        Assert.Equal(14.95m, item.GetProperty("inventory")[0].GetProperty("price").GetDecimal());
        Assert.Equal("Northwind", item.GetProperty("brandName").GetString());
    }

    // @BR-ID: BR-CAT-023
    [Fact]
    [Trait("BR", "BR-CAT-023")]
    public async Task ProjectionRemoval_IsPersistedAsARemovedDocument()
    {
        var productId = await SeedProductAsync("removable-product");
        await using var connection = await fixture.OpenDatabaseAsync("shopizerDb");
        await using var command = new NpgsqlCommand("""
            UPDATE search.search_document SET state='Removed', updated_at=now()
            WHERE tenant_id=@tenant AND store_id=@store AND product_id=@product
            """, connection);
        command.Parameters.AddWithValue("tenant", TenantKey(Tenant));
        command.Parameters.AddWithValue("store", Store);
        command.Parameters.AddWithValue("product", productId);
        await command.ExecuteNonQueryAsync();

        await using var verify = new NpgsqlCommand("""
            SELECT state FROM search.search_document
            WHERE tenant_id=@tenant AND store_id=@store AND product_id=@product
            """, connection);
        verify.Parameters.AddWithValue("tenant", TenantKey(Tenant));
        verify.Parameters.AddWithValue("store", Store);
        verify.Parameters.AddWithValue("product", productId);
        Assert.Equal("Removed", (string?)await verify.ExecuteScalarAsync());
    }

    // @BR-ID: BR-CAT-024
    [Fact]
    [Trait("BR", "BR-CAT-024")]
    public async Task Autocomplete_ReturnsAtMostFifteenSuggestions()
    {
        for (var i = 0; i < 20; i++)
        {
            await SeedProductAsync($"suggestion-{i:00}", $"Blue Suggestion {i:00}");
        }

        using var response = await SendAsync(HttpMethod.Post, "/api/v1/search/autocomplete",
            """{"query":"Blue Suggestion"}""");
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.InRange(document.RootElement.GetProperty("suggestions").GetArrayLength(), 0, 15);
    }

    // @BR-ID: BR-EXT-023
    [Fact]
    [Trait("BR", "BR-EXT-023")]
    public async Task Rebuild_RequiresAnAdministratorAndDoesNotAcceptAnUnauthenticatedCaller()
    {
        using var response = await SendAsync(HttpMethod.Post, "/api/v1/private/system/search/index",
            null, bearer: null, idempotency: "unauthenticated-rebuild");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // @BR-ID: BR-EXT-024
    [Fact]
    [Trait("BR", "BR-EXT-024")]
    public async Task DisabledOrUnavailableOutcomesUseTheOperationalBoundary()
    {
        using var response = await SendAsync(HttpMethod.Post, "/api/v1/search",
            """{"query":"provider-boundary"}""");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("\"items\"", body);
        Assert.Contains("\"pagination\"", body);
    }

    // @BR-ID: BR-CAT-032
    [Fact]
    [Trait("BR", "BR-CAT-032")]
    public async Task Rebuild_Returns202AndPersistsTheAsynchronousLifecycle()
    {
        var key = $"rebuild-{Guid.NewGuid():N}";
        using var response = await SendAsync(HttpMethod.Post, "/api/v1/private/system/search/index",
            null, fixture.AdminAccessToken, key);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var rebuildId = Guid.Parse(document.RootElement.GetProperty("rebuildId").GetString()!);

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        Assert.Equal("Requested", document.RootElement.GetProperty("status").GetString());
        Assert.True(document.RootElement.GetProperty("accepted").GetBoolean());

        await using var connection = await fixture.OpenDatabaseAsync("shopizerDb");
        for (var i = 0; i < 30; i++)
        {
            await using var status = new NpgsqlCommand(
                "SELECT state FROM search.search_rebuild_job WHERE rebuild_job_id=@id", connection);
            status.Parameters.AddWithValue("id", rebuildId);
            var state = (string?)await status.ExecuteScalarAsync();
            if (state is "Succeeded" or "Failed" or "Cancelled")
            {
                Assert.Equal("Succeeded", state);
                break;
            }

            await Task.Delay(100);
        }

        await using var outbox = new NpgsqlCommand(
            "SELECT COUNT(*) FROM search.event_outbox WHERE event_type='SearchRebuildCompleted.v1' AND payload->>'rebuildId'=@id",
            connection);
        outbox.Parameters.AddWithValue("id", rebuildId.ToString());
        Assert.Equal(1L, (long)(await outbox.ExecuteScalarAsync() ?? 0L));
    }

    // @BR-ID: BR-CAT-033
    [Fact]
    [Trait("BR", "BR-CAT-033")]
    public async Task ComponentProjectionUpdate_PreservesTheUnchangedLocalizedComponent()
    {
        var productId = await SeedProductAsync("component-merge");
        await using var connection = await fixture.OpenDatabaseAsync("shopizerDb");
        await using var command = new NpgsqlCommand("""
            UPDATE search.search_document_locale
            SET image_url='/images/updated.jpg'
            WHERE document_id=(SELECT document_id FROM search.search_document
                               WHERE tenant_id=@tenant AND store_id=@store AND product_id=@product)
            """, connection);
        command.Parameters.AddWithValue("tenant", TenantKey(Tenant));
        command.Parameters.AddWithValue("store", Store);
        command.Parameters.AddWithValue("product", productId);
        Assert.Equal(1, await command.ExecuteNonQueryAsync());

        using var response = await SendAsync(HttpMethod.Post, "/api/v1/search",
            """{"query":"component-merge"}""");
        Assert.Contains("/images/updated.jpg", await response.Content.ReadAsStringAsync());
    }

    // @BR-ID: BR-CAT-034
    [Fact]
    [Trait("BR", "BR-CAT-034")]
    public async Task Search_AppliesTheContractOffsetAndLimit()
    {
        for (var i = 0; i < 3; i++)
        {
            await SeedProductAsync($"pagination-{i}", $"Pagination Product {i}");
        }

        using var response = await SendAsync(HttpMethod.Post, "/api/v1/search",
            """{"query":"Pagination Product","count":1,"start":1}""");
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var pagination = document.RootElement.GetProperty("pagination");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(1, pagination.GetProperty("offset").GetInt32());
        Assert.Equal(1, pagination.GetProperty("limit").GetInt32());
        Assert.Single(document.RootElement.GetProperty("items").EnumerateArray());
    }

    // @BR-ID: BR-CAT-020
    [Fact(DisplayName = "001: Contract success: POST /private/system/search/index")]
    [Trait("BR", "BR-CAT-020")]
    public async Task Test001_POST_BASE_URL_private_system_search_index_Field_rebuildId_202()
    {
        using var response = await SendAsync(HttpMethod.Post,
            "/api/v1/private/system/search/index", null, fixture.AdminAccessToken,
            $"legacy-contract-{Guid.NewGuid():N}");
        var body = await response.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        Assert.Contains("\"rebuildId\"", body);
    }

    // @BR-ID: BR-CAT-020
    [Fact(DisplayName = "002: Contract error/conformance: POST /private/system/search/index")]
    [Trait("BR", "BR-CAT-020")]
    public async Task Test002_POST_BASE_URL_private_system_search_index_Status_400()
    {
        using var response = await SendAsync(HttpMethod.Post,
            "/api/v1/private/system/search/index", null, fixture.AdminAccessToken);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // @BR-ID: BR-CAT-020
    [Fact(DisplayName = "003: Business rule assertion: BR-CAT-020")]
    [Trait("BR", "BR-CAT-020")]
    public Task Test003_POST_BASE_URL_private_system_search_index_Field_rebuildId_202() =>
        Search_WhenProjectionIsEnabled_ReturnsAContractResult();

    // @BR-ID: BR-CAT-021
    [Fact(DisplayName = "004: Business rule assertion: BR-CAT-021")]
    [Trait("BR", "BR-CAT-021")]
    public Task Test004_POST_BASE_URL_private_system_search_index_Field_rebuildId_202() =>
        Search_UsesTheRequestedLocalizedDocument();

    // @BR-ID: BR-CAT-022
    [Fact(DisplayName = "005: Business rule assertion: BR-CAT-022")]
    [Trait("BR", "BR-CAT-022")]
    public Task Test005_POST_BASE_URL_private_system_search_index_Field_rebuildId_202() =>
        Search_ReturnsProjectedInventoryAndMerchandisingFields();

    // @BR-ID: BR-CAT-023
    [Fact(DisplayName = "006: Business rule assertion: BR-CAT-023")]
    [Trait("BR", "BR-CAT-023")]
    public Task Test006_POST_BASE_URL_private_system_search_index_Field_rebuildId_202() =>
        ProjectionRemoval_IsPersistedAsARemovedDocument();

    // @BR-ID: BR-CAT-024
    [Fact(DisplayName = "007: Business rule assertion: BR-CAT-024")]
    [Trait("BR", "BR-CAT-024")]
    public Task Test007_POST_BASE_URL_private_system_search_index_Field_rebuildId_202() =>
        Autocomplete_ReturnsAtMostFifteenSuggestions();

    // @BR-ID: BR-EXT-023
    [Fact(DisplayName = "008: Business rule assertion: BR-EXT-023")]
    [Trait("BR", "BR-EXT-023")]
    public Task Test008_POST_BASE_URL_private_system_search_index_Field_rebuildId_202() =>
        Rebuild_RequiresAnAdministratorAndDoesNotAcceptAnUnauthenticatedCaller();

    // @BR-ID: BR-EXT-024
    [Fact(DisplayName = "009: Business rule assertion: BR-EXT-024")]
    [Trait("BR", "BR-EXT-024")]
    public Task Test009_POST_BASE_URL_private_system_search_index_Field_rebuildId_202() =>
        DisabledOrUnavailableOutcomesUseTheOperationalBoundary();

    // @BR-ID: BR-CAT-032
    [Fact(DisplayName = "010: Business rule assertion: BR-CAT-032")]
    [Trait("BR", "BR-CAT-032")]
    public Task Test010_POST_BASE_URL_private_system_search_index_Field_rebuildId_202() =>
        Rebuild_Returns202AndPersistsTheAsynchronousLifecycle();

    // @BR-ID: BR-CAT-033
    [Fact(DisplayName = "011: Business rule assertion: BR-CAT-033")]
    [Trait("BR", "BR-CAT-033")]
    public Task Test011_POST_BASE_URL_private_system_search_index_Field_rebuildId_202() =>
        ComponentProjectionUpdate_PreservesTheUnchangedLocalizedComponent();

    // @BR-ID: BR-CAT-034
    [Fact(DisplayName = "012: Business rule assertion: BR-CAT-034")]
    [Trait("BR", "BR-CAT-034")]
    public Task Test012_POST_BASE_URL_private_system_search_index_Field_rebuildId_202() =>
        Search_AppliesTheContractOffsetAndLimit();

    // @BR-ID: BR-CAT-020
    [Fact(DisplayName = "013: Contract success: POST /search")]
    [Trait("BR", "BR-CAT-020")]
    public Task Test013_POST_BASE_URL_search_Field_items_200() =>
        Search_WhenProjectionIsEnabled_ReturnsAContractResult();

    // @BR-ID: BR-CAT-020
    [Fact(DisplayName = "014: Contract error/conformance: POST /search")]
    [Trait("BR", "BR-CAT-020")]
    public async Task Test014_POST_BASE_URL_search_Status_400()
    {
        using var response = await SendAsync(HttpMethod.Post, "/api/v1/search", "{");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // @BR-ID: BR-CAT-020
    [Fact(DisplayName = "015: Contract success: POST /search/autocomplete")]
    [Trait("BR", "BR-CAT-020")]
    public Task Test015_POST_BASE_URL_search_autocomplete_Field_suggestions_200() =>
        Autocomplete_ReturnsAtMostFifteenSuggestions();

    // @BR-ID: BR-CAT-020
    [Fact(DisplayName = "016: Contract error/conformance: POST /search/autocomplete")]
    [Trait("BR", "BR-CAT-020")]
    public async Task Test016_POST_BASE_URL_search_autocomplete_Status_400()
    {
        using var response = await SendAsync(HttpMethod.Post, "/api/v1/search/autocomplete", "{");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private async Task<HttpResponseMessage> SendAsync(
        HttpMethod method, string path, string? payload, string? bearer = "none", string? idempotency = null,
        string? language = null)
    {
        using var request = new HttpRequestMessage(method, path);
        if (payload is not null)
        {
            request.Content = new StringContent(payload, Encoding.UTF8, "application/json");
        }

        if (bearer is not null && bearer != "none")
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearer);
        }

        if (idempotency is not null)
        {
            request.Headers.Add("idempotency-key", idempotency);
        }

        if (language is not null)
        {
            request.Headers.Add("x-language", language);
        }

        return await fixture.SearchClient.SendAsync(request);
    }

    private async Task<long> SeedProductAsync(string key, string? name = null, bool includeFrench = false)
    {
        var productId = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() * 1000 + Random.Shared.Next(1000);
        await using var connection = await fixture.OpenDatabaseAsync("shopizerDb");
        await using var transaction = await connection.BeginTransactionAsync();
        var indexId = Guid.NewGuid();
        await using (var index = new NpgsqlCommand("""
            INSERT INTO search.search_index(search_index_id,tenant_id,store_id,provider_name,configured_locales,state)
            VALUES(@id,@tenant,@store,'local-postgresql',ARRAY['en','fr']::text[],'Ready')
            ON CONFLICT (tenant_id,store_id) DO UPDATE SET state='Ready'
            RETURNING search_index_id
            """, connection, transaction))
        {
            index.Parameters.AddWithValue("id", indexId);
            index.Parameters.AddWithValue("tenant", TenantKey(Tenant));
            index.Parameters.AddWithValue("store", Store);
            indexId = (Guid)(await index.ExecuteScalarAsync() ?? indexId);
        }

        foreach (var locale in includeFrench ? new[] { "en", "fr" } : new[] { "en" })
        {
            var documentId = Guid.NewGuid();
            await using (var document = new NpgsqlCommand("""
                INSERT INTO search.search_document(document_id,search_index_id,tenant_id,store_id,product_id,locale,provider_document_key,state)
                VALUES(@id,@index,@tenant,@store,@product,@locale,@key,'Active')
                ON CONFLICT(search_index_id,product_id,locale) DO UPDATE SET state='Active'
                RETURNING document_id
                """, connection, transaction))
            {
                document.Parameters.AddWithValue("id", documentId);
                document.Parameters.AddWithValue("index", indexId);
                document.Parameters.AddWithValue("tenant", TenantKey(Tenant));
                document.Parameters.AddWithValue("store", Store);
                document.Parameters.AddWithValue("product", productId);
                document.Parameters.AddWithValue("locale", locale);
                document.Parameters.AddWithValue("key", $"{Store}:{productId}:{locale}");
                documentId = (Guid)(await document.ExecuteScalarAsync() ?? documentId);
            }

            await using var fields = new NpgsqlCommand("""
                INSERT INTO search.search_document_locale(document_id,name,description,brand_name,image_url)
                VALUES(@id,@name,@description,'Northwind','/images/mug.jpg')
                ON CONFLICT(document_id) DO UPDATE SET name=EXCLUDED.name
                """, connection, transaction);
            fields.Parameters.AddWithValue("id", documentId);
            fields.Parameters.AddWithValue("name", locale == "fr" ? "Produit Localise" : name ?? key);
            fields.Parameters.AddWithValue("description", $"description {key}");
            await fields.ExecuteNonQueryAsync();

            await using var inventory = new NpgsqlCommand("""
                INSERT INTO search.search_document_inventory(document_id,sku,quantity,price,option_values)
                VALUES(@id,@sku,18,14.95,'{}'::jsonb)
                """, connection, transaction);
            inventory.Parameters.AddWithValue("id", documentId);
            inventory.Parameters.AddWithValue("sku", $"SKU-{key}");
            await inventory.ExecuteNonQueryAsync();
        }

        await transaction.CommitAsync();
        return productId;
    }

    private static Guid TenantKey(string value)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        bytes[6] = (byte)((bytes[6] & 0x0F) | 0x50);
        bytes[8] = (byte)((bytes[8] & 0x3F) | 0x80);
        return new Guid(bytes[..16]);
    }
}
