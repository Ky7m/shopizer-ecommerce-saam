using Npgsql;
using NpgsqlTypes;
using Shopizer.Tax.Models;

namespace Shopizer.Tax.Data;

public sealed class TaxRepository(NpgsqlDataSource dataSource)
{
    public async Task<TaxClassEntity?> FindTaxClassAsync(Guid id, RequestContext context, CancellationToken ct)
    {
        await using var connection = await dataSource.OpenConnectionAsync(ct);
        await using var command = new NpgsqlCommand(
            """
            SELECT id, tenant_id, store_id, code, title
            FROM tax_schema.tax_classes
            WHERE id = @id AND tenant_id = @tenant AND store_id = @store
            """, connection);
        command.Parameters.AddWithValue("id", id);
        AddContext(command, context);
        await using var reader = await command.ExecuteReaderAsync(ct);
        return await reader.ReadAsync(ct) ? ReadClass(reader) : null;
    }

    public async Task<TaxClassEntity?> FindTaxClassAnyScopeAsync(Guid id, CancellationToken ct)
    {
        await using var connection = await dataSource.OpenConnectionAsync(ct);
        await using var command = new NpgsqlCommand(
            "SELECT id, tenant_id, store_id, code, title FROM tax_schema.tax_classes WHERE id = @id",
            connection);
        command.Parameters.AddWithValue("id", id);
        await using var reader = await command.ExecuteReaderAsync(ct);
        return await reader.ReadAsync(ct) ? ReadClass(reader) : null;
    }

    public async Task<TaxClassEntity?> FindTaxClassByCodeAsync(string code, RequestContext context, CancellationToken ct)
    {
        await using var connection = await dataSource.OpenConnectionAsync(ct);
        await using var command = new NpgsqlCommand(
            """
            SELECT id, tenant_id, store_id, code, title
            FROM tax_schema.tax_classes
            WHERE tenant_id = @tenant AND store_id = @store AND code = @code
            """, connection);
        AddContext(command, context);
        command.Parameters.AddWithValue("code", code);
        await using var reader = await command.ExecuteReaderAsync(ct);
        return await reader.ReadAsync(ct) ? ReadClass(reader) : null;
    }

    public async Task<bool> TaxClassExistsAsync(string code, RequestContext context, CancellationToken ct) =>
        await ExistsAsync(
            "SELECT EXISTS (SELECT 1 FROM tax_schema.tax_classes WHERE tenant_id = @tenant AND store_id = @store AND code = @code)",
            code, context, ct);

    public async Task<IReadOnlyList<TaxClassEntity>> ListTaxClassesAsync(RequestContext context, int page, int pageSize, CancellationToken ct)
    {
        await using var connection = await dataSource.OpenConnectionAsync(ct);
        await using var command = new NpgsqlCommand(
            """
            SELECT id, tenant_id, store_id, code, title
            FROM tax_schema.tax_classes
            WHERE tenant_id = @tenant AND store_id = @store
            ORDER BY code
            LIMIT @limit OFFSET @offset
            """, connection);
        AddContext(command, context);
        command.Parameters.AddWithValue("limit", pageSize);
        command.Parameters.AddWithValue("offset", (page - 1) * pageSize);
        await using var reader = await command.ExecuteReaderAsync(ct);
        var result = new List<TaxClassEntity>();
        while (await reader.ReadAsync(ct)) result.Add(ReadClass(reader));
        return result;
    }

    public async Task<long> CountTaxClassesAsync(RequestContext context, CancellationToken ct) =>
        await CountAsync("tax_schema.tax_classes", context, ct);

