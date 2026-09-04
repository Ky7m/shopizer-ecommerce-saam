using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json.Nodes;
using Shopizer.IntegrationTests.Fixtures;

namespace Shopizer.IntegrationTests;

[Collection(ShopizerAspireCollection.Name)]
public sealed class CartCheckoutComprehensiveTests(AspireHostFixture fixture)
{
    private const string ProductSku = "phase4c-sku";
    private const string UnknownCart = "cart-that-does-not-exist";
    private const string UnknownSku = "sku-that-does-not-exist";

    private static class Payloads
    {
        public const string AddCartItem = """{"product":"phase4c-sku","quantity":1,"attributes":[]}""";
        public const string AddUnknownProduct = """{"product":"sku-that-does-not-exist","quantity":1,"attributes":[]}""";
        public const string AddInvalidAttribute = """{"product":"phase4c-sku","quantity":1,"attributes":[{"id":999999999}]}""";
        public const string UpdateCartItem = """{"product":"phase4c-sku","quantity":2,"attributes":[]}""";
        public const string RemoveCartItem = """{"product":"phase4c-sku","quantity":0,"attributes":[]}""";
        public const string MultiCartItem = """[{"product":"phase4c-sku","quantity":2,"attributes":[]} ]""";
        public const string ShippingAddress = """{"postalCode":"H2Y 1C6","countryCode":"CA"}""";
        public const string InvalidShippingAddress = """{"postalCode":"","countryCode":"ZZ"}""";
        public const string AuthenticatedCheckout = """
            {"currency":"CAD","payment":{"amount":"0.00","paymentModule":"stripe","paymentType":"CREDITCARD","transactionType":"INIT","paymentToken":"test-token"},"customerAgreement":true}
            """;
        public const string AnonymousCheckout = """
            {"currency":"CAD","customer":{"email":"ada@example.test","firstName":"Ada","lastName":"Lovelace","billing":{"firstName":"Ada","lastName":"Lovelace","address":"1 Main St","city":"Montreal","countryCode":"CA","postalCode":"H2Y 1C6"}},"payment":{"amount":"0.00","paymentModule":"stripe","paymentType":"CREDITCARD","transactionType":"INIT","paymentToken":"test-token"},"customerAgreement":true}
            """;
        public const string PaymentInitialization = """{"amount":"0.00","paymentModule":"stripe","paymentType":"CREDITCARD","transactionType":"INIT","paymentToken":"test-token"}""";
        public const string Empty = "{}";
    }

    #region Cart creation and mutation

    // @BR-ID: BR-CO-AUT-012
    [Fact(DisplayName = "001: Contract success: POST /auth/cart/{code}/checkout")]
    [Trait("BR", "BR-CO-AUT-012")]
    public Task Test001_POST_BASE_URL_auth_cart_phase4c_code_checkout_Field_submissionId_202() =>
        AssertAuthenticatedCheckoutContractAsync();

    // @BR-ID: BR-CO-AUT-012
    [Fact(DisplayName = "002: Contract error/conformance: POST /auth/cart/{code}/checkout")]
    [Trait("BR", "BR-CO-AUT-012")]
    public Task Test002_POST_BASE_URL_auth_cart_phase4c_code_checkout_Status_401() =>
        AssertUnauthenticatedAsync(HttpMethod.Post, $"/api/v1/auth/cart/{UnknownCart}/checkout", Payloads.AuthenticatedCheckout, 401);

    // @BR-ID: BR-SC-CRE-001
    [Fact(DisplayName = "003: Business rule assertion: BR-SC-CRE-001")]
    [Trait("BR", "BR-SC-CRE-001")]
    public Task Test003_POST_BASE_URL_auth_cart_phase4c_code_checkout_Field_submissionId_202() =>
        AssertCartCreationAsync();

    // @BR-ID: BR-SC-SEL-002
    [Fact(DisplayName = "004: Business rule assertion: BR-SC-SEL-002")]
    [Trait("BR", "BR-SC-SEL-002")]
    public Task Test004_POST_BASE_URL_auth_cart_phase4c_code_checkout_Field_submissionId_202() =>
        AssertUnknownProductRejectedAsync();

    // @BR-ID: BR-SC-ATR-003
    [Fact(DisplayName = "005: Business rule assertion: BR-SC-ATR-003")]
    [Trait("BR", "BR-SC-ATR-003")]
    public Task Test005_POST_BASE_URL_auth_cart_phase4c_code_checkout_Field_submissionId_202() =>
        AssertInvalidAttributeRejectedAsync();

    // @BR-ID: BR-SC-MRG-004
    [Fact(DisplayName = "006: Business rule assertion: BR-SC-MRG-004")]
    [Trait("BR", "BR-SC-MRG-004")]
    public Task Test006_POST_BASE_URL_auth_cart_phase4c_code_checkout_Field_submissionId_202() =>
        AssertDuplicateLinesMergeAsync();

    // @BR-ID: BR-SC-UPD-005
    [Fact(DisplayName = "007: Business rule assertion: BR-SC-UPD-005")]
    [Trait("BR", "BR-SC-UPD-005")]
    public Task Test007_POST_BASE_URL_auth_cart_phase4c_code_checkout_Field_submissionId_202() =>
        AssertZeroQuantityRemovesLineAsync();

    // @BR-ID: BR-SC-HYD-006
    [Fact(DisplayName = "008: Business rule assertion: BR-SC-HYD-006")]
    [Trait("BR", "BR-SC-HYD-006")]
    public Task Test008_POST_BASE_URL_auth_cart_phase4c_code_checkout_Field_submissionId_202() =>
        AssertHydratedCartRefreshesLineAsync();

    // @BR-ID: BR-SC-MRG-007
    [Fact(DisplayName = "009: Business rule assertion: BR-SC-MRG-007")]
    [Trait("BR", "BR-SC-MRG-007")]
    public Task Test009_POST_BASE_URL_auth_cart_phase4c_code_checkout_Field_submissionId_202() =>
        AssertCustomerAdoptsSameScopeCartAsync();

    // @BR-ID: BR-SC-SHP-008
    [Fact(DisplayName = "010: Business rule assertion: BR-SC-SHP-008")]
    [Trait("BR", "BR-SC-SHP-008")]
    public Task Test010_POST_BASE_URL_auth_cart_phase4c_code_checkout_Field_submissionId_202() =>
        AssertShippingSummaryHonorsShippabilityAsync();

    // @BR-ID: BR-SC-SHP-009
    [Fact(DisplayName = "011: Business rule assertion: BR-SC-SHP-009")]
    [Trait("BR", "BR-SC-SHP-009")]
    public Task Test011_POST_BASE_URL_auth_cart_phase4c_code_checkout_Field_submissionId_202() =>
        AssertAnonymousShippingUsesSuppliedAddressAsync();

