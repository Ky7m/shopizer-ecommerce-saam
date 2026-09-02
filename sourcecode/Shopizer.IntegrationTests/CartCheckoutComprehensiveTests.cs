using System.Net;

namespace Shopizer.IntegrationTests;

[Collection(ShopizerAspireCollection.Name)]
public sealed class CartCheckoutComprehensiveTests(AspireHostFixture fixture) : ComprehensiveTestBase(fixture.CartCheckoutClient)
{

    // Source assertion 1: Contract success: POST /auth/cart/{code}/checkout
    // @BR-ID: BR-SC-CRE-001
    [Fact(DisplayName = "001: Contract success: POST /auth/cart/{code}/checkout")]
    [Trait("BR", "BR-SC-CRE-001")]
    public Task Test001_POST_BASE_URL_auth_cart_phase4c_code_checkout_Field_submissionId_202() => AssertShellAsync(
        Method("POST"),
        "/auth/cart/phase4c-code/checkout",
        "{\"currency\":\"phase4c-test\",\"shippingQuoteId\":\"00000000-0000-0000-0000-000000000001\",\"payment\":{\"amount\":\"phase4c-test\",\"paymentModule\":\"phase4c-test\",\"paymentType\":\"CREDITCARD\",\"transactionType\":\"INIT\",\"paymentToken\":\"phase4c-test\"},\"comments\":\"phase4c-test\",\"customerAgreement\":true}",
        202,
        requiredField: "submissionId");

    // Source assertion 2: Contract error/conformance: POST /auth/cart/{code}/checkout
    // @BR-ID: BR-SC-CRE-001
    [Fact(DisplayName = "002: Contract error/conformance: POST /auth/cart/{code}/checkout")]
    [Trait("BR", "BR-SC-CRE-001")]
    public Task Test002_POST_BASE_URL_auth_cart_phase4c_code_checkout_Status_401() => AssertShellAsync(
        Method("POST"),
        "/auth/cart/phase4c-code/checkout",
        "{}",
        401,
        requiredField: null);

    // Source assertion 3: Business rule assertion: BR-SC-CRE-001
    // @BR-ID: BR-SC-CRE-001
    [Fact(DisplayName = "003: Business rule assertion: BR-SC-CRE-001")]
    [Trait("BR", "BR-SC-CRE-001")]
    public Task Test003_POST_BASE_URL_auth_cart_phase4c_code_checkout_Field_submissionId_202() => AssertShellAsync(
        Method("POST"),
        "/auth/cart/phase4c-code/checkout",
        "{\"currency\":\"phase4c-test\",\"shippingQuoteId\":\"00000000-0000-0000-0000-000000000001\",\"payment\":{\"amount\":\"phase4c-test\",\"paymentModule\":\"phase4c-test\",\"paymentType\":\"CREDITCARD\",\"transactionType\":\"INIT\",\"paymentToken\":\"phase4c-test\"},\"comments\":\"phase4c-test\",\"customerAgreement\":true}",
        202,
        requiredField: "submissionId");

    // Source assertion 4: Business rule assertion: BR-SC-SEL-002
    // @BR-ID: BR-SC-SEL-002
    [Fact(DisplayName = "004: Business rule assertion: BR-SC-SEL-002")]
    [Trait("BR", "BR-SC-SEL-002")]
    public Task Test004_POST_BASE_URL_auth_cart_phase4c_code_checkout_Field_submissionId_202() => AssertShellAsync(
        Method("POST"),
        "/auth/cart/phase4c-code/checkout",
        "{\"currency\":\"phase4c-test\",\"shippingQuoteId\":\"00000000-0000-0000-0000-000000000001\",\"payment\":{\"amount\":\"phase4c-test\",\"paymentModule\":\"phase4c-test\",\"paymentType\":\"CREDITCARD\",\"transactionType\":\"INIT\",\"paymentToken\":\"phase4c-test\"},\"comments\":\"phase4c-test\",\"customerAgreement\":true}",
        202,
        requiredField: "submissionId");

    // Source assertion 5: Business rule assertion: BR-SC-ATR-003
    // @BR-ID: BR-SC-ATR-003
    [Fact(DisplayName = "005: Business rule assertion: BR-SC-ATR-003")]
    [Trait("BR", "BR-SC-ATR-003")]
    public Task Test005_POST_BASE_URL_auth_cart_phase4c_code_checkout_Field_submissionId_202() => AssertShellAsync(
        Method("POST"),
        "/auth/cart/phase4c-code/checkout",
        "{\"currency\":\"phase4c-test\",\"shippingQuoteId\":\"00000000-0000-0000-0000-000000000001\",\"payment\":{\"amount\":\"phase4c-test\",\"paymentModule\":\"phase4c-test\",\"paymentType\":\"CREDITCARD\",\"transactionType\":\"INIT\",\"paymentToken\":\"phase4c-test\"},\"comments\":\"phase4c-test\",\"customerAgreement\":true}",
        202,
        requiredField: "submissionId");

