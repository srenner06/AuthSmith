# AuthSmith SDK - Complete Guide

**Date**: November 30, 2025  
**Status**: ? **COMPLETE**

---

## ?? Overview

The AuthSmith SDK provides strongly-typed clients for all API endpoints using **Refit** for automatic HTTP client generation.

---

## ?? Installation

```bash
dotnet add package AuthSmith.Sdk
```

Or add to your `.csproj`:
```xml
<PackageReference Include="AuthSmith.Sdk" Version="1.0.0" />
```

---

## ?? Quick Start

### **1. Basic Setup**

```csharp
using AuthSmith.Sdk;

// Create HTTP client with API key
var httpClient = AuthSmithClientFactory.CreateHttpClient(
    baseAddress: "https://api.authsmith.com",
    apiKey: "your-api-key-here"
);

// Create specific clients
var authClient = AuthSmithClientFactory.CreateAuthClient(httpClient);
var usersClient = AuthSmithClientFactory.CreateUsersClient(httpClient);
var profileClient = AuthSmithClientFactory.CreateUserProfileClient(httpClient);
```

### **2. System Client (No Auth Required)**

```csharp
// Ping endpoint doesn't require authentication
var systemClient = AuthSmithClientFactory.CreateSystemClient("https://api.authsmith.com");
var pingResponse = await systemClient.PingAsync();

Console.WriteLine($"Version: {pingResponse.VersionTag}");
Console.WriteLine($"Build: {pingResponse.BuildNumber}");
```

---

## ?? Available Clients

### **1. ISystemClient** - Health & Version
```csharp
var systemClient = AuthSmithClientFactory.CreateSystemClient(baseUrl);

// Ping endpoint
var ping = await systemClient.PingAsync();
```

**Endpoints:**
- `GET /api/ping` - Health check with version info

---

### **2. IAuthClient** - Authentication
```csharp
var authClient = AuthSmithClientFactory.CreateAuthClient(httpClient);

// Register user
var registerResult = await authClient.RegisterAsync("myapp", new RegisterRequestDto
{
    UserName = "john.doe",
    Email = "john@example.com",
    Password = "SecurePass123!"
});

// Login
var loginResult = await authClient.LoginAsync(new LoginRequestDto
{
    UsernameOrEmail = "john.doe",
    Password = "SecurePass123!",
    AppKey = "myapp"
});

// Refresh token
var refreshResult = await authClient.RefreshAsync(new RefreshRequestDto
{
    RefreshToken = "your-refresh-token"
});

// Revoke token
await authClient.RevokeRefreshTokenAsync(new RevokeRefreshTokenRequestDto
{
    RefreshToken = "token-to-revoke"
});
```

**Endpoints:**
- `POST /api/v1/auth/register/{appKey}` - Register user
- `POST /api/v1/auth/login` - Login
- `POST /api/v1/auth/refresh` - Refresh tokens
- `POST /api/v1/auth/revoke` - Revoke refresh token

---

### **3. IPasswordResetClient** - Password Reset
```csharp
var resetClient = AuthSmithClientFactory.CreatePasswordResetClient(httpClient);

// Request password reset
var response = await resetClient.RequestPasswordResetAsync(new PasswordResetRequestDto
{
    Email = "john@example.com",
    ApplicationKey = "myapp"
});

// Reset password with token
await resetClient.ResetPasswordAsync(new PasswordResetConfirmDto
{
    Token = "reset-token-from-email",
    NewPassword = "NewSecurePass123!"
});
```

**Endpoints:**
- `POST /api/v1/password-reset/request` - Request reset
- `POST /api/v1/password-reset/reset` - Confirm reset

---

### **4. IEmailVerificationClient** - Email Verification
```csharp
var verifyClient = AuthSmithClientFactory.CreateEmailVerificationClient(httpClient);

// Verify email
var result = await verifyClient.VerifyEmailAsync(new VerifyEmailDto
{
    Token = "verification-token-from-email"
});

// Resend verification email
await verifyClient.ResendVerificationEmailAsync(new ResendVerificationEmailDto
{
    Email = "john@example.com"
});
```

**Endpoints:**
- `POST /api/v1/email-verification/verify` - Verify email
- `POST /api/v1/email-verification/resend` - Resend verification

---

