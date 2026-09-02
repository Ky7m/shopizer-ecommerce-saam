using System.Net;

namespace Shopizer.IntegrationTests;

[Collection(ShopizerAspireCollection.Name)]
public sealed class PaymentsComprehensiveTests(AspireHostFixture fixture) : ComprehensiveTestBase(fixture.PaymentsClient)
{

    // Source assertion 1: Contract success: POST /callbacks/{providerCode}
    // @BR-ID: BR-ORD-014
    [Fact(DisplayName = "001: Contract success: POST /callbacks/{providerCode}")]
    [Trait("BR", "BR-ORD-014")]
    public Task Test001_POST_BASE_URL_callbacks_ById_Field_callbackId_202() => AssertShellAsync(
        Method("POST"),
        $"/callbacks/{ResourceId}",
        "{\"eventId\":\"00000000-0000-0000-0000-000000000001\",\"providerReference\":\"phase4c-test\",\"eventType\":\"phase4c-test\",\"payload\":{}}",
        202,
        requiredField: "callbackId");

    // Source assertion 2: Contract error/conformance: POST /callbacks/{providerCode}
    // @BR-ID: BR-ORD-014
    [Fact(DisplayName = "002: Contract error/conformance: POST /callbacks/{providerCode}")]
    [Trait("BR", "BR-ORD-014")]
    public Task Test002_POST_BASE_URL_callbacks_ById_Status_400() => AssertShellAsync(
        Method("POST"),
        $"/callbacks/{ResourceId}",
        "{}",
        400,
        requiredField: null);

    // Source assertion 3: Business rule assertion: BR-ORD-014
    // @BR-ID: BR-ORD-014
    [Fact(DisplayName = "003: Business rule assertion: BR-ORD-014")]
    [Trait("BR", "BR-ORD-014")]
    public Task Test003_POST_BASE_URL_callbacks_ById_Field_callbackId_202() => AssertShellAsync(
        Method("POST"),
        $"/callbacks/{ResourceId}",
        "{\"eventId\":\"00000000-0000-0000-0000-000000000001\",\"providerReference\":\"phase4c-test\",\"eventType\":\"phase4c-test\",\"payload\":{}}",
        202,
        requiredField: "callbackId");

    // Source assertion 4: Business rule assertion: BR-ORD-015
    // @BR-ID: BR-ORD-015
    [Fact(DisplayName = "004: Business rule assertion: BR-ORD-015")]
    [Trait("BR", "BR-ORD-015")]
    public Task Test004_POST_BASE_URL_callbacks_ById_Field_callbackId_202() => AssertShellAsync(
        Method("POST"),
        $"/callbacks/{ResourceId}",
        "{\"eventId\":\"00000000-0000-0000-0000-000000000001\",\"providerReference\":\"phase4c-test\",\"eventType\":\"phase4c-test\",\"payload\":{}}",
        202,
        requiredField: "callbackId");

    // Source assertion 5: Business rule assertion: BR-ORD-016
    // @BR-ID: BR-ORD-016
    [Fact(DisplayName = "005: Business rule assertion: BR-ORD-016")]
    [Trait("BR", "BR-ORD-016")]
    public Task Test005_POST_BASE_URL_callbacks_ById_Field_callbackId_202() => AssertShellAsync(
        Method("POST"),
        $"/callbacks/{ResourceId}",
        "{\"eventId\":\"00000000-0000-0000-0000-000000000001\",\"providerReference\":\"phase4c-test\",\"eventType\":\"phase4c-test\",\"payload\":{}}",
        202,
        requiredField: "callbackId");

    // Source assertion 6: Business rule assertion: BR-ORD-017
    // @BR-ID: BR-ORD-017
    [Fact(DisplayName = "006: Business rule assertion: BR-ORD-017")]
    [Trait("BR", "BR-ORD-017")]
    public Task Test006_POST_BASE_URL_callbacks_ById_Field_callbackId_202() => AssertShellAsync(
        Method("POST"),
        $"/callbacks/{ResourceId}",
        "{\"eventId\":\"00000000-0000-0000-0000-000000000001\",\"providerReference\":\"phase4c-test\",\"eventType\":\"phase4c-test\",\"payload\":{}}",
        202,
        requiredField: "callbackId");

