namespace Shopizer.IntegrationTests;

[Collection(ShopizerAspireCollection.Name)]
public sealed class CatalogProductComprehensiveTests(AspireHostFixture fixture) : ComprehensiveTestBase(fixture.CatalogProductClient)
{

    // Source assertion 1: Contract success: POST /categories
    // @BR-ID: BR-CAT-001
    [Fact(DisplayName = "001: Contract success: POST /categories")]
    [Trait("BR", "BR-CAT-001")]
    public Task Test001_POST_BASE_URL_categories_Field_id_201() => AssertShellAsync(
        Method("POST"),
        "/categories",
        "{\"code\":\"phase4c-test\",\"parentId\":\"00000000-0000-0000-0000-000000000001\",\"visible\":true,\"featured\":true,\"sortOrder\":1,\"descriptions\":[{\"languageCode\":\"phase4c-test\",\"name\":\"phase4c-test\",\"friendlyUrl\":\"https://example.com/phase4c\",\"description\":\"phase4c-test\",\"title\":\"phase4c-test\",\"metaDescription\":\"phase4c-test\"}]}",
        201,
        requiredField: "id");

    // Source assertion 2: Contract error/conformance: POST /categories
    // @BR-ID: BR-CAT-001
    [Fact(DisplayName = "002: Contract error/conformance: POST /categories")]
    [Trait("BR", "BR-CAT-001")]
    public Task Test002_POST_BASE_URL_categories_Status_401() => AssertShellAsync(
        Method("POST"),
        "/categories",
        "{}",
        401,
        requiredField: null);

    // Source assertion 3: Business rule assertion: BR-CAT-001
    // @BR-ID: BR-CAT-001
    [Fact(DisplayName = "003: Business rule assertion: BR-CAT-001")]
    [Trait("BR", "BR-CAT-001")]
    public Task Test003_POST_BASE_URL_categories_Field_id_201() => AssertShellAsync(
        Method("POST"),
        "/categories",
        "{\"code\":\"phase4c-test\",\"parentId\":\"00000000-0000-0000-0000-000000000001\",\"visible\":true,\"featured\":true,\"sortOrder\":1,\"descriptions\":[{\"languageCode\":\"phase4c-test\",\"name\":\"phase4c-test\",\"friendlyUrl\":\"https://example.com/phase4c\",\"description\":\"phase4c-test\",\"title\":\"phase4c-test\",\"metaDescription\":\"phase4c-test\"}]}",
        201,
        requiredField: "id");

    // Source assertion 4: Business rule assertion: BR-CAT-002
    // @BR-ID: BR-CAT-002
    [Fact(DisplayName = "004: Business rule assertion: BR-CAT-002")]
    [Trait("BR", "BR-CAT-002")]
    public Task Test004_POST_BASE_URL_categories_Field_id_201() => AssertShellAsync(
        Method("POST"),
        "/categories",
        "{\"code\":\"phase4c-test\",\"parentId\":\"00000000-0000-0000-0000-000000000001\",\"visible\":true,\"featured\":true,\"sortOrder\":1,\"descriptions\":[{\"languageCode\":\"phase4c-test\",\"name\":\"phase4c-test\",\"friendlyUrl\":\"https://example.com/phase4c\",\"description\":\"phase4c-test\",\"title\":\"phase4c-test\",\"metaDescription\":\"phase4c-test\"}]}",
        201,
        requiredField: "id");

    // Source assertion 5: Business rule assertion: BR-CAT-003
    // @BR-ID: BR-CAT-003
    [Fact(DisplayName = "005: Business rule assertion: BR-CAT-003")]
    [Trait("BR", "BR-CAT-003")]
    public Task Test005_POST_BASE_URL_categories_Field_id_201() => AssertShellAsync(
        Method("POST"),
        "/categories",
        "{\"code\":\"phase4c-test\",\"parentId\":\"00000000-0000-0000-0000-000000000001\",\"visible\":true,\"featured\":true,\"sortOrder\":1,\"descriptions\":[{\"languageCode\":\"phase4c-test\",\"name\":\"phase4c-test\",\"friendlyUrl\":\"https://example.com/phase4c\",\"description\":\"phase4c-test\",\"title\":\"phase4c-test\",\"metaDescription\":\"phase4c-test\"}]}",
        201,
        requiredField: "id");

    // Source assertion 6: Business rule assertion: BR-CAT-004
    // @BR-ID: BR-CAT-004
    [Fact(DisplayName = "006: Business rule assertion: BR-CAT-004")]
    [Trait("BR", "BR-CAT-004")]
    public Task Test006_POST_BASE_URL_categories_Field_id_201() => AssertShellAsync(
        Method("POST"),
        "/categories",
        "{\"code\":\"phase4c-test\",\"parentId\":\"00000000-0000-0000-0000-000000000001\",\"visible\":true,\"featured\":true,\"sortOrder\":1,\"descriptions\":[{\"languageCode\":\"phase4c-test\",\"name\":\"phase4c-test\",\"friendlyUrl\":\"https://example.com/phase4c\",\"description\":\"phase4c-test\",\"title\":\"phase4c-test\",\"metaDescription\":\"phase4c-test\"}]}",
        201,
        requiredField: "id");

    // Source assertion 7: Business rule assertion: BR-CAT-005
    // @BR-ID: BR-CAT-005
    [Fact(DisplayName = "007: Business rule assertion: BR-CAT-005")]
    [Trait("BR", "BR-CAT-005")]
    public Task Test007_POST_BASE_URL_categories_Field_id_201() => AssertShellAsync(
        Method("POST"),
        "/categories",
        "{\"code\":\"phase4c-test\",\"parentId\":\"00000000-0000-0000-0000-000000000001\",\"visible\":true,\"featured\":true,\"sortOrder\":1,\"descriptions\":[{\"languageCode\":\"phase4c-test\",\"name\":\"phase4c-test\",\"friendlyUrl\":\"https://example.com/phase4c\",\"description\":\"phase4c-test\",\"title\":\"phase4c-test\",\"metaDescription\":\"phase4c-test\"}]}",
        201,
        requiredField: "id");

    // Source assertion 8: Business rule assertion: BR-CAT-006
    // @BR-ID: BR-CAT-006
    [Fact(DisplayName = "008: Business rule assertion: BR-CAT-006")]
    [Trait("BR", "BR-CAT-006")]
    public Task Test008_POST_BASE_URL_categories_Field_id_201() => AssertShellAsync(
        Method("POST"),
        "/categories",
        "{\"code\":\"phase4c-test\",\"parentId\":\"00000000-0000-0000-0000-000000000001\",\"visible\":true,\"featured\":true,\"sortOrder\":1,\"descriptions\":[{\"languageCode\":\"phase4c-test\",\"name\":\"phase4c-test\",\"friendlyUrl\":\"https://example.com/phase4c\",\"description\":\"phase4c-test\",\"title\":\"phase4c-test\",\"metaDescription\":\"phase4c-test\"}]}",
        201,
        requiredField: "id");

    // Source assertion 9: Business rule assertion: BR-CAT-007
    // @BR-ID: BR-CAT-007
    [Fact(DisplayName = "009: Business rule assertion: BR-CAT-007")]
    [Trait("BR", "BR-CAT-007")]
    public Task Test009_POST_BASE_URL_categories_Field_id_201() => AssertShellAsync(
        Method("POST"),
        "/categories",
        "{\"code\":\"phase4c-test\",\"parentId\":\"00000000-0000-0000-0000-000000000001\",\"visible\":true,\"featured\":true,\"sortOrder\":1,\"descriptions\":[{\"languageCode\":\"phase4c-test\",\"name\":\"phase4c-test\",\"friendlyUrl\":\"https://example.com/phase4c\",\"description\":\"phase4c-test\",\"title\":\"phase4c-test\",\"metaDescription\":\"phase4c-test\"}]}",
        201,
        requiredField: "id");

    // Source assertion 10: Business rule assertion: BR-CAT-008
    // @BR-ID: BR-CAT-008
    [Fact(DisplayName = "010: Business rule assertion: BR-CAT-008")]
    [Trait("BR", "BR-CAT-008")]
    public Task Test010_POST_BASE_URL_categories_Field_id_201() => AssertShellAsync(
        Method("POST"),
        "/categories",
        "{\"code\":\"phase4c-test\",\"parentId\":\"00000000-0000-0000-0000-000000000001\",\"visible\":true,\"featured\":true,\"sortOrder\":1,\"descriptions\":[{\"languageCode\":\"phase4c-test\",\"name\":\"phase4c-test\",\"friendlyUrl\":\"https://example.com/phase4c\",\"description\":\"phase4c-test\",\"title\":\"phase4c-test\",\"metaDescription\":\"phase4c-test\"}]}",
        201,
        requiredField: "id");

    // Source assertion 11: Business rule assertion: BR-CAT-009
    // @BR-ID: BR-CAT-009
    [Fact(DisplayName = "011: Business rule assertion: BR-CAT-009")]
    [Trait("BR", "BR-CAT-009")]
    public Task Test011_POST_BASE_URL_categories_Field_id_201() => AssertShellAsync(
        Method("POST"),
        "/categories",
        "{\"code\":\"phase4c-test\",\"parentId\":\"00000000-0000-0000-0000-000000000001\",\"visible\":true,\"featured\":true,\"sortOrder\":1,\"descriptions\":[{\"languageCode\":\"phase4c-test\",\"name\":\"phase4c-test\",\"friendlyUrl\":\"https://example.com/phase4c\",\"description\":\"phase4c-test\",\"title\":\"phase4c-test\",\"metaDescription\":\"phase4c-test\"}]}",
        201,
        requiredField: "id");

