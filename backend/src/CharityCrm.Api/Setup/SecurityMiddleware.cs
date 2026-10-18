using CharityCrm.Api.Features.Auth;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;

namespace CharityCrm.Api.Setup;

/// <summary>NFR-SEC-02: validates the X-XSRF-TOKEN header on every state-changing API request.</summary>
public sealed class AntiforgeryValidationMiddleware(RequestDelegate next, IAntiforgery antiforgery)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var method = context.Request.Method;
        var isSafe = HttpMethods.IsGet(method) || HttpMethods.IsHead(method) || HttpMethods.IsOptions(method) || HttpMethods.IsTrace(method);

        if (!isSafe && context.Request.Path.StartsWithSegments("/api"))
        {
            try
            {
                await antiforgery.ValidateRequestAsync(context);
            }
            catch (AntiforgeryValidationException)
            {
                await Problems.WriteAsync(context, StatusCodes.Status400BadRequest, "Ungültige Anfrage",
                    "Die Sitzung ist abgelaufen oder ungültig. Bitte laden Sie die Seite neu.", "ANTIFORGERY_INVALID");
                return;
            }
        }

        await next(context);
    }
}

/// <summary>FR-AUTH-06: while a password change is pending, only the endpoints needed for it are reachable.</summary>
public sealed class PasswordChangeRequiredMiddleware(RequestDelegate next)
{
    private static readonly string[] AllowedPaths =
        ["/api/auth/me", "/api/auth/change-password", "/api/auth/logout", "/api/auth/antiforgery", "/api/health"];

    public async Task InvokeAsync(HttpContext context)
    {
        var mustChange = context.User.Identity?.IsAuthenticated == true
            && context.User.HasClaim(AppClaimsPrincipalFactory.MustChangePasswordClaim, "true");

        if (mustChange
            && context.Request.Path.StartsWithSegments("/api")
            && !AllowedPaths.Any(p => context.Request.Path.Equals(p, StringComparison.OrdinalIgnoreCase)))
        {
            await Problems.WriteAsync(context, StatusCodes.Status403Forbidden, "Passwortänderung erforderlich",
                "Bitte ändern Sie zuerst Ihr Passwort.", "PASSWORD_CHANGE_REQUIRED");
            return;
        }

        await next(context);
    }
}

public static class Problems
{
    public static async Task WriteAsync(HttpContext context, int status, string title, string detail, string code)
    {
        context.Response.StatusCode = status;
        var problem = new ProblemDetails { Status = status, Title = title, Detail = detail };
        problem.Extensions["code"] = code;
        var service = context.RequestServices.GetRequiredService<IProblemDetailsService>();
        await service.WriteAsync(new ProblemDetailsContext { HttpContext = context, ProblemDetails = problem });
    }
}
