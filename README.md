# AuthSmith

<div align="center">

[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![C#](https://img.shields.io/badge/C%23-14.0-239120?logo=csharp)](https://docs.microsoft.com/en-us/dotnet/csharp/)
[![License](https://img.shields.io/github/license/srenner06/AuthSmith)](LICENSE)
[![Build](https://img.shields.io/github/actions/workflow/status/srenner06/AuthSmith/publish.yml?branch=main&logo=github)](https://github.com/srenner06/AuthSmith/actions)

[![codecov](https://codecov.io/github/srenner06/AuthSmith/graph/badge.svg?token=YCX849VTWI)](https://codecov.io/github/srenner06/AuthSmith)
[![Issues](https://img.shields.io/github/issues/srenner06/AuthSmith)](https://github.com/srenner06/AuthSmith/issues)
[![Pull Requests](https://img.shields.io/github/issues-pr/srenner06/AuthSmith)](https://github.com/srenner06/AuthSmith/pulls)
[![Last Commit](https://img.shields.io/github/last-commit/srenner06/AuthSmith)](https://github.com/srenner06/AuthSmith/commits/main)

[![Docker](https://img.shields.io/badge/Docker-Supported-2496ED?logo=docker)](docker/QUICK_START.md)
[![PostgreSQL](https://img.shields.io/badge/PostgreSQL-16-4169E1?logo=postgresql)](https://www.postgresql.org/)
[![Redis](https://img.shields.io/badge/Redis-7-DC382D?logo=redis)](https://redis.io/)
[![OpenTelemetry](https://img.shields.io/badge/OpenTelemetry-Enabled-3d348b?logo=opentelemetry)](https://opentelemetry.io/)

</div>

---

**A production-ready authentication and authorization service built with .NET 10.** 

AuthSmith provides centralized identity management with support for multiple applications, fine-grained permissions, and modern security practices.

**📊 [View Code Coverage Report](docs/CODE_COVERAGE.md)** | **📚 [Documentation](docs/)** | **🐳 [Quick Start](docker/QUICK_START.md)**

---

## ⚠️ Personal Open-Source Project Disclaimer

**This is a personal hobby project, not production-grade commercial software.**

- ✅ **Good for**: Personal projects, learning, self-hosted applications
- ⚠️ **Use with caution**: Small production deployments (if you understand the risks)
- ❌ **Not recommended**: Critical production systems without thorough security audit

**Key Points:**
- No guaranteed support or response times
- Single maintainer with limited time
- May be abandoned in the future
- Use at your own risk - see [SECURITY.md](SECURITY.md)
- Not professionally security audited
- Provided "as-is" without warranty

**For serious production use, consider:**
- Commercial alternatives (Auth0, Okta, Azure AD B2C, AWS Cognito)
- Professional security audit of this codebase
- Forking and maintaining it yourself with a team

---

## Features

- **User Authentication**: Registration, login, and token refresh with JWT support
- **Multi-Application Support**: Manage users, roles, and permissions across multiple applications
- **Fine-Grained Authorization**: Role-based and permission-based access control
- **API Key Authentication**: Secure API access with key-based authentication
- **JWT Token Generation**: RSA/ECDSA key support for secure token signing
- **Refresh Token Management**: Secure refresh token lifecycle management
- **Account Lockout Protection**: Configurable account lockout to prevent brute-force attacks
- **Permission Caching**: High-performance permission checks with in-memory or Redis caching
- **RESTful API**: Clean, versioned REST API with OpenAPI documentation
- **Version Endpoint**: `/api/ping` endpoint for health checks and version information

## Architecture Overview

AuthSmith follows Clean Architecture principles with clear separation of concerns:

```
┌─────────────────────────────────────────────────────────┐
│                    AuthSmith.Api                        │
│  (Controllers, Middleware, Authentication)            │
└────────────────────┬────────────────────────────────────┘
                     │
┌────────────────────▼────────────────────────────────────┐
│              AuthSmith.Application                      │
│  (Business Logic, Services, Validators)                │
└────────────────────┬────────────────────────────────────┘
                     │
┌────────────────────▼────────────────────────────────────┐
│                AuthSmith.Domain                          │
│  (Entities, Interfaces, Errors, Enums)                  │
└────────────────────┬────────────────────────────────────┘
                     │
┌────────────────────▼────────────────────────────────────┐
│            AuthSmith.Infrastructure                       │
│  (EF Core, Database, External Services)                │
└─────────────────────────────────────────────────────────┘
```

### Layer Responsibilities

- **Api**: HTTP endpoints, request/response handling, authentication middleware
- **Application**: Business logic, use cases, validation rules
- **Domain**: Core entities, domain models, business rules
- **Infrastructure**: Data persistence, external service integrations, caching

## Prerequisites

- .NET 10.0 SDK or later
- PostgreSQL 12+ database
- (Optional) Redis 6+ for distributed caching
- RSA or ECDSA key pair for JWT signing

## Quick Start

### 🐳 Docker Compose (Easiest - Recommended for Development)

**Get running in 2 minutes:**

```bash
# 1. Clone repository
git clone https://github.com/srenner06/AuthSmith.git
cd AuthSmith/docker

# 2. Run setup script
./setup-dev.sh              # Linux/Mac
# or
powershell -ExecutionPolicy Bypass -File setup-dev.ps1  # Windows

# 3. Start everything
docker-compose up -d

# 4. Access the API
open http://localhost:8080/swagger  # API documentation
open http://localhost:8025          # MailHog (email testing)
```

**What you get:**
- ✅ AuthSmith API (http://localhost:8080)
- ✅ PostgreSQL database
- ✅ Redis cache
- ✅ MailHog email testing (view emails at http://localhost:8025)
- ✅ Jaeger distributed tracing (http://localhost:16686)
- ✅ Auto-generated secure credentials
- ✅ JWT keys automatically created
- ✅ Auto-migrations applied
- ✅ Ready to use!

**📚 Detailed setup:** See [docker/QUICK_START.md](docker/QUICK_START.md)

---

### 💻 Local Development (Without Docker)

## Prerequisites

- .NET 10.0 SDK or later
- PostgreSQL 12+ database
- (Optional) Redis 6+ for distributed caching
- RSA or ECDSA key pair for JWT signing

## Quick Start

### 1. Clone and Build

```bash
git clone <repository-url>
cd AuthSmith
dotnet build
```

### 2. Configure Database

Update `src/AuthSmith.Api/appsettings.json` or `appsettings.Development.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=authsmith;Username=postgres;Password=yourpassword"
  },
  "Database": {
    "AutoMigrate": true
  }
}
```

### 3. Generate JWT Keys

Generate an RSA key pair for JWT signing:

```bash
# Generate private key
openssl genpkey -algorithm RSA -out jwt_private_key.pem -pkeyopt rsa_keygen_bits:2048

# Generate public key
openssl rsa -pubout -in jwt_private_key.pem -out jwt_public_key.pem
```

Update configuration:

```json
{
  "Jwt": {
    "Issuer": "https://authsmith.example.com",
    "Audience": "authsmith-api",
    "ExpirationMinutes": 15,
    "PrivateKeyPath": "./jwt_private_key.pem"
  }
}
```

### 4. Configure API Keys

Set up initial API keys for admin access:

```json
{
  "ApiKeys": {
    "AdminKey": "your-secure-admin-key-here",
    "BootstrapKey": "your-secure-bootstrap-key-here"
  }
}
```

### 5. Run the Application

```bash
dotnet run --project src/AuthSmith.Api/AuthSmith.Api.csproj
```

The API will be available at `https://localhost:5001` (or your configured port).

Access Swagger documentation at `/swagger`.

## Configuration

The application uses `appsettings.json` for configuration. Key sections:

### Database Configuration

```json
{
  "Database": {
    "ConnectionString": "Host=localhost;Database=authsmith;Username=postgres;Password=password",
    "AutoMigrate": true
  }
}
```

### API Keys

```json
{
  "ApiKeys": {
    "AdminKey": "admin-api-key",
    "BootstrapKey": "bootstrap-api-key"
  }
}
```

### JWT Configuration

```json
{
  "Jwt": {
    "Issuer": "https://authsmith.example.com",
    "Audience": "authsmith-api",
    "ExpirationMinutes": 15,
    "PrivateKeyPath": "./keys/jwt_private_key.pem"
  }
}
```

### Redis (Optional)

```json
{
  "Redis": {
    "Enabled": true,
    "ConnectionString": "localhost:6379"
  }
}
```

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

## API Usage Examples

### Authentication

#### Register a User

```bash
curl -X POST https://localhost:5001/api/v1/auth/register/myapp \
  -H "X-API-Key: your-api-key" \
  -H "Content-Type: application/json" \
  -d '{
    "username": "johndoe",
    "email": "john@example.com",
    "password": "SecurePass123!"
  }'
```

Response:
```json
{
  "accessToken": "eyJhbGciOiJSUzI1NiIs...",
  "refreshToken": "refresh-token-here",
  "expiresIn": 900
}
```

#### Login

```bash
curl -X POST https://localhost:5001/api/v1/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "usernameOrEmail": "johndoe",
    "password": "SecurePass123!",
    "appKey": "myapp"
  }'
```

#### Refresh Token

```bash
curl -X POST https://localhost:5001/api/v1/auth/refresh \
  -H "Content-Type: application/json" \
  -d '{
    "refreshToken": "your-refresh-token"
  }'
```

### Application Management

#### Create Application

```bash
curl -X POST https://localhost:5001/api/v1/apps \
  -H "X-API-Key: admin-key" \
  -H "Content-Type: application/json" \
  -d '{
    "key": "myapp",
    "name": "My Application",
    "selfRegistrationMode": "Open",
    "accountLockoutEnabled": true,
    "maxFailedLoginAttempts": 5,
    "lockoutDurationMinutes": 30
  }'
```

#### Generate API Key

```bash
curl -X POST https://localhost:5001/api/v1/apps/{appId}/api-key \
  -H "X-API-Key: admin-key"
```

### Permission Checks

#### Check Single Permission

```bash
curl -X POST https://localhost:5001/api/v1/authorization/check \
  -H "X-API-Key: app-key" \
  -H "Content-Type: application/json" \
  -d '{
    "userId": "user-guid",
    "applicationKey": "myapp",
    "module": "Catalog",
    "action": "Read"
  }'
```

#### Bulk Permission Check

```bash
curl -X POST https://localhost:5001/api/v1/authorization/bulk-check \
  -H "X-API-Key: app-key" \
  -H "Content-Type: application/json" \
  -d '{
    "checks": [
      {
        "userId": "user-guid-1",
        "applicationKey": "myapp",
        "module": "Catalog",
        "action": "Read"
      },
      {
        "userId": "user-guid-2",
        "applicationKey": "myapp",
        "module": "Orders",
        "action": "Write"
      }
    ]
  }'
```

## Deployment

### Docker Deployment

#### Using Docker Compose (Recommended for Development)

Start all services (API, PostgreSQL, Redis):

```bash
docker-compose -f docker/docker-compose.yml up -d
```

The API will be available at `http://localhost:8080`

See `docker/README.md` for more details.

#### Building the Docker Image

Build the Docker image:

```bash
docker build -f docker/Dockerfile -t authsmith:latest .
```

#### Running the Container

Run the container:

```bash
docker run -p 8080:8080 \
  -e ConnectionStrings__DefaultConnection="Host=host.docker.internal;Database=authsmith;Username=postgres;Password=postgres" \
  -e Database__AutoMigrate=true \
  authsmith:latest
```

### Production Considerations

1. **Secrets Management**: Use environment variables or a secrets manager (e.g., Azure Key Vault, AWS Secrets Manager) for sensitive configuration
2. **HTTPS**: Always use HTTPS in production. Configure reverse proxy (Nginx, Caddy) with SSL certificates
3. **Database**: Use connection pooling and ensure proper backup strategy
4. **Caching**: Enable Redis for distributed caching in multi-instance deployments
5. **Monitoring**: Set up logging, metrics, and health checks
6. **Rate Limiting**: Consider implementing rate limiting for authentication endpoints

### Environment Variables

Key environment variables:

```bash
ConnectionStrings__DefaultConnection="Host=db;Database=authsmith;Username=postgres;Password=..."
Database__AutoMigrate=true
Jwt__Issuer="https://authsmith.example.com"
Jwt__Audience="authsmith-api"
Jwt__PrivateKeyPath="/secrets/jwt_private_key.pem"
ApiKeys__AdminKey="..."
Redis__Enabled=true
Redis__ConnectionString="redis:6379"
```

## API Documentation

Swagger/OpenAPI documentation is available at `/swagger` when running in development mode.

The API follows RESTful conventions and uses versioning via URL path (`/api/v1/...`).

## Project Structure

```
AuthSmith/
├── docker/                         # Docker configuration
│   ├── Dockerfile                  # Multi-stage Docker build
│   ├── docker-compose.yml          # Development environment
│   └── README.md                   # Docker documentation
├── docs/                           # Documentation
│   └── ARCHITECTURE.md             # Architecture documentation
├── src/
│   ├── AuthSmith.Api/              # REST API layer
│   │   ├── Controllers/            # API endpoints
│   │   ├── Authentication/         # API key authentication
│   │   ├── Authorization/          # Authorization attributes
│   │   ├── Middleware/            # Request/error handling
│   │   └── Extensions/              # Helper extensions
│   ├── AuthSmith.Application/      # Business logic layer
│   │   ├── Services/              # Application services
│   │   └── Validators/             # FluentValidation validators
│   ├── AuthSmith.Domain/           # Domain layer
│   │   ├── Entities/               # Domain entities
│   │   ├── Errors/                 # Domain error types
│   │   └── Interfaces/             # Domain interfaces
│   ├── AuthSmith.Infrastructure/   # Infrastructure layer
│   │   ├── Services/               # External service implementations
│   │   ├── Configuration/          # Configuration classes
│   │   └── Migrations/              # EF Core migrations
│   ├── AuthSmith.Contracts/        # Shared DTOs
│   └── AuthSmith.Sdk/              # Client SDK
└── tests/
    ├── AuthSmith.Api.Tests/        # API integration tests
    ├── AuthSmith.Application.Tests/# Service unit tests
    └── AuthSmith.Domain.Tests/      # Domain tests
```

## Development

### Running Tests

```bash
dotnet test
```

### Code Style

The project follows standard C# conventions. Run analyzers:

```bash
dotnet build
```

### Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md) for contribution guidelines.

## License

See [LICENSE](LICENSE) file for details.
