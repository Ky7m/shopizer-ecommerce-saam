using System.Text;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace Shopizer.IntegrationTests;

public abstract class ComprehensiveTestBase(HttpClient client)
{
    protected const string ResourceId = "00000000-0000-0000-0000-000000000001";
    protected const string EmptyPayload = "{}";

    protected async Task AssertShellAsync(
        HttpMethod method,
        string path,
        string? payload,
        int expectedStatus,
        string? requiredField = null,
        string? bodyRegex = null)
    {
        using var request = new HttpRequestMessage(method, path);
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
