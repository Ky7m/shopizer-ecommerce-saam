using System.Security.Claims;
using Shopizer.PricingPromotions.Models;
using Shopizer.PricingPromotions.Services;

namespace Shopizer.PricingPromotions.Middleware;

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
                        new(ClaimTypes.NameIdentifier, token.SubjectId.ToString()),
                        new(ClaimTypes.Name, token.Login),
                        new("sub", token.SubjectId.ToString()),
                        new("kind", token.Kind),
                        new("tenantId", token.TenantId),
                        new("storeId", token.StoreId)
                    };
                    claims.AddRange(token.Roles.Select(role => new Claim(ClaimTypes.Role, role)));
                    context.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Bearer"));
                }
            }
            catch (DomainException)
            {
                // Authorization remains action-level so every failure uses the service error contract.
            }
        }
        await next(context);
    }
}
