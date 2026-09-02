namespace Shopizer.IntegrationTests;

[Collection(ShopizerAspireCollection.Name)]
public sealed class TaxComprehensiveTests(AspireHostFixture fixture) : ComprehensiveTestBase(fixture.TaxClient)
{

    // Source assertion 1: Contract success: POST /tax-calculations
    // @BR-ID: BR-TAX-CFG-001
    [Fact(DisplayName = "001: Contract success: POST /tax-calculations")]
    [Trait("BR", "BR-TAX-CFG-001")]
    public Task Test001_POST_BASE_URL_tax_calculations_Field_quoteId_200() => AssertShellAsync(
        Method("POST"),
        "/tax-calculations",
        "{\"currencyCode\":\"phase4c-test\",\"customerId\":\"00000000-0000-0000-0000-000000000001\",\"orderId\":\"00000000-0000-0000-0000-000000000001\",\"billingAddress\":{\"countryCode\":\"phase4c-test\",\"zoneCode\":\"phase4c-test\",\"stateProvince\":\"phase4c-test\"},\"shippingAddress\":\"phase4c-test\",\"items\":[{\"productId\":\"00000000-0000-0000-0000-000000000001\",\"quantity\":1,\"unitAmount\":10.5,\"taxClassCode\":\"phase4c-test\"}],\"shipping\":\"phase4c-test\",\"languageCode\":\"phase4c-test\",\"idempotencyKey\":\"phase4c-test\"}",
        200,
        requiredField: "quoteId");

    // Source assertion 2: Contract error/conformance: POST /tax-calculations
    // @BR-ID: BR-TAX-CFG-001
    [Fact(DisplayName = "002: Contract error/conformance: POST /tax-calculations")]
    [Trait("BR", "BR-TAX-CFG-001")]
    public Task Test002_POST_BASE_URL_tax_calculations_Status_400() => AssertShellAsync(
        Method("POST"),
        "/tax-calculations",
        "{}",
        400,
        requiredField: null);

    // Source assertion 3: Business rule assertion: BR-TAX-CFG-001
    // @BR-ID: BR-TAX-CFG-001
    [Fact(DisplayName = "003: Business rule assertion: BR-TAX-CFG-001")]
    [Trait("BR", "BR-TAX-CFG-001")]
    public Task Test003_POST_BASE_URL_tax_calculations_Field_quoteId_200() => AssertShellAsync(
        Method("POST"),
        "/tax-calculations",
        "{\"currencyCode\":\"phase4c-test\",\"customerId\":\"00000000-0000-0000-0000-000000000001\",\"orderId\":\"00000000-0000-0000-0000-000000000001\",\"billingAddress\":{\"countryCode\":\"phase4c-test\",\"zoneCode\":\"phase4c-test\",\"stateProvince\":\"phase4c-test\"},\"shippingAddress\":\"phase4c-test\",\"items\":[{\"productId\":\"00000000-0000-0000-0000-000000000001\",\"quantity\":1,\"unitAmount\":10.5,\"taxClassCode\":\"phase4c-test\"}],\"shipping\":\"phase4c-test\",\"languageCode\":\"phase4c-test\",\"idempotencyKey\":\"phase4c-test\"}",
        200,
        requiredField: "quoteId");

    // Source assertion 4: Business rule assertion: BR-TAX-CFG-002
    // @BR-ID: BR-TAX-CFG-002
    [Fact(DisplayName = "004: Business rule assertion: BR-TAX-CFG-002")]
    [Trait("BR", "BR-TAX-CFG-002")]
    public Task Test004_POST_BASE_URL_tax_calculations_Field_quoteId_200() => AssertShellAsync(
        Method("POST"),
        "/tax-calculations",
        "{\"currencyCode\":\"phase4c-test\",\"customerId\":\"00000000-0000-0000-0000-000000000001\",\"orderId\":\"00000000-0000-0000-0000-000000000001\",\"billingAddress\":{\"countryCode\":\"phase4c-test\",\"zoneCode\":\"phase4c-test\",\"stateProvince\":\"phase4c-test\"},\"shippingAddress\":\"phase4c-test\",\"items\":[{\"productId\":\"00000000-0000-0000-0000-000000000001\",\"quantity\":1,\"unitAmount\":10.5,\"taxClassCode\":\"phase4c-test\"}],\"shipping\":\"phase4c-test\",\"languageCode\":\"phase4c-test\",\"idempotencyKey\":\"phase4c-test\"}",
        200,
        requiredField: "quoteId");

    // Source assertion 5: Business rule assertion: BR-TAX-CLS-001
    // @BR-ID: BR-TAX-CLS-001
    [Fact(DisplayName = "005: Business rule assertion: BR-TAX-CLS-001")]
    [Trait("BR", "BR-TAX-CLS-001")]
    public Task Test005_POST_BASE_URL_tax_calculations_Field_quoteId_200() => AssertShellAsync(
        Method("POST"),
        "/tax-calculations",
        "{\"currencyCode\":\"phase4c-test\",\"customerId\":\"00000000-0000-0000-0000-000000000001\",\"orderId\":\"00000000-0000-0000-0000-000000000001\",\"billingAddress\":{\"countryCode\":\"phase4c-test\",\"zoneCode\":\"phase4c-test\",\"stateProvince\":\"phase4c-test\"},\"shippingAddress\":\"phase4c-test\",\"items\":[{\"productId\":\"00000000-0000-0000-0000-000000000001\",\"quantity\":1,\"unitAmount\":10.5,\"taxClassCode\":\"phase4c-test\"}],\"shipping\":\"phase4c-test\",\"languageCode\":\"phase4c-test\",\"idempotencyKey\":\"phase4c-test\"}",
        200,
        requiredField: "quoteId");

