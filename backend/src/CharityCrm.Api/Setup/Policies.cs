using CharityCrm.Core.Entities;

namespace CharityCrm.Api.Setup;

/// <summary>Named authorization policies (ARCHITECTURE §6, NFR-SEC-01).</summary>
public static class Policies
{
    public const string AdminOnly = nameof(AdminOnly);
    public const string MemberRead = nameof(MemberRead);
    public const string MemberWrite = nameof(MemberWrite);
    public const string Finance = nameof(Finance);

    public static void Configure(Microsoft.AspNetCore.Authorization.AuthorizationBuilder builder) => builder
        .AddPolicy(AdminOnly, p => p.RequireRole(nameof(UserRole.Admin)))
        .AddPolicy(MemberRead, p => p.RequireRole(nameof(UserRole.Admin), nameof(UserRole.Cashier), nameof(UserRole.Secretary)))
        .AddPolicy(MemberWrite, p => p.RequireRole(nameof(UserRole.Admin), nameof(UserRole.Secretary)))
        .AddPolicy(Finance, p => p.RequireRole(nameof(UserRole.Admin), nameof(UserRole.Cashier)))
        // Safety net: any endpoint without an explicit policy still requires a logged-in user.
        .SetFallbackPolicy(new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());
}
