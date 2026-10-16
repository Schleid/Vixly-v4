using Vixly.Platform.Domain.Identity;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Localization;
using Vixly.Shared.Kernel.ValueObjects;

namespace Vixly.Platform.Domain.Tenants;

// =========================================================
// FILE: PlatformTenantRegistration.cs
//
// PURPOSE:
// A company signing itself up through the public site.
//
// RESPONSIBILITIES:
// - Start with validated company and owner details, the slug and
//   recorded consent (D-T18)
// - Throttle verification emails and allow one email correction
//   path before verification
// - Move to "choose a plan" once the email is verified (D-T9)
// - Expire on its own deadlines; let staff cancel it
//
// IMPORTANT NOTES:
// - Aggregate root. Slice 2 reaches PendingPlanSelection (D-T4);
//   Slice 3 records the plan choice (trial or awaiting payment)
// - The registrant proves who they are with an access token kept
//   by Public.Web; only its hash is stored
// - The verification deadline follows the latest link: 24 hours
//   after it was sent, never beyond the access token
// - The owner becomes a contact of the tenant, not a user:
//   tenant users are Operations data (ADR-0003)
// =========================================================
public sealed class PlatformTenantRegistration : AuditableEntity
{
    private PlatformTenantRegistration()
    {
        Company = null!;
        OwnerName = string.Empty;
        OwnerEmail = null!;
        OwnerPhone = null!;
        Language = LanguageCode.Default;
        TimeZone = PlatformTimeZones.Utc;
        Slug = string.Empty;
        AccessTokenHash = string.Empty;
        TermsVersion = string.Empty;
        PrivacyVersion = string.Empty;
    }

    private PlatformTenantRegistration(DateTimeOffset createdUtc)
        : base(createdUtc)
    {
        Company = null!;
        OwnerName = string.Empty;
        OwnerEmail = null!;
        OwnerPhone = null!;
        Language = LanguageCode.Default;
        TimeZone = PlatformTimeZones.Utc;
        Slug = string.Empty;
        AccessTokenHash = string.Empty;
        TermsVersion = string.Empty;
        PrivacyVersion = string.Empty;
    }

    public PlatformTenantRegistrationStatus Status { get; private set; }

    public PlatformCompanyDetails Company { get; private set; }

    public string OwnerName { get; private set; }

    public EmailAddress OwnerEmail { get; private set; }

    public PhoneNumber OwnerPhone { get; private set; }

    public LanguageCode Language { get; private set; }

    public string TimeZone { get; private set; }

    public string Slug { get; private set; }

    public Guid? PlatformTenantId { get; private set; }

    public string AccessTokenHash { get; private set; }

    public DateTimeOffset AccessExpiresUtc { get; private set; }

    public string TermsVersion { get; private set; }

    public string PrivacyVersion { get; private set; }

    public string? ConsentIpAddress { get; private set; }

    public DateTimeOffset? LastVerificationSentUtc { get; private set; }

    public DateTimeOffset? VerificationWindowStartUtc { get; private set; }

    public int VerificationSendsInWindow { get; private set; }

    public DateTimeOffset? EmailVerifiedUtc { get; private set; }

    public DateTimeOffset? ClosedUtc { get; private set; }

    public Guid? CancelledByUserId { get; private set; }

    public string? CancellationReason { get; private set; }

    public bool IsClosed => Status is PlatformTenantRegistrationStatus.Completed
        or PlatformTenantRegistrationStatus.Cancelled
        or PlatformTenantRegistrationStatus.Expired;

    public DateTimeOffset VerificationDeadlineUtc
    {
        get
        {
            DateTimeOffset deadline = (LastVerificationSentUtc ?? CreatedUtc).Add(PlatformTenantLimits.VerificationLinkLifetime);

            return deadline < AccessExpiresUtc ? deadline : AccessExpiresUtc;
        }
    }

