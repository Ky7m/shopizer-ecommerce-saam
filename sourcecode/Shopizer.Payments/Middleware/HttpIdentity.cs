using Shopizer.Payments.Models;

namespace Shopizer.Payments.Middleware;

public static class HttpIdentity
{
    public static RequestContext Context(HttpContext http) => RequestContext.From(http);

    public static Guid RequireSubject(HttpContext http, string kind, params string[] roles)
    {
        if (http.User.Identity?.IsAuthenticated != true ||
            !http.User.Kind().Equals(kind, StringComparison.OrdinalIgnoreCase))
            throw new DomainException("UNAUTHORIZED", "Authentication token is invalid", 401);
        if (!http.User.HasRole(roles))
            throw new DomainException("FORBIDDEN", "The authenticated identity is not authorized", 403);
        return http.User.SubjectId() ??
               throw new DomainException("UNAUTHORIZED", "Authentication token is invalid", 401);
    }
}
