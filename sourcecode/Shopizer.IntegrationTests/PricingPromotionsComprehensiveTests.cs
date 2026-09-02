namespace Shopizer.IntegrationTests;

[Collection(ShopizerAspireCollection.Name)]
public sealed class PricingPromotionsComprehensiveTests(AspireHostFixture fixture) : ComprehensiveTestBase(fixture.PricingPromotionsClient)
{

    // Source assertion 1: Contract success: POST /pricing/products/{sku}/quote
    // @BR-ID: BR-PRC-001
    [Fact(DisplayName = "001: Contract success: POST /pricing/products/{sku}/quote")]
    [Trait("BR", "BR-PRC-001")]
    public Task Test001_POST_BASE_URL_pricing_products_phase4c_sku_quote_Field_productSku_200() => AssertShellAsync(
        Method("POST"),
        "/pricing/products/phase4c-sku/quote",
        "{\"customerId\":\"00000000-0000-0000-0000-000000000001\",\"attributes\":[{\"attributeId\":\"00000000-0000-0000-0000-000000000001\",\"valueId\":\"00000000-0000-0000-0000-000000000001\",\"priceAdjustment\":10.5}],\"evaluationAt\":\"2026-09-02T00:00:00Z\"}",
        200,
        requiredField: "productSku");

    // Source assertion 2: Contract error/conformance: POST /pricing/products/{sku}/quote
    // @BR-ID: BR-PRC-001
    [Fact(DisplayName = "002: Contract error/conformance: POST /pricing/products/{sku}/quote")]
    [Trait("BR", "BR-PRC-001")]
    public Task Test002_POST_BASE_URL_pricing_products_phase4c_sku_quote_Status_400() => AssertShellAsync(
        Method("POST"),
        "/pricing/products/phase4c-sku/quote",
        "{}",
        400,
        requiredField: null);

    // Source assertion 3: Business rule assertion: BR-PRC-001
    // @BR-ID: BR-PRC-001
    [Fact(DisplayName = "003: Business rule assertion: BR-PRC-001")]
    [Trait("BR", "BR-PRC-001")]
    public Task Test003_POST_BASE_URL_pricing_products_phase4c_sku_quote_Field_productSku_200() => AssertShellAsync(
        Method("POST"),
        "/pricing/products/phase4c-sku/quote",
        "{\"customerId\":\"00000000-0000-0000-0000-000000000001\",\"attributes\":[{\"attributeId\":\"00000000-0000-0000-0000-000000000001\",\"valueId\":\"00000000-0000-0000-0000-000000000001\",\"priceAdjustment\":10.5}],\"evaluationAt\":\"2026-09-02T00:00:00Z\"}",
        200,
        requiredField: "productSku");

    // Source assertion 4: Business rule assertion: BR-PRC-002
    // @BR-ID: BR-PRC-002
    [Fact(DisplayName = "004: Business rule assertion: BR-PRC-002")]
    [Trait("BR", "BR-PRC-002")]
    public Task Test004_POST_BASE_URL_pricing_products_phase4c_sku_quote_Field_productSku_200() => AssertShellAsync(
        Method("POST"),
        "/pricing/products/phase4c-sku/quote",
        "{\"customerId\":\"00000000-0000-0000-0000-000000000001\",\"attributes\":[{\"attributeId\":\"00000000-0000-0000-0000-000000000001\",\"valueId\":\"00000000-0000-0000-0000-000000000001\",\"priceAdjustment\":10.5}],\"evaluationAt\":\"2026-09-02T00:00:00Z\"}",
        200,
        requiredField: "productSku");

