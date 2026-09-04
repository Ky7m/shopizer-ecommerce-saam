using System.Text.Json;
using Shopizer.Tax.Data;
using Shopizer.Tax.DTOs;
using Shopizer.Tax.Models;

namespace Shopizer.Tax.Services;

public sealed class TaxService(TaxRepository repository, ILogger<TaxService> logger)
{
    private static readonly HashSet<string> SupportedLanguages = new(StringComparer.OrdinalIgnoreCase)
    {
        "en", "fr", "de", "es", "it", "pt", "ja", "zh", "nl"
    };

    // @BR-TAX-CLS-001: A tax-class code is created once inside the authenticated tenant/store boundary.
    public async Task<TaxClassDto> CreateTaxClassAsync(CreateTaxClassRequestDto request, RequestContext context, CancellationToken ct)
    {
        ValidateTaxClass(request.Code, request.Title);
        var code = request.Code.Trim();
        if (await repository.TaxClassExistsAsync(code, context, ct))
            throw new DomainException("TAX_CLASS_ALREADY_EXISTS", $"Tax class code {code} already exists for store {context.StoreId}", 409);

        var entity = new TaxClassEntity
        {
            Id = Guid.NewGuid(),
            TenantId = context.TenantId,
            StoreId = context.StoreId,
            Code = code,
            Title = request.Title.Trim()
        };
        await repository.AddTaxClassAsync(entity, context, ct);
        return DtoMapper.TaxClass(entity);
    }

    // @BR-TAX-CLS-002: Tax-class lists expose only rows owned by the authenticated tenant and store.
    public async Task<TaxClassListResponseDto> ListTaxClassesAsync(int page, int pageSize, RequestContext context, CancellationToken ct)
    {
        ValidatePagination(page, pageSize);
        var total = await repository.CountTaxClassesAsync(context, ct);
        return new TaxClassListResponseDto
        {
            Items = (await repository.ListTaxClassesAsync(context, page, pageSize, ct)).Select(DtoMapper.TaxClass).ToList(),
            Pagination = Pagination(page, pageSize, total)
        };
    }

    // @BR-TAX-CLS-002: A tax-class lookup cannot cross the authenticated tenant/store boundary.
    public async Task<TaxClassDto> GetTaxClassAsync(Guid id, RequestContext context, CancellationToken ct) =>
        DtoMapper.TaxClass(await repository.FindTaxClassAsync(id, context, ct)
            ?? throw new DomainException("TAX_CLASS_NOT_FOUND", $"Tax class {id} was not found for store {context.StoreId}", 404));