    // Source assertion 6: Business rule assertion: BR-TAX-CLS-002
    // @BR-ID: BR-TAX-CLS-002
    [Fact(DisplayName = "006: Business rule assertion: BR-TAX-CLS-002")]
    [Trait("BR", "BR-TAX-CLS-002")]
    public Task Test006_POST_BASE_URL_tax_calculations_Field_quoteId_200() => AssertShellAsync(
        Method("POST"),
        "/tax-calculations",
        "{\"currencyCode\":\"phase4c-test\",\"customerId\":\"00000000-0000-0000-0000-000000000001\",\"orderId\":\"00000000-0000-0000-0000-000000000001\",\"billingAddress\":{\"countryCode\":\"phase4c-test\",\"zoneCode\":\"phase4c-test\",\"stateProvince\":\"phase4c-test\"},\"shippingAddress\":\"phase4c-test\",\"items\":[{\"productId\":\"00000000-0000-0000-0000-000000000001\",\"quantity\":1,\"unitAmount\":10.5,\"taxClassCode\":\"phase4c-test\"}],\"shipping\":\"phase4c-test\",\"languageCode\":\"phase4c-test\",\"idempotencyKey\":\"phase4c-test\"}",
        200,
        requiredField: "quoteId");

    // Source assertion 7: Business rule assertion: BR-TAX-CLS-003
    // @BR-ID: BR-TAX-CLS-003
    [Fact(DisplayName = "007: Business rule assertion: BR-TAX-CLS-003")]
    [Trait("BR", "BR-TAX-CLS-003")]
    public Task Test007_POST_BASE_URL_tax_calculations_Field_quoteId_200() => AssertShellAsync(
        Method("POST"),
        "/tax-calculations",
        "{\"currencyCode\":\"phase4c-test\",\"customerId\":\"00000000-0000-0000-0000-000000000001\",\"orderId\":\"00000000-0000-0000-0000-000000000001\",\"billingAddress\":{\"countryCode\":\"phase4c-test\",\"zoneCode\":\"phase4c-test\",\"stateProvince\":\"phase4c-test\"},\"shippingAddress\":\"phase4c-test\",\"items\":[{\"productId\":\"00000000-0000-0000-0000-000000000001\",\"quantity\":1,\"unitAmount\":10.5,\"taxClassCode\":\"phase4c-test\"}],\"shipping\":\"phase4c-test\",\"languageCode\":\"phase4c-test\",\"idempotencyKey\":\"phase4c-test\"}",
        200,
        requiredField: "quoteId");

    // Source assertion 8: Business rule assertion: BR-TAX-RAT-001
    // @BR-ID: BR-TAX-RAT-001
    [Fact(DisplayName = "008: Business rule assertion: BR-TAX-RAT-001")]
    [Trait("BR", "BR-TAX-RAT-001")]
    public Task Test008_POST_BASE_URL_tax_calculations_Field_quoteId_200() => AssertShellAsync(
        Method("POST"),
        "/tax-calculations",
        "{\"currencyCode\":\"phase4c-test\",\"customerId\":\"00000000-0000-0000-0000-000000000001\",\"orderId\":\"00000000-0000-0000-0000-000000000001\",\"billingAddress\":{\"countryCode\":\"phase4c-test\",\"zoneCode\":\"phase4c-test\",\"stateProvince\":\"phase4c-test\"},\"shippingAddress\":\"phase4c-test\",\"items\":[{\"productId\":\"00000000-0000-0000-0000-000000000001\",\"quantity\":1,\"unitAmount\":10.5,\"taxClassCode\":\"phase4c-test\"}],\"shipping\":\"phase4c-test\",\"languageCode\":\"phase4c-test\",\"idempotencyKey\":\"phase4c-test\"}",
        200,
        requiredField: "quoteId");

    // Source assertion 9: Business rule assertion: BR-TAX-RAT-002
    // @BR-ID: BR-TAX-RAT-002
    [Fact(DisplayName = "009: Business rule assertion: BR-TAX-RAT-002")]
    [Trait("BR", "BR-TAX-RAT-002")]
    public Task Test009_POST_BASE_URL_tax_calculations_Field_quoteId_200() => AssertShellAsync(
        Method("POST"),
        "/tax-calculations",
        "{\"currencyCode\":\"phase4c-test\",\"customerId\":\"00000000-0000-0000-0000-000000000001\",\"orderId\":\"00000000-0000-0000-0000-000000000001\",\"billingAddress\":{\"countryCode\":\"phase4c-test\",\"zoneCode\":\"phase4c-test\",\"stateProvince\":\"phase4c-test\"},\"shippingAddress\":\"phase4c-test\",\"items\":[{\"productId\":\"00000000-0000-0000-0000-000000000001\",\"quantity\":1,\"unitAmount\":10.5,\"taxClassCode\":\"phase4c-test\"}],\"shipping\":\"phase4c-test\",\"languageCode\":\"phase4c-test\",\"idempotencyKey\":\"phase4c-test\"}",
        200,
        requiredField: "quoteId");

