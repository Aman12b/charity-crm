using Microsoft.AspNetCore.Identity;

namespace CharityCrm.Core.Identity;

/// <summary>German texts for the Identity errors that can reach the UI.</summary>
public sealed class GermanIdentityErrorDescriber : IdentityErrorDescriber
{
    public override IdentityError PasswordMismatch() =>
        new() { Code = nameof(PasswordMismatch), Description = "Das aktuelle Passwort ist falsch." };

    public override IdentityError PasswordTooShort(int length) =>
        new() { Code = nameof(PasswordTooShort), Description = $"Das Passwort muss mindestens {length} Zeichen lang sein." };

    public override IdentityError PasswordRequiresDigit() =>
        new() { Code = nameof(PasswordRequiresDigit), Description = "Das Passwort muss mindestens eine Ziffer enthalten." };

    public override IdentityError DuplicateEmail(string email) =>
        new() { Code = nameof(DuplicateEmail), Description = "Diese E-Mail-Adresse wird bereits verwendet." };

    public override IdentityError DuplicateUserName(string userName) =>
        new() { Code = nameof(DuplicateUserName), Description = "Diese E-Mail-Adresse wird bereits verwendet." };

    public override IdentityError InvalidEmail(string? email) =>
        new() { Code = nameof(InvalidEmail), Description = "Die E-Mail-Adresse ist ungültig." };

    public override IdentityError ConcurrencyFailure() =>
        new() { Code = nameof(ConcurrencyFailure), Description = "Der Datensatz wurde inzwischen geändert. Bitte laden Sie die Seite neu." };
}
