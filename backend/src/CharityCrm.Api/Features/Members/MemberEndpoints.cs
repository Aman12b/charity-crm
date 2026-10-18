using CharityCrm.Api.Setup;
using CharityCrm.Core.Abstractions;
using CharityCrm.Core.Services.Members;

namespace CharityCrm.Api.Features.Members;

public static class MemberEndpoints
{
    public static void MapMemberEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/members").WithTags("Members");
        var read = group.MapGroup("").RequireAuthorization(Policies.MemberRead);
        var write = group.MapGroup("").RequireAuthorization(Policies.MemberWrite);

        read.MapGet("/", ([AsParameters] MemberListQuery query, MemberService members) => members.ListAsync(query));

        write.MapGet("/export.csv", async ([AsParameters] MemberListQuery query, MemberService members, IClock clock) =>
            Results.File(await members.ExportCsvAsync(query), "text/csv; charset=utf-8", DownloadName.For(clock, "Mitglieder", "csv")));

        read.MapGet("/{id:guid}", (Guid id, MemberService members) => members.GetAsync(id));

        write.MapPost("/duplicate-check", (DuplicateCheckRequest request, MemberService members) =>
                members.FindDuplicatesAsync(request))
            .WithValidation<DuplicateCheckRequest>();

        write.MapPost("/", async (CreateMemberRequest request, MemberService members) =>
            {
                var member = await members.CreateAsync(request);
                return Results.Created($"/api/members/{member.Id}", member);
            })
            .WithValidation<CreateMemberRequest>();

        write.MapPut("/{id:guid}", (Guid id, UpdateMemberRequest request, MemberService members) =>
                members.UpdateAsync(id, request))
            .WithValidation<UpdateMemberRequest>();

        write.MapPost("/{id:guid}/deregister", (Guid id, DeregisterMemberRequest request, MemberService members) =>
                members.DeregisterAsync(id, request))
            .WithValidation<DeregisterMemberRequest>();

        write.MapPost("/{id:guid}/reactivate", (Guid id, MemberService members) => members.ReactivateAsync(id));
    }
}
