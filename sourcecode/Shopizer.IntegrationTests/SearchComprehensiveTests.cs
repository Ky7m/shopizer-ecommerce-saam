using System.Net;

namespace Shopizer.IntegrationTests;

[Collection(ShopizerAspireCollection.Name)]
public sealed class SearchComprehensiveTests(AspireHostFixture fixture) : ComprehensiveTestBase(fixture.SearchClient)
{

    // Source assertion 1: Contract success: POST /private/system/search/index
    // @BR-ID: BR-CAT-020
    [Fact(DisplayName = "001: Contract success: POST /private/system/search/index")]
    [Trait("BR", "BR-CAT-020")]
    public Task Test001_POST_BASE_URL_private_system_search_index_Field_rebuildId_202() => AssertShellAsync(
        Method("POST"),
        "/private/system/search/index",
        null,
        202,
        requiredField: "rebuildId");

    // Source assertion 2: Contract error/conformance: POST /private/system/search/index
    // @BR-ID: BR-CAT-020
    [Fact(DisplayName = "002: Contract error/conformance: POST /private/system/search/index")]
    [Trait("BR", "BR-CAT-020")]
    public Task Test002_POST_BASE_URL_private_system_search_index_Status_400() => AssertShellAsync(
        Method("POST"),
        "/private/system/search/index",
        null,
        400,
        requiredField: null);

    // Source assertion 3: Business rule assertion: BR-CAT-020
    // @BR-ID: BR-CAT-020
    [Fact(DisplayName = "003: Business rule assertion: BR-CAT-020")]
    [Trait("BR", "BR-CAT-020")]
    public Task Test003_POST_BASE_URL_private_system_search_index_Field_rebuildId_202() => AssertShellAsync(
        Method("POST"),
        "/private/system/search/index",
        null,
        202,
        requiredField: "rebuildId");

    // Source assertion 4: Business rule assertion: BR-CAT-021
    // @BR-ID: BR-CAT-021
    [Fact(DisplayName = "004: Business rule assertion: BR-CAT-021")]
    [Trait("BR", "BR-CAT-021")]
    public Task Test004_POST_BASE_URL_private_system_search_index_Field_rebuildId_202() => AssertShellAsync(
        Method("POST"),
        "/private/system/search/index",
        null,
        202,
        requiredField: "rebuildId");

    // Source assertion 5: Business rule assertion: BR-CAT-022
    // @BR-ID: BR-CAT-022
    [Fact(DisplayName = "005: Business rule assertion: BR-CAT-022")]
    [Trait("BR", "BR-CAT-022")]
    public Task Test005_POST_BASE_URL_private_system_search_index_Field_rebuildId_202() => AssertShellAsync(
        Method("POST"),
        "/private/system/search/index",
        null,
        202,
        requiredField: "rebuildId");

    // Source assertion 6: Business rule assertion: BR-CAT-023
    // @BR-ID: BR-CAT-023
    [Fact(DisplayName = "006: Business rule assertion: BR-CAT-023")]
    [Trait("BR", "BR-CAT-023")]
    public Task Test006_POST_BASE_URL_private_system_search_index_Field_rebuildId_202() => AssertShellAsync(
        Method("POST"),
        "/private/system/search/index",
        null,
        202,
        requiredField: "rebuildId");

    // Source assertion 7: Business rule assertion: BR-CAT-024
    // @BR-ID: BR-CAT-024
    [Fact(DisplayName = "007: Business rule assertion: BR-CAT-024")]
    [Trait("BR", "BR-CAT-024")]
    public Task Test007_POST_BASE_URL_private_system_search_index_Field_rebuildId_202() => AssertShellAsync(
        Method("POST"),
        "/private/system/search/index",
        null,
        202,
        requiredField: "rebuildId");

    // Source assertion 8: Business rule assertion: BR-EXT-023
    // @BR-ID: BR-EXT-023
    [Fact(DisplayName = "008: Business rule assertion: BR-EXT-023")]
    [Trait("BR", "BR-EXT-023")]
    public Task Test008_POST_BASE_URL_private_system_search_index_Field_rebuildId_202() => AssertShellAsync(
        Method("POST"),
        "/private/system/search/index",
        null,
        202,
        requiredField: "rebuildId");

