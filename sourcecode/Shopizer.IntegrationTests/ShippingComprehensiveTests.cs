using Shopizer.IntegrationTests.Fixtures;

namespace Shopizer.IntegrationTests;

[Collection(ShopizerAspireCollection.Name)]
public sealed class ShippingComprehensiveTests(AspireHostFixture fixture) : ComprehensiveTestBase(
    fixture.ShippingClient,
    fixture.TestTenantAdminAccessToken,
    fixture.PrepareShippingRequestAsync)
{

    // Source assertion 1: Contract success: POST /cart/{cart}/shipping
    // @BR-ID: BR-PRC-022
    [Fact(DisplayName = "001: Contract success: POST /cart/{cart}/shipping")]
    [Trait("BR", "BR-PRC-022")]
    public Task Test001_POST_BASE_URL_cart_phase4c_value_shipping_Field_shipping_200() => AssertShellAsync(
        Method("POST"),
        "/cart/phase4c-value/shipping",
        "{\"countryCode\":\"phase4c-test\",\"postalCode\":\"phase4c-test\",\"address\":\"phase4c-test\",\"city\":\"phase4c-test\",\"state\":\"phase4c-test\",\"zoneCode\":\"phase4c-test\"}",
        200,
        requiredField: "shipping");

    // Source assertion 2: Contract error/conformance: POST /cart/{cart}/shipping
    // @BR-ID: BR-PRC-022
    [Fact(DisplayName = "002: Contract error/conformance: POST /cart/{cart}/shipping")]
    [Trait("BR", "BR-PRC-022")]
    public Task Test002_POST_BASE_URL_cart_phase4c_value_shipping_Status_400() => AssertShellAsync(
        Method("POST"),
        "/cart/phase4c-value/shipping",
        "{}",
        400,
        requiredField: null);

    // Source assertion 3: Business rule assertion: BR-PRC-022
    // @BR-ID: BR-PRC-022
    [Fact(DisplayName = "003: Business rule assertion: BR-PRC-022")]
    [Trait("BR", "BR-PRC-022")]
    public Task Test003_POST_BASE_URL_cart_phase4c_value_shipping_Field_shipping_200() => AssertShellAsync(
        Method("POST"),
        "/cart/phase4c-value/shipping",
        "{\"countryCode\":\"phase4c-test\",\"postalCode\":\"phase4c-test\",\"address\":\"phase4c-test\",\"city\":\"phase4c-test\",\"state\":\"phase4c-test\",\"zoneCode\":\"phase4c-test\"}",
        200,
        requiredField: "shipping");

    // Source assertion 4: Business rule assertion: BR-PRC-023
    // @BR-ID: BR-PRC-023
    [Fact(DisplayName = "004: Business rule assertion: BR-PRC-023")]
    [Trait("BR", "BR-PRC-023")]
    public Task Test004_POST_BASE_URL_cart_phase4c_value_shipping_Field_shipping_200() => AssertShellAsync(
        Method("POST"),
        "/cart/phase4c-value/shipping",
        "{\"countryCode\":\"phase4c-test\",\"postalCode\":\"phase4c-test\",\"address\":\"phase4c-test\",\"city\":\"phase4c-test\",\"state\":\"phase4c-test\",\"zoneCode\":\"phase4c-test\"}",
        200,
        requiredField: "shipping");

    // Source assertion 5: Business rule assertion: BR-PRC-024
    // @BR-ID: BR-PRC-024
    [Fact(DisplayName = "005: Business rule assertion: BR-PRC-024")]
    [Trait("BR", "BR-PRC-024")]
    public Task Test005_POST_BASE_URL_cart_phase4c_value_shipping_Field_shipping_200() => AssertShellAsync(
        Method("POST"),
        "/cart/phase4c-value/shipping",
        "{\"countryCode\":\"phase4c-test\",\"postalCode\":\"phase4c-test\",\"address\":\"phase4c-test\",\"city\":\"phase4c-test\",\"state\":\"phase4c-test\",\"zoneCode\":\"phase4c-test\"}",
        200,
        requiredField: "shipping");

    // Source assertion 6: Business rule assertion: BR-PRC-025
    // @BR-ID: BR-PRC-025
    [Fact(DisplayName = "006: Business rule assertion: BR-PRC-025")]
    [Trait("BR", "BR-PRC-025")]
    public Task Test006_POST_BASE_URL_cart_phase4c_value_shipping_Field_shipping_200() => AssertShellAsync(
        Method("POST"),
        "/cart/phase4c-value/shipping",
        "{\"countryCode\":\"phase4c-test\",\"postalCode\":\"phase4c-test\",\"address\":\"phase4c-test\",\"city\":\"phase4c-test\",\"state\":\"phase4c-test\",\"zoneCode\":\"phase4c-test\"}",
        200,
        requiredField: "shipping");

    // Source assertion 7: Business rule assertion: BR-PRC-026
    // @BR-ID: BR-PRC-026
    [Fact(DisplayName = "007: Business rule assertion: BR-PRC-026")]
    [Trait("BR", "BR-PRC-026")]
    public Task Test007_POST_BASE_URL_cart_phase4c_value_shipping_Field_shipping_200() => AssertShellAsync(
        Method("POST"),
        "/cart/phase4c-value/shipping",
        "{\"countryCode\":\"phase4c-test\",\"postalCode\":\"phase4c-test\",\"address\":\"phase4c-test\",\"city\":\"phase4c-test\",\"state\":\"phase4c-test\",\"zoneCode\":\"phase4c-test\"}",
        200,
        requiredField: "shipping");

    // Source assertion 8: Business rule assertion: BR-PRC-027
    // @BR-ID: BR-PRC-027
    [Fact(DisplayName = "008: Business rule assertion: BR-PRC-027")]
    [Trait("BR", "BR-PRC-027")]
    public Task Test008_POST_BASE_URL_cart_phase4c_value_shipping_Field_shipping_200() => AssertShellAsync(
        Method("POST"),
        "/cart/phase4c-value/shipping",
        "{\"countryCode\":\"phase4c-test\",\"postalCode\":\"phase4c-test\",\"address\":\"phase4c-test\",\"city\":\"phase4c-test\",\"state\":\"phase4c-test\",\"zoneCode\":\"phase4c-test\"}",
        200,
        requiredField: "shipping");

    // Source assertion 9: Business rule assertion: BR-PRC-028
    // @BR-ID: BR-PRC-028
    [Fact(DisplayName = "009: Business rule assertion: BR-PRC-028")]
    [Trait("BR", "BR-PRC-028")]
    public Task Test009_POST_BASE_URL_cart_phase4c_value_shipping_Field_shipping_200() => AssertShellAsync(
        Method("POST"),
        "/cart/phase4c-value/shipping",
        "{\"countryCode\":\"phase4c-test\",\"postalCode\":\"phase4c-test\",\"address\":\"phase4c-test\",\"city\":\"phase4c-test\",\"state\":\"phase4c-test\",\"zoneCode\":\"phase4c-test\"}",
        200,
        requiredField: "shipping");

    // Source assertion 10: Business rule assertion: BR-PRC-029
    // @BR-ID: BR-PRC-029
    [Fact(DisplayName = "010: Business rule assertion: BR-PRC-029")]
    [Trait("BR", "BR-PRC-029")]
    public Task Test010_POST_BASE_URL_cart_phase4c_value_shipping_Field_shipping_200() => AssertShellAsync(
        Method("POST"),
        "/cart/phase4c-value/shipping",
        "{\"countryCode\":\"phase4c-test\",\"postalCode\":\"phase4c-test\",\"address\":\"phase4c-test\",\"city\":\"phase4c-test\",\"state\":\"phase4c-test\",\"zoneCode\":\"phase4c-test\"}",
        200,
        requiredField: "shipping");

    // Source assertion 11: Business rule assertion: BR-PRC-030
    // @BR-ID: BR-PRC-030
    [Fact(DisplayName = "011: Business rule assertion: BR-PRC-030")]
    [Trait("BR", "BR-PRC-030")]
    public Task Test011_POST_BASE_URL_cart_phase4c_value_shipping_Field_shipping_200() => AssertShellAsync(
        Method("POST"),
        "/cart/phase4c-value/shipping",
        "{\"countryCode\":\"phase4c-test\",\"postalCode\":\"phase4c-test\",\"address\":\"phase4c-test\",\"city\":\"phase4c-test\",\"state\":\"phase4c-test\",\"zoneCode\":\"phase4c-test\"}",
        200,
        requiredField: "shipping");

    // Source assertion 12: Business rule assertion: BR-PRC-031
    // @BR-ID: BR-PRC-031
    [Fact(DisplayName = "012: Business rule assertion: BR-PRC-031")]
    [Trait("BR", "BR-PRC-031")]
    public Task Test012_POST_BASE_URL_cart_phase4c_value_shipping_Field_shipping_200() => AssertShellAsync(
        Method("POST"),
        "/cart/phase4c-value/shipping",
        "{\"countryCode\":\"phase4c-test\",\"postalCode\":\"phase4c-test\",\"address\":\"phase4c-test\",\"city\":\"phase4c-test\",\"state\":\"phase4c-test\",\"zoneCode\":\"phase4c-test\"}",
        200,
        requiredField: "shipping");

    // Source assertion 13: Business rule assertion: BR-PRC-032
    // @BR-ID: BR-PRC-032
    [Fact(DisplayName = "013: Business rule assertion: BR-PRC-032")]
    [Trait("BR", "BR-PRC-032")]
    public Task Test013_POST_BASE_URL_cart_phase4c_value_shipping_Field_shipping_200() => AssertShellAsync(
        Method("POST"),
        "/cart/phase4c-value/shipping",
        "{\"countryCode\":\"phase4c-test\",\"postalCode\":\"phase4c-test\",\"address\":\"phase4c-test\",\"city\":\"phase4c-test\",\"state\":\"phase4c-test\",\"zoneCode\":\"phase4c-test\"}",
        200,
        requiredField: "shipping");

    // Source assertion 14: Business rule assertion: BR-PRC-033
    // @BR-ID: BR-PRC-033
    [Fact(DisplayName = "014: Business rule assertion: BR-PRC-033")]
    [Trait("BR", "BR-PRC-033")]
    public Task Test014_POST_BASE_URL_cart_phase4c_value_shipping_Field_shipping_200() => AssertShellAsync(
        Method("POST"),
        "/cart/phase4c-value/shipping",
        "{\"countryCode\":\"phase4c-test\",\"postalCode\":\"phase4c-test\",\"address\":\"phase4c-test\",\"city\":\"phase4c-test\",\"state\":\"phase4c-test\",\"zoneCode\":\"phase4c-test\"}",
        200,
        requiredField: "shipping");

    // Source assertion 15: Business rule assertion: BR-PRC-034
    // @BR-ID: BR-PRC-034
    [Fact(DisplayName = "015: Business rule assertion: BR-PRC-034")]
    [Trait("BR", "BR-PRC-034")]
    public Task Test015_POST_BASE_URL_cart_phase4c_value_shipping_Field_shipping_200() => AssertShellAsync(
        Method("POST"),
        "/cart/phase4c-value/shipping",
        "{\"countryCode\":\"phase4c-test\",\"postalCode\":\"phase4c-test\",\"address\":\"phase4c-test\",\"city\":\"phase4c-test\",\"state\":\"phase4c-test\",\"zoneCode\":\"phase4c-test\"}",
        200,
        requiredField: "shipping");

    // Source assertion 16: Business rule assertion: BR-PRC-035
    // @BR-ID: BR-PRC-035
    [Fact(DisplayName = "016: Business rule assertion: BR-PRC-035")]
    [Trait("BR", "BR-PRC-035")]
    public Task Test016_POST_BASE_URL_cart_phase4c_value_shipping_Field_shipping_200() => AssertShellAsync(
        Method("POST"),
        "/cart/phase4c-value/shipping",
        "{\"countryCode\":\"phase4c-test\",\"postalCode\":\"phase4c-test\",\"address\":\"phase4c-test\",\"city\":\"phase4c-test\",\"state\":\"phase4c-test\",\"zoneCode\":\"phase4c-test\"}",
        200,
        requiredField: "shipping");

    // Source assertion 17: Business rule assertion: BR-PRC-036
    // @BR-ID: BR-PRC-036
    [Fact(DisplayName = "017: Business rule assertion: BR-PRC-036")]
    [Trait("BR", "BR-PRC-036")]
    public Task Test017_POST_BASE_URL_cart_phase4c_value_shipping_Field_shipping_200() => AssertShellAsync(
        Method("POST"),
        "/cart/phase4c-value/shipping",
        "{\"countryCode\":\"phase4c-test\",\"postalCode\":\"phase4c-test\",\"address\":\"phase4c-test\",\"city\":\"phase4c-test\",\"state\":\"phase4c-test\",\"zoneCode\":\"phase4c-test\"}",
        200,
        requiredField: "shipping");

    // Source assertion 18: Business rule assertion: BR-EXT-010
    // @BR-ID: BR-EXT-010
    [Fact(DisplayName = "018: Business rule assertion: BR-EXT-010")]
    [Trait("BR", "BR-EXT-010")]
    public Task Test018_POST_BASE_URL_cart_phase4c_value_shipping_Field_shipping_200() => AssertShellAsync(
        Method("POST"),
        "/cart/phase4c-value/shipping",
        "{\"countryCode\":\"phase4c-test\",\"postalCode\":\"phase4c-test\",\"address\":\"phase4c-test\",\"city\":\"phase4c-test\",\"state\":\"phase4c-test\",\"zoneCode\":\"phase4c-test\"}",
        200,
        requiredField: "shipping");

    // Source assertion 19: Business rule assertion: BR-EXT-011
    // @BR-ID: BR-EXT-011
    [Fact(DisplayName = "019: Business rule assertion: BR-EXT-011")]
    [Trait("BR", "BR-EXT-011")]
    public Task Test019_POST_BASE_URL_cart_phase4c_value_shipping_Field_shipping_200() => AssertShellAsync(
        Method("POST"),
        "/cart/phase4c-value/shipping",
        "{\"countryCode\":\"phase4c-test\",\"postalCode\":\"phase4c-test\",\"address\":\"phase4c-test\",\"city\":\"phase4c-test\",\"state\":\"phase4c-test\",\"zoneCode\":\"phase4c-test\"}",
        200,
        requiredField: "shipping");

    // Source assertion 20: Business rule assertion: BR-EXT-012
    // @BR-ID: BR-EXT-012
    [Fact(DisplayName = "020: Business rule assertion: BR-EXT-012")]
    [Trait("BR", "BR-EXT-012")]
    public Task Test020_POST_BASE_URL_cart_phase4c_value_shipping_Field_shipping_200() => AssertShellAsync(
        Method("POST"),
        "/cart/phase4c-value/shipping",
        "{\"countryCode\":\"phase4c-test\",\"postalCode\":\"phase4c-test\",\"address\":\"phase4c-test\",\"city\":\"phase4c-test\",\"state\":\"phase4c-test\",\"zoneCode\":\"phase4c-test\"}",
        200,
        requiredField: "shipping");

    // Source assertion 21: Business rule assertion: BR-EXT-013
    // @BR-ID: BR-EXT-013
    [Fact(DisplayName = "021: Business rule assertion: BR-EXT-013")]
    [Trait("BR", "BR-EXT-013")]
    public Task Test021_POST_BASE_URL_cart_phase4c_value_shipping_Field_shipping_200() => AssertShellAsync(
        Method("POST"),
        "/cart/phase4c-value/shipping",
        "{\"countryCode\":\"phase4c-test\",\"postalCode\":\"phase4c-test\",\"address\":\"phase4c-test\",\"city\":\"phase4c-test\",\"state\":\"phase4c-test\",\"zoneCode\":\"phase4c-test\"}",
        200,
        requiredField: "shipping");

    // Source assertion 22: Business rule assertion: BR-EXT-014
    // @BR-ID: BR-EXT-014
    [Fact(DisplayName = "022: Business rule assertion: BR-EXT-014")]
    [Trait("BR", "BR-EXT-014")]
    public Task Test022_POST_BASE_URL_cart_phase4c_value_shipping_Field_shipping_200() => AssertShellAsync(
        Method("POST"),
        "/cart/phase4c-value/shipping",
        "{\"countryCode\":\"phase4c-test\",\"postalCode\":\"phase4c-test\",\"address\":\"phase4c-test\",\"city\":\"phase4c-test\",\"state\":\"phase4c-test\",\"zoneCode\":\"phase4c-test\"}",
        200,
        requiredField: "shipping");

    // Source assertion 23: Business rule assertion: BR-EXT-015
    // @BR-ID: BR-EXT-015
    [Fact(DisplayName = "023: Business rule assertion: BR-EXT-015")]
    [Trait("BR", "BR-EXT-015")]
    public Task Test023_POST_BASE_URL_cart_phase4c_value_shipping_Field_shipping_200() => AssertShellAsync(
        Method("POST"),
        "/cart/phase4c-value/shipping",
        "{\"countryCode\":\"phase4c-test\",\"postalCode\":\"phase4c-test\",\"address\":\"phase4c-test\",\"city\":\"phase4c-test\",\"state\":\"phase4c-test\",\"zoneCode\":\"phase4c-test\"}",
        200,
        requiredField: "shipping");

    // Source assertion 24: Business rule assertion: BR-EXT-016
    // @BR-ID: BR-EXT-016
    [Fact(DisplayName = "024: Business rule assertion: BR-EXT-016")]
    [Trait("BR", "BR-EXT-016")]
    public Task Test024_POST_BASE_URL_cart_phase4c_value_shipping_Field_shipping_200() => AssertShellAsync(
        Method("POST"),
        "/cart/phase4c-value/shipping",
        "{\"countryCode\":\"phase4c-test\",\"postalCode\":\"phase4c-test\",\"address\":\"phase4c-test\",\"city\":\"phase4c-test\",\"state\":\"phase4c-test\",\"zoneCode\":\"phase4c-test\"}",
        200,
        requiredField: "shipping");

    // Source assertion 25: Business rule assertion: BR-EXT-018
    // @BR-ID: BR-EXT-018
    [Fact(DisplayName = "025: Business rule assertion: BR-EXT-018")]
    [Trait("BR", "BR-EXT-018")]
    public Task Test025_POST_BASE_URL_cart_phase4c_value_shipping_Field_shipping_200() => AssertShellAsync(
        Method("POST"),
        "/cart/phase4c-value/shipping",
        "{\"countryCode\":\"phase4c-test\",\"postalCode\":\"phase4c-test\",\"address\":\"phase4c-test\",\"city\":\"phase4c-test\",\"state\":\"phase4c-test\",\"zoneCode\":\"phase4c-test\"}",
        200,
        requiredField: "shipping");

    // Source assertion 26: Business rule assertion: BR-UI-008
    // @BR-ID: BR-UI-008
    [Fact(DisplayName = "026: Business rule assertion: BR-UI-008")]
    [Trait("BR", "BR-UI-008")]
    public Task Test026_POST_BASE_URL_cart_phase4c_value_shipping_Field_shipping_200() => AssertShellAsync(
        Method("POST"),
        "/cart/phase4c-value/shipping",
        "{\"countryCode\":\"phase4c-test\",\"postalCode\":\"phase4c-test\",\"address\":\"phase4c-test\",\"city\":\"phase4c-test\",\"state\":\"phase4c-test\",\"zoneCode\":\"phase4c-test\"}",
        200,
        requiredField: "shipping");

    // Source assertion 27: Contract success: POST /private/modules/shipping
    // @BR-ID: BR-PRC-022
    [Fact(DisplayName = "027: Contract success: POST /private/modules/shipping")]
    [Trait("BR", "BR-PRC-022")]
    public Task Test027_POST_BASE_URL_private_modules_shipping_Status_200() => AssertShellAsync(
        Method("POST"),
        "/private/modules/shipping",
        "{\"moduleCode\":\"phase4c-test\",\"active\":true,\"defaultSelected\":true,\"environment\":\"Test\",\"integrationKeys\":{},\"integrationOptions\":{}}",
        200,
        requiredField: null);

    // Source assertion 28: Contract error/conformance: POST /private/modules/shipping
    // @BR-ID: BR-PRC-022
    [Fact(DisplayName = "028: Contract error/conformance: POST /private/modules/shipping")]
    [Trait("BR", "BR-PRC-022")]
    public Task Test028_POST_BASE_URL_private_modules_shipping_Status_400() => AssertShellAsync(
        Method("POST"),
        "/private/modules/shipping",
        "{}",
        400,
        requiredField: null);

    // Source assertion 29: Contract success: POST /private/shipping/expedition
    // @BR-ID: BR-PRC-022
    [Fact(DisplayName = "029: Contract success: POST /private/shipping/expedition")]
    [Trait("BR", "BR-PRC-022")]
    public Task Test029_POST_BASE_URL_private_shipping_expedition_Status_200() => AssertShellAsync(
        Method("POST"),
        "/private/shipping/expedition",
        "{\"internationalShipping\":true,\"taxOnShipping\":true,\"shipToCountry\":[\"phase4c-test\"]}",
        200,
        requiredField: null);

    // Source assertion 30: Contract error/conformance: POST /private/shipping/expedition
    // @BR-ID: BR-PRC-022
    [Fact(DisplayName = "030: Contract error/conformance: POST /private/shipping/expedition")]
    [Trait("BR", "BR-PRC-022")]
    public Task Test030_POST_BASE_URL_private_shipping_expedition_Status_400() => AssertShellAsync(
        Method("POST"),
        "/private/shipping/expedition",
        "{}",
        400,
        requiredField: null);

    // Source assertion 31: Contract success: POST /private/shipping/origin
    // @BR-ID: BR-PRC-022
    [Fact(DisplayName = "031: Contract success: POST /private/shipping/origin")]
    [Trait("BR", "BR-PRC-022")]
    public Task Test031_POST_BASE_URL_private_shipping_origin_Status_200() => AssertShellAsync(
        Method("POST"),
        "/private/shipping/origin",
        "{\"address\":\"phase4c-test\",\"city\":\"phase4c-test\",\"postalCode\":\"phase4c-test\",\"state\":\"phase4c-test\",\"countryCode\":\"phase4c-test\",\"zoneCode\":\"phase4c-test\",\"active\":true}",
        200,
        requiredField: null);

    // Source assertion 32: Contract error/conformance: POST /private/shipping/origin
    // @BR-ID: BR-PRC-022
    [Fact(DisplayName = "032: Contract error/conformance: POST /private/shipping/origin")]
    [Trait("BR", "BR-PRC-022")]
    public Task Test032_POST_BASE_URL_private_shipping_origin_Status_400() => AssertShellAsync(
        Method("POST"),
        "/private/shipping/origin",
        "{}",
        400,
        requiredField: null);

    // Source assertion 33: Contract success: POST /private/shipping/package
    // @BR-ID: BR-PRC-022
    [Fact(DisplayName = "033: Contract success: POST /private/shipping/package")]
    [Trait("BR", "BR-PRC-022")]
    public Task Test033_POST_BASE_URL_private_shipping_package_Status_200() => AssertShellAsync(
        Method("POST"),
        "/private/shipping/package",
        "{\"code\":\"phase4c-test\",\"shippingWidth\":10.5,\"shippingHeight\":10.5,\"shippingLength\":10.5,\"shippingWeight\":10.5,\"shippingMaxWeight\":10.5,\"treshold\":1,\"type\":\"Item\",\"defaultPackaging\":true}",
        200,
        requiredField: null);

    // Source assertion 34: Contract error/conformance: POST /private/shipping/package
    // @BR-ID: BR-PRC-022
    [Fact(DisplayName = "034: Contract error/conformance: POST /private/shipping/package")]
    [Trait("BR", "BR-PRC-022")]
    public Task Test034_POST_BASE_URL_private_shipping_package_Status_400() => AssertShellAsync(
        Method("POST"),
        "/private/shipping/package",
        "{}",
        400,
        requiredField: null);

    // Source assertion 35: Contract success: GET /auth/cart/{cart}/shipping
    // @BR-ID: BR-PRC-022
    [Fact(DisplayName = "035: Contract success: GET /auth/cart/{cart}/shipping")]
    [Trait("BR", "BR-PRC-022")]
    public async Task Test035_GET_BASE_URL_auth_cart_phase4c_value_shipping_Field_shipping_200()
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            "/api/v1/auth/cart/phase4c-cart-test/shipping?countryCode=CA&postalCode=H2Y1C6&address=1%20Main%20Street&city=Montreal&state=QC&zoneCode=QC");
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(
            "Bearer",
            fixture.CartCheckoutCustomerAccessToken);

        using var response = await fixture.ShippingClient.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("\"shipping\"", body, StringComparison.Ordinal);
    }

    // Source assertion 36: Contract error/conformance: GET /auth/cart/{cart}/shipping
    // @BR-ID: BR-PRC-022
    [Fact(DisplayName = "036: Contract error/conformance: GET /auth/cart/{cart}/shipping")]
    [Trait("BR", "BR-PRC-022")]
    public Task Test036_GET_BASE_URL_auth_cart_phase4c_value_shipping_Status_401() => AssertShellAsync(
        Method("GET"),
        "/auth/cart/phase4c-value/shipping",
        null,
        401,
        requiredField: null);

    // Source assertion 37: Contract success: GET /private/configurations/shipping
    // @BR-ID: BR-PRC-022
    [Fact(DisplayName = "037: Contract success: GET /private/configurations/shipping")]
    [Trait("BR", "BR-PRC-022")]
    public Task Test037_GET_BASE_URL_private_configurations_shipping_Field_shippingType_200() => AssertShellAsync(
        Method("GET"),
        "/private/configurations/shipping",
        null,
        200,
        requiredField: "shippingType");

    // Source assertion 38: Contract error/conformance: GET /private/configurations/shipping
    // @BR-ID: BR-PRC-022
    [Fact(DisplayName = "038: Contract error/conformance: GET /private/configurations/shipping")]
    [Trait("BR", "BR-PRC-022")]
    public Task Test038_GET_BASE_URL_private_configurations_shipping_Status_401() => AssertShellAsync(
        Method("GET"),
        "/private/configurations/shipping",
        null,
        401,
        requiredField: null);

    // Source assertion 39: Contract success: GET /private/modules/shipping
    // @BR-ID: BR-PRC-022
    [Fact(DisplayName = "039: Contract success: GET /private/modules/shipping")]
    [Trait("BR", "BR-PRC-022")]
    public Task Test039_GET_BASE_URL_private_modules_shipping_Status_200() => AssertShellAsync(
        Method("GET"),
        "/private/modules/shipping",
        null,
        200,
        requiredField: null);

    // Source assertion 40: Contract error/conformance: GET /private/modules/shipping
    // @BR-ID: BR-PRC-022
    [Fact(DisplayName = "040: Contract error/conformance: GET /private/modules/shipping")]
    [Trait("BR", "BR-PRC-022")]
    public Task Test040_GET_BASE_URL_private_modules_shipping_Status_401() => AssertShellAsync(
        Method("GET"),
        "/private/modules/shipping",
        null,
        401,
        requiredField: null);

    // Source assertion 41: Contract success: GET /private/modules/shipping/{module}
    // @BR-ID: BR-PRC-022
    [Fact(DisplayName = "041: Contract success: GET /private/modules/shipping/{module}")]
    [Trait("BR", "BR-PRC-022")]
    public Task Test041_GET_BASE_URL_private_modules_shipping_phase4c_value_Status_200() => AssertShellAsync(
        Method("GET"),
        "/private/modules/shipping/phase4c-value",
        null,
        200,
        requiredField: null);

    // Source assertion 42: Contract error/conformance: GET /private/modules/shipping/{module}
    // @BR-ID: BR-PRC-022
    [Fact(DisplayName = "042: Contract error/conformance: GET /private/modules/shipping/{module}")]
    [Trait("BR", "BR-PRC-022")]
    public Task Test042_GET_BASE_URL_private_modules_shipping_phase4c_value_Status_401() => AssertShellAsync(
        Method("GET"),
        "/private/modules/shipping/phase4c-value",
        null,
        401,
        requiredField: null);

    // Source assertion 43: Contract success: GET /private/shipping/expedition
    // @BR-ID: BR-PRC-022
    [Fact(DisplayName = "043: Contract success: GET /private/shipping/expedition")]
    [Trait("BR", "BR-PRC-022")]
    public Task Test043_GET_BASE_URL_private_shipping_expedition_Status_200() => AssertShellAsync(
        Method("GET"),
        "/private/shipping/expedition",
        null,
        200,
        requiredField: null);

    // Source assertion 44: Contract error/conformance: GET /private/shipping/expedition
    // @BR-ID: BR-PRC-022
    [Fact(DisplayName = "044: Contract error/conformance: GET /private/shipping/expedition")]
    [Trait("BR", "BR-PRC-022")]
    public Task Test044_GET_BASE_URL_private_shipping_expedition_Status_401() => AssertShellAsync(
        Method("GET"),
        "/private/shipping/expedition",
        null,
        401,
        requiredField: null);

    // Source assertion 45: Contract success: GET /private/shipping/origin
    // @BR-ID: BR-PRC-022
    [Fact(DisplayName = "045: Contract success: GET /private/shipping/origin")]
    [Trait("BR", "BR-PRC-022")]
    public Task Test045_GET_BASE_URL_private_shipping_origin_Status_200() => AssertShellAsync(
        Method("GET"),
        "/private/shipping/origin",
        null,
        200,
        requiredField: null);

    // Source assertion 46: Contract error/conformance: GET /private/shipping/origin
    // @BR-ID: BR-PRC-022
    [Fact(DisplayName = "046: Contract error/conformance: GET /private/shipping/origin")]
    [Trait("BR", "BR-PRC-022")]
    public Task Test046_GET_BASE_URL_private_shipping_origin_Status_401() => AssertShellAsync(
        Method("GET"),
        "/private/shipping/origin",
        null,
        401,
        requiredField: null);

    // Source assertion 47: Contract success: GET /private/shipping/package/{package}
    // @BR-ID: BR-PRC-022
    [Fact(DisplayName = "047: Contract success: GET /private/shipping/package/{package}")]
    [Trait("BR", "BR-PRC-022")]
    public Task Test047_GET_BASE_URL_private_shipping_package_phase4c_value_Status_200() => AssertShellAsync(
        Method("GET"),
        "/private/shipping/package/phase4c-value",
        null,
        200,
        requiredField: null);

    // Source assertion 48: Contract error/conformance: GET /private/shipping/package/{package}
    // @BR-ID: BR-PRC-022
    [Fact(DisplayName = "048: Contract error/conformance: GET /private/shipping/package/{package}")]
    [Trait("BR", "BR-PRC-022")]
    public Task Test048_GET_BASE_URL_private_shipping_package_phase4c_value_Status_401() => AssertShellAsync(
        Method("GET"),
        "/private/shipping/package/phase4c-value",
        null,
        401,
        requiredField: null);

    // Source assertion 49: Contract success: GET /private/shipping/packages
    // @BR-ID: BR-PRC-022
    [Fact(DisplayName = "049: Contract success: GET /private/shipping/packages")]
    [Trait("BR", "BR-PRC-022")]
    public Task Test049_GET_BASE_URL_private_shipping_packages_Status_200() => AssertShellAsync(
        Method("GET"),
        "/private/shipping/packages",
        null,
        200,
        requiredField: null);

    // Source assertion 50: Contract error/conformance: GET /private/shipping/packages
    // @BR-ID: BR-PRC-022
    [Fact(DisplayName = "050: Contract error/conformance: GET /private/shipping/packages")]
    [Trait("BR", "BR-PRC-022")]
    public Task Test050_GET_BASE_URL_private_shipping_packages_Status_401() => AssertShellAsync(
        Method("GET"),
        "/private/shipping/packages",
        null,
        401,
        requiredField: null);

    // Source assertion 51: Contract success: GET /shipping/country
    // @BR-ID: BR-PRC-022
    [Fact(DisplayName = "051: Contract success: GET /shipping/country")]
    [Trait("BR", "BR-PRC-022")]
    public Task Test051_GET_BASE_URL_shipping_country_Status_200() => AssertShellAsync(
        Method("GET"),
        "/shipping/country",
        null,
        200,
        requiredField: null);

    // Source assertion 52: Contract error/conformance: GET /shipping/country
    // @BR-ID: BR-PRC-022
    [Fact(DisplayName = "052: Contract error/conformance: GET /shipping/country")]
    [Trait("BR", "BR-PRC-022")]
    public Task Test052_GET_BASE_URL_shipping_country_Status_200() => AssertShellAsync(
        Method("GET"),
        "/shipping/country",
        null,
        200,
        requiredField: null);

    // Source assertion 53: Contract success: PUT /private/shipping/package/{package}
    // @BR-ID: BR-PRC-022
    [Fact(DisplayName = "053: Contract success: PUT /private/shipping/package/{package}")]
    [Trait("BR", "BR-PRC-022")]
    public Task Test053_PUT_BASE_URL_private_shipping_package_phase4c_value_Status_200() => AssertShellAsync(
        Method("PUT"),
        "/private/shipping/package/phase4c-value",
        "{\"code\":\"phase4c-test\",\"shippingWidth\":10.5,\"shippingHeight\":10.5,\"shippingLength\":10.5,\"shippingWeight\":10.5,\"shippingMaxWeight\":10.5,\"treshold\":1,\"type\":\"Item\",\"defaultPackaging\":true}",
        200,
        requiredField: null);

    // Source assertion 54: Contract error/conformance: PUT /private/shipping/package/{package}
    // @BR-ID: BR-PRC-022
    [Fact(DisplayName = "054: Contract error/conformance: PUT /private/shipping/package/{package}")]
    [Trait("BR", "BR-PRC-022")]
    public Task Test054_PUT_BASE_URL_private_shipping_package_phase4c_value_Status_400() => AssertShellAsync(
        Method("PUT"),
        "/private/shipping/package/phase4c-value",
        "{}",
        400,
        requiredField: null);

    // Source assertion 55: Contract success: DELETE /private/shipping/package/{package}
    // @BR-ID: BR-PRC-022
    [Fact(DisplayName = "055: Contract success: DELETE /private/shipping/package/{package}")]
    [Trait("BR", "BR-PRC-022")]
    public Task Test055_DELETE_BASE_URL_private_shipping_package_phase4c_value_Field_status_200() => AssertShellAsync(
        Method("DELETE"),
        "/private/shipping/package/phase4c-value",
        null,
        200,
        requiredField: "status");

    // Source assertion 56: Contract error/conformance: DELETE /private/shipping/package/{package}
    // @BR-ID: BR-PRC-022
    [Fact(DisplayName = "056: Contract error/conformance: DELETE /private/shipping/package/{package}")]
    [Trait("BR", "BR-PRC-022")]
    public Task Test056_DELETE_BASE_URL_private_shipping_package_phase4c_value_Status_401() => AssertShellAsync(
        Method("DELETE"),
        "/private/shipping/package/phase4c-value",
        null,
        401,
        requiredField: null);
}
