# Ping Endpoint - Version Information

**Date**: November 30, 2025  
**Status**: ? **COMPLETE**

---

## ?? Feature

Added `/api/ping` endpoint that returns build and version information.

---

## ?? Endpoint Details

### **GET /api/ping**

**Authentication**: Not required (anonymous access)

**Response:**
```json
{
  "versionTag": "v1.0.0",
  "buildNumber": "19",
  "buildTime": "2025-11-30T09:27:12Z",
  "commitHash": "2041286",
  "message": "Alive and ready to serve",
  "authenticated": false
}
```

---

## ?? How It Works

### **1. Version.cs (Placeholders)**

```csharp
public static class Version
{
    public const string BuildNumber = "__BUILD_NUMBER__";
    public const string BuildTime = "__BUILD_TIME__";
    public const string VersionTag = "__VERSION_TAG__";
    public const string CommitHash = "__COMMIT_HASH__";
}
```

**During development:** Shows placeholder values  
**In CI/CD:** Placeholders replaced with real values

### **2. PingController**

```csharp
[HttpGet("ping")]
public IActionResult Ping()
{
    var isAuthenticated = User.Identity?.IsAuthenticated ?? false;
    
    return Ok(new PingResponse
    {
        VersionTag = Version.VersionTag,
        BuildNumber = Version.BuildNumber,
        BuildTime = Version.BuildTime,
        CommitHash = Version.CommitHash,
        Message = "Alive and ready to serve",
        Authenticated = isAuthenticated
    });
}
```

### **3. GitHub Actions (Version Injection)**

```yaml
- name: Inject version info into API (Version.cs)
  run: |
    sed -i \
      -e "s|__BUILD_NUMBER__|$BUILD_NUMBER|g" \
      -e "s|__BUILD_TIME__|$BUILD_TIME|g" \
      -e "s|__VERSION_TAG__|$VERSION_TAG|g" \
      -e "s|__COMMIT_HASH__|$COMMIT_HASH|g" \
      src/AuthSmith.Api/Version.cs
```

---

## ?? Usage Examples

### **Anonymous Request**

```bash
curl http://localhost:8080/api/ping
```

**Response:**
```json
{
  "versionTag": "__VERSION_TAG__",
  "buildNumber": "__BUILD_NUMBER__",
  "buildTime": "__BUILD_TIME__",
  "commitHash": "__COMMIT_HASH__",
  "message": "Alive and ready to serve",
  "authenticated": false
}
```

### **Authenticated Request**

```bash
curl http://localhost:8080/api/ping \
  -H "X-API-Key: your-api-key"
```

**Response:**
```json
{
  "versionTag": "__VERSION_TAG__",
  "buildNumber": "__BUILD_NUMBER__",
  "buildTime": "__BUILD_TIME__",
  "commitHash": "__COMMIT_HASH__",
  "message": "Alive and ready to serve",
  "authenticated": true
}
```

### **Production (After CI/CD)**

```bash
curl https://api.authsmith.com/api/ping
```

**Response:**
```json
{
  "versionTag": "v1.0.0",
  "buildNumber": "42",
  "buildTime": "2025-11-30T10:15:30Z",
  "commitHash": "a1b2c3d",
  "message": "Alive and ready to serve",
  "authenticated": false
}
```

---

## ?? Use Cases

### **1. Health Checks**

Verify the API is alive and responding:
```bash
curl -f http://localhost:8080/api/ping || echo "API is down"
```

### **2. Version Verification**

Check which version is deployed:
```bash
curl -s http://localhost:8080/api/ping | jq -r .versionTag
# Output: v1.0.0
```

### **3. Monitoring**

Use in monitoring scripts:
```bash
VERSION=$(curl -s http://api/ping | jq -r .versionTag)
if [ "$VERSION" != "v1.0.0" ]; then
  echo "Unexpected version: $VERSION"
  exit 1
fi
```

### **4. Debug Information**

Quick way to get build info:
```bash
curl -s http://localhost:8080/api/ping | jq
```

### **5. Authentication Test**