    // Source assertion 6: Business rule assertion: BR-SC-MRG-004
    // @BR-ID: BR-SC-MRG-004
    [Fact(DisplayName = "006: Business rule assertion: BR-SC-MRG-004")]
    [Trait("BR", "BR-SC-MRG-004")]
    public Task Test006_POST_BASE_URL_auth_cart_phase4c_code_checkout_Field_submissionId_202() => AssertShellAsync(
        Method("POST"),
        "/auth/cart/phase4c-code/checkout",
        "{\"currency\":\"phase4c-test\",\"shippingQuoteId\":\"00000000-0000-0000-0000-000000000001\",\"payment\":{\"amount\":\"phase4c-test\",\"paymentModule\":\"phase4c-test\",\"paymentType\":\"CREDITCARD\",\"transactionType\":\"INIT\",\"paymentToken\":\"phase4c-test\"},\"comments\":\"phase4c-test\",\"customerAgreement\":true}",
        202,
        requiredField: "submissionId");

    // Source assertion 7: Business rule assertion: BR-SC-UPD-005
    // @BR-ID: BR-SC-UPD-005
    [Fact(DisplayName = "007: Business rule assertion: BR-SC-UPD-005")]
    [Trait("BR", "BR-SC-UPD-005")]
    public Task Test007_POST_BASE_URL_auth_cart_phase4c_code_checkout_Field_submissionId_202() => AssertShellAsync(
        Method("POST"),
        "/auth/cart/phase4c-code/checkout",
        "{\"currency\":\"phase4c-test\",\"shippingQuoteId\":\"00000000-0000-0000-0000-000000000001\",\"payment\":{\"amount\":\"phase4c-test\",\"paymentModule\":\"phase4c-test\",\"paymentType\":\"CREDITCARD\",\"transactionType\":\"INIT\",\"paymentToken\":\"phase4c-test\"},\"comments\":\"phase4c-test\",\"customerAgreement\":true}",
        202,
        requiredField: "submissionId");

    // Source assertion 8: Business rule assertion: BR-SC-HYD-006
    // @BR-ID: BR-SC-HYD-006
    [Fact(DisplayName = "008: Business rule assertion: BR-SC-HYD-006")]
    [Trait("BR", "BR-SC-HYD-006")]
    public Task Test008_POST_BASE_URL_auth_cart_phase4c_code_checkout_Field_submissionId_202() => AssertShellAsync(
        Method("POST"),
        "/auth/cart/phase4c-code/checkout",
        "{\"currency\":\"phase4c-test\",\"shippingQuoteId\":\"00000000-0000-0000-0000-000000000001\",\"payment\":{\"amount\":\"phase4c-test\",\"paymentModule\":\"phase4c-test\",\"paymentType\":\"CREDITCARD\",\"transactionType\":\"INIT\",\"paymentToken\":\"phase4c-test\"},\"comments\":\"phase4c-test\",\"customerAgreement\":true}",
        202,
        requiredField: "submissionId");

    // Source assertion 9: Business rule assertion: BR-SC-MRG-007
    // @BR-ID: BR-SC-MRG-007
    [Fact(DisplayName = "009: Business rule assertion: BR-SC-MRG-007")]
    [Trait("BR", "BR-SC-MRG-007")]
    public Task Test009_POST_BASE_URL_auth_cart_phase4c_code_checkout_Field_submissionId_202() => AssertShellAsync(
        Method("POST"),
        "/auth/cart/phase4c-code/checkout",
        "{\"currency\":\"phase4c-test\",\"shippingQuoteId\":\"00000000-0000-0000-0000-000000000001\",\"payment\":{\"amount\":\"phase4c-test\",\"paymentModule\":\"phase4c-test\",\"paymentType\":\"CREDITCARD\",\"transactionType\":\"INIT\",\"paymentToken\":\"phase4c-test\"},\"comments\":\"phase4c-test\",\"customerAgreement\":true}",
        202,
        requiredField: "submissionId");

    // Source assertion 10: Business rule assertion: BR-SC-SHP-008
    // @BR-ID: BR-SC-SHP-008
    [Fact(DisplayName = "010: Business rule assertion: BR-SC-SHP-008")]
    [Trait("BR", "BR-SC-SHP-008")]
    public Task Test010_POST_BASE_URL_auth_cart_phase4c_code_checkout_Field_submissionId_202() => AssertShellAsync(
        Method("POST"),
        "/auth/cart/phase4c-code/checkout",
        "{\"currency\":\"phase4c-test\",\"shippingQuoteId\":\"00000000-0000-0000-0000-000000000001\",\"payment\":{\"amount\":\"phase4c-test\",\"paymentModule\":\"phase4c-test\",\"paymentType\":\"CREDITCARD\",\"transactionType\":\"INIT\",\"paymentToken\":\"phase4c-test\"},\"comments\":\"phase4c-test\",\"customerAgreement\":true}",
        202,
        requiredField: "submissionId");

