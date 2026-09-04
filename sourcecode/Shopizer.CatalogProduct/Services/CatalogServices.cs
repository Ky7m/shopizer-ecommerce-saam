using Shopizer.CatalogProduct.Data;
using Shopizer.CatalogProduct.DTOs;
using Shopizer.CatalogProduct.Models;

namespace Shopizer.CatalogProduct.Services;

public sealed class EventPublisher(CatalogRepository repository, RabbitMQ.Client.IConnection connection, ILogger<EventPublisher> logger)
{
    // @BR-CAT-036: Product change events are published from durable, complete aggregate mutations.
    public async Task PublishPendingAsync(RequestContext context, CancellationToken ct)
    {
        foreach (var message in await repository.PendingEventsAsync(context, ct))
        {
            try
            {
                await using var channel = await connection.CreateChannelAsync(cancellationToken: ct);
                await channel.ExchangeDeclareAsync("domain-events", RabbitMQ.Client.ExchangeType.Topic, durable: true, autoDelete: false, cancellationToken: ct);
                await channel.BasicPublishAsync("domain-events", message.EventType, false,
                    new RabbitMQ.Client.BasicProperties { ContentType = "application/json", Persistent = true },
                    System.Text.Encoding.UTF8.GetBytes(message.Payload), ct);
                await repository.MarkEventPublishedAsync(message.Id, ct);
                logger.LogInformation("Published catalog event {EventType} {EventId}.", message.EventType, message.Id);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
            catch (Exception ex) { logger.LogError(ex, "Catalog event delivery failed; outbox message {EventId} remains durable.", message.Id); }
        }
    }
}

public sealed class CatalogService(CatalogRepository repository, EventPublisher events)
{
    // @BR-CAT-001: Product SKUs remain unique inside the tenant and store boundary.
    // @BR-CAT-003: Product references use the merchant-scoped catalog identity boundary.
    // @BR-CAT-004: A product is persisted only when it has availability.
    // @BR-CAT-030: Missing wildcard availability produces a validation result instead of a null dereference.
    // @BR-CAT-005: Product category references are resolved in the current store.
    // @BR-CAT-017: Product metadata and media metadata are durable catalog records.
    // @BR-CAT-028: Catalog mutations are restricted to privileged administrative principals.
    // @BR-CAT-032: Visibility changes preserve an explicitly supplied effective date.
    // @BR-UI-003: Product SKU syntax and uniqueness are validated by the server.
    // @BR-UI-005: Empty localized description values receive the first usable fallback.
    public async Task<ProductDto> CreateProductAsync(CreateProductRequestDto request, RequestContext context, CancellationToken ct)
    {
        var result = await repository.CreateProductAsync(request, context, ct);
        await events.PublishPendingAsync(context, ct);
        return Map(result);
    }

    // @BR-CAT-009: Storefront listings contain only active, date-effective, region-eligible products.
    // @BR-CAT-025: Listing page size is caller-controlled and bounded independently from page number.
    // @BR-CAT-026: Count and fetch use the same eligibility predicate and distinct product IDs.
    public async Task<ProductListResponseDto> ListProductsAsync(RequestContext context, int page, int pageSize, string language, string? country,
        Guid? categoryId, string? sku, string? name, string? manufacturer, bool? available, bool storefront, CancellationToken ct)
    {
        ValidatePaging(page, pageSize);
        var result = await repository.ListProductsAsync(context, page, pageSize, language, country, categoryId, sku, name, manufacturer, available, storefront, ct);
        return new ProductListResponseDto { Items = result.Items.Select(Map).ToList(), Pagination = Pagination(page, pageSize, result.Total) };
    }

    // @BR-CAT-009: A product detail is returned only when its storefront availability is eligible.
    // @BR-CAT-031: Product output keeps persisted properties separate from selectable options.
    // @BR-CAT-033: Product locale and regional context are selected before the response is built.
    public async Task<ProductDto> GetProductAsync(Guid id, RequestContext context, string language, string? country, bool storefront, CancellationToken ct)
    {
        var result = await repository.FindProductAsync(id, context, language, country, storefront, ct)
            ?? throw new DomainException("LOCALIZED_PRODUCT_NOT_FOUND", "No product is eligible in the requested store and locale", 404);
        return Map(result);
    }

