using AuthSmith.Contracts.Errors;
using AuthSmith.Domain.Errors;
using Microsoft.AspNetCore.Mvc;
using OneOf;

namespace AuthSmith.Api.Extensions;

/// <summary>
/// Extension methods for converting OneOf results to HTTP action results.
/// </summary>
public static class OneOfExtensions
{
    private static ErrorResponseDto CreateErrorResponse(string message, int statusCode, string? instance = null)
    {
        return new ErrorResponseDto
        {
            Type = GetErrorType(statusCode),
            Title = GetErrorTitle(statusCode),
            Status = statusCode,
            Detail = message,
            Instance = instance
        };
    }

    private static string GetErrorType(int statusCode) => statusCode switch
    {
        400 => "https://tools.ietf.org/html/rfc7231#section-6.5.1",
        401 => "https://tools.ietf.org/html/rfc7235#section-3.1",
        404 => "https://tools.ietf.org/html/rfc7231#section-6.5.4",
        409 => "https://tools.ietf.org/html/rfc7231#section-6.5.8",
        500 => "https://tools.ietf.org/html/rfc7231#section-6.6.1",
        _ => "https://tools.ietf.org/html/rfc7231#section-6.5.1"
    };

    private static string GetErrorTitle(int statusCode) => statusCode switch
    {
        400 => "Bad Request",
        401 => "Unauthorized",
        404 => "Not Found",
        409 => "Conflict",
        500 => "Internal Server Error",
        _ => "An error occurred"
    };

    private static NotFoundObjectResult ToErrorResult(NotFoundError error) =>
        new NotFoundObjectResult(CreateErrorResponse(error.Message ?? "Resource not found.", 404));

    private static UnauthorizedObjectResult ToErrorResult(UnauthorizedError error) =>
        new UnauthorizedObjectResult(CreateErrorResponse(error.Message, 401));

    private static ConflictObjectResult ToErrorResult(ConflictError error) =>
        new ConflictObjectResult(CreateErrorResponse(error.Message, 409));

    private static BadRequestObjectResult ToErrorResult(InvalidOperationError error) =>
        new BadRequestObjectResult(CreateErrorResponse(error.Message, 400));

    private static BadRequestObjectResult ToErrorResult(ValidationError error)
    {
        var errors = error.Errors.ToDictionary(
            e => e.PropertyName,
            e => new[] { e.ErrorMessage });

        return new BadRequestObjectResult(new
        {
            type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
            title = "Validation Error",
            status = 400,
            errors
        });
    }

    private static ObjectResult ToErrorResult(FileNotFoundError error) =>
        new ObjectResult(CreateErrorResponse(error.Message, 500)) { StatusCode = 500 };

    public static ActionResult<T> ToActionResult<T>(this OneOf<T, NotFoundError> result)
    {
        return result.Match<ActionResult<T>>(
            success => new OkObjectResult(success),
            notFound => ToErrorResult(notFound)
        );
    }

    public static ActionResult<T> ToActionResult<T>(this OneOf<T, NotFoundError, ConflictError> result)
    {
        return result.Match<ActionResult<T>>(
            success => new OkObjectResult(success),
            notFound => ToErrorResult(notFound),
            conflict => ToErrorResult(conflict)
        );
    }

    public static ActionResult<T> ToActionResult<T>(this OneOf<T, UnauthorizedError> result)
    {
        return result.Match<ActionResult<T>>(
            success => new OkObjectResult(success),
            unauthorized => ToErrorResult(unauthorized)
        );
    }

    public static ActionResult<T> ToActionResult<T>(this OneOf<T, NotFoundError, UnauthorizedError> result)
    {
        return result.Match<ActionResult<T>>(
            success => new OkObjectResult(success),
            notFound => ToErrorResult(notFound),
            unauthorized => ToErrorResult(unauthorized)
        );
    }

    public static ActionResult<T> ToActionResult<T>(this OneOf<T, UnauthorizedError, NotFoundError> result)
    {
        return result.Match<ActionResult<T>>(
            success => new OkObjectResult(success),
            unauthorized => ToErrorResult(unauthorized),
            notFound => ToErrorResult(notFound)
        );
    }

    public static ActionResult<T> ToActionResult<T>(this OneOf<T, NotFoundError, ConflictError, InvalidOperationError> result)
    {
        return result.Match<ActionResult<T>>(
            success => new OkObjectResult(success),
            notFound => ToErrorResult(notFound),
            conflict => ToErrorResult(conflict),
            invalidOperation => ToErrorResult(invalidOperation)
        );
    }

    public static ActionResult<T> ToActionResult<T>(this OneOf<T, NotFoundError, InvalidOperationError> result)
    {
        return result.Match<ActionResult<T>>(
            success => new OkObjectResult(success),
            notFound => ToErrorResult(notFound),
            invalidOperation => ToErrorResult(invalidOperation)
        );
    }

    public static ActionResult<T> ToActionResult<T>(this OneOf<T, NotFoundError, FileNotFoundError> result)
    {
        return result.Match<ActionResult<T>>(
            success => new OkObjectResult(success),
            notFound => ToErrorResult(notFound),
            fileNotFound => ToErrorResult(fileNotFound)
        );
    }

    public static ActionResult ToActionResult(this OneOf<Success, NotFoundError> result)
    {
        return result.Match<ActionResult>(
            success => new OkResult(),
            notFound => ToErrorResult(notFound)
        );
    }

    public static ActionResult ToActionResult(this OneOf<Success, NotFoundError, ConflictError> result)
    {
        return result.Match<ActionResult>(
            success => new OkResult(),
            notFound => ToErrorResult(notFound),
            conflict => ToErrorResult(conflict)
        );
    }

    public static ActionResult<T> ToActionResult<T>(this OneOf<T, NotFoundError, ValidationError> result)
    {
        return result.Match<ActionResult<T>>(
            success => new OkObjectResult(success),
            notFound => ToErrorResult(notFound),
            validationError => ToErrorResult(validationError)
        );
    }

    public static ActionResult<T> ToActionResult<T>(this OneOf<T, NotFoundError, ConflictError, ValidationError> result)
    {
        return result.Match<ActionResult<T>>(
            success => new OkObjectResult(success),
            notFound => ToErrorResult(notFound),
            conflict => ToErrorResult(conflict),
            validationError => ToErrorResult(validationError)
        );
    }

    public static ActionResult ToActionResult(this OneOf<Success, NotFoundError, UnauthorizedError, ValidationError> result)
    {
        return result.Match<ActionResult>(
            success => new OkResult(),
            notFound => ToErrorResult(notFound),
            unauthorized => ToErrorResult(unauthorized),
            validationError => ToErrorResult(validationError)
        );
    }

    public static ActionResult ToActionResult(this OneOf<Success, NotFoundError, UnauthorizedError> result)
    {
        return result.Match<ActionResult>(
            success => new OkResult(),
            notFound => ToErrorResult(notFound),
            unauthorized => ToErrorResult(unauthorized)
        );
    }
}

