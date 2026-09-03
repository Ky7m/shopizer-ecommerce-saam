using Shopizer.IntegrationTests.Fixtures;

namespace Shopizer.IntegrationTests;

[Collection(ShopizerAspireCollection.Name)]
public sealed class ContentConfigurationComprehensiveTests(AspireHostFixture fixture) : ComprehensiveTestBase(fixture.ContentConfigurationClient)
{

    // Source assertion 1: Contract success: POST /private/configurations/payment
    // @BR-ID: BR-MER-013
    [Fact(DisplayName = "001: Contract success: POST /private/configurations/payment")]
    [Trait("BR", "BR-MER-013")]
    public Task Test001_POST_BASE_URL_private_configurations_payment_Status_200() => AssertShellAsync(
        Method("POST"),
        "/private/configurations/payment",
        "{\"legacyOperation\":\"phase4c-test\"}",
        200,
        requiredField: null);

    // Source assertion 2: Contract error/conformance: POST /private/configurations/payment
    // @BR-ID: BR-MER-013
    [Fact(DisplayName = "002: Contract error/conformance: POST /private/configurations/payment")]
    [Trait("BR", "BR-MER-013")]
    public Task Test002_POST_BASE_URL_private_configurations_payment_Status_410() => AssertShellAsync(
        Method("POST"),
        "/private/configurations/payment",
        "{}",
        410,
        requiredField: null);

    // Source assertion 3: Business rule assertion: BR-MER-013
    // @BR-ID: BR-MER-013
    [Fact(DisplayName = "003: Business rule assertion: BR-MER-013")]
    [Trait("BR", "BR-MER-013")]
    public Task Test003_POST_BASE_URL_private_configurations_payment_Status_200() => AssertShellAsync(
        Method("POST"),
        "/private/configurations/payment",
        "{\"legacyOperation\":\"phase4c-test\"}",
        200,
        requiredField: null);

    // Source assertion 4: Business rule assertion: BR-MER-014
    // @BR-ID: BR-MER-014
    [Fact(DisplayName = "004: Business rule assertion: BR-MER-014")]
    [Trait("BR", "BR-MER-014")]
    public Task Test004_POST_BASE_URL_private_configurations_payment_Status_200() => AssertShellAsync(
        Method("POST"),
        "/private/configurations/payment",
        "{\"legacyOperation\":\"phase4c-test\"}",
        200,
        requiredField: null);

    // Source assertion 5: Business rule assertion: BR-MER-015
    // @BR-ID: BR-MER-015
    [Fact(DisplayName = "005: Business rule assertion: BR-MER-015")]
    [Trait("BR", "BR-MER-015")]
    public Task Test005_POST_BASE_URL_private_configurations_payment_Status_200() => AssertShellAsync(
        Method("POST"),
        "/private/configurations/payment",
        "{\"legacyOperation\":\"phase4c-test\"}",
        200,
        requiredField: null);

    // Source assertion 6: Business rule assertion: BR-MER-016
    // @BR-ID: BR-MER-016
    [Fact(DisplayName = "006: Business rule assertion: BR-MER-016")]
    [Trait("BR", "BR-MER-016")]
    public Task Test006_POST_BASE_URL_private_configurations_payment_Status_200() => AssertShellAsync(
        Method("POST"),
        "/private/configurations/payment",
        "{\"legacyOperation\":\"phase4c-test\"}",
        200,
        requiredField: null);

    // Source assertion 7: Business rule assertion: BR-MER-017
    // @BR-ID: BR-MER-017
    [Fact(DisplayName = "007: Business rule assertion: BR-MER-017")]
    [Trait("BR", "BR-MER-017")]
    public Task Test007_POST_BASE_URL_private_configurations_payment_Status_200() => AssertShellAsync(
        Method("POST"),
        "/private/configurations/payment",
        "{\"legacyOperation\":\"phase4c-test\"}",
        200,
        requiredField: null);

    // Source assertion 8: Business rule assertion: BR-MER-018
    // @BR-ID: BR-MER-018
    [Fact(DisplayName = "008: Business rule assertion: BR-MER-018")]
    [Trait("BR", "BR-MER-018")]
    public Task Test008_POST_BASE_URL_private_configurations_payment_Status_200() => AssertShellAsync(
        Method("POST"),
        "/private/configurations/payment",
        "{\"legacyOperation\":\"phase4c-test\"}",
        200,
        requiredField: null);

    // Source assertion 9: Business rule assertion: BR-MER-019
    // @BR-ID: BR-MER-019
    [Fact(DisplayName = "009: Business rule assertion: BR-MER-019")]
    [Trait("BR", "BR-MER-019")]
    public Task Test009_POST_BASE_URL_private_configurations_payment_Status_200() => AssertShellAsync(
        Method("POST"),
        "/private/configurations/payment",
        "{\"legacyOperation\":\"phase4c-test\"}",
        200,
        requiredField: null);

    // Source assertion 10: Business rule assertion: BR-MER-020
    // @BR-ID: BR-MER-020
    [Fact(DisplayName = "010: Business rule assertion: BR-MER-020")]
    [Trait("BR", "BR-MER-020")]
    public Task Test010_POST_BASE_URL_private_configurations_payment_Status_200() => AssertShellAsync(
        Method("POST"),
        "/private/configurations/payment",
        "{\"legacyOperation\":\"phase4c-test\"}",
        200,
        requiredField: null);

    // Source assertion 11: Business rule assertion: BR-MER-021
    // @BR-ID: BR-MER-021
    [Fact(DisplayName = "011: Business rule assertion: BR-MER-021")]
    [Trait("BR", "BR-MER-021")]
    public Task Test011_POST_BASE_URL_private_configurations_payment_Status_200() => AssertShellAsync(
        Method("POST"),
        "/private/configurations/payment",
        "{\"legacyOperation\":\"phase4c-test\"}",
        200,
        requiredField: null);

    // Source assertion 12: Business rule assertion: BR-MER-022
    // @BR-ID: BR-MER-022
    [Fact(DisplayName = "012: Business rule assertion: BR-MER-022")]
    [Trait("BR", "BR-MER-022")]
    public Task Test012_POST_BASE_URL_private_configurations_payment_Status_200() => AssertShellAsync(
        Method("POST"),
        "/private/configurations/payment",
        "{\"legacyOperation\":\"phase4c-test\"}",
        200,
        requiredField: null);

    // Source assertion 13: Business rule assertion: BR-MER-023
    // @BR-ID: BR-MER-023
    [Fact(DisplayName = "013: Business rule assertion: BR-MER-023")]
    [Trait("BR", "BR-MER-023")]
    public Task Test013_POST_BASE_URL_private_configurations_payment_Status_200() => AssertShellAsync(
        Method("POST"),
        "/private/configurations/payment",
        "{\"legacyOperation\":\"phase4c-test\"}",
        200,
        requiredField: null);

    // Source assertion 14: Business rule assertion: BR-MER-024
    // @BR-ID: BR-MER-024
    [Fact(DisplayName = "014: Business rule assertion: BR-MER-024")]
    [Trait("BR", "BR-MER-024")]
    public Task Test014_POST_BASE_URL_private_configurations_payment_Status_200() => AssertShellAsync(
        Method("POST"),
        "/private/configurations/payment",
        "{\"legacyOperation\":\"phase4c-test\"}",
        200,
        requiredField: null);

    // Source assertion 15: Business rule assertion: BR-MER-025
    // @BR-ID: BR-MER-025
    [Fact(DisplayName = "015: Business rule assertion: BR-MER-025")]
    [Trait("BR", "BR-MER-025")]
    public Task Test015_POST_BASE_URL_private_configurations_payment_Status_200() => AssertShellAsync(
        Method("POST"),
        "/private/configurations/payment",
        "{\"legacyOperation\":\"phase4c-test\"}",
        200,
        requiredField: null);

    // Source assertion 16: Business rule assertion: BR-MER-028
    // @BR-ID: BR-MER-028
    [Fact(DisplayName = "016: Business rule assertion: BR-MER-028")]
    [Trait("BR", "BR-MER-028")]
    public Task Test016_POST_BASE_URL_private_configurations_payment_Status_200() => AssertShellAsync(
        Method("POST"),
        "/private/configurations/payment",
        "{\"legacyOperation\":\"phase4c-test\"}",
        200,
        requiredField: null);

    // Source assertion 17: Business rule assertion: BR-CF-001
    // @BR-ID: BR-CF-001
    [Fact(DisplayName = "017: Business rule assertion: BR-CF-001")]
    [Trait("BR", "BR-CF-001")]
    public Task Test017_POST_BASE_URL_private_configurations_payment_Status_200() => AssertShellAsync(
        Method("POST"),
        "/private/configurations/payment",
        "{\"legacyOperation\":\"phase4c-test\"}",
        200,
        requiredField: null);

    // Source assertion 18: Business rule assertion: BR-CF-002
    // @BR-ID: BR-CF-002
    [Fact(DisplayName = "018: Business rule assertion: BR-CF-002")]
    [Trait("BR", "BR-CF-002")]
    public Task Test018_POST_BASE_URL_private_configurations_payment_Status_200() => AssertShellAsync(
        Method("POST"),
        "/private/configurations/payment",
        "{\"legacyOperation\":\"phase4c-test\"}",
        200,
        requiredField: null);

    // Source assertion 19: Business rule assertion: BR-CF-003
    // @BR-ID: BR-CF-003
    [Fact(DisplayName = "019: Business rule assertion: BR-CF-003")]
    [Trait("BR", "BR-CF-003")]
    public Task Test019_POST_BASE_URL_private_configurations_payment_Status_200() => AssertShellAsync(
        Method("POST"),
        "/private/configurations/payment",
        "{\"legacyOperation\":\"phase4c-test\"}",
        200,
        requiredField: null);

    // Source assertion 20: Business rule assertion: BR-CF-004
    // @BR-ID: BR-CF-004
    [Fact(DisplayName = "020: Business rule assertion: BR-CF-004")]
    [Trait("BR", "BR-CF-004")]
    public Task Test020_POST_BASE_URL_private_configurations_payment_Status_200() => AssertShellAsync(
        Method("POST"),
        "/private/configurations/payment",
        "{\"legacyOperation\":\"phase4c-test\"}",
        200,
        requiredField: null);

    // Source assertion 21: Business rule assertion: BR-CF-005
    // @BR-ID: BR-CF-005
    [Fact(DisplayName = "021: Business rule assertion: BR-CF-005")]
    [Trait("BR", "BR-CF-005")]
    public Task Test021_POST_BASE_URL_private_configurations_payment_Status_200() => AssertShellAsync(
        Method("POST"),
        "/private/configurations/payment",
        "{\"legacyOperation\":\"phase4c-test\"}",
        200,
        requiredField: null);

