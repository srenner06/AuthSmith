using Asp.Versioning;
using AuthSmith.Api.Extensions;
using AuthSmith.Application.Services.Auth;
using AuthSmith.Contracts.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuthSmith.Api.Controllers;

[ApiController]
[Route("api/v1/email-verification")]
[ApiVersion("1.0")]
public class EmailVerificationController : ControllerBase
{
    private readonly IEmailVerificationService _emailVerificationService;

    public EmailVerificationController(IEmailVerificationService emailVerificationService)
    {
        _emailVerificationService = emailVerificationService;
    }

    /// <summary>
    /// Send or resend email verification link.
    /// </summary>
    /// <remarks>
    /// Sends an email verification link to the user.
    /// Always returns success to prevent email enumeration attacks.
    /// </remarks>
    [HttpPost("send")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(EmailVerificationResponseDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<EmailVerificationResponseDto>> SendVerificationEmailAsync(
        [FromBody] ResendVerificationEmailDto request,
        CancellationToken cancellationToken)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var result = await _emailVerificationService.SendVerificationEmailAsync(
            request.Email,
            ipAddress,
            cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>
    /// Verify email address with token (GET endpoint for email links).
    /// </summary>
    /// <remarks>
    /// Confirms the user's email address using the token from the email link.
    /// This endpoint accepts GET requests for direct browser navigation from email links.
    /// </remarks>
    [HttpGet("verify")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(EmailVerificationResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EmailVerificationResponseDto>> VerifyEmailViaGetAsync(
        [FromQuery] string token,
        CancellationToken cancellationToken)
    {
        var result = await _emailVerificationService.VerifyEmailAsync(
            new VerifyEmailDto { Token = token },
            cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>
    /// Verify email address with token.
    /// </summary>
    /// <remarks>
    /// Confirms the user's email address using the token received via email.
    /// </remarks>
    [HttpPost("verify")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(EmailVerificationResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EmailVerificationResponseDto>> VerifyEmailAsync(
        [FromBody] VerifyEmailDto request,
        CancellationToken cancellationToken)
    {
        var result = await _emailVerificationService.VerifyEmailAsync(request, cancellationToken);
        return result.ToActionResult();
    }
}
