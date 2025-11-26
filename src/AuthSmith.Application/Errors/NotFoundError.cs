namespace AuthSmith.Application.Errors;

/// <summary>
/// Shared error instance for "not found" scenarios.
/// </summary>
public sealed class NotFoundError
{
    public static NotFoundError Instance { get; } = new();

    private NotFoundError()
    {
    }

    public string? Message { get; init; }
}

