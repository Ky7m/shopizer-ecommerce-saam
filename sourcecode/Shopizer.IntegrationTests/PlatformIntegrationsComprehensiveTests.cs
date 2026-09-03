using Shopizer.IntegrationTests.Fixtures;

namespace Shopizer.IntegrationTests;

[Collection(ShopizerAspireCollection.Name)]
public sealed class PlatformIntegrationsComprehensiveTests(AspireHostFixture fixture) : ComprehensiveTestBase(fixture.PlatformIntegrationsClient)
{

    // Source assertion 1: Contract success: POST /adapters/refresh
    // @BR-ID: BR-INT-MS12-001
    [Fact(DisplayName = "001: Contract success: POST /adapters/refresh")]
    [Trait("BR", "BR-INT-MS12-001")]
    public Task Test001_POST_BASE_URL_adapters_refresh_Field_endpointId_200() => AssertShellAsync(
        Method("POST"),
        "/adapters/refresh",
        "{\"moduleType\":{},\"code\":\"phase4c-test\",\"provider\":\"phase4c-test\",\"environment\":\"phase4c-test\",\"configurationRef\":\"phase4c-test\",\"resolvedEndpointUri\":\"https://example.com/phase4c\",\"capabilities\":{},\"timeoutMs\":1,\"maxAttempts\":1,\"config1\":\"phase4c-test\",\"config2\":\"phase4c-test\",\"credentials\":{},\"packageTypes\":[\"phase4c-test\"]}",
        200,
        requiredField: "endpointId");

    // Source assertion 2: Contract error/conformance: POST /adapters/refresh
    // @BR-ID: BR-INT-MS12-001
    [Fact(DisplayName = "002: Contract error/conformance: POST /adapters/refresh")]
    [Trait("BR", "BR-INT-MS12-001")]
    public Task Test002_POST_BASE_URL_adapters_refresh_Status_400() => AssertShellAsync(
        Method("POST"),
        "/adapters/refresh",
        "{}",
        400,
        requiredField: null);

    // Source assertion 3: Business rule assertion: BR-INT-MS12-001
    // @BR-ID: BR-INT-MS12-001
    [Fact(DisplayName = "003: Business rule assertion: BR-INT-MS12-001")]
    [Trait("BR", "BR-INT-MS12-001")]
    public Task Test003_POST_BASE_URL_adapters_refresh_Field_endpointId_200() => AssertShellAsync(
        Method("POST"),
        "/adapters/refresh",
        "{\"moduleType\":{},\"code\":\"phase4c-test\",\"provider\":\"phase4c-test\",\"environment\":\"phase4c-test\",\"configurationRef\":\"phase4c-test\",\"resolvedEndpointUri\":\"https://example.com/phase4c\",\"capabilities\":{},\"timeoutMs\":1,\"maxAttempts\":1,\"config1\":\"phase4c-test\",\"config2\":\"phase4c-test\",\"credentials\":{},\"packageTypes\":[\"phase4c-test\"]}",
        200,
        requiredField: "endpointId");

    // Source assertion 4: Business rule assertion: BR-INT-MS12-002
    // @BR-ID: BR-INT-MS12-002
    [Fact(DisplayName = "004: Business rule assertion: BR-INT-MS12-002")]
    [Trait("BR", "BR-INT-MS12-002")]
    public Task Test004_POST_BASE_URL_adapters_refresh_Field_endpointId_200() => AssertShellAsync(
        Method("POST"),
        "/adapters/refresh",
        "{\"moduleType\":{},\"code\":\"phase4c-test\",\"provider\":\"phase4c-test\",\"environment\":\"phase4c-test\",\"configurationRef\":\"phase4c-test\",\"resolvedEndpointUri\":\"https://example.com/phase4c\",\"capabilities\":{},\"timeoutMs\":1,\"maxAttempts\":1,\"config1\":\"phase4c-test\",\"config2\":\"phase4c-test\",\"credentials\":{},\"packageTypes\":[\"phase4c-test\"]}",
        200,
        requiredField: "endpointId");

    // Source assertion 5: Business rule assertion: BR-INT-MS12-003
    // @BR-ID: BR-INT-MS12-003
    [Fact(DisplayName = "005: Business rule assertion: BR-INT-MS12-003")]
    [Trait("BR", "BR-INT-MS12-003")]
    public Task Test005_POST_BASE_URL_adapters_refresh_Field_endpointId_200() => AssertShellAsync(
        Method("POST"),
        "/adapters/refresh",
        "{\"moduleType\":{},\"code\":\"phase4c-test\",\"provider\":\"phase4c-test\",\"environment\":\"phase4c-test\",\"configurationRef\":\"phase4c-test\",\"resolvedEndpointUri\":\"https://example.com/phase4c\",\"capabilities\":{},\"timeoutMs\":1,\"maxAttempts\":1,\"config1\":\"phase4c-test\",\"config2\":\"phase4c-test\",\"credentials\":{},\"packageTypes\":[\"phase4c-test\"]}",
        200,
        requiredField: "endpointId");

    // Source assertion 6: Business rule assertion: BR-INT-MS12-004
    // @BR-ID: BR-INT-MS12-004
    [Fact(DisplayName = "006: Business rule assertion: BR-INT-MS12-004")]
    [Trait("BR", "BR-INT-MS12-004")]
    public Task Test006_POST_BASE_URL_adapters_refresh_Field_endpointId_200() => AssertShellAsync(
        Method("POST"),
        "/adapters/refresh",
        "{\"moduleType\":{},\"code\":\"phase4c-test\",\"provider\":\"phase4c-test\",\"environment\":\"phase4c-test\",\"configurationRef\":\"phase4c-test\",\"resolvedEndpointUri\":\"https://example.com/phase4c\",\"capabilities\":{},\"timeoutMs\":1,\"maxAttempts\":1,\"config1\":\"phase4c-test\",\"config2\":\"phase4c-test\",\"credentials\":{},\"packageTypes\":[\"phase4c-test\"]}",
        200,
        requiredField: "endpointId");

