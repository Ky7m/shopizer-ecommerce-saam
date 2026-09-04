using System.Text.Json;
using Npgsql;
using NpgsqlTypes;
using Shopizer.MerchantAdministration.Models;

namespace Shopizer.MerchantAdministration.Data;

public sealed class StoreRepository(NpgsqlDataSource dataSource)
{
    private static void P(NpgsqlCommand c, string name, object? value) => c.Parameters.AddWithValue(name, value ?? DBNull.Value);
    private static void Text(NpgsqlCommand c, string name, string? value) => c.Parameters.Add(name, NpgsqlDbType.Text).Value = value ?? (object)DBNull.Value;

    public async Task<StoreRecord?> FindAsync(string code, RequestContext ctx, CancellationToken ct, NpgsqlConnection? connection = null, NpgsqlTransaction? transaction = null)
    {
        var owned = connection is null;
        connection ??= await dataSource.OpenConnectionAsync(ct);
        try
        {
            await using var command = new NpgsqlCommand("""SELECT s.id,s.tenant_id,s.code,s.name,s.email_address,s.phone,s.street_address,s.city,s.postal_code,s.country_code,s.state_province,s.zone_code,s.retailer,s.parent_store_id,s.default_language_code,s.currency_code,s.dimension_unit,s.weight_unit,s.template_code,s.logo_uri,s.status,s.created_at,s.updated_at,(SELECT p.code FROM merchant_store.stores p WHERE p.id=s.parent_store_id),COALESCE((SELECT array_agg(sl.language_code ORDER BY sl.language_code) FROM merchant_store.store_languages sl WHERE sl.store_id=s.id), ARRAY[]::varchar[]) FROM merchant_store.stores s WHERE s.tenant_id=@tenant AND lower(s.code)=lower(@code) AND s.status <> 'Deleted'""", connection, transaction);
            P(command, "tenant", ctx.TenantId); P(command, "code", code);
            await using var reader = await command.ExecuteReaderAsync(ct);
            return await reader.ReadAsync(ct) ? Read(reader) : null;
        }
        finally { if (owned) await connection.DisposeAsync(); }
    }

    public async Task<(List<StoreRecord> Items, long Total)> ListAsync(RequestContext ctx, int page, int pageSize, CancellationToken ct, string? parentCode = null)
    {
        page = Math.Max(page, 1); pageSize = Math.Clamp(pageSize, 1, 200);
        await using var connection = await dataSource.OpenConnectionAsync(ct);
        var parentFilter = string.IsNullOrWhiteSpace(parentCode) ? "" : " AND s.parent_store_id = (SELECT id FROM merchant_store.stores WHERE tenant_id=@tenant AND lower(code)=lower(@parent) AND status <> 'Deleted')";
        await using var count = new NpgsqlCommand($"SELECT count(*) FROM merchant_store.stores s WHERE s.tenant_id=@tenant AND s.status <> 'Deleted'{parentFilter}", connection);
        P(count, "tenant", ctx.TenantId); if (!string.IsNullOrWhiteSpace(parentCode)) P(count, "parent", parentCode);
        var total = Convert.ToInt64(await count.ExecuteScalarAsync(ct));
        await using var command = new NpgsqlCommand($"""SELECT s.id,s.tenant_id,s.code,s.name,s.email_address,s.phone,s.street_address,s.city,s.postal_code,s.country_code,s.state_province,s.zone_code,s.retailer,s.parent_store_id,s.default_language_code,s.currency_code,s.dimension_unit,s.weight_unit,s.template_code,s.logo_uri,s.status,s.created_at,s.updated_at,(SELECT p.code FROM merchant_store.stores p WHERE p.id=s.parent_store_id),COALESCE((SELECT array_agg(sl.language_code ORDER BY sl.language_code) FROM merchant_store.store_languages sl WHERE sl.store_id=s.id), ARRAY[]::varchar[]) FROM merchant_store.stores s WHERE s.tenant_id=@tenant AND s.status <> 'Deleted'{parentFilter} ORDER BY lower(s.code),s.id OFFSET @offset LIMIT @limit""", connection);
        P(command, "tenant", ctx.TenantId); if (!string.IsNullOrWhiteSpace(parentCode)) P(command, "parent", parentCode); P(command, "offset", (page - 1) * pageSize); P(command, "limit", pageSize);
        var result = new List<StoreRecord>(); await using var reader = await command.ExecuteReaderAsync(ct); while (await reader.ReadAsync(ct)) result.Add(Read(reader));
        return (result, total);
    }

