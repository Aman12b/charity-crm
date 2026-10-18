using CharityCrm.Core.Entities;

namespace CharityCrm.Core.Abstractions;

public enum SignInOutcome
{
    Succeeded,
    Failed,
    LockedOut,
    NotAllowed,
}

/// <summary>Issues and removes the session cookie; implemented on top of ASP.NET Core Identity in the API.</summary>
public interface ISignInService
{
    /// <summary>Checks the password with lockout enabled (FR-AUTH-04) and signs in on success.</summary>
    Task<SignInOutcome> PasswordSignInAsync(AppUser user, string password);

    Task SignOutAsync();

    /// <summary>Re-issues the cookie of the current session with fresh claims.</summary>
    Task RefreshSignInAsync(AppUser user);
}
