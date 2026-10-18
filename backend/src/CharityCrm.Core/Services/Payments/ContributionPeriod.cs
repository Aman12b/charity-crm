using CharityCrm.Core.Common;
using CharityCrm.Core.Entities;

namespace CharityCrm.Core.Services.Payments;

/// <summary>The time span a payment covers: one calendar month or one calendar year.</summary>
public readonly record struct ContributionPeriod(DateOnly Start, DateOnly End)
{
    public static ContributionPeriod Month(int year, int month)
    {
        var start = new DateOnly(year, month, 1);
        return new(start, start.AddMonths(1).AddDays(-1));
    }

    public static ContributionPeriod Year(int year) => new(new DateOnly(year, 1, 1), new DateOnly(year, 12, 31));

    /// <summary>Period chosen from the member's billing interval: month + year for monthly, year for yearly payers.</summary>
    public static ContributionPeriod For(BillingInterval interval, int year, int? month) => interval switch
    {
        BillingInterval.Monthly when month is >= 1 and <= 12 => Month(year, month.Value),
        BillingInterval.Monthly => throw new InputValidationException("month", "Bitte einen Monat wählen."),
        BillingInterval.Yearly when month is null => Year(year),
        BillingInterval.Yearly => throw new InputValidationException("month",
            "Das Mitglied zahlt jährlich; es kann kein einzelner Monat gewählt werden."),
        _ => throw new ArgumentOutOfRangeException(nameof(interval)),
    };

    public bool Overlaps(DateOnly start, DateOnly end) => Start <= end && start <= End;

    public bool Covers(ContributionPeriod other) => Start <= other.Start && End >= other.End;

    public override string ToString() => GermanFormat.Period(Start, End);
}
