namespace AuthSmith.Application.Errors;

/// <summary>
/// Marker type for successful void operations.
/// </summary>
public sealed class Success
{
    public static Success Instance { get; } = new();

    private Success()
    {
    }
}

