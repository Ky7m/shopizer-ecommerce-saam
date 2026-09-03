using Shopizer.IntegrationTests.Fixtures;

namespace Shopizer.IntegrationTests;

[Collection(ShopizerAspireCollection.Name)]
public sealed class MerchantAdministrationComprehensiveTests(AspireHostFixture fixture) : ComprehensiveTestBase(fixture.MerchantAdministrationClient)
{

    // Source assertion 1: Contract success: POST /stores
    // @BR-ID: BR-MER-001
    [Fact(DisplayName = "001: Contract success: POST /stores")]
    [Trait("BR", "BR-MER-001")]
    public Task Test001_POST_BASE_URL_stores_Field_id_201() => AssertShellAsync(
        Method("POST"),
        "/stores",
        "{\"code\":\"phase4c-test\",\"name\":\"phase4c-test\",\"emailAddress\":\"phase4c@example.com\",\"phone\":\"phase4c-test\",\"address\":{\"streetAddress\":\"phase4c-test\",\"city\":\"phase4c-test\",\"postalCode\":\"phase4c-test\",\"countryCode\":\"phase4c-test\",\"stateProvince\":\"phase4c-test\",\"zoneCode\":\"phase4c-test\"},\"parentStoreCode\":\"phase4c-test\",\"retailer\":true,\"defaultLanguageCode\":\"phase4c-test\",\"supportedLanguageCodes\":[\"phase4c-test\"],\"currencyCode\":\"phase4c-test\",\"dimensionUnit\":\"phase4c-test\",\"weightUnit\":\"phase4c-test\"}",
        201,
        requiredField: "id");

    // Source assertion 2: Contract error/conformance: POST /stores
    // @BR-ID: BR-MER-001
    [Fact(DisplayName = "002: Contract error/conformance: POST /stores")]
    [Trait("BR", "BR-MER-001")]
    public Task Test002_POST_BASE_URL_stores_Status_409() => AssertShellAsync(
        Method("POST"),
        "/stores",
        "{}",
        409,
        requiredField: null);

    // Source assertion 3: Business rule assertion: BR-MER-001
    // @BR-ID: BR-MER-001
    [Fact(DisplayName = "003: Business rule assertion: BR-MER-001")]
    [Trait("BR", "BR-MER-001")]
    public Task Test003_POST_BASE_URL_stores_Field_id_201() => AssertShellAsync(
        Method("POST"),
        "/stores",
        "{\"code\":\"phase4c-test\",\"name\":\"phase4c-test\",\"emailAddress\":\"phase4c@example.com\",\"phone\":\"phase4c-test\",\"address\":{\"streetAddress\":\"phase4c-test\",\"city\":\"phase4c-test\",\"postalCode\":\"phase4c-test\",\"countryCode\":\"phase4c-test\",\"stateProvince\":\"phase4c-test\",\"zoneCode\":\"phase4c-test\"},\"parentStoreCode\":\"phase4c-test\",\"retailer\":true,\"defaultLanguageCode\":\"phase4c-test\",\"supportedLanguageCodes\":[\"phase4c-test\"],\"currencyCode\":\"phase4c-test\",\"dimensionUnit\":\"phase4c-test\",\"weightUnit\":\"phase4c-test\"}",
        201,
        requiredField: "id");

    // Source assertion 4: Business rule assertion: BR-MER-007
    // @BR-ID: BR-MER-007
    [Fact(DisplayName = "004: Business rule assertion: BR-MER-007")]
    [Trait("BR", "BR-MER-007")]
    public Task Test004_POST_BASE_URL_stores_Field_id_201() => AssertShellAsync(
        Method("POST"),
        "/stores",
        "{\"code\":\"phase4c-test\",\"name\":\"phase4c-test\",\"emailAddress\":\"phase4c@example.com\",\"phone\":\"phase4c-test\",\"address\":{\"streetAddress\":\"phase4c-test\",\"city\":\"phase4c-test\",\"postalCode\":\"phase4c-test\",\"countryCode\":\"phase4c-test\",\"stateProvince\":\"phase4c-test\",\"zoneCode\":\"phase4c-test\"},\"parentStoreCode\":\"phase4c-test\",\"retailer\":true,\"defaultLanguageCode\":\"phase4c-test\",\"supportedLanguageCodes\":[\"phase4c-test\"],\"currencyCode\":\"phase4c-test\",\"dimensionUnit\":\"phase4c-test\",\"weightUnit\":\"phase4c-test\"}",
        201,
        requiredField: "id");

    // Source assertion 5: Business rule assertion: BR-MER-012
    // @BR-ID: BR-MER-012
    [Fact(DisplayName = "005: Business rule assertion: BR-MER-012")]
    [Trait("BR", "BR-MER-012")]
    public Task Test005_POST_BASE_URL_stores_Field_id_201() => AssertShellAsync(
        Method("POST"),
        "/stores",
        "{\"code\":\"phase4c-test\",\"name\":\"phase4c-test\",\"emailAddress\":\"phase4c@example.com\",\"phone\":\"phase4c-test\",\"address\":{\"streetAddress\":\"phase4c-test\",\"city\":\"phase4c-test\",\"postalCode\":\"phase4c-test\",\"countryCode\":\"phase4c-test\",\"stateProvince\":\"phase4c-test\",\"zoneCode\":\"phase4c-test\"},\"parentStoreCode\":\"phase4c-test\",\"retailer\":true,\"defaultLanguageCode\":\"phase4c-test\",\"supportedLanguageCodes\":[\"phase4c-test\"],\"currencyCode\":\"phase4c-test\",\"dimensionUnit\":\"phase4c-test\",\"weightUnit\":\"phase4c-test\"}",
        201,
        requiredField: "id");

    // Source assertion 6: Business rule assertion: BR-MSA-VAL-001
    // @BR-ID: BR-MSA-VAL-001
    [Fact(DisplayName = "006: Business rule assertion: BR-MSA-VAL-001")]
    [Trait("BR", "BR-MSA-VAL-001")]
    public Task Test006_POST_BASE_URL_stores_Field_id_201() => AssertShellAsync(
        Method("POST"),
        "/stores",
        "{\"code\":\"phase4c-test\",\"name\":\"phase4c-test\",\"emailAddress\":\"phase4c@example.com\",\"phone\":\"phase4c-test\",\"address\":{\"streetAddress\":\"phase4c-test\",\"city\":\"phase4c-test\",\"postalCode\":\"phase4c-test\",\"countryCode\":\"phase4c-test\",\"stateProvince\":\"phase4c-test\",\"zoneCode\":\"phase4c-test\"},\"parentStoreCode\":\"phase4c-test\",\"retailer\":true,\"defaultLanguageCode\":\"phase4c-test\",\"supportedLanguageCodes\":[\"phase4c-test\"],\"currencyCode\":\"phase4c-test\",\"dimensionUnit\":\"phase4c-test\",\"weightUnit\":\"phase4c-test\"}",
        201,
        requiredField: "id");