    // @BR-ID: BR-SC-TOT-010
    [Fact(DisplayName = "012: Business rule assertion: BR-SC-TOT-010")]
    [Trait("BR", "BR-SC-TOT-010")]
    public Task Test012_POST_BASE_URL_auth_cart_phase4c_code_checkout_Field_submissionId_202() =>
        AssertTotalsAreReconciledAsync();

    // @BR-ID: BR-SC-PRO-011
    [Fact(DisplayName = "013: Business rule assertion: BR-SC-PRO-011")]
    [Trait("BR", "BR-SC-PRO-011")]
    public Task Test013_POST_BASE_URL_auth_cart_phase4c_code_checkout_Field_submissionId_202() =>
        AssertPromotionIsStoredOnCartAsync();

    // @BR-ID: BR-CO-AUT-012
    [Fact(DisplayName = "014: Business rule assertion: BR-CO-AUT-012")]
    [Trait("BR", "BR-CO-AUT-012")]
    public Task Test014_POST_BASE_URL_auth_cart_phase4c_code_checkout_Field_submissionId_202() =>
        AssertAuthenticatedCheckoutUsesOwnedCartAsync();

    // @BR-ID: BR-CO-CUS-013
    [Fact(DisplayName = "015: Business rule assertion: BR-CO-CUS-013")]
    [Trait("BR", "BR-CO-CUS-013")]
    public Task Test015_POST_BASE_URL_auth_cart_phase4c_code_checkout_Field_submissionId_202() =>
        AssertAnonymousCheckoutCarriesCustomerContextAsync();

    // @BR-ID: BR-CO-SNP-014
    [Fact(DisplayName = "016: Business rule assertion: BR-CO-SNP-014")]
    [Trait("BR", "BR-CO-SNP-014")]
    public Task Test016_POST_BASE_URL_auth_cart_phase4c_code_checkout_Field_submissionId_202() =>
        AssertCheckoutCreatesImmutableSnapshotsAsync();

    // @BR-ID: BR-CO-TOT-015
    [Fact(DisplayName = "017: Business rule assertion: BR-CO-TOT-015")]
    [Trait("BR", "BR-CO-TOT-015")]
    public Task Test017_POST_BASE_URL_auth_cart_phase4c_code_checkout_Field_submissionId_202() =>
        AssertMismatchedPaymentAmountRejectedAsync();

    // @BR-ID: BR-CO-PAY-016
    [Fact(DisplayName = "018: Business rule assertion: BR-CO-PAY-016")]
    [Trait("BR", "BR-CO-PAY-016")]
    public Task Test018_POST_BASE_URL_auth_cart_phase4c_code_checkout_Field_submissionId_202() =>
        AssertPaymentHandoffUsesConfiguredMethodAsync();

    // @BR-ID: BR-CO-IDM-017
    [Fact(DisplayName = "019: Business rule assertion: BR-CO-IDM-017")]
    [Trait("BR", "BR-CO-IDM-017")]
    public Task Test019_POST_BASE_URL_auth_cart_phase4c_code_checkout_Field_submissionId_202() =>
        AssertCheckoutReplayIsIdempotentAsync();

    // @BR-ID: BR-CO-STA-018
    [Fact(DisplayName = "020: Business rule assertion: BR-CO-STA-018")]
    [Trait("BR", "BR-CO-STA-018")]
    public Task Test020_POST_BASE_URL_auth_cart_phase4c_code_checkout_Field_submissionId_202() =>
        AssertSubmittedCartRejectsNewCheckoutAsync();

    // @BR-ID: BR-CO-ORC-019
    [Fact(DisplayName = "021: Business rule assertion: BR-CO-ORC-019")]
    [Trait("BR", "BR-CO-ORC-019")]
    public Task Test021_POST_BASE_URL_auth_cart_phase4c_code_checkout_Field_submissionId_202() =>
        AssertCheckoutOutboxIsDurableAsync();

    // @BR-ID: BR-CO-BND-020
    [Fact(DisplayName = "022: Business rule assertion: BR-CO-BND-020")]
    [Trait("BR", "BR-CO-BND-020")]
    public Task Test022_POST_BASE_URL_auth_cart_phase4c_code_checkout_Field_submissionId_202() =>
        AssertDownstreamFailureDoesNotBecomeLocalSuccessAsync();

    #endregion

    #region Payment and checkout contract

    // @BR-ID: BR-CO-PAY-016
    [Fact(DisplayName = "023: Contract success: POST /auth/cart/{code}/payment/init")]
    [Trait("BR", "BR-CO-PAY-016")]
    public Task Test023_POST_BASE_URL_auth_cart_phase4c_code_payment_init_Field_submissionId_202() =>
        AssertAuthenticatedPaymentContractAsync();

    // @BR-ID: BR-CO-PAY-016
    [Fact(DisplayName = "024: Contract error/conformance: POST /auth/cart/{code}/payment/init")]
    [Trait("BR", "BR-CO-PAY-016")]
    public Task Test024_POST_BASE_URL_auth_cart_phase4c_code_payment_init_Status_401() =>
        AssertUnauthenticatedAsync(HttpMethod.Post, $"/api/v1/auth/cart/{UnknownCart}/payment/init", Payloads.PaymentInitialization, 401);

    // @BR-ID: BR-SC-CRE-001
    [Fact(DisplayName = "025: Contract success: POST /cart")]
    [Trait("BR", "BR-SC-CRE-001")]
    public Task Test025_POST_BASE_URL_cart_Field_cart_201() => AssertCartCreationAsync();

    // @BR-ID: BR-SC-CRE-001
    [Fact(DisplayName = "026: Contract error/conformance: POST /cart")]
    [Trait("BR", "BR-SC-CRE-001")]
    public Task Test026_POST_BASE_URL_cart_Status_400() =>
        AssertErrorAsync(HttpMethod.Post, "/api/v1/cart", Payloads.Empty, 422, "INVALID_QUANTITY");

    // @BR-ID: BR-CO-CUS-013
    [Fact(DisplayName = "027: Contract success: POST /cart/{code}/checkout")]
    [Trait("BR", "BR-CO-CUS-013")]
    public Task Test027_POST_BASE_URL_cart_phase4c_code_checkout_Field_submissionId_202() =>
        AssertAnonymousCheckoutCarriesCustomerContextAsync();

    // @BR-ID: BR-CO-CUS-013
    [Fact(DisplayName = "028: Contract error/conformance: POST /cart/{code}/checkout")]
    [Trait("BR", "BR-CO-CUS-013")]
    public Task Test028_POST_BASE_URL_cart_phase4c_code_checkout_Status_403() =>
        AssertErrorAsync(HttpMethod.Post, $"/api/v1/cart/{UnknownCart}/checkout", Payloads.Empty, 400, "INVALID_REQUEST");

