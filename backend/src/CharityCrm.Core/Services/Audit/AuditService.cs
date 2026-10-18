using CharityCrm.Core.Abstractions;
using CharityCrm.Core.Common;
using Microsoft.EntityFrameworkCore;

namespace CharityCrm.Core.Services.Audit;

public sealed record AuditLogDto(
    long Id,
    DateTimeOffset Timestamp,
    Guid? UserId,
    string? UserName,
    string Action,
    string? EntityType,
    string? EntityId,
    string? Changes);

public sealed record AuditLogQuery(
    Guid? UserId,
    string? EntityType,
    string? EntityId,
    DateOnly? From,
    DateOnly? To,
    int? Page,
    int? PageSize);

/// <summary>FR-AUD-02: read-only, filterable audit log for admins.</summary>
public sealed class AuditService(IAppDbContext db)
{
    private static readonly TimeZoneInfo Vienna = TimeZoneInfo.FindSystemTimeZoneById("Europe/Vienna");

    public async Task<PagedResult<AuditLogDto>> ListAsync(AuditLogQuery query)
    {
        var page = Paging.NormalizePage(query.Page);
        var pageSize = Paging.NormalizePageSize(query.PageSize);

        var logs = db.AuditLogs.AsNoTracking();
        if (query.UserId is { } userId) logs = logs.Where(a => a.UserId == userId);
        if (!string.IsNullOrWhiteSpace(query.EntityType)) logs = logs.Where(a => a.EntityType == query.EntityType);
        if (!string.IsNullOrWhiteSpace(query.EntityId)) logs = logs.Where(a => a.EntityId == query.EntityId.Trim());
        // Date filters are calendar days in Vienna (NFR-DATA-03).
        if (query.From is { } from) { var start = StartOfDay(from); logs = logs.Where(a => a.Timestamp >= start); }
        if (query.To is { } to) { var end = StartOfDay(to.AddDays(1)); logs = logs.Where(a => a.Timestamp < end); }

        var total = await logs.CountAsync();
        var items = await logs
            .OrderByDescending(a => a.Timestamp).ThenByDescending(a => a.Id)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(a => new AuditLogDto(
                a.Id, a.Timestamp, a.UserId,
                db.Users.Where(u => u.Id == a.UserId).Select(u => u.FirstName + " " + u.LastName).FirstOrDefault(),
                a.Action, a.EntityType, a.EntityId, a.Changes))
            .ToListAsync();

        return new PagedResult<AuditLogDto>(items, page, pageSize, total);
    }

    private static DateTimeOffset StartOfDay(DateOnly date)
    {
        var local = date.ToDateTime(TimeOnly.MinValue);
        return new DateTimeOffset(local, Vienna.GetUtcOffset(local)).ToUniversalTime();
    }
}