    // Source assertion 7: Business rule assertion: BR-ORD-019
    // @BR-ID: BR-ORD-019
    [Fact(DisplayName = "007: Business rule assertion: BR-ORD-019")]
    [Trait("BR", "BR-ORD-019")]
    public Task Test007_POST_BASE_URL_callbacks_ById_Field_callbackId_202() => AssertShellAsync(
        Method("POST"),
        $"/callbacks/{ResourceId}",
        "{\"eventId\":\"00000000-0000-0000-0000-000000000001\",\"providerReference\":\"phase4c-test\",\"eventType\":\"phase4c-test\",\"payload\":{}}",
        202,
        requiredField: "callbackId");

    // Source assertion 8: Business rule assertion: BR-EXT-001
    // @BR-ID: BR-EXT-001
    [Fact(DisplayName = "008: Business rule assertion: BR-EXT-001")]
    [Trait("BR", "BR-EXT-001")]
    public Task Test008_POST_BASE_URL_callbacks_ById_Field_callbackId_202() => AssertShellAsync(
        Method("POST"),
        $"/callbacks/{ResourceId}",
        "{\"eventId\":\"00000000-0000-0000-0000-000000000001\",\"providerReference\":\"phase4c-test\",\"eventType\":\"phase4c-test\",\"payload\":{}}",
        202,
        requiredField: "callbackId");

    // Source assertion 9: Business rule assertion: BR-EXT-002
    // @BR-ID: BR-EXT-002
    [Fact(DisplayName = "009: Business rule assertion: BR-EXT-002")]
    [Trait("BR", "BR-EXT-002")]
    public Task Test009_POST_BASE_URL_callbacks_ById_Field_callbackId_202() => AssertShellAsync(
        Method("POST"),
        $"/callbacks/{ResourceId}",
        "{\"eventId\":\"00000000-0000-0000-0000-000000000001\",\"providerReference\":\"phase4c-test\",\"eventType\":\"phase4c-test\",\"payload\":{}}",
        202,
        requiredField: "callbackId");

    // Source assertion 10: Business rule assertion: BR-EXT-003
    // @BR-ID: BR-EXT-003
    [Fact(DisplayName = "010: Business rule assertion: BR-EXT-003")]
    [Trait("BR", "BR-EXT-003")]
    public Task Test010_POST_BASE_URL_callbacks_ById_Field_callbackId_202() => AssertShellAsync(
        Method("POST"),
        $"/callbacks/{ResourceId}",
        "{\"eventId\":\"00000000-0000-0000-0000-000000000001\",\"providerReference\":\"phase4c-test\",\"eventType\":\"phase4c-test\",\"payload\":{}}",
        202,
        requiredField: "callbackId");

    // Source assertion 11: Business rule assertion: BR-EXT-004
    // @BR-ID: BR-EXT-004
    [Fact(DisplayName = "011: Business rule assertion: BR-EXT-004")]
    [Trait("BR", "BR-EXT-004")]
    public Task Test011_POST_BASE_URL_callbacks_ById_Field_callbackId_202() => AssertShellAsync(
        Method("POST"),
        $"/callbacks/{ResourceId}",
        "{\"eventId\":\"00000000-0000-0000-0000-000000000001\",\"providerReference\":\"phase4c-test\",\"eventType\":\"phase4c-test\",\"payload\":{}}",
        202,
        requiredField: "callbackId");

    // Source assertion 12: Business rule assertion: BR-EXT-005
    // @BR-ID: BR-EXT-005
    [Fact(DisplayName = "012: Business rule assertion: BR-EXT-005")]
    [Trait("BR", "BR-EXT-005")]
    public Task Test012_POST_BASE_URL_callbacks_ById_Field_callbackId_202() => AssertShellAsync(
        Method("POST"),
        $"/callbacks/{ResourceId}",
        "{\"eventId\":\"00000000-0000-0000-0000-000000000001\",\"providerReference\":\"phase4c-test\",\"eventType\":\"phase4c-test\",\"payload\":{}}",
        202,
        requiredField: "callbackId");

    // Source assertion 13: Business rule assertion: BR-EXT-006
    // @BR-ID: BR-EXT-006
    [Fact(DisplayName = "013: Business rule assertion: BR-EXT-006")]
    [Trait("BR", "BR-EXT-006")]
    public Task Test013_POST_BASE_URL_callbacks_ById_Field_callbackId_202() => AssertShellAsync(
        Method("POST"),
        $"/callbacks/{ResourceId}",
        "{\"eventId\":\"00000000-0000-0000-0000-000000000001\",\"providerReference\":\"phase4c-test\",\"eventType\":\"phase4c-test\",\"payload\":{}}",
        202,
        requiredField: "callbackId");

