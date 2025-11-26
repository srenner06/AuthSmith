namespace AuthSmith.Domain.Errors;

/// <summary>
/// Error for authentication/authorization failures.
/// </summary>
public sealed class UnauthorizedError
{
    public string Message { get; }

    public UnauthorizedError(string message)
    {
        Message = message;
    }
}

