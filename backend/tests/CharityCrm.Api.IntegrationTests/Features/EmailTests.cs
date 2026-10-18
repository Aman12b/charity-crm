using System.Net;
using CharityCrm.Api.IntegrationTests.Infrastructure;
using CharityCrm.Core.Entities;
using CharityCrm.Core.Services.Invoices;
using CharityCrm.Core.Services.Members;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace CharityCrm.Api.IntegrationTests.Features;

[Collection(ApiCollection.Name)]
public sealed class EmailTests(ApiFactory factory)
{
    private static readonly int ThisYear = DateTime.Today.Year;

    [Fact]
    public async Task FR_MAIL_01_Invoice_is_emailed_with_pdf_attachment_and_marked_sent()
    {
        var (cashier, member, email) = await SetupAsync();
        var result = await FinanceTests.RecordAsync(cashier, member.Id, ThisYear, 1, 10m);

        await factory.ProcessOutboxAsync();

        var mail = factory.Email.SentTo(email).Should().ContainSingle().Subject;
        mail.Subject.Should().Be($"Ihre Rechnung {result.InvoiceNumber} – " + mail.Subject.Split(" – ", 2)[1]);
        mail.TextBody.Should().Contain(result.InvoiceNumber).And.Contain($"Jänner {ThisYear}").And.Contain("10,00 €");
        mail.HtmlBody.Should().Contain("<html lang=\"de\">").And.Contain(member.FirstName);
        mail.Attachments.Should().ContainSingle(a => a.FileName == $"Rechnung_{result.InvoiceNumber}.pdf"
            && a.ContentType == "application/pdf" && a.Content.Length > 0);

        var invoice = await cashier.GetJsonAsync<InvoiceDto>($"/api/invoices/{result.InvoiceId}");
        invoice.EmailStatus.Should().Be(EmailStatus.Sent);
        invoice.EmailSentAt.Should().NotBeNull();
    }

    [Fact]
    public async Task FR_MAIL_02_Failed_sends_are_retried_with_backoff_and_marked_failed_after_5_attempts()
    {
        var (cashier, member, email) = await SetupAsync();
        factory.Email.FailFor(email);
        var result = await FinanceTests.RecordAsync(cashier, member.Id, ThisYear, 2);

        await factory.ProcessOutboxAsync();

        var message = await OutboxFor(result.InvoiceId);
        message.Attempts.Should().Be(1);
        message.Status.Should().Be(OutboxStatus.Pending);
        message.NextAttemptAt.Should().BeCloseTo(DateTimeOffset.UtcNow.AddMinutes(2), TimeSpan.FromSeconds(30));
        var pending = await cashier.GetJsonAsync<InvoiceDto>($"/api/invoices/{result.InvoiceId}");
        pending.EmailStatus.Should().Be(EmailStatus.Pending);
        pending.EmailLastError.Should().Be("SMTP-Server nicht erreichbar");

        for (var attempt = 2; attempt <= 5; attempt++)
        {
            await MakeDueAsync(message.Id);
            await factory.ProcessOutboxAsync();
        }

        (await OutboxFor(result.InvoiceId)).Should().Match<EmailOutboxMessage>(m => m.Attempts == 5 && m.Status == OutboxStatus.Failed);
        (await cashier.GetJsonAsync<InvoiceDto>($"/api/invoices/{result.InvoiceId}")).EmailStatus.Should().Be(EmailStatus.Failed);
    }

    [Fact]
    public async Task FR_MAIL_01_Payment_succeeds_even_if_smtp_is_down()
    {
        var (cashier, member, email) = await SetupAsync();
        factory.Email.FailFor(email);

        var result = await FinanceTests.RecordAsync(cashier, member.Id, ThisYear, 3);
        await factory.ProcessOutboxAsync();

        (await cashier.GetJsonAsync<InvoiceDto>($"/api/invoices/{result.InvoiceId}")).Status.Should().Be(InvoiceStatus.Issued);
    }

    [Fact]
    public async Task FR_MAIL_03_Resend_to_a_different_address()
    {
        var (cashier, member, email) = await SetupAsync();
        var result = await FinanceTests.RecordAsync(cashier, member.Id, ThisYear, 4);
        await factory.ProcessOutboxAsync();
        var other = $"anders-{Guid.NewGuid():N}@example.at";

        var resent = await cashier.PostJsonAsync<InvoiceDto>($"/api/invoices/{result.InvoiceId}/resend", new { email = other });
        resent.EmailStatus.Should().Be(EmailStatus.Pending);
        await factory.ProcessOutboxAsync();

        factory.Email.SentTo(other).Should().ContainSingle();
        factory.Email.SentTo(email).Should().ContainSingle("the original address got only the first email");
        (await cashier.GetJsonAsync<InvoiceDto>($"/api/invoices/{result.InvoiceId}")).EmailStatus.Should().Be(EmailStatus.Sent);
    }

    [Fact]
    public async Task FR_MAIL_03_Resend_without_any_address_is_rejected()
    {
        var cashier = await factory.LoginAsNewUserAsync(UserRole.Cashier);
        var secretary = await factory.LoginAsNewUserAsync(UserRole.Secretary);
        var member = await secretary.CreateMemberAsync(email: null);
        var result = await FinanceTests.RecordAsync(cashier, member.Id, ThisYear, 5);

        var noAddress = await cashier.PostAsync($"/api/invoices/{result.InvoiceId}/resend", new { email = (string?)null });
        noAddress.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        (await noAddress.ReadProblemAsync()).Code.Should().Be("NO_EMAIL");

        (await cashier.PostAsync($"/api/invoices/{result.InvoiceId}/resend", new { email = "keine-mail" }))
            .StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task NFR_SEC_01_Secretary_cannot_resend()
    {
        var secretary = await factory.LoginAsNewUserAsync(UserRole.Secretary);

        (await secretary.PostAsync($"/api/invoices/{Guid.NewGuid()}/resend", new { }))
            .StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    private async Task<(ApiClient Cashier, MemberDto Member, string Email)> SetupAsync()
    {
        var email = $"mitglied-{Guid.NewGuid():N}@example.at";
        var secretary = await factory.LoginAsNewUserAsync(UserRole.Secretary);
        var member = await secretary.CreateMemberAsync(email: email);
        return (await factory.LoginAsNewUserAsync(UserRole.Cashier), member, email);
    }

    private Task<EmailOutboxMessage> OutboxFor(Guid invoiceId) => factory.WithDbAsync(db =>
        db.EmailOutboxMessages.AsNoTracking().SingleAsync(m => m.InvoiceId == invoiceId));

    private Task MakeDueAsync(Guid messageId) => factory.WithDbAsync(db => db.EmailOutboxMessages
        .Where(m => m.Id == messageId)
        .ExecuteUpdateAsync(s => s.SetProperty(m => m.NextAttemptAt, DateTimeOffset.UtcNow.AddSeconds(-1))));
}
