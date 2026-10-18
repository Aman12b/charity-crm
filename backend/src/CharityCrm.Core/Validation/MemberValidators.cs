using CharityCrm.Core.Abstractions;
using CharityCrm.Core.Services.Members;
using FluentValidation;

namespace CharityCrm.Core.Validation;

/// <summary>Field rules of FR-MEM-01, shared by create and update.</summary>
public abstract class MemberInputValidator<T> : AbstractValidator<T> where T : IMemberInput
{
    public const int MaxAgeYears = 120;

    protected MemberInputValidator(IClock clock)
    {
        RuleFor(x => x.FirstName).ValidName();
        RuleFor(x => x.LastName).ValidName();
        RuleFor(x => x.DateOfBirth)
            .Must(d => d < clock.Today).WithMessage("Das Geburtsdatum muss in der Vergangenheit liegen.")
            .Must(d => d >= clock.Today.AddYears(-MaxAgeYears)).WithMessage($"Das Alter darf höchstens {MaxAgeYears} Jahre betragen.");
        RuleFor(x => x.Street).ValidName(200);
        RuleFor(x => x.PostalCode).ValidName(10);
        RuleFor(x => x.City).ValidName(100);
        RuleFor(x => x.Country).MaximumLength(100).WithMessage("Maximal 100 Zeichen.");
        RuleFor(x => x.Email).ValidEmail().When(x => !string.IsNullOrWhiteSpace(x.Email));
        RuleFor(x => x.Phone).MaximumLength(30).WithMessage("Maximal 30 Zeichen.");
        RuleFor(x => x.BillingInterval).IsInEnum().WithMessage("Bitte ein gültiges Zahlungsintervall wählen.");
        RuleFor(x => x.Notes).MaximumLength(2000).WithMessage("Maximal 2000 Zeichen.");
        RuleFor(x => x.JoinDate)
            .Must((x, join) => join is null || join.Value >= x.DateOfBirth)
            .WithMessage("Das Eintrittsdatum darf nicht vor dem Geburtsdatum liegen.");
    }
}

public sealed class CreateMemberRequestValidator(IClock clock) : MemberInputValidator<CreateMemberRequest>(clock);

public sealed class UpdateMemberRequestValidator(IClock clock) : MemberInputValidator<UpdateMemberRequest>(clock);

public sealed class DuplicateCheckRequestValidator : AbstractValidator<DuplicateCheckRequest>
{
    public DuplicateCheckRequestValidator()
    {
        RuleFor(x => x.FirstName).ValidName();
        RuleFor(x => x.LastName).ValidName();
    }
}

public sealed class DeregisterMemberRequestValidator : AbstractValidator<DeregisterMemberRequest>
{
    public DeregisterMemberRequestValidator()
    {
        RuleFor(x => x.Reason).MaximumLength(500).WithMessage("Maximal 500 Zeichen.");
    }
}