    public async Task AddTaxClassAsync(TaxClassEntity entity, RequestContext context, CancellationToken ct)
    {
        await using var connection = await dataSource.OpenConnectionAsync(ct);
        await using var command = new NpgsqlCommand(
            """
            INSERT INTO tax_schema.tax_classes
                (id, tenant_id, store_id, code, title, created_by, correlation_id)
            VALUES (@id, @tenant, @store, @code, @title, @created_by, @correlation)
            """, connection);
        command.Parameters.AddWithValue("id", entity.Id);
        AddContext(command, context);
        command.Parameters.AddWithValue("code", entity.Code);
        command.Parameters.AddWithValue("title", entity.Title);
        command.Parameters.AddWithValue("created_by", DBNull.Value);
        command.Parameters.AddWithValue("correlation", context.CorrelationId);
        try
        {
            await command.ExecuteNonQueryAsync(ct);
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            throw new DomainException("TAX_CLASS_ALREADY_EXISTS", $"Tax class code {entity.Code} already exists for store {context.StoreId}", 409);
        }
    }

    public async Task UpdateTaxClassAsync(TaxClassEntity entity, RequestContext context, CancellationToken ct)
    {
        await using var connection = await dataSource.OpenConnectionAsync(ct);
        await using var command = new NpgsqlCommand(
            """
            UPDATE tax_schema.tax_classes
            SET code = @code, title = @title, updated_at = now(), correlation_id = @correlation
            WHERE id = @id AND tenant_id = @tenant AND store_id = @store
            """, connection);
        command.Parameters.AddWithValue("id", entity.Id);
        AddContext(command, context);
        command.Parameters.AddWithValue("code", entity.Code);
        command.Parameters.AddWithValue("title", entity.Title);
        command.Parameters.AddWithValue("correlation", context.CorrelationId);
        try
        {
            await command.ExecuteNonQueryAsync(ct);
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            throw new DomainException("TAX_CLASS_ALREADY_EXISTS", $"Tax class code {entity.Code} already exists for store {context.StoreId}", 409);
        }
    }

    public async Task DeleteTaxClassAsync(Guid id, RequestContext context, CancellationToken ct)
    {
        await using var connection = await dataSource.OpenConnectionAsync(ct);
        await using var command = new NpgsqlCommand(
            """
            DELETE FROM tax_schema.tax_classes
            WHERE id = @id AND tenant_id = @tenant AND store_id = @store
            """, connection);
        command.Parameters.AddWithValue("id", id);
        AddContext(command, context);
        try
        {
            await command.ExecuteNonQueryAsync(ct);
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.ForeignKeyViolation)
        {
            throw new DomainException("TAX_CLASS_IN_USE", "Tax class cannot be deleted while tax rates use it", 409);
        }
    }

    public async Task<TaxRateEntity?> FindTaxRateAsync(Guid id, RequestContext context, CancellationToken ct)
    {
        await using var connection = await dataSource.OpenConnectionAsync(ct);
        var entity = await ReadRateAsync(connection,
            """
            SELECT r.id, r.tenant_id, r.store_id, r.tax_class_id, c.code, r.code,
                   r.rate_percent, r.priority, r.piggyback, r.country_code,
                   r.zone_code, r.state_province, r.parent_rate_id
            FROM tax_schema.tax_rates r
            JOIN tax_schema.tax_classes c ON c.id = r.tax_class_id
            WHERE r.id = @id AND r.tenant_id = @tenant AND r.store_id = @store
            """, command => { command.Parameters.AddWithValue("id", id); AddContext(command, context); }, ct);
        if (entity is null) return null;
        await LoadDescriptionsAsync(connection, entity, null, ct);
        return entity;
    }

    public async Task<bool> TaxRateExistsAsync(string code, RequestContext context, CancellationToken ct) =>
        await ExistsAsync(
            "SELECT EXISTS (SELECT 1 FROM tax_schema.tax_rates WHERE tenant_id = @tenant AND store_id = @store AND code = @code)",
            code, context, ct);

