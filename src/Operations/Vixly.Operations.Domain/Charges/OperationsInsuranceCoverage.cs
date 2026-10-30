using Vixly.Shared.Kernel.Domain;

namespace Vixly.Operations.Domain.Charges;

// =========================================================
// FILE: OperationsInsuranceCoverage.cs
//
// PURPOSE:
// What an insurance covers and how a claim is paid: who covers, the risks, the exclusions, the liability without insurance, the payout and the claim rules (Slice 4C section 8).
//
// RESPONSIBILITIES:
// - Check and keep the terms of one version of an insurance plan
// - Give the industry-standard defaults for a new plan
//
// IMPORTANT NOTES:
// - Part of the version (JSON); a started version never changes
// - The claims workflow itself is a later slice
// =========================================================
public sealed class OperationsInsuranceCoverage
{
    public const int NameMaxLength = 120;

    public const int TermsMaxLength = 4000;

    public const int MaxClaimDays = 365;

    private OperationsInsuranceCoverage()
    {
    }

    public OperationsInsuranceCover Cover { get; private set; }

    public string? InsurerName { get; private set; }

    public string? PolicyNumber { get; private set; }

    public string? InsurerContact { get; private set; }

    public OperationsInsuredRisks Risks { get; private set; }

    public OperationsInsuranceExclusions Exclusions { get; private set; }

    // The company's own exclusions, one per line.
    public string? OtherExclusions { get; private set; }

    // What the company pays when the shipment was not insured: per lb or kg, and/or per shipment; none: nothing.
    public decimal? LiabilityPerUnit { get; private set; }

    public decimal? LiabilityPerShipment { get; private set; }

    public OperationsInsurancePayout Payout { get; private set; }

    public decimal? DeductibleAmount { get; private set; }

    public decimal? DeductiblePercent { get; private set; }

    public decimal? MaxPerItem { get; private set; }

    public int DamageClaimDays { get; private set; }

    public int LossClaimDays { get; private set; }

    public OperationsClaimProof Proof { get; private set; }

    public OperationsClaimFiler FiledBy { get; private set; }

    public string? TermsEn { get; private set; }

    public string? TermsFr { get; private set; }

    public string? TermsEs { get; private set; }

    // The policy's own words in the customer's language, English when not written in it (shown before the customer signs).
    public string? Terms(string? language) => language switch
    {
        "fr" => TermsFr ?? TermsEn,
        "es" => TermsEs ?? TermsEn,
        _ => TermsEn
    };

    // Common courier practice: own cover of loss and damage, the usual exclusions, paid on the lower of the declared and invoice value,
    // damage claimed within 7 days and loss within 21, with the invoice and photos, filed by the company.
    public static OperationsInsuranceCoverageTerms Defaults { get; } = new(OperationsInsuranceCover.Own, null, null, null, OperationsInsuredRisks.Loss | OperationsInsuredRisks.Damage,
        OperationsInsuranceExclusions.Standard, null, null, null, OperationsInsurancePayout.LowerOfDeclaredAndInvoice, null, null, null, 7, 21,
        OperationsClaimProof.Invoice | OperationsClaimProof.Photos, OperationsClaimFiler.Company, null, null, null);

    public static OperationsInsuranceCoverage Create(OperationsInsuranceCoverageTerms terms)
    {
        ArgumentNullException.ThrowIfNull(terms);

        if (!Enum.IsDefined(terms.Cover) || !Enum.IsDefined(terms.Payout) || !Enum.IsDefined(terms.FiledBy)
            || (terms.Risks & ~(OperationsInsuredRisks.Loss | OperationsInsuredRisks.Damage | OperationsInsuredRisks.Theft)) != OperationsInsuredRisks.None
            || terms.Risks == OperationsInsuredRisks.None
            || (terms.Exclusions & ~OperationsInsuranceExclusions.Standard) != OperationsInsuranceExclusions.None
            || (terms.Proof & ~(OperationsClaimProof.Invoice | OperationsClaimProof.Photos | OperationsClaimProof.NotedAtRelease)) != OperationsClaimProof.None
            || terms.DamageClaimDays is < 1 or > MaxClaimDays || terms.LossClaimDays is < 1 or > MaxClaimDays)
        {
            throw new DomainException(OperationsChargeErrors.InsuranceInvalid);
        }

        bool insurer = terms.Cover == OperationsInsuranceCover.Insurer;
        string? insurerName = Text(terms.InsurerName, NameMaxLength);

        // An insurer is named on the terms.
        if (insurer && insurerName is null)
        {
            throw new DomainException(OperationsChargeErrors.InsuranceInvalid);
        }

        return new OperationsInsuranceCoverage
        {
            Cover = terms.Cover,
            InsurerName = insurer ? insurerName : null,
            PolicyNumber = insurer ? Text(terms.PolicyNumber, NameMaxLength) : null,
            InsurerContact = insurer ? Text(terms.InsurerContact, NameMaxLength) : null,
            Risks = terms.Risks,
            Exclusions = terms.Exclusions,
            OtherExclusions = Text(terms.OtherExclusions, TermsMaxLength),
            LiabilityPerUnit = terms.LiabilityPerUnit is { } perUnit ? OperationsChargeLimits.UnitPrice(perUnit, OperationsChargeErrors.InsuranceInvalid) : null,
            LiabilityPerShipment = OperationsChargeLimits.Money(terms.LiabilityPerShipment, OperationsChargeErrors.InsuranceInvalid),
            Payout = terms.Payout,
            DeductibleAmount = OperationsChargeLimits.Money(terms.DeductibleAmount, OperationsChargeErrors.InsuranceInvalid),
            DeductiblePercent = terms.DeductiblePercent is { } percent ? OperationsChargeLimits.Percent(percent, OperationsChargeErrors.InsuranceInvalid) : null,
            MaxPerItem = terms.MaxPerItem is <= 0 ? throw new DomainException(OperationsChargeErrors.InsuranceInvalid)
                : OperationsChargeLimits.Money(terms.MaxPerItem, OperationsChargeErrors.InsuranceInvalid),
            DamageClaimDays = terms.DamageClaimDays,
            LossClaimDays = terms.LossClaimDays,
            Proof = terms.Proof,
            FiledBy = terms.FiledBy,
            TermsEn = Text(terms.TermsEn, TermsMaxLength),
            TermsFr = Text(terms.TermsFr, TermsMaxLength),
            TermsEs = Text(terms.TermsEs, TermsMaxLength)
        };
    }

    public OperationsInsuranceCoverageTerms ToTerms() => new(Cover, InsurerName, PolicyNumber, InsurerContact, Risks, Exclusions, OtherExclusions, LiabilityPerUnit,
        LiabilityPerShipment, Payout, DeductibleAmount, DeductiblePercent, MaxPerItem, DamageClaimDays, LossClaimDays, Proof, FiledBy, TermsEn, TermsFr, TermsEs);

    // The company's own text for a language (en, fr, es), else English.
    public string? TermsIn(string? language) => language switch
    {
        "fr" => TermsFr ?? TermsEn,
        "es" => TermsEs ?? TermsEn,
        _ => TermsEn
    };

    private static string? Text(string? value, int maxLength)
    {
        string text = (value ?? string.Empty).Replace("\r\n", "\n", StringComparison.Ordinal).Trim();

        return text.Length == 0 ? null
            : text.Length > maxLength || text.Any(character => char.IsControl(character) && character != '\n') || text.IndexOfAny(['<', '>']) >= 0
                ? throw new DomainException(OperationsChargeErrors.InsuranceInvalid)
                : text;
    }
}
