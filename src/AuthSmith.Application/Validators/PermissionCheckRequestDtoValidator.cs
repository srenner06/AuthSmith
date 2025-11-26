using AuthSmith.Contracts.Authorization;
using FluentValidation;

namespace AuthSmith.Application.Validators;

public class PermissionCheckRequestDtoValidator : AbstractValidator<PermissionCheckRequestDto>
{
    public PermissionCheckRequestDtoValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("User ID is required.");

        RuleFor(x => x.ApplicationKey)
            .NotEmpty().WithMessage("Application key is required.")
            .MaximumLength(100).WithMessage("Application key must not exceed 100 characters.");

        RuleFor(x => x.Module)
            .NotEmpty().WithMessage("Module is required.")
            .MaximumLength(100).WithMessage("Module must not exceed 100 characters.");

        RuleFor(x => x.Action)
            .NotEmpty().WithMessage("Action is required.")
            .MaximumLength(100).WithMessage("Action must not exceed 100 characters.");
    }
}

