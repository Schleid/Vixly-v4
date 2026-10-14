using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Domain.Subscriptions;

// =========================================================
// FILE: PlatformPlanVersion.cs
//
// PURPOSE:
// One priced version of a plan (D-S2).
//
// RESPONSIBILITIES:
// - Hold prices, setup fee, currency, minimum licences, trial, limits and features
// - Be editable while a draft and frozen once published
//
// IMPORTANT NOTES:
// - Part of the PlatformPlan aggregate; changed only through the plan
// - Subscriptions point at the version they pay for; publishing a new version never changes their current period (owner, D-S2)
// =========================================================
public sealed class PlatformPlanVersion : AuditableEntity
{
    private readonly List<PlatformPlanVersionLimit> _limits = new();
    private readonly List<PlatformPlanVersionFeature> _features = new();

    private PlatformPlanVersion()
    {
        CurrencyCode = string.Empty;
    }

    private PlatformPlanVersion(Guid platformPlanId, int number, PlatformPlanVersionTerms terms, DateTimeOffset createdUtc)
        : base(createdUtc)
    {
        PlatformPlanId = platformPlanId;
        Number = number;
        Status = PlatformPlanVersionStatus.Draft;
        CurrencyCode = terms.CurrencyCode;
        Apply(terms);
    }

    public Guid PlatformPlanId { get; private set; }

    public int Number { get; private set; }

    public PlatformPlanVersionStatus Status { get; private set; }

    public string CurrencyCode { get; private set; }

    public decimal MonthlyPricePerLicence { get; private set; }

    public decimal AnnualPricePerLicence { get; private set; }

    public decimal SetupFee { get; private set; }

    public int MinimumLicences { get; private set; }

    public int? TrialDays { get; private set; }

    public DateTimeOffset? PublishedUtc { get; private set; }

    public Guid? PublishedByUserId { get; private set; }

    public Guid? SubmittedByUserId { get; private set; }

    public DateTimeOffset? SubmittedUtc { get; private set; }

    public Guid? RejectedByUserId { get; private set; }

    public string? RejectionReason { get; private set; }

    public IReadOnlyList<PlatformPlanVersionLimit> Limits => _limits;

    public IReadOnlyList<PlatformPlanVersionFeature> Features => _features;

    public bool IsDraft => Status is PlatformPlanVersionStatus.Draft or PlatformPlanVersionStatus.PendingApproval;

    public bool IsAwaitingApproval => Status == PlatformPlanVersionStatus.PendingApproval;

    public bool OffersTrial => TrialDays is > 0;

    public PlatformPlanVersionTerms Terms => PlatformPlanVersionTerms.Create(
        CurrencyCode,
        MonthlyPricePerLicence,
        AnnualPricePerLicence,
        SetupFee,
        MinimumLicences,
        TrialDays,
        Limit(PlatformPlanLimitKind.MaxLicensedDevices),
        Limit(PlatformPlanLimitKind.MaxWarehouses),
        _features.Select(item => item.Feature));

    public int Limit(PlatformPlanLimitKind kind)
    {
        return _limits.FirstOrDefault(limit => limit.Kind == kind)?.Value ?? 0;
    }

    public bool Includes(PlatformPlanFeature feature)
    {
        return _features.Any(item => item.Feature == feature);
    }

    public decimal PricePerLicence(PlatformSubscriptionTerm term)
    {
        return term switch
        {
            PlatformSubscriptionTerm.Monthly => MonthlyPricePerLicence,
            PlatformSubscriptionTerm.Annual => AnnualPricePerLicence,
            _ => throw new DomainException(PlatformSubscriptionErrors.TermInvalid)
        };
    }

    public bool Offers(PlatformSubscriptionTerm term)
    {
        return Enum.IsDefined(term) && PricePerLicence(term) > 0m;
    }

    internal static PlatformPlanVersion Draft(Guid platformPlanId, int number, PlatformPlanVersionTerms terms, DateTimeOffset utcNow)
    {
        ArgumentNullException.ThrowIfNull(terms);

        return new PlatformPlanVersion(platformPlanId, number, terms, utcNow);
    }

    internal void Edit(PlatformPlanVersionTerms terms, bool currencyLocked, DateTimeOffset utcNow)
    {
        ArgumentNullException.ThrowIfNull(terms);

        if (!IsDraft)
        {
            throw new DomainException(PlatformPlanErrors.NoDraft);
        }

        if (IsAwaitingApproval)
        {
            throw new DomainException(PlatformPlanErrors.DraftSubmitted);
        }

        if (currencyLocked && terms.CurrencyCode != CurrencyCode)
        {
            throw new DomainException(PlatformPlanErrors.CurrencyFixed);
        }

        CurrencyCode = terms.CurrencyCode;
        Apply(terms);
        MarkAsModified(utcNow);
    }

    internal void Submit(Guid actorUserId, DateTimeOffset utcNow)
    {
        if (Status != PlatformPlanVersionStatus.Draft)
        {
            throw new DomainException(IsAwaitingApproval ? PlatformPlanErrors.DraftSubmitted : PlatformPlanErrors.NoDraft);
        }

        RequirePrice();
        Status = PlatformPlanVersionStatus.PendingApproval;
        SubmittedByUserId = actorUserId;
        SubmittedUtc = utcNow;
        RejectedByUserId = null;
        RejectionReason = null;
        MarkAsModified(utcNow);
    }

    internal void Reject(Guid actorUserId, string reason, DateTimeOffset utcNow)
    {
        if (!IsAwaitingApproval)
        {
            throw new DomainException(PlatformPlanErrors.DraftNotSubmitted);
        }

        Status = PlatformPlanVersionStatus.Draft;
        RejectedByUserId = actorUserId;
        RejectionReason = reason;
        MarkAsModified(utcNow);
    }

    internal void Publish(Guid actorUserId, DateTimeOffset utcNow)
    {
        if (!IsDraft)
        {
            throw new DomainException(PlatformPlanErrors.NoDraft);
        }

        RequirePrice();

        Status = PlatformPlanVersionStatus.Published;
        PublishedUtc = utcNow;
        PublishedByUserId = actorUserId;
        MarkAsModified(utcNow);
    }

    private void RequirePrice()
    {
        if (!(MonthlyPricePerLicence > 0m || AnnualPricePerLicence > 0m))
        {
            throw new DomainException(PlatformPlanErrors.PriceRequired);
        }
    }

    internal void Supersede(DateTimeOffset utcNow)
    {
        Status = PlatformPlanVersionStatus.Superseded;
        MarkAsModified(utcNow);
    }

    private void Apply(PlatformPlanVersionTerms terms)
    {
        MonthlyPricePerLicence = terms.MonthlyPricePerLicence;
        AnnualPricePerLicence = terms.AnnualPricePerLicence;
        SetupFee = terms.SetupFee;
        MinimumLicences = terms.MinimumLicences;
        TrialDays = terms.TrialDays;

        _limits.Clear();
        _limits.Add(new PlatformPlanVersionLimit(PlatformPlanLimitKind.MaxLicensedDevices, terms.MaxLicensedDevices));
        _limits.Add(new PlatformPlanVersionLimit(PlatformPlanLimitKind.MaxWarehouses, terms.MaxWarehouses));

        _features.Clear();
        _features.AddRange(terms.Features.Select(feature => new PlatformPlanVersionFeature(feature)));
    }
}