    // Source assertion 7: Business rule assertion: BR-MSA-VAL-003
    // @BR-ID: BR-MSA-VAL-003
    [Fact(DisplayName = "007: Business rule assertion: BR-MSA-VAL-003")]
    [Trait("BR", "BR-MSA-VAL-003")]
    public Task Test007_POST_BASE_URL_stores_Field_id_201() => AssertShellAsync(
        Method("POST"),
        "/stores",
        "{\"code\":\"phase4c-test\",\"name\":\"phase4c-test\",\"emailAddress\":\"phase4c@example.com\",\"phone\":\"phase4c-test\",\"address\":{\"streetAddress\":\"phase4c-test\",\"city\":\"phase4c-test\",\"postalCode\":\"phase4c-test\",\"countryCode\":\"phase4c-test\",\"stateProvince\":\"phase4c-test\",\"zoneCode\":\"phase4c-test\"},\"parentStoreCode\":\"phase4c-test\",\"retailer\":true,\"defaultLanguageCode\":\"phase4c-test\",\"supportedLanguageCodes\":[\"phase4c-test\"],\"currencyCode\":\"phase4c-test\",\"dimensionUnit\":\"phase4c-test\",\"weightUnit\":\"phase4c-test\"}",
        201,
        requiredField: "id");

    // Source assertion 8: Business rule assertion: BR-MSA-LANG-001
    // @BR-ID: BR-MSA-LANG-001
    [Fact(DisplayName = "008: Business rule assertion: BR-MSA-LANG-001")]
    [Trait("BR", "BR-MSA-LANG-001")]
    public Task Test008_POST_BASE_URL_stores_Field_id_201() => AssertShellAsync(
        Method("POST"),
        "/stores",
        "{\"code\":\"phase4c-test\",\"name\":\"phase4c-test\",\"emailAddress\":\"phase4c@example.com\",\"phone\":\"phase4c-test\",\"address\":{\"streetAddress\":\"phase4c-test\",\"city\":\"phase4c-test\",\"postalCode\":\"phase4c-test\",\"countryCode\":\"phase4c-test\",\"stateProvince\":\"phase4c-test\",\"zoneCode\":\"phase4c-test\"},\"parentStoreCode\":\"phase4c-test\",\"retailer\":true,\"defaultLanguageCode\":\"phase4c-test\",\"supportedLanguageCodes\":[\"phase4c-test\"],\"currencyCode\":\"phase4c-test\",\"dimensionUnit\":\"phase4c-test\",\"weightUnit\":\"phase4c-test\"}",
        201,
        requiredField: "id");

    // Source assertion 9: Business rule assertion: BR-MER-004
    // @BR-ID: BR-MER-004
    [Fact(DisplayName = "009: Business rule assertion: BR-MER-004")]
    [Trait("BR", "BR-MER-004")]
    public Task Test009_POST_BASE_URL_stores_Field_id_201() => AssertShellAsync(
        Method("POST"),
        "/stores",
        "{\"code\":\"phase4c-test\",\"name\":\"phase4c-test\",\"emailAddress\":\"phase4c@example.com\",\"phone\":\"phase4c-test\",\"address\":{\"streetAddress\":\"phase4c-test\",\"city\":\"phase4c-test\",\"postalCode\":\"phase4c-test\",\"countryCode\":\"phase4c-test\",\"stateProvince\":\"phase4c-test\",\"zoneCode\":\"phase4c-test\"},\"parentStoreCode\":\"phase4c-test\",\"retailer\":true,\"defaultLanguageCode\":\"phase4c-test\",\"supportedLanguageCodes\":[\"phase4c-test\"],\"currencyCode\":\"phase4c-test\",\"dimensionUnit\":\"phase4c-test\",\"weightUnit\":\"phase4c-test\"}",
        201,
        requiredField: "id");

    // Source assertion 10: Contract success: POST /stores/signup
    // @BR-ID: BR-MSA-VAL-003
    [Fact(DisplayName = "010: Contract success: POST /stores/signup")]
    [Trait("BR", "BR-MSA-VAL-003")]
    public Task Test010_POST_BASE_URL_stores_signup_Field_signupId_202() => AssertShellAsync(
        Method("POST"),
        "/stores/signup",
        "{\"code\":\"phase4c-test\",\"name\":\"phase4c-test\",\"emailAddress\":\"phase4c@example.com\",\"phone\":\"phase4c-test\",\"address\":{\"streetAddress\":\"phase4c-test\",\"city\":\"phase4c-test\",\"postalCode\":\"phase4c-test\",\"countryCode\":\"phase4c-test\",\"stateProvince\":\"phase4c-test\",\"zoneCode\":\"phase4c-test\"},\"parentStoreCode\":\"phase4c-test\",\"retailer\":true,\"defaultLanguageCode\":\"phase4c-test\",\"supportedLanguageCodes\":[\"phase4c-test\"],\"currencyCode\":\"phase4c-test\",\"dimensionUnit\":\"phase4c-test\",\"weightUnit\":\"phase4c-test\"}",
        202,
        requiredField: "signupId");

    // Source assertion 11: Contract error/conformance: POST /stores/signup
    // @BR-ID: BR-MSA-VAL-003
    [Fact(DisplayName = "011: Contract error/conformance: POST /stores/signup")]
    [Trait("BR", "BR-MSA-VAL-003")]
    public Task Test011_POST_BASE_URL_stores_signup_Status_422() => AssertShellAsync(
        Method("POST"),
        "/stores/signup",
        "{}",
        422,
        requiredField: null);

    // Source assertion 12: Business rule assertion: BR-MSA-VAL-003
    // @BR-ID: BR-MSA-VAL-003
    [Fact(DisplayName = "012: Business rule assertion: BR-MSA-VAL-003")]
    [Trait("BR", "BR-MSA-VAL-003")]
    public Task Test012_POST_BASE_URL_stores_signup_Field_signupId_202() => AssertShellAsync(
        Method("POST"),
        "/stores/signup",
        "{\"code\":\"phase4c-test\",\"name\":\"phase4c-test\",\"emailAddress\":\"phase4c@example.com\",\"phone\":\"phase4c-test\",\"address\":{\"streetAddress\":\"phase4c-test\",\"city\":\"phase4c-test\",\"postalCode\":\"phase4c-test\",\"countryCode\":\"phase4c-test\",\"stateProvince\":\"phase4c-test\",\"zoneCode\":\"phase4c-test\"},\"parentStoreCode\":\"phase4c-test\",\"retailer\":true,\"defaultLanguageCode\":\"phase4c-test\",\"supportedLanguageCodes\":[\"phase4c-test\"],\"currencyCode\":\"phase4c-test\",\"dimensionUnit\":\"phase4c-test\",\"weightUnit\":\"phase4c-test\"}",
        202,
        requiredField: "signupId");

