using CharityCrm.Core.Abstractions;
using CharityCrm.Core.Common;
using CharityCrm.Core.Entities;
using CharityCrm.Core.Services.Invoices;
using CharityCrm.Core.Services.Settings;
using Microsoft.EntityFrameworkCore;

namespace CharityCrm.Core.Services.Payments;

/// <summary>Recording and listing payments (FR-PAY-01/02).</summary>
public sealed class PaymentService(
    IAppDbContext db,
    IDbLocks locks,
    SettingsService settings,
    InvoiceIssuer invoices,
    IAuditLogger audit,
    ICurrentUser currentUser,
    IClock clock)
{
    /// <summary>
    /// Payment, invoice number, invoice, PDF and audit entries are created in one transaction (NFR-REL-01):
    /// either everything is stored or nothing.
    /// </summary>
    public async Task<RecordPaymentResult> RecordAsync(RecordPaymentRequest request, CancellationToken ct = default)
    {
        await using var tx = await db.BeginTransactionAsync(ct);

        // Serializes concurrent payments of the same member, so the overlap check below is reliable.
        await locks.LockMemberAsync(request.MemberId, ct);
        var member = await db.Members.SingleOrDefaultAsync(m => m.Id == request.MemberId, ct)
            ?? throw new NotFoundException("Mitglied nicht gefunden.");
        if (member.Status != MemberStatus.Active)
        {
            throw new BusinessRuleException("Für inaktive Mitglieder können keine Zahlungen erfasst werden.", "MEMBER_INACTIVE");
        }

        var period = ContributionPeriod.For(member.BillingInterval, request.Year, request.Month);
        await EnsureNoOverlapAsync(member.Id, period, ct);

        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            MemberId = member.Id,
            Member = member,
            Interval = member.BillingInterval,
            PeriodStart = period.Start,
            PeriodEnd = period.End,
            Amount = request.Amount,
            PaymentDate = request.PaymentDate,
            Method = request.Method,
            Reference = request.Reference.TrimToNull(),
            Status = PaymentStatus.Recorded,
            RecordedByUserId = currentUser.UserId ?? throw new InvalidOperationException("No current user."),
            CreatedAt = clock.UtcNow,
        };
        db.Payments.Add(payment);
        audit.Add(AuditActions.PaymentRecorded, AuditEntityTypes.Payment, payment.Id.ToString(), new AuditChanges()
            .Set("memberId", member.Id)
            .Set("period", period.ToString())
            .Set("amount", payment.Amount)
            .Set("paymentDate", payment.PaymentDate)
            .Set("method", payment.Method));

        var invoice = await invoices.IssueAsync(payment, member, await settings.LoadAsync(), ct);

        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        return new RecordPaymentResult(payment.Id, invoice.Id, invoice.InvoiceNumber);
    }

    public async Task<PaymentListResult> ListAsync(PaymentListQuery query)
    {
        var page = Paging.NormalizePage(query.Page);
        var pageSize = Paging.NormalizePageSize(query.PageSize);
        var payments = Filter(query);

        var total = await payments.CountAsync();
        var sum = await payments.Where(p => p.Status == PaymentStatus.Recorded).SumAsync(p => (decimal?)p.Amount) ?? 0m;
        var items = await Project(payments
            .OrderByDescending(p => p.PaymentDate).ThenByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize))
            .ToListAsync();

        return new PaymentListResult(items, page, pageSize, total, sum);
    }

    /// <summary>All payments of one member, newest period first (FR-MEM-03).</summary>
    public async Task<IReadOnlyList<PaymentDto>> ListForMemberAsync(Guid memberId) =>
        await Project(db.Payments.AsNoTracking()
            .Where(p => p.MemberId == memberId)
            .OrderByDescending(p => p.PeriodStart).ThenByDescending(p => p.CreatedAt))
            .ToListAsync();

    private async Task EnsureNoOverlapAsync(Guid memberId, ContributionPeriod period, CancellationToken ct)
    {
        // FR-PAY-01: no two non-cancelled payments may cover overlapping periods, which also prevents a yearly
        // payment over months that were already paid individually (and vice versa).
        var existing = await db.Payments.AsNoTracking()
            .Where(p => p.MemberId == memberId && p.Status == PaymentStatus.Recorded
                && p.PeriodStart <= period.End && period.Start <= p.PeriodEnd)
            .OrderBy(p => p.PeriodStart)
            .Select(p => new { p.PeriodStart, p.PeriodEnd })
            .ToListAsync(ct);

        if (existing.Count > 0)
        {
            var paid = string.Join(", ", existing.Select(p => GermanFormat.Period(p.PeriodStart, p.PeriodEnd)));
            throw new ConflictException(
                $"Für den Zeitraum {period} liegt bereits eine Zahlung vor (bezahlt: {paid}).", "DUPLICATE_PERIOD");
        }
    }

    private IQueryable<Payment> Filter(PaymentListQuery query)
    {
        var payments = db.Payments.AsNoTracking();
        if (query.From is { } from) payments = payments.Where(p => p.PaymentDate >= from);
        if (query.To is { } to) payments = payments.Where(p => p.PaymentDate <= to);
        if (query.MemberId is { } memberId) payments = payments.Where(p => p.MemberId == memberId);
        if (query.Method is { } method) payments = payments.Where(p => p.Method == method);
        if (query.Status is { } status) payments = payments.Where(p => p.Status == status);
        if (query.PeriodYear is { } year and >= 1 and <= 9999)
        {
            var period = query.PeriodMonth is { } month and >= 1 and <= 12
                ? ContributionPeriod.Month(year, month)
                : ContributionPeriod.Year(year);
            payments = payments.Where(p => p.PeriodStart <= period.End && period.Start <= p.PeriodEnd);
        }
        return payments;
    }

    private static IQueryable<PaymentDto> Project(IQueryable<Payment> payments) =>
        payments.Select(p => new PaymentDto(
            p.Id, p.MemberId, p.Member.MemberNumber, p.Member.LastName + " " + p.Member.FirstName,
            p.Interval, p.PeriodStart, p.PeriodEnd, p.Amount, p.PaymentDate, p.Method, p.Reference, p.Status,
            p.Invoice != null ? p.Invoice.Id : null,
            p.Invoice != null ? p.Invoice.InvoiceNumber : null,
            p.CreatedAt));
}
