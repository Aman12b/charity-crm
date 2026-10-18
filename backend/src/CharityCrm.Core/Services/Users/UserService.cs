using CharityCrm.Core.Abstractions;
using CharityCrm.Core.Common;
using CharityCrm.Core.Entities;
using CharityCrm.Core.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CharityCrm.Core.Services.Users;

/// <summary>User management for admins (FR-USR-01..06).</summary>
public sealed class UserService(
    UserManager<AppUser> userManager,
    IAppDbContext db,
    IAuditLogger audit,
    ICurrentUser currentUser,
    IClock clock)
{
    public async Task<PagedResult<UserDto>> ListAsync(UserListQuery query)
    {
        var page = Paging.NormalizePage(query.Page);
        var pageSize = Paging.NormalizePageSize(query.PageSize);

        var users = db.Users.AsNoTracking();
        if (query.Role is { } role)
        {
            users = users.Where(u => u.Role == role);
        }
        if (query.IsActive is { } isActive)
        {
            users = users.Where(u => u.IsActive == isActive);
        }

        var total = await users.CountAsync();
        var items = await users
            .OrderBy(u => u.LastName).ThenBy(u => u.FirstName).ThenBy(u => u.Id)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .ToListAsync();

        return new PagedResult<UserDto>(items.Select(ToDto).ToList(), page, pageSize, total);
    }

    public async Task<UserDto> GetAsync(Guid id) => ToDto(await FindAsync(id));

    public async Task<UserDto> CreateAsync(CreateUserRequest request)
    {
        var email = request.Email.Trim();
        if (await userManager.FindByEmailAsync(email) is not null)
        {
            throw new ConflictException("Diese E-Mail-Adresse wird bereits verwendet.", "DUPLICATE_EMAIL");
        }

        var user = new AppUser
        {
            Id = Guid.NewGuid(),
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            Role = request.Role,
            IsActive = true,
            MustChangePassword = true,
            CreatedAt = clock.UtcNow,
        };

        await using var tx = await db.BeginTransactionAsync();
        (await userManager.CreateAsync(user, request.TemporaryPassword)).EnsureSucceeded("temporaryPassword");

        audit.Add(AuditActions.UserCreated, AuditEntityTypes.User, user.Id.ToString(), new AuditChanges()
            .Set("email", user.Email)
            .Set("firstName", user.FirstName)
            .Set("lastName", user.LastName)
            .Set("role", user.Role));
        await db.SaveChangesAsync();
        await tx.CommitAsync();

        return ToDto(user);
    }

    public async Task<UserDto> UpdateAsync(Guid id, UpdateUserRequest request)
    {
        var user = await FindAsync(id);
        EnsureVersion(user, request.Version);

        var email = request.Email.Trim();
        if (!string.Equals(email, user.Email, StringComparison.OrdinalIgnoreCase)
            && await userManager.FindByEmailAsync(email) is not null)
        {
            throw new ConflictException("Diese E-Mail-Adresse wird bereits verwendet.", "DUPLICATE_EMAIL");
        }

        if (request.Role != UserRole.Admin)
        {
            UserRules.EnsureAdminRemains(user, staysActiveAdmin: false, await CountOtherActiveAdminsAsync(user.Id));
        }

        var changes = new AuditChanges()
            .Track("email", user.Email, email)
            .Track("firstName", user.FirstName, request.FirstName.Trim())
            .Track("lastName", user.LastName, request.LastName.Trim())
            .Track("role", user.Role, request.Role);

        user.Email = email;
        user.UserName = email;
        user.FirstName = request.FirstName.Trim();
        user.LastName = request.LastName.Trim();
        user.Role = request.Role;

        if (!changes.IsEmpty)
        {
            audit.Add(AuditActions.UserUpdated, AuditEntityTypes.User, user.Id.ToString(), changes);
        }
        (await userManager.UpdateAsync(user)).EnsureSucceeded();
        return ToDto(user);
    }

    /// <summary>FR-USR-04: deactivation also invalidates all sessions of the user.</summary>
    public async Task<UserDto> DeactivateAsync(Guid id)
    {
        var user = await FindAsync(id);
        UserRules.EnsureNotSelf(user.Id, currentUser.UserId);
        if (!user.IsActive)
        {
            return ToDto(user);
        }
        UserRules.EnsureAdminRemains(user, staysActiveAdmin: false, await CountOtherActiveAdminsAsync(user.Id));

        user.IsActive = false;
        audit.Add(AuditActions.UserDeactivated, AuditEntityTypes.User, user.Id.ToString(),
            new AuditChanges().Track("isActive", true, false));

        // Rotating the security stamp makes every existing cookie of this user invalid.
        (await userManager.UpdateSecurityStampAsync(user)).EnsureSucceeded();
        return ToDto(user);
    }

    public async Task<UserDto> ReactivateAsync(Guid id)
    {
        var user = await FindAsync(id);
        if (user.IsActive)
        {
            return ToDto(user);
        }

        user.IsActive = true;
        audit.Add(AuditActions.UserReactivated, AuditEntityTypes.User, user.Id.ToString(),
            new AuditChanges().Track("isActive", false, true));
        (await userManager.UpdateAsync(user)).EnsureSucceeded();
        return ToDto(user);
    }

    /// <summary>FR-USR-05: sets a temporary password and forces a change on next login.</summary>
    public async Task<UserDto> ResetPasswordAsync(Guid id, ResetPasswordRequest request)
    {
        var user = await FindAsync(id);

        await using var tx = await db.BeginTransactionAsync();
        var token = await userManager.GeneratePasswordResetTokenAsync(user);
        (await userManager.ResetPasswordAsync(user, token, request.TemporaryPassword)).EnsureSucceeded("temporaryPassword");

        // A reset also lifts an active lockout so the user can log in with the temporary password.
        await userManager.SetLockoutEndDateAsync(user, null);
        await userManager.ResetAccessFailedCountAsync(user);

        user.MustChangePassword = true;
        audit.Add(AuditActions.UserPasswordReset, AuditEntityTypes.User, user.Id.ToString());
        (await userManager.UpdateAsync(user)).EnsureSucceeded();
        await tx.CommitAsync();

        return ToDto(user);
    }

    private async Task<AppUser> FindAsync(Guid id) =>
        await userManager.FindByIdAsync(id.ToString()) ?? throw new NotFoundException("Benutzer nicht gefunden.");

    private Task<int> CountOtherActiveAdminsAsync(Guid excludedId) =>
        db.Users.CountAsync(u => u.Id != excludedId && u.IsActive && u.Role == UserRole.Admin);

    private static void EnsureVersion(AppUser user, string version)
    {
        if (user.ConcurrencyStamp != version)
        {
            throw new ConflictException(
                "Der Benutzer wurde inzwischen von jemand anderem geändert. Bitte laden Sie die Seite neu.",
                "CONCURRENCY_CONFLICT");
        }
    }

    internal static UserDto ToDto(AppUser u) => new(
        u.Id, u.Email!, u.FirstName, u.LastName, u.Role, u.IsActive, u.MustChangePassword,
        u.LastLoginAt, u.CreatedAt, u.ConcurrencyStamp ?? "");
}
