using System.Collections.Concurrent;
using System.Data;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Npgsql;
using Shopizer.CustomerIdentity.DTOs;
using Shopizer.CustomerIdentity.Models;

namespace Shopizer.CustomerIdentity.Data;

public sealed class IdentityRepository
{
    private readonly string? _connectionString;
    private readonly ILogger<IdentityRepository> _logger;
    private readonly ConcurrentDictionary<Guid, CustomerAccount> _customers = new();
    private readonly ConcurrentDictionary<Guid, AdministratorAccount> _admins = new();
    private readonly ConcurrentDictionary<Guid, List<AddressRecord>> _addresses = new();
    private readonly ConcurrentDictionary<Guid, List<(string OptionId, string ValueId, string? Text)>> _attributes = new();
    private readonly ConcurrentDictionary<Guid, ReviewRecord> _reviews = new();
    private readonly ConcurrentDictionary<string, NewsletterRecord> _newsletters = new();
    private readonly ConcurrentDictionary<string, ExternalIdentityRecord> _external = new();
    private readonly ConcurrentDictionary<string, ResetRecord> _resets = new();
    private readonly object _memoryLock = new();

    public IdentityRepository(IConfiguration configuration, ILogger<IdentityRepository> logger)
    {
        _logger = logger;
        _connectionString = configuration.GetConnectionString("customeridentitydb") ??
            configuration["ConnectionStrings__customeridentitydb"] ?? configuration["DATABASE_URL"];
    }

    public bool UsesDatabase => !string.IsNullOrWhiteSpace(_connectionString);
    private NpgsqlConnection Connection() => new(_connectionString!);
    private static void P(NpgsqlCommand c, string name, object? value) =>
        c.Parameters.AddWithValue(name, value ?? DBNull.Value);

    public async Task<CustomerAccount?> FindCustomerAsync(Guid id, RequestContext ctx, CancellationToken ct)
    {
        if (!UsesDatabase) return _customers.TryGetValue(id, out var c) && c.TenantId == ctx.TenantId && c.StoreId == ctx.StoreId ? c : null;
        await using var db = Connection(); await db.OpenAsync(ct);
        await using var cmd = new NpgsqlCommand("""SELECT id,tenant_id,store_id,login_name,email_address,password_hash,gender,date_of_birth,company_name,provider,status::text,default_language_code,review_average,review_count,anonymous,last_password_reset_at FROM customer_identity.customer_accounts WHERE id=@id AND tenant_id=@tenant AND store_id=@store AND status <> 'Deleted'""", db);
        P(cmd, "id", id); P(cmd, "tenant", ctx.TenantId); P(cmd, "store", ctx.StoreId);
        await using var r = await cmd.ExecuteReaderAsync(ct);
        return await r.ReadAsync(ct) ? ReadCustomer(r) : null;
    }

    public async Task<CustomerAccount?> FindCustomerByLoginAsync(string login, RequestContext ctx, CancellationToken ct)
    {
        if (!UsesDatabase) return _customers.Values.FirstOrDefault(c => c.TenantId == ctx.TenantId && c.StoreId == ctx.StoreId && string.Equals(c.LoginName, login, StringComparison.OrdinalIgnoreCase) && c.Status != "Deleted");
        await using var db = Connection(); await db.OpenAsync(ct);
        await using var cmd = new NpgsqlCommand("""SELECT id,tenant_id,store_id,login_name,email_address,password_hash,gender,date_of_birth,company_name,provider,status::text,default_language_code,review_average,review_count,anonymous,last_password_reset_at FROM customer_identity.customer_accounts WHERE tenant_id=@tenant AND store_id=@store AND login_name=@login AND status <> 'Deleted'""", db);
        P(cmd, "tenant", ctx.TenantId); P(cmd, "store", ctx.StoreId); P(cmd, "login", login);
        await using var r = await cmd.ExecuteReaderAsync(ct); return await r.ReadAsync(ct) ? ReadCustomer(r) : null;
    }

