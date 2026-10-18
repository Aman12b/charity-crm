using CharityCrm.Core.Abstractions;
using CharityCrm.Core.Services.Invoices;
using CharityCrm.Core.Services.Payments;
using CharityCrm.Core.Services.Settings;
using FluentValidation;

namespace CharityCrm.Core.Validation;

public static class MoneyRules
{
    /// <summary>numeric(10,2): up to 8 integer digits and 2 decimals.</summary>
    public const decimal MaxAmount = 99_999_999.99m;

    public static IRuleBuilderOptions<T, decimal> ValidAmount<T>(this IRuleBuilder<T, decimal> rule) =>
        rule.GreaterThan(0).WithMessage("Der Betrag muss größer als 0 sein.")
            .LessThanOrEqualTo(MaxAmount).WithMessage("Der Betrag ist zu groß.")
            .Must(a => decimal.Round(a, 2) == a).WithMessage("Höchstens zwei Nachkommastellen.");
}

/// <summary>FR-PAY-01 field rules.</summary>
public sealed class RecordPaymentRequestValidator : AbstractValidator<RecordPaymentRequest>
{
    public RecordPaymentRequestValidator(IClock clock)
    {
        RuleFor(x => x.MemberId).NotEmpty().WithMessage("Bitte ein Mitglied wählen.");
        RuleFor(x => x.Year).InclusiveBetween(2000, 2100).WithMessage("Bitte ein gültiges Jahr wählen.");
        RuleFor(x => x.Month).InclusiveBetween(1, 12).When(x => x.Month is not null).WithMessage("Bitte einen gültigen Monat wählen.");
        RuleFor(x => x.Amount).ValidAmount();
        RuleFor(x => x.PaymentDate)
            .Must(d => d <= clock.Today).WithMessage("Das Zahlungsdatum darf nicht in der Zukunft liegen.");
        RuleFor(x => x.Method).IsInEnum().WithMessage("Bitte eine Zahlungsart wählen.");
        RuleFor(x => x.Reference).MaximumLength(200).WithMessage("Maximal 200 Zeichen.");
    }
}

public sealed class CancelInvoiceRequestValidator : AbstractValidator<CancelInvoiceRequest>
{
    public CancelInvoiceRequestValidator()
    {
        RuleFor(x => x.Reason).Must(r => !string.IsNullOrWhiteSpace(r)).WithMessage("Bitte einen Grund angeben.")
            .MaximumLength(500).WithMessage("Maximal 500 Zeichen.");
    }
}

/// <summary>FR-FEE-01/02</summary>
public sealed class UpdateSettingsRequestValidator : AbstractValidator<UpdateSettingsRequest>
{
    public UpdateSettingsRequestValidator()
    {
        RuleFor(x => x.Name).ValidName(200);
        RuleFor(x => x.Street).ValidName(200);
        RuleFor(x => x.PostalCode).ValidName(10);
        RuleFor(x => x.City).ValidName(100);
        RuleFor(x => x.Country).MaximumLength(100).WithMessage("Maximal 100 Zeichen.");
        RuleFor(x => x.Email).ValidEmail().When(x => !string.IsNullOrWhiteSpace(x.Email));
        RuleFor(x => x.Phone).MaximumLength(30).WithMessage("Maximal 30 Zeichen.");
        RuleFor(x => SettingsService.NormalizeIban(x.Iban))
            .Matches("^[A-Z]{2}[0-9]{2}[A-Z0-9]{11,30}$").WithMessage("Bitte eine gültige IBAN eingeben.")
            .When(x => !string.IsNullOrWhiteSpace(x.Iban))
            .OverridePropertyName("iban");
        RuleFor(x => x.Bic).Matches("^[A-Za-z0-9]{8}([A-Za-z0-9]{3})?$").WithMessage("Bitte einen gültigen BIC eingeben.")
            .When(x => !string.IsNullOrWhiteSpace(x.Bic));
        RuleFor(x => x.ZvrNumber).MaximumLength(20).WithMessage("Maximal 20 Zeichen.");
        RuleFor(x => x.InvoiceFooterText).MaximumLength(1000).WithMessage("Maximal 1000 Zeichen.");
        RuleFor(x => x.MonthlyFee).ValidAmount();
        RuleFor(x => x.YearlyFee).ValidAmount();
    }
}

public sealed class ResendInvoiceRequestValidator : AbstractValidator<ResendInvoiceRequest>
{
    public ResendInvoiceRequestValidator()
    {
        RuleFor(x => x.Email).ValidEmail().When(x => !string.IsNullOrWhiteSpace(x.Email));
    }
}
