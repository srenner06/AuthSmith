using System.Security.Claims;
using Asp.Versioning;
using AuthSmith.Api.Extensions;
using AuthSmith.Application.Services.Auth;
using AuthSmith.Contracts.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuthSmith.Api.Controllers;

[ApiController]
[Route("api/v1/sessions")]
[ApiVersion("1.0")]
[Authorize]
public class SessionManagementController : ControllerBase
{
    private readonly ISessionManagementService _sessionManagementService;

    public SessionManagementController(ISessionManagementService sessionManagementService)
    {
        _sessionManagementService = sessionManagementService;
    }

    /// <summary>
    /// Get all active sessions for the current user.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(UserSessionsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserSessionsDto>> GetSessionsAsync(CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId == null)
        {
            return Unauthorized();
        }

        var currentTokenId = GetCurrentTokenId();
        var result = await _sessionManagementService.GetUserSessionsAsync(userId.Value, currentTokenId, cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>
    /// Revoke a specific session.
    /// </summary>
    [HttpDelete("{sessionId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RevokeSessionAsync(
        Guid sessionId,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId == null)
        {
            return Unauthorized();
        }

        var result = await _sessionManagementService.RevokeSessionAsync(userId.Value, sessionId, cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>
    /// Revoke all sessions except the current one.
    /// </summary>
    /// <remarks>
    /// Requires password confirmation for security.
    /// </remarks>
    [HttpPost("revoke-all")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RevokeAllSessionsAsync(
        [FromBody] RevokeAllSessionsDto request,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId == null)
        {
            return Unauthorized();
        }

        var currentTokenId = GetCurrentTokenId();
        var result = await _sessionManagementService.RevokeAllSessionsAsync(
            userId.Value,
            currentTokenId,
            request.Password,
            cancellationToken);
        return result.ToActionResult();
    }

    private Guid? GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                          ?? User.FindFirst("sub")?.Value;

        return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
    }

    private Guid? GetCurrentTokenId()
    {
        // This would come from JWT claims if we add it during token generation
        var tokenIdClaim = User.FindFirst("token_id")?.Value;
        return Guid.TryParse(tokenIdClaim, out var tokenId) ? tokenId : null;
    }
}
