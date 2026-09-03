using System.Data;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Npgsql;
using Shopizer.CatalogProduct.Models;
using Shopizer.Services.Ms02.Contracts;

namespace Shopizer.CatalogProduct.Data;

public sealed class CatalogAggregate(ProductRecord product)
{
    public ProductRecord Product { get; } = product;
    public List<ProductDescriptionRecord> Descriptions { get; } = [];
    public List<CategoryRecord> Categories { get; } = [];
    public List<VariantRecord> Variants { get; } = [];
    public List<AvailabilityRecord> Availabilities { get; } = [];
    public List<PriceRecord> Prices { get; } = [];
    public List<MediaRecord> Media { get; } = [];
    public List<(string Code, string Value, string Name)> Properties { get; } = [];
    public List<(Guid OptionId, string Code, string Name, bool DisplayOnly, List<ProductOptionValueDto> Values)> Options { get; } = [];
}

public sealed class CatalogRepository(NpgsqlDataSource dataSource, ILogger<CatalogRepository> logger)
{
    private static void Add(NpgsqlCommand c, string name, object? value) =>
        c.Parameters.AddWithValue(name, value ?? DBNull.Value);

    private static async Task<NpgsqlConnection> OpenAsync(NpgsqlDataSource source, CancellationToken ct) =>
        await source.OpenConnectionAsync(ct);

    public async Task<bool> ProductSkuExistsAsync(string sku, RequestContext ctx, Guid? exclude, CancellationToken ct)
    {
        await using var connection = await OpenAsync(dataSource, ct);
        await using var command = new NpgsqlCommand($"""
            SELECT EXISTS(SELECT 1 FROM catalog_product.product
                          WHERE tenant_id=@tenant AND store_id=@store AND sku=@sku
                            {(exclude.HasValue ? "AND id <> @exclude" : "")})
            """, connection);
        Add(command, "tenant", ctx.TenantId); Add(command, "store", ctx.StoreId); Add(command, "sku", sku);
        if (exclude.HasValue) Add(command, "exclude", exclude.Value);
        return (bool)(await command.ExecuteScalarAsync(ct) ?? false);
    }

    public async Task<bool> CategoryCodeExistsAsync(string code, RequestContext ctx, Guid? exclude, CancellationToken ct)
    {
        await using var connection = await OpenAsync(dataSource, ct);
        await using var command = new NpgsqlCommand($"""
            SELECT EXISTS(SELECT 1 FROM catalog_product.category
                          WHERE tenant_id=@tenant AND store_id=@store AND code=@code
                            {(exclude.HasValue ? "AND id <> @exclude" : "")})
            """, connection);
        Add(command, "tenant", ctx.TenantId); Add(command, "store", ctx.StoreId); Add(command, "code", code); if (exclude.HasValue) Add(command, "exclude", exclude.Value);
        return (bool)(await command.ExecuteScalarAsync(ct) ?? false);
    }

    public async Task<bool> VariantSkuExistsAsync(Guid productId, string sku, RequestContext ctx, Guid? exclude, CancellationToken ct)
    {
        await using var connection = await OpenAsync(dataSource, ct);
        await using var command = new NpgsqlCommand($"""
            SELECT EXISTS(SELECT 1 FROM catalog_product.product_variant
                          WHERE product_id=@product AND store_id=@store AND sku=@sku
                            {(exclude.HasValue ? "AND id <> @exclude" : "")})
            """, connection);
        Add(command, "product", productId); Add(command, "store", ctx.StoreId); Add(command, "sku", sku); if (exclude.HasValue) Add(command, "exclude", exclude.Value);
        return (bool)(await command.ExecuteScalarAsync(ct) ?? false);
    }

    public async Task<CatalogAggregate?> FindProductAsync(Guid id, RequestContext ctx, string? language, string? country, bool storefront, CancellationToken ct)
    {
        await using var connection = await OpenAsync(dataSource, ct);
        return await ReadProductAsync(connection, null, id, ctx, language ?? "en", country, storefront, ct);
    }

    public async Task<CatalogAggregate?> FindProductBySkuAsync(string sku, RequestContext ctx, CancellationToken ct)
    {
        await using var connection = await OpenAsync(dataSource, ct);
        await using var command = new NpgsqlCommand("""
            SELECT id FROM catalog_product.product WHERE tenant_id=@tenant AND store_id=@store AND sku=@sku
            """, connection);
        Add(command, "tenant", ctx.TenantId); Add(command, "store", ctx.StoreId); Add(command, "sku", sku);
        var value = await command.ExecuteScalarAsync(ct);
        return value is Guid id ? await ReadProductAsync(connection, null, id, ctx, "en", null, true, ct) : null;
    }

    public async Task<CatalogAggregate?> FindProductBySlugAsync(string slug, RequestContext ctx, string language, string? country, CancellationToken ct)
    {
        await using var connection = await OpenAsync(dataSource, ct);
        await using var command = new NpgsqlCommand("""
            SELECT p.id FROM catalog_product.product p
            JOIN catalog_product.product_description d ON d.product_id=p.id
            WHERE p.tenant_id=@tenant AND p.store_id=@store AND d.friendly_url=@slug AND d.language_code=@language
              AND p.visible AND p.available AND p.date_available <= now()
              AND EXISTS (SELECT 1 FROM catalog_product.product_availability a
                          WHERE a.product_id=p.id AND a.active AND a.quantity > a.reserved_quantity
                            AND (a.region_code='*' OR a.region_code=@country))
            LIMIT 1
            """, connection);
        Add(command, "tenant", ctx.TenantId); Add(command, "store", ctx.StoreId); Add(command, "slug", slug);
        Add(command, "language", language); Add(command, "country", country ?? "*");
        var value = await command.ExecuteScalarAsync(ct);
        return value is Guid id ? await ReadProductAsync(connection, null, id, ctx, language, country, true, ct) : null;
    }

    public async Task<(List<CatalogAggregate> Items, int Total)> ListProductsAsync(RequestContext ctx, int page, int pageSize,
        string language, string? country, Guid? categoryId, string? sku, string? name, string? manufacturer, bool? available, bool storefront, CancellationToken ct)
    {
        await using var connection = await OpenAsync(dataSource, ct);
        var predicate = """
            FROM catalog_product.product p
            LEFT JOIN catalog_product.product_description d ON d.product_id=p.id AND d.language_code=@language
            WHERE p.tenant_id=@tenant AND p.store_id=@store
            """;
        if (storefront) predicate += " AND p.visible AND p.available AND p.date_available <= now()";
        if (categoryId.HasValue) predicate += """
            AND EXISTS (SELECT 1 FROM catalog_product.product_category pc
                        JOIN catalog_product.category c ON c.id=pc.category_id
                        WHERE pc.product_id=p.id AND c.store_id=@store AND (c.id=@category OR c.lineage LIKE '%/'||@categoryText||'/%'))
            """;
        if (country is not null && storefront) predicate += """
            AND EXISTS (SELECT 1 FROM catalog_product.product_availability a
                        WHERE a.product_id=p.id AND a.active AND a.quantity > a.reserved_quantity
                          AND (a.region_code='*' OR a.region_code=@country))
            """;
        if (sku is not null) predicate += " AND p.sku ILIKE @sku";
        if (name is not null) predicate += " AND d.name ILIKE @name";
        if (manufacturer is not null) predicate += " AND p.manufacturer_code=@manufacturer";
        if (available.HasValue) predicate += " AND p.available=@available";

        await using var count = new NpgsqlCommand("SELECT COUNT(DISTINCT p.id) " + predicate, connection);
        AddCriteria(count, ctx, language, country, categoryId, sku, name, manufacturer, available);
        var total = Convert.ToInt32(await count.ExecuteScalarAsync(ct));

        await using var ids = new NpgsqlCommand("SELECT p.id " + predicate +
            " GROUP BY p.id, p.sort_order ORDER BY p.sort_order, p.id OFFSET @offset LIMIT @limit", connection);
        AddCriteria(ids, ctx, language, country, categoryId, sku, name, manufacturer, available);
        Add(ids, "offset", page * pageSize); Add(ids, "limit", pageSize);
        var productIds = new List<Guid>();
        await using (var reader = await ids.ExecuteReaderAsync(ct))
            while (await reader.ReadAsync(ct)) productIds.Add(reader.GetGuid(0));
        var items = new List<CatalogAggregate>();
        foreach (var id in productIds)
        {
            var item = await ReadProductAsync(connection, null, id, ctx, language, country, storefront, ct);
            if (item is not null) items.Add(item);
        }
        return (items, total);
    }

    private static void AddCriteria(NpgsqlCommand command, RequestContext ctx, string language, string? country,
        Guid? category, string? sku, string? name, string? manufacturer, bool? available)
    {
        Add(command, "tenant", ctx.TenantId); Add(command, "store", ctx.StoreId); Add(command, "language", language);
        Add(command, "country", country ?? "*"); Add(command, "category", category);
        Add(command, "categoryText", category?.ToString()); Add(command, "sku", sku is null ? null : $"%{sku}%");
        Add(command, "name", name is null ? null : $"%{name}%"); Add(command, "manufacturer", manufacturer); Add(command, "available", available);
    }

