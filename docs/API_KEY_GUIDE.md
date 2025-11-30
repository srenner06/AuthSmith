# API Key Configuration Guide

**Simplified and clarified API key configuration**

---

## ?? API Key Types

AuthSmith uses two types of API keys:

### **1. Admin API Keys**
- **Purpose**: Full administrative access to manage applications, users, roles, and permissions
- **Configuration**: `ApiKeys:Admin` (list)
- **Access Level**: `Admin`
- **Used for**: Backend administration, CI/CD, admin tools

### **2. Application API Keys**
- **Purpose**: Application-level access for registering users and managing within an application
- **Storage**: Database (per application)
- **Access Level**: `App`
- **Used for**: Frontend applications, mobile apps, third-party integrations

---

## ?? Configuration

### **appsettings.json**

```json
{
  "ApiKeys": {
    "Admin": [
      "your-admin-key-1",
      "your-admin-key-2"
    ]
  }
}
```

### **Environment Variables (Docker)**

```yaml
environment:
  # Single admin key
  ApiKeys__Admin__0: "admin-key-here"
  
  # Multiple admin keys (optional)
  ApiKeys__Admin__0: "admin-key-1"
  ApiKeys__Admin__1: "admin-key-2"
  ApiKeys__Admin__2: "backup-key"
```

### **Command Line**

```bash
dotnet run --ApiKeys:Admin:0="your-admin-key"
```

---

## ?? When to Use Each Type

### **Admin Keys - Use When:**
- ? Creating new applications
- ? Managing users across applications
- ? Creating roles and permissions
- ? Viewing audit logs
- ? Administrative automation/scripts
- ? CI/CD pipelines

### **Application Keys - Use When:**
- ? Registering users in your frontend
- ? Third-party integrations
- ? Mobile applications
- ? Microservices calling AuthSmith

---

## ?? Examples

### **1. Initial Setup with Admin Key**

```bash
# Create an application using admin key
curl -X POST http://localhost:8080/api/v1/apps \
  -H "X-API-Key: your-admin-key" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "My Web App",
    "key": "mywebapp"
  }'

# Response includes the application's API key
{
  "id": "...",
  "name": "My Web App",
  "key": "mywebapp",
  "apiKey": "app-specific-key-abc123"  # Use this in your frontend
}
```

### **2. Register User with Application Key**

```bash
# Frontend uses the application's API key
curl -X POST http://localhost:8080/api/v1/auth/register/mywebapp \
  -H "X-API-Key: app-specific-key-abc123" \
  -H "Content-Type: application/json" \
  -d '{
    "username": "john.doe",
    "email": "john@example.com",
    "password": "Secure123!"
  }'
```

### **3. Multiple Admin Keys (Team Scenario)**

```json
{
  "ApiKeys": {
    "Admin": [
      "alice-admin-key-2024",     // Alice's key
      "bob-admin-key-2024",       // Bob's key
      "ci-cd-pipeline-key-2024"   // CI/CD key
    ]
  }
}
```

**Benefits:**
- ? Individual accountability
- ? Easy to revoke one key without affecting others
- ? Audit who made which changes

---

## ?? Security Best Practices

### **Admin Keys**

1. **Use Strong Keys**
   ```bash
   # Generate secure random key
   openssl rand -base64 32
   ```

2. **Store Securely**
   - ? Don't commit to git
   - ? Use environment variables
   - ? Use secrets manager (AWS Secrets Manager, Azure Key Vault, etc.)

3. **Rotate Regularly**
   ```bash
   # Add new key
   ApiKeys__Admin__0: "new-key"
   
   # Remove old key after updating clients
   # ApiKeys__Admin__1: "old-key"  # commented out
   ```

4. **Limit Distribution**
   - Only share with trusted administrators
   - Use separate keys for different purposes (manual admin vs CI/CD)

### **Application Keys**

1. **Generate Unique Keys**
   - Each application gets its own key
   - Never reuse keys across applications

2. **Rotate When Compromised**
   ```bash
   # Generate new key for application
   curl -X POST http://localhost:8080/api/v1/apps/{id}/api-key \
     -H "X-API-Key: your-admin-key"
   ```

3. **Store in Application Config**
   ```csharp
   // appsettings.json (frontend)
   {
     "AuthSmith": {
       "BaseUrl": "https://auth.example.com",
       "ApiKey": "app-specific-key"  // From environment variable in production
     }
   }
   ```

---

## ?? Production Setup