    // Source assertion 14: Business rule assertion: BR-EXT-007
    // @BR-ID: BR-EXT-007
    [Fact(DisplayName = "014: Business rule assertion: BR-EXT-007")]
    [Trait("BR", "BR-EXT-007")]
    public Task Test014_POST_BASE_URL_callbacks_ById_Field_callbackId_202() => AssertShellAsync(
        Method("POST"),
        $"/callbacks/{ResourceId}",
        "{\"eventId\":\"00000000-0000-0000-0000-000000000001\",\"providerReference\":\"phase4c-test\",\"eventType\":\"phase4c-test\",\"payload\":{}}",
        202,
        requiredField: "callbackId");

    // Source assertion 15: Business rule assertion: BR-EXT-008
    // @BR-ID: BR-EXT-008
    [Fact(DisplayName = "015: Business rule assertion: BR-EXT-008")]
    [Trait("BR", "BR-EXT-008")]
    public Task Test015_POST_BASE_URL_callbacks_ById_Field_callbackId_202() => AssertShellAsync(
        Method("POST"),
        $"/callbacks/{ResourceId}",
        "{\"eventId\":\"00000000-0000-0000-0000-000000000001\",\"providerReference\":\"phase4c-test\",\"eventType\":\"phase4c-test\",\"payload\":{}}",
        202,
        requiredField: "callbackId");

    // Source assertion 16: Business rule assertion: BR-EXT-009
    // @BR-ID: BR-EXT-009
    [Fact(DisplayName = "016: Business rule assertion: BR-EXT-009")]
    [Trait("BR", "BR-EXT-009")]
    public Task Test016_POST_BASE_URL_callbacks_ById_Field_callbackId_202() => AssertShellAsync(
        Method("POST"),
        $"/callbacks/{ResourceId}",
        "{\"eventId\":\"00000000-0000-0000-0000-000000000001\",\"providerReference\":\"phase4c-test\",\"eventType\":\"phase4c-test\",\"payload\":{}}",
        202,
        requiredField: "callbackId");

    // Source assertion 17: Business rule assertion: BR-UI-015
    // @BR-ID: BR-UI-015
    [Fact(DisplayName = "017: Business rule assertion: BR-UI-015")]
    [Trait("BR", "BR-UI-015")]
    public Task Test017_POST_BASE_URL_callbacks_ById_Field_callbackId_202() => AssertShellAsync(
        Method("POST"),
        $"/callbacks/{ResourceId}",
        "{\"eventId\":\"00000000-0000-0000-0000-000000000001\",\"providerReference\":\"phase4c-test\",\"eventType\":\"phase4c-test\",\"payload\":{}}",
        202,
        requiredField: "callbackId");

    // Source assertion 18: Business rule assertion: BR-PA-020
    // @BR-ID: BR-PA-020
    [Fact(DisplayName = "018: Business rule assertion: BR-PA-020")]
    [Trait("BR", "BR-PA-020")]
    public Task Test018_POST_BASE_URL_callbacks_ById_Field_callbackId_202() => AssertShellAsync(
        Method("POST"),
        $"/callbacks/{ResourceId}",
        "{\"eventId\":\"00000000-0000-0000-0000-000000000001\",\"providerReference\":\"phase4c-test\",\"eventType\":\"phase4c-test\",\"payload\":{}}",
        202,
        requiredField: "callbackId");

    // Source assertion 19: Business rule assertion: BR-PA-021
    // @BR-ID: BR-PA-021
    [Fact(DisplayName = "019: Business rule assertion: BR-PA-021")]
    [Trait("BR", "BR-PA-021")]
    public Task Test019_POST_BASE_URL_callbacks_ById_Field_callbackId_202() => AssertShellAsync(
        Method("POST"),
        $"/callbacks/{ResourceId}",
        "{\"eventId\":\"00000000-0000-0000-0000-000000000001\",\"providerReference\":\"phase4c-test\",\"eventType\":\"phase4c-test\",\"payload\":{}}",
        202,
        requiredField: "callbackId");

    // Source assertion 20: Business rule assertion: BR-PA-022
    // @BR-ID: BR-PA-022
    [Fact(DisplayName = "020: Business rule assertion: BR-PA-022")]
    [Trait("BR", "BR-PA-022")]
    public Task Test020_POST_BASE_URL_callbacks_ById_Field_callbackId_202() => AssertShellAsync(
        Method("POST"),
        $"/callbacks/{ResourceId}",
        "{\"eventId\":\"00000000-0000-0000-0000-000000000001\",\"providerReference\":\"phase4c-test\",\"eventType\":\"phase4c-test\",\"payload\":{}}",
        202,
        requiredField: "callbackId");