    // Source assertion 10: Business rule assertion: BR-TAX-RAT-003
    // @BR-ID: BR-TAX-RAT-003
    [Fact(DisplayName = "010: Business rule assertion: BR-TAX-RAT-003")]
    [Trait("BR", "BR-TAX-RAT-003")]
    public Task Test010_POST_BASE_URL_tax_calculations_Field_quoteId_200() => AssertShellAsync(
        Method("POST"),
        "/tax-calculations",
        "{\"currencyCode\":\"phase4c-test\",\"customerId\":\"00000000-0000-0000-0000-000000000001\",\"orderId\":\"00000000-0000-0000-0000-000000000001\",\"billingAddress\":{\"countryCode\":\"phase4c-test\",\"zoneCode\":\"phase4c-test\",\"stateProvince\":\"phase4c-test\"},\"shippingAddress\":\"phase4c-test\",\"items\":[{\"productId\":\"00000000-0000-0000-0000-000000000001\",\"quantity\":1,\"unitAmount\":10.5,\"taxClassCode\":\"phase4c-test\"}],\"shipping\":\"phase4c-test\",\"languageCode\":\"phase4c-test\",\"idempotencyKey\":\"phase4c-test\"}",
        200,
        requiredField: "quoteId");

    // Source assertion 11: Business rule assertion: BR-TAX-RAT-004
    // @BR-ID: BR-TAX-RAT-004
    [Fact(DisplayName = "011: Business rule assertion: BR-TAX-RAT-004")]
    [Trait("BR", "BR-TAX-RAT-004")]
    public Task Test011_POST_BASE_URL_tax_calculations_Field_quoteId_200() => AssertShellAsync(
        Method("POST"),
        "/tax-calculations",
        "{\"currencyCode\":\"phase4c-test\",\"customerId\":\"00000000-0000-0000-0000-000000000001\",\"orderId\":\"00000000-0000-0000-0000-000000000001\",\"billingAddress\":{\"countryCode\":\"phase4c-test\",\"zoneCode\":\"phase4c-test\",\"stateProvince\":\"phase4c-test\"},\"shippingAddress\":\"phase4c-test\",\"items\":[{\"productId\":\"00000000-0000-0000-0000-000000000001\",\"quantity\":1,\"unitAmount\":10.5,\"taxClassCode\":\"phase4c-test\"}],\"shipping\":\"phase4c-test\",\"languageCode\":\"phase4c-test\",\"idempotencyKey\":\"phase4c-test\"}",
        200,
        requiredField: "quoteId");

    // Source assertion 12: Business rule assertion: BR-TAX-RAT-005
    // @BR-ID: BR-TAX-RAT-005
    [Fact(DisplayName = "012: Business rule assertion: BR-TAX-RAT-005")]
    [Trait("BR", "BR-TAX-RAT-005")]
    public Task Test012_POST_BASE_URL_tax_calculations_Field_quoteId_200() => AssertShellAsync(
        Method("POST"),
        "/tax-calculations",
        "{\"currencyCode\":\"phase4c-test\",\"customerId\":\"00000000-0000-0000-0000-000000000001\",\"orderId\":\"00000000-0000-0000-0000-000000000001\",\"billingAddress\":{\"countryCode\":\"phase4c-test\",\"zoneCode\":\"phase4c-test\",\"stateProvince\":\"phase4c-test\"},\"shippingAddress\":\"phase4c-test\",\"items\":[{\"productId\":\"00000000-0000-0000-0000-000000000001\",\"quantity\":1,\"unitAmount\":10.5,\"taxClassCode\":\"phase4c-test\"}],\"shipping\":\"phase4c-test\",\"languageCode\":\"phase4c-test\",\"idempotencyKey\":\"phase4c-test\"}",
        200,
        requiredField: "quoteId");

    // Source assertion 13: Business rule assertion: BR-TAX-CAL-001
    // @BR-ID: BR-TAX-CAL-001
    [Fact(DisplayName = "013: Business rule assertion: BR-TAX-CAL-001")]
    [Trait("BR", "BR-TAX-CAL-001")]
    public Task Test013_POST_BASE_URL_tax_calculations_Field_quoteId_200() => AssertShellAsync(
        Method("POST"),
        "/tax-calculations",
        "{\"currencyCode\":\"phase4c-test\",\"customerId\":\"00000000-0000-0000-0000-000000000001\",\"orderId\":\"00000000-0000-0000-0000-000000000001\",\"billingAddress\":{\"countryCode\":\"phase4c-test\",\"zoneCode\":\"phase4c-test\",\"stateProvince\":\"phase4c-test\"},\"shippingAddress\":\"phase4c-test\",\"items\":[{\"productId\":\"00000000-0000-0000-0000-000000000001\",\"quantity\":1,\"unitAmount\":10.5,\"taxClassCode\":\"phase4c-test\"}],\"shipping\":\"phase4c-test\",\"languageCode\":\"phase4c-test\",\"idempotencyKey\":\"phase4c-test\"}",
        200,
        requiredField: "quoteId");