### **5. IUserProfileClient** - User Profile Management
```csharp
var profileClient = AuthSmithClientFactory.CreateUserProfileClient(httpClient);

// Get profile
var profile = await profileClient.GetProfileAsync();

// Update profile
var updated = await profileClient.UpdateProfileAsync(new UpdateProfileDto
{
    UserName = "john.updated",
    Email = "john.new@example.com"
});

// Change password
await profileClient.ChangePasswordAsync(new ChangePasswordDto
{
    CurrentPassword = "OldPass123!",
    NewPassword = "NewPass123!"
});

// Delete account
await profileClient.DeleteAccountAsync(new DeleteAccountDto
{
    Password = "MyPassword123!",
    Confirmation = "DELETE"
});
```

**Endpoints:**
- `GET /api/v1/profile` - Get profile
- `PATCH /api/v1/profile` - Update profile
- `POST /api/v1/profile/change-password` - Change password
- `DELETE /api/v1/profile` - Delete account

---

### **6. ISessionManagementClient** - Session Management
```csharp
var sessionClient = AuthSmithClientFactory.CreateSessionManagementClient(httpClient);

// Get all active sessions
var sessions = await sessionClient.GetActiveSessionsAsync();

// Revoke specific session
await sessionClient.RevokeSessionAsync(sessionId);

// Revoke all other sessions
await sessionClient.RevokeOtherSessionsAsync();

// Revoke all sessions (logout everywhere)
await sessionClient.RevokeAllSessionsAsync();
```

**Endpoints:**
- `GET /api/v1/sessions` - List active sessions
- `DELETE /api/v1/sessions/{id}` - Revoke specific session
- `DELETE /api/v1/sessions/revoke-others` - Revoke other sessions
- `DELETE /api/v1/sessions/revoke-all` - Revoke all sessions

---

### **7. IUsersClient** - User Management (Admin)
```csharp
var usersClient = AuthSmithClientFactory.CreateUsersClient(httpClient);

// List users
var users = await usersClient.ListUsersAsync("myapp");

// Get user by ID
var user = await usersClient.GetUserAsync(userId);

// Update user
var updated = await usersClient.UpdateUserAsync(userId, new UpdateUserRequestDto
{
    IsActive = true
});

// Delete user
await usersClient.DeleteUserAsync(userId);
```

**Endpoints:**
- `GET /api/v1/apps/{appKey}/users` - List users
- `GET /api/v1/users/{id}` - Get user
- `PATCH /api/v1/users/{id}` - Update user
- `DELETE /api/v1/users/{id}` - Delete user

---

### **8. IApplicationsClient** - Application Management (Admin)
```csharp
var appsClient = AuthSmithClientFactory.CreateApplicationsClient(httpClient);

// Create application
var app = await appsClient.CreateApplicationAsync(new CreateApplicationRequestDto
{
    Name = "My App",
    Key = "myapp"
});

// List applications
var apps = await appsClient.ListApplicationsAsync();

// Get application
var app = await appsClient.GetApplicationAsync(appId);

// Update application
var updated = await appsClient.UpdateApplicationAsync(appId, new UpdateApplicationRequestDto
{
    Name = "Updated App Name"
});

// Generate new API key
var apiKey = await appsClient.GenerateApiKeyAsync(appId);
```

**Endpoints:**
- `POST /api/v1/apps` - Create application
- `GET /api/v1/apps` - List applications
- `GET /api/v1/apps/{id}` - Get application
- `PATCH /api/v1/apps/{id}` - Update application
- `POST /api/v1/apps/{id}/api-key` - Generate API key

---

### **9. IRolesClient** - Role Management (Admin)
```csharp
var rolesClient = AuthSmithClientFactory.CreateRolesClient(httpClient);

// Create role
var role = await rolesClient.CreateRoleAsync(new CreateRoleRequestDto
{
    Name = "Editor",
    ApplicationId = appId
});

// Assign role to user
await rolesClient.AssignRoleToUserAsync(new AssignRoleRequestDto
{
    UserId = userId,
    RoleId = roleId
});

// Remove role from user
await rolesClient.RemoveRoleFromUserAsync(userId, roleId);
```

**Endpoints:**
- `POST /api/v1/roles` - Create role
- `POST /api/v1/roles/assign` - Assign role
- `DELETE /api/v1/roles/{userId}/{roleId}` - Remove role

---

