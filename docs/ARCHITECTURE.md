# AuthSmith Architecture

This document describes the architecture, design decisions, and layer responsibilities of the AuthSmith authentication and authorization service.

## Overview

AuthSmith follows **Clean Architecture** principles, organizing code into layers with clear dependencies and responsibilities. The architecture promotes maintainability, testability, and separation of concerns.

## Architecture Layers

```
┌─────────────────────────────────────────────────────────┐
│                    AuthSmith.Api                        │
│  HTTP Layer - Controllers, Middleware, Filters         │
└────────────────────┬────────────────────────────────────┘
                     │ Depends on
┌────────────────────▼────────────────────────────────────┐
│              AuthSmith.Application                      │
│  Business Logic - Services, Validators, Use Cases      │
└────────────────────┬────────────────────────────────────┘
                     │ Depends on
┌────────────────────▼────────────────────────────────────┐
│                AuthSmith.Domain                          │
│  Core Domain - Entities, Interfaces, Errors            │
└────────────────────┬────────────────────────────────────┘
                     │
┌────────────────────▼────────────────────────────────────┐
│            AuthSmith.Infrastructure                       │
│  Technical Details - EF Core, External Services        │
└─────────────────────────────────────────────────────────┘
```

## Layer Responsibilities

### AuthSmith.Api

**Purpose**: HTTP API layer that handles web requests and responses.

**Responsibilities**:
- HTTP request/response handling
- Authentication and authorization middleware
- Input validation (via filters)
- Error formatting (ProblemDetails, RFC 7807)
- API versioning
- OpenAPI/Swagger documentation

**Key Components**:
- `Controllers/`: REST API endpoints
- `Authentication/`: API key authentication handlers
- `Authorization/`: Authorization attributes and policies
- `Middleware/`: Request logging, error handling
- `Extensions/`: Helper extensions (e.g., OneOf to ActionResult)

**Dependencies**: Application layer only

**Design Decisions**:
- Controllers are thin - delegate to application services
- Use `OneOf<T, Error1, Error2>` for type-safe error handling
- Middleware handles cross-cutting concerns
- No business logic in controllers

### AuthSmith.Application

**Purpose**: Business logic and use case orchestration.

**Responsibilities**:
- Implement business rules and use cases
- Coordinate domain entities and infrastructure services
- Input validation (FluentValidation)
- Transaction management
- Service orchestration

**Key Components**:
- `Services/`: Application services implementing use cases
- `Validators/`: FluentValidation validators for DTOs

**Dependencies**: Domain layer only

**Design Decisions**:
- Services are stateless
- Use `OneOf` for operation results (no exceptions for business errors)
- Services don't depend on infrastructure directly (via interfaces)
- Transaction boundaries at service method level

**Example Service Pattern**:

```csharp
public interface IAuthService
{
    Task<OneOf<AuthResultDto, NotFoundError, InvalidOperationError>> RegisterAsync(
        string appKey, 
        RegisterRequestDto request, 
        CancellationToken cancellationToken = default);
}
```

### AuthSmith.Domain

**Purpose**: Core business entities and domain logic.

**Responsibilities**:
- Define domain entities
- Define domain interfaces
- Define domain errors
- Business invariants and validation

**Key Components**:
- `Entities/`: Domain entities (User, Application, Role, Permission, etc.)
- `Interfaces/`: Domain interfaces (ICreated, IUpdated)
- `Errors/`: Domain error types (NotFoundError, ConflictError, etc.)
- `Enums/`: Domain enumerations

**Dependencies**: None (pure domain)