    public async Task<List<StoreRecord>> DescendantsAsync(StoreRecord root, CancellationToken ct)
    {
        await using var connection = await dataSource.OpenConnectionAsync(ct);
        await using var command = new NpgsqlCommand("""WITH RECURSIVE tree AS (SELECT id FROM merchant_store.stores WHERE id=@root UNION ALL SELECT s.id FROM merchant_store.stores s JOIN tree t ON s.parent_store_id=t.id WHERE s.status <> 'Deleted') SELECT s.id,s.tenant_id,s.code,s.name,s.email_address,s.phone,s.street_address,s.city,s.postal_code,s.country_code,s.state_province,s.zone_code,s.retailer,s.parent_store_id,s.default_language_code,s.currency_code,s.dimension_unit,s.weight_unit,s.template_code,s.logo_uri,s.status,s.created_at,s.updated_at,(SELECT p.code FROM merchant_store.stores p WHERE p.id=s.parent_store_id),COALESCE((SELECT array_agg(sl.language_code ORDER BY sl.language_code) FROM merchant_store.store_languages sl WHERE sl.store_id=s.id), ARRAY[]::varchar[]) FROM merchant_store.stores s JOIN tree t ON t.id=s.id WHERE s.status <> 'Deleted' ORDER BY lower(s.code),s.id""", connection);
        P(command, "root", root.Id); var result = new List<StoreRecord>(); await using var reader = await command.ExecuteReaderAsync(ct); while (await reader.ReadAsync(ct)) result.Add(Read(reader)); return result;
    }

    public async Task<StoreRecord> CreateAsync(StoreRecord store, string? parentCode, RequestContext ctx, CancellationToken ct)
    {
        await using var connection = await dataSource.OpenConnectionAsync(ct); await using var tx = await connection.BeginTransactionAsync(ct);
        try
        {
            if (!string.IsNullOrWhiteSpace(parentCode))
            {
                await using var parent = new NpgsqlCommand("SELECT id,status,retailer FROM merchant_store.stores WHERE tenant_id=@tenant AND lower(code)=lower(@code)", connection, tx); P(parent, "tenant", ctx.TenantId); P(parent, "code", parentCode);
                await using var r = await parent.ExecuteReaderAsync(ct); if (!await r.ReadAsync(ct)) throw new DomainException("INVALID_PARENT", "Parent store was not found", 422);
                if (r.GetString(1) != "Active" || !r.GetBoolean(2)) throw new DomainException("INVALID_PARENT", "Parent store must be an active retailer", 422); store.ParentStoreId = r.GetGuid(0); await r.CloseAsync();
            }
            await using (var command = new NpgsqlCommand("""INSERT INTO merchant_store.stores(id,tenant_id,code,name,email_address,phone,street_address,city,postal_code,country_code,state_province,zone_code,retailer,parent_store_id,default_language_code,currency_code,dimension_unit,weight_unit,status) VALUES(@id,@tenant,@code,@name,@email,@phone,@street,@city,@postal,@country,@state,@zone,@retailer,@parent,@language,@currency,@dimension,@weight,'Active')""", connection, tx))
            {
                P(command, "id", store.Id); P(command, "tenant", store.TenantId); P(command, "code", store.Code); P(command, "name", store.Name); P(command, "email", store.EmailAddress); P(command, "phone", store.Phone); P(command, "street", store.StreetAddress); P(command, "city", store.City); P(command, "postal", store.PostalCode); P(command, "country", store.CountryCode); P(command, "state", store.StateProvince); P(command, "zone", store.ZoneCode); P(command, "retailer", store.Retailer); P(command, "parent", store.ParentStoreId); P(command, "language", store.DefaultLanguageCode); P(command, "currency", store.CurrencyCode); P(command, "dimension", store.DimensionUnit); P(command, "weight", store.WeightUnit); await command.ExecuteNonQueryAsync(ct);
            }
            foreach (var language in store.SupportedLanguageCodes.Distinct(StringComparer.OrdinalIgnoreCase)) { await using var languageCommand = new NpgsqlCommand("INSERT INTO merchant_store.store_languages(store_id,language_code) VALUES(@store,@language)", connection, tx); P(languageCommand, "store", store.Id); P(languageCommand, "language", language); await languageCommand.ExecuteNonQueryAsync(ct); }
            var occurred = DateTimeOffset.UtcNow; var payload = JsonSerializer.Serialize(new { eventId = store.Id, eventType = "StoreCreated", eventVersion = 1, occurredAt = occurred, tenantId = ctx.TenantId, storeId = store.Code, correlationId = ctx.CorrelationId, code = store.Code, name = store.Name, emailAddress = store.EmailAddress, defaultLanguageCode = store.DefaultLanguageCode, supportedLanguageCodes = store.SupportedLanguageCodes });
            await using var outbox = new NpgsqlCommand("INSERT INTO merchant_store.event_outbox(id,event_type,tenant_id,store_id,correlation_id,payload,occurred_at) VALUES(@id,'StoreCreated',@tenant,@store,@correlation,@payload,@occurred)", connection, tx); P(outbox, "id", store.Id); P(outbox, "tenant", ctx.TenantId); P(outbox, "store", store.Code); P(outbox, "correlation", ctx.CorrelationId); outbox.Parameters.Add("payload", NpgsqlDbType.Jsonb).Value = payload; P(outbox, "occurred", occurred); await outbox.ExecuteNonQueryAsync(ct);
            await tx.CommitAsync(ct); return store;
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation) { await tx.RollbackAsync(ct); throw new DomainException("STORE_CODE_CONFLICT", "Store code is already registered for this tenant", 409); }
        catch { await tx.RollbackAsync(ct); throw; }
    }