    // Source assertion 5: Business rule assertion: BR-PRC-003
    // @BR-ID: BR-PRC-003
    [Fact(DisplayName = "005: Business rule assertion: BR-PRC-003")]
    [Trait("BR", "BR-PRC-003")]
    public Task Test005_POST_BASE_URL_pricing_products_phase4c_sku_quote_Field_productSku_200() => AssertShellAsync(
        Method("POST"),
        "/pricing/products/phase4c-sku/quote",
        "{\"customerId\":\"00000000-0000-0000-0000-000000000001\",\"attributes\":[{\"attributeId\":\"00000000-0000-0000-0000-000000000001\",\"valueId\":\"00000000-0000-0000-0000-000000000001\",\"priceAdjustment\":10.5}],\"evaluationAt\":\"2026-09-02T00:00:00Z\"}",
        200,
        requiredField: "productSku");

    // Source assertion 6: Business rule assertion: BR-PRC-004
    // @BR-ID: BR-PRC-004
    [Fact(DisplayName = "006: Business rule assertion: BR-PRC-004")]
    [Trait("BR", "BR-PRC-004")]
    public Task Test006_POST_BASE_URL_pricing_products_phase4c_sku_quote_Field_productSku_200() => AssertShellAsync(
        Method("POST"),
        "/pricing/products/phase4c-sku/quote",
        "{\"customerId\":\"00000000-0000-0000-0000-000000000001\",\"attributes\":[{\"attributeId\":\"00000000-0000-0000-0000-000000000001\",\"valueId\":\"00000000-0000-0000-0000-000000000001\",\"priceAdjustment\":10.5}],\"evaluationAt\":\"2026-09-02T00:00:00Z\"}",
        200,
        requiredField: "productSku");

    // Source assertion 7: Business rule assertion: BR-PRC-005
    // @BR-ID: BR-PRC-005
    [Fact(DisplayName = "007: Business rule assertion: BR-PRC-005")]
    [Trait("BR", "BR-PRC-005")]
    public Task Test007_POST_BASE_URL_pricing_products_phase4c_sku_quote_Field_productSku_200() => AssertShellAsync(
        Method("POST"),
        "/pricing/products/phase4c-sku/quote",
        "{\"customerId\":\"00000000-0000-0000-0000-000000000001\",\"attributes\":[{\"attributeId\":\"00000000-0000-0000-0000-000000000001\",\"valueId\":\"00000000-0000-0000-0000-000000000001\",\"priceAdjustment\":10.5}],\"evaluationAt\":\"2026-09-02T00:00:00Z\"}",
        200,
        requiredField: "productSku");

    // Source assertion 8: Business rule assertion: BR-PRC-006
    // @BR-ID: BR-PRC-006
    [Fact(DisplayName = "008: Business rule assertion: BR-PRC-006")]
    [Trait("BR", "BR-PRC-006")]
    public Task Test008_POST_BASE_URL_pricing_products_phase4c_sku_quote_Field_productSku_200() => AssertShellAsync(
        Method("POST"),
        "/pricing/products/phase4c-sku/quote",
        "{\"customerId\":\"00000000-0000-0000-0000-000000000001\",\"attributes\":[{\"attributeId\":\"00000000-0000-0000-0000-000000000001\",\"valueId\":\"00000000-0000-0000-0000-000000000001\",\"priceAdjustment\":10.5}],\"evaluationAt\":\"2026-09-02T00:00:00Z\"}",
        200,
        requiredField: "productSku");

    // Source assertion 9: Business rule assertion: BR-PRC-007
    // @BR-ID: BR-PRC-007
    [Fact(DisplayName = "009: Business rule assertion: BR-PRC-007")]
    [Trait("BR", "BR-PRC-007")]
    public Task Test009_POST_BASE_URL_pricing_products_phase4c_sku_quote_Field_productSku_200() => AssertShellAsync(
        Method("POST"),
        "/pricing/products/phase4c-sku/quote",
        "{\"customerId\":\"00000000-0000-0000-0000-000000000001\",\"attributes\":[{\"attributeId\":\"00000000-0000-0000-0000-000000000001\",\"valueId\":\"00000000-0000-0000-0000-000000000001\",\"priceAdjustment\":10.5}],\"evaluationAt\":\"2026-09-02T00:00:00Z\"}",
        200,
        requiredField: "productSku");

