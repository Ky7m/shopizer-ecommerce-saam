using System.Text.Json;
using Npgsql;
using NpgsqlTypes;
using Shopizer.ContentConfiguration.Models;

namespace Shopizer.ContentConfiguration.Data;

public sealed class ContentRepository(NpgsqlDataSource dataSource)
{
    private static void P(NpgsqlCommand c, string name, object? value)
    {
        if (name is "tenant" or "store" && value is string text && Guid.TryParse(text, out var id))
        {
            c.Parameters.Add(name, NpgsqlDbType.Uuid).Value = id;
            return;
        }

        if (value is null)
        {
            c.Parameters.Add(name, NpgsqlDbType.Text).Value = DBNull.Value;
            return;
        }

        c.Parameters.AddWithValue(name, value);
    }
    private static void J(NpgsqlCommand c, string name, object? value) =>
        c.Parameters.Add(name, NpgsqlDbType.Jsonb).Value = value is null ? DBNull.Value : JsonSerializer.Serialize(value);

    public async Task<(List<ContentRecord> Items, long Total)> ListContentAsync(
        RequestContext ctx, string type, string? language, int page, int count, CancellationToken ct)
    {
        if (page < 0 || count < 1 || count > 500)
            throw new DomainException("INVALID_REQUEST", "page must be non-negative and count must be between 1 and 500", 400);
        await using var db = await dataSource.OpenConnectionAsync(ct);
        const string where = "c.tenant_id=@tenant AND c.store_id=@store AND c.content_type=@type";
        await using var countCommand = new NpgsqlCommand($"SELECT count(*) FROM content_configuration.content c WHERE {where}", db);
        P(countCommand, "tenant", ctx.TenantId); P(countCommand, "store", ctx.StoreId); P(countCommand, "type", type);
        var total = (long)(await countCommand.ExecuteScalarAsync(ct) ?? 0L);
        await using var command = new NpgsqlCommand($"""
            SELECT c.content_id,c.tenant_id,c.store_id,c.code,c.content_type,c.content_position,c.link_to_menu,
                   c.product_group,c.sort_order,c.visible,c.created_at,c.updated_at,c.modified_by
            FROM content_configuration.content c WHERE {where}
            ORDER BY c.sort_order,c.content_id OFFSET @offset LIMIT @limit
            """, db);
        P(command, "tenant", ctx.TenantId); P(command, "store", ctx.StoreId); P(command, "type", type);
        P(command, "offset", page * count); P(command, "limit", count);
        var items = new List<ContentRecord>();
        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct)) items.Add(ReadContent(reader));
        await reader.CloseAsync();
        foreach (var item in items)
            await LoadDescriptionsAsync(db, item, language, ct);
        return (items, total);
    }

    public async Task<ContentRecord?> FindContentAsync(Guid id, RequestContext ctx, string? type, string? language, CancellationToken ct)
    {
        await using var db = await dataSource.OpenConnectionAsync(ct);
        await using var command = new NpgsqlCommand("""
            SELECT content_id,tenant_id,store_id,code,content_type,content_position,link_to_menu,
                   product_group,sort_order,visible,created_at,updated_at,modified_by
            FROM content_configuration.content
            WHERE content_id=@id AND tenant_id=@tenant AND store_id=@store
              AND (@type IS NULL OR content_type=@type)
            """, db);
        P(command, "id", id); P(command, "tenant", ctx.TenantId); P(command, "store", ctx.StoreId); P(command, "type", type);
        await using var reader = await command.ExecuteReaderAsync(ct);
        if (!await reader.ReadAsync(ct)) return null;
        var result = ReadContent(reader); await reader.CloseAsync();
        await LoadDescriptionsAsync(db, result, language, ct);
        return result;
    }

    public async Task<ContentRecord?> FindContentByCodeAsync(string code, RequestContext ctx, string type, string? language, bool visibleOnly, CancellationToken ct)
    {
        await using var db = await dataSource.OpenConnectionAsync(ct);
        await using var command = new NpgsqlCommand("""
            SELECT c.content_id,c.tenant_id,c.store_id,c.code,c.content_type,c.content_position,c.link_to_menu,
                   c.product_group,c.sort_order,c.visible,c.created_at,c.updated_at,c.modified_by
            FROM content_configuration.content c
            LEFT JOIN content_configuration.content_description d ON d.content_id=c.content_id
            WHERE c.tenant_id=@tenant AND c.store_id=@store AND c.content_type=@type AND c.code=@code
              AND (@visible=false OR c.visible=true)
              AND (@language IS NULL OR d.language_code=@language)
            LIMIT 1
            """, db);
        P(command, "tenant", ctx.TenantId); P(command, "store", ctx.StoreId); P(command, "type", type);
        P(command, "code", code); P(command, "language", language); P(command, "visible", visibleOnly);
        await using var reader = await command.ExecuteReaderAsync(ct);
        if (!await reader.ReadAsync(ct)) return null;
        var result = ReadContent(reader); await reader.CloseAsync();
        await LoadDescriptionsAsync(db, result, language, ct);
        return result;
    }

    public async Task<ContentRecord?> FindDescriptionByFriendlyUrlAsync(string url, RequestContext ctx, string language, CancellationToken ct)
    {
        await using var db = await dataSource.OpenConnectionAsync(ct);
        await using var command = new NpgsqlCommand("""
            SELECT c.content_id,c.tenant_id,c.store_id,c.code,c.content_type,c.content_position,c.link_to_menu,
                   c.product_group,c.sort_order,c.visible,c.created_at,c.updated_at,c.modified_by,
                   d.description_id,d.language_code,d.name,d.title,d.description,d.friendly_url,
                   d.meta_keywords,d.meta_title,d.meta_description
            FROM content_configuration.content c
            JOIN content_configuration.content_description d ON d.content_id=c.content_id
            WHERE c.tenant_id=@tenant AND c.store_id=@store AND c.content_type='PAGE'
              AND c.visible=true AND d.language_code=@language AND d.friendly_url=@url
            LIMIT 1
            """, db);
        P(command, "tenant", ctx.TenantId); P(command, "store", ctx.StoreId); P(command, "language", language); P(command, "url", url);
        await using var reader = await command.ExecuteReaderAsync(ct);
        if (!await reader.ReadAsync(ct)) return null;
        var content = ReadContent(reader);
        content.Descriptions.Add(ReadDescription(reader, 13));
        return content;
    }

    public async Task<Guid> SaveContentAsync(ContentRecord item, IEnumerable<ContentDescriptionInput> descriptions, RequestContext ctx, CancellationToken ct)
    {
        await using var db = await dataSource.OpenConnectionAsync(ct);
        await using var tx = await db.BeginTransactionAsync(ct);
        try
        {
            await using (var ownership = new NpgsqlCommand(
                "SELECT tenant_id,store_id FROM content_configuration.content WHERE content_id=@id", db, tx))
            {
                P(ownership, "id", item.Id);
                await using var owner = await ownership.ExecuteReaderAsync(ct);
                if (await owner.ReadAsync(ct) &&
                    (!owner.GetGuid(0).ToString().Equals(ctx.TenantId, StringComparison.Ordinal) ||
                     !owner.GetGuid(1).ToString().Equals(ctx.StoreId, StringComparison.Ordinal)))
                    throw new DomainException("CONTENT_NOT_FOUND", "Content was not found for this store.", 404);
            }
            await using (var command = new NpgsqlCommand("""
                INSERT INTO content_configuration.content(content_id,tenant_id,store_id,code,content_type,content_position,
                  link_to_menu,product_group,sort_order,visible,modified_by)
                VALUES(@id,@tenant,@store,@code,@type,@position,@menu,@group,@sort,@visible,@by)
                ON CONFLICT (content_id) DO UPDATE SET code=EXCLUDED.code,content_type=EXCLUDED.content_type,
                  content_position=EXCLUDED.content_position,link_to_menu=EXCLUDED.link_to_menu,
                  product_group=EXCLUDED.product_group,sort_order=EXCLUDED.sort_order,visible=EXCLUDED.visible,
                  updated_at=now(),modified_by=EXCLUDED.modified_by
                """, db, tx))
            {
                P(command, "id", item.Id); P(command, "tenant", ctx.TenantId); P(command, "store", ctx.StoreId);
                P(command, "code", item.Code); P(command, "type", item.ContentType); P(command, "position", item.ContentPosition);
                P(command, "menu", item.LinkToMenu); P(command, "group", item.ProductGroup); P(command, "sort", item.SortOrder);
                P(command, "visible", item.Visible); P(command, "by", "administrator");
                await command.ExecuteNonQueryAsync(ct);
            }
            await using (var remove = new NpgsqlCommand("DELETE FROM content_configuration.content_description WHERE content_id=@id", db, tx))
            { P(remove, "id", item.Id); await remove.ExecuteNonQueryAsync(ct); }
            foreach (var description in descriptions)
            {
                if (string.IsNullOrWhiteSpace(description.Language) || string.IsNullOrWhiteSpace(description.Name))
                    throw new DomainException("LANGUAGE_NOT_FOUND", "language and name are required", 422);
                await using var command = new NpgsqlCommand("""
                    INSERT INTO content_configuration.content_description(description_id,content_id,language_code,name,title,description,
                      friendly_url,meta_keywords,meta_title,meta_description)
                    VALUES(@did,@id,@language,@name,@title,@description,@url,@keywords,@metatitle,@metadescription)
                    """, db, tx);
                P(command, "did", Guid.NewGuid()); P(command, "id", item.Id); P(command, "language", description.Language);
                P(command, "name", description.Name); P(command, "title", description.Title); P(command, "description", description.Description);
                P(command, "url", description.FriendlyUrl); P(command, "keywords", description.MetaKeywords);
                P(command, "metatitle", description.MetaTitle); P(command, "metadescription", description.MetaDescription);
                await command.ExecuteNonQueryAsync(ct);
            }
            await tx.CommitAsync(ct);
            return item.Id;
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            await tx.RollbackAsync(ct);
            throw new DomainException("CONTENT_CODE_CONFLICT", $"Content code [{item.Code}] already exists for this store.", 409);
        }
    }

    public async Task DeleteContentAsync(Guid id, RequestContext ctx, string type, CancellationToken ct)
    {
        await using var db = await dataSource.OpenConnectionAsync(ct);
        await using var command = new NpgsqlCommand("DELETE FROM content_configuration.content WHERE content_id=@id AND tenant_id=@tenant AND store_id=@store AND content_type=@type", db);
        P(command, "id", id); P(command, "tenant", ctx.TenantId); P(command, "store", ctx.StoreId); P(command, "type", type);
        if (await command.ExecuteNonQueryAsync(ct) == 0)
            throw new DomainException("CONTENT_NOT_FOUND", "Content was not found for this store.", 404);
    }

    public async Task<bool> ContentCodeExistsAsync(string code, RequestContext ctx, CancellationToken ct)
    {
        await using var db = await dataSource.OpenConnectionAsync(ct);
        await using var command = new NpgsqlCommand("SELECT EXISTS(SELECT 1 FROM content_configuration.content WHERE tenant_id=@tenant AND store_id=@store AND code=@code)", db);
        P(command, "tenant", ctx.TenantId); P(command, "store", ctx.StoreId); P(command, "code", code);
        return (bool)(await command.ExecuteScalarAsync(ct) ?? false);
    }

    private static async Task LoadDescriptionsAsync(NpgsqlConnection db, ContentRecord item, string? language, CancellationToken ct)
    {
        await using var command = new NpgsqlCommand("""
            SELECT description_id,language_code,name,title,description,friendly_url,meta_keywords,meta_title,meta_description
            FROM content_configuration.content_description WHERE content_id=@id
              AND (@language IS NULL OR language_code=@language) ORDER BY language_code
            """, db);
        P(command, "id", item.Id); P(command, "language", language);
        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct)) item.Descriptions.Add(ReadDescription(reader, 0));
    }

    private static ContentRecord ReadContent(NpgsqlDataReader r) => new()
    {
        Id = r.GetGuid(0),
        TenantId = r.GetGuid(1).ToString(),
        StoreId = r.GetGuid(2).ToString(),
        Code = r.GetString(3),
        ContentType = r.GetString(4),
        ContentPosition = r.IsDBNull(5) ? null : r.GetString(5),
        LinkToMenu = r.GetBoolean(6),
        ProductGroup = r.IsDBNull(7) ? null : r.GetString(7),
        SortOrder = r.GetInt32(8),
        Visible = r.GetBoolean(9),
        CreatedAt = r.GetFieldValue<DateTimeOffset>(10),
        UpdatedAt = r.GetFieldValue<DateTimeOffset>(11),
        ModifiedBy = r.IsDBNull(12) ? null : r.GetString(12)
    };

    private static ContentDescription ReadDescription(NpgsqlDataReader r, int offset) => new(
        r.GetGuid(offset), r.GetString(offset + 1), r.GetString(offset + 2),
        r.IsDBNull(offset + 3) ? null : r.GetString(offset + 3),
        r.IsDBNull(offset + 4) ? null : r.GetString(offset + 4),
        r.IsDBNull(offset + 5) ? null : r.GetString(offset + 5),
        r.IsDBNull(offset + 6) ? null : r.GetString(offset + 6),
        r.IsDBNull(offset + 7) ? null : r.GetString(offset + 7),
        r.IsDBNull(offset + 8) ? null : r.GetString(offset + 8));

    public async Task<FileRecord?> FindFileAsync(string name, string type, string folder, RequestContext ctx, CancellationToken ct)
    {
        await using var db = await dataSource.OpenConnectionAsync(ct);
        await using var command = new NpgsqlCommand("""
            SELECT content_file_id,tenant_id,store_id,file_name,mime_type,file_content_type,folder_path,provider_name,provider_key,state
            FROM content_configuration.content_file
            WHERE tenant_id=@tenant AND store_id=@store AND file_name=@name AND file_content_type=@type
              AND folder_path=@folder AND state <> 'DELETED'
            """, db);
        P(command, "tenant", ctx.TenantId); P(command, "store", ctx.StoreId); P(command, "name", name);
        P(command, "type", type); P(command, "folder", folder);
        await using var reader = await command.ExecuteReaderAsync(ct);
        return await reader.ReadAsync(ct) ? ReadFile(reader) : null;
    }

    public async Task<(List<FileRecord> Items, long Total)> ListFilesAsync(RequestContext ctx, string type, string folder, int page, int count, CancellationToken ct)
    {
        await using var db = await dataSource.OpenConnectionAsync(ct);
        const string where = "tenant_id=@tenant AND store_id=@store AND file_content_type=@type AND folder_path=@folder AND state <> 'DELETED'";
        await using var totalCommand = new NpgsqlCommand($"SELECT count(*) FROM content_configuration.content_file WHERE {where}", db);
        P(totalCommand, "tenant", ctx.TenantId); P(totalCommand, "store", ctx.StoreId); P(totalCommand, "type", type); P(totalCommand, "folder", folder);
        var total = (long)(await totalCommand.ExecuteScalarAsync(ct) ?? 0L);
        await using var command = new NpgsqlCommand($"SELECT content_file_id,tenant_id,store_id,file_name,mime_type,file_content_type,folder_path,provider_name,provider_key,state FROM content_configuration.content_file WHERE {where} ORDER BY file_name OFFSET @offset LIMIT @limit", db);
        P(command, "tenant", ctx.TenantId); P(command, "store", ctx.StoreId); P(command, "type", type); P(command, "folder", folder);
        P(command, "offset", page * count); P(command, "limit", count);
        var items = new List<FileRecord>(); await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct)) items.Add(ReadFile(reader));
        return (items, total);
    }

    public async Task UpsertFileAsync(FileRecord file, CancellationToken ct)
    {
        await using var db = await dataSource.OpenConnectionAsync(ct);
        await using var command = new NpgsqlCommand("""
            INSERT INTO content_configuration.content_file(content_file_id,tenant_id,store_id,file_name,mime_type,file_content_type,
              folder_path,provider_name,provider_key,state)
            VALUES(@id,@tenant,@store,@name,@mime,@type,@folder,@provider,@key,'AVAILABLE')
            ON CONFLICT (tenant_id,store_id,file_content_type,folder_path,file_name) WHERE state <> 'DELETED'
            DO UPDATE SET mime_type=EXCLUDED.mime_type,provider_name=EXCLUDED.provider_name,provider_key=EXCLUDED.provider_key,
              state='AVAILABLE',updated_at=now()
            """, db);
        P(command, "id", file.Id); P(command, "tenant", file.TenantId); P(command, "store", file.StoreId); P(command, "name", file.FileName);
        P(command, "mime", file.MimeType); P(command, "type", file.ContentType); P(command, "folder", file.FolderPath);
        P(command, "provider", file.Provider); P(command, "key", file.ProviderKey);
        await command.ExecuteNonQueryAsync(ct);
    }

    public async Task RenameFileAsync(FileRecord original, string newName, string newKey, CancellationToken ct)
    {
        await using var db = await dataSource.OpenConnectionAsync(ct);
        await using var tx = await db.BeginTransactionAsync(ct);
        await using (var remove = new NpgsqlCommand("UPDATE content_configuration.content_file SET state='DELETED',updated_at=now() WHERE content_file_id=@id", db, tx))
        { P(remove, "id", original.Id); await remove.ExecuteNonQueryAsync(ct); }
        await using var add = new NpgsqlCommand("""
            INSERT INTO content_configuration.content_file(content_file_id,tenant_id,store_id,file_name,mime_type,file_content_type,
              folder_path,provider_name,provider_key,state) VALUES(@id,@tenant,@store,@name,@mime,@type,@folder,@provider,@key,'AVAILABLE')
            """, db, tx);
        P(add, "id", Guid.NewGuid()); P(add, "tenant", original.TenantId); P(add, "store", original.StoreId);
        P(add, "name", newName); P(add, "mime", original.MimeType); P(add, "type", original.ContentType); P(add, "folder", original.FolderPath);
        P(add, "provider", original.Provider); P(add, "key", newKey); await add.ExecuteNonQueryAsync(ct);
        await tx.CommitAsync(ct);
    }

    public async Task DeleteFileAsync(FileRecord? file, RequestContext ctx, CancellationToken ct)
    {
        if (file is null) return;
        await using var db = await dataSource.OpenConnectionAsync(ct);
        await using var command = new NpgsqlCommand("UPDATE content_configuration.content_file SET state='DELETED',updated_at=now() WHERE content_file_id=@id AND tenant_id=@tenant AND store_id=@store", db);
        P(command, "id", file.Id); P(command, "tenant", ctx.TenantId); P(command, "store", ctx.StoreId); await command.ExecuteNonQueryAsync(ct);
    }

    public async Task<Dictionary<string, object?>?> GetConfigurationAsync(string key, RequestContext ctx, CancellationToken ct)
    {
        await using var db = await dataSource.OpenConnectionAsync(ct);
        await using var command = new NpgsqlCommand("SELECT value FROM content_configuration.merchant_configuration WHERE tenant_id=@tenant AND store_id=@store AND config_key=@key", db);
        P(command, "tenant", ctx.TenantId); P(command, "store", ctx.StoreId); P(command, "key", key);
        var value = await command.ExecuteScalarAsync(ct);
        if (value is not string text || string.IsNullOrWhiteSpace(text)) return null;
        using var json = JsonDocument.Parse(text);
        return json.RootElement.ValueKind == JsonValueKind.Object ? json.RootElement.EnumerateObject().ToDictionary(x => x.Name, x => (object?)x.Value.Clone()) : null;
    }

    public async Task<(Guid Id, string Type, bool Active, string? Value)?> GetConfigurationRecordAsync(string key, RequestContext ctx, CancellationToken ct)
    {
        await using var db = await dataSource.OpenConnectionAsync(ct);
        await using var command = new NpgsqlCommand("SELECT merchant_configuration_id,configuration_type,active,value FROM content_configuration.merchant_configuration WHERE tenant_id=@tenant AND store_id=@store AND config_key=@key", db);
        P(command, "tenant", ctx.TenantId); P(command, "store", ctx.StoreId); P(command, "key", key);
        await using var reader = await command.ExecuteReaderAsync(ct);
        return await reader.ReadAsync(ct) ? (reader.GetGuid(0), reader.GetString(1), reader.GetBoolean(2), reader.IsDBNull(3) ? null : reader.GetString(3)) : null;
    }

    public async Task<string?> GetRawConfigurationAsync(string key, RequestContext ctx, CancellationToken ct)
    {
        var record = await GetConfigurationRecordAsync(key, ctx, ct);
        return record?.Value;
    }

    public async Task SaveConfigurationAsync(string key, string type, bool active, string value, RequestContext ctx, CancellationToken ct)
    {
        await using var db = await dataSource.OpenConnectionAsync(ct);
        await using var command = new NpgsqlCommand("""
            INSERT INTO content_configuration.merchant_configuration(tenant_id,store_id,config_key,configuration_type,active,value,modified_by)
            VALUES(@tenant,@store,@key,@type,@active,@value,'administrator')
            ON CONFLICT(tenant_id,store_id,config_key) DO UPDATE SET configuration_type=EXCLUDED.configuration_type,
              active=EXCLUDED.active,value=EXCLUDED.value,updated_at=now(),modified_by=EXCLUDED.modified_by
            """, db);
        P(command, "tenant", ctx.TenantId); P(command, "store", ctx.StoreId); P(command, "key", key); P(command, "type", type);
        P(command, "active", active); P(command, "value", value); await command.ExecuteNonQueryAsync(ct);
    }

    public async Task<List<ModuleRecord>> ListModulesAsync(string family, CancellationToken ct)
    {
        await using var db = await dataSource.OpenConnectionAsync(ct);
        await using var command = new NpgsqlCommand("SELECT module_configuration_id,module_family,code,module_type,image,custom_module,regions,configuration,details FROM content_configuration.module_configuration WHERE module_family=@family ORDER BY code", db);
        P(command, "family", family); await using var reader = await command.ExecuteReaderAsync(ct);
        var result = new List<ModuleRecord>();
        while (await reader.ReadAsync(ct))
        {
            result.Add(new ModuleRecord
            {
                Id = reader.GetGuid(0),
                Family = reader.GetString(1),
                Code = reader.GetString(2),
                Type = reader.IsDBNull(3) ? null : reader.GetString(3),
                Image = reader.IsDBNull(4) ? null : reader.GetString(4),
                CustomModule = reader.GetBoolean(5),
                Regions = ReadStringList(reader, 6),
                Configuration = ReadEnvironments(reader, 7),
                Details = ReadObject(reader, 8)
            });
        }
        return result;
    }

    public async Task<ModuleRecord?> FindModuleAsync(string family, string code, CancellationToken ct) =>
        (await ListModulesAsync(family, ct)).FirstOrDefault(x => x.Code.Equals(code, StringComparison.OrdinalIgnoreCase));

    public async Task ReplaceModuleAsync(ModuleRecord module, CancellationToken ct)
    {
        await using var db = await dataSource.OpenConnectionAsync(ct);
        await using var tx = await db.BeginTransactionAsync(ct);
        await using (var remove = new NpgsqlCommand("DELETE FROM content_configuration.module_configuration WHERE code=@code", db, tx))
        { P(remove, "code", module.Code); await remove.ExecuteNonQueryAsync(ct); }
        await using var add = new NpgsqlCommand("""
            INSERT INTO content_configuration.module_configuration(module_configuration_id,module_family,code,module_type,image,custom_module,regions,configuration,details)
            VALUES(@id,@family,@code,@type,@image,@custom,@regions,@configuration,@details)
            """, db, tx);
        P(add, "id", module.Id); P(add, "family", module.Family); P(add, "code", module.Code); P(add, "type", module.Type);
        P(add, "image", module.Image); P(add, "custom", module.CustomModule); J(add, "regions", module.Regions);
        J(add, "configuration", module.Configuration); J(add, "details", module.Details); await add.ExecuteNonQueryAsync(ct);
        await tx.CommitAsync(ct);
    }

    public async Task WriteOutboxAsync(Guid id, string type, RequestContext ctx, object payload, CancellationToken ct)
    {
        await using var db = await dataSource.OpenConnectionAsync(ct);
        await using var command = new NpgsqlCommand("""
            INSERT INTO content_configuration.event_outbox(id,event_type,tenant_id,store_id,correlation_id,payload,occurred_at)
            VALUES(@id,@type,@tenant,@store,@correlation,@payload,@occurred)
            ON CONFLICT(id) DO NOTHING
            """, db);
        P(command, "id", id); P(command, "type", type); P(command, "tenant", ctx.TenantId); P(command, "store", ctx.StoreId);
        P(command, "correlation", ctx.CorrelationId); J(command, "payload", payload); P(command, "occurred", DateTimeOffset.UtcNow);
        await command.ExecuteNonQueryAsync(ct);
    }

    public async Task MarkEventPublishedAsync(Guid id, CancellationToken ct)
    {
        await using var db = await dataSource.OpenConnectionAsync(ct);
        await using var command = new NpgsqlCommand("UPDATE content_configuration.event_outbox SET published_at=now() WHERE id=@id", db);
        P(command, "id", id); await command.ExecuteNonQueryAsync(ct);
    }

    private static FileRecord ReadFile(NpgsqlDataReader r) => new()
    {
        Id = r.GetGuid(0),
        TenantId = r.GetGuid(1).ToString(),
        StoreId = r.GetGuid(2).ToString(),
        FileName = r.GetString(3),
        MimeType = r.IsDBNull(4) ? null : r.GetString(4),
        ContentType = r.GetString(5),
        FolderPath = r.GetString(6),
        Provider = r.GetString(7),
        ProviderKey = r.GetString(8),
        State = r.GetString(9)
    };
    private static List<string> ReadStringList(NpgsqlDataReader r, int ordinal) =>
        r.IsDBNull(ordinal) ? [] : JsonDocument.Parse(r.GetFieldValue<string>(ordinal)).RootElement.EnumerateArray().Select(x => x.GetString() ?? "").ToList();
    private static Dictionary<string, object?> ReadObject(NpgsqlDataReader r, int ordinal) =>
        r.IsDBNull(ordinal) ? [] : JsonDocument.Parse(r.GetFieldValue<string>(ordinal)).RootElement.EnumerateObject().ToDictionary(x => x.Name, x => (object?)x.Value.Clone());
    private static List<ModuleEnvironment> ReadEnvironments(NpgsqlDataReader r, int ordinal) =>
        r.IsDBNull(ordinal) ? [] : JsonDocument.Parse(r.GetFieldValue<string>(ordinal)).RootElement.EnumerateArray().Select(x => new ModuleEnvironment(
            JsonValue.String(x, "env") ?? "", JsonValue.String(x, "scheme"), JsonValue.String(x, "host"),
            JsonValue.String(x, "port"), JsonValue.String(x, "uri"), JsonValue.String(x, "config1"), JsonValue.String(x, "config2"))).ToList();
}

public sealed record ContentDescriptionInput(string Language, string Name, string? Title, string? Description,
    string? FriendlyUrl, string? MetaKeywords, string? MetaTitle, string? MetaDescription);
