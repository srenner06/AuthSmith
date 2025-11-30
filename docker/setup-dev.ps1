# AuthSmith Local Development Setup Script (PowerShell)
# This script sets up everything you need to run AuthSmith locally

Write-Host "==========================================" -ForegroundColor Cyan
Write-Host "  AuthSmith Local Development Setup" -ForegroundColor Cyan
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host ""

# Change to script directory
Set-Location $PSScriptRoot

# Check for OpenSSL
$opensslPath = Get-Command openssl -ErrorAction SilentlyContinue
if (-not $opensslPath) {
    Write-Host "[ERROR] OpenSSL is not installed or not in PATH" -ForegroundColor Red
    Write-Host ""
    Write-Host "Please install OpenSSL:" -ForegroundColor Yellow
    Write-Host "  1. Download from: https://slproweb.com/products/Win32OpenSSL.html"
    Write-Host "  2. Or install via Chocolatey: choco install openssl"
    Write-Host "  3. Or install via Scoop: scoop install openssl"
    Write-Host ""
    Read-Host "Press Enter to exit"
    exit 1
}

# Create .env file
$envFile = ".env"

Write-Host "[Step 1] Creating environment configuration..." -ForegroundColor Yellow

if (Test-Path $envFile) {
    Write-Host "[WARNING] .env file already exists. Backing up to .env.backup" -ForegroundColor Yellow
    Copy-Item $envFile "$envFile.backup" -Force
}

# Generate secure random passwords and keys
function Generate-SecurePassword {
    $bytes = & openssl rand -base64 32
    $password = $bytes -replace '[/+=]', 'X'
    return $password.Substring(0, [Math]::Min(32, $password.Length))
}

$postgresPassword = Generate-SecurePassword
$adminApiKey = Generate-SecurePassword

# Create .env file
@"
# AuthSmith Local Development Environment
# Generated: $(Get-Date -Format "yyyy-MM-dd HH:mm:ss")

# Database Configuration
POSTGRES_USER=authsmith
POSTGRES_PASSWORD=$postgresPassword
POSTGRES_DB=authsmith
DATABASE_PORT=5433

# API Keys
ADMIN_API_KEY=$adminApiKey

# JWT Configuration
JWT_ISSUER=https://localhost:8080
JWT_AUDIENCE=authsmith-api
JWT_EXPIRATION_MINUTES=15
JWT_REFRESH_TOKEN_EXPIRATION_DAYS=7

# CORS Configuration
CORS_ALLOWED_ORIGINS=http://localhost:3000,http://localhost:5173,http://localhost:8080

# Redis Configuration
REDIS_ENABLED=true
REDIS_CONNECTION_STRING=redis:6379

# Email Configuration (MailHog for local development)
EMAIL_ENABLED=true
EMAIL_SMTP_HOST=mailhog
EMAIL_SMTP_PORT=1025
EMAIL_ENABLE_SSL=false
EMAIL_FROM_ADDRESS=noreply@authsmith.local
EMAIL_FROM_NAME=AuthSmith Dev
EMAIL_BASE_URL=http://localhost:8080

# Rate Limiting
RATE_LIMIT_ENABLED=true
RATE_LIMIT_GENERAL_LIMIT=100
RATE_LIMIT_AUTH_LIMIT=10
RATE_LIMIT_REGISTRATION_LIMIT=5
RATE_LIMIT_PASSWORD_RESET_LIMIT=3
RATE_LIMIT_WINDOW_SECONDS=60

# OpenTelemetry (Jaeger)
OTEL_ENABLED=true
OTEL_ENDPOINT=http://jaeger:4317
OTEL_SERVICE_NAME=AuthSmith
OTEL_SERVICE_VERSION=1.0.0-dev

# Logging
SERILOG_MIN_LEVEL=Information
"@ | Out-File -FilePath $envFile -Encoding ASCII

Write-Host "[OK] Created .env file" -ForegroundColor Green
Write-Host ""

# Generate JWT keys
Write-Host "[Step 2] Generating JWT keys..." -ForegroundColor Yellow

$keysDir = "keys"
$privateKey = Join-Path $keysDir "jwt_private_key.pem"
$publicKey = Join-Path $keysDir "jwt_public_key.pem"

if ((Test-Path $privateKey) -and (Test-Path $publicKey)) {
    Write-Host "[WARNING] JWT keys already exist. Skipping generation." -ForegroundColor Yellow
} else {
    # Ensure keys directory exists
    if (-not (Test-Path $keysDir)) {
        New-Item -ItemType Directory -Path $keysDir -Force | Out-Null
    }
    
    # Generate RSA private key
    Write-Host "Generating private key..." -ForegroundColor Gray
    & openssl genpkey -algorithm RSA -out $privateKey -pkeyopt rsa_keygen_bits:2048 2>$null
    if ($LASTEXITCODE -ne 0) {
        Write-Host "[ERROR] Failed to generate private key" -ForegroundColor Red
        Read-Host "Press Enter to exit"
        exit 1
    }
    
    # Extract public key from private key
    Write-Host "Generating public key..." -ForegroundColor Gray
    & openssl rsa -pubout -in $privateKey -out $publicKey 2>$null
    if ($LASTEXITCODE -ne 0) {
        Write-Host "[ERROR] Failed to generate public key" -ForegroundColor Red
        Read-Host "Press Enter to exit"
        exit 1
    }
    
    Write-Host "[OK] Generated JWT RSA key pair" -ForegroundColor Green
}
Write-Host ""

