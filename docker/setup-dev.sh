#!/bin/bash
set -euo pipefail

# AuthSmith Local Development Setup Script
# =============================================================================
# This script sets up everything you need to run AuthSmith locally with Docker.
#
# WHAT IT DOES:
#   1. Reads .env.template as source of truth
#   2. Merges with existing .env (if exists), preserving user values
#   3. Generates secure values for placeholders (<GENERATE_...>)
#   4. Generates JWT RSA key pair for token signing
#   5. Creates necessary directories (logs, keys)
#
# IMPORTANT - CONFIGURATION PATTERN:
#   - .env.template defines all required variables
#   - Script only updates missing or placeholder values
#   - Your existing .env customizations are preserved
#   - To reset: delete .env and run this script again
#
# =============================================================================

echo "=========================================="
echo "  AuthSmith Local Development Setup"
echo "=========================================="
echo ""

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
GRAY='\033[0;37m'
NC='\033[0m' # No Color

# Change to script directory
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$SCRIPT_DIR"

# Helper function to generate secure random string
generate_password() {
    openssl rand -base64 32 | tr -d "=+/" | cut -c1-32
}

# Helper function to parse .env file into associative array
declare -A existing_env
declare -A template_env

parse_env_file() {
    local file="$1"
    local target_array="$2"
    
    if [ -f "$file" ]; then
        while IFS='=' read -r key value; do
            # Skip comments and empty lines
            if [[ ! "$key" =~ ^[[:space:]]*# ]] && [[ -n "$key" ]]; then
                key=$(echo "$key" | xargs)  # Trim whitespace
                value=$(echo "$value" | xargs)
                eval "${target_array}[\${key}]=\"\${value}\""
            fi
        done < "$file"
    fi
}

# Helper function to write .env file
write_env_file() {
    local template_file="$1"
    local output_file="$2"
    
    {
        echo "# AuthSmith Local Development Environment"
        echo "# Generated: $(date '+%Y-%m-%d %H:%M:%S')"
        echo "# Based on: .env.template"
        echo ""
        
        while IFS= read -r line; do
            # Preserve comments and empty lines
            if [[ "$line" =~ ^[[:space:]]*# ]] || [[ -z "$line" ]]; then
                echo "$line"
            # Process variable lines
            elif [[ "$line" =~ ^([^=]+)=(.*)$ ]]; then
                key="${BASH_REMATCH[1]}"
                key=$(echo "$key" | xargs)  # Trim whitespace
                
                # Use new value if available
                if [ -n "${new_values[$key]:-}" ]; then
                    echo "${key}=${new_values[$key]}"
                else
                    echo "$line"
                fi
            else
                echo "$line"
            fi
        done < "$template_file"
    } > "$output_file"
}

echo "?? Step 1: Processing environment configuration..."

TEMPLATE_FILE=".env.template"
ENV_FILE=".env"

if [ ! -f "$TEMPLATE_FILE" ]; then
    echo -e "${RED}? ERROR: .env.template not found!${NC}"
    echo -e "${YELLOW}Make sure you're running this script from the docker/ directory${NC}"
    exit 1
fi

# Parse existing .env if it exists
IS_UPDATE=false
if [ -f "$ENV_FILE" ]; then
    echo -e "${YELLOW}Found existing .env file - will preserve your customizations${NC}"
    parse_env_file "$ENV_FILE" "existing_env"
    IS_UPDATE=true
    
    # Backup existing .env
    BACKUP_FILE="${ENV_FILE}.backup.$(date '+%Y%m%d-%H%M%S')"
    cp "$ENV_FILE" "$BACKUP_FILE"
    echo -e "${GRAY}Backed up existing .env to: $BACKUP_FILE${NC}"
fi

# Parse template
parse_env_file "$TEMPLATE_FILE" "template_env"

# Build new values
declare -A new_values

for key in "${!template_env[@]}"; do
    template_value="${template_env[$key]}"
    
    # If existing .env has this key and it's not a placeholder, keep it
    if [ -n "${existing_env[$key]:-}" ]; then
        existing_value="${existing_env[$key]}"
        if [[ ! "$existing_value" =~ \<GENERATE_ ]]; then
            new_values[$key]="$existing_value"
            continue
        fi
    fi
    
    # Generate value for placeholders
    if [[ "$template_value" =~ \<GENERATE_SECURE_PASSWORD\> ]]; then
        new_values[$key]=$(generate_password)
        echo -e "${GRAY}Generated secure password for: $key${NC}"
    elif [[ "$template_value" =~ \<GENERATE_SECURE_API_KEY\> ]]; then
        new_values[$key]=$(generate_password)
        echo -e "${GRAY}Generated secure API key for: $key${NC}"
    else
        # Use template default
        new_values[$key]="$template_value"
    fi
done

# Write the merged .env file
write_env_file "$TEMPLATE_FILE" "$ENV_FILE"

if [ "$IS_UPDATE" = true ]; then
    echo -e "${GREEN}? Updated .env file (preserved your customizations)${NC}"
else
    echo -e "${GREEN}? Created .env file${NC}"
fi
echo ""

# Generate JWT keys
echo "?? Step 2: Generating JWT keys..."

KEYS_DIR="keys"
PRIVATE_KEY="$KEYS_DIR/jwt_private_key.pem"
PUBLIC_KEY="$KEYS_DIR/jwt_public_key.pem"

if [ -f "$PRIVATE_KEY" ] && [ -f "$PUBLIC_KEY" ]; then
    echo -e "${GRAY}JWT keys already exist - skipping generation${NC}"
else
    mkdir -p "$KEYS_DIR"
    
    # Generate RSA key pair
    echo -e "${GRAY}Generating RSA private key...${NC}"
    openssl genpkey -algorithm RSA -out "$PRIVATE_KEY" -pkeyopt rsa_keygen_bits:2048 2>/dev/null
    
    echo -e "${GRAY}Generating RSA public key...${NC}"
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

# Print summary
echo "=========================================="
echo -e "${GREEN}  Setup Complete!${NC}"
echo "=========================================="
echo ""

if [ "$IS_UPDATE" = true ]; then
    echo "?? Summary:"
    echo "  ? Updated .env with missing values"
    echo "  ? Preserved your existing customizations"
    echo "  ? JWT keys verified/generated"
    echo ""
else
    echo "?? Summary:"
    echo "  ? Created .env file with secure credentials"
    echo "  ? Generated secure passwords and API keys"
    echo "  ? JWT keys generated in keys/"
    echo ""
    echo -e "${YELLOW}?? Your generated credentials:${NC}"
    if [ -n "${new_values[ADMIN_API_KEY]:-}" ]; then
        echo "  • Admin API Key: ${new_values[ADMIN_API_KEY]}"
    fi
    if [ -n "${new_values[POSTGRES_PASSWORD]:-}" ]; then
        echo "  • PostgreSQL Password: ${new_values[POSTGRES_PASSWORD]}"
    fi
    echo ""
fi

echo -e "${YELLOW}??  CONFIGURATION:${NC}"
echo "  • Source of truth: .env.template"
echo "  • Your config: .env (edit this file to customize)"
echo "  • After changes: docker-compose restart api"
echo ""
echo "  To reset configuration:"
echo "    1. Delete .env"
echo "    2. Run this script again"
echo ""

echo "?? Next steps:"
echo "  1. Review/edit .env if needed:"
echo -e "     ${GREEN}nano .env${NC}"
echo ""
echo "  2. Start services:"
echo -e "     ${GREEN}docker-compose up -d${NC}"
echo ""
echo "  3. Verify everything is running:"
echo -e "     ${GREEN}docker-compose ps${NC}"
echo ""
echo "  4. Test the API:"
echo -e "     ${GREEN}curl http://localhost:8080/api/ping${NC}"
echo ""
echo "  5. Access services:"
echo "     • API Swagger: http://localhost:8080/swagger"
echo "     • MailHog: http://localhost:8025"
echo "     • .NET Aspire Dashboard: http://localhost:18888"
echo ""
echo "?? For more information, see:"
echo "  • QUICK_START.md"
echo "  • COMPLETE_SETUP_GUIDE.md"
echo ""
echo "=========================================="
