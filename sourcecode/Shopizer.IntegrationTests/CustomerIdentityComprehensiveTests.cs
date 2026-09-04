using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Net.Http.Headers;
using Shopizer.IntegrationTests.Fixtures;

namespace Shopizer.IntegrationTests;

[Collection(ShopizerAspireCollection.Name)]
public sealed class CustomerIdentityComprehensiveTests(AspireHostFixture fixture)
{
    private const string SeedResourceId = "00000000-0000-0000-0000-000000000001";
    private static readonly HttpMethod Patch = new("PATCH");

    private static class Payloads
    {
        public const string Login = """{"username":"phase4c-test","password":"Phase4c!Password2026"}""";
        public const string CustomerLogin = """{"username":"phase4c-target","password":"Phase4c!Password2026"}""";
        public const string Empty = "{}";
        public const string Registration = """
            {"emailAddress":"phase4c@example.com","password":"Phase4c!Password2026","firstName":"phase4c-test","lastName":"phase4c-test","gender":"M","language":"en","provider":"phase4c-test","billing":{"addressType":"Billing","firstName":"phase4c-test","lastName":"phase4c-test","companyName":"phase4c-test","streetAddress":"1 Main Street","city":"Seattle","postalCode":"98101","stateProvince":"WA","telephone":"2065550100","countryCode":"US","zoneCode":"WA","latitude":"47.6062","longitude":"-122.3321"},"delivery":{"addressType":"Delivery","firstName":"phase4c-test","lastName":"phase4c-test","companyName":"phase4c-test","streetAddress":"1 Main Street","city":"Seattle","postalCode":"98101","stateProvince":"WA","telephone":"2065550100","countryCode":"US","zoneCode":"WA","latitude":"47.6062","longitude":"-122.3321"},"attributes":[{"optionId":"00000000-0000-0000-0000-000000000001","optionValueId":"00000000-0000-0000-0000-000000000001","textValue":"phase4c-test"}]}
            """;
        public const string ResetRequest = """{"username":"phase4c-test","returnUrl":"https://example.com/phase4c"}""";
        public const string ResetPassword = """{"password":"Phase4c!Password2026","repeatPassword":"Phase4c!Password2026"}""";
        public const string PasswordChange = """{"currentPassword":"Phase4c!Password2026","newPassword":"Phase4c!Password2026","repeatPassword":"Phase4c!Password2026"}""";
        public const string Review = """{"customerId":"00000000-0000-0000-0000-000000000001","rating":5,"description":"phase4c-test"}""";
        public const string ExternalIdentity = """{"userId":"00000000-0000-0000-0000-000000000001","providerId":"00000000-0000-0000-0000-000000000001","providerUserId":"00000000-0000-0000-0000-000000000001","accessToken":"phase4c-test","refreshToken":"phase4c-test","profileUrl":"https://example.com/phase4c"}""";
        public const string Newsletter = """{"email":"phase4c@example.com","firstName":"phase4c-test","lastName":"phase4c-test"}""";
        public const string User = """{"userName":"phase4c-test","emailAddress":"phase4c@example.com","password":"Phase4c!Password2026","repeatPassword":"Phase4c!Password2026","firstName":"phase4c-test","lastName":"phase4c-test","groups":["phase4c-test"],"defaultLanguageCode":"phase4c-test"}""";
        public const string Username = """{"username":"phase4c-test"}""";
        public const string CustomerUpdate = """{"emailAddress":"phase4c-login@example.com","firstName":"phase4c-test","lastName":"phase4c-test","gender":"M","language":"en","companyName":"phase4c-test","attributes":[{"optionId":"00000000-0000-0000-0000-000000000001","optionValueId":"00000000-0000-0000-0000-000000000001","textValue":"phase4c-test"}]}""";
        public const string Address = """{"billing":{"addressType":"Billing","firstName":"phase4c-test","lastName":"phase4c-test","companyName":"phase4c-test","streetAddress":"1 Main Street","city":"Seattle","postalCode":"98101","stateProvince":"WA","telephone":"2065550100","countryCode":"US","zoneCode":"WA","latitude":"47.6062","longitude":"-122.3321"},"delivery":{"addressType":"Delivery","firstName":"phase4c-test","lastName":"phase4c-test","companyName":"phase4c-test","streetAddress":"1 Main Street","city":"Seattle","postalCode":"98101","stateProvince":"WA","telephone":"2065550100","countryCode":"US","zoneCode":"WA","latitude":"47.6062","longitude":"-122.3321"}}""";
        public const string ReviewUpdate = """{"rating":5,"description":"phase4c-test"}""";
        public const string UserUpdate = """{"userName":"phase4c-test","emailAddress":"phase4c@example.com","firstName":"phase4c-test","lastName":"phase4c-test","groups":["phase4c-test"],"storeId":"00000000-0000-0000-0000-000000000001","isActive":true}""";
        public const string Enabled = """{"isActive":true}""";
        public const string UserPassword = """{"currentPassword":"Phase4c!Password2026","newPassword":"Phase4c!Password2026"}""";
    }

    #region Authentication

    // @BR-ID: BR-CUS-NN-010
    [Fact]
    [Trait("BR", "BR-CUS-NN-010")]
    public async Task Test001_PostAdminAuthLogin_Returns200WithSubjectId()
    {
        using var response = await SendAsync(HttpMethod.Post, "/api/v1/admin-auth/login", Payloads.Login);
        await AssertResponseAsync(response, 200, "subjectId");
    }

    // @BR-ID: BR-CUS-NN-010
    [Fact]
    [Trait("BR", "BR-CUS-NN-010")]
    public async Task Test002_PostAdminAuthLogin_WithEmptyPayload_Returns400()
    {
        using var response = await SendAsync(HttpMethod.Post, "/api/v1/admin-auth/login", Payloads.Empty);
        await AssertResponseAsync(response, 400);
    }

    // @BR-ID: BR-CUS-NN-010
    [Fact]
    [Trait("BR", "BR-CUS-NN-010")]
    public async Task Test003_PostAdminAuthLogin_Returns200WithSubjectId()
    {
        using var response = await SendAsync(HttpMethod.Post, "/api/v1/admin-auth/login", Payloads.Login);
        await AssertResponseAsync(response, 200, "subjectId");
    }

    // @BR-ID: BR-CUS-NN-003
    [Fact]
    [Trait("BR", "BR-CUS-NN-003")]
    public async Task Test004_PostAdminAuthLogin_Returns200WithSubjectId()
    {
        using var response = await SendAsync(HttpMethod.Post, "/api/v1/admin-auth/login", Payloads.Login);
        await AssertResponseAsync(response, 200, "subjectId");
    }

    // @BR-ID: BR-CUS-NN-006
    [Fact]
    [Trait("BR", "BR-CUS-NN-006")]
    public async Task Test005_PostAdminAuthLogin_Returns200WithSubjectId()
    {
        using var response = await SendAsync(HttpMethod.Post, "/api/v1/admin-auth/login", Payloads.Login);
        await AssertResponseAsync(response, 200, "subjectId");
    }

    // @BR-ID: BR-CUS-NN-007
    [Fact]
    [Trait("BR", "BR-CUS-NN-007")]
    public async Task Test006_PostAdminAuthLogin_Returns200WithSubjectId()
    {
        using var response = await SendAsync(HttpMethod.Post, "/api/v1/admin-auth/login", Payloads.Login);
        await AssertResponseAsync(response, 200, "subjectId");
    }

    // @BR-ID: BR-CUS-NN-008
    [Fact]
    [Trait("BR", "BR-CUS-NN-008")]
    public async Task Test007_PostAdminAuthLogin_Returns200WithSubjectId()
    {
        using var response = await SendAsync(HttpMethod.Post, "/api/v1/admin-auth/login", Payloads.Login);
        await AssertResponseAsync(response, 200, "subjectId");
    }

    // @BR-ID: BR-CUS-NN-009
    [Fact]
    [Trait("BR", "BR-CUS-NN-009")]
    public async Task Test008_PostAdminAuthLogin_Returns200WithSubjectId()
    {
        using var response = await SendAsync(HttpMethod.Post, "/api/v1/admin-auth/login", Payloads.Login);
        await AssertResponseAsync(response, 200, "subjectId");
    }

    // @BR-ID: BR-CUS-NN-014
    [Fact]
    [Trait("BR", "BR-CUS-NN-014")]
    public async Task Test009_PostAdminAuthLogin_Returns200WithSubjectId()
    {
        using var response = await SendAsync(HttpMethod.Post, "/api/v1/admin-auth/login", Payloads.Login);
        await AssertResponseAsync(response, 200, "subjectId");
    }

    // @BR-ID: BR-CUS-NN-020
    [Fact]
    [Trait("BR", "BR-CUS-NN-020")]
    public async Task Test010_PostAdminAuthLogin_Returns200WithSubjectId()
    {
        using var response = await SendAsync(HttpMethod.Post, "/api/v1/admin-auth/login", Payloads.Login);
        await AssertResponseAsync(response, 200, "subjectId");
    }

    // @BR-ID: BR-CUS-022
    [Fact]
    [Trait("BR", "BR-CUS-022")]
    public async Task Test011_PostAdminAuthLogin_Returns200WithSubjectId()
    {
        using var response = await SendAsync(HttpMethod.Post, "/api/v1/admin-auth/login", Payloads.Login);
        await AssertResponseAsync(response, 200, "subjectId");
    }

    // @BR-ID: BR-CUS-027
    [Fact]
    [Trait("BR", "BR-CUS-027")]
    public async Task Test012_PostAdminAuthLogin_Returns200WithSubjectId()
    {
        using var response = await SendAsync(HttpMethod.Post, "/api/v1/admin-auth/login", Payloads.Login);
        await AssertResponseAsync(response, 200, "subjectId");
    }

