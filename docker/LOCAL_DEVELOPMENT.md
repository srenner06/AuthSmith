# Local Development Guide

Quick guide to get AuthSmith running locally for development.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Docker Desktop](https://www.docker.com/products/docker-desktop) (for Docker Compose setup)
- [PostgreSQL 16+](https://www.postgresql.org/download/) (if running without Docker)
- [Git](https://git-scm.com/downloads)

**Optional:**
- [Redis](https://redis.io/download) (for distributed caching)
- SMTP server or [MailHog](https://github.com/mailhog/MailHog) (for email testing)

---

## ?? Quick Start (Docker Compose - Recommended)

**Easiest way to get started:**

### 1. Clone the Repository

```bash
git clone https://github.com/srenner06/AuthSmith.git
cd AuthSmith
```

### 2. Generate JWT Keys

```bash
# Create keys directory
mkdir -p docker/keys

# Generate RSA private key
openssl genpkey -algorithm RSA -out docker/keys/jwt_private_key.pem -pkeyopt rsa_keygen_bits:2048

# Generate public key
openssl rsa -pubout -in docker/keys/jwt_private_key.pem -out docker/keys/jwt_public_key.pem

# Set permissions (Linux/Mac)
chmod 600 docker/keys/jwt_private_key.pem
chmod 644 docker/keys/jwt_public_key.pem
```

### 3. Start Everything

```bash
cd docker
docker-compose up -d
```

This starts:
- ? **PostgreSQL** (port 5433) - Database (port changed to avoid conflicts)
- ? **Redis** (port 6379) - Caching & rate limiting
- ? **MailHog** (port 8025) - Email testing UI
- ? **Jaeger v2** (port 16686) - Distributed tracing UI
- ? **AuthSmith API** (port 8080) - The API

### 4. Verify It's Running

```bash
# Check health
curl http://localhost:8080/health

# View logs
docker-compose logs -f api

# Check email UI
open http://localhost:8025  # MailHog web interface

# Check tracing UI
open http://localhost:16686  # Jaeger web interface
```

### 5. Access Services

- **API**: http://localhost:8080
- **Swagger Documentation**: http://localhost:8080/swagger
- **MailHog (Email Testing)**: http://localhost:8025
- **Jaeger (Tracing)**: http://localhost:16686
- **PostgreSQL**: localhost:5433 (mapped to avoid conflicts with local PostgreSQL)
- **Redis**: localhost:6379

### 6. Test Email Functionality

- All emails are caught by MailHog
- View them at: **http://localhost:8025**
- No actual emails are sent (perfect for dev!)

### 7. Stop Everything

```bash
docker-compose down

# To remove volumes (deletes data)
docker-compose down -v
```

---

## ??? Running Locally (Without Docker)

### 1. Setup PostgreSQL

```bash
# Create database
createdb authsmith

# Or via psql
psql -U postgres -c "CREATE DATABASE authsmith;"
```

### 2. Setup Configuration

Copy `appsettings.Development.json.example` to `appsettings.Development.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=authsmith;Username=postgres;Password=yourpassword"
  },
  "Database": {
    "AutoMigrate": true
  },
  "Jwt": {
    "Issuer": "https://localhost:5001",
    "Audience": "authsmith-api",
    "ExpirationMinutes": 15,
    "PrivateKeyPath": "./keys/jwt_private_key.pem",
    "PublicKeyPath": "./keys/jwt_public_key.pem"
  },
  "ApiKeys": {
    "AdminKey": "dev-admin-key",
    "BootstrapKey": "dev-bootstrap-key"
  },
  "Email": {
    "Enabled": true,
    "SmtpHost": "localhost",
    "SmtpPort": 1025,
    "EnableSsl": false,
    "FromAddress": "noreply@authsmith.local",
    "FromName": "AuthSmith",
    "BaseUrl": "https://localhost:5001"
  },
  "Redis": {
    "Enabled": false,
    "ConnectionString": "localhost:6379"
  },
  "RateLimit": {
    "Enabled": true,
    "GeneralLimit": 100,
    "AuthLimit": 10,
    "RegistrationLimit": 5,
    "PasswordResetLimit": 3,
    "WindowSeconds": 60
  }
}
```

### 3. Generate JWT Keys

```bash
mkdir -p keys
openssl genpkey -algorithm RSA -out keys/jwt_private_key.pem -pkeyopt rsa_keygen_bits:2048
openssl rsa -pubout -in keys/jwt_private_key.pem -out keys/jwt_public_key.pem
```

### 4. Run Migrations (Optional - Auto-migrates on startup)

```bash
dotnet ef database update \
  --project src/AuthSmith.Infrastructure \
  --startup-project src/AuthSmith.Api \
  --context AuthSmithDbContext
```

### 5. Run the API

```bash
dotnet run --project src/AuthSmith.Api
```

API will be available at:
- HTTP: **http://localhost:5000**
- HTTPS: **https://localhost:5001**
- Swagger: **https://localhost:5001/swagger**

---

## ?? Email Testing Options

### Option 1: MailHog (Recommended)

```bash
# Using Docker
docker run -d -p 1025:1025 -p 8025:8025 mailhog/mailhog

# Or install locally
# https://github.com/mailhog/MailHog#installation
```

**Configuration:**
```json
{
  "Email": {
    "Enabled": true,
    "SmtpHost": "localhost",
    "SmtpPort": 1025,
    "EnableSsl": false
  }
}
```

**View emails:** http://localhost:8025

### Option 2: Mailtrap.io (Free for Dev)

1. Sign up at https://mailtrap.io
2. Get SMTP credentials
3. Configure:

```json
{
  "Email": {
    "Enabled": true,
    "SmtpHost": "smtp.mailtrap.io",
    "SmtpPort": 2525,
    "EnableSsl": true,
    "Username": "your-mailtrap-username",
    "Password": "your-mailtrap-password"
  }
}
```

### Option 3: Disable Email

```json
{
  "Email": {
    "Enabled": false
  }
}
```

---

## ?? OpenTelemetry / Distributed Tracing

### What is OpenTelemetry?

OpenTelemetry provides observability into your application:
- **Traces**: See request flow across services
- **Metrics**: Monitor performance and resource usage
- **Context**: Understand what's happening in production

### Using Jaeger (Included in Docker Compose)

**Jaeger** is automatically started with `docker-compose up`:

1. **Access Jaeger UI**: http://localhost:16686
2. **Select Service**: Choose "AuthSmith" from dropdown
3. **View Traces**: See all HTTP requests, database queries, Redis calls
4. **Analyze Performance**: Find slow endpoints and bottlenecks

**What You Can See:**
- HTTP request duration
- Database query execution times
- Redis cache hits/misses
- Error stack traces
- Request flow through middleware

### Enable/Disable OpenTelemetry

**Enable (default in Docker Compose):**
```json
{
  "OpenTelemetry": {
    "Enabled": true,
    "Endpoint": "http://jaeger:4317",
    "ServiceName": "AuthSmith",
    "EnableTracing": true,
    "EnableMetrics": true
  }
}
```

**Disable (for minimal overhead):**
```json
{
  "OpenTelemetry": {
    "Enabled": false
  }
}
```

### Using Other OTLP Backends

**Jaeger** is just one option. You can also use:

- **Grafana Tempo**: `http://tempo:4317`
- **Honeycomb**: `https://api.honeycomb.io` (with API key header)
- **New Relic**: `https://otlp.nr-data.net:4317` (with API key)
- **Azure Application Insights**: Via OTLP endpoint
- **AWS X-Ray**: Via OTLP endpoint
- **Datadog**: Via OTLP endpoint

**Example for external service:**
```json
{
  "OpenTelemetry": {
    "Enabled": true,
    "Endpoint": "https://api.honeycomb.io",
    "Headers": {
      "x-honeycomb-team": "your-api-key"
    }
  }
}
```

---

## ?? Common Development Tasks

### Database Migrations

```bash
# Create new migration
dotnet ef migrations add MigrationName \
  --project src/AuthSmith.Infrastructure \
  --startup-project src/AuthSmith.Api

# Apply migrations
dotnet ef database update \
  --project src/AuthSmith.Infrastructure \
  --startup-project src/AuthSmith.Api
```

### Running Tests

```bash
# All tests
dotnet test

# Specific project
dotnet test tests/AuthSmith.Application.Tests

# With coverage
dotnet test /p:CollectCoverage=true
```

### Format Code

```bash
dotnet format
```

### Build Release

```bash
dotnet build -c Release
```

---

## ?? Development vs Production

### Development Settings

```json
{
  "Database": { "AutoMigrate": true },
  "Email": { "Enabled": true, "SmtpHost": "mailhog" },
  "Redis": { "Enabled": false },
  "RateLimit": { "Enabled": false },
  "Serilog": { "MinimumLevel": { "Default": "Debug" } }
}
```

### Production Settings

```json
{
  "Database": { "AutoMigrate": false },
  "Email": { "Enabled": true, "SmtpHost": "real-smtp-server" },
  "Redis": { "Enabled": true },
  "RateLimit": { "Enabled": true },
  "Serilog": { "MinimumLevel": { "Default": "Information" } }
}
```

**?? NEVER commit:**
- Real API keys
- Production passwords
- SMTP credentials
- Database passwords

Use environment variables or secret managers!

---

## ?? Additional Resources

- [Architecture Documentation](../docs/ARCHITECTURE.md)
- [API Documentation](http://localhost:8080/swagger)
- [Contributing Guidelines](../CONTRIBUTING.md)
- [Security Policy](../SECURITY.md)

---

## ?? Need Help?

1. Check existing [GitHub Issues](https://github.com/srenner06/AuthSmith/issues)
2. Review [SECURITY.md](../SECURITY.md) for realistic expectations
3. Remember: Personal project, no guaranteed support! ??

---

**Happy coding!** ??
