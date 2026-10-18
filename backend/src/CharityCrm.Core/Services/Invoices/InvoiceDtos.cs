using CharityCrm.Core.Entities;

namespace CharityCrm.Core.Services.Invoices;

public sealed record InvoiceListItemDto(
    Guid Id,
    string InvoiceNumber,
    DateOnly IssueDate,
    Guid MemberId,
    string MemberNumber,
    string MemberName,
    DateOnly PeriodStart,
    DateOnly PeriodEnd,
    decimal Amount,
    InvoiceStatus Status,
    EmailStatus EmailStatus);

public sealed record InvoiceDto(
    Guid Id,
    string InvoiceNumber,
    DateOnly IssueDate,
    Guid MemberId,
    Guid PaymentId,
    DateOnly PeriodStart,
    DateOnly PeriodEnd,
    decimal Amount,
    BillingInterval Interval,
    PaymentMethod PaymentMethod,
    DateOnly PaymentDate,
    string? PaymentReference,
    MemberSnapshot Member,
    OrganizationSnapshot Organization,
    InvoiceStatus Status,
    DateTimeOffset? CancelledAt,
    string? CancellationReason,
    EmailStatus EmailStatus,
    DateTimeOffset? EmailSentAt,
    string? EmailLastError,
    DateTimeOffset CreatedAt);

/// <param name="Search">Matches the invoice number, member number or member name (snapshot).</param>
public sealed record InvoiceListQuery(
    string? Search,
    Guid? MemberId,
    DateOnly? From,
    DateOnly? To,
    InvoiceStatus? Status,
    EmailStatus? EmailStatus,
    int? Page,
    int? PageSize);

public sealed record CancelInvoiceRequest(string Reason);

public sealed record InvoicePdf(string FileName, byte[] Content);