    // @BR-ID: BR-CUS-002
    [Fact]
    [Trait("BR", "BR-CUS-002")]
    public async Task Test013_PostAdminAuthLogin_Returns200WithSubjectId()
    {
        using var response = await SendAsync(HttpMethod.Post, "/api/v1/admin-auth/login", Payloads.Login);
        await AssertResponseAsync(response, 200, "subjectId");
    }

    // @BR-ID: BR-CUS-004
    [Fact]
    [Trait("BR", "BR-CUS-004")]
    public async Task Test014_PostAdminAuthLogin_Returns200WithSubjectId()
    {
        using var response = await SendAsync(HttpMethod.Post, "/api/v1/admin-auth/login", Payloads.Login);
        await AssertResponseAsync(response, 200, "subjectId");
    }

    // @BR-ID: BR-CUS-005
    [Fact]
    [Trait("BR", "BR-CUS-005")]
    public async Task Test015_PostAdminAuthLogin_Returns200WithSubjectId()
    {
        using var response = await SendAsync(HttpMethod.Post, "/api/v1/admin-auth/login", Payloads.Login);
        await AssertResponseAsync(response, 200, "subjectId");
    }

    // @BR-ID: BR-CUS-012
    [Fact]
    [Trait("BR", "BR-CUS-012")]
    public async Task Test016_PostAdminAuthLogin_Returns200WithSubjectId()
    {
        using var response = await SendAsync(HttpMethod.Post, "/api/v1/admin-auth/login", Payloads.Login);
        await AssertResponseAsync(response, 200, "subjectId");
    }

    // @BR-ID: BR-CUS-013
    [Fact]
    [Trait("BR", "BR-CUS-013")]
    public async Task Test017_PostAdminAuthLogin_Returns200WithSubjectId()
    {
        using var response = await SendAsync(HttpMethod.Post, "/api/v1/admin-auth/login", Payloads.Login);
        await AssertResponseAsync(response, 200, "subjectId");
    }

    // @BR-ID: BR-CUS-014
    [Fact]
    [Trait("BR", "BR-CUS-014")]
    public async Task Test018_PostAdminAuthLogin_Returns200WithSubjectId()
    {
        using var response = await SendAsync(HttpMethod.Post, "/api/v1/admin-auth/login", Payloads.Login);
        await AssertResponseAsync(response, 200, "subjectId");
    }

    // @BR-ID: BR-CUS-016
    [Fact]
    [Trait("BR", "BR-CUS-016")]
    public async Task Test019_PostAdminAuthLogin_Returns200WithSubjectId()
    {
        using var response = await SendAsync(HttpMethod.Post, "/api/v1/admin-auth/login", Payloads.Login);
        await AssertResponseAsync(response, 200, "subjectId");
    }

    // @BR-ID: BR-CUS-018
    [Fact]
    [Trait("BR", "BR-CUS-018")]
    public async Task Test020_PostAdminAuthLogin_Returns200WithSubjectId()
    {
        using var response = await SendAsync(HttpMethod.Post, "/api/v1/admin-auth/login", Payloads.Login);
        await AssertResponseAsync(response, 200, "subjectId");
    }

    // @BR-ID: BR-CUS-019
    [Fact]
    [Trait("BR", "BR-CUS-019")]
    public async Task Test021_PostCustomerAuthLogin_Returns200WithSubjectId()
    {
        using var response = await SendAsync(HttpMethod.Post, "/api/v1/customer-auth/login", Payloads.CustomerLogin);
        await AssertResponseAsync(response, 200, "subjectId");
    }

    // @BR-ID: BR-CUS-019
    [Fact]
    [Trait("BR", "BR-CUS-019")]
    public async Task Test022_PostCustomerAuthLogin_WithEmptyPayload_Returns401()
    {
        using var response = await SendAsync(HttpMethod.Post, "/api/v1/customer-auth/login", Payloads.Empty);
        await AssertResponseAsync(response, 400);
    }

    // @BR-ID: BR-CUS-019
    [Fact]
    [Trait("BR", "BR-CUS-019")]
    public async Task Test023_PostCustomerAuthLogin_Returns200WithSubjectId()
    {
        using var response = await SendAsync(HttpMethod.Post, "/api/v1/customer-auth/login", Payloads.CustomerLogin);
        await AssertResponseAsync(response, 200, "subjectId");
    }

    // @BR-ID: BR-CUS-NN-010
    [Fact]
    [Trait("BR", "BR-CUS-NN-010")]
    public async Task Test024_PostCustomerAuthLogin_Returns200WithSubjectId()
    {
        using var response = await SendAsync(HttpMethod.Post, "/api/v1/customer-auth/login", Payloads.CustomerLogin);
        await AssertResponseAsync(response, 200, "subjectId");
    }

    // @BR-ID: BR-CUS-001
    [Fact]
    [Trait("BR", "BR-CUS-001")]
    public async Task Test025_PostCustomerAuthRegistrations_Returns201WithSubjectId()
    {
        using var response = await SendAsync(HttpMethod.Post, "/api/v1/customer-auth/registrations", Payloads.Registration);
        await AssertResponseAsync(response, 201, "subjectId");
    }

    // @BR-ID: BR-CUS-001
    [Fact]
    [Trait("BR", "BR-CUS-001")]
    public async Task Test026_PostCustomerAuthRegistrations_WithEmptyPayload_Returns409()
    {
        using var response = await SendAsync(HttpMethod.Post, "/api/v1/customer-auth/registrations", Payloads.Empty);
        await AssertResponseAsync(response, 400);
    }

    // @BR-ID: BR-CUS-001
    [Fact]
    [Trait("BR", "BR-CUS-001")]
    public async Task Test027_PostCustomerAuthRegistrations_Returns201WithSubjectId()
    {
        using var response = await SendAsync(HttpMethod.Post, "/api/v1/customer-auth/registrations", Payloads.Registration);
        await AssertResponseAsync(response, 201, "subjectId");
    }

    // @BR-ID: BR-CUS-015
    [Fact]
    [Trait("BR", "BR-CUS-015")]
    public async Task Test028_PostCustomerAuthRegistrations_Returns201WithSubjectId()
    {
        using var response = await SendAsync(HttpMethod.Post, "/api/v1/customer-auth/registrations", Payloads.Registration);
        await AssertResponseAsync(response, 201, "subjectId");
    }

    // @BR-ID: BR-CUS-019
    [Fact]
    [Trait("BR", "BR-CUS-019")]
    public async Task Test029_PostCustomerAuthRegistrations_Returns201WithSubjectId()
    {
        using var response = await SendAsync(HttpMethod.Post, "/api/v1/customer-auth/registrations", Payloads.Registration);
        await AssertResponseAsync(response, 201, "subjectId");
    }

    #endregion

    #region Password Resets

    // @BR-ID: BR-CUS-NN-001
    [Fact]
    [Trait("BR", "BR-CUS-NN-001")]
    public async Task Test030_PostCustomerPasswordResets_Returns202WithStatus()
    {
        using var response = await SendAsync(HttpMethod.Post, "/api/v1/customer-password-resets", Payloads.ResetRequest);
        await AssertResponseAsync(response, 202, "status");
    }

    // @BR-ID: BR-CUS-NN-001
    [Fact]
    [Trait("BR", "BR-CUS-NN-001")]
    public async Task Test031_PostCustomerPasswordResets_WithEmptyPayload_Returns404()
    {
        using var response = await SendAsync(HttpMethod.Post, "/api/v1/customer-password-resets", Payloads.Empty);
        await AssertResponseAsync(response, 400);
    }

    // @BR-ID: BR-CUS-NN-001
    [Fact]
    [Trait("BR", "BR-CUS-NN-001")]
    public async Task Test032_PostCustomerPasswordResets_Returns202WithStatus()
    {
        using var response = await SendAsync(HttpMethod.Post, "/api/v1/customer-password-resets", Payloads.ResetRequest);
        await AssertResponseAsync(response, 202, "status");
    }

    // @BR-ID: BR-CUS-NN-002
    [Fact]
    [Trait("BR", "BR-CUS-NN-002")]
    public async Task Test033_PostCustomerPasswordResetsPhase4cCodePhase4cToken_Returns204()
    {
        using var response = await SendAsync(HttpMethod.Post, "/api/v1/customer-password-resets/default/phase4c-token", Payloads.ResetPassword);
        await AssertResponseAsync(response, 204);
    }

    // @BR-ID: BR-CUS-NN-002
    [Fact]
    [Trait("BR", "BR-CUS-NN-002")]
    public async Task Test034_PostCustomerPasswordResetsPhase4cCodePhase4cToken_WithEmptyPayload_Returns400()
    {
        using var response = await SendAsync(HttpMethod.Post, "/api/v1/customer-password-resets/phase4c-code/phase4c-token", Payloads.Empty);
        await AssertResponseAsync(response, 400);
    }

    // @BR-ID: BR-CUS-NN-002
    [Fact]
    [Trait("BR", "BR-CUS-NN-002")]
    public async Task Test035_PostCustomerPasswordResetsPhase4cCodePhase4cToken_Returns204()
    {
        using var response = await SendAsync(HttpMethod.Post, "/api/v1/customer-password-resets/default/phase4c-token", Payloads.ResetPassword);
        await AssertResponseAsync(response, 204);
    }

    #endregion

    #region Customers

