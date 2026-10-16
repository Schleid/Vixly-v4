using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Localization;

namespace Vixly.Platform.Domain.Tenants;

// =========================================================
// FILE: PlatformTenant.cs
//
// PURPOSE:
// A company that uses Vixly: its codes, address, profile, contacts,
// labels and lifecycle.
//
// RESPONSIBILITIES:
// - Create a tenant from the console (D-T8) or from a verified
//   registration (D-T9)
// - Move it through its lifecycle (D-T1) and return the history
//   event of each move
// - Keep the portal slug fixed once the tenant has been active
//   (D-T11)
// - Hold contacts, tags, account manager, language and time zone
//
// IMPORTANT NOTES:
// - Aggregate root. Suspensions, contacts and tags are owned
// - History events and notes are not loaded with it: each move
//   returns its event and the use case adds it with the audit
//   entry and the outbox message (D-2)
// - Ban needs SuperUser authority; the use case checks that
// - The slug table (PlatformPortalSlug) guarantees uniqueness;
//   CurrentSlug is a copy for reads
// =========================================================
public sealed class PlatformTenant : AuditableEntity
{
    private readonly List<PlatformTenantContact> _contacts = new();
    private readonly List<PlatformTenantTag> _tags = new();
    private readonly List<PlatformTenantSuspension> _suspensions = new();

    private PlatformTenant()
    {
        TenantCode = string.Empty;
        CurrentSlug = string.Empty;
        Company = null!;
        DefaultLanguage = LanguageCode.Default;
        TimeZone = PlatformTimeZones.Utc;
    }

    private PlatformTenant(
        string tenantCode,
        string slug,
        PlatformCompanyDetails company,
        LanguageCode defaultLanguage,
        string timeZone,
        PlatformTenantOrigin origin,
        Guid? createdByUserId,
        DateTimeOffset createdUtc)
        : base(createdUtc)
    {
        TenantCode = PlatformTenantCodeFormat.Validate(tenantCode);
        CurrentSlug = PlatformPortalSlugFormat.Normalize(slug);
        Company = company;
        DefaultLanguage = defaultLanguage;
        TimeZone = PlatformTimeZones.Normalize(timeZone);
        Origin = origin;
        CreatedByUserId = createdByUserId;
        Status = PlatformTenantStatus.Draft;
        StatusChangedUtc = createdUtc;
    }

    public string TenantCode { get; private set; }

    public string CurrentSlug { get; private set; }

    public PlatformTenantStatus Status { get; private set; }

    public DateTimeOffset StatusChangedUtc { get; private set; }

    public DateTimeOffset? ActivatedUtc { get; private set; }

    public PlatformTenantOrigin Origin { get; private set; }

    public Guid? CreatedByUserId { get; private set; }

    public PlatformCompanyDetails Company { get; private set; }

    public LanguageCode DefaultLanguage { get; private set; }

    public string TimeZone { get; private set; }

    public Guid? AccountManagerUserId { get; private set; }

    public string? TaxId { get; private set; }

    public bool TaxExempt { get; private set; }

    public string? TaxExemptionReason { get; private set; }

    public string? TaxExemptionReference { get; private set; }

    public IReadOnlyList<PlatformTenantContact> Contacts => _contacts;

    public IReadOnlyList<PlatformTenantTag> Tags => _tags;

    public IReadOnlyList<PlatformTenantSuspension> Suspensions => _suspensions;

    public PlatformTenantSuspension? OpenSuspension => _suspensions.FirstOrDefault(suspension => suspension.IsOpen);

    public bool HasBeenActive => ActivatedUtc is not null;

    public bool CanCorrectSlug => !HasBeenActive
        && Status is PlatformTenantStatus.Draft or PlatformTenantStatus.PendingActivation;

    // A new subscription needs an open account: a suspended tenant is
    // reactivated first, a deactivated one too, a banned one never.
    public bool CanStartSubscription => Status is PlatformTenantStatus.Draft
        or PlatformTenantStatus.PendingActivation
        or PlatformTenantStatus.Active;

