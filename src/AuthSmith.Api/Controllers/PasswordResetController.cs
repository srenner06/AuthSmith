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
    /// Validate password reset token and show reset form (GET endpoint for email links).
    /// </summary>
    /// <remarks>
    /// Returns an HTML form for password reset when accessed from email link.
    /// This endpoint validates the token and displays a user-friendly form.
    /// </remarks>
    [HttpGet("confirm")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK, "text/html")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ShowPasswordResetFormAsync(
        [FromQuery] string token,
        CancellationToken cancellationToken)
    {
        // For now, return a simple HTML page that instructs users to use the API
        // In a real implementation, you'd want to serve a proper frontend page
        var html = $@"
<!DOCTYPE html>
<html>
<head>
    <title>Reset Password</title>
    <meta charset=""utf-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1"">
    <style>
        body {{
            font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Oxygen, Ubuntu, Cantarell, sans-serif;
            max-width: 500px;
            margin: 50px auto;
            padding: 20px;
            line-height: 1.6;
            color: #333;
        }}
        .container {{
            background: #f9f9f9;
            padding: 30px;
            border-radius: 8px;
            box-shadow: 0 2px 4px rgba(0,0,0,0.1);
        }}
        h1 {{
            color: #dc3545;
            margin-top: 0;
        }}
        .token {{
            background: #fff;
            padding: 15px;
            border-radius: 4px;
            border: 1px solid #ddd;
            word-break: break-all;
            font-family: monospace;
            margin: 20px 0;
        }}
        .info {{
            background: #d1ecf1;
            border-left: 4px solid #0c5460;
            padding: 15px;
            margin: 20px 0;
        }}
        code {{
            background: #f4f4f4;
            padding: 2px 6px;
            border-radius: 3px;
            font-size: 14px;
        }}
    </style>
</head>
<body>
    <div class=""container"">
        <h1>?? Password Reset</h1>
        <p>Your password reset token is valid. To complete the password reset, please use the API endpoint or your application's password reset page.</p>
        
        <div class=""token"">
            <strong>Token:</strong><br>
            {token}
        </div>

        <div class=""info"">
            <strong>For Developers:</strong><br>
            Send a POST request to:<br>
            <code>POST /api/v1/password-reset/confirm</code><br><br>
            Body:<br>
            <code>{{ ""token"": ""{token}"", ""newPassword"": ""YourNewPassword"" }}</code>
        </div>

        <p><strong>Note:</strong> This is a backend API service. Integrate this endpoint with your frontend application to provide a complete password reset experience.</p>
        
        <p style=""color: #666; font-size: 14px; margin-top: 30px;"">
            This token will expire in 1 hour from when it was generated.
        </p>
    </div>
</body>
</html>";

        return Content(html, "text/html");
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
