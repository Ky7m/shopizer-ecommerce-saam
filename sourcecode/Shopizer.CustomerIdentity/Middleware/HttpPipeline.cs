using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Shopizer.CustomerIdentity.Data;
using Shopizer.CustomerIdentity.DTOs;
using Shopizer.CustomerIdentity.Models;
using Shopizer.CustomerIdentity.Services;

namespace Shopizer.CustomerIdentity.Middleware;

public sealed class CorrelationMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var correlation = context.Request.Headers["x-correlation-id"].FirstOrDefault();
        correlation = string.IsNullOrWhiteSpace(correlation) ? Guid.NewGuid().ToString() : correlation;
        context.Response.Headers["x-correlation-id"] = correlation;
        await next(context);
    }
}

public sealed class ErrorMiddleware(RequestDelegate next, ILogger<ErrorMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try { await next(context); }
        catch (DomainException ex)
        {
            await WriteError(context, ex.StatusCode, ex.Code, ex.Message);
        }
        catch (FormatException)
        {
            await WriteError(context, 400, "INVALID_REQUEST", "A route identifier is invalid");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled customer identity failure");
            await WriteError(context, 500, "INTERNAL_ERROR", "Internal server error");
        }
    }

    private static async Task WriteError(HttpContext context, int status, string code, string message)
    {
        if (context.Response.HasStarted) return;
        context.Response.StatusCode = status; context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(new ErrorResponseDto
        {
            Error = code, Message = message, StatusCode = status,
            Timestamp = DateTimeOffset.UtcNow.ToString("O"),
            CorrelationId = context.Response.Headers["x-correlation-id"].FirstOrDefault()
        });
    }
}

public sealed class TokenMiddleware(RequestDelegate next, TokenService tokens)
{
    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Headers.Authorization.ToString() is { Length: > 7 } authorization &&
            authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                var request = RequestContext.From(context);
                var token = await tokens.ValidateAsync(authorization[7..].Trim(), request, context.RequestAborted);
                if (token is not null)
                {
                    var claims = new List<Claim>
                    {
                        new(ClaimTypes.NameIdentifier, token.SubjectId.ToString()), new(ClaimTypes.Name, token.Login),
                        new("sub", token.SubjectId.ToString()), new("kind", token.Kind), new("tenantId", token.TenantId), new("storeId", token.StoreId)
                    };
                    claims.AddRange(token.Roles.Select(role => new Claim(ClaimTypes.Role, role)));
                    context.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Bearer"));
                }
            }
            catch (DomainException) { }
        }
        await next(context);
    }
}

public static class HttpIdentity
{
    public static RequestContext Context(HttpContext http) => RequestContext.From(http);
    public static Guid RequireSubject(HttpContext http, string kind, params string[] roles)
    {
        if (http.User.Identity?.IsAuthenticated != true || !http.User.Kind().Equals(kind, StringComparison.OrdinalIgnoreCase))
            throw new DomainException("UNAUTHORIZED", "Authentication token is invalid", 401);
        if (roles.Length > 0 && !http.User.HasRole(roles)) throw new DomainException("FORBIDDEN", "Administrator is not authorized for this operation", 403);
        return http.User.SubjectId() ?? throw new DomainException("UNAUTHORIZED", "Authentication token is invalid", 401);
    }
}

[ApiController]
[Route("api/v1")]
public sealed class CustomerController(IdentityService service) : ControllerBase
{
    [HttpPost("customer-auth/registrations")]
    public async Task<ActionResult<AuthenticationResponseDto>> Register(CreateCustomerRequestDto request, CancellationToken ct) => StatusCode(201, (await service.RegisterAsync(request, HttpIdentity.Context(HttpContext), ct)).Token);

    [HttpPost("customer-auth/login")]
    public async Task<AuthenticationResponseDto> Login(AuthenticationRequestDto request, CancellationToken ct) => await service.LoginAsync(request, HttpIdentity.Context(HttpContext), false, ct);