    // Source assertion 7: Business rule assertion: BR-INT-MS12-005
    // @BR-ID: BR-INT-MS12-005
    [Fact(DisplayName = "007: Business rule assertion: BR-INT-MS12-005")]
    [Trait("BR", "BR-INT-MS12-005")]
    public Task Test007_POST_BASE_URL_adapters_refresh_Field_endpointId_200() => AssertShellAsync(
        Method("POST"),
        "/adapters/refresh",
        "{\"moduleType\":{},\"code\":\"phase4c-test\",\"provider\":\"phase4c-test\",\"environment\":\"phase4c-test\",\"configurationRef\":\"phase4c-test\",\"resolvedEndpointUri\":\"https://example.com/phase4c\",\"capabilities\":{},\"timeoutMs\":1,\"maxAttempts\":1,\"config1\":\"phase4c-test\",\"config2\":\"phase4c-test\",\"credentials\":{},\"packageTypes\":[\"phase4c-test\"]}",
        200,
        requiredField: "endpointId");

    // Source assertion 8: Business rule assertion: BR-INT-MS12-006
    // @BR-ID: BR-INT-MS12-006
    [Fact(DisplayName = "008: Business rule assertion: BR-INT-MS12-006")]
    [Trait("BR", "BR-INT-MS12-006")]
    public Task Test008_POST_BASE_URL_adapters_refresh_Field_endpointId_200() => AssertShellAsync(
        Method("POST"),
        "/adapters/refresh",
        "{\"moduleType\":{},\"code\":\"phase4c-test\",\"provider\":\"phase4c-test\",\"environment\":\"phase4c-test\",\"configurationRef\":\"phase4c-test\",\"resolvedEndpointUri\":\"https://example.com/phase4c\",\"capabilities\":{},\"timeoutMs\":1,\"maxAttempts\":1,\"config1\":\"phase4c-test\",\"config2\":\"phase4c-test\",\"credentials\":{},\"packageTypes\":[\"phase4c-test\"]}",
        200,
        requiredField: "endpointId");

    // Source assertion 9: Business rule assertion: BR-INT-MS12-007
    // @BR-ID: BR-INT-MS12-007
    [Fact(DisplayName = "009: Business rule assertion: BR-INT-MS12-007")]
    [Trait("BR", "BR-INT-MS12-007")]
    public Task Test009_POST_BASE_URL_adapters_refresh_Field_endpointId_200() => AssertShellAsync(
        Method("POST"),
        "/adapters/refresh",
        "{\"moduleType\":{},\"code\":\"phase4c-test\",\"provider\":\"phase4c-test\",\"environment\":\"phase4c-test\",\"configurationRef\":\"phase4c-test\",\"resolvedEndpointUri\":\"https://example.com/phase4c\",\"capabilities\":{},\"timeoutMs\":1,\"maxAttempts\":1,\"config1\":\"phase4c-test\",\"config2\":\"phase4c-test\",\"credentials\":{},\"packageTypes\":[\"phase4c-test\"]}",
        200,
        requiredField: "endpointId");

    // Source assertion 10: Business rule assertion: BR-INT-MS12-008
    // @BR-ID: BR-INT-MS12-008
    [Fact(DisplayName = "010: Business rule assertion: BR-INT-MS12-008")]
    [Trait("BR", "BR-INT-MS12-008")]
    public Task Test010_POST_BASE_URL_adapters_refresh_Field_endpointId_200() => AssertShellAsync(
        Method("POST"),
        "/adapters/refresh",
        "{\"moduleType\":{},\"code\":\"phase4c-test\",\"provider\":\"phase4c-test\",\"environment\":\"phase4c-test\",\"configurationRef\":\"phase4c-test\",\"resolvedEndpointUri\":\"https://example.com/phase4c\",\"capabilities\":{},\"timeoutMs\":1,\"maxAttempts\":1,\"config1\":\"phase4c-test\",\"config2\":\"phase4c-test\",\"credentials\":{},\"packageTypes\":[\"phase4c-test\"]}",
        200,
        requiredField: "endpointId");

    // Source assertion 11: Business rule assertion: BR-INT-MS12-009
    // @BR-ID: BR-INT-MS12-009
    [Fact(DisplayName = "011: Business rule assertion: BR-INT-MS12-009")]
    [Trait("BR", "BR-INT-MS12-009")]
    public Task Test011_POST_BASE_URL_adapters_refresh_Field_endpointId_200() => AssertShellAsync(
        Method("POST"),
        "/adapters/refresh",
        "{\"moduleType\":{},\"code\":\"phase4c-test\",\"provider\":\"phase4c-test\",\"environment\":\"phase4c-test\",\"configurationRef\":\"phase4c-test\",\"resolvedEndpointUri\":\"https://example.com/phase4c\",\"capabilities\":{},\"timeoutMs\":1,\"maxAttempts\":1,\"config1\":\"phase4c-test\",\"config2\":\"phase4c-test\",\"credentials\":{},\"packageTypes\":[\"phase4c-test\"]}",
        200,
        requiredField: "endpointId");

