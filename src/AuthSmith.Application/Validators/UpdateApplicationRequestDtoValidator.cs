using AuthSmith.Contracts.Applications;
using FluentValidation;

namespace AuthSmith.Application.Validators;

public class UpdateApplicationRequestDtoValidator : AbstractValidator<UpdateApplicationRequestDto>
{
    public UpdateApplicationRequestDtoValidator()
    {
        RuleFor(x => x.Name)
            .MaximumLength(256).WithMessage("Application name must not exceed 256 characters.")
            .When(x => !string.IsNullOrEmpty(x.Name));

        RuleFor(x => x.MaxFailedLoginAttempts)
            .GreaterThan(0).WithMessage("Max failed login attempts must be greater than 0.")
            .LessThanOrEqualTo(100).WithMessage("Max failed login attempts must not exceed 100.")
            .When(x => x.MaxFailedLoginAttempts.HasValue);

        RuleFor(x => x.LockoutDurationMinutes)
            .GreaterThan(0).WithMessage("Lockout duration must be greater than 0.")
            .LessThanOrEqualTo(1440).WithMessage("Lockout duration must not exceed 1440 minutes (24 hours).")
            .When(x => x.LockoutDurationMinutes.HasValue);
    }
}

