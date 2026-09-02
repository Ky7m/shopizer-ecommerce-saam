namespace Shopizer.IntegrationTests;

[Collection(ShopizerAspireCollection.Name)]
public sealed class OrderManagementComprehensiveTests(AspireHostFixture fixture) : ComprehensiveTestBase(fixture.OrderManagementClient)
{

    // Source assertion 1: Contract success: POST /orders/{orderId}/cancel
    // @BR-ID: BR-OR-SUB-001
    [Fact(DisplayName = "001: Contract success: POST /orders/{orderId}/cancel")]
    [Trait("BR", "BR-OR-SUB-001")]
    public Task Test001_POST_BASE_URL_orders_ById_cancel_Field_orderId_202() => AssertShellAsync(
        Method("POST"),
        $"/orders/{ResourceId}/cancel",
        "{\"reason\":\"phase4c-test\"}",
        202,
        requiredField: "orderId");

    // Source assertion 2: Contract error/conformance: POST /orders/{orderId}/cancel
    // @BR-ID: BR-OR-SUB-001
    [Fact(DisplayName = "002: Contract error/conformance: POST /orders/{orderId}/cancel")]
    [Trait("BR", "BR-OR-SUB-001")]
    public Task Test002_POST_BASE_URL_orders_ById_cancel_Status_401() => AssertShellAsync(
        Method("POST"),
        $"/orders/{ResourceId}/cancel",
        "{}",
        401,
        requiredField: null);

    // Source assertion 3: Business rule assertion: BR-OR-SUB-001
    // @BR-ID: BR-OR-SUB-001
    [Fact(DisplayName = "003: Business rule assertion: BR-OR-SUB-001")]
    [Trait("BR", "BR-OR-SUB-001")]
    public Task Test003_POST_BASE_URL_orders_ById_cancel_Field_orderId_202() => AssertShellAsync(
        Method("POST"),
        $"/orders/{ResourceId}/cancel",
        "{\"reason\":\"phase4c-test\"}",
        202,
        requiredField: "orderId");

    // Source assertion 4: Business rule assertion: BR-OR-SUB-002
    // @BR-ID: BR-OR-SUB-002
    [Fact(DisplayName = "004: Business rule assertion: BR-OR-SUB-002")]
    [Trait("BR", "BR-OR-SUB-002")]
    public Task Test004_POST_BASE_URL_orders_ById_cancel_Field_orderId_202() => AssertShellAsync(
        Method("POST"),
        $"/orders/{ResourceId}/cancel",
        "{\"reason\":\"phase4c-test\"}",
        202,
        requiredField: "orderId");

    // Source assertion 5: Business rule assertion: BR-OR-SUB-003
    // @BR-ID: BR-OR-SUB-003
    [Fact(DisplayName = "005: Business rule assertion: BR-OR-SUB-003")]
    [Trait("BR", "BR-OR-SUB-003")]
    public Task Test005_POST_BASE_URL_orders_ById_cancel_Field_orderId_202() => AssertShellAsync(
        Method("POST"),
        $"/orders/{ResourceId}/cancel",
        "{\"reason\":\"phase4c-test\"}",
        202,
        requiredField: "orderId");

    // Source assertion 6: Business rule assertion: BR-OR-SUB-004
    // @BR-ID: BR-OR-SUB-004
    [Fact(DisplayName = "006: Business rule assertion: BR-OR-SUB-004")]
    [Trait("BR", "BR-OR-SUB-004")]
    public Task Test006_POST_BASE_URL_orders_ById_cancel_Field_orderId_202() => AssertShellAsync(
        Method("POST"),
        $"/orders/{ResourceId}/cancel",
        "{\"reason\":\"phase4c-test\"}",
        202,
        requiredField: "orderId");

    // Source assertion 7: Business rule assertion: BR-OR-PAY-001
    // @BR-ID: BR-OR-PAY-001
    [Fact(DisplayName = "007: Business rule assertion: BR-OR-PAY-001")]
    [Trait("BR", "BR-OR-PAY-001")]
    public Task Test007_POST_BASE_URL_orders_ById_cancel_Field_orderId_202() => AssertShellAsync(
        Method("POST"),
        $"/orders/{ResourceId}/cancel",
        "{\"reason\":\"phase4c-test\"}",
        202,
        requiredField: "orderId");

