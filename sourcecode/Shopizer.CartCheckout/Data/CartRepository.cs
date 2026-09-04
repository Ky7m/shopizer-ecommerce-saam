using System.Data;
using System.Text.Json;
using Npgsql;
using NpgsqlTypes;
using Shopizer.CartCheckout.DTOs;
using Shopizer.CartCheckout.Models;

namespace Shopizer.CartCheckout.Data;

public sealed class CartRepository(NpgsqlDataSource dataSource)
{
    private static void Add(NpgsqlCommand command, string name, object? value) =>
        command.Parameters.AddWithValue(name, value ?? DBNull.Value);
    private static void Json(NpgsqlCommand command, string name, object value) =>
        command.Parameters.Add(name, NpgsqlDbType.Jsonb).Value = JsonSerializer.Serialize(value);

    public async Task<Cart?> FindByCodeAsync(string code, RequestContext context, CancellationToken ct)
    {
        await using var connection = await dataSource.OpenConnectionAsync(ct);
        await using var command = new NpgsqlCommand("""
            SELECT cart_id,cart_code,tenant_id,store_id,customer_id,submitted_order_id,status::text,promo_code,promo_added_at,currency_code
            FROM cart_checkout_schema.shopping_cart
            WHERE cart_code=@code AND tenant_id=@tenant AND store_id=@store
            """, connection);
        Add(command, "code", code); Add(command, "tenant", context.TenantId); Add(command, "store", context.StoreId);
        await using var reader = await command.ExecuteReaderAsync(ct);
        if (!await reader.ReadAsync(ct)) return null;
        var cart = ReadCart(reader);
        await reader.CloseAsync();
        await LoadItemsAsync(connection, cart, ct);
        return cart;
    }

    public async Task<Cart?> FindByIdAsync(long id, RequestContext context, CancellationToken ct)
    {
        await using var connection = await dataSource.OpenConnectionAsync(ct);
        await using var command = new NpgsqlCommand("""
            SELECT cart_id,cart_code,tenant_id,store_id,customer_id,submitted_order_id,status::text,promo_code,promo_added_at,currency_code
            FROM cart_checkout_schema.shopping_cart
            WHERE cart_id=@id AND tenant_id=@tenant AND store_id=@store
            """, connection);
        Add(command, "id", id); Add(command, "tenant", context.TenantId); Add(command, "store", context.StoreId);
        await using var reader = await command.ExecuteReaderAsync(ct);
        if (!await reader.ReadAsync(ct)) return null;
        var cart = ReadCart(reader);
        await reader.CloseAsync();
        await LoadItemsAsync(connection, cart, ct);
        return cart;
    }

    public async Task<Cart?> FindOpenCustomerCartAsync(long customerId, RequestContext context, CancellationToken ct)
    {
        await using var connection = await dataSource.OpenConnectionAsync(ct);
        await using var command = new NpgsqlCommand("""
            SELECT cart_id,cart_code,tenant_id,store_id,customer_id,submitted_order_id,status::text,promo_code,promo_added_at,currency_code
            FROM cart_checkout_schema.shopping_cart
            WHERE customer_id=@customer AND tenant_id=@tenant AND store_id=@store AND status='OPEN'
            ORDER BY updated_at DESC LIMIT 1
            """, connection);
        Add(command, "customer", customerId); Add(command, "tenant", context.TenantId); Add(command, "store", context.StoreId);
        await using var reader = await command.ExecuteReaderAsync(ct);
        if (!await reader.ReadAsync(ct)) return null;
        var cart = ReadCart(reader);
        await reader.CloseAsync();
        await LoadItemsAsync(connection, cart, ct);
        return cart;
    }

    private static Cart ReadCart(NpgsqlDataReader reader) => new()
    {
        Id = reader.GetInt64(0), Code = reader.GetString(1), TenantId = reader.GetString(2), StoreId = reader.GetString(3),
        CustomerId = reader.IsDBNull(4) ? null : reader.GetInt64(4),
        SubmittedOrderId = reader.IsDBNull(5) ? null : reader.GetGuid(5), Status = reader.GetString(6),
        PromoCode = reader.IsDBNull(7) ? null : reader.GetString(7),
        PromoAddedAt = reader.IsDBNull(8) ? null : reader.GetFieldValue<DateTimeOffset>(8),
        CurrencyCode = reader.GetString(9)
    };

