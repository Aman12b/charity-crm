using System.Security.Claims;
using CharityCrm.Api.Setup;
using CharityCrm.Core.Abstractions;
using CharityCrm.Core.Entities;
using CharityCrm.Core.Services.Audit;
using CharityCrm.Core.Services.Reports;

namespace CharityCrm.Api.Features.Reports;

public static class ReportEndpoints
{
    public static void MapReportEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/reports").WithTags("Reports");

        // Every role sees the dashboard; financial figures only for Admin and Cashier (FR-REP-01).
        group.MapGet("/dashboard", (ClaimsPrincipal user, ReportService reports) =>
                reports.DashboardAsync(user.IsInRole(nameof(UserRole.Admin)) || user.IsInRole(nameof(UserRole.Cashier))))
            .RequireAuthorization();

        var finance = group.MapGroup("").RequireAuthorization(Policies.Finance);

        finance.MapGet("/open-contributions", ([AsParameters] OpenContributionsQuery query, ReportService reports) =>
            reports.OpenContributionsAsync(query));

        finance.MapGet("/payment-summary", (int? year, ReportService reports) => reports.PaymentSummaryAsync(year));

        finance.MapGet("/payment-summary.csv", async (int? year, ReportService reports, IClock clock) =>
            Results.File(await reports.PaymentSummaryCsvAsync(year), "text/csv; charset=utf-8",
                $"Zahlungsuebersicht_{year ?? clock.Today.Year}.csv"));

        app.MapGet("/api/audit", ([AsParameters] AuditLogQuery query, AuditService audit) => audit.ListAsync(query))
            .WithTags("Audit")
            .RequireAuthorization(Policies.AdminOnly);
    }
}