    // Source assertion 8: Business rule assertion: BR-OR-DIG-001
    // @BR-ID: BR-OR-DIG-001
    [Fact(DisplayName = "008: Business rule assertion: BR-OR-DIG-001")]
    [Trait("BR", "BR-OR-DIG-001")]
    public Task Test008_POST_BASE_URL_orders_ById_cancel_Field_orderId_202() => AssertShellAsync(
        Method("POST"),
        $"/orders/{ResourceId}/cancel",
        "{\"reason\":\"phase4c-test\"}",
        202,
        requiredField: "orderId");

    // Source assertion 9: Business rule assertion: BR-OR-FAIL-001
    // @BR-ID: BR-OR-FAIL-001
    [Fact(DisplayName = "009: Business rule assertion: BR-OR-FAIL-001")]
    [Trait("BR", "BR-OR-FAIL-001")]
    public Task Test009_POST_BASE_URL_orders_ById_cancel_Field_orderId_202() => AssertShellAsync(
        Method("POST"),
        $"/orders/{ResourceId}/cancel",
        "{\"reason\":\"phase4c-test\"}",
        202,
        requiredField: "orderId");

    // Source assertion 10: Business rule assertion: BR-OR-LIFE-001
    // @BR-ID: BR-OR-LIFE-001
    [Fact(DisplayName = "010: Business rule assertion: BR-OR-LIFE-001")]
    [Trait("BR", "BR-OR-LIFE-001")]
    public Task Test010_POST_BASE_URL_orders_ById_cancel_Field_orderId_202() => AssertShellAsync(
        Method("POST"),
        $"/orders/{ResourceId}/cancel",
        "{\"reason\":\"phase4c-test\"}",
        202,
        requiredField: "orderId");

    // Source assertion 11: Business rule assertion: BR-OR-LIFE-002
    // @BR-ID: BR-OR-LIFE-002
    [Fact(DisplayName = "011: Business rule assertion: BR-OR-LIFE-002")]
    [Trait("BR", "BR-OR-LIFE-002")]
    public Task Test011_POST_BASE_URL_orders_ById_cancel_Field_orderId_202() => AssertShellAsync(
        Method("POST"),
        $"/orders/{ResourceId}/cancel",
        "{\"reason\":\"phase4c-test\"}",
        202,
        requiredField: "orderId");

    // Source assertion 12: Business rule assertion: BR-OR-PAY-002
    // @BR-ID: BR-OR-PAY-002
    [Fact(DisplayName = "012: Business rule assertion: BR-OR-PAY-002")]
    [Trait("BR", "BR-OR-PAY-002")]
    public Task Test012_POST_BASE_URL_orders_ById_cancel_Field_orderId_202() => AssertShellAsync(
        Method("POST"),
        $"/orders/{ResourceId}/cancel",
        "{\"reason\":\"phase4c-test\"}",
        202,
        requiredField: "orderId");

    // Source assertion 13: Business rule assertion: BR-OR-PAY-003
    // @BR-ID: BR-OR-PAY-003
    [Fact(DisplayName = "013: Business rule assertion: BR-OR-PAY-003")]
    [Trait("BR", "BR-OR-PAY-003")]
    public Task Test013_POST_BASE_URL_orders_ById_cancel_Field_orderId_202() => AssertShellAsync(
        Method("POST"),
        $"/orders/{ResourceId}/cancel",
        "{\"reason\":\"phase4c-test\"}",
        202,
        requiredField: "orderId");

    // Source assertion 14: Business rule assertion: BR-OR-PAY-004
    // @BR-ID: BR-OR-PAY-004
    [Fact(DisplayName = "014: Business rule assertion: BR-OR-PAY-004")]
    [Trait("BR", "BR-OR-PAY-004")]
    public Task Test014_POST_BASE_URL_orders_ById_cancel_Field_orderId_202() => AssertShellAsync(
        Method("POST"),
        $"/orders/{ResourceId}/cancel",
        "{\"reason\":\"phase4c-test\"}",
        202,
        requiredField: "orderId");

