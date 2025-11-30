#!/bin/bash
set -euo pipefail

# AuthSmith Local Development Setup Script
# This script sets up everything you need to run AuthSmith locally

echo "=========================================="
echo "  AuthSmith Local Development Setup"
echo "=========================================="
echo ""

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Change to script directory
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$SCRIPT_DIR"

# Create .env file if it doesn't exist
ENV_FILE=".env"

echo "?? Step 1: Creating environment configuration..."

if [ -f "$ENV_FILE" ]; then
    echo -e "${YELLOW}??  .env file already exists. Backing up to .env.backup${NC}"
    cp "$ENV_FILE" "${ENV_FILE}.backup"
fi

# Generate secure random passwords and keys
generate_password() {
    openssl rand -base64 32 | tr -d "=+/" | cut -c1-32
}

POSTGRES_PASSWORD=$(generate_password)
ADMIN_API_KEY=$(generate_password)
JWT_SECRET=$(openssl rand -base64 64 | tr -d '\n')

# Create .env file
cat > "$ENV_FILE" << EOF
# AuthSmith Local Development Environment
# Generated: $(date)

# Database Configuration
POSTGRES_USER=authsmith
POSTGRES_PASSWORD=${POSTGRES_PASSWORD}
POSTGRES_DB=authsmith
DATABASE_PORT=5433

# API Keys
ADMIN_API_KEY=${ADMIN_API_KEY}

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
EOF

echo -e "${GREEN}? Created .env file${NC}"
echo ""

# Generate JWT keys
echo "?? Step 2: Generating JWT keys..."

KEYS_DIR="keys"
PRIVATE_KEY="$KEYS_DIR/jwt_private_key.pem"
PUBLIC_KEY="$KEYS_DIR/jwt_public_key.pem"

if [ -f "$PRIVATE_KEY" ] && [ -f "$PUBLIC_KEY" ]; then
    echo -e "${YELLOW}??  JWT keys already exist. Skipping generation.${NC}"
else
    mkdir -p "$KEYS_DIR"
    
    # Generate RSA key pair
    openssl genpkey -algorithm RSA -out "$PRIVATE_KEY" -pkeyopt rsa_keygen_bits:2048 2>/dev/null
    openssl rsa -pubout -in "$PRIVATE_KEY" -out "$PUBLIC_KEY" 2>/dev/null
    
    # Set permissions
    chmod 600 "$PRIVATE_KEY"
    chmod 644 "$PUBLIC_KEY"
    
    echo -e "${GREEN}? Generated JWT RSA key pair${NC}"
fi
echo ""

# Create logs directory
echo "?? Step 3: Creating directories..."
mkdir -p logs
echo -e "${GREEN}? Created logs directory${NC}"
echo ""

# Update docker-compose.yml to use .env
echo "?? Step 4: Updating docker-compose.yml..."

# Create docker-compose.override.yml for local development
cat > "docker-compose.override.yml" << 'EOF'
services:
  db:
    env_file:
      - .env
    environment:
      POSTGRES_USER: ${POSTGRES_USER}
      POSTGRES_PASSWORD: ${POSTGRES_PASSWORD}
      POSTGRES_DB: ${POSTGRES_DB}

  api:
    env_file:
      - .env
    environment:
      # Database
      ConnectionStrings__DefaultConnection: "Host=db;Port=5432;Database=${POSTGRES_DB};Username=${POSTGRES_USER};Password=${POSTGRES_PASSWORD}"
      
      # API Keys
      ApiKeys__Admin__0: ${ADMIN_API_KEY}
      
      # JWT
      Jwt__Issuer: ${JWT_ISSUER}
      Jwt__Audience: ${JWT_AUDIENCE}
      Jwt__ExpirationMinutes: ${JWT_EXPIRATION_MINUTES}
      Jwt__RefreshTokenExpirationDays: ${JWT_REFRESH_TOKEN_EXPIRATION_DAYS}
      
      # CORS
      Cors__AllowedOrigins__0: "http://localhost:3000"
      Cors__AllowedOrigins__1: "http://localhost:5173"
      Cors__AllowedOrigins__2: "http://localhost:8080"
      
      # Email
      Email__Enabled: ${EMAIL_ENABLED}
      Email__SmtpHost: ${EMAIL_SMTP_HOST}
      Email__SmtpPort: ${EMAIL_SMTP_PORT}
      Email__EnableSsl: ${EMAIL_ENABLE_SSL}
      Email__FromAddress: ${EMAIL_FROM_ADDRESS}
      Email__FromName: ${EMAIL_FROM_NAME}
      Email__BaseUrl: ${EMAIL_BASE_URL}
      
      # Redis
      Redis__Enabled: ${REDIS_ENABLED}
      Redis__ConnectionString: ${REDIS_CONNECTION_STRING}
      
      # Rate Limiting
      RateLimit__Enabled: ${RATE_LIMIT_ENABLED}
      RateLimit__GeneralLimit: ${RATE_LIMIT_GENERAL_LIMIT}
      RateLimit__AuthLimit: ${RATE_LIMIT_AUTH_LIMIT}
      RateLimit__RegistrationLimit: ${RATE_LIMIT_REGISTRATION_LIMIT}
      RateLimit__PasswordResetLimit: ${RATE_LIMIT_PASSWORD_RESET_LIMIT}
      RateLimit__WindowSeconds: ${RATE_LIMIT_WINDOW_SECONDS}
      RateLimit__RedisConnectionString: ${REDIS_CONNECTION_STRING}
      
      # OpenTelemetry
      OpenTelemetry__Enabled: ${OTEL_ENABLED}
      OpenTelemetry__Endpoint: ${OTEL_ENDPOINT}
      OpenTelemetry__ServiceName: ${OTEL_SERVICE_NAME}
      OpenTelemetry__ServiceVersion: ${OTEL_SERVICE_VERSION}
      
      # Serilog
      Serilog__MinimumLevel__Default: ${SERILOG_MIN_LEVEL}
EOF

echo -e "${GREEN}? Created docker-compose.override.yml${NC}"
echo ""

# Print summary
echo "=========================================="
echo "  Setup Complete! ??"
echo "=========================================="
echo ""
echo "?? Summary:"
echo "  • .env file created with secure credentials"
echo "  • JWT keys generated in keys/"
echo "  • docker-compose.override.yml created"
echo ""
echo "?? Your credentials:"
echo "  • Admin API Key: ${ADMIN_API_KEY}"
echo "  • PostgreSQL Password: ${POSTGRES_PASSWORD}"
echo ""
echo -e "${YELLOW}??  IMPORTANT: These credentials are saved in .env${NC}"
echo ""
echo "?? Next steps:"
echo "  1. Start services:"
echo "     ${GREEN}docker-compose up -d${NC}"
echo ""
echo "  2. Verify everything is running:"
echo "     ${GREEN}docker-compose ps${NC}"
echo ""
echo "  3. Test the API:"
echo "     ${GREEN}curl http://localhost:8080/api/ping${NC}"
echo ""
echo "  4. Access services:"
echo "     • API Swagger: http://localhost:8080/swagger"
echo "     • MailHog: http://localhost:8025"
echo "     • Jaeger: http://localhost:16686"
echo ""
echo "?? For more information, see:"
echo "  • COMPLETE_SETUP_GUIDE.md"
echo "  • LOCAL_DEVELOPMENT.md"
echo ""
echo "=========================================="