    // Source assertion 12: Business rule assertion: BR-INT-MS12-010
    // @BR-ID: BR-INT-MS12-010
    [Fact(DisplayName = "012: Business rule assertion: BR-INT-MS12-010")]
    [Trait("BR", "BR-INT-MS12-010")]
    public Task Test012_POST_BASE_URL_adapters_refresh_Field_endpointId_200() => AssertShellAsync(
        Method("POST"),
        "/adapters/refresh",
        "{\"moduleType\":{},\"code\":\"phase4c-test\",\"provider\":\"phase4c-test\",\"environment\":\"phase4c-test\",\"configurationRef\":\"phase4c-test\",\"resolvedEndpointUri\":\"https://example.com/phase4c\",\"capabilities\":{},\"timeoutMs\":1,\"maxAttempts\":1,\"config1\":\"phase4c-test\",\"config2\":\"phase4c-test\",\"credentials\":{},\"packageTypes\":[\"phase4c-test\"]}",
        200,
        requiredField: "endpointId");

    // Source assertion 13: Business rule assertion: BR-INT-MS12-011
    // @BR-ID: BR-INT-MS12-011
    [Fact(DisplayName = "013: Business rule assertion: BR-INT-MS12-011")]
    [Trait("BR", "BR-INT-MS12-011")]
    public Task Test013_POST_BASE_URL_adapters_refresh_Field_endpointId_200() => AssertShellAsync(
        Method("POST"),
        "/adapters/refresh",
        "{\"moduleType\":{},\"code\":\"phase4c-test\",\"provider\":\"phase4c-test\",\"environment\":\"phase4c-test\",\"configurationRef\":\"phase4c-test\",\"resolvedEndpointUri\":\"https://example.com/phase4c\",\"capabilities\":{},\"timeoutMs\":1,\"maxAttempts\":1,\"config1\":\"phase4c-test\",\"config2\":\"phase4c-test\",\"credentials\":{},\"packageTypes\":[\"phase4c-test\"]}",
        200,
        requiredField: "endpointId");

    // Source assertion 14: Business rule assertion: BR-INT-MS12-012
    // @BR-ID: BR-INT-MS12-012
    [Fact(DisplayName = "014: Business rule assertion: BR-INT-MS12-012")]
    [Trait("BR", "BR-INT-MS12-012")]
    public Task Test014_POST_BASE_URL_adapters_refresh_Field_endpointId_200() => AssertShellAsync(
        Method("POST"),
        "/adapters/refresh",
        "{\"moduleType\":{},\"code\":\"phase4c-test\",\"provider\":\"phase4c-test\",\"environment\":\"phase4c-test\",\"configurationRef\":\"phase4c-test\",\"resolvedEndpointUri\":\"https://example.com/phase4c\",\"capabilities\":{},\"timeoutMs\":1,\"maxAttempts\":1,\"config1\":\"phase4c-test\",\"config2\":\"phase4c-test\",\"credentials\":{},\"packageTypes\":[\"phase4c-test\"]}",
        200,
        requiredField: "endpointId");

    // Source assertion 15: Business rule assertion: BR-INT-MS12-013
    // @BR-ID: BR-INT-MS12-013
    [Fact(DisplayName = "015: Business rule assertion: BR-INT-MS12-013")]
    [Trait("BR", "BR-INT-MS12-013")]
    public Task Test015_POST_BASE_URL_adapters_refresh_Field_endpointId_200() => AssertShellAsync(
        Method("POST"),
        "/adapters/refresh",
        "{\"moduleType\":{},\"code\":\"phase4c-test\",\"provider\":\"phase4c-test\",\"environment\":\"phase4c-test\",\"configurationRef\":\"phase4c-test\",\"resolvedEndpointUri\":\"https://example.com/phase4c\",\"capabilities\":{},\"timeoutMs\":1,\"maxAttempts\":1,\"config1\":\"phase4c-test\",\"config2\":\"phase4c-test\",\"credentials\":{},\"packageTypes\":[\"phase4c-test\"]}",
        200,
        requiredField: "endpointId");

    // Source assertion 16: Business rule assertion: BR-INT-MS12-014
    // @BR-ID: BR-INT-MS12-014
    [Fact(DisplayName = "016: Business rule assertion: BR-INT-MS12-014")]
    [Trait("BR", "BR-INT-MS12-014")]
    public Task Test016_POST_BASE_URL_adapters_refresh_Field_endpointId_200() => AssertShellAsync(
        Method("POST"),
        "/adapters/refresh",
        "{\"moduleType\":{},\"code\":\"phase4c-test\",\"provider\":\"phase4c-test\",\"environment\":\"phase4c-test\",\"configurationRef\":\"phase4c-test\",\"resolvedEndpointUri\":\"https://example.com/phase4c\",\"capabilities\":{},\"timeoutMs\":1,\"maxAttempts\":1,\"config1\":\"phase4c-test\",\"config2\":\"phase4c-test\",\"credentials\":{},\"packageTypes\":[\"phase4c-test\"]}",
        200,
        requiredField: "endpointId");

    // Source assertion 17: Business rule assertion: BR-INT-MS12-015
    // @BR-ID: BR-INT-MS12-015
    [Fact(DisplayName = "017: Business rule assertion: BR-INT-MS12-015")]
    [Trait("BR", "BR-INT-MS12-015")]
    public Task Test017_POST_BASE_URL_adapters_refresh_Field_endpointId_200() => AssertShellAsync(
        Method("POST"),
        "/adapters/refresh",
        "{\"moduleType\":{},\"code\":\"phase4c-test\",\"provider\":\"phase4c-test\",\"environment\":\"phase4c-test\",\"configurationRef\":\"phase4c-test\",\"resolvedEndpointUri\":\"https://example.com/phase4c\",\"capabilities\":{},\"timeoutMs\":1,\"maxAttempts\":1,\"config1\":\"phase4c-test\",\"config2\":\"phase4c-test\",\"credentials\":{},\"packageTypes\":[\"phase4c-test\"]}",
        200,
        requiredField: "endpointId");

