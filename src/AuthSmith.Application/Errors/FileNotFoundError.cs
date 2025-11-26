namespace AuthSmith.Application.Errors;

/// <summary>
/// Error for missing file scenarios (e.g., JWT keys).
/// </summary>
public sealed class FileNotFoundError
{
    public string FilePath { get; }
    public string Message { get; }

    public FileNotFoundError(string filePath, string message)
    {
        FilePath = filePath;
        Message = message;
    }
}

