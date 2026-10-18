using CharityCrm.Core.Common;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CharityCrm.Api.Setup;

/// <summary>Translates exceptions into RFC 7807 ProblemDetails responses.</summary>
public sealed class DomainExceptionHandler(IProblemDetailsService problemDetails, ILogger<DomainExceptionHandler> logger)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken ct)
    {
        ProblemDetails problem = exception switch
        {
            InputValidationException e => new ValidationProblemDetails(e.Errors)
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Ungültige Eingaben",
                Detail = e.Message,
            },
            AuthenticationFailedException e => Problem(StatusCodes.Status401Unauthorized, "Anmeldung fehlgeschlagen", e.Message),
            NotFoundException e =>Problem(StatusCodes.Status404NotFound, "Nicht gefunden", e.Message),
            ConflictException e => Problem(StatusCodes.Status409Conflict, "Konflikt", e.Message),
            BusinessRuleException e => Problem(StatusCodes.Status422UnprocessableEntity, "Aktion nicht möglich", e.Message),
            DbUpdateConcurrencyException => Problem(StatusCodes.Status409Conflict, "Konflikt",
                "Der Datensatz wurde inzwischen von jemand anderem geändert. Bitte laden Sie die Seite neu."),
            BadHttpRequestException e => Problem(e.StatusCode, "Ungültige Anfrage", "Die Anfrage konnte nicht gelesen werden."),
            _ => Problem(StatusCodes.Status500InternalServerError, "Serverfehler", "Ein unerwarteter Fehler ist aufgetreten."),
        };

        if (exception is DomainException domain)
        {
            problem.Extensions["code"] = domain.Code;
        }
        else if (exception is DbUpdateConcurrencyException)
        {
            problem.Extensions["code"] = "CONCURRENCY_CONFLICT";
        }

        if (problem.Status >= 500)
        {
            logger.LogError(exception, "Unhandled exception");
        }

        context.Response.StatusCode = problem.Status ?? StatusCodes.Status500InternalServerError;
        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = context,
            ProblemDetails = problem,
            Exception = exception,
        });
    }

    private static ProblemDetails Problem(int status, string title, string detail) =>
        new() { Status = status, Title = title, Detail = detail };
}
