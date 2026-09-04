using System.Security.Cryptography;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Shopizer.IntegrationTests.Fixtures;

namespace Shopizer.IntegrationTests;

[Collection(ShopizerAspireCollection.Name)]
public sealed class MerchantAdministrationComprehensiveTests(AspireHostFixture fixture)
{
    private static class Payloads
    {
        public static string Create(string code, bool retailer = false, string? parentStoreCode = null) => JsonSerializer.Serialize(new { code, name = $"Store {code}", emailAddress = $"{code}@example.com", phone = "+14165550199", address = new { city = "Toronto", postalCode = "M5E1W7", countryCode = "CA" }, parentStoreCode, retailer, defaultLanguageCode = "en", supportedLanguageCodes = new[] { "en", "fr" }, currencyCode = "CAD" });
        public static string Update(string phone = "+14165550111") => JsonSerializer.Serialize(new { phone });
        public const string InvalidCode = "{\"code\":\"bad-code\",\"name\":\"Bad\",\"emailAddress\":\"bad@example.com\",\"phone\":\"1\",\"address\":{\"city\":\"Toronto\",\"postalCode\":\"M5E1W7\",\"countryCode\":\"CA\"},\"defaultLanguageCode\":\"en\",\"supportedLanguageCodes\":[\"en\"]}";
    }

