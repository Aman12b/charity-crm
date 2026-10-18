using CharityCrm.Api.Setup;
using CharityCrm.Core.Services.Users;

namespace CharityCrm.Api.Features.Users;

public static class UserEndpoints
{
    public static void MapUserEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/users").WithTags("Users").RequireAuthorization(Policies.AdminOnly);

        group.MapGet("/", ([AsParameters] UserListQuery query, UserService users) => users.ListAsync(query));

        group.MapGet("/{id:guid}", (Guid id, UserService users) => users.GetAsync(id));

        group.MapPost("/", async (CreateUserRequest request, UserService users) =>
            {
                var user = await users.CreateAsync(request);
                return Results.Created($"/api/users/{user.Id}", user);
            })
            .WithValidation<CreateUserRequest>();

        group.MapPut("/{id:guid}", (Guid id, UpdateUserRequest request, UserService users) => users.UpdateAsync(id, request))
            .WithValidation<UpdateUserRequest>();

        group.MapPost("/{id:guid}/deactivate", (Guid id, UserService users) => users.DeactivateAsync(id));

        group.MapPost("/{id:guid}/reactivate", (Guid id, UserService users) => users.ReactivateAsync(id));

        group.MapPost("/{id:guid}/reset-password", (Guid id, ResetPasswordRequest request, UserService users) =>
                users.ResetPasswordAsync(id, request))
            .WithValidation<ResetPasswordRequest>();
    }
}
