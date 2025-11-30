using AuthSmith.Contracts.Auth;
using Refit;

namespace AuthSmith.Sdk.Sessions;

/// <summary>
/// Client for session management endpoints.
/// </summary>
public interface ISessionManagementClient
{
    /// <summary>
    /// Get all active sessions for the authenticated user.
    /// </summary>
    [Get("/api/v1/sessions")]
    Task<UserSessionsDto> GetActiveSessionsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Revoke a specific session.
    /// </summary>
    [Delete("/api/v1/sessions/{sessionId}")]
    Task RevokeSessionAsync(Guid sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Revoke all sessions except the current one.
    /// </summary>
    [Delete("/api/v1/sessions/revoke-others")]
    Task RevokeOtherSessionsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Revoke all sessions including the current one.
    /// </summary>
    [Delete("/api/v1/sessions/revoke-all")]
    Task RevokeAllSessionsAsync(CancellationToken cancellationToken = default);
}
