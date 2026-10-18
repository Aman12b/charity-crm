using CharityCrm.Core.Common;
using CharityCrm.Core.Entities;

namespace CharityCrm.Core.Services.Users;

/// <summary>FR-USR-06: there must always be at least one active admin.</summary>
public static class UserRules
{
    public static void EnsureAdminRemains(AppUser target, bool staysActiveAdmin, int otherActiveAdmins)
    {
        var isActiveAdmin = target.IsActive && target.Role == UserRole.Admin;
        if (isActiveAdmin && !staysActiveAdmin && otherActiveAdmins == 0)
        {
            throw new BusinessRuleException(
                "Diese Aktion ist nicht möglich, da es sonst keinen aktiven Administrator mehr gäbe.", "LAST_ADMIN");
        }
    }

    public static void EnsureNotSelf(Guid targetId, Guid? currentUserId)
    {
        if (targetId == currentUserId)
        {
            throw new BusinessRuleException("Sie können sich nicht selbst deaktivieren.", "SELF_DEACTIVATION");
        }
    }
}
