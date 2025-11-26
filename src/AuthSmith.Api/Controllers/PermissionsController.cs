using Asp.Versioning;
using AuthSmith.Api.Authorization;
using AuthSmith.Api.Extensions;
using AuthSmith.Application.Services.Permissions;
using AuthSmith.Contracts.Permissions;
using Microsoft.AspNetCore.Mvc;

namespace AuthSmith.Api.Controllers;

[ApiController]
[Route("api/v1/apps/{appId}/permissions")]
[ApiVersion("1.0")]
[RequireAdminAccess]
public class PermissionsController : ControllerBase
{
    private readonly IPermissionService _permissionService;

    public PermissionsController(IPermissionService permissionService)
    {
        _permissionService = permissionService;
    }

    /// <summary>
    /// Create a new permission for an application.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(PermissionDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PermissionDto>> CreatePermissionAsync(
        Guid appId,
        [FromBody] CreatePermissionRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await _permissionService.CreateAsync(appId, request, cancellationToken);
        if (result.TryPickT1(out var notFoundError, out var permissionRemaining))
        {
            return NotFound(new { error = notFoundError.Message ?? "Resource not found." });
        }
        if (result.TryPickT2(out var conflictError, out _))
        {
            return Conflict(new { error = conflictError.Message });
        }
        var permission = permissionRemaining.AsT0;
        return CreatedAtAction(nameof(GetPermissionAsync), new { appId, permissionId = permission.Id }, permission);
    }

    /// <summary>
    /// List all permissions for an application.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<PermissionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<PermissionDto>>> ListPermissionsAsync(Guid appId, CancellationToken cancellationToken)
    {
        var result = await _permissionService.ListAsync(appId, cancellationToken);
        if (result.TryPickT1(out var notFoundError, out var permissions))
        {
            return NotFound(new { error = notFoundError.Message ?? "Resource not found." });
        }
        return Ok(permissions);
    }

    /// <summary>
    /// Get a permission by ID.
    /// </summary>
    [HttpGet("{permissionId}")]
    [ProducesResponseType(typeof(PermissionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PermissionDto>> GetPermissionAsync(Guid appId, Guid permissionId, CancellationToken cancellationToken)
    {
        var result = await _permissionService.GetByIdAsync(appId, permissionId, cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>
    /// Delete a permission.
    /// </summary>
    [HttpDelete("{permissionId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeletePermissionAsync(
        Guid appId,
        Guid permissionId,
        CancellationToken cancellationToken)
    {
        var result = await _permissionService.DeleteAsync(appId, permissionId, cancellationToken);
        return result.ToActionResult();
    }
}