    // =====================================================
    // METHOD: CREATE
    // =====================================================
    public static PlatformTenantCreation CreateByStaff(
        string tenantCode,
        string slug,
        PlatformCompanyDetails company,
        PlatformTenantContactDetails owner,
        LanguageCode defaultLanguage,
        string? timeZone,
        Guid actorUserId,
        DateTimeOffset utcNow)
    {
        ArgumentNullException.ThrowIfNull(company);
        ArgumentNullException.ThrowIfNull(owner);
        ArgumentNullException.ThrowIfNull(defaultLanguage);

        PlatformTenant tenant = new(
            tenantCode,
            slug,
            company,
            defaultLanguage,
            PlatformTimeZones.Normalize(timeZone),
            PlatformTenantOrigin.Staff,
            PlatformTenantText.Actor(actorUserId),
            utcNow);

        tenant.ReplaceContacts(new[] { owner }, utcNow);

        return new PlatformTenantCreation(
            tenant,
            new[] { PlatformTenantLifecycleEvent.Record(tenant, PlatformTenantEventCodes.CreatedByStaff, null, actorUserId, utcNow) });
    }

    public static PlatformTenantCreation CreateFromRegistration(
        string tenantCode,
        PlatformTenantRegistration registration,
        DateTimeOffset utcNow)
    {
        ArgumentNullException.ThrowIfNull(registration);

        PlatformTenant tenant = new(
            tenantCode,
            registration.Slug,
            registration.Company,
            registration.Language,
            registration.TimeZone,
            PlatformTenantOrigin.SelfRegistration,
            null,
            utcNow);

        tenant.ReplaceContacts(
            new[]
            {
                PlatformTenantContactDetails.Create(
                    PlatformTenantContactRole.Owner,
                    registration.OwnerName,
                    registration.OwnerEmail,
                    registration.OwnerPhone)
            },
            utcNow);

        Dictionary<string, string> consent = new(StringComparer.Ordinal)
        {
            ["termsVersion"] = registration.TermsVersion,
            ["privacyVersion"] = registration.PrivacyVersion,
            ["acceptedUtc"] = registration.CreatedUtc.ToString("O", System.Globalization.CultureInfo.InvariantCulture)
        };

        return new PlatformTenantCreation(
            tenant,
            new[]
            {
                PlatformTenantLifecycleEvent.Record(tenant, PlatformTenantEventCodes.CreatedFromRegistration, null, null, utcNow),
                PlatformTenantLifecycleEvent.Record(tenant, PlatformTenantEventCodes.TermsAccepted, null, null, utcNow, arguments: consent)
            });
    }

    // =====================================================
    // METHOD: LIFECYCLE (D-T1)
    // =====================================================
    // Slice 7 (D-L7): the first device enrolled from the registration link activates the tenant;
    // no staff member acts, so the event has no actor and says it came from registration.
    public PlatformTenantLifecycleEvent? ActivateFromRegistration(DateTimeOffset utcNow)
    {
        if (Status is not (PlatformTenantStatus.Draft or PlatformTenantStatus.PendingActivation))
        {
            return null;
        }

        PlatformTenantStatus from = Status;
        MoveTo(PlatformTenantStatus.Active, utcNow);
        ActivatedUtc ??= utcNow;

        return PlatformTenantLifecycleEvent.Record(this, PlatformTenantEventCodes.Activated, from, null, utcNow,
            arguments: new Dictionary<string, string>(StringComparer.Ordinal) { ["by"] = "registration" });
    }

    public PlatformTenantLifecycleEvent Activate(Guid actorUserId, DateTimeOffset utcNow)
    {
        PlatformTenantStatus from = Require(
            PlatformTenantStatus.Active,
            PlatformTenantStatus.Draft,
            PlatformTenantStatus.PendingActivation);

        MoveTo(PlatformTenantStatus.Active, utcNow);
        ActivatedUtc ??= utcNow;

        return PlatformTenantLifecycleEvent.Record(this, PlatformTenantEventCodes.Activated, from, PlatformTenantText.Actor(actorUserId), utcNow);
    }

    public PlatformTenantLifecycleEvent Suspend(
        PlatformSuspensionCategory category,
        string? reason,
        Guid actorUserId,
        DateTimeOffset utcNow)
    {
        if (!Enum.IsDefined(category))
        {
            throw new DomainException(PlatformTenantErrors.SuspensionCategoryInvalid);
        }

        string why = PlatformTenantText.Reason(reason);
        Guid actor = PlatformTenantText.Actor(actorUserId);
        PlatformTenantStatus from = Require(PlatformTenantStatus.Suspended, PlatformTenantStatus.Active);

        _suspensions.Add(PlatformTenantSuspension.Open(Id, category, why, actor, utcNow));
        MoveTo(PlatformTenantStatus.Suspended, utcNow);

        return PlatformTenantLifecycleEvent.Record(this, PlatformTenantEventCodes.Suspended, from, actor, utcNow, category, why);
    }