    // Source assertion 12: Business rule assertion: BR-CAT-010
    // @BR-ID: BR-CAT-010
    [Fact(DisplayName = "012: Business rule assertion: BR-CAT-010")]
    [Trait("BR", "BR-CAT-010")]
    public Task Test012_POST_BASE_URL_categories_Field_id_201() => AssertShellAsync(
        Method("POST"),
        "/categories",
        "{\"code\":\"phase4c-test\",\"parentId\":\"00000000-0000-0000-0000-000000000001\",\"visible\":true,\"featured\":true,\"sortOrder\":1,\"descriptions\":[{\"languageCode\":\"phase4c-test\",\"name\":\"phase4c-test\",\"friendlyUrl\":\"https://example.com/phase4c\",\"description\":\"phase4c-test\",\"title\":\"phase4c-test\",\"metaDescription\":\"phase4c-test\"}]}",
        201,
        requiredField: "id");

    // Source assertion 13: Business rule assertion: BR-CAT-011
    // @BR-ID: BR-CAT-011
    [Fact(DisplayName = "013: Business rule assertion: BR-CAT-011")]
    [Trait("BR", "BR-CAT-011")]
    public Task Test013_POST_BASE_URL_categories_Field_id_201() => AssertShellAsync(
        Method("POST"),
        "/categories",
        "{\"code\":\"phase4c-test\",\"parentId\":\"00000000-0000-0000-0000-000000000001\",\"visible\":true,\"featured\":true,\"sortOrder\":1,\"descriptions\":[{\"languageCode\":\"phase4c-test\",\"name\":\"phase4c-test\",\"friendlyUrl\":\"https://example.com/phase4c\",\"description\":\"phase4c-test\",\"title\":\"phase4c-test\",\"metaDescription\":\"phase4c-test\"}]}",
        201,
        requiredField: "id");

    // Source assertion 14: Business rule assertion: BR-CAT-012
    // @BR-ID: BR-CAT-012
    [Fact(DisplayName = "014: Business rule assertion: BR-CAT-012")]
    [Trait("BR", "BR-CAT-012")]
    public Task Test014_POST_BASE_URL_categories_Field_id_201() => AssertShellAsync(
        Method("POST"),
        "/categories",
        "{\"code\":\"phase4c-test\",\"parentId\":\"00000000-0000-0000-0000-000000000001\",\"visible\":true,\"featured\":true,\"sortOrder\":1,\"descriptions\":[{\"languageCode\":\"phase4c-test\",\"name\":\"phase4c-test\",\"friendlyUrl\":\"https://example.com/phase4c\",\"description\":\"phase4c-test\",\"title\":\"phase4c-test\",\"metaDescription\":\"phase4c-test\"}]}",
        201,
        requiredField: "id");

    // Source assertion 15: Business rule assertion: BR-CAT-013
    // @BR-ID: BR-CAT-013
    [Fact(DisplayName = "015: Business rule assertion: BR-CAT-013")]
    [Trait("BR", "BR-CAT-013")]
    public Task Test015_POST_BASE_URL_categories_Field_id_201() => AssertShellAsync(
        Method("POST"),
        "/categories",
        "{\"code\":\"phase4c-test\",\"parentId\":\"00000000-0000-0000-0000-000000000001\",\"visible\":true,\"featured\":true,\"sortOrder\":1,\"descriptions\":[{\"languageCode\":\"phase4c-test\",\"name\":\"phase4c-test\",\"friendlyUrl\":\"https://example.com/phase4c\",\"description\":\"phase4c-test\",\"title\":\"phase4c-test\",\"metaDescription\":\"phase4c-test\"}]}",
        201,
        requiredField: "id");

    // Source assertion 16: Business rule assertion: BR-CAT-014
    // @BR-ID: BR-CAT-014
    [Fact(DisplayName = "016: Business rule assertion: BR-CAT-014")]
    [Trait("BR", "BR-CAT-014")]
    public Task Test016_POST_BASE_URL_categories_Field_id_201() => AssertShellAsync(
        Method("POST"),
        "/categories",
        "{\"code\":\"phase4c-test\",\"parentId\":\"00000000-0000-0000-0000-000000000001\",\"visible\":true,\"featured\":true,\"sortOrder\":1,\"descriptions\":[{\"languageCode\":\"phase4c-test\",\"name\":\"phase4c-test\",\"friendlyUrl\":\"https://example.com/phase4c\",\"description\":\"phase4c-test\",\"title\":\"phase4c-test\",\"metaDescription\":\"phase4c-test\"}]}",
        201,
        requiredField: "id");

    // Source assertion 17: Business rule assertion: BR-CAT-015
    // @BR-ID: BR-CAT-015
    [Fact(DisplayName = "017: Business rule assertion: BR-CAT-015")]
    [Trait("BR", "BR-CAT-015")]
    public Task Test017_POST_BASE_URL_categories_Field_id_201() => AssertShellAsync(
        Method("POST"),
        "/categories",
        "{\"code\":\"phase4c-test\",\"parentId\":\"00000000-0000-0000-0000-000000000001\",\"visible\":true,\"featured\":true,\"sortOrder\":1,\"descriptions\":[{\"languageCode\":\"phase4c-test\",\"name\":\"phase4c-test\",\"friendlyUrl\":\"https://example.com/phase4c\",\"description\":\"phase4c-test\",\"title\":\"phase4c-test\",\"metaDescription\":\"phase4c-test\"}]}",
        201,
        requiredField: "id");

    // Source assertion 18: Business rule assertion: BR-CAT-016
    // @BR-ID: BR-CAT-016
    [Fact(DisplayName = "018: Business rule assertion: BR-CAT-016")]
    [Trait("BR", "BR-CAT-016")]
    public Task Test018_POST_BASE_URL_categories_Field_id_201() => AssertShellAsync(
        Method("POST"),
        "/categories",
        "{\"code\":\"phase4c-test\",\"parentId\":\"00000000-0000-0000-0000-000000000001\",\"visible\":true,\"featured\":true,\"sortOrder\":1,\"descriptions\":[{\"languageCode\":\"phase4c-test\",\"name\":\"phase4c-test\",\"friendlyUrl\":\"https://example.com/phase4c\",\"description\":\"phase4c-test\",\"title\":\"phase4c-test\",\"metaDescription\":\"phase4c-test\"}]}",
        201,
        requiredField: "id");

    // Source assertion 19: Business rule assertion: BR-CAT-017
    // @BR-ID: BR-CAT-017
    [Fact(DisplayName = "019: Business rule assertion: BR-CAT-017")]
    [Trait("BR", "BR-CAT-017")]
    public Task Test019_POST_BASE_URL_categories_Field_id_201() => AssertShellAsync(
        Method("POST"),
        "/categories",
        "{\"code\":\"phase4c-test\",\"parentId\":\"00000000-0000-0000-0000-000000000001\",\"visible\":true,\"featured\":true,\"sortOrder\":1,\"descriptions\":[{\"languageCode\":\"phase4c-test\",\"name\":\"phase4c-test\",\"friendlyUrl\":\"https://example.com/phase4c\",\"description\":\"phase4c-test\",\"title\":\"phase4c-test\",\"metaDescription\":\"phase4c-test\"}]}",
        201,
        requiredField: "id");

    // Source assertion 20: Business rule assertion: BR-CAT-018
    // @BR-ID: BR-CAT-018
    [Fact(DisplayName = "020: Business rule assertion: BR-CAT-018")]
    [Trait("BR", "BR-CAT-018")]
    public Task Test020_POST_BASE_URL_categories_Field_id_201() => AssertShellAsync(
        Method("POST"),
        "/categories",
        "{\"code\":\"phase4c-test\",\"parentId\":\"00000000-0000-0000-0000-000000000001\",\"visible\":true,\"featured\":true,\"sortOrder\":1,\"descriptions\":[{\"languageCode\":\"phase4c-test\",\"name\":\"phase4c-test\",\"friendlyUrl\":\"https://example.com/phase4c\",\"description\":\"phase4c-test\",\"title\":\"phase4c-test\",\"metaDescription\":\"phase4c-test\"}]}",
        201,
        requiredField: "id");

    // Source assertion 21: Business rule assertion: BR-CAT-019
    // @BR-ID: BR-CAT-019
    [Fact(DisplayName = "021: Business rule assertion: BR-CAT-019")]
    [Trait("BR", "BR-CAT-019")]
    public Task Test021_POST_BASE_URL_categories_Field_id_201() => AssertShellAsync(
        Method("POST"),
        "/categories",
        "{\"code\":\"phase4c-test\",\"parentId\":\"00000000-0000-0000-0000-000000000001\",\"visible\":true,\"featured\":true,\"sortOrder\":1,\"descriptions\":[{\"languageCode\":\"phase4c-test\",\"name\":\"phase4c-test\",\"friendlyUrl\":\"https://example.com/phase4c\",\"description\":\"phase4c-test\",\"title\":\"phase4c-test\",\"metaDescription\":\"phase4c-test\"}]}",
        201,
        requiredField: "id");