    [HttpGet("customer-auth/refresh")]
    public async Task<AuthenticationResponseDto> Refresh(CancellationToken ct)
    {
        var context = HttpIdentity.Context(HttpContext); var id = HttpIdentity.RequireSubject(HttpContext, "customer");
        var authorization = Request.Headers.Authorization.ToString();
        var token = await serviceToken().ValidateAsync(authorization[7..].Trim(), context, ct) ?? throw new DomainException("REFRESH_NOT_ALLOWED", "Token cannot be refreshed", 400);
        return await service.RefreshAsync(token, context, ct);
        TokenService serviceToken() => HttpContext.RequestServices.GetRequiredService<TokenService>();
    }

    [HttpGet("customers")]
    public async Task<CustomerListResponseDto> List(int page = 1, int pageSize = 20, string? name = null, string? email = null, string? firstName = null, string? lastName = null, string? countryCode = null, CancellationToken ct = default)
    {
        HttpIdentity.RequireSubject(HttpContext, "administrator", "ADMIN", "SUPERADMIN", "ADMIN_RETAIL", "STORE_ADMIN");
        return await service.ListCustomersAsync(HttpIdentity.Context(HttpContext), page, pageSize, name, email, firstName, lastName, countryCode, ct);
    }

    [HttpPost("customers")]
    public async Task<ActionResult<CustomerDto>> Create(CreateCustomerRequestDto request, CancellationToken ct)
    {
        HttpIdentity.RequireSubject(HttpContext, "administrator", "ADMIN", "SUPERADMIN", "ADMIN_RETAIL", "STORE_ADMIN");
        return StatusCode(201, (await service.RegisterAsync(request, HttpIdentity.Context(HttpContext), ct)).Customer is { } c ? await service.CustomerDtoAsync(c.Id, HttpIdentity.Context(HttpContext), ct) : null);
    }

    [HttpGet("customers/{customerId}")]
    public async Task<CustomerDto> Get(string customerId, CancellationToken ct)
    {
        HttpIdentity.RequireSubject(HttpContext, "administrator", "ADMIN", "SUPERADMIN", "ADMIN_RETAIL", "STORE_ADMIN");
        return await service.CustomerDtoAsync(Guid.Parse(customerId), HttpIdentity.Context(HttpContext), ct);
    }

    [HttpGet("customers/me")]
    public async Task<CustomerDto> Me(CancellationToken ct) => await service.CustomerDtoAsync(HttpIdentity.RequireSubject(HttpContext, "customer"), HttpIdentity.Context(HttpContext), ct);

    [HttpPut("customers/{customerId}")]
    public async Task<CustomerDto> Update(string customerId, UpdateCustomerRequestDto request, CancellationToken ct)
    {
        var id = Guid.Parse(customerId); var context = HttpIdentity.Context(HttpContext);
        var administrator = HttpContext.User.Kind().Equals("administrator", StringComparison.OrdinalIgnoreCase);
        var subject = administrator
            ? HttpIdentity.RequireSubject(HttpContext, "administrator", "ADMIN", "SUPERADMIN", "ADMIN_RETAIL", "STORE_ADMIN")
            : HttpIdentity.RequireSubject(HttpContext, "customer", "ROLE_CUSTOMER_AUTHENTICATED");
        if (!administrator && subject != id) throw new DomainException("CUSTOMER_SCOPE_VIOLATION", "Authenticated customer cannot modify another profile", 403);
        var current = await service.CustomerDtoAsync(id, context, ct);
        if (request.EmailAddress is not null || request.FirstName is not null || request.LastName is not null || request.Gender is not null || request.Language is not null || request.CompanyName is not null || request.Attributes is not null)
        {
            // The service owns persistence and address invariants; this action retains the copied contract DTO.
            var account = await HttpContext.RequestServices.GetRequiredService<IdentityRepository>().FindCustomerAsync(id, context, ct) ?? throw new DomainException("CUSTOMER_NOT_FOUND", "Customer was not found in this store", 404);
            if (request.EmailAddress is not null) account.EmailAddress = request.EmailAddress; if (request.FirstName is not null || request.LastName is not null) { var address = current.Billing; if (address is not null) { address.FirstName = request.FirstName ?? address.FirstName; address.LastName = request.LastName ?? address.LastName; await service.UpdateAddressesAsync(id, new AddressUpdateRequestDto { Billing = address }, context, ct); } }
            if (request.Gender is not null) account.Gender = request.Gender; if (request.Language is not null) account.DefaultLanguageCode = request.Language; if (request.CompanyName is not null) account.CompanyName = request.CompanyName;
            await HttpContext.RequestServices.GetRequiredService<IdentityRepository>().UpdateCustomerAsync(account, context, ct);
        }
        return await service.CustomerDtoAsync(id, context, ct);
    }

