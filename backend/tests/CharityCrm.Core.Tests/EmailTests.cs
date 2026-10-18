using CharityCrm.Core.Entities;
using CharityCrm.Core.Services.Email;
using FluentAssertions;

namespace CharityCrm.Core.Tests;

public sealed class InvoiceEmailTemplateTests
{
    private static Invoice Sample() => new()
    {
        InvoiceNumber = "2026-00007",
        Amount = 120m,
        PeriodStart = new DateOnly(2026, 1, 1),
        PeriodEnd = new DateOnly(2026, 12, 31),
        MemberSnapshot = new MemberSnapshot { FirstName = "Anna", LastName = "<Müller>", MemberNumber = "M-000001" },
        OrganizationSnapshot = new OrganizationSnapshot
        {
            Name = "Hilfswerk", Street = "Gasse 1", PostalCode = "1010", City = "Wien", Email = "office@hilfswerk.at",
        },
    };

    [Fact]
    public void FR_MAIL_01_Subject_contains_number_and_organization() =>
        InvoiceEmailTemplate.Subject(Sample()).Should().Be("Ihre Rechnung 2026-00007 – Hilfswerk");

    [Fact]
    public void FR_MAIL_04_Body_has_greeting_number_period_amount_and_contact()
    {
        var (html, text) = InvoiceEmailTemplate.Body(Sample());

        text.Should().Contain("Sehr geehrte/r Anna <Müller>").And.Contain("2026-00007").And.Contain("2026")
            .And.Contain("120,00 €").And.Contain("office@hilfswerk.at");
        html.Should().Contain("120,00 €").And.Contain("office@hilfswerk.at");
    }

    [Fact]
    public void FR_MAIL_04_Html_values_are_encoded() =>
        InvoiceEmailTemplate.Body(Sample()).Html.Should().Contain("&lt;M&#252;ller&gt;").And.NotContain("<Müller>");

    [Theory]
    [InlineData(1, 2)]
    [InlineData(2, 4)]
    [InlineData(4, 16)]
    public void FR_MAIL_02_Retry_delay_grows_exponentially(int attempts, int minutes) =>
        EmailOutboxProcessor.RetryDelay(attempts).Should().Be(TimeSpan.FromMinutes(minutes));
}