    // Source assertion 22: Business rule assertion: BR-CAT-025
    // @BR-ID: BR-CAT-025
    [Fact(DisplayName = "022: Business rule assertion: BR-CAT-025")]
    [Trait("BR", "BR-CAT-025")]
    public Task Test022_POST_BASE_URL_categories_Field_id_201() => AssertShellAsync(
        Method("POST"),
        "/categories",
        "{\"code\":\"phase4c-test\",\"parentId\":\"00000000-0000-0000-0000-000000000001\",\"visible\":true,\"featured\":true,\"sortOrder\":1,\"descriptions\":[{\"languageCode\":\"phase4c-test\",\"name\":\"phase4c-test\",\"friendlyUrl\":\"https://example.com/phase4c\",\"description\":\"phase4c-test\",\"title\":\"phase4c-test\",\"metaDescription\":\"phase4c-test\"}]}",
        201,
        requiredField: "id");

    // Source assertion 23: Business rule assertion: BR-CAT-026
    // @BR-ID: BR-CAT-026
    [Fact(DisplayName = "023: Business rule assertion: BR-CAT-026")]
    [Trait("BR", "BR-CAT-026")]
    public Task Test023_POST_BASE_URL_categories_Field_id_201() => AssertShellAsync(
        Method("POST"),
        "/categories",
        "{\"code\":\"phase4c-test\",\"parentId\":\"00000000-0000-0000-0000-000000000001\",\"visible\":true,\"featured\":true,\"sortOrder\":1,\"descriptions\":[{\"languageCode\":\"phase4c-test\",\"name\":\"phase4c-test\",\"friendlyUrl\":\"https://example.com/phase4c\",\"description\":\"phase4c-test\",\"title\":\"phase4c-test\",\"metaDescription\":\"phase4c-test\"}]}",
        201,
        requiredField: "id");

    // Source assertion 24: Business rule assertion: BR-CAT-027
    // @BR-ID: BR-CAT-027
    [Fact(DisplayName = "024: Business rule assertion: BR-CAT-027")]
    [Trait("BR", "BR-CAT-027")]
    public Task Test024_POST_BASE_URL_categories_Field_id_201() => AssertShellAsync(
        Method("POST"),
        "/categories",
        "{\"code\":\"phase4c-test\",\"parentId\":\"00000000-0000-0000-0000-000000000001\",\"visible\":true,\"featured\":true,\"sortOrder\":1,\"descriptions\":[{\"languageCode\":\"phase4c-test\",\"name\":\"phase4c-test\",\"friendlyUrl\":\"https://example.com/phase4c\",\"description\":\"phase4c-test\",\"title\":\"phase4c-test\",\"metaDescription\":\"phase4c-test\"}]}",
        201,
        requiredField: "id");

    // Source assertion 25: Business rule assertion: BR-CAT-028
    // @BR-ID: BR-CAT-028
    [Fact(DisplayName = "025: Business rule assertion: BR-CAT-028")]
    [Trait("BR", "BR-CAT-028")]
    public Task Test025_POST_BASE_URL_categories_Field_id_201() => AssertShellAsync(
        Method("POST"),
        "/categories",
        "{\"code\":\"phase4c-test\",\"parentId\":\"00000000-0000-0000-0000-000000000001\",\"visible\":true,\"featured\":true,\"sortOrder\":1,\"descriptions\":[{\"languageCode\":\"phase4c-test\",\"name\":\"phase4c-test\",\"friendlyUrl\":\"https://example.com/phase4c\",\"description\":\"phase4c-test\",\"title\":\"phase4c-test\",\"metaDescription\":\"phase4c-test\"}]}",
        201,
        requiredField: "id");

    // Source assertion 26: Business rule assertion: BR-CAT-029
    // @BR-ID: BR-CAT-029
    [Fact(DisplayName = "026: Business rule assertion: BR-CAT-029")]
    [Trait("BR", "BR-CAT-029")]
    public Task Test026_POST_BASE_URL_categories_Field_id_201() => AssertShellAsync(
        Method("POST"),
        "/categories",
        "{\"code\":\"phase4c-test\",\"parentId\":\"00000000-0000-0000-0000-000000000001\",\"visible\":true,\"featured\":true,\"sortOrder\":1,\"descriptions\":[{\"languageCode\":\"phase4c-test\",\"name\":\"phase4c-test\",\"friendlyUrl\":\"https://example.com/phase4c\",\"description\":\"phase4c-test\",\"title\":\"phase4c-test\",\"metaDescription\":\"phase4c-test\"}]}",
        201,
        requiredField: "id");

    // Source assertion 27: Business rule assertion: BR-CAT-030
    // @BR-ID: BR-CAT-030
    [Fact(DisplayName = "027: Business rule assertion: BR-CAT-030")]
    [Trait("BR", "BR-CAT-030")]
    public Task Test027_POST_BASE_URL_categories_Field_id_201() => AssertShellAsync(
        Method("POST"),
        "/categories",
        "{\"code\":\"phase4c-test\",\"parentId\":\"00000000-0000-0000-0000-000000000001\",\"visible\":true,\"featured\":true,\"sortOrder\":1,\"descriptions\":[{\"languageCode\":\"phase4c-test\",\"name\":\"phase4c-test\",\"friendlyUrl\":\"https://example.com/phase4c\",\"description\":\"phase4c-test\",\"title\":\"phase4c-test\",\"metaDescription\":\"phase4c-test\"}]}",
        201,
        requiredField: "id");

    // Source assertion 28: Business rule assertion: BR-CAT-031
    // @BR-ID: BR-CAT-031
    [Fact(DisplayName = "028: Business rule assertion: BR-CAT-031")]
    [Trait("BR", "BR-CAT-031")]
    public Task Test028_POST_BASE_URL_categories_Field_id_201() => AssertShellAsync(
        Method("POST"),
        "/categories",
        "{\"code\":\"phase4c-test\",\"parentId\":\"00000000-0000-0000-0000-000000000001\",\"visible\":true,\"featured\":true,\"sortOrder\":1,\"descriptions\":[{\"languageCode\":\"phase4c-test\",\"name\":\"phase4c-test\",\"friendlyUrl\":\"https://example.com/phase4c\",\"description\":\"phase4c-test\",\"title\":\"phase4c-test\",\"metaDescription\":\"phase4c-test\"}]}",
        201,
        requiredField: "id");

    // Source assertion 29: Business rule assertion: BR-ORD-012
    // @BR-ID: BR-ORD-012
    [Fact(DisplayName = "029: Business rule assertion: BR-ORD-012")]
    [Trait("BR", "BR-ORD-012")]
    public Task Test029_POST_BASE_URL_categories_Field_id_201() => AssertShellAsync(
        Method("POST"),
        "/categories",
        "{\"code\":\"phase4c-test\",\"parentId\":\"00000000-0000-0000-0000-000000000001\",\"visible\":true,\"featured\":true,\"sortOrder\":1,\"descriptions\":[{\"languageCode\":\"phase4c-test\",\"name\":\"phase4c-test\",\"friendlyUrl\":\"https://example.com/phase4c\",\"description\":\"phase4c-test\",\"title\":\"phase4c-test\",\"metaDescription\":\"phase4c-test\"}]}",
        201,
        requiredField: "id");

    // Source assertion 30: Business rule assertion: BR-EXT-019
    // @BR-ID: BR-EXT-019
    [Fact(DisplayName = "030: Business rule assertion: BR-EXT-019")]
    [Trait("BR", "BR-EXT-019")]
    public Task Test030_POST_BASE_URL_categories_Field_id_201() => AssertShellAsync(
        Method("POST"),
        "/categories",
        "{\"code\":\"phase4c-test\",\"parentId\":\"00000000-0000-0000-0000-000000000001\",\"visible\":true,\"featured\":true,\"sortOrder\":1,\"descriptions\":[{\"languageCode\":\"phase4c-test\",\"name\":\"phase4c-test\",\"friendlyUrl\":\"https://example.com/phase4c\",\"description\":\"phase4c-test\",\"title\":\"phase4c-test\",\"metaDescription\":\"phase4c-test\"}]}",
        201,
        requiredField: "id");

    // Source assertion 31: Business rule assertion: BR-EXT-020
    // @BR-ID: BR-EXT-020
    [Fact(DisplayName = "031: Business rule assertion: BR-EXT-020")]
    [Trait("BR", "BR-EXT-020")]
    public Task Test031_POST_BASE_URL_categories_Field_id_201() => AssertShellAsync(
        Method("POST"),
        "/categories",
        "{\"code\":\"phase4c-test\",\"parentId\":\"00000000-0000-0000-0000-000000000001\",\"visible\":true,\"featured\":true,\"sortOrder\":1,\"descriptions\":[{\"languageCode\":\"phase4c-test\",\"name\":\"phase4c-test\",\"friendlyUrl\":\"https://example.com/phase4c\",\"description\":\"phase4c-test\",\"title\":\"phase4c-test\",\"metaDescription\":\"phase4c-test\"}]}",
        201,
        requiredField: "id");

    // Source assertion 32: Business rule assertion: BR-UI-003
    // @BR-ID: BR-UI-003
    [Fact(DisplayName = "032: Business rule assertion: BR-UI-003")]
    [Trait("BR", "BR-UI-003")]
    public Task Test032_POST_BASE_URL_categories_Field_id_201() => AssertShellAsync(
        Method("POST"),
        "/categories",
        "{\"code\":\"phase4c-test\",\"parentId\":\"00000000-0000-0000-0000-000000000001\",\"visible\":true,\"featured\":true,\"sortOrder\":1,\"descriptions\":[{\"languageCode\":\"phase4c-test\",\"name\":\"phase4c-test\",\"friendlyUrl\":\"https://example.com/phase4c\",\"description\":\"phase4c-test\",\"title\":\"phase4c-test\",\"metaDescription\":\"phase4c-test\"}]}",
        201,
        requiredField: "id");

