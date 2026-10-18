using System.Net;
using System.Net.Http.Json;
using CharityCrm.Api.IntegrationTests.Infrastructure;
using CharityCrm.Core.Abstractions;
using CharityCrm.Core.Entities;
using CharityCrm.Core.Services.Auth;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;

namespace CharityCrm.Api.IntegrationTests.Features;

[Collection(ApiCollection.Name)]
public sealed class AuthTests(ApiFactory factory)
{
    [Fact]
    public async Task FR_AUTH_07_Seeded_admin_can_log_in_and_must_change_password()
    {
        var client = factory.CreateApiClient();

        var me = await client.PostJsonAsync<CurrentUserDto>("/api/auth/login",
            new { email = ApiFactory.SeedAdminEmail, password = ApiFactory.SeedAdminPassword });

        me.Role.Should().Be(UserRole.Admin);
        me.MustChangePassword.Should().BeTrue();
    }

    [Fact]
    public async Task FR_AUTH_01_Valid_credentials_create_a_session()
    {
        var user = await factory.CreateUserAsync(UserRole.Cashier);
        var client = factory.CreateApiClient();

        await client.LoginAsync(user.Email!, ApiFactory.DefaultPassword);
        var me = await client.GetJsonAsync<CurrentUserDto>("/api/auth/me");

        me.Id.Should().Be(user.Id);
        me.Role.Should().Be(UserRole.Cashier);
    }

