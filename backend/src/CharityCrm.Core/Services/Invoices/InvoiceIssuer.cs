using System.Globalization;
using CharityCrm.Core.Abstractions;
using CharityCrm.Core.Entities;

namespace CharityCrm.Core.Services.Invoices;

/// <summary>Creates the invoice for a new payment inside the caller's transaction (FR-INV-01).</summary>
public sealed class InvoiceIssuer(
    IAppDbContext db,
    IDbLocks locks,
    IInvoicePdfRenderer pdf,
    IInvoiceStorage storage,
    IAuditLogger audit,
    IClock clock)
{
    public async Task<Invoice> IssueAsync(Payment payment, Member member, OrganizationSettings settings, CancellationToken ct)
    {
        var issueDate = clock.Today;
        var number = FormatNumber(issueDate.Year, await locks.NextInvoiceNumberAsync(issueDate.Year, ct));

        var invoice = new Invoice
        {
            Id = Guid.NewGuid(),
            InvoiceNumber = number,
            PaymentId = payment.Id,
            Payment = payment,
            MemberId = member.Id,
            IssueDate = issueDate,
            Amount = payment.Amount,
            PeriodStart = payment.PeriodStart,
            PeriodEnd = payment.PeriodEnd,
            MemberSnapshot = MemberSnapshot.From(member),
            OrganizationSnapshot = OrganizationSnapshot.From(settings),
            Status = InvoiceStatus.Issued,
            PdfPath = PdfPathFor(issueDate.Year, number),
            EmailStatus = string.IsNullOrWhiteSpace(member.Email) ? EmailStatus.NotApplicable : EmailStatus.Pending,
            CreatedAt = clock.UtcNow,
        };
        payment.Invoice = invoice;
        db.Invoices.Add(invoice);

        // Written before commit: if this fails, the whole payment transaction rolls back.
        await storage.SaveAsync(invoice.PdfPath, pdf.Render(invoice), ct);

        audit.Add(AuditActions.InvoiceIssued, AuditEntityTypes.Invoice, invoice.Id.ToString(), new AuditChanges()
            .Set("invoiceNumber", number)
            .Set("paymentId", payment.Id)
            .Set("amount", invoice.Amount));
        return invoice;
    }

    /// <summary>2026, 42 → "2026-00042"</summary>
    public static string FormatNumber(int year, int sequence) =>
        string.Create(CultureInfo.InvariantCulture, $"{year}-{sequence:D5}");

    public static string PdfPathFor(int year, string invoiceNumber) =>
        string.Create(CultureInfo.InvariantCulture, $"{year}/{invoiceNumber}.pdf");
}