    // Source assertion 15: Business rule assertion: BR-OR-REF-001
    // @BR-ID: BR-OR-REF-001
    [Fact(DisplayName = "015: Business rule assertion: BR-OR-REF-001")]
    [Trait("BR", "BR-OR-REF-001")]
    public Task Test015_POST_BASE_URL_orders_ById_cancel_Field_orderId_202() => AssertShellAsync(
        Method("POST"),
        $"/orders/{ResourceId}/cancel",
        "{\"reason\":\"phase4c-test\"}",
        202,
        requiredField: "orderId");

    // Source assertion 16: Business rule assertion: BR-OR-CAN-001
    // @BR-ID: BR-OR-CAN-001
    [Fact(DisplayName = "016: Business rule assertion: BR-OR-CAN-001")]
    [Trait("BR", "BR-OR-CAN-001")]
    public Task Test016_POST_BASE_URL_orders_ById_cancel_Field_orderId_202() => AssertShellAsync(
        Method("POST"),
        $"/orders/{ResourceId}/cancel",
        "{\"reason\":\"phase4c-test\"}",
        202,
        requiredField: "orderId");

    // Source assertion 17: Business rule assertion: BR-OR-FUL-001
    // @BR-ID: BR-OR-FUL-001
    [Fact(DisplayName = "017: Business rule assertion: BR-OR-FUL-001")]
    [Trait("BR", "BR-OR-FUL-001")]
    public Task Test017_POST_BASE_URL_orders_ById_cancel_Field_orderId_202() => AssertShellAsync(
        Method("POST"),
        $"/orders/{ResourceId}/cancel",
        "{\"reason\":\"phase4c-test\"}",
        202,
        requiredField: "orderId");

    // Source assertion 18: Business rule assertion: BR-OR-AUTH-001
    // @BR-ID: BR-OR-AUTH-001
    [Fact(DisplayName = "018: Business rule assertion: BR-OR-AUTH-001")]
    [Trait("BR", "BR-OR-AUTH-001")]
    public Task Test018_POST_BASE_URL_orders_ById_cancel_Field_orderId_202() => AssertShellAsync(
        Method("POST"),
        $"/orders/{ResourceId}/cancel",
        "{\"reason\":\"phase4c-test\"}",
        202,
        requiredField: "orderId");

    // Source assertion 19: Business rule assertion: BR-OR-AUTH-002
    // @BR-ID: BR-OR-AUTH-002
    [Fact(DisplayName = "019: Business rule assertion: BR-OR-AUTH-002")]
    [Trait("BR", "BR-OR-AUTH-002")]
    public Task Test019_POST_BASE_URL_orders_ById_cancel_Field_orderId_202() => AssertShellAsync(
        Method("POST"),
        $"/orders/{ResourceId}/cancel",
        "{\"reason\":\"phase4c-test\"}",
        202,
        requiredField: "orderId");

    // Source assertion 20: Business rule assertion: BR-OR-ADM-001
    // @BR-ID: BR-OR-ADM-001
    [Fact(DisplayName = "020: Business rule assertion: BR-OR-ADM-001")]
    [Trait("BR", "BR-OR-ADM-001")]
    public Task Test020_POST_BASE_URL_orders_ById_cancel_Field_orderId_202() => AssertShellAsync(
        Method("POST"),
        $"/orders/{ResourceId}/cancel",
        "{\"reason\":\"phase4c-test\"}",
        202,
        requiredField: "orderId");

    // Source assertion 21: Business rule assertion: BR-OR-ADM-002
    // @BR-ID: BR-OR-ADM-002
    [Fact(DisplayName = "021: Business rule assertion: BR-OR-ADM-002")]
    [Trait("BR", "BR-OR-ADM-002")]
    public Task Test021_POST_BASE_URL_orders_ById_cancel_Field_orderId_202() => AssertShellAsync(
        Method("POST"),
        $"/orders/{ResourceId}/cancel",
        "{\"reason\":\"phase4c-test\"}",
        202,
        requiredField: "orderId");