    // Source assertion 18: Business rule assertion: BR-INT-MS12-016
    // @BR-ID: BR-INT-MS12-016
    [Fact(DisplayName = "018: Business rule assertion: BR-INT-MS12-016")]
    [Trait("BR", "BR-INT-MS12-016")]
    public Task Test018_POST_BASE_URL_adapters_refresh_Field_endpointId_200() => AssertShellAsync(
        Method("POST"),
        "/adapters/refresh",
        "{\"moduleType\":{},\"code\":\"phase4c-test\",\"provider\":\"phase4c-test\",\"environment\":\"phase4c-test\",\"configurationRef\":\"phase4c-test\",\"resolvedEndpointUri\":\"https://example.com/phase4c\",\"capabilities\":{},\"timeoutMs\":1,\"maxAttempts\":1,\"config1\":\"phase4c-test\",\"config2\":\"phase4c-test\",\"credentials\":{},\"packageTypes\":[\"phase4c-test\"]}",
        200,
        requiredField: "endpointId");

    // Source assertion 19: Business rule assertion: BR-INT-MS12-017
    // @BR-ID: BR-INT-MS12-017
    [Fact(DisplayName = "019: Business rule assertion: BR-INT-MS12-017")]
    [Trait("BR", "BR-INT-MS12-017")]
    public Task Test019_POST_BASE_URL_adapters_refresh_Field_endpointId_200() => AssertShellAsync(
        Method("POST"),
        "/adapters/refresh",
        "{\"moduleType\":{},\"code\":\"phase4c-test\",\"provider\":\"phase4c-test\",\"environment\":\"phase4c-test\",\"configurationRef\":\"phase4c-test\",\"resolvedEndpointUri\":\"https://example.com/phase4c\",\"capabilities\":{},\"timeoutMs\":1,\"maxAttempts\":1,\"config1\":\"phase4c-test\",\"config2\":\"phase4c-test\",\"credentials\":{},\"packageTypes\":[\"phase4c-test\"]}",
        200,
        requiredField: "endpointId");

    // Source assertion 20: Business rule assertion: BR-INT-MS12-018
    // @BR-ID: BR-INT-MS12-018
    [Fact(DisplayName = "020: Business rule assertion: BR-INT-MS12-018")]
    [Trait("BR", "BR-INT-MS12-018")]
    public Task Test020_POST_BASE_URL_adapters_refresh_Field_endpointId_200() => AssertShellAsync(
        Method("POST"),
        "/adapters/refresh",
        "{\"moduleType\":{},\"code\":\"phase4c-test\",\"provider\":\"phase4c-test\",\"environment\":\"phase4c-test\",\"configurationRef\":\"phase4c-test\",\"resolvedEndpointUri\":\"https://example.com/phase4c\",\"capabilities\":{},\"timeoutMs\":1,\"maxAttempts\":1,\"config1\":\"phase4c-test\",\"config2\":\"phase4c-test\",\"credentials\":{},\"packageTypes\":[\"phase4c-test\"]}",
        200,
        requiredField: "endpointId");

    // Source assertion 21: Business rule assertion: BR-INT-MS12-019
    // @BR-ID: BR-INT-MS12-019
    [Fact(DisplayName = "021: Business rule assertion: BR-INT-MS12-019")]
    [Trait("BR", "BR-INT-MS12-019")]
    public Task Test021_POST_BASE_URL_adapters_refresh_Field_endpointId_200() => AssertShellAsync(
        Method("POST"),
        "/adapters/refresh",
        "{\"moduleType\":{},\"code\":\"phase4c-test\",\"provider\":\"phase4c-test\",\"environment\":\"phase4c-test\",\"configurationRef\":\"phase4c-test\",\"resolvedEndpointUri\":\"https://example.com/phase4c\",\"capabilities\":{},\"timeoutMs\":1,\"maxAttempts\":1,\"config1\":\"phase4c-test\",\"config2\":\"phase4c-test\",\"credentials\":{},\"packageTypes\":[\"phase4c-test\"]}",
        200,
        requiredField: "endpointId");

    // Source assertion 22: Business rule assertion: BR-INT-MS12-020
    // @BR-ID: BR-INT-MS12-020
    [Fact(DisplayName = "022: Business rule assertion: BR-INT-MS12-020")]
    [Trait("BR", "BR-INT-MS12-020")]
    public Task Test022_POST_BASE_URL_adapters_refresh_Field_endpointId_200() => AssertShellAsync(
        Method("POST"),
        "/adapters/refresh",
        "{\"moduleType\":{},\"code\":\"phase4c-test\",\"provider\":\"phase4c-test\",\"environment\":\"phase4c-test\",\"configurationRef\":\"phase4c-test\",\"resolvedEndpointUri\":\"https://example.com/phase4c\",\"capabilities\":{},\"timeoutMs\":1,\"maxAttempts\":1,\"config1\":\"phase4c-test\",\"config2\":\"phase4c-test\",\"credentials\":{},\"packageTypes\":[\"phase4c-test\"]}",
        200,
        requiredField: "endpointId");

