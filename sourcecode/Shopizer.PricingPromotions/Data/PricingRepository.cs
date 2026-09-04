using System.Text.Json;
using Npgsql;
using NpgsqlTypes;
using Shopizer.PricingPromotions.Models;

namespace Shopizer.PricingPromotions.Data;

public sealed class PricingRepository(NpgsqlDataSource dataSource)
{
    private static void P(NpgsqlCommand command, string name, object? value) =>
        command.Parameters.AddWithValue(name, value ?? DBNull.Value);

    private static void Json(NpgsqlCommand command, string name, object value) =>
        command.Parameters.Add(name, NpgsqlDbType.Jsonb).Value = JsonSerializer.Serialize(value);

    private const string PriceColumns = """
        p.price_entry_id,p.price_list_id,p.legacy_price_id,p.product_sku,p.variant_sku,
        p.availability_id,p.code,p.amount,p.price_type,p.is_default,p.special_start_date,
        p.special_end_date,p.special_amount,p.product_identifier_id,pl.currency_code
        """;

    public async Task<(PriceEntry Price, Guid EventId)> CreatePriceAsync(
        PriceEntry price, RequestContext context, string currency, CancellationToken ct)
    {
        await using var connection = await dataSource.OpenConnectionAsync(ct);
        await using var transaction = await connection.BeginTransactionAsync(ct);
        try
        {
            var priceListId = await EnsurePriceListAsync(connection, transaction, context, currency, ct);
            var eventId = Guid.NewGuid();
            await EnsureDefaultIsUniqueAsync(connection, transaction, priceListId, price, null, ct);
            price = new PriceEntry
            {
                Id = price.Id,
                PriceListId = priceListId,
                LegacyPriceId = price.LegacyPriceId,
                ProductSku = price.ProductSku,
                VariantSku = price.VariantSku,
                AvailabilityId = price.AvailabilityId,
                Code = price.Code,
                Amount = price.Amount,
                PriceType = price.PriceType,
                DefaultPrice = price.DefaultPrice,
                SpecialStartDate = price.SpecialStartDate,
                SpecialEndDate = price.SpecialEndDate,
                SpecialAmount = price.SpecialAmount,
                ProductIdentifierId = price.ProductIdentifierId,
                Currency = currency
            };
            await using (var command = new NpgsqlCommand("""
                INSERT INTO pricing_promotions.price_entry
                  (price_entry_id,price_list_id,legacy_price_id,product_sku,variant_sku,availability_id,code,amount,
                   price_type,is_default,special_start_date,special_end_date,special_amount,product_identifier_id,created_by)
                VALUES (@id,@list,@legacy,@sku,@variant,@availability,@code,@amount,@type,@default,@start,@end,@special,@identifier,@created)
                """, connection, transaction))
            {
                AddPriceParameters(command, price, context, includeList: true);
                await command.ExecuteNonQueryAsync(ct);
            }
            await AddOutboxAsync(connection, transaction, eventId, "PriceChanged.v1", context, new
            {
                eventId,
                eventType = "PriceChanged.v1",
                eventVersion = 1,
                occurredAt = DateTimeOffset.UtcNow,
                tenantId = context.TenantId,
                storeId = context.StoreId,
                correlationId = context.CorrelationId,
                productSku = price.ProductSku,
                variantSku = price.VariantSku,
                priceId = price.Id,
                changeType = "Created",
                amount = price.Amount,
                priceType = price.PriceType,
                defaultPrice = price.DefaultPrice,
                specialAmount = price.SpecialAmount
            }, ct);
            await transaction.CommitAsync(ct);
            return (price, eventId);
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            await transaction.RollbackAsync(ct);
            throw new DomainException("PRICE_CONFLICT", "The price identity conflicts with an existing price", 409);
        }
    }

