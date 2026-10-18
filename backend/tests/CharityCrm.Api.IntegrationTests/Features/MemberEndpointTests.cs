using System.Net;
using System.Text;
using CharityCrm.Api.IntegrationTests.Infrastructure;
using CharityCrm.Core.Abstractions;
using CharityCrm.Core.Common;
using CharityCrm.Core.Entities;
using CharityCrm.Core.Services.Members;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace CharityCrm.Api.IntegrationTests.Features;

[Collection(ApiCollection.Name)]
public sealed class MemberEndpointTests(ApiFactory factory)
{
    private static readonly string AnyId = Guid.NewGuid().ToString();

    public static TheoryData<string, string> WriteRequests() => new()
    {
        { "POST", "/api/members" },
        { "POST", "/api/members/duplicate-check" },
        { "PUT", $"/api/members/{AnyId}" },
        { "POST", $"/api/members/{AnyId}/deregister" },
        { "POST", $"/api/members/{AnyId}/reactivate" },
        { "GET", "/api/members/export.csv" },
    };

    [Theory]
    [MemberData(nameof(WriteRequests))]
    public async Task FR_MEM_Cashier_is_read_only_and_gets_403_on_writes(string method, string url)
    {
        var cashier = await factory.LoginAsNewUserAsync(UserRole.Cashier);

        var response = method == "GET" ? await cashier.GetAsync(url) : await cashier.SendAsync(new HttpMethod(method), url, new { });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task FR_MEM_02_03_Cashier_can_list_and_view_members()
    {
        var secretary = await factory.LoginAsNewUserAsync(UserRole.Secretary);
        var member = await secretary.CreateMemberAsync();
        var cashier = await factory.LoginAsNewUserAsync(UserRole.Cashier);

        (await cashier.GetAsync("/api/members")).StatusCode.Should().Be(HttpStatusCode.OK);
        (await cashier.GetJsonAsync<MemberDto>($"/api/members/{member.Id}")).MemberNumber.Should().Be(member.MemberNumber);
    }

    [Fact]
    public async Task FR_MEM_01_Secretary_registers_member_with_generated_number_and_defaults()
    {
        var secretary = await factory.LoginAsNewUserAsync(UserRole.Secretary);

        var first = await secretary.CreateMemberAsync();
        var second = await secretary.CreateMemberAsync();

        first.MemberNumber.Should().MatchRegex(@"^M-\d{6}$");
        int.Parse(second.MemberNumber[2..]).Should().BeGreaterThan(int.Parse(first.MemberNumber[2..]));
        first.Status.Should().Be(MemberStatus.Active);
        first.Country.Should().Be("Österreich");

        var audited = await factory.WithDbAsync(db => db.AuditLogs.AnyAsync(a =>
            a.Action == AuditActions.MemberCreated && a.EntityId == first.Id.ToString()));
        audited.Should().BeTrue();
    }

    [Fact]
    public async Task FR_MEM_01_Join_date_defaults_to_today()
    {
        var secretary = await factory.LoginAsNewUserAsync(UserRole.Secretary);
        var body = TestData.NewMember();
        body["joinDate"] = null;

        var member = await secretary.PostJsonAsync<MemberDto>("/api/members", body, HttpStatusCode.Created);

        member.JoinDate.Should().Be(DateOnly.FromDateTime(DateTime.Now));
    }

    [Fact]
    public async Task FR_MEM_01_Invalid_fields_are_rejected()
    {
        var secretary = await factory.LoginAsNewUserAsync(UserRole.Secretary);
        var body = TestData.NewMember(email: "keine-mail");
        body["firstName"] = "";
        body["dateOfBirth"] = DateTime.Today.AddDays(1).ToString("yyyy-MM-dd");
        body["phone"] = new string('1', 31);

        var response = await secretary.PostAsync("/api/members", body);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await response.ReadProblemAsync()).Errors.Should().ContainKeys("firstName", "dateOfBirth", "email", "phone");
    }

    [Fact]
    public async Task FR_MEM_01_Duplicate_is_reported_and_can_be_confirmed()
    {
        var secretary = await factory.LoginAsNewUserAsync(UserRole.Secretary);
        var body = TestData.NewMember();
        var original = await secretary.PostJsonAsync<MemberDto>("/api/members", body, HttpStatusCode.Created);

        var check = await secretary.PostJsonAsync<List<DuplicateMemberDto>>("/api/members/duplicate-check", new
        {
            firstName = original.FirstName.ToUpperInvariant(), lastName = original.LastName, dateOfBirth = "1980-05-17",
        });
        check.Should().ContainSingle(d => d.Id == original.Id);

        var rejected = await secretary.PostAsync("/api/members", body);
        rejected.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await rejected.ReadProblemAsync()).Code.Should().Be("DUPLICATE_MEMBER");

