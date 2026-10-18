using CharityCrm.Core.Entities;

namespace CharityCrm.Core.Services.Auth;

public sealed record LoginRequest(string Email, string Password);

public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);

public sealed record CurrentUserDto(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    UserRole Role,
    bool MustChangePassword);
