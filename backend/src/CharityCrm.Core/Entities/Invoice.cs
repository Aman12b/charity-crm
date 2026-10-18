namespace CharityCrm.Core.Entities;

public enum InvoiceStatus
{
    Issued,
    Cancelled,
}

public enum EmailStatus
{
    NotApplicable,
    Pending,
    Sent,
    Failed,
}

/// <summary>
/// Generated for every payment and immutable afterwards (FR-INV-01/03). Member and organization data are
/// snapshots taken at issue time, so later edits never change an existing invoice.
/// </summary>
public class Invoice
{
    public Guid Id { get; set; }

    /// <summary>"YYYY-00001", gap-free per year.</summary>
    public string InvoiceNumber { get; set; } = "";

    public Guid PaymentId { get; set; }
    public Payment Payment { get; set; } = null!;
    public Guid MemberId { get; set; }
    public DateOnly IssueDate { get; set; }
    public decimal Amount { get; set; }
    public DateOnly PeriodStart { get; set; }
    public DateOnly PeriodEnd { get; set; }
    public MemberSnapshot MemberSnapshot { get; set; } = null!;
    public OrganizationSnapshot OrganizationSnapshot { get; set; } = null!;
    public InvoiceStatus Status { get; set; } = InvoiceStatus.Issued;
    public DateTimeOffset? CancelledAt { get; set; }
    public string? CancellationReason { get; set; }

    /// <summary>Relative to the invoice storage root: "{year}/{number}.pdf".</summary>
    public string PdfPath { get; set; } = "";

    public EmailStatus EmailStatus { get; set; }
    public DateTimeOffset? EmailSentAt { get; set; }
    public string? EmailLastError { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public class MemberSnapshot
{
    public string MemberNumber { get; set; } = "";
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string Street { get; set; } = "";
    public string PostalCode { get; set; } = "";
    public string City { get; set; } = "";
    public string Country { get; set; } = "";
    public string? Email { get; set; }

    public static MemberSnapshot From(Member m) => new()
    {
        MemberNumber = m.MemberNumber,
        FirstName = m.FirstName,
        LastName = m.LastName,
        Street = m.Street,
        PostalCode = m.PostalCode,
        City = m.City,
        Country = m.Country,
        Email = m.Email,
    };
}

public class OrganizationSnapshot
{
    public string Name { get; set; } = "";
    public string Street { get; set; } = "";
    public string PostalCode { get; set; } = "";
    public string City { get; set; } = "";
    public string Country { get; set; } = "";
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Iban { get; set; }
    public string? Bic { get; set; }
    public string? ZvrNumber { get; set; }
    public string? FooterText { get; set; }

    public static OrganizationSnapshot From(OrganizationSettings s) => new()
    {
        Name = s.Name,
        Street = s.Street,
        PostalCode = s.PostalCode,
        City = s.City,
        Country = s.Country,
        Email = s.Email,
        Phone = s.Phone,
        Iban = s.Iban,
        Bic = s.Bic,
        ZvrNumber = s.ZvrNumber,
        FooterText = s.InvoiceFooterText,
    };
}

/// <summary>Last allocated invoice number per year (row-locked during allocation, NFR-REL-03).</summary>
public class InvoiceNumberSequence
{
    public int Year { get; set; }
    public int LastNumber { get; set; }
}