    // Source assertion 22: Business rule assertion: BR-OR-READ-001
    // @BR-ID: BR-OR-READ-001
    [Fact(DisplayName = "022: Business rule assertion: BR-OR-READ-001")]
    [Trait("BR", "BR-OR-READ-001")]
    public Task Test022_POST_BASE_URL_orders_ById_cancel_Field_orderId_202() => AssertShellAsync(
        Method("POST"),
        $"/orders/{ResourceId}/cancel",
        "{\"reason\":\"phase4c-test\"}",
        202,
        requiredField: "orderId");

    // Source assertion 23: Business rule assertion: BR-OR-RES-001
    // @BR-ID: BR-OR-RES-001
    [Fact(DisplayName = "023: Business rule assertion: BR-OR-RES-001")]
    [Trait("BR", "BR-OR-RES-001")]
    public Task Test023_POST_BASE_URL_orders_ById_cancel_Field_orderId_202() => AssertShellAsync(
        Method("POST"),
        $"/orders/{ResourceId}/cancel",
        "{\"reason\":\"phase4c-test\"}",
        202,
        requiredField: "orderId");

    // Source assertion 24: Business rule assertion: BR-OR-INV-001
    // @BR-ID: BR-OR-INV-001
    [Fact(DisplayName = "024: Business rule assertion: BR-OR-INV-001")]
    [Trait("BR", "BR-OR-INV-001")]
    public Task Test024_POST_BASE_URL_orders_ById_cancel_Field_orderId_202() => AssertShellAsync(
        Method("POST"),
        $"/orders/{ResourceId}/cancel",
        "{\"reason\":\"phase4c-test\"}",
        202,
        requiredField: "orderId");

    // Source assertion 25: Business rule assertion: BR-OR-UI-001
    // @BR-ID: BR-OR-UI-001
    [Fact(DisplayName = "025: Business rule assertion: BR-OR-UI-001")]
    [Trait("BR", "BR-OR-UI-001")]
    public Task Test025_POST_BASE_URL_orders_ById_cancel_Field_orderId_202() => AssertShellAsync(
        Method("POST"),
        $"/orders/{ResourceId}/cancel",
        "{\"reason\":\"phase4c-test\"}",
        202,
        requiredField: "orderId");

    // Source assertion 26: Contract success: POST /orders/{orderId}/capture
    // @BR-ID: BR-OR-SUB-001
    [Fact(DisplayName = "026: Contract success: POST /orders/{orderId}/capture")]
    [Trait("BR", "BR-OR-SUB-001")]
    public Task Test026_POST_BASE_URL_orders_ById_capture_Field_orderId_202() => AssertShellAsync(
        Method("POST"),
        $"/orders/{ResourceId}/capture",
        "{\"amount\":10.5,\"currency\":\"phase4c-test\",\"paymentReference\":\"phase4c-test\"}",
        202,
        requiredField: "orderId");

    // Source assertion 27: Contract error/conformance: POST /orders/{orderId}/capture
    // @BR-ID: BR-OR-SUB-001
    [Fact(DisplayName = "027: Contract error/conformance: POST /orders/{orderId}/capture")]
    [Trait("BR", "BR-OR-SUB-001")]
    public Task Test027_POST_BASE_URL_orders_ById_capture_Status_401() => AssertShellAsync(
        Method("POST"),
        $"/orders/{ResourceId}/capture",
        "{}",
        401,
        requiredField: null);

    // Source assertion 28: Contract success: POST /orders/{orderId}/fulfillment
    // @BR-ID: BR-OR-SUB-001
    [Fact(DisplayName = "028: Contract success: POST /orders/{orderId}/fulfillment")]
    [Trait("BR", "BR-OR-SUB-001")]
    public Task Test028_POST_BASE_URL_orders_ById_fulfillment_Field_orderId_202() => AssertShellAsync(
        Method("POST"),
        $"/orders/{ResourceId}/fulfillment",
        null,
        202,
        requiredField: "orderId");

    // Source assertion 29: Contract error/conformance: POST /orders/{orderId}/fulfillment
    // @BR-ID: BR-OR-SUB-001
    [Fact(DisplayName = "029: Contract error/conformance: POST /orders/{orderId}/fulfillment")]
    [Trait("BR", "BR-OR-SUB-001")]
    public Task Test029_POST_BASE_URL_orders_ById_fulfillment_Status_401() => AssertShellAsync(
        Method("POST"),
        $"/orders/{ResourceId}/fulfillment",
        null,
        401,
        requiredField: null);