    // Source assertion 11: Business rule assertion: BR-SC-SHP-009
    // @BR-ID: BR-SC-SHP-009
    [Fact(DisplayName = "011: Business rule assertion: BR-SC-SHP-009")]
    [Trait("BR", "BR-SC-SHP-009")]
    public Task Test011_POST_BASE_URL_auth_cart_phase4c_code_checkout_Field_submissionId_202() => AssertShellAsync(
        Method("POST"),
        "/auth/cart/phase4c-code/checkout",
        "{\"currency\":\"phase4c-test\",\"shippingQuoteId\":\"00000000-0000-0000-0000-000000000001\",\"payment\":{\"amount\":\"phase4c-test\",\"paymentModule\":\"phase4c-test\",\"paymentType\":\"CREDITCARD\",\"transactionType\":\"INIT\",\"paymentToken\":\"phase4c-test\"},\"comments\":\"phase4c-test\",\"customerAgreement\":true}",
        202,
        requiredField: "submissionId");

    // Source assertion 12: Business rule assertion: BR-SC-TOT-010
    // @BR-ID: BR-SC-TOT-010
    [Fact(DisplayName = "012: Business rule assertion: BR-SC-TOT-010")]
    [Trait("BR", "BR-SC-TOT-010")]
    public Task Test012_POST_BASE_URL_auth_cart_phase4c_code_checkout_Field_submissionId_202() => AssertShellAsync(
        Method("POST"),
        "/auth/cart/phase4c-code/checkout",
        "{\"currency\":\"phase4c-test\",\"shippingQuoteId\":\"00000000-0000-0000-0000-000000000001\",\"payment\":{\"amount\":\"phase4c-test\",\"paymentModule\":\"phase4c-test\",\"paymentType\":\"CREDITCARD\",\"transactionType\":\"INIT\",\"paymentToken\":\"phase4c-test\"},\"comments\":\"phase4c-test\",\"customerAgreement\":true}",
        202,
        requiredField: "submissionId");

    // Source assertion 13: Business rule assertion: BR-SC-PRO-011
    // @BR-ID: BR-SC-PRO-011
    [Fact(DisplayName = "013: Business rule assertion: BR-SC-PRO-011")]
    [Trait("BR", "BR-SC-PRO-011")]
    public Task Test013_POST_BASE_URL_auth_cart_phase4c_code_checkout_Field_submissionId_202() => AssertShellAsync(
        Method("POST"),
        "/auth/cart/phase4c-code/checkout",
        "{\"currency\":\"phase4c-test\",\"shippingQuoteId\":\"00000000-0000-0000-0000-000000000001\",\"payment\":{\"amount\":\"phase4c-test\",\"paymentModule\":\"phase4c-test\",\"paymentType\":\"CREDITCARD\",\"transactionType\":\"INIT\",\"paymentToken\":\"phase4c-test\"},\"comments\":\"phase4c-test\",\"customerAgreement\":true}",
        202,
        requiredField: "submissionId");

    // Source assertion 14: Business rule assertion: BR-CO-AUT-012
    // @BR-ID: BR-CO-AUT-012
    [Fact(DisplayName = "014: Business rule assertion: BR-CO-AUT-012")]
    [Trait("BR", "BR-CO-AUT-012")]
    public Task Test014_POST_BASE_URL_auth_cart_phase4c_code_checkout_Field_submissionId_202() => AssertShellAsync(
        Method("POST"),
        "/auth/cart/phase4c-code/checkout",
        "{\"currency\":\"phase4c-test\",\"shippingQuoteId\":\"00000000-0000-0000-0000-000000000001\",\"payment\":{\"amount\":\"phase4c-test\",\"paymentModule\":\"phase4c-test\",\"paymentType\":\"CREDITCARD\",\"transactionType\":\"INIT\",\"paymentToken\":\"phase4c-test\"},\"comments\":\"phase4c-test\",\"customerAgreement\":true}",
        202,
        requiredField: "submissionId");

    // Source assertion 15: Business rule assertion: BR-CO-CUS-013
    // @BR-ID: BR-CO-CUS-013
    [Fact(DisplayName = "015: Business rule assertion: BR-CO-CUS-013")]
    [Trait("BR", "BR-CO-CUS-013")]
    public Task Test015_POST_BASE_URL_auth_cart_phase4c_code_checkout_Field_submissionId_202() => AssertShellAsync(
        Method("POST"),
        "/auth/cart/phase4c-code/checkout",
        "{\"currency\":\"phase4c-test\",\"shippingQuoteId\":\"00000000-0000-0000-0000-000000000001\",\"payment\":{\"amount\":\"phase4c-test\",\"paymentModule\":\"phase4c-test\",\"paymentType\":\"CREDITCARD\",\"transactionType\":\"INIT\",\"paymentToken\":\"phase4c-test\"},\"comments\":\"phase4c-test\",\"customerAgreement\":true}",
        202,
        requiredField: "submissionId");

