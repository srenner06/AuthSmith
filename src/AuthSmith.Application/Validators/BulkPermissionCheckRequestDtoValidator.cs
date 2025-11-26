using AuthSmith.Contracts.Authorization;
using FluentValidation;

namespace AuthSmith.Application.Validators;

public class BulkPermissionCheckRequestDtoValidator : AbstractValidator<BulkPermissionCheckRequestDto>
{
    public BulkPermissionCheckRequestDtoValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("User ID is required.");

        RuleFor(x => x.ApplicationKey)
            .NotEmpty().WithMessage("Application key is required.")
            .MaximumLength(100).WithMessage("Application key must not exceed 100 characters.");

        RuleFor(x => x.Checks)
            .NotEmpty().WithMessage("At least one permission check is required.")
            .Must(checks => checks.Count > 0).WithMessage("At least one permission check is required.");

        RuleForEach(x => x.Checks)
            .SetValidator(new PermissionCheckItemDtoValidator());
    }
}

public class PermissionCheckItemDtoValidator : AbstractValidator<PermissionCheckItemDto>
{
    public PermissionCheckItemDtoValidator()
    {
        RuleFor(x => x.Module)
            .NotEmpty().WithMessage("Module is required.")
            .MaximumLength(100).WithMessage("Module must not exceed 100 characters.");

        RuleFor(x => x.Action)
            .NotEmpty().WithMessage("Action is required.")
            .MaximumLength(100).WithMessage("Action must not exceed 100 characters.");
    }
}

