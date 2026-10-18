using System.Net;
using CharityCrm.Api.IntegrationTests.Infrastructure;
using CharityCrm.Core.Abstractions;
using CharityCrm.Core.Entities;
using CharityCrm.Core.Services.Invoices;
using CharityCrm.Core.Services.Members;
using CharityCrm.Core.Services.Payments;
using CharityCrm.Core.Services.Settings;
using FluentAssertions;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CharityCrm.Api.IntegrationTests.Features;

[Collection(ApiCollection.Name)]
public sealed class FinanceTests(ApiFactory factory)
{
    private static readonly string AnyId = Guid.NewGuid().ToString();
    private static readonly int ThisYear = DateTime.Today.Year;
    private static readonly string Today = DateTime.Today.ToString("yyyy-MM-dd");

    public static TheoryData<string, string> FinanceRequests() => new()
    {
        { "GET", "/api/settings" },
        { "PUT", "/api/settings" },
        { "GET", "/api/payments" },
        { "POST", "/api/payments" },
        { "GET", "/api/invoices" },
        { "GET", $"/api/invoices/{AnyId}" },
        { "GET", $"/api/invoices/{AnyId}/pdf" },
        { "POST", $"/api/invoices/{AnyId}/cancel" },
        { "GET", $"/api/members/{AnyId}/payments" },
        { "GET", $"/api/members/{AnyId}/invoices" },
    };

    [Theory]
    [MemberData(nameof(FinanceRequests))]
    public async Task NFR_SEC_01_Secretary_sees_no_financial_data(string method, string url)
    {
        var secretary = await factory.LoginAsNewUserAsync(UserRole.Secretary);

        var response = method == "GET" ? await secretary.GetAsync(url) : await secretary.SendAsync(new HttpMethod(method), url, new { });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task FR_FEE_01_Cashier_reads_settings_but_cannot_change_them()
    {
        var cashier = await factory.LoginAsNewUserAsync(UserRole.Cashier);

        (await cashier.GetJsonAsync<SettingsDto>("/api/settings")).MonthlyFee.Should().BePositive();
        (await cashier.PutAsync("/api/settings", SettingsBody(1, 12))).StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task FR_FEE_01_Fees_must_be_positive_and_iban_valid()
    {
        var admin = await factory.LoginAsNewUserAsync(UserRole.Admin);
        var body = SettingsBody(0, 12);
        body["iban"] = "KEINE-IBAN";

        var response = await admin.PutAsync("/api/settings", body);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await response.ReadProblemAsync()).Errors.Should().ContainKeys("monthlyFee", "iban");
    }

    [Fact]
    public async Task FR_FEE_01_02_Settings_changes_do_not_affect_existing_invoices()
    {
        var admin = await factory.LoginAsNewUserAsync(UserRole.Admin);
        await admin.PutJsonAsync<SettingsDto>("/api/settings", SettingsBody(15, 150, name: "Verein Alt"));
        var member = await admin.CreateMemberAsync();
        var payment = await RecordAsync(admin, member.Id, ThisYear, 1, 15m);

        await admin.PutJsonAsync<SettingsDto>("/api/settings", SettingsBody(20, 200, name: "Verein Neu"));

        var invoice = await admin.GetJsonAsync<InvoiceDto>($"/api/invoices/{payment.InvoiceId}");
        invoice.Organization.Name.Should().Be("Verein Alt");
        invoice.Amount.Should().Be(15m);
        (await admin.GetJsonAsync<SettingsDto>("/api/settings")).MonthlyFee.Should().Be(20m);
    }

    [Fact]
    public async Task FR_PAY_01_FR_INV_01_Recording_a_payment_creates_invoice_with_number_and_pdf()
    {
        var cashier = await factory.LoginAsNewUserAsync(UserRole.Cashier);
        var member = await CreateMemberAsync();

        var result = await RecordAsync(cashier, member.Id, ThisYear, 3, 10m, reference: "Überweisung März");

        result.InvoiceNumber.Should().MatchRegex($"^{ThisYear}-\\d{{5}}$");
        var invoice = await cashier.GetJsonAsync<InvoiceDto>($"/api/invoices/{result.InvoiceId}");
        invoice.PeriodStart.Should().Be(new DateOnly(ThisYear, 3, 1));
        invoice.PeriodEnd.Should().Be(new DateOnly(ThisYear, 3, 31));
        invoice.Member.MemberNumber.Should().Be(member.MemberNumber);
        invoice.Status.Should().Be(InvoiceStatus.Issued);
        invoice.EmailStatus.Should().Be(EmailStatus.Pending);
        invoice.PaymentReference.Should().Be("Überweisung März");

        var pdf = await cashier.GetAsync($"/api/invoices/{result.InvoiceId}/pdf");
        pdf.StatusCode.Should().Be(HttpStatusCode.OK);
        pdf.Content.Headers.ContentType!.MediaType.Should().Be("application/pdf");
        (await pdf.Content.ReadAsByteArrayAsync()).Take(4).Should().Equal("%PDF"u8.ToArray());

        var audit = await factory.WithDbAsync(db => db.AuditLogs
            .Where(a => a.EntityId == result.PaymentId.ToString() || a.EntityId == result.InvoiceId.ToString())
            .Select(a => a.Action).ToListAsync());
        audit.Should().BeEquivalentTo([AuditActions.PaymentRecorded, AuditActions.InvoiceIssued]);
    }

    [Fact]
    public async Task FR_MAIL_02_Member_without_email_gets_email_status_not_applicable()
    {
        var cashier = await factory.LoginAsNewUserAsync(UserRole.Cashier);
        var member = await CreateMemberAsync(email: null);

        var result = await RecordAsync(cashier, member.Id, ThisYear, 4);

        (await cashier.GetJsonAsync<InvoiceDto>($"/api/invoices/{result.InvoiceId}")).EmailStatus.Should().Be(EmailStatus.NotApplicable);
    }

    [Fact]
    public async Task FR_PAY_01_Same_period_cannot_be_paid_twice()
    {
        var cashier = await factory.LoginAsNewUserAsync(UserRole.Cashier);
        var member = await CreateMemberAsync();
        await RecordAsync(cashier, member.Id, ThisYear, 5);

        var second = await cashier.PostAsync("/api/payments", PaymentBody(member.Id, ThisYear, 5));

        second.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await second.ReadProblemAsync()).Code.Should().Be("DUPLICATE_PERIOD");
    }

