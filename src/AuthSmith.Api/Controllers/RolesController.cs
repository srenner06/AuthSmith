using Asp.Versioning;
using AuthSmith.Api.Authorization;
using AuthSmith.Api.Extensions;
using AuthSmith.Application.Services.Roles;
using AuthSmith.Contracts.Roles;
using Microsoft.AspNetCore.Mvc;

namespace AuthSmith.Api.Controllers;

[ApiController]
[Route("api/v1/apps/{appId}/roles")]
[ApiVersion("1.0")]
[RequireAdminAccess]
public class RolesController : ControllerBase
{
    private readonly IRoleService _roleService;

    public RolesController(IRoleService roleService)
    {
        _roleService = roleService;
    }

    /// <summary>
    /// Create a new role for an application.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(RoleDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RoleDto>> CreateRoleAsync(
        Guid appId,
        [FromBody] CreateRoleRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await _roleService.CreateAsync(appId, request, cancellationToken);
        if (result.TryPickT1(out var notFoundError, out var roleRemaining))
        {
            return NotFound(new { error = notFoundError.Message ?? "Resource not found." });
        }
        if (result.TryPickT2(out var conflictError, out _))
        {
            return Conflict(new { error = conflictError.Message });
        }
        var role = roleRemaining.AsT0;
        return CreatedAtAction(nameof(GetRoleAsync), new { appId, roleId = role.Id }, role);
    }

    /// <summary>
    /// List all roles for an application.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<RoleDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<RoleDto>>> ListRolesAsync(Guid appId, CancellationToken cancellationToken)
    {
        var result = await _roleService.ListAsync(appId, cancellationToken);
        if (result.TryPickT1(out var notFoundError, out var roles))
        {
            return NotFound(new { error = notFoundError.Message ?? "Resource not found." });
        }
        return Ok(roles);
    }

    /// <summary>
    /// Get a role by ID.
    /// </summary>
    [HttpGet("{roleId}")]
    [ProducesResponseType(typeof(RoleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RoleDto>> GetRoleAsync(Guid appId, Guid roleId, CancellationToken cancellationToken)
    {
        var result = await _roleService.GetByIdAsync(appId, roleId, cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>
    /// Assign permissions to a role.
    /// </summary>
    [HttpPost("{roleId}/permissions")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AssignPermissionsAsync(
        Guid appId,
        Guid roleId,
        [FromBody] AssignPermissionRequestDto request,
        CancellationToken cancellationToken)
    {
        foreach (var permissionId in request.PermissionIds)
        {
            var result = await _roleService.AssignPermissionAsync(appId, roleId, permissionId, cancellationToken);
            if (result.TryPickT1(out var notFoundError, out _))
            {
                return NotFound(new { error = notFoundError.Message ?? "Resource not found." });
            }
            if (result.TryPickT2(out var conflictError, out _))
            {
                return Conflict(new { error = conflictError.Message });
            }
        }
        return NoContent();
    }

    /// <summary>
    /// Remove a permission from a role.
    /// </summary>
    [HttpDelete("{roleId}/permissions/{permissionId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemovePermissionAsync(
        Guid appId,
        Guid roleId,
        Guid permissionId,
        CancellationToken cancellationToken)
    {
        var result = await _roleService.RemovePermissionAsync(appId, roleId, permissionId, cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>
    /// Delete a role.
    /// </summary>
    [HttpDelete("{roleId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteRoleAsync(
        Guid appId,
        Guid roleId,
        CancellationToken cancellationToken)
    {
        var result = await _roleService.DeleteAsync(appId, roleId, cancellationToken);
        return result.ToActionResult();
    }
}