    public async Task<PriceEntry?> FindPriceAsync(Guid id, string sku, RequestContext context, CancellationToken ct)
    {
        await using var connection = await dataSource.OpenConnectionAsync(ct);
        await using var command = new NpgsqlCommand($"""
            SELECT {PriceColumns} FROM pricing_promotions.price_entry p
            JOIN pricing_promotions.price_list pl ON pl.price_list_id=p.price_list_id
            WHERE p.price_entry_id=@id AND p.product_sku=@sku AND pl.tenant_id=@tenant
              AND pl.store_id=@store AND pl.is_active
            """, connection);
        P(command, "id", id); P(command, "sku", sku); P(command, "tenant", context.TenantId); P(command, "store", context.StoreId);
        await using var reader = await command.ExecuteReaderAsync(ct);
        return await reader.ReadAsync(ct) ? ReadPrice(reader) : null;
    }

    public async Task<List<PriceEntry>> ListPricesAsync(
        string sku, long? availabilityId, RequestContext context, CancellationToken ct)
    {
        await using var connection = await dataSource.OpenConnectionAsync(ct);
        var availabilityFilter = availabilityId.HasValue ? "AND p.availability_id=@availability" : "";
        await using var command = new NpgsqlCommand($"""
            SELECT {PriceColumns} FROM pricing_promotions.price_entry p
            JOIN pricing_promotions.price_list pl ON pl.price_list_id=p.price_list_id
            WHERE p.product_sku=@sku AND pl.tenant_id=@tenant AND pl.store_id=@store AND pl.is_active
              {availabilityFilter}
            ORDER BY p.is_default DESC,p.code,p.price_entry_id
            """, connection);
        P(command, "sku", sku); P(command, "tenant", context.TenantId); P(command, "store", context.StoreId);
        if (availabilityId.HasValue) P(command, "availability", availabilityId.Value);
        return await ReadPricesAsync(command, ct);
    }

    public async Task<List<PriceEntry>> ListCalculationPricesAsync(
        string sku, string? variantSku, RequestContext context, CancellationToken ct)
    {
        await using var connection = await dataSource.OpenConnectionAsync(ct);
        await using var command = new NpgsqlCommand($"""
            SELECT {PriceColumns} FROM pricing_promotions.price_entry p
            JOIN pricing_promotions.price_list pl ON pl.price_list_id=p.price_list_id
            WHERE p.product_sku=@sku AND pl.tenant_id=@tenant AND pl.store_id=@store AND pl.is_active
              AND (CAST(@variant AS varchar(160)) IS NULL OR p.variant_sku=CAST(@variant AS varchar(160)))
            ORDER BY p.is_default DESC,p.variant_sku NULLS LAST,p.code,p.price_entry_id
            """, connection);
        P(command, "sku", sku); P(command, "tenant", context.TenantId); P(command, "store", context.StoreId);
        P(command, "variant", variantSku);
        return await ReadPricesAsync(command, ct);
    }