    public async Task<IReadOnlyList<TaxRateEntity>> ListTaxRatesAsync(
        RequestContext context, string languageCode, int page, int pageSize, CancellationToken ct)
    {
        await using var connection = await dataSource.OpenConnectionAsync(ct);
        await using var command = new NpgsqlCommand(
            """
            SELECT r.id, r.tenant_id, r.store_id, r.tax_class_id, c.code, r.code,
                   r.rate_percent, r.priority, r.piggyback, r.country_code,
                   r.zone_code, r.state_province, r.parent_rate_id,
                   d.id, d.language_code, d.name, d.title, d.description
            FROM tax_schema.tax_rates r
            JOIN tax_schema.tax_classes c ON c.id = r.tax_class_id
            JOIN tax_schema.tax_rate_descriptions d ON d.tax_rate_id = r.id
                AND d.language_code = @language
            WHERE r.tenant_id = @tenant AND r.store_id = @store
            ORDER BY r.priority, r.code
            LIMIT @limit OFFSET @offset
            """, connection);
        AddContext(command, context);
        command.Parameters.AddWithValue("language", languageCode);
        command.Parameters.AddWithValue("limit", pageSize);
        command.Parameters.AddWithValue("offset", (page - 1) * pageSize);
        await using var reader = await command.ExecuteReaderAsync(ct);
        var result = new List<TaxRateEntity>();
        while (await reader.ReadAsync(ct))
        {
            var entity = ReadRate(reader);
            entity.Descriptions.Add(ReadDescription(reader, 13, entity.Id));
            result.Add(entity);
        }
        return result;
    }

    public async Task<long> CountTaxRatesAsync(RequestContext context, string languageCode, CancellationToken ct) =>
        await CountRatesAsync(context, languageCode, ct);

    public async Task<IReadOnlyList<TaxRateEntity>> FindRatesForCalculationAsync(
        RequestContext context, Guid taxClassId, string countryCode, string? zoneCode,
        string? stateProvince, string languageCode, CancellationToken ct)
    {
        await using var connection = await dataSource.OpenConnectionAsync(ct);
        await using var command = new NpgsqlCommand(
            """
            SELECT r.id, r.tenant_id, r.store_id, r.tax_class_id, c.code, r.code,
                   r.rate_percent, r.priority, r.piggyback, r.country_code,
                   r.zone_code, r.state_province, r.parent_rate_id,
                   d.id, d.language_code, d.name, d.title, d.description
            FROM tax_schema.tax_rates r
            JOIN tax_schema.tax_classes c ON c.id = r.tax_class_id
            JOIN tax_schema.tax_rate_descriptions d ON d.tax_rate_id = r.id
                AND d.language_code = @language
            WHERE r.tenant_id = @tenant AND r.store_id = @store
              AND r.tax_class_id = @class AND r.country_code = @country
              AND (
                    (@state IS NOT NULL AND @zone IS NULL AND r.state_province = @state AND r.zone_code IS NULL)
                    OR (@state IS NULL AND @zone IS NOT NULL AND r.zone_code = @zone)
                  )
            ORDER BY r.priority, r.code
            """, connection);
        AddContext(command, context);
        command.Parameters.AddWithValue("class", taxClassId);
        command.Parameters.AddWithValue("country", countryCode);
        command.Parameters.Add("zone", NpgsqlDbType.Varchar).Value = (object?)zoneCode ?? DBNull.Value;
        command.Parameters.Add("state", NpgsqlDbType.Varchar).Value = (object?)stateProvince ?? DBNull.Value;
        command.Parameters.AddWithValue("language", languageCode);
        await using var reader = await command.ExecuteReaderAsync(ct);
        var result = new List<TaxRateEntity>();
        while (await reader.ReadAsync(ct))
        {
            var entity = ReadRate(reader);
            entity.Descriptions.Add(ReadDescription(reader, 13, entity.Id));
            result.Add(entity);
        }
        return result;
    }

    public async Task AddTaxRateAsync(TaxRateEntity entity, RequestContext context, CancellationToken ct)
    {
        await using var connection = await dataSource.OpenConnectionAsync(ct);
        await using var transaction = await connection.BeginTransactionAsync(ct);
        try
        {
            await InsertRateAsync(connection, transaction, entity, context, ct);
            await ReplaceDescriptionsAsync(connection, transaction, entity, context, ct);
            await transaction.CommitAsync(ct);
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            await transaction.RollbackAsync(ct);
            throw new DomainException("TAX_RATE_ALREADY_EXISTS", $"Tax rate code {entity.Code} already exists for store {context.StoreId}", 409);
        }
    }