# Create logs directory
Write-Host "[Step 3] Creating directories..." -ForegroundColor Yellow
if (-not (Test-Path "logs")) {
    New-Item -ItemType Directory -Path "logs" -Force | Out-Null
}
Write-Host "[OK] Created logs directory" -ForegroundColor Green
Write-Host ""

# Create docker-compose.override.yml
Write-Host "[Step 4] Updating docker-compose configuration..." -ForegroundColor Yellow

@"
services:
  db:
    env_file:
      - .env
    environment:
      POSTGRES_USER: `${POSTGRES_USER}
      POSTGRES_PASSWORD: `${POSTGRES_PASSWORD}
      POSTGRES_DB: `${POSTGRES_DB}

  api:
    env_file:
      - .env
    environment:
      # Database
      ConnectionStrings__DefaultConnection: "Host=db;Port=5432;Database=`${POSTGRES_DB};Username=`${POSTGRES_USER};Password=`${POSTGRES_PASSWORD}"
      
      # API Keys
      ApiKeys__Admin__0: `${ADMIN_API_KEY}
      
      # JWT
      Jwt__Issuer: `${JWT_ISSUER}
      Jwt__Audience: `${JWT_AUDIENCE}
      Jwt__ExpirationMinutes: `${JWT_EXPIRATION_MINUTES}
      Jwt__RefreshTokenExpirationDays: `${JWT_REFRESH_TOKEN_EXPIRATION_DAYS}
      
      # CORS
      Cors__AllowedOrigins__0: "http://localhost:3000"
      Cors__AllowedOrigins__1: "http://localhost:5173"
      Cors__AllowedOrigins__2: "http://localhost:8080"
      
      # Email
      Email__Enabled: `${EMAIL_ENABLED}
      Email__SmtpHost: `${EMAIL_SMTP_HOST}
      Email__SmtpPort: `${EMAIL_SMTP_PORT}
      Email__EnableSsl: `${EMAIL_ENABLE_SSL}
      Email__FromAddress: `${EMAIL_FROM_ADDRESS}
      Email__FromName: `${EMAIL_FROM_NAME}
      Email__BaseUrl: `${EMAIL_BASE_URL}
      
      # Redis
      Redis__Enabled: `${REDIS_ENABLED}
      Redis__ConnectionString: `${REDIS_CONNECTION_STRING}
      
      # Rate Limiting
      RateLimit__Enabled: `${RATE_LIMIT_ENABLED}
      RateLimit__GeneralLimit: `${RATE_LIMIT_GENERAL_LIMIT}
      RateLimit__AuthLimit: `${RATE_LIMIT_AUTH_LIMIT}
      RateLimit__RegistrationLimit: `${RATE_LIMIT_REGISTRATION_LIMIT}
      RateLimit__PasswordResetLimit: `${RATE_LIMIT_PASSWORD_RESET_LIMIT}
      RateLimit__WindowSeconds: `${RATE_LIMIT_WINDOW_SECONDS}
      RateLimit__RedisConnectionString: `${REDIS_CONNECTION_STRING}
      
      # OpenTelemetry
      OpenTelemetry__Enabled: `${OTEL_ENABLED}
      OpenTelemetry__Endpoint: `${OTEL_ENDPOINT}
      OpenTelemetry__ServiceName: `${OTEL_SERVICE_NAME}
      OpenTelemetry__ServiceVersion: `${OTEL_SERVICE_VERSION}
      
      # Serilog
      Serilog__MinimumLevel__Default: `${SERILOG_MIN_LEVEL}
"@ | Out-File -FilePath "docker-compose.override.yml" -Encoding ASCII

Write-Host "[OK] Created docker-compose.override.yml" -ForegroundColor Green
Write-Host ""

# Print summary
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host "  Setup Complete!" -ForegroundColor Green
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Summary:" -ForegroundColor White
Write-Host "  * .env file created with secure credentials"
Write-Host "  * JWT keys generated in keys/"
Write-Host "  * docker-compose.override.yml created"
Write-Host ""
Write-Host "Your credentials:" -ForegroundColor Yellow
Write-Host "  * Admin API Key: $adminApiKey"
Write-Host "  * PostgreSQL Password: $postgresPassword"
Write-Host ""
Write-Host "[WARNING] IMPORTANT: These credentials are saved in .env" -ForegroundColor Yellow
Write-Host ""
Write-Host "Next steps:" -ForegroundColor White
Write-Host "  1. Start services:"
Write-Host "     docker-compose up -d" -ForegroundColor Cyan
Write-Host ""
Write-Host "  2. Verify everything is running:"
Write-Host "     docker-compose ps" -ForegroundColor Cyan
Write-Host ""
Write-Host "  3. Test the API:"
Write-Host "     curl http://localhost:8080/api/ping" -ForegroundColor Cyan
Write-Host ""
Write-Host "  4. Access services:"
Write-Host "     * API Swagger: http://localhost:8080/swagger"
Write-Host "     * MailHog: http://localhost:8025"
Write-Host "     * Jaeger: http://localhost:16686"
Write-Host ""
Write-Host "For more information, see:"
Write-Host "  * COMPLETE_SETUP_GUIDE.md"
Write-Host "  * LOCAL_DEVELOPMENT.md"
Write-Host ""
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host ""
Read-Host "Press Enter to exit"