### **10. IPermissionsClient** - Permission Management (Admin)
```csharp
var permClient = AuthSmithClientFactory.CreatePermissionsClient(httpClient);

// Create permission
var perm = await permClient.CreatePermissionAsync(new CreatePermissionRequestDto
{
    Name = "posts.edit",
    ApplicationId = appId
});

// Assign permission to role
await permClient.AssignPermissionToRoleAsync(new AssignPermissionRequestDto
{
    RoleId = roleId,
    PermissionId = permId
});
```

**Endpoints:**
- `POST /api/v1/permissions` - Create permission
- `POST /api/v1/permissions/assign` - Assign to role

---

### **11. IAuthorizationClient** - Authorization Checks
```csharp
var authzClient = AuthSmithClientFactory.CreateAuthorizationClient(httpClient);

// Check permission
var hasPermission = await authzClient.CheckPermissionAsync(new CheckPermissionRequestDto
{
    UserId = userId,
    PermissionName = "posts.edit",
    ApplicationKey = "myapp"
});

// Check role
var hasRole = await authzClient.CheckRoleAsync(new CheckRoleRequestDto
{
    UserId = userId,
    RoleName = "Editor",
    ApplicationKey = "myapp"
});

// Get user permissions
var permissions = await authzClient.GetUserPermissionsAsync(userId, "myapp");

// Get user roles
var roles = await authzClient.GetUserRolesAsync(userId, "myapp");
```

**Endpoints:**
- `POST /api/v1/authorization/check-permission` - Check permission
- `POST /api/v1/authorization/check-role` - Check role
- `GET /api/v1/authorization/users/{id}/permissions` - Get permissions
- `GET /api/v1/authorization/users/{id}/roles` - Get roles

---

### **12. IAuditClient** - Audit Logs (Admin)
```csharp
var auditClient = AuthSmithClientFactory.CreateAuditClient(httpClient);

// Get audit logs (paginated)
var logs = await auditClient.GetAuditLogsAsync(
    page: 1,
    pageSize: 50,
    eventType: null,
    userId: null,
    applicationId: null,
    startDate: DateTime.UtcNow.AddDays(-7),
    endDate: DateTime.UtcNow
);

// Get user audit logs
var userLogs = await auditClient.GetUserAuditLogsAsync(userId);

// Get application audit logs
var appLogs = await auditClient.GetApplicationAuditLogsAsync(appId);
```

**Endpoints:**
- `GET /api/v1/audit/logs` - Get paginated logs
- `GET /api/v1/audit/users/{userId}/logs` - Get user logs
- `GET /api/v1/audit/applications/{appId}/logs` - Get app logs

---

## ?? Authentication

### **API Key Authentication**

Most clients require an API key:

```csharp
var httpClient = AuthSmithClientFactory.CreateHttpClient(
    baseAddress: "https://api.authsmith.com",
    apiKey: "your-admin-api-key"
);
```

The API key is automatically added to the `X-API-Key` header for all requests.

### **JWT Token Authentication**

For user-authenticated endpoints, use JWT tokens:

```csharp
var httpClient = new HttpClient
{
    BaseAddress = new Uri("https://api.authsmith.com")
};

// Add JWT token
httpClient.DefaultRequestHeaders.Authorization = 
    new AuthenticationHeaderValue("Bearer", jwtToken);

var profileClient = AuthSmithClientFactory.CreateUserProfileClient(httpClient);
```

---

## ?? Configuration

### **With Dependency Injection**

```csharp
services.AddSingleton(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var baseUrl = config["AuthSmith:BaseUrl"];
    var apiKey = config["AuthSmith:ApiKey"];
    
    return AuthSmithClientFactory.CreateHttpClient(baseUrl, apiKey);
});

services.AddScoped<IAuthClient>(sp =>
{
    var httpClient = sp.GetRequiredService<HttpClient>();
    return AuthSmithClientFactory.CreateAuthClient(httpClient);
});

services.AddScoped<IUsersClient>(sp =>
{
    var httpClient = sp.GetRequiredService<HttpClient>();
    return AuthSmithClientFactory.CreateUsersClient(httpClient);
});
```

### **With HttpClientFactory**

```csharp
services.AddHttpClient("AuthSmith", client =>
{
    client.BaseAddress = new Uri("https://api.authsmith.com");
    client.DefaultRequestHeaders.Add("X-API-Key", "your-api-key");
});

services.AddScoped<IAuthClient>(sp =>
{
    var factory = sp.GetRequiredService<IHttpClientFactory>();
    var client = factory.CreateClient("AuthSmith");
    return AuthSmithClientFactory.CreateAuthClient(client);
});
```

---

## ?? Examples

