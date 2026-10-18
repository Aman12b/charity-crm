using System.Globalization;
using CharityCrm.Core.Entities;

namespace CharityCrm.Core.Common;

/// <summary>German display formats for exports, PDFs and emails (NFR-UX-01).</summary>
public static class GermanFormat
{
    public static readonly CultureInfo Culture = CultureInfo.GetCultureInfo("de-AT");

    public static string Date(DateOnly date) => date.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture);

    public static string Date(DateOnly? date) => date is { } d ? Date(d) : "";

    /// <summary>1234.56 → "1.234,56 €"</summary>
    public static string Money(decimal amount) =>
        amount.ToString("#,##0.00", CultureInfo.GetCultureInfo("de-DE")) + " €";

    public static string Number(decimal amount) => amount.ToString("0.00", CultureInfo.GetCultureInfo("de-DE"));

    public static string MonthName(int month) => Culture.DateTimeFormat.GetMonthName(month);

    /// <summary>Monthly period → "Jänner 2026", yearly period → "2026".</summary>
    public static string Period(DateOnly start, DateOnly end) =>
        start.Month == 1 && end.Month == 12 && start.Year == end.Year
            ? start.Year.ToString(CultureInfo.InvariantCulture)
            : $"{MonthName(start.Month)} {start.Year}";

    public static string Interval(BillingInterval interval) => interval switch
    {
        BillingInterval.Monthly => "Monatlich",
        BillingInterval.Yearly => "Jährlich",
        _ => interval.ToString(),
    };

    public static string Status(MemberStatus status) => status == MemberStatus.Active ? "Aktiv" : "Inaktiv";

    public static string Method(PaymentMethod method) => method switch
    {
        PaymentMethod.BankTransfer => "Überweisung",
        PaymentMethod.Cash => "Bar",
        _ => "Sonstige",
    };
}
