namespace AuthSmith.Domain.Errors;

/// <summary>
/// Shared error instance for "not found" scenarios.
/// </summary>
public sealed class NotFoundError
{
    public static NotFoundError Instance { get; } = new();

    public NotFoundError()
    {
    }

    public NotFoundError(string? message)
    {
        Message = message;
    }

    public string? Message { get; set; }
}