    // @BR-CAT-010: Friendly URL reads apply the same store, date, visibility, language, and region filters as listings.
    // @BR-CAT-033: Friendly URL responses use one deterministic language and region context.
    public async Task<ProductDto> GetProductBySlugAsync(string slug, RequestContext context, string language, string? country, CancellationToken ct)
    {
        var result = await repository.FindProductBySlugAsync(slug, context, language, country, ct)
            ?? throw new DomainException("PRODUCT_NOT_FOUND", "No eligible product matches the friendly URL", 404);
        return Map(result);
    }

    // @BR-CAT-001: SKU reads are constrained to the requested tenant and store.
    // @BR-CAT-009: Storefront SKU reads do not expose unavailable products.
    public async Task<ProductDto> GetProductBySkuAsync(string sku, RequestContext context, CancellationToken ct)
    {
        var result = await repository.FindProductBySkuAsync(sku, context, ct)
            ?? throw new DomainException("PRODUCT_NOT_FOUND", "Product was not found in this store", 404);
        return Map(result);
    }

    // @BR-CAT-001: Product uniqueness checks exclude only the requested product identity within the store.
    // @BR-UI-003: The uniqueness endpoint provides authoritative server-side SKU validation.
    public async Task<ExistsResponseDto> ProductUniquenessAsync(string sku, Guid? exclude, RequestContext context, CancellationToken ct) =>
        new() { Exists = await repository.ProductSkuExistsAsync(sku, context, exclude, ct) };

    // @BR-CAT-001: Product update rejects duplicate SKUs without writing the conflicting aggregate.
    // @BR-CAT-003: Updated references retain their merchant scope.
    // @BR-CAT-004: Updated availability remains a prerequisite for an active product.
    // @BR-CAT-005: Updated product references are resolved in the current store.
    // @BR-CAT-017: Product metadata is persisted independently from provider media operations.
    // @BR-CAT-018: A media-side failure does not erase a successfully persisted product mutation.
    // @BR-CAT-028: Product updates require catalog-management permission.
    // @BR-CAT-032: Product updates preserve explicit availability dates.
    // @BR-UI-005: Localized product values use the first non-empty fallback.
    public async Task<ProductDto> UpdateProductAsync(Guid id, UpdateProductRequestDto request, RequestContext context, CancellationToken ct)
    {
        var result = await repository.UpdateProductAsync(id, request, context, ct);
        await events.PublishPendingAsync(context, ct);
        return Map(await repository.FindProductAsync(result.Id, context, "en", null, false, ct) ?? throw new DomainException("PRODUCT_NOT_FOUND", "Product was not found", 404));
    }

    // @BR-CAT-009: Storefront eligibility follows visibility, purchase permission, date, and sellable quantity.
    // @BR-CAT-032: An explicit product effective date is retained during visibility changes.
    // @BR-UI-004: Purchase eligibility requires all availability flags and positive sellable quantity.
    public async Task<ProductDto> UpdateProductVisibilityAsync(Guid id, UpdateVisibilityRequestDto request, RequestContext context, CancellationToken ct)
    {
        var result = await repository.UpdateVisibilityAsync(id, request, context, ct);
        await events.PublishPendingAsync(context, ct);
        return Map(await repository.FindProductAsync(result.Id, context, "en", null, false, ct) ?? throw new DomainException("PRODUCT_NOT_FOUND", "Product was not found", 404));
    }

    // @BR-CAT-019: Product deletion removes dependent catalog records before the product row.
    // @BR-CAT-038: A completed deletion leaves no catalog references to the product.
    public async Task<DeletionResultDto> DeleteProductAsync(Guid id, RequestContext context, CancellationToken ct)
    {
        var result = await repository.DeleteProductAsync(id, context, ct);
        await events.PublishPendingAsync(context, ct);
        return result;
    }

    // @BR-CAT-005: Product-category links are accepted only when both aggregates belong to the current store.
    // @BR-CAT-028: Category association mutations require catalog-management permission.
    public async Task<ProductDto> AttachCategoryAsync(Guid product, Guid category, RequestContext context, CancellationToken ct) =>
        await AssociateCategoryAsync(product, category, true, context, ct);

    // @BR-CAT-008: Detaching a category removes only the selected product-category association.
    // @BR-CAT-019: Product aggregate cleanup never leaves a stale category link.
    public async Task<ProductDto> DetachCategoryAsync(Guid product, Guid category, RequestContext context, CancellationToken ct) =>
        await AssociateCategoryAsync(product, category, false, context, ct);

