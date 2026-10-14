using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Localization;

namespace Vixly.Platform.Domain.Subscriptions;

// =========================================================
// FILE: PlatformPlan.cs
//
// PURPOSE:
// A plan Vixly sells, with its translations and versions (D-S1, D-S2).
//
// RESPONSIBILITIES:
// - Create a plan with a first draft version
// - Keep names and descriptions per language
// - Edit the draft, publish it, start a new draft from the current version
// - Retire and reactivate the plan; set catalogue visibility and order
//
// IMPORTANT NOTES:
// - Aggregate root. Translations and versions belong to it
// - One draft at most; the published version is the current one
// - Publishing never touches subscribers: they keep their version for the rest of their period (owner, D-S2)
// - The currency is fixed once a version has been published (one currency per plan, D-S7)
// =========================================================
public sealed class PlatformPlan : AuditableEntity
{
    private readonly List<PlatformPlanTranslation> _translations = new();
    private readonly List<PlatformPlanVersion> _versions = new();

    private PlatformPlan()
    {
        Code = string.Empty;
    }

    private PlatformPlan(string code, Guid createdByUserId, DateTimeOffset createdUtc)
        : base(createdUtc)
    {
        Code = PlatformPlanCodeFormat.Normalize(code);
        Status = PlatformPlanStatus.Active;
        CreatedByUserId = createdByUserId;
    }

    public string Code { get; private set; }

    public PlatformPlanStatus Status { get; private set; }

    public bool IsPublic { get; private set; }

    public int DisplayOrder { get; private set; }

    public Guid CreatedByUserId { get; private set; }

    public IReadOnlyList<PlatformPlanTranslation> Translations => _translations;

    public IReadOnlyList<PlatformPlanVersion> Versions => _versions;

    public PlatformPlanVersion? Current => _versions.FirstOrDefault(version => version.Status == PlatformPlanVersionStatus.Published);

    public PlatformPlanVersion? Draft => _versions.FirstOrDefault(version => version.IsDraft);

    public bool CanBeChosen => Status == PlatformPlanStatus.Active && Current is not null;

    public string CurrencyCode => (Current ?? Draft)?.CurrencyCode ?? string.Empty;

    // =====================================================
    // METHOD: CREATE
    // =====================================================
    public static PlatformPlan Create(
        string code,
        string? englishName,
        string? englishDescription,
        string? currencyCode,
        Guid actorUserId,
        DateTimeOffset utcNow)
    {
        Guid actor = actorUserId == Guid.Empty
            ? throw new DomainException(PlatformPlanErrors.ActorRequired)
            : actorUserId;

        PlatformPlan plan = new(code, actor, utcNow);
        plan._translations.Add(new PlatformPlanTranslation(plan.Id, LanguageCode.Default, englishName, englishDescription));
        plan._versions.Add(PlatformPlanVersion.Draft(plan.Id, 1, PlatformPlanVersionTerms.Empty(PlatformCurrencies.Normalize(currencyCode)), utcNow));

        return plan;
    }

    // =====================================================
    // METHOD: TEXT
    // =====================================================
    public string NameIn(LanguageCode language)
    {
        ArgumentNullException.ThrowIfNull(language);

        return (_translations.FirstOrDefault(item => item.Language == language)
            ?? _translations.First(item => item.Language == LanguageCode.Default)).Name;
    }

    public string? DescriptionIn(LanguageCode language)
    {
        ArgumentNullException.ThrowIfNull(language);

        PlatformPlanTranslation? translation = _translations.FirstOrDefault(item => item.Language == language);

        return translation is null || translation.Description is null
            ? _translations.First(item => item.Language == LanguageCode.Default).Description
            : translation.Description;
    }

    public void SetTranslation(LanguageCode language, string? name, string? description, DateTimeOffset utcNow)
    {
        ArgumentNullException.ThrowIfNull(language);

        PlatformPlanTranslation? existing = _translations.FirstOrDefault(item => item.Language == language);
        bool empty = string.IsNullOrWhiteSpace(name) && string.IsNullOrWhiteSpace(description);

        if (language == LanguageCode.Default && string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException(PlatformPlanErrors.EnglishNameRequired);
        }

        if (empty && existing is not null)
        {
            _translations.Remove(existing);
        }
        else if (existing is not null)
        {
            existing.Rename(name, description);
        }
        else if (!empty)
        {
            _translations.Add(new PlatformPlanTranslation(Id, language, name, description));
        }

        MarkAsModified(utcNow);
    }

    // =====================================================
    // METHOD: VERSIONS (D-S2)
    // =====================================================
    public void EditDraft(PlatformPlanVersionTerms terms, DateTimeOffset utcNow)
    {
        ArgumentNullException.ThrowIfNull(terms);

        PlatformPlanVersion draft = Draft ?? throw new DomainException(PlatformPlanErrors.NoDraft);
        draft.Edit(terms, currencyLocked: _versions.Any(version => !version.IsDraft), utcNow);
        MarkAsModified(utcNow);
    }