    private static async Task<CatalogAggregate?> ReadProductAsync(NpgsqlConnection connection, NpgsqlTransaction? tx, Guid id,
        RequestContext ctx, string language, string? country, bool storefront, CancellationToken ct)
    {
        await using var command = new NpgsqlCommand("""
            SELECT id,tenant_id,store_id,sku,ref_sku,status,visible,available,can_be_purchased,date_available,
                   manufacturer_code,product_type_code,tax_class_code,product_virtual,product_shippable,product_free,
                   length,width,height,weight,review_average,review_count,sort_order
            FROM catalog_product.product
            WHERE id=@id AND tenant_id=@tenant AND store_id=@store
            """, connection, tx);
        Add(command, "id", id); Add(command, "tenant", ctx.TenantId); Add(command, "store", ctx.StoreId);
        ProductRecord? product = null;
        await using (var reader = await command.ExecuteReaderAsync(ct))
        {
            if (await reader.ReadAsync(ct))
            {
                product = new ProductRecord
                {
                    Id = reader.GetGuid(0), TenantId = reader.GetString(1), StoreId = reader.GetString(2), Sku = reader.GetString(3),
                    RefSku = reader.IsDBNull(4) ? null : reader.GetString(4), Status = reader.GetString(5), Visible = reader.GetBoolean(6),
                    Available = reader.GetBoolean(7), CanBePurchased = reader.GetBoolean(8), DateAvailable = reader.GetFieldValue<DateTimeOffset>(9),
                    ManufacturerCode = reader.IsDBNull(10) ? null : reader.GetString(10), ProductTypeCode = reader.IsDBNull(11) ? null : reader.GetString(11),
                    TaxClassCode = reader.IsDBNull(12) ? null : reader.GetString(12), ProductVirtual = reader.GetBoolean(13),
                    ProductShippable = reader.GetBoolean(14), ProductFree = reader.GetBoolean(15),
                    Length = reader.IsDBNull(16) ? null : reader.GetDecimal(16), Width = reader.IsDBNull(17) ? null : reader.GetDecimal(17),
                    Height = reader.IsDBNull(18) ? null : reader.GetDecimal(18), Weight = reader.IsDBNull(19) ? null : reader.GetDecimal(19),
                    ReviewAverage = reader.IsDBNull(20) ? 0 : reader.GetDecimal(20), ReviewCount = reader.GetInt32(21), SortOrder = reader.GetInt32(22)
                };
            }
        }
        if (product is null || (storefront && (!product.Visible || !product.Available || product.DateAvailable > DateTimeOffset.UtcNow))) return null;
        var aggregate = new CatalogAggregate(product);
        await ReadChildrenAsync(connection, tx, aggregate, ctx, language, country, ct);
        if (storefront && !aggregate.Availabilities.Any(a => a.Active && a.Quantity > a.ReservedQuantity &&
            (country is null || a.RegionCode == "*" || a.RegionCode.Equals(country, StringComparison.OrdinalIgnoreCase)))) return null;
        return aggregate;
    }

