using System.Net;

namespace AuthSmith.Sdk.Exceptions;

/// <summary>
/// Exception thrown when the AuthSmith API returns an error response.
/// </summary>
public class AuthSmithApiException : AuthSmithException
{
    public HttpStatusCode StatusCode { get; }
    public string? ResponseContent { get; }

    public AuthSmithApiException(HttpStatusCode statusCode, string message, string? responseContent = null)
        : base(message)
    {
        StatusCode = statusCode;
        ResponseContent = responseContent;
    }
}