**Design Decisions**:
- Entities are POCOs (Plain Old C# Objects)
- No infrastructure dependencies
- Errors are value objects
- Interfaces for cross-cutting concerns (audit fields)

**Entity Example**:

```csharp
public class User : BaseEntity
{
    public string UserName { get; set; } = string.Empty;
    public string NormalizedUserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    
    // Navigation properties
    public ICollection<UserRole> UserRoles { get; set; } = [];
}
```

### AuthSmith.Infrastructure

**Purpose**: Technical implementations and external integrations.

**Responsibilities**:
- Data persistence (EF Core, PostgreSQL)
- External service implementations
- Caching (in-memory, Redis)
- Security (password hashing, JWT generation)
- Configuration management

**Key Components**:
- `Services/`: Infrastructure service implementations
- `Configuration/`: Configuration classes
- `EntityConfigurations/`: EF Core entity configurations
- `Migrations/`: Database migrations

**Dependencies**: Domain and Application layers

**Design Decisions**:
- Implements interfaces defined in Domain/Application
- EF Core for data access
- Repository pattern not used (DbContext is sufficient)
- Services are registered via dependency injection

## Design Patterns

### Result Pattern (OneOf)

Instead of throwing exceptions for business errors, we use `OneOf<T, Error1, Error2>` to represent operation results:

```csharp
Task<OneOf<UserDto, NotFoundError>> GetByIdAsync(Guid id);
```

**Benefits**:
- Type-safe error handling
- Explicit error types in method signatures
- No hidden exception paths
- Better API documentation

### Dependency Injection

All dependencies are injected via constructor injection:

```csharp
public class AuthService : IAuthService
{
    private readonly AuthSmithDbContext _dbContext;
    private readonly IPasswordHasher _passwordHasher;
    
    public AuthService(
        AuthSmithDbContext dbContext,
        IPasswordHasher passwordHasher)
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
    }
}
```

### Service Registration

Services are registered in extension methods:

- `ApplicationExtensions.AddApplication()`: Registers application services
- `InfrastructureExtensions.AddInfrastructure()`: Registers infrastructure services

## Error Handling Strategy

### Domain Errors

Domain errors are value objects representing business rule violations:

- `NotFoundError`: Resource not found
- `ConflictError`: Duplicate/conflict (e.g., already exists)
- `UnauthorizedError`: Authentication/authorization failure
- `InvalidOperationError`: Business rule violation
- `ValidationError`: Input validation failure

### Error Flow

1. **Domain/Application**: Returns `OneOf<T, Error>` from services
2. **API Extensions**: Convert `OneOf` to `ActionResult` via `ToActionResult()`
3. **Middleware**: Catches unhandled exceptions and formats as ProblemDetails

### Error Response Format

All errors follow RFC 7807 ProblemDetails:

```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.4",
  "title": "Not Found",
  "status": 404,
  "detail": "User not found.",
  "instance": "/api/v1/users/123"
}
```

## Data Access Strategy

### Entity Framework Core

- **DbContext**: `AuthSmithDbContext` manages database connections
- **Migrations**: Code-first migrations for schema management
- **Change Tracking**: Automatic audit field updates (CreatedAt, UpdatedAt)

### Query Patterns

- Use `AsNoTracking()` for read-only queries
- Use transactions for multi-step operations
- Optimize queries to avoid N+1 problems
- Use projections for DTOs when possible

### Example Transaction Usage

```csharp
await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
try
{
    // Multiple operations
    await _dbContext.SaveChangesAsync(cancellationToken);
    await transaction.CommitAsync(cancellationToken);
}
catch
{
    await transaction.RollbackAsync(cancellationToken);
    throw;
}
```

## Security Architecture

### Authentication

- **API Key Authentication**: For service-to-service communication
- **JWT Tokens**: For user authentication
- **Password Hashing**: Argon2id algorithm

### Authorization

- **Role-Based**: Users have roles, roles have permissions
- **Permission-Based**: Direct permission assignments (rare)
- **Caching**: Permission checks are cached for performance

### Token Management

- **Access Tokens**: Short-lived (15 minutes), signed with RSA/ECDSA
- **Refresh Tokens**: Long-lived, stored server-side, revocable
- **Token Claims**: User ID, roles, permissions, application context

## Caching Strategy

### Permission Caching

Permissions are cached to avoid repeated database queries:

- **In-Memory Cache**: Default, per-instance
- **Redis Cache**: Distributed, for multi-instance deployments
- **Cache Invalidation**: On role/permission/user changes

### Cache Keys

- User permissions: `permissions:user:{userId}:app:{appId}`
- Application permissions: `permissions:app:{appId}`

## Testing Strategy

### Test Layers

- **Unit Tests**: Test services in isolation with mocks
- **Integration Tests**: Test API endpoints with in-memory database
- **Domain Tests**: Test domain logic and business rules

### Test Organization

Tests mirror source structure:
```
tests/
├── AuthSmith.Api.Tests/
├── AuthSmith.Application.Tests/
└── AuthSmith.Domain.Tests/
```

### Test Patterns

- Use in-memory database for integration tests
- Mock external dependencies
- Follow AAA pattern (Arrange, Act, Assert)
- Use test data builders for complex setups

## Configuration Management

### Configuration Sources

1. `appsettings.json`: Default configuration
2. `appsettings.{Environment}.json`: Environment-specific
3. Environment variables: Override for deployment

### Configuration Classes

Strongly-typed configuration classes:

- `DatabaseConfiguration`
- `JwtConfiguration`
- `ApiKeysConfiguration`
- `RedisConfiguration`

### Validation

Configuration is validated on startup. Missing required values cause startup failure.

## API Versioning

- **Path-based**: `/api/v1/...`
- **Header-based**: `X-Version: 1.0`
- **Query-based**: `?version=1.0`

Versioning handled by `Asp.Versioning.Mvc` package.

## Future Considerations

### Potential Enhancements

- **Event Sourcing**: For audit trails
- **CQRS**: Separate read/write models
- **Domain Events**: For cross-aggregate communication
- **OIDC/OAuth2**: Full identity provider support
- **Multi-tenancy**: Organization/tenant support

### Scalability

- Horizontal scaling: Stateless services, shared database
- Caching: Redis for distributed caching
- Database: Connection pooling, read replicas
- Load balancing: Round-robin, sticky sessions not required

## Conclusion

This architecture provides:

- **Maintainability**: Clear separation of concerns
- **Testability**: Dependencies are injectable and mockable
- **Scalability**: Stateless services, caching support
- **Security**: Layered security, proper authentication/authorization
- **Flexibility**: Easy to extend and modify

For questions or suggestions, please open an issue or discussion.

