using FluentValidation.Results;

namespace AuthSmith.Application.Errors;

/// <summary>
/// Error containing validation failures from FluentValidation.
/// </summary>
public sealed class ValidationError
{
    public ValidationErrorDetail[] Errors { get; }

    public ValidationError(ValidationErrorDetail[] errors)
    {
        Errors = errors;
    }

    public static ValidationError FromFluentValidation(IEnumerable<ValidationFailure> failures)
    {
        var errors = failures.Select(f => new ValidationErrorDetail
        {
            PropertyName = f.PropertyName,
            ErrorMessage = f.ErrorMessage,
            AttemptedValue = f.AttemptedValue
        }).ToArray();

        return new ValidationError(errors);
    }
}

/// <summary>
/// Details of a single validation error.
/// </summary>
public sealed class ValidationErrorDetail
{
    public required string PropertyName { get; init; }
    public required string ErrorMessage { get; init; }
    public object? AttemptedValue { get; init; }
}