    // Source assertion 23: Business rule assertion: BR-INT-MS12-021
    // @BR-ID: BR-INT-MS12-021
    [Fact(DisplayName = "023: Business rule assertion: BR-INT-MS12-021")]
    [Trait("BR", "BR-INT-MS12-021")]
    public Task Test023_POST_BASE_URL_adapters_refresh_Field_endpointId_200() => AssertShellAsync(
        Method("POST"),
        "/adapters/refresh",
        "{\"moduleType\":{},\"code\":\"phase4c-test\",\"provider\":\"phase4c-test\",\"environment\":\"phase4c-test\",\"configurationRef\":\"phase4c-test\",\"resolvedEndpointUri\":\"https://example.com/phase4c\",\"capabilities\":{},\"timeoutMs\":1,\"maxAttempts\":1,\"config1\":\"phase4c-test\",\"config2\":\"phase4c-test\",\"credentials\":{},\"packageTypes\":[\"phase4c-test\"]}",
        200,
        requiredField: "endpointId");

    // Source assertion 24: Business rule assertion: BR-INT-MS12-022
    // @BR-ID: BR-INT-MS12-022
    [Fact(DisplayName = "024: Business rule assertion: BR-INT-MS12-022")]
    [Trait("BR", "BR-INT-MS12-022")]
    public Task Test024_POST_BASE_URL_adapters_refresh_Field_endpointId_200() => AssertShellAsync(
        Method("POST"),
        "/adapters/refresh",
        "{\"moduleType\":{},\"code\":\"phase4c-test\",\"provider\":\"phase4c-test\",\"environment\":\"phase4c-test\",\"configurationRef\":\"phase4c-test\",\"resolvedEndpointUri\":\"https://example.com/phase4c\",\"capabilities\":{},\"timeoutMs\":1,\"maxAttempts\":1,\"config1\":\"phase4c-test\",\"config2\":\"phase4c-test\",\"credentials\":{},\"packageTypes\":[\"phase4c-test\"]}",
        200,
        requiredField: "endpointId");

    // Source assertion 25: Business rule assertion: BR-INT-MS12-023
    // @BR-ID: BR-INT-MS12-023
    [Fact(DisplayName = "025: Business rule assertion: BR-INT-MS12-023")]
    [Trait("BR", "BR-INT-MS12-023")]
    public Task Test025_POST_BASE_URL_adapters_refresh_Field_endpointId_200() => AssertShellAsync(
        Method("POST"),
        "/adapters/refresh",
        "{\"moduleType\":{},\"code\":\"phase4c-test\",\"provider\":\"phase4c-test\",\"environment\":\"phase4c-test\",\"configurationRef\":\"phase4c-test\",\"resolvedEndpointUri\":\"https://example.com/phase4c\",\"capabilities\":{},\"timeoutMs\":1,\"maxAttempts\":1,\"config1\":\"phase4c-test\",\"config2\":\"phase4c-test\",\"credentials\":{},\"packageTypes\":[\"phase4c-test\"]}",
        200,
        requiredField: "endpointId");

    // Source assertion 26: Contract success: POST /carrier-quotes/ups
    // @BR-ID: BR-INT-MS12-001
    [Fact(DisplayName = "026: Contract success: POST /carrier-quotes/ups")]
    [Trait("BR", "BR-INT-MS12-001")]
    public Task Test026_POST_BASE_URL_carrier_quotes_ups_Field_provider_200() => AssertShellAsync(
        Method("POST"),
        "/carrier-quotes/ups",
        "{\"environment\":\"phase4c-test\",\"origin\":{\"address\":\"phase4c-test\",\"city\":\"phase4c-test\",\"state\":\"phase4c-test\",\"zoneCode\":\"phase4c-test\",\"countryCode\":\"phase4c-test\",\"postalCode\":\"phase4c-test\"},\"destination\":{\"address\":\"phase4c-test\",\"city\":\"phase4c-test\",\"state\":\"phase4c-test\",\"zoneCode\":\"phase4c-test\",\"countryCode\":\"phase4c-test\",\"postalCode\":\"phase4c-test\"},\"packages\":[{\"weight\":10.5,\"weightUnit\":\"KG\",\"length\":10.5,\"width\":10.5,\"height\":10.5,\"dimensionUnit\":\"CM\"}],\"orderTotal\":10.5}",
        200,
        requiredField: "provider");

    // Source assertion 27: Contract error/conformance: POST /carrier-quotes/ups
    // @BR-ID: BR-INT-MS12-001
    [Fact(DisplayName = "027: Contract error/conformance: POST /carrier-quotes/ups")]
    [Trait("BR", "BR-INT-MS12-001")]
    public Task Test027_POST_BASE_URL_carrier_quotes_ups_Status_400() => AssertShellAsync(
        Method("POST"),
        "/carrier-quotes/ups",
        "{}",
        400,
        requiredField: null);

    // Source assertion 28: Contract success: POST /carrier-quotes/usps
    // @BR-ID: BR-INT-MS12-001
    [Fact(DisplayName = "028: Contract success: POST /carrier-quotes/usps")]
    [Trait("BR", "BR-INT-MS12-001")]
    public Task Test028_POST_BASE_URL_carrier_quotes_usps_Field_provider_200() => AssertShellAsync(
        Method("POST"),
        "/carrier-quotes/usps",
        "{\"environment\":\"phase4c-test\",\"origin\":{\"address\":\"phase4c-test\",\"city\":\"phase4c-test\",\"state\":\"phase4c-test\",\"zoneCode\":\"phase4c-test\",\"countryCode\":\"phase4c-test\",\"postalCode\":\"phase4c-test\"},\"destination\":{\"address\":\"phase4c-test\",\"city\":\"phase4c-test\",\"state\":\"phase4c-test\",\"zoneCode\":\"phase4c-test\",\"countryCode\":\"phase4c-test\",\"postalCode\":\"phase4c-test\"},\"packages\":[{\"weight\":10.5,\"weightUnit\":\"KG\",\"length\":10.5,\"width\":10.5,\"height\":10.5,\"dimensionUnit\":\"CM\"}],\"orderTotal\":10.5}",
        200,
        requiredField: "provider");