    // Source assertion 22: Business rule assertion: BR-CF-006
    // @BR-ID: BR-CF-006
    [Fact(DisplayName = "022: Business rule assertion: BR-CF-006")]
    [Trait("BR", "BR-CF-006")]
    public Task Test022_POST_BASE_URL_private_configurations_payment_Status_200() => AssertShellAsync(
        Method("POST"),
        "/private/configurations/payment",
        "{\"legacyOperation\":\"phase4c-test\"}",
        200,
        requiredField: null);

    // Source assertion 23: Business rule assertion: BR-CF-007
    // @BR-ID: BR-CF-007
    [Fact(DisplayName = "023: Business rule assertion: BR-CF-007")]
    [Trait("BR", "BR-CF-007")]
    public Task Test023_POST_BASE_URL_private_configurations_payment_Status_200() => AssertShellAsync(
        Method("POST"),
        "/private/configurations/payment",
        "{\"legacyOperation\":\"phase4c-test\"}",
        200,
        requiredField: null);

    // Source assertion 24: Business rule assertion: BR-CF-008
    // @BR-ID: BR-CF-008
    [Fact(DisplayName = "024: Business rule assertion: BR-CF-008")]
    [Trait("BR", "BR-CF-008")]
    public Task Test024_POST_BASE_URL_private_configurations_payment_Status_200() => AssertShellAsync(
        Method("POST"),
        "/private/configurations/payment",
        "{\"legacyOperation\":\"phase4c-test\"}",
        200,
        requiredField: null);

    // Source assertion 25: Business rule assertion: BR-CF-009
    // @BR-ID: BR-CF-009
    [Fact(DisplayName = "025: Business rule assertion: BR-CF-009")]
    [Trait("BR", "BR-CF-009")]
    public Task Test025_POST_BASE_URL_private_configurations_payment_Status_200() => AssertShellAsync(
        Method("POST"),
        "/private/configurations/payment",
        "{\"legacyOperation\":\"phase4c-test\"}",
        200,
        requiredField: null);

    // Source assertion 26: Business rule assertion: BR-CF-010
    // @BR-ID: BR-CF-010
    [Fact(DisplayName = "026: Business rule assertion: BR-CF-010")]
    [Trait("BR", "BR-CF-010")]
    public Task Test026_POST_BASE_URL_private_configurations_payment_Status_200() => AssertShellAsync(
        Method("POST"),
        "/private/configurations/payment",
        "{\"legacyOperation\":\"phase4c-test\"}",
        200,
        requiredField: null);

    // Source assertion 27: Business rule assertion: BR-CF-011
    // @BR-ID: BR-CF-011
    [Fact(DisplayName = "027: Business rule assertion: BR-CF-011")]
    [Trait("BR", "BR-CF-011")]
    public Task Test027_POST_BASE_URL_private_configurations_payment_Status_200() => AssertShellAsync(
        Method("POST"),
        "/private/configurations/payment",
        "{\"legacyOperation\":\"phase4c-test\"}",
        200,
        requiredField: null);

    // Source assertion 28: Business rule assertion: BR-CF-012
    // @BR-ID: BR-CF-012
    [Fact(DisplayName = "028: Business rule assertion: BR-CF-012")]
    [Trait("BR", "BR-CF-012")]
    public Task Test028_POST_BASE_URL_private_configurations_payment_Status_200() => AssertShellAsync(
        Method("POST"),
        "/private/configurations/payment",
        "{\"legacyOperation\":\"phase4c-test\"}",
        200,
        requiredField: null);

    // Source assertion 29: Business rule assertion: BR-CF-013
    // @BR-ID: BR-CF-013
    [Fact(DisplayName = "029: Business rule assertion: BR-CF-013")]
    [Trait("BR", "BR-CF-013")]
    public Task Test029_POST_BASE_URL_private_configurations_payment_Status_200() => AssertShellAsync(
        Method("POST"),
        "/private/configurations/payment",
        "{\"legacyOperation\":\"phase4c-test\"}",
        200,
        requiredField: null);

    // Source assertion 30: Business rule assertion: BR-CF-014
    // @BR-ID: BR-CF-014
    [Fact(DisplayName = "030: Business rule assertion: BR-CF-014")]
    [Trait("BR", "BR-CF-014")]
    public Task Test030_POST_BASE_URL_private_configurations_payment_Status_200() => AssertShellAsync(
        Method("POST"),
        "/private/configurations/payment",
        "{\"legacyOperation\":\"phase4c-test\"}",
        200,
        requiredField: null);

    // Source assertion 31: Business rule assertion: BR-CF-015
    // @BR-ID: BR-CF-015
    [Fact(DisplayName = "031: Business rule assertion: BR-CF-015")]
    [Trait("BR", "BR-CF-015")]
    public Task Test031_POST_BASE_URL_private_configurations_payment_Status_200() => AssertShellAsync(
        Method("POST"),
        "/private/configurations/payment",
        "{\"legacyOperation\":\"phase4c-test\"}",
        200,
        requiredField: null);

    // Source assertion 32: Business rule assertion: BR-EXT-021
    // @BR-ID: BR-EXT-021
    [Fact(DisplayName = "032: Business rule assertion: BR-EXT-021")]
    [Trait("BR", "BR-EXT-021")]
    public Task Test032_POST_BASE_URL_private_configurations_payment_Status_200() => AssertShellAsync(
        Method("POST"),
        "/private/configurations/payment",
        "{\"legacyOperation\":\"phase4c-test\"}",
        200,
        requiredField: null);

    // Source assertion 33: Business rule assertion: BR-EXT-022
    // @BR-ID: BR-EXT-022
    [Fact(DisplayName = "033: Business rule assertion: BR-EXT-022")]
    [Trait("BR", "BR-EXT-022")]
    public Task Test033_POST_BASE_URL_private_configurations_payment_Status_200() => AssertShellAsync(
        Method("POST"),
        "/private/configurations/payment",
        "{\"legacyOperation\":\"phase4c-test\"}",
        200,
        requiredField: null);

    // Source assertion 34: Business rule assertion: BR-EXT-023
    // @BR-ID: BR-EXT-023
    [Fact(DisplayName = "034: Business rule assertion: BR-EXT-023")]
    [Trait("BR", "BR-EXT-023")]
    public Task Test034_POST_BASE_URL_private_configurations_payment_Status_200() => AssertShellAsync(
        Method("POST"),
        "/private/configurations/payment",
        "{\"legacyOperation\":\"phase4c-test\"}",
        200,
        requiredField: null);

    // Source assertion 35: Business rule assertion: BR-EXT-024
    // @BR-ID: BR-EXT-024
    [Fact(DisplayName = "035: Business rule assertion: BR-EXT-024")]
    [Trait("BR", "BR-EXT-024")]
    public Task Test035_POST_BASE_URL_private_configurations_payment_Status_200() => AssertShellAsync(
        Method("POST"),
        "/private/configurations/payment",
        "{\"legacyOperation\":\"phase4c-test\"}",
        200,
        requiredField: null);

    // Source assertion 36: Business rule assertion: BR-EXT-025
    // @BR-ID: BR-EXT-025
    [Fact(DisplayName = "036: Business rule assertion: BR-EXT-025")]
    [Trait("BR", "BR-EXT-025")]
    public Task Test036_POST_BASE_URL_private_configurations_payment_Status_200() => AssertShellAsync(
        Method("POST"),
        "/private/configurations/payment",
        "{\"legacyOperation\":\"phase4c-test\"}",
        200,
        requiredField: null);

    // Source assertion 37: Business rule assertion: BR-EXT-026
    // @BR-ID: BR-EXT-026
    [Fact(DisplayName = "037: Business rule assertion: BR-EXT-026")]
    [Trait("BR", "BR-EXT-026")]
    public Task Test037_POST_BASE_URL_private_configurations_payment_Status_200() => AssertShellAsync(
        Method("POST"),
        "/private/configurations/payment",
        "{\"legacyOperation\":\"phase4c-test\"}",
        200,
        requiredField: null);

    // Source assertion 38: Business rule assertion: BR-EXT-027
    // @BR-ID: BR-EXT-027
    [Fact(DisplayName = "038: Business rule assertion: BR-EXT-027")]
    [Trait("BR", "BR-EXT-027")]
    public Task Test038_POST_BASE_URL_private_configurations_payment_Status_200() => AssertShellAsync(
        Method("POST"),
        "/private/configurations/payment",
        "{\"legacyOperation\":\"phase4c-test\"}",
        200,
        requiredField: null);

    // Source assertion 39: Business rule assertion: BR-EXT-028
    // @BR-ID: BR-EXT-028
    [Fact(DisplayName = "039: Business rule assertion: BR-EXT-028")]
    [Trait("BR", "BR-EXT-028")]
    public Task Test039_POST_BASE_URL_private_configurations_payment_Status_200() => AssertShellAsync(
        Method("POST"),
        "/private/configurations/payment",
        "{\"legacyOperation\":\"phase4c-test\"}",
        200,
        requiredField: null);

    // Source assertion 40: Business rule assertion: BR-EXT-029
    // @BR-ID: BR-EXT-029
    [Fact(DisplayName = "040: Business rule assertion: BR-EXT-029")]
    [Trait("BR", "BR-EXT-029")]
    public Task Test040_POST_BASE_URL_private_configurations_payment_Status_200() => AssertShellAsync(
        Method("POST"),
        "/private/configurations/payment",
        "{\"legacyOperation\":\"phase4c-test\"}",
        200,
        requiredField: null);

    // Source assertion 41: Business rule assertion: BR-EXT-030
    // @BR-ID: BR-EXT-030
    [Fact(DisplayName = "041: Business rule assertion: BR-EXT-030")]
    [Trait("BR", "BR-EXT-030")]
    public Task Test041_POST_BASE_URL_private_configurations_payment_Status_200() => AssertShellAsync(
        Method("POST"),
        "/private/configurations/payment",
        "{\"legacyOperation\":\"phase4c-test\"}",
        200,
        requiredField: null);

    // Source assertion 42: Contract success: POST /private/configurations/shipping
    // @BR-ID: BR-MER-013
    [Fact(DisplayName = "042: Contract success: POST /private/configurations/shipping")]
    [Trait("BR", "BR-MER-013")]
    public Task Test042_POST_BASE_URL_private_configurations_shipping_Status_200() => AssertShellAsync(
        Method("POST"),
        "/private/configurations/shipping",
        "{\"legacyOperation\":\"phase4c-test\"}",
        200,
        requiredField: null);

    // Source assertion 43: Contract error/conformance: POST /private/configurations/shipping
    // @BR-ID: BR-MER-013
    [Fact(DisplayName = "043: Contract error/conformance: POST /private/configurations/shipping")]
    [Trait("BR", "BR-MER-013")]
    public Task Test043_POST_BASE_URL_private_configurations_shipping_Status_410() => AssertShellAsync(
        Method("POST"),
        "/private/configurations/shipping",
        "{}",
        410,
        requiredField: null);

