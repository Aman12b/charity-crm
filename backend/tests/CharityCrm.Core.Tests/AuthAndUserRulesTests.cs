using CharityCrm.Core.Abstractions;
using CharityCrm.Core.Common;
using CharityCrm.Core.Entities;
using CharityCrm.Core.Identity;
using CharityCrm.Core.Services.Users;
using FluentAssertions;

namespace CharityCrm.Core.Tests;

public sealed class PasswordPolicyTests
{
    [Theory]
    [InlineData("Passwort12", true)]
    [InlineData("a123456789", true)]
    [InlineData("Passwort1", false)]     // 9 chars
    [InlineData("PasswortOhne", false)]  // no digit
    [InlineData("1234567890", false)]    // no letter
    [InlineData("", false)]
    [InlineData(null, false)]
    public void NFR_SEC_03_Policy_requires_10_chars_letter_and_digit(string? password, bool expected) =>
        PasswordPolicy.IsSatisfiedBy(password).Should().Be(expected);
}

public sealed class UserRulesTests
{
    private static AppUser Admin(bool active = true) => new() { Role = UserRole.Admin, IsActive = active };

    [Fact]
    public void FR_USR_06_Removing_the_last_active_admin_is_rejected()
    {
        var act = () => UserRules.EnsureAdminRemains(Admin(), staysActiveAdmin: false, otherActiveAdmins: 0);

        act.Should().Throw<BusinessRuleException>().Which.Code.Should().Be("LAST_ADMIN");
    }

    [Fact]
    public void FR_USR_06_Removing_an_admin_is_allowed_when_another_active_admin_exists() =>
        UserRules.EnsureAdminRemains(Admin(), staysActiveAdmin: false, otherActiveAdmins: 1);

    [Fact]
    public void FR_USR_06_Inactive_or_non_admin_users_are_not_affected()
    {
        UserRules.EnsureAdminRemains(Admin(active: false), staysActiveAdmin: false, otherActiveAdmins: 0);
        UserRules.EnsureAdminRemains(new AppUser { Role = UserRole.Cashier, IsActive = true }, false, 0);
    }

    [Fact]
    public void FR_USR_06_Self_deactivation_is_rejected()
    {
        var id = Guid.NewGuid();

        var act = () => UserRules.EnsureNotSelf(id, id);

        act.Should().Throw<BusinessRuleException>().Which.Code.Should().Be("SELF_DEACTIVATION");
    }
}

public sealed class AuditChangesTests
{
    [Fact]
    public void FR_AUD_01_Only_changed_fields_are_recorded()
    {
        var changes = new AuditChanges()
            .Track("firstName", "Anna", "Anna")
            .Track("role", UserRole.Secretary, UserRole.Cashier)
            .Track("joinDate", new DateOnly(2026, 1, 1), new DateOnly(2026, 2, 1));

        changes.ToJson().Should().Be(
            """{"role":{"old":"Secretary","new":"Cashier"},"joinDate":{"old":"2026-01-01","new":"2026-02-01"}}""");
    }

    [Fact]
    public void FR_AUD_01_No_changes_is_empty() =>
        new AuditChanges().Track("x", 1, 1).IsEmpty.Should().BeTrue();
}