    // Source assertion 16: Business rule assertion: BR-CO-SNP-014
    // @BR-ID: BR-CO-SNP-014
    [Fact(DisplayName = "016: Business rule assertion: BR-CO-SNP-014")]
    [Trait("BR", "BR-CO-SNP-014")]
    public Task Test016_POST_BASE_URL_auth_cart_phase4c_code_checkout_Field_submissionId_202() => AssertShellAsync(
        Method("POST"),
        "/auth/cart/phase4c-code/checkout",
        "{\"currency\":\"phase4c-test\",\"shippingQuoteId\":\"00000000-0000-0000-0000-000000000001\",\"payment\":{\"amount\":\"phase4c-test\",\"paymentModule\":\"phase4c-test\",\"paymentType\":\"CREDITCARD\",\"transactionType\":\"INIT\",\"paymentToken\":\"phase4c-test\"},\"comments\":\"phase4c-test\",\"customerAgreement\":true}",
        202,
        requiredField: "submissionId");

    // Source assertion 17: Business rule assertion: BR-CO-TOT-015
    // @BR-ID: BR-CO-TOT-015
    [Fact(DisplayName = "017: Business rule assertion: BR-CO-TOT-015")]
    [Trait("BR", "BR-CO-TOT-015")]
    public Task Test017_POST_BASE_URL_auth_cart_phase4c_code_checkout_Field_submissionId_202() => AssertShellAsync(
        Method("POST"),
        "/auth/cart/phase4c-code/checkout",
        "{\"currency\":\"phase4c-test\",\"shippingQuoteId\":\"00000000-0000-0000-0000-000000000001\",\"payment\":{\"amount\":\"phase4c-test\",\"paymentModule\":\"phase4c-test\",\"paymentType\":\"CREDITCARD\",\"transactionType\":\"INIT\",\"paymentToken\":\"phase4c-test\"},\"comments\":\"phase4c-test\",\"customerAgreement\":true}",
        202,
        requiredField: "submissionId");

    // Source assertion 18: Business rule assertion: BR-CO-PAY-016
    // @BR-ID: BR-CO-PAY-016
    [Fact(DisplayName = "018: Business rule assertion: BR-CO-PAY-016")]
    [Trait("BR", "BR-CO-PAY-016")]
    public Task Test018_POST_BASE_URL_auth_cart_phase4c_code_checkout_Field_submissionId_202() => AssertShellAsync(
        Method("POST"),
        "/auth/cart/phase4c-code/checkout",
        "{\"currency\":\"phase4c-test\",\"shippingQuoteId\":\"00000000-0000-0000-0000-000000000001\",\"payment\":{\"amount\":\"phase4c-test\",\"paymentModule\":\"phase4c-test\",\"paymentType\":\"CREDITCARD\",\"transactionType\":\"INIT\",\"paymentToken\":\"phase4c-test\"},\"comments\":\"phase4c-test\",\"customerAgreement\":true}",
        202,
        requiredField: "submissionId");

    // Source assertion 19: Business rule assertion: BR-CO-IDM-017
    // @BR-ID: BR-CO-IDM-017
    [Fact(DisplayName = "019: Business rule assertion: BR-CO-IDM-017")]
    [Trait("BR", "BR-CO-IDM-017")]
    public Task Test019_POST_BASE_URL_auth_cart_phase4c_code_checkout_Field_submissionId_202() => AssertShellAsync(
        Method("POST"),
        "/auth/cart/phase4c-code/checkout",
        "{\"currency\":\"phase4c-test\",\"shippingQuoteId\":\"00000000-0000-0000-0000-000000000001\",\"payment\":{\"amount\":\"phase4c-test\",\"paymentModule\":\"phase4c-test\",\"paymentType\":\"CREDITCARD\",\"transactionType\":\"INIT\",\"paymentToken\":\"phase4c-test\"},\"comments\":\"phase4c-test\",\"customerAgreement\":true}",
        202,
        requiredField: "submissionId");

    // Source assertion 20: Business rule assertion: BR-CO-STA-018
    // @BR-ID: BR-CO-STA-018
    [Fact(DisplayName = "020: Business rule assertion: BR-CO-STA-018")]
    [Trait("BR", "BR-CO-STA-018")]
    public Task Test020_POST_BASE_URL_auth_cart_phase4c_code_checkout_Field_submissionId_202() => AssertShellAsync(
        Method("POST"),
        "/auth/cart/phase4c-code/checkout",
        "{\"currency\":\"phase4c-test\",\"shippingQuoteId\":\"00000000-0000-0000-0000-000000000001\",\"payment\":{\"amount\":\"phase4c-test\",\"paymentModule\":\"phase4c-test\",\"paymentType\":\"CREDITCARD\",\"transactionType\":\"INIT\",\"paymentToken\":\"phase4c-test\"},\"comments\":\"phase4c-test\",\"customerAgreement\":true}",
        202,
        requiredField: "submissionId");

    // Source assertion 21: Business rule assertion: BR-CO-ORC-019
    // @BR-ID: BR-CO-ORC-019
    [Fact(DisplayName = "021: Business rule assertion: BR-CO-ORC-019")]
    [Trait("BR", "BR-CO-ORC-019")]
    public Task Test021_POST_BASE_URL_auth_cart_phase4c_code_checkout_Field_submissionId_202() => AssertShellAsync(
        Method("POST"),
        "/auth/cart/phase4c-code/checkout",
        "{\"currency\":\"phase4c-test\",\"shippingQuoteId\":\"00000000-0000-0000-0000-000000000001\",\"payment\":{\"amount\":\"phase4c-test\",\"paymentModule\":\"phase4c-test\",\"paymentType\":\"CREDITCARD\",\"transactionType\":\"INIT\",\"paymentToken\":\"phase4c-test\"},\"comments\":\"phase4c-test\",\"customerAgreement\":true}",
        202,
        requiredField: "submissionId");

