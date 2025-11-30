# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added
- **Security Headers Middleware** - OWASP-compliant security headers (CSP, HSTS, X-Frame-Options, etc.)
- **CORS Configuration** - Flexible cross-origin resource sharing with configurable origins and credentials
- **Rate Limiting** - Sliding window rate limiting with endpoint-specific limits and IP/API key whitelisting
- **Email Service** - SMTP email service with MailKit integration and HTML templates
- **Password Reset Flow** - Secure password reset with time-limited tokens and email verification
- **Email Verification** - Email verification system for new user accounts with 24-hour token expiration
- **User Profile Management** - Complete profile management with update username, email, change password, and account deletion
- **Session Management** - View and manage active sessions with device tracking and bulk revocation
- **Comprehensive Audit Logging** - 30+ event types tracked with PostgreSQL JSONB storage and admin query API
- **Structured Logging** - Serilog integration with console and file sinks, enrichers, and proper log levels
- **Enhanced User Entity** - Added `EmailVerified`, `EmailVerifiedAt` fields for email verification tracking
- **RefreshToken Device Info** - Added `DeviceInfo` and `IpAddress` for session tracking
- **New Entities**:
  - `PasswordResetToken` - Secure password reset tokens
  - `EmailVerificationToken` - Email verification tokens
  - `AuditLog` - Comprehensive audit logging
- **New Controllers**:
  - `PasswordResetController` - Password reset endpoints
  - `EmailVerificationController` - Email verification endpoints
  - `UserProfileController` - User profile management
  - `SessionManagementController` - Session management
  - `AuditController` - Audit log querying (admin only)
- **New Services**:
  - `IPasswordResetService` - Password reset business logic
  - `IEmailVerificationService` - Email verification logic
  - `IUserProfileService` - Profile management
  - `ISessionManagementService` - Session tracking and revocation
  - `IAuditService` - Audit event logging
  - `IAuditQueryService` - Audit log querying
  - `IEmailService` / `SmtpEmailService` - Email sending
- **Configuration Classes**:
  - `CorsConfiguration` - CORS settings
  - `RateLimitConfiguration` - Rate limiting settings
  - `EmailConfiguration` - SMTP settings
- **OneOf Extensions** - Additional result type combinations for validation errors

### Changed
- Updated `.editorconfig` with code analysis rule suppressions
- Enhanced `OneOfExtensions` with additional error type combinations
- Configured Serilog as primary logging provider

### Security
- Implemented Argon2id password hashing (already existed, now complemented with email verification)
- Added email enumeration protection in password reset and email verification
- Implemented secure token generation with SHA256 hashing
- Added session revocation with password confirmation
- Comprehensive security headers for all responses
- Rate limiting to prevent brute force attacks

## [1.0.0] - Initial Release

### Added
- User authentication and authorization
- Multi-application support
- Role-based and permission-based access control
- JWT token generation with RSA/ECDSA support
- Refresh token management
- Account lockout protection
- API key authentication
- RESTful API with OpenAPI/Swagger documentation
- PostgreSQL database with Entity Framework Core
- Redis caching support (optional)
- Health checks
- Docker support

---

## Upgrade Notes

### From 1.0.0 to Unreleased

**Breaking Changes:**
- None - all changes are additive

**New Dependencies:**
- `MailKit` v4.9.0 - Email service
- `Serilog.AspNetCore` v8.0.3 - Structured logging
- `Serilog.Enrichers.Environment` v3.0.1
- `Serilog.Enrichers.Thread` v4.0.0
- `Serilog.Sinks.Console` v6.0.0
- `Serilog.Sinks.File` v6.0.0

**Database Migrations Required:**
```bash
dotnet ef migrations add UpgradeToV2 \
  --project src/AuthSmith.Infrastructure \
  --startup-project src/AuthSmith.Api \
  --context AuthSmithDbContext \
  --output-dir Migrations

dotnet ef database update \
  --project src/AuthSmith.Infrastructure \
  --startup-project src/AuthSmith.Api
```

**Configuration Updates:**
Add the following to `appsettings.json`:
```json
{
  "Cors": {
    "AllowedOrigins": ["http://localhost:3000"],
    "AllowCredentials": true,
    "MaxAge": 3600,
    "AllowedHeaders": ["X-API-Key", "X-Request-Id"],
    "ExposedHeaders": ["X-Request-Id", "X-Rate-Limit-Remaining"]
  },
  "RateLimit": {
    "Enabled": true,
    "GeneralLimit": 100,
    "AuthLimit": 10,
    "RegistrationLimit": 5,
    "PasswordResetLimit": 3,
    "WindowSeconds": 60,
    "WhitelistedIps": ["127.0.0.1", "::1"]
  },
  "Email": {
    "SmtpHost": "localhost",
    "SmtpPort": 587,
    "EnableSsl": true,
    "Username": "",
    "Password": "",
    "FromAddress": "noreply@authsmith.local",
    "FromName": "AuthSmith",
    "Enabled": false,
    "BaseUrl": "https://localhost:5001"
  },
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "System": "Warning"
      }
    }
  }
}
```

**Feature Highlights:**
1. **Password Reset**: Users can now reset forgotten passwords via email
2. **Email Verification**: New accounts require email verification
3. **Profile Management**: Users can update their profiles and change passwords
4. **Session Management**: View and revoke active sessions across devices
5. **Audit Logging**: Complete audit trail of all security-related events
6. **Enhanced Security**: Rate limiting, security headers, and CORS protection

---

[Unreleased]: https://github.com/srenner06/AuthSmith/compare/v1.0.0...HEAD
[1.0.0]: https://github.com/srenner06/AuthSmith/releases/tag/v1.0.0