    public async Task UpdateTaxRateAsync(TaxRateEntity entity, RequestContext context, CancellationToken ct)
    {
        await using var connection = await dataSource.OpenConnectionAsync(ct);
        await using var transaction = await connection.BeginTransactionAsync(ct);
        try
        {
            await using var command = new NpgsqlCommand(
                """
                UPDATE tax_schema.tax_rates
                SET tax_class_id = @class, code = @code, rate_percent = @rate,
                    priority = @priority, piggyback = @piggyback, country_code = @country,
                    zone_code = @zone, state_province = @state, updated_at = now(),
                    correlation_id = @correlation
                WHERE id = @id AND tenant_id = @tenant AND store_id = @store
                """, connection, transaction);
            command.Parameters.AddWithValue("id", entity.Id);
            AddContext(command, context);
            AddRateParameters(command, entity);
            command.Parameters.AddWithValue("correlation", context.CorrelationId);
            if (await command.ExecuteNonQueryAsync(ct) == 0)
                throw new DomainException("TAX_RATE_NOT_FOUND", $"Tax rate was not found for store {context.StoreId}", 404);
            await ReplaceDescriptionsAsync(connection, transaction, entity, context, ct);
            await transaction.CommitAsync(ct);
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            await transaction.RollbackAsync(ct);
            throw new DomainException("TAX_RATE_ALREADY_EXISTS", $"Tax rate code {entity.Code} already exists for store {context.StoreId}", 409);
        }
    }

    public async Task DeleteTaxRateAsync(Guid id, RequestContext context, CancellationToken ct)
    {
        await using var connection = await dataSource.OpenConnectionAsync(ct);
        await using var command = new NpgsqlCommand(
            "DELETE FROM tax_schema.tax_rates WHERE id = @id AND tenant_id = @tenant AND store_id = @store",
            connection);
        command.Parameters.AddWithValue("id", id);
        AddContext(command, context);
        await command.ExecuteNonQueryAsync(ct);
    }

    public async Task<TaxConfigurationEntity?> FindConfigurationAsync(RequestContext context, CancellationToken ct)
    {
        await using var connection = await dataSource.OpenConnectionAsync(ct);
        await using var command = new NpgsqlCommand(
            """
            SELECT id, tenant_id, store_id, tax_basis, collect_tax_if_different_province,
                   different_country_behavior
            FROM tax_schema.tax_configurations
            WHERE tenant_id = @tenant AND store_id = @store
            """, connection);
        AddContext(command, context);
        await using var reader = await command.ExecuteReaderAsync(ct);
        return await reader.ReadAsync(ct)
            ? new TaxConfigurationEntity
            {
                Id = reader.GetGuid(0),
                TenantId = reader.GetString(1),
                StoreId = reader.GetString(2),
                TaxBasis = reader.GetString(3),
                CollectTaxIfDifferentProvince = reader.GetBoolean(4),
                DifferentCountryBehavior = reader.GetString(5)
            }
            : null;
    }

    public async Task<TaxConfigurationEntity> SaveConfigurationAsync(
        TaxConfigurationEntity entity, RequestContext context, CancellationToken ct)
    {
        await using var connection = await dataSource.OpenConnectionAsync(ct);
        await using var command = new NpgsqlCommand(
            """
            INSERT INTO tax_schema.tax_configurations
                (id, tenant_id, store_id, tax_basis, collect_tax_if_different_province,
                 different_country_behavior, correlation_id)
            VALUES (@id, @tenant, @store, @basis, @province, @country, @correlation)
            ON CONFLICT (tenant_id, store_id) DO UPDATE SET
                tax_basis = EXCLUDED.tax_basis,
                collect_tax_if_different_province = EXCLUDED.collect_tax_if_different_province,
                different_country_behavior = EXCLUDED.different_country_behavior,
                updated_at = now(), correlation_id = EXCLUDED.correlation_id
            RETURNING id
            """, connection);
        command.Parameters.AddWithValue("id", entity.Id);
        AddContext(command, context);
        command.Parameters.AddWithValue("basis", entity.TaxBasis);
        command.Parameters.AddWithValue("province", entity.CollectTaxIfDifferentProvince);
        command.Parameters.AddWithValue("country", entity.DifferentCountryBehavior);
        command.Parameters.AddWithValue("correlation", context.CorrelationId);
        entity.Id = (Guid)(await command.ExecuteScalarAsync(ct) ?? entity.Id);
        return entity;
    }

