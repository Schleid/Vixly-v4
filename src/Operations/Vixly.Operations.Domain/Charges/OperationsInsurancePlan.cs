using Vixly.Operations.Domain.Common;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Operations.Domain.Charges;

// =========================================================
// FILE: OperationsInsurancePlan.cs
//
// PURPOSE:
// An insurance product the company sells, in dated versions (owner, Slice 4; effective dates, Slice 4B; full terms, Slice 4C;
// several products per mode, each with a name and a pitch, Slice 5D: "insurance A for 10 dollars, insurance B for 15, which one do you want?").
//
// RESPONSIBILITIES:
// - Schedule a version, remove a scheduled one, find the version in force at a moment
// - Take the product in or out of use; name it, describe it in a few words in each language, order it among the mode's products
//
// IMPORTANT NOTES:
// - Charged only when the customer insures the shipment with this product
// - The pitch is optional: without it the pages describe the product from its terms
// =========================================================
public sealed class OperationsInsurancePlan : OperationsEntity
{
    private readonly List<OperationsInsuranceVersion> _versions = new();

    private OperationsInsurancePlan()
    {
    }

    private OperationsInsurancePlan(Guid tenantId, DateTimeOffset utcNow)
        : base(tenantId, utcNow)
    {
    }

    public const int NameMaxLength = 60;

    public const int PitchMaxLength = 400;

    // Slice 4C: the mode its terms are written for; Slice 5D: several products per mode.
    public OperationsTransportMode Mode { get; private set; } = OperationsTransportMode.Air;

    // Slice 5D: what the clerk and the customer call it ("Basic", "Full value").
    public string Name { get; private set; } = string.Empty;

    // Slice 5D: a few words to read aloud at the counter, in each language; none: described from its terms.
    public string? PitchEn { get; private set; }

    public string? PitchFr { get; private set; }

    public string? PitchEs { get; private set; }

    // Slice 5D: its place in the mode's list, lowest first.
    public int Position { get; private set; }

    public string? Pitch(string language) => language switch
    {
        "fr" => PitchFr ?? PitchEn,
        "es" => PitchEs ?? PitchEn,
        _ => PitchEn
    };

    public void Describe(string? name, string? pitchEn, string? pitchFr, string? pitchEs, int position, DateTimeOffset utcNow)
    {
        Name = OperationsChargeLimits.Text(name, NameMaxLength, OperationsChargeErrors.TextInvalid) ?? throw new DomainException(OperationsChargeErrors.NameRequired);
        PitchEn = OperationsChargeLimits.Text(pitchEn, PitchMaxLength, OperationsChargeErrors.TextInvalid);
        PitchFr = OperationsChargeLimits.Text(pitchFr, PitchMaxLength, OperationsChargeErrors.TextInvalid);
        PitchEs = OperationsChargeLimits.Text(pitchEs, PitchMaxLength, OperationsChargeErrors.TextInvalid);
        Position = position is >= 0 and <= 99 ? position : throw new DomainException(OperationsChargeErrors.ChoiceInvalid);
        MarkAsModified(utcNow);
    }

    public IReadOnlyList<OperationsInsuranceVersion> Versions => _versions;

    public bool IsActive { get; private set; }

    // Slice 5B: the company policy this cover sits under; none: the company insures it itself.
    public Guid? PolicyId { get; private set; }

    public void SitUnder(Guid? policyId, DateTimeOffset utcNow)
    {
        PolicyId = policyId;
        MarkAsModified(utcNow);
    }

    public static OperationsInsurancePlan Create(Guid tenantId, OperationsTransportMode mode, OperationsInsuranceTerms terms, DateTimeOffset? effectiveFrom, DateTimeOffset utcNow,
        string? name = null)
    {
        OperationsInsurancePlan plan = new(tenantId, utcNow) { IsActive = true, Mode = Enum.IsDefined(mode) ? mode : throw new DomainException(OperationsChargeErrors.ChoiceInvalid) };
        plan.Describe(name ?? mode + " insurance", null, null, null, 0, utcNow);
        plan.Schedule(terms, effectiveFrom, utcNow);
        return plan;
    }

    public OperationsInsuranceVersion Schedule(OperationsInsuranceTerms terms, DateTimeOffset? effectiveFrom, DateTimeOffset utcNow)
    {
        OperationsInsuranceVersion version = OperationsInsuranceVersion.Create(terms, OperationsVersions.Start(effectiveFrom, utcNow));
        OperationsVersions.Schedule(_versions, version, utcNow);
        MarkAsModified(utcNow);
        return version;
    }

    public void RemoveScheduled(Guid versionId, DateTimeOffset utcNow)
    {
        OperationsVersions.Remove(_versions, versionId, utcNow);
        MarkAsModified(utcNow);
    }

    public void ChangeStatus(bool active, DateTimeOffset utcNow)
    {
        IsActive = active;
        MarkAsModified(utcNow);
    }

    public OperationsInsuranceVersion? VersionAt(DateTimeOffset at) => IsActive ? TermsAt(at) : null;

    // The terms in force at a moment, in use or not: what the staff form shows and compares with.
    public OperationsInsuranceVersion? TermsAt(DateTimeOffset at) => OperationsVersions.At(_versions, at);
}