    // Source assertion 10: Business rule assertion: BR-PRC-008
    // @BR-ID: BR-PRC-008
    [Fact(DisplayName = "010: Business rule assertion: BR-PRC-008")]
    [Trait("BR", "BR-PRC-008")]
    public Task Test010_POST_BASE_URL_pricing_products_phase4c_sku_quote_Field_productSku_200() => AssertShellAsync(
        Method("POST"),
        "/pricing/products/phase4c-sku/quote",
        "{\"customerId\":\"00000000-0000-0000-0000-000000000001\",\"attributes\":[{\"attributeId\":\"00000000-0000-0000-0000-000000000001\",\"valueId\":\"00000000-0000-0000-0000-000000000001\",\"priceAdjustment\":10.5}],\"evaluationAt\":\"2026-09-02T00:00:00Z\"}",
        200,
        requiredField: "productSku");

    // Source assertion 11: Business rule assertion: BR-PRC-009
    // @BR-ID: BR-PRC-009
    [Fact(DisplayName = "011: Business rule assertion: BR-PRC-009")]
    [Trait("BR", "BR-PRC-009")]
    public Task Test011_POST_BASE_URL_pricing_products_phase4c_sku_quote_Field_productSku_200() => AssertShellAsync(
        Method("POST"),
        "/pricing/products/phase4c-sku/quote",
        "{\"customerId\":\"00000000-0000-0000-0000-000000000001\",\"attributes\":[{\"attributeId\":\"00000000-0000-0000-0000-000000000001\",\"valueId\":\"00000000-0000-0000-0000-000000000001\",\"priceAdjustment\":10.5}],\"evaluationAt\":\"2026-09-02T00:00:00Z\"}",
        200,
        requiredField: "productSku");

    // Source assertion 12: Business rule assertion: BR-PRC-011
    // @BR-ID: BR-PRC-011
    [Fact(DisplayName = "012: Business rule assertion: BR-PRC-011")]
    [Trait("BR", "BR-PRC-011")]
    public Task Test012_POST_BASE_URL_pricing_products_phase4c_sku_quote_Field_productSku_200() => AssertShellAsync(
        Method("POST"),
        "/pricing/products/phase4c-sku/quote",
        "{\"customerId\":\"00000000-0000-0000-0000-000000000001\",\"attributes\":[{\"attributeId\":\"00000000-0000-0000-0000-000000000001\",\"valueId\":\"00000000-0000-0000-0000-000000000001\",\"priceAdjustment\":10.5}],\"evaluationAt\":\"2026-09-02T00:00:00Z\"}",
        200,
        requiredField: "productSku");

    // Source assertion 13: Business rule assertion: BR-PRC-012
    // @BR-ID: BR-PRC-012
    [Fact(DisplayName = "013: Business rule assertion: BR-PRC-012")]
    [Trait("BR", "BR-PRC-012")]
    public Task Test013_POST_BASE_URL_pricing_products_phase4c_sku_quote_Field_productSku_200() => AssertShellAsync(
        Method("POST"),
        "/pricing/products/phase4c-sku/quote",
        "{\"customerId\":\"00000000-0000-0000-0000-000000000001\",\"attributes\":[{\"attributeId\":\"00000000-0000-0000-0000-000000000001\",\"valueId\":\"00000000-0000-0000-0000-000000000001\",\"priceAdjustment\":10.5}],\"evaluationAt\":\"2026-09-02T00:00:00Z\"}",
        200,
        requiredField: "productSku");

    // Source assertion 14: Business rule assertion: BR-PRC-013
    // @BR-ID: BR-PRC-013
    [Fact(DisplayName = "014: Business rule assertion: BR-PRC-013")]
    [Trait("BR", "BR-PRC-013")]
    public Task Test014_POST_BASE_URL_pricing_products_phase4c_sku_quote_Field_productSku_200() => AssertShellAsync(
        Method("POST"),
        "/pricing/products/phase4c-sku/quote",
        "{\"customerId\":\"00000000-0000-0000-0000-000000000001\",\"attributes\":[{\"attributeId\":\"00000000-0000-0000-0000-000000000001\",\"valueId\":\"00000000-0000-0000-0000-000000000001\",\"priceAdjustment\":10.5}],\"evaluationAt\":\"2026-09-02T00:00:00Z\"}",
        200,
        requiredField: "productSku");

