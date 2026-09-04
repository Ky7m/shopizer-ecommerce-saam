using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Npgsql;
using NpgsqlTypes;
using Shopizer.Shipping.Models;

namespace Shopizer.Shipping.Data;

public sealed class ShippingRepository(NpgsqlDataSource dataSource)
{
    private static Guid Scope(string value)
    {
        if (Guid.TryParse(value, out var id)) return id;
        var bytes = MD5.HashData(Encoding.UTF8.GetBytes(value));
        return new Guid(bytes);
    }

    private static void P(NpgsqlCommand command, string name, object? value) =>
        command.Parameters.AddWithValue(name, value ?? DBNull.Value);

    private static void Inet(NpgsqlCommand command, string name, string? value)
    {
        var parameter = command.Parameters.Add(name, NpgsqlDbType.Inet);
        parameter.Value = IPAddress.TryParse(value, out var address) ? address : DBNull.Value;
    }

    private static DateTimeOffset? ParseTimestamp(string? value) =>
        DateTimeOffset.TryParse(value, out var timestamp) ? timestamp : null;

    private static void Json(NpgsqlCommand command, string name, object value) =>
        command.Parameters.Add(name, NpgsqlDbType.Jsonb).Value = System.Text.Json.JsonSerializer.Serialize(value);

    public async Task<ShippingOriginRecord?> GetOriginAsync(RequestContext context, CancellationToken ct)
    {
        await using var db = await dataSource.OpenConnectionAsync(ct);
        await using var command = new NpgsqlCommand("""
            SELECT id, tenant_id, store_id, address, city, postal_code, state, country_code,
                   zone_code, active, created_at, updated_at
            FROM shipping.shipping_origin
            WHERE tenant_id = @tenant AND store_id = @store AND active = true
            ORDER BY updated_at DESC LIMIT 1
            """, db);
        P(command, "tenant", Scope(context.TenantId)); P(command, "store", Scope(context.StoreId));
        await using var reader = await command.ExecuteReaderAsync(ct);
        return await reader.ReadAsync(ct) ? ReadOrigin(reader) : null;
    }

    public async Task<ShippingOriginRecord> SaveOriginAsync(ShippingOriginRecord origin, RequestContext context,
        CancellationToken ct)
    {
        await using var db = await dataSource.OpenConnectionAsync(ct);
        await using var tx = await db.BeginTransactionAsync(ct);
        await using (var retire = new NpgsqlCommand("""
            UPDATE shipping.shipping_origin SET active = false, updated_at = CURRENT_TIMESTAMP
            WHERE tenant_id = @tenant AND store_id = @store AND active = true
            """, db, tx))
        {
            P(retire, "tenant", Scope(context.TenantId)); P(retire, "store", Scope(context.StoreId));
            await retire.ExecuteNonQueryAsync(ct);
        }

        await using var command = new NpgsqlCommand("""
            INSERT INTO shipping.shipping_origin
              (id, tenant_id, store_id, address, city, postal_code, state, country_code, zone_code, active)
            VALUES (@id, @tenant, @store, @address, @city, @postal, @state, @country, @zone, @active)
            RETURNING created_at, updated_at
            """, db, tx);
        P(command, "id", origin.Id); P(command, "tenant", Scope(context.TenantId)); P(command, "store", Scope(context.StoreId));
        P(command, "address", origin.Address); P(command, "city", origin.City); P(command, "postal", origin.PostalCode);
        P(command, "state", origin.State); P(command, "country", origin.CountryCode);
        P(command, "zone", origin.ZoneCode); P(command, "active", origin.Active);
        await using var reader = await command.ExecuteReaderAsync(ct);
        await reader.ReadAsync(ct);
        origin.CreatedAt = reader.GetFieldValue<DateTimeOffset>(0);
        origin.UpdatedAt = reader.GetFieldValue<DateTimeOffset>(1);
        await reader.CloseAsync();
        await tx.CommitAsync(ct);
        return origin;
    }

