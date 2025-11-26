using AuthSmith.Api.Constants;
using Microsoft.AspNetCore.Authorization;

namespace AuthSmith.Api.Authorization;

/// <summary>
/// Authorization attribute that requires Admin-level API key access.
/// </summary>
public class RequireAdminAccessAttribute : AuthorizeAttribute
{
    public RequireAdminAccessAttribute()
    {
        Policy = AuthorizationConstants.RequireAdminAccessPolicy;
    }
}