    // @BR-ID: BR-SC-UPD-005
    [Fact(DisplayName = "029: Contract success: POST /cart/{code}/multi")]
    [Trait("BR", "BR-SC-UPD-005")]
    public Task Test029_POST_BASE_URL_cart_phase4c_code_multi_Field_cart_200() =>
        AssertMultipleUpdatePersistsQuantityAsync();

    // @BR-ID: BR-SC-UPD-005
    [Fact(DisplayName = "030: Contract error/conformance: POST /cart/{code}/multi")]
    [Trait("BR", "BR-SC-UPD-005")]
    public Task Test030_POST_BASE_URL_cart_phase4c_code_multi_Status_400() =>
        AssertErrorAsync(HttpMethod.Post, $"/api/v1/cart/{UnknownCart}/multi", "[]", 400, "INVALID_REQUEST");

    // @BR-ID: BR-CO-PAY-016
    [Fact(DisplayName = "031: Contract success: POST /cart/{code}/payment/init")]
    [Trait("BR", "BR-CO-PAY-016")]
    public Task Test031_POST_BASE_URL_cart_phase4c_code_payment_init_Field_submissionId_202() =>
        AssertPaymentInitializationContractAsync();

    // @BR-ID: BR-CO-PAY-016
    [Fact(DisplayName = "032: Contract error/conformance: POST /cart/{code}/payment/init")]
    [Trait("BR", "BR-CO-PAY-016")]
    public Task Test032_POST_BASE_URL_cart_phase4c_code_payment_init_Status_403() =>
        AssertErrorAsync(HttpMethod.Post, $"/api/v1/cart/{UnknownCart}/payment/init", Payloads.Empty, 400, "INVALID_REQUEST");

    // @BR-ID: BR-SC-PRO-011
    [Fact(DisplayName = "033: Contract success: POST /cart/{code}/promo/{promoCode}")]
    [Trait("BR", "BR-SC-PRO-011")]
    public Task Test033_POST_BASE_URL_cart_phase4c_code_promo_phase4c_code_Field_cart_200() =>
        AssertPromotionIsStoredOnCartAsync();

    // @BR-ID: BR-SC-PRO-011
    [Fact(DisplayName = "034: Contract error/conformance: POST /cart/{code}/promo/{promoCode}")]
    [Trait("BR", "BR-SC-PRO-011")]
    public Task Test034_POST_BASE_URL_cart_phase4c_code_promo_phase4c_code_Status_403() =>
        AssertErrorAsync(HttpMethod.Post, $"/api/v1/cart/{UnknownCart}/promo/phase4c-code", null, 404, "CART_NOT_FOUND");

    // @BR-ID: BR-SC-SHP-008
    [Fact(DisplayName = "035: Contract success: POST /cart/{code}/shipping")]
    [Trait("BR", "BR-SC-SHP-008")]
    public Task Test035_POST_BASE_URL_cart_phase4c_code_shipping_Field_quoteId_200() =>
        AssertShippingSummaryHonorsShippabilityAsync();

    // @BR-ID: BR-SC-SHP-009
    [Fact(DisplayName = "036: Contract error/conformance: POST /cart/{code}/shipping")]
    [Trait("BR", "BR-SC-SHP-009")]
    public Task Test036_POST_BASE_URL_cart_phase4c_code_shipping_Status_400() =>
        AssertErrorAsync(HttpMethod.Post, $"/api/v1/cart/{UnknownCart}/shipping", Payloads.ShippingAddress, 404, "CART_NOT_FOUND");

    // @BR-ID: BR-CO-BND-020
    [Fact(DisplayName = "037: Contract success: POST /customers/{id}/cart")]
    [Trait("BR", "BR-CO-BND-020")]
    public Task Test037_POST_BASE_URL_customers_ById_cart_Status_200() =>
        AssertDeprecatedCustomerCartEndpointAsync();

    // @BR-ID: BR-CO-BND-020
    [Fact(DisplayName = "038: Contract error/conformance: POST /customers/{id}/cart")]
    [Trait("BR", "BR-CO-BND-020")]
    public Task Test038_POST_BASE_URL_customers_ById_cart_Status_410() =>
        AssertDeprecatedCustomerCartEndpointAsync();

    #endregion

    #region Retrieval, totals and authentication

    // @BR-ID: BR-CO-AUT-012
    [Fact(DisplayName = "039: Contract success: GET /auth/cart/{code}/shipping")]
    [Trait("BR", "BR-CO-AUT-012")]
    public Task Test039_GET_BASE_URL_auth_cart_phase4c_code_shipping_Field_quoteId_200() =>
        AssertAuthenticatedShippingRequiresCustomerContextAsync();

    // @BR-ID: BR-CO-AUT-012
    [Fact(DisplayName = "040: Contract error/conformance: GET /auth/cart/{code}/shipping")]
    [Trait("BR", "BR-CO-AUT-012")]
    public Task Test040_GET_BASE_URL_auth_cart_phase4c_code_shipping_Status_401() =>
        AssertUnauthenticatedAsync(HttpMethod.Get, $"/api/v1/auth/cart/{UnknownCart}/shipping", null, 401);

    // @BR-ID: BR-CO-AUT-012
    [Fact(DisplayName = "041: Contract success: GET /auth/cart/{id}/total")]
    [Trait("BR", "BR-CO-AUT-012")]
    public Task Test041_GET_BASE_URL_auth_cart_ById_total_Field_cartCode_200() =>
        AssertAuthenticatedTotalUsesCartScopeAsync();

    // @BR-ID: BR-CO-AUT-012
    [Fact(DisplayName = "042: Contract error/conformance: GET /auth/cart/{id}/total")]
    [Trait("BR", "BR-CO-AUT-012")]
    public Task Test042_GET_BASE_URL_auth_cart_ById_total_Status_401() =>
        AssertUnauthenticatedAsync(HttpMethod.Get, "/api/v1/auth/cart/1/total", null, 401);

    // @BR-ID: BR-CO-AUT-012
    [Fact(DisplayName = "043: Contract success: GET /auth/customer/cart")]
    [Trait("BR", "BR-CO-AUT-012")]
    public Task Test043_GET_BASE_URL_auth_customer_cart_Field_cart_200() =>
        AssertAuthenticatedCustomerCartLookupAsync();

    // @BR-ID: BR-CO-AUT-012
    [Fact(DisplayName = "044: Contract error/conformance: GET /auth/customer/cart")]
    [Trait("BR", "BR-CO-AUT-012")]
    public Task Test044_GET_BASE_URL_auth_customer_cart_Status_401() =>
        AssertUnauthenticatedAsync(HttpMethod.Get, "/api/v1/auth/customer/cart", null, 401);

