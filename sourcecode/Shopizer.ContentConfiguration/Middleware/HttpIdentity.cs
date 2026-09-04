using Shopizer.ContentConfiguration.Models;

namespace Shopizer.ContentConfiguration.Middleware;

public static class HttpIdentity
{
    public static RequestContext Context(HttpContext http, bool storeRequired = true) =>
        RequestContext.From(http, storeRequired);

    public static Guid RequireAdministrator(HttpContext http, params string[] roles)
    {
        if (http.User.Identity?.IsAuthenticated != true ||
            !http.User.Kind().Equals("administrator", StringComparison.OrdinalIgnoreCase))
            throw new DomainException("UNAUTHORIZED", "Authentication token is invalid", 401);
        if (!http.User.HasRole(roles))
            throw new DomainException("FORBIDDEN", "Administrator is not authorized for this operation", 403);
        return http.User.SubjectId() ?? throw new DomainException("UNAUTHORIZED", "Authentication token is invalid", 401);
    }
}