    // Source assertion 14: Business rule assertion: BR-TAX-CAL-002
    // @BR-ID: BR-TAX-CAL-002
    [Fact(DisplayName = "014: Business rule assertion: BR-TAX-CAL-002")]
    [Trait("BR", "BR-TAX-CAL-002")]
    public Task Test014_POST_BASE_URL_tax_calculations_Field_quoteId_200() => AssertShellAsync(
        Method("POST"),
        "/tax-calculations",
        "{\"currencyCode\":\"phase4c-test\",\"customerId\":\"00000000-0000-0000-0000-000000000001\",\"orderId\":\"00000000-0000-0000-0000-000000000001\",\"billingAddress\":{\"countryCode\":\"phase4c-test\",\"zoneCode\":\"phase4c-test\",\"stateProvince\":\"phase4c-test\"},\"shippingAddress\":\"phase4c-test\",\"items\":[{\"productId\":\"00000000-0000-0000-0000-000000000001\",\"quantity\":1,\"unitAmount\":10.5,\"taxClassCode\":\"phase4c-test\"}],\"shipping\":\"phase4c-test\",\"languageCode\":\"phase4c-test\",\"idempotencyKey\":\"phase4c-test\"}",
        200,
        requiredField: "quoteId");

    // Source assertion 15: Business rule assertion: BR-TAX-CAL-003
    // @BR-ID: BR-TAX-CAL-003
    [Fact(DisplayName = "015: Business rule assertion: BR-TAX-CAL-003")]
    [Trait("BR", "BR-TAX-CAL-003")]
    public Task Test015_POST_BASE_URL_tax_calculations_Field_quoteId_200() => AssertShellAsync(
        Method("POST"),
        "/tax-calculations",
        "{\"currencyCode\":\"phase4c-test\",\"customerId\":\"00000000-0000-0000-0000-000000000001\",\"orderId\":\"00000000-0000-0000-0000-000000000001\",\"billingAddress\":{\"countryCode\":\"phase4c-test\",\"zoneCode\":\"phase4c-test\",\"stateProvince\":\"phase4c-test\"},\"shippingAddress\":\"phase4c-test\",\"items\":[{\"productId\":\"00000000-0000-0000-0000-000000000001\",\"quantity\":1,\"unitAmount\":10.5,\"taxClassCode\":\"phase4c-test\"}],\"shipping\":\"phase4c-test\",\"languageCode\":\"phase4c-test\",\"idempotencyKey\":\"phase4c-test\"}",
        200,
        requiredField: "quoteId");

    // Source assertion 16: Business rule assertion: BR-TAX-CAL-004
    // @BR-ID: BR-TAX-CAL-004
    [Fact(DisplayName = "016: Business rule assertion: BR-TAX-CAL-004")]
    [Trait("BR", "BR-TAX-CAL-004")]
    public Task Test016_POST_BASE_URL_tax_calculations_Field_quoteId_200() => AssertShellAsync(
        Method("POST"),
        "/tax-calculations",
        "{\"currencyCode\":\"phase4c-test\",\"customerId\":\"00000000-0000-0000-0000-000000000001\",\"orderId\":\"00000000-0000-0000-0000-000000000001\",\"billingAddress\":{\"countryCode\":\"phase4c-test\",\"zoneCode\":\"phase4c-test\",\"stateProvince\":\"phase4c-test\"},\"shippingAddress\":\"phase4c-test\",\"items\":[{\"productId\":\"00000000-0000-0000-0000-000000000001\",\"quantity\":1,\"unitAmount\":10.5,\"taxClassCode\":\"phase4c-test\"}],\"shipping\":\"phase4c-test\",\"languageCode\":\"phase4c-test\",\"idempotencyKey\":\"phase4c-test\"}",
        200,
        requiredField: "quoteId");

    // Source assertion 17: Business rule assertion: BR-TAX-CAL-005
    // @BR-ID: BR-TAX-CAL-005
    [Fact(DisplayName = "017: Business rule assertion: BR-TAX-CAL-005")]
    [Trait("BR", "BR-TAX-CAL-005")]
    public Task Test017_POST_BASE_URL_tax_calculations_Field_quoteId_200() => AssertShellAsync(
        Method("POST"),
        "/tax-calculations",
        "{\"currencyCode\":\"phase4c-test\",\"customerId\":\"00000000-0000-0000-0000-000000000001\",\"orderId\":\"00000000-0000-0000-0000-000000000001\",\"billingAddress\":{\"countryCode\":\"phase4c-test\",\"zoneCode\":\"phase4c-test\",\"stateProvince\":\"phase4c-test\"},\"shippingAddress\":\"phase4c-test\",\"items\":[{\"productId\":\"00000000-0000-0000-0000-000000000001\",\"quantity\":1,\"unitAmount\":10.5,\"taxClassCode\":\"phase4c-test\"}],\"shipping\":\"phase4c-test\",\"languageCode\":\"phase4c-test\",\"idempotencyKey\":\"phase4c-test\"}",
        200,
        requiredField: "quoteId");

