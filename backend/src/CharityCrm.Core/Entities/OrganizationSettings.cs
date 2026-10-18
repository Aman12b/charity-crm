namespace CharityCrm.Core.Entities;

/// <summary>Organization data and default fees; exactly one row with Id = 1 (FR-FEE-01/02).</summary>
public class OrganizationSettings
{
    public const int SingletonId = 1;

    public int Id { get; set; } = SingletonId;
    public string Name { get; set; } = "";
    public string Street { get; set; } = "";
    public string PostalCode { get; set; } = "";
    public string City { get; set; } = "";
    public string Country { get; set; } = Member.DefaultCountry;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Iban { get; set; }
    public string? Bic { get; set; }
    public string? ZvrNumber { get; set; }
    public string? InvoiceFooterText { get; set; }
    public decimal MonthlyFee { get; set; }
    public decimal YearlyFee { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
