using System.Net;
using System.Text;
using CharityCrm.Core.Common;
using CharityCrm.Core.Entities;

namespace CharityCrm.Core.Services.Email;

/// <summary>
/// FR-MAIL-04: German HTML email with plain-text alternative. All values come from the invoice snapshots and are
/// HTML-encoded.
/// </summary>
public static class InvoiceEmailTemplate
{
    public static string Subject(Invoice invoice) =>
        $"Ihre Rechnung {invoice.InvoiceNumber} – {invoice.OrganizationSnapshot.Name}";

    public static (string Html, string Text) Body(Invoice invoice)
    {
        var member = invoice.MemberSnapshot;
        var org = invoice.OrganizationSnapshot;
        var name = $"{member.FirstName} {member.LastName}";
        var period = GermanFormat.Period(invoice.PeriodStart, invoice.PeriodEnd);
        var amount = GermanFormat.Money(invoice.Amount);
        var contact = ContactLines(org);

        var text = new StringBuilder()
            .AppendLine($"Sehr geehrte/r {name},")
            .AppendLine()
            .AppendLine($"vielen Dank für Ihren Mitgliedsbeitrag. Im Anhang finden Sie Ihre Rechnung {invoice.InvoiceNumber}.")
            .AppendLine()
            .AppendLine($"Rechnungsnummer: {invoice.InvoiceNumber}")
            .AppendLine($"Zeitraum:        {period}")
            .AppendLine($"Betrag:          {amount}")
            .AppendLine()
            .AppendLine("Mit freundlichen Grüßen")
            .AppendLine(org.Name)
            .AppendLine()
            .AppendJoin(Environment.NewLine, contact)
            .ToString();

        static string E(string? value) => WebUtility.HtmlEncode(value ?? "");

        var html = $"""
            <!DOCTYPE html>
            <html lang="de">
            <body style="font-family: Arial, Helvetica, sans-serif; font-size: 14px; color: #222;">
              <p>Sehr geehrte/r {E(name)},</p>
              <p>vielen Dank für Ihren Mitgliedsbeitrag. Im Anhang finden Sie Ihre Rechnung {E(invoice.InvoiceNumber)}.</p>
              <table style="border-collapse: collapse; margin: 16px 0;">
                <tr><td style="padding: 2px 16px 2px 0; color: #666;">Rechnungsnummer</td><td>{E(invoice.InvoiceNumber)}</td></tr>
                <tr><td style="padding: 2px 16px 2px 0; color: #666;">Zeitraum</td><td>{E(period)}</td></tr>
                <tr><td style="padding: 2px 16px 2px 0; color: #666;">Betrag</td><td><strong>{E(amount)}</strong></td></tr>
              </table>
              <p>Mit freundlichen Grüßen<br>{E(org.Name)}</p>
              <p style="font-size: 12px; color: #666;">{string.Join("<br>", contact.Select(E))}</p>
            </body>
            </html>
            """;

        return (html, text);
    }

    private static List<string> ContactLines(OrganizationSnapshot org)
    {
        var lines = new List<string> { org.Name, $"{org.Street}, {org.PostalCode} {org.City}" };
        if (!string.IsNullOrWhiteSpace(org.Email)) lines.Add($"E-Mail: {org.Email}");
        if (!string.IsNullOrWhiteSpace(org.Phone)) lines.Add($"Telefon: {org.Phone}");
        if (!string.IsNullOrWhiteSpace(org.ZvrNumber)) lines.Add($"ZVR-Zahl: {org.ZvrNumber}");
        return lines;
    }
}
