namespace AuthSmith.Sdk.Exceptions;

/// <summary>
/// Base exception for AuthSmith SDK errors.
/// </summary>
public class AuthSmithException : Exception
{
    public AuthSmithException(string message) : base(message)
    {
    }

    public AuthSmithException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