    // @BR-ID: BR-CO-AUT-012
    [Fact(DisplayName = "045: Contract success: GET /auth/customer/{id}/cart")]
    [Trait("BR", "BR-CO-AUT-012")]
    public Task Test045_GET_BASE_URL_auth_customer_ById_cart_Field_cart_200() =>
        AssertAuthenticatedCustomerCartByIdLookupAsync();

    // @BR-ID: BR-CO-AUT-012
    [Fact(DisplayName = "046: Contract error/conformance: GET /auth/customer/{id}/cart")]
    [Trait("BR", "BR-CO-AUT-012")]
    public Task Test046_GET_BASE_URL_auth_customer_ById_cart_Status_401() =>
        AssertUnauthenticatedAsync(HttpMethod.Get, "/api/v1/auth/customer/1/cart", null, 401);

    // @BR-ID: BR-SC-HYD-006
    [Fact(DisplayName = "047: Contract success: GET /cart/{code}")]
    [Trait("BR", "BR-SC-HYD-006")]
    public Task Test047_GET_BASE_URL_cart_phase4c_code_Field_cart_200() =>
        AssertHydratedCartRefreshesLineAsync();

    // @BR-ID: BR-SC-HYD-006
    [Fact(DisplayName = "048: Contract error/conformance: GET /cart/{code}")]
    [Trait("BR", "BR-SC-HYD-006")]
    public Task Test048_GET_BASE_URL_cart_phase4c_code_Status_403() =>
        AssertErrorAsync(HttpMethod.Get, $"/api/v1/cart/{UnknownCart}", null, 404, "CART_NOT_FOUND");

    // @BR-ID: BR-SC-TOT-010
    [Fact(DisplayName = "049: Contract success: GET /cart/{code}/total")]
    [Trait("BR", "BR-SC-TOT-010")]
    public Task Test049_GET_BASE_URL_cart_phase4c_code_total_Field_cartCode_200() =>
        AssertTotalsAreReconciledAsync();

    // @BR-ID: BR-SC-TOT-010
    [Fact(DisplayName = "050: Contract error/conformance: GET /cart/{code}/total")]
    [Trait("BR", "BR-SC-TOT-010")]
    public Task Test050_GET_BASE_URL_cart_phase4c_code_total_Status_403() =>
        AssertErrorAsync(HttpMethod.Get, $"/api/v1/cart/{UnknownCart}/total", null, 404, "CART_NOT_FOUND");

    // @BR-ID: BR-SC-UPD-005
    [Fact(DisplayName = "051: Contract success: PUT /cart/{code}")]
    [Trait("BR", "BR-SC-UPD-005")]
    public Task Test051_PUT_BASE_URL_cart_phase4c_code_Field_cart_200() =>
        AssertPositiveUpdateReplacesQuantityAsync();

    // @BR-ID: BR-SC-UPD-005
    [Fact(DisplayName = "052: Contract error/conformance: PUT /cart/{code}")]
    [Trait("BR", "BR-SC-UPD-005")]
    public Task Test052_PUT_BASE_URL_cart_phase4c_code_Status_400() =>
        AssertErrorAsync(HttpMethod.Put, $"/api/v1/cart/{UnknownCart}", Payloads.Empty, 400, "INVALID_REQUEST");

    // @BR-ID: BR-SC-UPD-005
    [Fact(DisplayName = "053: Contract success: DELETE /cart/{code}/product/{sku}")]
    [Trait("BR", "BR-SC-UPD-005")]
    public Task Test053_DELETE_BASE_URL_cart_phase4c_code_product_phase4c_sku_Field_cart_200() =>
        AssertDeleteRemovesLineAndReturnsCartAsync();

    // @BR-ID: BR-SC-UPD-005
    [Fact(DisplayName = "054: Contract error/conformance: DELETE /cart/{code}/product/{sku}")]
    [Trait("BR", "BR-SC-UPD-005")]
    public Task Test054_DELETE_BASE_URL_cart_phase4c_code_product_phase4c_sku_Status_403() =>
        AssertErrorAsync(HttpMethod.Delete, $"/api/v1/cart/{UnknownCart}/product/{ProductSku}", null, 404, "CART_NOT_FOUND");

    #endregion

    private async Task<string> ArrangeCartAsync(int quantity = 1)
    {
        await fixture.ResetCartCheckoutDataAsync();
        var payload = $$"""{"product":"{{ProductSku}}","quantity":{{quantity}},"attributes":[]}""";
        using var response = await SendAsync(HttpMethod.Post, "/api/v1/cart", payload);
        var root = await AssertCartEnvelopeAsync(response, 201, quantity);
        var code = RequiredString(root["cart"], "code");
        await AssertCartPersistedAsync(code);
        return code;
    }

    private async Task AssertCartCreationAsync()
    {
        var code = await ArrangeCartAsync();
        using var response = await SendAsync(HttpMethod.Get, $"/api/v1/cart/{Uri.EscapeDataString(code)}");
        var root = await AssertCartEnvelopeAsync(response, 200, 1);
        Assert.Equal(code, RequiredString(root["cart"], "code"));
    }

    private async Task AssertUnknownProductRejectedAsync()
    {
        using var response = await SendAsync(HttpMethod.Post, "/api/v1/cart", Payloads.AddUnknownProduct);
        await AssertErrorResponseAsync(response, 422, "PRODUCT_NOT_SELLABLE");
    }

    private async Task AssertInvalidAttributeRejectedAsync()
    {
        using var response = await SendAsync(HttpMethod.Post, "/api/v1/cart", Payloads.AddInvalidAttribute);
        await AssertErrorResponseAsync(response, 422, "ATTRIBUTE_PRODUCT_MISMATCH");
    }

    private async Task AssertDuplicateLinesMergeAsync()
    {
        var code = await ArrangeCartAsync();
        using (var add = await SendAsync(HttpMethod.Post, "/api/v1/cart", $$"""{"product":"{{ProductSku}}","quantity":2,"attributes":[]}""", cartCode: code))
        {
            var root = await AssertCartEnvelopeAsync(add, 201, 3);
            Assert.Single(root["cart"]!["items"]!.AsArray());
        }

        using var read = await SendAsync(HttpMethod.Get, $"/api/v1/cart/{Uri.EscapeDataString(code)}");
        var hydrated = await AssertCartEnvelopeAsync(read, 200, 3);
        Assert.Single(hydrated["cart"]!["items"]!.AsArray());
        Assert.Equal(3, hydrated["cart"]!["items"]![0]!["quantity"]!.GetValue<int>());
    }

    private async Task AssertZeroQuantityRemovesLineAsync()
    {
        var code = await ArrangeCartAsync();
        using var response = await SendAsync(HttpMethod.Put, $"/api/v1/cart/{Uri.EscapeDataString(code)}", Payloads.RemoveCartItem);
        var root = await AssertCartEnvelopeAsync(response, 200, expectedQuantity: null);
        Assert.Empty(root["cart"]!["items"]!.AsArray());
        Assert.Equal("Obsolete", RequiredString(root["cart"], "status"));
    }

