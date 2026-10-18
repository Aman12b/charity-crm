using CharityCrm.Core.Entities;
using Microsoft.AspNetCore.Identity;

namespace CharityCrm.Core.Identity;

/// <summary>NFR-SEC-03: minimum 10 characters, at least one letter and one digit.</summary>
public static class PasswordPolicy
{
    public const int MinLength = 10;
    public const string Description = "Mindestens 10 Zeichen, davon mindestens ein Buchstabe und eine Ziffer.";

    public static bool IsSatisfiedBy(string? password) =>
        password is { Length: >= MinLength } && password.Any(char.IsLetter) && password.Any(char.IsDigit);
}

/// <summary>Identity password validator enforcing <see cref="PasswordPolicy"/> (length and digit are configured in Identity options).</summary>
public sealed class LetterRequiredPasswordValidator : IPasswordValidator<AppUser>
{
    public Task<IdentityResult> ValidateAsync(UserManager<AppUser> manager, AppUser user, string? password) =>
        Task.FromResult(password is not null && password.Any(char.IsLetter)
            ? IdentityResult.Success
            : IdentityResult.Failed(new IdentityError
            {
                Code = "PasswordRequiresLetter",
                Description = "Das Passwort muss mindestens einen Buchstaben enthalten.",
            }));
}