    // Source assertion 44: Contract success: POST /private/content/box
    // @BR-ID: BR-MER-013
    [Fact(DisplayName = "044: Contract success: POST /private/content/box")]
    [Trait("BR", "BR-MER-013")]
    public Task Test044_POST_BASE_URL_private_content_box_Field_id_201() => AssertShellAsync(
        Method("POST"),
        "/private/content/box",
        "{\"code\":\"phase4c-test\",\"visible\":true,\"sortOrder\":1,\"contentPosition\":\"Left\",\"productGroup\":\"phase4c-test\",\"descriptions\":[{\"language\":\"phase4c-test\",\"name\":\"phase4c-test\",\"title\":\"phase4c-test\",\"description\":\"phase4c-test\",\"friendlyUrl\":\"https://example.com/phase4c\",\"metaKeywords\":\"phase4c-test\",\"metaDescription\":\"phase4c-test\"}]}",
        201,
        requiredField: "id");

    // Source assertion 45: Contract error/conformance: POST /private/content/box
    // @BR-ID: BR-MER-013
    [Fact(DisplayName = "045: Contract error/conformance: POST /private/content/box")]
    [Trait("BR", "BR-MER-013")]
    public Task Test045_POST_BASE_URL_private_content_box_Status_400() => AssertShellAsync(
        Method("POST"),
        "/private/content/box",
        "{}",
        400,
        requiredField: null);

    // Source assertion 46: Contract success: POST /private/content/files
    // @BR-ID: BR-MER-013
    [Fact(DisplayName = "046: Contract success: POST /private/content/files")]
    [Trait("BR", "BR-MER-013")]
    public Task Test046_POST_BASE_URL_private_content_files_Field_id_201() => AssertShellAsync(
        Method("POST"),
        "/private/content/files",
        "{\"file\":\"phase4c-test\",\"fileName\":\"phase4c-test\",\"contentType\":{},\"path\":\"phase4c-test\"}",
        201,
        requiredField: "id");

    // Source assertion 47: Contract error/conformance: POST /private/content/files
    // @BR-ID: BR-MER-013
    [Fact(DisplayName = "047: Contract error/conformance: POST /private/content/files")]
    [Trait("BR", "BR-MER-013")]
    public Task Test047_POST_BASE_URL_private_content_files_Status_400() => AssertShellAsync(
        Method("POST"),
        "/private/content/files",
        "{}",
        400,
        requiredField: null);

    // Source assertion 48: Contract success: POST /private/content/files/rename
    // @BR-ID: BR-MER-013
    [Fact(DisplayName = "048: Contract success: POST /private/content/files/rename")]
    [Trait("BR", "BR-MER-013")]
    public Task Test048_POST_BASE_URL_private_content_files_rename_Field_success_200() => AssertShellAsync(
        Method("POST"),
        "/private/content/files/rename",
        "{\"fileName\":\"phase4c-test\",\"newName\":\"phase4c-test\",\"contentType\":{},\"path\":\"phase4c-test\"}",
        200,
        requiredField: "success");

    // Source assertion 49: Contract error/conformance: POST /private/content/files/rename
    // @BR-ID: BR-MER-013
    [Fact(DisplayName = "049: Contract error/conformance: POST /private/content/files/rename")]
    [Trait("BR", "BR-MER-013")]
    public Task Test049_POST_BASE_URL_private_content_files_rename_Status_400() => AssertShellAsync(
        Method("POST"),
        "/private/content/files/rename",
        "{}",
        400,
        requiredField: null);

    // Source assertion 50: Contract success: POST /private/content/folders
    // @BR-ID: BR-MER-013
    [Fact(DisplayName = "050: Contract success: POST /private/content/folders")]
    [Trait("BR", "BR-MER-013")]
    public Task Test050_POST_BASE_URL_private_content_folders_Field_path_201() => AssertShellAsync(
        Method("POST"),
        "/private/content/folders",
        "{\"path\":\"phase4c-test\",\"folderName\":\"phase4c-test\"}",
        201,
        requiredField: "path");

    // Source assertion 51: Contract error/conformance: POST /private/content/folders
    // @BR-ID: BR-MER-013
    [Fact(DisplayName = "051: Contract error/conformance: POST /private/content/folders")]
    [Trait("BR", "BR-MER-013")]
    public Task Test051_POST_BASE_URL_private_content_folders_Status_400() => AssertShellAsync(
        Method("POST"),
        "/private/content/folders",
        "{}",
        400,
        requiredField: null);

    // Source assertion 52: Contract success: POST /private/content/images/add
    // @BR-ID: BR-MER-013
    [Fact(DisplayName = "052: Contract success: POST /private/content/images/add")]
    [Trait("BR", "BR-MER-013")]
    public Task Test052_POST_BASE_URL_private_content_images_add_Field_success_201() => AssertShellAsync(
        Method("POST"),
        "/private/content/images/add",
        "{\"qqfile\":\"phase4c-test\",\"qquuid\":\"00000000-0000-0000-0000-000000000001\",\"qqfilename\":\"phase4c-test\",\"qqtotalfilesize\":1,\"parentPath\":\"phase4c-test\",\"qqpartindex\":1,\"qqtotalparts\":1}",
        201,
        requiredField: "success");

    // Source assertion 53: Contract error/conformance: POST /private/content/images/add
    // @BR-ID: BR-MER-013
    [Fact(DisplayName = "053: Contract error/conformance: POST /private/content/images/add")]
    [Trait("BR", "BR-MER-013")]
    public Task Test053_POST_BASE_URL_private_content_images_add_Status_400() => AssertShellAsync(
        Method("POST"),
        "/private/content/images/add",
        "{}",
        400,
        requiredField: null);

    // Source assertion 54: Contract success: POST /private/content/images/rename
    // @BR-ID: BR-MER-013
    [Fact(DisplayName = "054: Contract success: POST /private/content/images/rename")]
    [Trait("BR", "BR-MER-013")]
    public Task Test054_POST_BASE_URL_private_content_images_rename_Field_success_200() => AssertShellAsync(
        Method("POST"),
        "/private/content/images/rename",
        "{\"path\":\"phase4c-test\",\"newName\":\"phase4c-test\"}",
        200,
        requiredField: "success");

    // Source assertion 55: Contract error/conformance: POST /private/content/images/rename
    // @BR-ID: BR-MER-013
    [Fact(DisplayName = "055: Contract error/conformance: POST /private/content/images/rename")]
    [Trait("BR", "BR-MER-013")]
    public Task Test055_POST_BASE_URL_private_content_images_rename_Status_400() => AssertShellAsync(
        Method("POST"),
        "/private/content/images/rename",
        "{}",
        400,
        requiredField: null);

    // Source assertion 56: Contract success: POST /private/content/page
    // @BR-ID: BR-MER-013
    [Fact(DisplayName = "056: Contract success: POST /private/content/page")]
    [Trait("BR", "BR-MER-013")]
    public Task Test056_POST_BASE_URL_private_content_page_Field_id_201() => AssertShellAsync(
        Method("POST"),
        "/private/content/page",
        "{\"code\":\"phase4c-test\",\"visible\":true,\"linkToMenu\":true,\"sortOrder\":1,\"contentPosition\":\"Left\",\"productGroup\":\"phase4c-test\",\"descriptions\":[{\"language\":\"phase4c-test\",\"name\":\"phase4c-test\",\"title\":\"phase4c-test\",\"description\":\"phase4c-test\",\"friendlyUrl\":\"https://example.com/phase4c\",\"metaKeywords\":\"phase4c-test\",\"metaDescription\":\"phase4c-test\"}]}",
        201,
        requiredField: "id");

    // Source assertion 57: Contract error/conformance: POST /private/content/page
    // @BR-ID: BR-MER-013
    [Fact(DisplayName = "057: Contract error/conformance: POST /private/content/page")]
    [Trait("BR", "BR-MER-013")]
    public Task Test057_POST_BASE_URL_private_content_page_Status_400() => AssertShellAsync(
        Method("POST"),
        "/private/content/page",
        "{}",
        400,
        requiredField: null);

    // Source assertion 58: Contract success: POST /private/file
    // @BR-ID: BR-MER-013
    [Fact(DisplayName = "058: Contract success: POST /private/file")]
    [Trait("BR", "BR-MER-013")]
    public Task Test058_POST_BASE_URL_private_file_Field_id_201() => AssertShellAsync(
        Method("POST"),
        "/private/file",
        "{\"file\":\"phase4c-test\"}",
        201,
        requiredField: "id");

    // Source assertion 59: Contract error/conformance: POST /private/file
    // @BR-ID: BR-MER-013
    [Fact(DisplayName = "059: Contract error/conformance: POST /private/file")]
    [Trait("BR", "BR-MER-013")]
    public Task Test059_POST_BASE_URL_private_file_Status_400() => AssertShellAsync(
        Method("POST"),
        "/private/file",
        "{}",
        400,
        requiredField: null);

    // Source assertion 60: Contract success: POST /private/files
    // @BR-ID: BR-MER-013
    [Fact(DisplayName = "060: Contract success: POST /private/files")]
    [Trait("BR", "BR-MER-013")]
    public Task Test060_POST_BASE_URL_private_files_Field_items_201() => AssertShellAsync(
        Method("POST"),
        "/private/files",
        "{\"file\":[\"phase4c-test\"]}",
        201,
        requiredField: "items");

    // Source assertion 61: Contract error/conformance: POST /private/files
    // @BR-ID: BR-MER-013
    [Fact(DisplayName = "061: Contract error/conformance: POST /private/files")]
    [Trait("BR", "BR-MER-013")]
    public Task Test061_POST_BASE_URL_private_files_Status_400() => AssertShellAsync(
        Method("POST"),
        "/private/files",
        "{}",
        400,
        requiredField: null);

    // Source assertion 62: Contract success: POST /services/private/system/module
    // @BR-ID: BR-MER-013
    [Fact(DisplayName = "062: Contract success: POST /services/private/system/module")]
    [Trait("BR", "BR-MER-013")]
    public Task Test062_POST_BASE_URL_services_private_system_module_Field_status_200() => AssertShellAsync(
        Method("POST"),
        "/services/private/system/module",
        "{\"module\":\"phase4c-test\",\"code\":\"phase4c-test\",\"type\":\"phase4c-test\",\"image\":\"phase4c-test\",\"customModule\":true,\"regions\":[\"phase4c-test\"],\"details\":{},\"configuration\":[{\"env\":\"Test\",\"scheme\":\"phase4c-test\",\"host\":\"phase4c-test\",\"port\":\"phase4c-test\",\"uri\":\"phase4c-test\",\"config1\":\"phase4c-test\",\"config2\":\"phase4c-test\"}]}",
        200,
        requiredField: "status");

    // Source assertion 63: Contract error/conformance: POST /services/private/system/module
    // @BR-ID: BR-MER-013
    [Fact(DisplayName = "063: Contract error/conformance: POST /services/private/system/module")]
    [Trait("BR", "BR-MER-013")]
    public Task Test063_POST_BASE_URL_services_private_system_module_Status_400() => AssertShellAsync(
        Method("POST"),
        "/services/private/system/module",
        "{}",
        400,
        requiredField: null);

