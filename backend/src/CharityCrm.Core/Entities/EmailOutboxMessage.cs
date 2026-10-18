namespace CharityCrm.Core.Entities;

public enum OutboxStatus
{
    Pending,
    Sent,
    Failed,
}

/// <summary>
/// An invoice email waiting to be sent. Written in the same transaction as the invoice (outbox pattern,
/// NFR-REL-01) and processed by a background worker, so SMTP problems never affect users (NFR-REL-02).
/// </summary>
public class EmailOutboxMessage
{
    public const int MaxAttempts = 5;

    public Guid Id { get; set; }
    public Guid InvoiceId { get; set; }
    public Invoice Invoice { get; set; } = null!;
    public string ToAddress { get; set; } = "";
    public int Attempts { get; set; }
    public DateTimeOffset NextAttemptAt { get; set; }
    public OutboxStatus Status { get; set; } = OutboxStatus.Pending;
    public string? LastError { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? ProcessedAt { get; set; }
}