    // Source assertion 30: Contract success: POST /orders/{orderId}/history
    // @BR-ID: BR-OR-SUB-001
    [Fact(DisplayName = "030: Contract success: POST /orders/{orderId}/history")]
    [Trait("BR", "BR-OR-SUB-001")]
    public Task Test030_POST_BASE_URL_orders_ById_history_Field_historyId_201() => AssertShellAsync(
        Method("POST"),
        $"/orders/{ResourceId}/history",
        "{\"status\":{},\"comments\":\"phase4c-test\",\"source\":\"Admin\"}",
        201,
        requiredField: "historyId");

    // Source assertion 31: Contract error/conformance: POST /orders/{orderId}/history
    // @BR-ID: BR-OR-SUB-001
    [Fact(DisplayName = "031: Contract error/conformance: POST /orders/{orderId}/history")]
    [Trait("BR", "BR-OR-SUB-001")]
    public Task Test031_POST_BASE_URL_orders_ById_history_Status_401() => AssertShellAsync(
        Method("POST"),
        $"/orders/{ResourceId}/history",
        "{}",
        401,
        requiredField: null);

    // Source assertion 32: Contract success: POST /orders/{orderId}/refund
    // @BR-ID: BR-OR-SUB-001
    [Fact(DisplayName = "032: Contract success: POST /orders/{orderId}/refund")]
    [Trait("BR", "BR-OR-SUB-001")]
    public Task Test032_POST_BASE_URL_orders_ById_refund_Field_orderId_202() => AssertShellAsync(
        Method("POST"),
        $"/orders/{ResourceId}/refund",
        "{\"amount\":10.5,\"currency\":\"phase4c-test\",\"reason\":\"phase4c-test\"}",
        202,
        requiredField: "orderId");

    // Source assertion 33: Contract error/conformance: POST /orders/{orderId}/refund
    // @BR-ID: BR-OR-SUB-001
    [Fact(DisplayName = "033: Contract error/conformance: POST /orders/{orderId}/refund")]
    [Trait("BR", "BR-OR-SUB-001")]
    public Task Test033_POST_BASE_URL_orders_ById_refund_Status_401() => AssertShellAsync(
        Method("POST"),
        $"/orders/{ResourceId}/refund",
        "{}",
        401,
        requiredField: null);

    // Source assertion 34: Contract success: GET /customers/{customerId}/orders
    // @BR-ID: BR-OR-SUB-001
    [Fact(DisplayName = "034: Contract success: GET /customers/{customerId}/orders")]
    [Trait("BR", "BR-OR-SUB-001")]
    public Task Test034_GET_BASE_URL_customers_ById_orders_Field_items_200() => AssertShellAsync(
        Method("GET"),
        $"/customers/{ResourceId}/orders",
        null,
        200,
        requiredField: "items");

    // Source assertion 35: Contract error/conformance: GET /customers/{customerId}/orders
    // @BR-ID: BR-OR-SUB-001
    [Fact(DisplayName = "035: Contract error/conformance: GET /customers/{customerId}/orders")]
    [Trait("BR", "BR-OR-SUB-001")]
    public Task Test035_GET_BASE_URL_customers_ById_orders_Status_401() => AssertShellAsync(
        Method("GET"),
        $"/customers/{ResourceId}/orders",
        null,
        401,
        requiredField: null);

    // Source assertion 36: Contract success: GET /me/orders
    // @BR-ID: BR-OR-SUB-001
    [Fact(DisplayName = "036: Contract success: GET /me/orders")]
    [Trait("BR", "BR-OR-SUB-001")]
    public Task Test036_GET_BASE_URL_me_orders_Field_items_200() => AssertShellAsync(
        Method("GET"),
        "/me/orders",
        null,
        200,
        requiredField: "items");

    // Source assertion 37: Contract error/conformance: GET /me/orders
    // @BR-ID: BR-OR-SUB-001
    [Fact(DisplayName = "037: Contract error/conformance: GET /me/orders")]
    [Trait("BR", "BR-OR-SUB-001")]
    public Task Test037_GET_BASE_URL_me_orders_Status_401() => AssertShellAsync(
        Method("GET"),
        "/me/orders",
        null,
        401,
        requiredField: null);

