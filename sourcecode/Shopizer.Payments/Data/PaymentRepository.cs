using System.Text.Json;
using Npgsql;
using NpgsqlTypes;
using Shopizer.Payments.Models;

namespace Shopizer.Payments.Data;

public sealed class PaymentRepository(NpgsqlDataSource dataSource)
{
    public async Task<IReadOnlyList<PaymentMethodConfiguration>> ListMethodsAsync(RequestContext context, int page, int pageSize, CancellationToken ct)
    {
        await using var connection = await dataSource.OpenConnectionAsync(ct);
        await using var command = new NpgsqlCommand("""
            SELECT code,provider_code,active,default_selected,configurable,environment,configuration_version,
                   secret_reference,public_configuration,regions
            FROM payments.payment_method_configuration
            WHERE tenant_id=@tenant AND store_id=@store
            ORDER BY default_selected DESC, code
            OFFSET @offset LIMIT @limit
            """, connection);
        AddContext(command, context);
        command.Parameters.AddWithValue("offset", (page - 1) * pageSize);
        command.Parameters.AddWithValue("limit", pageSize);
        return await ReadMethodsAsync(command, ct);
    }

    public async Task<long> CountMethodsAsync(RequestContext context, CancellationToken ct)
    {
        await using var connection = await dataSource.OpenConnectionAsync(ct);
        await using var command = new NpgsqlCommand(
            "SELECT COUNT(*) FROM payments.payment_method_configuration WHERE tenant_id=@tenant AND store_id=@store", connection);
        AddContext(command, context);
        return (long)(await command.ExecuteScalarAsync(ct) ?? 0L);
    }

    public async Task<PaymentMethodConfiguration?> GetMethodAsync(string code, RequestContext context, CancellationToken ct)
    {
        await using var connection = await dataSource.OpenConnectionAsync(ct);
        await using var command = new NpgsqlCommand("""
            SELECT code,provider_code,active,default_selected,configurable,environment,configuration_version,
                   secret_reference,public_configuration,regions
            FROM payments.payment_method_configuration
            WHERE tenant_id=@tenant AND store_id=@store AND code=@code
            """, connection);
        AddContext(command, context);
        command.Parameters.AddWithValue("code", code);
        var methods = await ReadMethodsAsync(command, ct);
        return methods.SingleOrDefault();
    }

    public async Task<PaymentMethodConfiguration> UpsertMethodAsync(
        string code, ConfigureMethodValues values, RequestContext context, CancellationToken ct)
    {
        await using var connection = await dataSource.OpenConnectionAsync(ct);
        await using var command = new NpgsqlCommand("""
            INSERT INTO payments.payment_method_configuration
              (tenant_id,store_id,code,provider_code,active,default_selected,configurable,environment,
               configuration_version,secret_reference,public_configuration,regions)
            VALUES (@tenant,@store,@code,@provider,@active,@default,@configurable,@environment,@version,@secret,@public,@regions)
            ON CONFLICT (tenant_id,store_id,code) DO UPDATE SET
              active=EXCLUDED.active, default_selected=EXCLUDED.default_selected,
              environment=EXCLUDED.environment, configuration_version=EXCLUDED.configuration_version,
              secret_reference=EXCLUDED.secret_reference, public_configuration=EXCLUDED.public_configuration,
              regions=EXCLUDED.regions, updated_at=now()
            RETURNING code,provider_code,active,default_selected,configurable,environment,configuration_version,
                      secret_reference,public_configuration,regions
            """, connection);
        AddContext(command, context);
        command.Parameters.AddWithValue("code", code);
        command.Parameters.AddWithValue("provider", code);
        command.Parameters.AddWithValue("active", values.Active);
        command.Parameters.AddWithValue("default", values.DefaultSelected);
        command.Parameters.AddWithValue("configurable", true);
        command.Parameters.AddWithValue("environment", values.Environment);
        command.Parameters.AddWithValue("version", values.ConfigurationVersion ?? 1L);
        command.Parameters.AddWithValue("secret", values.SecretReference);
        command.Parameters.Add(new NpgsqlParameter("public", NpgsqlDbType.Jsonb) { Value = JsonSerializer.Serialize(values.PublicConfiguration) });
        command.Parameters.Add(new NpgsqlParameter("regions", NpgsqlDbType.Array | NpgsqlDbType.Text) { Value = new[] { "*" } });
        await using var reader = await command.ExecuteReaderAsync(ct);
        return await reader.ReadAsync(ct) ? ReadMethod(reader) :
            throw new InvalidOperationException("Provider configuration was not persisted.");
    }

    public async Task<PaymentIntent?> FindIntentAsync(Guid id, RequestContext context, CancellationToken ct, bool forUpdate = false, NpgsqlConnection? connection = null, NpgsqlTransaction? transaction = null)
    {
        var ownsConnection = connection is null;
        connection ??= await dataSource.OpenConnectionAsync(ct);
        try
        {
            await using var command = new NpgsqlCommand($"""
                SELECT payment_intent_id,tenant_id,store_id,checkout_session_id,order_id,provider_code,
                       provider_config_version,amount,currency_code,status,authorized_amount,captured_amount,
                       client_secret_reference,created_at,updated_at,created_by,correlation_id
                FROM payments.payment_intent
                WHERE payment_intent_id=@id AND tenant_id=@tenant AND store_id=@store
                {(forUpdate ? "FOR UPDATE" : "")}
                """, connection, transaction);
            command.Parameters.AddWithValue("id", id);
            AddContext(command, context);
            await using var reader = await command.ExecuteReaderAsync(ct);
            return await reader.ReadAsync(ct) ? ReadIntent(reader) : null;
        }
        finally
        {
            if (ownsConnection) await connection.DisposeAsync();
        }
    }

