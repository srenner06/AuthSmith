using AuthSmith.Contracts.Users;
using FluentValidation;

namespace AuthSmith.Application.Validators;

public class AssignRoleRequestDtoValidator : AbstractValidator<AssignRoleRequestDto>
{
    public AssignRoleRequestDtoValidator()
    {
        RuleFor(x => x.RoleId)
            .NotEmpty().WithMessage("Role ID is required.");
    }
}

