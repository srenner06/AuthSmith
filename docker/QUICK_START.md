# Quick Start - Local Development

**Get AuthSmith running in under 2 minutes!** ??

---

## ?? Configuration Pattern

**IMPORTANT:** All Docker configuration is in the `.env` file!

- ? **Edit `.env`** to change any configuration
- ? **Don't edit `docker-compose.yml`** - it uses variables from `.env`
- ?? **Restart containers** after changing `.env`

**The pattern:**
```yaml
# docker-compose.yml uses variable substitution
RateLimit__AuthLimit: "${RATE_LIMIT_AUTH_LIMIT:-10}"
# ? Value comes from .env, falls back to 10 if not set
```

```bash
# docker/.env is the single source of truth
RATE_LIMIT_AUTH_LIMIT=30  # ? Change this!
```

See `.env.template` for all available configuration variables.

---

## ?? One-Command Setup

### **Linux / Mac:**
```bash
cd docker
chmod +x setup-dev.sh
./setup-dev.sh
```

### **Windows:**
```powershell
cd docker
powershell -ExecutionPolicy Bypass -File setup-dev.ps1
```

**That's it!** The script will:
- ?? Read `.env.template` as source of truth
- ?? Merge with existing `.env` (preserves your customizations)
- ?? Generate secure passwords and API keys for placeholders
- ?? Generate JWT RSA key pair
- ?? Create necessary directories

**Smart merging:** If you already have a `.env` file, the script only updates missing values and generates new values for placeholders, preserving your customizations!

---

## ?? Start Services

```bash
docker-compose up -d
```

**Wait 20-30 seconds for services to start**, then:

```bash
# Test the API
curl http://localhost:8080/api/ping

# Or open in browser
open http://localhost:8080/swagger
```

---

## ?? Access Services

| Service | URL | Purpose |
|---------|-----|---------|
| **API Swagger** | http://localhost:8080/swagger | API documentation & testing |
| **MailHog** | http://localhost:8025 | Email testing |
| **.NET Aspire Dashboard** | http://localhost:18888 | Observability: Traces, Metrics & Logs |
| **PostgreSQL** | localhost:5433 | Database |
| **Redis** | localhost:6379 | Cache |

---

## ??? Your Credentials

After running the setup script, your credentials are in `.env`:

```bash
# View your admin API key
grep ADMIN_API_KEY .env

# View your PostgreSQL password
grep POSTGRES_PASSWORD .env
```

**?? Keep `.env` file secure - it contains secrets!**

---

## ?? Common Configuration Changes

All configuration is in the `.env` file. Edit it, then restart containers.

### Disable Rate Limiting (for testing)
```bash
# Edit .env
RATE_LIMIT_ENABLED=false

# Restart
docker-compose restart api
```

### Enable Debug Logging
```bash
# Edit .env
SERILOG_MIN_LEVEL=Debug

# Restart and view logs
docker-compose restart api
docker-compose logs -f api
```

### Change Rate Limits
```bash
# Edit .env
RATE_LIMIT_AUTH_LIMIT=50
RATE_LIMIT_REGISTRATION_LIMIT=100
RATE_LIMIT_PASSWORD_RESET_LIMIT=50

# Restart
docker-compose restart api
```

### Verify Configuration Loaded
```bash
# Check what the application loaded
docker-compose logs api | grep "Rate Limit Configuration"
```

**See `.env.example` for all available variables!**

---

## ? Test It

### **1. Register a User**

```bash
# Get your admin API key
ADMIN_KEY=$(grep ADMIN_API_KEY .env | cut -d'=' -f2)

# Register a user
curl -X POST http://localhost:8080/api/v1/auth/register/myapp \
  -H "X-API-Key: $ADMIN_KEY" \
  -H "Content-Type: application/json" \
  -d '{
    "username": "testuser",
    "email": "test@example.com",
    "password": "Test123!@#"
  }'
```

### **2. Check Email**

Open http://localhost:8025 - you'll see the verification email!

### **3. View Traces & Metrics**

Open http://localhost:18888 - .NET Aspire Dashboard shows:
1. **Structured Logs** - Real-time application logs
2. **Traces** - Request flow and performance
3. **Metrics** - Live performance counters
4. **Resources** - Service health and connections

---

## ?? Common Commands

```bash
# Start services
docker-compose up -d

# View logs
docker-compose logs -f

# Stop services
docker-compose down

# Restart API only
docker-compose restart api

# Rebuild and restart
docker-compose down
docker-compose build
docker-compose up -d

# Clean everything (including data)
docker-compose down -v
```

---

## ?? Reset Everything

Want to start fresh?

```bash
# Stop and remove everything (including database data)
docker-compose down -v

# Remove generated files
rm -f .env docker-compose.override.yml
rm -rf keys/ logs/

# Run setup again
./setup-dev.sh  # or setup-dev.ps1 on Windows

# Start fresh
docker-compose up -d
```

**Note:** If you regenerate the `.env` file with new passwords, you **must** use `docker-compose down -v` to remove the old database volume, otherwise the password mismatch will cause connection errors.

---

## ?? Troubleshooting

### **Port Already in Use**

If PostgreSQL port 5433 is in use:

```bash
# Edit .env
DATABASE_PORT=5434  # Change to different port

# Edit docker-compose.yml
# Change: "5433:5432" to "5434:5432"
```

### **OpenSSL Not Found (Windows)**

Install OpenSSL:
- Download: https://slproweb.com/products/Win32OpenSSL.html
- Or via Chocolatey: `choco install openssl`
- Or via Scoop: `scoop install openssl`

### **Services Won't Start**

```bash
# Check logs
docker-compose logs

# Check if ports are available
netstat -ano | findstr "5433 8080 6379"  # Windows
lsof -i :5433,8080,6379                   # Mac/Linux

# Try rebuilding
docker-compose down
docker-compose build --no-cache
docker-compose up -d
```

---

## ?? More Information

- **[COMPLETE_SETUP_GUIDE.md](COMPLETE_SETUP_GUIDE.md)** - Comprehensive guide
- **[LOCAL_DEVELOPMENT.md](LOCAL_DEVELOPMENT.md)** - Development workflow
- **[API_KEY_GUIDE.md](../docs/API_KEY_GUIDE.md)** - API key usage
- **[SDK_GUIDE.md](../docs/SDK_GUIDE.md)** - Client SDK

---

## ?? Quick Checklist

- [ ] Run setup script (`setup-dev.sh` or `setup-dev.ps1`)
- [ ] Start services (`docker-compose up -d`)
- [ ] Wait 30 seconds
- [ ] Test ping endpoint (`curl http://localhost:8080/api/ping`)
- [ ] Open Swagger (http://localhost:8080/swagger)
- [ ] Register a test user
- [ ] Check MailHog (http://localhost:8025)
- [ ] View observability in Aspire Dashboard (http://localhost:18888)

---

**You're ready to develop!** ??
