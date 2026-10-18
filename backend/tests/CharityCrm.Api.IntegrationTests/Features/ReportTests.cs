using System.Net;
using System.Text;
using CharityCrm.Api.IntegrationTests.Infrastructure;
using CharityCrm.Core.Abstractions;
using CharityCrm.Core.Common;
using CharityCrm.Core.Entities;
using CharityCrm.Core.Services.Audit;
using CharityCrm.Core.Services.Invoices;
using CharityCrm.Core.Services.Members;
using CharityCrm.Core.Services.Reports;
using FluentAssertions;

namespace CharityCrm.Api.IntegrationTests.Features;

[Collection(ApiCollection.Name)]
public sealed class ReportTests(ApiFactory factory)
{
    [Theory]
    [InlineData(UserRole.Secretary, "/api/reports/open-contributions")]
    [InlineData(UserRole.Secretary, "/api/reports/payment-summary")]
    [InlineData(UserRole.Secretary, "/api/reports/payment-summary.csv")]
    [InlineData(UserRole.Secretary, "/api/audit")]
    [InlineData(UserRole.Cashier, "/api/audit")]
    public async Task NFR_SEC_01_Report_and_audit_endpoints_return_403_for_unauthorized_roles(UserRole role, string url)
    {
        var client = await factory.LoginAsNewUserAsync(role);

        (await client.GetAsync(url)).StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task FR_REP_01_Dashboard_content_depends_on_role()
    {
        var secretary = await factory.LoginAsNewUserAsync(UserRole.Secretary);
        var cashier = await factory.LoginAsNewUserAsync(UserRole.Cashier);
        await secretary.CreateMemberAsync();

        var forSecretary = await secretary.GetJsonAsync<DashboardDto>("/api/reports/dashboard");
        var forCashier = await cashier.GetJsonAsync<DashboardDto>("/api/reports/dashboard");

        forSecretary.ActiveMembers.Should().BePositive();
        forSecretary.PaymentsThisMonthCount.Should().BeNull();
        forSecretary.PaymentsThisMonthSum.Should().BeNull();
        forSecretary.FailedEmails.Should().BeNull();
        forSecretary.OpenContributions.Should().BeNull();
        forCashier.PaymentsThisMonthCount.Should().NotBeNull();
        forCashier.OpenContributions.Should().NotBeNull();
    }

    [Fact]
    public async Task FR_REP_02_Open_contributions_for_monthly_and_yearly_payers()
    {
        var admin = await factory.LoginAsNewUserAsync(UserRole.Admin);
        var tag = "Offen" + Guid.NewGuid().ToString("N")[..8];

        var paidMarch = await CreateAsync(admin, tag, "A", "Monthly", "2002-01-01");
        var paidFebruary = await CreateAsync(admin, tag, "B", "Monthly", "2002-01-01");
        var cancelledMarch = await CreateAsync(admin, tag, "C", "Monthly", "2002-01-01");
        var paidYear = await CreateAsync(admin, tag, "D", "Yearly", "2002-01-01");
        var unpaidYear = await CreateAsync(admin, tag, "E", "Yearly", "2002-01-01");
        var joinedLater = await CreateAsync(admin, tag, "F", "Monthly", "2002-04-01");
        var inactive = await CreateAsync(admin, tag, "G", "Monthly", "2002-01-01");

        await FinanceTests.RecordAsync(admin, paidMarch.Id, 2002, 3);
        await FinanceTests.RecordAsync(admin, paidFebruary.Id, 2002, 2);
        var cancelled = await FinanceTests.RecordAsync(admin, cancelledMarch.Id, 2002, 3);
        await admin.PostJsonAsync<InvoiceDto>($"/api/invoices/{cancelled.InvoiceId}/cancel", new { reason = "Test" });
        await FinanceTests.RecordAsync(admin, paidYear.Id, 2002, null, 120m);
        await admin.PostJsonAsync<MemberDto>($"/api/members/{inactive.Id}/deregister", new { reason = "Test" });

        var result = await admin.GetJsonAsync<OpenContributionsResult>(
            $"/api/reports/open-contributions?year=2002&month=3&search={tag}&pageSize=100");

        result.Items.Select(i => i.MemberId).Should().BeEquivalentTo([paidFebruary.Id, cancelledMarch.Id, unpaidYear.Id]);
        var monthly = result.Items.Single(i => i.MemberId == paidFebruary.Id);
        monthly.PeriodStart.Should().Be(new DateOnly(2002, 3, 1));
        monthly.PeriodEnd.Should().Be(new DateOnly(2002, 3, 31));
        var yearly = result.Items.Single(i => i.MemberId == unpaidYear.Id);
        yearly.PeriodStart.Should().Be(new DateOnly(2002, 1, 1));
        yearly.PeriodEnd.Should().Be(new DateOnly(2002, 12, 31));
        result.Items.Should().NotContain(i => i.MemberId == joinedLater.Id || i.MemberId == inactive.Id);
    }

    [Fact]
    public async Task FR_REP_03_Payment_summary_per_month_excludes_cancelled_payments()
    {
        var admin = await factory.LoginAsNewUserAsync(UserRole.Admin);
        var tag = "Summe" + Guid.NewGuid().ToString("N")[..8];
        var member = await CreateAsync(admin, tag, "A", "Monthly", "2003-01-01");

        await Record(admin, member.Id, 1, 10m, "2003-02-10");
        await Record(admin, member.Id, 2, 5m, "2003-02-20");
        await Record(admin, member.Id, 3, 7m, "2003-05-01");
        var cancelled = await Record(admin, member.Id, 4, 100m, "2003-05-02");
        await admin.PostJsonAsync<InvoiceDto>($"/api/invoices/{cancelled.InvoiceId}/cancel", new { reason = "Test" });

        var summary = await admin.GetJsonAsync<PaymentSummaryDto>("/api/reports/payment-summary?year=2003");

        summary.Months.Should().HaveCount(12);
        summary.Months.Single(m => m.Month == 2).Should().Be(new MonthlySumDto(2, 2, 15m));
        summary.Months.Single(m => m.Month == 5).Should().Be(new MonthlySumDto(5, 1, 7m));
        summary.TotalCount.Should().Be(3);
        summary.TotalSum.Should().Be(22m);

        var csv = await admin.GetAsync("/api/reports/payment-summary.csv?year=2003");
        var lines = Encoding.UTF8.GetString((await csv.Content.ReadAsByteArrayAsync())[3..]).Split("\r\n", StringSplitOptions.RemoveEmptyEntries);
        lines.Should().HaveCount(14);
        lines[0].Should().Be("Monat;Anzahl;Summe (EUR)");
        lines[2].Should().Be("Februar 2003;2;15,00");
        lines[^1].Should().Be("Gesamt 2003;3;22,00");
    }

    [Fact]
    public async Task FR_AUD_02_Admin_filters_audit_log_by_entity_and_date()
    {
        var admin = await factory.LoginAsNewUserAsync(UserRole.Admin);
        var member = await admin.CreateMemberAsync();
        var today = DateTime.Today.ToString("yyyy-MM-dd");

        var result = await admin.GetJsonAsync<PagedResult<AuditLogDto>>(
            $"/api/audit?entityType=Member&entityId={member.Id}&from={today}&to={today}");

        result.Items.Should().ContainSingle().Which.Should().Match<AuditLogDto>(a =>
            a.Action == AuditActions.MemberCreated && a.UserName == "Test Admin" && a.Changes!.Contains(member.MemberNumber));

        var yesterday = DateTime.Today.AddDays(-1).ToString("yyyy-MM-dd");
        (await admin.GetJsonAsync<PagedResult<AuditLogDto>>($"/api/audit?entityId={member.Id}&to={yesterday}"))
            .Items.Should().BeEmpty();
    }

    private static async Task<MemberDto> CreateAsync(ApiClient client, string tag, string suffix, string interval, string joinDate)
    {
        var body = TestData.NewMember(interval);
        body["lastName"] = tag + suffix;
        body["joinDate"] = joinDate;
        return await client.PostJsonAsync<MemberDto>("/api/members", body, HttpStatusCode.Created);
    }

    private static Task<Core.Services.Payments.RecordPaymentResult> Record(ApiClient client, Guid memberId, int month,
        decimal amount, string paymentDate) =>
        client.PostJsonAsync<Core.Services.Payments.RecordPaymentResult>("/api/payments",
            FinanceTests.PaymentBody(memberId, 2003, month, amount, paymentDate), HttpStatusCode.Created);
}