    public async Task<TaxQuoteEntity?> FindQuoteByIdempotencyAsync(string key, RequestContext context, CancellationToken ct)
    {
        await using var connection = await dataSource.OpenConnectionAsync(ct);
        await using var command = new NpgsqlCommand(
            """
            SELECT id, tenant_id, store_id, idempotency_key, currency_code, status,
                   customer_id, order_id, jurisdiction_country_code, jurisdiction_zone_code,
                   jurisdiction_state_province, taxable_amount, total_tax_amount, calculated_at
            FROM tax_schema.tax_quotes
            WHERE tenant_id = @tenant AND store_id = @store AND idempotency_key = @key
            """, connection);
        AddContext(command, context);
        command.Parameters.AddWithValue("key", key);
        await using var reader = await command.ExecuteReaderAsync(ct);
        return await reader.ReadAsync(ct) ? ReadQuote(reader) : null;
    }

    public async Task<IReadOnlyList<TaxQuoteItemEntity>> FindQuoteItemsAsync(Guid quoteId, RequestContext context, CancellationToken ct)
    {
        await using var connection = await dataSource.OpenConnectionAsync(ct);
        await using var command = new NpgsqlCommand(
            """
            SELECT i.id, i.tax_quote_id, i.tax_class_id, i.tax_code, i.label,
                   i.rate_percent, i.taxable_amount, i.tax_amount, i.piggyback, i.priority, c.code
            FROM tax_schema.tax_quote_items i
            JOIN tax_schema.tax_quotes q ON q.id = i.tax_quote_id
            LEFT JOIN tax_schema.tax_classes c ON c.id = i.tax_class_id
            WHERE i.tax_quote_id = @quote AND q.tenant_id = @tenant AND q.store_id = @store
            ORDER BY i.priority, i.tax_code
            """, connection);
        command.Parameters.AddWithValue("quote", quoteId);
        AddContext(command, context);
        await using var reader = await command.ExecuteReaderAsync(ct);
        var result = new List<TaxQuoteItemEntity>();
        while (await reader.ReadAsync(ct))
            result.Add(new TaxQuoteItemEntity
            {
                Id = reader.GetGuid(0),
                TaxQuoteId = reader.GetGuid(1),
                TaxClassId = reader.IsDBNull(2) ? null : reader.GetGuid(2),
                TaxCode = reader.GetString(3),
                Label = reader.GetString(4),
                RatePercent = reader.GetDecimal(5),
                TaxableAmount = reader.GetDecimal(6),
                TaxAmount = reader.GetDecimal(7),
                Piggyback = reader.GetBoolean(8),
                Priority = reader.GetInt32(9),
                TaxClassCode = reader.IsDBNull(10) ? null : reader.GetString(10)
            });
        return result;
    }