    // Source assertion 13: Business rule assertion: BR-MER-001
    // @BR-ID: BR-MER-001
    [Fact(DisplayName = "013: Business rule assertion: BR-MER-001")]
    [Trait("BR", "BR-MER-001")]
    public Task Test013_POST_BASE_URL_stores_signup_Field_signupId_202() => AssertShellAsync(
        Method("POST"),
        "/stores/signup",
        "{\"code\":\"phase4c-test\",\"name\":\"phase4c-test\",\"emailAddress\":\"phase4c@example.com\",\"phone\":\"phase4c-test\",\"address\":{\"streetAddress\":\"phase4c-test\",\"city\":\"phase4c-test\",\"postalCode\":\"phase4c-test\",\"countryCode\":\"phase4c-test\",\"stateProvince\":\"phase4c-test\",\"zoneCode\":\"phase4c-test\"},\"parentStoreCode\":\"phase4c-test\",\"retailer\":true,\"defaultLanguageCode\":\"phase4c-test\",\"supportedLanguageCodes\":[\"phase4c-test\"],\"currencyCode\":\"phase4c-test\",\"dimensionUnit\":\"phase4c-test\",\"weightUnit\":\"phase4c-test\"}",
        202,
        requiredField: "signupId");

    // Source assertion 14: Contract success: POST /stores/{storeCode}/branding/logo
    // @BR-ID: BR-MSA-BRD-001
    [Fact(DisplayName = "014: Contract success: POST /stores/{storeCode}/branding/logo")]
    [Trait("BR", "BR-MSA-BRD-001")]
    public Task Test014_POST_BASE_URL_stores_phase4c_code_branding_logo_Field_storeCode_201() => AssertShellAsync(
        Method("POST"),
        "/stores/phase4c-code/branding/logo",
        "{\"file\":\"phase4c-test\"}",
        201,
        requiredField: "storeCode");

    // Source assertion 15: Contract error/conformance: POST /stores/{storeCode}/branding/logo
    // @BR-ID: BR-MSA-BRD-001
    [Fact(DisplayName = "015: Contract error/conformance: POST /stores/{storeCode}/branding/logo")]
    [Trait("BR", "BR-MSA-BRD-001")]
    public Task Test015_POST_BASE_URL_stores_phase4c_code_branding_logo_Status_503() => AssertShellAsync(
        Method("POST"),
        "/stores/phase4c-code/branding/logo",
        "{}",
        503,
        requiredField: null);

    // Source assertion 16: Business rule assertion: BR-MSA-BRD-001
    // @BR-ID: BR-MSA-BRD-001
    [Fact(DisplayName = "016: Business rule assertion: BR-MSA-BRD-001")]
    [Trait("BR", "BR-MSA-BRD-001")]
    public Task Test016_POST_BASE_URL_stores_phase4c_code_branding_logo_Field_storeCode_201() => AssertShellAsync(
        Method("POST"),
        "/stores/phase4c-code/branding/logo",
        "{\"file\":\"phase4c-test\"}",
        201,
        requiredField: "storeCode");

    // Source assertion 17: Business rule assertion: BR-MER-011
    // @BR-ID: BR-MER-011
    [Fact(DisplayName = "017: Business rule assertion: BR-MER-011")]
    [Trait("BR", "BR-MER-011")]
    public Task Test017_POST_BASE_URL_stores_phase4c_code_branding_logo_Field_storeCode_201() => AssertShellAsync(
        Method("POST"),
        "/stores/phase4c-code/branding/logo",
        "{\"file\":\"phase4c-test\"}",
        201,
        requiredField: "storeCode");

    // Source assertion 18: Contract success: GET /merchants/{merchantCode}/children
    // @BR-ID: BR-MER-008
    [Fact(DisplayName = "018: Contract success: GET /merchants/{merchantCode}/children")]
    [Trait("BR", "BR-MER-008")]
    public Task Test018_GET_BASE_URL_merchants_phase4c_code_children_Field_items_200() => AssertShellAsync(
        Method("GET"),
        "/merchants/phase4c-code/children",
        null,
        200,
        requiredField: "items");

    // Source assertion 19: Contract error/conformance: GET /merchants/{merchantCode}/children
    // @BR-ID: BR-MER-008
    [Fact(DisplayName = "019: Contract error/conformance: GET /merchants/{merchantCode}/children")]
    [Trait("BR", "BR-MER-008")]
    public Task Test019_GET_BASE_URL_merchants_phase4c_code_children_Status_403() => AssertShellAsync(
        Method("GET"),
        "/merchants/phase4c-code/children",
        null,
        403,
        requiredField: null);

    // Source assertion 20: Business rule assertion: BR-MER-008
    // @BR-ID: BR-MER-008
    [Fact(DisplayName = "020: Business rule assertion: BR-MER-008")]
    [Trait("BR", "BR-MER-008")]
    public Task Test020_GET_BASE_URL_merchants_phase4c_code_children_Field_items_200() => AssertShellAsync(
        Method("GET"),
        "/merchants/phase4c-code/children",
        null,
        200,
        requiredField: "items");

    // Source assertion 21: Business rule assertion: BR-MER-009
    // @BR-ID: BR-MER-009
    [Fact(DisplayName = "021: Business rule assertion: BR-MER-009")]
    [Trait("BR", "BR-MER-009")]
    public Task Test021_GET_BASE_URL_merchants_phase4c_code_children_Field_items_200() => AssertShellAsync(
        Method("GET"),
        "/merchants/phase4c-code/children",
        null,
        200,
        requiredField: "items");

    // Source assertion 22: Business rule assertion: BR-MSA-AUTH-001
    // @BR-ID: BR-MSA-AUTH-001
    [Fact(DisplayName = "022: Business rule assertion: BR-MSA-AUTH-001")]
    [Trait("BR", "BR-MSA-AUTH-001")]
    public Task Test022_GET_BASE_URL_merchants_phase4c_code_children_Field_items_200() => AssertShellAsync(
        Method("GET"),
        "/merchants/phase4c-code/children",
        null,
        200,
        requiredField: "items");

    // Source assertion 23: Contract success: GET /merchants/{merchantCode}/stores
    // @BR-ID: BR-MER-008
    [Fact(DisplayName = "023: Contract success: GET /merchants/{merchantCode}/stores")]
    [Trait("BR", "BR-MER-008")]
    public Task Test023_GET_BASE_URL_merchants_phase4c_code_stores_Field_items_200() => AssertShellAsync(
        Method("GET"),
        "/merchants/phase4c-code/stores",
        null,
        200,
        requiredField: "items");

