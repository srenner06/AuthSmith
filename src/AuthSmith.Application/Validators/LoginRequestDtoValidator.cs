using AuthSmith.Contracts.Auth;
using FluentValidation;

namespace AuthSmith.Application.Validators;

public class LoginRequestDtoValidator : AbstractValidator<LoginRequestDto>
{
    public LoginRequestDtoValidator()
    {
        RuleFor(x => x.UsernameOrEmail)
            .NotEmpty().WithMessage("Username or email is required.")
            .MaximumLength(256).WithMessage("Username or email must not exceed 256 characters.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.");

        RuleFor(x => x.AppKey)
            .NotEmpty().WithMessage("Application key is required.")
            .MaximumLength(100).WithMessage("Application key must not exceed 100 characters.");
    }
}