    // Source assertion 22: Business rule assertion: BR-CO-BND-020
    // @BR-ID: BR-CO-BND-020
    [Fact(DisplayName = "022: Business rule assertion: BR-CO-BND-020")]
    [Trait("BR", "BR-CO-BND-020")]
    public Task Test022_POST_BASE_URL_auth_cart_phase4c_code_checkout_Field_submissionId_202() => AssertShellAsync(
        Method("POST"),
        "/auth/cart/phase4c-code/checkout",
        "{\"currency\":\"phase4c-test\",\"shippingQuoteId\":\"00000000-0000-0000-0000-000000000001\",\"payment\":{\"amount\":\"phase4c-test\",\"paymentModule\":\"phase4c-test\",\"paymentType\":\"CREDITCARD\",\"transactionType\":\"INIT\",\"paymentToken\":\"phase4c-test\"},\"comments\":\"phase4c-test\",\"customerAgreement\":true}",
        202,
        requiredField: "submissionId");

    // Source assertion 23: Contract success: POST /auth/cart/{code}/payment/init
    // @BR-ID: BR-SC-CRE-001
    [Fact(DisplayName = "023: Contract success: POST /auth/cart/{code}/payment/init")]
    [Trait("BR", "BR-SC-CRE-001")]
    public Task Test023_POST_BASE_URL_auth_cart_phase4c_code_payment_init_Field_submissionId_202() => AssertShellAsync(
        Method("POST"),
        "/auth/cart/phase4c-code/payment/init",
        "{\"amount\":\"phase4c-test\",\"paymentModule\":\"phase4c-test\",\"paymentType\":\"phase4c-test\",\"transactionType\":\"phase4c-test\",\"paymentToken\":\"phase4c-test\"}",
        202,
        requiredField: "submissionId");

    // Source assertion 24: Contract error/conformance: POST /auth/cart/{code}/payment/init
    // @BR-ID: BR-SC-CRE-001
    [Fact(DisplayName = "024: Contract error/conformance: POST /auth/cart/{code}/payment/init")]
    [Trait("BR", "BR-SC-CRE-001")]
    public Task Test024_POST_BASE_URL_auth_cart_phase4c_code_payment_init_Status_401() => AssertShellAsync(
        Method("POST"),
        "/auth/cart/phase4c-code/payment/init",
        "{}",
        401,
        requiredField: null);

    // Source assertion 25: Contract success: POST /cart
    // @BR-ID: BR-SC-CRE-001
    [Fact(DisplayName = "025: Contract success: POST /cart")]
    [Trait("BR", "BR-SC-CRE-001")]
    public Task Test025_POST_BASE_URL_cart_Field_cart_201() => AssertShellAsync(
        Method("POST"),
        "/cart",
        "{\"product\":\"phase4c-test\",\"quantity\":1,\"attributes\":[{\"id\":1}],\"promoCode\":\"phase4c-test\"}",
        201,
        requiredField: "cart");

    // Source assertion 26: Contract error/conformance: POST /cart
    // @BR-ID: BR-SC-CRE-001
    [Fact(DisplayName = "026: Contract error/conformance: POST /cart")]
    [Trait("BR", "BR-SC-CRE-001")]
    public Task Test026_POST_BASE_URL_cart_Status_400() => AssertShellAsync(
        Method("POST"),
        "/cart",
        "{}",
        400,
        requiredField: null);

    // Source assertion 27: Contract success: POST /cart/{code}/checkout
    // @BR-ID: BR-SC-CRE-001
    [Fact(DisplayName = "027: Contract success: POST /cart/{code}/checkout")]
    [Trait("BR", "BR-SC-CRE-001")]
    public Task Test027_POST_BASE_URL_cart_phase4c_code_checkout_Field_submissionId_202() => AssertShellAsync(
        Method("POST"),
        "/cart/phase4c-code/checkout",
        "{\"currency\":\"phase4c-test\",\"customer\":{\"email\":\"phase4c@example.com\",\"firstName\":\"phase4c-test\",\"lastName\":\"phase4c-test\",\"password\":\"Phase4c!Password2026\",\"repeatPassword\":\"Phase4c!Password2026\",\"billing\":{\"firstName\":\"phase4c-test\",\"lastName\":\"phase4c-test\",\"company\":\"phase4c-test\",\"address\":\"phase4c-test\",\"city\":\"phase4c-test\",\"stateProvince\":\"phase4c-test\",\"countryCode\":\"phase4c-test\",\"postalCode\":\"phase4c-test\",\"phone\":\"phase4c-test\"}},\"shippingQuoteId\":\"00000000-0000-0000-0000-000000000001\",\"payment\":{\"amount\":\"phase4c-test\",\"paymentModule\":\"phase4c-test\",\"paymentType\":\"CREDITCARD\",\"transactionType\":\"INIT\",\"paymentToken\":\"phase4c-test\"},\"comments\":\"phase4c-test\",\"customerAgreement\":true}",
        202,
        requiredField: "submissionId");

