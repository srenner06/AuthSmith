using Asp.Versioning;
using AuthSmith.Contracts.System;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuthSmith.Api.Controllers;

/// <summary>
/// Ping endpoint for health checks and version information.
/// </summary>
[ApiController]
[Route("api")]
[ApiVersion("1.0")]
[AllowAnonymous]
public class PingController : ControllerBase
{
    /// <summary>
    /// Ping endpoint that returns build and version information.
    /// </summary>
    /// <returns>Build metadata and authentication status</returns>
    [HttpGet("ping")]
    [ProducesResponseType(typeof(PingResponseDto), StatusCodes.Status200OK)]
    public IActionResult Ping()
    {
        // Check if user is authenticated (either via JWT or API key)
        var isAuthenticated = User?.Identity?.IsAuthenticated ?? false;

        var response = new PingResponseDto
        {
            VersionTag = Version.VersionTag,
            BuildNumber = Version.BuildNumber,
            BuildTime = Version.BuildTime,
            CommitHash = Version.CommitHash,
            Message = "Alive and ready to serve",
            Authenticated = isAuthenticated
        };

        return Ok(response);
    }
}