    // Source assertion 33: Business rule assertion: BR-UI-004
    // @BR-ID: BR-UI-004
    [Fact(DisplayName = "033: Business rule assertion: BR-UI-004")]
    [Trait("BR", "BR-UI-004")]
    public Task Test033_POST_BASE_URL_categories_Field_id_201() => AssertShellAsync(
        Method("POST"),
        "/categories",
        "{\"code\":\"phase4c-test\",\"parentId\":\"00000000-0000-0000-0000-000000000001\",\"visible\":true,\"featured\":true,\"sortOrder\":1,\"descriptions\":[{\"languageCode\":\"phase4c-test\",\"name\":\"phase4c-test\",\"friendlyUrl\":\"https://example.com/phase4c\",\"description\":\"phase4c-test\",\"title\":\"phase4c-test\",\"metaDescription\":\"phase4c-test\"}]}",
        201,
        requiredField: "id");

    // Source assertion 34: Business rule assertion: BR-UI-005
    // @BR-ID: BR-UI-005
    [Fact(DisplayName = "034: Business rule assertion: BR-UI-005")]
    [Trait("BR", "BR-UI-005")]
    public Task Test034_POST_BASE_URL_categories_Field_id_201() => AssertShellAsync(
        Method("POST"),
        "/categories",
        "{\"code\":\"phase4c-test\",\"parentId\":\"00000000-0000-0000-0000-000000000001\",\"visible\":true,\"featured\":true,\"sortOrder\":1,\"descriptions\":[{\"languageCode\":\"phase4c-test\",\"name\":\"phase4c-test\",\"friendlyUrl\":\"https://example.com/phase4c\",\"description\":\"phase4c-test\",\"title\":\"phase4c-test\",\"metaDescription\":\"phase4c-test\"}]}",
        201,
        requiredField: "id");

    // Source assertion 35: Business rule assertion: BR-UI-006
    // @BR-ID: BR-UI-006
    [Fact(DisplayName = "035: Business rule assertion: BR-UI-006")]
    [Trait("BR", "BR-UI-006")]
    public Task Test035_POST_BASE_URL_categories_Field_id_201() => AssertShellAsync(
        Method("POST"),
        "/categories",
        "{\"code\":\"phase4c-test\",\"parentId\":\"00000000-0000-0000-0000-000000000001\",\"visible\":true,\"featured\":true,\"sortOrder\":1,\"descriptions\":[{\"languageCode\":\"phase4c-test\",\"name\":\"phase4c-test\",\"friendlyUrl\":\"https://example.com/phase4c\",\"description\":\"phase4c-test\",\"title\":\"phase4c-test\",\"metaDescription\":\"phase4c-test\"}]}",
        201,
        requiredField: "id");

    // Source assertion 36: Business rule assertion: BR-CAT-032
    // @BR-ID: BR-CAT-032
    [Fact(DisplayName = "036: Business rule assertion: BR-CAT-032")]
    [Trait("BR", "BR-CAT-032")]
    public Task Test036_POST_BASE_URL_categories_Field_id_201() => AssertShellAsync(
        Method("POST"),
        "/categories",
        "{\"code\":\"phase4c-test\",\"parentId\":\"00000000-0000-0000-0000-000000000001\",\"visible\":true,\"featured\":true,\"sortOrder\":1,\"descriptions\":[{\"languageCode\":\"phase4c-test\",\"name\":\"phase4c-test\",\"friendlyUrl\":\"https://example.com/phase4c\",\"description\":\"phase4c-test\",\"title\":\"phase4c-test\",\"metaDescription\":\"phase4c-test\"}]}",
        201,
        requiredField: "id");

    // Source assertion 37: Business rule assertion: BR-CAT-033
    // @BR-ID: BR-CAT-033
    [Fact(DisplayName = "037: Business rule assertion: BR-CAT-033")]
    [Trait("BR", "BR-CAT-033")]
    public Task Test037_POST_BASE_URL_categories_Field_id_201() => AssertShellAsync(
        Method("POST"),
        "/categories",
        "{\"code\":\"phase4c-test\",\"parentId\":\"00000000-0000-0000-0000-000000000001\",\"visible\":true,\"featured\":true,\"sortOrder\":1,\"descriptions\":[{\"languageCode\":\"phase4c-test\",\"name\":\"phase4c-test\",\"friendlyUrl\":\"https://example.com/phase4c\",\"description\":\"phase4c-test\",\"title\":\"phase4c-test\",\"metaDescription\":\"phase4c-test\"}]}",
        201,
        requiredField: "id");

    // Source assertion 38: Business rule assertion: BR-CAT-034
    // @BR-ID: BR-CAT-034
    [Fact(DisplayName = "038: Business rule assertion: BR-CAT-034")]
    [Trait("BR", "BR-CAT-034")]
    public Task Test038_POST_BASE_URL_categories_Field_id_201() => AssertShellAsync(
        Method("POST"),
        "/categories",
        "{\"code\":\"phase4c-test\",\"parentId\":\"00000000-0000-0000-0000-000000000001\",\"visible\":true,\"featured\":true,\"sortOrder\":1,\"descriptions\":[{\"languageCode\":\"phase4c-test\",\"name\":\"phase4c-test\",\"friendlyUrl\":\"https://example.com/phase4c\",\"description\":\"phase4c-test\",\"title\":\"phase4c-test\",\"metaDescription\":\"phase4c-test\"}]}",
        201,
        requiredField: "id");

    // Source assertion 39: Business rule assertion: BR-CAT-035
    // @BR-ID: BR-CAT-035
    [Fact(DisplayName = "039: Business rule assertion: BR-CAT-035")]
    [Trait("BR", "BR-CAT-035")]
    public Task Test039_POST_BASE_URL_categories_Field_id_201() => AssertShellAsync(
        Method("POST"),
        "/categories",
        "{\"code\":\"phase4c-test\",\"parentId\":\"00000000-0000-0000-0000-000000000001\",\"visible\":true,\"featured\":true,\"sortOrder\":1,\"descriptions\":[{\"languageCode\":\"phase4c-test\",\"name\":\"phase4c-test\",\"friendlyUrl\":\"https://example.com/phase4c\",\"description\":\"phase4c-test\",\"title\":\"phase4c-test\",\"metaDescription\":\"phase4c-test\"}]}",
        201,
        requiredField: "id");

    // Source assertion 40: Business rule assertion: BR-CAT-036
    // @BR-ID: BR-CAT-036
    [Fact(DisplayName = "040: Business rule assertion: BR-CAT-036")]
    [Trait("BR", "BR-CAT-036")]
    public Task Test040_POST_BASE_URL_categories_Field_id_201() => AssertShellAsync(
        Method("POST"),
        "/categories",
        "{\"code\":\"phase4c-test\",\"parentId\":\"00000000-0000-0000-0000-000000000001\",\"visible\":true,\"featured\":true,\"sortOrder\":1,\"descriptions\":[{\"languageCode\":\"phase4c-test\",\"name\":\"phase4c-test\",\"friendlyUrl\":\"https://example.com/phase4c\",\"description\":\"phase4c-test\",\"title\":\"phase4c-test\",\"metaDescription\":\"phase4c-test\"}]}",
        201,
        requiredField: "id");

    // Source assertion 41: Business rule assertion: BR-CAT-037
    // @BR-ID: BR-CAT-037
    [Fact(DisplayName = "041: Business rule assertion: BR-CAT-037")]
    [Trait("BR", "BR-CAT-037")]
    public Task Test041_POST_BASE_URL_categories_Field_id_201() => AssertShellAsync(
        Method("POST"),
        "/categories",
        "{\"code\":\"phase4c-test\",\"parentId\":\"00000000-0000-0000-0000-000000000001\",\"visible\":true,\"featured\":true,\"sortOrder\":1,\"descriptions\":[{\"languageCode\":\"phase4c-test\",\"name\":\"phase4c-test\",\"friendlyUrl\":\"https://example.com/phase4c\",\"description\":\"phase4c-test\",\"title\":\"phase4c-test\",\"metaDescription\":\"phase4c-test\"}]}",
        201,
        requiredField: "id");

    // Source assertion 42: Business rule assertion: BR-CAT-038
    // @BR-ID: BR-CAT-038
    [Fact(DisplayName = "042: Business rule assertion: BR-CAT-038")]
    [Trait("BR", "BR-CAT-038")]
    public Task Test042_POST_BASE_URL_categories_Field_id_201() => AssertShellAsync(
        Method("POST"),
        "/categories",
        "{\"code\":\"phase4c-test\",\"parentId\":\"00000000-0000-0000-0000-000000000001\",\"visible\":true,\"featured\":true,\"sortOrder\":1,\"descriptions\":[{\"languageCode\":\"phase4c-test\",\"name\":\"phase4c-test\",\"friendlyUrl\":\"https://example.com/phase4c\",\"description\":\"phase4c-test\",\"title\":\"phase4c-test\",\"metaDescription\":\"phase4c-test\"}]}",
        201,
        requiredField: "id");

    // Source assertion 43: Business rule assertion: BR-CAT-039
    // @BR-ID: BR-CAT-039
    [Fact(DisplayName = "043: Business rule assertion: BR-CAT-039")]
    [Trait("BR", "BR-CAT-039")]
    public Task Test043_POST_BASE_URL_categories_Field_id_201() => AssertShellAsync(
        Method("POST"),
        "/categories",
        "{\"code\":\"phase4c-test\",\"parentId\":\"00000000-0000-0000-0000-000000000001\",\"visible\":true,\"featured\":true,\"sortOrder\":1,\"descriptions\":[{\"languageCode\":\"phase4c-test\",\"name\":\"phase4c-test\",\"friendlyUrl\":\"https://example.com/phase4c\",\"description\":\"phase4c-test\",\"title\":\"phase4c-test\",\"metaDescription\":\"phase4c-test\"}]}",
        201,
        requiredField: "id");