    // Source assertion 21: Business rule assertion: BR-PA-023
    // @BR-ID: BR-PA-023
    [Fact(DisplayName = "021: Business rule assertion: BR-PA-023")]
    [Trait("BR", "BR-PA-023")]
    public Task Test021_POST_BASE_URL_callbacks_ById_Field_callbackId_202() => AssertShellAsync(
        Method("POST"),
        $"/callbacks/{ResourceId}",
        "{\"eventId\":\"00000000-0000-0000-0000-000000000001\",\"providerReference\":\"phase4c-test\",\"eventType\":\"phase4c-test\",\"payload\":{}}",
        202,
        requiredField: "callbackId");

    // Source assertion 22: Contract success: POST /payment-intents
    // @BR-ID: BR-ORD-014
    [Fact(DisplayName = "022: Contract success: POST /payment-intents")]
    [Trait("BR", "BR-ORD-014")]
    public Task Test022_POST_BASE_URL_payment_intents_Field_paymentIntentId_201() => AssertShellAsync(
        Method("POST"),
        "/payment-intents",
        "{\"checkoutSessionId\":\"00000000-0000-0000-0000-000000000001\",\"orderId\":\"00000000-0000-0000-0000-000000000001\",\"paymentMethodCode\":\"phase4c-test\",\"amount\":\"phase4c-test\",\"currency\":\"phase4c-test\",\"paymentToken\":\"phase4c-test\",\"amountSnapshotVersion\":1}",
        201,
        requiredField: "paymentIntentId");

    // Source assertion 23: Contract error/conformance: POST /payment-intents
    // @BR-ID: BR-ORD-014
    [Fact(DisplayName = "023: Contract error/conformance: POST /payment-intents")]
    [Trait("BR", "BR-ORD-014")]
    public Task Test023_POST_BASE_URL_payment_intents_Status_400() => AssertShellAsync(
        Method("POST"),
        "/payment-intents",
        "{}",
        400,
        requiredField: null);

    // Source assertion 24: Contract success: POST /payment-intents/{paymentIntentId}/authorize
    // @BR-ID: BR-ORD-014
    [Fact(DisplayName = "024: Contract success: POST /payment-intents/{paymentIntentId}/authorize")]
    [Trait("BR", "BR-ORD-014")]
    public Task Test024_POST_BASE_URL_payment_intents_ById_authorize_Field_paymentOperationId_201() => AssertShellAsync(
        Method("POST"),
        $"/payment-intents/{ResourceId}/authorize",
        "{\"amount\":\"phase4c-test\",\"currency\":\"phase4c-test\",\"paymentToken\":\"phase4c-test\",\"payerReference\":\"phase4c-test\",\"providerIntentReference\":\"phase4c-test\",\"metadata\":{}}",
        201,
        requiredField: "paymentOperationId");

    // Source assertion 25: Contract error/conformance: POST /payment-intents/{paymentIntentId}/authorize
    // @BR-ID: BR-ORD-014
    [Fact(DisplayName = "025: Contract error/conformance: POST /payment-intents/{paymentIntentId}/authorize")]
    [Trait("BR", "BR-ORD-014")]
    public Task Test025_POST_BASE_URL_payment_intents_ById_authorize_Status_400() => AssertShellAsync(
        Method("POST"),
        $"/payment-intents/{ResourceId}/authorize",
        "{}",
        400,
        requiredField: null);

    // Source assertion 26: Contract success: POST /payment-intents/{paymentIntentId}/capture
    // @BR-ID: BR-ORD-014
    [Fact(DisplayName = "026: Contract success: POST /payment-intents/{paymentIntentId}/capture")]
    [Trait("BR", "BR-ORD-014")]
    public Task Test026_POST_BASE_URL_payment_intents_ById_capture_Field_paymentOperationId_201() => AssertShellAsync(
        Method("POST"),
        $"/payment-intents/{ResourceId}/capture",
        "{\"amount\":\"phase4c-test\",\"currency\":\"phase4c-test\"}",
        201,
        requiredField: "paymentOperationId");

