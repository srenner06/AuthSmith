using System.Security.Claims;
using Asp.Versioning;
using AuthSmith.Api.Extensions;
using AuthSmith.Application.Services.Users;
using AuthSmith.Contracts.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuthSmith.Api.Controllers;

[ApiController]
[Route("api/v1/profile")]
[ApiVersion("1.0")]
[Authorize] // Require authentication for all endpoints
public class UserProfileController : ControllerBase
{
    private readonly IUserProfileService _userProfileService;

    public UserProfileController(IUserProfileService userProfileService)
    {
        _userProfileService = userProfileService;
    }

    /// <summary>
    /// Get current user's profile.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(UserProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserProfileDto>> GetProfileAsync(CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId == null)
        {
            return Unauthorized();
        }

        var result = await _userProfileService.GetProfileAsync(userId.Value, cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>
    /// Update current user's profile.
    /// </summary>
    [HttpPut]
    [ProducesResponseType(typeof(UserProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UserProfileDto>> UpdateProfileAsync(
        [FromBody] UpdateProfileDto request,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId == null)
        {
            return Unauthorized();
        }

        var result = await _userProfileService.UpdateProfileAsync(userId.Value, request, cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>
    /// Change current user's password.
    /// </summary>
    [HttpPost("change-password")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ChangePasswordAsync(
        [FromBody] ChangePasswordDto request,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId == null)
        {
            return Unauthorized();
        }

        var result = await _userProfileService.ChangePasswordAsync(userId.Value, request, cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>
    /// Delete current user's account.
    /// </summary>
    /// <remarks>
    /// This performs a soft delete (deactivates the account).
    /// Requires password confirmation and typing "DELETE" to confirm.
    /// </remarks>
    [HttpDelete]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteAccountAsync(
        [FromBody] DeleteAccountDto request,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId == null)
        {
            return Unauthorized();
        }

        var result = await _userProfileService.DeleteAccountAsync(userId.Value, request, cancellationToken);
        return result.ToActionResult();
    }

    private Guid? GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                          ?? User.FindFirst("sub")?.Value;

        return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
    }
}
