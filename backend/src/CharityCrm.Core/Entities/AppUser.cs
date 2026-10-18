using Microsoft.AspNetCore.Identity;

namespace CharityCrm.Core.Entities;

/// <summary>A staff member who logs into the system. Users are never hard-deleted (FR-USR-04).</summary>
public class AppUser : IdentityUser<Guid>
{
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public UserRole Role { get; set; }
    public bool IsActive { get; set; } = true;
    public bool MustChangePassword { get; set; }
    public DateTimeOffset? LastLoginAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
