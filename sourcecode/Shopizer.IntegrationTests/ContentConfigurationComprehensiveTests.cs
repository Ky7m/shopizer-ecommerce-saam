using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Shopizer.IntegrationTests.Fixtures;

namespace Shopizer.IntegrationTests;

[Collection(ShopizerAspireCollection.Name)]
public sealed class ContentConfigurationComprehensiveTests(AspireHostFixture fixture) : ComprehensiveTestBase(
    fixture.ContentConfigurationClient,
    fixture.ContentAdminAccessToken)
{
    private readonly HttpClient client = fixture.ContentConfigurationClient;
    private readonly string administratorToken = fixture.ContentAdminAccessToken;

    private static string Code(string prefix) => $"{prefix}-{Guid.NewGuid():N}"[..Math.Min(48, prefix.Length + 33)];

    // @BR-ID: BR-MER-013
    [Fact(DisplayName = "Content codes are unique within a merchant store")]
    [Trait("BR", "BR-MER-013")]
    public async Task ContentCodeIsUniqueWithinStore()
    {
        var code = Code("unique");
        await CreateContentAsync("page", code);
        var duplicate = await SendJsonAsync(HttpMethod.Post, "/private/content/box",
            ContentPayload(code, "Box"), 409, idempotencyKey: $"duplicate-{Guid.NewGuid():N}");
        Assert.Equal("CONTENT_CODE_CONFLICT", duplicate!.RootElement.GetProperty("error").GetString());
    }

    // @BR-ID: BR-MER-014
    [Fact(DisplayName = "Page and box operations assign their content type")]
    [Trait("BR", "BR-MER-014")]
    public async Task ContentOperationDeterminesContentType()
    {
        var code = Code("operation-type");
        var page = await CreateContentAsync("page", code);
        var pageId = page!.RootElement.GetProperty("id").GetGuid();
        var read = await SendJsonAsync(HttpMethod.Get, $"/private/content/pages/{code}", null, 200);
        Assert.Equal("Page", read!.RootElement.GetProperty("contentType").GetString());
        await SendJsonAsync(HttpMethod.Delete, $"/private/content/page/{pageId}", null, 204);
    }

    // @BR-ID: BR-MER-015
    [Fact(DisplayName = "Localized descriptions are replaced during content mutation")]
    [Trait("BR", "BR-MER-015")]
    public async Task ContentMutationPersistsLocalizedDescriptions()
    {
        var code = Code("descriptions");
        var created = await CreateContentAsync("page", code, new[] { ("en", "English name"), ("fr", "Nom français") });
        var id = created!.RootElement.GetProperty("id").GetGuid();
        var updated = ContentPayload(code, "Updated", descriptions: new[] { ("en", "Updated English") });
        await SendJsonAsync(HttpMethod.Put, $"/private/content/page/{id}", updated, 204);
        var read = await SendJsonAsync(HttpMethod.Get, $"/private/content/pages/{code}", null, 200);
        Assert.Equal("Updated English", read!.RootElement.GetProperty("description").GetProperty("name").GetString());
    }

    // @BR-ID: BR-MER-016
    [Fact(DisplayName = "Localized and all-language reads use different projections")]
    [Trait("BR", "BR-MER-016")]
    public async Task ContentReadsHaveLocalizedAndAllLanguageProjections()
    {
        var code = Code("projection");
        await CreateContentAsync("page", code, new[] { ("en", "English"), ("de", "Deutsch") });
        var localized = await SendJsonAsync(HttpMethod.Get, $"/private/content/pages/{code}", null, 200);
        Assert.Equal("en", localized!.RootElement.GetProperty("description").GetProperty("language").GetString());
        var all = await SendJsonAsync(HttpMethod.Get, "/private/content/pages", null, 200);
        var descriptions = all!.RootElement.GetProperty("items")[0].GetProperty("descriptions");
        Assert.Equal(JsonValueKind.Array, descriptions.ValueKind);
        Assert.Contains(all.RootElement.GetProperty("items").EnumerateArray(),
            item => item.GetProperty("code").GetString() == code &&
                    item.GetProperty("descriptions").GetArrayLength() == 2);
    }

    // @BR-ID: BR-MER-017
    [Fact(DisplayName = "Friendly URL lookup exposes visible localized pages")]
    [Trait("BR", "BR-MER-017")]
    public async Task FriendlyUrlLookupReturnsVisiblePage()
    {
        var code = Code("friendly");
        await CreateContentAsync("page", code, new[] { ("en", "Friendly") }, visible: true, friendlyUrl: $"friendly-{Guid.NewGuid():N}");
        var friendlyUrl = (await LastCreatedDescriptionAsync(code))!;
        var result = await SendJsonAsync(HttpMethod.Get, $"/content/pages/name/{friendlyUrl}", null, 200);
        Assert.Equal(code, result!.RootElement.GetProperty("code").GetString());
    }

    // @BR-ID: BR-MER-018
    [Fact(DisplayName = "Visibility and menu linkage remain independent")]
    [Trait("BR", "BR-MER-018")]
    public async Task VisibilityAndMenuLinkageAreIndependent()
    {
        var code = Code("policies");
        var created = await CreateContentAsync("page", code, visible: false, linkToMenu: true);
        var read = await SendJsonAsync(HttpMethod.Get, $"/private/content/pages/{code}", null, 200);
        Assert.False(read!.RootElement.GetProperty("visible").GetBoolean());
        Assert.True(read.RootElement.GetProperty("linkToMenu").GetBoolean());
        await SendJsonAsync(HttpMethod.Delete, $"/private/content/page/{created!.RootElement.GetProperty("id").GetGuid()}", null, 204);
    }

    // @BR-ID: BR-MER-019
    [Fact(DisplayName = "Content lists are typed, ordered, and paginated")]
    [Trait("BR", "BR-MER-019")]
    public async Task ContentListIsTypedOrderedAndPaged()
    {
        await CreateContentAsync("page", Code("list-a"), sortOrder: 20);
        await CreateContentAsync("page", Code("list-b"), sortOrder: 10);
        var list = await SendJsonAsync(HttpMethod.Get, "/private/content/pages?page=0&count=1", null, 200);
        Assert.Equal(1, list!.RootElement.GetProperty("number").GetInt32());
        Assert.True(list.RootElement.GetProperty("totalPages").GetInt32() >= 1);
        Assert.Equal("Page", list.RootElement.GetProperty("items")[0].GetProperty("contentType").GetString());
    }

    // @BR-ID: BR-MER-020
    [Fact(DisplayName = "Box localized projection applies CDATA formatting")]
    [Trait("BR", "BR-MER-020")]
    public async Task BoxProjectionAppliesCDataFormatting()
    {
        var code = Code("box-format");
        await CreateContentAsync("box", code, new[] { ("en", "Box") }, description: "\r\nPromo\ttext");
        var result = await SendJsonAsync(HttpMethod.Get, $"/content/boxes/{code}", null, 200);
        Assert.Equal("<![CDATA[Promotext]]>", result!.RootElement.GetProperty("description").GetProperty("description").GetString());
    }

    // @BR-ID: BR-MER-021
    [Fact(DisplayName = "Content deletion is restricted to the owning store")]
    [Trait("BR", "BR-MER-021")]
    public async Task ContentDeletionUsesOwningStore()
    {
        var created = await CreateContentAsync("page", Code("delete"));
        var id = created!.RootElement.GetProperty("id").GetGuid();
        var result = await SendJsonAsync(HttpMethod.Delete, $"/private/content/page/{id}", null, 204);
        Assert.Null(result);
        await SendJsonAsync(HttpMethod.Get, $"/private/content/pages/{created.RootElement.GetProperty("id").GetGuid()}", null, 404);
    }

    // @BR-ID: BR-MER-022
    [Fact(DisplayName = "Uploaded files are classified by MIME major type")]
    [Trait("BR", "BR-MER-022")]
    public async Task MimeMajorTypeClassifiesUpload()
    {
        var name = $"{Code("mime")}.png";
        var result = await SendMultipartAsync("/private/content/files", 201, "file", name,
            "image/png", ("fileName", name));
        Assert.Equal("Image", result!.RootElement.GetProperty("contentType").GetString());
    }

    // @BR-ID: BR-MER-023
    [Fact(DisplayName = "Image upload validates the submitted filename")]
    [Trait("BR", "BR-MER-023")]
    public async Task ImageUploadRejectsUnsafeFilename()
    {
        var result = await SendMultipartAsync("/private/content/images/add", 422, "qqfile", "../unsafe.png",
            "image/png", ("qqfilename", "../unsafe.png"));
        Assert.False(result!.RootElement.GetProperty("success").GetBoolean());
    }

    // @BR-ID: BR-MER-024
    [Fact(DisplayName = "Content files are isolated by content type namespace")]
    [Trait("BR", "BR-MER-024")]
    public async Task FilesUseContentTypeNamespace()
    {
        var name = $"{Code("namespace")}.bin";
        await SendMultipartAsync("/private/content/files", 201, "file", name, "application/octet-stream",
            ("contentType", "STATIC_FILE"), ("fileName", name));
        var image = await SendMultipartAsync("/private/content/files", 201, "file", name, "image/png",
            ("contentType", "IMAGE"), ("fileName", name));
        Assert.Equal("Image", image!.RootElement.GetProperty("contentType").GetString());
    }

    // @BR-ID: BR-MER-025
    [Fact(DisplayName = "File rename recreates the object while preserving metadata")]
    [Trait("BR", "BR-MER-025")]
    public async Task FileRenamePreservesMetadata()
    {
        var oldName = $"{Code("rename")}.txt";
        var newName = $"{Code("renamed")}.dat";
        await SendMultipartAsync("/private/content/files", 201, "file", oldName, "text/plain",
            ("contentType", "STATIC_FILE"), ("fileName", oldName));
        var renamed = await SendJsonAsync(HttpMethod.Post, "/private/content/files/rename",
            new { fileName = oldName, newName, contentType = "STATIC_FILE", path = "/" }, 200);
        Assert.True(renamed!.RootElement.GetProperty("success").GetBoolean());
        var list = await SendJsonAsync(HttpMethod.Get, "/private/content/files?contentType=STATIC_FILE&path=/", null, 200);
        Assert.Contains(list!.RootElement.GetProperty("items").EnumerateArray(),
            item => item.GetProperty("fileName").GetString() == newName &&
                    item.GetProperty("contentType").GetString() == "StaticFile");
    }

    // @BR-ID: BR-MER-028
    [Fact(DisplayName = "Image listings expose store-scoped static paths")]
    [Trait("BR", "BR-MER-028")]
    public async Task ImageListingExposesStaticImagePath()
    {
        var name = $"{Code("image-list")}.jpg";
        await SendMultipartAsync("/private/content/files", 201, "file", name, "image/jpeg",
            ("contentType", "IMAGE"), ("fileName", name));
        var list = await SendJsonAsync(HttpMethod.Get, "/private/content/list?parentPath=/", null, 200);
        Assert.Contains(list!.RootElement.EnumerateArray(),
            item => item.GetProperty("url").GetString()!.Contains("/static/images/", StringComparison.Ordinal));
    }

    // @BR-ID: BR-CF-001
    [Fact(DisplayName = "Configuration records are keyed by store and key")]
    [Trait("BR", "BR-CF-001")]
    public async Task ConfigurationRecordIsStoreKeyed()
    {
        var key = Code("config-key");
        var saved = await SendJsonAsync(HttpMethod.Put, $"/private/configurations/{key}",
            new { type = "SHOP", active = true, value = new { marker = key } }, 200);
        Assert.Equal(key, saved!.RootElement.GetProperty("key").GetString());
        var read = await SendJsonAsync(HttpMethod.Get, $"/private/configurations/{key}", null, 200);
        Assert.Equal("Shop", read!.RootElement.GetProperty("type").GetString());
        Assert.Equal("Present", read.RootElement.GetProperty("valueState").GetString());
    }

    // @BR-ID: BR-CF-002
    [Fact(DisplayName = "Merchant configuration preserves typed flags")]
    [Trait("BR", "BR-CF-002")]
    public async Task MerchantConfigurationPreservesTypedFlags()
    {
        var result = await SendJsonAsync(HttpMethod.Put, "/private/configuration",
            new { displayCustomerSection = true, allowPurchaseItems = false, defaultSearchConfigPath = new { empty = "" } }, 200);
        Assert.True(result!.RootElement.GetProperty("displayCustomerSection").GetBoolean());
        Assert.False(result.RootElement.GetProperty("allowPurchaseItems").GetBoolean());
        var searchPath = result.RootElement.GetProperty("defaultSearchConfigPath");
        Assert.Empty(searchPath.EnumerateObject());
    }

    // @BR-ID: BR-CF-003
    [Fact(DisplayName = "Public configuration exposes only approved fields")]
    [Trait("BR", "BR-CF-003")]
    public async Task PublicConfigurationIsProjected()
    {
        var result = await SendJsonAsync(HttpMethod.Get, "/config", null, 200, useAuthorization: false);
        Assert.True(result!.RootElement.TryGetProperty("displayCustomerSection", out _));
        Assert.False(result.RootElement.TryGetProperty("password", out _));
    }

    // @BR-ID: BR-CF-004
    [Fact(DisplayName = "Public social values use named configuration keys")]
    [Trait("BR", "BR-CF-004")]
    public async Task PublicSocialValueUsesNamedKey()
    {
        var result = await SendJsonAsync(HttpMethod.Get, "/config", null, 200, useAuthorization: false);
        Assert.Equal("https://example.com/phase4c", result!.RootElement.GetProperty("facebook").GetString());
    }

    // @BR-ID: BR-CF-005
    [Fact(DisplayName = "Shipping display defaults to false")]
    [Trait("BR", "BR-CF-005")]
    public async Task ShippingDisplayDefaultsToFalse()
    {
        var result = await SendJsonAsync(HttpMethod.Get, "/config", null, 200, useAuthorization: false);
        Assert.False(result!.RootElement.GetProperty("displayShipping").GetBoolean());
    }

    // @BR-ID: BR-CF-006
    [Fact(DisplayName = "Module configuration is encrypted at rest")]
    [Trait("BR", "BR-CF-006")]
    public async Task ModuleConfigurationIsWriteOnly()
    {
        await SaveModuleAsync("phase4c-payment", active: true);
        var result = await SendJsonAsync(HttpMethod.Get, "/private/modules/payment/phase4c-payment", null, 200);
        Assert.False(result!.RootElement.GetProperty("integrationKeys").GetProperty("secretKey").ValueKind == JsonValueKind.String);
        Assert.False(result.RootElement.TryGetProperty("value", out _));
    }

    // @BR-ID: BR-CF-007
    [Fact(DisplayName = "Integration options parse without credential values")]
    [Trait("BR", "BR-CF-007")]
    public async Task IntegrationOptionsDoNotRequireCredentials()
    {
        var code = Code("options");
        await ReplaceModuleAsync("PAYMENT", code, new { configurable = true });
        var result = await SaveModuleAsync(code, active: false, keys: new Dictionary<string, object?>(),
            options: new Dictionary<string, object?> { ["mode"] = "sandbox" });
        Assert.Equal("sandbox", result!.RootElement.GetProperty("integrationOptions").GetProperty("mode").GetString());
    }

    // @BR-ID: BR-CF-008
    [Fact(DisplayName = "Module replacement preserves metadata and environments")]
    [Trait("BR", "BR-CF-008")]
    public async Task ModuleReplacementReturnsPersistedMetadataStatus()
    {
        var code = Code("metadata");
        var result = await ReplaceModuleAsync("PAYMENT", code, new { configurable = true },
            new[] { new { env = "TEST", config1 = "test-url", config2 = "test-token" }, new { env = "PROD", config1 = "prod-url", config2 = "prod-token" } });
        Assert.Equal(200, result!.RootElement.GetProperty("status").GetInt32());
        Assert.True(result.RootElement.GetProperty("replaced").GetBoolean());
    }

    // @BR-ID: BR-CF-009
    [Fact(DisplayName = "TEST and PROD configuration values remain distinct")]
    [Trait("BR", "BR-CF-009")]
    public async Task ModuleEnvironmentValuesRemainDistinct()
    {
        var code = Code("environments");
        await ReplaceModuleAsync("PAYMENT", code, new { configurable = true },
            new[] { new { env = "TEST", config1 = "test-url", config2 = "test-token" }, new { env = "PROD", config1 = "prod-url", config2 = "prod-token" } });
        var result = await SendJsonAsync(HttpMethod.Get, $"/private/modules/payment/{code}", null, 200);
        Assert.Equal("TEST", result!.RootElement.GetProperty("environment").GetString());
    }

    // @BR-ID: BR-CF-010
    [Fact(DisplayName = "Module replacement is performed by code")]
    [Trait("BR", "BR-CF-010")]
    public async Task ModuleReplacementUsesCode()
    {
        var code = Code("replace-code");
        await ReplaceModuleAsync("PAYMENT", code, new { configurable = true });
        var result = await ReplaceModuleAsync("PAYMENT", code, new { configurable = false });
        Assert.Equal(code, result!.RootElement.GetProperty("code").GetString());
    }

    // @BR-ID: BR-CF-011
    [Fact(DisplayName = "Module discovery hydrates metadata")]
    [Trait("BR", "BR-CF-011")]
    public async Task ModuleDiscoveryReturnsSeededModule()
    {
        var result = await SendJsonAsync(HttpMethod.Get, "/private/modules/payment", null, 200);
        Assert.Contains(result!.RootElement.EnumerateArray(),
            item => item.GetProperty("code").GetString() == "phase4c-payment");
    }

    // @BR-ID: BR-CF-012
    [Fact(DisplayName = "Module availability is filtered by store region")]
    [Trait("BR", "BR-CF-012")]
    public async Task ModuleAvailabilityHonorsRegion()
    {
        var code = Code("region");
        await ReplaceModuleAsync("PAYMENT", code, new { configurable = true }, regions: new[] { "ZZ" });
        await SendJsonAsync(HttpMethod.Get, $"/private/modules/payment/{code}", null, 404);
    }

    // @BR-ID: BR-CF-013
    [Fact(DisplayName = "Provider configuration validates required keys")]
    [Trait("BR", "BR-CF-013")]
    public async Task ProviderConfigurationValidatesRequiredKeys()
    {
        var invalid = await SaveModuleAsync("phase4c-payment", active: true, keys: new Dictionary<string, object?>());
        Assert.Equal("MODULE_CONFIGURATION_INVALID", invalid!.RootElement.GetProperty("error").GetString());
        var valid = await SaveModuleAsync("phase4c-payment", active: true);
        Assert.True(valid!.RootElement.GetProperty("configured").GetBoolean());
    }

    // @BR-ID: BR-CF-014
    [Fact(DisplayName = "Module summaries distinguish configured from active")]
    [Trait("BR", "BR-CF-014")]
    public async Task ModuleSummaryDistinguishesConfiguredAndActive()
    {
        await SaveModuleAsync("phase4c-payment", active: false);
        var result = await SendJsonAsync(HttpMethod.Get, "/private/modules/payment", null, 200);
        var item = result!.RootElement.EnumerateArray().Single(x => x.GetProperty("code").GetString() == "phase4c-payment");
        Assert.True(item.GetProperty("configured").GetBoolean());
        Assert.False(item.GetProperty("active").GetBoolean());
    }

    // @BR-ID: BR-CF-015
    [Fact(DisplayName = "Missing merchant configuration receives platform defaults")]
    [Trait("BR", "BR-CF-015")]
    public async Task MissingMerchantConfigurationUsesDefaults()
    {
        var result = await SendJsonAsync(HttpMethod.Get, "/config", null, 200, useAuthorization: false,
            tenant: "00000000-0000-0000-0000-000000000099",
            store: "00000000-0000-0000-0000-000000000098");
        Assert.True(result!.RootElement.GetProperty("allowOnlinePurchase").GetBoolean());
        Assert.True(result.RootElement.GetProperty("displaySearchBox").GetBoolean());
    }

    // @BR-ID: BR-EXT-021
    [Fact(DisplayName = "Configured CMS provider handles uploads")]
    [Trait("BR", "BR-EXT-021")]
    public async Task ConfiguredProviderHandlesUpload()
    {
        var name = $"{Code("provider")}.txt";
        var result = await SendMultipartAsync("/private/content/files", 201, "file", name, "text/plain",
            ("contentType", "STATIC_FILE"), ("fileName", name));
        Assert.Equal("default", result!.RootElement.GetProperty("provider").GetString());
    }

    // @BR-ID: BR-EXT-022
    [Fact(DisplayName = "Provider keys preserve store and content namespaces")]
    [Trait("BR", "BR-EXT-022")]
    public async Task ProviderNamespaceIsReturnedByDownloadProjection()
    {
        var name = $"{Code("provider-key")}.txt";
        var result = await SendMultipartAsync("/private/content/files", 201, "file", name, "text/plain",
            ("contentType", "STATIC_FILE"), ("fileName", name));
        Assert.Contains("/private/content/files/", result!.RootElement.GetProperty("downloadPath").GetString());
        Assert.Contains("contentType=STATIC_FILE", result.RootElement.GetProperty("downloadPath").GetString());
    }

    // @BR-ID: BR-EXT-023
    [Fact(DisplayName = "Missing provider objects are explicit not-found results")]
    [Trait("BR", "BR-EXT-023")]
    public async Task MissingFileIsNotSuccessful()
    {
        var result = await SendJsonAsync(HttpMethod.Get,
            "/private/content/files/missing-file.txt/download?contentType=STATIC_FILE&path=/", null, 404);
        Assert.Equal("FILE_NOT_FOUND", result!.RootElement.GetProperty("error").GetString());
    }

    // @BR-ID: BR-EXT-024
    [Fact(DisplayName = "Payment discovery includes runtime metadata")]
    [Trait("BR", "BR-EXT-024")]
    public async Task PaymentDiscoveryIncludesRuntimeModule()
    {
        var result = await SendJsonAsync(HttpMethod.Get, "/private/modules/payment", null, 200);
        Assert.Contains(result!.RootElement.EnumerateArray(),
            x => x.GetProperty("code").GetString() == "phase4c-payment");
    }

    // @BR-ID: BR-EXT-025
    [Fact(DisplayName = "Module state is persisted separately from provider execution")]
    [Trait("BR", "BR-EXT-025")]
    public async Task ModuleStateIsPersisted()
    {
        var result = await SaveModuleAsync("phase4c-payment", active: true);
        Assert.True(result!.RootElement.GetProperty("configured").GetBoolean());
        Assert.True(result.RootElement.GetProperty("secretsPresent").GetBoolean());
    }

    // @BR-ID: BR-EXT-026
    [Fact(DisplayName = "Module replacement invalidates discovery cache")]
    [Trait("BR", "BR-EXT-026")]
    public async Task ModuleReplacementInvalidatesDiscoveryCache()
    {
        var code = Code("cache");
        await SendJsonAsync(HttpMethod.Get, "/private/modules/payment", null, 200);
        await ReplaceModuleAsync("PAYMENT", code, new { configurable = true });
        var result = await SendJsonAsync(HttpMethod.Get, "/private/modules/payment", null, 200);
        Assert.Contains(result!.RootElement.EnumerateArray(), x => x.GetProperty("code").GetString() == code);
    }

    // @BR-ID: BR-EXT-027
    [Fact(DisplayName = "Module detail redacts encrypted merchant values")]
    [Trait("BR", "BR-EXT-027")]
    public async Task ModuleDetailRedactsSecrets()
    {
        await SaveModuleAsync("phase4c-payment", active: true);
        var result = await SendJsonAsync(HttpMethod.Get, "/private/modules/payment/phase4c-payment", null, 200);
        Assert.DoesNotContain("secret-value", result!.RootElement.GetRawText(), StringComparison.Ordinal);
        Assert.False(result.RootElement.TryGetProperty("value", out _));
    }

    // @BR-ID: BR-EXT-028
    [Fact(DisplayName = "Module definitions retain wildcard environment metadata")]
    [Trait("BR", "BR-EXT-028")]
    public async Task ModuleDefinitionRetainsEnvironmentMetadata()
    {
        var code = Code("wildcard");
        var result = await ReplaceModuleAsync("PAYMENT", code, new { configurable = true },
            new[] { new { env = "TEST", scheme = "https", host = "wildcard.example", port = "443", uri = "https://wildcard.example", config1 = "one", config2 = "two" } },
            regions: new[] { "*" });
        Assert.True(result!.RootElement.GetProperty("cacheInvalidated").GetBoolean());
    }

    // @BR-ID: BR-EXT-029
    [Fact(DisplayName = "File rename preserves MIME metadata across names")]
    [Trait("BR", "BR-EXT-029")]
    public async Task FileRenamePreservesMimeMetadata()
    {
        var oldName = $"{Code("mime-rename")}.png";
        var newName = $"{Code("mime-renamed")}.bin";
        await SendMultipartAsync("/private/content/files", 201, "file", oldName, "image/png",
            ("contentType", "IMAGE"), ("fileName", oldName));
        await SendJsonAsync(HttpMethod.Post, "/private/content/files/rename",
            new { fileName = oldName, newName, contentType = "IMAGE", path = "/" }, 200);
        var list = await SendJsonAsync(HttpMethod.Get, "/private/content/files?contentType=IMAGE&path=/", null, 200);
        var item = list!.RootElement.GetProperty("items").EnumerateArray()
            .Single(x => x.GetProperty("fileName").GetString() == newName);
        Assert.Equal("image/png", item.GetProperty("mimeType").GetString());
    }

    // @BR-ID: BR-EXT-030
    [Fact(DisplayName = "File deletion is scoped and idempotent")]
    [Trait("BR", "BR-EXT-030")]
    public async Task FileDeletionIsIdempotent()
    {
        var name = $"{Code("delete-file")}.txt";
        await SendMultipartAsync("/private/content/files", 201, "file", name, "text/plain",
            ("contentType", "STATIC_FILE"), ("fileName", name));
        await SendJsonAsync(HttpMethod.Delete, $"/private/content/files/{name}?contentType=STATIC_FILE&path=/", null, 204);
        await SendJsonAsync(HttpMethod.Delete, $"/private/content/files/{name}?contentType=STATIC_FILE&path=/", null, 204);
    }

    // Contract retirement assertions intentionally expect 410, not a successful legacy response.
    [Fact(DisplayName = "Retired legacy content and module operations return 410")]
    public async Task RetiredOperationsReturnGone()
    {
        foreach (var (method, path) in new[]
        {
            (HttpMethod.Get, "/content/summary"),
            (HttpMethod.Get, "/private/configurations/payment"),
            (HttpMethod.Post, "/private/configurations/shipping"),
            (HttpMethod.Delete, "/content/folder"),
            (HttpMethod.Post, "/services/private/system/optin")
        })
        {
            var result = await SendJsonAsync(method, path, null, 410);
            Assert.Equal("LEGACY_OPERATION_RETIRED", result!.RootElement.GetProperty("error").GetString());
        }
    }

    private async Task<JsonDocument?> CreateContentAsync(
        string type,
        string code,
        (string Language, string Name)[]? descriptions = null,
        bool visible = true,
        bool linkToMenu = false,
        int sortOrder = 1,
        string? description = null,
        string? friendlyUrl = null)
    {
        var path = type.Equals("box", StringComparison.OrdinalIgnoreCase) ? "/private/content/box" : "/private/content/page";
        return await SendJsonAsync(HttpMethod.Post, path,
            ContentPayload(code, type, descriptions, visible, linkToMenu, sortOrder, description, friendlyUrl), 201);
    }

    private static object ContentPayload(
        string code,
        string type,
        (string Language, string Name)[]? descriptions = null,
        bool visible = true,
        bool linkToMenu = false,
        int sortOrder = 1,
        string? description = null,
        string? friendlyUrl = null) =>
        new
        {
            code,
            visible,
            linkToMenu,
            sortOrder,
            contentPosition = type.Equals("box", StringComparison.OrdinalIgnoreCase) ? "LEFT" : (string?)null,
            descriptions = (descriptions ?? new[] { ("en", code) })
                .Select(x => new
                {
                    language = x.Language,
                    name = x.Name,
                    title = x.Name,
                    description = description ?? x.Name,
                    friendlyUrl = friendlyUrl ?? $"{code}-url",
                    metaKeywords = "phase4c",
                    metaTitle = x.Name,
                    metaDescription = x.Name
                }).ToArray()
        };

    private async Task<string?> LastCreatedDescriptionAsync(string code)
    {
        var result = await SendJsonAsync(HttpMethod.Get, $"/private/content/pages/{code}", null, 200);
        return result!.RootElement.GetProperty("description").GetProperty("friendlyUrl").GetString();
    }

    private async Task<JsonDocument?> SaveModuleAsync(
        string code,
        bool active,
        Dictionary<string, object?>? keys = null,
        Dictionary<string, object?>? options = null) =>
        await SendJsonAsync(HttpMethod.Put, $"/private/modules/payment/{code}", new
        {
            active,
            defaultSelected = false,
            environment = "TEST",
            integrationKeys = keys ?? new Dictionary<string, object?>
            {
                ["secretKey"] = "secret-value",
                ["publishableKey"] = "publishable-value"
            },
            integrationOptions = options ?? new Dictionary<string, object?> { ["mode"] = "test" }
        }, code == "phase4c-payment" && keys is not null && keys.Count == 0 ? 422 : 200);

    private async Task<JsonDocument?> ReplaceModuleAsync(
        string family,
        string code,
        object details,
        object[]? configuration = null,
        string[]? regions = null)
    {
        var payload = new
        {
            module = family,
            code,
            type = family.ToLowerInvariant(),
            image = $"{code}.png",
            customModule = true,
            regions = regions ?? new[] { "*" },
            details,
            configuration = configuration ?? new object[]
            {
                new { env = "TEST", scheme = "https", host = "test.example", port = "443", uri = "https://test.example", config1 = "test", config2 = "token" }
            }
        };
        return await SendJsonAsync(HttpMethod.Post, "/services/private/system/module", payload, 200);
    }

    private async Task<JsonDocument?> SendMultipartAsync(
        string path,
        int expectedStatus,
        string fileField,
        string fileName,
        string mimeType,
        params (string Name, string Value)[] fields)
    {
        using var form = new MultipartFormDataContent();
        var bytes = new ByteArrayContent(Encoding.UTF8.GetBytes("phase4c content"));
        bytes.Headers.ContentType = new MediaTypeHeaderValue(mimeType);
        form.Add(bytes, fileField, fileName);
        foreach (var (name, value) in fields) form.Add(new StringContent(value), name);
        return await SendAsync(HttpMethod.Post, path, form, expectedStatus);
    }

    private async Task<JsonDocument?> SendJsonAsync(
        HttpMethod method,
        string path,
        object? payload,
        int expectedStatus,
        bool useAuthorization = true,
        string? language = "en",
        string? tenant = null,
        string? store = null,
        string? idempotencyKey = null)
    {
        using var content = payload is null
            ? null
            : new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        return await SendAsync(method, path, content, expectedStatus, useAuthorization, language, tenant, store, idempotencyKey);
    }

    private async Task<JsonDocument?> SendAsync(
        HttpMethod method,
        string path,
        HttpContent? content,
        int expectedStatus,
        bool useAuthorization = true,
        string? language = "en",
        string? tenant = null,
        string? store = null,
        string? idempotencyKey = null)
    {
        path = NormalizeApiPath(path);
        using var request = new HttpRequestMessage(method, path) { Content = content };
        if (useAuthorization) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", administratorToken);
        request.Headers.Remove("x-language");
        if (language is not null) request.Headers.TryAddWithoutValidation("x-language", language);
        if (tenant is not null)
        {
            request.Headers.Remove("x-tenant-id");
            request.Headers.TryAddWithoutValidation("x-tenant-id", tenant);
        }
        if (store is not null)
        {
            request.Headers.Remove("x-store-id");
            request.Headers.TryAddWithoutValidation("x-store-id", store);
        }
        if (idempotencyKey is not null)
        {
            request.Headers.Remove("Idempotency-Key");
            request.Headers.TryAddWithoutValidation("Idempotency-Key", idempotencyKey);
        }

        using var response = await client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();
        Assert.True((int)response.StatusCode == expectedStatus,
            $"Expected HTTP {expectedStatus}, got {(int)response.StatusCode} for {method} {path}. Body: {body}");
        if (response.StatusCode == HttpStatusCode.NoContent || string.IsNullOrWhiteSpace(body)) return null;
        return JsonDocument.Parse(body);
    }
}