    private async Task<ProductDto> AssociateCategoryAsync(Guid product, Guid category, bool attach, RequestContext context, CancellationToken ct)
    {
        var result = await repository.AttachCategoryAsync(product, category, attach, context, ct);
        await events.PublishPendingAsync(context, ct);
        return Map(await repository.FindProductAsync(result.Id, context, "en", null, false, ct) ?? throw new DomainException("PRODUCT_NOT_FOUND", "Product was not found", 404));
    }

    // @BR-CAT-003: Category codes are unique within a tenant store and reusable in another store.
    // @BR-CAT-006: Category creation materializes validated parent lineage and depth atomically.
    // @BR-CAT-028: Category creation requires catalog-management permission.
    // @BR-UI-006: Category code, description, parent, and hierarchy are validated together.
    public async Task<CategoryDto> CreateCategoryAsync(CreateCategoryRequestDto request, RequestContext context, CancellationToken ct)
    {
        var result = await repository.CreateCategoryAsync(request, context, ct);
        await events.PublishPendingAsync(context, ct);
        return result;
    }

    // @BR-CAT-003: Category listings are scoped to the current merchant.
    // @BR-UI-006: Category listing filters are applied to localized hierarchy data.
    public async Task<CategoryListResponseDto> ListCategoriesAsync(RequestContext context, int page, int pageSize, string language, string? name, bool? visible, bool? featured, CancellationToken ct)
    {
        ValidatePaging(page, pageSize);
        var result = await repository.ListCategoriesAsync(context, page, pageSize, language, name, visible, featured, ct);
        var items = new List<CategoryDto>(); foreach (var item in result.Items) items.Add(await repository.MapCategoryAsync(item, language, context, ct));
        return new CategoryListResponseDto { Items = items, Pagination = Pagination(page, pageSize, result.Total) };
    }

    // @BR-CAT-006: Category reads expose the persisted hierarchy depth and lineage.
    // @BR-UI-006: Category output contains its localized description and children.
    public async Task<CategoryDto> GetCategoryAsync(Guid id, RequestContext context, string language, CancellationToken ct) =>
        await repository.MapCategoryAsync(await repository.FindCategoryAsync(id, context, language, ct) ?? throw new DomainException("CATEGORY_NOT_FOUND", "Category was not found in this store", 404), language, context, ct);

    // @BR-UI-006: Category friendly URLs resolve inside the current store and language.
    public async Task<CategoryDto> GetCategoryBySlugAsync(string slug, RequestContext context, string language, CancellationToken ct) =>
        await repository.MapCategoryAsync(await repository.FindCategoryBySlugAsync(slug, context, language, ct) ?? throw new DomainException("CATEGORY_NOT_FOUND", "Category was not found in this store", 404), language, context, ct);

    // @BR-CAT-003: Category uniqueness checks use the store boundary and optional current identity exclusion.
    // @BR-UI-006: Category administration receives an authoritative code uniqueness result.
    public async Task<ExistsResponseDto> CategoryUniquenessAsync(string code, Guid? exclude, RequestContext context, CancellationToken ct) =>
        new() { Exists = await repository.CategoryCodeExistsAsync(code, context, exclude, ct) };

    // @BR-CAT-003: Category updates preserve code uniqueness inside the merchant store.
    // @BR-CAT-006: Parent changes recalculate the category hierarchy values.
    // @BR-UI-006: Category metadata and localized descriptions are updated as one operation.
    public async Task<CategoryDto> UpdateCategoryAsync(Guid id, UpdateCategoryRequestDto request, RequestContext context, CancellationToken ct) { var result = await repository.UpdateCategoryAsync(id, request, context, ct); await events.PublishPendingAsync(context, ct); return result; }

    // @BR-UI-006: Category visibility transitions between active and hidden states.
    public async Task<CategoryDto> UpdateCategoryVisibilityAsync(Guid id, UpdateCategoryVisibilityRequestDto request, RequestContext context, CancellationToken ct) { var result = await repository.UpdateCategoryVisibilityAsync(id, request, context, ct); await events.PublishPendingAsync(context, ct); return result; }

    // @BR-CAT-007: Moving a category recalculates lineage and depth for its complete descendant subtree.
    // @BR-CAT-034: Category moves reject cross-store parents and descendant cycles before mutation.
    public async Task<CategoryDto> MoveCategoryAsync(Guid id, Guid parent, RequestContext context, CancellationToken ct) { var result = await repository.MoveCategoryAsync(id, parent, context, ct); await events.PublishPendingAsync(context, ct); return result; }

