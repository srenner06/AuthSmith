using AuthSmith.Contracts.Auth;
using FluentValidation;

namespace AuthSmith.Application.Validators;

public class RevokeRefreshTokenRequestDtoValidator : AbstractValidator<RevokeRefreshTokenRequestDto>
{
    public RevokeRefreshTokenRequestDtoValidator()
    {
        RuleFor(x => x.RefreshToken)
            .NotEmpty().WithMessage("Refresh token is required.");
    }
}

