namespace CharityCrm.Core.Abstractions;

public interface IAuditLogger
{
    /// <summary>Stages an audit entry; it is persisted together with the next SaveChanges (same transaction).</summary>
    void Add(string action, string? entityType = null, string? entityId = null, AuditChanges? changes = null, Guid? userId = null);
}