    // Source assertion 38: Contract success: GET /me/orders/{orderId}
    // @BR-ID: BR-OR-SUB-001
    [Fact(DisplayName = "038: Contract success: GET /me/orders/{orderId}")]
    [Trait("BR", "BR-OR-SUB-001")]
    public Task Test038_GET_BASE_URL_me_orders_ById_Status_200() => AssertShellAsync(
        Method("GET"),
        $"/me/orders/{ResourceId}",
        null,
        200,
        requiredField: null);

    // Source assertion 39: Contract error/conformance: GET /me/orders/{orderId}
    // @BR-ID: BR-OR-SUB-001
    [Fact(DisplayName = "039: Contract error/conformance: GET /me/orders/{orderId}")]
    [Trait("BR", "BR-OR-SUB-001")]
    public Task Test039_GET_BASE_URL_me_orders_ById_Status_401() => AssertShellAsync(
        Method("GET"),
        $"/me/orders/{ResourceId}",
        null,
        401,
        requiredField: null);

    // Source assertion 40: Contract success: GET /orders
    // @BR-ID: BR-OR-SUB-001
    [Fact(DisplayName = "040: Contract success: GET /orders")]
    [Trait("BR", "BR-OR-SUB-001")]
    public Task Test040_GET_BASE_URL_orders_Field_items_200() => AssertShellAsync(
        Method("GET"),
        "/orders",
        null,
        200,
        requiredField: "items");

    // Source assertion 41: Contract error/conformance: GET /orders
    // @BR-ID: BR-OR-SUB-001
    [Fact(DisplayName = "041: Contract error/conformance: GET /orders")]
    [Trait("BR", "BR-OR-SUB-001")]
    public Task Test041_GET_BASE_URL_orders_Status_401() => AssertShellAsync(
        Method("GET"),
        "/orders",
        null,
        401,
        requiredField: null);

    // Source assertion 42: Contract success: GET /orders/capturable
    // @BR-ID: BR-OR-SUB-001
    [Fact(DisplayName = "042: Contract success: GET /orders/capturable")]
    [Trait("BR", "BR-OR-SUB-001")]
    public Task Test042_GET_BASE_URL_orders_capturable_Field_items_200() => AssertShellAsync(
        Method("GET"),
        "/orders/capturable",
        null,
        200,
        requiredField: "items");

    // Source assertion 43: Contract error/conformance: GET /orders/capturable
    // @BR-ID: BR-OR-SUB-001
    [Fact(DisplayName = "043: Contract error/conformance: GET /orders/capturable")]
    [Trait("BR", "BR-OR-SUB-001")]
    public Task Test043_GET_BASE_URL_orders_capturable_Status_401() => AssertShellAsync(
        Method("GET"),
        "/orders/capturable",
        null,
        401,
        requiredField: null);

    // Source assertion 44: Contract success: GET /orders/{orderId}
    // @BR-ID: BR-OR-SUB-001
    [Fact(DisplayName = "044: Contract success: GET /orders/{orderId}")]
    [Trait("BR", "BR-OR-SUB-001")]
    public Task Test044_GET_BASE_URL_orders_ById_Field_orderId_200() => AssertShellAsync(
        Method("GET"),
        $"/orders/{ResourceId}",
        null,
        200,
        requiredField: "orderId");

    // Source assertion 45: Contract error/conformance: GET /orders/{orderId}
    // @BR-ID: BR-OR-SUB-001
    [Fact(DisplayName = "045: Contract error/conformance: GET /orders/{orderId}")]
    [Trait("BR", "BR-OR-SUB-001")]
    public Task Test045_GET_BASE_URL_orders_ById_Status_401() => AssertShellAsync(
        Method("GET"),
        $"/orders/{ResourceId}",
        null,
        401,
        requiredField: null);

    // Source assertion 46: Contract success: GET /orders/{orderId}/fulfillment
    // @BR-ID: BR-OR-SUB-001
    [Fact(DisplayName = "046: Contract success: GET /orders/{orderId}/fulfillment")]
    [Trait("BR", "BR-OR-SUB-001")]
    public Task Test046_GET_BASE_URL_orders_ById_fulfillment_Field_orderId_200() => AssertShellAsync(
        Method("GET"),
        $"/orders/{ResourceId}/fulfillment",
        null,
        200,
        requiredField: "orderId");