    // @BR-CAT-008: Category deletion applies the requested subtree orphan-product policy atomically.
    public async Task<CategoryDeletionResultDto> DeleteCategoryAsync(Guid id, string policy, RequestContext context, CancellationToken ct) { var result = await repository.DeleteCategoryAsync(id, policy, context, ct); await events.PublishPendingAsync(context, ct); return result; }

    // @BR-CAT-009: Category product listings use the same regional storefront eligibility as product listings.
    // @BR-CAT-026: Category totals and pages are based on distinct products under the same predicate.
    public async Task<ProductListResponseDto> ListCategoryProductsAsync(Guid category, RequestContext context, int page, int pageSize, string language, string? country, CancellationToken ct)
    { var c = await repository.FindCategoryAsync(category, context, language, ct) ?? throw new DomainException("CATEGORY_NOT_FOUND", "Category was not found in this store", 404); return await ListProductsAsync(context, page, pageSize, language, country, category, null, null, null, null, true, ct); }

    // @BR-CAT-002: Variant SKUs remain unique within their parent product.
    // @BR-CAT-012: A default-selected usable variant is preferred for variant pricing.
    // @BR-CAT-028: Variant creation requires catalog-management permission.
    public async Task<ProductVariantDto> CreateVariantAsync(Guid product, CreateVariantRequestDto request, RequestContext context, CancellationToken ct) { var result = await repository.CreateVariantAsync(product, request, context, ct); await events.PublishPendingAsync(context, ct); return MapVariant(result, await repository.FindProductAsync(product, context, "en", null, false, ct)); }

    // @BR-CAT-002: Variant updates reject duplicate SKUs in the same parent.
    // @BR-CAT-028: Variant updates require catalog-management permission.
    public async Task<ProductVariantDto> UpdateVariantAsync(Guid product, Guid variant, UpdateVariantRequestDto request, RequestContext context, CancellationToken ct) { var result = await repository.UpdateVariantAsync(product, variant, request, context, ct); await events.PublishPendingAsync(context, ct); return MapVariant(result, await repository.FindProductAsync(product, context, "en", null, false, ct)); }

    // @BR-CAT-002: Variant reads are constrained to the requested parent and store.
    // @BR-CAT-012: Variant output exposes the selected default variant state.
    public async Task<ProductVariantDto> GetVariantAsync(Guid product, Guid variant, RequestContext context, CancellationToken ct) { var a = await repository.FindProductAsync(product, context, "en", null, false, ct) ?? throw new DomainException("PRODUCT_NOT_FOUND", "Product was not found in this store", 404); return MapVariant(a.Variants.FirstOrDefault(v => v.Id == variant) ?? throw new DomainException("VARIANT_NOT_FOUND", "Variant was not found in this product", 404), a); }

    // @BR-CAT-002: Variant pages return only variants owned by the requested parent product.
    public async Task<ProductVariantListResponseDto> ListVariantsAsync(Guid product, RequestContext context, int page, int pageSize, CancellationToken ct) { ValidatePaging(page, pageSize); var a = await repository.FindProductAsync(product, context, "en", null, false, ct) ?? throw new DomainException("PRODUCT_NOT_FOUND", "Product was not found in this store", 404); var all = a.Variants; var items = all.Skip(page * pageSize).Take(pageSize).Select(v => MapVariant(v, a)).ToList(); return new() { Items = items, Pagination = Pagination(page, pageSize, all.Count) }; }

    // @BR-CAT-019: Variant deletion removes its dependent availability through the aggregate foreign key.
    // @BR-CAT-038: Variant deletion leaves no product references to the deleted variant.
    public async Task<DeletionResultDto> DeleteVariantAsync(Guid product, Guid variant, RequestContext context, CancellationToken ct) { var result = await repository.DeleteVariantAsync(product, variant, context, ct); await events.PublishPendingAsync(context, ct); return result; }

    // @BR-CAT-002: Variant SKU uniqueness is checked inside its parent product boundary.
    public async Task<ExistsResponseDto> VariantUniquenessAsync(Guid product, string sku, RequestContext context, CancellationToken ct) { if (await repository.FindProductAsync(product, context, "en", null, false, ct) is null) throw new DomainException("PRODUCT_NOT_FOUND", "Product was not found in this store", 404); return new() { Exists = await repository.VariantSkuExistsAsync(product, sku, context, null, ct) }; }

    // @BR-CAT-011: Availability reads expose active regional stock including the wildcard region.
    public async Task<AvailabilityListResponseDto> GetAvailabilityAsync(Guid product, RequestContext context, CancellationToken ct) => new() { Items = (await repository.GetAvailabilityAsync(product, context, ct)).Select(DtoMapper.Availability).ToList() };