    private static async Task LoadItemsAsync(NpgsqlConnection connection, Cart cart, CancellationToken ct)
    {
        await using var command = new NpgsqlCommand("""
            SELECT cart_item_id,product_id,sku,variant_id,quantity,item_price,sub_total,obsolete
            FROM cart_checkout_schema.shopping_cart_item WHERE cart_id=@cart ORDER BY cart_item_id
            """, connection);
        Add(command, "cart", cart.Id);
        await using var reader = await command.ExecuteReaderAsync(ct);
        var items = new List<CartLine>();
        while (await reader.ReadAsync(ct))
            items.Add(new CartLine
            {
                Id = reader.GetInt64(0), CartId = cart.Id, ProductId = reader.GetInt64(1), Sku = reader.GetString(2),
                VariantId = reader.IsDBNull(3) ? null : reader.GetInt64(3), Quantity = reader.GetInt32(4),
                UnitPrice = reader.GetDecimal(5), SubTotal = reader.GetDecimal(6), Obsolete = reader.GetBoolean(7)
            });
        await reader.CloseAsync();
        foreach (var item in items)
        {
            await using var attributes = new NpgsqlCommand("""
                SELECT product_attribute_id FROM cart_checkout_schema.shopping_cart_attr_item
                WHERE cart_item_id=@item ORDER BY cart_attribute_id
                """, connection);
            Add(attributes, "item", item.Id);
            await using var attributeReader = await attributes.ExecuteReaderAsync(ct);
            while (await attributeReader.ReadAsync(ct)) item.Attributes.Add(attributeReader.GetInt64(0));
            await attributeReader.CloseAsync();
            cart.Items.Add(item);
        }
    }

