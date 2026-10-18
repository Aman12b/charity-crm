using CharityCrm.Core.Abstractions;
using CharityCrm.Core.Common;
using CharityCrm.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace CharityCrm.Core.Services.Invoices;

/// <summary>Viewing, downloading and cancelling invoices (FR-INV-02..04). Issued invoices are never edited.</summary>
public sealed class InvoiceService(
    IAppDbContext db,
    IInvoicePdfRenderer pdf,
    IInvoiceStorage storage,
    IAuditLogger audit,
    IClock clock)
{
    public async Task<PagedResult<InvoiceListItemDto>> ListAsync(InvoiceListQuery query)
    {
        var page = Paging.NormalizePage(query.Page);
        var pageSize = Paging.NormalizePageSize(query.PageSize);

        var invoices = db.Invoices.AsNoTracking();
        if (query.MemberId is { } memberId) invoices = invoices.Where(i => i.MemberId == memberId);
        if (query.From is { } from) invoices = invoices.Where(i => i.IssueDate >= from);
        if (query.To is { } to) invoices = invoices.Where(i => i.IssueDate <= to);
        if (query.Status is { } status) invoices = invoices.Where(i => i.Status == status);
        if (query.EmailStatus is { } emailStatus) invoices = invoices.Where(i => i.EmailStatus == emailStatus);
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var pattern = SearchPattern.Contains(query.Search);
            invoices = invoices.Where(i =>
                EF.Functions.ILike(i.InvoiceNumber, pattern, SearchPattern.EscapeChar)
                || EF.Functions.ILike(i.MemberSnapshot.MemberNumber, pattern, SearchPattern.EscapeChar)
                || EF.Functions.ILike(i.MemberSnapshot.FirstName + " " + i.MemberSnapshot.LastName, pattern, SearchPattern.EscapeChar)
                || EF.Functions.ILike(i.MemberSnapshot.LastName + " " + i.MemberSnapshot.FirstName, pattern, SearchPattern.EscapeChar));
        }

        var total = await invoices.CountAsync();
        var items = await invoices
            .OrderByDescending(i => i.IssueDate).ThenByDescending(i => i.InvoiceNumber)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(i => new InvoiceListItemDto(
                i.Id, i.InvoiceNumber, i.IssueDate, i.MemberId, i.MemberSnapshot.MemberNumber,
                i.MemberSnapshot.LastName + " " + i.MemberSnapshot.FirstName,
                i.PeriodStart, i.PeriodEnd, i.Amount, i.Status, i.EmailStatus))
            .ToListAsync();

        return new PagedResult<InvoiceListItemDto>(items, page, pageSize, total);
    }

    public async Task<IReadOnlyList<InvoiceListItemDto>> ListForMemberAsync(Guid memberId) =>
        (await ListAsync(new InvoiceListQuery(null, memberId, null, null, null, null, 1, Paging.MaxPageSize))).Items;

    public async Task<InvoiceDto> GetAsync(Guid id) => ToDto(await FindAsync(id, track: false));

    /// <summary>The stored PDF is a cache; if it is missing it is re-rendered from the snapshots.</summary>
    public async Task<InvoicePdf> GetPdfAsync(Guid id, CancellationToken ct = default)
    {
        var invoice = await FindAsync(id, track: false);
        var content = await storage.ReadAsync(invoice.PdfPath, ct);
        if (content is null)
        {
            content = pdf.Render(invoice);
            await storage.SaveAsync(invoice.PdfPath, content, ct);
        }
        return new InvoicePdf($"Rechnung_{invoice.InvoiceNumber}.pdf", content);
    }

    /// <summary>
    /// FR-INV-04: cancels invoice and payment, marks the PDF "STORNIERT". The number is never reused and the
    /// period becomes payable again (overlap checks only consider recorded payments).
    /// </summary>
    public async Task<InvoiceDto> CancelAsync(Guid id, CancelInvoiceRequest request, CancellationToken ct = default)
    {
        await using var tx = await db.BeginTransactionAsync(ct);
        var invoice = await FindAsync(id, track: true);
        if (invoice.Status == InvoiceStatus.Cancelled)
        {
            throw new BusinessRuleException("Die Rechnung ist bereits storniert.", "ALREADY_CANCELLED");
        }

        invoice.Status = InvoiceStatus.Cancelled;
        invoice.CancelledAt = clock.UtcNow;
        invoice.CancellationReason = request.Reason.Trim();
        invoice.Payment.Status = PaymentStatus.Cancelled;

        audit.Add(AuditActions.InvoiceCancelled, AuditEntityTypes.Invoice, invoice.Id.ToString(), new AuditChanges()
            .Track("status", InvoiceStatus.Issued, InvoiceStatus.Cancelled)
            .Set("reason", invoice.CancellationReason));
        audit.Add(AuditActions.PaymentCancelled, AuditEntityTypes.Payment, invoice.PaymentId.ToString(), new AuditChanges()
            .Track("status", PaymentStatus.Recorded, PaymentStatus.Cancelled));

        await storage.SaveAsync(invoice.PdfPath, pdf.Render(invoice), ct);
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return ToDto(invoice);
    }

    private async Task<Invoice> FindAsync(Guid id, bool track)
    {
        var invoices = track ? db.Invoices : db.Invoices.AsNoTracking();
        return await invoices.Include(i => i.Payment).SingleOrDefaultAsync(i => i.Id == id)
            ?? throw new NotFoundException("Rechnung nicht gefunden.");
    }

    internal static InvoiceDto ToDto(Invoice i) => new(
        i.Id, i.InvoiceNumber, i.IssueDate, i.MemberId, i.PaymentId, i.PeriodStart, i.PeriodEnd, i.Amount,
        i.Payment.Interval, i.Payment.Method, i.Payment.PaymentDate, i.Payment.Reference,
        i.MemberSnapshot, i.OrganizationSnapshot, i.Status, i.CancelledAt, i.CancellationReason,
        i.EmailStatus, i.EmailSentAt, i.EmailLastError, i.CreatedAt);
}