    // @BR-CAT-004: Replacing availability requires at least one valid availability record.
    // @BR-CAT-011: Regional availability replacement preserves each caller-supplied region.
    // @BR-CAT-028: Availability mutations require catalog-management permission.
    public async Task<AvailabilityListResponseDto> ReplaceAvailabilityAsync(Guid product, ReplaceAvailabilityRequestDto request, RequestContext context, CancellationToken ct) { var result = await repository.ReplaceAvailabilityAsync(product, request, context, ct); await events.PublishPendingAsync(context, ct); return new() { Items = result.Select(DtoMapper.Availability).ToList() }; }

    // @BR-ORD-012: Inventory acceptance atomically reserves positive sellable quantity.
    // @BR-CAT-037: Repeated reservation keys return the original reservation or an idempotency conflict.
    public async Task<InventoryReservationDto> CreateReservationAsync(Guid product, CreateReservationRequestDto request, RequestContext context, CancellationToken ct) { var result = await repository.CreateReservationAsync(product, request, context, ct); await events.PublishPendingAsync(context, ct); return MapReservation(result); }

    // @BR-CAT-039: A held reservation can transition once to the committed terminal outcome.
    public async Task<InventoryReservationDto> CommitReservationAsync(Guid id, RequestContext context, CancellationToken ct) { var result = await repository.TransitionReservationAsync(id, true, context, ct); await events.PublishPendingAsync(context, ct); return MapReservation(result); }

    // @BR-CAT-039: A held reservation can transition once to the released terminal outcome and restore stock.
    public async Task<InventoryReservationDto> ReleaseReservationAsync(Guid id, RequestContext context, CancellationToken ct) { var result = await repository.TransitionReservationAsync(id, false, context, ct); await events.PublishPendingAsync(context, ct); return MapReservation(result); }

    // @BR-CAT-014: Special prices apply only inside their configured active window.
    // @BR-CAT-015: Positive selected attribute adjustments are additive while non-positive adjustments do not increase price.
    // @BR-CAT-016: Missing usable variant pricing falls back to the eligible parent product price.
    // @BR-CAT-029: Only option/value pairs belonging to the product contribute to variation pricing.
    // @BR-CAT-013: Default and wildcard price candidates are evaluated before non-default candidates.
    // @BR-CAT-027: Product and variant price alternatives remain grouped inside the requested store.
    public Task<PriceResponseDto> CalculatePriceAsync(Guid product, CalculatePriceRequestDto request, RequestContext context, CancellationToken ct) => repository.CalculatePriceAsync(product, request, context, ct);

    // @BR-CAT-017: Media metadata is persisted independently from product metadata.
    // @BR-EXT-019: Binary content is separated from media metadata at the provider boundary.
    // @BR-EXT-020: Configured media representations are recorded for the uploaded asset.
    public async Task<ProductMediaDto> AddMediaAsync(Guid product, string fileName, string? externalUrl, bool defaultImage, RequestContext context, CancellationToken ct) { var result = await repository.AddMediaAsync(product, fileName, externalUrl, defaultImage, context, ct); await events.PublishPendingAsync(context, ct); return MapMedia(result); }

    // @BR-CAT-018: A provider-side media failure does not roll back an already durable product mutation.
    public async Task<ProductMediaDto> AddExternalMediaAsync(Guid product, ExternalMediaRequestDto request, RequestContext context, CancellationToken ct) => await AddMediaAsync(product, request.FileName, request.ExternalUrl, request.DefaultImage ?? false, context, ct);

    // @BR-CAT-035: Media deletion publishes a projection change so downstream projections invalidate stale media.
    public async Task<DeletionResultDto> DeleteMediaAsync(Guid product, Guid media, RequestContext context, CancellationToken ct) { var result = await repository.DeleteMediaAsync(product, media, context, ct); await events.PublishPendingAsync(context, ct); return result; }

