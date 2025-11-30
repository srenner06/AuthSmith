# AuthSmith Local Development Setup Script (PowerShell)
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

Write-Host "=========================================="
Write-Host "  AuthSmith Local Development Setup"
Write-Host "=========================================="
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

# Helper function to generate secure random string
function Generate-SecurePassword {
    $bytes = & openssl rand -base64 32
    $password = $bytes -replace '[/+=]', 'X'
    return $password.Substring(0, [Math]::Min(32, $password.Length))
}

# Helper function to parse .env file into hashtable
function Parse-EnvFile {
    param([string]$FilePath)
    
    $env = @{}
    if (Test-Path $FilePath) {
        Get-Content $FilePath | ForEach-Object {
            $line = $_.Trim()
            # Skip comments and empty lines
            if ($line -and !$line.StartsWith('#')) {
                if ($line -match '^([^=]+)=(.*)$') {
                    $key = $matches[1].Trim()
                    $value = $matches[2].Trim()
                    $env[$key] = $value
                }
            }
        }
    }
    return $env
}

# Helper function to write .env file
function Write-EnvFile {
    param(
        [string]$TemplatePath,
        [hashtable]$Values,
        [string]$OutputPath
    )
    
    $output = @()
    $output += "# AuthSmith Local Development Environment"
    $output += "# Generated: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')"
    $output += "# Based on: .env.template"
    $output += ""
    
    Get-Content $TemplatePath | ForEach-Object {
        $line = $_
        
        # Preserve comments and empty lines
        if (!$line.Trim() -or $line.Trim().StartsWith('#')) {
            $output += $line
        }
        # Process variable lines
        elseif ($line -match '^([^=]+)=(.*)$') {
            $key = $matches[1].Trim()
            $value = $matches[2].Trim()
            
            # Use existing value if available, otherwise use new value
            if ($Values.ContainsKey($key)) {
                $output += "$key=$($Values[$key])"
            } else {
                $output += $line
            }
        }
        else {
            $output += $line
        }
    }
    
    $output | Out-File -FilePath $OutputPath -Encoding UTF8
}

Write-Host "[Step 1] Processing environment configuration..." -ForegroundColor Yellow

$templateFile = ".env.template"
$envFile = ".env"

if (!(Test-Path $templateFile)) {
    Write-Host "[ERROR] .env.template not found!" -ForegroundColor Red
    Write-Host "Make sure you're running this script from the docker/ directory" -ForegroundColor Yellow
    Read-Host "Press Enter to exit"
    exit 1
}

# Parse existing .env if it exists
$existingEnv = @{}
$isUpdate = $false
if (Test-Path $envFile) {
    Write-Host "Found existing .env file - will preserve your customizations" -ForegroundColor Yellow
    $existingEnv = Parse-EnvFile $envFile
    $isUpdate = $true
    
    # Backup existing .env
    $backupFile = "$envFile.backup.$(Get-Date -Format 'yyyyMMdd-HHmmss')"
    Copy-Item $envFile $backupFile -Force
    Write-Host "Backed up existing .env to: $backupFile" -ForegroundColor Gray
}

# Parse template to find placeholders
$templateEnv = Parse-EnvFile $templateFile
$newValues = @{}

foreach ($key in $templateEnv.Keys) {
    $templateValue = $templateEnv[$key]
    
    # If existing .env has this key and it's not a placeholder, keep it
    if ($existingEnv.ContainsKey($key)) {
        $existingValue = $existingEnv[$key]
        if ($existingValue -notmatch '<GENERATE_') {
            $newValues[$key] = $existingValue
            continue
        }
    }
    
    # Generate value for placeholders
    if ($templateValue -match '<GENERATE_SECURE_PASSWORD>') {
        $newValues[$key] = Generate-SecurePassword
        Write-Host "Generated secure password for: $key" -ForegroundColor Gray
    }
    elseif ($templateValue -match '<GENERATE_SECURE_API_KEY>') {
        $newValues[$key] = Generate-SecurePassword
        Write-Host "Generated secure API key for: $key" -ForegroundColor Gray
    }
    else {
        # Use template default
        $newValues[$key] = $templateValue
    }
}

