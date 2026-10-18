using CharityCrm.Core.Services.Auth;
using FluentValidation;

namespace CharityCrm.Core.Validation;

public sealed class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().WithMessage("Bitte die E-Mail-Adresse eingeben.");
        RuleFor(x => x.Password).NotEmpty().WithMessage("Bitte das Passwort eingeben.");
    }
}

public sealed class ChangePasswordRequestValidator : AbstractValidator<ChangePasswordRequest>
{
    public ChangePasswordRequestValidator()
    {
        RuleFor(x => x.CurrentPassword).NotEmpty().WithMessage("Bitte das aktuelle Passwort eingeben.");
        RuleFor(x => x.NewPassword).MeetsPasswordPolicy();
    }
}