    // Source assertion 15: Contract success: POST /pricing/promotions/evaluate
    // @BR-ID: BR-PRC-001
    [Fact(DisplayName = "015: Contract success: POST /pricing/promotions/evaluate")]
    [Trait("BR", "BR-PRC-001")]
    public Task Test015_POST_BASE_URL_pricing_promotions_evaluate_Field_items_200() => AssertShellAsync(
        Method("POST"),
        "/pricing/promotions/evaluate",
        "{\"promoCode\":\"phase4c-test\",\"items\":[{\"productSku\":\"phase4c-test\",\"variantSku\":\"phase4c-test\",\"quantity\":1,\"attributes\":[{\"attributeId\":\"00000000-0000-0000-0000-000000000001\",\"valueId\":\"00000000-0000-0000-0000-000000000001\",\"priceAdjustment\":10.5}]}],\"evaluationAt\":\"2026-09-02T00:00:00Z\"}",
        200,
        requiredField: "items");

    // Source assertion 16: Contract error/conformance: POST /pricing/promotions/evaluate
    // @BR-ID: BR-PRC-001
    [Fact(DisplayName = "016: Contract error/conformance: POST /pricing/promotions/evaluate")]
    [Trait("BR", "BR-PRC-001")]
    public Task Test016_POST_BASE_URL_pricing_promotions_evaluate_Status_400() => AssertShellAsync(
        Method("POST"),
        "/pricing/promotions/evaluate",
        "{}",
        400,
        requiredField: null);

    // Source assertion 17: Contract success: POST /pricing/quotes
    // @BR-ID: BR-PRC-001
    [Fact(DisplayName = "017: Contract success: POST /pricing/quotes")]
    [Trait("BR", "BR-PRC-001")]
    public Task Test017_POST_BASE_URL_pricing_quotes_Field_items_200() => AssertShellAsync(
        Method("POST"),
        "/pricing/quotes",
        "{\"currency\":\"phase4c-test\",\"items\":[{\"productSku\":\"phase4c-test\",\"variantSku\":\"phase4c-test\",\"quantity\":1,\"attributes\":[{\"attributeId\":\"00000000-0000-0000-0000-000000000001\",\"valueId\":\"00000000-0000-0000-0000-000000000001\",\"priceAdjustment\":10.5}]}],\"promoCode\":\"phase4c-test\",\"evaluationAt\":\"2026-09-02T00:00:00Z\"}",
        200,
        requiredField: "items");

    // Source assertion 18: Contract error/conformance: POST /pricing/quotes
    // @BR-ID: BR-PRC-001
    [Fact(DisplayName = "018: Contract error/conformance: POST /pricing/quotes")]
    [Trait("BR", "BR-PRC-001")]
    public Task Test018_POST_BASE_URL_pricing_quotes_Status_400() => AssertShellAsync(
        Method("POST"),
        "/pricing/quotes",
        "{}",
        400,
        requiredField: null);

    // Source assertion 19: Contract success: POST /pricing/variants/{variantSku}/quote
    // @BR-ID: BR-PRC-001
    [Fact(DisplayName = "019: Contract success: POST /pricing/variants/{variantSku}/quote")]
    [Trait("BR", "BR-PRC-001")]
    public Task Test019_POST_BASE_URL_pricing_variants_phase4c_sku_quote_Field_productSku_200() => AssertShellAsync(
        Method("POST"),
        "/pricing/variants/phase4c-sku/quote",
        "{\"parentProductSku\":\"phase4c-test\",\"fallbackMode\":\"DirectOnly\",\"evaluationAt\":\"2026-09-02T00:00:00Z\"}",
        200,
        requiredField: "productSku");

