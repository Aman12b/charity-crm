using System.Net;
using CharityCrm.Api.IntegrationTests.Infrastructure;
using CharityCrm.Core.Abstractions;
using CharityCrm.Core.Common;
using CharityCrm.Core.Entities;
using CharityCrm.Core.Services.Auth;
using CharityCrm.Core.Services.Users;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace CharityCrm.Api.IntegrationTests.Features;

[Collection(ApiCollection.Name)]
public sealed class UserEndpointTests(ApiFactory factory)
{
    private static readonly string AnyId = Guid.NewGuid().ToString();

    public static TheoryData<UserRole, string, string> ForbiddenRequests()
    {
        var data = new TheoryData<UserRole, string, string>();
        foreach (var role in new[] { UserRole.Cashier, UserRole.Secretary })
        {
            data.Add(role, "GET", "/api/users");
            data.Add(role, "GET", $"/api/users/{AnyId}");
            data.Add(role, "POST", "/api/users");
            data.Add(role, "PUT", $"/api/users/{AnyId}");
            data.Add(role, "POST", $"/api/users/{AnyId}/deactivate");
            data.Add(role, "POST", $"/api/users/{AnyId}/reactivate");
            data.Add(role, "POST", $"/api/users/{AnyId}/reset-password");
        }
        return data;
    }

    [Theory]
    [MemberData(nameof(ForbiddenRequests))]
    public async Task NFR_SEC_01_User_endpoints_return_403_for_non_admins(UserRole role, string method, string url)
    {
        var client = await factory.LoginAsNewUserAsync(role);

        var response = method == "GET"
            ? await client.GetAsync(url)
            : await client.SendAsync(new HttpMethod(method), url, new { });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task FR_USR_01_Admin_creates_active_user_flagged_for_password_change()
    {
        var admin = await factory.LoginAsNewUserAsync(UserRole.Admin);
        var email = $"neu-{Guid.NewGuid():N}@test.local";

        var created = await admin.PostJsonAsync<UserDto>("/api/users",
            new { email, firstName = "Anna", lastName = "Kassierin", role = "Cashier", temporaryPassword = "Temporaer12345" },
            HttpStatusCode.Created);

        created.IsActive.Should().BeTrue();
        created.MustChangePassword.Should().BeTrue();
        created.Role.Should().Be(UserRole.Cashier);

        var login = await factory.CreateApiClient().PostJsonAsync<CurrentUserDto>("/api/auth/login",
            new { email, password = "Temporaer12345" });
        login.MustChangePassword.Should().BeTrue();

        var audited = await factory.WithDbAsync(db => db.AuditLogs.AnyAsync(a =>
            a.Action == AuditActions.UserCreated && a.EntityId == created.Id.ToString()));
        audited.Should().BeTrue();
    }

    [Fact]
    public async Task FR_USR_01_Email_must_be_unique_case_insensitive()
    {
        var existing = await factory.CreateUserAsync(UserRole.Secretary);
        var admin = await factory.LoginAsNewUserAsync(UserRole.Admin);

        var response = await admin.PostAsync("/api/users", new
        {
            email = existing.Email!.ToUpperInvariant(), firstName = "X", lastName = "Y", role = "Secretary",
            temporaryPassword = "Temporaer12345",
        });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await response.ReadProblemAsync()).Code.Should().Be("DUPLICATE_EMAIL");
    }

