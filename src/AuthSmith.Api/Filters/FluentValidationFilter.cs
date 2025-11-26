using AuthSmith.Contracts.Errors;
using AuthSmith.Domain.Errors;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace AuthSmith.Api.Filters;

public class FluentValidationFilter : IAsyncActionFilter
{
    private readonly IServiceProvider _serviceProvider;

    public FluentValidationFilter(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        foreach (var parameter in context.ActionDescriptor.Parameters)
        {
            if (!context.ActionArguments.TryGetValue(parameter.Name, out var argument) || argument == null)
                continue;

            var argumentType = argument.GetType();
            var validatorType = typeof(IValidator<>).MakeGenericType(argumentType);

            if (_serviceProvider.GetService(validatorType) is not IValidator validator)
                continue;

            var validationContext = new ValidationContext<object>(argument);
            var validationResult = await validator.ValidateAsync(validationContext, context.HttpContext.RequestAborted);

            if (!validationResult.IsValid)
            {
                var validationError = ValidationError.FromFluentValidation(validationResult.Errors);
                var errorResponse = new ValidationErrorResponseDto
                {
                    Instance = context.HttpContext.Request.Path,
                    Errors = [.. validationError.Errors.Select(e => new ValidationErrorDetailDto
                    {
                        PropertyName = e.PropertyName,
                        ErrorMessage = e.ErrorMessage,
                        AttemptedValue = e.AttemptedValue
                    })]
                };
                context.Result = new BadRequestObjectResult(errorResponse);
                return;
            }
        }

        await next();
    }
}