        body["confirmDuplicate"] = true;
        var confirmed = await secretary.PostJsonAsync<MemberDto>("/api/members", body, HttpStatusCode.Created);
        confirmed.MemberNumber.Should().NotBe(original.MemberNumber);
    }

    [Fact]
    public async Task FR_MEM_02_Search_by_name_number_or_email_case_insensitive()
    {
        var secretary = await factory.LoginAsNewUserAsync(UserRole.Secretary);
        var unique = $"suche{Guid.NewGuid():N}"[..14];
        var member = await secretary.CreateMemberAsync(email: $"{unique}@example.at");

        foreach (var term in new[] { member.LastName[..12].ToUpperInvariant(), member.MemberNumber, unique.ToUpperInvariant(),
                     $"{member.FirstName} {member.LastName}" })
        {
            var result = await secretary.GetJsonAsync<PagedResult<MemberListItemDto>>($"/api/members?search={Uri.EscapeDataString(term)}");
            result.Items.Should().Contain(m => m.Id == member.Id, "search term {0} should match", term);
        }
    }

    [Fact]
    public async Task FR_MEM_02_List_is_paginated_with_25_per_page_and_filterable()
    {
        var secretary = await factory.LoginAsNewUserAsync(UserRole.Secretary);
        for (var i = 0; i < 26; i++)
        {
            await secretary.CreateMemberAsync(interval: "Yearly");
        }

        var page1 = await secretary.GetJsonAsync<PagedResult<MemberListItemDto>>("/api/members?status=Active&interval=Yearly");

        page1.PageSize.Should().Be(25);
        page1.Items.Should().HaveCount(25).And.OnlyContain(m => m.BillingInterval == BillingInterval.Yearly && m.Status == MemberStatus.Active);
        page1.TotalCount.Should().BeGreaterThan(25);
    }

    [Fact]
    public async Task FR_MEM_04_Edit_all_fields_and_audit_changes()
    {
        var secretary = await factory.LoginAsNewUserAsync(UserRole.Secretary);
        var member = await secretary.CreateMemberAsync();
        var body = TestData.ToUpdate(member);
        body["city"] = "Graz";
        body["billingInterval"] = "Yearly";

        var updated = await secretary.PutJsonAsync<MemberDto>($"/api/members/{member.Id}", body);

        updated.City.Should().Be("Graz");
        updated.BillingInterval.Should().Be(BillingInterval.Yearly);
        updated.MemberNumber.Should().Be(member.MemberNumber);
        var changes = await factory.WithDbAsync(db => db.AuditLogs
            .Where(a => a.Action == AuditActions.MemberUpdated && a.EntityId == member.Id.ToString())
            .Select(a => a.Changes).SingleAsync());
        changes.Should().Contain("Graz").And.Contain("Wien").And.Contain("billingInterval");
    }

    [Fact]
    public async Task NFR_REL_03_Concurrent_member_edit_returns_409()
    {
        var secretary = await factory.LoginAsNewUserAsync(UserRole.Secretary);
        var member = await secretary.CreateMemberAsync();
        var body = TestData.ToUpdate(member);
        await secretary.PutJsonAsync<MemberDto>($"/api/members/{member.Id}", body);

        body["city"] = "Linz";
        var stale = await secretary.PutAsync($"/api/members/{member.Id}", body);

        stale.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task FR_MEM_05_06_Deregister_and_reactivate()
    {
        var admin = await factory.LoginAsNewUserAsync(UserRole.Admin);
        var member = await admin.CreateMemberAsync();

        var inactive = await admin.PostJsonAsync<MemberDto>($"/api/members/{member.Id}/deregister",
            new { leaveDate = (string?)null, reason = "Umzug" });
        inactive.Status.Should().Be(MemberStatus.Inactive);
        inactive.LeaveDate.Should().Be(DateOnly.FromDateTime(DateTime.Now));
        inactive.LeaveReason.Should().Be("Umzug");

        var active = await admin.PostJsonAsync<MemberDto>($"/api/members/{member.Id}/reactivate", null);
        active.Status.Should().Be(MemberStatus.Active);
        active.LeaveDate.Should().BeNull();
        active.LeaveReason.Should().BeNull();
    }

    [Fact]
    public async Task FR_MEM_05_Leave_date_before_join_date_is_rejected()
    {
        var secretary = await factory.LoginAsNewUserAsync(UserRole.Secretary);
        var member = await secretary.CreateMemberAsync();

        var response = await secretary.PostAsync($"/api/members/{member.Id}/deregister", new { leaveDate = "2020-01-01" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task FR_MEM_07_Export_is_utf8_bom_semicolon_csv_of_the_filtered_list()
    {
        var secretary = await factory.LoginAsNewUserAsync(UserRole.Secretary);
        var member = await secretary.CreateMemberAsync();

        var response = await secretary.GetAsync($"/api/members/export.csv?search={member.MemberNumber}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be("text/csv");
        var bytes = await response.Content.ReadAsByteArrayAsync();
        bytes.Take(3).Should().Equal(0xEF, 0xBB, 0xBF);
        var lines = Encoding.UTF8.GetString(bytes[3..]).Split("\r\n", StringSplitOptions.RemoveEmptyEntries);
        lines.Should().HaveCount(2);
        lines[0].Should().StartWith("Mitgliedsnummer;Vorname;Nachname;Geburtsdatum");
        lines[1].Should().StartWith($"{member.MemberNumber};{member.FirstName};{member.LastName};17.05.1980;Hauptstraße 1;1010;Wien;Österreich");
    }
}