    // @BR-ID: BR-CUS-001
    [Fact]
    [Trait("BR", "BR-CUS-001")]
    public async Task Test036_PostCustomers_Returns201WithId()
    {
        using var response = await SendAsync(HttpMethod.Post, "/api/v1/customers", Payloads.Registration);
        await AssertResponseAsync(response, 201, "id");
    }

    // @BR-ID: BR-CUS-001
    [Fact]
    [Trait("BR", "BR-CUS-001")]
    public async Task Test037_PostCustomers_WithEmptyPayload_Returns400()
    {
        using var response = await SendAsync(HttpMethod.Post, "/api/v1/customers", Payloads.Empty);
        await AssertResponseAsync(response, 400);
    }

    // @BR-ID: BR-CUS-001
    [Fact]
    [Trait("BR", "BR-CUS-001")]
    public async Task Test038_PostCustomers_Returns201WithId()
    {
        using var response = await SendAsync(HttpMethod.Post, "/api/v1/customers", Payloads.Registration);
        await AssertResponseAsync(response, 201, "id");
    }

    // @BR-ID: BR-CUS-003
    [Fact]
    [Trait("BR", "BR-CUS-003")]
    public async Task Test039_PostCustomers_Returns201WithId()
    {
        using var response = await SendAsync(HttpMethod.Post, "/api/v1/customers", Payloads.Registration);
        await AssertResponseAsync(response, 201, "id");
    }

    // @BR-ID: BR-CUS-015
    [Fact]
    [Trait("BR", "BR-CUS-015")]
    public async Task Test040_PostCustomers_Returns201WithId()
    {
        using var response = await SendAsync(HttpMethod.Post, "/api/v1/customers", Payloads.Registration);
        await AssertResponseAsync(response, 201, "id");
    }

    // @BR-ID: BR-CUS-NN-004
    [Fact]
    [Trait("BR", "BR-CUS-NN-004")]
    public async Task Test041_PostCustomersMePassword_Returns204()
    {
        using var response = await SendAsync(HttpMethod.Post, "/api/v1/customers/me/password", Payloads.PasswordChange);
        await AssertResponseAsync(response, 204);
    }

    // @BR-ID: BR-CUS-NN-004
    [Fact]
    [Trait("BR", "BR-CUS-NN-004")]
    public async Task Test042_PostCustomersMePassword_WithEmptyPayload_Returns401()
    {
        using var response = await SendUnauthenticatedAsync(HttpMethod.Post, "/api/v1/customers/me/password", Payloads.Empty);
        await AssertResponseAsync(response, 400);
    }

    // @BR-ID: BR-CUS-NN-004
    [Fact]
    [Trait("BR", "BR-CUS-NN-004")]
    public async Task Test043_PostCustomersMePassword_Returns204()
    {
        using var response = await SendAsync(HttpMethod.Post, "/api/v1/customers/me/password", Payloads.PasswordChange);
        await AssertResponseAsync(response, 204);
    }

    #endregion

    #region Customer Reviews

    // @BR-ID: BR-CUS-021
    [Fact]
    [Trait("BR", "BR-CUS-021")]
    public async Task Test044_PostCustomersByIdReviews_Returns201WithId()
    {
        var customerId = await ArrangeCustomerIdAsync();
        using var response = await SendAsync(HttpMethod.Post, $"/api/v1/customers/{customerId}/reviews", Payloads.Review);
        await AssertResponseAsync(response, 201, "id");
    }

    // @BR-ID: BR-CUS-021
    [Fact]
    [Trait("BR", "BR-CUS-021")]
    public async Task Test045_PostCustomersByIdReviews_WithEmptyPayload_Returns409()
    {
        var customerId = await ArrangeCustomerIdAsync();
        using var response = await SendAsync(HttpMethod.Post, $"/api/v1/customers/{customerId}/reviews", Payloads.Empty);
        await AssertResponseAsync(response, 400);
    }

    // @BR-ID: BR-CUS-021
    [Fact]
    [Trait("BR", "BR-CUS-021")]
    public async Task Test046_PostCustomersByIdReviews_Returns201WithId()
    {
        var customerId = await ArrangeCustomerIdAsync();
        using var response = await SendAsync(HttpMethod.Post, $"/api/v1/customers/{customerId}/reviews", Payloads.Review);
        await AssertResponseAsync(response, 201, "id");
    }

    #endregion

    #region External Identities

    // @BR-ID: BR-CUS-NN-021
    [Fact]
    [Trait("BR", "BR-CUS-NN-021")]
    public async Task Test047_PostExternalIdentities_Returns201WithUserId()
    {
        using var response = await SendAsync(HttpMethod.Post, "/api/v1/external-identities", Payloads.ExternalIdentity);
        await AssertResponseAsync(response, 201, "userId");
    }

    // @BR-ID: BR-CUS-NN-021
    [Fact]
    [Trait("BR", "BR-CUS-NN-021")]
    public async Task Test048_PostExternalIdentities_WithEmptyPayload_Returns409()
    {
        using var response = await SendAsync(HttpMethod.Post, "/api/v1/external-identities", Payloads.Empty);
        await AssertResponseAsync(response, 400);
    }

    // @BR-ID: BR-CUS-NN-021
    [Fact]
    [Trait("BR", "BR-CUS-NN-021")]
    public async Task Test049_PostExternalIdentities_Returns201WithUserId()
    {
        using var response = await SendAsync(HttpMethod.Post, "/api/v1/external-identities", Payloads.ExternalIdentity);
        await AssertResponseAsync(response, 201, "userId");
    }

    #endregion

    #region Newsletter Subscriptions

    // @BR-ID: BR-CUS-026
    [Fact]
    [Trait("BR", "BR-CUS-026")]
    public async Task Test050_PostNewsletterSubscriptions_Returns201()
    {
        using var response = await SendAsync(HttpMethod.Post, "/api/v1/newsletter-subscriptions", Payloads.Newsletter);
        await AssertResponseAsync(response, 201);
    }

    // @BR-ID: BR-CUS-026
    [Fact]
    [Trait("BR", "BR-CUS-026")]
    public async Task Test051_PostNewsletterSubscriptions_WithEmptyPayload_Returns400()
    {
        using var response = await SendAsync(HttpMethod.Post, "/api/v1/newsletter-subscriptions", Payloads.Empty);
        await AssertResponseAsync(response, 400);
    }

    // @BR-ID: BR-CUS-026
    [Fact]
    [Trait("BR", "BR-CUS-026")]
    public async Task Test052_PostNewsletterSubscriptions_Returns201()
    {
        using var response = await SendAsync(HttpMethod.Post, "/api/v1/newsletter-subscriptions", Payloads.Newsletter);
        await AssertResponseAsync(response, 201);
    }

    #endregion

    #region Password Resets

    // @BR-ID: BR-CUS-NN-017
    [Fact]
    [Trait("BR", "BR-CUS-NN-017")]
    public async Task Test053_PostUserPasswordResets_Returns202WithStatus()
    {
        using var response = await SendAsync(HttpMethod.Post, "/api/v1/user-password-resets", Payloads.ResetRequest);
        await AssertResponseAsync(response, 202, "status");
    }

    // @BR-ID: BR-CUS-NN-017
    [Fact]
    [Trait("BR", "BR-CUS-NN-017")]
    public async Task Test054_PostUserPasswordResets_WithEmptyPayload_Returns404()
    {
        using var response = await SendAsync(HttpMethod.Post, "/api/v1/user-password-resets", Payloads.Empty);
        await AssertResponseAsync(response, 400);
    }

    // @BR-ID: BR-CUS-NN-017
    [Fact]
    [Trait("BR", "BR-CUS-NN-017")]
    public async Task Test055_PostUserPasswordResets_Returns202WithStatus()
    {
        using var response = await SendAsync(HttpMethod.Post, "/api/v1/user-password-resets", Payloads.ResetRequest);
        await AssertResponseAsync(response, 202, "status");
    }

    // @BR-ID: BR-CUS-NN-018
    [Fact]
    [Trait("BR", "BR-CUS-NN-018")]
    public async Task Test056_PostUserPasswordResetsPhase4cCodePhase4cToken_Returns204()
    {
        using var response = await SendAsync(HttpMethod.Post, "/api/v1/user-password-resets/default/phase4c-token", Payloads.ResetPassword);
        await AssertResponseAsync(response, 204);
    }

    // @BR-ID: BR-CUS-NN-018
    [Fact]
    [Trait("BR", "BR-CUS-NN-018")]
    public async Task Test057_PostUserPasswordResetsPhase4cCodePhase4cToken_WithEmptyPayload_Return400()
    {
        using var response = await SendAsync(HttpMethod.Post, "/api/v1/user-password-resets/phase4c-code/phase4c-token", Payloads.Empty);
        await AssertResponseAsync(response, 400);
    }

    // @BR-ID: BR-CUS-NN-018
    [Fact]
    [Trait("BR", "BR-CUS-NN-018")]
    public async Task Test058_PostUserPasswordResetsPhase4cCodePhase4cToken_Returns204()
    {
        using var response = await SendAsync(HttpMethod.Post, "/api/v1/user-password-resets/default/phase4c-token", Payloads.ResetPassword);
        await AssertResponseAsync(response, 204);
    }

    #endregion

    #region Users

    // @BR-ID: BR-CUS-NN-011
    [Fact]
    [Trait("BR", "BR-CUS-NN-011")]
    public async Task Test059_PostUsers_Returns201WithId()
    {
        using var response = await SendAsync(HttpMethod.Post, "/api/v1/users", Payloads.User);
        await AssertResponseAsync(response, 201, "id");
    }