    public async Task<(PriceEntry Price, Guid EventId)> UpdatePriceAsync(
        PriceEntry update, string sku, RequestContext context, CancellationToken ct)
    {
        await using var connection = await dataSource.OpenConnectionAsync(ct);
        await using var transaction = await connection.BeginTransactionAsync(ct);
        var existing = await FindPriceAsync(connection, transaction, update.Id, sku, context, ct)
            ?? throw new DomainException("PRICE_NOT_FOUND", "Price was not found for this product and store", 404);
        await EnsureDefaultIsUniqueAsync(connection, transaction, existing.PriceListId, update, update.Id, ct);
        await using (var command = new NpgsqlCommand("""
            UPDATE pricing_promotions.price_entry
            SET code=@code,amount=@amount,price_type=@type,is_default=@default,
                special_start_date=@start,special_end_date=@end,special_amount=@special,
                product_identifier_id=@identifier,availability_id=@availability,updated_at=current_timestamp
            WHERE price_entry_id=@id
            """, connection, transaction))
        {
            update = new PriceEntry
            {
                Id = update.Id,
                PriceListId = existing.PriceListId,
                LegacyPriceId = existing.LegacyPriceId,
                ProductSku = sku,
                VariantSku = existing.VariantSku,
                AvailabilityId = update.AvailabilityId ?? existing.AvailabilityId,
                Code = update.Code,
                Amount = update.Amount,
                PriceType = update.PriceType,
                DefaultPrice = update.DefaultPrice,
                SpecialStartDate = update.SpecialStartDate,
                SpecialEndDate = update.SpecialEndDate,
                SpecialAmount = update.SpecialAmount,
                ProductIdentifierId = update.ProductIdentifierId,
                Currency = existing.Currency
            };
            AddPriceParameters(command, update, context, includeList: false);
            await command.ExecuteNonQueryAsync(ct);
        }
        var eventId = Guid.NewGuid();
        await AddOutboxAsync(connection, transaction, eventId, "PriceChanged.v1", context, new
        {
            eventId,
            eventType = "PriceChanged.v1",
            eventVersion = 1,
            occurredAt = DateTimeOffset.UtcNow,
            tenantId = context.TenantId,
            storeId = context.StoreId,
            correlationId = context.CorrelationId,
            productSku = sku,
            variantSku = update.VariantSku,
            priceId = update.Id,
            changeType = "Updated",
            amount = update.Amount,
            priceType = update.PriceType,
            defaultPrice = update.DefaultPrice,
            specialAmount = update.SpecialAmount
        }, ct);
        await transaction.CommitAsync(ct);
        return (update, eventId);
    }

    public async Task<(bool Deleted, Guid EventId)> DeletePriceAsync(
        Guid id, string sku, RequestContext context, CancellationToken ct)
    {
        await using var connection = await dataSource.OpenConnectionAsync(ct);
        await using var transaction = await connection.BeginTransactionAsync(ct);
        var existing = await FindPriceAsync(connection, transaction, id, sku, context, ct)
            ?? throw new DomainException("PRICE_NOT_FOUND", "Price was not found for this product and store", 404);
        await using (var command = new NpgsqlCommand(
            "DELETE FROM pricing_promotions.price_entry WHERE price_entry_id=@id", connection, transaction))
        {
            P(command, "id", id);
            if (await command.ExecuteNonQueryAsync(ct) != 1)
                throw new DomainException("PRICE_NOT_FOUND", "Price was not found for this product and store", 404);
        }
        var eventId = Guid.NewGuid();
        await AddOutboxAsync(connection, transaction, eventId, "PriceChanged.v1", context, new
        {
            eventId,
            eventType = "PriceChanged.v1",
            eventVersion = 1,
            occurredAt = DateTimeOffset.UtcNow,
            tenantId = context.TenantId,
            storeId = context.StoreId,
            correlationId = context.CorrelationId,
            productSku = sku,
            variantSku = existing.VariantSku,
            priceId = id,
            changeType = "Deleted",
            amount = existing.Amount,
            priceType = existing.PriceType,
            defaultPrice = existing.DefaultPrice,
            specialAmount = existing.SpecialAmount
        }, ct);
        await transaction.CommitAsync(ct);
        return (true, eventId);
    }