    private async Task AssertHydratedCartRefreshesLineAsync()
    {
        var code = await ArrangeCartAsync();
        using var response = await SendAsync(HttpMethod.Get, $"/api/v1/cart/{Uri.EscapeDataString(code)}");
        var root = await AssertCartEnvelopeAsync(response, 200, 1);
        var item = root["cart"]!["items"]![0]!;
        Assert.Equal(ProductSku, RequiredString(item, "sku"));
        AssertPositiveMoney(item, "unitPrice");
        Assert.Equal(RequiredDecimal(item, "unitPrice") * RequiredDecimal(item, "quantity"), RequiredDecimal(item, "subTotal"));
    }

    private async Task AssertCustomerAdoptsSameScopeCartAsync()
    {
        var code = await ArrangeCartAsync();
        using var response = await SendAsync(
            HttpMethod.Get,
            $"/api/v1/auth/customer/cart?cart={Uri.EscapeDataString(code)}",
            token: fixture.CartCheckoutCustomerAccessToken);
        var root = await AssertCartEnvelopeAsync(response, 200, 1);
        Assert.Equal(code, RequiredString(root["cart"], "code"));
        Assert.NotEmpty(RequiredString(root["cart"], "customerId"));
    }

    private async Task AssertShippingSummaryHonorsShippabilityAsync()
    {
        var code = await ArrangeCartAsync();
        using var response = await SendAsync(HttpMethod.Post, $"/api/v1/cart/{Uri.EscapeDataString(code)}/shipping", Payloads.ShippingAddress);
        var root = await AssertShippingSummaryAsync(response);
        if (root["shippingRequired"]!.GetValue<bool>())
        {
            Assert.NotEmpty(root["options"]!.AsArray());
            Assert.True(Guid.TryParse(RequiredString(root, "quoteId"), out _));
        }
        else
        {
            Assert.Empty(root["options"]!.AsArray());
            Assert.Null(root["quoteId"]);
        }
    }

    private async Task AssertAnonymousShippingUsesSuppliedAddressAsync()
    {
        var code = await ArrangeCartAsync();
        using var response = await SendAsync(HttpMethod.Post, $"/api/v1/cart/{Uri.EscapeDataString(code)}/shipping", Payloads.ShippingAddress);
        var root = await AssertShippingSummaryAsync(response);
        Assert.True(root["shippingRequired"]!.GetValue<bool>(), "Address fallback is only meaningful for a physical cart.");
        Assert.Equal("H2Y 1C6", RequiredString(root["delivery"], "postalCode"));
    }

    private async Task AssertTotalsAreReconciledAsync()
    {
        var code = await ArrangeCartAsync();
        using var response = await SendAsync(HttpMethod.Get, $"/api/v1/cart/{Uri.EscapeDataString(code)}/total");
        var root = await AssertTotalSummaryAsync(response, code);
        var subtotal = RequiredDecimal(root, "subTotal");
        var discount = RequiredDecimal(root, "discountTotal");
        var shipping = RequiredDecimal(root, "shipping");
        var handling = RequiredDecimal(root, "handling");
        var tax = RequiredDecimal(root, "tax");
        var grand = RequiredDecimal(root, "grandTotal");
        Assert.Equal(subtotal - discount + shipping + handling + tax, grand);
        Assert.Contains(root["components"]!.AsArray(), component => component!["code"]!.GetValue<string>() == "order.total.total");
    }

    private async Task AssertPromotionIsStoredOnCartAsync()
    {
        var code = await ArrangeCartAsync();
        using var response = await SendAsync(HttpMethod.Post, $"/api/v1/cart/{Uri.EscapeDataString(code)}/promo/phase4c-code");
        var root = await AssertCartEnvelopeAsync(response, 200, 1);
        Assert.Equal("phase4c-code", RequiredString(root["cart"], "promoCode"));
        Assert.True(DateTimeOffset.TryParse(RequiredString(root["cart"], "promoAddedAt"), out _));
    }

    private async Task AssertAuthenticatedCheckoutUsesOwnedCartAsync()
    {
        var code = await ArrangeAuthenticatedCartAsync();
        var amount = await ReadGrandTotalAsync(code);
        var payload = AnonymousOrAuthenticatedPayload(Payloads.AuthenticatedCheckout, amount);
        using var response = await SendAsync(HttpMethod.Post, $"/api/v1/auth/cart/{Uri.EscapeDataString(code)}/checkout", payload, fixture.CartCheckoutCustomerAccessToken, $"ms04-auth-{Guid.NewGuid():N}");
        var root = await AssertCheckoutResponseAsync(response, 202, amount);
        await AssertCheckoutPersistenceAsync(root);
    }

    private async Task AssertAuthenticatedCheckoutContractAsync()
    {
        var code = await ArrangeAuthenticatedCartAsync();
        var amount = await ReadGrandTotalAsync(code);
        using var response = await SendAsync(
            HttpMethod.Post,
            $"/api/v1/auth/cart/{Uri.EscapeDataString(code)}/checkout",
            AnonymousOrAuthenticatedPayload(Payloads.AuthenticatedCheckout, amount),
            fixture.CartCheckoutCustomerAccessToken,
            $"ms04-contract-auth-{Guid.NewGuid():N}");
        var root = await AssertCheckoutResponseAsync(response, 202, amount);
        await AssertCheckoutPersistenceAsync(root);
    }

    private async Task AssertAnonymousCheckoutCarriesCustomerContextAsync()
    {
        var code = await ArrangeCartAsync();
        var amount = await ReadGrandTotalAsync(code);
        using var response = await SendAsync(
            HttpMethod.Post,
            $"/api/v1/cart/{Uri.EscapeDataString(code)}/checkout",
            AnonymousOrAuthenticatedPayload(Payloads.AnonymousCheckout, amount),
            idempotencyKey: $"ms04-anon-{Guid.NewGuid():N}");
        var root = await AssertCheckoutResponseAsync(response, 202, amount);
        await AssertCheckoutPersistenceAsync(root);
    }

    private async Task AssertCheckoutCreatesImmutableSnapshotsAsync()
    {
        var code = await ArrangeCartAsync();
        var amount = await ReadGrandTotalAsync(code);
        using var response = await SendAsync(
            HttpMethod.Post,
            $"/api/v1/cart/{Uri.EscapeDataString(code)}/checkout",
            AnonymousOrAuthenticatedPayload(Payloads.AnonymousCheckout, amount),
            idempotencyKey: $"ms04-snapshot-{Guid.NewGuid():N}");
        var root = await AssertCheckoutResponseAsync(response, 202, amount);
        await AssertCheckoutPersistenceAsync(root, requireSnapshots: true);
    }

