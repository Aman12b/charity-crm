using System.Security.Claims;
using CharityCrm.Api.Setup;
using CharityCrm.Core.Services.Auth;
using Microsoft.AspNetCore.Antiforgery;

namespace CharityCrm.Api.Features.Auth;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth").WithTags("Auth");

        group.MapPost("/login", (LoginRequest request, AuthService auth) => auth.LoginAsync(request))
            .AllowAnonymous()
            .RequireRateLimiting(SecuritySetup.LoginRateLimitPolicy)
            .WithValidation<LoginRequest>();

        group.MapPost("/logout", async (ClaimsPrincipal user, AuthService auth) =>
            {
                await auth.LogoutAsync(user.GetUserId());
                return Results.NoContent();
            })
            .RequireAuthorization();

        group.MapGet("/me", (ClaimsPrincipal user, AuthService auth) => auth.GetCurrentAsync(user.GetUserId()))
            .RequireAuthorization();

        group.MapPost("/change-password", (ChangePasswordRequest request, ClaimsPrincipal user, AuthService auth) =>
                auth.ChangePasswordAsync(user.GetUserId(), request))
            .RequireAuthorization()
            .WithValidation<ChangePasswordRequest>();

        // NFR-SEC-02: the SPA fetches a request token and sends it as X-XSRF-TOKEN.
        group.MapGet("/antiforgery", (HttpContext context, IAntiforgery antiforgery) =>
            {
                var tokens = antiforgery.GetAndStoreTokens(context);
                return Results.Ok(new { token = tokens.RequestToken });
            })
            .AllowAnonymous();
    }
}