    public async Task<PromotionMatch?> FindPromotionAsync(
        string code, RequestContext context, DateOnly evaluationDate, CancellationToken ct)
    {
        await using var connection = await dataSource.OpenConnectionAsync(ct);
        await using var command = new NpgsqlCommand("""
            SELECT p.promotion_id,p.tenant_id,p.store_id,p.name,p.rule_key,p.discount_rate,
                   p.valid_from,p.valid_until,p.is_enabled
            FROM pricing_promotions.promotion p
            LEFT JOIN pricing_promotions.coupon c
              ON c.promotion_id=p.promotion_id AND c.tenant_id=p.tenant_id AND c.store_id=p.store_id
             AND c.code=@code AND c.is_enabled
             AND (c.valid_from IS NULL OR c.valid_from <= @evaluation)
             AND (c.valid_until IS NULL OR c.valid_until >= @evaluation)
            WHERE p.tenant_id=@tenant AND p.store_id=@store AND p.is_enabled
              AND (p.rule_key=@code OR c.coupon_id IS NOT NULL)
              AND (p.valid_from IS NULL OR p.valid_from <= @evaluation)
              AND (p.valid_until IS NULL OR p.valid_until >= @evaluation)
            ORDER BY p.promotion_id
            LIMIT 1
            """, connection);
        P(command, "code", code); P(command, "tenant", context.TenantId); P(command, "store", context.StoreId);
        P(command, "evaluation", evaluationDate);
        await using var reader = await command.ExecuteReaderAsync(ct);
        if (!await reader.ReadAsync(ct)) return null;
        return new PromotionMatch(
            reader.GetGuid(0), code,
            new Promotion
            {
                Id = reader.GetGuid(0),
                TenantId = reader.GetString(1),
                StoreId = reader.GetString(2),
                Name = reader.GetString(3),
                RuleKey = reader.GetString(4),
                DiscountRate = reader.GetDecimal(5),
                ValidFrom = reader.IsDBNull(6) ? null : reader.GetFieldValue<DateOnly>(6),
                ValidUntil = reader.IsDBNull(7) ? null : reader.GetFieldValue<DateOnly>(7),
                IsEnabled = reader.GetBoolean(8)
            });
    }

    public async Task MarkEventPublishedAsync(Guid eventId, CancellationToken ct)
    {
        await using var connection = await dataSource.OpenConnectionAsync(ct);
        await using var command = new NpgsqlCommand(
            "UPDATE pricing_promotions.event_outbox SET published_at=current_timestamp WHERE id=@id AND published_at IS NULL",
            connection);
        P(command, "id", eventId);
        await command.ExecuteNonQueryAsync(ct);
    }

    private static async Task<Guid> EnsurePriceListAsync(
        NpgsqlConnection connection, NpgsqlTransaction transaction, RequestContext context,
        string currency, CancellationToken ct)
    {
        await using var command = new NpgsqlCommand("""
            INSERT INTO pricing_promotions.price_list(tenant_id,store_id,name,currency_code,is_active)
            VALUES (@tenant,@store,'DEFAULT',@currency,true)
            ON CONFLICT (tenant_id,store_id,currency_code,name)
            DO UPDATE SET is_active=true,updated_at=current_timestamp
            RETURNING price_list_id
            """, connection, transaction);
        P(command, "tenant", context.TenantId); P(command, "store", context.StoreId); P(command, "currency", currency);
        return (Guid)(await command.ExecuteScalarAsync(ct)
            ?? throw new InvalidOperationException("The default price list could not be created."));
    }

    private static async Task EnsureDefaultIsUniqueAsync(
        NpgsqlConnection connection, NpgsqlTransaction transaction, Guid priceListId,
        PriceEntry price, Guid? excludingId, CancellationToken ct)
    {
        if (!price.DefaultPrice) return;
        await using var command = new NpgsqlCommand("""
            SELECT 1 FROM pricing_promotions.price_entry
            WHERE price_list_id=@list AND product_sku=@sku
              AND availability_id IS NOT DISTINCT FROM @availability
              AND variant_sku IS NOT DISTINCT FROM @variant
              AND is_default AND (@id IS NULL OR price_entry_id<>@id)
            LIMIT 1
            """, connection, transaction);
        P(command, "list", priceListId); P(command, "sku", price.ProductSku);
        P(command, "availability", price.AvailabilityId); P(command, "variant", price.VariantSku); P(command, "id", excludingId);
        if (await command.ExecuteScalarAsync(ct) is not null)
            throw new DomainException("PRICE_CONFLICT", "Only one default price is allowed for the price identity", 409);
    }