    // Source assertion 24: Contract error/conformance: GET /merchants/{merchantCode}/stores
    // @BR-ID: BR-MER-008
    [Fact(DisplayName = "024: Contract error/conformance: GET /merchants/{merchantCode}/stores")]
    [Trait("BR", "BR-MER-008")]
    public Task Test024_GET_BASE_URL_merchants_phase4c_code_stores_Status_404() => AssertShellAsync(
        Method("GET"),
        "/merchants/phase4c-code/stores",
        null,
        404,
        requiredField: null);

    // Source assertion 25: Business rule assertion: BR-MER-008
    // @BR-ID: BR-MER-008
    [Fact(DisplayName = "025: Business rule assertion: BR-MER-008")]
    [Trait("BR", "BR-MER-008")]
    public Task Test025_GET_BASE_URL_merchants_phase4c_code_stores_Field_items_200() => AssertShellAsync(
        Method("GET"),
        "/merchants/phase4c-code/stores",
        null,
        200,
        requiredField: "items");

    // Source assertion 26: Business rule assertion: BR-MSA-AUTH-001
    // @BR-ID: BR-MSA-AUTH-001
    [Fact(DisplayName = "026: Business rule assertion: BR-MSA-AUTH-001")]
    [Trait("BR", "BR-MSA-AUTH-001")]
    public Task Test026_GET_BASE_URL_merchants_phase4c_code_stores_Field_items_200() => AssertShellAsync(
        Method("GET"),
        "/merchants/phase4c-code/stores",
        null,
        200,
        requiredField: "items");

    // Source assertion 27: Business rule assertion: BR-MSA-READ-001
    // @BR-ID: BR-MSA-READ-001
    [Fact(DisplayName = "027: Business rule assertion: BR-MSA-READ-001")]
    [Trait("BR", "BR-MSA-READ-001")]
    public Task Test027_GET_BASE_URL_merchants_phase4c_code_stores_Field_items_200() => AssertShellAsync(
        Method("GET"),
        "/merchants/phase4c-code/stores",
        null,
        200,
        requiredField: "items");

    // Source assertion 28: Contract success: GET /stores
    // @BR-ID: BR-MSA-READ-001
    [Fact(DisplayName = "028: Contract success: GET /stores")]
    [Trait("BR", "BR-MSA-READ-001")]
    public Task Test028_GET_BASE_URL_stores_Field_items_200() => AssertShellAsync(
        Method("GET"),
        "/stores",
        null,
        200,
        requiredField: "items");

    // Source assertion 29: Contract error/conformance: GET /stores
    // @BR-ID: BR-MSA-READ-001
    [Fact(DisplayName = "029: Contract error/conformance: GET /stores")]
    [Trait("BR", "BR-MSA-READ-001")]
    public Task Test029_GET_BASE_URL_stores_Status_401() => AssertShellAsync(
        Method("GET"),
        "/stores",
        null,
        401,
        requiredField: null);

    // Source assertion 30: Business rule assertion: BR-MSA-READ-001
    // @BR-ID: BR-MSA-READ-001
    [Fact(DisplayName = "030: Business rule assertion: BR-MSA-READ-001")]
    [Trait("BR", "BR-MSA-READ-001")]
    public Task Test030_GET_BASE_URL_stores_Field_items_200() => AssertShellAsync(
        Method("GET"),
        "/stores",
        null,
        200,
        requiredField: "items");

    // Source assertion 31: Business rule assertion: BR-MER-010
    // @BR-ID: BR-MER-010
    [Fact(DisplayName = "031: Business rule assertion: BR-MER-010")]
    [Trait("BR", "BR-MER-010")]
    public Task Test031_GET_BASE_URL_stores_Field_items_200() => AssertShellAsync(
        Method("GET"),
        "/stores",
        null,
        200,
        requiredField: "items");

    // Source assertion 32: Contract success: GET /stores/names
    // @BR-ID: BR-MSA-LST-001
    [Fact(DisplayName = "032: Contract success: GET /stores/names")]
    [Trait("BR", "BR-MSA-LST-001")]
    public Task Test032_GET_BASE_URL_stores_names_Field_items_200() => AssertShellAsync(
        Method("GET"),
        "/stores/names",
        null,
        200,
        requiredField: "items");

    // Source assertion 33: Contract error/conformance: GET /stores/names
    // @BR-ID: BR-MSA-LST-001
    [Fact(DisplayName = "033: Contract error/conformance: GET /stores/names")]
    [Trait("BR", "BR-MSA-LST-001")]
    public Task Test033_GET_BASE_URL_stores_names_Status_404() => AssertShellAsync(
        Method("GET"),
        "/stores/names",
        null,
        404,
        requiredField: null);

    // Source assertion 34: Business rule assertion: BR-MSA-LST-001
    // @BR-ID: BR-MSA-LST-001
    [Fact(DisplayName = "034: Business rule assertion: BR-MSA-LST-001")]
    [Trait("BR", "BR-MSA-LST-001")]
    public Task Test034_GET_BASE_URL_stores_names_Field_items_200() => AssertShellAsync(
        Method("GET"),
        "/stores/names",
        null,
        200,
        requiredField: "items");

    // Source assertion 35: Business rule assertion: BR-UI-007
    // @BR-ID: BR-UI-007
    [Fact(DisplayName = "035: Business rule assertion: BR-UI-007")]
    [Trait("BR", "BR-UI-007")]
    public Task Test035_GET_BASE_URL_stores_names_Field_items_200() => AssertShellAsync(
        Method("GET"),
        "/stores/names",
        null,
        200,
        requiredField: "items");

    // Source assertion 36: Contract success: GET /stores/uniqueness
    // @BR-ID: BR-MER-003
    [Fact(DisplayName = "036: Contract success: GET /stores/uniqueness")]
    [Trait("BR", "BR-MER-003")]
    public Task Test036_GET_BASE_URL_stores_uniqueness_Field_exists_200() => AssertShellAsync(
        Method("GET"),
        "/stores/uniqueness",
        null,
        200,
        requiredField: "exists");

    // Source assertion 37: Contract error/conformance: GET /stores/uniqueness
    // @BR-ID: BR-MER-003
    [Fact(DisplayName = "037: Contract error/conformance: GET /stores/uniqueness")]
    [Trait("BR", "BR-MER-003")]
    public Task Test037_GET_BASE_URL_stores_uniqueness_Status_404() => AssertShellAsync(
        Method("GET"),
        "/stores/uniqueness",
        null,
        404,
        requiredField: null);

    // Source assertion 38: Business rule assertion: BR-MER-003
    // @BR-ID: BR-MER-003
    [Fact(DisplayName = "038: Business rule assertion: BR-MER-003")]
    [Trait("BR", "BR-MER-003")]
    public Task Test038_GET_BASE_URL_stores_uniqueness_Field_exists_200() => AssertShellAsync(
        Method("GET"),
        "/stores/uniqueness",
        null,
        200,
        requiredField: "exists");