    [Fact]
    public async Task FR_PAY_01_Yearly_payment_cannot_overlap_paid_months_and_vice_versa()
    {
        var admin = await factory.LoginAsNewUserAsync(UserRole.Admin);
        var monthlyMember = await admin.CreateMemberAsync();
        await RecordAsync(admin, monthlyMember.Id, ThisYear, 6);
        await SwitchIntervalAsync(admin, monthlyMember.Id, "Yearly");

        var yearly = await admin.PostAsync("/api/payments", PaymentBody(monthlyMember.Id, ThisYear, null));
        yearly.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await admin.PostAsync("/api/payments", PaymentBody(monthlyMember.Id, ThisYear - 1, null)))
            .StatusCode.Should().Be(HttpStatusCode.Created, "a different year does not overlap");

        var yearlyMember = await admin.CreateMemberAsync(interval: "Yearly");
        await RecordAsync(admin, yearlyMember.Id, ThisYear, null, 120m);
        await SwitchIntervalAsync(admin, yearlyMember.Id, "Monthly");
        (await admin.PostAsync("/api/payments", PaymentBody(yearlyMember.Id, ThisYear, 2)))
            .StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task FR_MEM_05_Inactive_members_cannot_receive_payments()
    {
        var admin = await factory.LoginAsNewUserAsync(UserRole.Admin);
        var member = await admin.CreateMemberAsync();
        await admin.PostJsonAsync<MemberDto>($"/api/members/{member.Id}/deregister", new { reason = "Austritt" });

        var response = await admin.PostAsync("/api/payments", PaymentBody(member.Id, ThisYear, 1));

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        (await response.ReadProblemAsync()).Code.Should().Be("MEMBER_INACTIVE");
    }

    [Fact]
    public async Task FR_PAY_01_Invalid_payment_input_is_rejected()
    {
        var cashier = await factory.LoginAsNewUserAsync(UserRole.Cashier);
        var monthly = await CreateMemberAsync();
        var future = DateTime.Today.AddDays(1).ToString("yyyy-MM-dd");

        var invalid = await cashier.PostAsync("/api/payments", PaymentBody(monthly.Id, ThisYear, 1, amount: 0, paymentDate: future));
        invalid.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await invalid.ReadProblemAsync()).Errors.Should().ContainKeys("amount", "paymentDate");

