using System.Security.Claims;
using Shopizer.Tax.Models;
using Shopizer.Tax.Services;

namespace Shopizer.Tax.Middleware;

public sealed class TokenMiddleware(RequestDelegate next, TokenService tokens)
{
    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Headers.Authorization.ToString() is { Length: > 7 } authorization &&
            authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                var tokenContext = RequestContext.From(context);
                var token = await tokens.ValidateAsync(authorization[7..].Trim(), tokenContext, context.RequestAborted);
                if (token is not null)
                {
                    var claims = new[]
                    {
                        new Claim(ClaimTypes.NameIdentifier, token.SubjectId.ToString()),
                        new Claim("sub", token.SubjectId.ToString()),
                        new Claim("kind", token.Kind),
                        new Claim("tenantId", token.TenantId),
                        new Claim("storeId", token.StoreId)
                    };
                    context.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Bearer"));
                }
            }
            catch (DomainException)
            {
            }
        }
        await next(context);
    }
}