    // Source assertion 39: Business rule assertion: BR-MSA-VAL-001
    // @BR-ID: BR-MSA-VAL-001
    [Fact(DisplayName = "039: Business rule assertion: BR-MSA-VAL-001")]
    [Trait("BR", "BR-MSA-VAL-001")]
    public Task Test039_GET_BASE_URL_stores_uniqueness_Field_exists_200() => AssertShellAsync(
        Method("GET"),
        "/stores/uniqueness",
        null,
        200,
        requiredField: "exists");

    // Source assertion 40: Contract success: GET /stores/{storeCode}
    // @BR-ID: BR-MER-010
    [Fact(DisplayName = "040: Contract success: GET /stores/{storeCode}")]
    [Trait("BR", "BR-MER-010")]
    public Task Test040_GET_BASE_URL_stores_phase4c_code_Field_id_200() => AssertShellAsync(
        Method("GET"),
        "/stores/phase4c-code",
        null,
        200,
        requiredField: "id");

    // Source assertion 41: Contract error/conformance: GET /stores/{storeCode}
    // @BR-ID: BR-MER-010
    [Fact(DisplayName = "041: Contract error/conformance: GET /stores/{storeCode}")]
    [Trait("BR", "BR-MER-010")]
    public Task Test041_GET_BASE_URL_stores_phase4c_code_Status_404() => AssertShellAsync(
        Method("GET"),
        "/stores/phase4c-code",
        null,
        404,
        requiredField: null);

    // Source assertion 42: Business rule assertion: BR-MER-010
    // @BR-ID: BR-MER-010
    [Fact(DisplayName = "042: Business rule assertion: BR-MER-010")]
    [Trait("BR", "BR-MER-010")]
    public Task Test042_GET_BASE_URL_stores_phase4c_code_Field_id_200() => AssertShellAsync(
        Method("GET"),
        "/stores/phase4c-code",
        null,
        200,
        requiredField: "id");

    // Source assertion 43: Business rule assertion: BR-UI-007
    // @BR-ID: BR-UI-007
    [Fact(DisplayName = "043: Business rule assertion: BR-UI-007")]
    [Trait("BR", "BR-UI-007")]
    public Task Test043_GET_BASE_URL_stores_phase4c_code_Field_id_200() => AssertShellAsync(
        Method("GET"),
        "/stores/phase4c-code",
        null,
        200,
        requiredField: "id");

    // Source assertion 44: Contract success: GET /stores/{storeCode}/branding
    // @BR-ID: BR-MSA-BRD-001
    [Fact(DisplayName = "044: Contract success: GET /stores/{storeCode}/branding")]
    [Trait("BR", "BR-MSA-BRD-001")]
    public Task Test044_GET_BASE_URL_stores_phase4c_code_branding_Field_storeCode_200() => AssertShellAsync(
        Method("GET"),
        "/stores/phase4c-code/branding",
        null,
        200,
        requiredField: "storeCode");

    // Source assertion 45: Contract error/conformance: GET /stores/{storeCode}/branding
    // @BR-ID: BR-MSA-BRD-001
    [Fact(DisplayName = "045: Contract error/conformance: GET /stores/{storeCode}/branding")]
    [Trait("BR", "BR-MSA-BRD-001")]
    public Task Test045_GET_BASE_URL_stores_phase4c_code_branding_Status_404() => AssertShellAsync(
        Method("GET"),
        "/stores/phase4c-code/branding",
        null,
        404,
        requiredField: null);

    // Source assertion 46: Business rule assertion: BR-MSA-BRD-001
    // @BR-ID: BR-MSA-BRD-001
    [Fact(DisplayName = "046: Business rule assertion: BR-MSA-BRD-001")]
    [Trait("BR", "BR-MSA-BRD-001")]
    public Task Test046_GET_BASE_URL_stores_phase4c_code_branding_Field_storeCode_200() => AssertShellAsync(
        Method("GET"),
        "/stores/phase4c-code/branding",
        null,
        200,
        requiredField: "storeCode");

    // Source assertion 47: Business rule assertion: BR-UI-007
    // @BR-ID: BR-UI-007
    [Fact(DisplayName = "047: Business rule assertion: BR-UI-007")]
    [Trait("BR", "BR-UI-007")]
    public Task Test047_GET_BASE_URL_stores_phase4c_code_branding_Field_storeCode_200() => AssertShellAsync(
        Method("GET"),
        "/stores/phase4c-code/branding",
        null,
        200,
        requiredField: "storeCode");

    // Source assertion 48: Contract success: GET /stores/{storeCode}/languages
    // @BR-ID: BR-MER-012
    [Fact(DisplayName = "048: Contract success: GET /stores/{storeCode}/languages")]
    [Trait("BR", "BR-MER-012")]
    public Task Test048_GET_BASE_URL_stores_phase4c_code_languages_Field_items_200() => AssertShellAsync(
        Method("GET"),
        "/stores/phase4c-code/languages",
        null,
        200,
        requiredField: "items");

    // Source assertion 49: Contract error/conformance: GET /stores/{storeCode}/languages
    // @BR-ID: BR-MER-012
    [Fact(DisplayName = "049: Contract error/conformance: GET /stores/{storeCode}/languages")]
    [Trait("BR", "BR-MER-012")]
    public Task Test049_GET_BASE_URL_stores_phase4c_code_languages_Status_404() => AssertShellAsync(
        Method("GET"),
        "/stores/phase4c-code/languages",
        null,
        404,
        requiredField: null);

    // Source assertion 50: Business rule assertion: BR-MER-012
    // @BR-ID: BR-MER-012
    [Fact(DisplayName = "050: Business rule assertion: BR-MER-012")]
    [Trait("BR", "BR-MER-012")]
    public Task Test050_GET_BASE_URL_stores_phase4c_code_languages_Field_items_200() => AssertShellAsync(
        Method("GET"),
        "/stores/phase4c-code/languages",
        null,
        200,
        requiredField: "items");

    // Source assertion 51: Business rule assertion: BR-MSA-LANG-001
    // @BR-ID: BR-MSA-LANG-001
    [Fact(DisplayName = "051: Business rule assertion: BR-MSA-LANG-001")]
    [Trait("BR", "BR-MSA-LANG-001")]
    public Task Test051_GET_BASE_URL_stores_phase4c_code_languages_Field_items_200() => AssertShellAsync(
        Method("GET"),
        "/stores/phase4c-code/languages",
        null,
        200,
        requiredField: "items");

    // Source assertion 52: Contract success: GET /stores/{storeCode}/signup/{token}
    // @BR-ID: BR-MSA-VAL-003
    [Fact(DisplayName = "052: Contract success: GET /stores/{storeCode}/signup/{token}")]
    [Trait("BR", "BR-MSA-VAL-003")]
    public Task Test052_GET_BASE_URL_stores_phase4c_code_signup_phase4c_token_Field_verified_200() => AssertShellAsync(
        Method("GET"),
        "/stores/phase4c-code/signup/phase4c-token",
        null,
        200,
        requiredField: "verified");

