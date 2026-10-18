using CharityCrm.Core.Entities;

namespace CharityCrm.Core.Services.Payments;

/// <param name="Month">1–12 for monthly payers, null for yearly payers.</param>
public sealed record RecordPaymentRequest(
    Guid MemberId,
    int Year,
    int? Month,
    decimal Amount,
    DateOnly PaymentDate,
    PaymentMethod Method,
    string? Reference);

public sealed record RecordPaymentResult(Guid PaymentId, Guid InvoiceId, string InvoiceNumber);

public sealed record PaymentDto(
    Guid Id,
    Guid MemberId,
    string MemberNumber,
    string MemberName,
    BillingInterval Interval,
    DateOnly PeriodStart,
    DateOnly PeriodEnd,
    decimal Amount,
    DateOnly PaymentDate,
    PaymentMethod Method,
    string? Reference,
    PaymentStatus Status,
    Guid? InvoiceId,
    string? InvoiceNumber,
    DateTimeOffset CreatedAt);

/// <param name="PeriodYear">Payments whose period overlaps this year (or this month if <paramref name="PeriodMonth"/> is set).</param>
public sealed record PaymentListQuery(
    DateOnly? From,
    DateOnly? To,
    Guid? MemberId,
    PaymentMethod? Method,
    PaymentStatus? Status,
    int? PeriodYear,
    int? PeriodMonth,
    int? Page,
    int? PageSize);

/// <param name="TotalAmount">Sum of all matching payments (all pages), cancelled payments excluded (FR-PAY-02).</param>
public sealed record PaymentListResult(
    IReadOnlyList<PaymentDto> Items,
    int Page,
    int PageSize,
    int TotalCount,
    decimal TotalAmount);