    // Source assertion 44: Contract success: POST /products
    // @BR-ID: BR-CAT-001
    [Fact(DisplayName = "044: Contract success: POST /products")]
    [Trait("BR", "BR-CAT-001")]
    public Task Test044_POST_BASE_URL_products_Field_id_201() => AssertShellAsync(
        Method("POST"),
        "/products",
        "{\"sku\":\"phase4c-test\",\"refSku\":\"phase4c-test\",\"visible\":true,\"canBePurchased\":true,\"dateAvailable\":\"2026-09-02T00:00:00Z\",\"manufacturerCode\":\"phase4c-test\",\"productTypeCode\":\"phase4c-test\",\"taxClassCode\":\"phase4c-test\",\"productVirtual\":true,\"productShippable\":true,\"productFree\":true,\"sortOrder\":1,\"descriptions\":[{\"languageCode\":\"phase4c-test\",\"name\":\"phase4c-test\",\"friendlyUrl\":\"https://example.com/phase4c\",\"description\":\"phase4c-test\",\"highlights\":\"phase4c-test\",\"title\":\"phase4c-test\",\"keywords\":\"phase4c-test\",\"metaDescription\":\"phase4c-test\"}],\"availabilities\":[{\"regionCode\":\"phase4c-test\",\"quantity\":1,\"active\":true}],\"categories\":[{\"id\":\"00000000-0000-0000-0000-000000000001\",\"code\":\"phase4c-test\"}],\"variants\":[{\"sku\":\"phase4c-test\",\"code\":\"phase4c-test\",\"defaultSelection\":true,\"available\":true,\"dateAvailable\":\"2026-09-02T00:00:00Z\",\"availability\":{\"regionCode\":\"phase4c-test\",\"quantity\":1,\"active\":true}}]}",
        201,
        requiredField: "id");

    // Source assertion 45: Contract error/conformance: POST /products
    // @BR-ID: BR-CAT-001
    [Fact(DisplayName = "045: Contract error/conformance: POST /products")]
    [Trait("BR", "BR-CAT-001")]
    public Task Test045_POST_BASE_URL_products_Status_401() => AssertShellAsync(
        Method("POST"),
        "/products",
        "{}",
        401,
        requiredField: null);

    // Source assertion 46: Contract success: POST /products/{productId}/categories/{categoryId}
    // @BR-ID: BR-CAT-001
    [Fact(DisplayName = "046: Contract success: POST /products/{productId}/categories/{categoryId}")]
    [Trait("BR", "BR-CAT-001")]
    public Task Test046_POST_BASE_URL_products_ById_categories_ById_Field_id_200() => AssertShellAsync(
        Method("POST"),
        $"/products/{ResourceId}/categories/{ResourceId}",
        null,
        200,
        requiredField: "id");

    // Source assertion 47: Contract error/conformance: POST /products/{productId}/categories/{categoryId}
    // @BR-ID: BR-CAT-001
    [Fact(DisplayName = "047: Contract error/conformance: POST /products/{productId}/categories/{categoryId}")]
    [Trait("BR", "BR-CAT-001")]
    public Task Test047_POST_BASE_URL_products_ById_categories_ById_Status_401() => AssertShellAsync(
        Method("POST"),
        $"/products/{ResourceId}/categories/{ResourceId}",
        null,
        401,
        requiredField: null);

    // Source assertion 48: Contract success: POST /products/{productId}/media
    // @BR-ID: BR-CAT-001
    [Fact(DisplayName = "048: Contract success: POST /products/{productId}/media")]
    [Trait("BR", "BR-CAT-001")]
    public Task Test048_POST_BASE_URL_products_ById_media_Field_id_201() => AssertShellAsync(
        Method("POST"),
        $"/products/{ResourceId}/media",
        "{\"file\":\"phase4c-test\",\"fileName\":\"phase4c-test\",\"defaultImage\":true}",
        201,
        requiredField: "id");

    // Source assertion 49: Contract error/conformance: POST /products/{productId}/media
    // @BR-ID: BR-CAT-001
    [Fact(DisplayName = "049: Contract error/conformance: POST /products/{productId}/media")]
    [Trait("BR", "BR-CAT-001")]
    public Task Test049_POST_BASE_URL_products_ById_media_Status_401() => AssertShellAsync(
        Method("POST"),
        $"/products/{ResourceId}/media",
        "{}",
        401,
        requiredField: null);

    // Source assertion 50: Contract success: POST /products/{productId}/options/price
    // @BR-ID: BR-CAT-001
    [Fact(DisplayName = "050: Contract success: POST /products/{productId}/options/price")]
    [Trait("BR", "BR-CAT-001")]
    public Task Test050_POST_BASE_URL_products_ById_options_price_Field_finalAmount_200() => AssertShellAsync(
        Method("POST"),
        $"/products/{ResourceId}/options/price",
        "{\"selections\":[{\"optionId\":\"00000000-0000-0000-0000-000000000001\",\"valueId\":\"00000000-0000-0000-0000-000000000001\"}],\"currencyCode\":\"phase4c-test\",\"countryCode\":\"phase4c-test\"}",
        200,
        requiredField: "finalAmount");

    // Source assertion 51: Contract error/conformance: POST /products/{productId}/options/price
    // @BR-ID: BR-CAT-001
    [Fact(DisplayName = "051: Contract error/conformance: POST /products/{productId}/options/price")]
    [Trait("BR", "BR-CAT-001")]
    public Task Test051_POST_BASE_URL_products_ById_options_price_Status_404() => AssertShellAsync(
        Method("POST"),
        $"/products/{ResourceId}/options/price",
        "{}",
        404,
        requiredField: null);

    // Source assertion 52: Contract success: POST /products/{productId}/reservations
    // @BR-ID: BR-CAT-001
    [Fact(DisplayName = "052: Contract success: POST /products/{productId}/reservations")]
    [Trait("BR", "BR-CAT-001")]
    public Task Test052_POST_BASE_URL_products_ById_reservations_Field_id_201() => AssertShellAsync(
        Method("POST"),
        $"/products/{ResourceId}/reservations",
        "{\"reservationKey\":\"phase4c-test\",\"variantId\":\"00000000-0000-0000-0000-000000000001\",\"availabilityId\":\"00000000-0000-0000-0000-000000000001\",\"regionCode\":\"phase4c-test\",\"quantity\":1,\"expiresAt\":\"2026-09-02T00:00:00Z\"}",
        201,
        requiredField: "id");

    // Source assertion 53: Contract error/conformance: POST /products/{productId}/reservations
    // @BR-ID: BR-CAT-001
    [Fact(DisplayName = "053: Contract error/conformance: POST /products/{productId}/reservations")]
    [Trait("BR", "BR-CAT-001")]
    public Task Test053_POST_BASE_URL_products_ById_reservations_Status_401() => AssertShellAsync(
        Method("POST"),
        $"/products/{ResourceId}/reservations",
        "{}",
        401,
        requiredField: null);

    // Source assertion 54: Contract success: POST /products/{productId}/variants
    // @BR-ID: BR-CAT-001
    [Fact(DisplayName = "054: Contract success: POST /products/{productId}/variants")]
    [Trait("BR", "BR-CAT-001")]
    public Task Test054_POST_BASE_URL_products_ById_variants_Field_id_201() => AssertShellAsync(
        Method("POST"),
        $"/products/{ResourceId}/variants",
        "{\"sku\":\"phase4c-test\",\"code\":\"phase4c-test\",\"defaultSelection\":true,\"available\":true,\"dateAvailable\":\"2026-09-02T00:00:00Z\",\"availability\":{\"regionCode\":\"phase4c-test\",\"quantity\":1,\"active\":true}}",
        201,
        requiredField: "id");

    // Source assertion 55: Contract error/conformance: POST /products/{productId}/variants
    // @BR-ID: BR-CAT-001
    [Fact(DisplayName = "055: Contract error/conformance: POST /products/{productId}/variants")]
    [Trait("BR", "BR-CAT-001")]
    public Task Test055_POST_BASE_URL_products_ById_variants_Status_401() => AssertShellAsync(
        Method("POST"),
        $"/products/{ResourceId}/variants",
        "{}",
        401,
        requiredField: null);

    // Source assertion 56: Contract success: POST /reservations/{reservationId}/commit
    // @BR-ID: BR-CAT-001
    [Fact(DisplayName = "056: Contract success: POST /reservations/{reservationId}/commit")]
    [Trait("BR", "BR-CAT-001")]
    public Task Test056_POST_BASE_URL_reservations_ById_commit_Field_id_200() => AssertShellAsync(
        Method("POST"),
        $"/reservations/{ResourceId}/commit",
        null,
        200,
        requiredField: "id");

    // Source assertion 57: Contract error/conformance: POST /reservations/{reservationId}/commit
    // @BR-ID: BR-CAT-001
    [Fact(DisplayName = "057: Contract error/conformance: POST /reservations/{reservationId}/commit")]
    [Trait("BR", "BR-CAT-001")]
    public Task Test057_POST_BASE_URL_reservations_ById_commit_Status_401() => AssertShellAsync(
        Method("POST"),
        $"/reservations/{ResourceId}/commit",
        null,
        401,
        requiredField: null);