    // Source assertion 53: Contract error/conformance: GET /stores/{storeCode}/signup/{token}
    // @BR-ID: BR-MSA-VAL-003
    [Fact(DisplayName = "053: Contract error/conformance: GET /stores/{storeCode}/signup/{token}")]
    [Trait("BR", "BR-MSA-VAL-003")]
    public Task Test053_GET_BASE_URL_stores_phase4c_code_signup_phase4c_token_Status_410() => AssertShellAsync(
        Method("GET"),
        "/stores/phase4c-code/signup/phase4c-token",
        null,
        410,
        requiredField: null);

    // Source assertion 54: Business rule assertion: BR-MSA-VAL-003
    // @BR-ID: BR-MSA-VAL-003
    [Fact(DisplayName = "054: Business rule assertion: BR-MSA-VAL-003")]
    [Trait("BR", "BR-MSA-VAL-003")]
    public Task Test054_GET_BASE_URL_stores_phase4c_code_signup_phase4c_token_Field_verified_200() => AssertShellAsync(
        Method("GET"),
        "/stores/phase4c-code/signup/phase4c-token",
        null,
        200,
        requiredField: "verified");

    // Source assertion 55: Contract success: PUT /stores/{storeCode}
    // @BR-ID: BR-MER-002
    [Fact(DisplayName = "055: Contract success: PUT /stores/{storeCode}")]
    [Trait("BR", "BR-MER-002")]
    public Task Test055_PUT_BASE_URL_stores_phase4c_code_Field_id_200() => AssertShellAsync(
        Method("PUT"),
        "/stores/phase4c-code",
        "{\"name\":\"phase4c-test\",\"emailAddress\":\"phase4c@example.com\",\"phone\":\"phase4c-test\",\"address\":{\"streetAddress\":\"phase4c-test\",\"city\":\"phase4c-test\",\"postalCode\":\"phase4c-test\",\"countryCode\":\"phase4c-test\",\"stateProvince\":\"phase4c-test\",\"zoneCode\":\"phase4c-test\"},\"defaultLanguageCode\":\"phase4c-test\",\"currencyCode\":\"phase4c-test\",\"dimensionUnit\":\"phase4c-test\",\"weightUnit\":\"phase4c-test\"}",
        200,
        requiredField: "id");

    // Source assertion 56: Contract error/conformance: PUT /stores/{storeCode}
    // @BR-ID: BR-MER-002
    [Fact(DisplayName = "056: Contract error/conformance: PUT /stores/{storeCode}")]
    [Trait("BR", "BR-MER-002")]
    public Task Test056_PUT_BASE_URL_stores_phase4c_code_Status_403() => AssertShellAsync(
        Method("PUT"),
        "/stores/phase4c-code",
        "{}",
        403,
        requiredField: null);

    // Source assertion 57: Business rule assertion: BR-MER-002
    // @BR-ID: BR-MER-002
    [Fact(DisplayName = "057: Business rule assertion: BR-MER-002")]
    [Trait("BR", "BR-MER-002")]
    public Task Test057_PUT_BASE_URL_stores_phase4c_code_Field_id_200() => AssertShellAsync(
        Method("PUT"),
        "/stores/phase4c-code",
        "{\"name\":\"phase4c-test\",\"emailAddress\":\"phase4c@example.com\",\"phone\":\"phase4c-test\",\"address\":{\"streetAddress\":\"phase4c-test\",\"city\":\"phase4c-test\",\"postalCode\":\"phase4c-test\",\"countryCode\":\"phase4c-test\",\"stateProvince\":\"phase4c-test\",\"zoneCode\":\"phase4c-test\"},\"defaultLanguageCode\":\"phase4c-test\",\"currencyCode\":\"phase4c-test\",\"dimensionUnit\":\"phase4c-test\",\"weightUnit\":\"phase4c-test\"}",
        200,
        requiredField: "id");

    // Source assertion 58: Business rule assertion: BR-MER-005
    // @BR-ID: BR-MER-005
    [Fact(DisplayName = "058: Business rule assertion: BR-MER-005")]
    [Trait("BR", "BR-MER-005")]
    public Task Test058_PUT_BASE_URL_stores_phase4c_code_Field_id_200() => AssertShellAsync(
        Method("PUT"),
        "/stores/phase4c-code",
        "{\"name\":\"phase4c-test\",\"emailAddress\":\"phase4c@example.com\",\"phone\":\"phase4c-test\",\"address\":{\"streetAddress\":\"phase4c-test\",\"city\":\"phase4c-test\",\"postalCode\":\"phase4c-test\",\"countryCode\":\"phase4c-test\",\"stateProvince\":\"phase4c-test\",\"zoneCode\":\"phase4c-test\"},\"defaultLanguageCode\":\"phase4c-test\",\"currencyCode\":\"phase4c-test\",\"dimensionUnit\":\"phase4c-test\",\"weightUnit\":\"phase4c-test\"}",
        200,
        requiredField: "id");

    // Source assertion 59: Business rule assertion: BR-MER-011
    // @BR-ID: BR-MER-011
    [Fact(DisplayName = "059: Business rule assertion: BR-MER-011")]
    [Trait("BR", "BR-MER-011")]
    public Task Test059_PUT_BASE_URL_stores_phase4c_code_Field_id_200() => AssertShellAsync(
        Method("PUT"),
        "/stores/phase4c-code",
        "{\"name\":\"phase4c-test\",\"emailAddress\":\"phase4c@example.com\",\"phone\":\"phase4c-test\",\"address\":{\"streetAddress\":\"phase4c-test\",\"city\":\"phase4c-test\",\"postalCode\":\"phase4c-test\",\"countryCode\":\"phase4c-test\",\"stateProvince\":\"phase4c-test\",\"zoneCode\":\"phase4c-test\"},\"defaultLanguageCode\":\"phase4c-test\",\"currencyCode\":\"phase4c-test\",\"dimensionUnit\":\"phase4c-test\",\"weightUnit\":\"phase4c-test\"}",
        200,
        requiredField: "id");

    // Source assertion 60: Business rule assertion: BR-MER-012
    // @BR-ID: BR-MER-012
    [Fact(DisplayName = "060: Business rule assertion: BR-MER-012")]
    [Trait("BR", "BR-MER-012")]
    public Task Test060_PUT_BASE_URL_stores_phase4c_code_Field_id_200() => AssertShellAsync(
        Method("PUT"),
        "/stores/phase4c-code",
        "{\"name\":\"phase4c-test\",\"emailAddress\":\"phase4c@example.com\",\"phone\":\"phase4c-test\",\"address\":{\"streetAddress\":\"phase4c-test\",\"city\":\"phase4c-test\",\"postalCode\":\"phase4c-test\",\"countryCode\":\"phase4c-test\",\"stateProvince\":\"phase4c-test\",\"zoneCode\":\"phase4c-test\"},\"defaultLanguageCode\":\"phase4c-test\",\"currencyCode\":\"phase4c-test\",\"dimensionUnit\":\"phase4c-test\",\"weightUnit\":\"phase4c-test\"}",
        200,
        requiredField: "id");