    public async Task<ShippingConfigurationRecord> GetConfigurationAsync(RequestContext context, CancellationToken ct)
    {
        await using var db = await dataSource.OpenConnectionAsync(ct);
        await using var command = new NpgsqlCommand("""
            SELECT shipping_type, shipping_basis_type, shipping_option_price_type, shipping_package_type,
                   shipping_description, free_shipping_type, box_width, box_height, box_length, box_weight,
                   max_weight, free_shipping_enabled, order_total_free_shipping, handling_fees, tax_on_shipping
            FROM shipping.shipping_configuration_projection
            WHERE tenant_id = @tenant AND store_id = @store
            """, db);
        P(command, "tenant", Scope(context.TenantId)); P(command, "store", Scope(context.StoreId));
        await using var reader = await command.ExecuteReaderAsync(ct);
        ShippingConfigurationRecord result;
        if (await reader.ReadAsync(ct))
            result = ReadConfiguration(reader);
        else
            result = new ShippingConfigurationRecord();
        await reader.CloseAsync();
        result.Packages = await ListPackagesAsync(context, ct);
        return result;
    }

    public async Task<ShippingConfigurationRecord> SaveConfigurationAsync(ShippingConfigurationRecord value,
        RequestContext context, CancellationToken ct)
    {
        await using var db = await dataSource.OpenConnectionAsync(ct);
        await using var command = new NpgsqlCommand("""
            INSERT INTO shipping.shipping_configuration_projection
              (tenant_id, store_id, shipping_type, shipping_basis_type, shipping_option_price_type,
               shipping_package_type, shipping_description, free_shipping_type, box_width, box_height,
               box_length, box_weight, max_weight, free_shipping_enabled, order_total_free_shipping,
               handling_fees, tax_on_shipping)
            VALUES (@tenant, @store, @shippingType, @basis, @priceType, @packageType, @description,
                    @freeType, @width, @height, @length, @boxWeight, @maxWeight, @freeEnabled,
                    @freeThreshold, @handling, @tax)
            ON CONFLICT (tenant_id, store_id) DO UPDATE SET
              shipping_type = EXCLUDED.shipping_type, shipping_basis_type = EXCLUDED.shipping_basis_type,
              shipping_option_price_type = EXCLUDED.shipping_option_price_type,
              shipping_package_type = EXCLUDED.shipping_package_type,
              shipping_description = EXCLUDED.shipping_description, free_shipping_type = EXCLUDED.free_shipping_type,
              box_width = EXCLUDED.box_width, box_height = EXCLUDED.box_height, box_length = EXCLUDED.box_length,
              box_weight = EXCLUDED.box_weight, max_weight = EXCLUDED.max_weight,
              free_shipping_enabled = EXCLUDED.free_shipping_enabled,
              order_total_free_shipping = EXCLUDED.order_total_free_shipping,
              handling_fees = EXCLUDED.handling_fees, tax_on_shipping = EXCLUDED.tax_on_shipping,
              updated_at = CURRENT_TIMESTAMP
            """, db);
        P(command, "tenant", Scope(context.TenantId)); P(command, "store", Scope(context.StoreId));
        P(command, "shippingType", value.ShippingType); P(command, "basis", value.ShippingBasisType);
        P(command, "priceType", value.ShippingOptionPriceType); P(command, "packageType", value.ShippingPackageType);
        P(command, "description", value.ShippingDescription); P(command, "freeType", value.FreeShippingType);
        P(command, "width", value.BoxWidth); P(command, "height", value.BoxHeight); P(command, "length", value.BoxLength);
        P(command, "boxWeight", value.BoxWeight); P(command, "maxWeight", value.MaxWeight);
        P(command, "freeEnabled", value.FreeShippingEnabled); P(command, "freeThreshold", value.OrderTotalFreeShipping);
        P(command, "handling", value.HandlingFees); P(command, "tax", value.TaxOnShipping);
        await command.ExecuteNonQueryAsync(ct);
        return value;
    }