    public async Task<PaymentIntent> CreateIntentAsync(PaymentIntent intent, RequestContext context, string idempotencyKey, string fingerprint, CancellationToken ct)
    {
        await using var connection = await dataSource.OpenConnectionAsync(ct);
        await using var transaction = await connection.BeginTransactionAsync(ct);
        await using (var command = new NpgsqlCommand("""
            INSERT INTO payments.payment_intent
              (payment_intent_id,tenant_id,store_id,checkout_session_id,order_id,provider_code,provider_config_version,
               amount,currency_code,status,authorized_amount,captured_amount,client_secret_reference,created_at,updated_at,created_by,correlation_id)
            VALUES (@id,@tenant,@store,@session,@order,@provider,@version,@amount,@currency,@status,0,0,@secret,@created,@updated,@createdBy,@correlation)
            """, connection, transaction))
        {
            command.Parameters.AddWithValue("id", intent.Id);
            AddContext(command, context);
            command.Parameters.AddWithValue("session", intent.CheckoutSessionId);
            command.Parameters.AddWithValue("order", (object?)intent.OrderId ?? DBNull.Value);
            command.Parameters.AddWithValue("provider", intent.ProviderCode);
            command.Parameters.AddWithValue("version", intent.ProviderConfigVersion);
            command.Parameters.AddWithValue("amount", intent.Amount);
            command.Parameters.AddWithValue("currency", intent.Currency);
            command.Parameters.AddWithValue("status", intent.Status);
            command.Parameters.AddWithValue("secret", (object?)intent.ClientSecretReference ?? DBNull.Value);
            command.Parameters.AddWithValue("created", intent.CreatedAt);
            command.Parameters.AddWithValue("updated", intent.UpdatedAt);
            command.Parameters.AddWithValue("createdBy", (object?)intent.CreatedBy ?? DBNull.Value);
            command.Parameters.AddWithValue("correlation", context.CorrelationId);
            await command.ExecuteNonQueryAsync(ct);
        }

        var operation = NewOperation(intent.Id, "Initialize", intent.Amount, intent.Currency, idempotencyKey, context);
        await InsertOperationAsync(connection, transaction, operation, fingerprint, context, ct);
        operation.Status = "Succeeded";
        operation.ProviderAttemptId = Guid.NewGuid();
        operation.CompletedAt = DateTimeOffset.UtcNow;
        await UpdateOperationAsync(connection, transaction, operation, ct);
        await InsertTransactionAsync(connection, transaction, intent, operation,
            new ProviderResult(true, "Initialized", null, ProviderStatus: "initialized"),
            "Initialize", "Succeeded", intent.Amount, ct);
        await transaction.CommitAsync(ct);
        return intent;
    }

    public async Task<PaymentOperation?> FindOperationAsync(Guid intentId, string operationType, string key, RequestContext context, CancellationToken ct)
    {
        await using var connection = await dataSource.OpenConnectionAsync(ct);
        await using var command = new NpgsqlCommand("""
            SELECT payment_operation_id,payment_intent_id,operation_type,status,requested_amount,currency_code,
                   idempotency_key,provider_attempt_id,provider_reference,failure_code,failure_message,
                   created_at,completed_at,correlation_id
            FROM payments.payment_operation
            WHERE tenant_id=@tenant AND store_id=@store AND payment_intent_id=@intent
              AND operation_type=@type AND idempotency_key=@key
            """, connection);
        AddContext(command, context);
        command.Parameters.AddWithValue("intent", intentId);
        command.Parameters.AddWithValue("type", operationType);
        command.Parameters.AddWithValue("key", key);
        await using var reader = await command.ExecuteReaderAsync(ct);
        return await reader.ReadAsync(ct) ? ReadOperation(reader) : null;
    }

    public async Task<(PaymentIntent Intent, PaymentOperation Operation)?> FindInitializationAsync(
        string key, RequestContext context, CancellationToken ct)
    {
        await using var connection = await dataSource.OpenConnectionAsync(ct);
        await using var command = new NpgsqlCommand("""
            SELECT i.payment_intent_id,i.tenant_id,i.store_id,i.checkout_session_id,i.order_id,i.provider_code,
                   i.provider_config_version,i.amount,i.currency_code,i.status,i.authorized_amount,i.captured_amount,
                   i.client_secret_reference,i.created_at,i.updated_at,i.created_by,i.correlation_id,
                   o.payment_operation_id,o.payment_intent_id,o.operation_type,o.status,o.requested_amount,o.currency_code,
                   o.idempotency_key,o.provider_attempt_id,o.provider_reference,o.failure_code,o.failure_message,
                   o.created_at,o.completed_at,o.correlation_id
            FROM payments.payment_operation o
            JOIN payments.payment_intent i ON i.payment_intent_id=o.payment_intent_id
            WHERE o.tenant_id=@tenant AND o.store_id=@store AND o.operation_type='Initialize' AND o.idempotency_key=@key
            """, connection);
        AddContext(command, context);
        command.Parameters.AddWithValue("key", key);
        await using var reader = await command.ExecuteReaderAsync(ct);
        if (!await reader.ReadAsync(ct)) return null;
        return (ReadIntent(reader), ReadOperationAt(reader, 18));
    }

    public async Task<PaymentOperation> AddAuthorizationAsync(
        PaymentIntent intent, PaymentOperation operation, ProviderResult result, string fingerprint, RequestContext context, CancellationToken ct)
    {
        await using var connection = await dataSource.OpenConnectionAsync(ct);
        await using var transaction = await connection.BeginTransactionAsync(ct);
        var locked = await FindIntentAsync(intent.Id, context, ct, true, connection, transaction)
                     ?? throw NotFound("PAYMENT_INTENT_NOT_FOUND", "Payment intent was not found");
        await InsertOperationAsync(connection, transaction, operation, fingerprint, context, ct);
        operation.Status = result.Succeeded ? "Succeeded" : "Failed";
        operation.ProviderAttemptId = Guid.NewGuid();
        operation.ProviderReference = result.Reference;
        operation.FailureCode = result.FailureCode;
        operation.FailureMessage = result.FailureMessage;
        operation.CompletedAt = DateTimeOffset.UtcNow;
        await UpdateOperationAsync(connection, transaction, operation, ct);

        var now = DateTimeOffset.UtcNow;
        var newStatus = result.Succeeded
            ? (locked.ProviderCode.Equals("stripe3", StringComparison.OrdinalIgnoreCase) && string.IsNullOrWhiteSpace(result.Reference)
                ? "RequiresAction" : "Authorized")
            : "Failed";
        if (result.Succeeded)
        {
            locked.Status = newStatus;
            locked.AuthorizedAmount = locked.Amount;
            locked.ClientSecretReference ??= result.ClientSecret;
            locked.UpdatedAt = now;
        }
        else
        {
            locked.Status = "Failed";
            locked.UpdatedAt = now;
        }
        await UpdateIntentAsync(connection, transaction, locked, context, ct);
        await InsertTransactionAsync(connection, transaction, locked, operation, result,
            "Authorize", result.Succeeded ? "Succeeded" : "Failed", locked.Amount, ct);
        await InsertOutboxAsync(connection, transaction, locked, result.Succeeded ? "PaymentAuthorized.v1" : "PaymentFailed.v1",
            new
            {
                paymentIntentId = locked.Id,
                orderId = locked.OrderId,
                amount = locked.Amount,
                currency = locked.Currency,
                providerReference = result.Reference,
                status = locked.Status
            }, context, ct);
        await transaction.CommitAsync(ct);
        return operation;
    }