    // Source assertion 29: Contract error/conformance: POST /carrier-quotes/usps
    // @BR-ID: BR-INT-MS12-001
    [Fact(DisplayName = "029: Contract error/conformance: POST /carrier-quotes/usps")]
    [Trait("BR", "BR-INT-MS12-001")]
    public Task Test029_POST_BASE_URL_carrier_quotes_usps_Status_400() => AssertShellAsync(
        Method("POST"),
        "/carrier-quotes/usps",
        "{}",
        400,
        requiredField: null);

    // Source assertion 30: Contract success: POST /delivery-attempts/{attemptId}/replay
    // @BR-ID: BR-INT-MS12-001
    [Fact(DisplayName = "030: Contract success: POST /delivery-attempts/{attemptId}/replay")]
    [Trait("BR", "BR-INT-MS12-001")]
    public Task Test030_POST_BASE_URL_delivery_attempts_ById_replay_Field_attemptId_202() => AssertShellAsync(
        Method("POST"),
        $"/delivery-attempts/{ResourceId}/replay",
        "{\"reason\":\"phase4c-test\"}",
        202,
        requiredField: "attemptId");

    // Source assertion 31: Contract error/conformance: POST /delivery-attempts/{attemptId}/replay
    // @BR-ID: BR-INT-MS12-001
    [Fact(DisplayName = "031: Contract error/conformance: POST /delivery-attempts/{attemptId}/replay")]
    [Trait("BR", "BR-INT-MS12-001")]
    public Task Test031_POST_BASE_URL_delivery_attempts_ById_replay_Status_400() => AssertShellAsync(
        Method("POST"),
        $"/delivery-attempts/{ResourceId}/replay",
        "{}",
        400,
        requiredField: null);

    // Source assertion 32: Contract success: POST /emails
    // @BR-ID: BR-INT-MS12-001
    [Fact(DisplayName = "032: Contract success: POST /emails")]
    [Trait("BR", "BR-INT-MS12-001")]
    public Task Test032_POST_BASE_URL_emails_Field_messageId_202() => AssertShellAsync(
        Method("POST"),
        "/emails",
        "{\"idempotencyKey\":\"phase4c-test\",\"templateKey\":\"phase4c-test\",\"locale\":\"phase4c-test\",\"recipientEmail\":\"phase4c@example.com\",\"senderEmail\":\"phase4c@example.com\",\"senderName\":\"phase4c-test\",\"subject\":\"phase4c-test\",\"tokenPayload\":{},\"orderReference\":\"phase4c-test\"}",
        202,
        requiredField: "messageId");

    // Source assertion 33: Contract error/conformance: POST /emails
    // @BR-ID: BR-INT-MS12-001
    [Fact(DisplayName = "033: Contract error/conformance: POST /emails")]
    [Trait("BR", "BR-INT-MS12-001")]
    public Task Test033_POST_BASE_URL_emails_Status_400() => AssertShellAsync(
        Method("POST"),
        "/emails",
        "{}",
        400,
        requiredField: null);

    // Source assertion 34: Contract success: POST /files
    // @BR-ID: BR-INT-MS12-001
    [Fact(DisplayName = "034: Contract success: POST /files")]
    [Trait("BR", "BR-INT-MS12-001")]
    public Task Test034_POST_BASE_URL_files_Status_201() => AssertShellAsync(
        Method("POST"),
        "/files",
        "{\"storeCode\":\"phase4c-test\",\"contentType\":{},\"folderPath\":\"phase4c-test\",\"fileName\":\"phase4c-test\",\"mimeType\":\"phase4c-test\",\"contentBase64\":\"phase4c-test\",\"idempotencyKey\":\"phase4c-test\"}",
        201,
        requiredField: null);

    // Source assertion 35: Contract error/conformance: POST /files
    // @BR-ID: BR-INT-MS12-001
    [Fact(DisplayName = "035: Contract error/conformance: POST /files")]
    [Trait("BR", "BR-INT-MS12-001")]
    public Task Test035_POST_BASE_URL_files_Status_400() => AssertShellAsync(
        Method("POST"),
        "/files",
        "{}",
        400,
        requiredField: null);

    // Source assertion 36: Contract success: POST /files/batch
    // @BR-ID: BR-INT-MS12-001
    [Fact(DisplayName = "036: Contract success: POST /files/batch")]
    [Trait("BR", "BR-INT-MS12-001")]
    public Task Test036_POST_BASE_URL_files_batch_Field_items_201() => AssertShellAsync(
        Method("POST"),
        "/files/batch",
        "{\"storeCode\":\"phase4c-test\",\"folderPath\":\"phase4c-test\",\"idempotencyKey\":\"phase4c-test\",\"files\":[{\"contentType\":{},\"fileName\":\"phase4c-test\",\"mimeType\":\"phase4c-test\",\"contentBase64\":\"phase4c-test\"}]}",
        201,
        requiredField: "items");

    // Source assertion 37: Contract error/conformance: POST /files/batch
    // @BR-ID: BR-INT-MS12-001
    [Fact(DisplayName = "037: Contract error/conformance: POST /files/batch")]
    [Trait("BR", "BR-INT-MS12-001")]
    public Task Test037_POST_BASE_URL_files_batch_Status_400() => AssertShellAsync(
        Method("POST"),
        "/files/batch",
        "{}",
        400,
        requiredField: null);