    public async Task<Cart> CreateAsync(string? code, long? customerId, string? promoCode, ProductFact product,
        int quantity, IReadOnlyCollection<long> attributes, decimal price, string currency, RequestContext context, CancellationToken ct)
    {
        await using var connection = await dataSource.OpenConnectionAsync(ct);
        await using var transaction = await connection.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);
        var cartCode = string.IsNullOrWhiteSpace(code) ? Guid.NewGuid().ToString("N") : code;
        var cartId = await InsertCartAsync(connection, transaction, cartCode, customerId, promoCode, currency, context, ct);
        var cart = new Cart { Id = cartId, Code = cartCode, CustomerId = customerId, TenantId = context.TenantId, StoreId = context.StoreId, PromoCode = promoCode, PromoAddedAt = promoCode is null ? null : DateTimeOffset.UtcNow, CurrencyCode = currency };
        var line = await InsertLineAsync(connection, transaction, cartId, product, quantity, attributes, price, ct);
        cart.Items.Add(line);
        await transaction.CommitAsync(ct);
        return cart;
    }

    private static async Task<long> InsertCartAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, string code,
        long? customerId, string? promoCode, string currency, RequestContext context, CancellationToken ct)
    {
        await using var command = new NpgsqlCommand("""
            INSERT INTO cart_checkout_schema.shopping_cart(cart_code,tenant_id,store_id,customer_id,promo_code,promo_added_at,currency_code,created_by,updated_by,correlation_id)
            VALUES(@code,@tenant,@store,@customer,@promo,@promoAt,@currency,@actor,@actor,@correlation) RETURNING cart_id
            """, connection, transaction);
        Add(command, "code", code); Add(command, "tenant", context.TenantId); Add(command, "store", context.StoreId);
        Add(command, "customer", customerId); Add(command, "promo", promoCode); Add(command, "promoAt", promoCode is null ? null : DateTimeOffset.UtcNow);
        Add(command, "currency", currency);
        Add(command, "actor", customerId?.ToString()); Add(command, "correlation", Guid.TryParse(context.CorrelationId, out var correlation) ? correlation : null);
        try { return (long)(await command.ExecuteScalarAsync(ct) ?? throw new InvalidOperationException("Cart insert returned no id")); }
        catch (PostgresException ex) when (ex.SqlState == "23505") { throw new DomainException("CART_CODE_CONFLICT", "The cart code is already in use for this store", 409); }
    }

    private static async Task<CartLine> InsertLineAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, long cartId,
        ProductFact product, int quantity, IEnumerable<long> attributes, decimal price, CancellationToken ct)
    {
        await using var command = new NpgsqlCommand("""
            INSERT INTO cart_checkout_schema.shopping_cart_item(cart_id,product_id,sku,variant_id,quantity,item_price,sub_total)
            VALUES(@cart,@product,@sku,@variant,@quantity,@price,@subtotal) RETURNING cart_item_id
            """, connection, transaction);
        Add(command, "cart", cartId); Add(command, "product", product.NumericId); Add(command, "sku", product.Sku);
        Add(command, "variant", null); Add(command, "quantity", quantity); Add(command, "price", price); Add(command, "subtotal", price * quantity);
        var id = (long)(await command.ExecuteScalarAsync(ct) ?? throw new InvalidOperationException("Cart line insert returned no id"));
        foreach (var attribute in attributes)
        {
            await using var attributeCommand = new NpgsqlCommand("""
                INSERT INTO cart_checkout_schema.shopping_cart_attr_item(cart_item_id,product_attribute_id) VALUES(@item,@attribute)
                """, connection, transaction);
            Add(attributeCommand, "item", id); Add(attributeCommand, "attribute", attribute);
            await attributeCommand.ExecuteNonQueryAsync(ct);
        }
        return new CartLine { Id = id, CartId = cartId, ProductId = product.NumericId, ProviderProductId = product.Id, Sku = product.Sku, Quantity = quantity, UnitPrice = price, SubTotal = price * quantity };
    }

    public async Task SaveAsync(Cart cart, RequestContext context, CancellationToken ct)
    {
        await using var connection = await dataSource.OpenConnectionAsync(ct);
        await using var transaction = await connection.BeginTransactionAsync(ct);
        await using (var update = new NpgsqlCommand("""
            UPDATE cart_checkout_schema.shopping_cart SET customer_id=@customer,status=@status::cart_checkout_schema.cart_status,
              promo_code=@promo,promo_added_at=@promoAt,currency_code=@currency,updated_at=now(),updated_by=@actor
            WHERE cart_id=@id AND tenant_id=@tenant AND store_id=@store
            """, connection, transaction))
        {
            Add(update, "customer", cart.CustomerId); Add(update, "status", cart.Status); Add(update, "promo", cart.PromoCode);
            Add(update, "promoAt", cart.PromoAddedAt); Add(update, "currency", cart.CurrencyCode); Add(update, "actor", cart.CustomerId?.ToString()); Add(update, "id", cart.Id);
            Add(update, "tenant", context.TenantId); Add(update, "store", context.StoreId);
            if (await update.ExecuteNonQueryAsync(ct) != 1) throw new DomainException("CART_SCOPE_MISMATCH", "Cart is not available in the requested store", 403);
        }
        await using (var deleteAttributes = new NpgsqlCommand("""
            DELETE FROM cart_checkout_schema.shopping_cart_attr_item WHERE cart_item_id IN
              (SELECT cart_item_id FROM cart_checkout_schema.shopping_cart_item WHERE cart_id=@cart)
            """, connection, transaction)) { Add(deleteAttributes, "cart", cart.Id); await deleteAttributes.ExecuteNonQueryAsync(ct); }
        await using (var deleteItems = new NpgsqlCommand("DELETE FROM cart_checkout_schema.shopping_cart_item WHERE cart_id=@cart", connection, transaction))
        { Add(deleteItems, "cart", cart.Id); await deleteItems.ExecuteNonQueryAsync(ct); }
        foreach (var item in cart.Items.Where(x => x.Quantity > 0 && !x.Obsolete))
        {
            var product = new ProductFact(item.ProviderProductId, item.ProductId, cart.StoreId, item.Sku, true, true, null, false, true, item.Sku, "", item.UnitPrice, item.Attributes.ToHashSet());
            await InsertLineAsync(connection, transaction, cart.Id, product, item.Quantity, item.Attributes, item.UnitPrice, ct);
        }
        await transaction.CommitAsync(ct);
    }

    public async Task MarkOrphanedAsync(long cartId, RequestContext context, CancellationToken ct)
    {
        await using var connection = await dataSource.OpenConnectionAsync(ct);
        await using var command = new NpgsqlCommand("""
            UPDATE cart_checkout_schema.shopping_cart SET status='OBSOLETE',updated_at=now()
            WHERE cart_id=@id AND tenant_id=@tenant AND store_id=@store
            """, connection);
        Add(command, "id", cartId); Add(command, "tenant", context.TenantId); Add(command, "store", context.StoreId);
        await command.ExecuteNonQueryAsync(ct);
    }

    public async Task SaveQuoteReferenceAsync(long cartId, string providerReference, string kind, string? expiresAt,
        RequestContext context, CancellationToken ct)
    {
        var expiry = DateTimeOffset.TryParse(expiresAt, out var parsed) ? parsed : DateTimeOffset.UtcNow.AddMinutes(15);
        await using var connection = await dataSource.OpenConnectionAsync(ct);
        await using var command = new NpgsqlCommand("""
            INSERT INTO cart_checkout_schema.cart_quote_reference(tenant_id,store_id,cart_id,quote_kind,provider_quote_reference,expires_at)
            VALUES(@tenant,@store,@cart,@kind::cart_checkout_schema.quote_kind,@reference,@expires)
            ON CONFLICT (tenant_id,store_id,cart_id,quote_kind,provider_quote_reference)
            DO UPDATE SET expires_at=EXCLUDED.expires_at
            """, connection);
        Add(command, "tenant", context.TenantId); Add(command, "store", context.StoreId); Add(command, "cart", cartId);
        Add(command, "kind", kind); Add(command, "reference", providerReference); Add(command, "expires", expiry);
        await command.ExecuteNonQueryAsync(ct);
    }

    public async Task<bool> HasActiveQuoteAsync(long cartId, string providerReference, string kind, RequestContext context, CancellationToken ct)
    {
        await using var connection = await dataSource.OpenConnectionAsync(ct);
        await using var command = new NpgsqlCommand("""
            SELECT EXISTS(SELECT 1 FROM cart_checkout_schema.cart_quote_reference
            WHERE cart_id=@cart AND tenant_id=@tenant AND store_id=@store AND quote_kind=@kind::cart_checkout_schema.quote_kind
              AND provider_quote_reference=@reference AND expires_at>now())
            """, connection);
        Add(command, "cart", cartId); Add(command, "tenant", context.TenantId); Add(command, "store", context.StoreId);
        Add(command, "kind", kind); Add(command, "reference", providerReference);
        return (bool)(await command.ExecuteScalarAsync(ct) ?? false);
    }

    public async Task<Guid> CreatePaymentSubmissionAsync(long cartId, long? customerId, string currency,
        PaymentInitializationRequestDto request, RequestContext context, CancellationToken ct)
    {
        var sessionId = Guid.NewGuid();
        await using var connection = await dataSource.OpenConnectionAsync(ct);
        await using var transaction = await connection.BeginTransactionAsync(ct);
        await using (var session = new NpgsqlCommand("""
            INSERT INTO cart_checkout_schema.checkout_session(checkout_session_id,cart_id,customer_id,tenant_id,store_id,state,currency_code,expires_at,correlation_id)
            VALUES(@id,@cart,@customer,@tenant,@store,'OPEN',@currency,@expires,@correlation)
            """, connection, transaction))
        {
            Add(session, "id", sessionId); Add(session, "cart", cartId); Add(session, "customer", customerId);
            Add(session, "tenant", context.TenantId); Add(session, "store", context.StoreId); Add(session, "currency", currency);
            Add(session, "expires", DateTimeOffset.UtcNow.AddHours(1));
            Add(session, "correlation", Guid.TryParse(context.CorrelationId, out var correlation) ? correlation : null);
            await session.ExecuteNonQueryAsync(ct);
        }
        await using (var submission = new NpgsqlCommand("""
            INSERT INTO cart_checkout_schema.checkout_submission(checkout_session_id,tenant_id,store_id,currency_code,amount,payment_module,payment_type,state)
            VALUES(@session,@tenant,@store,@currency,@amount,@module,@type,'PENDING')
            """, connection, transaction))
        {
            Add(submission, "session", sessionId); Add(submission, "tenant", context.TenantId); Add(submission, "store", context.StoreId);
            Add(submission, "currency", currency); Add(submission, "amount", decimal.TryParse(request.Amount, out var amount) ? amount : 0);
            Add(submission, "module", request.PaymentModule); Add(submission, "type", request.PaymentType);
            await submission.ExecuteNonQueryAsync(ct);
        }
        await transaction.CommitAsync(ct);
        return sessionId;
    }

    public async Task<PaymentInitializationResponseDto?> BeginPaymentIdempotencyAsync(
        long cartId, long? customerId, string idempotencyKey, string requestHash, RequestContext context, CancellationToken ct)
    {
        await using var connection = await dataSource.OpenConnectionAsync(ct);
        await using var transaction = await connection.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        await using (var insert = new NpgsqlCommand("""
            INSERT INTO cart_checkout_schema.checkout_idempotency_key
                (tenant_id,store_id,customer_id,cart_id,operation,idempotency_key,request_hash)
            VALUES(@tenant,@store,@customer,@cart,'payment-init',@key,@hash)
            ON CONFLICT DO NOTHING
            """, connection, transaction))
        {
            Add(insert, "tenant", context.TenantId); Add(insert, "store", context.StoreId); Add(insert, "customer", customerId);
            Add(insert, "cart", cartId); Add(insert, "key", idempotencyKey); Add(insert, "hash", requestHash);
            await insert.ExecuteNonQueryAsync(ct);
        }

        await using var select = new NpgsqlCommand("""
            SELECT request_hash,state::text,original_response
            FROM cart_checkout_schema.checkout_idempotency_key
            WHERE tenant_id=@tenant AND store_id=@store AND customer_id IS NOT DISTINCT FROM @customer
              AND cart_id=@cart AND operation='payment-init' AND idempotency_key=@key
            FOR UPDATE
            """, connection, transaction);
        Add(select, "tenant", context.TenantId); Add(select, "store", context.StoreId); Add(select, "customer", customerId);
        Add(select, "cart", cartId); Add(select, "key", idempotencyKey);
        await using var reader = await select.ExecuteReaderAsync(ct);
        if (!await reader.ReadAsync(ct))
            throw new DomainException("CHECKOUT_UNAVAILABLE", "The durable payment idempotency record could not be stored", 503);
        if (!reader.GetString(0).Equals(requestHash, StringComparison.Ordinal))
            throw new DomainException("IDEMPOTENCY_KEY_REUSED", "The idempotency key was already used with a different request", 409);
        var state = reader.GetString(1);
        if (state == "COMPLETED" && !reader.IsDBNull(2))
        {
            var replay = JsonSerializer.Deserialize<PaymentInitializationResponseDto>(reader.GetString(2))
                ?? throw new DomainException("CHECKOUT_UNAVAILABLE", "The stored payment response is invalid", 503);
            await reader.CloseAsync();
            await transaction.CommitAsync(ct);
            return replay;
        }
        await reader.CloseAsync();
        if (state == "IN_PROGRESS")
            throw new DomainException("CHECKOUT_IN_PROGRESS", "The payment request is already being processed", 409);
        await using var retry = new NpgsqlCommand("""
            UPDATE cart_checkout_schema.checkout_idempotency_key
            SET state='IN_PROGRESS',original_status=NULL,original_response=NULL,updated_at=now()
            WHERE tenant_id=@tenant AND store_id=@store AND customer_id IS NOT DISTINCT FROM @customer
              AND cart_id=@cart AND operation='payment-init' AND idempotency_key=@key
            """, connection, transaction);
        Add(retry, "tenant", context.TenantId); Add(retry, "store", context.StoreId); Add(retry, "customer", customerId);
        Add(retry, "cart", cartId); Add(retry, "key", idempotencyKey);
        await retry.ExecuteNonQueryAsync(ct);
        await transaction.CommitAsync(ct);
        return null;
    }

    public async Task CompletePaymentIdempotencyAsync(
        long cartId, long? customerId, string idempotencyKey, PaymentInitializationResponseDto response,
        RequestContext context, CancellationToken ct)
    {
        await using var connection = await dataSource.OpenConnectionAsync(ct);
        await using var command = new NpgsqlCommand("""
            UPDATE cart_checkout_schema.checkout_idempotency_key
            SET state='COMPLETED',original_status=202,original_response=@response,updated_at=now()
            WHERE tenant_id=@tenant AND store_id=@store AND customer_id IS NOT DISTINCT FROM @customer
              AND cart_id=@cart AND operation='payment-init' AND idempotency_key=@key
            """, connection);
        Json(command, "response", response);
        Add(command, "tenant", context.TenantId); Add(command, "store", context.StoreId); Add(command, "customer", customerId);
        Add(command, "cart", cartId); Add(command, "key", idempotencyKey);
        if (await command.ExecuteNonQueryAsync(ct) != 1)
            throw new DomainException("CHECKOUT_UNAVAILABLE", "The payment idempotency response could not be stored", 503);
    }

    public async Task FailPaymentIdempotencyAsync(
        long cartId, long? customerId, string idempotencyKey, RequestContext context, CancellationToken ct)
    {
        await using var connection = await dataSource.OpenConnectionAsync(ct);
        await using var command = new NpgsqlCommand("""
            UPDATE cart_checkout_schema.checkout_idempotency_key
            SET state='FAILED',updated_at=now()
            WHERE tenant_id=@tenant AND store_id=@store AND customer_id IS NOT DISTINCT FROM @customer
              AND cart_id=@cart AND operation='payment-init' AND idempotency_key=@key
            """, connection);
        Add(command, "tenant", context.TenantId); Add(command, "store", context.StoreId); Add(command, "customer", customerId);
        Add(command, "cart", cartId); Add(command, "key", idempotencyKey);
        await command.ExecuteNonQueryAsync(ct);
    }

    public async Task<CheckoutResult> PersistCheckoutAsync(Cart cart, long? customerId, string currency, string requestHash,
        string idempotencyKey, PaymentRequestDto payment, decimal subtotal, decimal discount, decimal shipping, decimal handling,
        decimal tax, decimal grandTotal, IReadOnlyList<ProductFact> products,
        RequestContext context, CancellationToken ct)
    {
        await using var connection = await dataSource.OpenConnectionAsync(ct);
        await using var transaction = await connection.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var idempotency = Guid.NewGuid();
        await using (var insertKey = new NpgsqlCommand("""
            INSERT INTO cart_checkout_schema.checkout_idempotency_key(idempotency_record_id,tenant_id,store_id,customer_id,cart_id,operation,idempotency_key,request_hash)
            VALUES(@id,@tenant,@store,@customer,@cart,'checkout',@key,@hash) ON CONFLICT DO NOTHING
            """, connection, transaction))
        {
            Add(insertKey, "id", idempotency); Add(insertKey, "tenant", context.TenantId); Add(insertKey, "store", context.StoreId);
            Add(insertKey, "customer", customerId); Add(insertKey, "cart", cart.Id); Add(insertKey, "key", idempotencyKey); Add(insertKey, "hash", requestHash);
            await insertKey.ExecuteNonQueryAsync(ct);
        }
        await using (var existing = new NpgsqlCommand("""
            SELECT request_hash,state::text,original_response FROM cart_checkout_schema.checkout_idempotency_key
            WHERE tenant_id=@tenant AND store_id=@store AND customer_id IS NOT DISTINCT FROM @customer
              AND cart_id=@cart AND operation='checkout' AND idempotency_key=@key FOR UPDATE
            """, connection, transaction))
        {
            Add(existing, "tenant", context.TenantId); Add(existing, "store", context.StoreId); Add(existing, "customer", customerId);
            Add(existing, "cart", cart.Id); Add(existing, "key", idempotencyKey);
            await using var reader = await existing.ExecuteReaderAsync(ct);
            if (!await reader.ReadAsync(ct)) throw new DomainException("CHECKOUT_UNAVAILABLE", "The durable idempotency record could not be stored", 503);
            var priorHash = reader.GetString(0); var state = reader.GetString(1);
            if (!priorHash.Equals(requestHash, StringComparison.Ordinal)) throw new DomainException("IDEMPOTENCY_KEY_REUSED", "The idempotency key was already used with a different request", 409);
            if (state == "COMPLETED" && !reader.IsDBNull(2))
            {
                var replay = JsonSerializer.Deserialize<CheckoutSubmissionResponseDto>(reader.GetString(2))
                    ?? throw new DomainException("CHECKOUT_UNAVAILABLE", "The stored checkout response is invalid", 503);
                await reader.CloseAsync(); await transaction.CommitAsync(ct);
                return new CheckoutResult { Response = replay, EventId = Guid.TryParse(replay.EventId, out var eventId) ? eventId : Guid.Empty };
            }
            if (state == "IN_PROGRESS") throw new DomainException("CHECKOUT_IN_PROGRESS", "The checkout request is already being processed", 409);
            await reader.CloseAsync();
        }

        var sessionId = Guid.NewGuid(); var submissionId = Guid.NewGuid(); var eventIdNew = Guid.NewGuid(); var now = DateTimeOffset.UtcNow;
        await using (var session = new NpgsqlCommand("""
            INSERT INTO cart_checkout_schema.checkout_session(checkout_session_id,cart_id,customer_id,tenant_id,store_id,state,cart_version,currency_code,expires_at,submitted_at,correlation_id)
            VALUES(@id,@cart,@customer,@tenant,@store,'FROZEN',@version,@currency,@expires,@submitted,@correlation)
            """, connection, transaction))
        {
            Add(session, "id", sessionId); Add(session, "cart", cart.Id); Add(session, "customer", customerId); Add(session, "tenant", context.TenantId); Add(session, "store", context.StoreId);
            Add(session, "version", cart.Version); Add(session, "currency", currency); Add(session, "expires", now.AddHours(1)); Add(session, "submitted", now);
            Add(session, "correlation", Guid.TryParse(context.CorrelationId, out var correlation) ? correlation : null); await session.ExecuteNonQueryAsync(ct);
        }
        var lines = cart.Items.Where(x => !x.Obsolete).Select((item, index) => new
        {
            lineNumber = index + 1, item.Sku, productName = products.FirstOrDefault(p => p.Sku == item.Sku)?.Name ?? item.Sku,
            item.Quantity, unitPrice = item.UnitPrice, lineSubTotal = item.SubTotal, item.ProductId, item.VariantId, isVirtual = products.FirstOrDefault(p => p.Sku == item.Sku)?.IsVirtual ?? false,
            attributes = item.Attributes.Select(id => new { id }).ToArray()
        }).ToArray();
        foreach (var line in lines)
        {
            await using var snapshot = new NpgsqlCommand("""
                INSERT INTO cart_checkout_schema.checkout_line_snapshot(checkout_session_id,line_number,sku,product_name,quantity,unit_price,line_sub_total,product_id,variant_id,is_virtual,attributes)
                VALUES(@session,@number,@sku,@name,@quantity,@unit,@subtotal,@product,@variant,@virtual,@attributes)
                """, connection, transaction);
            Add(snapshot, "session", sessionId); Add(snapshot, "number", line.lineNumber); Add(snapshot, "sku", line.Sku); Add(snapshot, "name", line.productName);
            Add(snapshot, "quantity", line.Quantity); Add(snapshot, "unit", line.unitPrice); Add(snapshot, "subtotal", line.lineSubTotal); Add(snapshot, "product", line.ProductId);
            Add(snapshot, "variant", line.VariantId); Add(snapshot, "virtual", line.isVirtual); Json(snapshot, "attributes", line.attributes); await snapshot.ExecuteNonQueryAsync(ct);
        }
        await using (var total = new NpgsqlCommand("""
            INSERT INTO cart_checkout_schema.checkout_total_snapshot(checkout_session_id,currency_code,sub_total,discount_total,shipping_total,handling_total,tax_total,grand_total,input_hash,quoted_at)
            VALUES(@session,@currency,@subtotal,@discount,@shipping,@handling,@tax,@grand,@hash,@at)
            """, connection, transaction))
        { Add(total, "session", sessionId); Add(total, "currency", currency); Add(total, "subtotal", subtotal); Add(total, "discount", discount); Add(total, "shipping", shipping); Add(total, "handling", handling); Add(total, "tax", tax); Add(total, "grand", grandTotal); Add(total, "hash", requestHash); Add(total, "at", now); await total.ExecuteNonQueryAsync(ct); }
        await using (var submission = new NpgsqlCommand("""
            INSERT INTO cart_checkout_schema.checkout_submission(submission_id,checkout_session_id,tenant_id,store_id,currency_code,amount,payment_module,payment_type,state)
            VALUES(@id,@session,@tenant,@store,@currency,@amount,@module,@type,'SUBMITTED')
            """, connection, transaction))
        { Add(submission, "id", submissionId); Add(submission, "session", sessionId); Add(submission, "tenant", context.TenantId); Add(submission, "store", context.StoreId); Add(submission, "currency", currency); Add(submission, "amount", grandTotal); Add(submission, "module", payment.PaymentModule); Add(submission, "type", payment.PaymentType); await submission.ExecuteNonQueryAsync(ct); }
        var response = new CheckoutSubmissionResponseDto { SubmissionId = submissionId.ToString(), CheckoutSessionId = sessionId.ToString(), State = "Submitted", Amount = DtoMapper.Money(grandTotal), Currency = currency, EventId = eventIdNew.ToString(), Downstream = new Dictionary<string, object?> { ["order"] = "Pending", ["payment"] = "Pending", ["inventory"] = "Pending" } };
        var envelope = new { eventId = eventIdNew, eventType = "OrderSubmitted.v1", eventVersion = 1, occurredAt = now, tenantId = context.TenantId, storeId = context.StoreId, correlationId = context.CorrelationId, submissionId, customerId, currency, total = grandTotal, lines };
        await using (var outbox = new NpgsqlCommand("""
            INSERT INTO cart_checkout_schema.ms04_outbox(event_id,tenant_id,store_id,aggregate_id,event_type,payload,correlation_id)
            VALUES(@id,@tenant,@store,@aggregate,'OrderSubmitted.v1',@payload,@correlation)
            """, connection, transaction))
        { Add(outbox, "id", eventIdNew); Add(outbox, "tenant", context.TenantId); Add(outbox, "store", context.StoreId); Add(outbox, "aggregate", submissionId); Json(outbox, "payload", envelope); Add(outbox, "correlation", Guid.TryParse(context.CorrelationId, out var correlation) ? correlation : null); await outbox.ExecuteNonQueryAsync(ct); }
        await using (var complete = new NpgsqlCommand("""
            UPDATE cart_checkout_schema.checkout_idempotency_key SET state='COMPLETED',original_status=202,original_response=@response,updated_at=now()
            WHERE tenant_id=@tenant AND store_id=@store AND customer_id IS NOT DISTINCT FROM @customer AND cart_id=@cart AND operation='checkout' AND idempotency_key=@key
            """, connection, transaction))
        { Json(complete, "response", response); Add(complete, "tenant", context.TenantId); Add(complete, "store", context.StoreId); Add(complete, "customer", customerId); Add(complete, "cart", cart.Id); Add(complete, "key", idempotencyKey); await complete.ExecuteNonQueryAsync(ct); }
        await using (var closeCart = new NpgsqlCommand("""
            UPDATE cart_checkout_schema.shopping_cart SET status='COMPLETED',submitted_order_id=@submission,updated_at=now()
            WHERE cart_id=@cart AND tenant_id=@tenant AND store_id=@store AND status='OPEN'
            """, connection, transaction))
        { Add(closeCart, "cart", cart.Id); Add(closeCart, "submission", submissionId); Add(closeCart, "tenant", context.TenantId); Add(closeCart, "store", context.StoreId); if (await closeCart.ExecuteNonQueryAsync(ct) != 1) throw new DomainException("CHECKOUT_TERMINAL", "The cart is no longer open", 409); }
        await using (var submitSession = new NpgsqlCommand("""
            UPDATE cart_checkout_schema.checkout_session SET state='SUBMITTED',updated_at=now()
            WHERE checkout_session_id=@session AND state='FROZEN'
            """, connection, transaction))
        { Add(submitSession, "session", sessionId); if (await submitSession.ExecuteNonQueryAsync(ct) != 1) throw new DomainException("CHECKOUT_TERMINAL", "Checkout session cannot be submitted", 409); }
        await transaction.CommitAsync(ct);
        return new CheckoutResult { Response = response, EventId = eventIdNew };
    }

    public async Task MarkOutboxPublishedAsync(Guid eventId, CancellationToken ct)
    {
        await using var connection = await dataSource.OpenConnectionAsync(ct);
        await using var command = new NpgsqlCommand("UPDATE cart_checkout_schema.ms04_outbox SET state='PUBLISHED',published_at=now() WHERE event_id=@id", connection);
        Add(command, "id", eventId); await command.ExecuteNonQueryAsync(ct);
    }

    public async Task MarkOutboxAttemptAsync(Guid eventId, CancellationToken ct)
    {
        await using var connection = await dataSource.OpenConnectionAsync(ct);
        await using var command = new NpgsqlCommand(
            "UPDATE cart_checkout_schema.ms04_outbox SET attempt_count=attempt_count+1 WHERE event_id=@id AND state='PENDING'", connection);
        Add(command, "id", eventId);
        await command.ExecuteNonQueryAsync(ct);
    }

    public async Task<IReadOnlyList<Guid>> GetPendingOutboxIdsAsync(int limit, CancellationToken ct)
    {
        await using var connection = await dataSource.OpenConnectionAsync(ct);
        await using var command = new NpgsqlCommand("""
            SELECT event_id FROM cart_checkout_schema.ms04_outbox
            WHERE state='PENDING'
            ORDER BY occurred_at
            LIMIT @limit
            """, connection);
        Add(command, "limit", limit);
        await using var reader = await command.ExecuteReaderAsync(ct);
        var ids = new List<Guid>();
        while (await reader.ReadAsync(ct)) ids.Add(reader.GetGuid(0));
        return ids;
    }

    public async Task<byte[]> GetOutboxPayloadAsync(Guid eventId, CancellationToken ct)
    {
        await using var connection = await dataSource.OpenConnectionAsync(ct);
        await using var command = new NpgsqlCommand("SELECT payload::text FROM cart_checkout_schema.ms04_outbox WHERE event_id=@id", connection);
        Add(command, "id", eventId);
        var payload = await command.ExecuteScalarAsync(ct) as string;
        return payload is null ? throw new DomainException("CHECKOUT_UNAVAILABLE", "The durable submission event could not be read", 503) : System.Text.Encoding.UTF8.GetBytes(payload);
    }
}
