using Asp.Versioning;
using AuthSmith.Api.Authorization;
using AuthSmith.Api.Extensions;
using AuthSmith.Application.Services.Auth;
using AuthSmith.Contracts.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuthSmith.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
[ApiVersion("1.0")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    /// <summary>
    /// Register a new user for an application.
    /// </summary>
    [HttpPost("register/{appKey}")]
    [RequireAppAccess]
    [ProducesResponseType(typeof(AuthResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AuthResultDto>> RegisterAsync(
        string appKey,
        [FromBody] RegisterRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await _authService.RegisterAsync(appKey, request, cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>
    /// Login with username/email and password.
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResultDto>> LoginAsync(
        [FromBody] LoginRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await _authService.LoginAsync(request, cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>
    /// Refresh an access token using a refresh token.
    /// </summary>
    [HttpPost("refresh")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResultDto>> RefreshAsync(
        [FromBody] RefreshRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await _authService.RefreshTokenAsync(request.RefreshToken, cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>
    /// Revoke a refresh token.
    /// </summary>
    [HttpPost("revoke")]
    [RequireAppAccess]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RevokeAsync(
        [FromBody] RevokeRefreshTokenRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await _authService.RevokeRefreshTokenAsync(request.RefreshToken, cancellationToken);
        return result.ToActionResult();
    }
}
