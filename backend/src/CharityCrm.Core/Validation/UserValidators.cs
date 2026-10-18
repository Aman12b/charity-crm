using CharityCrm.Core.Services.Users;
using FluentValidation;

namespace CharityCrm.Core.Validation;

public sealed class CreateUserRequestValidator : AbstractValidator<CreateUserRequest>
{
    public CreateUserRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().WithMessage("Bitte die E-Mail-Adresse eingeben.").ValidEmail();
        RuleFor(x => x.FirstName).ValidName();
        RuleFor(x => x.LastName).ValidName();
        RuleFor(x => x.Role).IsInEnum().WithMessage("Bitte eine gültige Rolle wählen.");
        RuleFor(x => x.TemporaryPassword).MeetsPasswordPolicy();
    }
}

public sealed class UpdateUserRequestValidator : AbstractValidator<UpdateUserRequest>
{
    public UpdateUserRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().WithMessage("Bitte die E-Mail-Adresse eingeben.").ValidEmail();
        RuleFor(x => x.FirstName).ValidName();
        RuleFor(x => x.LastName).ValidName();
        RuleFor(x => x.Role).IsInEnum().WithMessage("Bitte eine gültige Rolle wählen.");
        RuleFor(x => x.Version).NotEmpty();
    }
}

public sealed class ResetPasswordRequestValidator : AbstractValidator<ResetPasswordRequest>
{
    public ResetPasswordRequestValidator()
    {
        RuleFor(x => x.TemporaryPassword).MeetsPasswordPolicy();
    }
}