    // Source assertion 20: Contract error/conformance: POST /pricing/variants/{variantSku}/quote
    // @BR-ID: BR-PRC-001
    [Fact(DisplayName = "020: Contract error/conformance: POST /pricing/variants/{variantSku}/quote")]
    [Trait("BR", "BR-PRC-001")]
    public Task Test020_POST_BASE_URL_pricing_variants_phase4c_sku_quote_Status_400() => AssertShellAsync(
        Method("POST"),
        "/pricing/variants/phase4c-sku/quote",
        "{}",
        400,
        requiredField: null);

    // Source assertion 21: Contract success: POST /private/products/{sku}/availabilities/{availabilityId}/prices
    // @BR-ID: BR-PRC-001
    [Fact(DisplayName = "021: Contract success: POST /private/products/{sku}/availabilities/{availabilityId}/prices")]
    [Trait("BR", "BR-PRC-001")]
    public Task Test021_POST_BASE_URL_private_products_phase4c_sku_availabilities_ById_prices_Field_id_201() => AssertShellAsync(
        Method("POST"),
        $"/private/products/phase4c-sku/availabilities/{ResourceId}/prices",
        "{\"code\":\"phase4c-test\",\"amount\":10.5,\"priceType\":\"OneTime\",\"defaultPrice\":true,\"specialStartDate\":\"2026-09-02\",\"specialEndDate\":\"2026-09-02\",\"specialAmount\":10.5,\"productIdentifierId\":1}",
        201,
        requiredField: "id");

    // Source assertion 22: Contract error/conformance: POST /private/products/{sku}/availabilities/{availabilityId}/prices
    // @BR-ID: BR-PRC-001
    [Fact(DisplayName = "022: Contract error/conformance: POST /private/products/{sku}/availabilities/{availabilityId}/prices")]
    [Trait("BR", "BR-PRC-001")]
    public Task Test022_POST_BASE_URL_private_products_phase4c_sku_availabilities_ById_prices_Status_400() => AssertShellAsync(
        Method("POST"),
        $"/private/products/phase4c-sku/availabilities/{ResourceId}/prices",
        "{}",
        400,
        requiredField: null);

    // Source assertion 23: Contract success: POST /private/products/{sku}/prices
    // @BR-ID: BR-PRC-001
    [Fact(DisplayName = "023: Contract success: POST /private/products/{sku}/prices")]
    [Trait("BR", "BR-PRC-001")]
    public Task Test023_POST_BASE_URL_private_products_phase4c_sku_prices_Field_id_201() => AssertShellAsync(
        Method("POST"),
        "/private/products/phase4c-sku/prices",
        "{\"availabilityId\":1,\"code\":\"phase4c-test\",\"amount\":10.5,\"priceType\":\"OneTime\",\"defaultPrice\":true,\"specialStartDate\":\"2026-09-02\",\"specialEndDate\":\"2026-09-02\",\"specialAmount\":10.5,\"productIdentifierId\":1}",
        201,
        requiredField: "id");

    // Source assertion 24: Contract error/conformance: POST /private/products/{sku}/prices
    // @BR-ID: BR-PRC-001
    [Fact(DisplayName = "024: Contract error/conformance: POST /private/products/{sku}/prices")]
    [Trait("BR", "BR-PRC-001")]
    public Task Test024_POST_BASE_URL_private_products_phase4c_sku_prices_Status_400() => AssertShellAsync(
        Method("POST"),
        "/private/products/phase4c-sku/prices",
        "{}",
        400,
        requiredField: null);

    // Source assertion 25: Contract success: GET /pricing/products/{sku}/price
    // @BR-ID: BR-PRC-001
    [Fact(DisplayName = "025: Contract success: GET /pricing/products/{sku}/price")]
    [Trait("BR", "BR-PRC-001")]
    public Task Test025_GET_BASE_URL_pricing_products_phase4c_sku_price_Field_productSku_200() => AssertShellAsync(
        Method("GET"),
        "/pricing/products/phase4c-sku/price",
        null,
        200,
        requiredField: "productSku");