    // Source assertion 9: Business rule assertion: BR-EXT-024
    // @BR-ID: BR-EXT-024
    [Fact(DisplayName = "009: Business rule assertion: BR-EXT-024")]
    [Trait("BR", "BR-EXT-024")]
    public Task Test009_POST_BASE_URL_private_system_search_index_Field_rebuildId_202() => AssertShellAsync(
        Method("POST"),
        "/private/system/search/index",
        null,
        202,
        requiredField: "rebuildId");

    // Source assertion 10: Business rule assertion: BR-CAT-032
    // @BR-ID: BR-CAT-032
    [Fact(DisplayName = "010: Business rule assertion: BR-CAT-032")]
    [Trait("BR", "BR-CAT-032")]
    public Task Test010_POST_BASE_URL_private_system_search_index_Field_rebuildId_202() => AssertShellAsync(
        Method("POST"),
        "/private/system/search/index",
        null,
        202,
        requiredField: "rebuildId");

    // Source assertion 11: Business rule assertion: BR-CAT-033
    // @BR-ID: BR-CAT-033
    [Fact(DisplayName = "011: Business rule assertion: BR-CAT-033")]
    [Trait("BR", "BR-CAT-033")]
    public Task Test011_POST_BASE_URL_private_system_search_index_Field_rebuildId_202() => AssertShellAsync(
        Method("POST"),
        "/private/system/search/index",
        null,
        202,
        requiredField: "rebuildId");

    // Source assertion 12: Business rule assertion: BR-CAT-034
    // @BR-ID: BR-CAT-034
    [Fact(DisplayName = "012: Business rule assertion: BR-CAT-034")]
    [Trait("BR", "BR-CAT-034")]
    public Task Test012_POST_BASE_URL_private_system_search_index_Field_rebuildId_202() => AssertShellAsync(
        Method("POST"),
        "/private/system/search/index",
        null,
        202,
        requiredField: "rebuildId");

    // Source assertion 13: Contract success: POST /search
    // @BR-ID: BR-CAT-020
    [Fact(DisplayName = "013: Contract success: POST /search")]
    [Trait("BR", "BR-CAT-020")]
    public Task Test013_POST_BASE_URL_search_Field_items_200() => AssertShellAsync(
        Method("POST"),
        "/search",
        "{\"query\":\"phase4c-test\",\"count\":1,\"start\":1}",
        200,
        requiredField: "items");

    // Source assertion 14: Contract error/conformance: POST /search
    // @BR-ID: BR-CAT-020
    [Fact(DisplayName = "014: Contract error/conformance: POST /search")]
    [Trait("BR", "BR-CAT-020")]
    public Task Test014_POST_BASE_URL_search_Status_400() => AssertShellAsync(
        Method("POST"),
        "/search",
        "{}",
        400,
        requiredField: null);

    // Source assertion 15: Contract success: POST /search/autocomplete
    // @BR-ID: BR-CAT-020
    [Fact(DisplayName = "015: Contract success: POST /search/autocomplete")]
    [Trait("BR", "BR-CAT-020")]
    public Task Test015_POST_BASE_URL_search_autocomplete_Field_suggestions_200() => AssertShellAsync(
        Method("POST"),
        "/search/autocomplete",
        "{\"query\":\"phase4c-test\"}",
        200,
        requiredField: "suggestions");

    // Source assertion 16: Contract error/conformance: POST /search/autocomplete
    // @BR-ID: BR-CAT-020
    [Fact(DisplayName = "016: Contract error/conformance: POST /search/autocomplete")]
    [Trait("BR", "BR-CAT-020")]
    public Task Test016_POST_BASE_URL_search_autocomplete_Status_400() => AssertShellAsync(
        Method("POST"),
        "/search/autocomplete",
        "{}",
        400,
        requiredField: null);
}
