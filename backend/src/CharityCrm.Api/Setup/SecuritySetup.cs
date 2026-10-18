using System.Threading.RateLimiting;
using CharityCrm.Api.Features.Auth;
using CharityCrm.Core.Abstractions;
using CharityCrm.Core.Entities;
using CharityCrm.Core.Identity;
using CharityCrm.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;

namespace CharityCrm.Api.Setup;

public sealed class AuthOptions
{
    public const string Section = "Auth";

    /// <summary>true when served over HTTPS (cookies get the Secure flag).</summary>
    public bool CookieSecure { get; set; }
}

public sealed class RateLimitOptions
{
    public const string Section = "RateLimiting";

    public int LoginPermitsPerMinute { get; set; } = 10;
}

public static class SecuritySetup
{
    public const string LoginRateLimitPolicy = "login";

    public static IServiceCollection AddAppSecurity(this IServiceCollection services, IConfiguration configuration)
    {
        var cookieSecure = configuration.GetSection(AuthOptions.Section).Get<AuthOptions>()?.CookieSecure ?? false;
        var securePolicy = cookieSecure ? CookieSecurePolicy.Always : CookieSecurePolicy.SameAsRequest;

        // Identity: FR-AUTH-01..04, NFR-SEC-03.
        services
            .AddIdentityCore<AppUser>(o =>
            {
                o.User.RequireUniqueEmail = true;
                o.Password.RequiredLength = PasswordPolicy.MinLength;
                o.Password.RequireDigit = true;
                o.Password.RequireLowercase = false;
                o.Password.RequireUppercase = false;
                o.Password.RequireNonAlphanumeric = false;
                o.Password.RequiredUniqueChars = 1;
                o.Lockout.AllowedForNewUsers = true;
                o.Lockout.MaxFailedAccessAttempts = 5;
                o.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            })
            .AddEntityFrameworkStores<AppDbContext>()
            .AddSignInManager<AppSignInManager>()
            .AddClaimsPrincipalFactory<AppClaimsPrincipalFactory>()
            .AddErrorDescriber<GermanIdentityErrorDescriber>()
            .AddPasswordValidator<LetterRequiredPasswordValidator>()
            .AddDefaultTokenProviders();

        services.AddAuthentication(IdentityConstants.ApplicationScheme).AddIdentityCookies();
        services.ConfigureApplicationCookie(o =>
        {
            o.Cookie.Name = "charitycrm.auth";
            o.Cookie.HttpOnly = true;
            o.Cookie.SameSite = SameSiteMode.Strict;
            o.Cookie.SecurePolicy = securePolicy;
            o.ExpireTimeSpan = TimeSpan.FromMinutes(30); // FR-AUTH-03: 30 min sliding
            o.SlidingExpiration = true;
            // API: answer with status codes instead of redirects. The events object is kept as-is because
            // Identity hooks the security stamp validation into OnValidatePrincipal.
            o.Events.OnRedirectToLogin = ctx => { ctx.Response.StatusCode = StatusCodes.Status401Unauthorized; return Task.CompletedTask; };
            o.Events.OnRedirectToAccessDenied = ctx => { ctx.Response.StatusCode = StatusCodes.Status403Forbidden; return Task.CompletedTask; };
        });

        // FR-USR-04 requires that deactivation ends sessions immediately, so the security stamp
        // (and IsActive) is checked on every request. At ~10 concurrent users this is one cheap lookup.
        services.Configure<SecurityStampValidatorOptions>(o => o.ValidationInterval = TimeSpan.Zero);

        Policies.Configure(services.AddAuthorizationBuilder());

        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, CurrentUser>();
        services.AddScoped<ISignInService, SignInService>();

        // NFR-SEC-02: keys survive container restarts.
        var keyPath = configuration["DataProtection:KeyPath"] ?? Path.Combine(AppContext.BaseDirectory, "keys");
        services.AddDataProtection()
            .SetApplicationName("CharityCrm")
            .PersistKeysToFileSystem(new DirectoryInfo(keyPath));

        services.AddAntiforgery(o =>
        {
            o.HeaderName = "X-XSRF-TOKEN";
            o.Cookie.Name = "charitycrm.xsrf";
            o.Cookie.HttpOnly = true;
            o.Cookie.SameSite = SameSiteMode.Strict;
            o.Cookie.SecurePolicy = securePolicy;
        });

        // nginx in front of the API (ARCHITECTURE §9).
        services.Configure<ForwardedHeadersOptions>(o =>
        {
            o.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            o.KnownIPNetworks.Clear();
            o.KnownProxies.Clear();
        });

        // NFR-SEC-07: login limited per client IP.
        services.Configure<RateLimitOptions>(configuration.GetSection(RateLimitOptions.Section));
        services.AddRateLimiter(o =>
        {
            o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            o.OnRejected = (ctx, _) => new ValueTask(Problems.WriteAsync(ctx.HttpContext,
                StatusCodes.Status429TooManyRequests, "Zu viele Anfragen",
                "Zu viele Anmeldeversuche. Bitte warten Sie eine Minute.", "RATE_LIMITED"));
            o.AddPolicy(LoginRateLimitPolicy, http =>
            {
                var permits = http.RequestServices.GetRequiredService<Microsoft.Extensions.Options.IOptions<RateLimitOptions>>()
                    .Value.LoginPermitsPerMinute;
                return RateLimitPartition.GetFixedWindowLimiter(
                    http.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    _ => new FixedWindowRateLimiterOptions { PermitLimit = permits, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 });
            });
        });

        return services;
    }
}
