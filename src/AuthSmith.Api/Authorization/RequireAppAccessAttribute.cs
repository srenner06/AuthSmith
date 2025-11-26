using AuthSmith.Api.Constants;
using Microsoft.AspNetCore.Authorization;

namespace AuthSmith.Api.Authorization;

/// <summary>
/// Authorization attribute that requires App-level or Admin-level API key access.
/// </summary>
public class RequireAppAccessAttribute : AuthorizeAttribute
{
    public RequireAppAccessAttribute()
    {
        Policy = AuthorizationConstants.RequireAppAccessPolicy;
    }
}

