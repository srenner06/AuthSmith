using AuthSmith.Contracts.System;
using Refit;

namespace AuthSmith.Sdk.System;

/// <summary>
/// Client for system endpoints (health checks, version info, etc.).
/// </summary>
public interface ISystemClient
{
    /// <summary>
    /// Ping endpoint that returns build and version information.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Build metadata and authentication status</returns>
    [Get("/api/ping")]
    Task<PingResponseDto> PingAsync(CancellationToken cancellationToken = default);
}
