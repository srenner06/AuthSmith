namespace AuthSmith.Domain.Errors;

/// <summary>
/// Error for business rule violations.
/// </summary>
public sealed class InvalidOperationError
{
    public string Message { get; }

    public InvalidOperationError(string message)
    {
        Message = message;
    }
}

