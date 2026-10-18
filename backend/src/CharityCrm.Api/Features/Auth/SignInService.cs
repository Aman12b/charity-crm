using System.Security.Claims;
using CharityCrm.Core.Abstractions;
using CharityCrm.Core.Entities;
using Microsoft.AspNetCore.Identity;

namespace CharityCrm.Api.Features.Auth;

public sealed class SignInService(SignInManager<AppUser> signInManager) : ISignInService
{
    public async Task<SignInOutcome> PasswordSignInAsync(AppUser user, string password)
    {
        var result = await signInManager.PasswordSignInAsync(user, password, isPersistent: false, lockoutOnFailure: true);
        if (result.Succeeded) return SignInOutcome.Succeeded;
        if (result.IsLockedOut) return SignInOutcome.LockedOut;
        if (result.IsNotAllowed) return SignInOutcome.NotAllowed;
        return SignInOutcome.Failed;
    }

    public Task SignOutAsync() => signInManager.SignOutAsync();

    public Task RefreshSignInAsync(AppUser user) => signInManager.RefreshSignInAsync(user);
}

public sealed class CurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    public Guid? UserId =>
        Guid.TryParse(accessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;
}

public static class ClaimsPrincipalExtensions
{
    public static Guid GetUserId(this ClaimsPrincipal user) =>
        Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException("User is not authenticated."));
}
