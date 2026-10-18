using CharityCrm.Core.Abstractions;
using CharityCrm.Core.Entities;
using CharityCrm.Core.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CharityCrm.Infrastructure.Persistence;

/// <summary>Applies pending migrations (NFR-OPS-02) and seeds the initial admin (FR-AUTH-07).</summary>
public static class DatabaseInitializer
{
    public static async Task InitializeAsync(IServiceProvider services, CancellationToken ct = default)
    {
        await using var scope = services.CreateAsyncScope();
        var sp = scope.ServiceProvider;
        var logger = sp.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(DatabaseInitializer));
        var db = sp.GetRequiredService<AppDbContext>();

        logger.LogInformation("Applying database migrations");
        await db.Database.MigrateAsync(ct);

        await SeedAdminAsync(sp, db, logger, ct);
    }

    private static async Task SeedAdminAsync(IServiceProvider sp, AppDbContext db, ILogger logger, CancellationToken ct)
    {
        if (await db.Users.AnyAsync(ct))
        {
            return;
        }

        var options = sp.GetRequiredService<IOptions<SeedOptions>>().Value;
        if (string.IsNullOrWhiteSpace(options.AdminEmail) || string.IsNullOrWhiteSpace(options.AdminPassword))
        {
            logger.LogWarning("Database has no users and the seed admin settings are missing");
            return;
        }

        var admin = new AppUser
        {
            Id = Guid.NewGuid(),
            UserName = options.AdminEmail,
            Email = options.AdminEmail,
            EmailConfirmed = true,
            FirstName = "System",
            LastName = "Administrator",
            Role = UserRole.Admin,
            IsActive = true,
            MustChangePassword = true,
            CreatedAt = sp.GetRequiredService<IClock>().UtcNow,
        };

        var userManager = sp.GetRequiredService<UserManager<AppUser>>();
        (await userManager.CreateAsync(admin, options.AdminPassword)).EnsureSucceeded();
        logger.LogInformation("Initial admin {UserId} created", admin.Id);
    }
}