    // Source assertion 58: Contract success: POST /reservations/{reservationId}/release
    // @BR-ID: BR-CAT-001
    [Fact(DisplayName = "058: Contract success: POST /reservations/{reservationId}/release")]
    [Trait("BR", "BR-CAT-001")]
    public Task Test058_POST_BASE_URL_reservations_ById_release_Field_id_200() => AssertShellAsync(
        Method("POST"),
        $"/reservations/{ResourceId}/release",
        null,
        200,
        requiredField: "id");

    // Source assertion 59: Contract error/conformance: POST /reservations/{reservationId}/release
    // @BR-ID: BR-CAT-001
    [Fact(DisplayName = "059: Contract error/conformance: POST /reservations/{reservationId}/release")]
    [Trait("BR", "BR-CAT-001")]
    public Task Test059_POST_BASE_URL_reservations_ById_release_Status_401() => AssertShellAsync(
        Method("POST"),
        $"/reservations/{ResourceId}/release",
        null,
        401,
        requiredField: null);

    // Source assertion 60: Contract success: GET /categories
    // @BR-ID: BR-CAT-001
    [Fact(DisplayName = "060: Contract success: GET /categories")]
    [Trait("BR", "BR-CAT-001")]
    public Task Test060_GET_BASE_URL_categories_Field_items_200() => AssertShellAsync(
        Method("GET"),
        "/categories",
        null,
        200,
        requiredField: "items");

    // Source assertion 61: Contract error/conformance: GET /categories
    // @BR-ID: BR-CAT-001
    [Fact(DisplayName = "061: Contract error/conformance: GET /categories")]
    [Trait("BR", "BR-CAT-001")]
    public Task Test061_GET_BASE_URL_categories_Status_422() => AssertShellAsync(
        Method("GET"),
        "/categories",
        null,
        422,
        requiredField: null);

    // Source assertion 62: Contract success: GET /categories/slug/{friendlyUrl}
    // @BR-ID: BR-CAT-001
    [Fact(DisplayName = "062: Contract success: GET /categories/slug/{friendlyUrl}")]
    [Trait("BR", "BR-CAT-001")]
    public Task Test062_GET_BASE_URL_categories_slug_phase4c_value_Field_id_200() => AssertShellAsync(
        Method("GET"),
        "/categories/slug/phase4c-value",
        null,
        200,
        requiredField: "id");

    // Source assertion 63: Contract error/conformance: GET /categories/slug/{friendlyUrl}
    // @BR-ID: BR-CAT-001
    [Fact(DisplayName = "063: Contract error/conformance: GET /categories/slug/{friendlyUrl}")]
    [Trait("BR", "BR-CAT-001")]
    public Task Test063_GET_BASE_URL_categories_slug_phase4c_value_Status_404() => AssertShellAsync(
        Method("GET"),
        "/categories/slug/phase4c-value",
        null,
        404,
        requiredField: null);

    // Source assertion 64: Contract success: GET /categories/uniqueness
    // @BR-ID: BR-CAT-001
    [Fact(DisplayName = "064: Contract success: GET /categories/uniqueness")]
    [Trait("BR", "BR-CAT-001")]
    public Task Test064_GET_BASE_URL_categories_uniqueness_Field_exists_200() => AssertShellAsync(
        Method("GET"),
        "/categories/uniqueness",
        null,
        200,
        requiredField: "exists");

    // Source assertion 65: Contract error/conformance: GET /categories/uniqueness
    // @BR-ID: BR-CAT-001
    [Fact(DisplayName = "065: Contract error/conformance: GET /categories/uniqueness")]
    [Trait("BR", "BR-CAT-001")]
    public Task Test065_GET_BASE_URL_categories_uniqueness_Status_401() => AssertShellAsync(
        Method("GET"),
        "/categories/uniqueness",
        null,
        401,
        requiredField: null);

    // Source assertion 66: Contract success: GET /categories/{categoryId}
    // @BR-ID: BR-CAT-001
    [Fact(DisplayName = "066: Contract success: GET /categories/{categoryId}")]
    [Trait("BR", "BR-CAT-001")]
    public Task Test066_GET_BASE_URL_categories_ById_Field_id_200() => AssertShellAsync(
        Method("GET"),
        $"/categories/{ResourceId}",
        null,
        200,
        requiredField: "id");

    // Source assertion 67: Contract error/conformance: GET /categories/{categoryId}
    // @BR-ID: BR-CAT-001
    [Fact(DisplayName = "067: Contract error/conformance: GET /categories/{categoryId}")]
    [Trait("BR", "BR-CAT-001")]
    public Task Test067_GET_BASE_URL_categories_ById_Status_404() => AssertShellAsync(
        Method("GET"),
        $"/categories/{ResourceId}",
        null,
        404,
        requiredField: null);

    // Source assertion 68: Contract success: GET /categories/{categoryId}/products
    // @BR-ID: BR-CAT-001
    [Fact(DisplayName = "068: Contract success: GET /categories/{categoryId}/products")]
    [Trait("BR", "BR-CAT-001")]
    public Task Test068_GET_BASE_URL_categories_ById_products_Field_items_200() => AssertShellAsync(
        Method("GET"),
        $"/categories/{ResourceId}/products",
        null,
        200,
        requiredField: "items");

    // Source assertion 69: Contract error/conformance: GET /categories/{categoryId}/products
    // @BR-ID: BR-CAT-001
    [Fact(DisplayName = "069: Contract error/conformance: GET /categories/{categoryId}/products")]
    [Trait("BR", "BR-CAT-001")]
    public Task Test069_GET_BASE_URL_categories_ById_products_Status_404() => AssertShellAsync(
        Method("GET"),
        $"/categories/{ResourceId}/products",
        null,
        404,
        requiredField: null);

    // Source assertion 70: Contract success: GET /products
    // @BR-ID: BR-CAT-001
    [Fact(DisplayName = "070: Contract success: GET /products")]
    [Trait("BR", "BR-CAT-001")]
    public Task Test070_GET_BASE_URL_products_Field_items_200() => AssertShellAsync(
        Method("GET"),
        "/products",
        null,
        200,
        requiredField: "items");

    // Source assertion 71: Contract error/conformance: GET /products
    // @BR-ID: BR-CAT-001
    [Fact(DisplayName = "071: Contract error/conformance: GET /products")]
    [Trait("BR", "BR-CAT-001")]
    public Task Test071_GET_BASE_URL_products_Status_401() => AssertShellAsync(
        Method("GET"),
        "/products",
        null,
        401,
        requiredField: null);

    // Source assertion 72: Contract success: GET /products/sku/{sku}
    // @BR-ID: BR-CAT-001
    [Fact(DisplayName = "072: Contract success: GET /products/sku/{sku}")]
    [Trait("BR", "BR-CAT-001")]
    public Task Test072_GET_BASE_URL_products_sku_phase4c_sku_Field_id_200() => AssertShellAsync(
        Method("GET"),
        "/products/sku/phase4c-sku",
        null,
        200,
        requiredField: "id");

    // Source assertion 73: Contract error/conformance: GET /products/sku/{sku}
    // @BR-ID: BR-CAT-001
    [Fact(DisplayName = "073: Contract error/conformance: GET /products/sku/{sku}")]
    [Trait("BR", "BR-CAT-001")]
    public Task Test073_GET_BASE_URL_products_sku_phase4c_sku_Status_404() => AssertShellAsync(
        Method("GET"),
        "/products/sku/phase4c-sku",
        null,
        404,
        requiredField: null);

    // Source assertion 74: Contract success: GET /products/slug/{friendlyUrl}
    // @BR-ID: BR-CAT-001
    [Fact(DisplayName = "074: Contract success: GET /products/slug/{friendlyUrl}")]
    [Trait("BR", "BR-CAT-001")]
    public Task Test074_GET_BASE_URL_products_slug_phase4c_value_Field_id_200() => AssertShellAsync(
        Method("GET"),
        "/products/slug/phase4c-value",
        null,
        200,
        requiredField: "id");

    // Source assertion 75: Contract error/conformance: GET /products/slug/{friendlyUrl}
    // @BR-ID: BR-CAT-001
    [Fact(DisplayName = "075: Contract error/conformance: GET /products/slug/{friendlyUrl}")]
    [Trait("BR", "BR-CAT-001")]
    public Task Test075_GET_BASE_URL_products_slug_phase4c_value_Status_404() => AssertShellAsync(
        Method("GET"),
        "/products/slug/phase4c-value",
        null,
        404,
        requiredField: null);

    // Source assertion 76: Contract success: GET /products/uniqueness
    // @BR-ID: BR-CAT-001
    [Fact(DisplayName = "076: Contract success: GET /products/uniqueness")]
    [Trait("BR", "BR-CAT-001")]
    public Task Test076_GET_BASE_URL_products_uniqueness_Field_exists_200() => AssertShellAsync(
        Method("GET"),
        "/products/uniqueness",
        null,
        200,
        requiredField: "exists");

    // Source assertion 77: Contract error/conformance: GET /products/uniqueness
    // @BR-ID: BR-CAT-001
    [Fact(DisplayName = "077: Contract error/conformance: GET /products/uniqueness")]
    [Trait("BR", "BR-CAT-001")]
    public Task Test077_GET_BASE_URL_products_uniqueness_Status_401() => AssertShellAsync(
        Method("GET"),
        "/products/uniqueness",
        null,
        401,
        requiredField: null);

    // Source assertion 78: Contract success: GET /products/{productId}
    // @BR-ID: BR-CAT-001
    [Fact(DisplayName = "078: Contract success: GET /products/{productId}")]
    [Trait("BR", "BR-CAT-001")]
    public Task Test078_GET_BASE_URL_products_ById_Field_id_200() => AssertShellAsync(
        Method("GET"),
        $"/products/{ResourceId}",
        null,
        200,
        requiredField: "id");