    // Source assertion 27: Contract error/conformance: POST /payment-intents/{paymentIntentId}/capture
    // @BR-ID: BR-ORD-014
    [Fact(DisplayName = "027: Contract error/conformance: POST /payment-intents/{paymentIntentId}/capture")]
    [Trait("BR", "BR-ORD-014")]
    public Task Test027_POST_BASE_URL_payment_intents_ById_capture_Status_400() => AssertShellAsync(
        Method("POST"),
        $"/payment-intents/{ResourceId}/capture",
        "{}",
        400,
        requiredField: null);

    // Source assertion 28: Contract success: POST /payment-intents/{paymentIntentId}/refunds
    // @BR-ID: BR-ORD-014
    [Fact(DisplayName = "028: Contract success: POST /payment-intents/{paymentIntentId}/refunds")]
    [Trait("BR", "BR-ORD-014")]
    public Task Test028_POST_BASE_URL_payment_intents_ById_refunds_Field_refundId_201() => AssertShellAsync(
        Method("POST"),
        $"/payment-intents/{ResourceId}/refunds",
        "{\"amount\":\"phase4c-test\",\"currency\":\"phase4c-test\",\"reason\":\"phase4c-test\"}",
        201,
        requiredField: "refundId");

    // Source assertion 29: Contract error/conformance: POST /payment-intents/{paymentIntentId}/refunds
    // @BR-ID: BR-ORD-014
    [Fact(DisplayName = "029: Contract error/conformance: POST /payment-intents/{paymentIntentId}/refunds")]
    [Trait("BR", "BR-ORD-014")]
    public Task Test029_POST_BASE_URL_payment_intents_ById_refunds_Status_400() => AssertShellAsync(
        Method("POST"),
        $"/payment-intents/{ResourceId}/refunds",
        "{}",
        400,
        requiredField: null);

    // Source assertion 30: Contract success: GET /payment-intents/{paymentIntentId}
    // @BR-ID: BR-ORD-014
    [Fact(DisplayName = "030: Contract success: GET /payment-intents/{paymentIntentId}")]
    [Trait("BR", "BR-ORD-014")]
    public Task Test030_GET_BASE_URL_payment_intents_ById_Field_paymentIntentId_200() => AssertShellAsync(
        Method("GET"),
        $"/payment-intents/{ResourceId}",
        null,
        200,
        requiredField: "paymentIntentId");

    // Source assertion 31: Contract error/conformance: GET /payment-intents/{paymentIntentId}
    // @BR-ID: BR-ORD-014
    [Fact(DisplayName = "031: Contract error/conformance: GET /payment-intents/{paymentIntentId}")]
    [Trait("BR", "BR-ORD-014")]
    public Task Test031_GET_BASE_URL_payment_intents_ById_Status_401() => AssertShellAsync(
        Method("GET"),
        $"/payment-intents/{ResourceId}",
        null,
        401,
        requiredField: null);

    // Source assertion 32: Contract success: GET /payment-intents/{paymentIntentId}/transactions
    // @BR-ID: BR-ORD-014
    [Fact(DisplayName = "032: Contract success: GET /payment-intents/{paymentIntentId}/transactions")]
    [Trait("BR", "BR-ORD-014")]
    public Task Test032_GET_BASE_URL_payment_intents_ById_transactions_Field_items_200() => AssertShellAsync(
        Method("GET"),
        $"/payment-intents/{ResourceId}/transactions",
        null,
        200,
        requiredField: "items");

    // Source assertion 33: Contract error/conformance: GET /payment-intents/{paymentIntentId}/transactions
    // @BR-ID: BR-ORD-014
    [Fact(DisplayName = "033: Contract error/conformance: GET /payment-intents/{paymentIntentId}/transactions")]
    [Trait("BR", "BR-ORD-014")]
    public Task Test033_GET_BASE_URL_payment_intents_ById_transactions_Status_401() => AssertShellAsync(
        Method("GET"),
        $"/payment-intents/{ResourceId}/transactions",
        null,
        401,
        requiredField: null);

    // Source assertion 34: Contract success: GET /payment-methods
    // @BR-ID: BR-ORD-014
    [Fact(DisplayName = "034: Contract success: GET /payment-methods")]
    [Trait("BR", "BR-ORD-014")]
    public Task Test034_GET_BASE_URL_payment_methods_Field_items_200() => AssertShellAsync(
        Method("GET"),
        "/payment-methods",
        null,
        200,
        requiredField: "items");

    // Source assertion 35: Contract error/conformance: GET /payment-methods
    // @BR-ID: BR-ORD-014
    [Fact(DisplayName = "035: Contract error/conformance: GET /payment-methods")]
    [Trait("BR", "BR-ORD-014")]
    public Task Test035_GET_BASE_URL_payment_methods_Status_401() => AssertShellAsync(
        Method("GET"),
        "/payment-methods",
        null,
        401,
        requiredField: null);