    // Source assertion 64: Contract success: POST /services/private/system/optin
    // @BR-ID: BR-MER-013
    [Fact(DisplayName = "064: Contract success: POST /services/private/system/optin")]
    [Trait("BR", "BR-MER-013")]
    public Task Test064_POST_BASE_URL_services_private_system_optin_Status_200() => AssertShellAsync(
        Method("POST"),
        "/services/private/system/optin",
        "{\"legacyOperation\":\"phase4c-test\"}",
        200,
        requiredField: null);

    // Source assertion 65: Contract error/conformance: POST /services/private/system/optin
    // @BR-ID: BR-MER-013
    [Fact(DisplayName = "065: Contract error/conformance: POST /services/private/system/optin")]
    [Trait("BR", "BR-MER-013")]
    public Task Test065_POST_BASE_URL_services_private_system_optin_Status_410() => AssertShellAsync(
        Method("POST"),
        "/services/private/system/optin",
        "{}",
        410,
        requiredField: null);

    // Source assertion 66: Contract success: POST /services/private/system/optin/{code}/customer
    // @BR-ID: BR-MER-013
    [Fact(DisplayName = "066: Contract success: POST /services/private/system/optin/{code}/customer")]
    [Trait("BR", "BR-MER-013")]
    public Task Test066_POST_BASE_URL_services_private_system_optin_phase4c_code_customer_Status_200() => AssertShellAsync(
        Method("POST"),
        "/services/private/system/optin/phase4c-code/customer",
        "{\"legacyOperation\":\"phase4c-test\"}",
        200,
        requiredField: null);

    // Source assertion 67: Contract error/conformance: POST /services/private/system/optin/{code}/customer
    // @BR-ID: BR-MER-013
    [Fact(DisplayName = "067: Contract error/conformance: POST /services/private/system/optin/{code}/customer")]
    [Trait("BR", "BR-MER-013")]
    public Task Test067_POST_BASE_URL_services_private_system_optin_phase4c_code_customer_Status_410() => AssertShellAsync(
        Method("POST"),
        "/services/private/system/optin/phase4c-code/customer",
        "{}",
        410,
        requiredField: null);

    // Source assertion 68: Contract success: GET /config
    // @BR-ID: BR-MER-013
    [Fact(DisplayName = "068: Contract success: GET /config")]
    [Trait("BR", "BR-MER-013")]
    public Task Test068_GET_BASE_URL_config_Field_facebook_200() => AssertShellAsync(
        Method("GET"),
        "/config",
        null,
        200,
        requiredField: "facebook");

    // Source assertion 69: Contract error/conformance: GET /config
    // @BR-ID: BR-MER-013
    [Fact(DisplayName = "069: Contract error/conformance: GET /config")]
    [Trait("BR", "BR-MER-013")]
    public Task Test069_GET_BASE_URL_config_Status_400() => AssertShellAsync(
        Method("GET"),
        "/config",
        null,
        400,
        requiredField: null);

    // Source assertion 70: Contract success: GET /content/boxes
    // @BR-ID: BR-MER-013
    [Fact(DisplayName = "070: Contract success: GET /content/boxes")]
    [Trait("BR", "BR-MER-013")]
    public Task Test070_GET_BASE_URL_content_boxes_Field_items_200() => AssertShellAsync(
        Method("GET"),
        "/content/boxes",
        null,
        200,
        requiredField: "items");

    // Source assertion 71: Contract error/conformance: GET /content/boxes
    // @BR-ID: BR-MER-013
    [Fact(DisplayName = "071: Contract error/conformance: GET /content/boxes")]
    [Trait("BR", "BR-MER-013")]
    public Task Test071_GET_BASE_URL_content_boxes_Status_400() => AssertShellAsync(
        Method("GET"),
        "/content/boxes",
        null,
        400,
        requiredField: null);

    // Source assertion 72: Contract success: GET /content/boxes/{code}
    // @BR-ID: BR-MER-013
    [Fact(DisplayName = "072: Contract success: GET /content/boxes/{code}")]
    [Trait("BR", "BR-MER-013")]
    public Task Test072_GET_BASE_URL_content_boxes_phase4c_code_Field_id_200() => AssertShellAsync(
        Method("GET"),
        "/content/boxes/phase4c-code",
        null,
        200,
        requiredField: "id");

    // Source assertion 73: Contract error/conformance: GET /content/boxes/{code}
    // @BR-ID: BR-MER-013
    [Fact(DisplayName = "073: Contract error/conformance: GET /content/boxes/{code}")]
    [Trait("BR", "BR-MER-013")]
    public Task Test073_GET_BASE_URL_content_boxes_phase4c_code_Status_400() => AssertShellAsync(
        Method("GET"),
        "/content/boxes/phase4c-code",
        null,
        400,
        requiredField: null);

    // Source assertion 74: Contract success: GET /content/images/download
    // @BR-ID: BR-MER-013
    [Fact(DisplayName = "074: Contract success: GET /content/images/download")]
    [Trait("BR", "BR-MER-013")]
    public Task Test074_GET_BASE_URL_content_images_download_Status_200() => AssertShellAsync(
        Method("GET"),
        "/content/images/download",
        null,
        200,
        requiredField: null);

    // Source assertion 75: Contract error/conformance: GET /content/images/download
    // @BR-ID: BR-MER-013
    [Fact(DisplayName = "075: Contract error/conformance: GET /content/images/download")]
    [Trait("BR", "BR-MER-013")]
    public Task Test075_GET_BASE_URL_content_images_download_Status_400() => AssertShellAsync(
        Method("GET"),
        "/content/images/download",
        null,
        400,
        requiredField: null);

    // Source assertion 76: Contract success: GET /content/pages
    // @BR-ID: BR-MER-013
    [Fact(DisplayName = "076: Contract success: GET /content/pages")]
    [Trait("BR", "BR-MER-013")]
    public Task Test076_GET_BASE_URL_content_pages_Field_items_200() => AssertShellAsync(
        Method("GET"),
        "/content/pages",
        null,
        200,
        requiredField: "items");

    // Source assertion 77: Contract error/conformance: GET /content/pages
    // @BR-ID: BR-MER-013
    [Fact(DisplayName = "077: Contract error/conformance: GET /content/pages")]
    [Trait("BR", "BR-MER-013")]
    public Task Test077_GET_BASE_URL_content_pages_Status_400() => AssertShellAsync(
        Method("GET"),
        "/content/pages",
        null,
        400,
        requiredField: null);

    // Source assertion 78: Contract success: GET /content/pages/name/{name}
    // @BR-ID: BR-MER-013
    [Fact(DisplayName = "078: Contract success: GET /content/pages/name/{name}")]
    [Trait("BR", "BR-MER-013")]
    public Task Test078_GET_BASE_URL_content_pages_name_phase4c_value_Field_id_200() => AssertShellAsync(
        Method("GET"),
        "/content/pages/name/phase4c-value",
        null,
        200,
        requiredField: "id");

    // Source assertion 79: Contract error/conformance: GET /content/pages/name/{name}
    // @BR-ID: BR-MER-013
    [Fact(DisplayName = "079: Contract error/conformance: GET /content/pages/name/{name}")]
    [Trait("BR", "BR-MER-013")]
    public Task Test079_GET_BASE_URL_content_pages_name_phase4c_value_Status_400() => AssertShellAsync(
        Method("GET"),
        "/content/pages/name/phase4c-value",
        null,
        400,
        requiredField: null);

    // Source assertion 80: Contract success: GET /content/pages/{code}
    // @BR-ID: BR-MER-013
    [Fact(DisplayName = "080: Contract success: GET /content/pages/{code}")]
    [Trait("BR", "BR-MER-013")]
    public Task Test080_GET_BASE_URL_content_pages_phase4c_code_Field_id_200() => AssertShellAsync(
        Method("GET"),
        "/content/pages/phase4c-code",
        null,
        200,
        requiredField: "id");

    // Source assertion 81: Contract error/conformance: GET /content/pages/{code}
    // @BR-ID: BR-MER-013
    [Fact(DisplayName = "081: Contract error/conformance: GET /content/pages/{code}")]
    [Trait("BR", "BR-MER-013")]
    public Task Test081_GET_BASE_URL_content_pages_phase4c_code_Status_400() => AssertShellAsync(
        Method("GET"),
        "/content/pages/phase4c-code",
        null,
        400,
        requiredField: null);

    // Source assertion 82: Contract success: GET /content/summary
    // @BR-ID: BR-MER-013
    [Fact(DisplayName = "082: Contract success: GET /content/summary")]
    [Trait("BR", "BR-MER-013")]
    public Task Test082_GET_BASE_URL_content_summary_Status_200() => AssertShellAsync(
        Method("GET"),
        "/content/summary",
        null,
        200,
        requiredField: null);

    // Source assertion 83: Contract error/conformance: GET /content/summary
    // @BR-ID: BR-MER-013
    [Fact(DisplayName = "083: Contract error/conformance: GET /content/summary")]
    [Trait("BR", "BR-MER-013")]
    public Task Test083_GET_BASE_URL_content_summary_Status_410() => AssertShellAsync(
        Method("GET"),
        "/content/summary",
        null,
        410,
        requiredField: null);

    // Source assertion 84: Contract success: GET /private/configuration
    // @BR-ID: BR-MER-013
    [Fact(DisplayName = "084: Contract success: GET /private/configuration")]
    [Trait("BR", "BR-MER-013")]
    public Task Test084_GET_BASE_URL_private_configuration_Field_displayCustomerSection_200() => AssertShellAsync(
        Method("GET"),
        "/private/configuration",
        null,
        200,
        requiredField: "displayCustomerSection");

    // Source assertion 85: Contract error/conformance: GET /private/configuration
    // @BR-ID: BR-MER-013
    [Fact(DisplayName = "085: Contract error/conformance: GET /private/configuration")]
    [Trait("BR", "BR-MER-013")]
    public Task Test085_GET_BASE_URL_private_configuration_Status_400() => AssertShellAsync(
        Method("GET"),
        "/private/configuration",
        null,
        400,
        requiredField: null);

    // Source assertion 86: Contract success: GET /private/configurations/payment
    // @BR-ID: BR-MER-013
    [Fact(DisplayName = "086: Contract success: GET /private/configurations/payment")]
    [Trait("BR", "BR-MER-013")]
    public Task Test086_GET_BASE_URL_private_configurations_payment_Status_200() => AssertShellAsync(
        Method("GET"),
        "/private/configurations/payment",
        null,
        200,
        requiredField: null);

    // Source assertion 87: Contract error/conformance: GET /private/configurations/payment
    // @BR-ID: BR-MER-013
    [Fact(DisplayName = "087: Contract error/conformance: GET /private/configurations/payment")]
    [Trait("BR", "BR-MER-013")]
    public Task Test087_GET_BASE_URL_private_configurations_payment_Status_410() => AssertShellAsync(
        Method("GET"),
        "/private/configurations/payment",
        null,
        410,
        requiredField: null);

