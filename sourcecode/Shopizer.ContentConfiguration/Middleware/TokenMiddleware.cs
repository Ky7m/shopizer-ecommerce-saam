using System.Security.Claims;
using Shopizer.ContentConfiguration.Models;
using Shopizer.ContentConfiguration.Services;

namespace Shopizer.ContentConfiguration.Middleware;

public sealed class TokenMiddleware(RequestDelegate next, TokenService tokens)
{
    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Headers.Authorization.ToString() is { Length: > 7 } value &&
            value.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                var storeRequired = !context.Request.Path.StartsWithSegments("/api/v1/services/private/system/module");
                var request = RequestContext.From(context, storeRequired);
                var token = tokens.Validate(value[7..].Trim(), request);
                if (token is not null)
                {
                    var claims = new[]
                    {
                        new Claim(ClaimTypes.NameIdentifier, token.Id.ToString()),
                        new Claim("sub", token.Id.ToString()), new Claim("kind", token.Kind),
                        new Claim("tenantId", token.TenantId), new Claim("storeId", token.StoreId)
                    }.Concat(token.Roles.Select(x => new Claim(ClaimTypes.Role, x)));
                    context.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Bearer"));
                }
            }
            catch (DomainException) { }
        }
        await next(context);
    }
}
