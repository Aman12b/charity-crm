namespace CharityCrm.Core.Entities;

/// <summary>Immutable record of a state-changing action or login (FR-AUD-01).</summary>
public class AuditLog
{
    public long Id { get; set; }
    public DateTimeOffset Timestamp { get; set; }

    /// <summary>Acting user; null for a failed login with an unknown email.</summary>
    public Guid? UserId { get; set; }

    public string Action { get; set; } = "";
    public string? EntityType { get; set; }
    public string? EntityId { get; set; }

    /// <summary>JSON: <c>{ "field": { "old": ..., "new": ... } }</c>.</summary>
    public string? Changes { get; set; }
}