    private static void AddPriceParameters(NpgsqlCommand command, PriceEntry price, RequestContext context, bool includeList)
    {
        P(command, "id", price.Id); if (includeList) P(command, "list", price.PriceListId);
        P(command, "legacy", price.LegacyPriceId); P(command, "sku", price.ProductSku); P(command, "variant", price.VariantSku);
        P(command, "availability", price.AvailabilityId); P(command, "code", price.Code); P(command, "amount", price.Amount);
        P(command, "type", price.PriceType); P(command, "default", price.DefaultPrice); P(command, "start", price.SpecialStartDate);
        P(command, "end", price.SpecialEndDate); P(command, "special", price.SpecialAmount);
        P(command, "identifier", price.ProductIdentifierId); P(command, "created", context.TenantId);
    }

    private static async Task AddOutboxAsync(
        NpgsqlConnection connection, NpgsqlTransaction transaction, Guid eventId, string eventType,
        RequestContext context, object payload, CancellationToken ct)
    {
        await using var command = new NpgsqlCommand("""
            INSERT INTO pricing_promotions.event_outbox
              (id,event_type,tenant_id,store_id,correlation_id,payload,occurred_at)
            VALUES (@id,@type,@tenant,@store,@correlation,@payload,@occurred)
            """, connection, transaction);
        P(command, "id", eventId); P(command, "type", eventType); P(command, "tenant", context.TenantId);
        P(command, "store", context.StoreId); P(command, "correlation", context.CorrelationId);
        Json(command, "payload", payload); P(command, "occurred", DateTimeOffset.UtcNow);
        await command.ExecuteNonQueryAsync(ct);
    }

    private static async Task<PriceEntry?> FindPriceAsync(
        NpgsqlConnection connection, NpgsqlTransaction transaction, Guid id, string sku,
        RequestContext context, CancellationToken ct)
    {
        await using var command = new NpgsqlCommand($"""
            SELECT {PriceColumns} FROM pricing_promotions.price_entry p
            JOIN pricing_promotions.price_list pl ON pl.price_list_id=p.price_list_id
            WHERE p.price_entry_id=@id AND p.product_sku=@sku AND pl.tenant_id=@tenant
              AND pl.store_id=@store AND pl.is_active
            """, connection, transaction);
        P(command, "id", id); P(command, "sku", sku); P(command, "tenant", context.TenantId); P(command, "store", context.StoreId);
        await using var reader = await command.ExecuteReaderAsync(ct);
        return await reader.ReadAsync(ct) ? ReadPrice(reader) : null;
    }

    private static async Task<List<PriceEntry>> ReadPricesAsync(NpgsqlCommand command, CancellationToken ct)
    {
        var result = new List<PriceEntry>();
        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct)) result.Add(ReadPrice(reader));
        return result;
    }

    private static PriceEntry ReadPrice(NpgsqlDataReader reader) => new()
    {
        Id = reader.GetGuid(0),
        PriceListId = reader.GetGuid(1),
        LegacyPriceId = reader.IsDBNull(2) ? null : reader.GetInt64(2),
        ProductSku = reader.GetString(3),
        VariantSku = reader.IsDBNull(4) ? null : reader.GetString(4),
        AvailabilityId = reader.IsDBNull(5) ? null : reader.GetInt64(5),
        Code = reader.GetString(6),
        Amount = reader.GetDecimal(7),
        PriceType = reader.GetString(8),
        DefaultPrice = reader.GetBoolean(9),
        SpecialStartDate = reader.IsDBNull(10) ? null : reader.GetFieldValue<DateOnly>(10),
        SpecialEndDate = reader.IsDBNull(11) ? null : reader.GetFieldValue<DateOnly>(11),
        SpecialAmount = reader.IsDBNull(12) ? null : reader.GetDecimal(12),
        ProductIdentifierId = reader.IsDBNull(13) ? null : reader.GetInt64(13),
        Currency = reader.GetString(14).Trim()
    };
}
