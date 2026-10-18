using CharityCrm.Core.Abstractions;
using CharityCrm.Core.Common;
using CharityCrm.Core.Entities;
using CharityCrm.Core.Identity;
using Microsoft.AspNetCore.Identity;

namespace CharityCrm.Core.Services.Auth;

/// <summary>Login, logout and own-password handling (FR-AUTH-01..06).</summary>
public sealed class AuthService(
    UserManager<AppUser> userManager,
    ISignInService signIn,
    IAppDbContext db,
    IAuditLogger audit,
    IClock clock)
{
    public const string InvalidCredentialsMessage = "E-Mail oder Passwort falsch";
    public const string LockedOutMessage =
        "Zu viele fehlgeschlagene Anmeldeversuche. Das Konto ist für 15 Minuten gesperrt.";

    public async Task<CurrentUserDto> LoginAsync(LoginRequest request)
    {
        var user = await userManager.FindByEmailAsync(request.Email.Trim());
        if (user is null)
        {
            await LogFailedLoginAsync(null, "UnknownEmail");
            throw new AuthenticationFailedException(InvalidCredentialsMessage);
        }

        var outcome = await signIn.PasswordSignInAsync(user, request.Password);
        switch (outcome)
        {
            case SignInOutcome.Succeeded:
                user.LastLoginAt = clock.UtcNow;
                audit.Add(AuditActions.LoginSucceeded, AuditEntityTypes.User, user.Id.ToString(), userId: user.Id);
                (await userManager.UpdateAsync(user)).EnsureSucceeded();
                return ToDto(user);

            case SignInOutcome.LockedOut:
                await LogFailedLoginAsync(user.Id, "LockedOut");
                throw new AuthenticationFailedException(LockedOutMessage, "LOCKED_OUT");

            default:
                // Wrong password and deactivated users (NotAllowed) get the same generic message.
                await LogFailedLoginAsync(user.Id, outcome == SignInOutcome.NotAllowed ? "Inactive" : "InvalidPassword");
                throw new AuthenticationFailedException(InvalidCredentialsMessage);
        }
    }

    public async Task LogoutAsync(Guid userId)
    {
        audit.Add(AuditActions.Logout, AuditEntityTypes.User, userId.ToString(), userId: userId);
        await db.SaveChangesAsync();
        await signIn.SignOutAsync();
    }

    public async Task<CurrentUserDto> GetCurrentAsync(Guid userId)
    {
        var user = await userManager.FindByIdAsync(userId.ToString())
            ?? throw new NotFoundException("Benutzer nicht gefunden.");
        return ToDto(user);
    }

    /// <summary>FR-AUTH-05/06: requires the current password and clears the forced-change flag.</summary>
    public async Task<CurrentUserDto> ChangePasswordAsync(Guid userId, ChangePasswordRequest request)
    {
        var user = await userManager.FindByIdAsync(userId.ToString())
            ?? throw new NotFoundException("Benutzer nicht gefunden.");

        var result = await userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
        if (!result.Succeeded && result.Errors.Any(e => e.Code == nameof(IdentityErrorDescriber.PasswordMismatch)))
        {
            throw new InputValidationException(nameof(request.CurrentPassword).ToCamelCase(), "Das aktuelle Passwort ist falsch.");
        }
        result.EnsureSucceeded(passwordField: "newPassword");

        user.MustChangePassword = false;
        audit.Add(AuditActions.PasswordChanged, AuditEntityTypes.User, user.Id.ToString());
        (await userManager.UpdateAsync(user)).EnsureSucceeded();

        // Changing the password rotates the security stamp; keep this session alive with a fresh cookie.
        await signIn.RefreshSignInAsync(user);
        return ToDto(user);
    }

    private async Task LogFailedLoginAsync(Guid? userId, string reason)
    {
        var changes = new AuditChanges().Set("reason", reason);
        audit.Add(AuditActions.LoginFailed, AuditEntityTypes.User, userId?.ToString(), changes, userId);
        await db.SaveChangesAsync();
    }

    internal static CurrentUserDto ToDto(AppUser user) =>
        new(user.Id, user.Email!, user.FirstName, user.LastName, user.Role, user.MustChangePassword);
}