    public async Task SaveQuoteAsync(TaxQuoteEntity quote, IEnumerable<TaxQuoteItemEntity> items, RequestContext context, CancellationToken ct)
    {
        await using var connection = await dataSource.OpenConnectionAsync(ct);
        await using var transaction = await connection.BeginTransactionAsync(ct);
        await using (var command = new NpgsqlCommand(
            """
            INSERT INTO tax_schema.tax_quotes
                (id, tenant_id, store_id, idempotency_key, currency_code, status, customer_id,
                 order_id, jurisdiction_country_code, jurisdiction_zone_code, jurisdiction_state_province,
                 taxable_amount, total_tax_amount, calculated_at, correlation_id)
            VALUES (@id, @tenant, @store, @key, @currency, 'Calculated', @customer, @order,
                    @country, @zone, @state, @taxable, @total, @calculated, @correlation)
            """, connection, transaction))
        {
            command.Parameters.AddWithValue("id", quote.Id);
            AddContext(command, context);
            command.Parameters.AddWithValue("key", (object?)quote.IdempotencyKey ?? DBNull.Value);
            command.Parameters.AddWithValue("currency", quote.CurrencyCode);
            command.Parameters.AddWithValue("customer", (object?)quote.CustomerId ?? DBNull.Value);
            command.Parameters.AddWithValue("order", (object?)quote.OrderId ?? DBNull.Value);
            command.Parameters.AddWithValue("country", (object?)quote.JurisdictionCountryCode ?? DBNull.Value);
            command.Parameters.AddWithValue("zone", (object?)quote.JurisdictionZoneCode ?? DBNull.Value);
            command.Parameters.AddWithValue("state", (object?)quote.JurisdictionStateProvince ?? DBNull.Value);
            command.Parameters.AddWithValue("taxable", quote.TaxableAmount);
            command.Parameters.AddWithValue("total", quote.TotalTaxAmount);
            command.Parameters.AddWithValue("calculated", quote.CalculatedAt);
            command.Parameters.AddWithValue("correlation", context.CorrelationId);
            await command.ExecuteNonQueryAsync(ct);
        }

        foreach (var item in items)
        {
            await using var command = new NpgsqlCommand(
                """
                INSERT INTO tax_schema.tax_quote_items
                    (id, tax_quote_id, tax_class_id, tax_code, label, rate_percent,
                     taxable_amount, tax_amount, piggyback, priority)
                VALUES (@id, @quote, @class, @code, @label, @rate, @taxable, @amount, @piggyback, @priority)
                """, connection, transaction);
            command.Parameters.AddWithValue("id", item.Id);
            command.Parameters.AddWithValue("quote", item.TaxQuoteId);
            command.Parameters.AddWithValue("class", (object?)item.TaxClassId ?? DBNull.Value);
            command.Parameters.AddWithValue("code", item.TaxCode);
            command.Parameters.AddWithValue("label", item.Label);
            command.Parameters.AddWithValue("rate", item.RatePercent);
            command.Parameters.AddWithValue("taxable", item.TaxableAmount);
            command.Parameters.AddWithValue("amount", item.TaxAmount);
            command.Parameters.AddWithValue("piggyback", item.Piggyback);
            command.Parameters.AddWithValue("priority", item.Priority);
            await command.ExecuteNonQueryAsync(ct);
        }
        await transaction.CommitAsync(ct);
    }

    private async Task<bool> ExistsAsync(string sql, string code, RequestContext context, CancellationToken ct)
    {
        await using var connection = await dataSource.OpenConnectionAsync(ct);
        await using var command = new NpgsqlCommand(sql, connection);
        AddContext(command, context);
        command.Parameters.AddWithValue("code", code);
        return (bool)(await command.ExecuteScalarAsync(ct) ?? false);
    }

    private async Task<long> CountAsync(string tableOrQuery, RequestContext context, CancellationToken ct, params (string Name, object Value)[] extra)
    {
        var from = tableOrQuery.StartsWith('(') ? tableOrQuery : tableOrQuery;
        await using var connection = await dataSource.OpenConnectionAsync(ct);
        await using var command = new NpgsqlCommand(
            $"SELECT COUNT(*) FROM {from} WHERE tenant_id = @tenant AND store_id = @store", connection);
        AddContext(command, context);
        foreach (var parameter in extra) command.Parameters.AddWithValue(parameter.Name, parameter.Value);
        return (long)(await command.ExecuteScalarAsync(ct) ?? 0L);
    }