    // Source assertion 47: Contract error/conformance: GET /orders/{orderId}/fulfillment
    // @BR-ID: BR-OR-SUB-001
    [Fact(DisplayName = "047: Contract error/conformance: GET /orders/{orderId}/fulfillment")]
    [Trait("BR", "BR-OR-SUB-001")]
    public Task Test047_GET_BASE_URL_orders_ById_fulfillment_Status_401() => AssertShellAsync(
        Method("GET"),
        $"/orders/{ResourceId}/fulfillment",
        null,
        401,
        requiredField: null);

    // Source assertion 48: Contract success: GET /orders/{orderId}/history
    // @BR-ID: BR-OR-SUB-001
    [Fact(DisplayName = "048: Contract success: GET /orders/{orderId}/history")]
    [Trait("BR", "BR-OR-SUB-001")]
    public Task Test048_GET_BASE_URL_orders_ById_history_Field_items_200() => AssertShellAsync(
        Method("GET"),
        $"/orders/{ResourceId}/history",
        null,
        200,
        requiredField: "items");

    // Source assertion 49: Contract error/conformance: GET /orders/{orderId}/history
    // @BR-ID: BR-OR-SUB-001
    [Fact(DisplayName = "049: Contract error/conformance: GET /orders/{orderId}/history")]
    [Trait("BR", "BR-OR-SUB-001")]
    public Task Test049_GET_BASE_URL_orders_ById_history_Status_401() => AssertShellAsync(
        Method("GET"),
        $"/orders/{ResourceId}/history",
        null,
        401,
        requiredField: null);

    // Source assertion 50: Contract success: GET /orders/{orderId}/invoice
    // @BR-ID: BR-OR-SUB-001
    [Fact(DisplayName = "050: Contract success: GET /orders/{orderId}/invoice")]
    [Trait("BR", "BR-OR-SUB-001")]
    public Task Test050_GET_BASE_URL_orders_ById_invoice_Field_orderId_200() => AssertShellAsync(
        Method("GET"),
        $"/orders/{ResourceId}/invoice",
        null,
        200,
        requiredField: "orderId");

    // Source assertion 51: Contract error/conformance: GET /orders/{orderId}/invoice
    // @BR-ID: BR-OR-SUB-001
    [Fact(DisplayName = "051: Contract error/conformance: GET /orders/{orderId}/invoice")]
    [Trait("BR", "BR-OR-SUB-001")]
    public Task Test051_GET_BASE_URL_orders_ById_invoice_Status_401() => AssertShellAsync(
        Method("GET"),
        $"/orders/{ResourceId}/invoice",
        null,
        401,
        requiredField: null);

    // Source assertion 52: Contract success: GET /orders/{orderId}/payment-transactions
    // @BR-ID: BR-OR-SUB-001
    [Fact(DisplayName = "052: Contract success: GET /orders/{orderId}/payment-transactions")]
    [Trait("BR", "BR-OR-SUB-001")]
    public Task Test052_GET_BASE_URL_orders_ById_payment_transactions_Field_items_200() => AssertShellAsync(
        Method("GET"),
        $"/orders/{ResourceId}/payment-transactions",
        null,
        200,
        requiredField: "items");

    // Source assertion 53: Contract error/conformance: GET /orders/{orderId}/payment-transactions
    // @BR-ID: BR-OR-SUB-001
    [Fact(DisplayName = "053: Contract error/conformance: GET /orders/{orderId}/payment-transactions")]
    [Trait("BR", "BR-OR-SUB-001")]
    public Task Test053_GET_BASE_URL_orders_ById_payment_transactions_Status_401() => AssertShellAsync(
        Method("GET"),
        $"/orders/{ResourceId}/payment-transactions",
        null,
        401,
        requiredField: null);

