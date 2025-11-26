using AuthSmith.Contracts.Users;
using FluentValidation;

namespace AuthSmith.Application.Validators;

public class AssignPermissionRequestDtoValidator : AbstractValidator<AssignPermissionRequestDto>
{
    public AssignPermissionRequestDtoValidator()
    {
        RuleFor(x => x.PermissionId)
            .NotEmpty().WithMessage("Permission ID is required.");
    }
}

