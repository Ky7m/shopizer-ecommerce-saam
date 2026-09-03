using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Npgsql;
using NpgsqlTypes;
using Shopizer.Search.DTOs;
using Shopizer.Search.Models;

namespace Shopizer.Search.Data;

public sealed class SearchRepository(NpgsqlDataSource dataSource, IConfiguration configuration)
{
    private readonly string[] _locales = ReadLocales(configuration);
    private readonly string _provider = configuration["Search:Provider"] ?? "local-postgresql";

    public static Guid TenantKey(string value)
    {
        if (Guid.TryParse(value, out var id))
        {
            return id;
        }

        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        bytes[6] = (byte)((bytes[6] & 0x0F) | 0x50);
        bytes[8] = (byte)((bytes[8] & 0x3F) | 0x80);
        return new Guid(bytes[..16]);
    }

    // @BR-CAT-020: The store index records provider configuration and its enabled operational state.
    // @BR-EXT-024: Provider-neutral locale query profiles are persisted without coupling to an external provider.
    public async Task<SearchIndex> EnsureIndexAsync(RequestContext context, bool enabled, CancellationToken ct)
    {
        var tenant = TenantKey(context.TenantId);
        await using var connection = await dataSource.OpenConnectionAsync(ct);
        await using (var command = new NpgsqlCommand("""
            INSERT INTO search.search_index
                (tenant_id, store_id, provider_name, configured_locales, state)
            VALUES (@tenant, @store, @provider, @locales, @state)
            ON CONFLICT (tenant_id, store_id) DO NOTHING
            """, connection))
        {
            Add(command, "tenant", tenant);
            Add(command, "store", context.StoreId);
            Add(command, "provider", _provider);
            AddLocales(command, "locales", _locales);
            Add(command, "state", enabled ? "Configured" : "Disabled");
            await command.ExecuteNonQueryAsync(ct);
        }
        await EnsureQueryProfilesAsync(connection, null, context, ct);

        await using var select = new NpgsqlCommand("""
            SELECT search_index_id, tenant_id, store_id, provider_name,
                   configured_locales, configuration_version, state
            FROM search.search_index
            WHERE tenant_id=@tenant AND store_id=@store
            """, connection);
        Add(select, "tenant", tenant);
        Add(select, "store", context.StoreId);
        await using var reader = await select.ExecuteReaderAsync(ct);
        if (!await reader.ReadAsync(ct))
        {
            throw new InvalidOperationException("The search index could not be initialized.");
        }

        return ReadIndex(reader);
    }