    public PlatformPlanVersion PublishDraft(Guid actorUserId, DateTimeOffset utcNow)
    {
        if (actorUserId == Guid.Empty)
        {
            throw new DomainException(PlatformPlanErrors.ActorRequired);
        }

        PlatformPlanVersion draft = Draft ?? throw new DomainException(PlatformPlanErrors.NoDraft);
        PlatformPlanVersion? previous = Current;

        draft.Publish(actorUserId, utcNow);
        previous?.Supersede(utcNow);
        MarkAsModified(utcNow);

        return draft;
    }

    // Sales submit, the CFO approves (D-S26). PublishDraft stays for
    // demo data and tests; the use cases go through these two.
    public PlatformPlanVersion SubmitDraft(Guid actorUserId, DateTimeOffset utcNow)
    {
        Guid actor = actorUserId == Guid.Empty ? throw new DomainException(PlatformPlanErrors.ActorRequired) : actorUserId;
        PlatformPlanVersion draft = Draft ?? throw new DomainException(PlatformPlanErrors.NoDraft);
        draft.Submit(actor, utcNow);
        MarkAsModified(utcNow);

        return draft;
    }

    public PlatformPlanVersion ApproveDraft(Guid actorUserId, DateTimeOffset utcNow)
    {
        PlatformPlanVersion draft = Draft ?? throw new DomainException(PlatformPlanErrors.NoDraft);

        if (!draft.IsAwaitingApproval)
        {
            throw new DomainException(PlatformPlanErrors.DraftNotSubmitted);
        }

        if (draft.SubmittedByUserId == actorUserId)
        {
            throw new DomainException(PlatformPlanErrors.OwnSubmission);
        }

        return PublishDraft(actorUserId, utcNow);
    }

    public PlatformPlanVersion RejectDraft(Guid actorUserId, string? reason, DateTimeOffset utcNow)
    {
        Guid actor = actorUserId == Guid.Empty ? throw new DomainException(PlatformPlanErrors.ActorRequired) : actorUserId;
        string why = string.IsNullOrWhiteSpace(reason) ? throw new DomainException(PlatformPlanErrors.RejectionReasonRequired) : reason.Trim();

        if (why.Length > PlatformSubscriptionLimits.ReasonMaxLength)
        {
            throw new DomainException(PlatformSubscriptionErrors.ReasonTooLong);
        }

        PlatformPlanVersion draft = Draft ?? throw new DomainException(PlatformPlanErrors.NoDraft);

        if (draft.SubmittedByUserId == actor)
        {
            throw new DomainException(PlatformPlanErrors.OwnSubmission);
        }

        draft.Reject(actor, why, utcNow);
        MarkAsModified(utcNow);

        return draft;
    }

    public PlatformPlanVersion StartDraft(DateTimeOffset utcNow)
    {
        if (Draft is not null)
        {
            throw new DomainException(PlatformPlanErrors.DraftExists);
        }

        PlatformPlanVersion current = Current ?? throw new DomainException(PlatformPlanErrors.NothingPublished);
        PlatformPlanVersion draft = PlatformPlanVersion.Draft(Id, _versions.Max(version => version.Number) + 1, current.Terms, utcNow);
        _versions.Add(draft);
        MarkAsModified(utcNow);

        return draft;
    }

    public void DiscardDraft(DateTimeOffset utcNow)
    {
        PlatformPlanVersion draft = Draft ?? throw new DomainException(PlatformPlanErrors.NoDraft);

        if (draft.IsAwaitingApproval)
        {
            throw new DomainException(PlatformPlanErrors.DraftSubmitted);
        }

        if (Current is null)
        {
            throw new DomainException(PlatformPlanErrors.NothingPublished);
        }

        _versions.Remove(draft);
        MarkAsModified(utcNow);
    }

    public PlatformPlanVersion Version(Guid versionId)
    {
        return _versions.FirstOrDefault(version => version.Id == versionId)
            ?? throw new DomainException(PlatformPlanErrors.VersionNotFound);
    }

    // =====================================================
    // METHOD: CATALOGUE AND STATUS
    // =====================================================
    public void SetCatalogue(bool isPublic, int displayOrder, DateTimeOffset utcNow)
    {
        if (displayOrder < 0 || displayOrder > PlatformSubscriptionLimits.MaxDisplayOrder)
        {
            throw new DomainException(PlatformPlanErrors.DisplayOrderInvalid);
        }

        IsPublic = isPublic;
        DisplayOrder = displayOrder;
        MarkAsModified(utcNow);
    }

    public void Retire(DateTimeOffset utcNow)
    {
        if (Status == PlatformPlanStatus.Retired)
        {
            throw new DomainException(PlatformPlanErrors.AlreadyRetired);
        }

        Status = PlatformPlanStatus.Retired;
        MarkAsModified(utcNow);
    }

    public void Reactivate(DateTimeOffset utcNow)
    {
        if (Status != PlatformPlanStatus.Retired)
        {
            throw new DomainException(PlatformPlanErrors.NotRetired);
        }

        Status = PlatformPlanStatus.Active;
        MarkAsModified(utcNow);
    }
}
