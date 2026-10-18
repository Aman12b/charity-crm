namespace CharityCrm.Core.Entities;

public enum PaymentMethod
{
    BankTransfer,
    Cash,
    Other,
}

public enum PaymentStatus
{
    Recorded,
    Cancelled,
}

/// <summary>Money received from a member for one contribution period (FR-PAY-01).</summary>
public class Payment
{
    public Guid Id { get; set; }
    public Guid MemberId { get; set; }
    public Member Member { get; set; } = null!;

    /// <summary>The member's billing interval at the time of payment.</summary>
    public BillingInterval Interval { get; set; }

    public DateOnly PeriodStart { get; set; }
    public DateOnly PeriodEnd { get; set; }
    public decimal Amount { get; set; }
    public DateOnly PaymentDate { get; set; }
    public PaymentMethod Method { get; set; }
    public string? Reference { get; set; }
    public PaymentStatus Status { get; set; } = PaymentStatus.Recorded;
    public Guid RecordedByUserId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public Invoice? Invoice { get; set; }
}