    [Fact]
    public async Task FR_USR_01_Invalid_input_returns_field_errors()
    {
        var admin = await factory.LoginAsNewUserAsync(UserRole.Admin);

        var response = await admin.PostAsync("/api/users",
            new { email = "keine-mail", firstName = "", lastName = "Y", role = "Admin", temporaryPassword = "kurz" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await response.ReadProblemAsync()).Errors.Should().ContainKeys("email", "firstName", "temporaryPassword");
    }

    [Fact]
    public async Task FR_USR_02_List_filters_by_role_and_status()
    {
        var inactiveSecretary = await factory.CreateUserAsync(UserRole.Secretary, isActive: false);
        var admin = await factory.LoginAsNewUserAsync(UserRole.Admin);

        var result = await admin.GetJsonAsync<PagedResult<UserDto>>("/api/users?role=Secretary&isActive=false&pageSize=100");

        result.Items.Should().OnlyContain(u => u.Role == UserRole.Secretary && !u.IsActive);
        result.Items.Should().Contain(u => u.Id == inactiveSecretary.Id);
    }

    [Fact]
    public async Task FR_USR_03_Admin_edits_name_email_and_role()
    {
        var user = await factory.CreateUserAsync(UserRole.Secretary);
        var admin = await factory.LoginAsNewUserAsync(UserRole.Admin);
        var current = await admin.GetJsonAsync<UserDto>($"/api/users/{user.Id}");
        var newEmail = $"geaendert-{Guid.NewGuid():N}@test.local";

        var updated = await admin.PutJsonAsync<UserDto>($"/api/users/{user.Id}",
            new { email = newEmail, firstName = "Neu", lastName = "Name", role = "Cashier", version = current.Version });

        updated.Email.Should().Be(newEmail);
        updated.Role.Should().Be(UserRole.Cashier);
        updated.FirstName.Should().Be("Neu");

        var changes = await factory.WithDbAsync(db => db.AuditLogs
            .Where(a => a.Action == AuditActions.UserUpdated && a.EntityId == user.Id.ToString())
            .Select(a => a.Changes).SingleAsync());
        changes.Should().Contain("\"role\"").And.Contain("Secretary").And.Contain("Cashier");
    }

    [Fact]
    public async Task NFR_REL_03_Editing_with_a_stale_version_returns_409()
    {
        var user = await factory.CreateUserAsync(UserRole.Secretary);
        var admin = await factory.LoginAsNewUserAsync(UserRole.Admin);
        var current = await admin.GetJsonAsync<UserDto>($"/api/users/{user.Id}");
        await admin.PutJsonAsync<UserDto>($"/api/users/{user.Id}",
            new { email = user.Email, firstName = "Eins", lastName = "X", role = "Secretary", version = current.Version });

        var stale = await admin.PutAsync($"/api/users/{user.Id}",
            new { email = user.Email, firstName = "Zwei", lastName = "X", role = "Secretary", version = current.Version });

        stale.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task FR_USR_04_Deactivation_invalidates_sessions_and_reactivation_allows_login()
    {
        var user = await factory.CreateUserAsync(UserRole.Cashier);
        var userClient = factory.CreateApiClient();
        await userClient.LoginAsync(user.Email!, ApiFactory.DefaultPassword);
        var admin = await factory.LoginAsNewUserAsync(UserRole.Admin);

        var deactivated = await admin.PostJsonAsync<UserDto>($"/api/users/{user.Id}/deactivate", null);
        deactivated.IsActive.Should().BeFalse();
        (await userClient.GetAsync("/api/auth/me")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        await admin.PostJsonAsync<UserDto>($"/api/users/{user.Id}/reactivate", null);
        await factory.CreateApiClient().LoginAsync(user.Email!, ApiFactory.DefaultPassword);
    }

    [Fact]
    public async Task FR_USR_05_Reset_password_sets_temporary_password_and_forces_change()
    {
        var user = await factory.CreateUserAsync(UserRole.Secretary);
        var admin = await factory.LoginAsNewUserAsync(UserRole.Admin);

        var reset = await admin.PostJsonAsync<UserDto>($"/api/users/{user.Id}/reset-password",
            new { temporaryPassword = "Temporaer12345" });

        reset.MustChangePassword.Should().BeTrue();
        var login = await factory.CreateApiClient().PostJsonAsync<CurrentUserDto>("/api/auth/login",
            new { email = user.Email, password = "Temporaer12345" });
        login.MustChangePassword.Should().BeTrue();
    }

    [Fact]
    public async Task FR_USR_06_Admin_cannot_deactivate_themselves()
    {
        var adminUser = await factory.CreateUserAsync(UserRole.Admin);
        var admin = factory.CreateApiClient();
        await admin.LoginAsync(adminUser.Email!, ApiFactory.DefaultPassword);

        var response = await admin.PostAsync($"/api/users/{adminUser.Id}/deactivate");

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        (await response.ReadProblemAsync()).Code.Should().Be("SELF_DEACTIVATION");
    }

    [Fact]
    public async Task FR_USR_06_Role_change_that_would_leave_no_active_admin_is_rejected()
    {
        var adminUser = await factory.CreateUserAsync(UserRole.Admin);
        var admin = factory.CreateApiClient();
        await admin.LoginAsync(adminUser.Email!, ApiFactory.DefaultPassword);

        // Make this admin the only active one for the duration of the test.
        var otherAdminIds = await factory.WithDbAsync(db => db.Users
            .Where(u => u.Id != adminUser.Id && u.IsActive && u.Role == UserRole.Admin).Select(u => u.Id).ToListAsync());
        await SetActiveAsync(otherAdminIds, false);
        try
        {
            var current = await admin.GetJsonAsync<UserDto>($"/api/users/{adminUser.Id}");
            var response = await admin.PutAsync($"/api/users/{adminUser.Id}", new
            {
                email = adminUser.Email, firstName = "Test", lastName = "Admin", role = "Cashier", version = current.Version,
            });

            response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
            (await response.ReadProblemAsync()).Code.Should().Be("LAST_ADMIN");
        }
        finally
        {
            await SetActiveAsync(otherAdminIds, true);
        }
    }

    private Task SetActiveAsync(List<Guid> ids, bool active) => factory.WithDbAsync(db =>
        db.Users.Where(u => ids.Contains(u.Id)).ExecuteUpdateAsync(s => s.SetProperty(u => u.IsActive, active)));
}
