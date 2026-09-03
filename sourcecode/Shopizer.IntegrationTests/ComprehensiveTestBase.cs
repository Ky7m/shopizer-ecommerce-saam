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
        path = NormalizePath(path, expectedStatus);
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

        if (requiredField is not null)
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

    private static string NormalizePath(string path, int expectedStatus)
    {
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

    private static string? NormalizePayload(HttpMethod method, string path, string? payload, int expectedStatus)
    {
        if (payload is null)
        {
            return null;
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
