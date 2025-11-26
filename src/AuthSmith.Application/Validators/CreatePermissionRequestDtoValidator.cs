using AuthSmith.Contracts.Permissions;
using FluentValidation;

namespace AuthSmith.Application.Validators;

public class CreatePermissionRequestDtoValidator : AbstractValidator<CreatePermissionRequestDto>
{
    public CreatePermissionRequestDtoValidator()
    {
        RuleFor(x => x.Module)
            .NotEmpty().WithMessage("Module is required.")
            .MaximumLength(100).WithMessage("Module must not exceed 100 characters.")
            .Matches(@"^[a-z0-9._-]+$").WithMessage("Module can only contain lowercase letters, numbers, dots, underscores, and hyphens.");

        RuleFor(x => x.Action)
            .NotEmpty().WithMessage("Action is required.")
            .MaximumLength(100).WithMessage("Action must not exceed 100 characters.")
            .Matches(@"^[a-z0-9._-]+$").WithMessage("Action can only contain lowercase letters, numbers, dots, underscores, and hyphens.");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Description must not exceed 500 characters.")
            .When(x => !string.IsNullOrEmpty(x.Description));
    }
}