    // Source assertion 26: Contract error/conformance: GET /pricing/products/{sku}/price
    // @BR-ID: BR-PRC-001
    [Fact(DisplayName = "026: Contract error/conformance: GET /pricing/products/{sku}/price")]
    [Trait("BR", "BR-PRC-001")]
    public Task Test026_GET_BASE_URL_pricing_products_phase4c_sku_price_Status_400() => AssertShellAsync(
        Method("GET"),
        "/pricing/products/phase4c-sku/price",
        null,
        400,
        requiredField: null);

    // Source assertion 27: Contract success: GET /private/pricing/processors
    // @BR-ID: BR-PRC-001
    [Fact(DisplayName = "027: Contract success: GET /private/pricing/processors")]
    [Trait("BR", "BR-PRC-001")]
    public Task Test027_GET_BASE_URL_private_pricing_processors_Field_processors_200() => AssertShellAsync(
        Method("GET"),
        "/private/pricing/processors",
        null,
        200,
        requiredField: "processors");

    // Source assertion 28: Contract error/conformance: GET /private/pricing/processors
    // @BR-ID: BR-PRC-001
    [Fact(DisplayName = "028: Contract error/conformance: GET /private/pricing/processors")]
    [Trait("BR", "BR-PRC-001")]
    public Task Test028_GET_BASE_URL_private_pricing_processors_Status_401() => AssertShellAsync(
        Method("GET"),
        "/private/pricing/processors",
        null,
        401,
        requiredField: null);

    // Source assertion 29: Contract success: GET /private/products/{sku}/availabilities/{availabilityId}/prices
    // @BR-ID: BR-PRC-001
    [Fact(DisplayName = "029: Contract success: GET /private/products/{sku}/availabilities/{availabilityId}/prices")]
    [Trait("BR", "BR-PRC-001")]
    public Task Test029_GET_BASE_URL_private_products_phase4c_sku_availabilities_ById_prices_Field_items_200() => AssertShellAsync(
        Method("GET"),
        $"/private/products/phase4c-sku/availabilities/{ResourceId}/prices",
        null,
        200,
        requiredField: "items");

    // Source assertion 30: Contract error/conformance: GET /private/products/{sku}/availabilities/{availabilityId}/prices
    // @BR-ID: BR-PRC-001
    [Fact(DisplayName = "030: Contract error/conformance: GET /private/products/{sku}/availabilities/{availabilityId}/prices")]
    [Trait("BR", "BR-PRC-001")]
    public Task Test030_GET_BASE_URL_private_products_phase4c_sku_availabilities_ById_prices_Status_400() => AssertShellAsync(
        Method("GET"),
        $"/private/products/phase4c-sku/availabilities/{ResourceId}/prices",
        null,
        400,
        requiredField: null);

    // Source assertion 31: Contract success: GET /private/products/{sku}/prices
    // @BR-ID: BR-PRC-001
    [Fact(DisplayName = "031: Contract success: GET /private/products/{sku}/prices")]
    [Trait("BR", "BR-PRC-001")]
    public Task Test031_GET_BASE_URL_private_products_phase4c_sku_prices_Field_items_200() => AssertShellAsync(
        Method("GET"),
        "/private/products/phase4c-sku/prices",
        null,
        200,
        requiredField: "items");

    // Source assertion 32: Contract error/conformance: GET /private/products/{sku}/prices
    // @BR-ID: BR-PRC-001
    [Fact(DisplayName = "032: Contract error/conformance: GET /private/products/{sku}/prices")]
    [Trait("BR", "BR-PRC-001")]
    public Task Test032_GET_BASE_URL_private_products_phase4c_sku_prices_Status_400() => AssertShellAsync(
        Method("GET"),
        "/private/products/phase4c-sku/prices",
        null,
        400,
        requiredField: null);