    [HttpPatch("customers/me")]
    public Task<CustomerDto> UpdateMe(UpdateCustomerRequestDto request, CancellationToken ct) => Update(HttpIdentity.RequireSubject(HttpContext, "customer").ToString(), request, ct);

    [HttpPatch("customers/{customerId}/address")]
    public async Task<IActionResult> Address(string customerId, AddressUpdateRequestDto request, CancellationToken ct)
    {
        HttpIdentity.RequireSubject(HttpContext, "administrator", "ADMIN", "SUPERADMIN", "ADMIN_RETAIL", "STORE_ADMIN");
        await service.UpdateAddressesAsync(Guid.Parse(customerId), request, HttpIdentity.Context(HttpContext), ct); return NoContent();
    }

    [HttpPatch("customers/me/address")]
    public async Task<IActionResult> MyAddress(AddressUpdateRequestDto request, CancellationToken ct) { await service.UpdateAddressesAsync(HttpIdentity.RequireSubject(HttpContext, "customer"), request, HttpIdentity.Context(HttpContext), ct); return NoContent(); }

    [HttpDelete("customers/{customerId}")]
    public async Task<IActionResult> Delete(string customerId, CancellationToken ct) { HttpIdentity.RequireSubject(HttpContext, "administrator", "ADMIN", "SUPERADMIN", "ADMIN_RETAIL"); await service.DeleteCustomerAsync(Guid.Parse(customerId), HttpIdentity.Context(HttpContext), ct); return NoContent(); }
    [HttpDelete("customers/me")]
    public async Task<IActionResult> DeleteMe(CancellationToken ct) { await service.DeleteCustomerAsync(HttpIdentity.RequireSubject(HttpContext, "customer"), HttpIdentity.Context(HttpContext), ct); return NoContent(); }

    [HttpPost("customers/me/password")]
    public async Task<IActionResult> Password(CustomerPasswordChangeRequestDto request, CancellationToken ct) { var id = HttpIdentity.RequireSubject(HttpContext, "customer"); await service.ChangeCustomerPasswordAsync(id, request, HttpIdentity.Context(HttpContext), ct); return NoContent(); }

    [HttpPost("customer-password-resets")]
    public async Task<ActionResult<ResetRequestResponseDto>> Reset(ResetRequestDto request, CancellationToken ct) { await service.RequestResetAsync(request, HttpIdentity.Context(HttpContext), false, ct); return Accepted(new ResetRequestResponseDto { Status = "ResetLinkSent" }); }
    [HttpGet("customer-password-resets/{storeCode}/{token}")]
    public async Task<ResetTokenValidationResponseDto> Verify(string storeCode, string token, CancellationToken ct) { var r = await service.VerifyResetAsync(storeCode, token, "Customer", HttpIdentity.Context(HttpContext), ct); return new() { Valid = true, ExpiresAt = r.ExpiresAt.ToString("O") }; }
    [HttpPost("customer-password-resets/{storeCode}/{token}")]
    public async Task<IActionResult> Complete(string storeCode, string token, ResetPasswordRequestDto request, CancellationToken ct) { await service.CompleteResetAsync(storeCode, token, request, HttpIdentity.Context(HttpContext), false, ct); return NoContent(); }