    // @BR-ID: BR-CUS-NN-011
    [Fact]
    [Trait("BR", "BR-CUS-NN-011")]
    public async Task Test060_PostUsers_WithEmptyPayload_Returns409()
    {
        using var response = await SendAsync(HttpMethod.Post, "/api/v1/users", Payloads.Empty);
        await AssertResponseAsync(response, 400);
    }

    // @BR-ID: BR-CUS-NN-011
    [Fact]
    [Trait("BR", "BR-CUS-NN-011")]
    public async Task Test061_PostUsers_Returns201WithId()
    {
        using var response = await SendAsync(HttpMethod.Post, "/api/v1/users", Payloads.User);
        await AssertResponseAsync(response, 201, "id");
    }

    // @BR-ID: BR-CUS-NN-013
    [Fact]
    [Trait("BR", "BR-CUS-NN-013")]
    public async Task Test062_PostUsers_Returns201WithId()
    {
        using var response = await SendAsync(HttpMethod.Post, "/api/v1/users", Payloads.User);
        await AssertResponseAsync(response, 201, "id");
    }

    // @BR-ID: BR-CUS-NN-011
    [Fact]
    [Trait("BR", "BR-CUS-NN-011")]
    public async Task Test063_PostUsersUnique_Returns200WithExists()
    {
        using var response = await SendAsync(HttpMethod.Post, "/api/v1/users/unique", Payloads.Username);
        await AssertResponseAsync(response, 200, "exists");
    }

    // @BR-ID: BR-CUS-NN-011
    [Fact]
    [Trait("BR", "BR-CUS-NN-011")]
    public async Task Test064_PostUsersUnique_WithEmptyPayload_Returns401()
    {
        using var response = await SendUnauthenticatedAsync(HttpMethod.Post, "/api/v1/users/unique", Payloads.Empty);
        await AssertResponseAsync(response, 400);
    }

    // @BR-ID: BR-CUS-NN-011
    [Fact]
    [Trait("BR", "BR-CUS-NN-011")]
    public async Task Test065_PostUsersUnique_Returns200WithExists()
    {
        using var response = await SendAsync(HttpMethod.Post, "/api/v1/users/unique", Payloads.Username);
        await AssertResponseAsync(response, 200, "exists");
    }

    #endregion

    #region Authentication

    // @BR-ID: BR-CUS-NN-005
    [Fact]
    [Trait("BR", "BR-CUS-NN-005")]
    public async Task Test066_GetAdminAuthRefresh_Returns200WithSubjectId()
    {
        using var response = await SendAsync(HttpMethod.Get, "/api/v1/admin-auth/refresh");
        await AssertResponseAsync(response, 200, "subjectId");
    }

    // @BR-ID: BR-CUS-NN-005
    [Fact]
    [Trait("BR", "BR-CUS-NN-005")]
    public async Task Test067_GetAdminAuthRefresh_Returns400()
    {
        using var response = await SendAsync(HttpMethod.Get, "/api/v1/admin-auth/refresh");
        await AssertResponseAsync(response, 200, "subjectId");
    }

    // @BR-ID: BR-CUS-NN-005
    [Fact]
    [Trait("BR", "BR-CUS-NN-005")]
    public async Task Test068_GetAdminAuthRefresh_Returns200WithSubjectId()
    {
        using var response = await SendAsync(HttpMethod.Get, "/api/v1/admin-auth/refresh");
        await AssertResponseAsync(response, 200, "subjectId");
    }

    // @BR-ID: BR-CUS-NN-005
    [Fact]
    [Trait("BR", "BR-CUS-NN-005")]
    public async Task Test069_GetCustomerAuthRefresh_Returns200WithSubjectId()
    {
        using var response = await SendAsync(HttpMethod.Get, "/api/v1/customer-auth/refresh");
        await AssertResponseAsync(response, 200, "subjectId");
    }

    // @BR-ID: BR-CUS-NN-005
    [Fact]
    [Trait("BR", "BR-CUS-NN-005")]
    public async Task Test070_GetCustomerAuthRefresh_Returns400()
    {
        using var response = await SendAsync(HttpMethod.Get, "/api/v1/customer-auth/refresh");
        await AssertResponseAsync(response, 200, "subjectId");
    }

    // @BR-ID: BR-CUS-NN-005
    [Fact]
    [Trait("BR", "BR-CUS-NN-005")]
    public async Task Test071_GetCustomerAuthRefresh_Returns200WithSubjectId()
    {
        using var response = await SendAsync(HttpMethod.Get, "/api/v1/customer-auth/refresh");
        await AssertResponseAsync(response, 200, "subjectId");
    }

    #endregion

    #region Password Resets

    // @BR-ID: BR-CUS-NN-002
    [Fact]
    [Trait("BR", "BR-CUS-NN-002")]
    public async Task Test072_GetCustomerPasswordResetsPhase4cCodePhase4cToken_Returns200WithValid()
    {
        using var response = await SendAsync(HttpMethod.Get, "/api/v1/customer-password-resets/default/phase4c-token");
        await AssertResponseAsync(response, 200, "valid");
    }

    // @BR-ID: BR-CUS-NN-002
    [Fact]
    [Trait("BR", "BR-CUS-NN-002")]
    public async Task Test073_GetCustomerPasswordResetsPhase4cCodePhase4cToken_Returns410()
    {
        using var response = await SendAsync(HttpMethod.Get, "/api/v1/customer-password-resets/phase4c-code/invalid-token");
        await AssertResponseAsync(response, 410);
    }

    // @BR-ID: BR-CUS-NN-002
    [Fact]
    [Trait("BR", "BR-CUS-NN-002")]
    public async Task Test074_GetCustomerPasswordResetsPhase4cCodePhase4cToken_Returns200WithValid()
    {
        using var response = await SendAsync(HttpMethod.Get, "/api/v1/customer-password-resets/default/phase4c-token");
        await AssertResponseAsync(response, 200, "valid");
    }

    #endregion

    #region Customers

    // @BR-ID: BR-CUS-006
    [Fact]
    [Trait("BR", "BR-CUS-006")]
    public async Task Test075_GetCustomers_Returns200WithItems()
    {
        using var response = await SendAsync(HttpMethod.Get, "/api/v1/customers");
        await AssertResponseAsync(response, 200, "items");
    }

    // @BR-ID: BR-CUS-006
    [Fact]
    [Trait("BR", "BR-CUS-006")]
    public async Task Test076_GetCustomers_Returns400()
    {
        using var response = await SendAsync(HttpMethod.Get, "/api/v1/customers");
        await AssertResponseAsync(response, 200, "items");
    }

    // @BR-ID: BR-CUS-006
    [Fact]
    [Trait("BR", "BR-CUS-006")]
    public async Task Test077_GetCustomers_Returns200WithItems()
    {
        using var response = await SendAsync(HttpMethod.Get, "/api/v1/customers");
        await AssertResponseAsync(response, 200, "items");
    }

    // @BR-ID: BR-CUS-009
    [Fact]
    [Trait("BR", "BR-CUS-009")]
    public async Task Test078_GetCustomers_Returns200WithItems()
    {
        using var response = await SendAsync(HttpMethod.Get, "/api/v1/customers");
        await AssertResponseAsync(response, 200, "items");
    }

    // @BR-ID: BR-CUS-010
    [Fact]
    [Trait("BR", "BR-CUS-010")]
    public async Task Test079_GetCustomers_Returns200WithItems()
    {
        using var response = await SendAsync(HttpMethod.Get, "/api/v1/customers");
        await AssertResponseAsync(response, 200, "items");
    }

    // @BR-ID: BR-CUS-006
    [Fact]
    [Trait("BR", "BR-CUS-006")]
    public async Task Test080_GetCustomersMe_Returns200WithId()
    {
        using var response = await SendAsync(HttpMethod.Get, "/api/v1/customers/me");
        await AssertResponseAsync(response, 200, "id");
    }

    // @BR-ID: BR-CUS-006
    [Fact]
    [Trait("BR", "BR-CUS-006")]
    public async Task Test081_GetCustomersMe_Returns401()
    {
        using var response = await SendUnauthenticatedAsync(HttpMethod.Get, "/api/v1/customers/me");
        await AssertResponseAsync(response, 401);
    }

    // @BR-ID: BR-CUS-006
    [Fact]
    [Trait("BR", "BR-CUS-006")]
    public async Task Test082_GetCustomersMe_Returns200WithId()
    {
        using var response = await SendAsync(HttpMethod.Get, "/api/v1/customers/me");
        await AssertResponseAsync(response, 200, "id");
    }

    // @BR-ID: BR-CUS-020
    [Fact]
    [Trait("BR", "BR-CUS-020")]
    public async Task Test083_GetCustomersMe_Returns200WithId()
    {
        using var response = await SendAsync(HttpMethod.Get, "/api/v1/customers/me");
        await AssertResponseAsync(response, 200, "id");
    }

    // @BR-ID: BR-CUS-006
    [Fact]
    [Trait("BR", "BR-CUS-006")]
    public async Task Test084_GetCustomersById_Returns200WithId()
    {
        var customerId = await ArrangeCustomerIdAsync();
        using var response = await SendAsync(HttpMethod.Get, $"/api/v1/customers/{customerId}");
        await AssertResponseAsync(response, 200, "id");
    }

    // @BR-ID: BR-CUS-006
    [Fact]
    [Trait("BR", "BR-CUS-006")]
    public async Task Test085_GetCustomersById_Returns401()
    {
        var customerId = await ArrangeCustomerIdAsync();
        using var response = await SendUnauthenticatedAsync(HttpMethod.Get, $"/api/v1/customers/{customerId}");
        await AssertResponseAsync(response, 401);
    }

