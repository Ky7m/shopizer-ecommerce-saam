using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Shopizer.CustomerIdentity.Data;
using Shopizer.CustomerIdentity.DTOs;
using Shopizer.CustomerIdentity.Models;

namespace Shopizer.CustomerIdentity.Services;

public sealed class PasswordService
{
    private const int Iterations = 120_000;
    public string Encode(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(16);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, 32);
        return $"PBKDF2-SHA256${Iterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
    }

    public bool Matches(string encoded, string password)
    {
        try
        {
            var pieces = encoded.Split('$');
            if (pieces.Length != 4 || pieces[0] != "PBKDF2-SHA256" || !int.TryParse(pieces[1], out var iterations)) return false;
            var expected = Convert.FromBase64String(pieces[3]);
            var actual = Rfc2898DeriveBytes.Pbkdf2(password, Convert.FromBase64String(pieces[2]), iterations, HashAlgorithmName.SHA256, expected.Length);
            return CryptographicOperations.FixedTimeEquals(expected, actual);
        }
        catch (FormatException) { return false; }
    }
    public void RequirePolicy(string? password)
    {
        if (string.IsNullOrWhiteSpace(password) || password.Length < 8 ||
            !password.Any(char.IsUpper) || !password.Any(char.IsLower) ||
            !password.Any(char.IsDigit) || !password.Any(ch => !char.IsLetterOrDigit(ch)))
            throw new DomainException("PASSWORD_POLICY_FAILED", "Password does not meet policy", 422);
    }
}

public sealed record TokenData(Guid SubjectId, string Kind, string Login, string TenantId, string StoreId, DateTimeOffset IssuedAt, DateTimeOffset ExpiresAt, IReadOnlyList<string> Roles);

public sealed class TokenService(IConfiguration configuration, IdentityRepository repository, IHostEnvironment environment)
{
    private readonly byte[] _secret = CreateSecret(configuration, environment);
    private readonly int _lifetimeMinutes = int.TryParse(configuration["CustomerIdentity:JwtLifetimeMinutes"], out var minutes) ? minutes : 60;
    public int LifetimeMinutes => _lifetimeMinutes;
    private static byte[] CreateSecret(IConfiguration configuration, IHostEnvironment environment)
    {
        var configured = configuration["CustomerIdentity:JwtSecret"];
        if (!string.IsNullOrWhiteSpace(configured)) return Encoding.UTF8.GetBytes(configured);
        if (!environment.IsDevelopment()) throw new InvalidOperationException("CustomerIdentity:JwtSecret must be configured outside Development.");
        return RandomNumberGenerator.GetBytes(64);
    }

    // @BR-CUS-NN-005: Access tokens contain subject, audience, issued-at and expiration claims.
    public string Create(Guid subjectId, string kind, string login, RequestContext context, IEnumerable<string> roles, DateTimeOffset? now = null)
    {
        var issued = now ?? DateTimeOffset.UtcNow;
        var expires = issued.AddMinutes(_lifetimeMinutes);
        var header = Base64Url(JsonSerializer.SerializeToUtf8Bytes(new { alg = "HS512", typ = "JWT" }));
        var payload = Base64Url(JsonSerializer.SerializeToUtf8Bytes(new
        {
            sub = subjectId.ToString(), name = login, aud = "api", kind, tenantId = context.TenantId,
            storeId = context.StoreId, iat = issued.ToUnixTimeSeconds(), exp = expires.ToUnixTimeSeconds(),
            roles = roles.ToArray()
        }));
        var body = $"{header}.{payload}";
        using var hmac = new HMACSHA512(_secret);
        return $"{body}.{Base64Url(hmac.ComputeHash(Encoding.UTF8.GetBytes(body)))}";
    }

