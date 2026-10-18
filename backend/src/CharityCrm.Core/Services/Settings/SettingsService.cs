using CharityCrm.Core.Abstractions;
using CharityCrm.Core.Common;
using CharityCrm.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace CharityCrm.Core.Services.Settings;

public sealed record SettingsDto(
    string Name,
    string Street,
    string PostalCode,
    string City,
    string Country,
    string? Email,
    string? Phone,
    string? Iban,
    string? Bic,
    string? ZvrNumber,
    string? InvoiceFooterText,
    decimal MonthlyFee,
    decimal YearlyFee,
    DateTimeOffset UpdatedAt);

public sealed record UpdateSettingsRequest(
    string Name,
    string Street,
    string PostalCode,
    string City,
    string? Country,
    string? Email,
    string? Phone,
    string? Iban,
    string? Bic,
    string? ZvrNumber,
    string? InvoiceFooterText,
    decimal MonthlyFee,
    decimal YearlyFee);

/// <summary>Organization data and default fees (FR-FEE-01/02). Changes never touch existing payments or invoices.</summary>
public sealed class SettingsService(IAppDbContext db, IAuditLogger audit, IClock clock)
{
    public async Task<SettingsDto> GetAsync() => ToDto(await LoadAsync());

    public async Task<OrganizationSettings> LoadAsync() =>
        await db.OrganizationSettings.SingleOrDefaultAsync(s => s.Id == OrganizationSettings.SingletonId)
        ?? throw new InvalidOperationException("Organization settings row is missing.");

    public async Task<SettingsDto> UpdateAsync(UpdateSettingsRequest request)
    {
        var s = await LoadAsync();
        var country = request.Country.TrimToNull() ?? Member.DefaultCountry;
        var iban = NormalizeIban(request.Iban);

        var changes = new AuditChanges()
            .Track("name", s.Name, request.Name.Trim())
            .Track("street", s.Street, request.Street.Trim())
            .Track("postalCode", s.PostalCode, request.PostalCode.Trim())
            .Track("city", s.City, request.City.Trim())
            .Track("country", s.Country, country)
            .Track("email", s.Email, request.Email.TrimToNull())
            .Track("phone", s.Phone, request.Phone.TrimToNull())
            .Track("iban", s.Iban, iban)
            .Track("bic", s.Bic, request.Bic.TrimToNull()?.ToUpperInvariant())
            .Track("zvrNumber", s.ZvrNumber, request.ZvrNumber.TrimToNull())
            .Track("invoiceFooterText", s.InvoiceFooterText, request.InvoiceFooterText.TrimToNull())
            .Track("monthlyFee", s.MonthlyFee, request.MonthlyFee)
            .Track("yearlyFee", s.YearlyFee, request.YearlyFee);

        s.Name = request.Name.Trim();
        s.Street = request.Street.Trim();
        s.PostalCode = request.PostalCode.Trim();
        s.City = request.City.Trim();
        s.Country = country;
        s.Email = request.Email.TrimToNull();
        s.Phone = request.Phone.TrimToNull();
        s.Iban = iban;
        s.Bic = request.Bic.TrimToNull()?.ToUpperInvariant();
        s.ZvrNumber = request.ZvrNumber.TrimToNull();
        s.InvoiceFooterText = request.InvoiceFooterText.TrimToNull();
        s.MonthlyFee = request.MonthlyFee;
        s.YearlyFee = request.YearlyFee;
        s.UpdatedAt = clock.UtcNow;

        if (!changes.IsEmpty)
        {
            audit.Add(AuditActions.SettingsUpdated, AuditEntityTypes.Settings, s.Id.ToString(), changes);
        }
        await db.SaveChangesAsync();
        return ToDto(s);
    }

    /// <summary>Stores IBANs without spaces in upper case ("AT61 1904 ..." → "AT611904...").</summary>
    public static string? NormalizeIban(string? iban) =>
        iban.TrimToNull()?.Replace(" ", "").ToUpperInvariant();

    private static SettingsDto ToDto(OrganizationSettings s) => new(
        s.Name, s.Street, s.PostalCode, s.City, s.Country, s.Email, s.Phone, s.Iban, s.Bic, s.ZvrNumber,
        s.InvoiceFooterText, s.MonthlyFee, s.YearlyFee, s.UpdatedAt);
}