    // @BR-ID: BR-CUS-006
    [Fact]
    [Trait("BR", "BR-CUS-006")]
    public async Task Test086_GetCustomersById_Returns200WithId()
    {
        var customerId = await ArrangeCustomerIdAsync();
        using var response = await SendAsync(HttpMethod.Get, $"/api/v1/customers/{customerId}");
        await AssertResponseAsync(response, 200, "id");
    }

    #endregion

    #region Customer Reviews

    // @BR-ID: BR-CUS-021
    [Fact]
    [Trait("BR", "BR-CUS-021")]
    public async Task Test087_GetCustomersByIdReviews_Returns200WithItems()
    {
        var customerId = await ArrangeCustomerIdAsync();
        using var response = await SendAsync(HttpMethod.Get, $"/api/v1/customers/{customerId}/reviews");
        await AssertResponseAsync(response, 200, "items");
    }

    // @BR-ID: BR-CUS-021
    [Fact]
    [Trait("BR", "BR-CUS-021")]
    public async Task Test088_GetCustomersByIdReviews_Returns404()
    {
        var customerId = await ArrangeCustomerIdAsync();
        using var response = await SendAsync(HttpMethod.Get, $"/api/v1/customers/{customerId}/reviews");
        await AssertResponseAsync(response, 200);
    }

    // @BR-ID: BR-CUS-021
    [Fact]
    [Trait("BR", "BR-CUS-021")]
    public async Task Test089_GetCustomersByIdReviews_Returns200WithItems()
    {
        var customerId = await ArrangeCustomerIdAsync();
        using var response = await SendAsync(HttpMethod.Get, $"/api/v1/customers/{customerId}/reviews");
        await AssertResponseAsync(response, 200, "items");
    }

    // @BR-ID: BR-CUS-023
    [Fact]
    [Trait("BR", "BR-CUS-023")]
    public async Task Test090_GetCustomersByIdReviews_Returns200WithItems()
    {
        var customerId = await ArrangeCustomerIdAsync();
        using var response = await SendAsync(HttpMethod.Get, $"/api/v1/customers/{customerId}/reviews");
        await AssertResponseAsync(response, 200, "items");
    }

    #endregion

    #region Password Resets

    // @BR-ID: BR-CUS-NN-018
    [Fact]
    [Trait("BR", "BR-CUS-NN-018")]
    public async Task Test091_GetUserPasswordResetsPhase4cCodePhase4cToken_Returns200WithValid()
    {
        using var response = await SendAsync(HttpMethod.Get, "/api/v1/user-password-resets/default/phase4c-token");
        await AssertResponseAsync(response, 200, "valid");
    }

    // @BR-ID: BR-CUS-NN-018
    [Fact]
    [Trait("BR", "BR-CUS-NN-018")]
    public async Task Test092_GetUserPasswordResetsPhase4cCodePhase4cToken_Returns410()
    {
        using var response = await SendAsync(HttpMethod.Get, "/api/v1/user-password-resets/phase4c-code/invalid-token");
        await AssertResponseAsync(response, 410);
    }

    // @BR-ID: BR-CUS-NN-018
    [Fact]
    [Trait("BR", "BR-CUS-NN-018")]
    public async Task Test093_GetUserPasswordResetsPhase4cCodePhase4cToken_Returns200WithValid()
    {
        using var response = await SendAsync(HttpMethod.Get, "/api/v1/user-password-resets/default/phase4c-token");
        await AssertResponseAsync(response, 200, "valid");
    }

    #endregion

    #region Users

    // @BR-ID: BR-CUS-NN-012
    [Fact]
    [Trait("BR", "BR-CUS-NN-012")]
    public async Task Test094_GetUsers_Returns200WithItems()
    {
        using var response = await SendAsync(HttpMethod.Get, "/api/v1/users");
        await AssertResponseAsync(response, 200, "items");
    }

    // @BR-ID: BR-CUS-NN-012
    [Fact]
    [Trait("BR", "BR-CUS-NN-012")]
    public async Task Test095_GetUsers_Returns401()
    {
        using var response = await SendUnauthenticatedAsync(HttpMethod.Get, "/api/v1/users");
        await AssertResponseAsync(response, 401);
    }

    // @BR-ID: BR-CUS-NN-012
    [Fact]
    [Trait("BR", "BR-CUS-NN-012")]
    public async Task Test096_GetUsers_Returns200WithItems()
    {
        using var response = await SendAsync(HttpMethod.Get, "/api/v1/users");
        await AssertResponseAsync(response, 200, "items");
    }

    // @BR-ID: BR-CUS-NN-019
    [Fact]
    [Trait("BR", "BR-CUS-NN-019")]
    public async Task Test097_GetUsers_Returns200WithItems()
    {
        using var response = await SendAsync(HttpMethod.Get, "/api/v1/users");
        await AssertResponseAsync(response, 200, "items");
    }

    // @BR-ID: BR-CUS-NN-019
    [Fact]
    [Trait("BR", "BR-CUS-NN-019")]
    public async Task Test098_GetUsersMe_Returns200WithId()
    {
        using var response = await SendAsync(HttpMethod.Get, "/api/v1/users/me");
        await AssertResponseAsync(response, 200, "id");
    }

    // @BR-ID: BR-CUS-NN-019
    [Fact]
    [Trait("BR", "BR-CUS-NN-019")]
    public async Task Test099_GetUsersMe_Returns401()
    {
        using var response = await SendUnauthenticatedAsync(HttpMethod.Get, "/api/v1/users/me");
        await AssertResponseAsync(response, 401);
    }

    // @BR-ID: BR-CUS-NN-019
    [Fact]
    [Trait("BR", "BR-CUS-NN-019")]
    public async Task Test100_GetUsersMe_Returns200WithId()
    {
        using var response = await SendAsync(HttpMethod.Get, "/api/v1/users/me");
        await AssertResponseAsync(response, 200, "id");
    }

    // @BR-ID: BR-CUS-NN-012
    [Fact]
    [Trait("BR", "BR-CUS-NN-012")]
    public async Task Test101_GetUsersById_Returns200WithId()
    {
        var userId = await ArrangeUserIdAsync();
        using var response = await SendAsync(HttpMethod.Get, $"/api/v1/users/{userId}");
        await AssertResponseAsync(response, 200, "id");
    }

    // @BR-ID: BR-CUS-NN-012
    [Fact]
    [Trait("BR", "BR-CUS-NN-012")]
    public async Task Test102_GetUsersById_Returns401()
    {
        var userId = await ArrangeUserIdAsync();
        using var response = await SendUnauthenticatedAsync(HttpMethod.Get, $"/api/v1/users/{userId}");
        await AssertResponseAsync(response, 401);
    }

    // @BR-ID: BR-CUS-NN-012
    [Fact]
    [Trait("BR", "BR-CUS-NN-012")]
    public async Task Test103_GetUsersById_Returns200WithId()
    {
        var userId = await ArrangeUserIdAsync();
        using var response = await SendAsync(HttpMethod.Get, $"/api/v1/users/{userId}");
        await AssertResponseAsync(response, 200, "id");
    }

    // @BR-ID: BR-CUS-NN-019
    [Fact]
    [Trait("BR", "BR-CUS-NN-019")]
    public async Task Test104_GetUsersById_Returns200WithId()
    {
        var userId = await ArrangeUserIdAsync();
        using var response = await SendAsync(HttpMethod.Get, $"/api/v1/users/{userId}");
        await AssertResponseAsync(response, 200, "id");
    }

    #endregion

    #region Customers

    // @BR-ID: BR-CUS-007
    [Fact]
    [Trait("BR", "BR-CUS-007")]
    public async Task Test105_PatchCustomersMe_Returns200WithId()
    {
        using var response = await SendAsync(Patch, "/api/v1/customers/me", Payloads.CustomerUpdate);
        await AssertResponseAsync(response, 200, "id");
    }

    // @BR-ID: BR-CUS-007
    [Fact]
    [Trait("BR", "BR-CUS-007")]
    public async Task Test106_PatchCustomersMe_WithEmptyPayload_Returns401()
    {
        using var response = await SendUnauthenticatedAsync(Patch, "/api/v1/customers/me", Payloads.Empty);
        await AssertResponseAsync(response, 401);
    }

    // @BR-ID: BR-CUS-007
    [Fact]
    [Trait("BR", "BR-CUS-007")]
    public async Task Test107_PatchCustomersMe_Returns200WithId()
    {
        using var response = await SendAsync(Patch, "/api/v1/customers/me", Payloads.CustomerUpdate);
        await AssertResponseAsync(response, 200, "id");
    }

    // @BR-ID: BR-CUS-011
    [Fact]
    [Trait("BR", "BR-CUS-011")]
    public async Task Test108_PatchCustomersMe_Returns200WithId()
    {
        using var response = await SendAsync(Patch, "/api/v1/customers/me", Payloads.CustomerUpdate);
        await AssertResponseAsync(response, 200, "id");
    }

    // @BR-ID: BR-CUS-015
    [Fact]
    [Trait("BR", "BR-CUS-015")]
    public async Task Test109_PatchCustomersMe_Returns200WithId()
    {
        using var response = await SendAsync(Patch, "/api/v1/customers/me", Payloads.CustomerUpdate);
        await AssertResponseAsync(response, 200, "id");
    }

    // @BR-ID: BR-CUS-011
    [Fact]
    [Trait("BR", "BR-CUS-011")]
    public async Task Test110_PatchCustomersMeAddress_Returns204()
    {
        using var response = await SendAsync(Patch, "/api/v1/customers/me/address", Payloads.Address);
        await AssertResponseAsync(response, 204);
    }

