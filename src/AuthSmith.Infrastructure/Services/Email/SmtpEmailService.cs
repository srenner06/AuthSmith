using AuthSmith.Infrastructure.Configuration;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

namespace AuthSmith.Infrastructure.Services.Email;

/// <summary>
/// SMTP email service implementation using MailKit.
/// </summary>
public class SmtpEmailService : IEmailService
{
    private readonly IOptions<EmailConfiguration> _config;
    private readonly ILogger<SmtpEmailService> _logger;

    public SmtpEmailService(
        IOptions<EmailConfiguration> config,
        ILogger<SmtpEmailService> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task<bool> SendEmailAsync(string to, string subject, string body, CancellationToken cancellationToken = default)
    {
        var config = _config.Value;

        if (!config.Enabled)
        {
            _logger.LogInformation("Email sending is disabled. Would have sent to {To}: {Subject}", to, subject);
            return true;
        }

        try
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(config.FromName, config.FromAddress));
            message.To.Add(MailboxAddress.Parse(to));
            message.Subject = subject;

            var bodyBuilder = new BodyBuilder { TextBody = body };
            message.Body = bodyBuilder.ToMessageBody();

            using var client = new SmtpClient();
            await client.ConnectAsync(config.SmtpHost, config.SmtpPort,
                config.EnableSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None,
                cancellationToken);

            if (!string.IsNullOrEmpty(config.Username) && !string.IsNullOrEmpty(config.Password))
            {
                await client.AuthenticateAsync(config.Username, config.Password, cancellationToken);
            }

            await client.SendAsync(message, cancellationToken);
            await client.DisconnectAsync(true, cancellationToken);

            _logger.LogInformation("Email sent successfully to {To}: {Subject}", to, subject);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {To}: {Subject}", to, subject);
            return false;
        }
    }

    public async Task<bool> SendHtmlEmailAsync(string to, string subject, string htmlBody, CancellationToken cancellationToken = default)
    {
        var config = _config.Value;

        if (!config.Enabled)
        {
            _logger.LogInformation("Email sending is disabled. Would have sent HTML email to {To}: {Subject}", to, subject);
            return true;
        }

        try
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(config.FromName, config.FromAddress));
            message.To.Add(MailboxAddress.Parse(to));
            message.Subject = subject;

            var bodyBuilder = new BodyBuilder { HtmlBody = htmlBody };
            message.Body = bodyBuilder.ToMessageBody();

            using var client = new SmtpClient();
            await client.ConnectAsync(config.SmtpHost, config.SmtpPort,
                config.EnableSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None,
                cancellationToken);

            if (!string.IsNullOrEmpty(config.Username) && !string.IsNullOrEmpty(config.Password))
            {
                await client.AuthenticateAsync(config.Username, config.Password, cancellationToken);
            }

            await client.SendAsync(message, cancellationToken);
            await client.DisconnectAsync(true, cancellationToken);

            _logger.LogInformation("HTML email sent successfully to {To}: {Subject}", to, subject);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send HTML email to {To}: {Subject}", to, subject);
            return false;
        }
    }

    public Task<bool> SendWelcomeEmailAsync(string to, string username, string appName, CancellationToken cancellationToken = default)
    {
        var subject = $"Welcome to {appName}!";
        var body = $@"
Hello {username},

Welcome to {appName}! Your account has been successfully created.

If you didn't create this account, please contact support immediately.

Best regards,
The {appName} Team
";

        return SendEmailAsync(to, subject, body, cancellationToken);
    }

    public Task<bool> SendEmailVerificationAsync(string to, string username, string verificationToken, CancellationToken cancellationToken = default)
    {
        var config = _config.Value;
        var verificationUrl = $"{config.BaseUrl}/verify-email?token={verificationToken}";

        var subject = "Verify Your Email Address";
        var htmlBody = $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .button {{ display: inline-block; padding: 12px 24px; background-color: #007bff; color: white; text-decoration: none; border-radius: 4px; }}
        .footer {{ margin-top: 30px; font-size: 12px; color: #666; }}
    </style>
</head>
<body>
    <div class=""container"">
        <h2>Verify Your Email Address</h2>
        <p>Hello {username},</p>
        <p>Thank you for registering! Please verify your email address by clicking the button below:</p>
        <p><a href=""{verificationUrl}"" class=""button"">Verify Email Address</a></p>
        <p>Or copy and paste this link into your browser:</p>
        <p><a href=""{verificationUrl}"">{verificationUrl}</a></p>
        <p>This link will expire in 24 hours.</p>
        <p>If you didn't create this account, you can safely ignore this email.</p>
        <div class=""footer"">
            <p>This is an automated message, please do not reply.</p>
        </div>
    </div>
</body>
</html>";

        return SendHtmlEmailAsync(to, subject, htmlBody, cancellationToken);
    }

    public Task<bool> SendPasswordResetEmailAsync(string to, string username, string resetToken, CancellationToken cancellationToken = default)
    {
        var config = _config.Value;
        var resetUrl = $"{config.BaseUrl}/reset-password?token={resetToken}";

        var subject = "Password Reset Request";
        var htmlBody = $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .button {{ display: inline-block; padding: 12px 24px; background-color: #dc3545; color: white; text-decoration: none; border-radius: 4px; }}
        .footer {{ margin-top: 30px; font-size: 12px; color: #666; }}
        .warning {{ background-color: #fff3cd; padding: 10px; border-left: 4px solid #ffc107; margin: 20px 0; }}
    </style>
</head>
<body>
    <div class=""container"">
        <h2>Password Reset Request</h2>
        <p>Hello {username},</p>
        <p>We received a request to reset your password. Click the button below to create a new password:</p>
        <p><a href=""{resetUrl}"" class=""button"">Reset Password</a></p>
        <p>Or copy and paste this link into your browser:</p>
        <p><a href=""{resetUrl}"">{resetUrl}</a></p>
        <div class=""warning"">
            <strong>Security Notice:</strong>
            <ul>
                <li>This link will expire in 1 hour</li>
                <li>If you didn't request this, please ignore this email</li>
                <li>Your password won't change until you create a new one</li>
            </ul>
        </div>
        <div class=""footer"">
            <p>This is an automated message, please do not reply.</p>
        </div>
    </div>
</body>
</html>";

        return SendHtmlEmailAsync(to, subject, htmlBody, cancellationToken);
    }

    public Task<bool> SendPasswordChangedEmailAsync(string to, string username, CancellationToken cancellationToken = default)
    {
        var subject = "Password Changed Successfully";
        var body = $@"
Hello {username},

Your password has been changed successfully.

If you didn't make this change, please contact support immediately and secure your account.

Best regards,
AuthSmith Team
";

        return SendEmailAsync(to, subject, body, cancellationToken);
    }

    public Task<bool> SendAccountLockedEmailAsync(string to, string username, DateTime lockedUntil, CancellationToken cancellationToken = default)
    {
        var subject = "Account Locked - Security Alert";
        var body = $@"
Hello {username},

Your account has been temporarily locked due to multiple failed login attempts.

Locked until: {lockedUntil:yyyy-MM-dd HH:mm:ss} UTC

This is a security measure to protect your account. You can try logging in again after this time.

If you didn't attempt to log in, please contact support immediately.

Best regards,
AuthSmith Team
";

        return SendEmailAsync(to, subject, body, cancellationToken);
    }

    public Task<bool> SendTwoFactorCodeEmailAsync(string to, string username, string code, CancellationToken cancellationToken = default)
    {
        var subject = "Your Two-Factor Authentication Code";
        var htmlBody = $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .code {{ font-size: 32px; font-weight: bold; letter-spacing: 8px; text-align: center; padding: 20px; background-color: #f8f9fa; border: 2px solid #dee2e6; border-radius: 4px; margin: 20px 0; }}
        .footer {{ margin-top: 30px; font-size: 12px; color: #666; }}
    </style>
</head>
<body>
    <div class=""container"">
        <h2>Two-Factor Authentication</h2>
        <p>Hello {username},</p>
        <p>Your two-factor authentication code is:</p>
        <div class=""code"">{code}</div>
        <p>This code will expire in 10 minutes.</p>
        <p><strong>Never share this code with anyone.</strong></p>
        <div class=""footer"">
            <p>If you didn't request this code, please secure your account immediately.</p>
            <p>This is an automated message, please do not reply.</p>
        </div>
    </div>
</body>
</html>";

        return SendHtmlEmailAsync(to, subject, htmlBody, cancellationToken);
    }
}