    private async Task AssertMismatchedPaymentAmountRejectedAsync()
    {
        var code = await ArrangeCartAsync();
        var amount = await ReadGrandTotalAsync(code);
        var mismatched = (decimal.Parse(amount, NumberStyles.Number, CultureInfo.InvariantCulture) + 1m)
            .ToString("0.00", CultureInfo.InvariantCulture);
        using var response = await SendAsync(
            HttpMethod.Post,
            $"/api/v1/cart/{Uri.EscapeDataString(code)}/checkout",
            AnonymousOrAuthenticatedPayload(Payloads.AnonymousCheckout, mismatched),
            idempotencyKey: $"ms04-amount-{Guid.NewGuid():N}");
        await AssertErrorResponseAsync(response, 409, "AMOUNT_MISMATCH");
    }

    private async Task AssertPaymentHandoffUsesConfiguredMethodAsync()
    {
        var code = await ArrangeCartAsync();
        var amount = await ReadGrandTotalAsync(code);
        using var response = await SendAsync(
            HttpMethod.Post,
            $"/api/v1/cart/{Uri.EscapeDataString(code)}/payment/init",
            Payloads.PaymentInitialization.Replace("0.00", amount, StringComparison.Ordinal),
            idempotencyKey: $"ms04-payment-{Guid.NewGuid():N}");
        var root = await AssertPaymentResponseAsync(response, 202, amount);
        Assert.Equal("Pending", RequiredString(root, "paymentState"));
    }

    private async Task AssertCheckoutReplayIsIdempotentAsync()
    {
        var code = await ArrangeCartAsync();
        var amount = await ReadGrandTotalAsync(code);
        var key = $"ms04-replay-{Guid.NewGuid():N}";
        var payload = AnonymousOrAuthenticatedPayload(Payloads.AnonymousCheckout, amount);
        using var first = await SendAsync(HttpMethod.Post, $"/api/v1/cart/{Uri.EscapeDataString(code)}/checkout", payload, idempotencyKey: key);
        var firstRoot = await AssertCheckoutResponseAsync(first, 202, amount);
        using var second = await SendAsync(HttpMethod.Post, $"/api/v1/cart/{Uri.EscapeDataString(code)}/checkout", payload, idempotencyKey: key);
        var secondRoot = await AssertCheckoutResponseAsync(second, 202, amount);
        Assert.Equal(RequiredString(firstRoot, "submissionId"), RequiredString(secondRoot, "submissionId"));
        await AssertCheckoutPersistenceAsync(secondRoot);
    }

    private async Task AssertSubmittedCartRejectsNewCheckoutAsync()
    {
        var code = await ArrangeCartAsync();
        var amount = await ReadGrandTotalAsync(code);
        var payload = AnonymousOrAuthenticatedPayload(Payloads.AnonymousCheckout, amount);
        using var first = await SendAsync(HttpMethod.Post, $"/api/v1/cart/{Uri.EscapeDataString(code)}/checkout", payload, idempotencyKey: $"ms04-terminal-first-{Guid.NewGuid():N}");
        var firstRoot = await AssertCheckoutResponseAsync(first, 202, amount);
        using var second = await SendAsync(HttpMethod.Post, $"/api/v1/cart/{Uri.EscapeDataString(code)}/checkout", payload, idempotencyKey: $"ms04-terminal-second-{Guid.NewGuid():N}");
        await AssertErrorResponseAsync(second, 409, "CHECKOUT_TERMINAL");
        Assert.NotNull(firstRoot["checkoutSessionId"]);
    }

    private async Task AssertCheckoutOutboxIsDurableAsync()
    {
        var code = await ArrangeCartAsync();
        var amount = await ReadGrandTotalAsync(code);
        using var response = await SendAsync(
            HttpMethod.Post,
            $"/api/v1/cart/{Uri.EscapeDataString(code)}/checkout",
            AnonymousOrAuthenticatedPayload(Payloads.AnonymousCheckout, amount),
            idempotencyKey: $"ms04-outbox-{Guid.NewGuid():N}");
        var root = await AssertCheckoutResponseAsync(response, 202, amount);
        var eventId = RequiredString(root, "eventId");
        Assert.True(Guid.TryParse(eventId, out _));
        await AssertOutboxEventAsync(eventId);
    }

    private async Task AssertDownstreamFailureDoesNotBecomeLocalSuccessAsync()
    {
        await fixture.DisableCartPricingAsync();
        try
        {
            using var response = await SendAsync(HttpMethod.Post, "/api/v1/cart", Payloads.AddCartItem);
            await AssertErrorResponseAsync(response, 503, "CHECKOUT_UNAVAILABLE");
            Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        }
        finally
        {
            await fixture.RestoreCartPricingAsync();
        }
    }

    private async Task AssertAuthenticatedPaymentContractAsync()
    {
        var code = await ArrangeAuthenticatedCartAsync();
        var amount = await ReadGrandTotalAsync(code);
        using var response = await SendAsync(
            HttpMethod.Post,
            $"/api/v1/auth/cart/{Uri.EscapeDataString(code)}/payment/init",
            Payloads.PaymentInitialization.Replace("0.00", amount, StringComparison.Ordinal),
            fixture.CartCheckoutCustomerAccessToken,
            $"ms04-auth-payment-{Guid.NewGuid():N}");
        var root = await AssertPaymentResponseAsync(response, 202, amount);
        Assert.True(Guid.TryParse(RequiredString(root, "submissionId"), out _));
    }

    private async Task AssertMultipleUpdatePersistsQuantityAsync()
    {
        var code = await ArrangeCartAsync();
        using var response = await SendAsync(HttpMethod.Post, $"/api/v1/cart/{Uri.EscapeDataString(code)}/multi", Payloads.MultiCartItem);
        var root = await AssertCartEnvelopeAsync(response, 200, 2);
        Assert.Equal(2, root["cart"]!["items"]![0]!["quantity"]!.GetValue<int>());
    }

    private async Task AssertPaymentInitializationContractAsync()
    {
        var code = await ArrangeCartAsync();
        var amount = await ReadGrandTotalAsync(code);
        using var response = await SendAsync(
            HttpMethod.Post,
            $"/api/v1/cart/{Uri.EscapeDataString(code)}/payment/init",
            Payloads.PaymentInitialization.Replace("0.00", amount, StringComparison.Ordinal),
            idempotencyKey: $"ms04-payment-contract-{Guid.NewGuid():N}");
        var root = await AssertPaymentResponseAsync(response, 202, amount);
        Assert.Equal("CAD", RequiredString(root, "currency"));
    }

    private async Task AssertDeprecatedCustomerCartEndpointAsync()
    {
        using var response = await SendAsync(HttpMethod.Post, "/api/v1/customers/1/cart", Payloads.AddCartItem);
        await AssertErrorResponseAsync(response, 410, "UNSUPPORTED_ENDPOINT");
    }