    // Source assertion 18: Business rule assertion: BR-TAX-CAL-006
    // @BR-ID: BR-TAX-CAL-006
    [Fact(DisplayName = "018: Business rule assertion: BR-TAX-CAL-006")]
    [Trait("BR", "BR-TAX-CAL-006")]
    public Task Test018_POST_BASE_URL_tax_calculations_Field_quoteId_200() => AssertShellAsync(
        Method("POST"),
        "/tax-calculations",
        "{\"currencyCode\":\"phase4c-test\",\"customerId\":\"00000000-0000-0000-0000-000000000001\",\"orderId\":\"00000000-0000-0000-0000-000000000001\",\"billingAddress\":{\"countryCode\":\"phase4c-test\",\"zoneCode\":\"phase4c-test\",\"stateProvince\":\"phase4c-test\"},\"shippingAddress\":\"phase4c-test\",\"items\":[{\"productId\":\"00000000-0000-0000-0000-000000000001\",\"quantity\":1,\"unitAmount\":10.5,\"taxClassCode\":\"phase4c-test\"}],\"shipping\":\"phase4c-test\",\"languageCode\":\"phase4c-test\",\"idempotencyKey\":\"phase4c-test\"}",
        200,
        requiredField: "quoteId");

    // Source assertion 19: Business rule assertion: BR-TAX-CAL-007
    // @BR-ID: BR-TAX-CAL-007
    [Fact(DisplayName = "019: Business rule assertion: BR-TAX-CAL-007")]
    [Trait("BR", "BR-TAX-CAL-007")]
    public Task Test019_POST_BASE_URL_tax_calculations_Field_quoteId_200() => AssertShellAsync(
        Method("POST"),
        "/tax-calculations",
        "{\"currencyCode\":\"phase4c-test\",\"customerId\":\"00000000-0000-0000-0000-000000000001\",\"orderId\":\"00000000-0000-0000-0000-000000000001\",\"billingAddress\":{\"countryCode\":\"phase4c-test\",\"zoneCode\":\"phase4c-test\",\"stateProvince\":\"phase4c-test\"},\"shippingAddress\":\"phase4c-test\",\"items\":[{\"productId\":\"00000000-0000-0000-0000-000000000001\",\"quantity\":1,\"unitAmount\":10.5,\"taxClassCode\":\"phase4c-test\"}],\"shipping\":\"phase4c-test\",\"languageCode\":\"phase4c-test\",\"idempotencyKey\":\"phase4c-test\"}",
        200,
        requiredField: "quoteId");

    // Source assertion 20: Business rule assertion: BR-TAX-CAL-008
    // @BR-ID: BR-TAX-CAL-008
    [Fact(DisplayName = "020: Business rule assertion: BR-TAX-CAL-008")]
    [Trait("BR", "BR-TAX-CAL-008")]
    public Task Test020_POST_BASE_URL_tax_calculations_Field_quoteId_200() => AssertShellAsync(
        Method("POST"),
        "/tax-calculations",
        "{\"currencyCode\":\"phase4c-test\",\"customerId\":\"00000000-0000-0000-0000-000000000001\",\"orderId\":\"00000000-0000-0000-0000-000000000001\",\"billingAddress\":{\"countryCode\":\"phase4c-test\",\"zoneCode\":\"phase4c-test\",\"stateProvince\":\"phase4c-test\"},\"shippingAddress\":\"phase4c-test\",\"items\":[{\"productId\":\"00000000-0000-0000-0000-000000000001\",\"quantity\":1,\"unitAmount\":10.5,\"taxClassCode\":\"phase4c-test\"}],\"shipping\":\"phase4c-test\",\"languageCode\":\"phase4c-test\",\"idempotencyKey\":\"phase4c-test\"}",
        200,
        requiredField: "quoteId");

    // Source assertion 21: Business rule assertion: BR-TAX-CAL-009
    // @BR-ID: BR-TAX-CAL-009
    [Fact(DisplayName = "021: Business rule assertion: BR-TAX-CAL-009")]
    [Trait("BR", "BR-TAX-CAL-009")]
    public Task Test021_POST_BASE_URL_tax_calculations_Field_quoteId_200() => AssertShellAsync(
        Method("POST"),
        "/tax-calculations",
        "{\"currencyCode\":\"phase4c-test\",\"customerId\":\"00000000-0000-0000-0000-000000000001\",\"orderId\":\"00000000-0000-0000-0000-000000000001\",\"billingAddress\":{\"countryCode\":\"phase4c-test\",\"zoneCode\":\"phase4c-test\",\"stateProvince\":\"phase4c-test\"},\"shippingAddress\":\"phase4c-test\",\"items\":[{\"productId\":\"00000000-0000-0000-0000-000000000001\",\"quantity\":1,\"unitAmount\":10.5,\"taxClassCode\":\"phase4c-test\"}],\"shipping\":\"phase4c-test\",\"languageCode\":\"phase4c-test\",\"idempotencyKey\":\"phase4c-test\"}",
        200,
        requiredField: "quoteId");