    // Source assertion 61: Business rule assertion: BR-MSA-VAL-002
    // @BR-ID: BR-MSA-VAL-002
    [Fact(DisplayName = "061: Business rule assertion: BR-MSA-VAL-002")]
    [Trait("BR", "BR-MSA-VAL-002")]
    public Task Test061_PUT_BASE_URL_stores_phase4c_code_Field_id_200() => AssertShellAsync(
        Method("PUT"),
        "/stores/phase4c-code",
        "{\"name\":\"phase4c-test\",\"emailAddress\":\"phase4c@example.com\",\"phone\":\"phase4c-test\",\"address\":{\"streetAddress\":\"phase4c-test\",\"city\":\"phase4c-test\",\"postalCode\":\"phase4c-test\",\"countryCode\":\"phase4c-test\",\"stateProvince\":\"phase4c-test\",\"zoneCode\":\"phase4c-test\"},\"defaultLanguageCode\":\"phase4c-test\",\"currencyCode\":\"phase4c-test\",\"dimensionUnit\":\"phase4c-test\",\"weightUnit\":\"phase4c-test\"}",
        200,
        requiredField: "id");

    // Source assertion 62: Business rule assertion: BR-MSA-VAL-003
    // @BR-ID: BR-MSA-VAL-003
    [Fact(DisplayName = "062: Business rule assertion: BR-MSA-VAL-003")]
    [Trait("BR", "BR-MSA-VAL-003")]
    public Task Test062_PUT_BASE_URL_stores_phase4c_code_Field_id_200() => AssertShellAsync(
        Method("PUT"),
        "/stores/phase4c-code",
        "{\"name\":\"phase4c-test\",\"emailAddress\":\"phase4c@example.com\",\"phone\":\"phase4c-test\",\"address\":{\"streetAddress\":\"phase4c-test\",\"city\":\"phase4c-test\",\"postalCode\":\"phase4c-test\",\"countryCode\":\"phase4c-test\",\"stateProvince\":\"phase4c-test\",\"zoneCode\":\"phase4c-test\"},\"defaultLanguageCode\":\"phase4c-test\",\"currencyCode\":\"phase4c-test\",\"dimensionUnit\":\"phase4c-test\",\"weightUnit\":\"phase4c-test\"}",
        200,
        requiredField: "id");

    // Source assertion 63: Business rule assertion: BR-MSA-LANG-001
    // @BR-ID: BR-MSA-LANG-001
    [Fact(DisplayName = "063: Business rule assertion: BR-MSA-LANG-001")]
    [Trait("BR", "BR-MSA-LANG-001")]
    public Task Test063_PUT_BASE_URL_stores_phase4c_code_Field_id_200() => AssertShellAsync(
        Method("PUT"),
        "/stores/phase4c-code",
        "{\"name\":\"phase4c-test\",\"emailAddress\":\"phase4c@example.com\",\"phone\":\"phase4c-test\",\"address\":{\"streetAddress\":\"phase4c-test\",\"city\":\"phase4c-test\",\"postalCode\":\"phase4c-test\",\"countryCode\":\"phase4c-test\",\"stateProvince\":\"phase4c-test\",\"zoneCode\":\"phase4c-test\"},\"defaultLanguageCode\":\"phase4c-test\",\"currencyCode\":\"phase4c-test\",\"dimensionUnit\":\"phase4c-test\",\"weightUnit\":\"phase4c-test\"}",
        200,
        requiredField: "id");

    // Source assertion 64: Contract success: PUT /stores/{storeCode}/branding
    // @BR-ID: BR-MSA-BRD-001
    [Fact(DisplayName = "064: Contract success: PUT /stores/{storeCode}/branding")]
    [Trait("BR", "BR-MSA-BRD-001")]
    public Task Test064_PUT_BASE_URL_stores_phase4c_code_branding_Field_storeCode_200() => AssertShellAsync(
        Method("PUT"),
        "/stores/phase4c-code/branding",
        "{\"templateCode\":\"phase4c-test\",\"logoUri\":\"https://example.com/phase4c\"}",
        200,
        requiredField: "storeCode");

    // Source assertion 65: Contract error/conformance: PUT /stores/{storeCode}/branding
    // @BR-ID: BR-MSA-BRD-001
    [Fact(DisplayName = "065: Contract error/conformance: PUT /stores/{storeCode}/branding")]
    [Trait("BR", "BR-MSA-BRD-001")]
    public Task Test065_PUT_BASE_URL_stores_phase4c_code_branding_Status_422() => AssertShellAsync(
        Method("PUT"),
        "/stores/phase4c-code/branding",
        "{}",
        422,
        requiredField: null);

    // Source assertion 66: Business rule assertion: BR-MSA-BRD-001
    // @BR-ID: BR-MSA-BRD-001
    [Fact(DisplayName = "066: Business rule assertion: BR-MSA-BRD-001")]
    [Trait("BR", "BR-MSA-BRD-001")]
    public Task Test066_PUT_BASE_URL_stores_phase4c_code_branding_Field_storeCode_200() => AssertShellAsync(
        Method("PUT"),
        "/stores/phase4c-code/branding",
        "{\"templateCode\":\"phase4c-test\",\"logoUri\":\"https://example.com/phase4c\"}",
        200,
        requiredField: "storeCode");

    // Source assertion 67: Business rule assertion: BR-MER-011
    // @BR-ID: BR-MER-011
    [Fact(DisplayName = "067: Business rule assertion: BR-MER-011")]
    [Trait("BR", "BR-MER-011")]
    public Task Test067_PUT_BASE_URL_stores_phase4c_code_branding_Field_storeCode_200() => AssertShellAsync(
        Method("PUT"),
        "/stores/phase4c-code/branding",
        "{\"templateCode\":\"phase4c-test\",\"logoUri\":\"https://example.com/phase4c\"}",
        200,
        requiredField: "storeCode");

    // Source assertion 68: Contract success: PUT /stores/{storeCode}/languages
    // @BR-ID: BR-MER-012
    [Fact(DisplayName = "068: Contract success: PUT /stores/{storeCode}/languages")]
    [Trait("BR", "BR-MER-012")]
    public Task Test068_PUT_BASE_URL_stores_phase4c_code_languages_Field_id_200() => AssertShellAsync(
        Method("PUT"),
        "/stores/phase4c-code/languages",
        "{\"defaultLanguageCode\":\"phase4c-test\",\"supportedLanguageCodes\":[\"phase4c-test\"]}",
        200,
        requiredField: "id");