### **Step 1: Generate Admin Keys**

```bash
# Generate 2-3 strong admin keys
openssl rand -base64 32 > admin-key-1.txt
openssl rand -base64 32 > admin-key-2.txt
```

### **Step 2: Configure in Secrets Manager**

**AWS Secrets Manager:**
```bash
aws secretsmanager create-secret \
  --name authsmith/admin-keys \
  --secret-string '["key1","key2"]'
```

**Azure Key Vault:**
```bash
az keyvault secret set \
  --vault-name mykeyvault \
  --name authsmith-admin-keys \
  --value '["key1","key2"]'
```

### **Step 3: Reference in Deployment**

**Docker Compose:**
```yaml
environment:
  ApiKeys__Admin__0: ${ADMIN_KEY_1}
  ApiKeys__Admin__1: ${ADMIN_KEY_2}
```

**Kubernetes:**
```yaml
env:
  - name: ApiKeys__Admin__0
    valueFrom:
      secretKeyRef:
        name: authsmith-secrets
        key: admin-key-1
```

---

## ?? Validation

### **Admin Key**

```bash
# Test admin key
curl -H "X-API-Key: your-admin-key" \
  http://localhost:8080/api/v1/apps

# Should return list of applications
```

### **Application Key**

```bash
# Test application key
curl -X POST http://localhost:8080/api/v1/auth/register/myapp \
  -H "X-API-Key: app-key" \
  -H "Content-Type: application/json" \
  -d '{"username":"test","email":"test@example.com","password":"Test123!"}'

# Should register user successfully
```

---

## ?? Access Level Comparison

| Action | Admin Key | Application Key |
|--------|-----------|-----------------|
| Create application | ? | ? |
| List all applications | ? | ? |
| Generate app API key | ? | ? |
| Register user in app | ? | ? (own app only) |
| List users in app | ? | ? |
| Create roles | ? | ? |
| Assign permissions | ? | ? |
| View audit logs | ? | ? |
| Check permissions | ? | ? |

---

## ?? Troubleshooting

### **"Unauthorized" with Admin Key**

1. **Check key is in configuration:**
   ```bash
   # Print config (be careful in production!)
   curl http://localhost:8080/api/ping
   ```

2. **Verify header format:**
   ```bash
   # Correct
   -H "X-API-Key: your-key"
   
   # Incorrect
   -H "Authorization: Bearer your-key"  # Wrong!
   ```

3. **Check for whitespace:**
   ```bash
   # Remove any whitespace
   ADMIN_KEY=$(echo "your-key" | tr -d '[:space:]')
   ```

### **Application Key Not Working**

1. **Verify application is active:**
   ```bash
   curl -H "X-API-Key: admin-key" \
     http://localhost:8080/api/v1/apps
   
   # Check "isActive": true
   ```

2. **Regenerate if compromised:**
   ```bash
   curl -X POST http://localhost:8080/api/v1/apps/{id}/api-key \
     -H "X-API-Key: admin-key"
   ```

---

## ? Migration from Old Configuration

**If you had `Bootstrap` key before:**

### **Old (v1):**
```json
{
  "ApiKeys": {
    "Admin": ["admin-key-1"],
    "Bootstrap": "bootstrap-key"
  }
}
```

### **New (v2):**
```json
{
  "ApiKeys": {
    "Admin": [
      "admin-key-1",
      "bootstrap-key"  // Just add it to the list
    ]
  }
}
```

**No breaking changes** - both keys still work, now just in a simpler configuration!

---

## ?? SDK Usage

```csharp
using AuthSmith.Sdk;

// Admin client
var httpClient = AuthSmithClientFactory.CreateHttpClient(
    baseAddress: "https://auth.example.com",
    apiKey: "your-admin-key"
);

var appsClient = AuthSmithClientFactory.CreateApplicationsClient(httpClient);

// Create application
var app = await appsClient.CreateApplicationAsync(new CreateApplicationRequestDto
{
    Name = "My App",
    Key = "myapp"
});

// Application client (for frontend)
var appHttpClient = AuthSmithClientFactory.CreateHttpClient(
    baseAddress: "https://auth.example.com",
    apiKey: app.ApiKey  // Use application's key
);

var authClient = AuthSmithClientFactory.CreateAuthClient(appHttpClient);

// Register user
await authClient.RegisterAsync("myapp", new RegisterRequestDto { ... });
```

---

**API key configuration is now simplified and clear!** ?
