using Vixly.Platform.Domain.Subscriptions;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Localization;

namespace Vixly.Platform.Domain.Sales;

// =========================================================
// FILE: PlatformPromotion.cs
//
// PURPOSE:
// A reusable offer such as "first month free", used at registration or applied by staff (D-S20, D-S26).
//
// RESPONSIBILITIES:
// - Hold the code, names, discount, dates, eligible plans and term, and the redemption limit
// - Go from Draft to Active only through CFO approval
// - Say whether it can be used for a plan and term now
//
// IMPORTANT NOTES:
// - Aggregate root. Designed by sales, approved by the CFO; nobody approves their own submission
// - A refused use gives a precise code without revealing other promotions
// - One use per tenant is checked by the use case against the tenant's discounts
// =========================================================
public sealed class PlatformPromotion : AuditableEntity
{
    public const int NameMaxLength = 100;

    private readonly List<PlatformPromotionName> _names = new();
    private readonly List<PlatformPromotionPlan> _plans = new();

    private PlatformPromotion()
    {
        Code = string.Empty;
        Terms = null!;
    }

    private PlatformPromotion(string code, Guid designedByUserId, DateTimeOffset utcNow)
        : base(utcNow)
    {
        Code = code;
        Terms = null!;
        DesignedByUserId = designedByUserId;
        Status = PlatformPromotionStatus.Draft;
    }

    public string Code { get; private set; }

    public PlatformDiscountTerms Terms { get; private set; }

    public DateTimeOffset ValidFromUtc { get; private set; }

    public DateTimeOffset? ValidToUtc { get; private set; }

    public PlatformSubscriptionTerm? Term { get; private set; }

    public bool AtRegistration { get; private set; }

    public bool ByStaff { get; private set; }

    public int? RedemptionLimit { get; private set; }

    public int Redemptions { get; private set; }

    public PlatformPromotionStatus Status { get; private set; }

    public Guid DesignedByUserId { get; private set; }

    public Guid? SubmittedByUserId { get; private set; }

    public Guid? DecidedByUserId { get; private set; }

    public DateTimeOffset? DecidedUtc { get; private set; }

    public string? DecisionReason { get; private set; }

    public IReadOnlyList<PlatformPromotionName> Names => _names;

    public IReadOnlyList<PlatformPromotionPlan> Plans => _plans;

    public bool IsEditable => Status is PlatformPromotionStatus.Draft or PlatformPromotionStatus.Rejected;

    // =====================================================
    // METHOD: CREATE AND EDIT
    // =====================================================
    public static PlatformPromotion Create(
        string? code,
        string? englishName,
        PlatformDiscountTerms terms,
        DateTimeOffset validFromUtc,
        DateTimeOffset? validToUtc,
        IReadOnlyCollection<Guid> planIds,
        PlatformSubscriptionTerm? term,
        bool atRegistration,
        bool byStaff,
        int? redemptionLimit,
        Guid actorUserId,
        DateTimeOffset utcNow)
    {
        Guid actor = actorUserId == Guid.Empty ? throw new DomainException(PlatformSalesErrors.ActorRequired) : actorUserId;
        string normalized;

        try
        {
            normalized = PlatformPlanCodeFormat.Normalize(code);
        }
        catch (DomainException)
        {
            throw new DomainException(PlatformSalesErrors.PromotionCodeInvalid);
        }

        PlatformPromotion promotion = new(normalized, actor, utcNow);
        promotion.SetName(LanguageCode.Default, englishName, utcNow);
        promotion.Define(terms, validFromUtc, validToUtc, planIds, term, atRegistration, byStaff, redemptionLimit, utcNow);

        return promotion;
    }