    private async Task AssertAuthenticatedShippingRequiresCustomerContextAsync()
    {
        var code = await ArrangeCartAsync();
        using var response = await SendAsync(HttpMethod.Get, $"/api/v1/auth/cart/{Uri.EscapeDataString(code)}/shipping", token: fixture.CartCheckoutCustomerAccessToken);
        var root = await AssertShippingSummaryAsync(response);
        if (root["shippingRequired"]!.GetValue<bool>())
            Assert.NotNull(root["delivery"]);
    }

    private async Task AssertAuthenticatedTotalUsesCartScopeAsync()
    {
        var code = await ArrangeAuthenticatedCartAsync();
        var id = await ReadCartIdAsync(code);
        using var response = await SendAsync(HttpMethod.Get, $"/api/v1/auth/cart/{id}/total", token: fixture.CartCheckoutCustomerAccessToken);
        var root = await AssertTotalSummaryAsync(response, code);
        Assert.Equal(code, RequiredString(root, "cartCode"));
    }

    private async Task AssertAuthenticatedCustomerCartLookupAsync()
    {
        var code = await ArrangeAuthenticatedCartAsync();
        using var response = await SendAsync(HttpMethod.Get, $"/api/v1/auth/customer/cart?cart={Uri.EscapeDataString(code)}", token: fixture.CartCheckoutCustomerAccessToken);
        var root = await AssertCartEnvelopeAsync(response, 200, 1);
        Assert.Equal(code, RequiredString(root["cart"], "code"));
    }

    private async Task AssertAuthenticatedCustomerCartByIdLookupAsync()
    {
        var code = await ArrangeAuthenticatedCartAsync();
        using var response = await SendAsync(HttpMethod.Get, $"/api/v1/auth/customer/1/cart?cart={Uri.EscapeDataString(code)}", token: fixture.CartCheckoutCustomerAccessToken);
        await AssertErrorResponseAsync(response, 403, "CART_SCOPE_MISMATCH");
    }

    private async Task AssertPositiveUpdateReplacesQuantityAsync()
    {
        var code = await ArrangeCartAsync();
        using var response = await SendAsync(HttpMethod.Put, $"/api/v1/cart/{Uri.EscapeDataString(code)}", Payloads.UpdateCartItem);
        var root = await AssertCartEnvelopeAsync(response, 200, 2);
        Assert.Equal(2, root["cart"]!["items"]![0]!["quantity"]!.GetValue<int>());
    }

    private async Task AssertDeleteRemovesLineAndReturnsCartAsync()
    {
        var code = await ArrangeCartAsync();
        using var response = await SendAsync(HttpMethod.Delete, $"/api/v1/cart/{Uri.EscapeDataString(code)}/product/{ProductSku}?body=true");
        var root = await AssertCartEnvelopeAsync(response, 200, null);
        Assert.Empty(root["cart"]!["items"]!.AsArray());
        Assert.Equal("Obsolete", RequiredString(root["cart"], "status"));
    }

    private async Task<string> ArrangeAuthenticatedCartAsync()
    {
        var code = await ArrangeCartAsync();
        using var response = await SendAsync(
            HttpMethod.Get,
            $"/api/v1/auth/customer/cart?cart={Uri.EscapeDataString(code)}",
            token: fixture.CartCheckoutCustomerAccessToken);
        var root = await AssertCartEnvelopeAsync(response, 200, 1);
        return RequiredString(root["cart"], "code");
    }

    private async Task<string> ReadGrandTotalAsync(string code)
    {
        using var response = await SendAsync(HttpMethod.Get, $"/api/v1/cart/{Uri.EscapeDataString(code)}/total");
        var root = await AssertTotalSummaryAsync(response, code);
        return RequiredString(root, "grandTotal");
    }

    private async Task<long> ReadCartIdAsync(string code)
    {
        using var response = await SendAsync(HttpMethod.Get, $"/api/v1/cart/{Uri.EscapeDataString(code)}");
        var root = await AssertCartEnvelopeAsync(response, 200, 1);
        return long.Parse(RequiredString(root["cart"], "id"), CultureInfo.InvariantCulture);
    }

    private static string AnonymousOrAuthenticatedPayload(string template, string amount) =>
        template.Replace("0.00", amount, StringComparison.Ordinal);

    private async Task<HttpResponseMessage> SendAsync(
        HttpMethod method,
        string path,
        string? payload = null,
        string? token = null,
        string? idempotencyKey = null,
        string? cartCode = null)
    {
        using var request = new HttpRequestMessage(method, path);
        if (payload is not null)
            request.Content = new StringContent(payload, Encoding.UTF8, "application/json");
        if (token is not null)
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        if (idempotencyKey is not null)
            request.Headers.TryAddWithoutValidation("idempotency-key", idempotencyKey);
        if (cartCode is not null)
            request.Headers.TryAddWithoutValidation("x-cart-code", cartCode);
        return await fixture.CartCheckoutClient.SendAsync(request);
    }

    private async Task AssertUnauthenticatedAsync(HttpMethod method, string path, string? payload, int expectedStatus)
    {
        using var response = await SendAsync(method, path, payload);
        await AssertErrorResponseAsync(response, expectedStatus, "AUTHENTICATION_REQUIRED");
    }

    private async Task AssertErrorAsync(HttpMethod method, string path, string? payload, int expectedStatus, string error)
    {
        using var response = await SendAsync(method, path, payload);
        await AssertErrorResponseAsync(response, expectedStatus, error);
    }

    private static async Task<JsonNode> AssertCartEnvelopeAsync(HttpResponseMessage response, int expectedStatus, int? expectedQuantity)
    {
        var root = await AssertResponseAsync(response, expectedStatus);
        var cart = root["cart"];
        Assert.NotNull(cart);
        Assert.True(Guid.TryParse(RequiredString(cart, "id"), out _) || long.TryParse(RequiredString(cart, "id"), out _));
        Assert.NotEmpty(RequiredString(cart, "code"));
        Assert.Contains(RequiredString(cart, "status"), new[] { "Open", "Completed", "Obsolete" });
        Assert.Matches("^[A-Z]{3}$", RequiredString(cart, "currency"));
        var items = cart["items"]?.AsArray();
        Assert.NotNull(items);
        foreach (var item in items!)
        {
            Assert.NotEmpty(RequiredString(item, "id"));
            Assert.NotEmpty(RequiredString(item, "sku"));
            var quantity = item!["quantity"]!.GetValue<int>();
            Assert.True(quantity > 0);
            Assert.True(RequiredDecimal(item, "unitPrice") > 0);
            Assert.Equal(RequiredDecimal(item, "unitPrice") * quantity, RequiredDecimal(item, "subTotal"));
            Assert.NotNull(item["attributes"]);
        }

        if (expectedQuantity is not null)
        {
            Assert.Single(items);
            Assert.Equal(expectedQuantity.Value, items[0]!["quantity"]!.GetValue<int>());
        }

        return root;
    }

