namespace AuthSmith.Application.Services.Context;

/// <summary>
/// Provides access to HTTP request context information for audit logging.
/// </summary>
public interface IRequestContextService
{
    /// <summary>
    /// Gets the client's IP address from the current HTTP request.
    /// </summary>
    string? GetClientIpAddress();

    /// <summary>
    /// Gets the User-Agent header from the current HTTP request.
    /// </summary>
    string? GetUserAgent();

    /// <summary>
    /// Gets the authenticated user ID from the current HTTP context, if available.
    /// </summary>
    Guid? GetCurrentUserId();
}