# Write the merged .env file
Write-EnvFile -TemplatePath $templateFile -Values $newValues -OutputPath $envFile

if ($isUpdate) {
    Write-Host "[OK] Updated .env file (preserved your customizations)" -ForegroundColor Green
} else {
    Write-Host "[OK] Created .env file" -ForegroundColor Green
}
Write-Host ""

# Generate JWT keys
Write-Host "[Step 2] Generating JWT keys..." -ForegroundColor Yellow

$keysDir = "keys"
$privateKey = Join-Path $keysDir "jwt_private_key.pem"
$publicKey = Join-Path $keysDir "jwt_public_key.pem"

if ((Test-Path $privateKey) -and (Test-Path $publicKey)) {
    Write-Host "JWT keys already exist - skipping generation" -ForegroundColor Gray
} else {
    # Ensure keys directory exists
    if (-not (Test-Path $keysDir)) {
        New-Item -ItemType Directory -Path $keysDir -Force | Out-Null
    }
    
    # Generate RSA private key
    Write-Host "Generating RSA private key..." -ForegroundColor Gray
    & openssl genpkey -algorithm RSA -out $privateKey -pkeyopt rsa_keygen_bits:2048 2>$null
    if ($LASTEXITCODE -ne 0) {
        Write-Host "[ERROR] Failed to generate private key" -ForegroundColor Red
        Read-Host "Press Enter to exit"
        exit 1
    }
    
    # Extract public key from private key
    Write-Host "Generating RSA public key..." -ForegroundColor Gray
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

# Print summary
Write-Host "=========================================="
Write-Host "  Setup Complete!" -ForegroundColor Green
Write-Host "=========================================="
Write-Host ""

if ($isUpdate) {
    Write-Host "Summary:" -ForegroundColor White
    Write-Host "  * Updated .env with missing values"
    Write-Host "  * Preserved your existing customizations"
    Write-Host "  * JWT keys verified/generated"
    Write-Host ""
} else {
    Write-Host "Summary:" -ForegroundColor White
    Write-Host "  * Created .env file with secure credentials"
    Write-Host "  * Generated secure passwords and API keys"
    Write-Host "  * JWT keys generated in keys/"
    Write-Host ""
    Write-Host "Your generated credentials:" -ForegroundColor Yellow
    if ($newValues.ContainsKey('ADMIN_API_KEY')) {
        Write-Host "  * Admin API Key: $($newValues['ADMIN_API_KEY'])"
    }
    if ($newValues.ContainsKey('POSTGRES_PASSWORD')) {
        Write-Host "  * PostgreSQL Password: $($newValues['POSTGRES_PASSWORD'])"
    }
    Write-Host ""
}

Write-Host "CONFIGURATION:" -ForegroundColor Yellow
Write-Host "  * Source of truth: .env.template" -ForegroundColor White
Write-Host "  * Your config: .env (edit this file to customize)"
Write-Host "  * After changes: docker-compose restart api"
Write-Host ""
Write-Host "  To reset configuration:"
Write-Host "    1. Delete .env"
Write-Host "    2. Run this script again"
Write-Host ""

Write-Host "Next steps:" -ForegroundColor White
Write-Host "  1. Review/edit .env if needed:"
Write-Host "     notepad .env" -ForegroundColor Cyan
Write-Host ""
Write-Host "  2. Start services:"
Write-Host "     docker-compose up -d" -ForegroundColor Cyan
Write-Host ""
Write-Host "  3. Verify everything is running:"
Write-Host "     docker-compose ps" -ForegroundColor Cyan
Write-Host ""
Write-Host "  4. Test the API:"
Write-Host "     curl http://localhost:8080/api/ping" -ForegroundColor Cyan
Write-Host ""
Write-Host "  5. Access services:"
Write-Host "     * API Swagger: http://localhost:8080/swagger"
Write-Host "     * MailHog: http://localhost:8025"
Write-Host "     * Jaeger: http://localhost:16686"
Write-Host ""
Write-Host "For more information, see:"
Write-Host "  * QUICK_START.md"
Write-Host "  * COMPLETE_SETUP_GUIDE.md"
Write-Host ""
Write-Host "=========================================="
Write-Host ""
Read-Host "Press Enter to exit"