    [Fact]
    public async Task FR_AUTH_01_Wrong_password_and_unknown_email_get_the_same_generic_error()
    {
        var user = await factory.CreateUserAsync(UserRole.Secretary);
        var client = factory.CreateApiClient();

        var wrongPassword = await client.PostAsync("/api/auth/login", new { email = user.Email, password = "Falsch1234567" });
        var unknownEmail = await client.PostAsync("/api/auth/login", new { email = "nobody@test.local", password = "Falsch1234567" });

        wrongPassword.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        unknownEmail.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await wrongPassword.ReadProblemAsync()).Detail.Should().Be(AuthService.InvalidCredentialsMessage);
        (await unknownEmail.ReadProblemAsync()).Detail.Should().Be(AuthService.InvalidCredentialsMessage);
    }

    [Fact]
    public async Task FR_AUTH_01_Deactivated_user_cannot_log_in()
    {
        var user = await factory.CreateUserAsync(UserRole.Cashier, isActive: false);
        var client = factory.CreateApiClient();

        var response = await client.PostAsync("/api/auth/login", new { email = user.Email, password = ApiFactory.DefaultPassword });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await response.ReadProblemAsync()).Detail.Should().Be(AuthService.InvalidCredentialsMessage);
    }

    [Fact]
    public async Task FR_AUTH_02_Logout_ends_the_session()
    {
        var client = await factory.LoginAsNewUserAsync(UserRole.Secretary);

        (await client.PostAsync("/api/auth/logout")).StatusCode.Should().Be(HttpStatusCode.NoContent);

        (await client.GetAsync("/api/auth/me")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Theory]
    [InlineData("/api/auth/me")]
    [InlineData("/api/users")]
    public async Task FR_AUTH_03_Unauthenticated_requests_return_401(string url)
    {
        var client = factory.CreateApiClient();

        (await client.GetAsync(url)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task FR_AUTH_04_Account_is_locked_after_5_failed_attempts()
    {
        var user = await factory.CreateUserAsync(UserRole.Cashier);
        var client = factory.CreateApiClient();

        for (var i = 0; i < 5; i++)
        {
            await client.PostAsync("/api/auth/login", new { email = user.Email, password = "Falsch1234567" });
        }
        var response = await client.PostAsync("/api/auth/login", new { email = user.Email, password = ApiFactory.DefaultPassword });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await response.ReadProblemAsync()).Code.Should().Be("LOCKED_OUT");
        var lockoutEnd = await factory.WithDbAsync(db => db.Users.Where(u => u.Id == user.Id).Select(u => u.LockoutEnd).SingleAsync());
        lockoutEnd.Should().BeCloseTo(DateTimeOffset.UtcNow.AddMinutes(15), TimeSpan.FromMinutes(1));
    }

    [Fact]
    public async Task FR_AUTH_05_Change_password_requires_the_current_password()
    {
        var user = await factory.CreateUserAsync(UserRole.Secretary);
        var client = factory.CreateApiClient();
        await client.LoginAsync(user.Email!, ApiFactory.DefaultPassword);

        var wrong = await client.PostAsync("/api/auth/change-password",
            new { currentPassword = "Falsch1234567", newPassword = "NeuesPasswort1" });
        wrong.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await wrong.ReadProblemAsync()).Errors.Should().ContainKey("currentPassword");

        var ok = await client.PostAsync("/api/auth/change-password",
            new { currentPassword = ApiFactory.DefaultPassword, newPassword = "NeuesPasswort1" });
        ok.StatusCode.Should().Be(HttpStatusCode.OK);

        // Current session stays valid, new password works for a fresh login.
        (await client.GetAsync("/api/auth/me")).StatusCode.Should().Be(HttpStatusCode.OK);
        await factory.CreateApiClient().LoginAsync(user.Email!, "NeuesPasswort1");
    }

    [Theory]
    [InlineData("kurz1")]          // too short
    [InlineData("nurbuchstaben")]  // no digit
    [InlineData("12345678901")]    // no letter
    public async Task NFR_SEC_03_New_password_must_meet_policy(string newPassword)
    {
        var client = await factory.LoginAsNewUserAsync(UserRole.Cashier);

        var response = await client.PostAsync("/api/auth/change-password",
            new { currentPassword = ApiFactory.DefaultPassword, newPassword });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await response.ReadProblemAsync()).Errors.Should().ContainKey("newPassword");
    }

    [Fact]
    public async Task FR_AUTH_06_Forced_password_change_blocks_everything_else_until_changed()
    {
        var user = await factory.CreateUserAsync(UserRole.Admin, mustChangePassword: true);
        var client = factory.CreateApiClient();
        await client.LoginAsync(user.Email!, ApiFactory.DefaultPassword);

        var blocked = await client.GetAsync("/api/users");
        blocked.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await blocked.ReadProblemAsync()).Code.Should().Be("PASSWORD_CHANGE_REQUIRED");
        (await client.GetAsync("/api/auth/me")).StatusCode.Should().Be(HttpStatusCode.OK);

        var changed = await client.PostJsonAsync<CurrentUserDto>("/api/auth/change-password",
            new { currentPassword = ApiFactory.DefaultPassword, newPassword = "NeuesPasswort1" });

        changed.MustChangePassword.Should().BeFalse();
        (await client.GetAsync("/api/users")).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task NFR_SEC_02_State_changing_request_without_antiforgery_token_is_rejected()
    {
        var client = factory.CreateApiClient();

        var response = await client.Http.PostAsJsonAsync("/api/auth/login",
            new { email = ApiFactory.SeedAdminEmail, password = ApiFactory.SeedAdminPassword });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await response.ReadProblemAsync()).Code.Should().Be("ANTIFORGERY_INVALID");
    }

    [Fact]
    public async Task NFR_SEC_02_Session_cookie_is_http_only_and_same_site_strict()
    {
        var user = await factory.CreateUserAsync(UserRole.Cashier);
        var client = factory.CreateApiClient();

        var response = await client.PostAsync("/api/auth/login", new { email = user.Email, password = ApiFactory.DefaultPassword });

        var cookie = response.Headers.GetValues("Set-Cookie").Single(c => c.StartsWith("charitycrm.auth=", StringComparison.Ordinal));
        cookie.Should().Contain("httponly").And.Contain("samesite=strict");
    }

    [Fact]
    public async Task FR_AUD_01_Successful_and_failed_logins_are_audited()
    {
        var user = await factory.CreateUserAsync(UserRole.Cashier);
        var client = factory.CreateApiClient();

        await client.PostAsync("/api/auth/login", new { email = user.Email, password = "Falsch1234567" });
        await client.LoginAsync(user.Email!, ApiFactory.DefaultPassword);

        var actions = await factory.WithDbAsync(db => db.AuditLogs
            .Where(a => a.UserId == user.Id).OrderBy(a => a.Id).Select(a => a.Action).ToListAsync());
        actions.Should().Equal(AuditActions.LoginFailed, AuditActions.LoginSucceeded);
        var lastLogin = await factory.WithDbAsync(db => db.Users.Where(u => u.Id == user.Id).Select(u => u.LastLoginAt).SingleAsync());
        lastLogin.Should().NotBeNull();
    }

    [Fact]
    public async Task NFR_SEC_07_Login_is_rate_limited_per_client()
    {
        using var limited = factory.WithWebHostBuilder(b => b.UseSetting("RateLimiting:LoginPermitsPerMinute", "3"));
        var client = new ApiClient(limited.CreateClient());

        var statuses = new List<HttpStatusCode>();
        for (var i = 0; i < 4; i++)
        {
            statuses.Add((await client.PostAsync("/api/auth/login", new { email = "x@test.local", password = "Falsch1234567" })).StatusCode);
        }

        statuses.Should().Equal(HttpStatusCode.Unauthorized, HttpStatusCode.Unauthorized, HttpStatusCode.Unauthorized,
            HttpStatusCode.TooManyRequests);
    }
}