    // Source assertion 22: Business rule assertion: BR-TAX-CAL-010
    // @BR-ID: BR-TAX-CAL-010
    [Fact(DisplayName = "022: Business rule assertion: BR-TAX-CAL-010")]
    [Trait("BR", "BR-TAX-CAL-010")]
    public Task Test022_POST_BASE_URL_tax_calculations_Field_quoteId_200() => AssertShellAsync(
        Method("POST"),
        "/tax-calculations",
        "{\"currencyCode\":\"phase4c-test\",\"customerId\":\"00000000-0000-0000-0000-000000000001\",\"orderId\":\"00000000-0000-0000-0000-000000000001\",\"billingAddress\":{\"countryCode\":\"phase4c-test\",\"zoneCode\":\"phase4c-test\",\"stateProvince\":\"phase4c-test\"},\"shippingAddress\":\"phase4c-test\",\"items\":[{\"productId\":\"00000000-0000-0000-0000-000000000001\",\"quantity\":1,\"unitAmount\":10.5,\"taxClassCode\":\"phase4c-test\"}],\"shipping\":\"phase4c-test\",\"languageCode\":\"phase4c-test\",\"idempotencyKey\":\"phase4c-test\"}",
        200,
        requiredField: "quoteId");

    // Source assertion 23: Contract success: POST /tax-classes
    // @BR-ID: BR-TAX-CFG-001
    [Fact(DisplayName = "023: Contract success: POST /tax-classes")]
    [Trait("BR", "BR-TAX-CFG-001")]
    public Task Test023_POST_BASE_URL_tax_classes_Field_id_201() => AssertShellAsync(
        Method("POST"),
        "/tax-classes",
        "{\"code\":\"phase4c-test\",\"title\":\"phase4c-test\"}",
        201,
        requiredField: "id");

    // Source assertion 24: Contract error/conformance: POST /tax-classes
    // @BR-ID: BR-TAX-CFG-001
    [Fact(DisplayName = "024: Contract error/conformance: POST /tax-classes")]
    [Trait("BR", "BR-TAX-CFG-001")]
    public Task Test024_POST_BASE_URL_tax_classes_Status_400() => AssertShellAsync(
        Method("POST"),
        "/tax-classes",
        "{}",
        400,
        requiredField: null);

    // Source assertion 25: Contract success: POST /tax-rates
    // @BR-ID: BR-TAX-CFG-001
    [Fact(DisplayName = "025: Contract success: POST /tax-rates")]
    [Trait("BR", "BR-TAX-CFG-001")]
    public Task Test025_POST_BASE_URL_tax_rates_Field_id_201() => AssertShellAsync(
        Method("POST"),
        "/tax-rates",
        "{\"taxClassCode\":\"phase4c-test\",\"code\":\"phase4c-test\",\"rate\":10.5,\"priority\":1,\"piggyback\":true,\"countryCode\":\"phase4c-test\",\"zoneCode\":\"phase4c-test\",\"stateProvince\":\"phase4c-test\",\"descriptions\":[{\"id\":\"00000000-0000-0000-0000-000000000001\",\"languageCode\":\"phase4c-test\",\"name\":\"phase4c-test\",\"title\":\"phase4c-test\",\"description\":\"phase4c-test\"}]}",
        201,
        requiredField: "id");

    // Source assertion 26: Contract error/conformance: POST /tax-rates
    // @BR-ID: BR-TAX-CFG-001
    [Fact(DisplayName = "026: Contract error/conformance: POST /tax-rates")]
    [Trait("BR", "BR-TAX-CFG-001")]
    public Task Test026_POST_BASE_URL_tax_rates_Status_400() => AssertShellAsync(
        Method("POST"),
        "/tax-rates",
        "{}",
        400,
        requiredField: null);

    // Source assertion 27: Contract success: GET /tax-classes
    // @BR-ID: BR-TAX-CFG-001
    [Fact(DisplayName = "027: Contract success: GET /tax-classes")]
    [Trait("BR", "BR-TAX-CFG-001")]
    public Task Test027_GET_BASE_URL_tax_classes_Field_items_200() => AssertShellAsync(
        Method("GET"),
        "/tax-classes",
        null,
        200,
        requiredField: "items");

    // Source assertion 28: Contract error/conformance: GET /tax-classes
    // @BR-ID: BR-TAX-CFG-001
    [Fact(DisplayName = "028: Contract error/conformance: GET /tax-classes")]
    [Trait("BR", "BR-TAX-CFG-001")]
    public Task Test028_GET_BASE_URL_tax_classes_Status_400() => AssertShellAsync(
        Method("GET"),
        "/tax-classes",
        null,
        400,
        requiredField: null);

    // Source assertion 29: Contract success: GET /tax-classes/exists
    // @BR-ID: BR-TAX-CFG-001
    [Fact(DisplayName = "029: Contract success: GET /tax-classes/exists")]
    [Trait("BR", "BR-TAX-CFG-001")]
    public Task Test029_GET_BASE_URL_tax_classes_exists_Field_exists_200() => AssertShellAsync(
        Method("GET"),
        "/tax-classes/exists",
        null,
        200,
        requiredField: "exists");

    // Source assertion 30: Contract error/conformance: GET /tax-classes/exists
    // @BR-ID: BR-TAX-CFG-001
    [Fact(DisplayName = "030: Contract error/conformance: GET /tax-classes/exists")]
    [Trait("BR", "BR-TAX-CFG-001")]
    public Task Test030_GET_BASE_URL_tax_classes_exists_Status_400() => AssertShellAsync(
        Method("GET"),
        "/tax-classes/exists",
        null,
        400,
        requiredField: null);

