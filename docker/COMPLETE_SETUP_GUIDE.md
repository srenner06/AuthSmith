# AuthSmith - Docker Setup Guide

**Complete guide for running AuthSmith with Docker Compose**

---

## ? TL;DR - Quick Start

**Want to get started in under 2 minutes?**

```bash
cd docker
./setup-dev.sh  # Linux/Mac
# or
powershell -ExecutionPolicy Bypass -File setup-dev.ps1  # Windows

docker-compose up -d
```

**Done!** See [QUICK_START.md](QUICK_START.md) for details.

---

## ?? Manual Setup

### **1. Generate Configuration and Keys**

**Run the setup script:**

**Linux / Mac:**
```bash
cd docker
chmod +x setup-dev.sh
./setup-dev.sh
```

**Windows:**
```powershell
cd docker
powershell -ExecutionPolicy Bypass -File setup-dev.ps1
```

This will:
- Generate secure passwords and API keys
- Create `.env` file with all configuration
- Generate JWT RSA key pair
- Create `docker-compose.override.yml`

### **2. Start All Services**

```bash
cd docker
docker-compose up -d
```

**Expected output:**
```
[+] Running 6/6
 ? Network docker_authsmith-network     Created
 ? Container authsmith-db               Started
 ? Container authsmith-redis            Started
 ? Container authsmith-mailhog          Started
 ? Container authsmith-aspire-dashboard Started
 ? Container authsmith-api              Started
```

### **3. Verify Services**

```bash
# Check all containers running
docker-compose ps

# Test API health
curl http://localhost:8080/health

# Should return: {"status":"Healthy"}
```

### **4. Access Services**

- **API Swagger**: http://localhost:8080/swagger
- **MailHog (Email Testing)**: http://localhost:8025
- **.NET Aspire Dashboard (Observability)**: http://localhost:18888

---

## ?? Services Overview

| Service | Port | Purpose |
|---------|------|---------|
| **AuthSmith API** | 8080 | Main application |
| **PostgreSQL** | 5433 | Database (note: non-standard port to avoid conflicts) |
| **Redis** | 6379 | Distributed cache & rate limiting |
| **MailHog** | 8025 | Email testing (catches all outgoing emails) |
| **.NET Aspire Dashboard** | 18888 | Unified observability: traces, metrics & logs |

---

## ?? Configuration

### **Environment Variables**

All configured in `.env` file. Key settings:

```bash
# Database
DATABASE_AUTO_MIGRATE=true  # Automatically applies migrations on startup

# Email (MailHog for local development)
EMAIL_ENABLED=true
EMAIL_SMTP_HOST=mailhog

# Redis
REDIS_ENABLED=true

# OpenTelemetry
OTEL_ENABLED=true
OTEL_ENDPOINT=http://aspire-dashboard:18889

# Rate Limiting
RATE_LIMIT_ENABLED=true
```

---

## ?? Features Included

### **Core Features**:
- ? JWT & API Key Authentication
- ? Multi-application support
- ? Role-based authorization
- ? Permission-based access control
- ? Password reset flow
- ? Email verification
- ? User profile management
- ? Session management with device tracking
- ? Audit logging (30+ event types)

### **Security**:
- ? Security headers (OWASP-compliant)
- ? CORS configuration
- ? Rate limiting
- ? Refresh token rotation

### **Observability**:
- ? Serilog structured logging
- ? OpenTelemetry distributed tracing
- ? .NET Aspire Dashboard for unified observability
- ? Health check endpoints (`/health`, `/ready`)

---

## ?? Testing the Setup

### **1. Register a User**

```bash
curl -X POST http://localhost:8080/api/v1/auth/register/myapp \
  -H "X-API-Key: dev-admin-key-change-in-production" \
  -H "Content-Type: application/json" \
  -d '{
    "username": "testuser",
    "email": "test@example.com",
    "password": "Test123!@#"
  }'
```

### **2. Check Email in MailHog**

Open http://localhost:8025 to see the verification email.

### **3. View Request Traces and Metrics**

Open http://localhost:18888 - .NET Aspire Dashboard:
1. **Structured Logs**: Filter and search real-time logs
2. **Traces**: View the registration request with full timing breakdown
3. **Metrics**: Monitor live performance counters
4. **Resources**: Check service health and connections

### **4. Test Ping Endpoint**

```bash
curl http://localhost:8080/api/ping | jq
```

Returns version and build information.

---

## ?? Production Considerations

### **Security**

**?? Change these before deploying to production:**

- Database password (`POSTGRES_PASSWORD` in `.env`)
- Admin API keys (`ADMIN_API_KEY` in `.env`)
- Disable auto-migrations (`DATABASE_AUTO_MIGRATE=false`)
- Use real SMTP server instead of MailHog
- Enable HTTPS with proper certificates

### **Recommended Changes**

```bash
# Production .env changes
ASPNETCORE_ENVIRONMENT=Production
DATABASE_AUTO_MIGRATE=false  # Run migrations manually

# Admin API Keys (use strong, unique values)
ADMIN_API_KEY=<strong-random-key-from-secrets-manager>

# Email
EMAIL_SMTP_HOST=smtp.sendgrid.net  # Real SMTP
EMAIL_USERNAME=apikey
EMAIL_PASSWORD=<sendgrid-api-key-from-secrets>

# OpenTelemetry - consider production APM
OTEL_ENDPOINT=https://your-apm-endpoint
```

---

## ?? Additional Documentation

- **[SDK Guide](../docs/SDK_GUIDE.md)** - Client SDK usage
- **[Ping Endpoint](../docs/PING_ENDPOINT.md)** - Health check endpoint
- **[Local Development](LOCAL_DEVELOPMENT.md)** - Detailed development guide
- **[README](../README.md)** - Project overview

---

## ? Verification Checklist

Before considering your setup complete:

- [ ] JWT keys generated in `docker/keys/`
- [ ] All 5 containers running (`docker-compose ps`)
- [ ] API health check passes (`curl http://localhost:8080/health`)
- [ ] Swagger UI accessible (http://localhost:8080/swagger)
- [ ] MailHog UI accessible (http://localhost:8025)
- [ ] .NET Aspire Dashboard accessible (http://localhost:18888)
- [ ] Can register a user successfully
- [ ] Email appears in MailHog
- [ ] Traces and metrics appear in Aspire Dashboard

---

**Your AuthSmith development environment is ready!** ??

For production deployment, see the security notes above and update all default credentials.