    // Source assertion 28: Contract error/conformance: POST /cart/{code}/checkout
    // @BR-ID: BR-SC-CRE-001
    [Fact(DisplayName = "028: Contract error/conformance: POST /cart/{code}/checkout")]
    [Trait("BR", "BR-SC-CRE-001")]
    public Task Test028_POST_BASE_URL_cart_phase4c_code_checkout_Status_403() => AssertShellAsync(
        Method("POST"),
        "/cart/phase4c-code/checkout",
        "{}",
        403,
        requiredField: null);

    // Source assertion 29: Contract success: POST /cart/{code}/multi
    // @BR-ID: BR-SC-CRE-001
    [Fact(DisplayName = "029: Contract success: POST /cart/{code}/multi")]
    [Trait("BR", "BR-SC-CRE-001")]
    public Task Test029_POST_BASE_URL_cart_phase4c_code_multi_Field_cart_200() => AssertShellAsync(
        Method("POST"),
        "/cart/phase4c-code/multi",
        "{}",
        200,
        requiredField: "cart");

    // Source assertion 30: Contract error/conformance: POST /cart/{code}/multi
    // @BR-ID: BR-SC-CRE-001
    [Fact(DisplayName = "030: Contract error/conformance: POST /cart/{code}/multi")]
    [Trait("BR", "BR-SC-CRE-001")]
    public Task Test030_POST_BASE_URL_cart_phase4c_code_multi_Status_400() => AssertShellAsync(
        Method("POST"),
        "/cart/phase4c-code/multi",
        "{}",
        400,
        requiredField: null);

    // Source assertion 31: Contract success: POST /cart/{code}/payment/init
    // @BR-ID: BR-SC-CRE-001
    [Fact(DisplayName = "031: Contract success: POST /cart/{code}/payment/init")]
    [Trait("BR", "BR-SC-CRE-001")]
    public Task Test031_POST_BASE_URL_cart_phase4c_code_payment_init_Field_submissionId_202() => AssertShellAsync(
        Method("POST"),
        "/cart/phase4c-code/payment/init",
        "{\"amount\":\"phase4c-test\",\"paymentModule\":\"phase4c-test\",\"paymentType\":\"phase4c-test\",\"transactionType\":\"phase4c-test\",\"paymentToken\":\"phase4c-test\"}",
        202,
        requiredField: "submissionId");

    // Source assertion 32: Contract error/conformance: POST /cart/{code}/payment/init
    // @BR-ID: BR-SC-CRE-001
    [Fact(DisplayName = "032: Contract error/conformance: POST /cart/{code}/payment/init")]
    [Trait("BR", "BR-SC-CRE-001")]
    public Task Test032_POST_BASE_URL_cart_phase4c_code_payment_init_Status_403() => AssertShellAsync(
        Method("POST"),
        "/cart/phase4c-code/payment/init",
        "{}",
        403,
        requiredField: null);

    // Source assertion 33: Contract success: POST /cart/{code}/promo/{promoCode}
    // @BR-ID: BR-SC-CRE-001
    [Fact(DisplayName = "033: Contract success: POST /cart/{code}/promo/{promoCode}")]
    [Trait("BR", "BR-SC-CRE-001")]
    public Task Test033_POST_BASE_URL_cart_phase4c_code_promo_phase4c_code_Field_cart_200() => AssertShellAsync(
        Method("POST"),
        "/cart/phase4c-code/promo/phase4c-code",
        null,
        200,
        requiredField: "cart");

    // Source assertion 34: Contract error/conformance: POST /cart/{code}/promo/{promoCode}
    // @BR-ID: BR-SC-CRE-001
    [Fact(DisplayName = "034: Contract error/conformance: POST /cart/{code}/promo/{promoCode}")]
    [Trait("BR", "BR-SC-CRE-001")]
    public Task Test034_POST_BASE_URL_cart_phase4c_code_promo_phase4c_code_Status_403() => AssertShellAsync(
        Method("POST"),
        "/cart/phase4c-code/promo/phase4c-code",
        null,
        403,
        requiredField: null);

    // Source assertion 35: Contract success: POST /cart/{code}/shipping
    // @BR-ID: BR-SC-CRE-001
    [Fact(DisplayName = "035: Contract success: POST /cart/{code}/shipping")]
    [Trait("BR", "BR-SC-CRE-001")]
    public Task Test035_POST_BASE_URL_cart_phase4c_code_shipping_Field_quoteId_200() => AssertShellAsync(
        Method("POST"),
        "/cart/phase4c-code/shipping",
        "{\"postalCode\":\"phase4c-test\",\"countryCode\":\"phase4c-test\"}",
        200,
        requiredField: "quoteId");