    public async Task<PaymentOperation> AddCaptureAsync(
        PaymentIntent intent, PaymentOperation operation, ProviderResult result, string fingerprint, RequestContext context, CancellationToken ct)
    {
        await using var connection = await dataSource.OpenConnectionAsync(ct);
        await using var transaction = await connection.BeginTransactionAsync(ct);
        var locked = await FindIntentAsync(intent.Id, context, ct, true, connection, transaction)
                     ?? throw NotFound("PAYMENT_INTENT_NOT_FOUND", "Payment intent was not found");
        await InsertOperationAsync(connection, transaction, operation, fingerprint, context, ct);
        operation.Status = result.Succeeded ? "Succeeded" : "Failed";
        operation.ProviderAttemptId = Guid.NewGuid();
        operation.ProviderReference = result.Reference;
        operation.FailureCode = result.FailureCode;
        operation.FailureMessage = result.FailureMessage;
        operation.CompletedAt = DateTimeOffset.UtcNow;
        await UpdateOperationAsync(connection, transaction, operation, ct);
        locked.Status = result.Succeeded ? "Captured" : "Failed";
        if (result.Succeeded) locked.CapturedAmount += operation.RequestedAmount;
        locked.UpdatedAt = DateTimeOffset.UtcNow;
        await UpdateIntentAsync(connection, transaction, locked, context, ct);
        await InsertTransactionAsync(connection, transaction, locked, operation, result,
            "Capture", result.Succeeded ? "Succeeded" : "Failed", operation.RequestedAmount, ct);
        await InsertOutboxAsync(connection, transaction, locked, result.Succeeded ? "PaymentCaptured.v1" : "PaymentFailed.v1",
            new
            {
                paymentIntentId = locked.Id,
                orderId = locked.OrderId,
                amount = operation.RequestedAmount,
                currency = locked.Currency,
                providerReference = result.Reference,
                status = locked.Status
            }, context, ct);
        await transaction.CommitAsync(ct);
        return operation;
    }

    public async Task<(PaymentIntent Intent, PaymentRefund Refund, PaymentOperation Operation)> ReserveRefundAsync(
        PaymentIntent intent, decimal amount, string currency, string key, string fingerprint, RequestContext context, CancellationToken ct)
    {
        await using var connection = await dataSource.OpenConnectionAsync(ct);
        await using var transaction = await connection.BeginTransactionAsync(ct);
        var locked = await FindIntentAsync(intent.Id, context, ct, true, connection, transaction)
                     ?? throw NotFound("PAYMENT_INTENT_NOT_FOUND", "Payment intent was not found");
        var remaining = await RefundableBalanceAsync(connection, transaction, locked.Id, locked.CapturedAmount, ct);
        if (amount > remaining)
            throw new DomainException("REFUND_EXCEEDS_REMAINING_BALANCE",
                $"Only {remaining:0.00} {locked.Currency} remains refundable", 422);
        var operation = NewOperation(locked.Id, "Refund", amount, currency, key, context);
        await InsertOperationAsync(connection, transaction, operation, fingerprint, context, ct);
        var refund = new PaymentRefund
        {
            Id = Guid.NewGuid(),
            PaymentIntentId = locked.Id,
            PaymentOperationId = operation.Id,
            Amount = amount,
            Currency = currency,
            RequestedAt = DateTimeOffset.UtcNow,
            CorrelationId = context.CorrelationId
        };
        await using (var command = new NpgsqlCommand("""
            INSERT INTO payments.payment_refund(payment_refund_id,payment_intent_id,payment_operation_id,tenant_id,store_id,
              amount,currency_code,status,requested_at,correlation_id)
            VALUES(@id,@intent,@operation,@tenant,@store,@amount,@currency,'Reserved',@requested,@correlation)
            """, connection, transaction))
        {
            command.Parameters.AddWithValue("id", refund.Id);
            command.Parameters.AddWithValue("intent", refund.PaymentIntentId);
            command.Parameters.AddWithValue("operation", refund.PaymentOperationId);
            AddContext(command, context);
            command.Parameters.AddWithValue("amount", amount);
            command.Parameters.AddWithValue("currency", currency);
            command.Parameters.AddWithValue("requested", refund.RequestedAt);
            command.Parameters.AddWithValue("correlation", context.CorrelationId);
            await command.ExecuteNonQueryAsync(ct);
        }
        await transaction.CommitAsync(ct);
        return (locked, refund, operation);
    }