    // Source assertion 31: Contract success: GET /tax-classes/{id}
    // @BR-ID: BR-TAX-CFG-001
    [Fact(DisplayName = "031: Contract success: GET /tax-classes/{id}")]
    [Trait("BR", "BR-TAX-CFG-001")]
    public Task Test031_GET_BASE_URL_tax_classes_ById_Field_id_200() => AssertShellAsync(
        Method("GET"),
        $"/tax-classes/{ResourceId}",
        null,
        200,
        requiredField: "id");

    // Source assertion 32: Contract error/conformance: GET /tax-classes/{id}
    // @BR-ID: BR-TAX-CFG-001
    [Fact(DisplayName = "032: Contract error/conformance: GET /tax-classes/{id}")]
    [Trait("BR", "BR-TAX-CFG-001")]
    public Task Test032_GET_BASE_URL_tax_classes_ById_Status_401() => AssertShellAsync(
        Method("GET"),
        $"/tax-classes/{ResourceId}",
        null,
        401,
        requiredField: null);

    // Source assertion 33: Contract success: GET /tax-configuration
    // @BR-ID: BR-TAX-CFG-001
    [Fact(DisplayName = "033: Contract success: GET /tax-configuration")]
    [Trait("BR", "BR-TAX-CFG-001")]
    public Task Test033_GET_BASE_URL_tax_configuration_Field_taxBasis_200() => AssertShellAsync(
        Method("GET"),
        "/tax-configuration",
        null,
        200,
        requiredField: "taxBasis");

    // Source assertion 34: Contract error/conformance: GET /tax-configuration
    // @BR-ID: BR-TAX-CFG-001
    [Fact(DisplayName = "034: Contract error/conformance: GET /tax-configuration")]
    [Trait("BR", "BR-TAX-CFG-001")]
    public Task Test034_GET_BASE_URL_tax_configuration_Status_401() => AssertShellAsync(
        Method("GET"),
        "/tax-configuration",
        null,
        401,
        requiredField: null);

    // Source assertion 35: Contract success: GET /tax-rates
    // @BR-ID: BR-TAX-CFG-001
    [Fact(DisplayName = "035: Contract success: GET /tax-rates")]
    [Trait("BR", "BR-TAX-CFG-001")]
    public Task Test035_GET_BASE_URL_tax_rates_Field_items_200() => AssertShellAsync(
        Method("GET"),
        "/tax-rates",
        null,
        200,
        requiredField: "items");

    // Source assertion 36: Contract error/conformance: GET /tax-rates
    // @BR-ID: BR-TAX-CFG-001
    [Fact(DisplayName = "036: Contract error/conformance: GET /tax-rates")]
    [Trait("BR", "BR-TAX-CFG-001")]
    public Task Test036_GET_BASE_URL_tax_rates_Status_400() => AssertShellAsync(
        Method("GET"),
        "/tax-rates",
        null,
        400,
        requiredField: null);

    // Source assertion 37: Contract success: GET /tax-rates/exists
    // @BR-ID: BR-TAX-CFG-001
    [Fact(DisplayName = "037: Contract success: GET /tax-rates/exists")]
    [Trait("BR", "BR-TAX-CFG-001")]
    public Task Test037_GET_BASE_URL_tax_rates_exists_Field_exists_200() => AssertShellAsync(
        Method("GET"),
        "/tax-rates/exists",
        null,
        200,
        requiredField: "exists");

    // Source assertion 38: Contract error/conformance: GET /tax-rates/exists
    // @BR-ID: BR-TAX-CFG-001
    [Fact(DisplayName = "038: Contract error/conformance: GET /tax-rates/exists")]
    [Trait("BR", "BR-TAX-CFG-001")]
    public Task Test038_GET_BASE_URL_tax_rates_exists_Status_400() => AssertShellAsync(
        Method("GET"),
        "/tax-rates/exists",
        null,
        400,
        requiredField: null);

    // Source assertion 39: Contract success: GET /tax-rates/{id}
    // @BR-ID: BR-TAX-CFG-001
    [Fact(DisplayName = "039: Contract success: GET /tax-rates/{id}")]
    [Trait("BR", "BR-TAX-CFG-001")]
    public Task Test039_GET_BASE_URL_tax_rates_ById_Field_id_200() => AssertShellAsync(
        Method("GET"),
        $"/tax-rates/{ResourceId}",
        null,
        200,
        requiredField: "id");

    // Source assertion 40: Contract error/conformance: GET /tax-rates/{id}
    // @BR-ID: BR-TAX-CFG-001
    [Fact(DisplayName = "040: Contract error/conformance: GET /tax-rates/{id}")]
    [Trait("BR", "BR-TAX-CFG-001")]
    public Task Test040_GET_BASE_URL_tax_rates_ById_Status_401() => AssertShellAsync(
        Method("GET"),
        $"/tax-rates/{ResourceId}",
        null,
        401,
        requiredField: null);