    // =====================================================
    // METHOD: START
    // =====================================================
    public static PlatformTenantRegistration Start(
        PlatformCompanyDetails company,
        string? ownerName,
        EmailAddress ownerEmail,
        PhoneNumber ownerPhone,
        LanguageCode language,
        string? timeZone,
        string slug,
        string accessTokenHash,
        bool termsAccepted,
        string? termsVersion,
        string? privacyVersion,
        string? consentIpAddress,
        DateTimeOffset utcNow)
    {
        ArgumentNullException.ThrowIfNull(company);
        ArgumentNullException.ThrowIfNull(ownerEmail);
        ArgumentNullException.ThrowIfNull(ownerPhone);
        ArgumentNullException.ThrowIfNull(language);

        if (!termsAccepted)
        {
            throw new DomainException(PlatformRegistrationErrors.TermsNotAccepted);
        }

        PlatformTenantRegistration registration = new(utcNow)
        {
            Status = PlatformTenantRegistrationStatus.PendingEmailVerification,
            Company = company,
            OwnerName = PlatformTenantText.Required(
                ownerName,
                PlatformTenantLimits.ContactNameMaxLength,
                PlatformRegistrationErrors.OwnerNameRequired,
                PlatformRegistrationErrors.OwnerNameTooLong),
            OwnerEmail = ownerEmail,
            OwnerPhone = ownerPhone,
            Language = language,
            TimeZone = PlatformTimeZones.Normalize(timeZone),
            Slug = PlatformPortalSlugFormat.Normalize(slug),
            AccessTokenHash = PlatformTokenHash.Require(accessTokenHash),
            AccessExpiresUtc = utcNow.Add(PlatformTenantLimits.RegistrationAccessLifetime),
            TermsVersion = LegalVersion(termsVersion),
            PrivacyVersion = LegalVersion(privacyVersion),
            ConsentIpAddress = consentIpAddress?.Trim() is { Length: > 0 and <= PlatformTenantLimits.IpAddressMaxLength } address
                ? address
                : null
        };

        return registration;
    }

    // =====================================================
    // METHOD: EXPIRY
    // =====================================================
    public bool ExpireIfDue(DateTimeOffset utcNow)
    {
        bool due = Status switch
        {
            PlatformTenantRegistrationStatus.PendingEmailVerification => utcNow >= VerificationDeadlineUtc,
            PlatformTenantRegistrationStatus.PendingPlanSelection =>
                utcNow >= EmailVerifiedUtc!.Value.Add(PlatformTenantLimits.PlanSelectionWindow),
            _ => false
        };

        if (due)
        {
            Close(PlatformTenantRegistrationStatus.Expired, utcNow);
        }

        return due;
    }

    public bool AcceptsAccess(DateTimeOffset utcNow)
    {
        return !IsClosed && utcNow < AccessExpiresUtc;
    }

    // =====================================================
    // METHOD: VERIFICATION EMAIL
    // =====================================================
    public void RecordVerificationSent(DateTimeOffset utcNow)
    {
        EnsureAwaitingVerification(utcNow);

        if (VerificationWindowStartUtc is null || utcNow >= VerificationWindowStartUtc.Value.AddHours(1))
        {
            VerificationWindowStartUtc = utcNow;
            VerificationSendsInWindow = 0;
        }

        if (VerificationSendsInWindow >= PlatformTenantLimits.MaxVerificationSendsPerHour)
        {
            throw new DomainException(PlatformRegistrationErrors.ResendTooSoon);
        }

        VerificationSendsInWindow++;
        LastVerificationSentUtc = utcNow;
        MarkAsModified(utcNow);
    }

    public void CorrectOwnerEmail(EmailAddress email, DateTimeOffset utcNow)
    {
        ArgumentNullException.ThrowIfNull(email);
        EnsureAwaitingVerification(utcNow);

        if (email == OwnerEmail)
        {
            throw new DomainException(PlatformRegistrationErrors.EmailUnchanged);
        }

        OwnerEmail = email;
        MarkAsModified(utcNow);
    }

    public void MarkEmailVerified(Guid tenantId, DateTimeOffset utcNow)
    {
        EnsureAwaitingVerification(utcNow);

        if (tenantId == Guid.Empty)
        {
            throw new DomainException(PlatformTenantErrors.TenantRequired);
        }

        PlatformTenantId = tenantId;
        EmailVerifiedUtc = utcNow;
        Status = PlatformTenantRegistrationStatus.PendingPlanSelection;
        AccessExpiresUtc = utcNow.Add(PlatformTenantLimits.PlanSelectionWindow);
        MarkAsModified(utcNow);
    }

