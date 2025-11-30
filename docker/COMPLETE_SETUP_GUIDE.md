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
 ? Network docker_authsmith-network  Created
 ? Container authsmith-db            Started
 ? Container authsmith-redis         Started
 ? Container authsmith-mailhog       Started
 ? Container authsmith-jaeger        Started
 ? Container authsmith-api           Started
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
- **Jaeger (Distributed Tracing)**: http://localhost:16686

---

## ?? Services Overview

| Service | Port | Purpose |
|---------|------|---------|
| **AuthSmith API** | 8080 | Main application |
| **PostgreSQL** | 5433 | Database (note: non-standard port to avoid conflicts) |
| **Redis** | 6379 | Distributed cache & rate limiting |
| **MailHog** | 8025 | Email testing (catches all outgoing emails) |
| **Jaeger** | 16686 | Distributed tracing & observability |

---

## ?? Configuration

### **Environment Variables**

All configured in `docker-compose.yml`. Key settings:

```yaml
# Database
Database__AutoMigrate: "true"  # Automatically applies migrations on startup

# Email (MailHog for local development)
Email__Enabled: "true"
Email__SmtpHost: "mailhog"

# Redis
Redis__Enabled: "true"

# OpenTelemetry
OpenTelemetry__Enabled: "true"
OpenTelemetry__Endpoint: "http://jaeger:4317"

# Rate Limiting
RateLimit__Enabled: "true"
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
- ? Jaeger trace visualization
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

### **3. View Request Traces**

Open http://localhost:16686:
1. Select "AuthSmith" service
2. Click "Find Traces"
3. View the registration request trace

### **4. Test Ping Endpoint**

```bash
curl http://localhost:8080/api/ping | jq
```

Returns version and build information.

---

## ?? Production Considerations

### **Security**

**?? Change these before deploying to production:**

- Database password (`POSTGRES_PASSWORD`)
- Admin API keys (`ApiKeys__Admin__0`, `ApiKeys__Admin__1`, etc.)
- Disable auto-migrations (`Database__AutoMigrate: "false"`)
- Use real SMTP server instead of MailHog
- Enable HTTPS with proper certificates

### **Recommended Changes**

```yaml
# Production docker-compose.yml changes
environment:
  ASPNETCORE_ENVIRONMENT: Production
  Database__AutoMigrate: "false"  # Run migrations manually
  
  # Admin API Keys (use strong, unique values)
  ApiKeys__Admin__0: "${ADMIN_API_KEY_1}"  # From secrets manager
  ApiKeys__Admin__1: "${ADMIN_API_KEY_2}"  # Optional: multiple keys for different admins
  
  # Email
  Email__SmtpHost: "smtp.sendgrid.net"  # Real SMTP
  Email__Username: "apikey"
  Email__Password: "${SENDGRID_API_KEY}"  # From secrets
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
- [ ] Jaeger UI accessible (http://localhost:16686)
- [ ] Can register a user successfully
- [ ] Email appears in MailHog
- [ ] Traces appear in Jaeger

---

**Your AuthSmith development environment is ready!** ??

For production deployment, see the security notes above and update all default credentials.