    // Source assertion 41: Contract success: PUT /tax-classes/{id}
    // @BR-ID: BR-TAX-CFG-001
    [Fact(DisplayName = "041: Contract success: PUT /tax-classes/{id}")]
    [Trait("BR", "BR-TAX-CFG-001")]
    public Task Test041_PUT_BASE_URL_tax_classes_ById_Field_id_200() => AssertShellAsync(
        Method("PUT"),
        $"/tax-classes/{ResourceId}",
        "{\"code\":\"phase4c-test\",\"title\":\"phase4c-test\"}",
        200,
        requiredField: "id");

    // Source assertion 42: Contract error/conformance: PUT /tax-classes/{id}
    // @BR-ID: BR-TAX-CFG-001
    [Fact(DisplayName = "042: Contract error/conformance: PUT /tax-classes/{id}")]
    [Trait("BR", "BR-TAX-CFG-001")]
    public Task Test042_PUT_BASE_URL_tax_classes_ById_Status_400() => AssertShellAsync(
        Method("PUT"),
        $"/tax-classes/{ResourceId}",
        "{}",
        400,
        requiredField: null);

    // Source assertion 43: Contract success: PUT /tax-configuration
    // @BR-ID: BR-TAX-CFG-001
    [Fact(DisplayName = "043: Contract success: PUT /tax-configuration")]
    [Trait("BR", "BR-TAX-CFG-001")]
    public Task Test043_PUT_BASE_URL_tax_configuration_Field_taxBasis_200() => AssertShellAsync(
        Method("PUT"),
        "/tax-configuration",
        "{\"taxBasis\":\"StoreAddress\",\"collectTaxIfDifferentProvince\":true,\"differentCountryBehavior\":\"UseCustomerJurisdiction\"}",
        200,
        requiredField: "taxBasis");

    // Source assertion 44: Contract error/conformance: PUT /tax-configuration
    // @BR-ID: BR-TAX-CFG-001
    [Fact(DisplayName = "044: Contract error/conformance: PUT /tax-configuration")]
    [Trait("BR", "BR-TAX-CFG-001")]
    public Task Test044_PUT_BASE_URL_tax_configuration_Status_400() => AssertShellAsync(
        Method("PUT"),
        "/tax-configuration",
        "{}",
        400,
        requiredField: null);

    // Source assertion 45: Contract success: PUT /tax-rates/{id}
    // @BR-ID: BR-TAX-CFG-001
    [Fact(DisplayName = "045: Contract success: PUT /tax-rates/{id}")]
    [Trait("BR", "BR-TAX-CFG-001")]
    public Task Test045_PUT_BASE_URL_tax_rates_ById_Field_id_200() => AssertShellAsync(
        Method("PUT"),
        $"/tax-rates/{ResourceId}",
        "{}",
        200,
        requiredField: "id");

    // Source assertion 46: Contract error/conformance: PUT /tax-rates/{id}
    // @BR-ID: BR-TAX-CFG-001
    [Fact(DisplayName = "046: Contract error/conformance: PUT /tax-rates/{id}")]
    [Trait("BR", "BR-TAX-CFG-001")]
    public Task Test046_PUT_BASE_URL_tax_rates_ById_Status_400() => AssertShellAsync(
        Method("PUT"),
        $"/tax-rates/{ResourceId}",
        "{}",
        400,
        requiredField: null);

    // Source assertion 47: Contract success: DELETE /tax-classes/{id}
    // @BR-ID: BR-TAX-CFG-001
    [Fact(DisplayName = "047: Contract success: DELETE /tax-classes/{id}")]
    [Trait("BR", "BR-TAX-CFG-001")]
    public Task Test047_DELETE_BASE_URL_tax_classes_ById_Field_deleted_200() => AssertShellAsync(
        Method("DELETE"),
        $"/tax-classes/{ResourceId}",
        null,
        200,
        requiredField: "deleted");

    // Source assertion 48: Contract error/conformance: DELETE /tax-classes/{id}
    // @BR-ID: BR-TAX-CFG-001
    [Fact(DisplayName = "048: Contract error/conformance: DELETE /tax-classes/{id}")]
    [Trait("BR", "BR-TAX-CFG-001")]
    public Task Test048_DELETE_BASE_URL_tax_classes_ById_Status_401() => AssertShellAsync(
        Method("DELETE"),
        $"/tax-classes/{ResourceId}",
        null,
        401,
        requiredField: null);

    // Source assertion 49: Contract success: DELETE /tax-rates/{id}
    // @BR-ID: BR-TAX-CFG-001
    [Fact(DisplayName = "049: Contract success: DELETE /tax-rates/{id}")]
    [Trait("BR", "BR-TAX-CFG-001")]
    public Task Test049_DELETE_BASE_URL_tax_rates_ById_Field_deleted_200() => AssertShellAsync(
        Method("DELETE"),
        $"/tax-rates/{ResourceId}",
        null,
        200,
        requiredField: "deleted");

    // Source assertion 50: Contract error/conformance: DELETE /tax-rates/{id}
    // @BR-ID: BR-TAX-CFG-001
    [Fact(DisplayName = "050: Contract error/conformance: DELETE /tax-rates/{id}")]
    [Trait("BR", "BR-TAX-CFG-001")]
    public Task Test050_DELETE_BASE_URL_tax_rates_ById_Status_401() => AssertShellAsync(
        Method("DELETE"),
        $"/tax-rates/{ResourceId}",
        null,
        401,
        requiredField: null);
}