    // @BR-TAX-CLS-001: Tax-class existence checks return a boolean for both present and absent codes.
    public async Task<ExistsResponseDto> TaxClassExistsAsync(string? code, RequestContext context, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(code)) throw new DomainException("MISSING_CODE", "Query parameter code is required", 400);
        ValidateLength(code, 10, "code");
        return new ExistsResponseDto { Exists = await repository.TaxClassExistsAsync(code.Trim(), context, ct) };
    }

    // @BR-TAX-CLS-003: Tax-class mutation requires an existing row owned by the authenticated tenant/store.
    public async Task<TaxClassDto> UpdateTaxClassAsync(Guid id, UpdateTaxClassRequestDto request, RequestContext context, CancellationToken ct)
    {
        ValidateTaxClass(request.Code, request.Title);
        var current = await repository.FindTaxClassAsync(id, context, ct);
        if (current is null)
        {
            if (await repository.FindTaxClassAnyScopeAsync(id, ct) is not null)
                throw new DomainException("TAX_CLASS_STORE_MISMATCH", $"Tax class is not owned by store {context.StoreId}", 403);
            throw new DomainException("TAX_CLASS_NOT_FOUND", $"Tax class {id} was not found for store {context.StoreId}", 404);
        }
        if (await repository.TaxClassExistsAsync(request.Code.Trim(), context, ct) &&
            !string.Equals(current.Code, request.Code.Trim(), StringComparison.Ordinal))
            throw new DomainException("TAX_CLASS_ALREADY_EXISTS", $"Tax class code {request.Code.Trim()} already exists for store {context.StoreId}", 409);

        current.Code = request.Code.Trim();
        current.Title = request.Title.Trim();
        await repository.UpdateTaxClassAsync(current, context, ct);
        return DtoMapper.TaxClass(current);
    }

    // @BR-TAX-CLS-003: A tax class is deleted only from its authenticated tenant/store scope.
    public async Task<DeleteResponseDto> DeleteTaxClassAsync(Guid id, RequestContext context, CancellationToken ct)
    {
        if (await repository.FindTaxClassAsync(id, context, ct) is null)
        {
            if (await repository.FindTaxClassAnyScopeAsync(id, ct) is not null)
                throw new DomainException("TAX_CLASS_STORE_MISMATCH", $"Tax class is not owned by store {context.StoreId}", 403);
            throw new DomainException("TAX_CLASS_NOT_FOUND", $"Tax class {id} was not found for store {context.StoreId}", 404);
        }
        await repository.DeleteTaxClassAsync(id, context, ct);
        return new DeleteResponseDto { Deleted = true, Id = id.ToString() };
    }

    // @BR-TAX-RAT-001: A tax rate is persisted with its tenant/store, tax class, geography, and localized descriptions.
    public async Task<TaxRateDto> CreateTaxRateAsync(CreateTaxRateRequestDto request, RequestContext context, CancellationToken ct)
    {
        var entity = await BuildRateAsync(Guid.NewGuid(), request, context, ct);
        if (await repository.TaxRateExistsAsync(entity.Code, context, ct))
            throw new DomainException("TAX_RATE_ALREADY_EXISTS", $"Tax rate code {entity.Code} already exists for store {context.StoreId}", 409);
        await repository.AddTaxRateAsync(entity, context, ct);
        return DtoMapper.TaxRate(entity);
    }

    // @BR-TAX-RAT-003: Tax-rate lists are tenant/store scoped, localized, and ordered by ascending priority.
    public async Task<TaxRateListResponseDto> ListTaxRatesAsync(
        string? languageCode, int page, int pageSize, RequestContext context, CancellationToken ct)
    {
        var language = ValidateLanguage(languageCode);
        ValidatePagination(page, pageSize);
        var total = await repository.CountTaxRatesAsync(context, language, ct);
        return new TaxRateListResponseDto
        {
            Items = (await repository.ListTaxRatesAsync(context, language, page, pageSize, ct))
                .Select(rate => DtoMapper.TaxRate(rate, language)).ToList(),
            Pagination = Pagination(page, pageSize, total)
        };
    }

    // @BR-TAX-RAT-004: A tax-rate read is restricted to the authenticated tenant/store.
    public async Task<TaxRateDto> GetTaxRateAsync(Guid id, RequestContext context, CancellationToken ct) =>
        DtoMapper.TaxRate(await repository.FindTaxRateAsync(id, context, ct)
            ?? throw new DomainException("TAX_RATE_NOT_FOUND", $"Tax rate was not found for store {context.StoreId}", 404));

    // @BR-TAX-RAT-005: Tax-rate existence checks return false when the requested code is absent.
    public async Task<ExistsResponseDto> TaxRateExistsAsync(string? code, RequestContext context, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(code)) throw new DomainException("MISSING_CODE", "Query parameter code is required", 400);
        ValidateLength(code, 100, "code");
        return new ExistsResponseDto { Exists = await repository.TaxRateExistsAsync(code.Trim(), context, ct) };
    }

    // @BR-TAX-RAT-002: Updating a rate replaces editable fields while retaining its identifier and store ownership.
    public async Task<TaxRateDto> UpdateTaxRateAsync(Guid id, CreateTaxRateRequestDto request, RequestContext context, CancellationToken ct)
    {
        var current = await repository.FindTaxRateAsync(id, context, ct)
            ?? throw new DomainException("TAX_RATE_NOT_FOUND", $"Tax rate was not found for store {context.StoreId}", 404);
        var entity = await BuildRateAsync(id, request, context, ct);
        if (await repository.TaxRateExistsAsync(entity.Code, context, ct) &&
            !string.Equals(entity.Code, current.Code, StringComparison.Ordinal))
            throw new DomainException("TAX_RATE_ALREADY_EXISTS", $"Tax rate code {entity.Code} already exists for store {context.StoreId}", 409);
        await repository.UpdateTaxRateAsync(entity, context, ct);
        return DtoMapper.TaxRate(entity);
    }

    // @BR-TAX-RAT-004: Deleting a rate affects only a rate found in the authenticated tenant/store scope.
    public async Task<DeleteResponseDto> DeleteTaxRateAsync(Guid id, RequestContext context, CancellationToken ct)
    {
        if (await repository.FindTaxRateAsync(id, context, ct) is null)
            throw new DomainException("TAX_RATE_NOT_FOUND", $"Tax rate was not found for store {context.StoreId}", 404);
        await repository.DeleteTaxRateAsync(id, context, ct);
        return new DeleteResponseDto { Deleted = true, Id = id.ToString() };
    }

    // @BR-TAX-CFG-001: An absent tax configuration defaults to shipping-address jurisdiction and customer-country behavior.
    public async Task<TaxConfigurationDto> GetConfigurationAsync(RequestContext context, CancellationToken ct)
    {
        var configuration = await repository.FindConfigurationAsync(context, ct) ?? new TaxConfigurationEntity();
        return DtoMapper.TaxConfiguration(configuration);
    }

    // @BR-TAX-CFG-002: Saving tax configuration preserves basis, province policy, and different-country behavior.
    public async Task<TaxConfigurationDto> SaveConfigurationAsync(UpdateTaxConfigurationRequestDto request, RequestContext context, CancellationToken ct)
    {
        ValidateConfiguration(request.TaxBasis, request.DifferentCountryBehavior);
        var current = await repository.FindConfigurationAsync(context, ct) ?? new TaxConfigurationEntity
        {
            Id = Guid.NewGuid(),
            TenantId = context.TenantId,
            StoreId = context.StoreId
        };
        current.TaxBasis = request.TaxBasis;
        current.CollectTaxIfDifferentProvince = request.CollectTaxIfDifferentProvince;
        current.DifferentCountryBehavior = request.DifferentCountryBehavior;
        await repository.SaveConfigurationAsync(current, context, ct);
        return DtoMapper.TaxConfiguration(current);
    }

    // @BR-TAX-CAL-001: Structurally incomplete calculation input is rejected before a quote is written.
    // @BR-TAX-CAL-002: The configured tax basis selects billing or shipping jurisdiction.
    // @BR-TAX-CAL-003: A disallowed province difference produces a zero-tax result.
    // @BR-TAX-CAL-004: Different-country policy explicitly selects customer, store, or no-tax behavior.
    // @BR-TAX-CAL-005: A jurisdiction without a zone or state produces a defined zero-tax result.
    // @BR-TAX-CAL-006: Item amounts are aggregated by tax class and unclassified items use DEFAULT.
    // @BR-TAX-CAL-007: Positive shipping and handling are aggregated under DEFAULT.
    // @BR-TAX-CAL-008: Rates are resolved by tenant/store, country, geography, language, and tax class.
    // @BR-TAX-CAL-009: Rates use ordered non-compound and compound bases with two-decimal half-up rounding.
    // @BR-TAX-CAL-010: Same-code tax items are consolidated by writing the summed amount to the retained item.
    public async Task<TaxCalculationResponseDto> CalculateAsync(CalculateTaxRequestDto request, RequestContext context, CancellationToken ct)
    {
        ValidateCalculationInput(request);
        var language = ValidateLanguage(request.LanguageCode);
        if (!string.IsNullOrWhiteSpace(request.IdempotencyKey))
        {
            var existing = await repository.FindQuoteByIdempotencyAsync(request.IdempotencyKey.Trim(), context, ct);
            if (existing is not null)
                return DtoMapper.TaxCalculation(existing, await repository.FindQuoteItemsAsync(existing.Id, context, ct));
        }

        var configuration = await repository.FindConfigurationAsync(context, ct) ?? new TaxConfigurationEntity();
        var shippingAddress = ReadAddress(request.ShippingAddress);
        var jurisdiction = SelectJurisdiction(request.BillingAddress, shippingAddress, configuration);
        var customerAddress = shippingAddress ?? request.BillingAddress;
        var noTax = ApplyCountryPolicy(customerAddress, jurisdiction, configuration);
        if (!noTax && jurisdiction.ZoneCode is null && string.IsNullOrWhiteSpace(jurisdiction.StateProvince))
            logger.LogInformation("Tax jurisdiction has no zone or state; returning a zero-tax quote.");

        var classAmounts = new Dictionary<Guid, decimal>();
        var classes = new Dictionary<Guid, TaxClassEntity>();
        foreach (var item in request.Items)
        {
            ValidateCalculationItem(item);
            var classCode = string.IsNullOrWhiteSpace(item.TaxClassCode) ? "DEFAULT" : item.TaxClassCode.Trim();
            var taxClass = await repository.FindTaxClassByCodeAsync(classCode, context, ct)
                ?? throw new DomainException("TAX_CLASS_NOT_FOUND", $"Tax class {classCode} was not found for store {context.StoreId}", 422);
            classes[taxClass.Id] = taxClass;
            classAmounts[taxClass.Id] = classAmounts.GetValueOrDefault(taxClass.Id) +
                                         item.UnitAmount * item.Quantity;
        }

        var shipping = ReadShipping(request.Shipping);
        if (shipping is not null)
        {
            if (shipping.ShippingAmount < 0) throw new DomainException("INVALID_SHIPPING_AMOUNT", "shippingAmount cannot be negative", 422);
            if (shipping.HandlingAmount < 0) throw new DomainException("INVALID_HANDLING_AMOUNT", "handlingAmount cannot be negative", 422);
        }
        if (shipping is not null && (shipping.ShippingAmount > 0 || shipping.HandlingAmount > 0))
        {
            var defaultClass = await repository.FindTaxClassByCodeAsync("DEFAULT", context, ct)
                ?? throw new DomainException("DEFAULT_TAX_CLASS_REQUIRED", "DEFAULT tax class is required for shipping tax", 422);
            classes[defaultClass.Id] = defaultClass;
            classAmounts[defaultClass.Id] = classAmounts.GetValueOrDefault(defaultClass.Id) +
                                             (shipping.ShippingAmount > 0 ? shipping.ShippingAmount : 0) +
                                             (shipping.HandlingAmount > 0 ? shipping.HandlingAmount : 0);
        }

        var taxItemsByCode = new Dictionary<string, TaxItemDto>(StringComparer.Ordinal);
        if (!noTax && (jurisdiction.ZoneCode is not null || !string.IsNullOrWhiteSpace(jurisdiction.StateProvince)))
        {
            foreach (var (classId, taxableAmount) in classAmounts)
            {
                var rates = await repository.FindRatesForCalculationAsync(
                    context, classId, jurisdiction.CountryCode, jurisdiction.ZoneCode,
                    jurisdiction.ZoneCode is null ? jurisdiction.StateProvince : null, language, ct);
                var runningTaxedAmount = 0m;
                foreach (var rate in rates)
                {
                    var calculationBase = rate.Piggyback && runningTaxedAmount > 0
                        ? runningTaxedAmount
                        : taxableAmount;
                    var amount = Math.Round(calculationBase * rate.RatePercent / 100m, 2, MidpointRounding.AwayFromZero);
                    runningTaxedAmount = calculationBase + amount;
                    var label = rate.Descriptions.FirstOrDefault()?.Name ?? rate.Code;
                    if (taxItemsByCode.TryGetValue(rate.Code, out var retained))
                    {
                        retained.TaxableAmount += taxableAmount;
                        retained.TaxAmount += amount;
                    }
                    else
                    {
                        taxItemsByCode[rate.Code] = new TaxItemDto
                        {
                            TaxCode = rate.Code,
                            Label = label,
                            TaxClassCode = classes[classId].Code,
                            TaxRatePercent = rate.RatePercent,
                            TaxableAmount = taxableAmount,
                            TaxAmount = amount,
                            Piggyback = rate.Piggyback,
                            Priority = rate.Priority
                        };
                    }
                }
            }
        }

        var taxableTotal = classAmounts.Values.Sum();
        var totalTax = taxItemsByCode.Values.Sum(item => item.TaxAmount);
        var quote = new TaxQuoteEntity
        {
            Id = Guid.NewGuid(),
            TenantId = context.TenantId,
            StoreId = context.StoreId,
            IdempotencyKey = string.IsNullOrWhiteSpace(request.IdempotencyKey) ? null : request.IdempotencyKey.Trim(),
            CurrencyCode = request.CurrencyCode.Trim().ToUpperInvariant(),
            CustomerId = ParseOptionalGuid(request.CustomerId, "customerId"),
            OrderId = ParseOptionalGuid(request.OrderId, "orderId"),
            JurisdictionCountryCode = jurisdiction.CountryCode,
            JurisdictionZoneCode = jurisdiction.ZoneCode,
            JurisdictionStateProvince = jurisdiction.StateProvince,
            TaxableAmount = taxableTotal,
            TotalTaxAmount = totalTax,
            CalculatedAt = DateTimeOffset.UtcNow
        };
        var entities = taxItemsByCode.Values.Select(item => new TaxQuoteItemEntity
        {
            Id = Guid.NewGuid(),
            TaxQuoteId = quote.Id,
            TaxClassId = classes.Values.FirstOrDefault(c => c.Code == item.TaxClassCode)?.Id,
            TaxClassCode = item.TaxClassCode,
            TaxCode = item.TaxCode,
            Label = item.Label,
            RatePercent = item.TaxRatePercent,
            TaxableAmount = item.TaxableAmount,
            TaxAmount = item.TaxAmount,
            Piggyback = item.Piggyback,
            Priority = item.Priority
        }).ToList();
        await repository.SaveQuoteAsync(quote, entities, context, ct);
        return DtoMapper.TaxCalculation(quote, entities);
    }

    private async Task<TaxRateEntity> BuildRateAsync(Guid id, CreateTaxRateRequestDto request, RequestContext context, CancellationToken ct)
    {
        ValidateRate(request);
        var taxClass = await repository.FindTaxClassByCodeAsync(request.TaxClassCode.Trim(), context, ct)
            ?? throw new DomainException("TAX_CLASS_NOT_FOUND", $"Tax class {request.TaxClassCode.Trim()} was not found for store {context.StoreId}", 422);
        var descriptions = request.Descriptions
            .Select(description => new TaxRateDescriptionEntity
            {
                Id = Guid.NewGuid(),
                TaxRateId = id,
                LanguageCode = ValidateLanguage(description.LanguageCode),
                Name = description.Name.Trim(),
                Title = description.Title?.Trim(),
                Description = description.Description
            }).ToList();
        if (descriptions.Count == 0) throw new DomainException("DESCRIPTION_REQUIRED", "At least one tax-rate description is required", 422);
        if (descriptions.Select(x => x.LanguageCode).Distinct(StringComparer.OrdinalIgnoreCase).Count() != descriptions.Count)
            throw new DomainException("DUPLICATE_DESCRIPTION_LANGUAGE", "Each tax-rate language may be supplied only once", 422);
        return new TaxRateEntity
        {
            Id = id,
            TenantId = context.TenantId,
            StoreId = context.StoreId,
            TaxClassId = taxClass.Id,
            TaxClassCode = taxClass.Code,
            Code = request.Code.Trim(),
            RatePercent = request.Rate,
            Priority = request.Priority,
            Piggyback = request.Piggyback,
            CountryCode = request.CountryCode.Trim().ToUpperInvariant(),
            ZoneCode = string.IsNullOrWhiteSpace(request.ZoneCode) ? null : request.ZoneCode.Trim(),
            StateProvince = string.IsNullOrWhiteSpace(request.StateProvince) ? null : request.StateProvince.Trim(),
        }.WithDescriptions(descriptions);
    }

    private static AddressSnapshotDto SelectJurisdiction(AddressSnapshotDto billing, AddressSnapshotDto? shipping, TaxConfigurationEntity configuration)
    {
        if (configuration.TaxBasis == "ShippingAddress" && shipping is not null) return NormalizeAddress(shipping);
        if (configuration.TaxBasis == "BillingAddress") return NormalizeAddress(billing);
        if (configuration.TaxBasis == "ShippingAddress" && shipping is null)
            return NormalizeAddress(billing);
        throw new DomainException("STORE_JURISDICTION_REQUIRED", "Store address is required by the configured tax basis", 422);
    }

    private static bool ApplyCountryPolicy(AddressSnapshotDto billing, AddressSnapshotDto jurisdiction, TaxConfigurationEntity configuration)
    {
        if (string.Equals(billing.CountryCode, jurisdiction.CountryCode, StringComparison.OrdinalIgnoreCase))
            return false;
        if (configuration.DifferentCountryBehavior == "NoTax") return true;
        if (configuration.DifferentCountryBehavior == "UseCustomerJurisdiction") return false;
        throw new DomainException("STORE_JURISDICTION_REQUIRED", "Store address is required for UseStoreJurisdiction", 422);
    }

    private static AddressSnapshotDto NormalizeAddress(AddressSnapshotDto address) => new()
    {
        CountryCode = address.CountryCode.Trim().ToUpperInvariant(),
        ZoneCode = string.IsNullOrWhiteSpace(address.ZoneCode) ? null : address.ZoneCode.Trim(),
        StateProvince = string.IsNullOrWhiteSpace(address.StateProvince) ? null : address.StateProvince.Trim()
    };

    private static AddressSnapshotDto? ReadAddress(object? value) =>
        value is JsonElement { ValueKind: not JsonValueKind.Null and not JsonValueKind.Undefined } element
            ? JsonSerializer.Deserialize<AddressSnapshotDto>(element.GetRawText())
            : null;

    private static ShippingInputDto? ReadShipping(object? value) =>
        value is JsonElement { ValueKind: not JsonValueKind.Null and not JsonValueKind.Undefined } element
            ? JsonSerializer.Deserialize<ShippingInputDto>(element.GetRawText())
            : null;

    private static void ValidateCalculationInput(CalculateTaxRequestDto request)
    {
        if (request.BillingAddress is null)
            throw new DomainException("CUSTOMER_CONTEXT_REQUIRED", "Customer address context is required for tax calculation", 422);
        if (request.Items is null || request.Items.Count == 0)
            throw new DomainException("ITEMS_REQUIRED", "At least one tax calculation item is required", 422);
        if (!System.Text.RegularExpressions.Regex.IsMatch(request.CurrencyCode ?? "", "^[A-Z]{3}$"))
            throw new DomainException("INVALID_CURRENCY", "currencyCode must be a three-letter uppercase code", 422);
    }

    private static void ValidateCalculationItem(TaxCalculationItemDto item)
    {
        if (item.Quantity <= 0) throw new DomainException("INVALID_QUANTITY", "quantity must be greater than zero", 422);
        if (item.UnitAmount < 0) throw new DomainException("INVALID_UNIT_AMOUNT", "unitAmount cannot be negative", 422);
        if (string.IsNullOrWhiteSpace(item.ProductId))
            throw new DomainException("INVALID_CALCULATION_ITEM", "productId is required", 422);
    }

    private static void ValidateTaxClass(string? code, string? title)
    {
        ValidateLength(code, 10, "code");
        ValidateLength(title, 32, "title");
    }

    private static void ValidateRate(CreateTaxRateRequestDto request)
    {
        ValidateLength(request.Code, 100, "code");
        ValidateLength(request.TaxClassCode, 10, "taxClassCode");
        ValidateLength(request.CountryCode, 3, "countryCode");
        if (request.CountryCode.Trim().Length < 2) throw new DomainException("INVALID_COUNTRY_CODE", "countryCode must contain two or three characters", 422);
        if (request.Rate < 0 || request.Rate > 100) throw new DomainException("INVALID_TAX_RATE", "rate must be between 0 and 100", 422);
        if (request.Priority < 0) throw new DomainException("INVALID_PRIORITY", "priority cannot be negative", 422);
        if (request.StateProvince?.Length > 100) throw new DomainException("INVALID_STATE_PROVINCE", "stateProvince cannot exceed 100 characters", 422);
    }

    private static void ValidateConfiguration(string? basis, string? behavior)
    {
        if (basis is not ("StoreAddress" or "ShippingAddress" or "BillingAddress"))
            throw new DomainException("INVALID_TAX_BASIS", "taxBasis must be StoreAddress, ShippingAddress, or BillingAddress", 422);
        if (behavior is not ("UseCustomerJurisdiction" or "UseStoreJurisdiction" or "NoTax"))
            throw new DomainException("INVALID_COUNTRY_BEHAVIOR", "differentCountryBehavior is invalid", 422);
    }

    private static string ValidateLanguage(string? language)
    {
        var value = string.IsNullOrWhiteSpace(language) ? "en" : language.Trim();
        if (!SupportedLanguages.Contains(value))
            throw new DomainException("LANGUAGE_NOT_SUPPORTED", $"Language {value} is not supported", 422);
        return value;
    }

    private static void ValidateLength(string? value, int max, string name)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Trim().Length > max)
            throw new DomainException("INVALID_" + name.ToUpperInvariant(), $"{name} is required and cannot exceed {max} characters", 422);
    }

    private static void ValidatePagination(int page, int pageSize)
    {
        if (page < 1 || pageSize < 1 || pageSize > 100)
            throw new DomainException("INVALID_PAGINATION", "page must be positive and pageSize must be between 1 and 100", 400);
    }

    private static PaginationInfoDto Pagination(int page, int pageSize, long total) => new()
    {
        Page = page,
        PageSize = pageSize,
        TotalItems = total,
        TotalPages = total == 0 ? 0 : (int)Math.Ceiling(total / (double)pageSize)
    };

    private static Guid? ParseOptionalGuid(string? value, string name)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        if (Guid.TryParse(value, out var id)) return id;
        throw new DomainException("INVALID_" + name.ToUpperInvariant(), $"{name} must be a UUID", 422);
    }
}

internal static class TaxRateBuilderExtensions
{
    public static TaxRateEntity WithDescriptions(this TaxRateEntity entity, IEnumerable<TaxRateDescriptionEntity> descriptions)
    {
        entity.Descriptions.AddRange(descriptions);
        return entity;
    }
}
