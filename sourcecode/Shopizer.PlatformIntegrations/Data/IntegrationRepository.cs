using System.Text.Json;
using Npgsql;
using NpgsqlTypes;
using Shopizer.PlatformIntegrations.Models;

namespace Shopizer.PlatformIntegrations.Data;

public sealed class IntegrationRepository(NpgsqlDataSource dataSource)
{
    private static void P(NpgsqlCommand c, string name, object? value) =>
        c.Parameters.AddWithValue(name, value ?? DBNull.Value);

    private static void Json(NpgsqlCommand c, string name, object value) =>
        c.Parameters.Add(name, NpgsqlDbType.Jsonb).Value = JsonSerializer.Serialize(value);

    public async Task<(IReadOnlyList<IntegrationEndpoint> Items, long Total)> ListEndpointsAsync(
        RequestContext ctx, string? type, string? environment, int page, int pageSize, CancellationToken ct)
    {
        await using var db = await dataSource.OpenConnectionAsync(ct);
        const string where = "tenant_id=@tenant AND store_id=@store AND status <> 'RETIRED' " +
                             "AND (@type IS NULL OR integration_type=@type) AND (@environment IS NULL OR environment=@environment)";
        await using var count = new NpgsqlCommand($"SELECT count(*) FROM platform_integrations.integration_endpoint WHERE {where}", db);
        P(count, "tenant", ctx.TenantId); P(count, "store", ctx.StoreId);
        count.Parameters.Add("type", NpgsqlDbType.Text).Value = (object?)type?.ToUpperInvariant() ?? DBNull.Value;
        count.Parameters.Add("environment", NpgsqlDbType.Text).Value = (object?)environment ?? DBNull.Value;
        var total = Convert.ToInt64(await count.ExecuteScalarAsync(ct));
        await using var cmd = new NpgsqlCommand(
            $"SELECT endpoint_id,tenant_id,store_id,integration_type,provider,code,environment,status,configuration_ref,endpoint_uri,capabilities,supplemental_configuration,timeout_ms,max_attempts FROM platform_integrations.integration_endpoint WHERE {where} ORDER BY code,environment OFFSET @offset LIMIT @limit", db);
        P(cmd, "tenant", ctx.TenantId); P(cmd, "store", ctx.StoreId);
        cmd.Parameters.Add("type", NpgsqlDbType.Text).Value = (object?)type?.ToUpperInvariant() ?? DBNull.Value;
        cmd.Parameters.Add("environment", NpgsqlDbType.Text).Value = (object?)environment ?? DBNull.Value;
        P(cmd, "offset", (Math.Max(1, page) - 1) * pageSize); P(cmd, "limit", pageSize);
        var result = new List<IntegrationEndpoint>();
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct)) result.Add(ReadEndpoint(reader));
        return (result, total);
    }

    public async Task<IntegrationEndpoint?> FindEndpointAsync(RequestContext ctx, string type, string code,
        string environment, CancellationToken ct)
    {
        await using var db = await dataSource.OpenConnectionAsync(ct);
        await using var cmd = new NpgsqlCommand(@"
SELECT endpoint_id,tenant_id,store_id,integration_type,provider,code,environment,status,configuration_ref,endpoint_uri,capabilities,supplemental_configuration,timeout_ms,max_attempts
            FROM platform_integrations.integration_endpoint WHERE tenant_id=@tenant AND store_id=@store AND integration_type=@type AND code=@code AND environment=@environment AND status='ACTIVE'
", db);
        P(cmd, "tenant", ctx.TenantId); P(cmd, "store", ctx.StoreId); P(cmd, "type", type.ToUpperInvariant()); P(cmd, "code", code); P(cmd, "environment", environment);
        await using var r = await cmd.ExecuteReaderAsync(ct);
        return await r.ReadAsync(ct) ? ReadEndpoint(r) : null;
    }

    public async Task UpdateConfigurationReferenceAsync(RequestContext ctx, string type, string code,
        string environment, string configurationRef, CancellationToken ct)
    {
        await using var db = await dataSource.OpenConnectionAsync(ct);
        await using var cmd = new NpgsqlCommand(
            "UPDATE platform_integrations.integration_endpoint SET configuration_ref=@reference,updated_at=current_timestamp WHERE tenant_id=@tenant AND store_id=@store AND integration_type=@type AND code=@code AND environment=@environment AND status='ACTIVE'",
            db);
        P(cmd, "reference", configurationRef); P(cmd, "tenant", ctx.TenantId); P(cmd, "store", ctx.StoreId);
        P(cmd, "type", type.ToUpperInvariant()); P(cmd, "code", code); P(cmd, "environment", environment);
        if (await cmd.ExecuteNonQueryAsync(ct) == 0)
            throw new DomainException("ADAPTER_CONFIGURATION_INVALID", "The referenced adapter projection was not found", 422);
    }

    // @BR-INT-MS12-002: A replacement retires the previous active projection and publishes the new one in one transaction.
    // @BR-INT-MS12-003: The endpoint projection is selected by its requested environment.
    // @BR-INT-MS12-004: config1 and config2 are persisted as independent supplemental settings.
    public async Task<IntegrationEndpoint> ReplaceEndpointAsync(IntegrationEndpoint endpoint, CancellationToken ct)
    {
        await using var db = await dataSource.OpenConnectionAsync(ct);
        await using var tx = await db.BeginTransactionAsync(ct);
        try
        {
            await using (var retire = new NpgsqlCommand(@"
UPDATE platform_integrations.integration_endpoint SET status='RETIRED',updated_at=current_timestamp
                WHERE tenant_id=@tenant AND store_id=@store AND integration_type=@type AND code=@code AND environment=@environment AND status='ACTIVE'", db, tx))
            {
                P(retire, "tenant", endpoint.TenantId); P(retire, "store", endpoint.StoreId); P(retire, "type", endpoint.IntegrationType);
                P(retire, "code", endpoint.Code); P(retire, "environment", endpoint.Environment);
                await retire.ExecuteNonQueryAsync(ct);
            }
            await using var insert = new NpgsqlCommand(@"
INSERT INTO platform_integrations.integration_endpoint
                (endpoint_id,tenant_id,store_id,integration_type,provider,code,environment,status,configuration_ref,endpoint_uri,capabilities,supplemental_configuration,timeout_ms,max_attempts)
                VALUES(@id,@tenant,@store,@type,@provider,@code,@environment,'ACTIVE',@configuration,@uri,@capabilities,@supplemental,@timeout,@attempts)", db, tx);
            P(insert, "id", endpoint.Id); P(insert, "tenant", endpoint.TenantId); P(insert, "store", endpoint.StoreId);
            P(insert, "type", endpoint.IntegrationType); P(insert, "provider", endpoint.Provider); P(insert, "code", endpoint.Code);
            P(insert, "environment", endpoint.Environment); P(insert, "configuration", endpoint.ConfigurationRef); P(insert, "uri", endpoint.EndpointUri);
            Json(insert, "capabilities", endpoint.Capabilities); Json(insert, "supplemental", endpoint.SupplementalConfiguration);
            P(insert, "timeout", endpoint.TimeoutMs); P(insert, "attempts", endpoint.MaxAttempts);
            await insert.ExecuteNonQueryAsync(ct);
            await tx.CommitAsync(ct);
            return endpoint;
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            await tx.RollbackAsync(ct);
            throw new DomainException("ADAPTER_UPDATE_CONFLICT", $"Adapter '{endpoint.Code}' was modified concurrently", 409);
        }
    }

    public async Task<IntegrationEndpoint> EnsureStorageEndpointAsync(RequestContext ctx, string provider, CancellationToken ct)
    {
        var existing = await FindEndpointAsync(ctx, "STORAGE", provider.ToLowerInvariant(), "DEFAULT", ct);
        if (existing is not null) return existing;
        var endpoint = new IntegrationEndpoint
        {
            Id = Guid.NewGuid(),
            TenantId = ctx.TenantId,
            StoreId = ctx.StoreId,
            IntegrationType = "STORAGE",
            Provider = provider.ToUpperInvariant(),
            Code = provider.ToLowerInvariant(),
            Environment = "DEFAULT",
            ConfigurationRef = $"internal://storage/{provider.ToLowerInvariant()}",
            Capabilities = new Dictionary<string, object?> { ["upload"] = true, ["read"] = provider.Equals("Local", StringComparison.OrdinalIgnoreCase) },
            MaxAttempts = 3
        };
        try { return await ReplaceEndpointAsync(endpoint, ct); }
        catch (DomainException ex) when (ex.StatusCode == 409)
        {
            var concurrent = await FindEndpointAsync(ctx, "STORAGE", endpoint.Code, endpoint.Environment, ct);
            return concurrent ?? throw new DomainException("ADAPTER_UPDATE_CONFLICT", "Storage endpoint could not be established", 409);
        }
    }

    public async Task<(DeliveryOperation Operation, IReadOnlyList<DeliveryAttempt> Attempts, EmailMessage? Email)> CreateDeliveryAsync(
        RequestContext ctx, string type, string key, string hash, IntegrationEndpoint endpoint,
        IReadOnlyList<(string ItemKey, object Payload, Guid? MessageId)> items, EmailMessage? email, CancellationToken ct)
    {
        await using var db = await dataSource.OpenConnectionAsync(ct);
        await using var tx = await db.BeginTransactionAsync(ct);
        var operationId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        await using (var op = new NpgsqlCommand(@"
INSERT INTO platform_integrations.delivery_idempotency
            (operation_id,tenant_id,store_id,operation_type,idempotency_key,request_hash,item_count,status)
            VALUES(@id,@tenant,@store,@type,@key,@hash,@count,'QUEUED')", db, tx))
        {
            P(op, "id", operationId); P(op, "tenant", ctx.TenantId); P(op, "store", ctx.StoreId); P(op, "type", type);
            P(op, "key", key); P(op, "hash", hash); P(op, "count", items.Count); await op.ExecuteNonQueryAsync(ct);
        }
        if (email is not null)
        {
            await using var message = new NpgsqlCommand(@"
INSERT INTO platform_integrations.email_message
              (message_id,operation_id,endpoint_id,tenant_id,store_id,idempotency_key,template_key,locale,recipient_email,sender_email,sender_name,subject,token_payload,status,order_reference)
              VALUES(@id,@operation,@endpoint,@tenant,@store,@key,@template,@locale,@recipient,@sender,@senderName,@subject,@payload,'QUEUED',@order)", db, tx);
            P(message, "id", email.MessageId); P(message, "operation", operationId); P(message, "endpoint", endpoint.Id);
            P(message, "tenant", ctx.TenantId); P(message, "store", ctx.StoreId); P(message, "key", email.IdempotencyKey);
            P(message, "template", email.TemplateKey); P(message, "locale", email.Locale); P(message, "recipient", email.RecipientEmail);
            P(message, "sender", email.SenderEmail); P(message, "senderName", email.SenderName); P(message, "subject", email.Subject);
            Json(message, "payload", email.TokenPayload); P(message, "order", email.OrderReference); await message.ExecuteNonQueryAsync(ct);
        }
        var attempts = new List<DeliveryAttempt>();
        foreach (var item in items)
        {
            var attempt = new DeliveryAttempt
            {
                AttemptId = Guid.NewGuid(),
                OperationId = operationId,
                EndpointId = endpoint.Id,
                MessageId = item.MessageId,
                TenantId = ctx.TenantId,
                StoreId = ctx.StoreId,
                OperationItemKey = item.ItemKey,
                AttemptNumber = 1
            };
            attempts.Add(attempt);
            await using var insert = new NpgsqlCommand(@"
INSERT INTO platform_integrations.delivery_attempt
              (attempt_id,operation_id,endpoint_id,message_id,tenant_id,store_id,operation_item_key,attempt_number,status,request_payload)
              VALUES(@id,@operation,@endpoint,@message,@tenant,@store,@item,1,'PENDING',@payload)", db, tx);
            P(insert, "id", attempt.AttemptId); P(insert, "operation", operationId); P(insert, "endpoint", endpoint.Id);
            P(insert, "message", item.MessageId); P(insert, "tenant", ctx.TenantId); P(insert, "store", ctx.StoreId); P(insert, "item", item.ItemKey);
            Json(insert, "payload", item.Payload); await insert.ExecuteNonQueryAsync(ct);
        }
        var eventId = Guid.NewGuid();
        var envelope = new
        {
            eventId,
            eventType = "IntegrationDeliveryQueued",
            eventVersion = 1,
            occurredAt = now,
            tenantId = ctx.TenantId,
            storeId = ctx.StoreId,
            correlationId = ctx.CorrelationId,
            operationId,
            attemptId = attempts[0].AttemptId,
            endpointId = endpoint.Id,
            idempotencyKey = key,
            availableAt = now,
            requestPayload = items.Select(x => x.Payload)
        };
        await using (var outbox = new NpgsqlCommand(@"
INSERT INTO platform_integrations.outbox_event
          (event_id,operation_id,tenant_id,store_id,event_type,aggregate_type,aggregate_id,payload,status)
          VALUES(@id,@operation,@tenant,@store,'IntegrationDeliveryQueued','DeliveryOperation',@aggregate,@payload,'PENDING')", db, tx))
        {
            P(outbox, "id", eventId); P(outbox, "operation", operationId); P(outbox, "tenant", ctx.TenantId); P(outbox, "store", ctx.StoreId);
            P(outbox, "aggregate", operationId); Json(outbox, "payload", envelope); await outbox.ExecuteNonQueryAsync(ct);
        }
        attempts[0].OutboxEventId = eventId;
        await using (var link = new NpgsqlCommand(
            "UPDATE platform_integrations.delivery_attempt SET outbox_event_id=@event WHERE attempt_id=@attempt", db, tx))
        {
            P(link, "event", eventId); P(link, "attempt", attempts[0].AttemptId);
            await link.ExecuteNonQueryAsync(ct);
        }
        await tx.CommitAsync(ct);
        return (new DeliveryOperation { OperationId = operationId, OperationType = type, IdempotencyKey = key, RequestHash = hash, ItemCount = items.Count, Status = "QUEUED", CreatedAt = now, UpdatedAt = now }, attempts, email);
    }

    public async Task<DeliveryOperation?> FindOperationAsync(RequestContext ctx, string key, CancellationToken ct)
    {
        await using var db = await dataSource.OpenConnectionAsync(ct);
        await using var cmd = new NpgsqlCommand(@"
SELECT operation_id,operation_type,idempotency_key,request_hash,item_count,status,created_at,updated_at,completed_at
            FROM platform_integrations.delivery_idempotency WHERE tenant_id=@tenant AND store_id=@store AND idempotency_key=@key
", db);
        P(cmd, "tenant", ctx.TenantId); P(cmd, "store", ctx.StoreId); P(cmd, "key", key);
        await using var r = await cmd.ExecuteReaderAsync(ct);
        if (!await r.ReadAsync(ct)) return null;
        return new DeliveryOperation
        {
            OperationId = r.GetGuid(0),
            OperationType = r.GetString(1),
            IdempotencyKey = r.GetString(2),
            RequestHash = r.GetString(3),
            ItemCount = r.GetInt32(4),
            Status = r.GetString(5),
            CreatedAt = r.GetFieldValue<DateTimeOffset>(6),
            UpdatedAt = r.GetFieldValue<DateTimeOffset>(7),
            CompletedAt = r.IsDBNull(8) ? null : r.GetFieldValue<DateTimeOffset>(8)
        };
    }

    public async Task<IReadOnlyList<DeliveryAttempt>> FindAttemptsAsync(Guid operationId, RequestContext ctx, CancellationToken ct)
    {
        await using var db = await dataSource.OpenConnectionAsync(ct);
        await using var cmd = new NpgsqlCommand(@"
SELECT attempt_id,operation_id,endpoint_id,message_id,tenant_id,store_id,operation_item_key,attempt_number,status,
            provider_request_ref,provider_outcome_code,provider_error_code,provider_error_message,next_attempt_at,attempted_at,completed_at,replay_of_attempt_id,outbox_event_id,dead_lettered_at
            FROM platform_integrations.delivery_attempt WHERE operation_id=@operation AND tenant_id=@tenant AND store_id=@store ORDER BY operation_item_key,attempt_number
", db);
        P(cmd, "operation", operationId); P(cmd, "tenant", ctx.TenantId); P(cmd, "store", ctx.StoreId);
        await using var r = await cmd.ExecuteReaderAsync(ct); var result = new List<DeliveryAttempt>();
        while (await r.ReadAsync(ct)) result.Add(ReadAttempt(r));
        return result;
    }

    // @BR-INT-MS12-022: Provider outcomes are durably recorded and retryable failures receive a new bounded attempt.
    public async Task SetAttemptResultAsync(Guid attemptId, string status, string? outcome, string? errorCode,
        string? errorMessage, DateTimeOffset? nextAttempt, CancellationToken ct)
    {
        await using var db = await dataSource.OpenConnectionAsync(ct);
        await using var cmd = new NpgsqlCommand(@"
UPDATE platform_integrations.delivery_attempt SET status=@status,
          provider_outcome_code=@outcome,provider_error_code=@errorCode,provider_error_message=@errorMessage,
          next_attempt_at=@next,attempted_at=coalesce(attempted_at,current_timestamp),
          completed_at=CASE WHEN @status IN ('SUCCEEDED','DEAD_LETTERED') THEN current_timestamp ELSE completed_at END,
          dead_lettered_at=CASE WHEN @status='DEAD_LETTERED' THEN current_timestamp ELSE dead_lettered_at END,updated_at=current_timestamp
          WHERE attempt_id=@id
", db);
        P(cmd, "id", attemptId); P(cmd, "status", status); P(cmd, "outcome", outcome); P(cmd, "errorCode", errorCode);
        P(cmd, "errorMessage", errorMessage); P(cmd, "next", nextAttempt); await cmd.ExecuteNonQueryAsync(ct);
        await using var operation = new NpgsqlCommand("""
            UPDATE platform_integrations.delivery_idempotency o
               SET status = CASE
                 WHEN @status='DEAD_LETTERED' THEN 'DEAD_LETTERED'
                 WHEN NOT EXISTS (
                   SELECT 1 FROM platform_integrations.delivery_attempt a
                    WHERE a.operation_id=o.operation_id AND a.status <> 'SUCCEEDED') THEN 'SUCCEEDED'
                 WHEN @status='FAILED' THEN 'FAILED'
                 ELSE o.status END,
                   updated_at=current_timestamp,
                   completed_at=CASE WHEN @status IN ('SUCCEEDED','DEAD_LETTERED') THEN current_timestamp ELSE o.completed_at END
             WHERE o.operation_id=(SELECT operation_id FROM platform_integrations.delivery_attempt WHERE attempt_id=@id)
            """, db);
        P(operation, "status", status); P(operation, "id", attemptId);
        await operation.ExecuteNonQueryAsync(ct);
    }

    public async Task MarkOutboxPublishedAsync(Guid eventId, CancellationToken ct)
    {
        await using var db = await dataSource.OpenConnectionAsync(ct);
        await using var cmd = new NpgsqlCommand(
            "UPDATE platform_integrations.outbox_event SET status='PUBLISHED',published_at=current_timestamp,updated_at=current_timestamp WHERE event_id=@id", db);
        P(cmd, "id", eventId);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task<DeliveryAttempt?> FindAttemptAsync(Guid id, RequestContext ctx, CancellationToken ct)
    {
        await using var db = await dataSource.OpenConnectionAsync(ct);
        await using var cmd = new NpgsqlCommand(@"
SELECT attempt_id,operation_id,endpoint_id,message_id,tenant_id,store_id,operation_item_key,attempt_number,status,
          provider_request_ref,provider_outcome_code,provider_error_code,provider_error_message,next_attempt_at,attempted_at,completed_at,replay_of_attempt_id,outbox_event_id,dead_lettered_at
          FROM platform_integrations.delivery_attempt WHERE attempt_id=@id AND tenant_id=@tenant AND store_id=@store
", db);
        P(cmd, "id", id); P(cmd, "tenant", ctx.TenantId); P(cmd, "store", ctx.StoreId);
        await using var r = await cmd.ExecuteReaderAsync(ct); return await r.ReadAsync(ct) ? ReadAttempt(r) : null;
    }

    // @BR-INT-MS12-023: Replay preserves the terminal original and creates a separately linked pending attempt.
    public async Task<DeliveryAttempt> ReplayAsync(DeliveryAttempt original, string reason, RequestContext ctx, CancellationToken ct)
    {
        if (original.Status is not ("FAILED" or "DEAD_LETTERED"))
            throw new DomainException("DELIVERY_REPLAY_NOT_ALLOWED", "Only failed or dead-lettered attempts can be replayed", 409);
        await using var db = await dataSource.OpenConnectionAsync(ct);
        await using var tx = await db.BeginTransactionAsync(ct);
        var next = new DeliveryAttempt
        {
            AttemptId = Guid.NewGuid(),
            OperationId = original.OperationId,
            EndpointId = original.EndpointId,
            MessageId = original.MessageId,
            TenantId = ctx.TenantId,
            StoreId = ctx.StoreId,
            OperationItemKey = original.OperationItemKey,
            AttemptNumber = original.AttemptNumber + 1,
            ReplayOfAttemptId = original.AttemptId
        };
        await using var cmd = new NpgsqlCommand(@"
INSERT INTO platform_integrations.delivery_attempt
          (attempt_id,operation_id,endpoint_id,message_id,tenant_id,store_id,operation_item_key,attempt_number,status,replay_of_attempt_id,request_payload)
          SELECT @id,operation_id,endpoint_id,message_id,tenant_id,store_id,operation_item_key,@number,'PENDING',@replay,request_payload
          FROM platform_integrations.delivery_attempt WHERE attempt_id=@original", db, tx);
        P(cmd, "id", next.AttemptId); P(cmd, "number", next.AttemptNumber); P(cmd, "replay", original.AttemptId); P(cmd, "original", original.AttemptId);
        if (await cmd.ExecuteNonQueryAsync(ct) != 1) throw new DomainException("DELIVERY_ATTEMPT_NOT_FOUND", "Delivery attempt was not found", 404);
        await using var outbox = new NpgsqlCommand(@"
INSERT INTO platform_integrations.outbox_event
          (event_id,operation_id,tenant_id,store_id,event_type,aggregate_type,aggregate_id,payload,status)
          VALUES(@id,@operation,@tenant,@store,'IntegrationDeliveryQueued','DeliveryAttempt',@attempt,@payload,'PENDING')", db, tx);
        var eventId = Guid.NewGuid(); next.OutboxEventId = eventId;
        P(outbox, "id", eventId); P(outbox, "operation", original.OperationId); P(outbox, "tenant", ctx.TenantId); P(outbox, "store", ctx.StoreId); P(outbox, "attempt", next.AttemptId);
        Json(outbox, "payload", new { eventId, eventType = "IntegrationDeliveryReplayRequested", eventVersion = 1, occurredAt = DateTimeOffset.UtcNow, tenantId = ctx.TenantId, storeId = ctx.StoreId, correlationId = ctx.CorrelationId, originalAttemptId = original.AttemptId, reason });
        await outbox.ExecuteNonQueryAsync(ct);
        await using var link = new NpgsqlCommand("UPDATE platform_integrations.delivery_attempt SET outbox_event_id=@event WHERE attempt_id=@attempt", db, tx);
        P(link, "event", eventId); P(link, "attempt", next.AttemptId); await link.ExecuteNonQueryAsync(ct);
        await tx.CommitAsync(ct);
        return next;
    }

    private static IntegrationEndpoint ReadEndpoint(NpgsqlDataReader r) => new()
    {
        Id = r.GetGuid(0),
        TenantId = r.GetString(1),
        StoreId = r.GetString(2),
        IntegrationType = r.GetString(3),
        Provider = r.GetString(4),
        Code = r.GetString(5),
        Environment = r.GetString(6),
        Status = r.GetString(7),
        ConfigurationRef = r.GetString(8),
        EndpointUri = r.IsDBNull(9) ? null : r.GetString(9),
        Capabilities = ReadJson(r, 10),
        SupplementalConfiguration = ReadJson(r, 11),
        TimeoutMs = r.GetInt32(12),
        MaxAttempts = r.GetInt32(13)
    };

    private static DeliveryAttempt ReadAttempt(NpgsqlDataReader r) => new()
    {
        AttemptId = r.GetGuid(0),
        OperationId = r.GetGuid(1),
        EndpointId = r.GetGuid(2),
        MessageId = r.IsDBNull(3) ? null : r.GetGuid(3),
        TenantId = r.GetString(4),
        StoreId = r.GetString(5),
        OperationItemKey = r.GetString(6),
        AttemptNumber = r.GetInt32(7),
        Status = r.GetString(8),
        ProviderRequestRef = r.IsDBNull(9) ? null : r.GetString(9),
        ProviderOutcomeCode = r.IsDBNull(10) ? null : r.GetString(10),
        ProviderErrorCode = r.IsDBNull(11) ? null : r.GetString(11),
        ProviderErrorMessage = r.IsDBNull(12) ? null : r.GetString(12),
        NextAttemptAt = r.IsDBNull(13) ? null : r.GetFieldValue<DateTimeOffset>(13),
        AttemptedAt = r.IsDBNull(14) ? null : r.GetFieldValue<DateTimeOffset>(14),
        CompletedAt = r.IsDBNull(15) ? null : r.GetFieldValue<DateTimeOffset>(15),
        ReplayOfAttemptId = r.IsDBNull(16) ? null : r.GetGuid(16),
        OutboxEventId = r.IsDBNull(17) ? null : r.GetGuid(17),
        DeadLetteredAt = r.IsDBNull(18) ? null : r.GetFieldValue<DateTimeOffset>(18)
    };

    private static Dictionary<string, object?> ReadJson(NpgsqlDataReader r, int index)
    {
        if (r.IsDBNull(index)) return new();
        try { return JsonSerializer.Deserialize<Dictionary<string, object?>>(r.GetString(index)) ?? new(); }
        catch (InvalidCastException) { return JsonSerializer.Deserialize<Dictionary<string, object?>>(r.GetFieldValue<JsonDocument>(index).RootElement.GetRawText()) ?? new(); }
    }
}