    // Source assertion 88: Contract success: GET /private/configurations/shipping
    // @BR-ID: BR-MER-013
    [Fact(DisplayName = "088: Contract success: GET /private/configurations/shipping")]
    [Trait("BR", "BR-MER-013")]
    public Task Test088_GET_BASE_URL_private_configurations_shipping_Status_200() => AssertShellAsync(
        Method("GET"),
        "/private/configurations/shipping",
        null,
        200,
        requiredField: null);

    // Source assertion 89: Contract error/conformance: GET /private/configurations/shipping
    // @BR-ID: BR-MER-013
    [Fact(DisplayName = "089: Contract error/conformance: GET /private/configurations/shipping")]
    [Trait("BR", "BR-MER-013")]
    public Task Test089_GET_BASE_URL_private_configurations_shipping_Status_410() => AssertShellAsync(
        Method("GET"),
        "/private/configurations/shipping",
        null,
        410,
        requiredField: null);

    // Source assertion 90: Contract success: GET /private/configurations/{key}
    // @BR-ID: BR-MER-013
    [Fact(DisplayName = "090: Contract success: GET /private/configurations/{key}")]
    [Trait("BR", "BR-MER-013")]
    public Task Test090_GET_BASE_URL_private_configurations_phase4c_value_Field_id_200() => AssertShellAsync(
        Method("GET"),
        "/private/configurations/phase4c-value",
        null,
        200,
        requiredField: "id");

    // Source assertion 91: Contract error/conformance: GET /private/configurations/{key}
    // @BR-ID: BR-MER-013
    [Fact(DisplayName = "091: Contract error/conformance: GET /private/configurations/{key}")]
    [Trait("BR", "BR-MER-013")]
    public Task Test091_GET_BASE_URL_private_configurations_phase4c_value_Status_400() => AssertShellAsync(
        Method("GET"),
        "/private/configurations/phase4c-value",
        null,
        400,
        requiredField: null);

    // Source assertion 92: Contract success: GET /private/content/any/{code}
    // @BR-ID: BR-MER-013
    [Fact(DisplayName = "092: Contract success: GET /private/content/any/{code}")]
    [Trait("BR", "BR-MER-013")]
    public Task Test092_GET_BASE_URL_private_content_any_phase4c_code_Field_id_200() => AssertShellAsync(
        Method("GET"),
        "/private/content/any/phase4c-code",
        null,
        200,
        requiredField: "id");

    // Source assertion 93: Contract error/conformance: GET /private/content/any/{code}
    // @BR-ID: BR-MER-013
    [Fact(DisplayName = "093: Contract error/conformance: GET /private/content/any/{code}")]
    [Trait("BR", "BR-MER-013")]
    public Task Test093_GET_BASE_URL_private_content_any_phase4c_code_Status_400() => AssertShellAsync(
        Method("GET"),
        "/private/content/any/phase4c-code",
        null,
        400,
        requiredField: null);

    // Source assertion 94: Contract success: GET /private/content/box/{code}/exists
    // @BR-ID: BR-MER-013
    [Fact(DisplayName = "094: Contract success: GET /private/content/box/{code}/exists")]
    [Trait("BR", "BR-MER-013")]
    public Task Test094_GET_BASE_URL_private_content_box_phase4c_code_exists_Field_exists_200() => AssertShellAsync(
        Method("GET"),
        "/private/content/box/phase4c-code/exists",
        null,
        200,
        requiredField: "exists");

    // Source assertion 95: Contract error/conformance: GET /private/content/box/{code}/exists
    // @BR-ID: BR-MER-013
    [Fact(DisplayName = "095: Contract error/conformance: GET /private/content/box/{code}/exists")]
    [Trait("BR", "BR-MER-013")]
    public Task Test095_GET_BASE_URL_private_content_box_phase4c_code_exists_Status_400() => AssertShellAsync(
        Method("GET"),
        "/private/content/box/phase4c-code/exists",
        null,
        400,
        requiredField: null);

    // Source assertion 96: Contract success: GET /private/content/boxes
    // @BR-ID: BR-MER-013
    [Fact(DisplayName = "096: Contract success: GET /private/content/boxes")]
    [Trait("BR", "BR-MER-013")]
    public Task Test096_GET_BASE_URL_private_content_boxes_Field_items_200() => AssertShellAsync(
        Method("GET"),
        "/private/content/boxes",
        null,
        200,
        requiredField: "items");

    // Source assertion 97: Contract error/conformance: GET /private/content/boxes
    // @BR-ID: BR-MER-013
    [Fact(DisplayName = "097: Contract error/conformance: GET /private/content/boxes")]
    [Trait("BR", "BR-MER-013")]
    public Task Test097_GET_BASE_URL_private_content_boxes_Status_400() => AssertShellAsync(
        Method("GET"),
        "/private/content/boxes",
        null,
        400,
        requiredField: null);

    // Source assertion 98: Contract success: GET /private/content/boxes/{code}
    // @BR-ID: BR-MER-013
    [Fact(DisplayName = "098: Contract success: GET /private/content/boxes/{code}")]
    [Trait("BR", "BR-MER-013")]
    public Task Test098_GET_BASE_URL_private_content_boxes_phase4c_code_Field_id_200() => AssertShellAsync(
        Method("GET"),
        "/private/content/boxes/phase4c-code",
        null,
        200,
        requiredField: "id");

    // Source assertion 99: Contract error/conformance: GET /private/content/boxes/{code}
    // @BR-ID: BR-MER-013
    [Fact(DisplayName = "099: Contract error/conformance: GET /private/content/boxes/{code}")]
    [Trait("BR", "BR-MER-013")]
    public Task Test099_GET_BASE_URL_private_content_boxes_phase4c_code_Status_400() => AssertShellAsync(
        Method("GET"),
        "/private/content/boxes/phase4c-code",
        null,
        400,
        requiredField: null);

    // Source assertion 100: Contract success: GET /private/content/files
    // @BR-ID: BR-MER-013
    [Fact(DisplayName = "100: Contract success: GET /private/content/files")]
    [Trait("BR", "BR-MER-013")]
    public Task Test100_GET_BASE_URL_private_content_files_Field_items_200() => AssertShellAsync(
        Method("GET"),
        "/private/content/files",
        null,
        200,
        requiredField: "items");

    // Source assertion 101: Contract error/conformance: GET /private/content/files
    // @BR-ID: BR-MER-013
    [Fact(DisplayName = "101: Contract error/conformance: GET /private/content/files")]
    [Trait("BR", "BR-MER-013")]
    public Task Test101_GET_BASE_URL_private_content_files_Status_400() => AssertShellAsync(
        Method("GET"),
        "/private/content/files",
        null,
        400,
        requiredField: null);

    // Source assertion 102: Contract success: GET /private/content/files/{fileName}/download
    // @BR-ID: BR-MER-013
    [Fact(DisplayName = "102: Contract success: GET /private/content/files/{fileName}/download")]
    [Trait("BR", "BR-MER-013")]
    public Task Test102_GET_BASE_URL_private_content_files_phase4c_value_download_Status_200() => AssertShellAsync(
        Method("GET"),
        "/private/content/files/phase4c-value/download",
        null,
        200,
        requiredField: null);

    // Source assertion 103: Contract error/conformance: GET /private/content/files/{fileName}/download
    // @BR-ID: BR-MER-013
    [Fact(DisplayName = "103: Contract error/conformance: GET /private/content/files/{fileName}/download")]
    [Trait("BR", "BR-MER-013")]
    public Task Test103_GET_BASE_URL_private_content_files_phase4c_value_download_Status_400() => AssertShellAsync(
        Method("GET"),
        "/private/content/files/phase4c-value/download",
        null,
        400,
        requiredField: null);

    // Source assertion 104: Contract success: GET /private/content/folder
    // @BR-ID: BR-MER-013
    [Fact(DisplayName = "104: Contract success: GET /private/content/folder")]
    [Trait("BR", "BR-MER-013")]
    public Task Test104_GET_BASE_URL_private_content_folder_Field_path_200() => AssertShellAsync(
        Method("GET"),
        "/private/content/folder",
        null,
        200,
        requiredField: "path");

    // Source assertion 105: Contract error/conformance: GET /private/content/folder
    // @BR-ID: BR-MER-013
    [Fact(DisplayName = "105: Contract error/conformance: GET /private/content/folder")]
    [Trait("BR", "BR-MER-013")]
    public Task Test105_GET_BASE_URL_private_content_folder_Status_400() => AssertShellAsync(
        Method("GET"),
        "/private/content/folder",
        null,
        400,
        requiredField: null);

    // Source assertion 106: Contract success: GET /private/content/folders
    // @BR-ID: BR-MER-013
    [Fact(DisplayName = "106: Contract success: GET /private/content/folders")]
    [Trait("BR", "BR-MER-013")]
    public Task Test106_GET_BASE_URL_private_content_folders_Field_path_200() => AssertShellAsync(
        Method("GET"),
        "/private/content/folders",
        null,
        200,
        requiredField: "path");

    // Source assertion 107: Contract error/conformance: GET /private/content/folders
    // @BR-ID: BR-MER-013
    [Fact(DisplayName = "107: Contract error/conformance: GET /private/content/folders")]
    [Trait("BR", "BR-MER-013")]
    public Task Test107_GET_BASE_URL_private_content_folders_Status_400() => AssertShellAsync(
        Method("GET"),
        "/private/content/folders",
        null,
        400,
        requiredField: null);

    // Source assertion 108: Contract success: GET /private/content/list
    // @BR-ID: BR-MER-013
    [Fact(DisplayName = "108: Contract success: GET /private/content/list")]
    [Trait("BR", "BR-MER-013")]
    public Task Test108_GET_BASE_URL_private_content_list_Status_200() => AssertShellAsync(
        Method("GET"),
        "/private/content/list",
        null,
        200,
        requiredField: null);

    // Source assertion 109: Contract error/conformance: GET /private/content/list
    // @BR-ID: BR-MER-013
    [Fact(DisplayName = "109: Contract error/conformance: GET /private/content/list")]
    [Trait("BR", "BR-MER-013")]
    public Task Test109_GET_BASE_URL_private_content_list_Status_400() => AssertShellAsync(
        Method("GET"),
        "/private/content/list",
        null,
        400,
        requiredField: null);

    // Source assertion 110: Contract success: GET /private/content/page/{code}/exists
    // @BR-ID: BR-MER-013
    [Fact(DisplayName = "110: Contract success: GET /private/content/page/{code}/exists")]
    [Trait("BR", "BR-MER-013")]
    public Task Test110_GET_BASE_URL_private_content_page_phase4c_code_exists_Field_exists_200() => AssertShellAsync(
        Method("GET"),
        "/private/content/page/phase4c-code/exists",
        null,
        200,
        requiredField: "exists");

