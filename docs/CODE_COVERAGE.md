# Code Coverage Report

[![codecov](https://codecov.io/github/srenner06/AuthSmith/graph/badge.svg?token=YCX849VTWI)](https://codecov.io/github/srenner06/AuthSmith)

AuthSmith maintains comprehensive test coverage across all layers of the application to ensure code quality, reliability, and maintainability.

---

## ?? Overall Coverage

[![codecov](https://codecov.io/github/srenner06/AuthSmith/graphs/sunburst.svg?token=YCX849VTWI)](https://codecov.io/github/srenner06/AuthSmith)

**Latest Coverage:** [View on Codecov](https://codecov.io/github/srenner06/AuthSmith)

---

## ?? Coverage by Component

### Icicle Graph (Hierarchical View)

![Icicle Graph](https://codecov.io/github/srenner06/AuthSmith/graphs/icicle.svg?token=YCX849VTWI)

### Grid View (Detailed Breakdown)

![Grid](https://codecov.io/github/srenner06/AuthSmith/graphs/tree.svg?token=YCX849VTWI)

---

## ?? Coverage Goals

| Layer | Target | Status |
|-------|--------|--------|
| **Domain** | 95%+ | Domain logic is critical and should have near-complete coverage |
| **Application** | 85%+ | Business logic requires thorough testing |
| **Infrastructure** | 70%+ | External integrations may have limited testability |
| **API** | 75%+ | Integration tests cover critical endpoints |

---

## ?? Test Structure

### Test Projects

```
tests/
??? AuthSmith.Domain.Tests/          # Domain entity and logic tests
??? AuthSmith.Application.Tests/     # Service and business logic tests
??? AuthSmith.Api.Tests/             # API integration tests
```

### Testing Strategy

**Unit Tests:**
- Domain entities and value objects
- Application services and validators
- Infrastructure services (with mocking)

**Integration Tests:**
- API endpoints (E2E)
- Database operations
- Authentication and authorization flows

**Test Exclusions:**
- Program.cs (entry point)
- Auto-generated code (migrations, designers)
- Infrastructure configuration classes
- DTOs and simple data structures

---

## ?? Viewing Coverage Locally

### Generate Coverage Report

```bash
# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage"

# Generate HTML report (requires reportgenerator)
dotnet tool install -g dotnet-reportgenerator-globaltool

reportgenerator \
  -reports:"**/coverage.cobertura.xml" \
  -targetdir:"coverage-report" \
  -reporttypes:"Html"

# Open report
open coverage-report/index.html  # macOS
start coverage-report/index.html # Windows
```

### View in IDE

**Visual Studio:**
1. Run tests with code coverage
2. View > Other Windows > Code Coverage Results

**VS Code:**
1. Install "Coverage Gutters" extension
2. Run tests with coverage
3. Click "Watch" in status bar

**Rider:**
1. Run > Cover All Tests
2. View coverage in Coverage window

---

## ?? Coverage Configuration

Coverage is configured in `.github/codecov.yml`:

```yaml
coverage:
  status:
    project:
      default:
        target: 75%        # Overall project target
        threshold: 2%      # Allow 2% drop before failing
    patch:
      default:
        target: 80%        # New code should have higher coverage

ignore:
  - "**/Program.cs"
  - "**/Migrations/**"
  - "**/*.Designer.cs"
  - "**/Contracts/**"
```

---

## ?? Continuous Integration

Coverage is automatically:
- ? **Measured** on every push and pull request
- ? **Reported** to Codecov for analysis
- ? **Tracked** over time to monitor trends
- ? **Commented** on pull requests with changes

**CI/CD Pipeline:** [GitHub Actions](../.github/workflows/publish.yml)

---

## ?? Resources

- **Codecov Dashboard:** https://codecov.io/github/srenner06/AuthSmith
- **GitHub Actions:** https://github.com/srenner06/AuthSmith/actions
- **Test Documentation:** [CONTRIBUTING.md](../CONTRIBUTING.md#testing)
- **Architecture:** [ARCHITECTURE.md](ARCHITECTURE.md)

---

## ?? Improving Coverage

### Priority Areas

1. **Critical Business Logic** - Authentication, authorization, token generation
2. **Data Integrity** - Entity validation, database operations
3. **Security Features** - API key validation, password hashing, lockout logic
4. **Error Handling** - Exception scenarios, validation failures

### Contributing Tests

See [CONTRIBUTING.md](../CONTRIBUTING.md) for guidelines on:
- Writing effective unit tests
- Creating integration tests
- Mocking external dependencies
- Test naming conventions

---

**Last Updated:** Auto-generated on each commit  
**Maintained By:** [Codecov](https://codecov.io/)
