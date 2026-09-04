using System.Net.Http.Headers;
using System.Text;
using Shopizer.MerchantAdministration.Models;

namespace Shopizer.MerchantAdministration.Services;

public sealed class FileProviderClient(IHttpClientFactory clients, IConfiguration configuration)
{
    public async Task<string> UploadAsync(StoreRecord store, string file, RequestContext context, CancellationToken ct)
    {
        var baseUrl = configuration["MerchantAdministration:FileProviderBaseUrl"];
        if (string.IsNullOrWhiteSpace(baseUrl)) throw new DomainException("STORAGE_UNAVAILABLE", "No file provider is configured", 503);
        byte[] bytes; try { bytes = Convert.FromBase64String(file); } catch (FormatException) { bytes = Encoding.UTF8.GetBytes(file); }
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl.TrimEnd('/')}/objects/{Uri.EscapeDataString(context.TenantId)}/{Uri.EscapeDataString(store.Code)}"); request.Content = new ByteArrayContent(bytes); request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream"); request.Headers.TryAddWithoutValidation("x-tenant-id", context.TenantId); request.Headers.TryAddWithoutValidation("x-store-id", store.Code); request.Headers.TryAddWithoutValidation("x-correlation-id", context.CorrelationId);
        using var response = await clients.CreateClient("file-provider").SendAsync(request, ct); if (!response.IsSuccessStatusCode) throw new DomainException("STORAGE_UNAVAILABLE", "File provider rejected the logo", 503); return response.Headers.Location?.ToString() ?? await response.Content.ReadAsStringAsync(ct);
    }
    public async Task DeleteAsync(StoreRecord store, RequestContext context, CancellationToken ct)
    {
        var baseUrl = configuration["MerchantAdministration:FileProviderBaseUrl"]; if (string.IsNullOrWhiteSpace(baseUrl)) throw new DomainException("STORAGE_UNAVAILABLE", "No file provider is configured", 503);
        using var request = new HttpRequestMessage(HttpMethod.Delete, $"{baseUrl.TrimEnd('/')}/objects/{Uri.EscapeDataString(context.TenantId)}/{Uri.EscapeDataString(store.Code)}"); request.Headers.TryAddWithoutValidation("x-tenant-id", context.TenantId); request.Headers.TryAddWithoutValidation("x-store-id", store.Code); request.Headers.TryAddWithoutValidation("x-correlation-id", context.CorrelationId); using var response = await clients.CreateClient("file-provider").SendAsync(request, ct); if (!response.IsSuccessStatusCode && response.StatusCode != System.Net.HttpStatusCode.NotFound) throw new DomainException("STORAGE_UNAVAILABLE", "File provider rejected logo deletion", 503);
    }
}
