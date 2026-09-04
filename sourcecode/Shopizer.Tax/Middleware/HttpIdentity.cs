using Shopizer.Tax.Models;

namespace Shopizer.Tax.Middleware;

public static class HttpIdentity
{
    public static RequestContext Context(HttpContext http) => RequestContext.From(http);

    public static Guid RequireAuthenticated(HttpContext http)
    {
        if (http.User.Identity?.IsAuthenticated != true)
            throw new DomainException("UNAUTHORIZED", "Authentication token is invalid", 401);
        return http.User.SubjectId()
            ?? throw new DomainException("UNAUTHORIZED", "Authentication token is invalid", 401);
    }

    public static Guid RequireAdministrator(HttpContext http)
    {
        var subject = RequireAuthenticated(http);
        if (!string.Equals(http.User.Kind(), "administrator", StringComparison.OrdinalIgnoreCase))
            throw new DomainException("FORBIDDEN", "Administrator authorization is required", 403);
        return subject;
    }
}