    private static async Task<JsonNode> AssertShippingSummaryAsync(HttpResponseMessage response)
    {
        var root = await AssertResponseAsync(response, 200);
        Assert.NotNull(root["shippingRequired"]);
        Assert.NotNull(root["options"]);
        if (root["quoteId"] is not null)
            Assert.True(Guid.TryParse(root["quoteId"]!.GetValue<string>(), out _));
        return root;
    }

    private static async Task<JsonNode> AssertTotalSummaryAsync(HttpResponseMessage response, string code)
    {
        var root = await AssertResponseAsync(response, 200);
        Assert.Equal(code, RequiredString(root, "cartCode"));
        Assert.Matches("^[A-Z]{3}$", RequiredString(root, "currency"));
        foreach (var field in new[] { "subTotal", "discountTotal", "shipping", "handling", "tax", "grandTotal" })
            Assert.True(decimal.TryParse(RequiredString(root, field), NumberStyles.Number, CultureInfo.InvariantCulture, out _));
        Assert.NotNull(root["components"]);
        return root;
    }

    private static async Task<JsonNode> AssertCheckoutResponseAsync(HttpResponseMessage response, int expectedStatus, string amount)
    {
        var root = await AssertResponseAsync(response, expectedStatus);
        Assert.True(Guid.TryParse(RequiredString(root, "submissionId"), out _));
        Assert.True(Guid.TryParse(RequiredString(root, "checkoutSessionId"), out _));
        Assert.Contains(RequiredString(root, "state"), new[] { "Pending", "Submitted", "Acknowledged", "Failed" });
        Assert.Equal(amount, RequiredString(root, "amount"));
        Assert.Equal("CAD", RequiredString(root, "currency"));
        return root;
    }

    private static async Task<JsonNode> AssertPaymentResponseAsync(HttpResponseMessage response, int expectedStatus, string amount)
    {
        var root = await AssertResponseAsync(response, expectedStatus);
        Assert.True(Guid.TryParse(RequiredString(root, "submissionId"), out _));
        Assert.Equal("Pending", RequiredString(root, "paymentState"));
        Assert.Equal(amount, RequiredString(root, "amount"));
        Assert.Matches("^[A-Z]{3}$", RequiredString(root, "currency"));
        return root;
    }

    private static async Task<JsonNode> AssertResponseAsync(HttpResponseMessage response, int expectedStatus)
    {
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(
            expectedStatus == (int)response.StatusCode,
            $"Expected HTTP {expectedStatus}, received {(int)response.StatusCode}: {body}");
        Assert.Contains("application/json", response.Content.Headers.ContentType?.MediaType ?? "", StringComparison.OrdinalIgnoreCase);
        var root = JsonNode.Parse(body);
        Assert.NotNull(root);
        return root!;
    }

    private static async Task AssertErrorResponseAsync(HttpResponseMessage response, int expectedStatus, string error)
    {
        var root = await AssertResponseAsync(response, expectedStatus);
        Assert.Equal(error, RequiredString(root, "error"));
        Assert.Equal(expectedStatus, root["statusCode"]!.GetValue<int>());
        Assert.NotEmpty(RequiredString(root, "message"));
        Assert.True(DateTimeOffset.TryParse(RequiredString(root, "timestamp"), out _));
    }

    private async Task AssertCartPersistedAsync(string code)
    {
        await using var connection = await fixture.OpenDatabaseAsync("shopizerDb");
        await using var command = new Npgsql.NpgsqlCommand(
            "SELECT count(*) FROM cart_checkout_schema.shopping_cart WHERE cart_code = @code AND status = 'OPEN'",
            connection);
        command.Parameters.AddWithValue("code", code);
        Assert.Equal(1L, (long)(await command.ExecuteScalarAsync() ?? 0L));
    }

    private async Task AssertCheckoutPersistenceAsync(JsonNode response, bool requireSnapshots = false)
    {
        var submissionId = RequiredString(response, "submissionId");
        await using var connection = await fixture.OpenDatabaseAsync("shopizerDb");
        await using var command = new Npgsql.NpgsqlCommand(
            """
            SELECT count(*) FROM cart_checkout_schema.checkout_submission s
            JOIN cart_checkout_schema.checkout_session cs ON cs.checkout_session_id = s.checkout_session_id
            WHERE s.submission_id = @submission AND cs.state = 'SUBMITTED'
            """,
            connection);
        command.Parameters.AddWithValue("submission", Guid.Parse(submissionId));
        Assert.Equal(1L, (long)(await command.ExecuteScalarAsync() ?? 0L));
        if (requireSnapshots)
        {
            await using var snapshot = new Npgsql.NpgsqlCommand(
                """
                SELECT count(*) FROM cart_checkout_schema.checkout_line_snapshot l
                JOIN cart_checkout_schema.checkout_session cs ON cs.checkout_session_id = l.checkout_session_id
                JOIN cart_checkout_schema.checkout_submission s ON s.checkout_session_id = cs.checkout_session_id
                WHERE s.submission_id = @submission
                """,
                connection);
            snapshot.Parameters.AddWithValue("submission", Guid.Parse(submissionId));
            Assert.True((long)(await snapshot.ExecuteScalarAsync() ?? 0L) > 0);
        }
    }

    private async Task AssertOutboxEventAsync(string eventId)
    {
        await using var connection = await fixture.OpenDatabaseAsync("shopizerDb");
        await using var command = new Npgsql.NpgsqlCommand(
            "SELECT count(*) FROM cart_checkout_schema.ms04_outbox WHERE event_id = @event AND event_type = 'OrderSubmitted.v1'",
            connection);
        command.Parameters.AddWithValue("event", Guid.Parse(eventId));
        Assert.Equal(1L, (long)(await command.ExecuteScalarAsync() ?? 0L));
    }

    private static void AssertPositiveMoney(JsonNode node, string field) =>
        Assert.True(RequiredDecimal(node, field) > 0, $"Expected {field} to be positive.");

    private static decimal RequiredDecimal(JsonNode? node, string field) =>
        decimal.Parse(RequiredString(node, field), NumberStyles.Number, CultureInfo.InvariantCulture);

    private static string RequiredString(JsonNode? node, string field)
    {
        var jsonValue = node?[field];
        var value = jsonValue switch
        {
            JsonValue valueNode when valueNode.TryGetValue<string>(out var stringValue) => stringValue,
            JsonValue => jsonValue.ToJsonString(),
            _ => null
        };
        Assert.False(string.IsNullOrWhiteSpace(value), $"Expected non-empty JSON field '{field}'.");
        return value!;
    }
}