    // =====================================================
    // METHOD: PLAN (Slice 3, D-S12)
    // =====================================================
    public void RecordPlanChosen(bool trialStarted, DateTimeOffset utcNow)
    {
        EnsureOpen();

        if (Status == PlatformTenantRegistrationStatus.PendingEmailVerification)
        {
            throw new DomainException(PlatformRegistrationErrors.NotVerified);
        }

        if (Status != PlatformTenantRegistrationStatus.PendingPlanSelection)
        {
            throw new DomainException(PlatformRegistrationErrors.PlanAlreadyChosen);
        }

        if (ExpireIfDue(utcNow))
        {
            throw new DomainException(PlatformRegistrationErrors.Expired);
        }

        Status = trialStarted
            ? PlatformTenantRegistrationStatus.PendingAccountSetup
            : PlatformTenantRegistrationStatus.PendingPayment;
        MarkAsModified(utcNow);
    }

    // =====================================================
    // METHOD: ACCESS
    // =====================================================
    public void ReplaceAccessToken(string accessTokenHash, DateTimeOffset utcNow)
    {
        EnsureOpen();
        AccessTokenHash = PlatformTokenHash.Require(accessTokenHash);
        MarkAsModified(utcNow);
    }

    // =====================================================
    // METHOD: PAID (Slice 5)
    // =====================================================
    // The registration invoice was paid: the owner can now set up the account. Returns false when
    // the registration is not waiting for payment, so a replayed "invoice paid" changes nothing.
    public bool RecordPaid(DateTimeOffset utcNow)
    {
        if (Status != PlatformTenantRegistrationStatus.PendingPayment)
        {
            return false;
        }

        Status = PlatformTenantRegistrationStatus.PendingAccountSetup;
        MarkAsModified(utcNow);
        return true;
    }

    // =====================================================
    // METHOD: FIRST DEVICE (Slice 7, D-L7)
    // =====================================================
    // Account setup is done: the owner gets a first-device link. False when the registration is not there yet.
    public bool StartFirstDevice(DateTimeOffset utcNow)
    {
        if (Status == PlatformTenantRegistrationStatus.PendingFirstDeviceRegistration)
        {
            return true;
        }

        if (Status != PlatformTenantRegistrationStatus.PendingAccountSetup)
        {
            return false;
        }

        Status = PlatformTenantRegistrationStatus.PendingFirstDeviceRegistration;
        MarkAsModified(utcNow);
        return true;
    }

    // The first device enrolled from the link: the registration is complete.
    public bool CompleteWithFirstDevice(DateTimeOffset utcNow)
    {
        if (Status != PlatformTenantRegistrationStatus.PendingFirstDeviceRegistration)
        {
            return false;
        }

        Close(PlatformTenantRegistrationStatus.Completed, utcNow);
        return true;
    }

    // =====================================================
    // METHOD: CANCEL
    // =====================================================
    public void Cancel(Guid actorUserId, string? reason, DateTimeOffset utcNow)
    {
        string why = PlatformTenantText.Reason(reason);
        Guid actor = PlatformTenantText.Actor(actorUserId);

        EnsureOpen();
        CancelledByUserId = actor;
        CancellationReason = why;
        Close(PlatformTenantRegistrationStatus.Cancelled, utcNow);
    }

    private void EnsureAwaitingVerification(DateTimeOffset utcNow)
    {
        EnsureOpen();

        if (Status != PlatformTenantRegistrationStatus.PendingEmailVerification)
        {
            throw new DomainException(PlatformRegistrationErrors.AlreadyVerified);
        }

        if (ExpireIfDue(utcNow))
        {
            throw new DomainException(PlatformRegistrationErrors.Expired);
        }
    }

    private void EnsureOpen()
    {
        if (!IsClosed)
        {
            return;
        }

        throw new DomainException(Status switch
        {
            PlatformTenantRegistrationStatus.Cancelled => PlatformRegistrationErrors.Cancelled,
            PlatformTenantRegistrationStatus.Expired => PlatformRegistrationErrors.Expired,
            _ => PlatformRegistrationErrors.Closed
        });
    }

    private void Close(PlatformTenantRegistrationStatus status, DateTimeOffset utcNow)
    {
        Status = status;
        ClosedUtc = utcNow;
        MarkAsModified(utcNow);
    }

    private static string LegalVersion(string? value)
    {
        return PlatformTenantText.Required(
            value,
            PlatformTenantLimits.LegalVersionMaxLength,
            PlatformRegistrationErrors.LegalVersionInvalid,
            PlatformRegistrationErrors.LegalVersionInvalid);
    }
}