    // Source assertion 111: Contract error/conformance: GET /private/content/page/{code}/exists
    // @BR-ID: BR-MER-013
    [Fact(DisplayName = "111: Contract error/conformance: GET /private/content/page/{code}/exists")]
    [Trait("BR", "BR-MER-013")]
    public Task Test111_GET_BASE_URL_private_content_page_phase4c_code_exists_Status_400() => AssertShellAsync(
        Method("GET"),
        "/private/content/page/phase4c-code/exists",
        null,
        400,
        requiredField: null);

    // Source assertion 112: Contract success: GET /private/content/pages
    // @BR-ID: BR-MER-013
    [Fact(DisplayName = "112: Contract success: GET /private/content/pages")]
    [Trait("BR", "BR-MER-013")]
    public Task Test112_GET_BASE_URL_private_content_pages_Field_items_200() => AssertShellAsync(
        Method("GET"),
        "/private/content/pages",
        null,
        200,
        requiredField: "items");

    // Source assertion 113: Contract error/conformance: GET /private/content/pages
    // @BR-ID: BR-MER-013
    [Fact(DisplayName = "113: Contract error/conformance: GET /private/content/pages")]
    [Trait("BR", "BR-MER-013")]
    public Task Test113_GET_BASE_URL_private_content_pages_Status_400() => AssertShellAsync(
        Method("GET"),
        "/private/content/pages",
        null,
        400,
        requiredField: null);

    // Source assertion 114: Contract success: GET /private/content/pages/{code}
    // @BR-ID: BR-MER-013
    [Fact(DisplayName = "114: Contract success: GET /private/content/pages/{code}")]
    [Trait("BR", "BR-MER-013")]
    public Task Test114_GET_BASE_URL_private_content_pages_phase4c_code_Field_id_200() => AssertShellAsync(
        Method("GET"),
        "/private/content/pages/phase4c-code",
        null,
        200,
        requiredField: "id");

    // Source assertion 115: Contract error/conformance: GET /private/content/pages/{code}
    // @BR-ID: BR-MER-013
    [Fact(DisplayName = "115: Contract error/conformance: GET /private/content/pages/{code}")]
    [Trait("BR", "BR-MER-013")]
    public Task Test115_GET_BASE_URL_private_content_pages_phase4c_code_Status_400() => AssertShellAsync(
        Method("GET"),
        "/private/content/pages/phase4c-code",
        null,
        400,
        requiredField: null);

    // Source assertion 116: Contract success: GET /private/contents/any
    // @BR-ID: BR-MER-013
    [Fact(DisplayName = "116: Contract success: GET /private/contents/any")]
    [Trait("BR", "BR-MER-013")]
    public Task Test116_GET_BASE_URL_private_contents_any_Status_200() => AssertShellAsync(
        Method("GET"),
        "/private/contents/any",
        null,
        200,
        requiredField: null);

    // Source assertion 117: Contract error/conformance: GET /private/contents/any
    // @BR-ID: BR-MER-013
    [Fact(DisplayName = "117: Contract error/conformance: GET /private/contents/any")]
    [Trait("BR", "BR-MER-013")]
    public Task Test117_GET_BASE_URL_private_contents_any_Status_400() => AssertShellAsync(
        Method("GET"),
        "/private/contents/any",
        null,
        400,
        requiredField: null);

    // Source assertion 118: Contract success: GET /private/modules/payment
    // @BR-ID: BR-MER-013
    [Fact(DisplayName = "118: Contract success: GET /private/modules/payment")]
    [Trait("BR", "BR-MER-013")]
    public Task Test118_GET_BASE_URL_private_modules_payment_Status_200() => AssertShellAsync(
        Method("GET"),
        "/private/modules/payment",
        null,
        200,
        requiredField: null);

    // Source assertion 119: Contract error/conformance: GET /private/modules/payment
    // @BR-ID: BR-MER-013
    [Fact(DisplayName = "119: Contract error/conformance: GET /private/modules/payment")]
    [Trait("BR", "BR-MER-013")]
    public Task Test119_GET_BASE_URL_private_modules_payment_Status_400() => AssertShellAsync(
        Method("GET"),
        "/private/modules/payment",
        null,
        400,
        requiredField: null);

    // Source assertion 120: Contract success: GET /private/modules/payment/{code}
    // @BR-ID: BR-MER-013
    [Fact(DisplayName = "120: Contract success: GET /private/modules/payment/{code}")]
    [Trait("BR", "BR-MER-013")]
    public Task Test120_GET_BASE_URL_private_modules_payment_phase4c_code_Field_code_200() => AssertShellAsync(
        Method("GET"),
        "/private/modules/payment/phase4c-code",
        null,
        200,
        requiredField: "code");

    // Source assertion 121: Contract error/conformance: GET /private/modules/payment/{code}
    // @BR-ID: BR-MER-013
    [Fact(DisplayName = "121: Contract error/conformance: GET /private/modules/payment/{code}")]
    [Trait("BR", "BR-MER-013")]
    public Task Test121_GET_BASE_URL_private_modules_payment_phase4c_code_Status_400() => AssertShellAsync(
        Method("GET"),
        "/private/modules/payment/phase4c-code",
        null,
        400,
        requiredField: null);

    // Source assertion 122: Contract success: GET /private/modules/shipping
    // @BR-ID: BR-MER-013
    [Fact(DisplayName = "122: Contract success: GET /private/modules/shipping")]
    [Trait("BR", "BR-MER-013")]
    public Task Test122_GET_BASE_URL_private_modules_shipping_Status_200() => AssertShellAsync(
        Method("GET"),
        "/private/modules/shipping",
        null,
        200,
        requiredField: null);

    // Source assertion 123: Contract error/conformance: GET /private/modules/shipping
    // @BR-ID: BR-MER-013
    [Fact(DisplayName = "123: Contract error/conformance: GET /private/modules/shipping")]
    [Trait("BR", "BR-MER-013")]
    public Task Test123_GET_BASE_URL_private_modules_shipping_Status_400() => AssertShellAsync(
        Method("GET"),
        "/private/modules/shipping",
        null,
        400,
        requiredField: null);

    // Source assertion 124: Contract success: GET /private/modules/shipping/{code}
    // @BR-ID: BR-MER-013
    [Fact(DisplayName = "124: Contract success: GET /private/modules/shipping/{code}")]
    [Trait("BR", "BR-MER-013")]
    public Task Test124_GET_BASE_URL_private_modules_shipping_phase4c_code_Status_200() => AssertShellAsync(
        Method("GET"),
        "/private/modules/shipping/phase4c-code",
        null,
        200,
        requiredField: null);

    // Source assertion 125: Contract error/conformance: GET /private/modules/shipping/{code}
    // @BR-ID: BR-MER-013
    [Fact(DisplayName = "125: Contract error/conformance: GET /private/modules/shipping/{code}")]
    [Trait("BR", "BR-MER-013")]
    public Task Test125_GET_BASE_URL_private_modules_shipping_phase4c_code_Status_400() => AssertShellAsync(
        Method("GET"),
        "/private/modules/shipping/phase4c-code",
        null,
        400,
        requiredField: null);

    // Source assertion 126: Contract success: PUT /private/configuration
    // @BR-ID: BR-MER-013
    [Fact(DisplayName = "126: Contract success: PUT /private/configuration")]
    [Trait("BR", "BR-MER-013")]
    public Task Test126_PUT_BASE_URL_private_configuration_Field_displayCustomerSection_200() => AssertShellAsync(
        Method("PUT"),
        "/private/configuration",
        "{\"displayCustomerSection\":true,\"displayContactUs\":true,\"displayStoreAddress\":true,\"displayAddToCartOnFeaturedItems\":true,\"displayCustomerAgreement\":true,\"displayPagesMenu\":true,\"allowPurchaseItems\":true,\"displaySearchBox\":true,\"testMode\":true,\"debugMode\":true,\"useDefaultSearchConfig\":{},\"defaultSearchConfigPath\":{},\"socialValues\":{}}",
        200,
        requiredField: "displayCustomerSection");

    // Source assertion 127: Contract error/conformance: PUT /private/configuration
    // @BR-ID: BR-MER-013
    [Fact(DisplayName = "127: Contract error/conformance: PUT /private/configuration")]
    [Trait("BR", "BR-MER-013")]
    public Task Test127_PUT_BASE_URL_private_configuration_Status_400() => AssertShellAsync(
        Method("PUT"),
        "/private/configuration",
        "{}",
        400,
        requiredField: null);

    // Source assertion 128: Contract success: PUT /private/configurations/{key}
    // @BR-ID: BR-MER-013
    [Fact(DisplayName = "128: Contract success: PUT /private/configurations/{key}")]
    [Trait("BR", "BR-MER-013")]
    public Task Test128_PUT_BASE_URL_private_configurations_phase4c_value_Field_id_200() => AssertShellAsync(
        Method("PUT"),
        "/private/configurations/phase4c-value",
        "{\"type\":{},\"active\":true,\"value\":{}}",
        200,
        requiredField: "id");

    // Source assertion 129: Contract error/conformance: PUT /private/configurations/{key}
    // @BR-ID: BR-MER-013
    [Fact(DisplayName = "129: Contract error/conformance: PUT /private/configurations/{key}")]
    [Trait("BR", "BR-MER-013")]
    public Task Test129_PUT_BASE_URL_private_configurations_phase4c_value_Status_400() => AssertShellAsync(
        Method("PUT"),
        "/private/configurations/phase4c-value",
        "{}",
        400,
        requiredField: null);

    // Source assertion 130: Contract success: PUT /private/content/box/{contentId}
    // @BR-ID: BR-MER-013
    [Fact(DisplayName = "130: Contract success: PUT /private/content/box/{contentId}")]
    [Trait("BR", "BR-MER-013")]
    public Task Test130_PUT_BASE_URL_private_content_box_ById_Status_204() => AssertShellAsync(
        Method("PUT"),
        $"/private/content/box/{ResourceId}",
        "{}",
        204,
        requiredField: null);

    // Source assertion 131: Contract error/conformance: PUT /private/content/box/{contentId}
    // @BR-ID: BR-MER-013
    [Fact(DisplayName = "131: Contract error/conformance: PUT /private/content/box/{contentId}")]
    [Trait("BR", "BR-MER-013")]
    public Task Test131_PUT_BASE_URL_private_content_box_ById_Status_400() => AssertShellAsync(
        Method("PUT"),
        $"/private/content/box/{ResourceId}",
        "{}",
        400,
        requiredField: null);

    // Source assertion 132: Contract success: PUT /private/content/page/{contentId}
    // @BR-ID: BR-MER-013
    [Fact(DisplayName = "132: Contract success: PUT /private/content/page/{contentId}")]
    [Trait("BR", "BR-MER-013")]
    public Task Test132_PUT_BASE_URL_private_content_page_ById_Status_204() => AssertShellAsync(
        Method("PUT"),
        $"/private/content/page/{ResourceId}",
        "{}",
        204,
        requiredField: null);

