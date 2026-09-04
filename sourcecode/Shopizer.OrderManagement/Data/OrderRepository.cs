using System.Data;
using System.Globalization;
using System.Text.Json;
using Npgsql;
using Shopizer.OrderManagement.Models;

namespace Shopizer.OrderManagement.Data;

public sealed class OrderRepository(NpgsqlDataSource dataSource)
{
    public async Task<Order?> FindAsync(long id, RequestContext context, long? customerId, bool children, CancellationToken ct)
    {
        await using var connection = await dataSource.OpenConnectionAsync(ct);
        const string sql = """
            SELECT order_id,tenant_id,store_id,customer_id,customer_email_address,order_status,payment_status,
                   fulfillment_status,currency_code,order_total,refunded_amount,refundable_amount,date_purchased,
                   order_date_finished,payment_type,payment_module_code,shipping_module_code,customer_agreed,
                   confirmed_address,locale
            FROM order_management.orders
            WHERE order_id=@id AND tenant_id=@tenant AND store_id=@store
              AND (@customer IS NULL OR customer_id=@customer)
            """;
        await using var command = new NpgsqlCommand(sql, connection);
        Add(command, "id", id); Add(command, "tenant", context.TenantId); Add(command, "store", context.StoreNumber);
        AddNullable(command, "customer", customerId);
        await using var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleRow, ct);
        if (!await reader.ReadAsync(ct)) return null;
        var order = ReadOrder(reader);
        await reader.DisposeAsync();
        if (children) await LoadChildrenAsync(connection, order, ct);
        return order;
    }

    public async Task<(List<Order> Items, long Total)> ListAsync(RequestContext context, int page, int pageSize,
        string? status, string? customerName, string? email, string? phone, long? orderId, long? customerId, CancellationToken ct)
    {
        await using var connection = await dataSource.OpenConnectionAsync(ct);
        var filters = "o.tenant_id=@tenant AND o.store_id=@store";
        if (customerId is not null) filters += " AND o.customer_id=@customer";
        if (orderId is not null) filters += " AND o.order_id=@orderId";
        if (!string.IsNullOrWhiteSpace(status)) filters += " AND o.order_status=@status";
        if (!string.IsNullOrWhiteSpace(email)) filters += " AND lower(o.customer_email_address)=lower(@email)";
        if (!string.IsNullOrWhiteSpace(customerName)) filters += " AND (lower(coalesce(b.first_name,'') || ' ' || coalesce(b.last_name,'')) LIKE lower(@name))";
        if (!string.IsNullOrWhiteSpace(phone)) filters += " AND (b.telephone=@phone OR d.telephone=@phone)";
        var from = "FROM order_management.orders o LEFT JOIN order_management.order_billing_address b ON b.order_id=o.order_id LEFT JOIN order_management.order_delivery_address d ON d.order_id=o.order_id";
        await using var count = new NpgsqlCommand($"SELECT count(*) {from} WHERE {filters}", connection);
        AddCommon(count, context, customerId, orderId, status, email, customerName, phone);
        var total = (long)(await count.ExecuteScalarAsync(ct) ?? 0L);
        await using var command = new NpgsqlCommand($"""
            SELECT o.order_id,o.tenant_id,o.store_id,o.customer_id,o.customer_email_address,o.order_status,o.payment_status,
                   o.fulfillment_status,o.currency_code,o.order_total,o.refunded_amount,o.refundable_amount,o.date_purchased,
                   o.order_date_finished,o.payment_type,o.payment_module_code,o.shipping_module_code,o.customer_agreed,
                   o.confirmed_address,o.locale
            {from} WHERE {filters} GROUP BY o.order_id ORDER BY o.order_id DESC OFFSET @offset LIMIT @limit
            """, connection);
        AddCommon(command, context, customerId, orderId, status, email, customerName, phone);
        Add(command, "offset", (page - 1) * pageSize); Add(command, "limit", pageSize);
        var items = new List<Order>();
        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct)) items.Add(ReadOrder(reader));
        return (items, total);
    }

    // @BR-OR-SUB-001: An accepted submission is atomically created in ORDERED state with its initial history.
    // @BR-OR-SUB-002: Acceptance persists tenant, store, customer, payment, currency, and address snapshots.
    // @BR-OR-SUB-003: Purchased lines, selected attributes, prices, and digital metadata are persisted as facts.
    // @BR-OR-SUB-004: The validated monetary snapshot is persisted without recalculating checkout totals.
    // @BR-OR-DIG-001: A digital line receives one independent download entitlement at acceptance.
    // @BR-OR-RES-001: Submission identity is unique within the tenant/store and replay returns the original order.
    public async Task<Order> CreateSubmissionAsync(Submission submission, RequestContext context, CancellationToken ct)
    {
        await using var connection = await dataSource.OpenConnectionAsync(ct);
        await using var transaction = await connection.BeginTransactionAsync(ct);
        try
        {
            var existing = await FindBySubmissionAsync(connection, transaction, context, submission.SubmissionId, ct);
            if (existing is not null) { await transaction.CommitAsync(ct); return existing; }
            await using var command = new NpgsqlCommand("""
                INSERT INTO order_management.orders
                  (tenant_id,store_id,customer_id,customer_email_address,order_status,date_purchased,currency_code,
                   order_total,payment_type,payment_module_code,shipping_module_code,customer_agreed,confirmed_address,
                   locale,submission_id,correlation_id,refundable_amount)
                VALUES (@tenant,@store,@customer,@email,'ORDERED',@purchased,@currency,@total,@paymentType,@paymentModule,
                        @shippingModule,@agreed,@confirmed,@locale,@submission,@correlation,@total)
                RETURNING order_id
                """, connection, transaction);
            Add(command, "tenant", context.TenantId); Add(command, "store", submission.StoreId);
            Add(command, "customer", submission.CustomerId is null ? DBNull.Value : submission.CustomerId.Value);
            Add(command, "email", submission.CustomerEmailAddress); Add(command, "purchased", DateTimeOffset.UtcNow);
            Add(command, "currency", submission.Currency); Add(command, "total", submission.Total);
            Add(command, "paymentType", (object?)submission.PaymentType ?? DBNull.Value);
            Add(command, "paymentModule", (object?)submission.PaymentModuleCode ?? DBNull.Value);
            Add(command, "shippingModule", (object?)submission.ShippingModuleCode ?? DBNull.Value);
            Add(command, "agreed", submission.CustomerAgreed); Add(command, "confirmed", submission.BillingAddress is not null && submission.DeliveryAddress is not null);
            Add(command, "locale", (object?)submission.Locale ?? DBNull.Value); Add(command, "submission", submission.SubmissionId);
            Add(command, "correlation", context.CorrelationId);
            var orderId = (long)(await command.ExecuteScalarAsync(ct) ?? throw new InvalidOperationException("Order identity was not generated."));
            await InsertInboxAsync(connection, transaction, context, submission.SubmissionId, "OrderSubmitted.v1", submission.SubmissionId, ct);
            await InsertAddressAsync(connection, transaction, orderId, submission.BillingAddress, true, ct);
            await InsertAddressAsync(connection, transaction, orderId, submission.DeliveryAddress, false, ct);
            foreach (var line in submission.Lines)
            {
                await using var lineCommand = new NpgsqlCommand("""
                    INSERT INTO order_management.order_products(order_id,product_sku,product_name,product_quantity,onetime_charge)
                    VALUES(@order,@sku,@name,@quantity,@charge) RETURNING order_product_id
                    """, connection, transaction);
                Add(lineCommand, "order", orderId); Add(lineCommand, "sku", line.Sku); Add(lineCommand, "name", line.ProductName);
                Add(lineCommand, "quantity", line.Quantity); Add(lineCommand, "charge", line.UnitPrice);
                var lineId = (long)(await lineCommand.ExecuteScalarAsync(ct) ?? 0L);
                foreach (var attribute in line.Attributes)
                {
                    await using var attributeCommand = new NpgsqlCommand("""
                        INSERT INTO order_management.order_product_attributes
                          (order_product_id,product_attribute_price,product_attribute_is_free,product_attribute_weight,
                           product_option_id,product_option_value_id,product_attribute_name,product_attribute_val_name)
                        VALUES(@line,@price,@free,@weight,@option,@value,@name,@val)
                        """, connection, transaction);
                    Add(attributeCommand, "line", lineId); Add(attributeCommand, "price", attribute.Price); Add(attributeCommand, "free", attribute.Free);
                    Add(attributeCommand, "weight", attribute.Weight is null ? DBNull.Value : attribute.Weight.Value);
                    Add(attributeCommand, "option", attribute.OptionId); Add(attributeCommand, "value", attribute.OptionValueId);
                    Add(attributeCommand, "name", attribute.Name); Add(attributeCommand, "val", attribute.Value);
                    await attributeCommand.ExecuteNonQueryAsync(ct);
                }
                foreach (var price in line.Prices.DefaultIfEmpty(new SubmissionPrice("accepted", null, line.UnitPrice, null, null, null, true)))
                    await InsertPriceAsync(connection, transaction, lineId, price, ct);
                if (!string.IsNullOrWhiteSpace(line.DigitalFileName))
                {
                    await using var download = new NpgsqlCommand("""
                        INSERT INTO order_management.order_product_downloads(order_product_id,order_product_filename,download_maxdays,expires_at)
                        VALUES(@line,@file,@days,@expires)
                        """, connection, transaction);
                    Add(download, "line", lineId); Add(download, "file", line.DigitalFileName!); Add(download, "days", line.DownloadExpiryDays);
                    Add(download, "expires", DateTimeOffset.UtcNow.AddDays(line.DownloadExpiryDays));
                    await download.ExecuteNonQueryAsync(ct);
                }
            }
            foreach (var total in submission.Totals)
            {
                await using var totalCommand = new NpgsqlCommand("""
                    INSERT INTO order_management.order_totals(order_id,code,title,text,value,module,order_total_type,order_value_type,sort_order)
                    VALUES(@order,@code,@title,@text,@value,@module,@type,@valueType,@sort)
                    """, connection, transaction);
                Add(totalCommand, "order", orderId); Add(totalCommand, "code", total.Code); Add(totalCommand, "title", (object?)total.Title ?? DBNull.Value);
                Add(totalCommand, "text", (object?)total.Text ?? DBNull.Value); Add(totalCommand, "value", total.Value); Add(totalCommand, "module", (object?)total.Module ?? DBNull.Value);
                Add(totalCommand, "type", (object?)total.Type ?? DBNull.Value); Add(totalCommand, "valueType", total.ValueType ?? "ONE_TIME"); Add(totalCommand, "sort", total.SortOrder);
                await totalCommand.ExecuteNonQueryAsync(ct);
            }
            if (!string.IsNullOrWhiteSpace(submission.Comments))
                await InsertHistoryAsync(connection, transaction, orderId, "ORDERED", submission.Comments, null, "SUBMISSION", null, ct);
            await InsertHistoryAsync(connection, transaction, orderId, "ORDERED", null, null, "SUBMISSION", null, ct);
            var payload = new
            {
                eventId = Guid.NewGuid(),
                eventType = "OrderAccepted",
                eventVersion = 1,
                occurredAt = DateTimeOffset.UtcNow,
                tenantId = context.TenantId,
                storeId = context.StoreId,
                correlationId = context.CorrelationId,
                orderId,
                status = "Ordered",
                currency = submission.Currency,
                total = submission.Total,
                lines = submission.Lines.Select(x => new { sku = x.Sku, productName = x.ProductName, quantity = x.Quantity, unitPrice = x.UnitPrice, attributes = x.Attributes }).ToArray()
            };
            await InsertOutboxAsync(connection, transaction, context, orderId, "OrderAccepted", payload, ct);
            if (!string.IsNullOrWhiteSpace(submission.PaymentToken))
            {
                var payment = new { eventId = Guid.NewGuid(), eventType = "PaymentRequested.v1", eventVersion = 1, occurredAt = DateTimeOffset.UtcNow, tenantId = context.TenantId, storeId = context.StoreId, correlationId = context.CorrelationId, submissionId = submission.SubmissionId, amount = submission.Total.ToString(CultureInfo.InvariantCulture), currency = submission.Currency, paymentType = submission.PaymentType ?? "", paymentModule = submission.PaymentModuleCode ?? "", tokenReference = submission.PaymentToken };
                await InsertOutboxAsync(connection, transaction, context, orderId, "PaymentRequested.v1", payment, ct);
            }
            await transaction.CommitAsync(ct);
            return await FindAsync(orderId, context, null, true, ct) ?? throw new InvalidOperationException("Created order could not be read.");
        }
        catch (PostgresException ex) when (ex.SqlState == "23505")
        {
            await transaction.RollbackAsync(ct);
            var duplicate = await FindBySubmissionAsync(connection, null, context, submission.SubmissionId, ct);
            if (duplicate is not null) return duplicate;
            throw new DomainException("ORDER_COMPENSATION_REQUIRED", "The order could not be accepted because its submission conflicts with existing state.", 409);
        }
    }

    // @BR-OR-LIFE-001: A lifecycle mutation is committed only when the requested transition is legal for the current state.
    // @BR-OR-LIFE-002: Each legal transition appends immutable actor, source, timestamp, and comment history.
    // @BR-OR-RES-001: Idempotent status commands do not append a second transition.
    public async Task<Order> TransitionAsync(long id, string status, string? reason, string actor, RequestContext context, string idempotencyKey, CancellationToken ct)
    {
        await using var connection = await dataSource.OpenConnectionAsync(ct);
        await using var transaction = await connection.BeginTransactionAsync(ct);
        var existing = await FindInboxAsync(connection, transaction, context, idempotencyKey, ct);
        if (existing is not null) { await transaction.CommitAsync(ct); return await FindAsync(id, context, null, true, ct) ?? throw NotFound(id); }
        var current = await FindAsync(connection, transaction, id, context, ct) ?? throw NotFound(id);
        if (!Legal(current.Status, status)) throw new DomainException(current.Status is "CANCELED" or "REFUNDED" ? "ORDER_TERMINAL" : "ORDER_STATUS_TRANSITION_INVALID", $"{current.Status} cannot transition to {status}.", 409);
        await InsertInboxAsync(connection, transaction, context, Guid.NewGuid().ToString(), "StatusCommand", idempotencyKey, ct);
        await using var update = new NpgsqlCommand("UPDATE order_management.orders SET order_status=@status, order_date_finished=CASE WHEN @status IN ('DELIVERED','REFUNDED','CANCELED') THEN now() ELSE order_date_finished END, version=version+1, updated_at=now(), last_modified=now() WHERE order_id=@id AND tenant_id=@tenant AND store_id=@store", connection, transaction);
        Add(update, "status", status); Add(update, "id", id); Add(update, "tenant", context.TenantId); Add(update, "store", context.StoreNumber); await update.ExecuteNonQueryAsync(ct);
        await InsertHistoryAsync(connection, transaction, id, status, reason, actor, "ADMIN", null, ct);
        await InsertOutboxAsync(connection, transaction, context, id, "OrderStatusChanged", new { eventId = Guid.NewGuid(), eventType = "OrderStatusChanged", eventVersion = 1, occurredAt = DateTimeOffset.UtcNow, tenantId = context.TenantId, storeId = context.StoreId, correlationId = context.CorrelationId, orderId = id, previousStatus = current.Status, status, source = "ADMIN" }, ct);
        await transaction.CommitAsync(ct);
        return await FindAsync(id, context, null, true, ct) ?? throw NotFound(id);
    }

    // @BR-OR-LIFE-002: Administrative history is appended and can never update or delete an existing record.
    // @BR-OR-RES-001: A repeated history command returns the already-created history entry.
    public async Task<OrderHistory> AppendHistoryAsync(long id, string status, string? comments, string source, string actor, RequestContext context, string key, CancellationToken ct)
    {
        await using var connection = await dataSource.OpenConnectionAsync(ct);
        await using var transaction = await connection.BeginTransactionAsync(ct);
        if (await FindInboxAsync(connection, transaction, context, key, ct) is not null)
        {
            await transaction.CommitAsync(ct);
            var history = await HistoryAsync(id, context, ct);
            return history.FirstOrDefault() ?? throw NotFound(id);
        }
        if (await FindAsync(connection, transaction, id, context, ct) is null) throw NotFound(id);
        await InsertInboxAsync(connection, transaction, context, Guid.NewGuid().ToString(), "HistoryCommand", key, ct);
        var result = await InsertHistoryAsync(connection, transaction, id, status, comments, actor, source, null, ct);
        await transaction.CommitAsync(ct);
        return result;
    }

    // @BR-OR-ADM-002: Snapshot correction updates only the order's stored customer and address facts.
    public async Task<Order> UpdateSnapshotAsync(long id, CustomerSnapshotUpdateRequest update, RequestContext context, string key, CancellationToken ct)
    {
        ValidateAddress(update.BillingAddress); ValidateAddress(update.DeliveryAddress);
        await using var connection = await dataSource.OpenConnectionAsync(ct);
        await using var transaction = await connection.BeginTransactionAsync(ct);
        if (await FindInboxAsync(connection, transaction, context, key, ct) is null)
        {
            if (await FindAsync(connection, transaction, id, context, ct) is null) throw NotFound(id);
            await InsertInboxAsync(connection, transaction, context, Guid.NewGuid().ToString(), "SnapshotCommand", key, ct);
            await using var command = new NpgsqlCommand("UPDATE order_management.orders SET customer_email_address=@email,updated_at=now(),last_modified=now(),version=version+1 WHERE order_id=@id AND tenant_id=@tenant AND store_id=@store", connection, transaction);
            Add(command, "email", update.EmailAddress); Add(command, "id", id); Add(command, "tenant", context.TenantId); Add(command, "store", context.StoreNumber); await command.ExecuteNonQueryAsync(ct);
            await UpsertAddressAsync(connection, transaction, id, update.BillingAddress, true, ct);
            await UpsertAddressAsync(connection, transaction, id, update.DeliveryAddress, false, ct);
        }
        await transaction.CommitAsync(ct);
        return await FindAsync(id, context, null, true, ct) ?? throw NotFound(id);
    }

    // @BR-OR-PAY-002: Only authenticated payment outcomes update the payment projection and eligible order lifecycle.
    // @BR-OR-RES-001: Payment event IDs are deduplicated before any projection or history write.
    public async Task<bool> ApplyPaymentAsync(PaymentOutcome outcome, RequestContext context, string eventId, CancellationToken ct)
    {
        await using var connection = await dataSource.OpenConnectionAsync(ct);
        await using var transaction = await connection.BeginTransactionAsync(ct);
        if (await FindInboxAsync(connection, transaction, context, eventId, ct) is not null) { await transaction.CommitAsync(ct); return false; }
        var order = await FindAsync(connection, transaction, outcome.OrderId, context, ct) ?? throw NotFound(outcome.OrderId);
        await InsertInboxAsync(connection, transaction, context, eventId, "PaymentOutcome", null, ct);
        await using var insert = new NpgsqlCommand("""
            INSERT INTO order_management.payment_outcomes(transaction_id,tenant_id,store_id,order_id,action,status,amount,currency,payment_reference,occurred_at,event_id)
            VALUES(@transaction,@tenant,@store,@order,@action,@status,@amount,@currency,@reference,@occurred,@event)
            ON CONFLICT(transaction_id) DO NOTHING
            """, connection, transaction);
        Add(insert, "transaction", outcome.TransactionId); Add(insert, "tenant", context.TenantId); Add(insert, "store", context.StoreNumber); Add(insert, "order", outcome.OrderId);
        Add(insert, "action", outcome.Action); Add(insert, "status", outcome.Status); Add(insert, "amount", outcome.Amount); Add(insert, "currency", outcome.Currency);
        Add(insert, "reference", (object?)outcome.PaymentReference ?? DBNull.Value); Add(insert, "occurred", outcome.OccurredAt); Add(insert, "event", eventId);
        await insert.ExecuteNonQueryAsync(ct);
        var paymentStatus = outcome.Action switch { "AUTHORIZE" => "AUTHORIZED", "CAPTURE" or "AUTHORIZECAPTURE" => "CAPTURED", "REFUND" => "REFUNDED", "VOID" => "VOIDED", "FAIL" => "FAILED", _ => "UNKNOWN" };
        await using var update = new NpgsqlCommand("UPDATE order_management.orders SET payment_status=@status,refundable_amount=CASE WHEN @status='CAPTURED' THEN greatest(refundable_amount,@amount) ELSE refundable_amount END,updated_at=now(),version=version+1 WHERE order_id=@id AND tenant_id=@tenant AND store_id=@store", connection, transaction);
        Add(update, "status", paymentStatus); Add(update, "amount", outcome.Amount); Add(update, "id", outcome.OrderId); Add(update, "tenant", context.TenantId); Add(update, "store", context.StoreNumber); await update.ExecuteNonQueryAsync(ct);
        if (outcome.Action is "CAPTURE" or "AUTHORIZECAPTURE" && outcome.Status == "SUCCEEDED" && order.Status == "ORDERED")
        {
            await using var transition = new NpgsqlCommand("UPDATE order_management.orders SET order_status='PROCESSED',updated_at=now(),version=version+1 WHERE order_id=@id AND tenant_id=@tenant AND store_id=@store", connection, transaction);
            Add(transition, "id", outcome.OrderId); Add(transition, "tenant", context.TenantId); Add(transition, "store", context.StoreNumber); await transition.ExecuteNonQueryAsync(ct);
            await InsertHistoryAsync(connection, transaction, outcome.OrderId, "PROCESSED", "Payment captured", null, "PAYMENT_CAPTURED", eventId, ct);
            await InsertOutboxAsync(connection, transaction, context, outcome.OrderId, "OrderStatusChanged", new { eventId = Guid.NewGuid(), eventType = "OrderStatusChanged", eventVersion = 1, occurredAt = DateTimeOffset.UtcNow, tenantId = context.TenantId, storeId = context.StoreId, correlationId = context.CorrelationId, orderId = outcome.OrderId, previousStatus = "ORDERED", status = "PROCESSED", source = "PAYMENT_CAPTURED" }, ct);
        }
        await transaction.CommitAsync(ct);
        return true;
    }

    // @BR-OR-PAY-003: Next payment action is selected from the latest outcome timestamp, not action-name ordering.
    public async Task<PaymentOutcome?> LatestPaymentAsync(long id, RequestContext context, CancellationToken ct)
    {
        await using var connection = await dataSource.OpenConnectionAsync(ct);
        await using var command = new NpgsqlCommand("SELECT transaction_id,order_id,action,status,amount,currency,payment_reference,occurred_at FROM order_management.payment_outcomes WHERE order_id=@id AND tenant_id=@tenant AND store_id=@store ORDER BY occurred_at DESC,transaction_id DESC LIMIT 1", connection);
        Add(command, "id", id); Add(command, "tenant", context.TenantId); Add(command, "store", context.StoreNumber);
        await using var reader = await command.ExecuteReaderAsync(ct);
        return await reader.ReadAsync(ct) ? ReadPayment(reader) : null;
    }

    // @BR-OR-PAY-004: Capturable discovery requires authorization and excludes capture, authorize-capture, and refund outcomes.
    public async Task<List<Order>> CapturableAsync(RequestContext context, DateTimeOffset? start, DateTimeOffset? end, int page, int pageSize, CancellationToken ct)
    {
        await using var connection = await dataSource.OpenConnectionAsync(ct);
        await using var command = new NpgsqlCommand("""
            SELECT o.order_id,o.tenant_id,o.store_id,o.customer_id,o.customer_email_address,o.order_status,o.payment_status,
                   o.fulfillment_status,o.currency_code,o.order_total,o.refunded_amount,o.refundable_amount,o.date_purchased,
                   o.order_date_finished,o.payment_type,o.payment_module_code,o.shipping_module_code,o.customer_agreed,o.confirmed_address,o.locale
            FROM order_management.orders o
            WHERE o.tenant_id=@tenant AND o.store_id=@store
              AND (@start IS NULL OR o.date_purchased >= @start) AND (@end IS NULL OR o.date_purchased < @end)
              AND EXISTS(SELECT 1 FROM order_management.payment_outcomes p WHERE p.order_id=o.order_id AND p.action='AUTHORIZE' AND p.status='SUCCEEDED')
              AND NOT EXISTS(SELECT 1 FROM order_management.payment_outcomes p WHERE p.order_id=o.order_id AND p.action IN ('CAPTURE','AUTHORIZECAPTURE','REFUND'))
            ORDER BY o.order_id DESC OFFSET @offset LIMIT @limit
            """, connection);
        Add(command, "tenant", context.TenantId); Add(command, "store", context.StoreNumber); AddNullable(command, "start", start);
        AddNullable(command, "end", end); Add(command, "offset", (page - 1) * pageSize); Add(command, "limit", pageSize);
        var result = new List<Order>(); await using var reader = await command.ExecuteReaderAsync(ct); while (await reader.ReadAsync(ct)) result.Add(ReadOrder(reader)); return result;
    }

    // @BR-OR-REF-001: Refund commands reserve cumulative balance and never execute provider work inside MS-05.
    public async Task<RefundReservation> ReserveRefundAsync(long id, decimal amount, string currency, string reason, string key, RequestContext context, CancellationToken ct)
    {
        await using var connection = await dataSource.OpenConnectionAsync(ct); await using var transaction = await connection.BeginTransactionAsync(ct);
        await using var existing = new NpgsqlCommand("SELECT refund_id,amount,status,coalesce((SELECT refundable_amount-refunded_amount FROM order_management.orders WHERE order_id=@order),0) FROM order_management.refund_applications WHERE tenant_id=@tenant AND store_id=@store AND idempotency_key=@key", connection, transaction);
        Add(existing, "order", id); Add(existing, "tenant", context.TenantId); Add(existing, "store", context.StoreNumber); Add(existing, "key", key);
        await using var existingReader = await existing.ExecuteReaderAsync(ct);
        if (await existingReader.ReadAsync(ct))
        {
            var value = new RefundReservation(existingReader.GetString(0), existingReader.GetDecimal(1), existingReader.GetString(2), existingReader.GetDecimal(3));
            await existingReader.DisposeAsync(); await transaction.CommitAsync(ct); return value;
        }
        await existingReader.DisposeAsync();
        var order = await FindAsync(connection, transaction, id, context, ct) ?? throw NotFound(id);
        var captured = await SumPaymentAsync(connection, transaction, id, "CAPTURE", ct) + await SumPaymentAsync(connection, transaction, id, "AUTHORIZECAPTURE", ct);
        var remaining = Math.Max(0, captured - order.RefundedAmount);
        if (amount <= 0 || amount > remaining) throw new DomainException("REFUND_AMOUNT_INVALID", $"Refund amount {amount:0.00} exceeds remaining refundable balance {remaining:0.00}.", 422);
        var refundId = $"rfd-{Guid.NewGuid():N}";
        await using var insert = new NpgsqlCommand("INSERT INTO order_management.refund_applications(refund_id,tenant_id,store_id,order_id,amount,currency,reason,idempotency_key) VALUES(@id,@tenant,@store,@order,@amount,@currency,@reason,@key)", connection, transaction);
        Add(insert, "id", refundId); Add(insert, "tenant", context.TenantId); Add(insert, "store", context.StoreNumber); Add(insert, "order", id); Add(insert, "amount", amount); Add(insert, "currency", currency); Add(insert, "reason", reason); Add(insert, "key", key); await insert.ExecuteNonQueryAsync(ct);
        await InsertInboxAsync(connection, transaction, context, refundId, "RefundCommand", key, ct);
        await InsertOutboxAsync(connection, transaction, context, id, "PaymentRefundRequested", new { eventId = Guid.NewGuid(), eventType = "PaymentRefundRequested", eventVersion = 1, occurredAt = DateTimeOffset.UtcNow, tenantId = context.TenantId, storeId = context.StoreId, correlationId = context.CorrelationId, orderId = id, refundId, amount, currency, reason }, ct);
        await transaction.CommitAsync(ct); return new(refundId, amount, "PROCESSING", remaining - amount);
    }

    // @BR-OR-REF-001: Authenticated refund outcomes increase the durable refund total once and close the order only at full capture reconciliation.
    // @BR-OR-RES-001: Refund outcome event IDs make replay harmless.
    public async Task<RefundReservation> ApplyRefundAsync(long id, string refundId, decimal amount, string currency, RequestContext context, string eventId, CancellationToken ct)
    {
        var applied = await ApplyPaymentAsync(new PaymentOutcome($"refund-{refundId}", id, "REFUND", "SUCCEEDED", amount, currency, refundId, DateTimeOffset.UtcNow), context, eventId, ct);
        if (!applied)
        {
            var existingOrder = await FindAsync(id, context, null, false, ct) ?? throw NotFound(id);
            return new(refundId, amount, "APPLIED", Math.Max(0, existingOrder.RefundableAmount - existingOrder.RefundedAmount));
        }
        await using var connection = await dataSource.OpenConnectionAsync(ct); await using var tx = await connection.BeginTransactionAsync(ct);
        var prior = await FindAsync(connection, tx, id, context, ct) ?? throw NotFound(id);
        await using var command = new NpgsqlCommand("UPDATE order_management.refund_applications SET status='APPLIED',applied_at=now() WHERE refund_id=@refund AND tenant_id=@tenant AND store_id=@store; UPDATE order_management.orders SET refunded_amount=refunded_amount+@amount,refundable_amount=greatest(0,refundable_amount-@amount),payment_status=CASE WHEN refundable_amount-@amount<=0 THEN 'REFUNDED' ELSE payment_status END WHERE order_id=@order AND tenant_id=@tenant AND store_id=@store RETURNING refundable_amount", connection, tx);
        Add(command, "refund", refundId); Add(command, "tenant", context.TenantId); Add(command, "store", context.StoreNumber); Add(command, "amount", amount); Add(command, "order", id);
        var remaining = (decimal)(await command.ExecuteScalarAsync(ct) ?? 0m);
        if (remaining == 0 && prior.Status is "PROCESSED" or "DELIVERED")
        {
            await using var transition = new NpgsqlCommand("UPDATE order_management.orders SET order_status='REFUNDED',order_date_finished=now(),updated_at=now(),version=version+1 WHERE order_id=@order AND tenant_id=@tenant AND store_id=@store", connection, tx);
            Add(transition, "order", id); Add(transition, "tenant", context.TenantId); Add(transition, "store", context.StoreNumber); await transition.ExecuteNonQueryAsync(ct);
            await InsertHistoryAsync(connection, tx, id, "REFUNDED", "Captured balance fully refunded", null, "PAYMENT_REFUNDED", eventId, ct);
            await InsertOutboxAsync(connection, tx, context, id, "OrderRefundApplied", new { eventId = Guid.NewGuid(), eventType = "OrderRefundApplied", eventVersion = 1, occurredAt = DateTimeOffset.UtcNow, tenantId = context.TenantId, storeId = context.StoreId, correlationId = context.CorrelationId, orderId = id, refundId, amount, currency, remainingRefundable = remaining, status = "Applied" }, ct);
        }
        await tx.CommitAsync(ct); return new(refundId, amount, "APPLIED", remaining);
    }

    // @BR-OR-CAN-001: Cancellation validates terminal and fulfillment guards, records compensation, and emits downstream requests.
    // @BR-OR-FAIL-001: Failed downstream compensation remains explicitly represented for retry/reconciliation.
    // @BR-OR-RES-001: Cancellation idempotency prevents repeated compensation commands.
    public async Task<CancellationResult> CancelAsync(long id, string reason, string key, RequestContext context, CancellationToken ct)
    {
        await using var connection = await dataSource.OpenConnectionAsync(ct); await using var tx = await connection.BeginTransactionAsync(ct);
        await using var existing = new NpgsqlCommand("SELECT c.compensation_state,o.order_status FROM order_management.cancellation_records c JOIN order_management.orders o ON o.order_id=c.order_id WHERE c.tenant_id=@tenant AND c.store_id=@store AND c.idempotency_key=@key", connection, tx);
        Add(existing, "tenant", context.TenantId); Add(existing, "store", context.StoreNumber); Add(existing, "key", key); await using var er = await existing.ExecuteReaderAsync(ct);
        if (await er.ReadAsync(ct)) { var result = new CancellationResult(id, er.GetString(1), er.GetString(0)); await er.DisposeAsync(); await tx.CommitAsync(ct); return result; }
        await er.DisposeAsync();
        var order = await FindAsync(connection, tx, id, context, ct) ?? throw NotFound(id);
        if (order.Status is "DELIVERED" or "REFUNDED" or "CANCELED") throw new DomainException("ORDER_CANNOT_BE_CANCELED", "A terminal order cannot be canceled.", 409);
        if (order.FulfillmentStatus is "SHIPPED" or "DELIVERED") throw new DomainException("FULFILLMENT_ALREADY_STARTED", "A shipped order cannot be canceled.", 409);
        await using var insert = new NpgsqlCommand("INSERT INTO order_management.cancellation_records(tenant_id,store_id,order_id,reason,idempotency_key) VALUES(@tenant,@store,@order,@reason,@key)", connection, tx);
        Add(insert, "tenant", context.TenantId); Add(insert, "store", context.StoreNumber); Add(insert, "order", id); Add(insert, "reason", reason); Add(insert, "key", key); await insert.ExecuteNonQueryAsync(ct);
        await using var update = new NpgsqlCommand("UPDATE order_management.orders SET order_status='CANCELED',updated_at=now(),version=version+1 WHERE order_id=@order AND tenant_id=@tenant AND store_id=@store", connection, tx);
        Add(update, "order", id); Add(update, "tenant", context.TenantId); Add(update, "store", context.StoreNumber); await update.ExecuteNonQueryAsync(ct);
        await InsertHistoryAsync(connection, tx, id, "CANCELED", reason, null, "ADMIN", null, ct);
        await InsertOutboxAsync(connection, tx, context, id, "OrderCanceled", new { eventId = Guid.NewGuid(), eventType = "OrderCanceled", eventVersion = 1, occurredAt = DateTimeOffset.UtcNow, tenantId = context.TenantId, storeId = context.StoreId, correlationId = context.CorrelationId, orderId = id, previousStatus = order.Status, status = "Canceled", reason, compensationState = "Pending" }, ct);
        if (order.PaymentStatus is "AUTHORIZED" or "CAPTURED")
            await InsertOutboxAsync(connection, tx, context, id, "OrderCompensationRequired", new { eventId = Guid.NewGuid(), eventType = "OrderCompensationRequired", eventVersion = 1, occurredAt = DateTimeOffset.UtcNow, tenantId = context.TenantId, storeId = context.StoreId, correlationId = context.CorrelationId, orderId = id, compensationType = order.PaymentStatus == "AUTHORIZED" ? "PaymentVoid" : "PaymentRefund", reason }, ct);
        await InsertOutboxAsync(connection, tx, context, id, "InventoryReservationReleaseRequested", new { eventId = Guid.NewGuid(), eventType = "InventoryReservationReleaseRequested", eventVersion = 1, occurredAt = DateTimeOffset.UtcNow, tenantId = context.TenantId, storeId = context.StoreId, correlationId = context.CorrelationId, orderId = id, reason }, ct);
        await tx.CommitAsync(ct); return new(id, "CANCELED", "PENDING");
    }

    // @BR-OR-FUL-001: Fulfillment is requested only for a processed order containing physical purchased lines.
    // @BR-OR-RES-001: A fulfillment idempotency key creates at most one fulfillment order.
    public async Task<Fulfillment> RequestFulfillmentAsync(long id, string key, RequestContext context, CancellationToken ct)
    {
        await using var connection = await dataSource.OpenConnectionAsync(ct); await using var tx = await connection.BeginTransactionAsync(ct);
        var order = await FindAsync(connection, tx, id, context, ct) ?? throw NotFound(id);
        if (order.Status != "PROCESSED") throw new DomainException("FULFILLMENT_ORDER_NOT_READY", "Fulfillment requires an order in PROCESSED state.", 409);
        if (!order.Lines.Any() || order.Lines.All(x => x.IsDigital)) throw new DomainException("FULFILLMENT_ORDER_NOT_READY", "Fulfillment requires at least one physical purchased line.", 409);
        await using var select = new NpgsqlCommand("SELECT fulfillment_order_id,status,carrier_reference,last_updated_at FROM order_management.fulfillment_orders WHERE tenant_id=@tenant AND store_id=@store AND order_id=@order", connection, tx);
        Add(select, "tenant", context.TenantId); Add(select, "store", context.StoreNumber); Add(select, "order", id); await using var reader = await select.ExecuteReaderAsync(ct);
        if (await reader.ReadAsync(ct)) { var f = ReadFulfillment(reader); await reader.DisposeAsync(); await tx.CommitAsync(ct); return f; }
        await reader.DisposeAsync();
        var fulfillmentId = Guid.NewGuid();
        await using var insert = new NpgsqlCommand("INSERT INTO order_management.fulfillment_orders(fulfillment_order_id,order_id,tenant_id,store_id) VALUES(@id,@order,@tenant,@store)", connection, tx);
        Add(insert, "id", fulfillmentId); Add(insert, "order", id); Add(insert, "tenant", context.TenantId); Add(insert, "store", context.StoreNumber); await insert.ExecuteNonQueryAsync(ct);
        await using var status = new NpgsqlCommand("UPDATE order_management.orders SET fulfillment_status='REQUESTED',updated_at=now() WHERE order_id=@order", connection, tx); Add(status, "order", id); await status.ExecuteNonQueryAsync(ct);
        await InsertOutboxAsync(connection, tx, context, id, "FulfillmentRequested", new { eventId = Guid.NewGuid(), eventType = "FulfillmentRequested", eventVersion = 1, occurredAt = DateTimeOffset.UtcNow, tenantId = context.TenantId, storeId = context.StoreId, correlationId = context.CorrelationId, orderId = id, fulfillmentId, lines = order.Lines.Select(x => new { sku = x.Sku, productName = x.ProductName, quantity = x.Quantity, oneTimeCharge = x.OneTimeCharge }), deliverySnapshot = order.DeliveryAddress }, ct);
        await tx.CommitAsync(ct); return new(fulfillmentId, id, "REQUESTED", null, DateTimeOffset.UtcNow);
    }

    // @BR-OR-FUL-001: Shipment updates follow the fulfillment state machine and deliver the order only on confirmed delivery.
    // @BR-OR-RES-001: Shipment event identity is deduplicated before state application.
    public async Task<Fulfillment> ApplyShipmentAsync(long id, string status, string? carrierReference, RequestContext context, string eventId, CancellationToken ct)
    {
        await using var connection = await dataSource.OpenConnectionAsync(ct); await using var tx = await connection.BeginTransactionAsync(ct);
        if (await FindInboxAsync(connection, tx, context, eventId, ct) is not null) { await tx.CommitAsync(ct); return await GetFulfillmentAsync(id, context, ct) ?? throw NotFound(id); }
        var current = await GetFulfillmentAsync(connection, tx, id, context, ct) ?? throw new DomainException("FULFILLMENT_NOT_FOUND", "Fulfillment was not found.", 404);
        if (!FulfillmentLegal(current.Status, status)) throw new DomainException("FULFILLMENT_STATUS_INVALID", $"{current.Status} cannot transition to {status}.", 409);
        await InsertInboxAsync(connection, tx, context, eventId, "ShipmentStatusUpdated", null, ct);
        await using var update = new NpgsqlCommand("UPDATE order_management.fulfillment_orders SET status=@status,carrier_reference=coalesce(@reference,carrier_reference),last_updated_at=now(),version=version+1 WHERE fulfillment_order_id=@id", connection, tx);
        Add(update, "status", status); Add(update, "reference", (object?)carrierReference ?? DBNull.Value); Add(update, "id", current.Id); await update.ExecuteNonQueryAsync(ct);
        await using var history = new NpgsqlCommand("INSERT INTO order_management.fulfillment_status_history(fulfillment_order_id,status,source,external_reference) VALUES(@id,@status,'SHIPMENT',@reference)", connection, tx);
        Add(history, "id", current.Id); Add(history, "status", status); Add(history, "reference", (object?)carrierReference ?? DBNull.Value); await history.ExecuteNonQueryAsync(ct);
        if (status == "DELIVERED") { await using var order = new NpgsqlCommand("UPDATE order_management.orders SET fulfillment_status='DELIVERED',order_status=CASE WHEN order_status='PROCESSED' THEN 'DELIVERED' ELSE order_status END,updated_at=now() WHERE order_id=@order", connection, tx); Add(order, "order", id); await order.ExecuteNonQueryAsync(ct); }
        await tx.CommitAsync(ct); return await GetFulfillmentAsync(id, context, ct) ?? throw NotFound(id);
    }

    public async Task<List<OrderHistory>> HistoryAsync(long id, RequestContext context, CancellationToken ct)
    {
        await using var connection = await dataSource.OpenConnectionAsync(ct); await using var command = new NpgsqlCommand("SELECT order_status_history_id,order_id,status,date_added,comments,actor_id,source,customer_notified FROM order_management.order_status_history WHERE order_id=@id AND EXISTS(SELECT 1 FROM order_management.orders WHERE order_id=@id AND tenant_id=@tenant AND store_id=@store) ORDER BY date_added DESC,order_status_history_id DESC", connection);
        Add(command, "id", id); Add(command, "tenant", context.TenantId); Add(command, "store", context.StoreNumber); var result = new List<OrderHistory>(); await using var reader = await command.ExecuteReaderAsync(ct); while (await reader.ReadAsync(ct)) result.Add(ReadHistory(reader)); return result;
    }
    public async Task<List<PaymentOutcome>> PaymentsAsync(long id, RequestContext context, CancellationToken ct)
    {
        await using var connection = await dataSource.OpenConnectionAsync(ct); await using var command = new NpgsqlCommand("SELECT transaction_id,order_id,action,status,amount,currency,payment_reference,occurred_at FROM order_management.payment_outcomes WHERE order_id=@id AND tenant_id=@tenant AND store_id=@store ORDER BY occurred_at DESC", connection); Add(command, "id", id); Add(command, "tenant", context.TenantId); Add(command, "store", context.StoreNumber); var result = new List<PaymentOutcome>(); await using var reader = await command.ExecuteReaderAsync(ct); while (await reader.ReadAsync(ct)) result.Add(ReadPayment(reader)); return result;
    }
    public async Task<Fulfillment?> GetFulfillmentAsync(long id, RequestContext context, CancellationToken ct)
    {
        await using var connection = await dataSource.OpenConnectionAsync(ct); return await GetFulfillmentAsync(connection, null, id, context, ct);
    }
    public async Task<InvoiceState> RequestInvoiceAsync(long id, RequestContext context, CancellationToken ct)
    {
        await using var connection = await dataSource.OpenConnectionAsync(ct); await using var tx = await connection.BeginTransactionAsync(ct);
        var order = await FindAsync(connection, tx, id, context, ct) ?? throw NotFound(id);
        await LoadChildrenAsync(connection, order, ct);
        if (order.Totals.Count == 0) throw new DomainException("INVOICE_SNAPSHOT_INCOMPLETE", "An invoice requires accepted order totals.", 422);
        await using var existing = new NpgsqlCommand("SELECT request_id,status,artifact_url,generated_at FROM order_management.invoice_requests WHERE order_id=@order AND tenant_id=@tenant AND store_id=@store", connection, tx); Add(existing, "order", id); Add(existing, "tenant", context.TenantId); Add(existing, "store", context.StoreNumber); await using var er = await existing.ExecuteReaderAsync(ct);
        if (await er.ReadAsync(ct)) { var result = new InvoiceState(id, er.GetString(0), er.GetString(1), er.IsDBNull(2) ? null : er.GetString(2), er.IsDBNull(3) ? null : er.GetFieldValue<DateTimeOffset>(3)); await er.DisposeAsync(); await tx.CommitAsync(ct); return result; }
        await er.DisposeAsync();
        var requestId = $"inv-{id}-{Guid.NewGuid():N}";
        await using var insert = new NpgsqlCommand("INSERT INTO order_management.invoice_requests(request_id,tenant_id,store_id,order_id) VALUES(@request,@tenant,@store,@order)", connection, tx); Add(insert, "request", requestId); Add(insert, "tenant", context.TenantId); Add(insert, "store", context.StoreNumber); Add(insert, "order", id); await insert.ExecuteNonQueryAsync(ct);
        await InsertOutboxAsync(connection, tx, context, id, "InvoiceGenerationRequested", new { eventId = Guid.NewGuid(), eventType = "InvoiceGenerationRequested", eventVersion = 1, occurredAt = DateTimeOffset.UtcNow, tenantId = context.TenantId, storeId = context.StoreId, correlationId = context.CorrelationId, orderId = id, requestId, orderDate = order.DatePurchased, billingSnapshot = order.BillingAddress, lines = order.Lines, acceptedTotals = order.Totals, currency = order.CurrencyCode }, ct);
        await tx.CommitAsync(ct); return new(id, requestId, "PROCESSING", null, null);
    }

    public async Task MarkInvoiceAsync(string requestId, string status, string? url, RequestContext context, CancellationToken ct)
    {
        await using var connection = await dataSource.OpenConnectionAsync(ct); await using var command = new NpgsqlCommand("UPDATE order_management.invoice_requests SET status=@status,artifact_url=@url,generated_at=CASE WHEN @status='AVAILABLE' THEN now() ELSE generated_at END WHERE request_id=@request AND tenant_id=@tenant AND store_id=@store", connection); Add(command, "status", status); Add(command, "url", (object?)url ?? DBNull.Value); Add(command, "request", requestId); Add(command, "tenant", context.TenantId); Add(command, "store", context.StoreNumber); await command.ExecuteNonQueryAsync(ct);
    }
    public async Task<List<Guid>> PendingOutboxAsync(int limit, CancellationToken ct)
    {
        await using var connection = await dataSource.OpenConnectionAsync(ct); await using var command = new NpgsqlCommand("SELECT event_id FROM order_management.order_outbox WHERE published_at IS NULL ORDER BY occurred_at LIMIT @limit", connection); Add(command, "limit", limit); var result = new List<Guid>(); await using var reader = await command.ExecuteReaderAsync(ct); while (await reader.ReadAsync(ct)) result.Add(reader.GetGuid(0)); return result;
    }
    public async Task<byte[]> OutboxPayloadAsync(Guid id, CancellationToken ct)
    {
        await using var connection = await dataSource.OpenConnectionAsync(ct); await using var command = new NpgsqlCommand("SELECT payload::text FROM order_management.order_outbox WHERE event_id=@id", connection); Add(command, "id", id); return JsonSerializer.SerializeToUtf8Bytes(await command.ExecuteScalarAsync(ct) ?? "{}");
    }
    public async Task<string> OutboxEventTypeAsync(Guid id, CancellationToken ct)
    {
        await using var connection = await dataSource.OpenConnectionAsync(ct); await using var command = new NpgsqlCommand("SELECT event_type FROM order_management.order_outbox WHERE event_id=@id", connection); Add(command, "id", id); return (string?)await command.ExecuteScalarAsync(ct) ?? "OrderManagementEvent";
    }
    public async Task MarkOutboxAttemptAsync(Guid id, CancellationToken ct) { await using var c = await dataSource.OpenConnectionAsync(ct); await using var q = new NpgsqlCommand("UPDATE order_management.order_outbox SET delivery_attempts=delivery_attempts+1 WHERE event_id=@id", c); Add(q, "id", id); await q.ExecuteNonQueryAsync(ct); }
    public async Task MarkOutboxPublishedAsync(Guid id, CancellationToken ct) { await using var c = await dataSource.OpenConnectionAsync(ct); await using var q = new NpgsqlCommand("UPDATE order_management.order_outbox SET published_at=now() WHERE event_id=@id", c); Add(q, "id", id); await q.ExecuteNonQueryAsync(ct); }
    public async Task EnqueueCommandAsync(RequestContext context, long orderId, string type, object payload, string key, CancellationToken ct)
    {
        await using var connection = await dataSource.OpenConnectionAsync(ct);
        await using var transaction = await connection.BeginTransactionAsync(ct);
        if (await FindInboxAsync(connection, transaction, context, key, ct) is null)
        {
            if (await FindAsync(connection, transaction, orderId, context, ct) is null) throw NotFound(orderId);
            await InsertInboxAsync(connection, transaction, context, Guid.NewGuid().ToString(), type, key, ct);
            await InsertOutboxAsync(connection, transaction, context, orderId, type, payload, ct);
        }
        await transaction.CommitAsync(ct);
    }
    public async Task RecordProcessingFailureAsync(RequestContext context, string submissionId, string reason, CancellationToken ct)
    {
        await using var connection = await dataSource.OpenConnectionAsync(ct);
        await using var transaction = await connection.BeginTransactionAsync(ct);
        await InsertOutboxAsync(connection, transaction, context, 0, "OrderProcessingFailed", new
        {
            eventId = Guid.NewGuid(),
            eventType = "OrderProcessingFailed",
            eventVersion = 1,
            occurredAt = DateTimeOffset.UtcNow,
            tenantId = context.TenantId,
            storeId = context.StoreId,
            correlationId = context.CorrelationId,
            orderId = (long?)null,
            submissionId,
            failureCode = "ORDER_PROCESSING_FAILED",
            message = reason
        }, ct);
        await transaction.CommitAsync(ct);
    }

    private async Task LoadChildrenAsync(NpgsqlConnection connection, Order order, CancellationToken ct)
    {
        await using (var c = new NpgsqlCommand("SELECT first_name,last_name,company,address,city,state,country_code,zone_code,postal_code,telephone FROM order_management.order_billing_address WHERE order_id=@id", connection)) { Add(c, "id", order.OrderId); await using var r = await c.ExecuteReaderAsync(ct); if (await r.ReadAsync(ct)) order.BillingAddress = ReadAddress(r); }
        await using (var c = new NpgsqlCommand("SELECT first_name,last_name,company,address,city,state,country_code,zone_code,postal_code,telephone FROM order_management.order_delivery_address WHERE order_id=@id", connection)) { Add(c, "id", order.OrderId); await using var r = await c.ExecuteReaderAsync(ct); if (await r.ReadAsync(ct)) order.DeliveryAddress = ReadAddress(r); }
        await using (var c = new NpgsqlCommand("SELECT p.order_product_id,p.product_sku,p.product_name,p.product_quantity,p.onetime_charge,EXISTS(SELECT 1 FROM order_management.order_product_downloads d WHERE d.order_product_id=p.order_product_id) FROM order_management.order_products p WHERE p.order_id=@id ORDER BY p.order_product_id", connection)) { Add(c, "id", order.OrderId); await using var r = await c.ExecuteReaderAsync(ct); while (await r.ReadAsync(ct)) order.Lines.Add(new OrderLine { Id = r.GetInt64(0), Sku = r.IsDBNull(1) ? "" : r.GetString(1), ProductName = r.GetString(2), Quantity = r.GetInt32(3), OneTimeCharge = r.GetDecimal(4), IsDigital = r.GetBoolean(5) }); }
        foreach (var line in order.Lines)
        {
            await using (var c = new NpgsqlCommand("SELECT product_attribute_price,product_attribute_is_free,product_attribute_weight,product_option_id,product_option_value_id,product_attribute_name,product_attribute_val_name FROM order_management.order_product_attributes WHERE order_product_id=@id", connection))
            {
                Add(c, "id", line.Id);
                await using var r = await c.ExecuteReaderAsync(ct);
                while (await r.ReadAsync(ct))
                    line.Attributes.Add(new LineAttribute(r.GetInt64(3), r.GetInt64(4), r.IsDBNull(5) ? "" : r.GetString(5), r.IsDBNull(6) ? "" : r.GetString(6), r.GetDecimal(0), r.GetBoolean(1), r.IsDBNull(2) ? null : r.GetDecimal(2)));
            }

            await using (var p = new NpgsqlCommand("SELECT product_price_code,product_price_name,product_price,product_price_special,prd_price_special_st_dt,prd_price_special_end_dt,default_price FROM order_management.order_product_prices WHERE order_product_id=@id", connection))
            {
                Add(p, "id", line.Id);
                await using var pr = await p.ExecuteReaderAsync(ct);
                while (await pr.ReadAsync(ct))
                    line.Prices.Add(new LinePrice(pr.GetString(0), pr.IsDBNull(1) ? null : pr.GetString(1), pr.GetDecimal(2), pr.IsDBNull(3) ? null : pr.GetDecimal(3), pr.IsDBNull(4) ? null : pr.GetFieldValue<DateTimeOffset>(4), pr.IsDBNull(5) ? null : pr.GetFieldValue<DateTimeOffset>(5), pr.GetBoolean(6)));
            }
        }
        await using (var c = new NpgsqlCommand("SELECT order_total_id,code,title,text,value,module,order_total_type,order_value_type,sort_order,is_refund FROM order_management.order_totals WHERE order_id=@id ORDER BY sort_order,order_total_id", connection)) { Add(c, "id", order.OrderId); await using var r = await c.ExecuteReaderAsync(ct); while (await r.ReadAsync(ct)) order.Totals.Add(new OrderTotal(r.GetInt64(0), r.GetString(1), r.IsDBNull(2) ? null : r.GetString(2), r.IsDBNull(3) ? null : r.GetString(3), r.GetDecimal(4), r.IsDBNull(5) ? null : r.GetString(5), r.IsDBNull(6) ? null : r.GetString(6), r.IsDBNull(7) ? null : r.GetString(7), r.GetInt32(8), r.GetBoolean(9))); }
        await using (var c = new NpgsqlCommand("SELECT identifier,value FROM order_management.order_attributes WHERE order_id=@id", connection)) { Add(c, "id", order.OrderId); await using var r = await c.ExecuteReaderAsync(ct); while (await r.ReadAsync(ct)) order.Attributes.Add(new OrderAttribute(r.GetString(0), r.GetString(1))); }
        order.History.AddRange(await HistoryAsync(order.OrderId, new RequestContext(order.TenantId, order.StoreId.ToString(CultureInfo.InvariantCulture), ""), ct));
        await using (var c = new NpgsqlCommand("SELECT d.order_product_download_id,o.order_id,o.product_name,d.order_product_filename,d.download_count,d.download_maxdays,d.access_state,d.expires_at FROM order_management.order_product_downloads d JOIN order_management.order_products o ON o.order_product_id=d.order_product_id WHERE o.order_id=@id", connection)) { Add(c, "id", order.OrderId); await using var r = await c.ExecuteReaderAsync(ct); while (await r.ReadAsync(ct)) order.Downloads.Add(new DownloadEntitlement(r.GetInt64(0), r.GetInt64(1), r.GetString(2), r.GetString(3), r.GetInt32(4), r.GetInt32(5), r.GetString(6), r.IsDBNull(7) ? null : r.GetFieldValue<DateTimeOffset>(7))); }
    }

    private async Task<Order?> FindAsync(NpgsqlConnection connection, NpgsqlTransaction? tx, long id, RequestContext context, CancellationToken ct)
    {
        await using var command = new NpgsqlCommand("SELECT order_id,tenant_id,store_id,customer_id,customer_email_address,order_status,payment_status,fulfillment_status,currency_code,order_total,refunded_amount,refundable_amount,date_purchased,order_date_finished,payment_type,payment_module_code,shipping_module_code,customer_agreed,confirmed_address,locale FROM order_management.orders WHERE order_id=@id AND tenant_id=@tenant AND store_id=@store FOR UPDATE", connection, tx); Add(command, "id", id); Add(command, "tenant", context.TenantId); Add(command, "store", context.StoreNumber); await using var r = await command.ExecuteReaderAsync(ct); return await r.ReadAsync(ct) ? ReadOrder(r) : null;
    }
    private async Task<Order?> FindBySubmissionAsync(NpgsqlConnection c, NpgsqlTransaction? tx, RequestContext context, string submission, CancellationToken ct) { await using var q = new NpgsqlCommand("SELECT order_id FROM order_management.orders WHERE tenant_id=@tenant AND store_id=@store AND submission_id=@submission", c, tx); Add(q, "tenant", context.TenantId); Add(q, "store", context.StoreNumber); Add(q, "submission", submission); var value = await q.ExecuteScalarAsync(ct); return value is null ? null : await FindAsync((NpgsqlConnection)c, tx, (long)value, context, ct); }
    private async Task<object?> FindInboxAsync(NpgsqlConnection c, NpgsqlTransaction? tx, RequestContext context, string key, CancellationToken ct) { await using var q = new NpgsqlCommand("SELECT inbox_id FROM order_management.order_inbox WHERE tenant_id=@tenant AND store_id=@store AND (idempotency_key=@key OR message_id=@key)", c, tx); Add(q, "tenant", context.TenantId); Add(q, "store", context.StoreNumber); Add(q, "key", key); return await q.ExecuteScalarAsync(ct); }
    private static async Task InsertInboxAsync(NpgsqlConnection c, NpgsqlTransaction tx, RequestContext context, string messageId, string type, string? key, CancellationToken ct) { await using var q = new NpgsqlCommand("INSERT INTO order_management.order_inbox(tenant_id,store_id,message_id,message_type,idempotency_key,processed_at,processing_status) VALUES(@tenant,@store,@message,@type,@key,now(),'PROCESSED') ON CONFLICT DO NOTHING", c, tx); Add(q, "tenant", context.TenantId); Add(q, "store", context.StoreNumber); Add(q, "message", messageId); Add(q, "type", type); Add(q, "key", (object?)key ?? DBNull.Value); await q.ExecuteNonQueryAsync(ct); }
    private static async Task<OrderHistory> InsertHistoryAsync(NpgsqlConnection c, NpgsqlTransaction tx, long id, string? status, string? comments, string? actor, string source, string? eventId, CancellationToken ct) { await using var q = new NpgsqlCommand("INSERT INTO order_management.order_status_history(order_id,status,comments,actor_id,source,event_id) VALUES(@order,@status,@comments,@actor,@source,@event) RETURNING order_status_history_id,date_added,customer_notified", c, tx); Add(q, "order", id); Add(q, "status", (object?)status ?? DBNull.Value); Add(q, "comments", (object?)comments ?? DBNull.Value); Add(q, "actor", (object?)actor ?? DBNull.Value); Add(q, "source", source); Add(q, "event", (object?)eventId ?? DBNull.Value); await using var r = await q.ExecuteReaderAsync(ct); await r.ReadAsync(ct); return new(r.GetInt64(0), id, status, r.GetFieldValue<DateTimeOffset>(1), comments, actor, source, r.GetBoolean(2)); }
    private static async Task InsertAddressAsync(NpgsqlConnection c, NpgsqlTransaction tx, long id, AddressSnapshot? a, bool billing, CancellationToken ct) { if (a is null) return; var table = billing ? "order_billing_address" : "order_delivery_address"; await using var q = new NpgsqlCommand($"INSERT INTO order_management.{table}(order_id,first_name,last_name,company,address,city,state,country_code,zone_code,postal_code,telephone) VALUES(@id,@first,@last,@company,@address,@city,@state,@country,@zone,@postal,@phone)", c, tx); AddAddress(q, id, a); await q.ExecuteNonQueryAsync(ct); }
    private static async Task UpsertAddressAsync(NpgsqlConnection c, NpgsqlTransaction tx, long id, AddressSnapshot a, bool billing, CancellationToken ct) { var table = billing ? "order_billing_address" : "order_delivery_address"; await using var q = new NpgsqlCommand($"INSERT INTO order_management.{table}(order_id,first_name,last_name,company,address,city,state,country_code,zone_code,postal_code,telephone) VALUES(@id,@first,@last,@company,@address,@city,@state,@country,@zone,@postal,@phone) ON CONFLICT(order_id) DO UPDATE SET first_name=excluded.first_name,last_name=excluded.last_name,company=excluded.company,address=excluded.address,city=excluded.city,state=excluded.state,country_code=excluded.country_code,zone_code=excluded.zone_code,postal_code=excluded.postal_code,telephone=excluded.telephone", c, tx); AddAddress(q, id, a); await q.ExecuteNonQueryAsync(ct); }
    private static void AddAddress(NpgsqlCommand q, long id, AddressSnapshot a) { Add(q, "id", id); Add(q, "first", a.FirstName); Add(q, "last", a.LastName); Add(q, "company", (object?)a.Company ?? DBNull.Value); Add(q, "address", a.Address); Add(q, "city", a.City); Add(q, "state", (object?)a.State ?? DBNull.Value); Add(q, "country", a.CountryCode); Add(q, "zone", (object?)a.ZoneCode ?? DBNull.Value); Add(q, "postal", a.PostalCode); Add(q, "phone", (object?)a.Telephone ?? DBNull.Value); }
    private static async Task InsertPriceAsync(NpgsqlConnection c, NpgsqlTransaction tx, long id, SubmissionPrice p, CancellationToken ct) { await using var q = new NpgsqlCommand("INSERT INTO order_management.order_product_prices(order_product_id,product_price_code,product_price,product_price_special,prd_price_special_st_dt,prd_price_special_end_dt,default_price,product_price_name) VALUES(@id,@code,@price,@special,@start,@end,@default,@name)", c, tx); Add(q, "id", id); Add(q, "code", p.Code); Add(q, "price", p.Price); Add(q, "special", p.SpecialPrice is null ? DBNull.Value : p.SpecialPrice.Value); Add(q, "start", p.SpecialStartDate is null ? DBNull.Value : p.SpecialStartDate.Value); Add(q, "end", p.SpecialEndDate is null ? DBNull.Value : p.SpecialEndDate.Value); Add(q, "default", p.DefaultPrice); Add(q, "name", (object?)p.Name ?? DBNull.Value); await q.ExecuteNonQueryAsync(ct); }
    private static async Task InsertOutboxAsync(NpgsqlConnection c, NpgsqlTransaction tx, RequestContext context, long aggregate, string type, object payload, CancellationToken ct) { await using var q = new NpgsqlCommand("INSERT INTO order_management.order_outbox(tenant_id,store_id,aggregate_type,aggregate_id,event_type,payload) VALUES(@tenant,@store,'Order',@aggregate,@type,CAST(@payload AS jsonb))", c, tx); Add(q, "tenant", context.TenantId); Add(q, "store", context.StoreNumber); Add(q, "aggregate", aggregate); Add(q, "type", type); Add(q, "payload", JsonSerializer.Serialize(payload)); await q.ExecuteNonQueryAsync(ct); }
    private static async Task<decimal> SumPaymentAsync(NpgsqlConnection c, NpgsqlTransaction tx, long id, string action, CancellationToken ct) { await using var q = new NpgsqlCommand("SELECT coalesce(sum(amount),0) FROM order_management.payment_outcomes WHERE order_id=@id AND action=@action AND status='SUCCEEDED'", c, tx); Add(q, "id", id); Add(q, "action", action); return (decimal)(await q.ExecuteScalarAsync(ct) ?? 0m); }
    private async Task<Fulfillment?> GetFulfillmentAsync(NpgsqlConnection c, NpgsqlTransaction? tx, long orderId, RequestContext context, CancellationToken ct) { await using var q = new NpgsqlCommand("SELECT fulfillment_order_id,order_id,status,carrier_reference,last_updated_at FROM order_management.fulfillment_orders WHERE order_id=@order AND tenant_id=@tenant AND store_id=@store", c, tx); Add(q, "order", orderId); Add(q, "tenant", context.TenantId); Add(q, "store", context.StoreNumber); await using var r = await q.ExecuteReaderAsync(ct); return await r.ReadAsync(ct) ? ReadFulfillment(r) : null; }
    private static Order ReadOrder(NpgsqlDataReader r) => new() { OrderId = r.GetInt64(0), TenantId = r.GetString(1), StoreId = r.GetInt64(2), CustomerId = r.IsDBNull(3) ? null : r.GetInt64(3), CustomerEmailAddress = r.GetString(4), Status = r.GetString(5), PaymentStatus = r.GetString(6), FulfillmentStatus = r.GetString(7), CurrencyCode = r.GetString(8).Trim(), Total = r.GetDecimal(9), RefundedAmount = r.GetDecimal(10), RefundableAmount = r.GetDecimal(11), DatePurchased = r.GetFieldValue<DateTimeOffset>(12), OrderDateFinished = r.IsDBNull(13) ? null : r.GetFieldValue<DateTimeOffset>(13), PaymentType = r.IsDBNull(14) ? null : r.GetString(14), PaymentModuleCode = r.IsDBNull(15) ? null : r.GetString(15), ShippingModuleCode = r.IsDBNull(16) ? null : r.GetString(16), CustomerAgreed = r.GetBoolean(17), ConfirmedAddress = r.GetBoolean(18), Locale = r.IsDBNull(19) ? null : r.GetString(19) };
    private static AddressSnapshot ReadAddress(NpgsqlDataReader r) => new(r.IsDBNull(0) ? "" : r.GetString(0), r.IsDBNull(1) ? "" : r.GetString(1), r.IsDBNull(2) ? null : r.GetString(2), r.IsDBNull(3) ? "" : r.GetString(3), r.IsDBNull(4) ? "" : r.GetString(4), r.IsDBNull(5) ? null : r.GetString(5), r.IsDBNull(6) ? "" : r.GetString(6).Trim(), r.IsDBNull(7) ? null : r.GetString(7), r.IsDBNull(8) ? "" : r.GetString(8), r.IsDBNull(9) ? null : r.GetString(9));
    private static OrderHistory ReadHistory(NpgsqlDataReader r) => new(r.GetInt64(0), r.GetInt64(1), r.IsDBNull(2) ? null : r.GetString(2), r.GetFieldValue<DateTimeOffset>(3), r.IsDBNull(4) ? null : r.GetString(4), r.IsDBNull(5) ? null : r.GetString(5), r.GetString(6), r.GetBoolean(7));
    private static PaymentOutcome ReadPayment(NpgsqlDataReader r) => new(r.GetString(0), r.GetInt64(1), r.GetString(2), r.GetString(3), r.GetDecimal(4), r.GetString(5).Trim(), r.IsDBNull(6) ? null : r.GetString(6), r.GetFieldValue<DateTimeOffset>(7));
    private static Fulfillment ReadFulfillment(NpgsqlDataReader r) => new(r.GetGuid(0), r.GetInt64(1), r.GetString(2), r.IsDBNull(3) ? null : r.GetString(3), r.GetFieldValue<DateTimeOffset>(4));
    private static void AddCommon(NpgsqlCommand q, RequestContext context, long? customer, long? id, string? status, string? email, string? name, string? phone) { Add(q, "tenant", context.TenantId); Add(q, "store", context.StoreNumber); AddNullable(q, "customer", customer); AddNullable(q, "orderId", id); Add(q, "status", (object?)status ?? DBNull.Value); Add(q, "email", (object?)email ?? DBNull.Value); Add(q, "name", name is null ? DBNull.Value : $"%{name}%"); Add(q, "phone", (object?)phone ?? DBNull.Value); }
    private static void AddNullable(NpgsqlCommand q, string name, long? value) { var parameter = q.Parameters.Add(name, NpgsqlTypes.NpgsqlDbType.Bigint); parameter.Value = value ?? (object)DBNull.Value; }
    private static void AddNullable(NpgsqlCommand q, string name, DateTimeOffset? value) { var parameter = q.Parameters.Add(name, NpgsqlTypes.NpgsqlDbType.TimestampTz); parameter.Value = value ?? (object)DBNull.Value; }
    private static void Add(NpgsqlCommand q, string name, object value) => q.Parameters.AddWithValue(name, value);
    private static bool Legal(string current, string next) => current switch { "ORDERED" => next is "PROCESSED" or "CANCELED", "PROCESSED" => next is "DELIVERED" or "REFUNDED" or "CANCELED", "DELIVERED" => next == "REFUNDED", _ => false };
    private static bool FulfillmentLegal(string current, string next) => current switch { "REQUESTED" => next is "IN_PROGRESS" or "CANCELED", "IN_PROGRESS" => next is "SHIPPED" or "CANCELED", "SHIPPED" => next == "DELIVERED", _ => false };
    private static void ValidateAddress(AddressSnapshot a) { if (string.IsNullOrWhiteSpace(a.FirstName) || string.IsNullOrWhiteSpace(a.LastName) || string.IsNullOrWhiteSpace(a.Address) || string.IsNullOrWhiteSpace(a.City) || string.IsNullOrWhiteSpace(a.CountryCode) || string.IsNullOrWhiteSpace(a.PostalCode)) throw new DomainException("ADDRESS_INVALID", "Billing and delivery snapshots require firstName, lastName, address, city, countryCode and postalCode.", 422); }
    private static DomainException NotFound(long id) => new("ORDER_NOT_FOUND", $"Order {id} was not found in this store.", 404);
}

public sealed record RefundReservation(string RefundId, decimal Amount, string Status, decimal RemainingRefundable);
public sealed record CancellationResult(long OrderId, string Status, string CompensationState);