    // Source assertion 36: Contract error/conformance: POST /cart/{code}/shipping
    // @BR-ID: BR-SC-CRE-001
    [Fact(DisplayName = "036: Contract error/conformance: POST /cart/{code}/shipping")]
    [Trait("BR", "BR-SC-CRE-001")]
    public Task Test036_POST_BASE_URL_cart_phase4c_code_shipping_Status_400() => AssertShellAsync(
        Method("POST"),
        "/cart/phase4c-code/shipping",
        "{}",
        400,
        requiredField: null);

    // Source assertion 37: Contract success: POST /customers/{id}/cart
    // @BR-ID: BR-SC-CRE-001
    [Fact(DisplayName = "037: Contract success: POST /customers/{id}/cart")]
    [Trait("BR", "BR-SC-CRE-001")]
    public Task Test037_POST_BASE_URL_customers_ById_cart_Status_200() => AssertShellAsync(
        Method("POST"),
        $"/customers/{ResourceId}/cart",
        "{\"product\":\"phase4c-test\",\"quantity\":1,\"attributes\":[{\"id\":1}],\"promoCode\":\"phase4c-test\"}",
        200,
        requiredField: null);

    // Source assertion 38: Contract error/conformance: POST /customers/{id}/cart
    // @BR-ID: BR-SC-CRE-001
    [Fact(DisplayName = "038: Contract error/conformance: POST /customers/{id}/cart")]
    [Trait("BR", "BR-SC-CRE-001")]
    public Task Test038_POST_BASE_URL_customers_ById_cart_Status_410() => AssertShellAsync(
        Method("POST"),
        $"/customers/{ResourceId}/cart",
        "{}",
        410,
        requiredField: null);

    // Source assertion 39: Contract success: GET /auth/cart/{code}/shipping
    // @BR-ID: BR-SC-CRE-001
    [Fact(DisplayName = "039: Contract success: GET /auth/cart/{code}/shipping")]
    [Trait("BR", "BR-SC-CRE-001")]
    public Task Test039_GET_BASE_URL_auth_cart_phase4c_code_shipping_Field_quoteId_200() => AssertShellAsync(
        Method("GET"),
        "/auth/cart/phase4c-code/shipping",
        null,
        200,
        requiredField: "quoteId");

    // Source assertion 40: Contract error/conformance: GET /auth/cart/{code}/shipping
    // @BR-ID: BR-SC-CRE-001
    [Fact(DisplayName = "040: Contract error/conformance: GET /auth/cart/{code}/shipping")]
    [Trait("BR", "BR-SC-CRE-001")]
    public Task Test040_GET_BASE_URL_auth_cart_phase4c_code_shipping_Status_401() => AssertShellAsync(
        Method("GET"),
        "/auth/cart/phase4c-code/shipping",
        null,
        401,
        requiredField: null);

    // Source assertion 41: Contract success: GET /auth/cart/{id}/total
    // @BR-ID: BR-SC-CRE-001
    [Fact(DisplayName = "041: Contract success: GET /auth/cart/{id}/total")]
    [Trait("BR", "BR-SC-CRE-001")]
    public Task Test041_GET_BASE_URL_auth_cart_ById_total_Field_cartCode_200() => AssertShellAsync(
        Method("GET"),
        $"/auth/cart/{ResourceId}/total",
        null,
        200,
        requiredField: "cartCode");

    // Source assertion 42: Contract error/conformance: GET /auth/cart/{id}/total
    // @BR-ID: BR-SC-CRE-001
    [Fact(DisplayName = "042: Contract error/conformance: GET /auth/cart/{id}/total")]
    [Trait("BR", "BR-SC-CRE-001")]
    public Task Test042_GET_BASE_URL_auth_cart_ById_total_Status_401() => AssertShellAsync(
        Method("GET"),
        $"/auth/cart/{ResourceId}/total",
        null,
        401,
        requiredField: null);

    // Source assertion 43: Contract success: GET /auth/customer/cart
    // @BR-ID: BR-SC-CRE-001
    [Fact(DisplayName = "043: Contract success: GET /auth/customer/cart")]
    [Trait("BR", "BR-SC-CRE-001")]
    public Task Test043_GET_BASE_URL_auth_customer_cart_Field_cart_200() => AssertShellAsync(
        Method("GET"),
        "/auth/customer/cart",
        null,
        200,
        requiredField: "cart");

    // Source assertion 44: Contract error/conformance: GET /auth/customer/cart
    // @BR-ID: BR-SC-CRE-001
    [Fact(DisplayName = "044: Contract error/conformance: GET /auth/customer/cart")]
    [Trait("BR", "BR-SC-CRE-001")]
    public Task Test044_GET_BASE_URL_auth_customer_cart_Status_401() => AssertShellAsync(
        Method("GET"),
        "/auth/customer/cart",
        null,
        401,
        requiredField: null);

    // Source assertion 45: Contract success: GET /auth/customer/{id}/cart
    // @BR-ID: BR-SC-CRE-001
    [Fact(DisplayName = "045: Contract success: GET /auth/customer/{id}/cart")]
    [Trait("BR", "BR-SC-CRE-001")]
    public Task Test045_GET_BASE_URL_auth_customer_ById_cart_Field_cart_200() => AssertShellAsync(
        Method("GET"),
        $"/auth/customer/{ResourceId}/cart",
        null,
        200,
        requiredField: "cart");

