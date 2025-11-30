# Quick Start - Local Development

**Get AuthSmith running in under 2 minutes!** ?

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
- ? Generate secure passwords and API keys
- ? Create `.env` file with all configuration
- ? Generate JWT RSA key pair
- ? Create `docker-compose.override.yml`
- ? Create necessary directories

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
| **Jaeger** | http://localhost:16686 | Distributed tracing |
| **PostgreSQL** | localhost:5433 | Database |
| **Redis** | localhost:6379 | Cache |

---

## ?? Your Credentials

After running the setup script, your credentials are in `.env`:

```bash
# View your admin API key
grep ADMIN_API_KEY .env

# View your PostgreSQL password
grep POSTGRES_PASSWORD .env
```

**?? Keep `.env` file secure - it contains secrets!**

---

## ?? Test It

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

### **3. View Traces**

Open http://localhost:16686:
1. Select "AuthSmith" service
2. Click "Find Traces"
3. See your request!

---

## ??? Common Commands

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

## ? Quick Checklist

- [ ] Run setup script (`setup-dev.sh` or `setup-dev.ps1`)
- [ ] Start services (`docker-compose up -d`)
- [ ] Wait 30 seconds
- [ ] Test ping endpoint (`curl http://localhost:8080/api/ping`)
- [ ] Open Swagger (http://localhost:8080/swagger)
- [ ] Register a test user
- [ ] Check MailHog (http://localhost:8025)
- [ ] View traces in Jaeger (http://localhost:16686)

---

**You're ready to develop!** ??