    public async Task<(PaymentIntent Intent, PaymentRefund Refund, PaymentOperation Operation)> CompleteRefundAsync(
        Guid refundId, Guid operationId, ProviderResult result, RequestContext context, CancellationToken ct)
    {
        await using var connection = await dataSource.OpenConnectionAsync(ct);
        await using var transaction = await connection.BeginTransactionAsync(ct);
        PaymentRefund refund;
        await using (var command = new NpgsqlCommand("""
            SELECT payment_refund_id,payment_intent_id,payment_operation_id,amount,currency_code,status,
                   provider_reference,requested_at,completed_at,correlation_id
            FROM payments.payment_refund WHERE payment_refund_id=@id AND tenant_id=@tenant AND store_id=@store FOR UPDATE
            """, connection, transaction))
        {
            command.Parameters.AddWithValue("id", refundId);
            AddContext(command, context);
            await using var reader = await command.ExecuteReaderAsync(ct);
            refund = await reader.ReadAsync(ct) ? ReadRefund(reader) :
                throw NotFound("REFUND_NOT_FOUND", "Refund reservation was not found");
        }
        var intent = await FindIntentAsync(refund.PaymentIntentId, context, ct, true, connection, transaction)
                     ?? throw NotFound("PAYMENT_INTENT_NOT_FOUND", "Payment intent was not found");
        var operation = await FindOperationInTransactionAsync(operationId, context, connection, transaction, ct)
                        ?? throw NotFound("PAYMENT_OPERATION_NOT_FOUND", "Payment operation was not found");
        refund.Status = result.Succeeded ? "Succeeded" : "Released";
        refund.ProviderReference = result.Reference;
        refund.CompletedAt = DateTimeOffset.UtcNow;
        operation.Status = result.Succeeded ? "Succeeded" : "Failed";
        operation.ProviderAttemptId = Guid.NewGuid();
        operation.ProviderReference = result.Reference;
        operation.FailureCode = result.FailureCode;
        operation.FailureMessage = result.FailureMessage;
        operation.CompletedAt = DateTimeOffset.UtcNow;
        await UpdateRefundAsync(connection, transaction, refund, context, ct);
        await UpdateOperationAsync(connection, transaction, operation, ct);
        if (result.Succeeded)
        {
            var balance = await RefundableBalanceAsync(connection, transaction, intent.Id, intent.CapturedAmount, ct);
            intent.RefundableAmount = balance;
            intent.Status = balance == 0 ? "Refunded" : "PartiallyRefunded";
            intent.UpdatedAt = DateTimeOffset.UtcNow;
            await UpdateIntentAsync(connection, transaction, intent, context, ct);
            await InsertTransactionAsync(connection, transaction, intent, operation, result, "Refund", "Succeeded", refund.Amount, ct);
            await InsertOutboxAsync(connection, transaction, intent, "PaymentRefunded.v1",
                new
                {
                    paymentIntentId = intent.Id,
                    orderId = intent.OrderId,
                    amount = refund.Amount,
                    currency = refund.Currency,
                    providerReference = result.Reference,
                    status = intent.Status
                }, context, ct);
        }
        else
        {
            await InsertOutboxAsync(connection, transaction, intent, "PaymentFailed.v1",
                new
                {
                    paymentIntentId = intent.Id,
                    amount = refund.Amount,
                    currency = refund.Currency,
                    providerReference = result.Reference,
                    failureCode = result.FailureCode
                }, context, ct);
        }
        await transaction.CommitAsync(ct);
        return (intent, refund, operation);
    }

