using AuthSmith.Domain.Errors;
using Microsoft.AspNetCore.Mvc;
using OneOf;

namespace AuthSmith.Api.Extensions;

/// <summary>
/// Extension methods for converting OneOf results to HTTP action results.
/// </summary>
public static class OneOfExtensions
{
    /// <summary>
    /// Converts a OneOf result with NotFoundError to an ActionResult.
    /// </summary>
    public static ActionResult<T> ToActionResult<T>(this OneOf<T, NotFoundError> result)
    {
        return result.Match<ActionResult<T>>(
            success => new OkObjectResult(success),
            notFound => new NotFoundObjectResult(new { error = notFound.Message ?? "Resource not found." })
        );
    }

    /// <summary>
    /// Converts a OneOf result with NotFoundError and ConflictError to an ActionResult.
    /// </summary>
    public static ActionResult<T> ToActionResult<T>(this OneOf<T, NotFoundError, ConflictError> result)
    {
        return result.Match<ActionResult<T>>(
            success => new OkObjectResult(success),
            notFound => new NotFoundObjectResult(new { error = notFound.Message ?? "Resource not found." }),
            conflict => new ConflictObjectResult(new { error = conflict.Message })
        );
    }

    /// <summary>
    /// Converts a OneOf result with UnauthorizedError to an ActionResult.
    /// </summary>
    public static ActionResult<T> ToActionResult<T>(this OneOf<T, UnauthorizedError> result)
    {
        return result.Match<ActionResult<T>>(
            success => new OkObjectResult(success),
            unauthorized => new UnauthorizedObjectResult(new { error = unauthorized.Message })
        );
    }

    /// <summary>
    /// Converts a OneOf result with NotFoundError and UnauthorizedError to an ActionResult.
    /// </summary>
    public static ActionResult<T> ToActionResult<T>(this OneOf<T, NotFoundError, UnauthorizedError> result)
    {
        return result.Match<ActionResult<T>>(
            success => new OkObjectResult(success),
            notFound => new NotFoundObjectResult(new { error = notFound.Message ?? "Resource not found." }),
            unauthorized => new UnauthorizedObjectResult(new { error = unauthorized.Message })
        );
    }

    /// <summary>
    /// Converts a OneOf result with UnauthorizedError and NotFoundError to an ActionResult.
    /// </summary>
    public static ActionResult<T> ToActionResult<T>(this OneOf<T, UnauthorizedError, NotFoundError> result)
    {
        return result.Match<ActionResult<T>>(
            success => new OkObjectResult(success),
            unauthorized => new UnauthorizedObjectResult(new { error = unauthorized.Message }),
            notFound => new NotFoundObjectResult(new { error = notFound.Message ?? "Resource not found." })
        );
    }

    /// <summary>
    /// Converts a OneOf result with NotFoundError, ConflictError, and InvalidOperationError to an ActionResult.
    /// </summary>
    public static ActionResult<T> ToActionResult<T>(this OneOf<T, NotFoundError, ConflictError, InvalidOperationError> result)
    {
        return result.Match<ActionResult<T>>(
            success => new OkObjectResult(success),
            notFound => new NotFoundObjectResult(new { error = notFound.Message ?? "Resource not found." }),
            conflict => new ConflictObjectResult(new { error = conflict.Message }),
            invalidOperation => new BadRequestObjectResult(new { error = invalidOperation.Message })
        );
    }

    /// <summary>
    /// Converts a OneOf result with NotFoundError and InvalidOperationError to an ActionResult.
    /// </summary>
    public static ActionResult<T> ToActionResult<T>(this OneOf<T, NotFoundError, InvalidOperationError> result)
    {
        return result.Match<ActionResult<T>>(
            success => new OkObjectResult(success),
            notFound => new NotFoundObjectResult(new { error = notFound.Message ?? "Resource not found." }),
            invalidOperation => new BadRequestObjectResult(new { error = invalidOperation.Message })
        );
    }

    /// <summary>
    /// Converts a OneOf result with NotFoundError and FileNotFoundError to an ActionResult.
    /// </summary>
    public static ActionResult<T> ToActionResult<T>(this OneOf<T, NotFoundError, FileNotFoundError> result)
    {
        return result.Match<ActionResult<T>>(
            success => new OkObjectResult(success),
            notFound => new NotFoundObjectResult(new { error = notFound.Message ?? "Resource not found." }),
            fileNotFound => new ObjectResult(new { error = fileNotFound.Message })
            {
                StatusCode = 500
            }
        );
    }

    /// <summary>
    /// Converts a OneOf result with Success and NotFoundError to an ActionResult.
    /// </summary>
    public static ActionResult ToActionResult(this OneOf<Success, NotFoundError> result)
    {
        return result.Match<ActionResult>(
            success => new OkResult(),
            notFound => new NotFoundObjectResult(new { error = notFound.Message ?? "Resource not found." })
        );
    }

    /// <summary>
    /// Converts a OneOf result with Success, NotFoundError, and ConflictError to an ActionResult.
    /// </summary>
    public static ActionResult ToActionResult(this OneOf<Success, NotFoundError, ConflictError> result)
    {
        return result.Match<ActionResult>(
            success => new OkResult(),
            notFound => new NotFoundObjectResult(new { error = notFound.Message ?? "Resource not found." }),
            conflict => new ConflictObjectResult(new { error = conflict.Message })
        );
    }
}

