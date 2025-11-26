using AuthSmith.Contracts.Roles;
using FluentValidation;

namespace AuthSmith.Application.Validators;

public class RolesAssignPermissionRequestDtoValidator : AbstractValidator<AssignPermissionRequestDto>
{
    public RolesAssignPermissionRequestDtoValidator()
    {
        RuleFor(x => x.PermissionIds)
            .NotEmpty().WithMessage("At least one permission ID is required.")
            .Must(ids => ids.Count > 0).WithMessage("At least one permission ID is required.");

        RuleForEach(x => x.PermissionIds)
            .NotEmpty().WithMessage("Permission ID cannot be empty.");
    }
}