    public async Task UpdateAsync(StoreRecord store, RequestContext ctx, CancellationToken ct)
    {
        await using var connection = await dataSource.OpenConnectionAsync(ct); await using var tx = await connection.BeginTransactionAsync(ct);
        await using var command = new NpgsqlCommand("""UPDATE merchant_store.stores SET name=@name,email_address=@email,phone=@phone,street_address=@street,city=@city,postal_code=@postal,country_code=@country,state_province=@state,zone_code=@zone,default_language_code=@language,currency_code=@currency,dimension_unit=@dimension,weight_unit=@weight,updated_at=now() WHERE id=@id AND tenant_id=@tenant AND status <> 'Deleted'""", connection, tx);
        P(command, "name", store.Name); P(command, "email", store.EmailAddress); P(command, "phone", store.Phone); P(command, "street", store.StreetAddress); P(command, "city", store.City); P(command, "postal", store.PostalCode); P(command, "country", store.CountryCode); P(command, "state", store.StateProvince); P(command, "zone", store.ZoneCode); P(command, "language", store.DefaultLanguageCode); P(command, "currency", store.CurrencyCode); P(command, "dimension", store.DimensionUnit); P(command, "weight", store.WeightUnit); P(command, "id", store.Id); P(command, "tenant", ctx.TenantId); if (await command.ExecuteNonQueryAsync(ct) == 0) throw new DomainException("STORE_NOT_FOUND", "Store was not found", 404);
        await using var clear = new NpgsqlCommand("DELETE FROM merchant_store.store_languages WHERE store_id=@id", connection, tx); P(clear, "id", store.Id); await clear.ExecuteNonQueryAsync(ct);
        foreach (var language in store.SupportedLanguageCodes.Distinct(StringComparer.OrdinalIgnoreCase)) { await using var add = new NpgsqlCommand("INSERT INTO merchant_store.store_languages(store_id,language_code) VALUES(@id,@language)", connection, tx); P(add, "id", store.Id); P(add, "language", language); await add.ExecuteNonQueryAsync(ct); }
        await tx.CommitAsync(ct);
    }

    public async Task UpdateBrandingAsync(StoreRecord store, RequestContext ctx, CancellationToken ct)
    { await using var connection = await dataSource.OpenConnectionAsync(ct); await using var command = new NpgsqlCommand("UPDATE merchant_store.stores SET template_code=@template,logo_uri=@logo,updated_at=now() WHERE id=@id AND tenant_id=@tenant AND status <> 'Deleted'", connection); P(command, "template", store.TemplateCode); P(command, "logo", store.LogoUri); P(command, "id", store.Id); P(command, "tenant", ctx.TenantId); if (await command.ExecuteNonQueryAsync(ct) == 0) throw new DomainException("STORE_NOT_FOUND", "Store was not found", 404); }

    public async Task DeleteAsync(StoreRecord store, RequestContext ctx, CancellationToken ct)
    { await using var connection = await dataSource.OpenConnectionAsync(ct); await using var tx = await connection.BeginTransactionAsync(ct); await using var children = new NpgsqlCommand("SELECT count(*) FROM merchant_store.stores WHERE tenant_id=@tenant AND parent_store_id=@id AND status <> 'Deleted'", connection, tx); P(children, "tenant", ctx.TenantId); P(children, "id", store.Id); if (Convert.ToInt64(await children.ExecuteScalarAsync(ct)) > 0) throw new DomainException("CHILD_STORES_EXIST", "Store has active child stores", 409); await using var command = new NpgsqlCommand("UPDATE merchant_store.stores SET status='Deleted',updated_at=now() WHERE id=@id AND tenant_id=@tenant AND status <> 'Deleted'", connection, tx); P(command, "id", store.Id); P(command, "tenant", ctx.TenantId); if (await command.ExecuteNonQueryAsync(ct) == 0) throw new DomainException("STORE_NOT_FOUND", "Store was not found", 404); await tx.CommitAsync(ct); }


