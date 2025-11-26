using Asp.Versioning;
using AuthSmith.Api.Authorization;
using AuthSmith.Api.Extensions;
using AuthSmith.Application.Services.Authorization;
using AuthSmith.Application.Services.Users;
using AuthSmith.Contracts.Users;
using Microsoft.AspNetCore.Mvc;

namespace AuthSmith.Api.Controllers;

[ApiController]
[Route("api/v1/users")]
[ApiVersion("1.0")]
[RequireAdminAccess]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly IAuthorizationService _authorizationService;

    public UsersController(
        IUserService userService,
        IAuthorizationService authorizationService)
    {
        _userService = userService;
        _authorizationService = authorizationService;
    }

    /// <summary>
    /// Get a user by ID.
    /// </summary>
    [HttpGet("{userId}")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserDto>> GetUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        var result = await _userService.GetByIdAsync(userId, cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>
    /// Search users by query string.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<UserDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<UserDto>>> SearchUsersAsync(
        [FromQuery] string? query,
        CancellationToken cancellationToken)
    {
        var users = await _userService.SearchAsync(query, cancellationToken);
        return Ok(users);
    }

    /// <summary>
    /// Get user permissions for an application and optional module.
    /// </summary>
    [HttpGet("{userId}/permissions")]
    [ProducesResponseType(typeof(List<string>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<string>>> GetUserPermissionsAsync(
        Guid userId,
        [FromQuery] string appKey,
        [FromQuery] string? moduleName,
        CancellationToken cancellationToken)
    {
        var result = await _authorizationService.GetUserPermissionsAsync(userId, appKey, moduleName, cancellationToken);
        if (result.TryPickT1(out var notFoundError, out var permissions))
        {
            return NotFound(new { error = notFoundError.Message ?? "Resource not found." });
        }
        return Ok(permissions.ToList());
    }

    /// <summary>
    /// Assign a role to a user.
    /// </summary>
    [HttpPost("{userId}/roles")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AssignRoleAsync(
        Guid userId,
        [FromBody] AssignRoleRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await _userService.AssignRoleAsync(userId, request.RoleId, cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>
    /// Remove a role from a user.
    /// </summary>
    [HttpDelete("{userId}/roles/{roleId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveRoleAsync(
        Guid userId,
        Guid roleId,
        CancellationToken cancellationToken)
    {
        var result = await _userService.RemoveRoleAsync(userId, roleId, cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>
    /// Assign a direct permission to a user.
    /// </summary>
    [HttpPost("{userId}/permissions")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AssignPermissionAsync(
        Guid userId,
        [FromBody] AssignPermissionRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await _userService.AssignPermissionAsync(userId, request.PermissionId, cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>
    /// Remove a direct permission from a user.
    /// </summary>
    [HttpDelete("{userId}/permissions/{permissionId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemovePermissionAsync(
        Guid userId,
        Guid permissionId,
        CancellationToken cancellationToken)
    {
        var result = await _userService.RemovePermissionAsync(userId, permissionId, cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>
    /// Revoke all assignments (roles and permissions) for a user in an application.
    /// </summary>
    [HttpDelete("{userId}/apps/{appId}/assignments")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RevokeAllAssignmentsAsync(
        Guid userId,
        Guid appId,
        CancellationToken cancellationToken)
    {
        var result = await _userService.RevokeAllAssignmentsAsync(userId, appId, cancellationToken);
        return result.ToActionResult();
    }
}