    // Source assertion 36: Contract success: GET /payment-methods/{code}
    // @BR-ID: BR-ORD-014
    [Fact(DisplayName = "036: Contract success: GET /payment-methods/{code}")]
    [Trait("BR", "BR-ORD-014")]
    public Task Test036_GET_BASE_URL_payment_methods_phase4c_code_Field_code_200() => AssertShellAsync(
        Method("GET"),
        "/payment-methods/phase4c-code",
        null,
        200,
        requiredField: "code");

    // Source assertion 37: Contract error/conformance: GET /payment-methods/{code}
    // @BR-ID: BR-ORD-014
    [Fact(DisplayName = "037: Contract error/conformance: GET /payment-methods/{code}")]
    [Trait("BR", "BR-ORD-014")]
    public Task Test037_GET_BASE_URL_payment_methods_phase4c_code_Status_401() => AssertShellAsync(
        Method("GET"),
        "/payment-methods/phase4c-code",
        null,
        401,
        requiredField: null);

    // Source assertion 38: Contract success: GET /payment-operations/{paymentOperationId}
    // @BR-ID: BR-ORD-014
    [Fact(DisplayName = "038: Contract success: GET /payment-operations/{paymentOperationId}")]
    [Trait("BR", "BR-ORD-014")]
    public Task Test038_GET_BASE_URL_payment_operations_ById_Field_paymentOperationId_200() => AssertShellAsync(
        Method("GET"),
        $"/payment-operations/{ResourceId}",
        null,
        200,
        requiredField: "paymentOperationId");

    // Source assertion 39: Contract error/conformance: GET /payment-operations/{paymentOperationId}
    // @BR-ID: BR-ORD-014
    [Fact(DisplayName = "039: Contract error/conformance: GET /payment-operations/{paymentOperationId}")]
    [Trait("BR", "BR-ORD-014")]
    public Task Test039_GET_BASE_URL_payment_operations_ById_Status_401() => AssertShellAsync(
        Method("GET"),
        $"/payment-operations/{ResourceId}",
        null,
        401,
        requiredField: null);

    // Source assertion 40: Contract success: GET /reconciliation/capturable
    // @BR-ID: BR-ORD-014
    [Fact(DisplayName = "040: Contract success: GET /reconciliation/capturable")]
    [Trait("BR", "BR-ORD-014")]
    public Task Test040_GET_BASE_URL_reconciliation_capturable_Field_items_200() => AssertShellAsync(
        Method("GET"),
        "/reconciliation/capturable",
        null,
        200,
        requiredField: "items");

    // Source assertion 41: Contract error/conformance: GET /reconciliation/capturable
    // @BR-ID: BR-ORD-014
    [Fact(DisplayName = "041: Contract error/conformance: GET /reconciliation/capturable")]
    [Trait("BR", "BR-ORD-014")]
    public Task Test041_GET_BASE_URL_reconciliation_capturable_Status_400() => AssertShellAsync(
        Method("GET"),
        "/reconciliation/capturable",
        null,
        400,
        requiredField: null);

    // Source assertion 42: Contract success: PUT /payment-methods/{code}/configuration
    // @BR-ID: BR-ORD-014
    [Fact(DisplayName = "042: Contract success: PUT /payment-methods/{code}/configuration")]
    [Trait("BR", "BR-ORD-014")]
    public Task Test042_PUT_BASE_URL_payment_methods_phase4c_code_configuration_Field_code_200() => AssertShellAsync(
        Method("PUT"),
        "/payment-methods/phase4c-code/configuration",
        "{\"active\":true,\"defaultSelected\":true,\"environment\":\"Test\",\"publicConfiguration\":{},\"secretReference\":\"phase4c-test\",\"configurationVersion\":1}",
        200,
        requiredField: "code");

    // Source assertion 43: Contract error/conformance: PUT /payment-methods/{code}/configuration
    // @BR-ID: BR-ORD-014
    [Fact(DisplayName = "043: Contract error/conformance: PUT /payment-methods/{code}/configuration")]
    [Trait("BR", "BR-ORD-014")]
    public Task Test043_PUT_BASE_URL_payment_methods_phase4c_code_configuration_Status_400() => AssertShellAsync(
        Method("PUT"),
        "/payment-methods/phase4c-code/configuration",
        "{}",
        400,
        requiredField: null);
}
