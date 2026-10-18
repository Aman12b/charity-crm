using CharityCrm.Core.Entities;

namespace CharityCrm.Core.Services.Users;

public sealed record UserDto(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    UserRole Role,
    bool IsActive,
    bool MustChangePassword,
    DateTimeOffset? LastLoginAt,
    DateTimeOffset CreatedAt,
    string Version);

public sealed record UserListQuery(UserRole? Role, bool? IsActive, int? Page, int? PageSize);

public sealed record CreateUserRequest(string Email, string FirstName, string LastName, UserRole Role, string TemporaryPassword);

/// <summary><see cref="Version"/> is the concurrency token from <see cref="UserDto.Version"/> (NFR-REL-03).</summary>
public sealed record UpdateUserRequest(string Email, string FirstName, string LastName, UserRole Role, string Version);

public sealed record ResetPasswordRequest(string TemporaryPassword);