    // Source assertion 33: Contract success: GET /private/products/{sku}/prices/{priceId}
    // @BR-ID: BR-PRC-001
    [Fact(DisplayName = "033: Contract success: GET /private/products/{sku}/prices/{priceId}")]
    [Trait("BR", "BR-PRC-001")]
    public Task Test033_GET_BASE_URL_private_products_phase4c_sku_prices_ById_Field_id_200() => AssertShellAsync(
        Method("GET"),
        $"/private/products/phase4c-sku/prices/{ResourceId}",
        null,
        200,
        requiredField: "id");

    // Source assertion 34: Contract error/conformance: GET /private/products/{sku}/prices/{priceId}
    // @BR-ID: BR-PRC-001
    [Fact(DisplayName = "034: Contract error/conformance: GET /private/products/{sku}/prices/{priceId}")]
    [Trait("BR", "BR-PRC-001")]
    public Task Test034_GET_BASE_URL_private_products_phase4c_sku_prices_ById_Status_400() => AssertShellAsync(
        Method("GET"),
        $"/private/products/phase4c-sku/prices/{ResourceId}",
        null,
        400,
        requiredField: null);

    // Source assertion 35: Contract success: PUT /private/products/{sku}/availabilities/{availabilityId}/prices/{priceId}
    // @BR-ID: BR-PRC-001
    [Fact(DisplayName = "035: Contract success: PUT /private/products/{sku}/availabilities/{availabilityId}/prices/{priceId}")]
    [Trait("BR", "BR-PRC-001")]
    public Task Test035_PUT_BASE_URL_private_products_phase4c_sku_availabilities_ById_prices_ById_Field_id_200() => AssertShellAsync(
        Method("PUT"),
        $"/private/products/phase4c-sku/availabilities/{ResourceId}/prices/{ResourceId}",
        "{\"code\":\"phase4c-test\",\"amount\":10.5,\"priceType\":\"OneTime\",\"defaultPrice\":true,\"specialStartDate\":\"2026-09-02\",\"specialEndDate\":\"2026-09-02\",\"specialAmount\":10.5,\"productIdentifierId\":1}",
        200,
        requiredField: "id");

    // Source assertion 36: Contract error/conformance: PUT /private/products/{sku}/availabilities/{availabilityId}/prices/{priceId}
    // @BR-ID: BR-PRC-001
    [Fact(DisplayName = "036: Contract error/conformance: PUT /private/products/{sku}/availabilities/{availabilityId}/prices/{priceId}")]
    [Trait("BR", "BR-PRC-001")]
    public Task Test036_PUT_BASE_URL_private_products_phase4c_sku_availabilities_ById_prices_ById_Status_400() => AssertShellAsync(
        Method("PUT"),
        $"/private/products/phase4c-sku/availabilities/{ResourceId}/prices/{ResourceId}",
        "{}",
        400,
        requiredField: null);

    // Source assertion 37: Contract success: DELETE /private/products/{sku}/prices/{priceId}
    // @BR-ID: BR-PRC-001
    [Fact(DisplayName = "037: Contract success: DELETE /private/products/{sku}/prices/{priceId}")]
    [Trait("BR", "BR-PRC-001")]
    public Task Test037_DELETE_BASE_URL_private_products_phase4c_sku_prices_ById_Field_id_204() => AssertShellAsync(
        Method("DELETE"),
        $"/private/products/phase4c-sku/prices/{ResourceId}",
        null,
        204,
        requiredField: "id");

    // Source assertion 38: Contract error/conformance: DELETE /private/products/{sku}/prices/{priceId}
    // @BR-ID: BR-PRC-001
    [Fact(DisplayName = "038: Contract error/conformance: DELETE /private/products/{sku}/prices/{priceId}")]
    [Trait("BR", "BR-PRC-001")]
    public Task Test038_DELETE_BASE_URL_private_products_phase4c_sku_prices_ById_Status_400() => AssertShellAsync(
        Method("DELETE"),
        $"/private/products/phase4c-sku/prices/{ResourceId}",
        null,
        400,
        requiredField: null);
}