    public async Task<(List<SearchResultItemDto> Items, int Total)> SearchAsync(
        RequestContext context, string locale, string query, int start, int count, CancellationToken ct)
    {
        var tenant = TenantKey(context.TenantId);
        await using var connection = await dataSource.OpenConnectionAsync(ct);
        await using var totalCommand = new NpgsqlCommand("""
            SELECT COUNT(DISTINCT d.document_id)
            FROM search.search_document d
            JOIN search.search_index i ON i.search_index_id=d.search_index_id
            JOIN search.search_document_locale l ON l.document_id=d.document_id
            WHERE i.tenant_id=@tenant AND i.store_id=@store AND i.state <> 'Disabled'
              AND d.tenant_id=@tenant AND d.store_id=@store AND d.locale=@locale
              AND d.state='Active'
              AND (l.name ILIKE '%' || @query || '%'
                   OR COALESCE(l.description,'') ILIKE '%' || @query || '%'
                   OR COALESCE(l.brand_name,'') ILIKE '%' || @query || '%'
                   OR COALESCE(l.category_name,'') ILIKE '%' || @query || '%'
                   OR l.attributes::text ILIKE '%' || @query || '%')
            """, connection);
        AddScope(totalCommand, context, tenant);
        Add(totalCommand, "locale", locale);
        Add(totalCommand, "query", query);
        var total = Convert.ToInt32(await totalCommand.ExecuteScalarAsync(ct) ?? 0);

        await using var command = new NpgsqlCommand("""
            SELECT d.product_id, d.locale, l.name, l.description, l.product_link,
                   l.brand_name, l.category_name, l.image_url, l.review_average,
                   inv.sku, inv.variant_sku, inv.quantity, inv.price,
                   inv.discounted_price, inv.option_values::text
            FROM search.search_document d
            JOIN search.search_index i ON i.search_index_id=d.search_index_id
            JOIN search.search_document_locale l ON l.document_id=d.document_id
            LEFT JOIN search.search_document_inventory inv ON inv.document_id=d.document_id
            WHERE i.tenant_id=@tenant AND i.store_id=@store AND i.state <> 'Disabled'
              AND d.tenant_id=@tenant AND d.store_id=@store AND d.locale=@locale
              AND d.state='Active'
              AND (l.name ILIKE '%' || @query || '%'
                   OR COALESCE(l.description,'') ILIKE '%' || @query || '%'
                   OR COALESCE(l.brand_name,'') ILIKE '%' || @query || '%'
                   OR COALESCE(l.category_name,'') ILIKE '%' || @query || '%'
                   OR l.attributes::text ILIKE '%' || @query || '%')
            ORDER BY d.product_id, d.document_id
            """, connection);
        AddScope(command, context, tenant);
        Add(command, "locale", locale);
        Add(command, "query", query);

        var items = new List<SearchResultItemDto>();
        var byProduct = new Dictionary<long, SearchResultItemDto>();
        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            var productId = reader.GetInt64(0);
            if (!byProduct.TryGetValue(productId, out var item))
            {
                item = new SearchResultItemDto
                {
                    ProductId = productId,
                    Locale = reader.GetString(1),
                    Name = reader.GetString(2),
                    Description = reader.IsDBNull(3) ? null : reader.GetString(3),
                    ProductLink = reader.IsDBNull(4) ? null : reader.GetString(4),
                    BrandName = reader.IsDBNull(5) ? null : reader.GetString(5),
                    CategoryName = reader.IsDBNull(6) ? null : reader.GetString(6),
                    ImageUrl = reader.IsDBNull(7) ? null : reader.GetString(7),
                    ReviewAverage = reader.IsDBNull(8) ? null : reader.GetDecimal(8)
                };
                byProduct[productId] = item;
                items.Add(item);
            }

            if (!reader.IsDBNull(9))
            {
                item.Inventory.Add(new SearchInventoryEntryDto
                {
                    Sku = reader.GetString(9),
                    VariantSku = reader.IsDBNull(10) ? null : reader.GetString(10),
                    Quantity = reader.GetDecimal(11),
                    Price = reader.GetDecimal(12),
                    DiscountedPrice = reader.IsDBNull(13) ? null : reader.GetDecimal(13),
                    OptionValues = DeserializeDictionary(reader.IsDBNull(14) ? "{}" : reader.GetString(14))
                });
            }
        }

