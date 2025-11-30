using Asp.Versioning;
using AuthSmith.Api.Extensions;
using AuthSmith.Application.Services.Auth;
using AuthSmith.Contracts.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuthSmith.Api.Controllers;

[ApiController]
[Route("api/v1/password-reset")]
[ApiVersion("1.0")]
public class PasswordResetController : ControllerBase
{
    private readonly IPasswordResetService _passwordResetService;

    public PasswordResetController(IPasswordResetService passwordResetService)
    {
        _passwordResetService = passwordResetService;
    }

    /// <summary>
    /// Request a password reset token.
    /// </summary>
    /// <remarks>
    /// Sends a password reset email to the user if the account exists.
    /// Always returns success to prevent email enumeration attacks.
    /// </remarks>
    [HttpPost("request")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PasswordResetResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PasswordResetResponseDto>> RequestPasswordResetAsync(
        [FromBody] PasswordResetRequestDto request,
        CancellationToken cancellationToken)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var result = await _passwordResetService.RequestPasswordResetAsync(request, ipAddress, cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>
    /// Confirm password reset with token.
    /// </summary>
    /// <remarks>
    /// Resets the user's password using the token received via email.
    /// </remarks>
    [HttpPost("confirm")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PasswordResetResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PasswordResetResponseDto>> ConfirmPasswordResetAsync(
        [FromBody] PasswordResetConfirmDto request,
        CancellationToken cancellationToken)
    {
        var result = await _passwordResetService.ConfirmPasswordResetAsync(request, cancellationToken);
        return result.ToActionResult();
    }
}