    // Source assertion 38: Contract success: POST /files/folders
    // @BR-ID: BR-INT-MS12-001
    [Fact(DisplayName = "038: Contract success: POST /files/folders")]
    [Trait("BR", "BR-INT-MS12-001")]
    public Task Test038_POST_BASE_URL_files_folders_Field_path_201() => AssertShellAsync(
        Method("POST"),
        "/files/folders",
        "{\"storeCode\":\"phase4c-test\",\"provider\":{},\"folderPath\":\"phase4c-test\",\"folderName\":\"phase4c-test\"}",
        201,
        requiredField: "path");

    // Source assertion 39: Contract error/conformance: POST /files/folders
    // @BR-ID: BR-INT-MS12-001
    [Fact(DisplayName = "039: Contract error/conformance: POST /files/folders")]
    [Trait("BR", "BR-INT-MS12-001")]
    public Task Test039_POST_BASE_URL_files_folders_Status_400() => AssertShellAsync(
        Method("POST"),
        "/files/folders",
        "{}",
        400,
        requiredField: null);

    // Source assertion 40: Contract success: POST /geolocation/ip
    // @BR-ID: BR-INT-MS12-001
    [Fact(DisplayName = "040: Contract success: POST /geolocation/ip")]
    [Trait("BR", "BR-INT-MS12-001")]
    public Task Test040_POST_BASE_URL_geolocation_ip_Field_resolved_200() => AssertShellAsync(
        Method("POST"),
        "/geolocation/ip",
        "{\"ipAddress\":\"phase4c-test\"}",
        200,
        requiredField: "resolved");

    // Source assertion 41: Contract error/conformance: POST /geolocation/ip
    // @BR-ID: BR-INT-MS12-001
    [Fact(DisplayName = "041: Contract error/conformance: POST /geolocation/ip")]
    [Trait("BR", "BR-INT-MS12-001")]
    public Task Test041_POST_BASE_URL_geolocation_ip_Status_400() => AssertShellAsync(
        Method("POST"),
        "/geolocation/ip",
        "{}",
        400,
        requiredField: null);

    // Source assertion 42: Contract success: POST /maps/distance
    // @BR-ID: BR-INT-MS12-001
    [Fact(DisplayName = "042: Contract success: POST /maps/distance")]
    [Trait("BR", "BR-INT-MS12-001")]
    public Task Test042_POST_BASE_URL_maps_distance_Field_enriched_200() => AssertShellAsync(
        Method("POST"),
        "/maps/distance",
        "{\"origin\":{\"address\":\"phase4c-test\",\"city\":\"phase4c-test\",\"state\":\"phase4c-test\",\"zoneCode\":\"phase4c-test\",\"countryCode\":\"phase4c-test\",\"postalCode\":\"phase4c-test\"},\"destination\":{\"address\":\"phase4c-test\",\"city\":\"phase4c-test\",\"state\":\"phase4c-test\",\"zoneCode\":\"phase4c-test\",\"countryCode\":\"phase4c-test\",\"postalCode\":\"phase4c-test\"},\"allowedZoneCodes\":[\"phase4c-test\"]}",
        200,
        requiredField: "enriched");

    // Source assertion 43: Contract error/conformance: POST /maps/distance
    // @BR-ID: BR-INT-MS12-001
    [Fact(DisplayName = "043: Contract error/conformance: POST /maps/distance")]
    [Trait("BR", "BR-INT-MS12-001")]
    public Task Test043_POST_BASE_URL_maps_distance_Status_400() => AssertShellAsync(
        Method("POST"),
        "/maps/distance",
        "{}",
        400,
        requiredField: null);

    // Source assertion 44: Contract success: GET /adapters
    // @BR-ID: BR-INT-MS12-001
    [Fact(DisplayName = "044: Contract success: GET /adapters")]
    [Trait("BR", "BR-INT-MS12-001")]
    public Task Test044_GET_BASE_URL_adapters_Field_items_200() => AssertShellAsync(
        Method("GET"),
        "/adapters",
        null,
        200,
        requiredField: "items");

    // Source assertion 45: Contract error/conformance: GET /adapters
    // @BR-ID: BR-INT-MS12-001
    [Fact(DisplayName = "045: Contract error/conformance: GET /adapters")]
    [Trait("BR", "BR-INT-MS12-001")]
    public Task Test045_GET_BASE_URL_adapters_Status_401() => AssertShellAsync(
        Method("GET"),
        "/adapters",
        null,
        401,
        requiredField: null);

    // Source assertion 46: Contract success: GET /delivery-attempts/{attemptId}
    // @BR-ID: BR-INT-MS12-001
    [Fact(DisplayName = "046: Contract success: GET /delivery-attempts/{attemptId}")]
    [Trait("BR", "BR-INT-MS12-001")]
    public Task Test046_GET_BASE_URL_delivery_attempts_ById_Field_attemptId_200() => AssertShellAsync(
        Method("GET"),
        $"/delivery-attempts/{ResourceId}",
        null,
        200,
        requiredField: "attemptId");

    // Source assertion 47: Contract error/conformance: GET /delivery-attempts/{attemptId}
    // @BR-ID: BR-INT-MS12-001
    [Fact(DisplayName = "047: Contract error/conformance: GET /delivery-attempts/{attemptId}")]
    [Trait("BR", "BR-INT-MS12-001")]
    public Task Test047_GET_BASE_URL_delivery_attempts_ById_Status_400() => AssertShellAsync(
        Method("GET"),
        $"/delivery-attempts/{ResourceId}",
        null,
        400,
        requiredField: null);