    // Source assertion 54: Contract success: GET /orders/{orderId}/payment/next-action
    // @BR-ID: BR-OR-SUB-001
    [Fact(DisplayName = "054: Contract success: GET /orders/{orderId}/payment/next-action")]
    [Trait("BR", "BR-OR-SUB-001")]
    public Task Test054_GET_BASE_URL_orders_ById_payment_next_action_Field_orderId_200() => AssertShellAsync(
        Method("GET"),
        $"/orders/{ResourceId}/payment/next-action",
        null,
        200,
        requiredField: "orderId");

    // Source assertion 55: Contract error/conformance: GET /orders/{orderId}/payment/next-action
    // @BR-ID: BR-OR-SUB-001
    [Fact(DisplayName = "055: Contract error/conformance: GET /orders/{orderId}/payment/next-action")]
    [Trait("BR", "BR-OR-SUB-001")]
    public Task Test055_GET_BASE_URL_orders_ById_payment_next_action_Status_401() => AssertShellAsync(
        Method("GET"),
        $"/orders/{ResourceId}/payment/next-action",
        null,
        401,
        requiredField: null);

    // Source assertion 56: Contract success: PATCH /orders/{orderId}/customer-snapshot
    // @BR-ID: BR-OR-SUB-001
    [Fact(DisplayName = "056: Contract success: PATCH /orders/{orderId}/customer-snapshot")]
    [Trait("BR", "BR-OR-SUB-001")]
    public Task Test056_PATCH_BASE_URL_orders_ById_customer_snapshot_Field_orderId_200() => AssertShellAsync(
        Method("PATCH"),
        $"/orders/{ResourceId}/customer-snapshot",
        "{\"emailAddress\":\"phase4c@example.com\",\"billingAddress\":{\"firstName\":\"phase4c-test\",\"lastName\":\"phase4c-test\",\"company\":\"phase4c-test\",\"address\":\"phase4c-test\",\"city\":\"phase4c-test\",\"state\":\"phase4c-test\",\"countryCode\":\"phase4c-test\",\"zoneCode\":\"phase4c-test\",\"postalCode\":\"phase4c-test\",\"telephone\":\"phase4c-test\"},\"deliveryAddress\":{\"firstName\":\"phase4c-test\",\"lastName\":\"phase4c-test\",\"company\":\"phase4c-test\",\"address\":\"phase4c-test\",\"city\":\"phase4c-test\",\"state\":\"phase4c-test\",\"countryCode\":\"phase4c-test\",\"zoneCode\":\"phase4c-test\",\"postalCode\":\"phase4c-test\",\"telephone\":\"phase4c-test\"}}",
        200,
        requiredField: "orderId");

    // Source assertion 57: Contract error/conformance: PATCH /orders/{orderId}/customer-snapshot
    // @BR-ID: BR-OR-SUB-001
    [Fact(DisplayName = "057: Contract error/conformance: PATCH /orders/{orderId}/customer-snapshot")]
    [Trait("BR", "BR-OR-SUB-001")]
    public Task Test057_PATCH_BASE_URL_orders_ById_customer_snapshot_Status_401() => AssertShellAsync(
        Method("PATCH"),
        $"/orders/{ResourceId}/customer-snapshot",
        "{}",
        401,
        requiredField: null);

    // Source assertion 58: Contract success: PUT /orders/{orderId}/status
    // @BR-ID: BR-OR-SUB-001
    [Fact(DisplayName = "058: Contract success: PUT /orders/{orderId}/status")]
    [Trait("BR", "BR-OR-SUB-001")]
    public Task Test058_PUT_BASE_URL_orders_ById_status_Field_orderId_200() => AssertShellAsync(
        Method("PUT"),
        $"/orders/{ResourceId}/status",
        "{\"status\":{},\"reason\":\"phase4c-test\"}",
        200,
        requiredField: "orderId");

    // Source assertion 59: Contract error/conformance: PUT /orders/{orderId}/status
    // @BR-ID: BR-OR-SUB-001
    [Fact(DisplayName = "059: Contract error/conformance: PUT /orders/{orderId}/status")]
    [Trait("BR", "BR-OR-SUB-001")]
    public Task Test059_PUT_BASE_URL_orders_ById_status_Status_401() => AssertShellAsync(
        Method("PUT"),
        $"/orders/{ResourceId}/status",
        "{}",
        401,
        requiredField: null);
}
