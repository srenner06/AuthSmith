# AuthSmith

Authentication and Authorization service built with .NET.

## Features

- User authentication (registration, login, token refresh)
- Role-based and permission-based authorization
- API key authentication
- JWT token generation with RSA/ECDSA keys
- Refresh token management
- Account lockout protection
- Permission caching (in-memory or Redis)
- Multi-application support

## Prerequisites

- .NET 10.0 SDK
- PostgreSQL database
- (Optional) Redis for distributed caching

## Configuration

The application uses `appsettings.json` for configuration. Key sections:

- `Database`: Connection string and auto-migration settings
- `ApiKeys`: Admin and bootstrap API keys
- `Jwt`: JWT token configuration (issuer, audience, expiration, key paths)
- `Redis`: Redis connection settings (optional)
- `OpenTelemetry`: OpenTelemetry configuration (optional)

## Database Migrations

### Creating Migrations

To create a new migration after making changes to entity models:

```bash
dotnet ef migrations add <MigrationName> --project src/AuthSmith.Infrastructure/AuthSmith.Infrastructure.csproj --startup-project src/AuthSmith.Api/AuthSmith.Api.csproj --context AuthSmithDbContext --output-dir Migrations
```

Replace `<MigrationName>` with a descriptive name for your migration (e.g., `AddUserEmailVerification`, `UpdateRoleSchema`).

### Applying Migrations

Migrations are automatically applied on application startup if `Database:AutoMigrate` is set to `true` in configuration.

To manually apply migrations:

```bash
dotnet ef database update --project src/AuthSmith.Infrastructure/AuthSmith.Infrastructure.csproj --startup-project src/AuthSmith.Api/AuthSmith.Api.csproj --context AuthSmithDbContext
```

### Removing the Last Migration

To remove the last migration (if it hasn't been applied to the database):

```bash
dotnet ef migrations remove --project src/AuthSmith.Infrastructure/AuthSmith.Infrastructure.csproj --startup-project src/AuthSmith.Api/AuthSmith.Api.csproj --context AuthSmithDbContext
```

### Listing Migrations

To see all migrations:

```bash
dotnet ef migrations list --project src/AuthSmith.Infrastructure/AuthSmith.Infrastructure.csproj --startup-project src/AuthSmith.Api/AuthSmith.Api.csproj --context AuthSmithDbContext
```

## Running the Application

1. Configure your database connection string in `appsettings.json`
2. Generate JWT key pair (private key for signing, public key for validation)
3. Run the application:

```bash
dotnet run --project src/AuthSmith.Api/AuthSmith.Api.csproj
```

The API will be available at `https://localhost:5001` (or the configured port).

## API Documentation

Swagger/OpenAPI documentation is available at `/swagger` when running in development mode.

## Project Structure

- `AuthSmith.Domain`: Domain entities, interfaces, and enums
- `AuthSmith.Application`: Business logic and application services
- `AuthSmith.Infrastructure`: Data access, external services, and infrastructure concerns
- `AuthSmith.Api`: REST API controllers and middleware
- `AuthSmith.Contracts`: DTOs and API contracts
- `AuthSmith.Sdk`: Client SDK for consuming the API