    private static async Task ReadChildrenAsync(NpgsqlConnection connection, NpgsqlTransaction? tx, CatalogAggregate aggregate,
        RequestContext ctx, string language, string? country, CancellationToken ct)
    {
        async Task Read(string sql, Func<NpgsqlDataReader, Task> action)
        {
            await using var command = new NpgsqlCommand(sql, connection, tx);
            Add(command, "product", aggregate.Product.Id); Add(command, "tenant", ctx.TenantId); Add(command, "store", ctx.StoreId);
            Add(command, "language", language); Add(command, "country", country ?? "*");
            await using var reader = await command.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct)) await action(reader);
        }
        await Read("SELECT id,language_code,name,friendly_url,description,highlights,title,keywords,meta_description FROM catalog_product.product_description WHERE product_id=@product AND language_code=@language",
            r => { aggregate.Descriptions.Add(new() { Id = r.GetGuid(0), ProductId = aggregate.Product.Id, LanguageCode = r.GetString(1), Name = r.GetString(2), FriendlyUrl = r.GetString(3), Description = N(r, 4), Highlights = N(r, 5), Title = N(r, 6), Keywords = N(r, 7), MetaDescription = N(r, 8) }); return Task.CompletedTask; });
        if (aggregate.Descriptions.Count == 0)
            await Read("SELECT id,language_code,name,friendly_url,description,highlights,title,keywords,meta_description FROM catalog_product.product_description WHERE product_id=@product ORDER BY id LIMIT 1",
                r => { aggregate.Descriptions.Add(new() { Id = r.GetGuid(0), ProductId = aggregate.Product.Id, LanguageCode = r.GetString(1), Name = r.GetString(2), FriendlyUrl = r.GetString(3), Description = N(r, 4), Highlights = N(r, 5), Title = N(r, 6), Keywords = N(r, 7), MetaDescription = N(r, 8) }); return Task.CompletedTask; });
        await Read("SELECT c.id,c.tenant_id,c.store_id,c.code,c.parent_id,c.category_image_uri,c.sort_order,c.status,c.visible,c.featured,c.depth,c.lineage FROM catalog_product.category c JOIN catalog_product.product_category pc ON pc.category_id=c.id WHERE pc.product_id=@product AND c.tenant_id=@tenant AND c.store_id=@store",
            r => { aggregate.Categories.Add(new() { Id=r.GetGuid(0), TenantId=r.GetString(1), StoreId=r.GetString(2), Code=r.GetString(3), ParentId=G(r,4), CategoryImageUri=N(r,5), SortOrder=r.GetInt32(6), Status=r.GetString(7), Visible=r.GetBoolean(8), Featured=r.GetBoolean(9), Depth=r.GetInt32(10), Lineage=r.GetString(11) }); return Task.CompletedTask; });
        await Read("SELECT id,product_id,store_id,sku,code,status,available,default_selection,date_available,sort_order FROM catalog_product.product_variant WHERE product_id=@product AND store_id=@store ORDER BY sort_order,id",
            r => { aggregate.Variants.Add(new() { Id=r.GetGuid(0), ProductId=r.GetGuid(1), StoreId=r.GetString(2), Sku=r.GetString(3), Code=N(r,4), Status=r.GetString(5), Available=r.GetBoolean(6), DefaultSelection=r.GetBoolean(7), DateAvailable=r.GetFieldValue<DateTimeOffset>(8), SortOrder=r.GetInt32(9) }); return Task.CompletedTask; });
        await Read("""
            SELECT a.option_id,o.code,a.option_value_id,v.code,a.display_only,a.price_adjustment,v.image_uri
            FROM catalog_product.product_attribute a
            JOIN catalog_product.product_option o ON o.id=a.option_id AND o.store_id=@store
            JOIN catalog_product.product_option_value v ON v.id=a.option_value_id AND v.store_id=@store
            WHERE a.product_id=@product
            ORDER BY o.sort_order,v.sort_order,a.id
            """, r =>
            {
                var optionId=r.GetGuid(0);var optionCode=r.GetString(1);var valueId=r.GetGuid(2);
                var value=new ProductOptionValueDto{Id=valueId.ToString(),Code=r.GetString(3),Name=r.GetString(3),DisplayOnly=r.GetBoolean(4),PriceAdjustment=r.GetDecimal(5),ImageUri=N(r,6)};
                if(r.GetBoolean(4)) aggregate.Properties.Add((optionCode,value.Code,value.Name??value.Code));
                else
                {
                    var index=aggregate.Options.FindIndex(x=>x.OptionId==optionId);
                    if(index<0) aggregate.Options.Add((optionId,optionCode,optionCode,false,[value]));
                    else aggregate.Options[index].Values.Add(value);
                }
                return Task.CompletedTask;
            });
        await Read("SELECT id,product_id,variant_id,store_id,region_code,quantity,reserved_quantity,active FROM catalog_product.product_availability WHERE (product_id=@product OR variant_id IN (SELECT id FROM catalog_product.product_variant WHERE product_id=@product AND store_id=@store)) AND store_id=@store ORDER BY region_code",
            r => { aggregate.Availabilities.Add(new() { Id=r.GetGuid(0), ProductId=G(r,1), VariantId=G(r,2), StoreId=r.GetString(3), RegionCode=r.GetString(4), Quantity=r.GetInt32(5), ReservedQuantity=r.GetInt32(6), Active=r.GetBoolean(7) }); return Task.CompletedTask; });
        await Read("SELECT i.id,i.product_id,i.variant_id,i.image_type,i.file_name,i.original_uri,i.transformed_uri,i.provider_key,i.external_url,i.default_image,i.media_status FROM catalog_product.product_image i WHERE i.product_id=@product",
            r => { aggregate.Media.Add(new() { Id=r.GetGuid(0), ProductId=r.GetGuid(1), VariantId=G(r,2), ImageType=r.GetString(3), FileName=r.GetString(4), OriginalUri=N(r,5), TransformedUri=N(r,6), ProviderKey=N(r,7), ExternalUrl=N(r,8), DefaultImage=r.GetBoolean(9), MediaStatus=r.GetString(10) }); return Task.CompletedTask; });
        await Read("SELECT p.id,p.availability_id,p.store_id,p.currency_code,p.amount,p.price_type,p.default_price,p.special_amount,p.special_start_at,p.special_end_at FROM catalog_product.product_price p JOIN catalog_product.product_availability a ON a.id=p.availability_id WHERE p.store_id=@store AND (a.product_id=@product OR a.variant_id IN (SELECT id FROM catalog_product.product_variant WHERE product_id=@product AND store_id=@store)) ORDER BY p.default_price DESC,p.id",
            r => { aggregate.Prices.Add(new() { Id=r.GetGuid(0), AvailabilityId=r.GetGuid(1), StoreId=r.GetString(2), CurrencyCode=r.GetString(3), Amount=r.GetDecimal(4), PriceType=r.GetString(5), DefaultPrice=r.GetBoolean(6), SpecialAmount=D(r,7), SpecialStartAt=T(r,8), SpecialEndAt=T(r,9) }); return Task.CompletedTask; });
    }

    private static string? N(NpgsqlDataReader r, int i) => r.IsDBNull(i) ? null : r.GetString(i);
    private static Guid? G(NpgsqlDataReader r, int i) => r.IsDBNull(i) ? null : r.GetGuid(i);
    private static decimal? D(NpgsqlDataReader r, int i) => r.IsDBNull(i) ? null : r.GetDecimal(i);
    private static DateTimeOffset? T(NpgsqlDataReader r, int i) => r.IsDBNull(i) ? null : r.GetFieldValue<DateTimeOffset>(i);

    public async Task<CatalogAggregate> CreateProductAsync(CreateProductRequestDto request, RequestContext ctx, CancellationToken ct)
    {
        if (request.Availabilities is null || request.Availabilities.Count == 0) throw new DomainException("AVAILABILITY_REQUIRED", "At least one product availability is required", 422);
        if (request.Descriptions is null || request.Descriptions.Count == 0) throw new DomainException("DESCRIPTION_REQUIRED", "At least one product description is required", 422);
        if (await ProductSkuExistsAsync(request.Sku, ctx, null, ct)) throw new DomainException("PRODUCT_SKU_CONFLICT", $"SKU '{request.Sku}' already exists in store '{ctx.StoreId}'", 409);
        var id = Guid.NewGuid(); var date = ParseDate(request.DateAvailable);
        await using var connection = await OpenAsync(dataSource, ct);
        await using var tx = await connection.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);
        try
        {
            await InsertProductAsync(connection, tx, id, ctx, request, date, ct);
            await InsertDescriptionsAsync(connection, tx, id, request.Descriptions, ct);
            foreach (var availability in request.Availabilities) await InsertAvailabilityAsync(connection, tx, id, null, ctx.StoreId, availability, ct);
            if (request.Categories is not null)
                foreach (var category in request.Categories) await AttachCategoryAsync(connection, tx, id, ParseOptionalGuid(category.Id), category.Code, ctx, ct);
            if (request.Variants is not null)
                foreach (var variant in request.Variants) await InsertVariantAsync(connection, tx, id, ctx, variant, ct);
            await InsertOutboxAsync(connection, tx, "ProductChanged.v1", id, ctx, new { productId = id, operation = "Created" }, ct);
            await tx.CommitAsync(ct);
        }
        catch (PostgresException ex) when (ex.SqlState == "23505")
        {
            await tx.RollbackAsync(ct); throw new DomainException("PRODUCT_SKU_CONFLICT", "SKU is already used in this store", 409);
        }
        catch { await tx.RollbackAsync(ct); throw; }
        return await FindProductAsync(id, ctx, "en", null, false, ct) ?? throw new DomainException("INTERNAL_ERROR", "Created product could not be reloaded", 500);
    }

    private static DateTimeOffset ParseDate(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return DateTimeOffset.UtcNow;
        if (!DateTimeOffset.TryParse(text, null, System.Globalization.DateTimeStyles.RoundtripKind, out var result))
            throw new DomainException("DATE_INVALID", "dateAvailable must be a valid ISO date", 422);
        return result;
    }

    private static Guid? ParseOptionalGuid(string? text) => string.IsNullOrWhiteSpace(text) ? null : Guid.Parse(text);

    private static async Task InsertProductAsync(NpgsqlConnection c, NpgsqlTransaction tx, Guid id, RequestContext ctx, CreateProductRequestDto r, DateTimeOffset date, CancellationToken ct)
    {
        await using var command = new NpgsqlCommand("""
            INSERT INTO catalog_product.product(id,tenant_id,store_id,sku,ref_sku,status,visible,available,can_be_purchased,date_available,manufacturer_code,product_type_code,tax_class_code,product_virtual,product_shippable,product_free,sort_order)
            VALUES(@id,@tenant,@store,@sku,@ref,'Active',@visible,@available,@purchase,@date,@manufacturer,@type,@tax,@virtual,@shippable,@free,@sort)
            """, c, tx);
        Add(command,"id",id); Add(command,"tenant",ctx.TenantId); Add(command,"store",ctx.StoreId); Add(command,"sku",r.Sku); Add(command,"ref",r.RefSku);
        Add(command,"visible",r.Visible ?? false); Add(command,"available",r.Availabilities.Any(a => a.Quantity > 0 && (a.Active ?? true)));
        Add(command,"purchase",r.CanBePurchased ?? true); Add(command,"date",date); Add(command,"manufacturer",r.ManufacturerCode); Add(command,"type",r.ProductTypeCode); Add(command,"tax",r.TaxClassCode);
        Add(command,"virtual",r.ProductVirtual ?? false); Add(command,"shippable",r.ProductShippable ?? false); Add(command,"free",r.ProductFree ?? false); Add(command,"sort",r.SortOrder ?? 0);
        await command.ExecuteNonQueryAsync(ct);
    }

    private static async Task InsertDescriptionsAsync(NpgsqlConnection c, NpgsqlTransaction tx, Guid id, IEnumerable<ProductDescriptionDto> descriptions, CancellationToken ct)
    {
        var values = descriptions.ToList(); var fallback = values.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x.Name));
        if (values.Any(x => string.IsNullOrWhiteSpace(x.Name) || string.IsNullOrWhiteSpace(x.FriendlyUrl)))
            throw new DomainException("DESCRIPTION_REQUIRED", "Every description requires a name and friendly URL", 422);
        foreach (var d in values)
        {
            await using var command = new NpgsqlCommand("""
                INSERT INTO catalog_product.product_description(product_id,language_code,name,friendly_url,description,highlights,title,keywords,meta_description)
                VALUES(@product,@language,@name,@url,@description,@highlights,@title,@keywords,@meta)
                """, c, tx);
            Add(command,"product",id); Add(command,"language",d.LanguageCode); Add(command,"name",string.IsNullOrWhiteSpace(d.Name) ? fallback?.Name : d.Name);
            Add(command,"url",string.IsNullOrWhiteSpace(d.FriendlyUrl) ? fallback?.FriendlyUrl : d.FriendlyUrl); Add(command,"description",d.Description); Add(command,"highlights",d.Highlights); Add(command,"title",d.Title); Add(command,"keywords",d.Keywords); Add(command,"meta",d.MetaDescription);
            await command.ExecuteNonQueryAsync(ct);
        }
    }

    private static async Task InsertAvailabilityAsync(NpgsqlConnection c, NpgsqlTransaction tx, Guid? product, Guid? variant, string store, AvailabilityInputDto a, CancellationToken ct)
    {
        await using var command = new NpgsqlCommand("INSERT INTO catalog_product.product_availability(product_id,variant_id,store_id,region_code,quantity,active) VALUES(@product,@variant,@store,@region,@quantity,@active)",c,tx);
        Add(command,"product",product); Add(command,"variant",variant); Add(command,"store",store); Add(command,"region",a.RegionCode); Add(command,"quantity",a.Quantity); Add(command,"active",a.Active ?? true); await command.ExecuteNonQueryAsync(ct);
    }

    private static async Task AttachCategoryAsync(NpgsqlConnection c, NpgsqlTransaction tx, Guid product, Guid? categoryId, string? code, RequestContext ctx, CancellationToken ct)
    {
        await using var command = new NpgsqlCommand("""
            INSERT INTO catalog_product.product_category(product_id,category_id)
            SELECT @product,id FROM catalog_product.category WHERE tenant_id=@tenant AND store_id=@store AND
              ((@id IS NOT NULL AND id=@id) OR (@id IS NULL AND code=@code))
            """, c, tx);
        Add(command,"product",product); Add(command,"id",categoryId); Add(command,"code",code); Add(command,"tenant",ctx.TenantId); Add(command,"store",ctx.StoreId);
        if (await command.ExecuteNonQueryAsync(ct) == 0) throw new DomainException("CATEGORY_SCOPE_INVALID", "Category does not belong to the current store", 422);
    }

    private static async Task<Guid> InsertVariantAsync(NpgsqlConnection c, NpgsqlTransaction tx, Guid product, RequestContext ctx, CreateVariantRequestDto v, CancellationToken ct)
    {
        var id = Guid.NewGuid();
        await using var command = new NpgsqlCommand("""
            INSERT INTO catalog_product.product_variant(id,product_id,store_id,sku,code,status,available,default_selection,date_available)
            VALUES(@id,@product,@store,@sku,@code,'Active',@available,@default,@date)
            """, c, tx);
        Add(command,"id",id); Add(command,"product",product); Add(command,"store",ctx.StoreId); Add(command,"sku",v.Sku); Add(command,"code",v.Code);
        Add(command,"available",v.Available ?? false); Add(command,"default",v.DefaultSelection ?? false); Add(command,"date",ParseDate(v.DateAvailable)); await command.ExecuteNonQueryAsync(ct);
        if (v.Availability is not null) await InsertAvailabilityAsync(c,tx,null,id,ctx.StoreId,v.Availability,ct);
        return id;
    }

    public async Task<ProductRecord> UpdateProductAsync(Guid id, UpdateProductRequestDto request, RequestContext ctx, CancellationToken ct)
    {
        var current = await FindProductAsync(id,ctx,"en",null,false,ct) ?? throw new DomainException("PRODUCT_NOT_FOUND","Product was not found in this store",404);
        if (request.Sku is not null && await ProductSkuExistsAsync(request.Sku,ctx,id,ct)) throw new DomainException("PRODUCT_SKU_CONFLICT","SKU is already used in this store",409);
        await using var c = await OpenAsync(dataSource,ct); await using var tx = await c.BeginTransactionAsync(ct);
        var date = request.DateAvailable is null ? current.Product.DateAvailable : ParseDate(request.DateAvailable);
        await using var cmd = new NpgsqlCommand("""
            UPDATE catalog_product.product SET sku=COALESCE(@sku,sku),ref_sku=COALESCE(@ref,ref_sku),
            visible=COALESCE(@visible,visible),available=COALESCE(@available,available),can_be_purchased=COALESCE(@purchase,can_be_purchased),
            date_available=@date,manufacturer_code=COALESCE(@manufacturer,manufacturer_code),product_type_code=COALESCE(@type,product_type_code),
            tax_class_code=COALESCE(@tax,tax_class_code),product_virtual=COALESCE(@virtual,product_virtual),
            product_shippable=COALESCE(@shippable,product_shippable),product_free=COALESCE(@free,product_free),
            sort_order=COALESCE(@sort,sort_order),updated_at=now(),version=version+1
            WHERE id=@id AND tenant_id=@tenant AND store_id=@store
            """,c,tx);
        Add(cmd,"id",id); Add(cmd,"tenant",ctx.TenantId); Add(cmd,"store",ctx.StoreId); Add(cmd,"sku",request.Sku); Add(cmd,"ref",request.RefSku);
        Add(cmd,"visible",request.Visible); Add(cmd,"available",request.Availabilities is null ? null : request.Availabilities.Any(a=>a.Quantity>a.Quantity*0 && (a.Active??true)));
        Add(cmd,"purchase",request.CanBePurchased); Add(cmd,"date",date); Add(cmd,"manufacturer",request.ManufacturerCode); Add(cmd,"type",request.ProductTypeCode); Add(cmd,"tax",request.TaxClassCode);
        Add(cmd,"virtual",request.ProductVirtual); Add(cmd,"shippable",request.ProductShippable); Add(cmd,"free",request.ProductFree); Add(cmd,"sort",request.SortOrder); await cmd.ExecuteNonQueryAsync(ct);
        if (request.Descriptions is not null && request.Descriptions.Count > 0) { await using var del = new NpgsqlCommand("DELETE FROM catalog_product.product_description WHERE product_id=@id",c,tx); Add(del,"id",id); await del.ExecuteNonQueryAsync(ct); await InsertDescriptionsAsync(c,tx,id,request.Descriptions,ct); }
        if (request.Availabilities is not null && request.Availabilities.Count > 0) { await using var del = new NpgsqlCommand("DELETE FROM catalog_product.product_availability WHERE product_id=@id",c,tx); Add(del,"id",id); await del.ExecuteNonQueryAsync(ct); foreach(var a in request.Availabilities) await InsertAvailabilityAsync(c,tx,id,null,ctx.StoreId,a,ct); }
        await InsertOutboxAsync(c,tx,"ProductChanged.v1",id,ctx,new { productId=id, operation="Updated" },ct); await tx.CommitAsync(ct);
        return (await FindProductAsync(id,ctx,"en",null,false,ct))!.Product;
    }

    public async Task<ProductRecord> UpdateVisibilityAsync(Guid id, UpdateVisibilityRequestDto request, RequestContext ctx, CancellationToken ct)
    {
        var date = ParseDate(request.DateAvailable);
        await using var c=await OpenAsync(dataSource,ct); await using var tx=await c.BeginTransactionAsync(ct); await using var cmd=new NpgsqlCommand("UPDATE catalog_product.product SET visible=@visible,can_be_purchased=@purchase,date_available=CASE WHEN @hasDate THEN @date ELSE date_available END,updated_at=now(),version=version+1 WHERE id=@id AND tenant_id=@tenant AND store_id=@store",c,tx);
        Add(cmd,"visible",request.Visible); Add(cmd,"purchase",request.CanBePurchased); Add(cmd,"hasDate",request.DateAvailable is not null); Add(cmd,"date",date); Add(cmd,"id",id);Add(cmd,"tenant",ctx.TenantId);Add(cmd,"store",ctx.StoreId);
        if(await cmd.ExecuteNonQueryAsync(ct)==0) throw new DomainException("PRODUCT_NOT_FOUND","Product was not found in this store",404);
        await InsertOutboxAsync(c,tx,"ProductChanged.v1",id,ctx,new {productId=id,operation="VisibilityChanged"},ct); await tx.CommitAsync(ct);
        return (await FindProductAsync(id,ctx,"en",null,false,ct))!.Product;
    }

    public async Task<DeletionResultDto> DeleteProductAsync(Guid id, RequestContext ctx, CancellationToken ct)
    {
        await using var c=await OpenAsync(dataSource,ct); await using var tx=await c.BeginTransactionAsync(ct);
        await using var exists=new NpgsqlCommand("SELECT EXISTS(SELECT 1 FROM catalog_product.product WHERE id=@id AND tenant_id=@tenant AND store_id=@store)",c,tx);Add(exists,"id",id);Add(exists,"tenant",ctx.TenantId);Add(exists,"store",ctx.StoreId);
        if(!(bool)(await exists.ExecuteScalarAsync(ct)??false)) throw new DomainException("PRODUCT_NOT_FOUND","Product was not found in this store",404);
        await using var activeReservation = new NpgsqlCommand("""
            SELECT EXISTS(SELECT 1 FROM catalog_product.inventory_reservation
                          WHERE product_id=@id AND store_id=@store AND state='Held')
            """,c,tx);
        Add(activeReservation,"id",id);Add(activeReservation,"store",ctx.StoreId);
        if((bool)(await activeReservation.ExecuteScalarAsync(ct)??false))
            throw new DomainException("ACTIVE_RESERVATION_EXISTS","Product cannot be deleted while an active reservation exists",409);
        var removed=0; foreach(var table in new[]{"product_relationship","product_category","product_image","product_attribute","product_price","product_availability","product_variant","product_description"})
        { await using var del=new NpgsqlCommand($"DELETE FROM catalog_product.{table} WHERE {(table=="product_relationship" ? "product_id=@id OR related_product_id=@id" : table=="product_price" ? "availability_id IN (SELECT id FROM catalog_product.product_availability WHERE product_id=@id)" : table=="product_variant" ? "product_id=@id" : "product_id=@id")}",c,tx);Add(del,"id",id);removed+=await del.ExecuteNonQueryAsync(ct); }
        await using var delProduct=new NpgsqlCommand("DELETE FROM catalog_product.product WHERE id=@id AND tenant_id=@tenant AND store_id=@store",c,tx);Add(delProduct,"id",id);Add(delProduct,"tenant",ctx.TenantId);Add(delProduct,"store",ctx.StoreId);await delProduct.ExecuteNonQueryAsync(ct);
        await InsertOutboxAsync(c,tx,"ProductChanged.v1",id,ctx,new {productId=id,operation="Deleted"},ct);await tx.CommitAsync(ct);
        return new DeletionResultDto{Id=id.ToString(),Status="Deleted",DependentsRemoved=removed,ProjectionEventPublished=true};
    }

    public async Task<List<AvailabilityRecord>> GetAvailabilityAsync(Guid productId, RequestContext ctx, CancellationToken ct)
    {
        var aggregate=await FindProductAsync(productId,ctx,"en",null,false,ct)??throw new DomainException("PRODUCT_NOT_FOUND","Product was not found in this store",404);
        return aggregate.Availabilities.Where(a=>a.ProductId==productId).ToList();
    }

    public async Task<List<AvailabilityRecord>> ReplaceAvailabilityAsync(Guid productId, ReplaceAvailabilityRequestDto request, RequestContext ctx, CancellationToken ct)
    {
        if(request.Items is null || request.Items.Count==0) throw new DomainException("AVAILABILITY_REQUIRED","At least one product availability is required",422);
        await using var c=await OpenAsync(dataSource,ct);await using var tx=await c.BeginTransactionAsync(ct);
        await using var check=new NpgsqlCommand("SELECT EXISTS(SELECT 1 FROM catalog_product.product WHERE id=@id AND tenant_id=@tenant AND store_id=@store)",c,tx);Add(check,"id",productId);Add(check,"tenant",ctx.TenantId);Add(check,"store",ctx.StoreId);if(!(bool)(await check.ExecuteScalarAsync(ct)??false))throw new DomainException("PRODUCT_NOT_FOUND","Product was not found in this store",404);
        await using(var del=new NpgsqlCommand("DELETE FROM catalog_product.product_availability WHERE product_id=@id",c,tx)){Add(del,"id",productId);await del.ExecuteNonQueryAsync(ct);}
        foreach(var a in request.Items)await InsertAvailabilityAsync(c,tx,productId,null,ctx.StoreId,a,ct);
        await InsertOutboxAsync(c,tx,"AvailabilityChanged.v1",productId,ctx,new{productId,operation="Replaced"},ct);await tx.CommitAsync(ct);return await GetAvailabilityAsync(productId,ctx,ct);
    }

    public async Task<ProductRecord> AttachCategoryAsync(Guid productId,Guid categoryId,bool attach,RequestContext ctx,CancellationToken ct)
    {
        await using var c=await OpenAsync(dataSource,ct);await using var tx=await c.BeginTransactionAsync(ct);
        await using var verify=new NpgsqlCommand("SELECT EXISTS(SELECT 1 FROM catalog_product.product WHERE id=@product AND tenant_id=@tenant AND store_id=@store) AND EXISTS(SELECT 1 FROM catalog_product.category WHERE id=@category AND tenant_id=@tenant AND store_id=@store)",c,tx);Add(verify,"product",productId);Add(verify,"category",categoryId);Add(verify,"tenant",ctx.TenantId);Add(verify,"store",ctx.StoreId);if(!(bool)(await verify.ExecuteScalarAsync(ct)??false))throw new DomainException("CATEGORY_SCOPE_INVALID","Product or category does not belong to this store",422);
        await using var cmd=new NpgsqlCommand(attach?"INSERT INTO catalog_product.product_category(product_id,category_id) VALUES(@product,@category) ON CONFLICT DO NOTHING":"DELETE FROM catalog_product.product_category WHERE product_id=@product AND category_id=@category",c,tx);Add(cmd,"product",productId);Add(cmd,"category",categoryId);await cmd.ExecuteNonQueryAsync(ct);await InsertOutboxAsync(c,tx,"ProductChanged.v1",productId,ctx,new{productId,operation=attach?"CategoryAttached":"CategoryDetached"},ct);await tx.CommitAsync(ct);return (await FindProductAsync(productId,ctx,"en",null,false,ct))!.Product;
    }

    public async Task<VariantRecord> CreateVariantAsync(Guid productId,CreateVariantRequestDto request,RequestContext ctx,CancellationToken ct)
    { if(await FindProductAsync(productId,ctx,"en",null,false,ct) is null)throw new DomainException("PRODUCT_NOT_FOUND","Product was not found in this store",404);if(await VariantSkuExistsAsync(productId,request.Sku,ctx,null,ct))throw new DomainException("VARIANT_SKU_CONFLICT","Variant SKU already exists for this product",409);await using var c=await OpenAsync(dataSource,ct);await using var tx=await c.BeginTransactionAsync(ct);var id=await InsertVariantAsync(c,tx,productId,ctx,request,ct);await InsertOutboxAsync(c,tx,"ProductChanged.v1",productId,ctx,new{productId,variantId=id,operation="VariantCreated"},ct);await tx.CommitAsync(ct);var a=await FindProductAsync(productId,ctx,"en",null,false,ct);return a!.Variants.First(v=>v.Id==id); }

    public async Task<VariantRecord> UpdateVariantAsync(Guid productId,Guid variantId,UpdateVariantRequestDto request,RequestContext ctx,CancellationToken ct)
    {var a=await FindProductAsync(productId,ctx,"en",null,false,ct)??throw new DomainException("PRODUCT_NOT_FOUND","Product was not found in this store",404);var v=a.Variants.FirstOrDefault(x=>x.Id==variantId)??throw new DomainException("VARIANT_NOT_FOUND","Variant was not found in this product",404);if(request.Sku is not null&&await VariantSkuExistsAsync(productId,request.Sku,ctx,variantId,ct))throw new DomainException("VARIANT_SKU_CONFLICT","Variant SKU already exists for this product",409);await using var c=await OpenAsync(dataSource,ct);await using var tx=await c.BeginTransactionAsync(ct);await using var cmd=new NpgsqlCommand("UPDATE catalog_product.product_variant SET sku=COALESCE(@sku,sku),code=COALESCE(@code,code),default_selection=COALESCE(@default,default_selection),available=COALESCE(@available,available),date_available=COALESCE(@date,date_available),updated_at=now() WHERE id=@id AND product_id=@product AND store_id=@store",c,tx);Add(cmd,"sku",request.Sku);Add(cmd,"code",request.Code);Add(cmd,"default",request.DefaultSelection);Add(cmd,"available",request.Available);Add(cmd,"date",request.DateAvailable is null?null:ParseDate(request.DateAvailable));Add(cmd,"id",variantId);Add(cmd,"product",productId);Add(cmd,"store",ctx.StoreId);await cmd.ExecuteNonQueryAsync(ct);await InsertOutboxAsync(c,tx,"ProductChanged.v1",productId,ctx,new{productId,variantId,operation="VariantUpdated"},ct);await tx.CommitAsync(ct);return (await FindProductAsync(productId,ctx,"en",null,false,ct))!.Variants.First(x=>x.Id==variantId);}

    public async Task<DeletionResultDto> DeleteVariantAsync(Guid productId,Guid variantId,RequestContext ctx,CancellationToken ct)
    {await using var c=await OpenAsync(dataSource,ct);await using var tx=await c.BeginTransactionAsync(ct);await using var cmd=new NpgsqlCommand("DELETE FROM catalog_product.product_variant WHERE id=@id AND product_id=@product AND store_id=@store",c,tx);Add(cmd,"id",variantId);Add(cmd,"product",productId);Add(cmd,"store",ctx.StoreId);if(await cmd.ExecuteNonQueryAsync(ct)==0)throw new DomainException("VARIANT_NOT_FOUND","Variant was not found in this product",404);await InsertOutboxAsync(c,tx,"ProductChanged.v1",productId,ctx,new{productId,variantId,operation="VariantDeleted"},ct);await tx.CommitAsync(ct);return new DeletionResultDto{Id=variantId.ToString(),Status="Deleted",DependentsRemoved=1,ProjectionEventPublished=true};}

    public async Task<PriceResponseDto> CalculatePriceAsync(Guid productId,CalculatePriceRequestDto request,RequestContext ctx,CancellationToken ct)
    {var a=await FindProductAsync(productId,ctx,"en",request.CountryCode,true,ct)??throw new DomainException("PRODUCT_NOT_FOUND","Product is not eligible in this store",404);var defaultVariant=a.Variants.FirstOrDefault(v=>v.DefaultSelection);var variantPrices=defaultVariant is null?[]:a.Prices.Where(p=>a.Availabilities.Any(x=>x.VariantId==defaultVariant.Id&&x.Id==p.AvailabilityId&&x.Active&&x.Quantity>x.ReservedQuantity&&(x.RegionCode=="*"||x.RegionCode.Equals(request.CountryCode??"*",StringComparison.OrdinalIgnoreCase)))).ToArray();var parentPrices=a.Prices.Where(p=>a.Availabilities.Any(x=>x.ProductId==productId&&x.Id==p.AvailabilityId&&x.Active&&x.Quantity>x.ReservedQuantity&&(x.RegionCode=="*"||x.RegionCode.Equals(request.CountryCode??"*",StringComparison.OrdinalIgnoreCase)))).ToArray();var basePrice=variantPrices.OrderByDescending(p=>a.Availabilities.First(x=>x.Id==p.AvailabilityId).RegionCode=="*").ThenByDescending(p=>p.DefaultPrice).FirstOrDefault()??parentPrices.OrderByDescending(p=>a.Availabilities.First(x=>x.Id==p.AvailabilityId).RegionCode=="*").ThenByDescending(p=>p.DefaultPrice).FirstOrDefault();if(basePrice is null)throw new DomainException("PRICE_UNAVAILABLE","No price exists for the variant or parent product",422);var now=DateTimeOffset.UtcNow;var p=DtoMapper.Price(basePrice,now);var amount=p.FinalAmount??p.Amount;var matched=0m;foreach(var selection in request.Selections){var price=await OptionAdjustmentAsync(productId,Guid.Parse(selection.OptionId),Guid.Parse(selection.ValueId),ctx,ct);if(price is null)throw new DomainException("OPTION_VALUE_NOT_ALLOWED","The selected option value is not available for this product",422);if(price>0){matched+=price.Value;amount+=price.Value;}}return new PriceResponseDto{FinalAmount=amount,OriginalAmount=p.Amount+matched,CurrencyCode=request.CurrencyCode??p.CurrencyCode,PriceSource=variantPrices.Contains(basePrice)?"DefaultVariant":"ParentProduct",Discounted=p.Discounted,MatchedSelections=request.Selections.Count};}

    private async Task<decimal?> OptionAdjustmentAsync(Guid product,Guid option,Guid value,RequestContext ctx,CancellationToken ct){await using var c=await OpenAsync(dataSource,ct);await using var cmd=new NpgsqlCommand("SELECT price_adjustment FROM catalog_product.product_attribute a JOIN catalog_product.product_option o ON o.id=a.option_id JOIN catalog_product.product_option_value v ON v.id=a.option_value_id WHERE a.product_id=@product AND a.option_id=@option AND a.option_value_id=@value AND o.store_id=@store AND v.store_id=@store",c);Add(cmd,"product",product);Add(cmd,"option",option);Add(cmd,"value",value);Add(cmd,"store",ctx.StoreId);var x=await cmd.ExecuteScalarAsync(ct);return x is null?null:Convert.ToDecimal(x);}

    public async Task<ReservationRecord> CreateReservationAsync(Guid productId,CreateReservationRequestDto request,RequestContext ctx,CancellationToken ct)
    {if(!DateTimeOffset.TryParse(request.ExpiresAt,null,System.Globalization.DateTimeStyles.RoundtripKind,out var expiry)||expiry<=DateTimeOffset.UtcNow)throw new DomainException("RESERVATION_EXPIRY_INVALID","expiresAt must be in the future",422);var hash=Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(request))));await using var c=await OpenAsync(dataSource,ct);await using var tx=await c.BeginTransactionAsync(IsolationLevel.Serializable,ct);await using(var existing=new NpgsqlCommand("SELECT id,request_hash,quantity,state,expires_at,product_id,variant_id,availability_id,committed_at,released_at FROM catalog_product.inventory_reservation WHERE tenant_id=@tenant AND store_id=@store AND reservation_key=@key FOR UPDATE",c,tx)){Add(existing,"tenant",ctx.TenantId);Add(existing,"store",ctx.StoreId);Add(existing,"key",request.ReservationKey);await using var r=await existing.ExecuteReaderAsync(ct);if(await r.ReadAsync(ct)){var oldHash=r.GetString(1);var original=ReadReservation(r);await r.CloseAsync();if(oldHash!=hash)throw new DomainException("IDEMPOTENCY_KEY_REUSED","Reservation key was previously used with different contents",409);await tx.CommitAsync(ct);original.ReservationKey=request.ReservationKey;original.TenantId=ctx.TenantId;original.StoreId=ctx.StoreId;return original;}}
        var availability=await SelectAvailabilityAsync(c,tx,productId,request,ctx,ct);if(availability is null)throw new DomainException("AVAILABILITY_NOT_FOUND","Availability was not found in this store",404);if(availability.Value.Quantity-availability.Value.Reserved<request.Quantity)throw new DomainException("INSUFFICIENT_AVAILABILITY","Requested quantity is not available",409);await using(var update=new NpgsqlCommand("UPDATE catalog_product.product_availability SET reserved_quantity=reserved_quantity+@quantity,version=version+1,updated_at=now() WHERE id=@id",c,tx)){Add(update,"quantity",request.Quantity);Add(update,"id",availability.Value.Id);await update.ExecuteNonQueryAsync(ct);}var id=Guid.NewGuid();await using(var insert=new NpgsqlCommand("INSERT INTO catalog_product.inventory_reservation(id,tenant_id,store_id,product_id,variant_id,availability_id,reservation_key,request_hash,quantity,state,expires_at) VALUES(@id,@tenant,@store,@product,@variant,@availability,@key,@hash,@quantity,'Held',@expires)",c,tx)){Add(insert,"id",id);Add(insert,"tenant",ctx.TenantId);Add(insert,"store",ctx.StoreId);Add(insert,"product",productId);Add(insert,"variant",request.VariantId is null?null:Guid.Parse(request.VariantId));Add(insert,"availability",availability.Value.Id);Add(insert,"key",request.ReservationKey);Add(insert,"hash",hash);Add(insert,"quantity",request.Quantity);Add(insert,"expires",expiry);await insert.ExecuteNonQueryAsync(ct);}await InsertOutboxAsync(c,tx,"AvailabilityChanged.v1",productId,ctx,new{productId,reservationId=id,operation="Reserved"},ct);await InsertOutboxAsync(c,tx,"InventoryReservationChanged.v1",id,ctx,new{reservationId=id,state="Held"},ct);await tx.CommitAsync(ct);return new ReservationRecord{Id=id,TenantId=ctx.TenantId,StoreId=ctx.StoreId,ProductId=productId,VariantId=request.VariantId is null?null:Guid.Parse(request.VariantId),AvailabilityId=availability.Value.Id,ReservationKey=request.ReservationKey,Quantity=request.Quantity,ExpiresAt=expiry};}

    private static ReservationRecord ReadReservation(NpgsqlDataReader r)=>new(){Id=r.GetGuid(0),Quantity=r.GetInt32(2),State=r.GetString(3),ExpiresAt=r.GetFieldValue<DateTimeOffset>(4),ProductId=r.IsDBNull(5)?null:r.GetGuid(5),VariantId=r.IsDBNull(6)?null:r.GetGuid(6),AvailabilityId=r.GetGuid(7),CommittedAt=r.IsDBNull(8)?null:r.GetFieldValue<DateTimeOffset>(8),ReleasedAt=r.IsDBNull(9)?null:r.GetFieldValue<DateTimeOffset>(9),ReservationKey=""};
    private static async Task<(Guid Id,int Quantity,int Reserved)?> SelectAvailabilityAsync(NpgsqlConnection c,NpgsqlTransaction tx,Guid product,CreateReservationRequestDto request,RequestContext ctx,CancellationToken ct){var sql="SELECT id,quantity,reserved_quantity FROM catalog_product.product_availability WHERE store_id=@store AND "+(request.AvailabilityId is not null?"id=@id":"product_id=@product AND region_code=@region")+" AND active ORDER BY (region_code='*') DESC,id LIMIT 1 FOR UPDATE";await using var cmd=new NpgsqlCommand(sql,c,tx);Add(cmd,"store",ctx.StoreId);Add(cmd,"id",request.AvailabilityId is null?null:Guid.Parse(request.AvailabilityId));Add(cmd,"product",product);Add(cmd,"region",request.RegionCode??"*");await using var r=await cmd.ExecuteReaderAsync(ct);return await r.ReadAsync(ct)?(r.GetGuid(0),r.GetInt32(1),r.GetInt32(2)):null;}

    public async Task<ReservationRecord> TransitionReservationAsync(Guid id,bool commit,RequestContext ctx,CancellationToken ct)
    {await using var c=await OpenAsync(dataSource,ct);await using var tx=await c.BeginTransactionAsync(IsolationLevel.Serializable,ct);await using var read=new NpgsqlCommand("SELECT id,product_id,variant_id,availability_id,reservation_key,quantity,state,expires_at,committed_at,released_at FROM catalog_product.inventory_reservation WHERE id=@id AND tenant_id=@tenant AND store_id=@store FOR UPDATE",c,tx);Add(read,"id",id);Add(read,"tenant",ctx.TenantId);Add(read,"store",ctx.StoreId);await using var r=await read.ExecuteReaderAsync(ct);if(!await r.ReadAsync(ct))throw new DomainException("RESERVATION_NOT_FOUND","Reservation was not found in this store",404);var state=r.GetString(6);var reservation=new ReservationRecord{Id=r.GetGuid(0),ProductId=r.IsDBNull(1)?null:r.GetGuid(1),VariantId=r.IsDBNull(2)?null:r.GetGuid(2),AvailabilityId=r.GetGuid(3),ReservationKey=r.GetString(4),Quantity=r.GetInt32(5),State=state,ExpiresAt=r.GetFieldValue<DateTimeOffset>(7),CommittedAt=r.IsDBNull(8)?null:r.GetFieldValue<DateTimeOffset>(8),ReleasedAt=r.IsDBNull(9)?null:r.GetFieldValue<DateTimeOffset>(9)};await r.CloseAsync();if(state!="Held")throw new DomainException("RESERVATION_TERMINAL","Reservation has already reached a terminal state",409);if(reservation.ExpiresAt<=DateTimeOffset.UtcNow)throw new DomainException("RESERVATION_EXPIRED","Reservation has expired",409);var next=commit?"Committed":"Released";await using(var update=new NpgsqlCommand("UPDATE catalog_product.inventory_reservation SET state=@state,committed_at=CASE WHEN @commit THEN now() ELSE committed_at END,released_at=CASE WHEN @commit THEN released_at ELSE now() END,updated_at=now() WHERE id=@id",c,tx)){Add(update,"state",next);Add(update,"commit",commit);Add(update,"id",id);await update.ExecuteNonQueryAsync(ct);}if(!commit){await using var stock=new NpgsqlCommand("UPDATE catalog_product.product_availability SET reserved_quantity=GREATEST(0,reserved_quantity-@quantity),version=version+1 WHERE id=@id",c,tx);Add(stock,"quantity",reservation.Quantity);Add(stock,"id",reservation.AvailabilityId);await stock.ExecuteNonQueryAsync(ct);}await InsertOutboxAsync(c,tx,"InventoryReservationChanged.v1",id,ctx,new{reservationId=id,state=next},ct);if(!commit)await InsertOutboxAsync(c,tx,"InventoryReservationReleased.v1",id,ctx,new{reservationId=id},ct);await tx.CommitAsync(ct);reservation.State=next;if(commit)reservation.CommittedAt=DateTimeOffset.UtcNow;else reservation.ReleasedAt=DateTimeOffset.UtcNow;return reservation;}

    private static async Task InsertOutboxAsync(NpgsqlConnection c,NpgsqlTransaction tx,string eventType,Guid aggregate,RequestContext ctx,object payload,CancellationToken ct){var id=Guid.NewGuid();var envelope=JsonSerializer.Serialize(new{eventId=id,eventType,eventVersion=1,occurredAt=DateTimeOffset.UtcNow,tenantId=ctx.TenantId,storeId=ctx.StoreId,correlationId=ctx.CorrelationId,aggregateId=aggregate,payload});await using var cmd=new NpgsqlCommand("INSERT INTO catalog_product.event_outbox(id,event_type,tenant_id,store_id,correlation_id,payload,occurred_at) VALUES(@id,@type,@tenant,@store,@correlation,@payload::jsonb,now())",c,tx);Add(cmd,"id",id);Add(cmd,"type",eventType);Add(cmd,"tenant",ctx.TenantId);Add(cmd,"store",ctx.StoreId);Add(cmd,"correlation",ctx.CorrelationId);Add(cmd,"payload",envelope);await cmd.ExecuteNonQueryAsync(ct);}
    public async Task MarkEventPublishedAsync(Guid id,CancellationToken ct){await using var c=await OpenAsync(dataSource,ct);await using var cmd=new NpgsqlCommand("UPDATE catalog_product.event_outbox SET published_at=now() WHERE id=@id",c);Add(cmd,"id",id);await cmd.ExecuteNonQueryAsync(ct);}

    public async Task<IReadOnlyList<OutboxMessage>> PendingEventsAsync(RequestContext ctx, CancellationToken ct)
    {
        await using var c = await OpenAsync(dataSource, ct);
        await using var cmd = new NpgsqlCommand("""
            SELECT id,event_type,payload::text FROM catalog_product.event_outbox
            WHERE tenant_id=@tenant AND store_id=@store AND published_at IS NULL
            ORDER BY occurred_at,id
            """, c);
        Add(cmd, "tenant", ctx.TenantId); Add(cmd, "store", ctx.StoreId);
        var result = new List<OutboxMessage>();
        await using var r = await cmd.ExecuteReaderAsync(ct);
        while (await r.ReadAsync(ct))
            result.Add(new OutboxMessage(r.GetGuid(0), r.GetString(1), r.GetString(2), ctx));
        return result;
    }

    public async Task<CategoryRecord?> FindCategoryAsync(Guid id, RequestContext ctx, string language, CancellationToken ct)
    {
        await using var c=await OpenAsync(dataSource,ct);
        return await ReadCategoryAsync(c,null,id,ctx,language,ct);
    }

    public async Task<CategoryRecord?> FindCategoryBySlugAsync(string slug, RequestContext ctx, string language, CancellationToken ct)
    {
        await using var c=await OpenAsync(dataSource,ct);
        await using var cmd=new NpgsqlCommand("""
            SELECT c.id FROM catalog_product.category c
            JOIN catalog_product.category_description d ON d.category_id=c.id
            WHERE c.tenant_id=@tenant AND c.store_id=@store AND d.friendly_url=@slug AND d.language_code=@language
            """,c);
        Add(cmd,"tenant",ctx.TenantId);Add(cmd,"store",ctx.StoreId);Add(cmd,"slug",slug);Add(cmd,"language",language);
        var value=await cmd.ExecuteScalarAsync(ct);
        return value is Guid id?await ReadCategoryAsync(c,null,id,ctx,language,ct):null;
    }

    public async Task<(List<CategoryRecord> Items,int Total)> ListCategoriesAsync(RequestContext ctx,int page,int pageSize,string language,string? name,bool? visible,bool? featured,CancellationToken ct)
    {
        await using var c=await OpenAsync(dataSource,ct);
        var filter="FROM catalog_product.category x LEFT JOIN catalog_product.category_description d ON d.category_id=x.id AND d.language_code=@language WHERE x.tenant_id=@tenant AND x.store_id=@store AND x.parent_id IS NULL";
        if(name is not null)filter+=" AND d.name ILIKE @name";if(visible.HasValue)filter+=" AND x.visible=@visible";if(featured.HasValue)filter+=" AND x.featured=@featured";
        await using var count=new NpgsqlCommand("SELECT COUNT(*) "+filter,c);Add(count,"tenant",ctx.TenantId);Add(count,"store",ctx.StoreId);Add(count,"language",language);Add(count,"name",name is null?null:$"%{name}%");Add(count,"visible",visible);Add(count,"featured",featured);var total=Convert.ToInt32(await count.ExecuteScalarAsync(ct));
        await using var ids=new NpgsqlCommand("SELECT x.id "+filter+" ORDER BY x.sort_order,x.id OFFSET @offset LIMIT @limit",c);Add(ids,"tenant",ctx.TenantId);Add(ids,"store",ctx.StoreId);Add(ids,"language",language);Add(ids,"name",name is null?null:$"%{name}%");Add(ids,"visible",visible);Add(ids,"featured",featured);Add(ids,"offset",page*pageSize);Add(ids,"limit",pageSize);var categoryIds=new List<Guid>();await using(var r=await ids.ExecuteReaderAsync(ct)){while(await r.ReadAsync(ct))categoryIds.Add(r.GetGuid(0));}var result=new List<CategoryRecord>();foreach(var id in categoryIds){var item=await ReadCategoryAsync(c,null,id,ctx,language,ct);if(item is not null)result.Add(item);}return(result,total);
    }

    private static async Task<CategoryRecord?> ReadCategoryAsync(NpgsqlConnection c,NpgsqlTransaction? tx,Guid id,RequestContext ctx,string language,CancellationToken ct)
    {
        await using var cmd=new NpgsqlCommand("SELECT id,tenant_id,store_id,code,parent_id,category_image_uri,sort_order,status,visible,featured,depth,lineage FROM catalog_product.category WHERE id=@id AND tenant_id=@tenant AND store_id=@store",c,tx);Add(cmd,"id",id);Add(cmd,"tenant",ctx.TenantId);Add(cmd,"store",ctx.StoreId);CategoryRecord? category=null;await using(var r=await cmd.ExecuteReaderAsync(ct)){if(await r.ReadAsync(ct))category=new(){Id=r.GetGuid(0),TenantId=r.GetString(1),StoreId=r.GetString(2),Code=r.GetString(3),ParentId=G(r,4),CategoryImageUri=N(r,5),SortOrder=r.GetInt32(6),Status=r.GetString(7),Visible=r.GetBoolean(8),Featured=r.GetBoolean(9),Depth=r.GetInt32(10),Lineage=r.GetString(11)};}if(category is null)return null;
        return category;
    }

    public async Task<CategoryDto> MapCategoryAsync(CategoryRecord category,string language,RequestContext ctx,CancellationToken ct)
    {
        await using var c=await OpenAsync(dataSource,ct);
        var dto=new CategoryDto{Id=category.Id.ToString(),StoreId=category.StoreId,Code=category.Code,ParentId=category.ParentId?.ToString(),Status=category.Status,Visible=category.Visible,Featured=category.Featured,SortOrder=category.SortOrder,Depth=category.Depth,Lineage=category.Lineage};
        await using(var d=new NpgsqlCommand("SELECT language_code,name,friendly_url,description,title,meta_description FROM catalog_product.category_description WHERE category_id=@id ORDER BY (language_code=@language) DESC,language_code",c)){Add(d,"id",category.Id);Add(d,"language",language);await using var r=await d.ExecuteReaderAsync(ct);while(await r.ReadAsync(ct))dto.Descriptions.Add(new CategoryDescriptionDto{LanguageCode=r.GetString(0),Name=r.GetString(1),FriendlyUrl=r.GetString(2),Description=N(r,3),Title=N(r,4),MetaDescription=N(r,5)});}
        await using(var children=new NpgsqlCommand("SELECT id,code FROM catalog_product.category WHERE tenant_id=@tenant AND store_id=@store AND parent_id=@id ORDER BY sort_order,id",c)){Add(children,"tenant",ctx.TenantId);Add(children,"store",ctx.StoreId);Add(children,"id",category.Id);await using var r=await children.ExecuteReaderAsync(ct);while(await r.ReadAsync(ct))dto.Children!.Add(new CategoryReferenceDto{Id=r.GetGuid(0).ToString(),Code=r.GetString(1)});}
        return dto;
    }

    public async Task<CategoryDto> CreateCategoryAsync(CreateCategoryRequestDto request,RequestContext ctx,CancellationToken ct)
    {
        if(request.Descriptions is null||request.Descriptions.Count==0)throw new DomainException("DESCRIPTION_REQUIRED","At least one category description is required",422);
        if(await CategoryCodeExistsAsync(request.Code,ctx,null,ct))throw new DomainException("CATEGORY_CODE_CONFLICT","Category code is already used in this store",409);
        Guid? parent=ParseOptionalGuid(request.ParentId);var depth=0;var lineage="/";await using var c=await OpenAsync(dataSource,ct);await using var tx=await c.BeginTransactionAsync(ct);
        if(parent.HasValue){await using var p=new NpgsqlCommand("SELECT depth,lineage FROM catalog_product.category WHERE id=@id AND tenant_id=@tenant AND store_id=@store",c,tx);Add(p,"id",parent);Add(p,"tenant",ctx.TenantId);Add(p,"store",ctx.StoreId);await using var r=await p.ExecuteReaderAsync(ct);if(!await r.ReadAsync(ct))throw new DomainException("PARENT_CATEGORY_NOT_FOUND","Parent category does not exist in this store",422);depth=r.GetInt32(0)+1;lineage=r.GetString(1);}
        var id=Guid.NewGuid();lineage=$"{lineage.TrimEnd('/')}/{id}/";await using(var insert=new NpgsqlCommand("INSERT INTO catalog_product.category(id,tenant_id,store_id,code,parent_id,sort_order,status,visible,featured,depth,lineage) VALUES(@id,@tenant,@store,@code,@parent,@sort,'Active',@visible,@featured,@depth,@lineage)",c,tx)){Add(insert,"id",id);Add(insert,"tenant",ctx.TenantId);Add(insert,"store",ctx.StoreId);Add(insert,"code",request.Code);Add(insert,"parent",parent);Add(insert,"sort",request.SortOrder??0);Add(insert,"visible",request.Visible??false);Add(insert,"featured",request.Featured??false);Add(insert,"depth",depth);Add(insert,"lineage",lineage);await insert.ExecuteNonQueryAsync(ct);}
        foreach(var d in request.Descriptions)await InsertCategoryDescriptionAsync(c,tx,id,d,ct);await InsertOutboxAsync(c,tx,"CategoryChanged.v1",id,ctx,new{categoryId=id,operation="Created"},ct);await tx.CommitAsync(ct);return await MapCategoryAsync((await FindCategoryAsync(id,ctx,"en",ct))!, "en",ctx,ct);
    }
    private static async Task InsertCategoryDescriptionAsync(NpgsqlConnection c,NpgsqlTransaction tx,Guid id,CategoryDescriptionDto d,CancellationToken ct){if(string.IsNullOrWhiteSpace(d.Name)||string.IsNullOrWhiteSpace(d.FriendlyUrl))throw new DomainException("CATEGORY_FORM_INVALID","Category name and friendly URL are required",422);await using var cmd=new NpgsqlCommand("INSERT INTO catalog_product.category_description(category_id,language_code,name,friendly_url,description,title,meta_description) VALUES(@id,@language,@name,@url,@description,@title,@meta)",c,tx);Add(cmd,"id",id);Add(cmd,"language",d.LanguageCode);Add(cmd,"name",d.Name);Add(cmd,"url",d.FriendlyUrl);Add(cmd,"description",d.Description);Add(cmd,"title",d.Title);Add(cmd,"meta",d.MetaDescription);await cmd.ExecuteNonQueryAsync(ct);}

    public async Task<CategoryDto> UpdateCategoryAsync(Guid id,UpdateCategoryRequestDto request,RequestContext ctx,CancellationToken ct)
    {var current=await FindCategoryAsync(id,ctx,"en",ct)??throw new DomainException("CATEGORY_NOT_FOUND","Category was not found in this store",404);if(request.Code is not null&&await CategoryCodeExistsAsync(request.Code,ctx,id,ct))throw new DomainException("CATEGORY_CODE_CONFLICT","Category code is already used in this store",409);await using var c=await OpenAsync(dataSource,ct);await using var tx=await c.BeginTransactionAsync(ct);var parent=request.ParentId is null?current.ParentId:ParseOptionalGuid(request.ParentId);var depth=current.Depth;var lineage=current.Lineage;if(parent!=current.ParentId){if(parent is null){depth=0;lineage=$"/{id}/";}else{await using var p=new NpgsqlCommand("SELECT depth,lineage FROM catalog_product.category WHERE id=@id AND tenant_id=@tenant AND store_id=@store",c,tx);Add(p,"id",parent);Add(p,"tenant",ctx.TenantId);Add(p,"store",ctx.StoreId);await using var r=await p.ExecuteReaderAsync(ct);if(!await r.ReadAsync(ct))throw new DomainException("PARENT_CATEGORY_NOT_FOUND","Parent category does not exist in this store",422);depth=r.GetInt32(0)+1;lineage=$"{r.GetString(1).TrimEnd('/')}/{id}/";}}await using(var u=new NpgsqlCommand("UPDATE catalog_product.category SET code=COALESCE(@code,code),parent_id=@parent,visible=COALESCE(@visible,visible),featured=COALESCE(@featured,featured),sort_order=COALESCE(@sort,sort_order),depth=@depth,lineage=@lineage,status=CASE WHEN COALESCE(@visible,visible) THEN 'Active' ELSE 'Hidden' END,updated_at=now() WHERE id=@id AND tenant_id=@tenant AND store_id=@store",c,tx)){Add(u,"code",request.Code);Add(u,"parent",parent);Add(u,"visible",request.Visible);Add(u,"featured",request.Featured);Add(u,"sort",request.SortOrder);Add(u,"depth",depth);Add(u,"lineage",lineage);Add(u,"id",id);Add(u,"tenant",ctx.TenantId);Add(u,"store",ctx.StoreId);await u.ExecuteNonQueryAsync(ct);}if(request.Descriptions is not null&&request.Descriptions.Count>0){await using var d=new NpgsqlCommand("DELETE FROM catalog_product.category_description WHERE category_id=@id",c,tx);Add(d,"id",id);await d.ExecuteNonQueryAsync(ct);foreach(var x in request.Descriptions)await InsertCategoryDescriptionAsync(c,tx,id,x,ct);}await InsertOutboxAsync(c,tx,"CategoryChanged.v1",id,ctx,new{categoryId=id,operation="Updated"},ct);await tx.CommitAsync(ct);return await MapCategoryAsync((await FindCategoryAsync(id,ctx,"en",ct))!, "en",ctx,ct);}

    public async Task<CategoryDto> UpdateCategoryVisibilityAsync(Guid id,UpdateCategoryVisibilityRequestDto request,RequestContext ctx,CancellationToken ct)
    {await using var c=await OpenAsync(dataSource,ct);await using var tx=await c.BeginTransactionAsync(ct);await using var cmd=new NpgsqlCommand("UPDATE catalog_product.category SET visible=@visible,status=CASE WHEN @visible THEN 'Active' ELSE 'Hidden' END,updated_at=now() WHERE id=@id AND tenant_id=@tenant AND store_id=@store",c,tx);Add(cmd,"visible",request.Visible);Add(cmd,"id",id);Add(cmd,"tenant",ctx.TenantId);Add(cmd,"store",ctx.StoreId);if(await cmd.ExecuteNonQueryAsync(ct)==0)throw new DomainException("CATEGORY_NOT_FOUND","Category was not found in this store",404);await InsertOutboxAsync(c,tx,"CategoryChanged.v1",id,ctx,new{categoryId=id,operation="VisibilityChanged"},ct);await tx.CommitAsync(ct);return await MapCategoryAsync((await FindCategoryAsync(id,ctx,"en",ct))!,"en",ctx,ct);}

    public async Task<CategoryDto> MoveCategoryAsync(Guid id,Guid parentId,RequestContext ctx,CancellationToken ct)
    {var current=await FindCategoryAsync(id,ctx,"en",ct)??throw new DomainException("CATEGORY_NOT_FOUND","Category was not found in this store",404);if(id==parentId||current.Lineage.Contains($"/{parentId}/"))throw new DomainException("CATEGORY_CYCLE","A category cannot be moved below itself or its descendants",409);await using var c=await OpenAsync(dataSource,ct);await using var tx=await c.BeginTransactionAsync(IsolationLevel.Serializable,ct);await using var p=new NpgsqlCommand("SELECT depth,lineage FROM catalog_product.category WHERE id=@id AND tenant_id=@tenant AND store_id=@store FOR UPDATE",c,tx);Add(p,"id",parentId);Add(p,"tenant",ctx.TenantId);Add(p,"store",ctx.StoreId);await using var r=await p.ExecuteReaderAsync(ct);if(!await r.ReadAsync(ct))throw new DomainException("PARENT_STORE_MISMATCH","Parent category belongs to another store",422);var newDepth=r.GetInt32(0)+1;var newLineage=$"{r.GetString(1).TrimEnd('/')}/{id}/";await r.CloseAsync();await using(var u=new NpgsqlCommand("UPDATE catalog_product.category SET parent_id=@parent,depth=@depth,lineage=@lineage,updated_at=now() WHERE id=@id AND tenant_id=@tenant AND store_id=@store",c,tx)){Add(u,"parent",parentId);Add(u,"depth",newDepth);Add(u,"lineage",newLineage);Add(u,"id",id);Add(u,"tenant",ctx.TenantId);Add(u,"store",ctx.StoreId);await u.ExecuteNonQueryAsync(ct);}await using(var descendants=new NpgsqlCommand("UPDATE catalog_product.category child SET depth=@depth+((length(child.lineage)-length(replace(child.lineage,'/','')))-(length(@old)-length(replace(@old,'/','')))),lineage=@new||substring(child.lineage from length(@old)+1),updated_at=now() WHERE child.store_id=@store AND child.lineage LIKE @old||'%' AND child.id<>@id",c,tx)){Add(descendants,"depth",newDepth);Add(descendants,"old",current.Lineage);Add(descendants,"new",newLineage);Add(descendants,"store",ctx.StoreId);Add(descendants,"id",id);await descendants.ExecuteNonQueryAsync(ct);}await InsertOutboxAsync(c,tx,"CategoryChanged.v1",id,ctx,new{categoryId=id,operation="Moved"},ct);await tx.CommitAsync(ct);return await MapCategoryAsync((await FindCategoryAsync(id,ctx,"en",ct))!,"en",ctx,ct);}

    public async Task<CategoryDeletionResultDto> DeleteCategoryAsync(Guid id,string policy,RequestContext ctx,CancellationToken ct)
    {var category=await FindCategoryAsync(id,ctx,"en",ct)??throw new DomainException("CATEGORY_NOT_FOUND","Category was not found in this store",404);if(!new[]{"Detach","Delete","Reject"}.Contains(policy,StringComparer.OrdinalIgnoreCase))throw new DomainException("CATEGORY_DELETE_BLOCKED","Unknown orphan product policy",409);await using var c=await OpenAsync(dataSource,ct);await using var tx=await c.BeginTransactionAsync(IsolationLevel.Serializable,ct);await using var affected=new NpgsqlCommand("SELECT DISTINCT pc.product_id FROM catalog_product.product_category pc JOIN catalog_product.category x ON x.id=pc.category_id WHERE x.store_id=@store AND x.lineage LIKE @lineage||'%'",c,tx);Add(affected,"store",ctx.StoreId);Add(affected,"lineage",category.Lineage);var products=new List<Guid>();await using(var r=await affected.ExecuteReaderAsync(ct)){while(await r.ReadAsync(ct))products.Add(r.GetGuid(0));}if(policy.Equals("Reject",StringComparison.OrdinalIgnoreCase)&&products.Count>0)throw new DomainException("CATEGORY_DELETE_BLOCKED","Deletion would orphan protected products",409);var cats=new NpgsqlCommand("DELETE FROM catalog_product.category WHERE tenant_id=@tenant AND store_id=@store AND lineage LIKE @lineage||'%' ",c,tx);Add(cats,"tenant",ctx.TenantId);Add(cats,"store",ctx.StoreId);Add(cats,"lineage",category.Lineage);var count=await cats.ExecuteNonQueryAsync(ct);if(policy.Equals("Delete",StringComparison.OrdinalIgnoreCase))foreach(var product in products){await using var d=new NpgsqlCommand("DELETE FROM catalog_product.product WHERE id=@id AND tenant_id=@tenant AND store_id=@store",c,tx);Add(d,"id",product);Add(d,"tenant",ctx.TenantId);Add(d,"store",ctx.StoreId);await d.ExecuteNonQueryAsync(ct);}else foreach(var product in products){await using var d=new NpgsqlCommand("DELETE FROM catalog_product.product_category WHERE product_id=@id AND category_id NOT IN(SELECT id FROM catalog_product.category)",c,tx);Add(d,"id",product);await d.ExecuteNonQueryAsync(ct);}await InsertOutboxAsync(c,tx,"CategoryChanged.v1",id,ctx,new{categoryId=id,operation="Deleted"},ct);await tx.CommitAsync(ct);return new CategoryDeletionResultDto{CategoryId=id.ToString(),Status="Deleted",DeletedCategoryCount=count,DetachedProductCount=policy.Equals("Detach",StringComparison.OrdinalIgnoreCase)?products.Count:0,DeletedProductCount=policy.Equals("Delete",StringComparison.OrdinalIgnoreCase)?products.Count:0};}

    public async Task<MediaRecord> AddMediaAsync(Guid productId,string fileName,string? externalUrl,bool defaultImage,RequestContext ctx,CancellationToken ct)
    {if(await FindProductAsync(productId,ctx,"en",null,false,ct) is null)throw new DomainException("PRODUCT_NOT_FOUND","Product was not found in this store",404);var id=Guid.NewGuid();await using var c=await OpenAsync(dataSource,ct);await using var tx=await c.BeginTransactionAsync(ct);await using(var cmd=new NpgsqlCommand("INSERT INTO catalog_product.product_image(id,product_id,image_type,file_name,external_url,original_uri,default_image,media_status) VALUES(@id,@product,@type,@file,@url,@original,@default,'Ready')",c,tx)){Add(cmd,"id",id);Add(cmd,"product",productId);Add(cmd,"type",externalUrl is null?"Binary":"ExternalUrl");Add(cmd,"file",fileName);Add(cmd,"url",externalUrl);Add(cmd,"original",externalUrl is null?$"media://{ctx.StoreId}/{id}/{fileName}":externalUrl);Add(cmd,"default",defaultImage);await cmd.ExecuteNonQueryAsync(ct);}await InsertOutboxAsync(c,tx,"MediaChanged.v1",productId,ctx,new{productId,mediaId=id,operation="Created"},ct);await tx.CommitAsync(ct);return new MediaRecord{Id=id,ProductId=productId,ImageType=externalUrl is null?"Binary":"ExternalUrl",FileName=fileName,OriginalUri=externalUrl is null?$"media://{ctx.StoreId}/{id}/{fileName}":externalUrl,ExternalUrl=externalUrl,DefaultImage=defaultImage,MediaStatus="Ready"};}
    public async Task<DeletionResultDto> DeleteMediaAsync(Guid productId,Guid mediaId,RequestContext ctx,CancellationToken ct)
    {await using var c=await OpenAsync(dataSource,ct);await using var tx=await c.BeginTransactionAsync(ct);await using var cmd=new NpgsqlCommand("DELETE FROM catalog_product.product_image WHERE id=@media AND product_id=@product AND product_id IN(SELECT id FROM catalog_product.product WHERE tenant_id=@tenant AND store_id=@store)",c,tx);Add(cmd,"media",mediaId);Add(cmd,"product",productId);Add(cmd,"tenant",ctx.TenantId);Add(cmd,"store",ctx.StoreId);if(await cmd.ExecuteNonQueryAsync(ct)==0)throw new DomainException("MEDIA_NOT_FOUND","Media was not found for this product",404);await InsertOutboxAsync(c,tx,"MediaChanged.v1",productId,ctx,new{productId,mediaId,operation="Deleted"},ct);await tx.CommitAsync(ct);return new DeletionResultDto{Id=mediaId.ToString(),Status="Deleted",DependentsRemoved=1,ProjectionEventPublished=true};}
}
