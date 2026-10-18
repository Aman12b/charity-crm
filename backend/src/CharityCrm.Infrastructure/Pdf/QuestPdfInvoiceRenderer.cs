using CharityCrm.Core.Abstractions;
using CharityCrm.Core.Common;
using CharityCrm.Core.Entities;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace CharityCrm.Infrastructure.Pdf;

/// <summary>
/// Invoice layout (FR-INV-01): organization header, member address, number, issue date, period, amount,
/// payment method and date, "Betrag dankend erhalten", footer. Cancelled invoices get a "STORNIERT" mark (FR-INV-04).
/// </summary>
public sealed class QuestPdfInvoiceRenderer : IInvoicePdfRenderer
{
    private const string Accent = "#0B7285";

    static QuestPdfInvoiceRenderer()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public byte[] Render(Invoice invoice) => Document.Create(container => container.Page(page =>
    {
        var org = invoice.OrganizationSnapshot;
        var member = invoice.MemberSnapshot;
        var payment = invoice.Payment;

        page.Size(PageSizes.A4);
        page.MarginHorizontal(2, Unit.Centimetre);
        page.MarginVertical(1.6f, Unit.Centimetre);
        page.DefaultTextStyle(t => t.FontSize(10));

        if (invoice.Status == InvoiceStatus.Cancelled)
        {
            page.Foreground().AlignCenter().AlignMiddle().Rotate(-35)
                .Text("STORNIERT").FontSize(96).Bold().FontColor("#55E53935"); // translucent, keeps the content readable
        }

        page.Header().Column(col =>
        {
            col.Item().Text(org.Name).FontSize(18).Bold().FontColor(Accent);
            col.Item().Text($"{org.Street} · {org.PostalCode} {org.City} · {org.Country}").FontSize(9).FontColor(Colors.Grey.Darken1);
            var contact = string.Join(" · ", new[] { org.Email, org.Phone, org.ZvrNumber is null ? null : $"ZVR-Zahl {org.ZvrNumber}" }
                .Where(s => !string.IsNullOrWhiteSpace(s)));
            if (contact.Length > 0)
            {
                col.Item().Text(contact).FontSize(9).FontColor(Colors.Grey.Darken1);
            }
            col.Item().PaddingTop(6).LineHorizontal(1).LineColor(Accent);
        });

        page.Content().PaddingTop(28).Column(col =>
        {
            col.Spacing(4);

            // Address block and invoice meta data side by side.
            col.Item().Row(row =>
            {
                row.RelativeItem().Column(address =>
                {
                    address.Item().Text($"{member.FirstName} {member.LastName}");
                    address.Item().Text(member.Street);
                    address.Item().Text($"{member.PostalCode} {member.City}");
                    address.Item().Text(member.Country);
                });
                row.ConstantItem(200).Column(meta =>
                {
                    MetaRow(meta, "Rechnungsnummer", invoice.InvoiceNumber);
                    MetaRow(meta, "Rechnungsdatum", GermanFormat.Date(invoice.IssueDate));
                    MetaRow(meta, "Mitgliedsnummer", member.MemberNumber);
                });
            });

            col.Item().PaddingTop(36).Text($"Rechnung {invoice.InvoiceNumber}").FontSize(16).Bold();
            col.Item().PaddingTop(4).Text($"Sehr geehrte/r {member.FirstName} {member.LastName},");
            col.Item().Text("wir bestätigen den Erhalt Ihres Mitgliedsbeitrags:");

            col.Item().PaddingTop(12).Table(table =>
            {
                table.ColumnsDefinition(c =>
                {
                    c.RelativeColumn(3);
                    c.RelativeColumn(2);
                    c.ConstantColumn(100);
                });

                table.Header(h =>
                {
                    HeaderCell(h.Cell(), "Leistung");
                    HeaderCell(h.Cell(), "Zeitraum");
                    HeaderCell(h.Cell().AlignRight(), "Betrag");
                });

                BodyCell(table.Cell()).Text($"Mitgliedsbeitrag ({GermanFormat.Interval(payment.Interval)})");
                BodyCell(table.Cell()).Text(GermanFormat.Period(invoice.PeriodStart, invoice.PeriodEnd));
                BodyCell(table.Cell()).AlignRight().Text(GermanFormat.Money(invoice.Amount));

                table.Cell().ColumnSpan(2).PaddingTop(6).AlignRight().Text("Gesamtbetrag").Bold();
                table.Cell().PaddingTop(6).AlignRight().Text(GermanFormat.Money(invoice.Amount)).Bold();
            });

            col.Item().PaddingTop(16).Column(info =>
            {
                MetaRow(info, "Zahlungsart", GermanFormat.Method(payment.Method));
                MetaRow(info, "Zahlungsdatum", GermanFormat.Date(payment.PaymentDate));
                if (!string.IsNullOrWhiteSpace(payment.Reference))
                {
                    MetaRow(info, "Referenz", payment.Reference);
                }
            });

            col.Item().PaddingTop(16).Text("Betrag dankend erhalten.").Bold().FontColor(Accent);

            if (invoice.Status == InvoiceStatus.Cancelled)
            {
                col.Item().PaddingTop(16).Background(Colors.Red.Lighten5).Padding(8).Column(c =>
                {
                    c.Item().Text("Diese Rechnung wurde storniert.").Bold().FontColor(Colors.Red.Darken2);
                    if (invoice.CancelledAt is { } cancelledAt)
                    {
                        c.Item().Text($"Storniert am {GermanFormat.Date(DateOnly.FromDateTime(cancelledAt.UtcDateTime))}");
                    }
                    if (!string.IsNullOrWhiteSpace(invoice.CancellationReason))
                    {
                        c.Item().Text($"Grund: {invoice.CancellationReason}");
                    }
                });
            }
        });

        page.Footer().Column(col =>
        {
            col.Item().LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten1);
            if (!string.IsNullOrWhiteSpace(org.FooterText))
            {
                col.Item().PaddingTop(4).Text(org.FooterText).FontSize(9);
            }
            var bank = string.Join(" · ", new[] { org.Iban is null ? null : $"IBAN {FormatIban(org.Iban)}", org.Bic is null ? null : $"BIC {org.Bic}" }
                .Where(s => s is not null));
            if (bank.Length > 0)
            {
                col.Item().Text(bank).FontSize(8).FontColor(Colors.Grey.Darken1);
            }
            col.Item().AlignRight().Text(t =>
            {
                t.DefaultTextStyle(s => s.FontSize(8).FontColor(Colors.Grey.Darken1));
                t.Span("Seite ");
                t.CurrentPageNumber();
                t.Span(" von ");
                t.TotalPages();
            });
        });
    })).GeneratePdf();

    /// <summary>"AT611904300234573201" → "AT61 1904 3002 3457 3201"</summary>
    public static string FormatIban(string iban) =>
        string.Join(" ", Enumerable.Range(0, (iban.Length + 3) / 4).Select(i => iban.Substring(i * 4, Math.Min(4, iban.Length - i * 4))));

    private static void MetaRow(ColumnDescriptor col, string label, string value) => col.Item().Row(r =>
    {
        r.ConstantItem(110).Text(label).FontColor(Colors.Grey.Darken1);
        r.RelativeItem().Text(value);
    });

    private static void HeaderCell(IContainer cell, string text) =>
        cell.BorderBottom(1).BorderColor(Accent).PaddingVertical(4).Text(text).Bold();

    private static IContainer BodyCell(IContainer cell) =>
        cell.BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(6);
}
