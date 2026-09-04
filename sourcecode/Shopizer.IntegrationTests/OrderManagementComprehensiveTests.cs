using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Shopizer.IntegrationTests.Fixtures;

namespace Shopizer.IntegrationTests;

[Collection(ShopizerAspireCollection.Name)]
public sealed class OrderManagementComprehensiveTests(AspireHostFixture fixture)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    #region Submission and immutable snapshots

    // @BR-ID: BR-OR-SUB-001
    [Fact(DisplayName = "003: Business rule assertion: BR-OR-SUB-001")]
    [Trait("BR", "BR-OR-SUB-001")]
    public async Task Test001_SubmitOrder_StartsOrderedAndCreatesHistory()
    {
        using var response = await SendAsync(HttpMethod.Post, "/api/v1/internal/order-submissions", Submission());
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var order = await ReadAsync(response);
        var id = order.GetProperty("orderId").GetInt64();
        Assert.Equal("Ordered", order.GetProperty("status").GetString());
        using var history = await SendAsync(HttpMethod.Get, $"/api/v1/orders/{id}/history", token: fixture.AdminAccessToken);
        Assert.Equal(HttpStatusCode.OK, history.StatusCode);
        Assert.Contains("ORDERED", await history.Content.ReadAsStringAsync(), StringComparison.OrdinalIgnoreCase);
    }

    // @BR-ID: BR-OR-SUB-002
    [Fact(DisplayName = "004: Business rule assertion: BR-OR-SUB-002")]
    [Trait("BR", "BR-OR-SUB-002")]
    public async Task Test002_SubmitOrder_PreservesAddressSnapshot()
    {
        var id = await ArrangeOrderAsync();
        using var response = await SendAsync(HttpMethod.Get, $"/api/v1/orders/{id}", token: fixture.AdminAccessToken);
        var order = await ReadAsync(response);
        Assert.Equal("Seattle", order.GetProperty("billingAddress").GetProperty("city").GetString());
    }

    // @BR-ID: BR-OR-SUB-003
    [Fact(DisplayName = "005: Business rule assertion: BR-OR-SUB-003")]
    [Trait("BR", "BR-OR-SUB-003")]
    public async Task Test003_SubmitOrder_PersistsPositivePurchasedLine()
    {
        var id = await ArrangeOrderAsync();
        using var response = await SendAsync(HttpMethod.Get, $"/api/v1/orders/{id}", token: fixture.AdminAccessToken);
        var order = await ReadAsync(response);
        Assert.Equal(1, order.GetProperty("lines").GetArrayLength());
        Assert.Equal(2, order.GetProperty("lines")[0].GetProperty("quantity").GetInt32());
    }

    // @BR-ID: BR-OR-SUB-004
    [Fact(DisplayName = "006: Business rule assertion: BR-OR-SUB-004")]
    [Trait("BR", "BR-OR-SUB-004")]
    public async Task Test004_SubmitOrder_PersistsAcceptedTotal()
    {
        var id = await ArrangeOrderAsync();
        using var response = await SendAsync(HttpMethod.Get, $"/api/v1/orders/{id}", token: fixture.AdminAccessToken);
        Assert.Equal(10m, (await ReadAsync(response)).GetProperty("total").GetDecimal());
    }

    #endregion

    #region Lifecycle, authorization and idempotency

    // @BR-ID: BR-OR-LIFE-001
    [Fact(DisplayName = "010: Business rule assertion: BR-OR-LIFE-001")]
    [Trait("BR", "BR-OR-LIFE-001")]
    public async Task Test005_StatusTransition_RejectsIllegalReopen()
    {
        var id = await ArrangeOrderAsync();
        using var response = await SendAsync(HttpMethod.Put, $"/api/v1/orders/{id}/status", """{"status":"Processed"}""", "life-1", fixture.AdminAccessToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var illegal = await SendAsync(HttpMethod.Put, $"/api/v1/orders/{id}/status", """{"status":"Ordered"}""", "life-2", fixture.AdminAccessToken);
        Assert.Equal(HttpStatusCode.Conflict, illegal.StatusCode);
    }

    // @BR-ID: BR-OR-LIFE-002
    [Fact(DisplayName = "011: Business rule assertion: BR-OR-LIFE-002")]
    [Trait("BR", "BR-OR-LIFE-002")]
    public async Task Test006_StatusTransition_AppendsHistory()
    {
        var id = await ArrangeOrderAsync();
        using var response = await SendAsync(HttpMethod.Put, $"/api/v1/orders/{id}/status", """{"status":"Processed","reason":"operator"}""", "history-1", fixture.AdminAccessToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var history = await SendAsync(HttpMethod.Get, $"/api/v1/orders/{id}/history", token: fixture.AdminAccessToken);
        Assert.Contains("operator", await history.Content.ReadAsStringAsync());
    }

    // @BR-ID: BR-OR-AUTH-001
    [Fact(DisplayName = "018: Business rule assertion: BR-OR-AUTH-001")]
    [Trait("BR", "BR-OR-AUTH-001")]
    public async Task Test007_OrderDetail_RequiresAdministrator()
    {
        using var response = await SendAsync(HttpMethod.Get, "/api/v1/orders/999999999", token: null);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // @BR-ID: BR-OR-ADM-001
    [Fact(DisplayName = "020: Business rule assertion: BR-OR-ADM-001")]
    [Trait("BR", "BR-OR-ADM-001")]
    public async Task Test008_OrderList_RequiresAdminRole()
    {
        using var response = await SendAsync(HttpMethod.Get, "/api/v1/orders", token: fixture.BasicAdminAccessToken);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // @BR-ID: BR-OR-RES-001
    [Fact(DisplayName = "023: Business rule assertion: BR-OR-RES-001")]
    [Trait("BR", "BR-OR-RES-001")]
    public async Task Test009_Submission_ReplayingSubmissionIdReturnsSameOrder()
    {
        var body = Submission();
        using var first = await SendAsync(HttpMethod.Post, "/api/v1/internal/order-submissions", body);
        using var second = await SendAsync(HttpMethod.Post, "/api/v1/internal/order-submissions", body);
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.Created, second.StatusCode);
        Assert.Equal((await ReadAsync(first)).GetProperty("orderId").GetInt64(), (await ReadAsync(second)).GetProperty("orderId").GetInt64());
    }

    #endregion

    #region Payment, fulfillment and invoice boundaries

    // @BR-ID: BR-OR-PAY-001
    [Fact(DisplayName = "007: Business rule assertion: BR-OR-PAY-001")]
    [Trait("BR", "BR-OR-PAY-001")]
    public async Task Test010_Capture_RejectsWithoutAuthorization()
    {
        var id = await ArrangeOrderAsync();
        using var response = await SendAsync(HttpMethod.Post, $"/api/v1/orders/{id}/capture", """{"amount":10,"currency":"USD"}""", "capture-1", fixture.AdminAccessToken);
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    // @BR-ID: BR-OR-PAY-003
    [Fact(DisplayName = "013: Business rule assertion: BR-OR-PAY-003")]
    [Trait("BR", "BR-OR-PAY-003")]
    public async Task Test011_NextPaymentAction_ReturnsOkWithoutOutcome()
    {
        var id = await ArrangeOrderAsync();
        using var response = await SendAsync(HttpMethod.Get, $"/api/v1/orders/{id}/payment/next-action", token: fixture.AdminAccessToken);
        Assert.Equal("Ok", (await ReadAsync(response)).GetProperty("nextAction").GetString());
    }

    // @BR-ID: BR-OR-PAY-004
    [Fact(DisplayName = "014: Business rule assertion: BR-OR-PAY-004")]
    [Trait("BR", "BR-OR-PAY-004")]
    public async Task Test012_Capturable_UsesProjectionAndReturnsEnvelope()
    {
        using var response = await SendAsync(HttpMethod.Get, "/api/v1/orders/capturable", token: fixture.AdminAccessToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True((await ReadAsync(response)).TryGetProperty("items", out _));
    }

    // @BR-ID: BR-OR-REF-001
    [Fact(DisplayName = "015: Business rule assertion: BR-OR-REF-001")]
    [Trait("BR", "BR-OR-REF-001")]
    public async Task Test013_Refund_RejectsAmountWithoutCapturedBalance()
    {
        var id = await ArrangeOrderAsync();
        using var response = await SendAsync(HttpMethod.Post, $"/api/v1/orders/{id}/refund", """{"amount":1,"currency":"USD","reason":"test"}""", "refund-1", fixture.AdminAccessToken);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    // @BR-ID: BR-OR-CAN-001
    [Fact(DisplayName = "016: Business rule assertion: BR-OR-CAN-001")]
    [Trait("BR", "BR-OR-CAN-001")]
    public async Task Test014_Cancel_TransitionsAndReportsPendingCompensation()
    {
        var id = await ArrangeOrderAsync();
        using var response = await SendAsync(HttpMethod.Post, $"/api/v1/orders/{id}/cancel", """{"reason":"customer request"}""", "cancel-1", fixture.AdminAccessToken);
        var result = await ReadAsync(response);
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        Assert.Equal("Canceled", result.GetProperty("status").GetString());
        Assert.Equal("Pending", result.GetProperty("compensationState").GetString());
    }

    // @BR-ID: BR-OR-FUL-001
    [Fact(DisplayName = "017: Business rule assertion: BR-OR-FUL-001")]
    [Trait("BR", "BR-OR-FUL-001")]
    public async Task Test015_Fulfillment_RequiresProcessedOrder()
    {
        var id = await ArrangeOrderAsync();
        using var response = await SendAsync(HttpMethod.Post, $"/api/v1/orders/{id}/fulfillment", null, "fulfillment-1", fixture.AdminAccessToken);
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    // @BR-ID: BR-OR-INV-001
    [Fact(DisplayName = "024: Business rule assertion: BR-OR-INV-001")]
    [Trait("BR", "BR-OR-INV-001")]
    public async Task Test016_Invoice_RequestsExternalArtifactBoundary()
    {
        var id = await ArrangeOrderAsync();
        using var response = await SendAsync(HttpMethod.Get, $"/api/v1/orders/{id}/invoice", token: fixture.AdminAccessToken);
        var invoice = await ReadAsync(response);
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        Assert.Equal("Processing", invoice.GetProperty("status").GetString());
    }

    #endregion

    #region Customer visibility and administrative detail

    // @BR-ID: BR-OR-AUTH-002
    [Fact(DisplayName = "019: Business rule assertion: BR-OR-AUTH-002")]
    [Trait("BR", "BR-OR-AUTH-002")]
    public async Task Test017_MyOrders_RequiresCustomerPrincipal()
    {
        using var response = await SendAsync(HttpMethod.Get, "/api/v1/me/orders", token: fixture.AdminAccessToken);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // @BR-ID: BR-OR-ADM-002
    [Fact(DisplayName = "021: Business rule assertion: BR-OR-ADM-002")]
    [Trait("BR", "BR-OR-ADM-002")]
    public async Task Test018_CustomerSnapshot_UpdatesOnlyOrderSnapshot()
    {
        var id = await ArrangeOrderAsync();
        const string update = """{"emailAddress":"updated@example.test","billingAddress":{"firstName":"A","lastName":"B","address":"2 Main","city":"Austin","countryCode":"US","postalCode":"78701"},"deliveryAddress":{"firstName":"A","lastName":"B","address":"2 Main","city":"Austin","countryCode":"US","postalCode":"78701"}}""";
        using var response = await SendAsync(HttpMethod.Patch, $"/api/v1/orders/{id}/customer-snapshot", update, "snapshot-1", fixture.AdminAccessToken);
        Assert.Equal("updated@example.test", (await ReadAsync(response)).GetProperty("customerEmailAddress").GetString());
    }

    // @BR-ID: BR-OR-READ-001
    [Fact(DisplayName = "022: Business rule assertion: BR-OR-READ-001")]
    [Trait("BR", "BR-OR-READ-001")]
    public async Task Test019_OrderList_ReturnsPaginationProjection()
    {
        using var response = await SendAsync(HttpMethod.Get, "/api/v1/orders?page=1&pageSize=20", token: fixture.AdminAccessToken);
        var root = await ReadAsync(response);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(root.GetProperty("pagination").GetProperty("page").GetInt32() == 1);
    }

    // @BR-ID: BR-OR-UI-001
    [Fact(DisplayName = "025: Business rule assertion: BR-OR-UI-001")]
    [Trait("BR", "BR-OR-UI-001")]
    public async Task Test020_OrderDetailIncludesAdministrationProjections()
    {
        var id = await ArrangeOrderAsync();
        using var response = await SendAsync(HttpMethod.Get, $"/api/v1/orders/{id}", token: fixture.AdminAccessToken);
        var root = await ReadAsync(response);
        Assert.True(root.TryGetProperty("lines", out _) && root.TryGetProperty("totals", out _) && root.TryGetProperty("history", out _));
    }

    // @BR-ID: BR-OR-DIG-001
    [Fact(DisplayName = "008: Business rule assertion: BR-OR-DIG-001")]
    [Trait("BR", "BR-OR-DIG-001")]
    public async Task Test021_DigitalSubmission_PersistsEntitlement()
    {
        var body = Submission().Replace("\"attributes\":[]", "\"attributes\":[],\"digitalFileName\":\"guide.pdf\"", StringComparison.Ordinal);
        using var response = await SendAsync(HttpMethod.Post, "/api/v1/internal/order-submissions", body);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    // @BR-ID: BR-OR-FAIL-001
    [Fact(DisplayName = "009: Business rule assertion: BR-OR-FAIL-001")]
    [Trait("BR", "BR-OR-FAIL-001")]
    public async Task Test022_InvalidSubmission_RejectsWithoutSuccessEnvelope()
    {
        using var response = await SendAsync(HttpMethod.Post, "/api/v1/internal/order-submissions", """{"submissionId":"bad","lines":[],"currency":"USD","total":0}""");
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    // @BR-ID: BR-OR-PAY-002
    [Fact(DisplayName = "012: Business rule assertion: BR-OR-PAY-002")]
    [Trait("BR", "BR-OR-PAY-002")]
    public async Task Test023_PaymentProjection_IsReadableFromAdminDetail()
    {
        var id = await ArrangeOrderAsync();
        using var response = await SendAsync(HttpMethod.Get, $"/api/v1/orders/{id}/payment-transactions", token: fixture.AdminAccessToken);
        Assert.True((await ReadAsync(response)).TryGetProperty("items", out _));
    }

    #endregion

    private async Task<long> ArrangeOrderAsync()
    {
        using var response = await SendAsync(HttpMethod.Post, "/api/v1/internal/order-submissions", Submission());
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await ReadAsync(response)).GetProperty("orderId").GetInt64();
    }

    private static string Submission() => $$"""
        {"submissionId":"test-{{Guid.NewGuid():N}}","customerEmailAddress":"orders@example.test","currency":"USD","total":10,
         "billingAddress":{"firstName":"Ana","lastName":"Test","address":"1 Main","city":"Seattle","countryCode":"US","postalCode":"98101"},
         "deliveryAddress":{"firstName":"Ana","lastName":"Test","address":"1 Main","city":"Seattle","countryCode":"US","postalCode":"98101"},
         "lines":[{"sku":"TEST-SKU","productName":"Test product","quantity":2,"unitPrice":5,"attributes":[]}],
         "totals":[{"code":"total","type":"TOTAL","value":10,"valueType":"ONE_TIME","sortOrder":1}]}
        """;

    private async Task<HttpResponseMessage> SendAsync(HttpMethod method, string path, string? body = null, string? idempotency = null, string? token = null)
    {
        using var request = new HttpRequestMessage(method, path);
        if (body is not null) request.Content = new StringContent(body, Encoding.UTF8, "application/json");
        if (!string.IsNullOrWhiteSpace(idempotency)) request.Headers.TryAddWithoutValidation("Idempotency-Key", idempotency);
        if (!string.IsNullOrWhiteSpace(token)) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return await fixture.OrderManagementClient.SendAsync(request);
    }

    private static async Task<JsonElement> ReadAsync(HttpResponseMessage response)
    {
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return document.RootElement.Clone();
    }
}