    // Source assertion 133: Contract error/conformance: PUT /private/content/page/{contentId}
    // @BR-ID: BR-MER-013
    [Fact(DisplayName = "133: Contract error/conformance: PUT /private/content/page/{contentId}")]
    [Trait("BR", "BR-MER-013")]
    public Task Test133_PUT_BASE_URL_private_content_page_ById_Status_400() => AssertShellAsync(
        Method("PUT"),
        $"/private/content/page/{ResourceId}",
        "{}",
        400,
        requiredField: null);

    // Source assertion 134: Contract success: PUT /private/content/{contentId}
    // @BR-ID: BR-MER-013
    [Fact(DisplayName = "134: Contract success: PUT /private/content/{contentId}")]
    [Trait("BR", "BR-MER-013")]
    public Task Test134_PUT_BASE_URL_private_content_ById_Status_200() => AssertShellAsync(
        Method("PUT"),
        $"/private/content/{ResourceId}",
        "{\"legacyOperation\":\"phase4c-test\"}",
        200,
        requiredField: null);

    // Source assertion 135: Contract error/conformance: PUT /private/content/{contentId}
    // @BR-ID: BR-MER-013
    [Fact(DisplayName = "135: Contract error/conformance: PUT /private/content/{contentId}")]
    [Trait("BR", "BR-MER-013")]
    public Task Test135_PUT_BASE_URL_private_content_ById_Status_410() => AssertShellAsync(
        Method("PUT"),
        $"/private/content/{ResourceId}",
        "{}",
        410,
        requiredField: null);

    // Source assertion 136: Contract success: PUT /private/modules/payment/{code}
    // @BR-ID: BR-MER-013
    [Fact(DisplayName = "136: Contract success: PUT /private/modules/payment/{code}")]
    [Trait("BR", "BR-MER-013")]
    public Task Test136_PUT_BASE_URL_private_modules_payment_phase4c_code_Field_code_200() => AssertShellAsync(
        Method("PUT"),
        "/private/modules/payment/phase4c-code",
        "{\"active\":true,\"defaultSelected\":true,\"integrationKeys\":{},\"integrationOptions\":{},\"environment\":\"Test\"}",
        200,
        requiredField: "code");

    // Source assertion 137: Contract error/conformance: PUT /private/modules/payment/{code}
    // @BR-ID: BR-MER-013
    [Fact(DisplayName = "137: Contract error/conformance: PUT /private/modules/payment/{code}")]
    [Trait("BR", "BR-MER-013")]
    public Task Test137_PUT_BASE_URL_private_modules_payment_phase4c_code_Status_400() => AssertShellAsync(
        Method("PUT"),
        "/private/modules/payment/phase4c-code",
        "{}",
        400,
        requiredField: null);

    // Source assertion 138: Contract success: PUT /private/modules/shipping/{code}
    // @BR-ID: BR-MER-013
    [Fact(DisplayName = "138: Contract success: PUT /private/modules/shipping/{code}")]
    [Trait("BR", "BR-MER-013")]
    public Task Test138_PUT_BASE_URL_private_modules_shipping_phase4c_code_Status_200() => AssertShellAsync(
        Method("PUT"),
        "/private/modules/shipping/phase4c-code",
        "{\"active\":true,\"defaultSelected\":true,\"integrationKeys\":{},\"integrationOptions\":{},\"environment\":\"Test\"}",
        200,
        requiredField: null);

    // Source assertion 139: Contract error/conformance: PUT /private/modules/shipping/{code}
    // @BR-ID: BR-MER-013
    [Fact(DisplayName = "139: Contract error/conformance: PUT /private/modules/shipping/{code}")]
    [Trait("BR", "BR-MER-013")]
    public Task Test139_PUT_BASE_URL_private_modules_shipping_phase4c_code_Status_400() => AssertShellAsync(
        Method("PUT"),
        "/private/modules/shipping/phase4c-code",
        "{}",
        400,
        requiredField: null);

    // Source assertion 140: Contract success: DELETE /content/folder
    // @BR-ID: BR-MER-013
    [Fact(DisplayName = "140: Contract success: DELETE /content/folder")]
    [Trait("BR", "BR-MER-013")]
    public Task Test140_DELETE_BASE_URL_content_folder_Status_200() => AssertShellAsync(
        Method("DELETE"),
        "/content/folder",
        null,
        200,
        requiredField: null);

    // Source assertion 141: Contract error/conformance: DELETE /content/folder
    // @BR-ID: BR-MER-013
    [Fact(DisplayName = "141: Contract error/conformance: DELETE /content/folder")]
    [Trait("BR", "BR-MER-013")]
    public Task Test141_DELETE_BASE_URL_content_folder_Status_410() => AssertShellAsync(
        Method("DELETE"),
        "/content/folder",
        null,
        410,
        requiredField: null);

    // Source assertion 142: Contract success: DELETE /private/content/box/{contentId}
    // @BR-ID: BR-MER-013
    [Fact(DisplayName = "142: Contract success: DELETE /private/content/box/{contentId}")]
    [Trait("BR", "BR-MER-013")]
    public Task Test142_DELETE_BASE_URL_private_content_box_ById_Status_204() => AssertShellAsync(
        Method("DELETE"),
        $"/private/content/box/{ResourceId}",
        null,
        204,
        requiredField: null);

    // Source assertion 143: Contract error/conformance: DELETE /private/content/box/{contentId}
    // @BR-ID: BR-MER-013
    [Fact(DisplayName = "143: Contract error/conformance: DELETE /private/content/box/{contentId}")]
    [Trait("BR", "BR-MER-013")]
    public Task Test143_DELETE_BASE_URL_private_content_box_ById_Status_400() => AssertShellAsync(
        Method("DELETE"),
        $"/private/content/box/{ResourceId}",
        null,
        400,
        requiredField: null);

    // Source assertion 144: Contract success: DELETE /private/content/files/{fileName}
    // @BR-ID: BR-MER-013
    [Fact(DisplayName = "144: Contract success: DELETE /private/content/files/{fileName}")]
    [Trait("BR", "BR-MER-013")]
    public Task Test144_DELETE_BASE_URL_private_content_files_phase4c_value_Status_204() => AssertShellAsync(
        Method("DELETE"),
        "/private/content/files/phase4c-value",
        null,
        204,
        requiredField: null);

    // Source assertion 145: Contract error/conformance: DELETE /private/content/files/{fileName}
    // @BR-ID: BR-MER-013
    [Fact(DisplayName = "145: Contract error/conformance: DELETE /private/content/files/{fileName}")]
    [Trait("BR", "BR-MER-013")]
    public Task Test145_DELETE_BASE_URL_private_content_files_phase4c_value_Status_400() => AssertShellAsync(
        Method("DELETE"),
        "/private/content/files/phase4c-value",
        null,
        400,
        requiredField: null);

    // Source assertion 146: Contract success: DELETE /private/content/folders
    // @BR-ID: BR-MER-013
    [Fact(DisplayName = "146: Contract success: DELETE /private/content/folders")]
    [Trait("BR", "BR-MER-013")]
    public Task Test146_DELETE_BASE_URL_private_content_folders_Status_204() => AssertShellAsync(
        Method("DELETE"),
        "/private/content/folders",
        null,
        204,
        requiredField: null);

    // Source assertion 147: Contract error/conformance: DELETE /private/content/folders
    // @BR-ID: BR-MER-013
    [Fact(DisplayName = "147: Contract error/conformance: DELETE /private/content/folders")]
    [Trait("BR", "BR-MER-013")]
    public Task Test147_DELETE_BASE_URL_private_content_folders_Status_400() => AssertShellAsync(
        Method("DELETE"),
        "/private/content/folders",
        null,
        400,
        requiredField: null);

    // Source assertion 148: Contract success: DELETE /private/content/images/remove
    // @BR-ID: BR-MER-013
    [Fact(DisplayName = "148: Contract success: DELETE /private/content/images/remove")]
    [Trait("BR", "BR-MER-013")]
    public Task Test148_DELETE_BASE_URL_private_content_images_remove_Status_204() => AssertShellAsync(
        Method("DELETE"),
        "/private/content/images/remove",
        null,
        204,
        requiredField: null);

    // Source assertion 149: Contract error/conformance: DELETE /private/content/images/remove
    // @BR-ID: BR-MER-013
    [Fact(DisplayName = "149: Contract error/conformance: DELETE /private/content/images/remove")]
    [Trait("BR", "BR-MER-013")]
    public Task Test149_DELETE_BASE_URL_private_content_images_remove_Status_400() => AssertShellAsync(
        Method("DELETE"),
        "/private/content/images/remove",
        null,
        400,
        requiredField: null);

    // Source assertion 150: Contract success: DELETE /private/content/page/{contentId}
    // @BR-ID: BR-MER-013
    [Fact(DisplayName = "150: Contract success: DELETE /private/content/page/{contentId}")]
    [Trait("BR", "BR-MER-013")]
    public Task Test150_DELETE_BASE_URL_private_content_page_ById_Status_204() => AssertShellAsync(
        Method("DELETE"),
        $"/private/content/page/{ResourceId}",
        null,
        204,
        requiredField: null);

    // Source assertion 151: Contract error/conformance: DELETE /private/content/page/{contentId}
    // @BR-ID: BR-MER-013
    [Fact(DisplayName = "151: Contract error/conformance: DELETE /private/content/page/{contentId}")]
    [Trait("BR", "BR-MER-013")]
    public Task Test151_DELETE_BASE_URL_private_content_page_ById_Status_400() => AssertShellAsync(
        Method("DELETE"),
        $"/private/content/page/{ResourceId}",
        null,
        400,
        requiredField: null);

    // Source assertion 152: Contract success: DELETE /private/content/{contentId}
    // @BR-ID: BR-MER-013
    [Fact(DisplayName = "152: Contract success: DELETE /private/content/{contentId}")]
    [Trait("BR", "BR-MER-013")]
    public Task Test152_DELETE_BASE_URL_private_content_ById_Status_200() => AssertShellAsync(
        Method("DELETE"),
        $"/private/content/{ResourceId}",
        null,
        200,
        requiredField: null);

    // Source assertion 153: Contract error/conformance: DELETE /private/content/{contentId}
    // @BR-ID: BR-MER-013
    [Fact(DisplayName = "153: Contract error/conformance: DELETE /private/content/{contentId}")]
    [Trait("BR", "BR-MER-013")]
    public Task Test153_DELETE_BASE_URL_private_content_ById_Status_410() => AssertShellAsync(
        Method("DELETE"),
        $"/private/content/{ResourceId}",
        null,
        410,
        requiredField: null);

    // Source assertion 154: Contract success: DELETE /services/private/system/optin/{code}
    // @BR-ID: BR-MER-013
    [Fact(DisplayName = "154: Contract success: DELETE /services/private/system/optin/{code}")]
    [Trait("BR", "BR-MER-013")]
    public Task Test154_DELETE_BASE_URL_services_private_system_optin_phase4c_code_Status_200() => AssertShellAsync(
        Method("DELETE"),
        "/services/private/system/optin/phase4c-code",
        null,
        200,
        requiredField: null);