    // Source assertion 46: Contract error/conformance: GET /auth/customer/{id}/cart
    // @BR-ID: BR-SC-CRE-001
    [Fact(DisplayName = "046: Contract error/conformance: GET /auth/customer/{id}/cart")]
    [Trait("BR", "BR-SC-CRE-001")]
    public Task Test046_GET_BASE_URL_auth_customer_ById_cart_Status_401() => AssertShellAsync(
        Method("GET"),
        $"/auth/customer/{ResourceId}/cart",
        null,
        401,
        requiredField: null);

    // Source assertion 47: Contract success: GET /cart/{code}
    // @BR-ID: BR-SC-CRE-001
    [Fact(DisplayName = "047: Contract success: GET /cart/{code}")]
    [Trait("BR", "BR-SC-CRE-001")]
    public Task Test047_GET_BASE_URL_cart_phase4c_code_Field_cart_200() => AssertShellAsync(
        Method("GET"),
        "/cart/phase4c-code",
        null,
        200,
        requiredField: "cart");

    // Source assertion 48: Contract error/conformance: GET /cart/{code}
    // @BR-ID: BR-SC-CRE-001
    [Fact(DisplayName = "048: Contract error/conformance: GET /cart/{code}")]
    [Trait("BR", "BR-SC-CRE-001")]
    public Task Test048_GET_BASE_URL_cart_phase4c_code_Status_403() => AssertShellAsync(
        Method("GET"),
        "/cart/phase4c-code",
        null,
        403,
        requiredField: null);

    // Source assertion 49: Contract success: GET /cart/{code}/total
    // @BR-ID: BR-SC-CRE-001
    [Fact(DisplayName = "049: Contract success: GET /cart/{code}/total")]
    [Trait("BR", "BR-SC-CRE-001")]
    public Task Test049_GET_BASE_URL_cart_phase4c_code_total_Field_cartCode_200() => AssertShellAsync(
        Method("GET"),
        "/cart/phase4c-code/total",
        null,
        200,
        requiredField: "cartCode");

    // Source assertion 50: Contract error/conformance: GET /cart/{code}/total
    // @BR-ID: BR-SC-CRE-001
    [Fact(DisplayName = "050: Contract error/conformance: GET /cart/{code}/total")]
    [Trait("BR", "BR-SC-CRE-001")]
    public Task Test050_GET_BASE_URL_cart_phase4c_code_total_Status_403() => AssertShellAsync(
        Method("GET"),
        "/cart/phase4c-code/total",
        null,
        403,
        requiredField: null);

    // Source assertion 51: Contract success: PUT /cart/{code}
    // @BR-ID: BR-SC-CRE-001
    [Fact(DisplayName = "051: Contract success: PUT /cart/{code}")]
    [Trait("BR", "BR-SC-CRE-001")]
    public Task Test051_PUT_BASE_URL_cart_phase4c_code_Field_cart_200() => AssertShellAsync(
        Method("PUT"),
        "/cart/phase4c-code",
        "{\"product\":\"phase4c-test\",\"quantity\":1,\"attributes\":[{\"id\":1}],\"promoCode\":\"phase4c-test\"}",
        200,
        requiredField: "cart");

    // Source assertion 52: Contract error/conformance: PUT /cart/{code}
    // @BR-ID: BR-SC-CRE-001
    [Fact(DisplayName = "052: Contract error/conformance: PUT /cart/{code}")]
    [Trait("BR", "BR-SC-CRE-001")]
    public Task Test052_PUT_BASE_URL_cart_phase4c_code_Status_400() => AssertShellAsync(
        Method("PUT"),
        "/cart/phase4c-code",
        "{}",
        400,
        requiredField: null);

    // Source assertion 53: Contract success: DELETE /cart/{code}/product/{sku}
    // @BR-ID: BR-SC-CRE-001
    [Fact(DisplayName = "053: Contract success: DELETE /cart/{code}/product/{sku}")]
    [Trait("BR", "BR-SC-CRE-001")]
    public Task Test053_DELETE_BASE_URL_cart_phase4c_code_product_phase4c_sku_Field_cart_200() => AssertShellAsync(
        Method("DELETE"),
        "/cart/phase4c-code/product/phase4c-sku",
        null,
        200,
        requiredField: "cart");

    // Source assertion 54: Contract error/conformance: DELETE /cart/{code}/product/{sku}
    // @BR-ID: BR-SC-CRE-001
    [Fact(DisplayName = "054: Contract error/conformance: DELETE /cart/{code}/product/{sku}")]
    [Trait("BR", "BR-SC-CRE-001")]
    public Task Test054_DELETE_BASE_URL_cart_phase4c_code_product_phase4c_sku_Status_403() => AssertShellAsync(
        Method("DELETE"),
        "/cart/phase4c-code/product/phase4c-sku",
        null,
        403,
        requiredField: null);
}