Verify authentication is working:
```bash
# Without auth
curl -s http://localhost:8080/api/ping | jq .authenticated
# Output: false

# With auth
curl -s http://localhost:8080/api/ping \
  -H "X-API-Key: admin-key" | jq .authenticated
# Output: true
```

---

## ?? CI/CD Flow

### **1. Push Tag**
```bash
git tag v1.0.0
git push origin v1.0.0
```

### **2. GitHub Actions Triggers**
- Checks out code
- Derives version from tag
- Injects version into `Version.cs`
- Builds Docker image
- Pushes to registry

### **3. Version.cs After Injection**
```csharp
public static class Version
{
    public const string BuildNumber = "42";
    public const string BuildTime = "2025-11-30T10:15:30Z";
    public const string VersionTag = "v1.0.0";
    public const string CommitHash = "a1b2c3d";
}
```

### **4. Result**
API now returns real version information!

---

## ?? Response Fields

| Field | Type | Description | Example |
|-------|------|-------------|---------|
| `versionTag` | string | Git tag or manual version | `v1.0.0` |
| `buildNumber` | string | CI/CD build number | `42` |
| `buildTime` | string | Build timestamp (UTC, ISO 8601) | `2025-11-30T10:15:30Z` |
| `commitHash` | string | Git commit hash (short) | `a1b2c3d` |
| `message` | string | Status message | `Alive and ready to serve` |
| `authenticated` | boolean | Is request authenticated? | `true` / `false` |

---

## ?? Swagger Documentation

The endpoint appears in Swagger UI:

**GET /api/ping**
- **Summary**: Ping endpoint that returns build and version information
- **Responses**:
  - `200 OK` - Returns `PingResponse`
- **Security**: None (anonymous access)

---

## ?? Security Notes

### **Anonymous Access**
- ? Endpoint is public (no authentication required)
- ? Safe to expose - no sensitive information
- ? Useful for health checks and monitoring

### **What's Exposed**
- ? Version tag - public information
- ? Build number - public information
- ? Build time - public information
- ? Commit hash - public information
- ? Authentication status - user-specific

### **What's NOT Exposed**
- ? API keys
- ? Connection strings
- ? User data
- ? Internal configurations

---

## ?? Testing

### **Unit Test Example**
```csharp
[Test]
public void Ping_ReturnsVersionInformation()
{
    // Arrange
    var controller = new PingController();
    
    // Act
    var result = controller.Ping();
    
    // Assert
    var okResult = result as OkObjectResult;
    var response = okResult.Value as PingResponse;
    
    Assert.That(response.Message, Is.EqualTo("Alive and ready to serve"));
    Assert.That(response.VersionTag, Is.Not.Null);
}
```

### **Integration Test Example**
```csharp
[Test]
public async Task Ping_ReturnsOk()
{
    // Arrange
    var client = _factory.CreateClient();
    
    // Act
    var response = await client.GetAsync("/api/ping");
    
    // Assert
    response.EnsureSuccessStatusCode();
    var content = await response.Content.ReadAsStringAsync();
    var ping = JsonSerializer.Deserialize<PingResponse>(content);
    
    Assert.That(ping.Message, Is.EqualTo("Alive and ready to serve"));
}
```

---

## ?? Files Created

1. ? `src/AuthSmith.Api/Version.cs` - Version constants
2. ? `src/AuthSmith.Api/Controllers/PingController.cs` - Ping endpoint
3. ? Updated `.github/workflows/publish.yml` - Version injection

---

## ? Result

**You now have:**
- ? `/api/ping` endpoint
- ? Build version information
- ? Authentication status
- ? CI/CD integration
- ? Swagger documentation
- ? Health check capability

**Use it for:**
- Health monitoring
- Version verification
- Deployment validation
- Debug information
- Authentication testing

---

## ?? Try It

### **Local (Development)**
```bash
curl http://localhost:8080/api/ping | jq
```

### **Docker**
```bash
curl http://localhost:8080/api/ping | jq
```

### **Production (After Deploy)**
```bash
curl https://your-api-domain.com/api/ping | jq
```

---

**Ping endpoint is ready!** ??