    public async Task<(List<CustomerAccount> Items, long Total)> ListCustomersAsync(RequestContext ctx, int page, int pageSize, string? name, string? email, string? firstName, string? lastName, string? country, CancellationToken ct)
    {
        page = Math.Max(1, page); pageSize = Math.Clamp(pageSize, 1, 200);
        if (!UsesDatabase)
        {
            var values = _customers.Values.Where(c => c.TenantId == ctx.TenantId && c.StoreId == ctx.StoreId && c.Status != "Deleted").ToList();
            var address = _addresses.Values.SelectMany(x => x).ToList();
            values = values.Where(c =>
                (string.IsNullOrWhiteSpace(name) || c.LoginName.Contains(name, StringComparison.OrdinalIgnoreCase) || c.EmailAddress.Contains(name, StringComparison.OrdinalIgnoreCase)) &&
                (string.IsNullOrWhiteSpace(email) || c.EmailAddress.Contains(email, StringComparison.OrdinalIgnoreCase)) &&
                (string.IsNullOrWhiteSpace(firstName) || address.Any(a => a.CustomerId == c.Id && a.AddressType == "Billing" && a.FirstName.Contains(firstName, StringComparison.OrdinalIgnoreCase))) &&
                (string.IsNullOrWhiteSpace(lastName) || address.Any(a => a.CustomerId == c.Id && a.AddressType == "Billing" && a.LastName.Contains(lastName, StringComparison.OrdinalIgnoreCase))) &&
                (string.IsNullOrWhiteSpace(country) || address.Any(a => a.CustomerId == c.Id && a.AddressType == "Billing" && a.CountryCode.Equals(country, StringComparison.OrdinalIgnoreCase)))).OrderBy(c => c.EmailAddress).ToList();
            return (values.Skip((page - 1) * pageSize).Take(pageSize).ToList(), values.Count);
        }
        await using var db = Connection(); await db.OpenAsync(ct);
        const string where = """
          c.tenant_id=@tenant AND c.store_id=@store AND c.status <> 'Deleted'
          AND (@name IS NULL OR c.login_name ILIKE '%' || @name || '%' OR c.email_address ILIKE '%' || @name || '%')
          AND (@email IS NULL OR c.email_address ILIKE '%' || @email || '%')
          AND (@first IS NULL OR EXISTS (SELECT 1 FROM customer_identity.customer_addresses a WHERE a.customer_id=c.id AND a.address_type='Billing' AND a.first_name ILIKE '%' || @first || '%'))
          AND (@last IS NULL OR EXISTS (SELECT 1 FROM customer_identity.customer_addresses a WHERE a.customer_id=c.id AND a.address_type='Billing' AND a.last_name ILIKE '%' || @last || '%'))
          AND (@country IS NULL OR EXISTS (SELECT 1 FROM customer_identity.customer_addresses a WHERE a.customer_id=c.id AND a.address_type='Billing' AND a.country_code=@country))
          """;
        await using var count = new NpgsqlCommand($"SELECT COUNT(*) FROM customer_identity.customer_accounts c WHERE {where}", db);
        AddFilterParameters(count, ctx, name, email, firstName, lastName, country);
        var total = (long)(await count.ExecuteScalarAsync(ct) ?? 0L);
        await using var cmd = new NpgsqlCommand($"SELECT c.id,c.tenant_id,c.store_id,c.login_name,c.email_address,c.password_hash,c.gender,c.date_of_birth,c.company_name,c.provider,c.status::text,c.default_language_code,c.review_average,c.review_count,c.anonymous,c.last_password_reset_at FROM customer_identity.customer_accounts c WHERE {where} ORDER BY c.email_address OFFSET @offset LIMIT @limit", db);
        AddFilterParameters(cmd, ctx, name, email, firstName, lastName, country); P(cmd, "offset", (page - 1) * pageSize); P(cmd, "limit", pageSize);
        var items = new List<CustomerAccount>(); await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct)) items.Add(ReadCustomer(reader));
        return (items, total);
    }

    private static void AddFilterParameters(NpgsqlCommand c, RequestContext ctx, string? name, string? email, string? first, string? last, string? country)
    {
        P(c, "tenant", ctx.TenantId); P(c, "store", ctx.StoreId); P(c, "name", string.IsNullOrWhiteSpace(name) ? null : name);
        P(c, "email", string.IsNullOrWhiteSpace(email) ? null : email); P(c, "first", string.IsNullOrWhiteSpace(first) ? null : first);
        P(c, "last", string.IsNullOrWhiteSpace(last) ? null : last); P(c, "country", string.IsNullOrWhiteSpace(country) ? null : country);
    }

    public async Task<List<AddressRecord>> GetAddressesAsync(Guid customerId, CancellationToken ct)
    {
        if (!UsesDatabase) return _addresses.TryGetValue(customerId, out var list) ? list.Select(Clone).ToList() : [];
        await using var db = Connection(); await db.OpenAsync(ct);
        await using var cmd = new NpgsqlCommand("""SELECT id,customer_id,address_type::text,first_name,last_name,company_name,street_address,city,postal_code,state_province,telephone,country_code,zone_code,latitude,longitude FROM customer_identity.customer_addresses WHERE customer_id=@id ORDER BY address_type""", db);
        P(cmd, "id", customerId); await using var r = await cmd.ExecuteReaderAsync(ct); var result = new List<AddressRecord>();
        while (await r.ReadAsync(ct)) result.Add(ReadAddress(r)); return result;
    }

    public async Task<List<(string OptionId, string ValueId, string? Text)>> GetAttributesAsync(Guid customerId, CancellationToken ct)
    {
        if (!UsesDatabase) return _attributes.TryGetValue(customerId, out var a) ? a.ToList() : [];
        await using var db = Connection(); await db.OpenAsync(ct);
        await using var cmd = new NpgsqlCommand("SELECT option_id::text,option_value_id::text,text_value FROM customer_identity.customer_attributes WHERE customer_id=@id", db);
        P(cmd, "id", customerId); await using var r = await cmd.ExecuteReaderAsync(ct); var result = new List<(string, string, string?)>();
        while (await r.ReadAsync(ct)) result.Add((r.GetString(0), r.GetString(1), r.IsDBNull(2) ? null : r.GetString(2))); return result;
    }

    public async Task AddCustomerAsync(CustomerAccount c, AddressDto billing, AddressDto? delivery, IEnumerable<CustomerAttributeDto> attributes, RequestContext ctx, CancellationToken ct)
    {
        var effectiveDelivery = delivery ?? CopyAddress(billing, "Delivery");
        if (!UsesDatabase)
        {
            lock (_memoryLock)
            {
                if (_customers.Values.Any(x => x.TenantId == ctx.TenantId && x.StoreId == ctx.StoreId && (x.LoginName.Equals(c.LoginName, StringComparison.OrdinalIgnoreCase) || x.EmailAddress.Equals(c.EmailAddress, StringComparison.OrdinalIgnoreCase))))
                    throw new DomainException("CUSTOMER_IDENTITY_CONFLICT", "Login identifier is already registered for this store", 409);
                _customers[c.Id] = c; _addresses[c.Id] = [ToAddress(c.Id, billing, "Billing"), ToAddress(c.Id, effectiveDelivery, "Delivery")];
                _attributes[c.Id] = attributes.Select(a => (a.OptionId, a.OptionValueId, a.TextValue)).ToList();
            } return;
        }
        await using var db = Connection(); await db.OpenAsync(ct); await using var tx = await db.BeginTransactionAsync(ct);
        try
        {
            await using (var cmd = new NpgsqlCommand("""INSERT INTO customer_identity.customer_accounts(id,tenant_id,store_id,login_name,email_address,password_hash,gender,date_of_birth,company_name,provider,status,default_language_code,review_average,review_count,anonymous,created_by,correlation_id) VALUES(@id,@tenant,@store,@login,@email,@hash,@gender,@dob,@company,@provider,@status::customer_identity.customer_status,@language,0,0,@anonymous,@created,@correlation)""", db, tx))
            {
                P(cmd, "id", c.Id); P(cmd, "tenant", c.TenantId); P(cmd, "store", c.StoreId); P(cmd, "login", c.LoginName); P(cmd, "email", c.EmailAddress); P(cmd, "hash", c.PasswordHash); P(cmd, "gender", c.Gender); P(cmd, "dob", c.DateOfBirth); P(cmd, "company", c.CompanyName); P(cmd, "provider", c.Provider); P(cmd, "status", c.Status); P(cmd, "language", c.DefaultLanguageCode); P(cmd, "anonymous", c.Anonymous); P(cmd, "created", c.LoginName); P(cmd, "correlation", Guid.TryParse(ctx.CorrelationId, out var correlation) ? correlation : null); await cmd.ExecuteNonQueryAsync(ct);
            }
            await InsertAddressAsync(db, tx, c.Id, billing, "Billing", ct); await InsertAddressAsync(db, tx, c.Id, effectiveDelivery, "Delivery", ct);
            foreach (var attr in attributes) await InsertAttributeAsync(db, tx, c, attr, ct);
            var eventId = c.Id; var occurredAt = DateTimeOffset.UtcNow;
            await using var eventCmd = new NpgsqlCommand("INSERT INTO customer_identity.event_outbox(id,event_type,tenant_id,store_id,correlation_id,payload,occurred_at) VALUES(@id,'CustomerRegistered',@tenant,@store,@correlation,@payload,@at)", db, tx);
            P(eventCmd, "id", eventId); P(eventCmd, "tenant", ctx.TenantId); P(eventCmd, "store", ctx.StoreId); P(eventCmd, "correlation", ctx.CorrelationId); P(eventCmd, "payload", JsonSerializer.Serialize(new { eventId, eventType = "CustomerRegistered", eventVersion = 1, occurredAt, tenantId = ctx.TenantId, storeId = ctx.StoreId, correlationId = ctx.CorrelationId, customerId = c.Id, loginName = c.LoginName, emailAddress = c.EmailAddress, status = c.Status })); P(eventCmd, "at", occurredAt); await eventCmd.ExecuteNonQueryAsync(ct);
            await tx.CommitAsync(ct);
        }
        catch (PostgresException ex) when (ex.SqlState == "23505") { await tx.RollbackAsync(ct); throw new DomainException("CUSTOMER_IDENTITY_CONFLICT", "Login identifier is already registered for this store", 409); }
    }

    public async Task UpdateCustomerAsync(CustomerAccount c, RequestContext ctx, CancellationToken ct)
    {
        if (!UsesDatabase) { _customers[c.Id] = c; return; }
        await using var db = Connection(); await db.OpenAsync(ct);
        await using var cmd = new NpgsqlCommand("UPDATE customer_identity.customer_accounts SET email_address=@email,gender=@gender,company_name=@company,provider=@provider,default_language_code=@language,updated_at=now() WHERE id=@id AND tenant_id=@tenant AND store_id=@store AND status <> 'Deleted'", db);
        P(cmd, "email", c.EmailAddress); P(cmd, "gender", c.Gender); P(cmd, "company", c.CompanyName); P(cmd, "provider", c.Provider); P(cmd, "language", c.DefaultLanguageCode); P(cmd, "id", c.Id); P(cmd, "tenant", ctx.TenantId); P(cmd, "store", ctx.StoreId); await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task UpdateAddressesAsync(Guid id, AddressDto? billing, AddressDto? delivery, CancellationToken ct)
    {
        if (!UsesDatabase) { var list = _addresses.GetOrAdd(id, _ => []); if (billing is not null) UpsertMemoryAddress(list, id, billing, "Billing"); if (delivery is not null) UpsertMemoryAddress(list, id, delivery, "Delivery"); return; }
        await using var db = Connection(); await db.OpenAsync(ct); await using var tx = await db.BeginTransactionAsync(ct);
        if (billing is not null) await UpsertAddressAsync(db, tx, id, billing, "Billing", ct);
        if (delivery is not null) await UpsertAddressAsync(db, tx, id, delivery, "Delivery", ct);
        await tx.CommitAsync(ct);
    }

    public async Task DeleteCustomerAsync(Guid id, RequestContext ctx, CancellationToken ct)
    {
        if (!UsesDatabase) { if (_customers.TryGetValue(id, out var c)) { c.Status = "Deleted"; _attributes.TryRemove(id, out _); _addresses.TryRemove(id, out _); } return; }
        await using var db = Connection(); await db.OpenAsync(ct);
        await using var cmd = new NpgsqlCommand("UPDATE customer_identity.customer_accounts SET status='Deleted',updated_at=now() WHERE id=@id AND tenant_id=@tenant AND store_id=@store", db);
        P(cmd, "id", id); P(cmd, "tenant", ctx.TenantId); P(cmd, "store", ctx.StoreId); await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task<AdministratorAccount?> FindAdminAsync(Guid id, RequestContext ctx, CancellationToken ct)
    {
        if (!UsesDatabase) return _admins.TryGetValue(id, out var a) && a.TenantId == ctx.TenantId && a.StoreId == ctx.StoreId ? a : null;
        await using var db = Connection(); await db.OpenAsync(ct);
        await using var cmd = new NpgsqlCommand("""SELECT id,tenant_id,store_id,user_name,email_address,password_hash,first_name,last_name,is_active,default_language_code,last_password_reset_at FROM customer_identity.administrator_accounts WHERE id=@id AND tenant_id=@tenant AND store_id=@store""", db);
        P(cmd, "id", id); P(cmd, "tenant", ctx.TenantId); P(cmd, "store", ctx.StoreId); await using var r = await cmd.ExecuteReaderAsync(ct);
        if (!await r.ReadAsync(ct)) return null; var admin = ReadAdmin(r); await r.CloseAsync(); await LoadAdminGroupsAsync(db, admin, ct); return admin;
    }

    public async Task<AdministratorAccount?> FindAdminByLoginAsync(string login, RequestContext ctx, CancellationToken ct)
    {
        if (!UsesDatabase) return _admins.Values.FirstOrDefault(a => a.TenantId == ctx.TenantId && a.StoreId == ctx.StoreId && a.UserName.Equals(login, StringComparison.OrdinalIgnoreCase));
        await using var db = Connection(); await db.OpenAsync(ct);
        await using var cmd = new NpgsqlCommand("""SELECT id,tenant_id,store_id,user_name,email_address,password_hash,first_name,last_name,is_active,default_language_code,last_password_reset_at FROM customer_identity.administrator_accounts WHERE tenant_id=@tenant AND store_id=@store AND user_name=@login""", db);
        P(cmd, "tenant", ctx.TenantId); P(cmd, "store", ctx.StoreId); P(cmd, "login", login); await using var r = await cmd.ExecuteReaderAsync(ct);
        if (!await r.ReadAsync(ct)) return null; var admin = ReadAdmin(r); await r.CloseAsync(); await LoadAdminGroupsAsync(db, admin, ct); return admin;
    }

    public async Task<(List<AdministratorAccount> Items, long Total)> ListAdminsAsync(RequestContext ctx, int page, int pageSize, string? email, CancellationToken ct)
    {
        if (!UsesDatabase)
        {
            var all = _admins.Values.Where(a => a.TenantId == ctx.TenantId && a.StoreId == ctx.StoreId && (string.IsNullOrWhiteSpace(email) || a.EmailAddress.Contains(email, StringComparison.OrdinalIgnoreCase))).OrderBy(a => a.UserName).ToList();
            return (all.Skip(Math.Max(0, page - 1) * pageSize).Take(pageSize).ToList(), all.Count);
        }
        await using var db = Connection(); await db.OpenAsync(ct);
        await using var cmd = new NpgsqlCommand("SELECT id,tenant_id,store_id,user_name,email_address,password_hash,first_name,last_name,is_active,default_language_code,last_password_reset_at FROM customer_identity.administrator_accounts WHERE tenant_id=@tenant AND store_id=@store AND (@email IS NULL OR email_address ILIKE '%' || @email || '%') ORDER BY user_name OFFSET @offset LIMIT @limit", db);
        P(cmd, "tenant", ctx.TenantId); P(cmd, "store", ctx.StoreId); P(cmd, "email", string.IsNullOrWhiteSpace(email) ? null : email); P(cmd, "offset", Math.Max(0, page - 1) * pageSize); P(cmd, "limit", pageSize);
        var result = new List<AdministratorAccount>(); await using var r = await cmd.ExecuteReaderAsync(ct); while (await r.ReadAsync(ct)) result.Add(ReadAdmin(r)); await r.CloseAsync();
        await using var count = new NpgsqlCommand("SELECT COUNT(*) FROM customer_identity.administrator_accounts WHERE tenant_id=@tenant AND store_id=@store AND (@email IS NULL OR email_address ILIKE '%' || @email || '%')", db);
        P(count, "tenant", ctx.TenantId); P(count, "store", ctx.StoreId); P(count, "email", string.IsNullOrWhiteSpace(email) ? null : email); return (result, (long)(await count.ExecuteScalarAsync(ct) ?? 0L));
    }

    public async Task AddAdminAsync(AdministratorAccount a, IEnumerable<string> groups, RequestContext ctx, CancellationToken ct)
    {
        a.Groups.AddRange(groups.Distinct(StringComparer.OrdinalIgnoreCase));
        if (!UsesDatabase) { if (_admins.Values.Any(x => x.TenantId == ctx.TenantId && x.StoreId == ctx.StoreId && x.UserName.Equals(a.UserName, StringComparison.OrdinalIgnoreCase))) throw new DomainException("USERNAME_CONFLICT", "Username is already registered for this store", 409); _admins[a.Id] = a; return; }
        await using var db = Connection(); await db.OpenAsync(ct); await using var tx = await db.BeginTransactionAsync(ct);
        try
        {
            await using var cmd = new NpgsqlCommand("INSERT INTO customer_identity.administrator_accounts(id,tenant_id,store_id,user_name,email_address,password_hash,first_name,last_name,is_active,default_language_code) VALUES(@id,@tenant,@store,@username,@email,@hash,@first,@last,true,@language)", db, tx);
            P(cmd, "id", a.Id); P(cmd, "tenant", a.TenantId); P(cmd, "store", a.StoreId); P(cmd, "username", a.UserName); P(cmd, "email", a.EmailAddress); P(cmd, "hash", a.PasswordHash); P(cmd, "first", a.FirstName); P(cmd, "last", a.LastName); P(cmd, "language", a.DefaultLanguageCode); await cmd.ExecuteNonQueryAsync(ct);
            foreach (var group in a.Groups) await EnsureGroupAndMembershipAsync(db, tx, a.Id, group, ct);
            await tx.CommitAsync(ct);
        }
        catch (PostgresException ex) when (ex.SqlState == "23505") { await tx.RollbackAsync(ct); throw new DomainException("USERNAME_CONFLICT", "Username is already registered for this store", 409); }
    }

    public async Task UpdateAdminAsync(AdministratorAccount a, RequestContext ctx, CancellationToken ct)
    {
        if (!UsesDatabase) { _admins[a.Id] = a; return; }
        await using var db = Connection(); await db.OpenAsync(ct);
        await using var cmd = new NpgsqlCommand("UPDATE customer_identity.administrator_accounts SET user_name=@username,email_address=@email,first_name=@first,last_name=@last,store_id=@store,is_active=@active,default_language_code=@language,updated_at=now() WHERE id=@id AND tenant_id=@tenant AND store_id=@oldstore", db);
        P(cmd, "username", a.UserName); P(cmd, "email", a.EmailAddress); P(cmd, "first", a.FirstName); P(cmd, "last", a.LastName); P(cmd, "store", a.StoreId); P(cmd, "active", a.IsActive); P(cmd, "language", a.DefaultLanguageCode); P(cmd, "id", a.Id); P(cmd, "tenant", ctx.TenantId); P(cmd, "oldstore", ctx.StoreId); await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task DeleteAdminAsync(Guid id, RequestContext ctx, CancellationToken ct)
    {
        if (!UsesDatabase) { _admins.TryRemove(id, out _); return; }
        await using var db = Connection(); await db.OpenAsync(ct); await using var cmd = new NpgsqlCommand("DELETE FROM customer_identity.administrator_accounts WHERE id=@id AND tenant_id=@tenant AND store_id=@store", db);
        P(cmd, "id", id); P(cmd, "tenant", ctx.TenantId); P(cmd, "store", ctx.StoreId); await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task SaveResetAsync(string token, string subjectType, Guid subjectId, RequestContext ctx, CancellationToken ct)
    {
        var hash = Hash(token); var expiry = DateTimeOffset.UtcNow.AddDays(2);
        if (!UsesDatabase) { _resets[hash] = new ResetRecord(subjectType, subjectId, ctx.TenantId, ctx.StoreId, expiry); return; }
        await using var db = Connection(); await db.OpenAsync(ct);
        await using var cmd = new NpgsqlCommand("INSERT INTO customer_identity.credential_reset_tokens(id,subject_type,customer_id,administrator_id,token_hash,tenant_id,store_id,expires_at) VALUES(@id,@type::customer_identity.reset_subject_type,@customer,@admin,@hash,@tenant,@store,@expiry)", db);
        P(cmd, "id", Guid.NewGuid()); P(cmd, "type", subjectType); P(cmd, "customer", subjectType == "Customer" ? subjectId : null); P(cmd, "admin", subjectType == "Administrator" ? subjectId : null); P(cmd, "hash", hash); P(cmd, "tenant", ctx.TenantId); P(cmd, "store", ctx.StoreId); P(cmd, "expiry", expiry); await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task<(string Type, Guid SubjectId, DateTimeOffset ExpiresAt)?> FindResetAsync(string token, string store, string tenant, CancellationToken ct)
    {
        var hash = Hash(token);
        if (!UsesDatabase) return _resets.TryGetValue(hash, out var value) && value.TenantId == tenant && value.StoreId == store && value.ExpiresAt > DateTimeOffset.UtcNow ? (value.SubjectType, value.SubjectId, value.ExpiresAt) : null;
        await using var db = Connection(); await db.OpenAsync(ct);
        await using var cmd = new NpgsqlCommand("SELECT subject_type::text,COALESCE(customer_id,administrator_id),expires_at FROM customer_identity.credential_reset_tokens WHERE token_hash=@hash AND tenant_id=@tenant AND store_id=@store AND consumed_at IS NULL AND expires_at > now()", db);
        P(cmd, "hash", hash); P(cmd, "tenant", tenant); P(cmd, "store", store); await using var r = await cmd.ExecuteReaderAsync(ct); return await r.ReadAsync(ct) ? (r.GetString(0), r.GetGuid(1), ReadOffset(r, 2)) : null;
    }

    public async Task ConsumeResetAsync(string token, string store, CancellationToken ct)
    {
        var hash = Hash(token); if (!UsesDatabase) { _resets.TryRemove(hash, out _); return; }
        await using var db = Connection(); await db.OpenAsync(ct); await using var cmd = new NpgsqlCommand("UPDATE customer_identity.credential_reset_tokens SET consumed_at=now() WHERE token_hash=@hash AND store_id=@store AND consumed_at IS NULL", db);
        P(cmd, "hash", hash); P(cmd, "store", store); await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task QueueResetEmailAsync(string recipient, string returnUrl, RequestContext context, string subjectType, string token, CancellationToken ct)
    {
        if (!UsesDatabase)
        {
            _logger.LogInformation("Password reset email queued for {Recipient}; token content is withheld from logs.", recipient);
            return;
        }
        await using var db = Connection(); await db.OpenAsync(ct);
        await using var cmd = new NpgsqlCommand("INSERT INTO customer_identity.email_outbox(id,tenant_id,store_id,recipient,template,payload) VALUES(@id,@tenant,@store,@recipient,@template,@payload)", db);
        P(cmd, "id", Guid.NewGuid()); P(cmd, "tenant", context.TenantId); P(cmd, "store", context.StoreId); P(cmd, "recipient", recipient);
        P(cmd, "template", subjectType == "Administrator" ? "administrator-password-reset" : "customer-password-reset");
        P(cmd, "payload", JsonSerializer.Serialize(new { returnUrl, token, storeCode = context.StoreId }));
        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task MarkEventPublishedAsync(Guid eventId, CancellationToken ct)
    {
        if (!UsesDatabase) return;
        await using var db = Connection(); await db.OpenAsync(ct);
        await using var cmd = new NpgsqlCommand("UPDATE customer_identity.event_outbox SET published_at=now() WHERE id=@id AND published_at IS NULL", db);
        P(cmd, "id", eventId); await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task<List<ReviewRecord>> ReviewsAsync(Guid target, CancellationToken ct)
    {
        if (!UsesDatabase) return _reviews.Values.Where(r => r.ReviewedCustomerId == target && r.Status != "Deleted").OrderBy(r => r.ReviewDate).ToList();
        await using var db = Connection(); await db.OpenAsync(ct); await using var cmd = new NpgsqlCommand("SELECT id,reviewer_customer_id,reviewed_customer_id,rating,review_text,review_date,status::text FROM customer_identity.customer_reviews WHERE reviewed_customer_id=@target AND status <> 'Deleted' ORDER BY review_date", db);
        P(cmd, "target", target); await using var rd = await cmd.ExecuteReaderAsync(ct); var list = new List<ReviewRecord>(); while (await rd.ReadAsync(ct)) list.Add(ReadReview(rd)); return list;
    }

    public async Task<ReviewRecord?> FindReviewAsync(Guid id, CancellationToken ct)
    {
        if (!UsesDatabase) return _reviews.TryGetValue(id, out var r) && r.Status != "Deleted" ? r : null;
        await using var db = Connection(); await db.OpenAsync(ct); await using var cmd = new NpgsqlCommand("SELECT id,reviewer_customer_id,reviewed_customer_id,rating,review_text,review_date,status::text FROM customer_identity.customer_reviews WHERE id=@id", db);
        P(cmd, "id", id); await using var rd = await cmd.ExecuteReaderAsync(ct); return await rd.ReadAsync(ct) ? ReadReview(rd) : null;
    }

    public async Task AddReviewAsync(ReviewRecord review, CancellationToken ct)
    {
        if (!UsesDatabase) { if (_reviews.Values.Any(r => r.ReviewerCustomerId == review.ReviewerCustomerId && r.ReviewedCustomerId == review.ReviewedCustomerId && r.Status != "Deleted")) throw new DomainException("DUPLICATE_REVIEW", "A review already exists for this customer", 409); _reviews[review.Id] = review; return; }
        await using var db = Connection(); await db.OpenAsync(ct); await using var cmd = new NpgsqlCommand("INSERT INTO customer_identity.customer_reviews(id,reviewer_customer_id,reviewed_customer_id,rating,review_text,review_date,status) VALUES(@id,@reviewer,@target,@rating,@text,@date,@status::customer_identity.review_status)", db);
        P(cmd, "id", review.Id); P(cmd, "reviewer", review.ReviewerCustomerId); P(cmd, "target", review.ReviewedCustomerId); P(cmd, "rating", review.Rating); P(cmd, "text", review.Description); P(cmd, "date", review.ReviewDate); P(cmd, "status", review.Status);
        try { await cmd.ExecuteNonQueryAsync(ct); } catch (PostgresException ex) when (ex.SqlState == "23505") { throw new DomainException("DUPLICATE_REVIEW", "A review already exists for this customer", 409); }
    }

    public async Task AddReviewWithAggregateAsync(ReviewRecord review, decimal average, int count, CancellationToken ct)
    {
        if (!UsesDatabase)
        {
            await AddReviewAsync(review, ct);
            await SetAggregateAsync(review.ReviewedCustomerId, average, count, ct);
            return;
        }
        await using var db = Connection(); await db.OpenAsync(ct); await using var tx = await db.BeginTransactionAsync(ct);
        await using (var insert = new NpgsqlCommand("INSERT INTO customer_identity.customer_reviews(id,reviewer_customer_id,reviewed_customer_id,rating,review_text,review_date,status) VALUES(@id,@reviewer,@target,@rating,@text,@date,@status::customer_identity.review_status)", db, tx))
        {
            P(insert, "id", review.Id); P(insert, "reviewer", review.ReviewerCustomerId); P(insert, "target", review.ReviewedCustomerId); P(insert, "rating", review.Rating); P(insert, "text", review.Description); P(insert, "date", review.ReviewDate); P(insert, "status", review.Status);
            try { await insert.ExecuteNonQueryAsync(ct); } catch (PostgresException ex) when (ex.SqlState == "23505") { throw new DomainException("DUPLICATE_REVIEW", "A review already exists for this customer", 409); }
        }
        await using (var aggregate = new NpgsqlCommand("UPDATE customer_identity.customer_accounts SET review_average=@average,review_count=@count,updated_at=now() WHERE id=@target", db, tx))
        {
            P(aggregate, "average", average); P(aggregate, "count", count); P(aggregate, "target", review.ReviewedCustomerId); await aggregate.ExecuteNonQueryAsync(ct);
        }
        await tx.CommitAsync(ct);
    }

    public async Task SaveReviewAsync(ReviewRecord review, CancellationToken ct)
    {
        if (!UsesDatabase) { _reviews[review.Id] = review; return; }
        await using var db = Connection(); await db.OpenAsync(ct); await using var cmd = new NpgsqlCommand("UPDATE customer_identity.customer_reviews SET rating=@rating,review_text=@text,status=@status::customer_identity.review_status,updated_at=now() WHERE id=@id", db);
        P(cmd, "rating", review.Rating); P(cmd, "text", review.Description); P(cmd, "status", review.Status); P(cmd, "id", review.Id); await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task SaveReviewWithAggregateAsync(ReviewRecord review, decimal average, int count, CancellationToken ct)
    {
        if (!UsesDatabase)
        {
            await SaveReviewAsync(review, ct); await SetAggregateAsync(review.ReviewedCustomerId, average, count, ct); return;
        }
        await using var db = Connection(); await db.OpenAsync(ct); await using var tx = await db.BeginTransactionAsync(ct);
        await using (var update = new NpgsqlCommand("UPDATE customer_identity.customer_reviews SET rating=@rating,review_text=@text,status=@status::customer_identity.review_status,updated_at=now() WHERE id=@id", db, tx))
        {
            P(update, "rating", review.Rating); P(update, "text", review.Description); P(update, "status", review.Status); P(update, "id", review.Id); await update.ExecuteNonQueryAsync(ct);
        }
        await using (var aggregate = new NpgsqlCommand("UPDATE customer_identity.customer_accounts SET review_average=@average,review_count=@count,updated_at=now() WHERE id=@target", db, tx))
        {
            P(aggregate, "average", average); P(aggregate, "count", count); P(aggregate, "target", review.ReviewedCustomerId); await aggregate.ExecuteNonQueryAsync(ct);
        }
        await tx.CommitAsync(ct);
    }

    public async Task DeleteReviewAsync(Guid id, CancellationToken ct)
    {
        if (!UsesDatabase) { if (_reviews.TryGetValue(id, out var r)) r.Status = "Deleted"; return; }
        await using var db = Connection(); await db.OpenAsync(ct); await using var cmd = new NpgsqlCommand("UPDATE customer_identity.customer_reviews SET status='Deleted',updated_at=now() WHERE id=@id", db); P(cmd, "id", id); await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task DeleteReviewWithAggregateAsync(Guid id, Guid target, decimal average, int count, CancellationToken ct)
    {
        if (!UsesDatabase)
        {
            await DeleteReviewAsync(id, ct); await SetAggregateAsync(target, average, count, ct); return;
        }
        await using var db = Connection(); await db.OpenAsync(ct); await using var tx = await db.BeginTransactionAsync(ct);
        await using (var remove = new NpgsqlCommand("UPDATE customer_identity.customer_reviews SET status='Deleted',updated_at=now() WHERE id=@id", db, tx)) { P(remove, "id", id); await remove.ExecuteNonQueryAsync(ct); }
        await using (var aggregate = new NpgsqlCommand("UPDATE customer_identity.customer_accounts SET review_average=@average,review_count=@count,updated_at=now() WHERE id=@target", db, tx)) { P(aggregate, "average", average); P(aggregate, "count", count); P(aggregate, "target", target); await aggregate.ExecuteNonQueryAsync(ct); }
        await tx.CommitAsync(ct);
    }

    public async Task SetAggregateAsync(Guid target, decimal average, int count, CancellationToken ct)
    {
        if (!UsesDatabase) { if (_customers.TryGetValue(target, out var c)) { c.ReviewAverage = average; c.ReviewCount = count; } return; }
        await using var db = Connection(); await db.OpenAsync(ct); await using var cmd = new NpgsqlCommand("UPDATE customer_identity.customer_accounts SET review_average=@average,review_count=@count,updated_at=now() WHERE id=@id", db); P(cmd, "average", average); P(cmd, "count", count); P(cmd, "id", target); await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task<NewsletterRecord> UpsertNewsletterAsync(NewsletterRecord n, CancellationToken ct)
    {
        var key = $"{n.TenantId}|{n.StoreId}|{n.CampaignCode}|{n.Email.ToLowerInvariant()}";
        if (!UsesDatabase) { _newsletters.AddOrUpdate(key, n, (_, existing) => { existing.FirstName = n.FirstName; existing.LastName = n.LastName; existing.Status = "Subscribed"; existing.UnsubscribedAt = null; return existing; }); return _newsletters[key]; }
        await using var db = Connection(); await db.OpenAsync(ct);
        await using var cmd = new NpgsqlCommand("""INSERT INTO customer_identity.newsletter_subscriptions(tenant_id,store_id,campaign_code,email_address,first_name,last_name,status) VALUES(@tenant,@store,@campaign,@email,@first,@last,'Subscribed') ON CONFLICT(tenant_id,store_id,campaign_code,email_address) DO UPDATE SET first_name=EXCLUDED.first_name,last_name=EXCLUDED.last_name,status='Subscribed',unsubscribed_at=NULL,updated_at=now() RETURNING id,store_id,campaign_code,email_address,first_name,last_name,status::text,subscribed_at,unsubscribed_at""", db);
        P(cmd, "tenant", n.TenantId); P(cmd, "store", n.StoreId); P(cmd, "campaign", n.CampaignCode); P(cmd, "email", n.Email.ToLowerInvariant()); P(cmd, "first", n.FirstName); P(cmd, "last", n.LastName);
        await using var r = await cmd.ExecuteReaderAsync(ct); await r.ReadAsync(ct); return ReadNewsletter(r);
    }

    public async Task<bool> UnsubscribeAsync(string email, RequestContext ctx, string campaign, CancellationToken ct)
    {
        if (!UsesDatabase) { var found = _newsletters.Values.FirstOrDefault(n => n.TenantId == ctx.TenantId && n.StoreId == ctx.StoreId && n.CampaignCode == campaign && n.Email.Equals(email, StringComparison.OrdinalIgnoreCase)); if (found is null) return false; found.Status = "Unsubscribed"; found.UnsubscribedAt = DateTimeOffset.UtcNow; return true; }
        await using var db = Connection(); await db.OpenAsync(ct); await using var cmd = new NpgsqlCommand("UPDATE customer_identity.newsletter_subscriptions SET status='Unsubscribed',unsubscribed_at=now(),updated_at=now() WHERE tenant_id=@tenant AND store_id=@store AND campaign_code=@campaign AND email_address=@email", db); P(cmd, "tenant", ctx.TenantId); P(cmd, "store", ctx.StoreId); P(cmd, "campaign", campaign); P(cmd, "email", email.ToLowerInvariant()); return await cmd.ExecuteNonQueryAsync(ct) > 0;
    }

    public async Task<ExternalIdentityRecord> AddExternalAsync(ExternalIdentityRecord e, CancellationToken ct)
    {
        var key = $"{e.UserId}|{e.ProviderId}|{e.ProviderUserId}";
        if (!UsesDatabase) { if (!_external.TryAdd(key, e)) throw new DomainException("IDENTITY_CONNECTION_EXISTS", "External identity is already linked", 409); return e; }
        await using var db = Connection(); await db.OpenAsync(ct); await using var cmd = new NpgsqlCommand("INSERT INTO customer_identity.external_identity_connections(user_id,provider_id,provider_user_id,access_token,refresh_token,profile_url) VALUES(@user,@provider,@providerUser,@access,@refresh,@profile)", db);
        P(cmd, "user", e.UserId); P(cmd, "provider", e.ProviderId); P(cmd, "providerUser", e.ProviderUserId); P(cmd, "access", e.AccessToken); P(cmd, "refresh", e.RefreshToken); P(cmd, "profile", e.ProfileUrl);
        try { await cmd.ExecuteNonQueryAsync(ct); return e; } catch (PostgresException ex) when (ex.SqlState == "23505") { throw new DomainException("IDENTITY_CONNECTION_EXISTS", "External identity is already linked", 409); }
    }

    public async Task<bool> CustomerLoginExistsAsync(string login, RequestContext ctx, CancellationToken ct) => await FindCustomerByLoginAsync(login, ctx, ct) is not null;
    public async Task<bool> AdminLoginExistsAsync(string login, RequestContext ctx, CancellationToken ct) => await FindAdminByLoginAsync(login, ctx, ct) is not null;

    public async Task SetCustomerPasswordAsync(Guid id, string hash, RequestContext ctx, CancellationToken ct)
    {
        if (!UsesDatabase) { if (_customers.TryGetValue(id, out var c)) { c.PasswordHash = hash; c.LastPasswordResetAt = DateTimeOffset.UtcNow; } return; }
        await using var db = Connection(); await db.OpenAsync(ct); await using var cmd = new NpgsqlCommand("UPDATE customer_identity.customer_accounts SET password_hash=@hash,last_password_reset_at=now(),updated_at=now() WHERE id=@id AND tenant_id=@tenant AND store_id=@store", db); P(cmd, "hash", hash); P(cmd, "id", id); P(cmd, "tenant", ctx.TenantId); P(cmd, "store", ctx.StoreId); await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task SetAdminPasswordAsync(Guid id, string hash, RequestContext ctx, CancellationToken ct)
    {
        if (!UsesDatabase) { if (_admins.TryGetValue(id, out var a)) { a.PasswordHash = hash; a.LastPasswordResetAt = DateTimeOffset.UtcNow; } return; }
        await using var db = Connection(); await db.OpenAsync(ct); await using var cmd = new NpgsqlCommand("UPDATE customer_identity.administrator_accounts SET password_hash=@hash,last_password_reset_at=now(),updated_at=now() WHERE id=@id AND tenant_id=@tenant AND store_id=@store", db); P(cmd, "hash", hash); P(cmd, "id", id); P(cmd, "tenant", ctx.TenantId); P(cmd, "store", ctx.StoreId); await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task SetAdminEnabledAsync(Guid id, bool enabled, RequestContext ctx, CancellationToken ct)
    {
        if (!UsesDatabase) { if (_admins.TryGetValue(id, out var a)) a.IsActive = enabled; return; }
        await using var db = Connection(); await db.OpenAsync(ct); await using var cmd = new NpgsqlCommand("UPDATE customer_identity.administrator_accounts SET is_active=@active,updated_at=now() WHERE id=@id AND tenant_id=@tenant AND store_id=@store", db); P(cmd, "active", enabled); P(cmd, "id", id); P(cmd, "tenant", ctx.TenantId); P(cmd, "store", ctx.StoreId); await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task RemoveOptionAsync(Guid optionId, string storeId, CancellationToken ct)
    {
        if (!UsesDatabase)
        {
            foreach (var entry in _attributes) _attributes[entry.Key] = entry.Value.Where(x => x.OptionId != optionId.ToString()).ToList();
            return;
        }
        await using var db = Connection(); await db.OpenAsync(ct); await using var tx = await db.BeginTransactionAsync(ct);
        await using (var assignments = new NpgsqlCommand("DELETE FROM customer_identity.customer_attributes WHERE option_id=@option", db, tx)) { P(assignments, "option", optionId); await assignments.ExecuteNonQueryAsync(ct); }
        await using (var values = new NpgsqlCommand("DELETE FROM customer_identity.customer_option_values WHERE option_id=@option AND store_id=@store", db, tx)) { P(values, "option", optionId); P(values, "store", storeId); await values.ExecuteNonQueryAsync(ct); }
        await using (var option = new NpgsqlCommand("DELETE FROM customer_identity.customer_options WHERE id=@option AND store_id=@store", db, tx)) { P(option, "option", optionId); P(option, "store", storeId); if (await option.ExecuteNonQueryAsync(ct) == 0) throw new DomainException("OPTION_NOT_FOUND", "Customer option was not found in this store", 404); }
        await tx.CommitAsync(ct);
    }

    public async Task RemoveOptionValueAsync(Guid valueId, string storeId, CancellationToken ct)
    {
        if (!UsesDatabase) { foreach (var entry in _attributes) _attributes[entry.Key] = entry.Value.Where(x => x.ValueId != valueId.ToString()).ToList(); return; }
        await using var db = Connection(); await db.OpenAsync(ct); await using var tx = await db.BeginTransactionAsync(ct);
        await using (var assignments = new NpgsqlCommand("DELETE FROM customer_identity.customer_attributes WHERE option_value_id=@value", db, tx)) { P(assignments, "value", valueId); await assignments.ExecuteNonQueryAsync(ct); }
        await using (var value = new NpgsqlCommand("DELETE FROM customer_identity.customer_option_values WHERE id=@value AND store_id=@store", db, tx)) { P(value, "value", valueId); P(value, "store", storeId); if (await value.ExecuteNonQueryAsync(ct) == 0) throw new DomainException("OPTION_VALUE_NOT_FOUND", "Customer option value was not found in this store", 404); }
        await tx.CommitAsync(ct);
    }

    private static CustomerAccount ReadCustomer(IDataRecord r) => new() { Id = r.GetGuid(0), TenantId = r.GetString(1), StoreId = r.GetString(2), LoginName = r.GetString(3), EmailAddress = r.GetString(4), PasswordHash = r.GetString(5), Gender = r.GetString(6), DateOfBirth = r.IsDBNull(7) ? null : DateOnly.FromDateTime(r.GetDateTime(7)), CompanyName = r.IsDBNull(8) ? null : r.GetString(8), Provider = r.IsDBNull(9) ? null : r.GetString(9), Status = r.GetString(10), DefaultLanguageCode = r.GetString(11), ReviewAverage = r.GetDecimal(12), ReviewCount = r.GetInt32(13), Anonymous = r.GetBoolean(14), LastPasswordResetAt = r.IsDBNull(15) ? null : ReadOffset(r, 15) };
    private static AdministratorAccount ReadAdmin(IDataRecord r) => new() { Id = r.GetGuid(0), TenantId = r.GetString(1), StoreId = r.GetString(2), UserName = r.GetString(3), EmailAddress = r.GetString(4), PasswordHash = r.GetString(5), FirstName = r.IsDBNull(6) ? null : r.GetString(6), LastName = r.IsDBNull(7) ? null : r.GetString(7), IsActive = r.GetBoolean(8), DefaultLanguageCode = r.IsDBNull(9) ? null : r.GetString(9), LastPasswordResetAt = r.IsDBNull(10) ? null : ReadOffset(r, 10) };
    private static AddressRecord ReadAddress(IDataRecord r) => new() { Id = r.GetGuid(0), CustomerId = r.GetGuid(1), AddressType = r.GetString(2), FirstName = r.GetString(3), LastName = r.GetString(4), CompanyName = r.IsDBNull(5) ? null : r.GetString(5), StreetAddress = r.GetString(6), City = r.GetString(7), PostalCode = r.GetString(8), StateProvince = r.IsDBNull(9) ? null : r.GetString(9), Telephone = r.IsDBNull(10) ? null : r.GetString(10), CountryCode = r.GetString(11), ZoneCode = r.IsDBNull(12) ? null : r.GetString(12), Latitude = r.IsDBNull(13) ? null : r.GetString(13), Longitude = r.IsDBNull(14) ? null : r.GetString(14) };
    private static ReviewRecord ReadReview(IDataRecord r) => new() { Id = r.GetGuid(0), ReviewerCustomerId = r.GetGuid(1), ReviewedCustomerId = r.GetGuid(2), Rating = r.GetDecimal(3), Description = r.IsDBNull(4) ? null : r.GetString(4), ReviewDate = ReadOffset(r, 5), Status = r.GetString(6) };
    private static NewsletterRecord ReadNewsletter(IDataRecord r) => new() { Id = r.GetGuid(0), StoreId = r.GetString(1), CampaignCode = r.GetString(2), Email = r.GetString(3), FirstName = r.IsDBNull(4) ? null : r.GetString(4), LastName = r.IsDBNull(5) ? null : r.GetString(5), Status = r.GetString(6), SubscribedAt = ReadOffset(r, 7), UnsubscribedAt = r.IsDBNull(8) ? null : ReadOffset(r, 8) };
    private static DateTimeOffset ReadOffset(IDataRecord r, int index) => r.GetValue(index) switch { DateTimeOffset value => value, DateTime value => new DateTimeOffset(DateTime.SpecifyKind(value, DateTimeKind.Utc)), _ => DateTimeOffset.Parse(r.GetValue(index).ToString()!) };
    private async Task LoadAdminGroupsAsync(NpgsqlConnection db, AdministratorAccount a, CancellationToken ct) { await using var c = new NpgsqlCommand("SELECT g.name,p.name FROM customer_identity.administrator_group_memberships m JOIN customer_identity.permission_groups g ON g.id=m.group_id LEFT JOIN customer_identity.group_permissions gp ON gp.group_id=g.id LEFT JOIN customer_identity.permissions p ON p.id=gp.permission_id WHERE m.administrator_id=@id", db); P(c, "id", a.Id); await using var r = await c.ExecuteReaderAsync(ct); while (await r.ReadAsync(ct)) { a.Groups.Add(r.GetString(0)); if (!r.IsDBNull(1)) a.Permissions.Add(r.GetString(1)); } }
    private static async Task EnsureGroupAndMembershipAsync(NpgsqlConnection db, NpgsqlTransaction tx, Guid admin, string group, CancellationToken ct) { await using var c = new NpgsqlCommand("INSERT INTO customer_identity.permission_groups(name,group_type) VALUES(@name,'Administrator') ON CONFLICT(name) DO UPDATE SET name=EXCLUDED.name RETURNING id", db, tx); P(c, "name", group); var id = (Guid)(await c.ExecuteScalarAsync(ct) ?? throw new InvalidOperationException("Group creation returned no id")); await using var m = new NpgsqlCommand("INSERT INTO customer_identity.administrator_group_memberships(administrator_id,group_id) VALUES(@admin,@group) ON CONFLICT DO NOTHING", db, tx); P(m, "admin", admin); P(m, "group", id); await m.ExecuteNonQueryAsync(ct); }
    private static async Task InsertAddressAsync(NpgsqlConnection db, NpgsqlTransaction tx, Guid id, AddressDto a, string type, CancellationToken ct) { await using var c = new NpgsqlCommand("INSERT INTO customer_identity.customer_addresses(customer_id,address_type,first_name,last_name,company_name,street_address,city,postal_code,state_province,telephone,country_code,zone_code,latitude,longitude) VALUES(@id,@type::customer_identity.address_type,@first,@last,@company,@street,@city,@postal,@state,@telephone,@country,@zone,@lat,@lon)", db, tx); AddAddressParameters(c, id, a, type); await c.ExecuteNonQueryAsync(ct); }
    private static async Task InsertAttributeAsync(NpgsqlConnection db, NpgsqlTransaction tx, CustomerAccount c, CustomerAttributeDto a, CancellationToken ct) { await using var check = new NpgsqlCommand("SELECT 1 FROM customer_identity.customer_options o JOIN customer_identity.customer_option_values v ON v.option_id=o.id WHERE o.id=@option AND v.id=@value AND o.store_id=@store AND v.store_id=@store", db, tx); P(check, "option", Guid.Parse(a.OptionId)); P(check, "value", Guid.Parse(a.OptionValueId)); P(check, "store", c.StoreId); if (await check.ExecuteScalarAsync(ct) is null) throw new DomainException("ATTRIBUTE_SCOPE_VIOLATION", "Customer option is not valid for this store", 422); await using var cmd = new NpgsqlCommand("INSERT INTO customer_identity.customer_attributes(customer_id,option_id,option_value_id,text_value) VALUES(@customer,@option,@value,@text)", db, tx); P(cmd, "customer", c.Id); P(cmd, "option", Guid.Parse(a.OptionId)); P(cmd, "value", Guid.Parse(a.OptionValueId)); P(cmd, "text", a.TextValue); await cmd.ExecuteNonQueryAsync(ct); }
    private static async Task UpsertAddressAsync(NpgsqlConnection db, NpgsqlTransaction tx, Guid id, AddressDto a, string type, CancellationToken ct) { await using var c = new NpgsqlCommand("INSERT INTO customer_identity.customer_addresses(customer_id,address_type,first_name,last_name,company_name,street_address,city,postal_code,state_province,telephone,country_code,zone_code,latitude,longitude) VALUES(@id,@type::customer_identity.address_type,@first,@last,@company,@street,@city,@postal,@state,@telephone,@country,@zone,@lat,@lon) ON CONFLICT(customer_id,address_type) DO UPDATE SET first_name=EXCLUDED.first_name,last_name=EXCLUDED.last_name,company_name=EXCLUDED.company_name,street_address=EXCLUDED.street_address,city=EXCLUDED.city,postal_code=EXCLUDED.postal_code,state_province=EXCLUDED.state_province,telephone=EXCLUDED.telephone,country_code=EXCLUDED.country_code,zone_code=EXCLUDED.zone_code,latitude=EXCLUDED.latitude,longitude=EXCLUDED.longitude,updated_at=now()", db, tx); AddAddressParameters(c, id, a, type); await c.ExecuteNonQueryAsync(ct); }
    private static void AddAddressParameters(NpgsqlCommand c, Guid id, AddressDto a, string type) { P(c, "id", id); P(c, "type", type); P(c, "first", a.FirstName); P(c, "last", a.LastName); P(c, "company", a.CompanyName); P(c, "street", a.StreetAddress); P(c, "city", a.City); P(c, "postal", a.PostalCode); P(c, "state", a.StateProvince); P(c, "telephone", a.Telephone); P(c, "country", a.CountryCode); P(c, "zone", a.ZoneCode); P(c, "lat", a.Latitude); P(c, "lon", a.Longitude); }
    private static AddressRecord ToAddress(Guid id, AddressDto a, string type) => new() { Id = Guid.NewGuid(), CustomerId = id, AddressType = type, FirstName = a.FirstName, LastName = a.LastName, CompanyName = a.CompanyName, StreetAddress = a.StreetAddress, City = a.City, PostalCode = a.PostalCode, StateProvince = a.StateProvince, Telephone = a.Telephone, CountryCode = a.CountryCode, ZoneCode = a.ZoneCode, Latitude = a.Latitude, Longitude = a.Longitude };
    private static AddressDto CopyAddress(AddressDto a, string type) => new() { AddressType = type, FirstName = a.FirstName, LastName = a.LastName, CompanyName = a.CompanyName, StreetAddress = a.StreetAddress, City = a.City, PostalCode = a.PostalCode, StateProvince = a.StateProvince, Telephone = a.Telephone, CountryCode = a.CountryCode, ZoneCode = a.ZoneCode, Latitude = a.Latitude, Longitude = a.Longitude };
    private static void UpsertMemoryAddress(List<AddressRecord> list, Guid id, AddressDto a, string type) { var old = list.FirstOrDefault(x => x.AddressType == type); if (old is null) list.Add(ToAddress(id, a, type)); else { var replacement = ToAddress(id, a, type); list[list.IndexOf(old)] = replacement; } }
    private static AddressRecord Clone(AddressRecord a) => new() { Id = a.Id, CustomerId = a.CustomerId, AddressType = a.AddressType, FirstName = a.FirstName, LastName = a.LastName, CompanyName = a.CompanyName, StreetAddress = a.StreetAddress, City = a.City, PostalCode = a.PostalCode, StateProvince = a.StateProvince, Telephone = a.Telephone, CountryCode = a.CountryCode, ZoneCode = a.ZoneCode, Latitude = a.Latitude, Longitude = a.Longitude };
    private static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
    private sealed record ResetRecord(string SubjectType, Guid SubjectId, string TenantId, string StoreId, DateTimeOffset ExpiresAt);
}
