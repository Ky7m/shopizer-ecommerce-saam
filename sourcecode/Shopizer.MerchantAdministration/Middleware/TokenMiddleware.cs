using System.Security.Claims;
using Shopizer.MerchantAdministration.Models;
using Shopizer.MerchantAdministration.Services;

namespace Shopizer.MerchantAdministration.Middleware;

public sealed class TokenMiddleware(RequestDelegate next, TokenService tokens)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var authorization = context.Request.Headers.Authorization.ToString();
        if (authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                var token = tokens.Validate(authorization[7..].Trim(), RequestContext.From(context));
                if (token is not null)
                {
                    var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, token.SubjectId.ToString()), new("sub", token.SubjectId.ToString()), new(ClaimTypes.Name, token.Login), new("kind", token.Kind), new("tenantId", token.TenantId), new("storeId", token.StoreId) };
                    claims.AddRange(token.Roles.Select(x => new Claim(ClaimTypes.Role, x))); context.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Bearer"));
                }
            }
            catch (DomainException) { }
        }
        await next(context);
    }
}