    // Source assertion 48: Contract success: GET /files
    // @BR-ID: BR-INT-MS12-001
    [Fact(DisplayName = "048: Contract success: GET /files")]
    [Trait("BR", "BR-INT-MS12-001")]
    public Task Test048_GET_BASE_URL_files_Field_items_200() => AssertShellAsync(
        Method("GET"),
        "/files",
        null,
        200,
        requiredField: "items");

    // Source assertion 49: Contract error/conformance: GET /files
    // @BR-ID: BR-INT-MS12-001
    [Fact(DisplayName = "049: Contract error/conformance: GET /files")]
    [Trait("BR", "BR-INT-MS12-001")]
    public Task Test049_GET_BASE_URL_files_Status_400() => AssertShellAsync(
        Method("GET"),
        "/files",
        null,
        400,
        requiredField: null);

    // Source assertion 50: Contract success: GET /files/folders
    // @BR-ID: BR-INT-MS12-001
    [Fact(DisplayName = "050: Contract success: GET /files/folders")]
    [Trait("BR", "BR-INT-MS12-001")]
    public Task Test050_GET_BASE_URL_files_folders_Field_items_200() => AssertShellAsync(
        Method("GET"),
        "/files/folders",
        null,
        200,
        requiredField: "items");

    // Source assertion 51: Contract error/conformance: GET /files/folders
    // @BR-ID: BR-INT-MS12-001
    [Fact(DisplayName = "051: Contract error/conformance: GET /files/folders")]
    [Trait("BR", "BR-INT-MS12-001")]
    public Task Test051_GET_BASE_URL_files_folders_Status_400() => AssertShellAsync(
        Method("GET"),
        "/files/folders",
        null,
        400,
        requiredField: null);

    // Source assertion 52: Contract success: GET /files/{fileName}
    // @BR-ID: BR-INT-MS12-001
    [Fact(DisplayName = "052: Contract success: GET /files/{fileName}")]
    [Trait("BR", "BR-INT-MS12-001")]
    public Task Test052_GET_BASE_URL_files_phase4c_value_Field_fileName_200() => AssertShellAsync(
        Method("GET"),
        "/files/phase4c-value",
        null,
        200,
        requiredField: "fileName");

    // Source assertion 53: Contract error/conformance: GET /files/{fileName}
    // @BR-ID: BR-INT-MS12-001
    [Fact(DisplayName = "053: Contract error/conformance: GET /files/{fileName}")]
    [Trait("BR", "BR-INT-MS12-001")]
    public Task Test053_GET_BASE_URL_files_phase4c_value_Status_400() => AssertShellAsync(
        Method("GET"),
        "/files/phase4c-value",
        null,
        400,
        requiredField: null);

    // Source assertion 54: Contract success: DELETE /files
    // @BR-ID: BR-INT-MS12-001
    [Fact(DisplayName = "054: Contract success: DELETE /files")]
    [Trait("BR", "BR-INT-MS12-001")]
    public Task Test054_DELETE_BASE_URL_files_Status_204() => AssertShellAsync(
        Method("DELETE"),
        "/files",
        null,
        204,
        requiredField: null);

    // Source assertion 55: Contract error/conformance: DELETE /files
    // @BR-ID: BR-INT-MS12-001
    [Fact(DisplayName = "055: Contract error/conformance: DELETE /files")]
    [Trait("BR", "BR-INT-MS12-001")]
    public Task Test055_DELETE_BASE_URL_files_Status_400() => AssertShellAsync(
        Method("DELETE"),
        "/files",
        null,
        400,
        requiredField: null);

    // Source assertion 56: Contract success: DELETE /files/folders
    // @BR-ID: BR-INT-MS12-001
    [Fact(DisplayName = "056: Contract success: DELETE /files/folders")]
    [Trait("BR", "BR-INT-MS12-001")]
    public Task Test056_DELETE_BASE_URL_files_folders_Status_204() => AssertShellAsync(
        Method("DELETE"),
        "/files/folders",
        null,
        204,
        requiredField: null);

    // Source assertion 57: Contract error/conformance: DELETE /files/folders
    // @BR-ID: BR-INT-MS12-001
    [Fact(DisplayName = "057: Contract error/conformance: DELETE /files/folders")]
    [Trait("BR", "BR-INT-MS12-001")]
    public Task Test057_DELETE_BASE_URL_files_folders_Status_400() => AssertShellAsync(
        Method("DELETE"),
        "/files/folders",
        null,
        400,
        requiredField: null);

    // Source assertion 58: Contract success: DELETE /files/{fileName}
    // @BR-ID: BR-INT-MS12-001
    [Fact(DisplayName = "058: Contract success: DELETE /files/{fileName}")]
    [Trait("BR", "BR-INT-MS12-001")]
    public Task Test058_DELETE_BASE_URL_files_phase4c_value_Status_204() => AssertShellAsync(
        Method("DELETE"),
        "/files/phase4c-value",
        null,
        204,
        requiredField: null);

    // Source assertion 59: Contract error/conformance: DELETE /files/{fileName}
    // @BR-ID: BR-INT-MS12-001
    [Fact(DisplayName = "059: Contract error/conformance: DELETE /files/{fileName}")]
    [Trait("BR", "BR-INT-MS12-001")]
    public Task Test059_DELETE_BASE_URL_files_phase4c_value_Status_400() => AssertShellAsync(
        Method("DELETE"),
        "/files/phase4c-value",
        null,
        400,
        requiredField: null);
}