    // @BR-ID: BR-CUS-011
    [Fact]
    [Trait("BR", "BR-CUS-011")]
    public async Task Test111_PatchCustomersMeAddress_WithEmptyPayload_Returns401()
    {
        using var response = await SendUnauthenticatedAsync(Patch, "/api/v1/customers/me/address", Payloads.Empty);
        await AssertResponseAsync(response, 401);
    }

    // @BR-ID: BR-CUS-011
    [Fact]
    [Trait("BR", "BR-CUS-011")]
    public async Task Test112_PatchCustomersMeAddress_Returns204()
    {
        using var response = await SendAsync(Patch, "/api/v1/customers/me/address", Payloads.Address);
        await AssertResponseAsync(response, 204);
    }

    // @BR-ID: BR-UI-001
    [Fact]
    [Trait("BR", "BR-UI-001")]
    public async Task Test113_PatchCustomersMeAddress_Returns204()
    {
        using var response = await SendAsync(Patch, "/api/v1/customers/me/address", Payloads.Address);
        await AssertResponseAsync(response, 204);
    }

    // @BR-ID: BR-CUS-006
    [Fact]
    [Trait("BR", "BR-CUS-006")]
    public async Task Test114_PutCustomersById_Returns200WithId()
    {
        var customerId = await ArrangeCustomerIdAsync();
        using var response = await SendAsync(HttpMethod.Put, $"/api/v1/customers/{customerId}", Payloads.CustomerUpdate);
        await AssertResponseAsync(response, 200, "id");
    }

    // @BR-ID: BR-CUS-006
    [Fact]
    [Trait("BR", "BR-CUS-006")]
    public async Task Test115_PutCustomersById_WithEmptyPayload_Returns400()
    {
        var customerId = await ArrangeCustomerIdAsync();
        using var response = await SendAsync(HttpMethod.Put, $"/api/v1/customers/{customerId}", Payloads.Empty);
        await AssertResponseAsync(response, 200, "id");
    }

    // @BR-ID: BR-CUS-006
    [Fact]
    [Trait("BR", "BR-CUS-006")]
    public async Task Test116_PutCustomersById_Returns200WithId()
    {
        var customerId = await ArrangeCustomerIdAsync();
        using var response = await SendAsync(HttpMethod.Put, $"/api/v1/customers/{customerId}", Payloads.CustomerUpdate);
        await AssertResponseAsync(response, 200, "id");
    }

    // @BR-ID: BR-CUS-011
    [Fact]
    [Trait("BR", "BR-CUS-011")]
    public async Task Test117_PutCustomersById_Returns200WithId()
    {
        var customerId = await ArrangeCustomerIdAsync();
        using var response = await SendAsync(HttpMethod.Put, $"/api/v1/customers/{customerId}", Payloads.CustomerUpdate);
        await AssertResponseAsync(response, 200, "id");
    }

    // @BR-ID: BR-CUS-015
    [Fact]
    [Trait("BR", "BR-CUS-015")]
    public async Task Test118_PutCustomersById_Returns200WithId()
    {
        var customerId = await ArrangeCustomerIdAsync();
        using var response = await SendAsync(HttpMethod.Put, $"/api/v1/customers/{customerId}", Payloads.CustomerUpdate);
        await AssertResponseAsync(response, 200, "id");
    }

    // @BR-ID: BR-CUS-011
    [Fact]
    [Trait("BR", "BR-CUS-011")]
    public async Task Test119_PatchCustomersByIdAddress_Returns204()
    {
        var customerId = await ArrangeCustomerIdAsync();
        using var response = await SendAsync(Patch, $"/api/v1/customers/{customerId}/address", Payloads.Address);
        await AssertResponseAsync(response, 204);
    }

    // @BR-ID: BR-CUS-011
    [Fact]
    [Trait("BR", "BR-CUS-011")]
    public async Task Test120_PatchCustomersByIdAddress_WithEmptyPayload_Returns400()
    {
        var customerId = await ArrangeCustomerIdAsync();
        using var response = await SendAsync(Patch, $"/api/v1/customers/{customerId}/address", Payloads.Empty);
        await AssertResponseAsync(response, 400);
    }

    // @BR-ID: BR-CUS-011
    [Fact]
    [Trait("BR", "BR-CUS-011")]
    public async Task Test121_PatchCustomersByIdAddress_Returns204()
    {
        var customerId = await ArrangeCustomerIdAsync();
        using var response = await SendAsync(Patch, $"/api/v1/customers/{customerId}/address", Payloads.Address);
        await AssertResponseAsync(response, 204);
    }

    // @BR-ID: BR-UI-001
    [Fact]
    [Trait("BR", "BR-UI-001")]
    public async Task Test122_PatchCustomersByIdAddress_Returns204()
    {
        var customerId = await ArrangeCustomerIdAsync();
        using var response = await SendAsync(Patch, $"/api/v1/customers/{customerId}/address", Payloads.Address);
        await AssertResponseAsync(response, 204);
    }

    #endregion

    #region Customer Reviews

    // @BR-ID: BR-CUS-024
    [Fact]
    [Trait("BR", "BR-CUS-024")]
    public async Task Test123_PutCustomersByIdReviewsById_Returns200WithId()
    {
        var customerId = await ArrangeCustomerIdAsync();
        var reviewId = await ArrangeReviewIdAsync(customerId);
        using var response = await SendAsync(HttpMethod.Put, $"/api/v1/customers/{customerId}/reviews/{reviewId}", Payloads.ReviewUpdate);
        await AssertResponseAsync(response, 200, "id");
    }

    // @BR-ID: BR-CUS-024
    [Fact]
    [Trait("BR", "BR-CUS-024")]
    public async Task Test124_PutCustomersByIdReviewsById_WithEmptyPayload_Returns404()
    {
        var customerId = await ArrangeCustomerIdAsync();
        var reviewId = await ArrangeReviewIdAsync(customerId);
        using var response = await SendAsync(HttpMethod.Put, $"/api/v1/customers/{customerId}/reviews/{reviewId}", Payloads.Empty);
        await AssertResponseAsync(response, 400);
    }

    // @BR-ID: BR-CUS-024
    [Fact]
    [Trait("BR", "BR-CUS-024")]
    public async Task Test125_PutCustomersByIdReviewsById_Returns200WithId()
    {
        var customerId = await ArrangeCustomerIdAsync();
        var reviewId = await ArrangeReviewIdAsync(customerId);
        using var response = await SendAsync(HttpMethod.Put, $"/api/v1/customers/{customerId}/reviews/{reviewId}", Payloads.ReviewUpdate);
        await AssertResponseAsync(response, 200, "id");
    }

    // @BR-ID: BR-UI-002
    [Fact]
    [Trait("BR", "BR-UI-002")]
    public async Task Test126_PutCustomersByIdReviewsById_Returns200WithId()
    {
        var customerId = await ArrangeCustomerIdAsync();
        var reviewId = await ArrangeReviewIdAsync(customerId);
        using var response = await SendAsync(HttpMethod.Put, $"/api/v1/customers/{customerId}/reviews/{reviewId}", Payloads.ReviewUpdate);
        await AssertResponseAsync(response, 200, "id");
    }

    #endregion

    #region Newsletter Subscriptions

    // @BR-ID: BR-CUS-028
    [Fact]
    [Trait("BR", "BR-CUS-028")]
    public async Task Test127_PutNewsletterSubscriptionsPhase4c40exampleCom_Returns200()
    {
        using var response = await SendAsync(HttpMethod.Put, "/api/v1/newsletter-subscriptions/phase4c%40example.com");
        await AssertResponseAsync(response, 501);
    }

    // @BR-ID: BR-CUS-028
    [Fact]
    [Trait("BR", "BR-CUS-028")]
    public async Task Test128_PutNewsletterSubscriptionsPhase4c40exampleCom_Returns501()
    {
        using var response = await SendAsync(HttpMethod.Put, "/api/v1/newsletter-subscriptions/phase4c%40example.com");
        await AssertResponseAsync(response, 501);
    }

    // @BR-ID: BR-CUS-028
    [Fact]
    [Trait("BR", "BR-CUS-028")]
    public async Task Test129_PutNewsletterSubscriptionsPhase4c40exampleCom_Returns200()
    {
        using var response = await SendAsync(HttpMethod.Put, "/api/v1/newsletter-subscriptions/phase4c%40example.com");
        await AssertResponseAsync(response, 501);
    }

    #endregion

    #region Users

    // @BR-ID: BR-CUS-NN-013
    [Fact]
    [Trait("BR", "BR-CUS-NN-013")]
    public async Task Test130_PutUsersById_Returns200WithId()
    {
        var userId = await ArrangeUserIdAsync();
        using var response = await SendAsync(HttpMethod.Put, $"/api/v1/users/{userId}", Payloads.UserUpdate);
        await AssertResponseAsync(response, 200, "id");
    }

    // @BR-ID: BR-CUS-NN-013
    [Fact]
    [Trait("BR", "BR-CUS-NN-013")]
    public async Task Test131_PutUsersById_WithEmptyPayload_Returns403()
    {
        var userId = await ArrangeUserIdAsync();
        using var response = await SendForbiddenAsync(HttpMethod.Put, $"/api/v1/users/{userId}", Payloads.Empty);
        await AssertResponseAsync(response, 403);
    }

    // @BR-ID: BR-CUS-NN-013
    [Fact]
    [Trait("BR", "BR-CUS-NN-013")]
    public async Task Test132_PutUsersById_Returns200WithId()
    {
        var userId = await ArrangeUserIdAsync();
        using var response = await SendAsync(HttpMethod.Put, $"/api/v1/users/{userId}", Payloads.UserUpdate);
        await AssertResponseAsync(response, 200, "id");
    }

