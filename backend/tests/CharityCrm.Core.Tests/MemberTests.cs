using System.Text;
using CharityCrm.Core.Abstractions;
using CharityCrm.Core.Common;
using CharityCrm.Core.Entities;
using CharityCrm.Core.Services.Members;
using CharityCrm.Core.Validation;
using FluentAssertions;

namespace CharityCrm.Core.Tests;

public sealed class FixedClock(DateOnly today) : IClock
{
    public DateTimeOffset UtcNow => new(today.ToDateTime(new TimeOnly(10, 0)), TimeSpan.Zero);
    public DateOnly Today => today;
}

public sealed class MemberValidatorTests
{
    private static readonly DateOnly Today = new(2026, 10, 18);
    private readonly CreateMemberRequestValidator _validator = new(new FixedClock(Today));

    private static CreateMemberRequest Valid() => new("Anna", "Muster", new DateOnly(1990, 1, 1), "Gasse 1", "1010", "Wien",
        null, null, null, BillingInterval.Monthly, null, null);

    [Fact]
    public void FR_MEM_01_Valid_request_passes() => _validator.Validate(Valid()).IsValid.Should().BeTrue();

    [Theory]
    [InlineData(0)]   // today is not in the past
    [InlineData(-1)]  // tomorrow
    public void FR_MEM_01_Date_of_birth_must_be_in_the_past(int daysAgo) =>
        _validator.Validate(Valid() with { DateOfBirth = Today.AddDays(-daysAgo) })
            .Errors.Should().Contain(e => e.PropertyName == "DateOfBirth");

    [Fact]
    public void FR_MEM_01_Age_is_at_most_120_years()
    {
        _validator.Validate(Valid() with { DateOfBirth = Today.AddYears(-120) }).IsValid.Should().BeTrue();
        _validator.Validate(Valid() with { DateOfBirth = Today.AddYears(-120).AddDays(-1) }).IsValid.Should().BeFalse();
    }

    [Fact]
    public void FR_MEM_01_Required_fields_and_lengths()
    {
        var result = _validator.Validate(Valid() with
        {
            FirstName = " ",
            LastName = new string('x', 101),
            Street = "",
            PostalCode = "12345678901",
            City = "",
            Notes = new string('x', 2001),
        });

        result.Errors.Select(e => e.PropertyName).Should()
            .Contain(["FirstName", "LastName", "Street", "PostalCode", "City", "Notes"]);
    }

    [Theory]
    [InlineData("", true)]
    [InlineData(null, true)]
    [InlineData("anna@example.at", true)]
    [InlineData("keine-mail", false)]
    public void FR_MEM_01_Email_is_optional_but_must_be_valid(string? email, bool valid) =>
        _validator.Validate(Valid() with { Email = email }).IsValid.Should().Be(valid);
}

public sealed class CsvWriterTests
{
    [Fact]
    public void FR_MEM_07_Csv_has_bom_semicolons_and_crlf()
    {
        var bytes = new CsvWriter().Row("a", "b").Row("1", null).ToBytes();

        bytes.Take(3).Should().Equal(0xEF, 0xBB, 0xBF);
        Encoding.UTF8.GetString(bytes[3..]).Should().Be("a;b\r\n1;\r\n");
    }

    [Theory]
    [InlineData("Wien", "Wien")]
    [InlineData("a;b", "\"a;b\"")]
    [InlineData("Sag \"Hallo\"", "\"Sag \"\"Hallo\"\"\"")]
    [InlineData("=SUMME(A1)", "'=SUMME(A1)")]
    [InlineData("-5", "'-5")]
    public void Cells_are_escaped(string input, string expected) => CsvWriter.Escape(input).Should().Be(expected);
}

public sealed class SearchPatternTests
{
    [Fact]
    public void Wildcards_in_user_input_are_matched_literally() =>
        SearchPattern.Contains(" 50%_x ").Should().Be("%50\\%\\_x%");
}

public sealed class GermanFormatTests
{
    [Fact]
    public void NFR_UX_01_Dates_and_amounts_are_german()
    {
        GermanFormat.Date(new DateOnly(2026, 1, 5)).Should().Be("05.01.2026");
        GermanFormat.Money(1234.56m).Should().Be("1.234,56 €");
    }

    [Fact]
    public void Periods_use_austrian_month_names()
    {
        GermanFormat.Period(new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 31)).Should().Be("Jänner 2026");
        GermanFormat.Period(new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31)).Should().Be("2026");
    }
}
