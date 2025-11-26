using Asp.Versioning;
using AuthSmith.Api.Authorization;
using AuthSmith.Api.Extensions;
using AuthSmith.Application.Services.Authorization;
using AuthSmith.Contracts.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuthSmith.Api.Controllers;

[ApiController]
[Route("api/v1/authorization")]
[ApiVersion("1.0")]
[RequireAppAccess]
public class AuthorizationController : ControllerBase
{
    private readonly IAuthorizationService _authorizationService;

    public AuthorizationController(IAuthorizationService authorizationService)
    {
        _authorizationService = authorizationService;
    }

    /// <summary>
    /// Check if a user has a specific permission.
    /// </summary>
    [HttpPost("check")]
    [ProducesResponseType(typeof(PermissionCheckResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PermissionCheckResultDto>> CheckPermissionAsync(
        [FromBody] PermissionCheckRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await _authorizationService.CheckPermissionAsync(request, cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>
    /// Check multiple permissions in a single request.
    /// </summary>
    [HttpPost("bulk-check")]
    [ProducesResponseType(typeof(BulkPermissionCheckResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<BulkPermissionCheckResultDto>> BulkCheckPermissionsAsync(
        [FromBody] BulkPermissionCheckRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await _authorizationService.BulkCheckPermissionsAsync(request, cancellationToken);
        return result.ToActionResult();
    }
}