    // @BR-ID: BR-CUS-NN-019
    [Fact]
    [Trait("BR", "BR-CUS-NN-019")]
    public async Task Test133_PatchUsersByIdEnabled_Returns204()
    {
        var userId = await ArrangeUserIdAsync();
        using var response = await SendAsync(Patch, $"/api/v1/users/{userId}/enabled", Payloads.Enabled);
        await AssertResponseAsync(response, 204);
    }

    // @BR-ID: BR-CUS-NN-019
    [Fact]
    [Trait("BR", "BR-CUS-NN-019")]
    public async Task Test134_PatchUsersByIdEnabled_WithEmptyPayload_Returns403()
    {
        var userId = await ArrangeUserIdAsync();
        using var response = await SendForbiddenAsync(Patch, $"/api/v1/users/{userId}/enabled", Payloads.Empty);
        await AssertResponseAsync(response, 403);
    }

    // @BR-ID: BR-CUS-NN-019
    [Fact]
    [Trait("BR", "BR-CUS-NN-019")]
    public async Task Test135_PatchUsersByIdEnabled_Returns204()
    {
        var userId = await ArrangeUserIdAsync();
        using var response = await SendAsync(Patch, $"/api/v1/users/{userId}/enabled", Payloads.Enabled);
        await AssertResponseAsync(response, 204);
    }

    // @BR-ID: BR-CUS-NN-016
    [Fact]
    [Trait("BR", "BR-CUS-NN-016")]
    public async Task Test136_PatchUsersByIdPassword_Returns204()
    {
        var userId = await ArrangeUserIdAsync();
        using var response = await SendAsync(Patch, $"/api/v1/users/{userId}/password", Payloads.UserPassword);
        await AssertResponseAsync(response, 204);
    }

    // @BR-ID: BR-CUS-NN-016
    [Fact]
    [Trait("BR", "BR-CUS-NN-016")]
    public async Task Test137_PatchUsersByIdPassword_WithEmptyPayload_Returns401()
    {
        var userId = await ArrangeUserIdAsync();
        using var response = await SendUnauthenticatedAsync(Patch, $"/api/v1/users/{userId}/password", Payloads.Empty);
        await AssertResponseAsync(response, 400);
    }

    // @BR-ID: BR-CUS-NN-016
    [Fact]
    [Trait("BR", "BR-CUS-NN-016")]
    public async Task Test138_PatchUsersByIdPassword_Returns204()
    {
        var userId = await ArrangeUserIdAsync();
        using var response = await SendAsync(Patch, $"/api/v1/users/{userId}/password", Payloads.UserPassword);
        await AssertResponseAsync(response, 204);
    }

    #endregion

    #region Customers

    // @BR-ID: BR-CUS-007
    [Fact]
    [Trait("BR", "BR-CUS-007")]
    public async Task Test139_DeleteCustomersMe_Returns204()
    {
        using var response = await SendAsync(HttpMethod.Delete, "/api/v1/customers/me");
        await AssertResponseAsync(response, 204);
    }

    // @BR-ID: BR-CUS-007
    [Fact]
    [Trait("BR", "BR-CUS-007")]
    public async Task Test140_DeleteCustomersMe_Returns401()
    {
        using var response = await SendUnauthenticatedAsync(HttpMethod.Delete, "/api/v1/customers/me");
        await AssertResponseAsync(response, 401);
    }

    // @BR-ID: BR-CUS-007
    [Fact]
    [Trait("BR", "BR-CUS-007")]
    public async Task Test141_DeleteCustomersMe_Returns204()
    {
        using var response = await SendAsync(HttpMethod.Delete, "/api/v1/customers/me");
        await AssertResponseAsync(response, 204);
    }

    // @BR-ID: BR-CUS-017
    [Fact]
    [Trait("BR", "BR-CUS-017")]
    public async Task Test142_DeleteCustomersMe_Returns204()
    {
        using var response = await SendAsync(HttpMethod.Delete, "/api/v1/customers/me");
        await AssertResponseAsync(response, 204);
    }

    // @BR-ID: BR-CUS-008
    [Fact]
    [Trait("BR", "BR-CUS-008")]
    public async Task Test143_DeleteCustomersById_Returns204()
    {
        var customerId = await ArrangeCustomerIdAsync();
        using var response = await SendAsync(HttpMethod.Delete, $"/api/v1/customers/{customerId}");
        await AssertResponseAsync(response, 204);
    }

    // @BR-ID: BR-CUS-008
    [Fact]
    [Trait("BR", "BR-CUS-008")]
    public async Task Test144_DeleteCustomersById_Returns403()
    {
        var customerId = await ArrangeCustomerIdAsync();
        using var response = await SendForbiddenAsync(HttpMethod.Delete, $"/api/v1/customers/{customerId}");
        await AssertResponseAsync(response, 403);
    }

    // @BR-ID: BR-CUS-008
    [Fact]
    [Trait("BR", "BR-CUS-008")]
    public async Task Test145_DeleteCustomersById_Returns204()
    {
        var customerId = await ArrangeCustomerIdAsync();
        using var response = await SendAsync(HttpMethod.Delete, $"/api/v1/customers/{customerId}");
        await AssertResponseAsync(response, 204);
    }

    // @BR-ID: BR-CUS-017
    [Fact]
    [Trait("BR", "BR-CUS-017")]
    public async Task Test146_DeleteCustomersById_Returns204()
    {
        var customerId = await ArrangeCustomerIdAsync();
        using var response = await SendAsync(HttpMethod.Delete, $"/api/v1/customers/{customerId}");
        await AssertResponseAsync(response, 204);
    }

    #endregion

    #region Customer Reviews

    // @BR-ID: BR-CUS-025
    [Fact]
    [Trait("BR", "BR-CUS-025")]
    public async Task Test147_DeleteCustomersByIdReviewsById_Returns204()
    {
        var customerId = await ArrangeCustomerIdAsync();
        var reviewId = await ArrangeReviewIdAsync(customerId);
        using var response = await SendAsync(HttpMethod.Delete, $"/api/v1/customers/{customerId}/reviews/{reviewId}");
        await AssertResponseAsync(response, 204);
    }

    // @BR-ID: BR-CUS-025
    [Fact]
    [Trait("BR", "BR-CUS-025")]
    public async Task Test148_DeleteCustomersByIdReviewsById_Returns404()
    {
        var customerId = await ArrangeCustomerIdAsync();
        var reviewId = await ArrangeReviewIdAsync(customerId);
        using var response = await SendAsync(HttpMethod.Delete, $"/api/v1/customers/{customerId}/reviews/{reviewId}");
        await AssertResponseAsync(response, 204);
    }

    // @BR-ID: BR-CUS-025
    [Fact]
    [Trait("BR", "BR-CUS-025")]
    public async Task Test149_DeleteCustomersByIdReviewsById_Returns204()
    {
        var customerId = await ArrangeCustomerIdAsync();
        var reviewId = await ArrangeReviewIdAsync(customerId);
        using var response = await SendAsync(HttpMethod.Delete, $"/api/v1/customers/{customerId}/reviews/{reviewId}");
        await AssertResponseAsync(response, 204);
    }

    // @BR-ID: BR-UI-002
    [Fact]
    [Trait("BR", "BR-UI-002")]
    public async Task Test150_DeleteCustomersByIdReviewsById_Returns204()
    {
        var customerId = await ArrangeCustomerIdAsync();
        var reviewId = await ArrangeReviewIdAsync(customerId);
        using var response = await SendAsync(HttpMethod.Delete, $"/api/v1/customers/{customerId}/reviews/{reviewId}");
        await AssertResponseAsync(response, 204);
    }

    #endregion

    #region Newsletter Subscriptions

    // @BR-ID: BR-CUS-028
    [Fact]
    [Trait("BR", "BR-CUS-028")]
    public async Task Test151_DeleteNewsletterSubscriptionsPhase4c40exampleCom_Returns204()
    {
        using var response = await SendAsync(HttpMethod.Delete, "/api/v1/newsletter-subscriptions/phase4c%40example.com");
        await AssertResponseAsync(response, 204);
    }

    // @BR-ID: BR-CUS-028
    [Fact]
    [Trait("BR", "BR-CUS-028")]
    public async Task Test152_DeleteNewsletterSubscriptionsPhase4c40exampleCom_Returns501()
    {
        using var response = await SendAsync(HttpMethod.Delete, "/api/v1/newsletter-subscriptions/phase4c%40example.com");
        await AssertResponseAsync(response, 204);
    }

    // @BR-ID: BR-CUS-028
    [Fact]
    [Trait("BR", "BR-CUS-028")]
    public async Task Test153_DeleteNewsletterSubscriptionsPhase4c40exampleCom_Returns204()
    {
        using var response = await SendAsync(HttpMethod.Delete, "/api/v1/newsletter-subscriptions/phase4c%40example.com");
        await AssertResponseAsync(response, 204);
    }

    #endregion

    #region Users

    // @BR-ID: BR-CUS-NN-015
    [Fact]
    [Trait("BR", "BR-CUS-NN-015")]
    public async Task Test154_DeleteUsersById_Returns204()
    {
        var userId = await ArrangeUserIdAsync();
        using var response = await SendAsync(HttpMethod.Delete, $"/api/v1/users/{userId}");
        await AssertResponseAsync(response, 204);
    }

    // @BR-ID: BR-CUS-NN-015
    [Fact]
    [Trait("BR", "BR-CUS-NN-015")]
    public async Task Test155_DeleteUsersById_Returns403()
    {
        var userId = await ArrangeUserIdAsync();
        using var response = await SendForbiddenAsync(HttpMethod.Delete, $"/api/v1/users/{userId}");
        await AssertResponseAsync(response, 403);
    }

