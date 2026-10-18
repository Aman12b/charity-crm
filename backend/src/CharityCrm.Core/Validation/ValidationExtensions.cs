using CharityCrm.Core.Identity;
using FluentValidation;

namespace CharityCrm.Core.Validation;

public static class ValidationExtensions
{
    public static IRuleBuilderOptions<T, string> MeetsPasswordPolicy<T>(this IRuleBuilder<T, string> rule) =>
        rule.NotEmpty().WithMessage("Bitte ein Passwort eingeben.")
            .Must(PasswordPolicy.IsSatisfiedBy).WithMessage(PasswordPolicy.Description);

    public static IRuleBuilderOptions<T, string> ValidName<T>(this IRuleBuilder<T, string> rule, int maxLength = 100) =>
        rule.Must(v => !string.IsNullOrWhiteSpace(v)).WithMessage("Dieses Feld ist erforderlich.")
            .MaximumLength(maxLength).WithMessage($"Maximal {maxLength} Zeichen.");

    public static IRuleBuilderOptions<T, string?> ValidEmail<T>(this IRuleBuilder<T, string?> rule) =>
        rule.MaximumLength(254).WithMessage("Maximal 254 Zeichen.")
            .EmailAddress().WithMessage("Bitte eine gültige E-Mail-Adresse eingeben.");
}