    public async Task<IReadOnlyList<PaymentTransaction>> ListTransactionsAsync(Guid intentId, RequestContext context, int page, int pageSize, CancellationToken ct)
    {
        await using var connection = await dataSource.OpenConnectionAsync(ct);
        await using var command = new NpgsqlCommand("""
            SELECT payment_transaction_id,payment_intent_id,payment_operation_id,operation_type,status,amount,
                   currency_code,provider_code,provider_reference,provider_status,provider_correlation_id,
                   provider_details,occurred_at,sequence_no,created_at,correlation_id
            FROM payments.payment_transaction
            WHERE payment_intent_id=@intent AND tenant_id=@tenant AND store_id=@store
            ORDER BY sequence_no ASC, occurred_at ASC, payment_transaction_id ASC
            OFFSET @offset LIMIT @limit
            """, connection);
        command.Parameters.AddWithValue("intent", intentId);
        AddContext(command, context);
        command.Parameters.AddWithValue("offset", (page - 1) * pageSize);
        command.Parameters.AddWithValue("limit", pageSize);
        var list = new List<PaymentTransaction>();
        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct)) list.Add(ReadTransaction(reader));
        return list;
    }

    public async Task<long> CountTransactionsAsync(Guid intentId, RequestContext context, CancellationToken ct)
    {
        await using var connection = await dataSource.OpenConnectionAsync(ct);
        await using var command = new NpgsqlCommand("""
            SELECT COUNT(*) FROM payments.payment_transaction
            WHERE payment_intent_id=@intent AND tenant_id=@tenant AND store_id=@store
            """, connection);
        command.Parameters.AddWithValue("intent", intentId);
        AddContext(command, context);
        return (long)(await command.ExecuteScalarAsync(ct) ?? 0L);
    }

    public async Task<PaymentOperation?> FindOperationAsync(Guid id, RequestContext context, CancellationToken ct)
    {
        await using var connection = await dataSource.OpenConnectionAsync(ct);
        return await FindOperationInTransactionAsync(id, context, connection, null, ct);
    }

    public async Task<string?> GetFingerprintAsync(Guid operationId, RequestContext context, CancellationToken ct)
    {
        await using var connection = await dataSource.OpenConnectionAsync(ct);
        await using var command = new NpgsqlCommand("""
            SELECT request_fingerprint FROM payments.payment_operation
            WHERE payment_operation_id=@id AND tenant_id=@tenant AND store_id=@store
            """, connection);
        command.Parameters.AddWithValue("id", operationId);
        AddContext(command, context);
        return (string?)await command.ExecuteScalarAsync(ct);
    }

    public async Task<decimal> GetRefundableBalanceAsync(Guid intentId, decimal capturedAmount, RequestContext context, CancellationToken ct)
    {
        await using var connection = await dataSource.OpenConnectionAsync(ct);
        await using var command = new NpgsqlCommand("""
            SELECT GREATEST(0, @captured - COALESCE(SUM(r.amount) FILTER (WHERE r.status IN ('Reserved','Succeeded')),0))
            FROM payments.payment_refund r
            WHERE r.payment_intent_id=@intent AND r.tenant_id=@tenant AND r.store_id=@store
            """, connection);
        command.Parameters.AddWithValue("captured", capturedAmount);
        command.Parameters.AddWithValue("intent", intentId);
        AddContext(command, context);
        return (decimal)(await command.ExecuteScalarAsync(ct) ?? 0m);
    }

    public async Task<PaymentRefund?> FindRefundByOperationAsync(Guid operationId, RequestContext context, CancellationToken ct)
    {
        await using var connection = await dataSource.OpenConnectionAsync(ct);
        await using var command = new NpgsqlCommand("""
            SELECT payment_refund_id,payment_intent_id,payment_operation_id,amount,currency_code,status,
                   provider_reference,requested_at,completed_at,correlation_id
            FROM payments.payment_refund
            WHERE payment_operation_id=@operation AND tenant_id=@tenant AND store_id=@store
            """, connection);
        command.Parameters.AddWithValue("operation", operationId);
        AddContext(command, context);
        await using var reader = await command.ExecuteReaderAsync(ct);
        return await reader.ReadAsync(ct) ? ReadRefund(reader) : null;
    }

    public async Task<IReadOnlyList<(PaymentIntent Intent, DateTimeOffset AuthorizedAt)>> CapturableAsync(
        DateTimeOffset? from, DateTimeOffset? to, RequestContext context, int page, int pageSize, CancellationToken ct)
    {
        await using var connection = await dataSource.OpenConnectionAsync(ct);
        await using var command = new NpgsqlCommand("""
            SELECT i.payment_intent_id,i.tenant_id,i.store_id,i.checkout_session_id,i.order_id,i.provider_code,
                   i.provider_config_version,i.amount,i.currency_code,i.status,i.authorized_amount,i.captured_amount,
                   i.client_secret_reference,i.created_at,i.updated_at,i.created_by,i.correlation_id,
                   MAX(t.occurred_at) FILTER (WHERE t.operation_type='Authorize' AND t.status='Succeeded') AS authorized_at
            FROM payments.payment_intent i
            JOIN payments.payment_transaction t ON t.payment_intent_id=i.payment_intent_id
            WHERE i.tenant_id=@tenant AND i.store_id=@store AND i.status IN ('Authorized','CapturePending')
            GROUP BY i.payment_intent_id
            HAVING (CAST(@from AS timestamptz) IS NULL OR MAX(t.occurred_at) FILTER (WHERE t.operation_type='Authorize' AND t.status='Succeeded') >= CAST(@from AS timestamptz))
               AND (CAST(@to AS timestamptz) IS NULL OR MAX(t.occurred_at) FILTER (WHERE t.operation_type='Authorize' AND t.status='Succeeded') <= CAST(@to AS timestamptz))
            ORDER BY authorized_at ASC, i.payment_intent_id
            OFFSET @offset LIMIT @limit
            """, connection);
        AddContext(command, context);
        command.Parameters.AddWithValue("from", (object?)from ?? DBNull.Value);
        command.Parameters.AddWithValue("to", (object?)to ?? DBNull.Value);
        command.Parameters.AddWithValue("offset", (page - 1) * pageSize);
        command.Parameters.AddWithValue("limit", pageSize);
        var list = new List<(PaymentIntent, DateTimeOffset)>();
        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
            list.Add((ReadIntent(reader), reader.GetFieldValue<DateTimeOffset>(17)));
        return list;
    }

    public async Task<long> CountCapturableAsync(DateTimeOffset? from, DateTimeOffset? to, RequestContext context, CancellationToken ct)
    {
        await using var connection = await dataSource.OpenConnectionAsync(ct);
        await using var command = new NpgsqlCommand("""
            SELECT COUNT(*) FROM payments.payment_intent i
            WHERE i.tenant_id=@tenant AND i.store_id=@store AND i.status IN ('Authorized','CapturePending')
            """, connection);
        AddContext(command, context);
        return (long)(await command.ExecuteScalarAsync(ct) ?? 0L);
    }

    public async Task<CallbackWriteResult> StoreCallbackAsync(
        string provider, string? eventId, string? providerReference, Guid? intentId, string verification,
        string processing, string hash, Dictionary<string, object?> payload, RequestContext context, CancellationToken ct)
    {
        await using var connection = await dataSource.OpenConnectionAsync(ct);
        var id = Guid.NewGuid();
        await using var command = new NpgsqlCommand("""
            INSERT INTO payments.payment_callback(payment_callback_id,tenant_id,store_id,provider_code,provider_event_id,
              provider_reference,payment_intent_id,verification_status,processing_status,payload_hash,protected_payload,correlation_id)
            VALUES(@id,@tenant,@store,@provider,@event,@reference,@intent,@verification,@processing,@hash,@payload,@correlation)
            ON CONFLICT (provider_code,provider_event_id) WHERE provider_event_id IS NOT NULL
            DO NOTHING
            RETURNING payment_callback_id
            """, connection);
        command.Parameters.AddWithValue("id", id);
        AddContext(command, context);
        command.Parameters.AddWithValue("provider", provider);
        command.Parameters.AddWithValue("event", (object?)eventId ?? DBNull.Value);
        command.Parameters.AddWithValue("reference", (object?)providerReference ?? DBNull.Value);
        command.Parameters.AddWithValue("intent", (object?)intentId ?? DBNull.Value);
        command.Parameters.AddWithValue("verification", verification);
        command.Parameters.AddWithValue("processing", processing);
        command.Parameters.AddWithValue("hash", hash);
        command.Parameters.Add(new NpgsqlParameter("payload", NpgsqlDbType.Jsonb) { Value = JsonSerializer.Serialize(payload) });
        command.Parameters.AddWithValue("correlation", context.CorrelationId);
        var returned = await command.ExecuteScalarAsync(ct);
        return returned is null ? new CallbackWriteResult(id, true) : new CallbackWriteResult((Guid)returned, false);
    }

    public async Task<PaymentIntent?> FindByProviderReferenceAsync(string provider, string reference, RequestContext context, CancellationToken ct)
    {
        await using var connection = await dataSource.OpenConnectionAsync(ct);
        await using var command = new NpgsqlCommand("""
            SELECT i.payment_intent_id,i.tenant_id,i.store_id,i.checkout_session_id,i.order_id,i.provider_code,
                   i.provider_config_version,i.amount,i.currency_code,i.status,i.authorized_amount,i.captured_amount,
                   i.client_secret_reference,i.created_at,i.updated_at,i.created_by,i.correlation_id
            FROM payments.payment_intent i
            JOIN payments.payment_provider_reference r ON r.payment_intent_id=i.payment_intent_id
            WHERE i.tenant_id=@tenant AND i.store_id=@store AND r.provider_code=@provider AND r.provider_reference=@reference
            """, connection);
        AddContext(command, context);
        command.Parameters.AddWithValue("provider", provider);
        command.Parameters.AddWithValue("reference", reference);
        await using var reader = await command.ExecuteReaderAsync(ct);
        return await reader.ReadAsync(ct) ? ReadIntent(reader) : null;
    }

    public async Task MarkOutboxPublishedAsync(Guid aggregateId, string eventType, RequestContext context, CancellationToken ct)
    {
        await using var connection = await dataSource.OpenConnectionAsync(ct);
        await using var command = new NpgsqlCommand("""
            UPDATE payments.payment_outbox SET publish_status='Published', published_at=now()
            WHERE aggregate_id=@aggregate AND event_type=@type AND tenant_id=@tenant AND store_id=@store
              AND publish_status='Pending'
            """, connection);
        command.Parameters.AddWithValue("aggregate", aggregateId);
        command.Parameters.AddWithValue("type", eventType);
        AddContext(command, context);
        await command.ExecuteNonQueryAsync(ct);
    }

    private static async Task<List<PaymentMethodConfiguration>> ReadMethodsAsync(NpgsqlCommand command, CancellationToken ct)
    {
        var result = new List<PaymentMethodConfiguration>();
        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct)) result.Add(ReadMethod(reader));
        return result;
    }

    private static PaymentMethodConfiguration ReadMethod(NpgsqlDataReader r) => new()
    {
        Code = r.GetString(0),
        ProviderCode = r.GetString(1),
        Eligible = true,
        Active = r.GetBoolean(2),
        DefaultSelected = r.GetBoolean(3),
        Configurable = r.GetBoolean(4),
        Environment = r.GetString(5),
        ConfigurationVersion = r.GetInt64(6),
        SecretReference = r.GetString(7),
        PublicConfiguration = JsonSerializer.Deserialize<Dictionary<string, object?>>(r.GetString(8)) ?? [],
        Regions = r.IsDBNull(9) ? ["*"] : r.GetFieldValue<string[]>(9)
    };

    private static PaymentIntent ReadIntent(NpgsqlDataReader r) => new()
    {
        Id = r.GetGuid(0),
        TenantId = r.GetString(1),
        StoreId = r.GetString(2),
        CheckoutSessionId = r.GetString(3),
        OrderId = r.IsDBNull(4) ? null : r.GetString(4),
        ProviderCode = r.GetString(5),
        ProviderConfigVersion = r.GetInt64(6),
        Amount = r.GetDecimal(7),
        Currency = r.GetString(8).Trim(),
        Status = r.GetString(9),
        AuthorizedAmount = r.GetDecimal(10),
        CapturedAmount = r.GetDecimal(11),
        ClientSecretReference = r.IsDBNull(12) ? null : r.GetString(12),
        CreatedAt = r.GetFieldValue<DateTimeOffset>(13),
        UpdatedAt = r.GetFieldValue<DateTimeOffset>(14),
        CreatedBy = r.IsDBNull(15) ? null : r.GetString(15),
        CorrelationId = r.IsDBNull(16) ? null : r.GetString(16)
    };

    private static PaymentOperation ReadOperation(NpgsqlDataReader r) => new()
    {
        Id = r.GetGuid(0),
        PaymentIntentId = r.GetGuid(1),
        OperationType = r.GetString(2),
        Status = r.GetString(3),
        RequestedAmount = r.GetDecimal(4),
        Currency = r.GetString(5).Trim(),
        IdempotencyKey = r.GetString(6),
        ProviderAttemptId = r.IsDBNull(7) ? null : r.GetGuid(7),
        ProviderReference = r.IsDBNull(8) ? null : r.GetString(8),
        FailureCode = r.IsDBNull(9) ? null : r.GetString(9),
        FailureMessage = r.IsDBNull(10) ? null : r.GetString(10),
        CreatedAt = r.GetFieldValue<DateTimeOffset>(11),
        CompletedAt = r.IsDBNull(12) ? null : r.GetFieldValue<DateTimeOffset>(12),
        CorrelationId = r.IsDBNull(13) ? null : r.GetString(13)
    };

    private static PaymentOperation ReadOperationAt(NpgsqlDataReader r, int offset) => new()
    {
        Id = r.GetGuid(offset),
        PaymentIntentId = r.GetGuid(offset + 1),
        OperationType = r.GetString(offset + 2),
        Status = r.GetString(offset + 3),
        RequestedAmount = r.GetDecimal(offset + 4),
        Currency = r.GetString(offset + 5).Trim(),
        IdempotencyKey = r.GetString(offset + 6),
        ProviderAttemptId = r.IsDBNull(offset + 7) ? null : r.GetGuid(offset + 7),
        ProviderReference = r.IsDBNull(offset + 8) ? null : r.GetString(offset + 8),
        FailureCode = r.IsDBNull(offset + 9) ? null : r.GetString(offset + 9),
        FailureMessage = r.IsDBNull(offset + 10) ? null : r.GetString(offset + 10),
        CreatedAt = r.GetFieldValue<DateTimeOffset>(offset + 11),
        CompletedAt = r.IsDBNull(offset + 12) ? null : r.GetFieldValue<DateTimeOffset>(offset + 12),
        CorrelationId = r.IsDBNull(offset + 13) ? null : r.GetString(offset + 13)
    };

    private static PaymentTransaction ReadTransaction(NpgsqlDataReader r) => new()
    {
        Id = r.GetGuid(0),
        PaymentIntentId = r.GetGuid(1),
        PaymentOperationId = r.IsDBNull(2) ? null : r.GetGuid(2),
        OperationType = r.GetString(3),
        Status = r.GetString(4),
        Amount = r.GetDecimal(5),
        Currency = r.GetString(6).Trim(),
        ProviderCode = r.GetString(7),
        ProviderReference = r.IsDBNull(8) ? null : r.GetString(8),
        ProviderStatus = r.IsDBNull(9) ? null : r.GetString(9),
        ProviderCorrelationId = r.IsDBNull(10) ? null : r.GetString(10),
        ProviderDetails = JsonSerializer.Deserialize<Dictionary<string, object?>>(r.GetString(11)) ?? [],
        OccurredAt = r.GetFieldValue<DateTimeOffset>(12),
        SequenceNo = r.GetInt64(13),
        CreatedAt = r.GetFieldValue<DateTimeOffset>(14),
        CorrelationId = r.IsDBNull(15) ? null : r.GetString(15)
    };

    private static PaymentRefund ReadRefund(NpgsqlDataReader r) => new()
    {
        Id = r.GetGuid(0),
        PaymentIntentId = r.GetGuid(1),
        PaymentOperationId = r.GetGuid(2),
        Amount = r.GetDecimal(3),
        Currency = r.GetString(4).Trim(),
        Status = r.GetString(5),
        ProviderReference = r.IsDBNull(6) ? null : r.GetString(6),
        RequestedAt = r.GetFieldValue<DateTimeOffset>(7),
        CompletedAt = r.IsDBNull(8) ? null : r.GetFieldValue<DateTimeOffset>(8),
        CorrelationId = r.IsDBNull(9) ? null : r.GetString(9)
    };

    private static PaymentOperation NewOperation(Guid intentId, string type, decimal amount, string currency, string key, RequestContext context) => new()
    {
        Id = Guid.NewGuid(),
        PaymentIntentId = intentId,
        OperationType = type,
        RequestedAmount = amount,
        Currency = currency,
        IdempotencyKey = key,
        CorrelationId = context.CorrelationId,
        CreatedAt = DateTimeOffset.UtcNow
    };

    private static async Task InsertOperationAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, PaymentOperation operation,
        string fingerprint, RequestContext context, CancellationToken ct)
    {
        await using var command = new NpgsqlCommand("""
            INSERT INTO payments.payment_operation(payment_operation_id,payment_intent_id,tenant_id,store_id,operation_type,status,
              requested_amount,currency_code,idempotency_key,request_fingerprint,correlation_id)
            VALUES(@id,@intent,@tenant,@store,@type,'InProgress',@amount,@currency,@key,@fingerprint,@correlation)
            """, connection, transaction);
        command.Parameters.AddWithValue("id", operation.Id); command.Parameters.AddWithValue("intent", operation.PaymentIntentId);
        command.Parameters.AddWithValue("tenant", context.TenantId);
        command.Parameters.AddWithValue("store", context.StoreId);
        command.Parameters.AddWithValue("type", operation.OperationType); command.Parameters.AddWithValue("amount", operation.RequestedAmount);
        command.Parameters.AddWithValue("currency", operation.Currency); command.Parameters.AddWithValue("key", operation.IdempotencyKey);
        command.Parameters.AddWithValue("fingerprint", fingerprint); command.Parameters.AddWithValue("correlation", operation.CorrelationId ?? "");
        await command.ExecuteNonQueryAsync(ct);
        await using var idempotency = new NpgsqlCommand("""
            INSERT INTO payments.payment_idempotency
              (tenant_id,store_id,payment_intent_id,operation_type,idempotency_key,request_fingerprint,
               payment_operation_id,replay_status,created_at,expires_at)
            VALUES(@tenant,@store,@intent,@type,@key,@fingerprint,@operation,'InProgress',now(),now()+interval '24 hours')
            ON CONFLICT (tenant_id,store_id,payment_intent_id,operation_type,idempotency_key)
            DO UPDATE SET request_fingerprint=EXCLUDED.request_fingerprint
            """, connection, transaction);
        idempotency.Parameters.AddWithValue("tenant", context.TenantId);
        idempotency.Parameters.AddWithValue("store", context.StoreId);
        idempotency.Parameters.AddWithValue("intent", operation.PaymentIntentId);
        idempotency.Parameters.AddWithValue("type", operation.OperationType);
        idempotency.Parameters.AddWithValue("key", operation.IdempotencyKey);
        idempotency.Parameters.AddWithValue("fingerprint", fingerprint);
        idempotency.Parameters.AddWithValue("operation", operation.Id);
        await idempotency.ExecuteNonQueryAsync(ct);
    }

    private static async Task UpdateOperationAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, PaymentOperation operation, CancellationToken ct)
    {
        await using var command = new NpgsqlCommand("""
            UPDATE payments.payment_operation SET status=@status,provider_attempt_id=@attempt,provider_reference=@reference,
              failure_code=@failureCode,failure_message=@failureMessage,completed_at=@completed
            WHERE payment_operation_id=@id
            """, connection, transaction);
        command.Parameters.AddWithValue("status", operation.Status); command.Parameters.AddWithValue("attempt", (object?)operation.ProviderAttemptId ?? DBNull.Value);
        command.Parameters.AddWithValue("reference", (object?)operation.ProviderReference ?? DBNull.Value);
        command.Parameters.AddWithValue("failureCode", (object?)operation.FailureCode ?? DBNull.Value);
        command.Parameters.AddWithValue("failureMessage", (object?)operation.FailureMessage ?? DBNull.Value);
        command.Parameters.AddWithValue("completed", (object?)operation.CompletedAt ?? DBNull.Value);
        command.Parameters.AddWithValue("id", operation.Id);
        await command.ExecuteNonQueryAsync(ct);
        await using var idempotency = new NpgsqlCommand("""
            UPDATE payments.payment_idempotency SET replay_status=@replay, payment_operation_id=@id
            WHERE payment_operation_id=@id
            """, connection, transaction);
        idempotency.Parameters.AddWithValue("replay", operation.Status == "Succeeded" ? "Completed" : "Conflicted");
        idempotency.Parameters.AddWithValue("id", operation.Id);
        await idempotency.ExecuteNonQueryAsync(ct);
    }

    private static async Task UpdateIntentAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, PaymentIntent intent, RequestContext context, CancellationToken ct)
    {
        await using var command = new NpgsqlCommand("""
            UPDATE payments.payment_intent SET status=@status,authorized_amount=@authorized,captured_amount=@captured,
              client_secret_reference=@secret,updated_at=@updated
            WHERE payment_intent_id=@id AND tenant_id=@tenant AND store_id=@store
            """, connection, transaction);
        command.Parameters.AddWithValue("status", intent.Status); command.Parameters.AddWithValue("authorized", intent.AuthorizedAmount);
        command.Parameters.AddWithValue("captured", intent.CapturedAmount);
        command.Parameters.AddWithValue("secret", (object?)intent.ClientSecretReference ?? DBNull.Value);
        command.Parameters.AddWithValue("updated", intent.UpdatedAt); command.Parameters.AddWithValue("id", intent.Id); AddContext(command, context);
        await command.ExecuteNonQueryAsync(ct);
    }

    private static async Task UpdateRefundAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, PaymentRefund refund, RequestContext context, CancellationToken ct)
    {
        await using var command = new NpgsqlCommand("""
            UPDATE payments.payment_refund SET status=@status,provider_reference=@reference,completed_at=@completed
            WHERE payment_refund_id=@id AND tenant_id=@tenant AND store_id=@store
            """, connection, transaction);
        command.Parameters.AddWithValue("status", refund.Status); command.Parameters.AddWithValue("reference", (object?)refund.ProviderReference ?? DBNull.Value);
        command.Parameters.AddWithValue("completed", (object?)refund.CompletedAt ?? DBNull.Value); command.Parameters.AddWithValue("id", refund.Id); AddContext(command, context);
        await command.ExecuteNonQueryAsync(ct);
    }

    private static async Task InsertTransactionAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, PaymentIntent intent,
        PaymentOperation operation, ProviderResult result, string type, string status, decimal amount, CancellationToken ct)
    {
        var id = Guid.NewGuid();
        await using var command = new NpgsqlCommand("""
            INSERT INTO payments.payment_transaction(payment_transaction_id,payment_intent_id,payment_operation_id,tenant_id,store_id,
              operation_type,status,amount,currency_code,provider_code,provider_reference,provider_status,provider_details,sequence_no,correlation_id)
            VALUES(@id,@intent,@operation,@tenant,@store,@type,@status,@amount,@currency,@provider,@reference,@providerStatus,@details,
              (SELECT COALESCE(MAX(sequence_no),0)+1 FROM payments.payment_transaction WHERE payment_intent_id=@intent),@correlation)
            """, connection, transaction);
        command.Parameters.AddWithValue("id", id); command.Parameters.AddWithValue("intent", intent.Id); command.Parameters.AddWithValue("operation", operation.Id);
        command.Parameters.AddWithValue("tenant", intent.TenantId); command.Parameters.AddWithValue("store", intent.StoreId);
        command.Parameters.AddWithValue("type", type); command.Parameters.AddWithValue("status", status); command.Parameters.AddWithValue("amount", amount);
        command.Parameters.AddWithValue("currency", intent.Currency); command.Parameters.AddWithValue("provider", intent.ProviderCode);
        command.Parameters.AddWithValue("reference", (object?)result.Reference ?? DBNull.Value); command.Parameters.AddWithValue("providerStatus", (object?)result.ProviderStatus ?? DBNull.Value);
        command.Parameters.Add(new NpgsqlParameter("details", NpgsqlDbType.Jsonb) { Value = JsonSerializer.Serialize(new { providerStatus = result.ProviderStatus }) });
        command.Parameters.AddWithValue("correlation", intent.CorrelationId ?? "");
        await command.ExecuteNonQueryAsync(ct);
        if (!string.IsNullOrWhiteSpace(result.Reference))
        {
            await using var reference = new NpgsqlCommand("""
                INSERT INTO payments.payment_provider_reference
                  (payment_intent_id,payment_transaction_id,tenant_id,store_id,provider_code,reference_type,provider_reference,correlation_id)
                VALUES(@intent,@transaction,@tenant,@store,@provider,@type,@reference,@correlation)
                """, connection, transaction);
            reference.Parameters.AddWithValue("intent", intent.Id);
            reference.Parameters.AddWithValue("transaction", id);
            reference.Parameters.AddWithValue("tenant", intent.TenantId);
            reference.Parameters.AddWithValue("store", intent.StoreId);
            reference.Parameters.AddWithValue("provider", intent.ProviderCode);
            reference.Parameters.AddWithValue("type", type);
            reference.Parameters.AddWithValue("reference", result.Reference);
            reference.Parameters.AddWithValue("correlation", intent.CorrelationId ?? "");
            await reference.ExecuteNonQueryAsync(ct);
        }
    }

    private static async Task InsertOutboxAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, PaymentIntent intent, string eventType,
        object body, RequestContext context, CancellationToken ct)
    {
        var eventId = Guid.NewGuid();
        var envelope = new
        {
            eventId,
            eventType,
            eventVersion = 1,
            occurredAt = DateTimeOffset.UtcNow,
            tenantId = context.TenantId,
            storeId = context.StoreId,
            correlationId = context.CorrelationId,
            payload = body
        };
        await using var command = new NpgsqlCommand("""
            INSERT INTO payments.payment_outbox(payment_outbox_id,tenant_id,store_id,aggregate_type,aggregate_id,event_type,event_version,payload,correlation_id)
            VALUES(@id,@tenant,@store,'PaymentIntent',@aggregate,@type,1,@payload,@correlation)
            """, connection, transaction);
        command.Parameters.AddWithValue("id", eventId); AddContext(command, context); command.Parameters.AddWithValue("aggregate", intent.Id);
        command.Parameters.AddWithValue("type", eventType);
        command.Parameters.Add(new NpgsqlParameter("payload", NpgsqlDbType.Jsonb) { Value = JsonSerializer.Serialize(envelope) });
        command.Parameters.AddWithValue("correlation", context.CorrelationId);
        await command.ExecuteNonQueryAsync(ct);
    }

    private static async Task<decimal> RefundableBalanceAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, Guid intentId, decimal captured, CancellationToken ct)
    {
        await using var command = new NpgsqlCommand("""
            SELECT GREATEST(0, @captured - COALESCE(SUM(amount) FILTER (WHERE status IN ('Reserved','Succeeded')),0))
            FROM payments.payment_refund WHERE payment_intent_id=@intent
            """, connection, transaction);
        command.Parameters.AddWithValue("captured", captured); command.Parameters.AddWithValue("intent", intentId);
        return (decimal)(await command.ExecuteScalarAsync(ct) ?? 0m);
    }

    private static async Task<PaymentOperation?> FindOperationInTransactionAsync(Guid id, RequestContext context, NpgsqlConnection connection, NpgsqlTransaction? transaction, CancellationToken ct)
    {
        await using var command = new NpgsqlCommand("""
            SELECT payment_operation_id,payment_intent_id,operation_type,status,requested_amount,currency_code,idempotency_key,
                   provider_attempt_id,provider_reference,failure_code,failure_message,created_at,completed_at,correlation_id
            FROM payments.payment_operation WHERE payment_operation_id=@id AND tenant_id=@tenant AND store_id=@store
            """, connection, transaction);
        command.Parameters.AddWithValue("id", id); AddContext(command, context);
        await using var reader = await command.ExecuteReaderAsync(ct);
        return await reader.ReadAsync(ct) ? ReadOperation(reader) : null;
    }

    private static void AddContext(NpgsqlCommand command, RequestContext context)
    {
        command.Parameters.AddWithValue("tenant", context.TenantId);
        command.Parameters.AddWithValue("store", context.StoreId);
    }

    private static DomainException NotFound(string code, string message) => new(code, message, 404);
}

public sealed record ConfigureMethodValues(bool Active, bool DefaultSelected, string Environment,
    Dictionary<string, object?> PublicConfiguration, string SecretReference, long? ConfigurationVersion);
public sealed record CallbackWriteResult(Guid CallbackId, bool Duplicate);