        var missingMonth = await cashier.PostAsync("/api/payments", PaymentBody(monthly.Id, ThisYear, null));
        missingMonth.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await missingMonth.ReadProblemAsync()).Errors.Should().ContainKey("month");
    }

    [Fact]
    public async Task FR_INV_01_NFR_REL_03_Concurrent_payments_get_unique_gap_free_invoice_numbers()
    {
        var cashier = await factory.LoginAsNewUserAsync(UserRole.Cashier);
        var members = new List<MemberDto>();
        for (var i = 0; i < 12; i++)
        {
            members.Add(await CreateMemberAsync());
        }

        var results = await Task.WhenAll(members.Select(m => RecordAsync(cashier, m.Id, ThisYear, 7)));

        var numbers = results.Select(r => int.Parse(r.InvoiceNumber[5..])).Order().ToList();
        numbers.Should().OnlyHaveUniqueItems();
        (numbers[^1] - numbers[0]).Should().Be(numbers.Count - 1, "numbers must be consecutive without gaps");
    }

    [Fact]
    public async Task FR_INV_01_Invoice_keeps_member_snapshot_after_member_edit()
    {
        var admin = await factory.LoginAsNewUserAsync(UserRole.Admin);
        var member = await admin.CreateMemberAsync();
        var result = await RecordAsync(admin, member.Id, ThisYear, 8);

        var body = TestData.ToUpdate(await admin.GetJsonAsync<MemberDto>($"/api/members/{member.Id}"));
        body["city"] = "Salzburg";
        await admin.PutJsonAsync<MemberDto>($"/api/members/{member.Id}", body);

        (await admin.GetJsonAsync<InvoiceDto>($"/api/invoices/{result.InvoiceId}")).Member.City.Should().Be("Wien");
    }

    [Fact]
    public async Task FR_INV_04_Cancel_requires_reason_marks_both_cancelled_and_frees_the_period()
    {
        var cashier = await factory.LoginAsNewUserAsync(UserRole.Cashier);
        var member = await CreateMemberAsync();
        var first = await RecordAsync(cashier, member.Id, ThisYear, 9);

        (await cashier.PostAsync($"/api/invoices/{first.InvoiceId}/cancel", new { reason = " " }))
            .StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var cancelled = await cashier.PostJsonAsync<InvoiceDto>($"/api/invoices/{first.InvoiceId}/cancel", new { reason = "Doppelt erfasst" });
        cancelled.Status.Should().Be(InvoiceStatus.Cancelled);
        cancelled.CancellationReason.Should().Be("Doppelt erfasst");

        var payments = await cashier.GetJsonAsync<List<PaymentDto>>($"/api/members/{member.Id}/payments");
        payments.Single(p => p.Id == first.PaymentId).Status.Should().Be(PaymentStatus.Cancelled);
        (await cashier.GetAsync($"/api/invoices/{first.InvoiceId}/pdf")).StatusCode.Should().Be(HttpStatusCode.OK);

        var again = await RecordAsync(cashier, member.Id, ThisYear, 9);
        again.InvoiceNumber.Should().NotBe(first.InvoiceNumber);
        string.CompareOrdinal(again.InvoiceNumber, first.InvoiceNumber).Should().BePositive();

        (await cashier.PostAsync($"/api/invoices/{first.InvoiceId}/cancel", new { reason = "nochmal" }))
            .StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task FR_PAY_02_List_filters_by_member_and_sums_without_cancelled()
    {
        var cashier = await factory.LoginAsNewUserAsync(UserRole.Cashier);
        var member = await CreateMemberAsync();
        await RecordAsync(cashier, member.Id, ThisYear, 1, 10m);
        await RecordAsync(cashier, member.Id, ThisYear, 2, 12.5m);
        var cancelled = await RecordAsync(cashier, member.Id, ThisYear, 3, 100m);
        await cashier.PostJsonAsync<InvoiceDto>($"/api/invoices/{cancelled.InvoiceId}/cancel", new { reason = "Fehler" });

        var all = await cashier.GetJsonAsync<PaymentListResult>($"/api/payments?memberId={member.Id}");
        all.TotalCount.Should().Be(3);
        all.TotalAmount.Should().Be(22.5m);

        var february = await cashier.GetJsonAsync<PaymentListResult>(
            $"/api/payments?memberId={member.Id}&periodYear={ThisYear}&periodMonth=2&method=BankTransfer&from={Today}&to={Today}");
        february.Items.Should().ContainSingle().Which.Amount.Should().Be(12.5m);
    }

    [Fact]
    public async Task FR_INV_02_Invoice_list_filters_by_search_and_status()
    {
        var cashier = await factory.LoginAsNewUserAsync(UserRole.Cashier);
        var member = await CreateMemberAsync();
        var result = await RecordAsync(cashier, member.Id, ThisYear, 10);

        var byNumber = await cashier.GetJsonAsync<Core.Common.PagedResult<InvoiceListItemDto>>($"/api/invoices?search={result.InvoiceNumber}");
        byNumber.Items.Should().ContainSingle(i => i.Id == result.InvoiceId);

        var byName = await cashier.GetJsonAsync<Core.Common.PagedResult<InvoiceListItemDto>>(
            $"/api/invoices?search={Uri.EscapeDataString(member.LastName)}&status=Issued&emailStatus=Pending");
        byName.Items.Should().ContainSingle(i => i.Id == result.InvoiceId);

        var memberInvoices = await cashier.GetJsonAsync<List<InvoiceListItemDto>>($"/api/members/{member.Id}/invoices");
        memberInvoices.Should().ContainSingle(i => i.Id == result.InvoiceId);
    }

    [Fact]
    public async Task NFR_REL_01_Failed_pdf_storage_rolls_back_payment_and_invoice_number()
    {
        using var failing = factory.WithWebHostBuilder(b => b.ConfigureTestServices(s =>
            s.AddSingleton<IInvoiceStorage, FailingStorage>()));
        var user = await factory.CreateUserAsync(UserRole.Cashier);
        var client = new ApiClient(failing.CreateClient());
        await client.LoginAsync(user.Email!, ApiFactory.DefaultPassword);
        var member = await CreateMemberAsync();
        var lastNumberBefore = await LastInvoiceNumberAsync();

        var response = await client.PostAsync("/api/payments", PaymentBody(member.Id, ThisYear, 11));

        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        (await factory.WithDbAsync(db => db.Payments.AnyAsync(p => p.MemberId == member.Id))).Should().BeFalse();
        (await LastInvoiceNumberAsync()).Should().Be(lastNumberBefore);
    }

    private Task<int> LastInvoiceNumberAsync() => factory.WithDbAsync(db =>
        db.InvoiceNumberSequences.Where(s => s.Year == ThisYear).Select(s => s.LastNumber).SingleOrDefaultAsync());

    private async Task<MemberDto> CreateMemberAsync(string interval = "Monthly", string? email = "max@example.at")
    {
        var secretary = await factory.LoginAsNewUserAsync(UserRole.Secretary);
        return await secretary.CreateMemberAsync(interval, email);
    }

    private static async Task SwitchIntervalAsync(ApiClient client, Guid memberId, string interval)
    {
        var body = TestData.ToUpdate(await client.GetJsonAsync<MemberDto>($"/api/members/{memberId}"));
        body["billingInterval"] = interval;
        await client.PutJsonAsync<MemberDto>($"/api/members/{memberId}", body);
    }

    public static async Task<RecordPaymentResult> RecordAsync(ApiClient client, Guid memberId, int year, int? month,
        decimal amount = 10m, string? reference = null) =>
        await client.PostJsonAsync<RecordPaymentResult>("/api/payments",
            PaymentBody(memberId, year, month, amount, reference: reference), HttpStatusCode.Created);

    public static object PaymentBody(Guid memberId, int year, int? month, decimal amount = 10m, string? paymentDate = null,
        string? reference = null) => new
    {
        memberId,
        year,
        month,
        amount,
        paymentDate = paymentDate ?? Today,
        method = "BankTransfer",
        reference,
    };

    private static Dictionary<string, object?> SettingsBody(decimal monthly, decimal yearly, string name = "Testverein") => new()
    {
        ["name"] = name,
        ["street"] = "Vereinsgasse 3",
        ["postalCode"] = "1070",
        ["city"] = "Wien",
        ["country"] = "Österreich",
        ["email"] = "office@verein.at",
        ["phone"] = "+43 1 555",
        ["iban"] = "AT61 1904 3002 3457 3201",
        ["bic"] = "BKAUATWW",
        ["zvrNumber"] = "123456789",
        ["invoiceFooterText"] = "Danke!",
        ["monthlyFee"] = monthly,
        ["yearlyFee"] = yearly,
    };

    private sealed class FailingStorage : IInvoiceStorage
    {
        public Task SaveAsync(string relativePath, byte[] content, CancellationToken ct = default) =>
            throw new IOException("Disk full");

        public Task<byte[]?> ReadAsync(string relativePath, CancellationToken ct = default) => Task.FromResult<byte[]?>(null);
    }
}