    // Source assertion 155: Contract error/conformance: DELETE /services/private/system/optin/{code}
    // @BR-ID: BR-MER-013
    [Fact(DisplayName = "155: Contract error/conformance: DELETE /services/private/system/optin/{code}")]
    [Trait("BR", "BR-MER-013")]
    public Task Test155_DELETE_BASE_URL_services_private_system_optin_phase4c_code_Status_410() => AssertShellAsync(
        Method("DELETE"),
        "/services/private/system/optin/phase4c-code",
        null,
        410,
        requiredField: null);

    // Source assertion 156: Extension point EXT-CMS-021 - configured behavior
    // @BR-ID: BR-MER-013
    [Fact(DisplayName = "156: Extension point EXT-CMS-021 - configured behavior")]
    [Trait("BR", "BR-MER-013")]
    public Task Test156_POST_BASE_URL_private_configurations_payment_Status_200() => AssertShellAsync(
        Method("POST"),
        "/private/configurations/payment",
        null,
        200,
        requiredField: null);

    // Source assertion 157: Extension point EXT-CMS-021 - default when unconfigured
    // @BR-ID: BR-MER-013
    [Fact(DisplayName = "157: Extension point EXT-CMS-021 - default when unconfigured")]
    [Trait("BR", "BR-MER-013")]
    public Task Test157_POST_BASE_URL_private_configurations_payment_Status_200() => AssertShellAsync(
        Method("POST"),
        "/private/configurations/payment",
        null,
        200,
        requiredField: null);

    // Source assertion 158: Extension point EXT-CMS-021 - metadata round-trip
    // @BR-ID: BR-MER-013
    [Fact(DisplayName = "158: Extension point EXT-CMS-021 - metadata round-trip")]
    [Trait("BR", "BR-MER-013")]
    public Task Test158_POST_BASE_URL_private_configurations_payment_Status_200() => AssertShellAsync(
        Method("POST"),
        "/private/configurations/payment",
        null,
        200,
        requiredField: null);

    // Source assertion 159: Extension point EXT-PAY-024 - configured behavior
    // @BR-ID: BR-MER-013
    [Fact(DisplayName = "159: Extension point EXT-PAY-024 - configured behavior")]
    [Trait("BR", "BR-MER-013")]
    public Task Test159_POST_BASE_URL_private_configurations_payment_Status_200() => AssertShellAsync(
        Method("POST"),
        "/private/configurations/payment",
        null,
        200,
        requiredField: null);

    // Source assertion 160: Extension point EXT-PAY-024 - default when unconfigured
    // @BR-ID: BR-MER-013
    [Fact(DisplayName = "160: Extension point EXT-PAY-024 - default when unconfigured")]
    [Trait("BR", "BR-MER-013")]
    public Task Test160_POST_BASE_URL_private_configurations_payment_Status_200() => AssertShellAsync(
        Method("POST"),
        "/private/configurations/payment",
        null,
        200,
        requiredField: null);

    // Source assertion 161: Extension point EXT-PAY-024 - metadata round-trip
    // @BR-ID: BR-MER-013
    [Fact(DisplayName = "161: Extension point EXT-PAY-024 - metadata round-trip")]
    [Trait("BR", "BR-MER-013")]
    public Task Test161_POST_BASE_URL_private_configurations_payment_Status_200() => AssertShellAsync(
        Method("POST"),
        "/private/configurations/payment",
        null,
        200,
        requiredField: null);

    // Source assertion 162: Extension point EXT-PROVIDER-025 - configured behavior
    // @BR-ID: BR-MER-013
    [Fact(DisplayName = "162: Extension point EXT-PROVIDER-025 - configured behavior")]
    [Trait("BR", "BR-MER-013")]
    public Task Test162_POST_BASE_URL_private_configurations_payment_Status_200() => AssertShellAsync(
        Method("POST"),
        "/private/configurations/payment",
        null,
        200,
        requiredField: null);

    // Source assertion 163: Extension point EXT-PROVIDER-025 - default when unconfigured
    // @BR-ID: BR-MER-013
    [Fact(DisplayName = "163: Extension point EXT-PROVIDER-025 - default when unconfigured")]
    [Trait("BR", "BR-MER-013")]
    public Task Test163_POST_BASE_URL_private_configurations_payment_Status_200() => AssertShellAsync(
        Method("POST"),
        "/private/configurations/payment",
        null,
        200,
        requiredField: null);

    // Source assertion 164: Extension point EXT-PROVIDER-025 - metadata round-trip
    // @BR-ID: BR-MER-013
    [Fact(DisplayName = "164: Extension point EXT-PROVIDER-025 - metadata round-trip")]
    [Trait("BR", "BR-MER-013")]
    public Task Test164_POST_BASE_URL_private_configurations_payment_Status_200() => AssertShellAsync(
        Method("POST"),
        "/private/configurations/payment",
        null,
        200,
        requiredField: null);

    // Source assertion 165: Extension point EXT-MODULE-CACHE-026 - configured behavior
    // @BR-ID: BR-MER-013
    [Fact(DisplayName = "165: Extension point EXT-MODULE-CACHE-026 - configured behavior")]
    [Trait("BR", "BR-MER-013")]
    public Task Test165_POST_BASE_URL_private_configurations_payment_Status_200() => AssertShellAsync(
        Method("POST"),
        "/private/configurations/payment",
        null,
        200,
        requiredField: null);

    // Source assertion 166: Extension point EXT-MODULE-CACHE-026 - default when unconfigured
    // @BR-ID: BR-MER-013
    [Fact(DisplayName = "166: Extension point EXT-MODULE-CACHE-026 - default when unconfigured")]
    [Trait("BR", "BR-MER-013")]
    public Task Test166_POST_BASE_URL_private_configurations_payment_Status_200() => AssertShellAsync(
        Method("POST"),
        "/private/configurations/payment",
        null,
        200,
        requiredField: null);

    // Source assertion 167: Extension point EXT-MODULE-CACHE-026 - metadata round-trip
    // @BR-ID: BR-MER-013
    [Fact(DisplayName = "167: Extension point EXT-MODULE-CACHE-026 - metadata round-trip")]
    [Trait("BR", "BR-MER-013")]
    public Task Test167_POST_BASE_URL_private_configurations_payment_Status_200() => AssertShellAsync(
        Method("POST"),
        "/private/configurations/payment",
        null,
        200,
        requiredField: null);

    // Source assertion 168: Extension point EXT-CONFIG-027 - configured behavior
    // @BR-ID: BR-MER-013
    [Fact(DisplayName = "168: Extension point EXT-CONFIG-027 - configured behavior")]
    [Trait("BR", "BR-MER-013")]
    public Task Test168_POST_BASE_URL_private_configurations_payment_Status_200() => AssertShellAsync(
        Method("POST"),
        "/private/configurations/payment",
        null,
        200,
        requiredField: null);

    // Source assertion 169: Extension point EXT-CONFIG-027 - default when unconfigured
    // @BR-ID: BR-MER-013
    [Fact(DisplayName = "169: Extension point EXT-CONFIG-027 - default when unconfigured")]
    [Trait("BR", "BR-MER-013")]
    public Task Test169_POST_BASE_URL_private_configurations_payment_Status_200() => AssertShellAsync(
        Method("POST"),
        "/private/configurations/payment",
        null,
        200,
        requiredField: null);

    // Source assertion 170: Extension point EXT-CONFIG-027 - metadata round-trip
    // @BR-ID: BR-MER-013
    [Fact(DisplayName = "170: Extension point EXT-CONFIG-027 - metadata round-trip")]
    [Trait("BR", "BR-MER-013")]
    public Task Test170_POST_BASE_URL_private_configurations_payment_Status_200() => AssertShellAsync(
        Method("POST"),
        "/private/configurations/payment",
        null,
        200,
        requiredField: null);

    // Source assertion 171: Extension point EXT-MODULE-METADATA-028 - configured behavior
    // @BR-ID: BR-MER-013
    [Fact(DisplayName = "171: Extension point EXT-MODULE-METADATA-028 - configured behavior")]
    [Trait("BR", "BR-MER-013")]
    public Task Test171_POST_BASE_URL_private_configurations_payment_Status_200() => AssertShellAsync(
        Method("POST"),
        "/private/configurations/payment",
        null,
        200,
        requiredField: null);

    // Source assertion 172: Extension point EXT-MODULE-METADATA-028 - default when unconfigured
    // @BR-ID: BR-MER-013
    [Fact(DisplayName = "172: Extension point EXT-MODULE-METADATA-028 - default when unconfigured")]
    [Trait("BR", "BR-MER-013")]
    public Task Test172_POST_BASE_URL_private_configurations_payment_Status_200() => AssertShellAsync(
        Method("POST"),
        "/private/configurations/payment",
        null,
        200,
        requiredField: null);

    // Source assertion 173: Extension point EXT-MODULE-METADATA-028 - metadata round-trip
    // @BR-ID: BR-MER-013
    [Fact(DisplayName = "173: Extension point EXT-MODULE-METADATA-028 - metadata round-trip")]
    [Trait("BR", "BR-MER-013")]
    public Task Test173_POST_BASE_URL_private_configurations_payment_Status_200() => AssertShellAsync(
        Method("POST"),
        "/private/configurations/payment",
        null,
        200,
        requiredField: null);

    // Source assertion 174: Extension point EXT-CMS-DELETE-030 - configured behavior
    // @BR-ID: BR-MER-013
    [Fact(DisplayName = "174: Extension point EXT-CMS-DELETE-030 - configured behavior")]
    [Trait("BR", "BR-MER-013")]
    public Task Test174_POST_BASE_URL_private_configurations_payment_Status_200() => AssertShellAsync(
        Method("POST"),
        "/private/configurations/payment",
        null,
        200,
        requiredField: null);

    // Source assertion 175: Extension point EXT-CMS-DELETE-030 - default when unconfigured
    // @BR-ID: BR-MER-013
    [Fact(DisplayName = "175: Extension point EXT-CMS-DELETE-030 - default when unconfigured")]
    [Trait("BR", "BR-MER-013")]
    public Task Test175_POST_BASE_URL_private_configurations_payment_Status_200() => AssertShellAsync(
        Method("POST"),
        "/private/configurations/payment",
        null,
        200,
        requiredField: null);

    // Source assertion 176: Extension point EXT-CMS-DELETE-030 - metadata round-trip
    // @BR-ID: BR-MER-013
    [Fact(DisplayName = "176: Extension point EXT-CMS-DELETE-030 - metadata round-trip")]
    [Trait("BR", "BR-MER-013")]
    public Task Test176_POST_BASE_URL_private_configurations_payment_Status_200() => AssertShellAsync(
        Method("POST"),
        "/private/configurations/payment",
        null,
        200,
        requiredField: null);
}