    // Source assertion 79: Contract error/conformance: GET /products/{productId}
    // @BR-ID: BR-CAT-001
    [Fact(DisplayName = "079: Contract error/conformance: GET /products/{productId}")]
    [Trait("BR", "BR-CAT-001")]
    public Task Test079_GET_BASE_URL_products_ById_Status_404() => AssertShellAsync(
        Method("GET"),
        $"/products/{ResourceId}",
        null,
        404,
        requiredField: null);

    // Source assertion 80: Contract success: GET /products/{productId}/availability
    // @BR-ID: BR-CAT-001
    [Fact(DisplayName = "080: Contract success: GET /products/{productId}/availability")]
    [Trait("BR", "BR-CAT-001")]
    public Task Test080_GET_BASE_URL_products_ById_availability_Field_items_200() => AssertShellAsync(
        Method("GET"),
        $"/products/{ResourceId}/availability",
        null,
        200,
        requiredField: "items");

    // Source assertion 81: Contract error/conformance: GET /products/{productId}/availability
    // @BR-ID: BR-CAT-001
    [Fact(DisplayName = "081: Contract error/conformance: GET /products/{productId}/availability")]
    [Trait("BR", "BR-CAT-001")]
    public Task Test081_GET_BASE_URL_products_ById_availability_Status_404() => AssertShellAsync(
        Method("GET"),
        $"/products/{ResourceId}/availability",
        null,
        404,
        requiredField: null);

    // Source assertion 82: Contract success: GET /products/{productId}/variants
    // @BR-ID: BR-CAT-001
    [Fact(DisplayName = "082: Contract success: GET /products/{productId}/variants")]
    [Trait("BR", "BR-CAT-001")]
    public Task Test082_GET_BASE_URL_products_ById_variants_Field_items_200() => AssertShellAsync(
        Method("GET"),
        $"/products/{ResourceId}/variants",
        null,
        200,
        requiredField: "items");

    // Source assertion 83: Contract error/conformance: GET /products/{productId}/variants
    // @BR-ID: BR-CAT-001
    [Fact(DisplayName = "083: Contract error/conformance: GET /products/{productId}/variants")]
    [Trait("BR", "BR-CAT-001")]
    public Task Test083_GET_BASE_URL_products_ById_variants_Status_404() => AssertShellAsync(
        Method("GET"),
        $"/products/{ResourceId}/variants",
        null,
        404,
        requiredField: null);

    // Source assertion 84: Contract success: GET /products/{productId}/variants/uniqueness/{sku}
    // @BR-ID: BR-CAT-001
    [Fact(DisplayName = "084: Contract success: GET /products/{productId}/variants/uniqueness/{sku}")]
    [Trait("BR", "BR-CAT-001")]
    public Task Test084_GET_BASE_URL_products_ById_variants_uniqueness_phase4c_sku_Field_exists_200() => AssertShellAsync(
        Method("GET"),
        $"/products/{ResourceId}/variants/uniqueness/phase4c-sku",
        null,
        200,
        requiredField: "exists");

    // Source assertion 85: Contract error/conformance: GET /products/{productId}/variants/uniqueness/{sku}
    // @BR-ID: BR-CAT-001
    [Fact(DisplayName = "085: Contract error/conformance: GET /products/{productId}/variants/uniqueness/{sku}")]
    [Trait("BR", "BR-CAT-001")]
    public Task Test085_GET_BASE_URL_products_ById_variants_uniqueness_phase4c_sku_Status_401() => AssertShellAsync(
        Method("GET"),
        $"/products/{ResourceId}/variants/uniqueness/phase4c-sku",
        null,
        401,
        requiredField: null);

    // Source assertion 86: Contract success: GET /products/{productId}/variants/{variantId}
    // @BR-ID: BR-CAT-001
    [Fact(DisplayName = "086: Contract success: GET /products/{productId}/variants/{variantId}")]
    [Trait("BR", "BR-CAT-001")]
    public Task Test086_GET_BASE_URL_products_ById_variants_ById_Field_id_200() => AssertShellAsync(
        Method("GET"),
        $"/products/{ResourceId}/variants/{ResourceId}",
        null,
        200,
        requiredField: "id");

    // Source assertion 87: Contract error/conformance: GET /products/{productId}/variants/{variantId}
    // @BR-ID: BR-CAT-001
    [Fact(DisplayName = "087: Contract error/conformance: GET /products/{productId}/variants/{variantId}")]
    [Trait("BR", "BR-CAT-001")]
    public Task Test087_GET_BASE_URL_products_ById_variants_ById_Status_404() => AssertShellAsync(
        Method("GET"),
        $"/products/{ResourceId}/variants/{ResourceId}",
        null,
        404,
        requiredField: null);

    // Source assertion 88: Contract success: PUT /categories/{categoryId}
    // @BR-ID: BR-CAT-001
    [Fact(DisplayName = "088: Contract success: PUT /categories/{categoryId}")]
    [Trait("BR", "BR-CAT-001")]
    public Task Test088_PUT_BASE_URL_categories_ById_Field_id_200() => AssertShellAsync(
        Method("PUT"),
        $"/categories/{ResourceId}",
        "{\"code\":\"phase4c-test\",\"parentId\":\"00000000-0000-0000-0000-000000000001\",\"visible\":true,\"featured\":true,\"sortOrder\":1,\"descriptions\":[{\"languageCode\":\"phase4c-test\",\"name\":\"phase4c-test\",\"friendlyUrl\":\"https://example.com/phase4c\",\"description\":\"phase4c-test\",\"title\":\"phase4c-test\",\"metaDescription\":\"phase4c-test\"}]}",
        200,
        requiredField: "id");

    // Source assertion 89: Contract error/conformance: PUT /categories/{categoryId}
    // @BR-ID: BR-CAT-001
    [Fact(DisplayName = "089: Contract error/conformance: PUT /categories/{categoryId}")]
    [Trait("BR", "BR-CAT-001")]
    public Task Test089_PUT_BASE_URL_categories_ById_Status_401() => AssertShellAsync(
        Method("PUT"),
        $"/categories/{ResourceId}",
        "{}",
        401,
        requiredField: null);

    // Source assertion 90: Contract success: PUT /categories/{categoryId}/move/{parentId}
    // @BR-ID: BR-CAT-001
    [Fact(DisplayName = "090: Contract success: PUT /categories/{categoryId}/move/{parentId}")]
    [Trait("BR", "BR-CAT-001")]
    public Task Test090_PUT_BASE_URL_categories_ById_move_ById_Field_id_200() => AssertShellAsync(
        Method("PUT"),
        $"/categories/{ResourceId}/move/{ResourceId}",
        null,
        200,
        requiredField: "id");

    // Source assertion 91: Contract error/conformance: PUT /categories/{categoryId}/move/{parentId}
    // @BR-ID: BR-CAT-001
    [Fact(DisplayName = "091: Contract error/conformance: PUT /categories/{categoryId}/move/{parentId}")]
    [Trait("BR", "BR-CAT-001")]
    public Task Test091_PUT_BASE_URL_categories_ById_move_ById_Status_401() => AssertShellAsync(
        Method("PUT"),
        $"/categories/{ResourceId}/move/{ResourceId}",
        null,
        401,
        requiredField: null);

    // Source assertion 92: Contract success: PATCH /categories/{categoryId}/visibility
    // @BR-ID: BR-CAT-001
    [Fact(DisplayName = "092: Contract success: PATCH /categories/{categoryId}/visibility")]
    [Trait("BR", "BR-CAT-001")]
    public Task Test092_PATCH_BASE_URL_categories_ById_visibility_Field_id_200() => AssertShellAsync(
        Method("PATCH"),
        $"/categories/{ResourceId}/visibility",
        "{\"visible\":true}",
        200,
        requiredField: "id");

    // Source assertion 93: Contract error/conformance: PATCH /categories/{categoryId}/visibility
    // @BR-ID: BR-CAT-001
    [Fact(DisplayName = "093: Contract error/conformance: PATCH /categories/{categoryId}/visibility")]
    [Trait("BR", "BR-CAT-001")]
    public Task Test093_PATCH_BASE_URL_categories_ById_visibility_Status_401() => AssertShellAsync(
        Method("PATCH"),
        $"/categories/{ResourceId}/visibility",
        "{}",
        401,
        requiredField: null);

    // Source assertion 94: Contract success: PUT /products/{productId}
    // @BR-ID: BR-CAT-001
    [Fact(DisplayName = "094: Contract success: PUT /products/{productId}")]
    [Trait("BR", "BR-CAT-001")]
    public Task Test094_PUT_BASE_URL_products_ById_Field_id_200() => AssertShellAsync(
        Method("PUT"),
        $"/products/{ResourceId}",
        "{\"sku\":\"phase4c-test\",\"refSku\":\"phase4c-test\",\"visible\":true,\"canBePurchased\":true,\"dateAvailable\":\"2026-09-02T00:00:00Z\",\"manufacturerCode\":\"phase4c-test\",\"productTypeCode\":\"phase4c-test\",\"taxClassCode\":\"phase4c-test\",\"productVirtual\":true,\"productShippable\":true,\"productFree\":true,\"sortOrder\":1,\"descriptions\":[{\"languageCode\":\"phase4c-test\",\"name\":\"phase4c-test\",\"friendlyUrl\":\"https://example.com/phase4c\",\"description\":\"phase4c-test\",\"highlights\":\"phase4c-test\",\"title\":\"phase4c-test\",\"keywords\":\"phase4c-test\",\"metaDescription\":\"phase4c-test\"}],\"availabilities\":[{\"regionCode\":\"phase4c-test\",\"quantity\":1,\"active\":true}]}",
        200,
        requiredField: "id");

