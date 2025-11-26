using AuthSmith.Contracts.Applications;
using FluentValidation;

namespace AuthSmith.Application.Validators;

public class CreateApplicationRequestDtoValidator : AbstractValidator<CreateApplicationRequestDto>
{
    public CreateApplicationRequestDtoValidator()
    {
        RuleFor(x => x.Key)
            .NotEmpty().WithMessage("Application key is required.")
            .MinimumLength(2).WithMessage("Application key must be at least 2 characters long.")
            .MaximumLength(100).WithMessage("Application key must not exceed 100 characters.")
            .Matches(@"^[a-z0-9-]+$").WithMessage("Application key can only contain lowercase letters, numbers, and hyphens.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Application name is required.")
            .MaximumLength(256).WithMessage("Application name must not exceed 256 characters.");

        RuleFor(x => x.MaxFailedLoginAttempts)
            .GreaterThan(0).WithMessage("Max failed login attempts must be greater than 0.")
            .LessThanOrEqualTo(100).WithMessage("Max failed login attempts must not exceed 100.");

        RuleFor(x => x.LockoutDurationMinutes)
            .GreaterThan(0).WithMessage("Lockout duration must be greater than 0.")
            .LessThanOrEqualTo(1440).WithMessage("Lockout duration must not exceed 1440 minutes (24 hours).");
    }
}

