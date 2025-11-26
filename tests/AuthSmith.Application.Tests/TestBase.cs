using Microsoft.Extensions.Logging;
using Moq;

namespace AuthSmith.Application.Tests;

/// <summary>
/// Base class for unit tests providing common utilities and mock factories.
/// </summary>
public abstract class TestBase
{
    protected static Mock<ILogger<T>> CreateLoggerMock<T>() => new();

    protected static T CreateMock<T>() where T : class => new Mock<T>().Object;
}