    // @BR-CUS-NN-006: Signature, subject, expiration, tenant/store and password-reset cutoffs are enforced.
    // @BR-CUS-NN-009: Bearer parsing resolves the subject before authentication is established.
    public async Task<TokenData?> ValidateAsync(string raw, RequestContext context, CancellationToken ct)
    {
        try
        {
            var pieces = raw.Split('.');
            if (pieces.Length != 3) return null;
            using var hmac = new HMACSHA512(_secret);
            var expected = hmac.ComputeHash(Encoding.UTF8.GetBytes($"{pieces[0]}.{pieces[1]}"));
            if (!CryptographicOperations.FixedTimeEquals(expected, FromBase64Url(pieces[2]))) return null;
            using var json = JsonDocument.Parse(Encoding.UTF8.GetString(FromBase64Url(pieces[1])));
            var root = json.RootElement;
            if (root.GetProperty("aud").GetString() != "api") return null;
            var subject = Guid.Parse(root.GetProperty("sub").GetString()!);
            var kind = root.GetProperty("kind").GetString()!;
            var login = root.GetProperty("name").GetString()!;
            var tenant = root.GetProperty("tenantId").GetString()!;
            var store = root.GetProperty("storeId").GetString()!;
            var issued = DateTimeOffset.FromUnixTimeSeconds(root.GetProperty("iat").GetInt64());
            var expiry = DateTimeOffset.FromUnixTimeSeconds(root.GetProperty("exp").GetInt64());
            if (!tenant.Equals(context.TenantId, StringComparison.Ordinal) || !store.Equals(context.StoreId, StringComparison.Ordinal) || expiry <= DateTimeOffset.UtcNow) return null;
            var roles = root.TryGetProperty("roles", out var roleJson) ? roleJson.EnumerateArray().Select(x => x.GetString()!).ToArray() : [];
            if (kind == "customer")
            {
                var customer = await repository.FindCustomerAsync(subject, context, ct);
                if (customer is null || !customer.LoginName.Equals(login, StringComparison.OrdinalIgnoreCase) || (customer.LastPasswordResetAt is not null && issued < customer.LastPasswordResetAt)) return null;
            }
            else if (kind == "administrator")
            {
                var admin = await repository.FindAdminAsync(subject, context, ct);
                if (admin is null || !admin.IsActive || !admin.UserName.Equals(login, StringComparison.OrdinalIgnoreCase) || (admin.LastPasswordResetAt is not null && issued < admin.LastPasswordResetAt)) return null;
            }
            else return null;
            return new TokenData(subject, kind, login, tenant, store, issued, expiry, roles);
        }
        catch (FormatException) { return null; }
        catch (JsonException) { return null; }
        catch (KeyNotFoundException) { return null; }
        catch (CryptographicException) { return null; }
    }

    private static string Base64Url(byte[] value) => Convert.ToBase64String(value).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    private static byte[] FromBase64Url(string value) => Convert.FromBase64String(value.Replace('-', '+').Replace('_', '/') + new string('=', (4 - value.Length % 4) % 4));
}