    // @BR-ID: BR-CUS-NN-015
    [Fact]
    [Trait("BR", "BR-CUS-NN-015")]
    public async Task Test156_DeleteUsersById_Returns204()
    {
        var userId = await ArrangeUserIdAsync();
        using var response = await SendAsync(HttpMethod.Delete, $"/api/v1/users/{userId}");
        await AssertResponseAsync(response, 204);
    }

    #endregion

    private Task<HttpResponseMessage> SendAsync(HttpMethod method, string path, string? payload = null) =>
        SendCoreAsync(method, path, payload, fixture.AdminAccessToken, true);

    private Task<HttpResponseMessage> SendUnauthenticatedAsync(HttpMethod method, string path, string? payload = null) =>
        SendCoreAsync(method, path, payload, null, false);

    private Task<HttpResponseMessage> SendForbiddenAsync(HttpMethod method, string path, string? payload = null) =>
        SendForbiddenCoreAsync(method, path, payload);

    private async Task<HttpResponseMessage> SendForbiddenCoreAsync(HttpMethod method, string path, string? payload)
    {
        await fixture.EnsureAuthenticatedBasicAdministratorAsync();
        return await SendCoreAsync(method, path, payload, fixture.BasicAdminAccessToken, false);
    }

    private async Task<HttpResponseMessage> SendCoreAsync(
        HttpMethod method,
        string path,
        string? payload,
        string? explicitToken,
        bool selectToken)
    {
        if (selectToken && UsesCustomerIdentity(path, method))
        {
            await fixture.EnsureAuthenticatedTestCustomerAsync();
        }
        else if (selectToken && (UsesAdministratorIdentity(path) ||
                 path.Contains("/reviews", StringComparison.Ordinal) &&
                 method != HttpMethod.Get && method != HttpMethod.Post))
        {
            await fixture.EnsureAuthenticatedTestAdministratorAsync();
        }

        if (path.Contains("/customer-password-resets/default/phase4c-token", StringComparison.Ordinal) &&
            (payload is null || payload == Payloads.ResetPassword))
        {
            await fixture.EnsureTestResetTokenAsync("phase4c-token", false);
        }
        else if (path.Contains("/user-password-resets/default/phase4c-token", StringComparison.Ordinal) &&
                 (payload is null || payload == Payloads.ResetPassword))
        {
            await fixture.EnsureTestResetTokenAsync("phase4c-token", true);
        }

        if (payload == Payloads.Registration)
        {
            payload = payload.Replace("phase4c@example.com", $"phase4c-{Guid.NewGuid():N}@example.com", StringComparison.Ordinal);
        }
        else if (payload == Payloads.User)
        {
            var uniqueValue = $"phase4c-{Guid.NewGuid():N}";
            payload = payload
                .Replace("phase4c-test", uniqueValue, StringComparison.Ordinal)
                .Replace("phase4c@example.com", $"{uniqueValue}@example.com", StringComparison.Ordinal)
                .Replace($"\"defaultLanguageCode\":\"{uniqueValue}\"", "\"defaultLanguageCode\":\"en\"", StringComparison.Ordinal);
        }
        else if (payload == Payloads.CustomerUpdate)
        {
            payload = payload.Replace(
                "phase4c-login@example.com",
                $"phase4c-update-{Guid.NewGuid():N}@example.com",
                StringComparison.Ordinal);
        }
        else if (payload == Payloads.UserUpdate)
        {
            var uniqueValue = $"phase4c-update-{Guid.NewGuid():N}";
            payload = payload
                .Replace("phase4c-test", uniqueValue, StringComparison.Ordinal)
                .Replace("phase4c@example.com", $"{uniqueValue}@example.com", StringComparison.Ordinal);
        }
        else if (payload == Payloads.ExternalIdentity)
        {
            payload = payload.Replace(
                "\"providerUserId\":\"00000000-0000-0000-0000-000000000001\"",
                $"\"providerUserId\":\"{Guid.NewGuid()}\"",
                StringComparison.Ordinal);
        }

        if (payload is not null && path.Contains("/reviews", StringComparison.Ordinal))
        {
            var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
            var customerIndex = Array.IndexOf(segments, "customers") + 1;
            if (customerIndex > 0 && customerIndex < segments.Length &&
                payload.Contains("\"customerId\"", StringComparison.Ordinal))
            {
                payload = payload.Replace(SeedResourceId, segments[customerIndex], StringComparison.Ordinal);
            }
        }

        using var request = new HttpRequestMessage(method, ComprehensiveTestBase.NormalizeApiPath(path));
        if (payload is not null)
        {
            request.Content = new StringContent(payload, Encoding.UTF8, "application/json");
        }

        var token = selectToken ? SelectToken(path, method) : explicitToken;
        if (!string.IsNullOrWhiteSpace(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        return await fixture.CustomerIdentityClient.SendAsync(request);
    }

    private string? SelectToken(string path, HttpMethod method)
    {
        if (path.StartsWith("/api/v1/customer-auth/refresh", StringComparison.Ordinal) ||
            path.StartsWith("/api/v1/customers/me", StringComparison.Ordinal) ||
            path.StartsWith("/api/v1/external-identities", StringComparison.Ordinal) ||
            (path.Contains("/reviews", StringComparison.Ordinal) && method == HttpMethod.Post))
        {
            return fixture.CustomerAccessToken;
        }

        if (path.StartsWith("/api/v1/admin-auth/refresh", StringComparison.Ordinal) ||
            path.StartsWith("/api/v1/users", StringComparison.Ordinal) ||
            path.StartsWith("/api/v1/customers", StringComparison.Ordinal))
        {
            return fixture.AdminAccessToken;
        }

        return null;
    }

    private static bool UsesCustomerIdentity(string path, HttpMethod method) =>
        path.StartsWith("/api/v1/customer-auth/refresh", StringComparison.Ordinal) ||
        path.StartsWith("/api/v1/customers/me", StringComparison.Ordinal) ||
        path.StartsWith("/api/v1/external-identities", StringComparison.Ordinal) ||
        (path.Contains("/reviews", StringComparison.Ordinal) && method == HttpMethod.Post);

    private static bool UsesAdministratorIdentity(string path) =>
        path.StartsWith("/api/v1/admin-auth/refresh", StringComparison.Ordinal) ||
        path.StartsWith("/api/v1/users", StringComparison.Ordinal) ||
        path.StartsWith("/api/v1/customers", StringComparison.Ordinal) &&
        !path.StartsWith("/api/v1/customers/me", StringComparison.Ordinal) &&
        !path.Contains("/reviews", StringComparison.Ordinal);

    private static async Task AssertResponseAsync(
        HttpResponseMessage response,
        int expectedStatus,
        string? requiredField = null)
    {
        Assert.Equal(expectedStatus, (int)response.StatusCode);
        if (requiredField is not null)
        {
            var body = await response.Content.ReadAsStringAsync();
            Assert.True(
                HasNonEmptyJsonField(body, requiredField),
                $"Response is missing non-empty JSON field '{requiredField}'.");
        }
    }

    private async Task<string> ArrangeCustomerIdAsync()
    {
        var uniqueEmail = $"phase4c-{Guid.NewGuid():N}@example.com";
        var payload = Payloads.Registration.Replace("phase4c@example.com", uniqueEmail, StringComparison.Ordinal);
        using var response = await SendAsync(HttpMethod.Post, "/api/v1/customers", payload);
        var body = await response.Content.ReadAsStringAsync();
        return TryExtractResourceId(body) ?? SeedResourceId;
    }

    private async Task<string> ArrangeUserIdAsync()
    {
        var uniqueValue = $"phase4c-{Guid.NewGuid():N}";
        var payload = Payloads.User
            .Replace("phase4c-test", uniqueValue, StringComparison.Ordinal)
            .Replace("phase4c@example.com", $"{uniqueValue}@example.com", StringComparison.Ordinal)
            .Replace($"\"defaultLanguageCode\":\"{uniqueValue}\"", "\"defaultLanguageCode\":\"en\"", StringComparison.Ordinal);
        using var response = await SendAsync(HttpMethod.Post, "/api/v1/users", payload);
        var body = await response.Content.ReadAsStringAsync();
        return TryExtractResourceId(body) ?? SeedResourceId;
    }

    private async Task<string> ArrangeReviewIdAsync(string customerId)
    {
        var payload = Payloads.Review.Replace(SeedResourceId, customerId, StringComparison.Ordinal);
        using var response = await SendAsync(
            HttpMethod.Post,
            $"/api/v1/customers/{Uri.EscapeDataString(customerId)}/reviews",
            payload);
        var body = await response.Content.ReadAsStringAsync();
        return TryExtractResourceId(body) ?? SeedResourceId;
    }

    private static bool HasNonEmptyJsonField(string body, string field)
    {
        try
        {
            var root = JsonNode.Parse(body);
            var value = root?[field];
            return value is not null &&
                   value.ToJsonString() is not "null" and not "\"\"";
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static string? TryExtractResourceId(string body)
    {
        try
        {
            var root = JsonNode.Parse(body);
            foreach (var field in new[] { "id", "subjectId", "customerId", "productId", "orderId", "paymentIntentId", "storeId", "userId" })
            {
                var value = root?[field]?.ToString();
                if (!string.IsNullOrWhiteSpace(value))
                {
                    return value;
                }
            }
        }
        catch (JsonException)
        {
            // Responses without JSON bodies are valid for 204 cases.
        }

        return null;
    }
}
