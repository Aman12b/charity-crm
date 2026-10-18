using FluentValidation;

namespace CharityCrm.Api.Setup;

/// <summary>Runs the registered FluentValidation validator for the request body of type <typeparamref name="T"/>.</summary>
public sealed class ValidationFilter<T> : IEndpointFilter where T : class
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var validator = context.HttpContext.RequestServices.GetRequiredService<IValidator<T>>();
        var argument = context.Arguments.OfType<T>().FirstOrDefault();
        if (argument is null)
        {
            return Results.Problem(statusCode: StatusCodes.Status400BadRequest, title: "Ungültige Anfrage",
                detail: "Der Anfrageinhalt fehlt.");
        }

        var result = await validator.ValidateAsync(argument, context.HttpContext.RequestAborted);
        if (!result.IsValid)
        {
            return Results.ValidationProblem(result.ToDictionary(), title: "Ungültige Eingaben",
                detail: "Die Eingaben sind ungültig.");
        }

        return await next(context);
    }
}

public static class ValidationFilterExtensions
{
    public static RouteHandlerBuilder WithValidation<T>(this RouteHandlerBuilder builder) where T : class =>
        builder.AddEndpointFilter<ValidationFilter<T>>().ProducesValidationProblem();
}
