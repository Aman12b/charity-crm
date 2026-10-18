using CharityCrm.Core.Entities;
using CharityCrm.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using CharityCrm.Core.Abstractions;
using CharityCrm.Core.Services.Email;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

namespace CharityCrm.Api.IntegrationTests.Infrastructure;

/// <summary>
/// Hosts the API in-memory against a real PostgreSQL started with Testcontainers.
/// One container is shared by all tests in the "api" collection, so tests create their own data.
/// </summary>
public class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string SeedAdminEmail = "seed-admin@test.local";
    public const string SeedAdminPassword = "SeedAdmin12345";
    public const string DefaultPassword = "Passwort12345";

    private readonly PostgreSqlContainer _db = new PostgreSqlBuilder("postgres:17-alpine").Build();
    private readonly string _dataPath = Path.Combine(Path.GetTempPath(), "charitycrm-tests", Guid.NewGuid().ToString("N"));

    public string DataPath => _dataPath;

    /// <summary>Replaces SMTP for all tests; the outbox is processed explicitly via <see cref="ProcessOutboxAsync"/>.</summary>
    public FakeEmailSender Email { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:Default", _db.GetConnectionString());
        builder.UseSetting("Storage:InvoicePath", Path.Combine(_dataPath, "invoices"));
        builder.UseSetting("DataProtection:KeyPath", Path.Combine(_dataPath, "keys"));
        builder.UseSetting("Seed:AdminEmail", SeedAdminEmail);
        builder.UseSetting("Seed:AdminPassword", SeedAdminPassword);
        // All tests share one client IP; the limit itself is tested with a dedicated host.
        builder.UseSetting("RateLimiting:LoginPermitsPerMinute", "100000");
        builder.UseSetting("Email:WorkerEnabled", "false");
        builder.ConfigureTestServices(services => services.AddSingleton<IEmailSender>(Email));
    }

    public async Task InitializeAsync() => await _db.StartAsync();

    public new async Task DisposeAsync()
    {
        await base.DisposeAsync();
        await _db.DisposeAsync();
        try { Directory.Delete(_dataPath, recursive: true); } catch (IOException) { } catch (UnauthorizedAccessException) { }
    }

    public ApiClient CreateApiClient() =>
        new(CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true, AllowAutoRedirect = false }));

    /// <summary>Creates a user directly in the database (bypassing the API).</summary>
    public async Task<AppUser> CreateUserAsync(UserRole role, bool mustChangePassword = false, bool isActive = true,
        string password = DefaultPassword)
    {
        await using var scope = Services.CreateAsyncScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        var email = $"{role.ToString().ToLowerInvariant()}-{Guid.NewGuid():N}@test.local";
        var user = new AppUser
        {
            Id = Guid.NewGuid(),
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            FirstName = "Test",
            LastName = role.ToString(),
            Role = role,
            IsActive = isActive,
            MustChangePassword = mustChangePassword,
            CreatedAt = DateTimeOffset.UtcNow,
        };
        var result = await userManager.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(string.Join(", ", result.Errors.Select(e => e.Description)));
        }
        return user;
    }

    /// <summary>Creates a user of the given role and returns a client logged in as that user.</summary>
    public async Task<ApiClient> LoginAsNewUserAsync(UserRole role)
    {
        var user = await CreateUserAsync(role);
        var client = CreateApiClient();
        await client.LoginAsync(user.Email!, DefaultPassword);
        return client;
    }

    public async Task<T> WithDbAsync<T>(Func<AppDbContext, Task<T>> action)
    {
        await using var scope = Services.CreateAsyncScope();
        return await action(scope.ServiceProvider.GetRequiredService<AppDbContext>());
    }

    /// <summary>Runs the outbox processor until no due message is left.</summary>
    public async Task ProcessOutboxAsync()
    {
        int processed;
        do
        {
            await using var scope = Services.CreateAsyncScope();
            processed = await scope.ServiceProvider.GetRequiredService<EmailOutboxProcessor>().ProcessBatchAsync();
        } while (processed > 0);
    }

    public async Task WithDbAsync(Func<AppDbContext, Task> action)
    {
        await using var scope = Services.CreateAsyncScope();
        await action(scope.ServiceProvider.GetRequiredService<AppDbContext>());
    }
}

[CollectionDefinition(Name)]
public sealed class ApiCollection : ICollectionFixture<ApiFactory>
{
    public const string Name = "api";
}
