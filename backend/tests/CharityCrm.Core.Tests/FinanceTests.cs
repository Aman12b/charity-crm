using CharityCrm.Core.Common;
using CharityCrm.Core.Entities;
using CharityCrm.Core.Services.Invoices;
using CharityCrm.Core.Services.Payments;
using CharityCrm.Core.Services.Settings;
using CharityCrm.Core.Validation;
using FluentAssertions;

namespace CharityCrm.Core.Tests;

public sealed class ContributionPeriodTests
{
    [Fact]
    public void FR_PAY_01_Monthly_period_is_first_to_last_day_of_month()
    {
        var period = ContributionPeriod.For(BillingInterval.Monthly, 2028, 2);

        period.Should().Be(new ContributionPeriod(new DateOnly(2028, 2, 1), new DateOnly(2028, 2, 29)));
    }

    [Fact]
    public void FR_PAY_01_Yearly_period_is_whole_calendar_year() =>
        ContributionPeriod.For(BillingInterval.Yearly, 2026, null)
            .Should().Be(new ContributionPeriod(new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31)));

    [Theory]
    [InlineData(BillingInterval.Monthly, null)]
    [InlineData(BillingInterval.Monthly, 13)]
    [InlineData(BillingInterval.Yearly, 5)]
    public void FR_PAY_01_Period_must_match_billing_interval(BillingInterval interval, int? month)
    {
        var act = () => ContributionPeriod.For(interval, 2026, month);

        act.Should().Throw<InputValidationException>().Which.Errors.Should().ContainKey("month");
    }

    [Fact]
    public void FR_PAY_01_Yearly_period_overlaps_each_of_its_months()
    {
        var year = ContributionPeriod.Year(2026);

        year.Overlaps(new DateOnly(2026, 6, 1), new DateOnly(2026, 6, 30)).Should().BeTrue();
        year.Overlaps(new DateOnly(2025, 12, 1), new DateOnly(2025, 12, 31)).Should().BeFalse();
        year.Covers(ContributionPeriod.Month(2026, 12)).Should().BeTrue();
        ContributionPeriod.Month(2026, 12).Covers(year).Should().BeFalse();
    }

    [Fact]
    public void Period_is_displayed_in_german() =>
        ContributionPeriod.Month(2026, 3).ToString().Should().Be("März 2026");
}

public sealed class InvoiceNumberTests
{
    [Theory]
    [InlineData(2026, 1, "2026-00001")]
    [InlineData(2026, 42, "2026-00042")]
    [InlineData(2027, 12345, "2027-12345")]
    public void FR_INV_01_Number_format_is_year_and_five_digits(int year, int sequence, string expected) =>
        InvoiceIssuer.FormatNumber(year, sequence).Should().Be(expected);

    [Fact]
    public void PDF_path_is_grouped_by_year() =>
        InvoiceIssuer.PdfPathFor(2026, "2026-00001").Should().Be("2026/2026-00001.pdf");
}

public sealed class PaymentValidatorTests
{
    private static readonly DateOnly Today = new(2026, 10, 18);
    private readonly RecordPaymentRequestValidator _validator = new(new FixedClock(Today));

    private static RecordPaymentRequest Valid() =>
        new(Guid.NewGuid(), 2026, 10, 10m, Today, PaymentMethod.Cash, null);

    [Fact]
    public void Valid_payment_passes() => _validator.Validate(Valid()).IsValid.Should().BeTrue();

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    [InlineData(10.001)]
    [InlineData(100000000)]
    public void FR_PAY_01_Amount_must_be_positive_with_two_decimals(decimal amount) =>
        _validator.Validate(Valid() with { Amount = amount }).IsValid.Should().BeFalse();

    [Fact]
    public void FR_PAY_01_Payment_date_must_not_be_in_the_future() =>
        _validator.Validate(Valid() with { PaymentDate = Today.AddDays(1) }).Errors
            .Should().ContainSingle(e => e.PropertyName == "PaymentDate");

    [Fact]
    public void FR_PAY_01_Reference_is_limited_to_200_chars() =>
        _validator.Validate(Valid() with { Reference = new string('x', 201) }).IsValid.Should().BeFalse();
}

public sealed class SettingsValidatorTests
{
    private readonly UpdateSettingsRequestValidator _validator = new();

    private static UpdateSettingsRequest Valid() => new("Verein", "Gasse 1", "1010", "Wien", null, null, null,
        "AT61 1904 3002 3457 3201", "BKAUATWW", null, null, 10m, 120m);

    [Fact]
    public void Valid_settings_pass() => _validator.Validate(Valid()).IsValid.Should().BeTrue();

    [Fact]
    public void FR_FEE_01_Fees_must_be_greater_than_zero() =>
        _validator.Validate(Valid() with { MonthlyFee = 0, YearlyFee = -1 }).Errors.Should().HaveCount(2);

    [Theory]
    [InlineData("AT61")]
    [InlineData("1234567890123456")]
    public void FR_FEE_02_Iban_must_be_well_formed(string iban) =>
        _validator.Validate(Valid() with { Iban = iban }).IsValid.Should().BeFalse();

    [Fact]
    public void Iban_is_normalized() =>
        SettingsService.NormalizeIban(" at61 1904 3002 3457 3201 ").Should().Be("AT611904300234573201");
}