    private async Task<long> CountRatesAsync(RequestContext context, string languageCode, CancellationToken ct)
    {
        await using var connection = await dataSource.OpenConnectionAsync(ct);
        await using var command = new NpgsqlCommand(
            """
            SELECT COUNT(*)
            FROM tax_schema.tax_rates r
            JOIN tax_schema.tax_rate_descriptions d ON d.tax_rate_id = r.id
            WHERE r.tenant_id = @tenant AND r.store_id = @store AND d.language_code = @language
            """, connection);
        AddContext(command, context);
        command.Parameters.AddWithValue("language", languageCode);
        return (long)(await command.ExecuteScalarAsync(ct) ?? 0L);
    }

    private static TaxClassEntity ReadClass(NpgsqlDataReader reader) => new()
    {
        Id = reader.GetGuid(0),
        TenantId = reader.GetString(1),
        StoreId = reader.GetString(2),
        Code = reader.GetString(3),
        Title = reader.GetString(4)
    };

    private static TaxRateEntity ReadRate(NpgsqlDataReader reader) => new()
    {
        Id = reader.GetGuid(0),
        TenantId = reader.GetString(1),
        StoreId = reader.GetString(2),
        TaxClassId = reader.GetGuid(3),
        TaxClassCode = reader.GetString(4),
        Code = reader.GetString(5),
        RatePercent = reader.GetDecimal(6),
        Priority = reader.GetInt32(7),
        Piggyback = reader.GetBoolean(8),
        CountryCode = reader.GetString(9),
        ZoneCode = reader.IsDBNull(10) ? null : reader.GetString(10),
        StateProvince = reader.IsDBNull(11) ? null : reader.GetString(11),
        ParentRateId = reader.IsDBNull(12) ? null : reader.GetGuid(12)
    };

    private static TaxRateDescriptionEntity ReadDescription(NpgsqlDataReader reader, int offset, Guid rateId) => new()
    {
        Id = reader.GetGuid(offset),
        TaxRateId = rateId,
        LanguageCode = reader.GetString(offset + 1),
        Name = reader.GetString(offset + 2),
        Title = reader.IsDBNull(offset + 3) ? null : reader.GetString(offset + 3),
        Description = reader.IsDBNull(offset + 4) ? null : reader.GetString(offset + 4)
    };

    private static TaxQuoteEntity ReadQuote(NpgsqlDataReader reader) => new()
    {
        Id = reader.GetGuid(0),
        TenantId = reader.GetString(1),
        StoreId = reader.GetString(2),
        IdempotencyKey = reader.IsDBNull(3) ? null : reader.GetString(3),
        CurrencyCode = reader.GetString(4),
        Status = reader.GetString(5),
        CustomerId = reader.IsDBNull(6) ? null : reader.GetGuid(6),
        OrderId = reader.IsDBNull(7) ? null : reader.GetGuid(7),
        JurisdictionCountryCode = reader.IsDBNull(8) ? null : reader.GetString(8),
        JurisdictionZoneCode = reader.IsDBNull(9) ? null : reader.GetString(9),
        JurisdictionStateProvince = reader.IsDBNull(10) ? null : reader.GetString(10),
        TaxableAmount = reader.GetDecimal(11),
        TotalTaxAmount = reader.GetDecimal(12),
        CalculatedAt = reader.GetFieldValue<DateTimeOffset>(13)
    };

    private async Task<TaxRateEntity?> ReadRateAsync(
        NpgsqlConnection connection, string sql, Action<NpgsqlCommand> configure, CancellationToken ct)
    {
        await using var command = new NpgsqlCommand(sql, connection);
        configure(command);
        await using var reader = await command.ExecuteReaderAsync(ct);
        return await reader.ReadAsync(ct) ? ReadRate(reader) : null;
    }

    private static async Task LoadDescriptionsAsync(
        NpgsqlConnection connection, TaxRateEntity entity, string? languageCode, CancellationToken ct)
    {
        await using var command = new NpgsqlCommand(
            """
            SELECT id, language_code, name, title, description
            FROM tax_schema.tax_rate_descriptions
            WHERE tax_rate_id = @rate AND (@language IS NULL OR language_code = @language)
            ORDER BY language_code
            """, connection);
        command.Parameters.AddWithValue("rate", entity.Id);
        command.Parameters.Add("language", NpgsqlDbType.Varchar).Value = (object?)languageCode ?? DBNull.Value;
        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
            entity.Descriptions.Add(new TaxRateDescriptionEntity
            {
                Id = reader.GetGuid(0),
                TaxRateId = entity.Id,
                LanguageCode = reader.GetString(1),
                Name = reader.GetString(2),
                Title = reader.IsDBNull(3) ? null : reader.GetString(3),
                Description = reader.IsDBNull(4) ? null : reader.GetString(4)
            });
    }