    // Source assertion 95: Contract error/conformance: PUT /products/{productId}
    // @BR-ID: BR-CAT-001
    [Fact(DisplayName = "095: Contract error/conformance: PUT /products/{productId}")]
    [Trait("BR", "BR-CAT-001")]
    public Task Test095_PUT_BASE_URL_products_ById_Status_401() => AssertShellAsync(
        Method("PUT"),
        $"/products/{ResourceId}",
        "{}",
        401,
        requiredField: null);

    // Source assertion 96: Contract success: PUT /products/{productId}/availability
    // @BR-ID: BR-CAT-001
    [Fact(DisplayName = "096: Contract success: PUT /products/{productId}/availability")]
    [Trait("BR", "BR-CAT-001")]
    public Task Test096_PUT_BASE_URL_products_ById_availability_Field_items_200() => AssertShellAsync(
        Method("PUT"),
        $"/products/{ResourceId}/availability",
        "{\"items\":[{\"regionCode\":\"phase4c-test\",\"quantity\":1,\"active\":true}]}",
        200,
        requiredField: "items");

    // Source assertion 97: Contract error/conformance: PUT /products/{productId}/availability
    // @BR-ID: BR-CAT-001
    [Fact(DisplayName = "097: Contract error/conformance: PUT /products/{productId}/availability")]
    [Trait("BR", "BR-CAT-001")]
    public Task Test097_PUT_BASE_URL_products_ById_availability_Status_401() => AssertShellAsync(
        Method("PUT"),
        $"/products/{ResourceId}/availability",
        "{}",
        401,
        requiredField: null);

    // Source assertion 98: Contract success: PUT /products/{productId}/variants/{variantId}
    // @BR-ID: BR-CAT-001
    [Fact(DisplayName = "098: Contract success: PUT /products/{productId}/variants/{variantId}")]
    [Trait("BR", "BR-CAT-001")]
    public Task Test098_PUT_BASE_URL_products_ById_variants_ById_Field_id_200() => AssertShellAsync(
        Method("PUT"),
        $"/products/{ResourceId}/variants/{ResourceId}",
        "{\"sku\":\"phase4c-test\",\"code\":\"phase4c-test\",\"defaultSelection\":true,\"available\":true,\"dateAvailable\":\"2026-09-02T00:00:00Z\"}",
        200,
        requiredField: "id");

    // Source assertion 99: Contract error/conformance: PUT /products/{productId}/variants/{variantId}
    // @BR-ID: BR-CAT-001
    [Fact(DisplayName = "099: Contract error/conformance: PUT /products/{productId}/variants/{variantId}")]
    [Trait("BR", "BR-CAT-001")]
    public Task Test099_PUT_BASE_URL_products_ById_variants_ById_Status_401() => AssertShellAsync(
        Method("PUT"),
        $"/products/{ResourceId}/variants/{ResourceId}",
        "{}",
        401,
        requiredField: null);

    // Source assertion 100: Contract success: PATCH /products/{productId}/visibility
    // @BR-ID: BR-CAT-001
    [Fact(DisplayName = "100: Contract success: PATCH /products/{productId}/visibility")]
    [Trait("BR", "BR-CAT-001")]
    public Task Test100_PATCH_BASE_URL_products_ById_visibility_Field_id_200() => AssertShellAsync(
        Method("PATCH"),
        $"/products/{ResourceId}/visibility",
        "{\"visible\":true,\"canBePurchased\":true,\"dateAvailable\":\"2026-09-02T00:00:00Z\"}",
        200,
        requiredField: "id");

    // Source assertion 101: Contract error/conformance: PATCH /products/{productId}/visibility
    // @BR-ID: BR-CAT-001
    [Fact(DisplayName = "101: Contract error/conformance: PATCH /products/{productId}/visibility")]
    [Trait("BR", "BR-CAT-001")]
    public Task Test101_PATCH_BASE_URL_products_ById_visibility_Status_401() => AssertShellAsync(
        Method("PATCH"),
        $"/products/{ResourceId}/visibility",
        "{}",
        401,
        requiredField: null);

    // Source assertion 102: Contract success: DELETE /categories/{categoryId}
    // @BR-ID: BR-CAT-001
    [Fact(DisplayName = "102: Contract success: DELETE /categories/{categoryId}")]
    [Trait("BR", "BR-CAT-001")]
    public Task Test102_DELETE_BASE_URL_categories_ById_Field_categoryId_200() => AssertShellAsync(
        Method("DELETE"),
        $"/categories/{ResourceId}",
        null,
        200,
        requiredField: "categoryId");

    // Source assertion 103: Contract error/conformance: DELETE /categories/{categoryId}
    // @BR-ID: BR-CAT-001
    [Fact(DisplayName = "103: Contract error/conformance: DELETE /categories/{categoryId}")]
    [Trait("BR", "BR-CAT-001")]
    public Task Test103_DELETE_BASE_URL_categories_ById_Status_401() => AssertShellAsync(
        Method("DELETE"),
        $"/categories/{ResourceId}",
        null,
        401,
        requiredField: null);

    // Source assertion 104: Contract success: DELETE /products/{productId}
    // @BR-ID: BR-CAT-001
    [Fact(DisplayName = "104: Contract success: DELETE /products/{productId}")]
    [Trait("BR", "BR-CAT-001")]
    public Task Test104_DELETE_BASE_URL_products_ById_Field_id_200() => AssertShellAsync(
        Method("DELETE"),
        $"/products/{ResourceId}",
        null,
        200,
        requiredField: "id");

    // Source assertion 105: Contract error/conformance: DELETE /products/{productId}
    // @BR-ID: BR-CAT-001
    [Fact(DisplayName = "105: Contract error/conformance: DELETE /products/{productId}")]
    [Trait("BR", "BR-CAT-001")]
    public Task Test105_DELETE_BASE_URL_products_ById_Status_401() => AssertShellAsync(
        Method("DELETE"),
        $"/products/{ResourceId}",
        null,
        401,
        requiredField: null);

    // Source assertion 106: Contract success: DELETE /products/{productId}/categories/{categoryId}
    // @BR-ID: BR-CAT-001
    [Fact(DisplayName = "106: Contract success: DELETE /products/{productId}/categories/{categoryId}")]
    [Trait("BR", "BR-CAT-001")]
    public Task Test106_DELETE_BASE_URL_products_ById_categories_ById_Field_id_200() => AssertShellAsync(
        Method("DELETE"),
        $"/products/{ResourceId}/categories/{ResourceId}",
        null,
        200,
        requiredField: "id");

    // Source assertion 107: Contract error/conformance: DELETE /products/{productId}/categories/{categoryId}
    // @BR-ID: BR-CAT-001
    [Fact(DisplayName = "107: Contract error/conformance: DELETE /products/{productId}/categories/{categoryId}")]
    [Trait("BR", "BR-CAT-001")]
    public Task Test107_DELETE_BASE_URL_products_ById_categories_ById_Status_401() => AssertShellAsync(
        Method("DELETE"),
        $"/products/{ResourceId}/categories/{ResourceId}",
        null,
        401,
        requiredField: null);

    // Source assertion 108: Contract success: DELETE /products/{productId}/media/{mediaId}
    // @BR-ID: BR-CAT-001
    [Fact(DisplayName = "108: Contract success: DELETE /products/{productId}/media/{mediaId}")]
    [Trait("BR", "BR-CAT-001")]
    public Task Test108_DELETE_BASE_URL_products_ById_media_ById_Field_id_200() => AssertShellAsync(
        Method("DELETE"),
        $"/products/{ResourceId}/media/{ResourceId}",
        null,
        200,
        requiredField: "id");

    // Source assertion 109: Contract error/conformance: DELETE /products/{productId}/media/{mediaId}
    // @BR-ID: BR-CAT-001
    [Fact(DisplayName = "109: Contract error/conformance: DELETE /products/{productId}/media/{mediaId}")]
    [Trait("BR", "BR-CAT-001")]
    public Task Test109_DELETE_BASE_URL_products_ById_media_ById_Status_401() => AssertShellAsync(
        Method("DELETE"),
        $"/products/{ResourceId}/media/{ResourceId}",
        null,
        401,
        requiredField: null);

    // Source assertion 110: Contract success: DELETE /products/{productId}/variants/{variantId}
    // @BR-ID: BR-CAT-001
    [Fact(DisplayName = "110: Contract success: DELETE /products/{productId}/variants/{variantId}")]
    [Trait("BR", "BR-CAT-001")]
    public Task Test110_DELETE_BASE_URL_products_ById_variants_ById_Field_id_200() => AssertShellAsync(
        Method("DELETE"),
        $"/products/{ResourceId}/variants/{ResourceId}",
        null,
        200,
        requiredField: "id");

    // Source assertion 111: Contract error/conformance: DELETE /products/{productId}/variants/{variantId}
    // @BR-ID: BR-CAT-001
    [Fact(DisplayName = "111: Contract error/conformance: DELETE /products/{productId}/variants/{variantId}")]
    [Trait("BR", "BR-CAT-001")]
    public Task Test111_DELETE_BASE_URL_products_ById_variants_ById_Status_401() => AssertShellAsync(
        Method("DELETE"),
        $"/products/{ResourceId}/variants/{ResourceId}",
        null,
        401,
        requiredField: null);
}
