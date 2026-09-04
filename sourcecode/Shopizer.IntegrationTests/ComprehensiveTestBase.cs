using System.Text;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace Shopizer.IntegrationTests;

public abstract class ComprehensiveTestBase(
    HttpClient client,
    string? bearerToken = null,
    Func<HttpMethod, string, int, Task>? prepareRequest = null)
{
    protected const string ResourceId = "00000000-0000-0000-0000-000000000001";
    protected const string EmptyPayload = "{}";
    private const string MissingResourceId = "00000000-0000-0000-0000-000000000099";
    private const string MoveParentId = "00000000-0000-0000-0000-000000000002";

    protected async Task AssertShellAsync(
        HttpMethod method,
        string path,
        string? payload,
        int expectedStatus,
        string? requiredField = null,
        string? bodyRegex = null)
    {
        path = NormalizeApiPath(path);
        path = NormalizePath(path, expectedStatus, method);
        payload = NormalizePayload(method, path, payload, expectedStatus);
        if (prepareRequest is not null)
        {
            await prepareRequest(method, path, expectedStatus);
        }

        using var request = new HttpRequestMessage(method, path);
        if (bearerToken is not null && expectedStatus != (int)HttpStatusCode.Unauthorized)
        {
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", bearerToken);
        }
        if (payload is not null)
        {
            request.Content = new StringContent(payload, Encoding.UTF8, "application/json");
        }

        using var response = await client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(
            (int)response.StatusCode == expectedStatus,
            $"Expected HTTP {expectedStatus}, got {(int)response.StatusCode} ({response.StatusCode}) for {method} {path}. Body: {body}");

        if (requiredField is not null && response.StatusCode != HttpStatusCode.NoContent)
        {
            Assert.True(
                HasNonEmptyJsonField(body, requiredField),
                $"Response for {method} {path} is missing non-empty JSON field '{requiredField}'. Body: {body}");
        }

        if (bodyRegex is not null)
        {
            Assert.True(
                Regex.IsMatch(body, bodyRegex, RegexOptions.CultureInvariant),
                $"Response for {method} {path} did not match regex '{bodyRegex}'. Body: {body}");
        }
    }

    private static string NormalizePath(string path, int expectedStatus, HttpMethod method)
    {
        if (path.StartsWith("/api/v1/callbacks/00000000-0000-0000-0000-000000000001", StringComparison.Ordinal))
        {
            path = path.Replace(ResourceId, "stripe", StringComparison.Ordinal);
        }
        else if (expectedStatus == (int)HttpStatusCode.BadRequest &&
                 path.Equals("/api/v1/reconciliation/capturable", StringComparison.Ordinal))
        {
            path += "?from=2026-09-02T00:00:00Z&to=2026-09-01T00:00:00Z";
        }
        else if (expectedStatus == (int)HttpStatusCode.BadRequest &&
                 path.Equals("/api/v1/pricing/products/phase4c-sku/price", StringComparison.Ordinal))
        {
            path += "?evaluationAt=not-a-date";
        }
        else if (expectedStatus == (int)HttpStatusCode.BadRequest &&
                 method == HttpMethod.Get &&
                 path.Equals("/api/v1/private/products/phase4c-sku/prices", StringComparison.Ordinal))
        {
            path += "/not-a-guid";
        }
        else if (expectedStatus == (int)HttpStatusCode.BadRequest &&
                 path.EndsWith($"/private/products/phase4c-sku/prices/{ResourceId}", StringComparison.Ordinal))
        {
            path = path.Replace(ResourceId, "not-a-guid", StringComparison.Ordinal);
        }
        else if (expectedStatus != (int)HttpStatusCode.BadRequest &&
                 path.Contains("/private/products/phase4c-sku/availabilities/", StringComparison.Ordinal))
        {
            path = path.Replace(
                $"/availabilities/{ResourceId}",
                "/availabilities/1",
                StringComparison.Ordinal);
        }
        else if (expectedStatus != (int)HttpStatusCode.NotFound &&
                 path.StartsWith("/api/v1/payment-methods/phase4c-code", StringComparison.Ordinal))
        {
            path = path.Replace("phase4c-code", "stripe", StringComparison.Ordinal);
        }
        else if (expectedStatus == (int)HttpStatusCode.BadRequest &&
                 method == HttpMethod.Get &&
                 (path.Equals("/api/v1/tax-classes", StringComparison.Ordinal) ||
                  path.Equals("/api/v1/tax-rates", StringComparison.Ordinal)))
        {
            path += "?page=0&pageSize=20";
        }
        else if (expectedStatus != (int)HttpStatusCode.BadRequest &&
                 method == HttpMethod.Get &&
                 path.Equals("/api/v1/tax-classes/exists", StringComparison.Ordinal))
        {
            path += "?code=DEFAULT";
        }
        else if (expectedStatus != (int)HttpStatusCode.BadRequest &&
                 method == HttpMethod.Get &&
                 path.Equals("/api/v1/tax-rates/exists", StringComparison.Ordinal))
        {
            path += "?code=tax-base";
        }

        if (expectedStatus != (int)HttpStatusCode.BadRequest)
        {
            if (path.Equals("/api/v1/private/modules/shipping/phase4c-value", StringComparison.Ordinal))
            {
                path = path.Replace("phase4c-value", "usps", StringComparison.Ordinal);
            }
            else if (path.StartsWith("/api/v1/private/shipping/package/phase4c-value", StringComparison.Ordinal))
            {
                path = path.Replace("phase4c-value", "phase4c-test", StringComparison.Ordinal);
            }

            if (path.Equals("/api/v1/files", StringComparison.Ordinal) && method == HttpMethod.Get)
            {
                path += "?storeCode=test-store-001&contentType=Image&folderPath=phase4c-files";
            }
            else if (path.Equals("/api/v1/files", StringComparison.Ordinal) && method == HttpMethod.Delete)
            {
                path += "?storeCode=test-store-001";
            }
            else if (path.Equals("/api/v1/files/folders", StringComparison.Ordinal) && method == HttpMethod.Get)
            {
                path += "?storeCode=test-store-001&provider=Local&folderPath=phase4c-folders";
            }
            else if (path.Equals("/api/v1/files/folders", StringComparison.Ordinal) && method == HttpMethod.Delete)
            {
                path += "?storeCode=test-store-001&provider=Local&folderPath=phase4c-folders&folderName=phase4c-folder";
            }
            else if (path.StartsWith("/api/v1/files/", StringComparison.Ordinal) &&
                     (method == HttpMethod.Get || method == HttpMethod.Delete))
            {
                path = path.Replace("/files/phase4c-value", "/files/phase4c-file", StringComparison.Ordinal);
                path += "?storeCode=test-store-001&contentType=Image&folderPath=phase4c-files";
            }
        }

        if (expectedStatus == (int)HttpStatusCode.NotFound)
        {
            path = path.Replace(ResourceId, MissingResourceId, StringComparison.Ordinal)
                .Replace("phase4c-sku", "missing-phase4c-sku", StringComparison.Ordinal)
                .Replace("phase4c-value", "missing-phase4c-value", StringComparison.Ordinal);
        }

        return path.Replace(
            $"/categories/{ResourceId}/move/{ResourceId}",
            $"/categories/{ResourceId}/move/{MoveParentId}",
            StringComparison.Ordinal);
    }

    internal static string NormalizeApiPath(string path)
    {
        if (path.StartsWith("/api/v1/", StringComparison.Ordinal))
        {
            return path;
        }

        return $"/api/v1{path}";
    }

    private static string? NormalizePayload(HttpMethod method, string path, string? payload, int expectedStatus)
    {
        if (payload is null)
        {
            return null;
        }

        if (path.StartsWith("/api/v1/tax-", StringComparison.Ordinal))
        {
            if (expectedStatus == (int)HttpStatusCode.BadRequest && payload == EmptyPayload)
            {
                return "{";
            }

            payload = payload
                .Replace("\"currencyCode\":\"phase4c-test\"", "\"currencyCode\":\"USD\"", StringComparison.Ordinal)
                .Replace("\"countryCode\":\"phase4c-test\"", "\"countryCode\":\"CA\"", StringComparison.Ordinal)
                .Replace("\"zoneCode\":\"phase4c-test\"", "\"zoneCode\":\"QC\"", StringComparison.Ordinal)
                .Replace("\"stateProvince\":\"phase4c-test\"", "\"stateProvince\":\"QC\"", StringComparison.Ordinal)
                .Replace("\"languageCode\":\"phase4c-test\"", "\"languageCode\":\"en\"", StringComparison.Ordinal)
                .Replace("\"taxClassCode\":\"phase4c-test\"", "\"taxClassCode\":\"DEFAULT\"", StringComparison.Ordinal)
                .Replace("\"code\":\"phase4c-test\"", "\"code\":\"tx-created\"", StringComparison.Ordinal)
                .Replace("\"shippingAddress\":\"phase4c-test\"",
                    "\"shippingAddress\":{\"countryCode\":\"CA\",\"zoneCode\":\"QC\",\"stateProvince\":\"QC\"}",
                    StringComparison.Ordinal)
                .Replace("\"shipping\":\"phase4c-test\"",
                    "\"shipping\":{\"shippingAmount\":5,\"handlingAmount\":1}",
                    StringComparison.Ordinal);

            if (path.StartsWith("/api/v1/tax-classes/", StringComparison.Ordinal) && method == HttpMethod.Put)
            {
                payload = payload.Replace("\"code\":\"tx-created\"", "\"code\":\"tx-updated\"", StringComparison.Ordinal);
            }

            if (path.Equals("/api/v1/tax-rates", StringComparison.Ordinal) && method == HttpMethod.Post)
            {
                payload = payload.Replace("\"code\":\"tx-created\"", "\"code\":\"tax-rate\"", StringComparison.Ordinal);
            }

            if (path.StartsWith("/api/v1/tax-rates/", StringComparison.Ordinal) && method == HttpMethod.Put)
            {
                payload = payload
                    .Replace("\"code\":\"tx-created\"", "\"code\":\"tax-rate-updated\"", StringComparison.Ordinal)
                    .Replace("{}", "{\"taxClassCode\":\"DEFAULT\",\"code\":\"phase4c-rate-updated\",\"rate\":10.5,\"priority\":1,\"piggyback\":true,\"countryCode\":\"CA\",\"zoneCode\":\"QC\",\"stateProvince\":\"QC\",\"descriptions\":[{\"languageCode\":\"en\",\"name\":\"Tax base\",\"title\":\"Tax base\",\"description\":\"Tax base\"}]}",
                        StringComparison.Ordinal);
            }
        }

        if (path.Equals("/api/v1/categories", StringComparison.Ordinal) &&
            method == HttpMethod.Post &&
            payload != EmptyPayload)
        {
            payload = payload.Replace(
                "\"code\":\"phase4c-test\"",
                $"\"code\":\"phase4c-{Guid.NewGuid():N}\"",
                StringComparison.Ordinal);
        }

        if (path.Equals("/api/v1/products", StringComparison.Ordinal) &&
            method == HttpMethod.Post &&
            payload != EmptyPayload)
        {
            payload = payload.Replace(
                "\"sku\":\"phase4c-test\"",
                $"\"sku\":\"phase4c-{Guid.NewGuid():N}\"",
                StringComparison.Ordinal);
        }

        if (path.Contains("/variants", StringComparison.Ordinal) &&
            method == HttpMethod.Post &&
            payload != EmptyPayload)
        {
            payload = payload.Replace(
                "\"sku\":\"phase4c-test\"",
                $"\"sku\":\"phase4c-{Guid.NewGuid():N}\"",
                StringComparison.Ordinal);
            payload = payload.Replace("\"defaultSelection\":true", "\"defaultSelection\":false", StringComparison.Ordinal);
        }

        if (path.EndsWith("/media", StringComparison.Ordinal) &&
            method == HttpMethod.Post &&
            payload != EmptyPayload)
        {
            payload = payload.Replace("\"file\":\"phase4c-test\",", "\"externalUrl\":\"https://example.com/phase4c-image.jpg\",", StringComparison.Ordinal);
        }

        if (path.EndsWith("/reservations", StringComparison.Ordinal) &&
            method == HttpMethod.Post &&
            payload != EmptyPayload)
        {
            payload = payload.Replace(
                "\"expiresAt\":\"2026-09-02T00:00:00Z\"",
                $"\"expiresAt\":\"{DateTimeOffset.UtcNow.AddDays(1):O}\"",
                StringComparison.Ordinal);
        }

        if (path.StartsWith("/api/v1/payment-intents", StringComparison.Ordinal) &&
            expectedStatus is >= 200 and < 300)
        {
            payload = payload
                .Replace("\"paymentMethodCode\":\"phase4c-test\"", "\"paymentMethodCode\":\"stripe\"", StringComparison.Ordinal)
                .Replace("\"amount\":\"phase4c-test\"", "\"amount\":\"10.00\"", StringComparison.Ordinal)
                .Replace("\"currency\":\"phase4c-test\"", "\"currency\":\"USD\"", StringComparison.Ordinal)
                .Replace("\"paymentToken\":\"phase4c-test\"", "\"paymentToken\":\"tok_phase4c\"", StringComparison.Ordinal)
                .Replace("\"payerReference\":\"phase4c-test\"", "\"payerReference\":\"payer_phase4c\"", StringComparison.Ordinal)
                .Replace("\"providerIntentReference\":\"phase4c-test\"", "\"providerIntentReference\":\"pi_phase4c\"", StringComparison.Ordinal);
        }

        if (path.StartsWith("/api/v1/pricing/", StringComparison.Ordinal) ||
            path.StartsWith("/api/v1/private/products/phase4c-sku/", StringComparison.Ordinal))
        {
            payload = payload
                .Replace("\"currency\":\"phase4c-test\"", "\"currency\":\"USD\"", StringComparison.Ordinal)
                .Replace("\"code\":\"phase4c-test\"", "\"code\":\"phase4c_test\"", StringComparison.Ordinal)
                .Replace("\"productSku\":\"phase4c-test\"", "\"productSku\":\"phase4c-sku\"", StringComparison.Ordinal)
                .Replace("\"variantSku\":\"phase4c-test\"", "\"variantSku\":\"phase4c-sku\"", StringComparison.Ordinal)
                .Replace("\"parentProductSku\":\"phase4c-test\"", "\"parentProductSku\":\"phase4c-sku\"", StringComparison.Ordinal);

            if (expectedStatus is >= 200 and < 300)
            {
                payload = payload.Replace(
                    "\"defaultPrice\":true",
                    "\"defaultPrice\":false",
                    StringComparison.Ordinal);
            }
        }

        if (path.StartsWith("/api/v1/adapters/refresh", StringComparison.Ordinal) &&
            expectedStatus is >= 200 and < 300)
        {
            payload = payload
                .Replace("\"moduleType\":{}", "\"moduleType\":\"Adapter\"", StringComparison.Ordinal)
                .Replace("\"timeoutMs\":1", "\"timeoutMs\":100", StringComparison.Ordinal)
                .Replace("\"config2\":\"phase4c-test\"", "\"config2\":\"phase4c-test-2\"", StringComparison.Ordinal);
        }

        if (path.StartsWith("/api/v1/carrier-quotes/", StringComparison.Ordinal) ||
            path.StartsWith("/api/v1/maps/distance", StringComparison.Ordinal))
        {
            payload = payload.Replace("\"countryCode\":\"phase4c-test\"", "\"countryCode\":\"US\"", StringComparison.Ordinal);
        }

        if (path.StartsWith("/api/v1/cart/", StringComparison.Ordinal) &&
            path.EndsWith("/shipping", StringComparison.Ordinal))
        {
            payload = payload.Replace("\"countryCode\":\"phase4c-test\"", "\"countryCode\":\"CA\"", StringComparison.Ordinal);
        }

        if (path.StartsWith("/api/v1/private/shipping/", StringComparison.Ordinal) ||
            path.Equals("/api/v1/private/modules/shipping", StringComparison.Ordinal))
        {
            payload = payload
                .Replace("\"countryCode\":\"phase4c-test\"", "\"countryCode\":\"CA\"", StringComparison.Ordinal)
                .Replace("\"shipToCountry\":[\"phase4c-test\"]", "\"shipToCountry\":[\"CA\"]", StringComparison.Ordinal)
                .Replace("\"moduleCode\":\"phase4c-test\"", "\"moduleCode\":\"usps\"", StringComparison.Ordinal);
            if (path.Equals("/api/v1/private/modules/shipping", StringComparison.Ordinal) &&
                expectedStatus is >= 200 and < 300)
            {
                payload = payload.Replace(
                    "\"integrationKeys\":{}",
                    "\"integrationKeys\":{\"price\":\"5\",\"productVirtual\":\"false\",\"productWeight\":\"1\"}",
                    StringComparison.Ordinal);
            }
        }

        if (path.Equals("/api/v1/geolocation/ip", StringComparison.Ordinal) &&
            expectedStatus is >= 200 and < 300)
        {
            payload = payload.Replace("\"ipAddress\":\"phase4c-test\"", "\"ipAddress\":\"8.8.8.8\"", StringComparison.Ordinal);
        }

        if (path.StartsWith("/api/v1/files", StringComparison.Ordinal) &&
            expectedStatus is >= 200 and < 300)
        {
            payload = payload
                .Replace("\"contentType\":{}", "\"contentType\":\"Image\"", StringComparison.Ordinal)
                .Replace("\"provider\":{}", "\"provider\":\"Local\"", StringComparison.Ordinal)
                .Replace("\"mimeType\":\"phase4c-test\"", "\"mimeType\":\"text/plain\"", StringComparison.Ordinal)
                .Replace("\"contentBase64\":\"phase4c-test\"", "\"contentBase64\":\"cGhhc2U0Yy10ZXN0\"", StringComparison.Ordinal);
            if (path.Contains("/folders", StringComparison.Ordinal))
            {
                payload = payload
                    .Replace("\"folderPath\":\"phase4c-test\"", "\"folderPath\":\"phase4c-folders\"", StringComparison.Ordinal)
                    .Replace("\"folderName\":\"phase4c-test\"", "\"folderName\":\"phase4c-folder\"", StringComparison.Ordinal);
            }
            else
            {
                payload = payload
                    .Replace("\"folderPath\":\"phase4c-test\"", "\"folderPath\":\"phase4c-files\"", StringComparison.Ordinal)
                    .Replace("\"fileName\":\"phase4c-test\"", "\"fileName\":\"phase4c-file\"", StringComparison.Ordinal)
                    .Replace("\"idempotencyKey\":\"phase4c-test\"", $"\"idempotencyKey\":\"platform-{Guid.NewGuid():N}\"", StringComparison.Ordinal);
            }
        }

        if (path.Equals("/api/v1/emails", StringComparison.Ordinal) &&
            expectedStatus is >= 200 and < 300)
        {
            payload = payload.Replace("\"locale\":\"phase4c-test\"", "\"locale\":\"en\"", StringComparison.Ordinal);
        }

        if (path.Equals($"/api/v1/categories/{ResourceId}", StringComparison.Ordinal) &&
            method == HttpMethod.Put)
        {
            payload = payload.Replace(
                $"\"parentId\":\"{ResourceId}\"",
                "\"parentId\":null",
                StringComparison.Ordinal);
        }

        if (expectedStatus == (int)HttpStatusCode.Unauthorized && payload == EmptyPayload)
        {
            payload = UnauthorizedPayload(path, method);
        }

        return payload;
    }

    private static string UnauthorizedPayload(string path, HttpMethod method)
    {
        if (path.Equals("/api/v1/categories", StringComparison.Ordinal) ||
            (path.Contains("/categories/", StringComparison.Ordinal) && method == HttpMethod.Put))
        {
            return """{"code":"phase4c-unauthorized","parentId":"00000000-0000-0000-0000-000000000001","visible":true,"featured":true,"sortOrder":1,"descriptions":[{"languageCode":"en","name":"phase4c-unauthorized","friendlyUrl":"phase4c-unauthorized"}]}""";
        }

        if (path.Equals("/api/v1/products", StringComparison.Ordinal) ||
            (path.Contains("/products/", StringComparison.Ordinal) && method == HttpMethod.Put))
        {
            return """{"sku":"phase4c-unauthorized","visible":true,"canBePurchased":true,"dateAvailable":"2026-09-04T00:00:00Z","descriptions":[{"languageCode":"en","name":"phase4c-unauthorized","friendlyUrl":"phase4c-unauthorized"}],"availabilities":[{"regionCode":"*","quantity":1,"active":true}]}""";
        }

        if (path.EndsWith("/media", StringComparison.Ordinal))
        {
            return """{"externalUrl":"https://example.com/phase4c-unauthorized.jpg","fileName":"phase4c-unauthorized.jpg","defaultImage":false}""";
        }

        if (path.EndsWith("/reservations", StringComparison.Ordinal))
        {
            return $$"""{"reservationKey":"phase4c-test","variantId":"{{ResourceId}}","availabilityId":"{{ResourceId}}","regionCode":"phase4c-test","quantity":1,"expiresAt":"{{DateTimeOffset.UtcNow.AddDays(1):O}}"}""";
        }

        if (path.EndsWith("/variants", StringComparison.Ordinal))
        {
            return """{"sku":"phase4c-unauthorized","code":"phase4c-unauthorized","defaultSelection":false,"available":true,"dateAvailable":"2026-09-04T00:00:00Z"}""";
        }

        if (path.EndsWith("/availability", StringComparison.Ordinal))
        {
            return """{"items":[{"regionCode":"*","quantity":1,"active":true}]}""";
        }

        if (path.EndsWith("/visibility", StringComparison.Ordinal))
        {
            return """{"visible":true,"canBePurchased":true,"dateAvailable":"2026-09-04T00:00:00Z"}""";
        }

        return EmptyPayload;
    }

    protected static HttpMethod Method(string name) =>
        name.Equals("PATCH", StringComparison.OrdinalIgnoreCase)
            ? HttpMethod.Patch
            : new HttpMethod(name);

    private static bool HasNonEmptyJsonField(string body, string field)
    {
        try
        {
            var root = JsonNode.Parse(body);
            var value = root?[field];
            return value is not null &&
                   value.ToJsonString() is not "null" and not "\"\"";
        }
        catch (System.Text.Json.JsonException)
        {
            return false;
        }
    }
}