    private static async Task InsertRateAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, TaxRateEntity entity, RequestContext context, CancellationToken ct)
    {
        await using var command = new NpgsqlCommand(
            """
            INSERT INTO tax_schema.tax_rates
                (id, tenant_id, store_id, tax_class_id, code, rate_percent, priority, piggyback,
                 country_code, zone_code, state_province, parent_rate_id, created_by, correlation_id)
            VALUES (@id, @tenant, @store, @class, @code, @rate, @priority, @piggyback,
                    @country, @zone, @state, @parent, @created_by, @correlation)
            """, connection, transaction);
        command.Parameters.AddWithValue("id", entity.Id);
        AddContext(command, context);
        AddRateParameters(command, entity);
        command.Parameters.AddWithValue("parent", (object?)entity.ParentRateId ?? DBNull.Value);
        command.Parameters.AddWithValue("created_by", DBNull.Value);
        command.Parameters.AddWithValue("correlation", context.CorrelationId);
        await command.ExecuteNonQueryAsync(ct);
    }

    private static async Task ReplaceDescriptionsAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, TaxRateEntity entity, RequestContext context, CancellationToken ct)
    {
        await using (var delete = new NpgsqlCommand(
            "DELETE FROM tax_schema.tax_rate_descriptions WHERE tax_rate_id = @rate", connection, transaction))
        {
            delete.Parameters.AddWithValue("rate", entity.Id);
            await delete.ExecuteNonQueryAsync(ct);
        }

        foreach (var description in entity.Descriptions)
        {
            await using var insert = new NpgsqlCommand(
                """
                INSERT INTO tax_schema.tax_rate_descriptions
                    (id, tax_rate_id, language_code, name, title, description, created_by, correlation_id)
                VALUES (@id, @rate, @language, @name, @title, @description, @created_by, @correlation)
                """, connection, transaction);
            insert.Parameters.AddWithValue("id", description.Id);
            insert.Parameters.AddWithValue("rate", entity.Id);
            insert.Parameters.AddWithValue("language", description.LanguageCode);
            insert.Parameters.AddWithValue("name", description.Name);
            insert.Parameters.AddWithValue("title", (object?)description.Title ?? DBNull.Value);
            insert.Parameters.AddWithValue("description", (object?)description.Description ?? DBNull.Value);
            insert.Parameters.AddWithValue("created_by", DBNull.Value);
            insert.Parameters.AddWithValue("correlation", context.CorrelationId);
            await insert.ExecuteNonQueryAsync(ct);
        }
    }

    private static void AddRateParameters(NpgsqlCommand command, TaxRateEntity entity)
    {
        command.Parameters.AddWithValue("class", entity.TaxClassId);
        command.Parameters.AddWithValue("code", entity.Code);
        command.Parameters.AddWithValue("rate", entity.RatePercent);
        command.Parameters.AddWithValue("priority", entity.Priority);
        command.Parameters.AddWithValue("piggyback", entity.Piggyback);
        command.Parameters.AddWithValue("country", entity.CountryCode);
        command.Parameters.Add("zone", NpgsqlDbType.Varchar).Value = (object?)entity.ZoneCode ?? DBNull.Value;
        command.Parameters.Add("state", NpgsqlDbType.Varchar).Value = (object?)entity.StateProvince ?? DBNull.Value;
    }

    private static void AddContext(NpgsqlCommand command, RequestContext context)
    {
        command.Parameters.AddWithValue("tenant", context.TenantId);
        command.Parameters.AddWithValue("store", context.StoreId);
    }
}