    public async Task<SignupRecord> CreateSignupAsync(string tenant, string code, string payload, string tokenHash, DateTimeOffset expiry, CancellationToken ct)
    { await using var connection = await dataSource.OpenConnectionAsync(ct); var id = Guid.NewGuid(); await using var command = new NpgsqlCommand("INSERT INTO merchant_store.store_signups(id,tenant_id,code,payload,token_hash,expires_at) VALUES(@id,@tenant,@code,@payload,@hash,@expiry)", connection); P(command, "id", id); P(command, "tenant", tenant); P(command, "code", code); command.Parameters.Add("payload", NpgsqlDbType.Jsonb).Value = payload; P(command, "hash", tokenHash); P(command, "expiry", expiry); await command.ExecuteNonQueryAsync(ct); return new SignupRecord(id, tenant, code, payload, tokenHash, expiry, null); }

    public async Task<SignupRecord?> FindSignupAsync(string tenant, string code, string tokenHash, CancellationToken ct)
    { await using var connection = await dataSource.OpenConnectionAsync(ct); await using var command = new NpgsqlCommand("SELECT id,tenant_id,code,payload::text,token_hash,expires_at,consumed_at FROM merchant_store.store_signups WHERE tenant_id=@tenant AND lower(code)=lower(@code) AND token_hash=@hash", connection); P(command, "tenant", tenant); P(command, "code", code); P(command, "hash", tokenHash); await using var reader = await command.ExecuteReaderAsync(ct); return await reader.ReadAsync(ct) ? new SignupRecord(reader.GetGuid(0), reader.GetString(1), reader.GetString(2), reader.GetString(3), reader.GetString(4), reader.GetFieldValue<DateTimeOffset>(5), reader.IsDBNull(6) ? null : reader.GetFieldValue<DateTimeOffset>(6)) : null; }

    public async Task ConsumeSignupAsync(Guid id, CancellationToken ct)
    { await using var connection = await dataSource.OpenConnectionAsync(ct); await using var command = new NpgsqlCommand("UPDATE merchant_store.store_signups SET consumed_at=now() WHERE id=@id AND consumed_at IS NULL", connection); P(command, "id", id); await command.ExecuteNonQueryAsync(ct); }

    public async Task MarkEventPublishedAsync(Guid id, CancellationToken ct)
    { await using var connection = await dataSource.OpenConnectionAsync(ct); await using var command = new NpgsqlCommand("UPDATE merchant_store.event_outbox SET published_at=now() WHERE id=@id AND published_at IS NULL", connection); P(command, "id", id); await command.ExecuteNonQueryAsync(ct); }

    private static StoreRecord Read(NpgsqlDataReader r)
    {
        var store = new StoreRecord { Id = r.GetGuid(0), TenantId = r.GetString(1), Code = r.GetString(2), Name = r.GetString(3), EmailAddress = r.GetString(4), Phone = r.GetString(5), StreetAddress = r.IsDBNull(6) ? null : r.GetString(6), City = r.GetString(7), PostalCode = r.GetString(8), CountryCode = r.GetString(9), StateProvince = r.IsDBNull(10) ? null : r.GetString(10), ZoneCode = r.IsDBNull(11) ? null : r.GetString(11), Retailer = r.GetBoolean(12), ParentStoreId = r.IsDBNull(13) ? null : r.GetGuid(13), ParentStoreCode = r.IsDBNull(23) ? null : r.GetString(23), DefaultLanguageCode = r.GetString(14), CurrencyCode = r.GetString(15), DimensionUnit = r.GetString(16), WeightUnit = r.GetString(17), TemplateCode = r.IsDBNull(18) ? null : r.GetString(18), LogoUri = r.IsDBNull(19) ? null : r.GetString(19), Status = r.GetString(20), CreatedAt = r.GetFieldValue<DateTimeOffset>(21), UpdatedAt = r.GetFieldValue<DateTimeOffset>(22) };
        if (!r.IsDBNull(24)) store.SupportedLanguageCodes.AddRange(r.GetFieldValue<string[]>(24)); return store;
    }
}