    public PlatformTenantLifecycleEvent Reactivate(string? reason, Guid actorUserId, DateTimeOffset utcNow)
    {
        string why = PlatformTenantText.Reason(reason);
        Guid actor = PlatformTenantText.Actor(actorUserId);
        PlatformTenantStatus target = HasBeenActive ? PlatformTenantStatus.Active : PlatformTenantStatus.Draft;
        PlatformTenantStatus from = Require(target, PlatformTenantStatus.Suspended, PlatformTenantStatus.Deactivated);

        OpenSuspension?.Resolve(actor, why, utcNow);
        MoveTo(target, utcNow);

        return PlatformTenantLifecycleEvent.Record(this, PlatformTenantEventCodes.Reactivated, from, actor, utcNow, reason: why);
    }

    public PlatformTenantLifecycleEvent Deactivate(string? reason, Guid actorUserId, DateTimeOffset utcNow)
    {
        string why = PlatformTenantText.Reason(reason);
        Guid actor = PlatformTenantText.Actor(actorUserId);
        PlatformTenantStatus from = Require(
            PlatformTenantStatus.Deactivated,
            PlatformTenantStatus.Draft,
            PlatformTenantStatus.PendingActivation,
            PlatformTenantStatus.Active,
            PlatformTenantStatus.Suspended);

        MoveTo(PlatformTenantStatus.Deactivated, utcNow);

        return PlatformTenantLifecycleEvent.Record(this, PlatformTenantEventCodes.Deactivated, from, actor, utcNow, reason: why);
    }

    public PlatformTenantLifecycleEvent Ban(string? reason, Guid actorUserId, DateTimeOffset utcNow)
    {
        string why = PlatformTenantText.Reason(reason);
        Guid actor = PlatformTenantText.Actor(actorUserId);

        if (Status == PlatformTenantStatus.Banned)
        {
            throw new DomainException(PlatformTenantErrors.AlreadyBanned);
        }

        PlatformTenantStatus from = Status;
        MoveTo(PlatformTenantStatus.Banned, utcNow);

        return PlatformTenantLifecycleEvent.Record(this, PlatformTenantEventCodes.Banned, from, actor, utcNow, reason: why);
    }

    // =====================================================
    // METHOD: SLUG (D-T11)
    // =====================================================
    public PlatformTenantLifecycleEvent CorrectSlug(string newSlug, string? reason, Guid actorUserId, DateTimeOffset utcNow)
    {
        string slug = PlatformPortalSlugFormat.Normalize(newSlug);
        string why = PlatformTenantText.Reason(reason);
        Guid actor = PlatformTenantText.Actor(actorUserId);

        if (!CanCorrectSlug)
        {
            throw new DomainException(PlatformTenantErrors.SlugFixed);
        }

        if (slug == CurrentSlug)
        {
            throw new DomainException(PlatformTenantErrors.SlugUnchanged);
        }

        Dictionary<string, string> arguments = new(StringComparer.Ordinal)
        {
            ["from"] = CurrentSlug,
            ["to"] = slug
        };

        CurrentSlug = slug;
        MarkAsModified(utcNow);

        return PlatformTenantLifecycleEvent.Record(
            this,
            PlatformTenantEventCodes.SlugCorrected,
            Status,
            actor,
            utcNow,
            reason: why,
            arguments: arguments);
    }

    // =====================================================
    // METHOD: PROFILE AND SETTINGS
    // =====================================================
    // Billing (Slice 4, D-B16): tax ID printed on invoices, and an exemption that sets tax to 0 %.
    public void SetTaxDetails(string? taxId, bool exempt, string? exemptionReason, string? exemptionReference, DateTimeOffset utcNow)
    {
        string? tax = string.IsNullOrWhiteSpace(taxId) ? null : taxId.Trim();
        string? reason = string.IsNullOrWhiteSpace(exemptionReason) ? null : exemptionReason.Trim();
        string? reference = string.IsNullOrWhiteSpace(exemptionReference) ? null : exemptionReference.Trim();

        if (tax?.Length > Billing.PlatformBillingLimits.TaxIdMaxLength || reference?.Length > Billing.PlatformBillingLimits.ReferenceMaxLength)
        {
            throw new DomainException(Billing.PlatformBillingErrors.TaxIdTooLong);
        }

        if (exempt && reason is null)
        {
            throw new DomainException(Billing.PlatformBillingErrors.ReasonRequired);
        }

        if (reason?.Length > Billing.PlatformBillingLimits.ReasonMaxLength)
        {
            throw new DomainException(Billing.PlatformBillingErrors.ReasonTooLong);
        }

        TaxId = tax;
        TaxExempt = exempt;
        TaxExemptionReason = exempt ? reason : null;
        TaxExemptionReference = exempt ? reference : null;
        MarkAsModified(utcNow);
    }