    public void Edit(
        PlatformDiscountTerms terms,
        DateTimeOffset validFromUtc,
        DateTimeOffset? validToUtc,
        IReadOnlyCollection<Guid> planIds,
        PlatformSubscriptionTerm? term,
        bool atRegistration,
        bool byStaff,
        int? redemptionLimit,
        DateTimeOffset utcNow)
    {
        RequireEditable();
        Define(terms, validFromUtc, validToUtc, planIds, term, atRegistration, byStaff, redemptionLimit, utcNow);
        Status = PlatformPromotionStatus.Draft;
    }

    public void SetName(LanguageCode language, string? name, DateTimeOffset utcNow)
    {
        ArgumentNullException.ThrowIfNull(language);
        string text = name?.Trim() ?? string.Empty;

        if (text.Length == 0)
        {
            throw new DomainException(PlatformSalesErrors.PromotionNameRequired);
        }

        if (text.Length > NameMaxLength)
        {
            throw new DomainException(PlatformSalesErrors.PromotionNameTooLong);
        }

        PlatformPromotionName? existing = _names.FirstOrDefault(item => item.Language == language.Value);

        if (existing is null)
        {
            _names.Add(new PlatformPromotionName(language.Value, text));
        }
        else
        {
            existing.Rename(text);
        }

        MarkAsModified(utcNow);
    }

    public string NameIn(LanguageCode language)
    {
        ArgumentNullException.ThrowIfNull(language);

        return (_names.FirstOrDefault(item => item.Language == language.Value)
            ?? _names.First(item => item.Language == LanguageCode.Default.Value)).Name;
    }

    // =====================================================
    // METHOD: APPROVAL (D-S26)
    // =====================================================
    public void Submit(Guid actorUserId, DateTimeOffset utcNow)
    {
        RequireEditable();
        SubmittedByUserId = actorUserId == Guid.Empty ? throw new DomainException(PlatformSalesErrors.ActorRequired) : actorUserId;
        Status = PlatformPromotionStatus.PendingApproval;
        DecidedByUserId = null;
        DecidedUtc = null;
        DecisionReason = null;
        MarkAsModified(utcNow);
    }

    public void Approve(Guid actorUserId, string? reason, DateTimeOffset utcNow)
    {
        Decide(PlatformPromotionStatus.Active, actorUserId, reason, utcNow);
    }

