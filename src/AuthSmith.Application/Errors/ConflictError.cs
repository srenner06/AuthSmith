namespace AuthSmith.Application.Errors;

/// <summary>
/// Error for duplicate/conflict scenarios (e.g., "already exists").
/// </summary>
public sealed class ConflictError
{
    public string Message { get; }

    public ConflictError(string message)
    {
        Message = message;
    }
}