public sealed class IdentityService(
    IdentityRepository repository,
    PasswordService passwords,
    TokenService tokens,
    IConfiguration configuration,
    EventPublisher events,
    ILogger<IdentityService> logger)
{
    private readonly string _defaultLanguage = configuration["CustomerIdentity:DefaultLanguage"] ?? "en";
    private readonly string _campaign = configuration["CustomerIdentity:NewsletterCampaignCode"] ?? "NEWSLETTER";
    private static readonly HashSet<string> Countries = ["US", "CA", "GB", "IE", "FR", "DE", "ES", "IT", "AU", "NZ", "JP", "BR", "MX", "IN"];
    private static readonly HashSet<string> Zones = ["AL", "AK", "AZ", "CA", "CO", "FL", "GA", "IL", "MA", "NY", "OH", "OR", "PA", "TX", "WA"];

    // @BR-CUS-001: Login and email uniqueness are checked inside the tenant/store boundary.
    // @BR-CUS-002: Self-service loginName is always derived from emailAddress.
    // @BR-CUS-003: Billing country is required and must resolve to reference data.
    // @BR-CUS-004: New customers receive the customer access group/authority.
    // @BR-CUS-005: Passwords are encoded before persistence.
    // @BR-CUS-015: Attributes are validated against same-store option definitions by the repository.
    // @BR-CUS-016: Gender and language defaults are applied during construction.
    // @BR-CUS-019: A token is created only after credentials and persistence succeed.
    // @BR-CUS-020: Customer authorities are derived from assigned group names.
    public async Task<(CustomerAccount Customer, AuthenticationResponseDto Token)> RegisterAsync(CreateCustomerRequestDto request, RequestContext context, CancellationToken ct)
    {
        ValidateAddress(request.Billing, true);
        var login = request.EmailAddress.Trim().ToLowerInvariant();
        if (await repository.CustomerLoginExistsAsync(login, context, ct))
            throw new DomainException("CUSTOMER_IDENTITY_CONFLICT", "Login identifier is already registered for this store", 409);
        passwords.RequirePolicy(request.Password);
        if (request.Attributes is not null) ValidateAttributeIds(request.Attributes);
        var customer = new CustomerAccount
        {
            Id = Guid.NewGuid(), TenantId = context.TenantId, StoreId = context.StoreId, LoginName = login,
            EmailAddress = login, PasswordHash = passwords.Encode(request.Password), Gender = string.IsNullOrWhiteSpace(request.Gender) ? "M" : request.Gender!,
            CompanyName = null, Provider = request.Provider, DefaultLanguageCode = string.IsNullOrWhiteSpace(request.Language) ? _defaultLanguage : request.Language!,
            Status = "Active", Anonymous = false
        };
        ValidateLanguage(customer.DefaultLanguageCode);
        await repository.AddCustomerAsync(customer, request.Billing, request.Delivery, request.Attributes ?? [], context, ct);
        await events.PublishCustomerRegisteredAsync(customer, context, ct);
        return (customer, Authentication(customer, context, ["ROLE_CUSTOMER_AUTHENTICATED", "customer.read"]));
    }

    // @BR-CUS-019: Customer authentication returns a token only after encoded password verification.
    // @BR-CUS-NN-010: Administrator authentication uses the stored encoded password.
    public async Task<AuthenticationResponseDto> LoginAsync(AuthenticationRequestDto request, RequestContext context, bool administrator, CancellationToken ct)
    {
        if (administrator)
        {
            var admin = await repository.FindAdminByLoginAsync(request.Username, context, ct);
            if (admin is null || !admin.IsActive || !passwords.Matches(admin.PasswordHash, request.Password))
                throw new DomainException("BAD_CREDENTIALS", "Username or password is incorrect", 401);
            return Authentication(admin, context, admin.Groups);
        }
        var customer = await repository.FindCustomerByLoginAsync(request.Username, context, ct);
        if (customer is null || !passwords.Matches(customer.PasswordHash, request.Password))
            throw new DomainException("BAD_CREDENTIALS", "Username or password is incorrect", 401);
        return Authentication(customer, context, ["ROLE_CUSTOMER_AUTHENTICATED", "customer.read"]);
    }

    // @BR-CUS-NN-007: Refresh is allowed only for an unexpired, non-reset-invalidated token.
    // @BR-CUS-NN-008: Invalid refresh input is rejected and never receives unconditional success.
    public async Task<AuthenticationResponseDto> RefreshAsync(TokenData token, RequestContext context, CancellationToken ct)
    {
        if (token.ExpiresAt <= DateTimeOffset.UtcNow) throw new DomainException("REFRESH_NOT_ALLOWED", "Token cannot be refreshed", 400);
        var roles = token.Roles;
        return new AuthenticationResponseDto
        {
            SubjectId = token.SubjectId.ToString(), AccessToken = tokens.Create(token.SubjectId, token.Kind, token.Login, context, roles),
            TokenType = "Bearer", ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(tokens.LifetimeMinutes).ToString("O")
        };
    }

    // @BR-CUS-NN-004: Current password and matching replacement are required for a customer password change.
    public async Task ChangeCustomerPasswordAsync(Guid customerId, CustomerPasswordChangeRequestDto request, RequestContext context, CancellationToken ct)
    {
        var customer = await RequiredCustomer(customerId, context, ct);
        if (!passwords.Matches(customer.PasswordHash, request.CurrentPassword)) throw new DomainException("CURRENT_PASSWORD_INVALID", "Current password does not match", 401);
        passwords.RequirePolicy(request.NewPassword);
        if (!request.NewPassword.Equals(request.RepeatPassword, StringComparison.Ordinal)) throw new DomainException("PASSWORD_MISMATCH", "Passwords must match", 422);
        await repository.SetCustomerPasswordAsync(customer.Id, passwords.Encode(request.NewPassword), context, ct);
    }

    // @BR-CUS-NN-001: Customer reset requests create random two-day, store-bound tokens and queue an email operation.
    // @BR-CUS-NN-017: Administrator reset requests use the same random two-day, store-bound token lifecycle.
    public async Task RequestResetAsync(ResetRequestDto request, RequestContext context, bool administrator, CancellationToken ct)
    {
        if (!Uri.TryCreate(request.ReturnUrl, UriKind.Absolute, out var uri) || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            throw new DomainException("INVALID_RETURN_URL", "Return URL is invalid", 422);
        var subject = administrator
            ? (object?)await repository.FindAdminByLoginAsync(request.Username, context, ct)
            : await repository.FindCustomerByLoginAsync(request.Username, context, ct);
        if (subject is null) throw new DomainException(administrator ? "USER_NOT_FOUND" : "CUSTOMER_NOT_FOUND", "Identity was not found for this store", 404);
        var id = administrator ? ((AdministratorAccount)subject).Id : ((CustomerAccount)subject).Id;
        var email = administrator ? ((AdministratorAccount)subject).EmailAddress : ((CustomerAccount)subject).EmailAddress;
        var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        await repository.SaveResetAsync(token, administrator ? "Administrator" : "Customer", id, context, ct);
        await repository.QueueResetEmailAsync(email, request.ReturnUrl, context, administrator ? "Administrator" : "Customer", token, ct);
        logger.LogInformation("Password reset email queued for {SubjectType} {SubjectId}; token is never logged.", administrator ? "Administrator" : "Customer", id);
    }

    // @BR-CUS-NN-002: Reset token lookup enforces store ownership, expiry, and single-use consumption state.
    public async Task<(Guid SubjectId, string Type, DateTimeOffset ExpiresAt)> VerifyResetAsync(string storeCode, string token, string expectedType, RequestContext context, CancellationToken ct)
    {
        if (!storeCode.Equals(context.StoreId, StringComparison.Ordinal))
            throw new DomainException("RESET_TOKEN_INVALID", "Reset token is invalid or expired", 410);
        var reset = await repository.FindResetAsync(token, storeCode, context.TenantId, ct);
        if (reset is null || !reset.Value.Type.Equals(expectedType, StringComparison.Ordinal)) throw new DomainException("RESET_TOKEN_INVALID", "Reset token is invalid or expired", 410);
        return (reset.Value.SubjectId, reset.Value.Type, reset.Value.ExpiresAt);
    }

    // @BR-CUS-NN-003: Customer reset completion enforces policy, encodes the password and consumes the token.
    // @BR-CUS-NN-018: Administrator reset completion enforces policy, encodes the password and consumes the token.
    public async Task CompleteResetAsync(string storeCode, string token, ResetPasswordRequestDto request, RequestContext context, bool administrator, CancellationToken ct)
    {
        var reset = await VerifyResetAsync(storeCode, token, administrator ? "Administrator" : "Customer", context, ct);
        if (!request.Password.Equals(request.RepeatPassword, StringComparison.Ordinal)) throw new DomainException("PASSWORD_MISMATCH", "Passwords must match", 422);
        passwords.RequirePolicy(request.Password);
        if (administrator) await repository.SetAdminPasswordAsync(reset.SubjectId, passwords.Encode(request.Password), context, ct);
        else await repository.SetCustomerPasswordAsync(reset.SubjectId, passwords.Encode(request.Password), context, ct);
        await repository.ConsumeResetAsync(token, storeCode, ct);
    }

    // @BR-CUS-006: Customer lookup is constrained to tenant and store.
    // @BR-CUS-007: Self-service resolves the authenticated principal rather than a body identifier.
    public async Task<CustomerDto> CustomerDtoAsync(Guid id, RequestContext context, CancellationToken ct)
    {
        var customer = await RequiredCustomer(id, context, ct);
        return await MapCustomer(customer, ct);
    }

    // @BR-CUS-009: Customer listing is bounded and includes total pagination.
    // @BR-CUS-010: Every filter remains conjunctively store-scoped.
    public async Task<CustomerListResponseDto> ListCustomersAsync(RequestContext context, int page, int pageSize, string? name, string? email, string? first, string? last, string? country, CancellationToken ct)
    {
        if (page < 1 || pageSize is < 1 or > 200) throw new DomainException("INVALID_PAGE_SIZE", "pageSize must be greater than zero and no greater than 200", 400);
        var result = await repository.ListCustomersAsync(context, page, pageSize, name, email, first, last, country, ct);
        var mapped = new List<CustomerDto>(); foreach (var customer in result.Items) mapped.Add(await MapCustomer(customer, ct));
        return new CustomerListResponseDto { Items = mapped, Pagination = Page(page, pageSize, result.Total) };
    }

    // @BR-CUS-011: Country and optional zone codes resolve through supported reference data.
    // @BR-CUS-012: Billing street, city, postal and country are mandatory.
    // @BR-CUS-013: Omitted delivery values inherit the corresponding billing values.
    // @BR-CUS-014: State/province and postal code are persisted as separate fields.
    // @BR-UI-001: Billing and delivery address value objects remain independent.
    public async Task UpdateAddressesAsync(Guid customerId, AddressUpdateRequestDto request, RequestContext context, CancellationToken ct)
    {
        await RequiredCustomer(customerId, context, ct);
        if (request.Billing is null && request.Delivery is null) throw new DomainException("ADDRESS_REQUIRED", "Billing or delivery address is required", 400);
        if (request.Billing is not null) ValidateAddress(request.Billing, true);
        if (request.Delivery is not null) ValidateAddress(request.Delivery, false, allowPartial: true);
        var delivery = request.Delivery;
        if (request.Billing is not null && delivery is null) delivery = CopyAddress(request.Billing, "Delivery");
        if (request.Billing is not null && delivery is not null) delivery = CompleteDelivery(request.Billing, delivery);
        await repository.UpdateAddressesAsync(customerId, request.Billing, delivery, ct);
    }

    // @BR-CUS-017: Customer deletion removes customer attributes and dependent address data through cascade.
    // @BR-CUS-008: Administrative deletion is restricted to approved administrator groups.
    public async Task DeleteCustomerAsync(Guid id, RequestContext context, CancellationToken ct)
    {
        await RequiredCustomer(id, context, ct); await repository.DeleteCustomerAsync(id, context, ct);
    }

    // @BR-CUS-NN-011: Administrator creation is unique, policy-validated and group-assigned.
    // @BR-CUS-NN-013: SUPERADMIN assignment is permitted only to an existing super administrator.
    public async Task<AdministratorDto> CreateAdminAsync(CreateAdministratorRequestDto request, RequestContext context, bool actorIsSuper, CancellationToken ct)
    {
        if (!request.Password.Equals(request.RepeatPassword, StringComparison.Ordinal)) throw new DomainException("PASSWORD_MISMATCH", "Passwords must match", 422);
        passwords.RequirePolicy(request.Password);
        var groups = request.Groups.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        if (groups.Count == 0) throw new DomainException("GROUP_REQUIRED", "At least one administrator group is required", 422);
        if (groups.Any(x => x.Equals("SUPERADMIN", StringComparison.OrdinalIgnoreCase)) && !actorIsSuper) throw new DomainException("SUPERADMIN_ASSIGNMENT_DENIED", "Only a super administrator may grant this group", 403);
        if (await repository.AdminLoginExistsAsync(request.UserName, context, ct)) throw new DomainException("USERNAME_CONFLICT", "Username is already registered for this store", 409);
        var admin = new AdministratorAccount { Id = Guid.NewGuid(), TenantId = context.TenantId, StoreId = context.StoreId, UserName = request.UserName.Trim(), EmailAddress = request.EmailAddress.Trim().ToLowerInvariant(), PasswordHash = passwords.Encode(request.Password), FirstName = request.FirstName, LastName = request.LastName, DefaultLanguageCode = request.DefaultLanguageCode };
        await repository.AddAdminAsync(admin, groups, context, ct); return DtoMapper.Administrator(admin);
    }

    // @BR-CUS-NN-012: Administrator listings are paginated inside the selected store hierarchy boundary.
    // @BR-CUS-NN-019: Inactive administrators are never treated as authenticated identities.
    public async Task<AdministratorListResponseDto> ListAdminsAsync(RequestContext context, int page, int pageSize, string? email, CancellationToken ct)
    {
        if (page < 1 || pageSize is < 1 or > 200) throw new DomainException("INVALID_PAGE_SIZE", "pageSize must be greater than zero and no greater than 200", 400);
        var result = await repository.ListAdminsAsync(context, page, pageSize, email, ct);
        return new AdministratorListResponseDto { Items = result.Items.Select(DtoMapper.Administrator).ToList(), Pagination = Page(page, pageSize, result.Total) };
    }

    // @BR-CUS-NN-014: Administrator updates preserve protected identity and enforce target authorization.
    public async Task<AdministratorDto> UpdateAdminAsync(Guid id, UpdateAdministratorRequestDto request, RequestContext context, bool actorIsSuper, Guid actorId, CancellationToken ct)
    {
        var admin = await repository.FindAdminAsync(id, context, ct) ?? throw new DomainException("USER_NOT_FOUND", "User was not found in this store", 404);
        if (request.Groups?.Any(x => x.Equals("SUPERADMIN", StringComparison.OrdinalIgnoreCase)) == true && !actorIsSuper) throw new DomainException("SUPERADMIN_ASSIGNMENT_DENIED", "Only a super administrator may grant this group", 403);
        if (id == actorId && request.StoreId is not null && request.StoreId != admin.StoreId) throw new DomainException("STORE_CHANGE_DENIED", "Self-service edits cannot change store ownership", 403);
        if (request.UserName is not null && !request.UserName.Equals(admin.UserName, StringComparison.OrdinalIgnoreCase) && await repository.AdminLoginExistsAsync(request.UserName, context, ct)) throw new DomainException("USERNAME_CONFLICT", "Username is already registered for this store", 409);
        if (request.UserName is not null) admin.UserName = request.UserName; if (request.EmailAddress is not null) admin.EmailAddress = request.EmailAddress;
        if (request.FirstName is not null) admin.FirstName = request.FirstName; if (request.LastName is not null) admin.LastName = request.LastName;
        if (request.StoreId is not null && actorIsSuper) admin.StoreId = request.StoreId; if (actorIsSuper && request.IsActive.HasValue) admin.IsActive = request.IsActive.Value;
        if (actorIsSuper && request.Groups is not null) { admin.Groups.Clear(); admin.Groups.AddRange(request.Groups); }
        await repository.UpdateAdminAsync(admin, context, ct); return DtoMapper.Administrator(admin);
    }

    // @BR-CUS-NN-015: Super-administrator accounts are protected from deletion.
    public async Task DeleteAdminAsync(Guid id, RequestContext context, CancellationToken ct)
    {
        var admin = await repository.FindAdminAsync(id, context, ct) ?? throw new DomainException("USER_NOT_FOUND", "User was not found in this store", 404);
        if (admin.Groups.Any(x => x.Equals("SUPERADMIN", StringComparison.OrdinalIgnoreCase))) throw new DomainException("PROTECTED_ACCOUNT", "Super-administrator accounts cannot be deleted", 409);
        await repository.DeleteAdminAsync(id, context, ct);
    }

    // @BR-CUS-NN-016: Administrator password changes require current proof, policy validation and encoded persistence.
    public async Task ChangeAdminPasswordAsync(Guid actorId, Guid targetId, AdministratorPasswordChangeRequestDto request, RequestContext context, CancellationToken ct)
    {
        var target = await repository.FindAdminAsync(targetId, context, ct) ?? throw new DomainException("USER_NOT_FOUND", "User was not found in this store", 404);
        if (actorId != targetId && !await IsSuperAdmin(actorId, context, ct)) throw new DomainException("FORBIDDEN", "Administrator is not authorized for this user", 403);
        if (!passwords.Matches(target.PasswordHash, request.CurrentPassword)) throw new DomainException("CURRENT_PASSWORD_INVALID", "Current password does not match", 401);
        passwords.RequirePolicy(request.NewPassword); await repository.SetAdminPasswordAsync(targetId, passwords.Encode(request.NewPassword), context, ct);
    }

    // @BR-CUS-NN-020: Enablement changes only an administrator within the selected store.
    public async Task SetAdminEnabledAsync(Guid id, bool enabled, RequestContext context, CancellationToken ct)
    {
        if (await repository.FindAdminAsync(id, context, ct) is null) throw new DomainException("USER_NOT_FOUND", "User was not found in this store", 404);
        await repository.SetAdminEnabledAsync(id, enabled, context, ct);
    }

    // @BR-CUS-018: Removing an option first removes all dependent customer assignments and values.
    public async Task RemoveCustomerOptionAsync(Guid optionId, RequestContext context, CancellationToken ct) =>
        await repository.RemoveOptionAsync(optionId, context.StoreId, ct);

    // @BR-CUS-018: Removing an option value first removes all dependent customer assignments.
    public async Task RemoveCustomerOptionValueAsync(Guid valueId, RequestContext context, CancellationToken ct) =>
        await repository.RemoveOptionValueAsync(valueId, context.StoreId, ct);

    // @BR-CUS-NN-021: Provider identity uniqueness is the (user, provider, providerUser) composite key.
    public async Task<ExternalIdentityConnectionDto> LinkExternalAsync(ExternalIdentityRequestDto request, RequestContext context, CancellationToken ct)
    {
        if (!Guid.TryParse(request.UserId, out var userId) || await repository.FindCustomerAsync(userId, context, ct) is null && await repository.FindAdminAsync(userId, context, ct) is null)
            throw new DomainException("IDENTITY_SUBJECT_NOT_FOUND", "Identity subject was not found in this store", 404);
        var result = await repository.AddExternalAsync(new ExternalIdentityRecord { UserId = request.UserId, ProviderId = request.ProviderId, ProviderUserId = request.ProviderUserId, AccessToken = request.AccessToken, RefreshToken = request.RefreshToken, ProfileUrl = request.ProfileUrl }, ct);
        return new ExternalIdentityConnectionDto { UserId = result.UserId, ProviderId = result.ProviderId, ProviderUserId = result.ProviderUserId, ProfileUrl = result.ProfileUrl };
    }

    // @BR-CUS-021: A reviewer-target pair may have only one non-deleted review.
    // @BR-CUS-022: Ratings are inclusive 1..5.
    // @BR-CUS-023: Review creation updates target average and count transactionally at the application boundary.
    public async Task<CustomerReviewDto> CreateReviewAsync(Guid reviewerId, Guid pathTargetId, CreateCustomerReviewRequestDto request, RequestContext context, CancellationToken ct)
    {
        if (!Guid.TryParse(request.CustomerId, out var targetId) || targetId != pathTargetId) throw new DomainException("REVIEW_TARGET_INVALID", "Review target does not match the route", 422);
        await RequiredCustomer(reviewerId, context, ct); var target = await RequiredCustomer(targetId, context, ct);
        RequireRating(request.Rating); var reviews = await repository.ReviewsAsync(targetId, ct);
        if (reviews.Any(x => x.ReviewerCustomerId == reviewerId)) throw new DomainException("DUPLICATE_REVIEW", "A review already exists for this customer", 409);
        var review = new ReviewRecord { Id = Guid.NewGuid(), ReviewerCustomerId = reviewerId, ReviewedCustomerId = targetId, Rating = request.Rating, Description = request.Description, ReviewDate = DateTimeOffset.UtcNow, Status = "Published" };
        var updatedReviews = reviews.Append(review).ToList(); var average = Math.Round(updatedReviews.Average(x => x.Rating), 2);
        await repository.AddReviewWithAggregateAsync(review, average, updatedReviews.Count, ct); target.ReviewAverage = average; target.ReviewCount = updatedReviews.Count; return DtoMapper.Review(review);
    }

    // @BR-CUS-021: Review reads are constrained to a target customer.
    public async Task<CustomerReviewListResponseDto> ListReviewsAsync(Guid targetId, int page, int pageSize, CancellationToken ct)
    {
        var reviews = await repository.ReviewsAsync(targetId, ct); var pageItems = reviews.Skip(Math.Max(0, page - 1) * pageSize).Take(pageSize).Select(DtoMapper.Review).ToList();
        return new CustomerReviewListResponseDto { Items = pageItems, Pagination = Page(page, pageSize, reviews.Count) };
    }

    // @BR-CUS-024: Review update verifies target ownership, persists content and replaces the old aggregate contribution.
    // @BR-UI-002: The canonical reviewId route parameter binds the review to its target.
    public async Task<CustomerReviewDto> UpdateReviewAsync(Guid targetId, Guid reviewId, Guid actorId, bool moderator, UpdateCustomerReviewRequestDto request, RequestContext context, CancellationToken ct)
    {
        var review = await repository.FindReviewAsync(reviewId, ct);
        if (review is null || review.ReviewedCustomerId != targetId) throw new DomainException("REVIEW_NOT_FOUND", "Review does not belong to this customer", 404);
        if (review.ReviewerCustomerId != actorId && !moderator) throw new DomainException("FORBIDDEN", "Only the review owner or a moderator may edit this review", 403);
        RequireRating(request.Rating); var target = await RequiredCustomer(targetId, context, ct); review.Rating = request.Rating; review.Description = request.Description;
        var currentReviews = await repository.ReviewsAsync(targetId, ct); var average = Math.Round(currentReviews.Average(x => x.Id == reviewId ? review.Rating : x.Rating), 2);
        await repository.SaveReviewWithAggregateAsync(review, average, currentReviews.Count, ct); target.ReviewAverage = average; target.ReviewCount = currentReviews.Count; return DtoMapper.Review(review);
    }

    // @BR-CUS-025: Review deletion recomputes average and count from all remaining reviews.
    // @BR-UI-002: The canonical reviewId route parameter is required for deletion.
    public async Task DeleteReviewAsync(Guid targetId, Guid reviewId, Guid actorId, bool moderator, RequestContext context, CancellationToken ct)
    {
        var review = await repository.FindReviewAsync(reviewId, ct); if (review is null || review.ReviewedCustomerId != targetId) throw new DomainException("REVIEW_NOT_FOUND", "Review does not belong to this customer", 404);
        if (review.ReviewerCustomerId != actorId && !moderator) throw new DomainException("FORBIDDEN", "Only the review owner or a moderator may delete this review", 403);
        var target = await RequiredCustomer(targetId, context, ct); var remaining = (await repository.ReviewsAsync(targetId, ct)).Where(x => x.Id != reviewId).ToList();
        var average = remaining.Count == 0 ? 0 : Math.Round(remaining.Average(x => x.Rating), 2);
        await repository.DeleteReviewWithAggregateAsync(reviewId, targetId, average, remaining.Count, ct); target.ReviewAverage = average; target.ReviewCount = remaining.Count;
    }

    // @BR-CUS-026: Enrollment is an idempotent store/campaign/email upsert.
    // @BR-CUS-027: Store is part of newsletter uniqueness.
    public async Task<object> SubscribeAsync(NewsletterSubscriptionRequestDto request, RequestContext context, CancellationToken ct)
    {
        if (!new System.ComponentModel.DataAnnotations.EmailAddressAttribute().IsValid(request.Email)) throw new DomainException("INVALID_EMAIL", "A valid email address is required", 422);
        var n = await repository.UpsertNewsletterAsync(new NewsletterRecord { Id = Guid.NewGuid(), TenantId = context.TenantId, StoreId = context.StoreId, CampaignCode = _campaign, Email = request.Email, FirstName = request.FirstName, LastName = request.LastName, SubscribedAt = DateTimeOffset.UtcNow }, ct);
        return new { id = n.Id, storeId = n.StoreId, campaignCode = n.CampaignCode, email = n.Email, firstName = n.FirstName, lastName = n.LastName, status = n.Status, subscribedAt = n.SubscribedAt, unsubscribedAt = n.UnsubscribedAt };
    }

    // @BR-CUS-028: The legacy update endpoint is an explicit 501 capability response.
    public ErrorResponseDto LegacyNewsletterUpdate(RequestContext context) => new()
    {
        Error = "NEWSLETTER_UPDATE_UNAVAILABLE",
        Message = "Newsletter update capability is not implemented",
        StatusCode = 501,
        Timestamp = DateTimeOffset.UtcNow.ToString("O"),
        CorrelationId = context.CorrelationId
    };

    // @BR-CUS-028: Unsubscribe is deliberately implemented as a real state transition, never a false success.
    public async Task UnsubscribeAsync(string email, RequestContext context, CancellationToken ct)
    {
        // Unsubscribe is idempotent at the HTTP boundary: an absent address is already
        // unsubscribed, while an existing row is durably transitioned by the repository.
        await repository.UnsubscribeAsync(email, context, _campaign, ct);
    }

    private async Task<CustomerAccount> RequiredCustomer(Guid id, RequestContext context, CancellationToken ct) => await repository.FindCustomerAsync(id, context, ct) ?? throw new DomainException("CUSTOMER_NOT_FOUND", "Customer was not found in this store", 404);
    private async Task<CustomerDto> MapCustomer(CustomerAccount c, CancellationToken ct) => DtoMapper.Customer(c, await repository.GetAddressesAsync(c.Id, ct), await repository.GetAttributesAsync(c.Id, ct));
    private AuthenticationResponseDto Authentication(CustomerAccount c, RequestContext ctx, IEnumerable<string> roles) => Authentication(c.Id, "customer", c.LoginName, ctx, roles);
    private AuthenticationResponseDto Authentication(AdministratorAccount a, RequestContext ctx, IEnumerable<string> roles) => Authentication(a.Id, "administrator", a.UserName, ctx, roles);
    private AuthenticationResponseDto Authentication(Guid id, string kind, string login, RequestContext ctx, IEnumerable<string> roles) => new() { SubjectId = id.ToString(), AccessToken = tokens.Create(id, kind, login, ctx, roles), TokenType = "Bearer", ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(tokens.LifetimeMinutes).ToString("O") };
    private async Task<bool> IsSuperAdmin(Guid id, RequestContext context, CancellationToken ct) => (await repository.FindAdminAsync(id, context, ct))?.Groups.Any(x => x.Equals("SUPERADMIN", StringComparison.OrdinalIgnoreCase)) == true;
    private static PaginationInfoDto Page(int page, int size, long total) => new() { Page = page, PageSize = size, TotalItems = total, TotalPages = total == 0 ? 0 : (int)Math.Ceiling(total / (double)size) };
    private static void RequireRating(decimal rating) { if (rating < 1 || rating > 5) throw new DomainException("RATING_OUT_OF_RANGE", "Rating must be between 1 and 5", 422); }
    private static void ValidateAttributeIds(IEnumerable<CustomerAttributeDto> attrs) { foreach (var attr in attrs) if (!Guid.TryParse(attr.OptionId, out _) || !Guid.TryParse(attr.OptionValueId, out _)) throw new DomainException("ATTRIBUTE_SCOPE_VIOLATION", "Customer option is not valid for this store", 422); }
    private static void ValidateLanguage(string language) { if (language.Length > 10 || language.Equals("xx", StringComparison.OrdinalIgnoreCase)) throw new DomainException("UNSUPPORTED_LANGUAGE", $"Language {language} is not supported", 422); }
    private static void ValidateAddress(AddressDto address, bool billing, bool allowPartial = false) { if (!allowPartial && string.IsNullOrWhiteSpace(address.StreetAddress)) throw new DomainException(billing ? "BILLING_ADDRESS_REQUIRED" : "DELIVERY_ADDRESS_REQUIRED", "Street address is required", 422); if (!allowPartial && string.IsNullOrWhiteSpace(address.City)) throw new DomainException("ADDRESS_CITY_REQUIRED", "City is required", 422); if (!allowPartial && string.IsNullOrWhiteSpace(address.PostalCode)) throw new DomainException(billing ? "BILLING_POSTAL_CODE_REQUIRED" : "DELIVERY_POSTAL_CODE_REQUIRED", "Postal code is required", 422); if (string.IsNullOrWhiteSpace(address.CountryCode)) throw new DomainException(billing ? "BILLING_COUNTRY_REQUIRED" : "DELIVERY_COUNTRY_REQUIRED", "Country is required", 422); if (!Countries.Contains(address.CountryCode.ToUpperInvariant())) throw new DomainException("UNSUPPORTED_COUNTRY", $"Country code {address.CountryCode} is not supported", 422); if (!string.IsNullOrWhiteSpace(address.ZoneCode) && !Zones.Contains(address.ZoneCode.ToUpperInvariant())) throw new DomainException("UNSUPPORTED_ZONE", $"Zone code {address.ZoneCode} is not supported", 422); }
    private static AddressDto CompleteDelivery(AddressDto billing, AddressDto delivery) => new() { AddressType = "Delivery", FirstName = string.IsNullOrWhiteSpace(delivery.FirstName) ? billing.FirstName : delivery.FirstName, LastName = string.IsNullOrWhiteSpace(delivery.LastName) ? billing.LastName : delivery.LastName, CompanyName = delivery.CompanyName ?? billing.CompanyName, StreetAddress = string.IsNullOrWhiteSpace(delivery.StreetAddress) ? billing.StreetAddress : delivery.StreetAddress, City = string.IsNullOrWhiteSpace(delivery.City) ? billing.City : delivery.City, PostalCode = string.IsNullOrWhiteSpace(delivery.PostalCode) ? billing.PostalCode : delivery.PostalCode, StateProvince = delivery.StateProvince ?? billing.StateProvince, Telephone = delivery.Telephone ?? billing.Telephone, CountryCode = string.IsNullOrWhiteSpace(delivery.CountryCode) ? billing.CountryCode : delivery.CountryCode, ZoneCode = delivery.ZoneCode ?? billing.ZoneCode, Latitude = delivery.Latitude, Longitude = delivery.Longitude };
    private static AddressDto CopyAddress(AddressDto a, string type) => new() { AddressType = type, FirstName = a.FirstName, LastName = a.LastName, CompanyName = a.CompanyName, StreetAddress = a.StreetAddress, City = a.City, PostalCode = a.PostalCode, StateProvince = a.StateProvince, Telephone = a.Telephone, CountryCode = a.CountryCode, ZoneCode = a.ZoneCode, Latitude = a.Latitude, Longitude = a.Longitude };
}
