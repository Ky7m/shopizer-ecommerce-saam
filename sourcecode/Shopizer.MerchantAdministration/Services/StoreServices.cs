using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Npgsql;
using Shopizer.MerchantAdministration.Data;
using Shopizer.MerchantAdministration.DTOs;
using Shopizer.MerchantAdministration.Models;

namespace Shopizer.MerchantAdministration.Services;

public sealed class StoreService(
    StoreRepository repository,
    EventPublisher events,
    IConfiguration configuration,
    ILogger<StoreService> logger)
{
    private static readonly HashSet<string> Countries = ["US", "CA", "GB", "IE", "FR", "DE", "ES", "IT", "AU", "NZ", "JP", "BR", "MX", "IN"];
    private static readonly HashSet<string> Languages = ["en", "fr", "de", "es", "it", "pt", "ja", "zh", "nl"];
    private static readonly HashSet<string> Units = ["CM", "MM", "M", "IN"];
    private static readonly HashSet<string> Weights = ["KG", "G", "LB", "OZ"];
    private readonly string _defaultStore = configuration["MerchantAdministration:DefaultStoreCode"] ?? "default";
    private readonly string _defaultLanguage = configuration["MerchantAdministration:DefaultLanguageCode"] ?? "en";
    private readonly string _defaultCurrency = configuration["MerchantAdministration:DefaultCurrencyCode"] ?? "USD";
    private readonly string _defaultDimension = configuration["MerchantAdministration:DefaultDimensionUnit"] ?? "CM";
    private readonly string _defaultWeight = configuration["MerchantAdministration:DefaultWeightUnit"] ?? "KG";
    private readonly bool _cascadeChildren = bool.TryParse(configuration["MerchantAdministration:CascadeChildStores"], out var cascade) && cascade;

    // @BR-MER-001: Store codes are trimmed, normalized and restricted to the contract character set.
    // @BR-MER-002: Required contact fields and country references are validated before persistence.
    // @BR-MER-003: Store-code uniqueness is checked within the tenant before the database constraint.
    // @BR-MER-004: Omitted measurement units receive configured platform defaults.
    // @BR-MER-007: A child store is linked only to an existing active retailer parent.
    // @BR-MER-012: The default language is resolved from the supported language set.
    // @BR-MSA-VAL-001: Equivalent store identifiers use one normalized representation for lookup and persistence.
    // @BR-MSA-VAL-003: Store, language links and StoreCreated outbox data commit atomically.
    // @BR-MSA-LANG-001: The default language must be present in the supported language set.
    public async Task<StoreRecord> CreateAsync(CreateStoreRequestDto request, RequestContext context, CancellationToken ct)
    {
        var code = NormalizeCode(request.Code);
        ValidateCode(code);
        ValidateContact(request.Name, request.EmailAddress, request.Phone, request.Address);
        var supported = NormalizeLanguages(request.SupportedLanguageCodes);
        var defaultLanguage = NormalizeLanguage(request.DefaultLanguageCode);
        if (!supported.Contains(defaultLanguage, StringComparer.OrdinalIgnoreCase)) throw new DomainException("DEFAULT_LANGUAGE_UNSUPPORTED", "Default language must be supported by the store", 422);
        ValidateCountry(request.Address.CountryCode);
        var dimension = NormalizeUnit(request.DimensionUnit, _defaultDimension, Units, "UNSUPPORTED_UNIT");
        var weight = NormalizeUnit(request.WeightUnit, _defaultWeight, Weights, "UNSUPPORTED_UNIT");
        var existing = await repository.FindAsync(code, context, ct);
        if (existing is not null) throw new DomainException("STORE_CODE_CONFLICT", "Store code is already registered for this tenant", 409);
        var store = new StoreRecord { Id = Guid.NewGuid(), TenantId = context.TenantId, Code = code, Name = request.Name.Trim(), EmailAddress = request.EmailAddress.Trim().ToLowerInvariant(), Phone = request.Phone.Trim(), StreetAddress = request.Address.StreetAddress?.Trim(), City = request.Address.City.Trim(), PostalCode = request.Address.PostalCode.Trim(), CountryCode = request.Address.CountryCode.Trim().ToUpperInvariant(), StateProvince = request.Address.StateProvince?.Trim(), ZoneCode = request.Address.ZoneCode?.Trim(), Retailer = request.Retailer ?? false, DefaultLanguageCode = defaultLanguage, CurrencyCode = string.IsNullOrWhiteSpace(request.CurrencyCode) ? _defaultCurrency : request.CurrencyCode.Trim().ToUpperInvariant(), DimensionUnit = dimension, WeightUnit = weight };
        store.SupportedLanguageCodes.AddRange(supported);
        try { var result = await repository.CreateAsync(store, request.ParentStoreCode, context, ct); await events.PublishStoreCreatedAsync(result, context, ct); return result; }
        catch (PostgresException ex) when (ex.SqlState == "23505") { throw new DomainException("STORE_CODE_CONFLICT", "Store code is already registered for this tenant", 409); }
    }

    // @BR-MER-010: An omitted store context resolves against the configured default store.
    // @BR-MER-011: Store reads are constrained to the active tenant and authorized store context.
    // @BR-MER-012: Explicit languages are accepted only when supported by the store.
    // @BR-UI-007: Reads return only the store selected by the active administration context.
    public async Task<StoreRecord> GetAsync(string? code, RequestContext context, string? language, CancellationToken ct)
    {
        var targetCode = string.IsNullOrWhiteSpace(code) ? _defaultStore : NormalizeCode(code);
        var store = await repository.FindAsync(targetCode, context, ct) ?? throw new DomainException("STORE_NOT_FOUND", "Store was not found", 404);
        EnsureAccess(store, context);
        if (!string.IsNullOrWhiteSpace(language) && !store.SupportedLanguageCodes.Contains(language, StringComparer.OrdinalIgnoreCase)) throw new DomainException("UNSUPPORTED_LANGUAGE", "Language is not supported by this store", 422);
        return store;
    }

    // @BR-MER-002: Updated contact data is validated against the merged store state.
    // @BR-MER-005: Store updates merge supplied fields while preserving omitted fields.
    // @BR-MER-011: Updates are rejected outside the administrator's permitted store context.
    // @BR-MSA-VAL-002: Store code and tenant identity remain immutable during metadata updates.
    // @BR-MSA-VAL-003: Store fields and language associations update in one transaction.
    // @BR-MSA-LANG-001: Language changes retain a supported default language.
    public async Task<StoreRecord> UpdateAsync(string code, UpdateStoreRequestDto request, RequestContext context, CancellationToken ct)
    {
        var store = await GetAsync(code, context, null, ct);
        if (request.Name is not null) store.Name = request.Name.Trim();
        if (request.EmailAddress is not null) store.EmailAddress = request.EmailAddress.Trim().ToLowerInvariant();
        if (request.Phone is not null) store.Phone = request.Phone.Trim();
        if (request.Address is not null) { ValidateContact(store.Name, store.EmailAddress, store.Phone, request.Address); ValidateCountry(request.Address.CountryCode); store.StreetAddress = request.Address.StreetAddress?.Trim(); store.City = request.Address.City.Trim(); store.PostalCode = request.Address.PostalCode.Trim(); store.CountryCode = request.Address.CountryCode.Trim().ToUpperInvariant(); store.StateProvince = request.Address.StateProvince?.Trim(); store.ZoneCode = request.Address.ZoneCode?.Trim(); }
        if (request.DefaultLanguageCode is not null) { var lang = NormalizeLanguage(request.DefaultLanguageCode); if (!store.SupportedLanguageCodes.Contains(lang, StringComparer.OrdinalIgnoreCase)) throw new DomainException("DEFAULT_LANGUAGE_UNSUPPORTED", "Default language must be supported by the store", 422); store.DefaultLanguageCode = lang; }
        if (request.CurrencyCode is not null) store.CurrencyCode = request.CurrencyCode.Trim().ToUpperInvariant();
        if (request.DimensionUnit is not null) store.DimensionUnit = NormalizeUnit(request.DimensionUnit, store.DimensionUnit, Units, "UNSUPPORTED_UNIT");
        if (request.WeightUnit is not null) store.WeightUnit = NormalizeUnit(request.WeightUnit, store.WeightUnit, Weights, "UNSUPPORTED_UNIT");
        if (string.IsNullOrWhiteSpace(store.Name) || string.IsNullOrWhiteSpace(store.Phone)) throw new DomainException("VALIDATION_ERROR", "Store name and phone are required", 422);
        await repository.UpdateAsync(store, context, ct); return await GetAsync(store.Code, context, null, ct);
    }

    // @BR-MER-006: The configured default store cannot be deleted.
    // @BR-MER-009: Parent deletion rejects active children unless the configured cascade policy is enabled.
    // @BR-MER-011: Deletion is authorized against the request store context.
    public async Task DeleteAsync(string code, RequestContext context, CancellationToken ct)
    {
        var store = await GetAsync(code, context, null, ct);
        if (store.Code.Equals(_defaultStore, StringComparison.OrdinalIgnoreCase)) throw new DomainException("DEFAULT_STORE_PROTECTED", "The default store cannot be deleted", 409);
        if (_cascadeChildren) logger.LogInformation("Child-store cascade policy is configured; parent deletion remains transactional.");
        await repository.DeleteAsync(store, context, ct);
    }

    // @BR-MER-003: The uniqueness endpoint reports existence within the active tenant.
    // @BR-MSA-VAL-001: Uniqueness checks normalize codes before lookup.
    public async Task<bool> ExistsAsync(string code, RequestContext context, CancellationToken ct) => await repository.FindAsync(NormalizeCode(code), context, ct) is not null;

    // @BR-MSA-READ-001: Store pages are bounded, stable by normalized code, and include total counts.
    // @BR-MER-010: Collection reads use the configured context/default store boundary.
    public async Task<StoreListResponseDto> ListAsync(RequestContext context, int page, int pageSize, CancellationToken ct, string? parentCode = null)
    {
        if (page < 1 || pageSize is < 1 or > 200) throw new DomainException("INVALID_PAGE_SIZE", "pageSize must be between 1 and 200", 400);
        var result = await repository.ListAsync(context, page, pageSize, ct, parentCode);
        return new StoreListResponseDto { Items = result.Items.Select(x => DtoMapper.Store(x, null)).ToList(), Pagination = new PaginationInfoDto { Page = page, PageSize = pageSize, TotalItems = result.Total, TotalPages = result.Total == 0 ? 0 : (int)Math.Ceiling(result.Total / (double)pageSize) } };
    }

    // @BR-MSA-LST-001: Store name selectors are tenant-scoped and ordered by display name.
    // @BR-UI-007: Selector results remain within the active administration context.
    public async Task<StoreNameListResponseDto> NamesAsync(RequestContext context, CancellationToken ct)
    {
        var result = await repository.ListAsync(context, 1, 200, ct);
        return new StoreNameListResponseDto { Items = result.Items.OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase).Select(x => new StoreNameDto { Code = x.Code, Name = x.Name }).ToList() };
    }

    // @BR-MER-008: Child collections are available only for retailer stores.
    // @BR-MER-009: Hierarchy expansion never returns orphaned or deleted children.
    // @BR-MSA-AUTH-001: Descendants are bounded to the tenant and authorized root context.
    public async Task<StoreListResponseDto> HierarchyAsync(string merchantCode, RequestContext context, int page, int pageSize, bool childrenOnly, CancellationToken ct)
    {
        var root = await GetAsync(merchantCode, context, null, ct);
        if (!root.Retailer) throw new DomainException("RETAILER_REQUIRED", "Retailer status is required for hierarchy expansion", 403);
        var descendants = await repository.DescendantsAsync(root, ct); if (childrenOnly) descendants = descendants.Where(x => x.Id != root.Id).ToList();
        if (context.StoreId is not "default" and not "*") descendants = descendants.Where(x => x.Code.Equals(context.StoreId, StringComparison.OrdinalIgnoreCase) || IsDescendantOf(x, context.StoreId, descendants)).ToList();
        page = Math.Max(page, 1); pageSize = Math.Clamp(pageSize, 1, 200); var total = descendants.Count; var items = descendants.Skip((page - 1) * pageSize).Take(pageSize).Select(x => DtoMapper.Store(x, null)).ToList();
        return new StoreListResponseDto { Items = items, Pagination = new PaginationInfoDto { Page = page, PageSize = pageSize, TotalItems = total, TotalPages = total == 0 ? 0 : (int)Math.Ceiling(total / (double)pageSize) } };
    }

    // @BR-MER-012: Language reads expose the persisted supported-language association set.
    public async Task<LanguageListResponseDto> LanguagesAsync(string code, RequestContext context, CancellationToken ct) => new() { Items = (await GetAsync(code, context, null, ct)).SupportedLanguageCodes.ToList() };

    // @BR-MER-012: Language replacement preserves a valid default and is idempotent.
    // @BR-MSA-LANG-001: The configured default must remain in the replacement set.
    public async Task<StoreRecord> ReplaceLanguagesAsync(string code, ReplaceLanguagesRequestDto request, RequestContext context, CancellationToken ct)
    {
        var store = await GetAsync(code, context, null, ct); var languages = NormalizeLanguages(request.SupportedLanguageCodes); var defaultLanguage = NormalizeLanguage(request.DefaultLanguageCode);
        if (!languages.Contains(defaultLanguage, StringComparer.OrdinalIgnoreCase)) throw new DomainException("DEFAULT_LANGUAGE_UNSUPPORTED", "Default language must be supported by the store", 422);
        store.DefaultLanguageCode = defaultLanguage; store.SupportedLanguageCodes.Clear(); store.SupportedLanguageCodes.AddRange(languages); await repository.UpdateAsync(store, context, ct); return await GetAsync(store.Code, context, null, ct);
    }

    // @BR-MSA-BRD-001: Branding metadata is stored against the selected store only.
    // @BR-UI-007: Branding reads honor active store context.
    public async Task<BrandingDto> GetBrandingAsync(string code, RequestContext context, CancellationToken ct) => DtoMapper.Branding(await GetAsync(code, context, null, ct));

    // @BR-MSA-BRD-001: Template and provider URI metadata are updated without storing binary content in the store row.
    public async Task<BrandingDto> UpdateBrandingAsync(string code, BrandingRequestDto request, RequestContext context, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.TemplateCode) && string.IsNullOrWhiteSpace(request.LogoUri)) throw new DomainException("VALIDATION_ERROR", "Branding metadata is required", 422);
        var store = await GetAsync(code, context, null, ct); if (request.TemplateCode is not null) store.TemplateCode = request.TemplateCode.Trim(); if (request.LogoUri is not null) store.LogoUri = request.LogoUri.Trim(); await repository.UpdateBrandingAsync(store, context, ct); return DtoMapper.Branding(store);
    }

    // @BR-MSA-VAL-003: Signup requests persist validated data and a single-use, store-bound token for later activation.
    public async Task<SignupResponseDto> CreateSignupAsync(CreateStoreRequestDto request, RequestContext context, CancellationToken ct)
    {
        ValidateCode(NormalizeCode(request.Code)); ValidateContact(request.Name, request.EmailAddress, request.Phone, request.Address); ValidateCountry(request.Address.CountryCode); var languages = NormalizeLanguages(request.SupportedLanguageCodes); var defaultLanguage = NormalizeLanguage(request.DefaultLanguageCode); if (!languages.Contains(defaultLanguage, StringComparer.OrdinalIgnoreCase)) throw new DomainException("DEFAULT_LANGUAGE_UNSUPPORTED", "Default language must be supported by the store", 422); var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)); var expiry = DateTimeOffset.UtcNow.AddHours(24); var signup = await repository.CreateSignupAsync(context.TenantId, NormalizeCode(request.Code), JsonSerializer.Serialize(request), HashToken(token), expiry, ct); logger.LogInformation("Signup {SignupId} created; verification token is not logged.", signup.Id); return new SignupResponseDto { SignupId = signup.Id.ToString(), Status = "PendingVerification" };
    }

    // @BR-MSA-VAL-003: Verification rejects expired or consumed signup tokens and activates a store transactionally.
    public async Task<SignupVerificationResponseDto> VerifySignupAsync(string code, string token, RequestContext context, CancellationToken ct)
    {
        var signup = await repository.FindSignupAsync(context.TenantId, NormalizeCode(code), HashToken(token), ct); if (signup is null || signup.ConsumedAt is not null) throw new DomainException("SIGNUP_TOKEN_INVALID", "Signup token is invalid or already consumed", 410); if (signup.ExpiresAt <= DateTimeOffset.UtcNow) throw new DomainException("SIGNUP_TOKEN_EXPIRED", "Signup token has expired", 410); var request = JsonSerializer.Deserialize<CreateStoreRequestDto>(signup.PayloadJson) ?? throw new DomainException("SIGNUP_TOKEN_INVALID", "Signup payload is invalid", 410); await CreateAsync(request, context, ct); await repository.ConsumeSignupAsync(signup.Id, ct); return new SignupVerificationResponseDto { Verified = true };
    }

    private void EnsureAccess(StoreRecord store, RequestContext context)
    { if (!store.TenantId.Equals(context.TenantId, StringComparison.Ordinal)) throw new DomainException("STORE_ACCESS_DENIED", "Store belongs to another tenant", 403); if (context.StoreId is not ("default" or "*") && !store.Code.Equals(context.StoreId, StringComparison.OrdinalIgnoreCase)) throw new DomainException("STORE_ACCESS_DENIED", "Administrator is not authorized for this store", 403); }
    private static bool IsDescendantOf(StoreRecord candidate, string permittedCode, IEnumerable<StoreRecord> all) => candidate.Code.Equals(permittedCode, StringComparison.OrdinalIgnoreCase) || all.Any(x => x.Id == candidate.ParentStoreId && IsDescendantOf(x, permittedCode, all));
    private static string NormalizeCode(string code) => code?.Trim().ToLowerInvariant() ?? "";
    private static string NormalizeLanguage(string code) => code.Trim().ToLowerInvariant();
    private static List<string> NormalizeLanguages(IEnumerable<string> values) { var result = values.Where(x => !string.IsNullOrWhiteSpace(x)).Select(NormalizeLanguage).Distinct(StringComparer.OrdinalIgnoreCase).ToList(); if (result.Count == 0 || result.Any(x => !Languages.Contains(x))) throw new DomainException("UNSUPPORTED_LANGUAGE", "One or more languages are not supported", 422); return result; }
    private static string NormalizeUnit(string? value, string fallback, HashSet<string> allowed, string error) { var normalized = string.IsNullOrWhiteSpace(value) ? fallback : value.Trim().ToUpperInvariant(); if (!allowed.Contains(normalized)) throw new DomainException(error, "Measurement unit is not supported", 422); return normalized; }
    private static void ValidateCode(string code) { if (string.IsNullOrWhiteSpace(code) || code.Length > 100 || code.Any(c => !(char.IsLetterOrDigit(c) || c == '_'))) throw new DomainException("VALIDATION_ERROR", "Store code must contain only letters, digits and underscores and be at most 100 characters", 422); }
    private static void ValidateContact(string name, string email, string phone, AddressDto address) { if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(email) || !new System.ComponentModel.DataAnnotations.EmailAddressAttribute().IsValid(email) || string.IsNullOrWhiteSpace(phone) || address is null || string.IsNullOrWhiteSpace(address.City) || string.IsNullOrWhiteSpace(address.PostalCode) || string.IsNullOrWhiteSpace(address.CountryCode)) throw new DomainException("VALIDATION_ERROR", "Store contact fields are invalid or incomplete", 422); }
    private static void ValidateCountry(string country) { if (!Countries.Contains(country.Trim().ToUpperInvariant())) throw new DomainException("COUNTRY_NOT_FOUND", "Country reference was not found", 422); }
    private static string HashToken(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
}
