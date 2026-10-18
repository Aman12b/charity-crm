using System.Globalization;
using CharityCrm.Core.Abstractions;
using CharityCrm.Core.Common;
using CharityCrm.Core.Entities;
using CharityCrm.Core.Services.Payments;
using Microsoft.EntityFrameworkCore;

namespace CharityCrm.Core.Services.Reports;

/// <param name="PaymentsThisMonthCount">Finance figures are null for roles without financial access (FR-REP-01).</param>
public sealed record DashboardDto(
    int ActiveMembers,
    int NewMembersThisMonth,
    int? PaymentsThisMonthCount,
    decimal? PaymentsThisMonthSum,
    int? FailedEmails,
    int? OpenContributions);

public sealed record OpenContributionDto(
    Guid MemberId,
    string MemberNumber,
    string FirstName,
    string LastName,
    string? Email,
    BillingInterval BillingInterval,
    DateOnly PeriodStart,
    DateOnly PeriodEnd,
    decimal ExpectedAmount);

public sealed record OpenContributionsQuery(int? Year, int? Month, string? Search, int? Page, int? PageSize);

public sealed record OpenContributionsResult(
    int Year,
    int Month,
    IReadOnlyList<OpenContributionDto> Items,
    int Page,
    int PageSize,
    int TotalCount);

public sealed record MonthlySumDto(int Month, int Count, decimal Sum);

public sealed record PaymentSummaryDto(int Year, IReadOnlyList<MonthlySumDto> Months, int TotalCount, decimal TotalSum);

/// <summary>Dashboard, open contributions and payment summary (FR-REP-01..03).</summary>
public sealed class ReportService(IAppDbContext db, IClock clock)
{
    public async Task<DashboardDto> DashboardAsync(bool includeFinance)
    {
        var today = clock.Today;
        var month = ContributionPeriod.Month(today.Year, today.Month);

        var activeMembers = await db.Members.CountAsync(m => m.Status == MemberStatus.Active);
        var newMembers = await db.Members.CountAsync(m => m.JoinDate >= month.Start && m.JoinDate <= month.End);
        if (!includeFinance)
        {
            return new DashboardDto(activeMembers, newMembers, null, null, null, null);
        }

        var paymentsThisMonth = db.Payments.Where(p => p.Status == PaymentStatus.Recorded
            && p.PaymentDate >= month.Start && p.PaymentDate <= month.End);
        return new DashboardDto(
            activeMembers,
            newMembers,
            await paymentsThisMonth.CountAsync(),
            await paymentsThisMonth.SumAsync(p => (decimal?)p.Amount) ?? 0m,
            await db.Invoices.CountAsync(i => i.EmailStatus == EmailStatus.Failed),
            await OpenMembers(today.Year, today.Month).CountAsync());
    }

    /// <summary>
    /// FR-REP-02: active members without a recorded payment for the reference period – the month for monthly payers,
    /// the year for yearly payers. "Open" uses the same overlap rule as recording, so every listed member can pay.
    /// </summary>
    public async Task<OpenContributionsResult> OpenContributionsAsync(OpenContributionsQuery query)
    {
        var today = clock.Today;
        var year = query.Year is >= 2000 and <= 2100 ? query.Year.Value : today.Year;
        var month = query.Month is >= 1 and <= 12 ? query.Month.Value : today.Month;
        var page = Paging.NormalizePage(query.Page);
        var pageSize = Paging.NormalizePageSize(query.PageSize);

        var members = OpenMembers(year, month);
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var pattern = SearchPattern.Contains(query.Search);
            members = members.Where(m => EF.Functions.ILike(m.MemberNumber, pattern, SearchPattern.EscapeChar)
                || EF.Functions.ILike(m.LastName + " " + m.FirstName, pattern, SearchPattern.EscapeChar)
                || EF.Functions.ILike(m.FirstName + " " + m.LastName, pattern, SearchPattern.EscapeChar));
        }

        var total = await members.CountAsync();
        var rows = await members
            .OrderBy(m => m.LastName).ThenBy(m => m.FirstName).ThenBy(m => m.MemberNumber)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .ToListAsync();

        var settings = await db.OrganizationSettings.AsNoTracking().SingleAsync();
        var monthly = ContributionPeriod.Month(year, month);
        var yearly = ContributionPeriod.Year(year);
        var items = rows.Select(m =>
        {
            var (period, fee) = m.BillingInterval == BillingInterval.Monthly
                ? (monthly, settings.MonthlyFee)
                : (yearly, settings.YearlyFee);
            return new OpenContributionDto(m.Id, m.MemberNumber, m.FirstName, m.LastName, m.Email, m.BillingInterval,
                period.Start, period.End, fee);
        }).ToList();

        return new OpenContributionsResult(year, month, items, page, pageSize, total);
    }

    /// <summary>FR-REP-03: recorded payments per month of the payment date, plus the yearly total.</summary>
    public async Task<PaymentSummaryDto> PaymentSummaryAsync(int? year)
    {
        var y = year is >= 2000 and <= 2100 ? year.Value : clock.Today.Year;
        var period = ContributionPeriod.Year(y);

        var grouped = await db.Payments.AsNoTracking()
            .Where(p => p.Status == PaymentStatus.Recorded && p.PaymentDate >= period.Start && p.PaymentDate <= period.End)
            .GroupBy(p => p.PaymentDate.Month)
            .Select(g => new { Month = g.Key, Count = g.Count(), Sum = g.Sum(p => p.Amount) })
            .ToListAsync();

        var months = Enumerable.Range(1, 12)
            .Select(m => grouped.FirstOrDefault(g => g.Month == m) is { } g
                ? new MonthlySumDto(m, g.Count, g.Sum)
                : new MonthlySumDto(m, 0, 0m))
            .ToList();
        return new PaymentSummaryDto(y, months, months.Sum(m => m.Count), months.Sum(m => m.Sum));
    }

    public async Task<byte[]> PaymentSummaryCsvAsync(int? year)
    {
        var summary = await PaymentSummaryAsync(year);
        var csv = new CsvWriter().Row("Monat", "Anzahl", "Summe (EUR)");
        foreach (var m in summary.Months)
        {
            csv.Row($"{GermanFormat.MonthName(m.Month)} {summary.Year}", m.Count.ToString(CultureInfo.InvariantCulture),
                GermanFormat.Number(m.Sum));
        }
        csv.Row($"Gesamt {summary.Year}", summary.TotalCount.ToString(CultureInfo.InvariantCulture), GermanFormat.Number(summary.TotalSum));
        return csv.ToBytes();
    }

    /// <summary>Active members who had joined by the end of the period and have no recorded payment overlapping it.</summary>
    private IQueryable<Member> OpenMembers(int year, int month)
    {
        var monthly = ContributionPeriod.Month(year, month);
        var yearly = ContributionPeriod.Year(year);

        return db.Members.AsNoTracking().Where(m => m.Status == MemberStatus.Active && (
            (m.BillingInterval == BillingInterval.Monthly && m.JoinDate <= monthly.End
                && !db.Payments.Any(p => p.MemberId == m.Id && p.Status == PaymentStatus.Recorded
                    && p.PeriodStart <= monthly.End && monthly.Start <= p.PeriodEnd))
            || (m.BillingInterval == BillingInterval.Yearly && m.JoinDate <= yearly.End
                && !db.Payments.Any(p => p.MemberId == m.Id && p.Status == PaymentStatus.Recorded
                    && p.PeriodStart <= yearly.End && yearly.Start <= p.PeriodEnd))));
    }
}