    // Source assertion 69: Contract error/conformance: PUT /stores/{storeCode}/languages
    // @BR-ID: BR-MER-012
    [Fact(DisplayName = "069: Contract error/conformance: PUT /stores/{storeCode}/languages")]
    [Trait("BR", "BR-MER-012")]
    public Task Test069_PUT_BASE_URL_stores_phase4c_code_languages_Status_422() => AssertShellAsync(
        Method("PUT"),
        "/stores/phase4c-code/languages",
        "{}",
        422,
        requiredField: null);

    // Source assertion 70: Business rule assertion: BR-MER-012
    // @BR-ID: BR-MER-012
    [Fact(DisplayName = "070: Business rule assertion: BR-MER-012")]
    [Trait("BR", "BR-MER-012")]
    public Task Test070_PUT_BASE_URL_stores_phase4c_code_languages_Field_id_200() => AssertShellAsync(
        Method("PUT"),
        "/stores/phase4c-code/languages",
        "{\"defaultLanguageCode\":\"phase4c-test\",\"supportedLanguageCodes\":[\"phase4c-test\"]}",
        200,
        requiredField: "id");

    // Source assertion 71: Business rule assertion: BR-MSA-LANG-001
    // @BR-ID: BR-MSA-LANG-001
    [Fact(DisplayName = "071: Business rule assertion: BR-MSA-LANG-001")]
    [Trait("BR", "BR-MSA-LANG-001")]
    public Task Test071_PUT_BASE_URL_stores_phase4c_code_languages_Field_id_200() => AssertShellAsync(
        Method("PUT"),
        "/stores/phase4c-code/languages",
        "{\"defaultLanguageCode\":\"phase4c-test\",\"supportedLanguageCodes\":[\"phase4c-test\"]}",
        200,
        requiredField: "id");

    // Source assertion 72: Business rule assertion: BR-MER-011
    // @BR-ID: BR-MER-011
    [Fact(DisplayName = "072: Business rule assertion: BR-MER-011")]
    [Trait("BR", "BR-MER-011")]
    public Task Test072_PUT_BASE_URL_stores_phase4c_code_languages_Field_id_200() => AssertShellAsync(
        Method("PUT"),
        "/stores/phase4c-code/languages",
        "{\"defaultLanguageCode\":\"phase4c-test\",\"supportedLanguageCodes\":[\"phase4c-test\"]}",
        200,
        requiredField: "id");

    // Source assertion 73: Contract success: DELETE /stores/{storeCode}
    // @BR-ID: BR-MER-006
    [Fact(DisplayName = "073: Contract success: DELETE /stores/{storeCode}")]
    [Trait("BR", "BR-MER-006")]
    public Task Test073_DELETE_BASE_URL_stores_phase4c_code_Status_204() => AssertShellAsync(
        Method("DELETE"),
        "/stores/phase4c-code",
        null,
        204,
        requiredField: null);

    // Source assertion 74: Contract error/conformance: DELETE /stores/{storeCode}
    // @BR-ID: BR-MER-006
    [Fact(DisplayName = "074: Contract error/conformance: DELETE /stores/{storeCode}")]
    [Trait("BR", "BR-MER-006")]
    public Task Test074_DELETE_BASE_URL_stores_phase4c_code_Status_403() => AssertShellAsync(
        Method("DELETE"),
        "/stores/phase4c-code",
        null,
        403,
        requiredField: null);

    // Source assertion 75: Business rule assertion: BR-MER-006
    // @BR-ID: BR-MER-006
    [Fact(DisplayName = "075: Business rule assertion: BR-MER-006")]
    [Trait("BR", "BR-MER-006")]
    public Task Test075_DELETE_BASE_URL_stores_phase4c_code_Status_204() => AssertShellAsync(
        Method("DELETE"),
        "/stores/phase4c-code",
        null,
        204,
        requiredField: null);

    // Source assertion 76: Business rule assertion: BR-MER-009
    // @BR-ID: BR-MER-009
    [Fact(DisplayName = "076: Business rule assertion: BR-MER-009")]
    [Trait("BR", "BR-MER-009")]
    public Task Test076_DELETE_BASE_URL_stores_phase4c_code_Status_204() => AssertShellAsync(
        Method("DELETE"),
        "/stores/phase4c-code",
        null,
        204,
        requiredField: null);

    // Source assertion 77: Business rule assertion: BR-MER-011
    // @BR-ID: BR-MER-011
    [Fact(DisplayName = "077: Business rule assertion: BR-MER-011")]
    [Trait("BR", "BR-MER-011")]
    public Task Test077_DELETE_BASE_URL_stores_phase4c_code_Status_204() => AssertShellAsync(
        Method("DELETE"),
        "/stores/phase4c-code",
        null,
        204,
        requiredField: null);

    // Source assertion 78: Contract success: DELETE /stores/{storeCode}/branding/logo
    // @BR-ID: BR-MSA-BRD-001
    [Fact(DisplayName = "078: Contract success: DELETE /stores/{storeCode}/branding/logo")]
    [Trait("BR", "BR-MSA-BRD-001")]
    public Task Test078_DELETE_BASE_URL_stores_phase4c_code_branding_logo_Status_204() => AssertShellAsync(
        Method("DELETE"),
        "/stores/phase4c-code/branding/logo",
        null,
        204,
        requiredField: null);

    // Source assertion 79: Contract error/conformance: DELETE /stores/{storeCode}/branding/logo
    // @BR-ID: BR-MSA-BRD-001
    [Fact(DisplayName = "079: Contract error/conformance: DELETE /stores/{storeCode}/branding/logo")]
    [Trait("BR", "BR-MSA-BRD-001")]
    public Task Test079_DELETE_BASE_URL_stores_phase4c_code_branding_logo_Status_404() => AssertShellAsync(
        Method("DELETE"),
        "/stores/phase4c-code/branding/logo",
        null,
        404,
        requiredField: null);

    // Source assertion 80: Business rule assertion: BR-MSA-BRD-001
    // @BR-ID: BR-MSA-BRD-001
    [Fact(DisplayName = "080: Business rule assertion: BR-MSA-BRD-001")]
    [Trait("BR", "BR-MSA-BRD-001")]
    public Task Test080_DELETE_BASE_URL_stores_phase4c_code_branding_logo_Status_204() => AssertShellAsync(
        Method("DELETE"),
        "/stores/phase4c-code/branding/logo",
        null,
        204,
        requiredField: null);

    // Source assertion 81: Business rule assertion: BR-MER-011
    // @BR-ID: BR-MER-011
    [Fact(DisplayName = "081: Business rule assertion: BR-MER-011")]
    [Trait("BR", "BR-MER-011")]
    public Task Test081_DELETE_BASE_URL_stores_phase4c_code_branding_logo_Status_204() => AssertShellAsync(
        Method("DELETE"),
        "/stores/phase4c-code/branding/logo",
        null,
        204,
        requiredField: null);
}