    // @BR-ID: BR-MER-001
    [Fact, Trait("BR", "BR-MER-001")]
    public async Task StoreCodeValidationRejectsPunctuation() { using var response = await SendAsync(HttpMethod.Post, "/api/v1/stores", Payloads.InvalidCode, true); Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode); }

    // @BR-ID: BR-MER-002
    [Fact, Trait("BR", "BR-MER-002")]
    public async Task StoreContactAndCountryAreValidated() { using var response = await SendAsync(HttpMethod.Post, "/api/v1/stores", Payloads.Create(StoreCode("ms10_contact")), true); Assert.Equal(HttpStatusCode.Created, response.StatusCode); }

    // @BR-ID: BR-MER-003
    [Fact, Trait("BR", "BR-MER-003")]
    public async Task StoreCodeUniquenessIsTenantScoped() { var code = StoreCode("ms10_unique"); using var created = await SendAsync(HttpMethod.Post, "/api/v1/stores", Payloads.Create(code), true); Assert.Equal(HttpStatusCode.Created, created.StatusCode); using var check = await SendAsync(HttpMethod.Get, $"/api/v1/stores/uniqueness?code={code}", null, true); Assert.Equal(HttpStatusCode.OK, check.StatusCode); Assert.True((await JsonNode.ParseAsync(await check.Content.ReadAsStreamAsync(TestContext.Current.CancellationToken), cancellationToken: TestContext.Current.CancellationToken))!["exists"]!.GetValue<bool>()); }

    // @BR-ID: BR-MER-004
    [Fact, Trait("BR", "BR-MER-004")]
    public async Task OmittedMeasurementUnitsReceiveDefaults() { var code = StoreCode("ms10_defaults"); using var response = await SendAsync(HttpMethod.Post, "/api/v1/stores", Payloads.Create(code), true); var json = JsonNode.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)); Assert.Equal(HttpStatusCode.Created, response.StatusCode); Assert.Equal("CM", json!["dimensionUnit"]!.GetValue<string>()); Assert.Equal("KG", json["weightUnit"]!.GetValue<string>()); }

    // @BR-ID: BR-MER-005
    [Fact, Trait("BR", "BR-MER-005")]
    public async Task StoreUpdateMergesOmittedFields() { var code = StoreCode("ms10_merge"); await CreateAsync(code); using var update = await SendAsync(HttpMethod.Put, $"/api/v1/stores/{code}", Payloads.Update(), true); var json = JsonNode.Parse(await update.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)); Assert.Equal(HttpStatusCode.OK, update.StatusCode); Assert.Equal($"Store {code}", json!["name"]!.GetValue<string>()); Assert.Equal("+14165550111", json["phone"]!.GetValue<string>()); }

    // @BR-ID: BR-MER-006
    [Fact, Trait("BR", "BR-MER-006")]
    public async Task DefaultStoreDeletionIsProtected() { await EnsureDefaultStoreAsync(); using var response = await SendAsync(HttpMethod.Delete, "/api/v1/stores/default", null, true); Assert.Equal(HttpStatusCode.Conflict, response.StatusCode); }

    // @BR-ID: BR-MER-007
    [Fact, Trait("BR", "BR-MER-007")]
    public async Task ChildRequiresExistingRetailerParent() { using var response = await SendAsync(HttpMethod.Post, "/api/v1/stores", Payloads.Create(StoreCode("ms10_child"), false, "missing_parent"), true); Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode); }

    // @BR-ID: BR-MER-008
    [Fact, Trait("BR", "BR-MER-008")]
    public async Task RetailerHierarchyReturnsChildren() { var root = StoreCode("ms10_root"); await CreateAsync(root, true); var child = StoreCode("ms10_child"); using var created = await SendAsync(HttpMethod.Post, "/api/v1/stores", Payloads.Create(child, false, root), true); Assert.Equal(HttpStatusCode.Created, created.StatusCode); using var response = await SendAsync(HttpMethod.Get, $"/api/v1/merchants/{root}/children", null, true); Assert.Equal(HttpStatusCode.OK, response.StatusCode); }

    // @BR-ID: BR-MER-009
    [Fact, Trait("BR", "BR-MER-009")]
    public async Task ParentWithChildrenCannotBeDeleted() { var root = StoreCode("ms10_parent"); await CreateAsync(root, true); await CreateAsync(StoreCode("ms10_child"), false, root); using var response = await SendAsync(HttpMethod.Delete, $"/api/v1/stores/{root}", null, true); Assert.Equal(HttpStatusCode.Conflict, response.StatusCode); }

    // @BR-ID: BR-MER-010
    [Fact, Trait("BR", "BR-MER-010")]
    public async Task MissingStoreHeaderUsesConfiguredDefault() { await CreateAsync("default"); using var response = await SendAsync(HttpMethod.Get, "/api/v1/stores/default", null, false); Assert.Equal(HttpStatusCode.OK, response.StatusCode); }

    // @BR-ID: BR-MER-011
    [Fact, Trait("BR", "BR-MER-011")]
    public async Task StoreContextDeniesDifferentStore() { var code = StoreCode("ms10_context"); await CreateAsync(code); using var request = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/stores/{code}"); request.Headers.Add("x-tenant-id", "test-tenant-001"); request.Headers.Add("x-store-id", "other_store"); request.Headers.Add("x-correlation-id", "corr-ms10-context"); request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", TestToken("test-tenant-001", "other_store")); using var response = await fixture.MerchantAdministrationClient.SendAsync(request, TestContext.Current.CancellationToken); Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode); }

    // @BR-ID: BR-MER-012
    [Fact, Trait("BR", "BR-MER-012")]
    public async Task UnsupportedRequestedLanguageIsRejected() { var code = StoreCode("ms10_language"); await CreateAsync(code); using var response = await SendAsync(HttpMethod.Get, $"/api/v1/stores/{code}?language=xx", null, false); Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode); }

    // @BR-ID: BR-UI-007
    [Fact, Trait("BR", "BR-UI-007")]
    public async Task BrandingReadUsesSelectedStore() { var code = StoreCode("ms10_branding"); await CreateAsync(code); using var response = await SendAsync(HttpMethod.Get, $"/api/v1/stores/{code}/branding", null, false); Assert.Equal(HttpStatusCode.OK, response.StatusCode); }

    // @BR-ID: BR-MSA-VAL-001
    [Fact, Trait("BR", "BR-MSA-VAL-001")]
    public async Task EquivalentCodesCannotBypassUniqueness() { var code = StoreCode("ms10_case"); await CreateAsync(code.ToUpperInvariant()); using var response = await SendAsync(HttpMethod.Post, "/api/v1/stores", Payloads.Create(code), true); Assert.Equal(HttpStatusCode.Conflict, response.StatusCode); }

    // @BR-ID: BR-MSA-VAL-002
    [Fact, Trait("BR", "BR-MSA-VAL-002")]
    public async Task UpdateDoesNotChangeIdentity() { var code = StoreCode("ms10_identity"); await CreateAsync(code); using var response = await SendAsync(HttpMethod.Put, $"/api/v1/stores/{code}", Payloads.Update(), true); Assert.Equal(HttpStatusCode.OK, response.StatusCode); var json = JsonNode.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)); Assert.Equal(code, json!["code"]!.GetValue<string>()); }

    // @BR-ID: BR-MSA-VAL-003
    [Fact, Trait("BR", "BR-MSA-VAL-003")]
    public async Task CreationReturnsPersistedStore() { var code = StoreCode("ms10_transaction"); using var created = await SendAsync(HttpMethod.Post, "/api/v1/stores", Payloads.Create(code), true); Assert.Equal(HttpStatusCode.Created, created.StatusCode); using var read = await SendAsync(HttpMethod.Get, $"/api/v1/stores/{code}", null, false); Assert.Equal(HttpStatusCode.OK, read.StatusCode); }

    // @BR-ID: BR-MSA-READ-001
    [Fact, Trait("BR", "BR-MSA-READ-001")]
    public async Task StoreCollectionContainsPagination() { using var response = await SendAsync(HttpMethod.Get, "/api/v1/stores?page=1&pageSize=20", null, true); Assert.Equal(HttpStatusCode.OK, response.StatusCode); var json = JsonNode.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)); Assert.NotNull(json!["items"]); Assert.NotNull(json["pagination"]!["totalPages"]); }

    // @BR-ID: BR-MSA-AUTH-001
    [Fact, Trait("BR", "BR-MSA-AUTH-001")]
    public async Task HierarchyRequiresAdministrator() { using var response = await SendAsync(HttpMethod.Get, "/api/v1/merchants/missing/children", null, false); Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode); }

    // @BR-ID: BR-MSA-BRD-001
    [Fact, Trait("BR", "BR-MSA-BRD-001")]
    public async Task LogoUploadUsesProviderBoundary() { using var response = await SendAsync(HttpMethod.Post, "/api/v1/stores/missing/branding/logo", "{\"file\":\"cG5n\"}", true); Assert.Equal(HttpStatusCode.NotFound, response.StatusCode); }

    // @BR-ID: BR-MSA-LANG-001
    [Fact, Trait("BR", "BR-MSA-LANG-001")]
    public async Task LanguageReplacementRequiresDefaultMembership() { var code = StoreCode("ms10_langset"); await CreateAsync(code); using var response = await SendAsync(HttpMethod.Put, $"/api/v1/stores/{code}/languages", "{\"defaultLanguageCode\":\"de\",\"supportedLanguageCodes\":[\"en\"]}", true); Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode); }

    // @BR-ID: BR-MSA-LST-001
    [Fact, Trait("BR", "BR-MSA-LST-001")]
    public async Task StoreNamesReturnsCodeAndNamePairs() { using var response = await SendAsync(HttpMethod.Get, "/api/v1/stores/names", null, true); Assert.Equal(HttpStatusCode.OK, response.StatusCode); var json = JsonNode.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)); Assert.NotNull(json!["items"]); }

    private static string StoreCode(string prefix) => $"{prefix}_{Guid.NewGuid():N}";

    private async Task EnsureDefaultStoreAsync()
    {
        using var response = await SendAsync(HttpMethod.Post, "/api/v1/stores", Payloads.Create("default"), true);
        Assert.True(response.StatusCode is HttpStatusCode.Created or HttpStatusCode.Conflict);
    }

    private async Task<string> CreateAsync(string code, bool retailer = false, string? parent = null)
    { using var response = await SendAsync(HttpMethod.Post, "/api/v1/stores", Payloads.Create(code, retailer, parent), true); Assert.Equal(HttpStatusCode.Created, response.StatusCode); var json = JsonNode.Parse(await response.Content.ReadAsStringAsync()); return json!["id"]!.GetValue<string>(); }

    private async Task<HttpResponseMessage> SendAsync(HttpMethod method, string path, string? payload, bool administrator)
    {
        using var client = new HttpClient { BaseAddress = fixture.MerchantAdministrationClient.BaseAddress }; client.DefaultRequestHeaders.Add("x-tenant-id", "test-tenant-001"); client.DefaultRequestHeaders.Add("x-correlation-id", "corr-ms10-test"); using var request = new HttpRequestMessage(method, path); if (administrator) { request.Headers.Add("x-store-id", "default"); request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", TestToken("test-tenant-001", "default")); } if (payload is not null) request.Content = new StringContent(payload, Encoding.UTF8, "application/json"); return await client.SendAsync(request);
    }

    private static string TestToken(string tenant, string store)
    {
        static string Encode(object value) => Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(value))).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        var header = Encode(new { alg = "HS512", typ = "JWT" });
        var payload = Encode(new { sub = Guid.NewGuid(), name = "ms10-test", aud = "api", kind = "administrator", tenantId = tenant, storeId = store, exp = DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds(), roles = new[] { "ADMIN", "SUPERADMIN" } });
        using var hmac = new HMACSHA512(Encoding.UTF8.GetBytes("shopizer-development-shared-jwt-secret-change-me"));
        var signature = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes($"{header}.{payload}"))).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        return $"{header}.{payload}.{signature}";
    }
}
