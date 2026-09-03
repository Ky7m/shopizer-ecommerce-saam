using System.Threading.Channels;
using Shopizer.Search.Data;
using Shopizer.Search.DTOs;
using Shopizer.Search.Models;

namespace Shopizer.Search.Services;

public sealed class RebuildQueue
{
    private readonly Channel<(SearchRebuildJob Job, RequestContext Context)> _items =
        Channel.CreateUnbounded<(SearchRebuildJob Job, RequestContext Context)>();

    public bool Enqueue(SearchRebuildJob job, RequestContext context) =>
        _items.Writer.TryWrite((job, context));

    public IAsyncEnumerable<(SearchRebuildJob Job, RequestContext Context)> ReadAllAsync(CancellationToken ct) =>
        _items.Reader.ReadAllAsync(ct);
}

public sealed class SearchService(
    SearchRepository repository,
    EventPublisher events,
    RebuildQueue queue,
    IConfiguration configuration,
    ILogger<SearchService> logger)
{
    private readonly bool _enabled = !IsFalse(configuration["Search:Enabled"]) &&
                                     !IsTrue(configuration["Search:NoIndex"]);
    private readonly bool _providerAvailable = !IsFalse(configuration["Search:ProviderAvailable"]);
    private readonly string[] _locales =
        (configuration.GetSection("Search:Locales").Get<string[]>() ?? ["en"])
        .Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim().ToLowerInvariant()).Distinct().ToArray();

    // @BR-CAT-020: Search queries run only when the configured provider boundary is enabled and available.
    // @BR-CAT-021: Search returns one localized product document for the requested store and locale.
    // @BR-CAT-022: Search returns the persisted localized product projection and inventory values.
    // @BR-CAT-034: Search validates and defensively applies the requested offset and limit at the service boundary.
    public async Task<SearchResultsResponseDto> SearchAsync(
        SearchRequestDto request, RequestContext context, string locale, CancellationToken ct)
    {
        RequireProvider();
        var normalizedQuery = request.Query?.Trim();
        if (string.IsNullOrWhiteSpace(normalizedQuery))
        {
            throw new DomainException("QUERY_REQUIRED", "query must not be blank", 422);
        }

        if (normalizedQuery.Length > 500)
        {
            throw new DomainException("QUERY_TOO_LONG", "query must not exceed 500 characters", 422);
        }

        var count = request.Count ?? 100;
        var start = request.Start ?? 0;
        if (count is < 1 or > 100)
        {
            throw new DomainException("INVALID_LIMIT", "count must be between 1 and 100", 422);
        }

        if (start < 0)
        {
            throw new DomainException("INVALID_OFFSET", "start must be non-negative", 422);
        }

        var normalizedLocale = NormalizeLocale(locale);
        var index = await repository.EnsureIndexAsync(context, true, ct);
        if (index.State == "Disabled")
        {
            throw new DomainException("SEARCH_UNAVAILABLE", "Search is disabled for this store", 503);
        }

        var result = await repository.SearchAsync(context, normalizedLocale, normalizedQuery, start, count, ct);
        return new SearchResultsResponseDto
        {
            Items = result.Items,
            Pagination = new PaginationInfoDto
            {
                Offset = start,
                Limit = count,
                TotalItems = result.Total,
                TotalPages = result.Total == 0 ? 0 : (result.Total + count - 1) / count
            }
        };
    }

    // @BR-CAT-020: Autocomplete refuses disabled or unavailable search instead of calling a missing provider.
    // @BR-CAT-024: Autocomplete returns no more than fifteen localized keyword suggestions.
    // @BR-EXT-024: Autocomplete delegates through the provider-neutral local PostgreSQL adapter.
    public async Task<AutocompleteResponseDto> AutocompleteAsync(
        AutocompleteRequestDto request, RequestContext context, string locale, CancellationToken ct)
    {
        RequireProvider();
        var normalizedQuery = request.Query?.Trim();
        if (string.IsNullOrWhiteSpace(normalizedQuery))
        {
            throw new DomainException("QUERY_REQUIRED", "query must not be blank", 422);
        }

        if (normalizedQuery.Length > 500)
        {
            throw new DomainException("QUERY_TOO_LONG", "query must not exceed 500 characters", 422);
        }

        var normalizedLocale = NormalizeLocale(locale);
        var index = await repository.EnsureIndexAsync(context, true, ct);
        if (index.State == "Disabled")
        {
            throw new DomainException("SEARCH_UNAVAILABLE", "Search is disabled for this store", 503);
        }

        return new AutocompleteResponseDto
        {
            Suggestions = await repository.AutocompleteAsync(context, normalizedLocale, normalizedQuery, 15, ct)
        };
    }

    // @BR-CAT-020: Rebuild scheduling uses the enabled provider boundary and records unavailable outcomes.
    // @BR-EXT-023: A disabled deployment rejects indexing before creating a provider mutation.
    // @BR-EXT-024: Rebuild configuration is persisted in the store-scoped search index.
    // @BR-CAT-032: A rebuild creates a durable Requested job and queues its asynchronous lifecycle.
    public async Task<RebuildAcceptedResponseDto> RequestRebuildAsync(
        RequestContext context, string requestedBy, string idempotencyKey, CancellationToken ct)
    {
        var key = idempotencyKey.Trim();
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new DomainException("IDEMPOTENCY_KEY_REQUIRED", "Idempotency-Key is required", 400);
        }

        var result = await repository.CreateRebuildAsync(context, requestedBy, key, _enabled && _providerAvailable, ct);
        if (result.Created && !queue.Enqueue(result.Job, context))
        {
            await repository.FailRebuildAsync(result.Job.Id, context, "REBUILD_QUEUE_FAILED",
                "The rebuild could not be scheduled", ct);
            throw new DomainException("REBUILD_SCHEDULING_FAILED", "The rebuild could not be scheduled", 500);
        }

        return new RebuildAcceptedResponseDto
        {
            RebuildId = result.Job.Id.ToString(),
            Status = RebuildStatusRegistry.Create(result.Job.State),
            Accepted = true,
            AcceptedAt = result.Job.RequestedAt.ToString("O")
        };
    }

    // @BR-CAT-032: The worker moves a durable rebuild through Running to Succeeded or Failed.
    // @BR-EXT-024: Rebuild failures use three bounded attempts before a terminal operational event.
    public async Task ProcessRebuildAsync(SearchRebuildJob job, RequestContext context, CancellationToken ct)
    {
        if (!await repository.ClaimRebuildAsync(job.Id, context, ct))
        {
            return;
        }

        try
        {
            var indexed = await WithRetriesAsync(
                () => repository.RebuildProjectionAsync(job.Id, context, ct), job.Id, ct);
            var eventId = await repository.CompleteRebuildAsync(job.Id, context, indexed, ct);
            await events.PublishPendingAsync(eventId, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !ct.IsCancellationRequested)
        {
            var eventId = await repository.FailRebuildAsync(job.Id, context, "REBUILD_FAILED", ex.Message, ct);
            await events.PublishPendingAsync(eventId, ct);
        }
    }

    // @BR-CAT-023: Product and component events refresh or remove the complete store-scoped product projection.
    // @BR-CAT-033: Component changes require an identity and merge the complete refreshed projection without dropping unrelated components.
    public async Task HandleProductChangedAsync(ProductChangedEvent change, RequestContext context, CancellationToken ct)
    {
        if (!_enabled || !_providerAvailable)
        {
            logger.LogInformation("Ignoring {EventType} while search indexing is disabled or unavailable.", change.EventType);
            return;
        }

        var componentEvent = change.EventType.Contains("Image", StringComparison.OrdinalIgnoreCase) ||
                             change.EventType.Contains("Attribute", StringComparison.OrdinalIgnoreCase);
        if (componentEvent && string.IsNullOrWhiteSpace(change.ComponentId))
        {
            throw new DomainException("COMPONENT_IDENTIFIER_REQUIRED",
                "A component identifier is required for component changes", 422);
        }

        try
        {
            await WithRetriesAsync(async () =>
            {
                if (change.Deleted || change.EventType.EndsWith("Deleted", StringComparison.OrdinalIgnoreCase))
                {
                    await repository.RemoveProductAsync(change.ProductId, context, ct);
                    return true;
                }

                if (change.Projection is null)
                {
                    throw new DomainException("PRODUCT_PROJECTION_REQUIRED",
                        "A complete product projection is required", 422);
                }

                await repository.UpsertProductAsync(change.Projection, context, ct);
                return true;
            }, change.ProductId, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !ct.IsCancellationRequested)
        {
            var eventId = await repository.RecordProjectionFailureAsync(
                context, change.ProductId, change.SourceVersion, "INDEX_PROJECTION_FAILED", ct);
            await events.PublishPendingAsync(eventId, ct);
            logger.LogError(ex, "Product projection {ProductId} reached terminal failure.", change.ProductId);
        }
    }

    private async Task<T> WithRetriesAsync<T>(Func<Task<T>> operation, object operationId, CancellationToken ct)
    {
        Exception? last = null;
        for (var attempt = 1; attempt <= 3; attempt++)
        {
            try
            {
                return await operation();
            }
            catch (Exception ex) when (ex is not OperationCanceledException || !ct.IsCancellationRequested)
            {
                last = ex;
                if (attempt < 3)
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(Math.Min(1000, 100 * Math.Pow(2, attempt - 1))), ct);
                }
            }
        }

        logger.LogError(last, "Search operation {OperationId} exhausted its three attempts.", operationId);
        throw last ?? new InvalidOperationException("Search operation failed.");
    }

    private void RequireProvider()
    {
        if (!_enabled)
        {
            throw new DomainException("SEARCH_UNAVAILABLE", "Search indexing is disabled", 503);
        }

        if (!_providerAvailable)
        {
            throw new DomainException("SEARCH_PROVIDER_UNAVAILABLE",
                "The configured search provider is unavailable", 503);
        }
    }

    private string NormalizeLocale(string? locale)
    {
        var value = (locale ?? "").Trim().ToLowerInvariant().Split(',', ';')[0].Trim();
        if (value.Contains('-', StringComparison.Ordinal))
        {
            value = value[..value.IndexOf('-', StringComparison.Ordinal)];
        }

        if (!_locales.Contains(value, StringComparer.OrdinalIgnoreCase))
        {
            throw new DomainException("LOCALE_UNSUPPORTED", "The requested locale is not configured", 422);
        }

        return value;
    }

    private static bool IsTrue(string? value) =>
        bool.TryParse(value, out var parsed) && parsed;

    private static bool IsFalse(string? value) =>
        bool.TryParse(value, out var parsed) && !parsed;

}

public sealed class SearchRebuildWorker(
    RebuildQueue queue,
    SearchService service,
    ILogger<SearchRebuildWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var item in queue.ReadAllAsync(stoppingToken))
        {
            try
            {
                await service.ProcessRebuildAsync(item.Job, item.Context, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Search rebuild worker failed for {RebuildId}.", item.Job.Id);
            }
        }
    }
}