    public void UpdateCompany(PlatformCompanyDetails company, DateTimeOffset utcNow)
    {
        ArgumentNullException.ThrowIfNull(company);

        Company = company;
        MarkAsModified(utcNow);
    }

    public void UpdateSettings(LanguageCode defaultLanguage, string? timeZone, DateTimeOffset utcNow)
    {
        ArgumentNullException.ThrowIfNull(defaultLanguage);

        DefaultLanguage = defaultLanguage;
        TimeZone = PlatformTimeZones.Normalize(timeZone);
        MarkAsModified(utcNow);
    }

    public void AssignAccountManager(Guid? platformUserId, DateTimeOffset utcNow)
    {
        if (platformUserId == Guid.Empty)
        {
            throw new DomainException(PlatformTenantErrors.AccountManagerInvalid);
        }

        AccountManagerUserId = platformUserId;
        MarkAsModified(utcNow);
    }

    // =====================================================
    // METHOD: CONTACTS (D-T15)
    // =====================================================
    public PlatformTenantContact ContactFor(PlatformTenantContactRole role)
    {
        return _contacts.FirstOrDefault(contact => contact.Role == role)
            ?? _contacts.First(contact => contact.Role == PlatformTenantContactRole.Owner);
    }

    public bool ReplaceContacts(IReadOnlyList<PlatformTenantContactDetails> contacts, DateTimeOffset utcNow)
    {
        ArgumentNullException.ThrowIfNull(contacts);

        if (contacts.Select(contact => contact.Role).Distinct().Count() != contacts.Count)
        {
            throw new DomainException(PlatformTenantErrors.ContactRoleDuplicate);
        }

        if (contacts.All(contact => contact.Role != PlatformTenantContactRole.Owner))
        {
            throw new DomainException(PlatformTenantErrors.OwnerContactRequired);
        }

        bool changed = _contacts.RemoveAll(existing => contacts.All(contact => contact.Role != existing.Role)) > 0;

        foreach (PlatformTenantContactDetails details in contacts)
        {
            PlatformTenantContact? existing = _contacts.FirstOrDefault(contact => contact.Role == details.Role);

            if (existing is null)
            {
                _contacts.Add(PlatformTenantContact.Create(Id, details, utcNow));
                changed = true;
            }
            else
            {
                changed |= existing.Change(details, utcNow);
            }
        }

        if (changed)
        {
            MarkAsModified(utcNow);
        }

        return changed;
    }

    // =====================================================
    // METHOD: TAGS (D-T16)
    // =====================================================
    public bool SetTags(IEnumerable<string?> tags, DateTimeOffset utcNow)
    {
        ArgumentNullException.ThrowIfNull(tags);

        List<string> wanted = tags.Select(PlatformTenantTag.Normalize).Distinct(StringComparer.Ordinal).ToList();

        if (wanted.Count > PlatformTenantLimits.MaxTagsPerTenant)
        {
            throw new DomainException(PlatformTenantErrors.TooManyTags);
        }

        bool changed = _tags.RemoveAll(tag => !wanted.Contains(tag.Value, StringComparer.Ordinal)) > 0;

        foreach (string value in wanted.Where(value => _tags.All(tag => tag.Value != value)))
        {
            _tags.Add(PlatformTenantTag.Create(Id, value, utcNow));
            changed = true;
        }

        if (changed)
        {
            MarkAsModified(utcNow);
        }

        return changed;
    }

    private PlatformTenantStatus Require(PlatformTenantStatus target, params PlatformTenantStatus[] allowedFrom)
    {
        if (!allowedFrom.Contains(Status))
        {
            throw new DomainException(PlatformTenantErrors.MoveNotAllowed(Status, target));
        }

        return Status;
    }

    private void MoveTo(PlatformTenantStatus status, DateTimeOffset utcNow)
    {
        Status = status;
        StatusChangedUtc = utcNow;
        MarkAsModified(utcNow);
    }
}