    public void Reject(Guid actorUserId, string? reason, DateTimeOffset utcNow)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new DomainException(PlatformSalesErrors.ReasonRequired);
        }

        Decide(PlatformPromotionStatus.Rejected, actorUserId, reason, utcNow);
    }

    public void Pause(DateTimeOffset utcNow)
    {
        Move(PlatformPromotionStatus.Active, PlatformPromotionStatus.Paused, utcNow);
    }

    public void Resume(DateTimeOffset utcNow)
    {
        Move(PlatformPromotionStatus.Paused, PlatformPromotionStatus.Active, utcNow);
    }

    public void End(DateTimeOffset utcNow)
    {
        if (Status is not (PlatformPromotionStatus.Active or PlatformPromotionStatus.Paused))
        {
            throw new DomainException(PlatformSalesErrors.PromotionNotActive);
        }

        Status = PlatformPromotionStatus.Ended;
        MarkAsModified(utcNow);
    }

    // =====================================================
    // METHOD: USE (D-S20)
    // =====================================================
    public Vixly.Shared.Kernel.Errors.Error? Refusal(Guid platformPlanId, PlatformSubscriptionTerm term, bool atRegistration, DateTimeOffset utcNow)
    {
        if (Status != PlatformPromotionStatus.Active)
        {
            return PlatformSalesErrors.PromotionNotActive;
        }

        if (utcNow < ValidFromUtc)
        {
            return PlatformSalesErrors.PromotionNotStarted;
        }

        if (ValidToUtc <= utcNow)
        {
            return PlatformSalesErrors.PromotionExpired;
        }

        if (atRegistration ? !AtRegistration : !ByStaff)
        {
            return atRegistration ? PlatformSalesErrors.PromotionNotAtRegistration : PlatformSalesErrors.PromotionNotByStaff;
        }

        if (_plans.Count > 0 && _plans.All(item => item.PlatformPlanId != platformPlanId))
        {
            return PlatformSalesErrors.PromotionNotForPlan;
        }

        if (Term is PlatformSubscriptionTerm only && only != term)
        {
            return PlatformSalesErrors.PromotionNotForTerm;
        }

        return RedemptionLimit is int limit && Redemptions >= limit ? PlatformSalesErrors.PromotionUsedUp : null;
    }

    public void Redeem(Guid platformPlanId, PlatformSubscriptionTerm term, bool atRegistration, DateTimeOffset utcNow)
    {
        if (Refusal(platformPlanId, term, atRegistration, utcNow) is { } refused)
        {
            throw new DomainException(refused);
        }

        Redemptions++;
        MarkAsModified(utcNow);
    }

    // =====================================================
    // METHOD: HELPERS
    // =====================================================
    private void Define(
        PlatformDiscountTerms terms,
        DateTimeOffset validFromUtc,
        DateTimeOffset? validToUtc,
        IReadOnlyCollection<Guid> planIds,
        PlatformSubscriptionTerm? term,
        bool atRegistration,
        bool byStaff,
        int? redemptionLimit,
        DateTimeOffset utcNow)
    {
        ArgumentNullException.ThrowIfNull(terms);
        ArgumentNullException.ThrowIfNull(planIds);

        if (validToUtc is DateTimeOffset to && (to <= validFromUtc || to <= utcNow))
        {
            throw new DomainException(PlatformSalesErrors.PromotionDatesInvalid);
        }

        if (!atRegistration && !byStaff)
        {
            throw new DomainException(PlatformSalesErrors.PromotionUseInvalid);
        }

        if (redemptionLimit is < 1 || (term is PlatformSubscriptionTerm value && !Enum.IsDefined(value)))
        {
            throw new DomainException(PlatformSalesErrors.PromotionLimitInvalid);
        }

        Terms = terms;
        ValidFromUtc = validFromUtc;
        ValidToUtc = validToUtc;
        Term = term;
        AtRegistration = atRegistration;
        ByStaff = byStaff;
        RedemptionLimit = redemptionLimit;
        _plans.Clear();
        _plans.AddRange(planIds.Where(id => id != Guid.Empty).Distinct().Select(id => new PlatformPromotionPlan(id)));
        MarkAsModified(utcNow);
    }

    private void Decide(PlatformPromotionStatus status, Guid actorUserId, string? reason, DateTimeOffset utcNow)
    {
        if (Status != PlatformPromotionStatus.PendingApproval)
        {
            throw new DomainException(PlatformSalesErrors.PromotionNotPending);
        }

        if (actorUserId == Guid.Empty)
        {
            throw new DomainException(PlatformSalesErrors.ActorRequired);
        }

        if (SubmittedByUserId == actorUserId)
        {
            throw new DomainException(PlatformSalesErrors.OwnSubmission);
        }

        string? why = reason?.Trim();

        if (why is { Length: > PlatformSubscriptionLimits.ReasonMaxLength })
        {
            throw new DomainException(PlatformSalesErrors.ReasonTooLong);
        }

        Status = status;
        DecidedByUserId = actorUserId;
        DecidedUtc = utcNow;
        DecisionReason = string.IsNullOrEmpty(why) ? null : why;
        MarkAsModified(utcNow);
    }

    private void Move(PlatformPromotionStatus from, PlatformPromotionStatus to, DateTimeOffset utcNow)
    {
        if (Status != from)
        {
            throw new DomainException(PlatformSalesErrors.PromotionNotActive);
        }

        Status = to;
        MarkAsModified(utcNow);
    }

    private void RequireEditable()
    {
        if (!IsEditable)
        {
            throw new DomainException(PlatformSalesErrors.PromotionNotEditable);
        }
    }
}
