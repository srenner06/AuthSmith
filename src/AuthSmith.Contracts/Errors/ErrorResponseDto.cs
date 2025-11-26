namespace AuthSmith.Contracts.Errors;

/// <summary>
/// Standard error response following RFC 7807 ProblemDetails format.
/// </summary>
public class ErrorResponseDto
{
    public string Type { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public int Status { get; set; }
    public string Detail { get; set; } = string.Empty;
    public string? Instance { get; set; }
}

/// <summary>
/// Validation error response containing field-level validation failures.
/// </summary>
public class ValidationErrorResponseDto
{
    public string Type { get; set; } = "https://tools.ietf.org/html/rfc7231#section-6.5.1";
    public string Title { get; set; } = "Validation failed";
    public int Status { get; set; } = 400;
    public string Detail { get; set; } = "One or more validation errors occurred.";
    public string? Instance { get; set; }
    public ValidationErrorDetailDto[] Errors { get; set; } = [];
}

/// <summary>
/// Details of a single validation error.
/// </summary>
public class ValidationErrorDetailDto
{
    public string PropertyName { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
    public object? AttemptedValue { get; set; }
}