### **Complete User Registration Flow**

```csharp
var httpClient = AuthSmithClientFactory.CreateHttpClient(baseUrl, apiKey);
var authClient = AuthSmithClientFactory.CreateAuthClient(httpClient);
var verifyClient = AuthSmithClientFactory.CreateEmailVerificationClient(httpClient);

// 1. Register user
var registerResult = await authClient.RegisterAsync("myapp", new RegisterRequestDto
{
    UserName = "john.doe",
    Email = "john@example.com",
    Password = "SecurePass123!"
});

Console.WriteLine($"User registered! Access Token: {registerResult.AccessToken}");

// 2. User clicks link in email, verify email
await verifyClient.VerifyEmailAsync(new VerifyEmailDto
{
    Token = "token-from-email"
});

Console.WriteLine("Email verified!");
```

### **Password Reset Flow**

```csharp
var resetClient = AuthSmithClientFactory.CreatePasswordResetClient(httpClient);

// 1. User requests password reset
var response = await resetClient.RequestPasswordResetAsync(new PasswordResetRequestDto
{
    Email = "john@example.com",
    ApplicationKey = "myapp"
});

Console.WriteLine(response.Message); // "Password reset email sent"

// 2. User clicks link in email, resets password
await resetClient.ResetPasswordAsync(new PasswordResetConfirmDto
{
    Token = "token-from-email",
    NewPassword = "NewSecurePass123!"
});

Console.WriteLine("Password reset successfully!");
```

### **Session Management**

```csharp
// Use JWT token for authenticated requests
var httpClient = new HttpClient { BaseAddress = new Uri(baseUrl) };
httpClient.DefaultRequestHeaders.Authorization = 
    new AuthenticationHeaderValue("Bearer", userJwtToken);

var sessionClient = AuthSmithClientFactory.CreateSessionManagementClient(httpClient);

// Get all active sessions
var sessions = await sessionClient.GetActiveSessionsAsync();

Console.WriteLine($"You have {sessions.TotalCount} active sessions:");
foreach (var session in sessions.Sessions)
{
    Console.WriteLine($"- Device: {session.DeviceInfo}, Last used: {session.LastUsedAt}");
    
    if (!session.IsCurrentSession)
    {
        // Revoke old session
        await sessionClient.RevokeSessionAsync(session.Id);
    }
}
```

---

## ?? Testing

### **Mock Clients**

Since clients are interfaces, you can easily mock them:

```csharp
using Moq;

var mockAuthClient = new Mock<IAuthClient>();
mockAuthClient
    .Setup(x => x.LoginAsync(It.IsAny<LoginRequestDto>(), default))
    .ReturnsAsync(new AuthResultDto
    {
        AccessToken = "mock-access-token",
        RefreshToken = "mock-refresh-token"
    });

// Use in tests
var result = await mockAuthClient.Object.LoginAsync(new LoginRequestDto
{
    UsernameOrEmail = "test",
    Password = "test",
    AppKey = "test"
});
```

---

## ?? All Client Methods

### **Summary Table**

| Client | Methods | Authentication Required |
|--------|---------|------------------------|
| **ISystemClient** | Ping | No |
| **IAuthClient** | Register, Login, Refresh, Revoke | API Key |
| **IPasswordResetClient** | Request, Reset | API Key |
| **IEmailVerificationClient** | Verify, Resend | API Key |
| **IUserProfileClient** | Get, Update, ChangePassword, Delete | JWT Token |
| **ISessionManagementClient** | GetSessions, Revoke, RevokeOthers, RevokeAll | JWT Token |
| **IUsersClient** | List, Get, Update, Delete | Admin API Key |
| **IApplicationsClient** | Create, List, Get, Update, GenerateKey | Admin API Key |
| **IRolesClient** | Create, Assign, Remove | Admin API Key |
| **IPermissionsClient** | Create, Assign | Admin API Key |
| **IAuthorizationClient** | CheckPermission, CheckRole, GetPermissions, GetRoles | API Key |
| **IAuditClient** | GetLogs, GetUserLogs, GetAppLogs | Admin API Key |

---

## ? Benefits

- ? **Strongly-typed** - Compile-time checking
- ? **Async/await** - Modern async patterns
- ? **Refit-based** - Automatic HTTP client generation
- ? **Mockable** - Easy to test
- ? **Complete** - All API endpoints covered
- ? **Documented** - XML documentation on all methods

---

**SDK is complete and ready to use!** ??
