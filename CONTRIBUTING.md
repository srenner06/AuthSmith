# Contributing to AuthSmith

Thank you for your interest in contributing to AuthSmith! This document provides guidelines and instructions for contributing.

## Code of Conduct

- Be respectful and inclusive
- Focus on constructive feedback
- Help maintain a welcoming environment

## Getting Started

1. Fork the repository
2. Clone your fork: `git clone https://github.com/yourusername/AuthSmith.git`
3. Create a branch: `git checkout -b feature/your-feature-name`
4. Make your changes
5. Run tests: `dotnet test`
6. Commit your changes: `git commit -m "Add feature: description"`
7. Push to your fork: `git push origin feature/your-feature-name`
8. Open a Pull Request

## Development Setup

### Prerequisites

- .NET 10.0 SDK
- PostgreSQL 12+ (or use Docker)
- (Optional) Redis for testing caching features

### Local Development

1. Clone the repository
2. Restore dependencies: `dotnet restore`
3. Build the solution: `dotnet build`
4. Run tests: `dotnet test`
5. Start the API: `dotnet run --project src/AuthSmith.Api/AuthSmith.Api.csproj`

## Code Style Guidelines

### General Principles

- Follow C# coding conventions
- Use meaningful variable and method names
- Keep methods focused and small
- Prefer composition over inheritance
- Write self-documenting code

### Naming Conventions

- **Classes**: PascalCase (e.g., `UserService`, `AuthController`)
- **Methods**: PascalCase (e.g., `RegisterAsync`, `GetUserById`)
- **Variables**: camelCase (e.g., `userId`, `applicationKey`)
- **Constants**: PascalCase (e.g., `DefaultExpirationMinutes`)
- **Interfaces**: Prefix with `I` (e.g., `IAuthService`)

### Code Organization

- One class per file
- Group related functionality together
- Use regions sparingly, only for very large files
- Order members: fields, properties, constructors, methods

### Formatting

- Use 4 spaces for indentation
- Use `var` when the type is obvious from the right-hand side
- Prefer expression-bodied members for simple properties/methods
- Use trailing commas in multi-line initializers

### Example

```csharp
public class UserService : IUserService
{
    private readonly AuthSmithDbContext _dbContext;
    private readonly ILogger<UserService> _logger;

    public UserService(
        AuthSmithDbContext dbContext,
        ILogger<UserService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<OneOf<UserDto, NotFoundError>> GetByIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user == null)
            return new NotFoundError { Message = "User not found." };

        return MapToDto(user);
    }
}
```

## Testing Requirements

### Test Organization

- Tests should mirror the source structure
- Use descriptive test method names: `MethodName_Scenario_ExpectedResult`
- Follow AAA pattern: Arrange, Act, Assert

### Example Test

```csharp
[Test]
public async Task GetByIdAsync_ShouldReturnUser_WhenUserExists()
{
    // Arrange
    var dbContext = CreateDbContext();
    var user = TestDataBuilder.CreateUser(userName: "testuser");
    dbContext.Users.Add(user);
    await dbContext.SaveChangesAsync();

    var service = CreateService(dbContext);

    // Act
    var result = await service.GetByIdAsync(user.Id);

    // Assert
    await Assert.That(result.IsT0).IsTrue();
    var userDto = result.AsT0;
    await Assert.That(userDto.UserName).IsEqualTo("testuser");
}
```

### Test Coverage

- Aim for high coverage of business logic
- Focus on testing behavior, not implementation
- Include edge cases and error scenarios
- Integration tests for critical paths

### Running Tests

```bash
# Run all tests
dotnet test

# Run specific test project
dotnet test tests/AuthSmith.Application.Tests
```

### Running Tests with Coverage

Since TUnit test projects are executables, use `dotnet run` instead of `dotnet test` for coverage:

**PowerShell:**
```powershell
cd tests/AuthSmith.Application.Tests
dotnet run --configuration Release --coverage --coverage-output-format cobertura
cd ../AuthSmith.Api.Tests
dotnet run --configuration Release --coverage --coverage-output-format cobertura
```

**Bash:**
```bash
cd tests/AuthSmith.Application.Tests
dotnet run --configuration Release --coverage --coverage-output-format cobertura
cd ../AuthSmith.Api.Tests
dotnet run --configuration Release --coverage --coverage-output-format cobertura
```

Coverage files are generated in each test project's `bin/Release/net10.0/TestResults/` directory as `.cobertura.xml` files.

**Note**: AuthSmith uses TUnit, which includes built-in code coverage via `Microsoft.Testing.Extensions.CodeCoverage`. No additional packages needed!

## Pull Request Process

### Before Submitting

1. **Update Documentation**: Update README.md if you've changed setup or usage
2. **Add Tests**: Include tests for new features or bug fixes
3. **Run Tests**: Ensure all tests pass
4. **Check Linting**: Fix any code analysis warnings
5. **Update CHANGELOG**: Document your changes (if applicable)

### PR Description

Include:
- Description of changes
- Related issue numbers
- Testing performed
- Breaking changes (if any)

### Review Process

- All PRs require at least one approval
- Address review feedback promptly
- Keep PRs focused and reasonably sized
- Rebase on main if there are conflicts

## Commit Messages

Use clear, descriptive commit messages:

```
Add transaction support for user registration

- Wrap user creation and role assignment in database transaction
- Ensure atomicity of multi-step operations
- Add rollback handling for error cases
```

### Commit Message Format

```
<type>: <subject>

<body>

<footer>
```

Types: `feat`, `fix`, `docs`, `style`, `refactor`, `test`, `chore`

## Architecture Guidelines

### Layer Responsibilities

- **Api**: HTTP concerns only, no business logic
- **Application**: Business logic and use cases
- **Domain**: Core entities and business rules
- **Infrastructure**: Technical implementations

### Dependencies

- Dependencies should point inward (toward Domain)
- Api depends on Application
- Application depends on Domain
- Infrastructure depends on Domain and Application

### Error Handling

- Use `OneOf<T, Error1, Error2>` for operation results
- Return domain errors, not exceptions
- Let middleware handle exception-to-HTTP mapping

## Questions?

- Open an issue for bugs or feature requests
- Check existing issues and discussions
- Review the architecture documentation

Thank you for contributing to AuthSmith!