    private static void ValidatePaging(int page, int size) { if (page < 0 || size < 1 || size > 200) throw new DomainException("PAGE_SIZE_INVALID", "page must be zero-based and pageSize must be between 1 and 200", 422); }
    private static PaginationInfoDto Pagination(int page, int size, int total) => new() { Page = page, PageSize = size, TotalItems = total, TotalPages = (int)Math.Ceiling(total / (double)size) };
    private static ProductDto Map(CatalogAggregate a) { var dto = new ProductDto { Id = a.Product.Id.ToString(), StoreId = a.Product.StoreId, Sku = a.Product.Sku, RefSku = a.Product.RefSku, Status = a.Product.Status, Visible = a.Product.Visible, Available = a.Product.Available, CanBePurchased = a.Product.CanBePurchased, DateAvailable = a.Product.DateAvailable.ToString("O"), ManufacturerCode = a.Product.ManufacturerCode, ProductTypeCode = a.Product.ProductTypeCode, TaxClassCode = a.Product.TaxClassCode, ProductVirtual = a.Product.ProductVirtual, ProductShippable = a.Product.ProductShippable, ProductFree = a.Product.ProductFree, Length = a.Product.Length, Width = a.Product.Width, Height = a.Product.Height, Weight = a.Product.Weight, ReviewAverage = a.Product.ReviewAverage, ReviewCount = a.Product.ReviewCount, SortOrder = a.Product.SortOrder, Descriptions = a.Descriptions.Select(DtoMapper.Description).ToList(), Categories = a.Categories.Select(c => new CategoryReferenceDto { Id = c.Id.ToString(), Code = c.Code }).ToList(), Availabilities = a.Availabilities.Where(x => x.ProductId == a.Product.Id).Select(DtoMapper.Availability).ToList(), Media = a.Media.Select(MapMedia).ToList(), Properties = a.Properties.Select(p => new ProductPropertyDto { Code = p.Code, Value = p.Value, Name = p.Name }).ToList(), Options = a.Options.Select(o => new ProductOptionDto { Id = o.OptionId.ToString(), Code = o.Code, Name = o.Name, DisplayOnly = o.DisplayOnly, Values = o.Values }).ToList() }; var defaultVariant = a.Variants.FirstOrDefault(v => v.DefaultSelection); var variantPrice = defaultVariant is null ? null : a.Prices.Where(p => a.Availabilities.Any(x => x.VariantId == defaultVariant.Id && x.Id == p.AvailabilityId && x.Active && x.Quantity > x.ReservedQuantity)).OrderByDescending(p => a.Availabilities.First(x => x.Id == p.AvailabilityId).RegionCode == "*").ThenByDescending(p => p.DefaultPrice).FirstOrDefault(); var price = variantPrice ?? a.Prices.Where(p => a.Availabilities.Any(x => x.ProductId == a.Product.Id && x.Id == p.AvailabilityId && x.Active && x.Quantity > x.ReservedQuantity)).OrderByDescending(p => a.Availabilities.First(x => x.Id == p.AvailabilityId).RegionCode == "*").ThenByDescending(p => p.DefaultPrice).FirstOrDefault(); if (price is not null) dto.Price = DtoMapper.Price(price, DateTimeOffset.UtcNow); dto.Variants = a.Variants.Select(v => MapVariant(v, a)).ToList(); return dto; }
    private static ProductVariantDto MapVariant(VariantRecord v, CatalogAggregate? a) => new() { Id = v.Id.ToString(), ProductId = v.ProductId.ToString(), Sku = v.Sku, Code = v.Code, Status = v.Status, DefaultSelection = v.DefaultSelection, Available = v.Available, DateAvailable = v.DateAvailable.ToString("O"), Availability = a?.Availabilities.Where(x => x.VariantId == v.Id).Select(DtoMapper.Availability).ToList(), Price = a?.Prices.FirstOrDefault(p => a.Availabilities.Any(x => x.VariantId == v.Id && x.Id == p.AvailabilityId)) is { } p ? DtoMapper.Price(p, DateTimeOffset.UtcNow) : null };
    private static ProductMediaDto MapMedia(MediaRecord m) => new() { Id = m.Id.ToString(), FileName = m.FileName, ImageType = m.ImageType, OriginalUri = m.OriginalUri, TransformedUri = m.TransformedUri, ExternalUrl = m.ExternalUrl, DefaultImage = m.DefaultImage, MediaStatus = m.MediaStatus };
    private static InventoryReservationDto MapReservation(ReservationRecord r) => new() { Id = r.Id.ToString(), ProductId = r.ProductId?.ToString(), VariantId = r.VariantId?.ToString(), AvailabilityId = r.AvailabilityId.ToString(), ReservationKey = r.ReservationKey, Quantity = r.Quantity, State = r.State, ExpiresAt = r.ExpiresAt.ToString("O"), CommittedAt = r.CommittedAt?.ToString("O"), ReleasedAt = r.ReleasedAt?.ToString("O") };
}