        // Inventory is a one-to-many projection, so pagination must be applied after
        // rows have been merged into product documents rather than to joined rows.
        return (items.Skip(start).Take(count).ToList(), total);
    }

    public async Task<List<string>> AutocompleteAsync(
        RequestContext context, string locale, string query, int limit, CancellationToken ct)
    {
        var tenant = TenantKey(context.TenantId);
        await using var connection = await dataSource.OpenConnectionAsync(ct);
        await using var command = new NpgsqlCommand("""
            SELECT DISTINCT l.name
            FROM search.search_document d
            JOIN search.search_index i ON i.search_index_id=d.search_index_id
            JOIN search.search_document_locale l ON l.document_id=d.document_id
            WHERE i.tenant_id=@tenant AND i.store_id=@store AND i.state <> 'Disabled'
              AND d.tenant_id=@tenant AND d.store_id=@store AND d.locale=@locale
              AND d.state='Active' AND l.name ILIKE '%' || @query || '%'
            ORDER BY l.name
            LIMIT @limit
            """, connection);
        AddScope(command, context, tenant);
        Add(command, "locale", locale);
        Add(command, "query", query);
        Add(command, "limit", limit);
        var suggestions = new List<string>();
        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            suggestions.Add(reader.GetString(0));
        }

        return suggestions;
    }

    // @BR-CAT-032: Rebuild idempotency and the initial Requested state are committed atomically with index Building.
    public async Task<(SearchRebuildJob Job, bool Created)> CreateRebuildAsync(
        RequestContext context, string requestedBy, string idempotencyKey, bool enabled, CancellationToken ct)
    {
        if (!enabled)
        {
            throw new DomainException("INDEXING_DISABLED", "Search indexing is disabled", 409);
        }

        var tenant = TenantKey(context.TenantId);
        await using var connection = await dataSource.OpenConnectionAsync(ct);
        await using var transaction = await connection.BeginTransactionAsync(ct);
        var index = await EnsureIndexInTransactionAsync(connection, transaction, context, enabled, ct);
        if (index.State == "Disabled")
        {
            throw new DomainException("INDEXING_DISABLED", "Search indexing is disabled", 409);
        }

        var existing = await FindJobAsync(connection, transaction, index.Id, idempotencyKey, ct);
        if (existing is not null)
        {
            if (existing.State is "Requested" or "Running")
            {
                throw new DomainException("REBUILD_ALREADY_RUNNING",
                    $"A rebuild is already running ({existing.Id})", 409);
            }

            await transaction.CommitAsync(ct);
            return (existing, false);
        }

        if (await HasActiveJobAsync(connection, transaction, index.Id, ct))
        {
            throw new DomainException("REBUILD_ALREADY_RUNNING",
                "A search rebuild is already running for this store", 409);
        }

        var jobId = Guid.NewGuid();
        await using (var insert = new NpgsqlCommand("""
            INSERT INTO search.search_rebuild_job
                (rebuild_job_id, search_index_id, tenant_id, store_id, requested_by, idempotency_key, state)
            VALUES (@id, @index, @tenant, @store, @requested, @key, 'Requested')
            """, connection, transaction))
        {
            Add(insert, "id", jobId);
            Add(insert, "index", index.Id);
            Add(insert, "tenant", tenant);
            Add(insert, "store", context.StoreId);
            Add(insert, "requested", requestedBy);
            Add(insert, "key", idempotencyKey);
            await insert.ExecuteNonQueryAsync(ct);
            await EnsureQueryProfilesAsync(connection, transaction, context, ct);
        }

        await using (var update = new NpgsqlCommand("""
            UPDATE search.search_index SET state='Building', updated_at=now()
            WHERE search_index_id=@index AND tenant_id=@tenant AND store_id=@store
            """, connection, transaction))
        {
            Add(update, "index", index.Id);
            Add(update, "tenant", tenant);
            Add(update, "store", context.StoreId);
            await update.ExecuteNonQueryAsync(ct);
        }

        await transaction.CommitAsync(ct);
        return (new SearchRebuildJob
        {
            Id = jobId, SearchIndexId = index.Id, TenantId = context.TenantId,
            StoreId = context.StoreId, RequestedBy = requestedBy,
            IdempotencyKey = idempotencyKey, State = "Requested",
            RequestedAt = DateTimeOffset.UtcNow
        }, true);
    }

    public async Task<bool> ClaimRebuildAsync(Guid jobId, RequestContext context, CancellationToken ct)
    {
        await using var connection = await dataSource.OpenConnectionAsync(ct);
        await using var command = new NpgsqlCommand("""
            UPDATE search.search_rebuild_job
            SET state='Running', started_at=COALESCE(started_at, now()), updated_at=now()
            WHERE rebuild_job_id=@id AND tenant_id=@tenant AND store_id=@store AND state='Requested'
            """, connection);
        Add(command, "id", jobId);
        AddScope(command, context, TenantKey(context.TenantId));
        return await command.ExecuteNonQueryAsync(ct) == 1;
    }

    // @BR-CAT-032: Rebuilding replays every active durable product document and returns a persisted count.
    public async Task<long> RebuildProjectionAsync(Guid jobId, RequestContext context, CancellationToken ct)
    {
        await using var connection = await dataSource.OpenConnectionAsync(ct);
        await using var command = new NpgsqlCommand("""
            UPDATE search.search_document d
            SET indexed_at=now(), updated_at=now(), state='Active'
            FROM search.search_rebuild_job j
            WHERE j.rebuild_job_id=@job AND d.search_index_id=j.search_index_id
              AND d.tenant_id=@tenant AND d.store_id=@store AND d.state='Active'
            """, connection);
        Add(command, "job", jobId);
        AddScope(command, context, TenantKey(context.TenantId));
        await command.ExecuteNonQueryAsync(ct);

        await using var count = new NpgsqlCommand("""
            SELECT COUNT(*) FROM search.search_document d
            WHERE d.tenant_id=@tenant AND d.store_id=@store AND d.state='Active'
              AND d.search_index_id=(SELECT search_index_id FROM search.search_rebuild_job WHERE rebuild_job_id=@job)
            """, connection);
        Add(count, "job", jobId);
        AddScope(count, context, TenantKey(context.TenantId));
        return Convert.ToInt64(await count.ExecuteScalarAsync(ct) ?? 0L);
    }

    public async Task<Guid> CompleteRebuildAsync(Guid jobId, RequestContext context, long count, CancellationToken ct) =>
        await FinishRebuildAsync(jobId, context, "Succeeded", count, null, null, "SearchRebuildCompleted.v1", ct);

    public async Task<Guid> FailRebuildAsync(Guid jobId, RequestContext context, string code, string message, CancellationToken ct) =>
        await FinishRebuildAsync(jobId, context, "Failed", 0, 1, code, "SearchIndexingFailed.v1", ct, message);

    public async Task<Guid> RecordProjectionFailureAsync(
        RequestContext context, long productId, long? sourceVersion, string code, CancellationToken ct)
    {
        await using var connection = await dataSource.OpenConnectionAsync(ct);
        await using var transaction = await connection.BeginTransactionAsync(ct);
        var eventId = Guid.NewGuid();
        await using var command = new NpgsqlCommand("""
            INSERT INTO search.event_outbox
                (id, event_type, tenant_id, store_id, correlation_id, payload, occurred_at)
            VALUES (@id, 'SearchIndexingFailed.v1', @tenant, @store, @correlation, @payload, now())
            """, connection, transaction);
        Add(command, "id", eventId);
        Add(command, "tenant", context.TenantId);
        Add(command, "store", context.StoreId);
        Add(command, "correlation", context.CorrelationId);
        AddJson(command, "payload", new
        {
            eventId, eventType = "SearchIndexingFailed.v1", eventVersion = 1,
            occurredAt = DateTimeOffset.UtcNow, tenantId = context.TenantId,
            storeId = context.StoreId, correlationId = context.CorrelationId,
            productId = productId.ToString(), rebuildId = (Guid?)null,
            sourceVersion, failureCode = code
        });
        await command.ExecuteNonQueryAsync(ct);
        await transaction.CommitAsync(ct);
        return eventId;
    }

    // @BR-CAT-021: Each current product locale is replaced by one store-scoped search document.
    // @BR-CAT-022: The document stores localized merchandising data and all non-negative inventory entries.
    public async Task UpsertProductAsync(ProductProjection projection, RequestContext context, CancellationToken ct)
    {
        if (projection.Locales.Count == 0)
        {
            throw new DomainException("DOCUMENT_LOCALE_REQUIRED",
                "At least one localized product description is required", 422);
        }

        var index = await EnsureIndexAsync(context, true, ct);
        await using var connection = await dataSource.OpenConnectionAsync(ct);
        await using var transaction = await connection.BeginTransactionAsync(ct);
        await DeleteProductRowsAsync(connection, transaction, index.Id, context, projection.ProductId, ct);

        foreach (var locale in projection.Locales)
        {
            var documentId = Guid.NewGuid();
            await using (var insert = new NpgsqlCommand("""
                INSERT INTO search.search_document
                    (document_id, search_index_id, tenant_id, store_id, product_id, locale,
                     provider_document_key, state, source_version, indexed_at)
                VALUES (@id,@index,@tenant,@store,@product,@locale,@key,'Active',@version,now())
                """, connection, transaction))
            {
                Add(insert, "id", documentId);
                Add(insert, "index", index.Id);
                Add(insert, "tenant", TenantKey(context.TenantId));
                Add(insert, "store", context.StoreId);
                Add(insert, "product", projection.ProductId);
                Add(insert, "locale", locale.Locale);
                Add(insert, "key", $"{context.StoreId}:{projection.ProductId}:{locale.Locale}");
                Add(insert, "version", projection.SourceVersion);
                await insert.ExecuteNonQueryAsync(ct);
            }

            await using (var fields = new NpgsqlCommand("""
                INSERT INTO search.search_document_locale
                    (document_id,name,description,product_link,brand_name,category_name,
                     attributes,image_url,review_average)
                VALUES (@id,@name,@description,@link,@brand,@category,@attributes,@image,@review)
                """, connection, transaction))
            {
                Add(fields, "id", documentId);
                Add(fields, "name", locale.Name);
                Add(fields, "description", locale.Description);
                Add(fields, "link", locale.ProductLink);
                Add(fields, "brand", locale.BrandName);
                Add(fields, "category", locale.CategoryName);
                AddJson(fields, "attributes", locale.Attributes);
                Add(fields, "image", locale.ImageUrl);
                Add(fields, "review", locale.ReviewAverage);
                await fields.ExecuteNonQueryAsync(ct);
            }

            foreach (var inventory in projection.Inventory)
            {
                await using var entry = new NpgsqlCommand("""
                    INSERT INTO search.search_document_inventory
                        (document_id,sku,variant_sku,quantity,price,discounted_price,option_values)
                    VALUES (@document,@sku,@variant,@quantity,@price,@discounted,@options)
                    """, connection, transaction);
                Add(entry, "document", documentId);
                Add(entry, "sku", inventory.Sku);
                Add(entry, "variant", inventory.VariantSku);
                Add(entry, "quantity", inventory.Quantity);
                Add(entry, "price", inventory.Price);
                Add(entry, "discounted", inventory.DiscountedPrice);
                AddJson(entry, "options", inventory.OptionValues);
                await entry.ExecuteNonQueryAsync(ct);
            }
        }

        await transaction.CommitAsync(ct);
    }

    // @BR-CAT-023: Product removal marks every localized document removed within the tenant/store boundary.
    public async Task RemoveProductAsync(long productId, RequestContext context, CancellationToken ct)
    {
        var tenant = TenantKey(context.TenantId);
        await using var connection = await dataSource.OpenConnectionAsync(ct);
        await using var command = new NpgsqlCommand("""
            UPDATE search.search_document
            SET state='Removed', updated_at=now()
            WHERE tenant_id=@tenant AND store_id=@store AND product_id=@product
            """, connection);
        Add(command, "tenant", tenant);
        Add(command, "store", context.StoreId);
        Add(command, "product", productId);
        await command.ExecuteNonQueryAsync(ct);
    }

    private async Task<Guid> FinishRebuildAsync(Guid jobId, RequestContext context, string state, long indexed,
        long? failed, string? errorCode, string eventType, CancellationToken ct, string? errorMessage = null)
    {
        await using var connection = await dataSource.OpenConnectionAsync(ct);
        await using var transaction = await connection.BeginTransactionAsync(ct);
        var tenant = TenantKey(context.TenantId);
        await using (var update = new NpgsqlCommand("""
            UPDATE search.search_rebuild_job
            SET state=@state, indexed_document_count=@indexed,
                failed_document_count=COALESCE(@failed, failed_document_count),
                error_code=@code, error_message=@message, completed_at=now(), updated_at=now()
            WHERE rebuild_job_id=@job AND tenant_id=@tenant AND store_id=@store
            """, connection, transaction))
        {
            Add(update, "state", state);
            Add(update, "indexed", indexed);
            Add(update, "failed", failed);
            Add(update, "code", errorCode);
            Add(update, "message", errorMessage);
            Add(update, "job", jobId);
            AddScope(update, context, tenant);
            await update.ExecuteNonQueryAsync(ct);
        }

        await using (var index = new NpgsqlCommand("""
            UPDATE search.search_index SET state=@state, last_success_at=
              CASE WHEN @state='Ready' THEN now() ELSE last_success_at END,
              last_failure_at=CASE WHEN @state='Degraded' THEN now() ELSE last_failure_at END,
              last_failure_code=CASE WHEN @state='Degraded' THEN @code ELSE last_failure_code END,
              updated_at=now()
            WHERE tenant_id=@tenant AND store_id=@store
            """, connection, transaction))
        {
            Add(index, "state", state == "Succeeded" ? "Ready" : "Degraded");
            Add(index, "code", errorCode);
            Add(index, "tenant", tenant);
            Add(index, "store", context.StoreId);
            await index.ExecuteNonQueryAsync(ct);
        }

        var eventId = Guid.NewGuid();
        await using (var outbox = new NpgsqlCommand("""
            INSERT INTO search.event_outbox
                (id,event_type,tenant_id,store_id,correlation_id,payload,occurred_at)
            VALUES (@id,@type,@tenantText,@store,@correlation,@payload,now())
            """, connection, transaction))
        {
            Add(outbox, "id", eventId);
            Add(outbox, "type", eventType);
            Add(outbox, "tenantText", context.TenantId);
            Add(outbox, "store", context.StoreId);
            Add(outbox, "correlation", context.CorrelationId);
            object payload = state == "Succeeded"
                ? new
                {
                    eventId, eventType, eventVersion = 1, occurredAt = DateTimeOffset.UtcNow,
                    tenantId = context.TenantId, storeId = context.StoreId,
                    correlationId = context.CorrelationId, rebuildId = jobId, status = "Succeeded"
                }
                : new
                {
                    eventId, eventType, eventVersion = 1, occurredAt = DateTimeOffset.UtcNow,
                    tenantId = context.TenantId, storeId = context.StoreId,
                    correlationId = context.CorrelationId, productId = (string?)null,
                    rebuildId = jobId, failureCode = errorCode ?? "REBUILD_FAILED"
                };
            AddJson(outbox, "payload", payload);
            await outbox.ExecuteNonQueryAsync(ct);
        }

        await transaction.CommitAsync(ct);
        return eventId;
    }

    private async Task<SearchIndex> EnsureIndexInTransactionAsync(
        NpgsqlConnection connection, NpgsqlTransaction transaction, RequestContext context, bool enabled, CancellationToken ct)
    {
        await using var insert = new NpgsqlCommand("""
            INSERT INTO search.search_index (tenant_id,store_id,provider_name,configured_locales,state)
            VALUES (@tenant,@store,@provider,@locales,@state)
            ON CONFLICT (tenant_id,store_id) DO NOTHING
            """, connection, transaction);
        Add(insert, "tenant", TenantKey(context.TenantId));
        Add(insert, "store", context.StoreId);
        Add(insert, "provider", _provider);
        AddLocales(insert, "locales", _locales);
        Add(insert, "state", enabled ? "Configured" : "Disabled");
        await insert.ExecuteNonQueryAsync(ct);
        await using var select = new NpgsqlCommand("""
            SELECT search_index_id,tenant_id,store_id,provider_name,configured_locales,
                   configuration_version,state
            FROM search.search_index WHERE tenant_id=@tenant AND store_id=@store FOR UPDATE
            """, connection, transaction);
        Add(select, "tenant", TenantKey(context.TenantId));
        Add(select, "store", context.StoreId);
        await using var reader = await select.ExecuteReaderAsync(ct);
        return await reader.ReadAsync(ct)
            ? ReadIndex(reader)
            : throw new InvalidOperationException("The search index could not be initialized.");
    }

    private static async Task<SearchRebuildJob?> FindJobAsync(
        NpgsqlConnection connection, NpgsqlTransaction transaction, Guid indexId, string key, CancellationToken ct)
    {
        await using var command = new NpgsqlCommand("""
            SELECT rebuild_job_id,search_index_id,tenant_id,store_id,requested_by,idempotency_key,
                   state,requested_at,started_at,completed_at,indexed_document_count,
                   failed_document_count,error_code
            FROM search.search_rebuild_job
            WHERE search_index_id=@index AND idempotency_key=@key
            """, connection, transaction);
        Add(command, "index", indexId);
        Add(command, "key", key);
        await using var reader = await command.ExecuteReaderAsync(ct);
        return await reader.ReadAsync(ct) ? ReadJob(reader) : null;
    }

    private async Task EnsureQueryProfilesAsync(
        NpgsqlConnection connection, NpgsqlTransaction? transaction,
        RequestContext context, CancellationToken ct)
    {
        var indexId = await GetIndexIdAsync(connection, transaction, context, ct);
        foreach (var locale in _locales)
        {
            await using var command = new NpgsqlCommand("""
                INSERT INTO search.search_query_profile
                    (search_index_id,locale,provider_query_name,product_mapping_version,
                     keyword_mapping_version)
                VALUES (@index,@locale,@query,'local-v1','local-v1')
                ON CONFLICT (search_index_id,locale) DO NOTHING
                """, connection, transaction);
            Add(command, "index", indexId);
            Add(command, "locale", locale);
            Add(command, "query", $"{_provider}-products-{locale}");
            await command.ExecuteNonQueryAsync(ct);
        }
    }

    private static async Task<Guid> GetIndexIdAsync(
        NpgsqlConnection connection, NpgsqlTransaction? transaction,
        RequestContext context, CancellationToken ct)
    {
        await using var command = new NpgsqlCommand("""
            SELECT search_index_id FROM search.search_index
            WHERE tenant_id=@tenant AND store_id=@store
            """, connection, transaction);
        Add(command, "tenant", TenantKey(context.TenantId));
        Add(command, "store", context.StoreId);
        return (Guid)(await command.ExecuteScalarAsync(ct) ??
                      throw new InvalidOperationException("The search index could not be initialized."));
    }

    private static async Task<bool> HasActiveJobAsync(
        NpgsqlConnection connection, NpgsqlTransaction transaction, Guid indexId, CancellationToken ct)
    {
        await using var command = new NpgsqlCommand("""
            SELECT EXISTS(SELECT 1 FROM search.search_rebuild_job
              WHERE search_index_id=@index AND state IN ('Requested','Running'))
            """, connection, transaction);
        Add(command, "index", indexId);
        return (bool)(await command.ExecuteScalarAsync(ct) ?? false);
    }

    private static async Task DeleteProductRowsAsync(
        NpgsqlConnection connection, NpgsqlTransaction transaction, Guid indexId,
        RequestContext context, long productId, CancellationToken ct)
    {
        await using var command = new NpgsqlCommand("""
            DELETE FROM search.search_document
            WHERE search_index_id=@index AND tenant_id=@tenant AND store_id=@store AND product_id=@product
            """, connection, transaction);
        Add(command, "index", indexId);
        Add(command, "tenant", TenantKey(context.TenantId));
        Add(command, "store", context.StoreId);
        Add(command, "product", productId);
        await command.ExecuteNonQueryAsync(ct);
    }

    private static SearchIndex ReadIndex(NpgsqlDataReader reader) => new()
    {
        Id = reader.GetGuid(0), TenantId = reader.GetGuid(1).ToString(),
        StoreId = reader.GetString(2), ProviderName = reader.GetString(3),
        ConfiguredLocales = reader.GetFieldValue<string[]>(4),
        ConfigurationVersion = reader.GetInt64(5), State = reader.GetString(6)
    };

    private static SearchRebuildJob ReadJob(NpgsqlDataReader reader) => new()
    {
        Id = reader.GetGuid(0), SearchIndexId = reader.GetGuid(1),
        TenantId = reader.GetGuid(2).ToString(), StoreId = reader.GetString(3),
        RequestedBy = reader.GetString(4), IdempotencyKey = reader.GetString(5),
        State = reader.GetString(6), RequestedAt = reader.GetFieldValue<DateTimeOffset>(7),
        StartedAt = reader.IsDBNull(8) ? null : reader.GetFieldValue<DateTimeOffset>(8),
        CompletedAt = reader.IsDBNull(9) ? null : reader.GetFieldValue<DateTimeOffset>(9),
        IndexedDocumentCount = reader.GetInt64(10), FailedDocumentCount = reader.GetInt64(11),
        ErrorCode = reader.IsDBNull(12) ? null : reader.GetString(12)
    };

    private static string[] ReadLocales(IConfiguration configuration) =>
        (configuration.GetSection("Search:Locales").Get<string[]>() ?? ["en"])
        .Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim().ToLowerInvariant()).Distinct().ToArray();

    private static Dictionary<string, object?> DeserializeDictionary(string json) =>
        JsonSerializer.Deserialize<Dictionary<string, object?>>(json) ?? [];

    private static void AddScope(NpgsqlCommand command, RequestContext context, Guid tenant)
    {
        Add(command, "tenant", tenant);
        Add(command, "store", context.StoreId);
    }

    private static void AddLocales(NpgsqlCommand command, string name, string[] locales) =>
        command.Parameters.Add(name, NpgsqlDbType.Array | NpgsqlDbType.Text).Value = locales;

    private static void AddJson(NpgsqlCommand command, string name, object value) =>
        command.Parameters.Add(name, NpgsqlDbType.Jsonb).Value = JsonSerializer.Serialize(value);

    private static void Add(NpgsqlCommand command, string name, object? value) =>
        command.Parameters.AddWithValue(name, value ?? DBNull.Value);
}