    [HttpPost("newsletter-subscriptions")]
    public async Task<ActionResult<object>> Subscribe(NewsletterSubscriptionRequestDto request, CancellationToken ct) => StatusCode(201, await service.SubscribeAsync(request, HttpIdentity.Context(HttpContext), ct));
    [HttpPut("newsletter-subscriptions/{email}")]
    public IActionResult Legacy(string email) => StatusCode(501, service.LegacyNewsletterUpdate(HttpIdentity.Context(HttpContext)));
    [HttpDelete("newsletter-subscriptions/{email}")]
    public async Task<IActionResult> Unsubscribe(string email, CancellationToken ct) { await service.UnsubscribeAsync(email, HttpIdentity.Context(HttpContext), ct); return NoContent(); }

    [HttpGet("customers/{customerId}/reviews")]
    public async Task<CustomerReviewListResponseDto> Reviews(string customerId, int page = 1, int pageSize = 20, CancellationToken ct = default) { await service.CustomerDtoAsync(Guid.Parse(customerId), HttpIdentity.Context(HttpContext), ct); return await service.ListReviewsAsync(Guid.Parse(customerId), page, pageSize, ct); }
    [HttpPost("customers/{customerId}/reviews")]
    public async Task<ActionResult<CustomerReviewDto>> CreateReview(string customerId, CreateCustomerReviewRequestDto request, CancellationToken ct) => StatusCode(201, await service.CreateReviewAsync(HttpIdentity.RequireSubject(HttpContext, "customer"), Guid.Parse(customerId), request, HttpIdentity.Context(HttpContext), ct));
    [HttpPut("customers/{customerId}/reviews/{reviewId}")]
    public async Task<CustomerReviewDto> UpdateReview(string customerId, string reviewId, UpdateCustomerReviewRequestDto request, CancellationToken ct)
    {
        var administrator = HttpContext.User.Kind().Equals("administrator", StringComparison.OrdinalIgnoreCase);
        var actor = administrator
            ? HttpIdentity.RequireSubject(HttpContext, "administrator", "ADMIN", "SUPERADMIN", "ADMIN_RETAIL", "STORE_ADMIN")
            : HttpIdentity.RequireSubject(HttpContext, "customer");
        var moderator = administrator || HttpContext.User.HasRole("ADMIN", "SUPERADMIN", "ADMIN_RETAIL", "STORE_ADMIN");
        return await service.UpdateReviewAsync(Guid.Parse(customerId), Guid.Parse(reviewId), actor, moderator, request, HttpIdentity.Context(HttpContext), ct);
    }
    [HttpDelete("customers/{customerId}/reviews/{reviewId}")]
    public async Task<IActionResult> DeleteReview(string customerId, string reviewId, CancellationToken ct) { var administrator = HttpContext.User.Kind().Equals("administrator", StringComparison.OrdinalIgnoreCase); var actor = administrator ? HttpIdentity.RequireSubject(HttpContext, "administrator", "ADMIN", "SUPERADMIN", "ADMIN_RETAIL", "STORE_ADMIN") : HttpIdentity.RequireSubject(HttpContext, "customer"); var moderator = administrator || HttpContext.User.HasRole("ADMIN", "SUPERADMIN", "ADMIN_RETAIL", "STORE_ADMIN"); await service.DeleteReviewAsync(Guid.Parse(customerId), Guid.Parse(reviewId), actor, moderator, HttpIdentity.Context(HttpContext), ct); return NoContent(); }

    [HttpPost("external-identities")]
    public async Task<ActionResult<ExternalIdentityConnectionDto>> ExternalIdentity(ExternalIdentityRequestDto request, CancellationToken ct)
    {
        var administrator = HttpContext.User.Kind().Equals("administrator", StringComparison.OrdinalIgnoreCase);
        HttpIdentity.RequireSubject(HttpContext, administrator ? "administrator" : "customer");
        return StatusCode(201, await service.LinkExternalAsync(request, HttpIdentity.Context(HttpContext), ct));
    }
}