    public async Task<List<ShippingModuleRecord>> ListModulesAsync(RequestContext context, CancellationToken ct)
    {
        await using var db = await dataSource.OpenConnectionAsync(ct);
        await using var command = new NpgsqlCommand("""
            SELECT module_code, active, default_selected, environment, integration_keys, integration_options
            FROM shipping.shipping_module_projection
            WHERE tenant_id = @tenant AND store_id = @store
            ORDER BY module_code
            """, db);
        P(command, "tenant", Scope(context.TenantId)); P(command, "store", Scope(context.StoreId));
        var result = new List<ShippingModuleRecord>();
        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct)) result.Add(ReadModule(reader));
        return result;
    }

    public async Task<ShippingModuleRecord?> GetModuleAsync(string code, RequestContext context, CancellationToken ct)
    {
        await using var db = await dataSource.OpenConnectionAsync(ct);
        await using var command = new NpgsqlCommand("""
            SELECT module_code, active, default_selected, environment, integration_keys, integration_options
            FROM shipping.shipping_module_projection
            WHERE tenant_id = @tenant AND store_id = @store AND module_code = @code
            """, db);
        P(command, "tenant", Scope(context.TenantId)); P(command, "store", Scope(context.StoreId)); P(command, "code", code);
        await using var reader = await command.ExecuteReaderAsync(ct);
        return await reader.ReadAsync(ct) ? ReadModule(reader) : null;
    }

    public async Task<ShippingModuleRecord> SaveModuleAsync(ShippingModuleRecord value, RequestContext context,
        CancellationToken ct)
    {
        await using var db = await dataSource.OpenConnectionAsync(ct);
        await using var command = new NpgsqlCommand("""
            INSERT INTO shipping.shipping_module_projection
              (tenant_id, store_id, module_code, active, default_selected, environment,
               integration_keys, integration_options)
            VALUES (@tenant, @store, @code, @active, @selected, @environment, @keys, @options)
            ON CONFLICT (tenant_id, store_id, module_code) DO UPDATE SET
              active = EXCLUDED.active, default_selected = EXCLUDED.default_selected,
              environment = EXCLUDED.environment, integration_keys = EXCLUDED.integration_keys,
              integration_options = EXCLUDED.integration_options, updated_at = CURRENT_TIMESTAMP
            """, db);
        P(command, "tenant", Scope(context.TenantId)); P(command, "store", Scope(context.StoreId));
        P(command, "code", value.ModuleCode); P(command, "active", value.Active);
        P(command, "selected", value.DefaultSelected); P(command, "environment", value.Environment);
        Json(command, "keys", value.IntegrationKeys); Json(command, "options", value.IntegrationOptions);
        await command.ExecuteNonQueryAsync(ct);
        return value;
    }

    public async Task<List<ShippingPackageRecord>> ListPackagesAsync(RequestContext context, CancellationToken ct)
    {
        await using var db = await dataSource.OpenConnectionAsync(ct);
        await using var command = new NpgsqlCommand("""
            SELECT id, code, shipping_width, shipping_height, shipping_length, shipping_weight,
                   shipping_max_weight, treshold, type, default_packaging
            FROM shipping.shipping_package_projection
            WHERE tenant_id = @tenant AND store_id = @store ORDER BY code
            """, db);
        P(command, "tenant", Scope(context.TenantId)); P(command, "store", Scope(context.StoreId));
        var result = new List<ShippingPackageRecord>();
        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct)) result.Add(ReadPackage(reader, context));
        return result;
    }

    public async Task<ShippingPackageRecord?> GetPackageAsync(string code, RequestContext context, CancellationToken ct)
    {
        await using var db = await dataSource.OpenConnectionAsync(ct);
        await using var command = new NpgsqlCommand("""
            SELECT id, code, shipping_width, shipping_height, shipping_length, shipping_weight,
                   shipping_max_weight, treshold, type, default_packaging
            FROM shipping.shipping_package_projection
            WHERE tenant_id = @tenant AND store_id = @store AND (code = @code OR id::text = @code)
            """, db);
        P(command, "tenant", Scope(context.TenantId)); P(command, "store", Scope(context.StoreId)); P(command, "code", code);
        await using var reader = await command.ExecuteReaderAsync(ct);
        return await reader.ReadAsync(ct) ? ReadPackage(reader, context) : null;
    }

    public async Task<ShippingPackageRecord> SavePackageAsync(ShippingPackageRecord value, RequestContext context,
        CancellationToken ct)
    {
        await using var db = await dataSource.OpenConnectionAsync(ct);
        await using var command = new NpgsqlCommand("""
            INSERT INTO shipping.shipping_package_projection
              (id, tenant_id, store_id, code, shipping_width, shipping_height, shipping_length,
               shipping_weight, shipping_max_weight, treshold, type, default_packaging)
            VALUES (@id, @tenant, @store, @code, @width, @height, @length, @weight, @maxWeight,
                    @treshold, @type, @defaultPackaging)
            ON CONFLICT (tenant_id, store_id, code) DO UPDATE SET
              shipping_width = EXCLUDED.shipping_width, shipping_height = EXCLUDED.shipping_height,
              shipping_length = EXCLUDED.shipping_length, shipping_weight = EXCLUDED.shipping_weight,
              shipping_max_weight = EXCLUDED.shipping_max_weight, treshold = EXCLUDED.treshold,
              type = EXCLUDED.type, default_packaging = EXCLUDED.default_packaging
            """, db);
        P(command, "id", value.Id); P(command, "tenant", Scope(context.TenantId)); P(command, "store", Scope(context.StoreId));
        P(command, "code", value.Code); P(command, "width", value.ShippingWidth); P(command, "height", value.ShippingHeight);
        P(command, "length", value.ShippingLength); P(command, "weight", value.ShippingWeight);
        P(command, "maxWeight", value.ShippingMaxWeight); P(command, "treshold", value.Treshold);
        P(command, "type", value.Type); P(command, "defaultPackaging", value.DefaultPackaging);
        await command.ExecuteNonQueryAsync(ct);
        return value;
    }

    public async Task<bool> DeletePackageAsync(string code, RequestContext context, CancellationToken ct)
    {
        await using var db = await dataSource.OpenConnectionAsync(ct);
        await using var command = new NpgsqlCommand("""
            DELETE FROM shipping.shipping_package_projection
            WHERE tenant_id = @tenant AND store_id = @store AND (code = @code OR id::text = @code)
            """, db);
        P(command, "tenant", Scope(context.TenantId)); P(command, "store", Scope(context.StoreId)); P(command, "code", code);
        return await command.ExecuteNonQueryAsync(ct) > 0;
    }

    public async Task<ExpeditionConfigurationRecord?> GetExpeditionAsync(RequestContext context, CancellationToken ct)
    {
        await using var db = await dataSource.OpenConnectionAsync(ct);
        await using var command = new NpgsqlCommand("""
            SELECT international_shipping, tax_on_shipping, ship_to_country, updated_at
            FROM shipping.shipping_expedition_projection
            WHERE tenant_id = @tenant AND store_id = @store
            """, db);
        P(command, "tenant", Scope(context.TenantId)); P(command, "store", Scope(context.StoreId));
        await using var reader = await command.ExecuteReaderAsync(ct);
        if (!await reader.ReadAsync(ct)) return null;
        return new ExpeditionConfigurationRecord
        {
            InternationalShipping = reader.GetBoolean(0),
            TaxOnShipping = reader.GetBoolean(1),
            ShipToCountry = ReadStringList(reader.GetFieldValue<JsonDocument>(2)),
            UpdatedAt = reader.GetFieldValue<DateTimeOffset>(3)
        };
    }

    public async Task<ExpeditionConfigurationRecord> SaveExpeditionAsync(ExpeditionConfigurationRecord value,
        RequestContext context, CancellationToken ct)
    {
        await using var db = await dataSource.OpenConnectionAsync(ct);
        await using var command = new NpgsqlCommand("""
            INSERT INTO shipping.shipping_expedition_projection
              (tenant_id, store_id, international_shipping, tax_on_shipping, ship_to_country)
            VALUES (@tenant, @store, @international, @tax, @countries)
            ON CONFLICT (tenant_id, store_id) DO UPDATE SET
              international_shipping = EXCLUDED.international_shipping,
              tax_on_shipping = EXCLUDED.tax_on_shipping, ship_to_country = EXCLUDED.ship_to_country,
              updated_at = CURRENT_TIMESTAMP
            RETURNING updated_at
            """, db);
        P(command, "tenant", Scope(context.TenantId)); P(command, "store", Scope(context.StoreId));
        P(command, "international", value.InternationalShipping); P(command, "tax", value.TaxOnShipping);
        Json(command, "countries", value.ShipToCountry);
        value.UpdatedAt = await command.ExecuteScalarAsync(ct) is DateTimeOffset timestamp
            ? timestamp : DateTimeOffset.UtcNow;
        return value;
    }

    public async Task PersistQuotesAsync(IReadOnlyList<ShippingQuoteRecord> quotes, RequestContext context,
        string? ipAddress, bool emitAdapterRequest, CancellationToken ct)
    {
        if (quotes.Count == 0) return;
        await using var db = await dataSource.OpenConnectionAsync(ct);
        await using var tx = await db.BeginTransactionAsync(ct);
        foreach (var quote in quotes)
        {
            await using var command = new NpgsqlCommand("""
                INSERT INTO shipping.shipping_quote
                  (id, tenant_id, store_id, cart_id, provider_code, option_code, option_name,
                   option_delivery_at, option_shipping_at, estimated_number_of_days, price, handling,
                   free_shipping, ip_address, delivery_first_name, delivery_last_name, delivery_company,
                   delivery_address, delivery_city, delivery_postal_code, delivery_state,
                   delivery_country_code, delivery_zone_code, delivery_latitude, delivery_longitude,
                   calculation_audit)
                VALUES (@id, @tenant, @store, @cart, @provider, @optionCode, @optionName, @deliveryAt,
                   @shippingAt, @days, @price, @handling, @free, @ip, @first, @last, @company, @address,
                   @city, @postal, @state, @country, @zone, @latitude, @longitude, @audit)
                """, db, tx);
            var delivery = quote.Delivery;
            P(command, "id", quote.Id); P(command, "tenant", Scope(context.TenantId)); P(command, "store", Scope(context.StoreId));
            P(command, "cart", quote.CartId); P(command, "provider", quote.ProviderCode);
            P(command, "optionCode", quote.Option.OptionCode); P(command, "optionName", quote.Option.OptionName);
            P(command, "deliveryAt", ParseTimestamp(quote.Option.OptionDeliveryDate)); P(command, "shippingAt", ParseTimestamp(quote.Option.OptionShippingDate));
            P(command, "days", quote.Option.EstimatedNumberOfDays); P(command, "price", quote.Option.OptionPrice);
            P(command, "handling", quote.Handling); P(command, "free", quote.FreeShipping); Inet(command, "ip", ipAddress);
            P(command, "first", delivery.FirstName); P(command, "last", delivery.LastName); P(command, "company", delivery.Company);
            P(command, "address", delivery.Address); P(command, "city", delivery.City); P(command, "postal", delivery.PostalCode);
            P(command, "state", delivery.State); P(command, "country", delivery.CountryCode); P(command, "zone", delivery.ZoneCode);
            P(command, "latitude", delivery.Latitude); P(command, "longitude", delivery.Longitude);
            Json(command, "audit", new { quote.DistanceKm, quote.AppliedRate, quote.ProviderCode, quote.QuotedAt });
            await command.ExecuteNonQueryAsync(ct);
            if (!emitAdapterRequest) continue;
            await using var eventCommand = new NpgsqlCommand("""
                INSERT INTO shipping.event_outbox
                  (id, event_type, tenant_id, store_id, correlation_id, payload, occurred_at)
                VALUES (@id, 'ShippingAdapterExecutionRequested.v1', @tenant, @store, @correlation, @payload, @at)
                """, db, tx);
            P(eventCommand, "id", quote.Id); P(eventCommand, "tenant", Scope(context.TenantId));
            P(eventCommand, "store", Scope(context.StoreId)); P(eventCommand, "correlation", context.CorrelationId);
            Json(eventCommand, "payload", new
            {
                eventId = quote.Id,
                eventType = "ShippingAdapterExecutionRequested.v1",
                eventVersion = 1,
                occurredAt = quote.QuotedAt,
                tenantId = context.TenantId,
                storeId = context.StoreId,
                correlationId = context.CorrelationId,
                requestType = "CarrierQuote",
                providerCode = quote.ProviderCode
            });
            P(eventCommand, "at", quote.QuotedAt);
            await eventCommand.ExecuteNonQueryAsync(ct);
        }
        await tx.CommitAsync(ct);
    }

    public async Task MarkEventPublishedAsync(Guid eventId, CancellationToken ct)
    {
        await using var db = await dataSource.OpenConnectionAsync(ct);
        await using var command = new NpgsqlCommand("""
            UPDATE shipping.event_outbox SET published_at = CURRENT_TIMESTAMP
            WHERE id = @id AND published_at IS NULL
            """, db);
        P(command, "id", eventId);
        await command.ExecuteNonQueryAsync(ct);
    }

    private static ShippingOriginRecord ReadOrigin(NpgsqlDataReader r) => new()
    {
        Id = r.GetGuid(0),
        TenantId = r.GetGuid(1).ToString(),
        StoreId = r.GetGuid(2).ToString(),
        Address = r.GetString(3),
        City = r.GetString(4),
        PostalCode = r.GetString(5),
        State = r.IsDBNull(6) ? null : r.GetString(6),
        CountryCode = r.IsDBNull(7) ? "" : r.GetString(7),
        ZoneCode = r.IsDBNull(8) ? null : r.GetString(8),
        Active = r.GetBoolean(9),
        CreatedAt = r.GetFieldValue<DateTimeOffset>(10),
        UpdatedAt = r.GetFieldValue<DateTimeOffset>(11)
    };

    private static ShippingConfigurationRecord ReadConfiguration(NpgsqlDataReader r) => new()
    {
        ShippingType = r.GetString(0),
        ShippingBasisType = r.GetString(1),
        ShippingOptionPriceType = r.GetString(2),
        ShippingPackageType = r.GetString(3),
        ShippingDescription = r.IsDBNull(4) ? null : r.GetString(4),
        FreeShippingType = r.IsDBNull(5) ? null : r.GetString(5),
        BoxWidth = r.IsDBNull(6) ? null : r.GetInt32(6),
        BoxHeight = r.IsDBNull(7) ? null : r.GetInt32(7),
        BoxLength = r.IsDBNull(8) ? null : r.GetInt32(8),
        BoxWeight = r.IsDBNull(9) ? null : r.GetDecimal(9),
        MaxWeight = r.IsDBNull(10) ? null : r.GetDecimal(10),
        FreeShippingEnabled = r.GetBoolean(11),
        OrderTotalFreeShipping = r.IsDBNull(12) ? null : r.GetDecimal(12),
        HandlingFees = r.IsDBNull(13) ? null : r.GetDecimal(13),
        TaxOnShipping = r.GetBoolean(14)
    };

    private static ShippingModuleRecord ReadModule(NpgsqlDataReader r) => new()
    {
        ModuleCode = r.GetString(0),
        Active = r.GetBoolean(1),
        DefaultSelected = r.GetBoolean(2),
        Environment = r.GetString(3),
        IntegrationKeys = ReadStringDictionary(r.GetFieldValue<JsonDocument>(4)),
        IntegrationOptions = ReadObjectDictionary(r.GetFieldValue<JsonDocument>(5))
    };

    private static ShippingPackageRecord ReadPackage(NpgsqlDataReader r, RequestContext context) => new()
    {
        Id = r.GetGuid(0),
        TenantId = context.TenantId,
        StoreId = context.StoreId,
        Code = r.GetString(1),
        ShippingWidth = r.GetDecimal(2),
        ShippingHeight = r.GetDecimal(3),
        ShippingLength = r.GetDecimal(4),
        ShippingWeight = r.GetDecimal(5),
        ShippingMaxWeight = r.GetDecimal(6),
        Treshold = r.IsDBNull(7) ? null : r.GetInt32(7),
        Type = r.GetString(8),
        DefaultPackaging = r.IsDBNull(9) ? null : r.GetBoolean(9)
    };

    private static Dictionary<string, string?> ReadStringDictionary(JsonDocument document) =>
        document.RootElement.EnumerateObject().ToDictionary(x => x.Name,
            x => x.Value.ValueKind == JsonValueKind.Null ? null : x.Value.ToString(),
            StringComparer.OrdinalIgnoreCase);

    private static Dictionary<string, object?> ReadObjectDictionary(JsonDocument document) =>
        document.RootElement.EnumerateObject().ToDictionary(x => x.Name, x => (object?)x.Value.Clone(),
            StringComparer.OrdinalIgnoreCase);

    private static List<string> ReadStringList(JsonDocument document) =>
        document.RootElement.ValueKind == JsonValueKind.Array
            ? document.RootElement.EnumerateArray().Select(x => x.GetString() ?? "").ToList()
            : [];
}
