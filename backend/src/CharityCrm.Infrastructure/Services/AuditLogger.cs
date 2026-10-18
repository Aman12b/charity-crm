using CharityCrm.Core.Abstractions;
using CharityCrm.Core.Entities;
using CharityCrm.Infrastructure.Persistence;

namespace CharityCrm.Infrastructure.Services;

/// <summary>Adds audit entries to the request's DbContext so they commit atomically with the change (FR-AUD-01).</summary>
public sealed class AuditLogger(AppDbContext db, ICurrentUser currentUser, IClock clock) : IAuditLogger
{
    public void Add(string action, string? entityType = null, string? entityId = null, AuditChanges? changes = null, Guid? userId = null)
    {
        db.AuditLogs.Add(new AuditLog
        {
            Timestamp = clock.UtcNow,
            UserId = userId ?? currentUser.UserId,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            Changes = changes is null || changes.IsEmpty ? null : changes.ToJson(),
        });
    }
}
