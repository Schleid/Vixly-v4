using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Identity;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Localization;
using Vixly.Shared.Kernel.ValueObjects;

namespace Vixly.Operations.Domain.Customers;

// =========================================================
// FILE: OperationsCustomer.cs
//
// PURPOSE:
// A tenant's customer and their sign-in account (Slice 2).
//
// RESPONSIBILITIES:
// - Sign-up, email confirmation with the customer code, sign-in counting and lockout
// - Profile, status with a reason, credit flag with a reason
//
// IMPORTANT NOTES:
// - Email unique per tenant; the same person may be a customer of two tenants
// - The code is given when the email is confirmed (D-C1)
// =========================================================
public sealed class OperationsCustomer : OperationsEntity
{
    private OperationsCustomer()
    {
        FirstName = string.Empty;
        LastName = string.Empty;
        Email = null!;
        Phone = string.Empty;
        PreferredLanguage = LanguageCode.Default;
    }

    private OperationsCustomer(Guid tenantId, DateTimeOffset createdUtc)
        : base(tenantId, createdUtc)
    {
        FirstName = string.Empty;
        LastName = string.Empty;
        Email = null!;
        Phone = string.Empty;
        PreferredLanguage = LanguageCode.Default;
    }

    public string? Code { get; private set; }

    public string FirstName { get; private set; }

    public string LastName { get; private set; }

    public string? CompanyName { get; private set; }

    public EmailAddress Email { get; private set; }

    public string Phone { get; private set; }

    // Slice 9 (owner, 2026-10-22: the customer picks the channel): how the company's notices reach them, and when they chose it (their consent to texts).
    public Vixly.Operations.Domain.Texts.OperationsNoticeChannel NoticeChannel { get; private set; }

    public DateTimeOffset? NoticeChannelUtc { get; private set; }

    public void ChangeNoticeChannel(Vixly.Operations.Domain.Texts.OperationsNoticeChannel channel, DateTimeOffset utcNow)
    {
        NoticeChannel = Enum.IsDefined(channel) ? channel : Vixly.Operations.Domain.Texts.OperationsNoticeChannel.Email;
        NoticeChannelUtc = utcNow;
        MarkAsModified(utcNow);
    }

    public LanguageCode PreferredLanguage { get; private set; }

    public OperationsCustomerStatus Status { get; private set; }

    public string? StatusReason { get; private set; }

    public DateTimeOffset? StatusChangedUtc { get; private set; }

    // Owner, 2026-10-31: when a suspension ends by itself; null for one lifted by hand.
    public DateTimeOffset? SuspendedUntil { get; private set; }

    public bool CreditAllowed { get; private set; }

    public string? CreditReason { get; private set; }

    public DateTimeOffset? CreditChangedUtc { get; private set; }

    public string? PasswordHash { get; private set; }

    public int FailedSignInCount { get; private set; }

    public DateTimeOffset? LockoutEndsUtc { get; private set; }

    public DateTimeOffset? EmailVerifiedUtc { get; private set; }

    public DateTimeOffset? LastSignInUtc { get; private set; }

    // The customer's home address, asked at the first sign-in or copied from V3 (owner, 2026-10-26). None until then.
    public Vixly.Operations.Domain.Locations.OperationsPostalAddress? HomeAddress { get; private set; }

    public DateTimeOffset? HomeAddressUtc { get; private set; }

    public DateTimeOffset? WelcomeTourDismissedUtc { get; private set; }

    public void DismissWelcomeTour(DateTimeOffset utcNow)
    {
        if (WelcomeTourDismissedUtc is not null) return;
        WelcomeTourDismissedUtc = utcNow;
        MarkAsModified(utcNow);
    }

    // The portal's invitation to review (owner, 2026-11-03): once dismissed it does not come back.
    public DateTimeOffset? ReviewInviteDismissedUtc { get; private set; }

    public void DismissReviewInvite(DateTimeOffset utcNow)
    {
        if (ReviewInviteDismissedUtc is not null) return;
        ReviewInviteDismissedUtc = utcNow;
        MarkAsModified(utcNow);
    }

    public string FullName => FirstName + " " + LastName;

    // =====================================================
    // METHOD: SIGN UP (customers register themselves; staff never create them)
    // =====================================================
    public static OperationsCustomer SignUp(Guid tenantId, string? firstName, string? lastName, string? companyName, EmailAddress email, string? phone, LanguageCode language, string passwordHash, DateTimeOffset utcNow)
    {
        OperationsCustomer customer = New(tenantId, firstName, lastName, companyName, email, phone, language, utcNow);
        customer.Status = OperationsCustomerStatus.PendingVerification;
        customer.PasswordHash = RequireHash(passwordHash);
        return customer;
    }

    // =====================================================
    // METHOD: ENTERED BY STAFF (Vixly Lite, owner 2026-10-30): a company without the customer portal enters its customers;
    // active at once with its code, no password (no sign-in) until the company has a portal and invites them
    // =====================================================
    public static OperationsCustomer EnteredByStaff(Guid tenantId, string? firstName, string? lastName, string? companyName, EmailAddress email, string? phone, LanguageCode language,
        string code, DateTimeOffset utcNow)
    {
        OperationsCustomer customer = New(tenantId, firstName, lastName, companyName, email, phone, language, utcNow);
        customer.Code = RequireCode(code);
        customer.Status = OperationsCustomerStatus.Active;
        customer.StatusChangedUtc = utcNow;
        return customer;
    }

    // =====================================================
    // METHOD: CONFIRM EMAIL: the account becomes active and gets its code
    // =====================================================
    public void ConfirmEmail(string code, DateTimeOffset utcNow)
    {
        if (Status != OperationsCustomerStatus.PendingVerification)
        {
            throw new DomainException(OperationsCustomerErrors.NotPending);
        }

        Code = RequireCode(code);
        Status = OperationsCustomerStatus.Active;
        StatusChangedUtc = utcNow;
        EmailVerifiedUtc = utcNow;
        MarkAsModified(utcNow);
    }

    // =====================================================
    // METHOD: CORRECT EMAIL (2026-11-03): staff fix a mistyped address while the account waits for confirmation
    // =====================================================
    public void CorrectEmail(EmailAddress email, DateTimeOffset utcNow)
    {
        ArgumentNullException.ThrowIfNull(email);

        if (Status != OperationsCustomerStatus.PendingVerification)
        {
            throw new DomainException(OperationsCustomerErrors.NotPending);
        }

        Email = email;
        MarkAsModified(utcNow);
    }

    // A reset or invitation link proves the email as well.
    public void SetPassword(string passwordHash, DateTimeOffset utcNow)
    {
        PasswordHash = RequireHash(passwordHash);
        FailedSignInCount = 0;
        LockoutEndsUtc = null;
        EmailVerifiedUtc ??= utcNow;
        MarkAsModified(utcNow);
    }

    // =====================================================
    // METHOD: SIGN-IN COUNTING
    // =====================================================
    public bool CanSignIn(DateTimeOffset utcNow) =>
        Status == OperationsCustomerStatus.Active && PasswordHash is not null && (LockoutEndsUtc is null || LockoutEndsUtc <= utcNow);

    public bool IsLockedOut(DateTimeOffset utcNow) => LockoutEndsUtc is { } end && end > utcNow;

    public void RecordFailedSignIn(DateTimeOffset utcNow)
    {
        FailedSignInCount++;

        if (FailedSignInCount >= OperationsCustomerLimits.MaxFailedSignIns)
        {
            LockoutEndsUtc = utcNow.Add(OperationsCustomerLimits.LockoutDuration);
            FailedSignInCount = 0;
        }

        MarkAsModified(utcNow);
    }

    public void RecordSignIn(DateTimeOffset utcNow)
    {
        FailedSignInCount = 0;
        LockoutEndsUtc = null;
        LastSignInUtc = utcNow;
        MarkAsModified(utcNow);
    }

    // =====================================================
    // METHOD: PROFILE, STATUS, CREDIT
    // =====================================================
    public void ChangeProfile(string? firstName, string? lastName, string? companyName, string? phone, LanguageCode language, DateTimeOffset utcNow)
    {
        ArgumentNullException.ThrowIfNull(language);

        // Every value is checked first: a refused change leaves the customer as it was.
        (string first, string last, string? company, string checkedPhone) = (Name(firstName), Name(lastName), Company(companyName), RequirePhone(phone));
        FirstName = first;
        LastName = last;
        CompanyName = company;

        // Slice 9 (review): consent to texts was given for the old number; a new one gets email until the customer chooses again.
        if (checkedPhone != Phone && NoticeChannel != Vixly.Operations.Domain.Texts.OperationsNoticeChannel.Email)
        {
            NoticeChannel = Vixly.Operations.Domain.Texts.OperationsNoticeChannel.Email;
            NoticeChannelUtc = utcNow;
        }

        Phone = checkedPhone;
        PreferredLanguage = language;
        MarkAsModified(utcNow);
    }

    // The home address; the value object checks each part before anything changes.
    public void ChangeHomeAddress(Vixly.Operations.Domain.Locations.OperationsPostalAddress address, DateTimeOffset utcNow)
    {
        ArgumentNullException.ThrowIfNull(address);

        HomeAddress = address;
        HomeAddressUtc = utcNow;
        MarkAsModified(utcNow);
    }

    // Suspend and reactivate an active account; close any account that is not closed. Closed is final.
    // Owner, 2026-10-31: suspend (until a day, or until lifted), reactivate (also reopens a closed account), close, or ban (permanent).
    public void ChangeStatus(OperationsCustomerStatus status, string? reason, DateTimeOffset utcNow, DateTimeOffset? suspendedUntil = null)
    {
        bool allowed = status switch
        {
            OperationsCustomerStatus.Suspended => Status is OperationsCustomerStatus.Active or OperationsCustomerStatus.Suspended,
            OperationsCustomerStatus.Active => Status is OperationsCustomerStatus.Suspended or OperationsCustomerStatus.Closed,
            OperationsCustomerStatus.Closed => Status is OperationsCustomerStatus.Active or OperationsCustomerStatus.Suspended or OperationsCustomerStatus.PendingVerification,
            OperationsCustomerStatus.Banned => Status != OperationsCustomerStatus.Banned,
            _ => false
        };

        if (!allowed)
        {
            throw new DomainException(OperationsCustomerErrors.StatusChangeInvalid);
        }

        if (suspendedUntil is { } until && (status != OperationsCustomerStatus.Suspended || until <= utcNow))
        {
            throw new DomainException(OperationsCustomerErrors.SuspendedUntilInvalid);
        }

        // Checked before anything changes, so a refused change leaves the customer as it was.
        string checkedReason = Reason(reason);
        Status = status;
        StatusReason = checkedReason;
        StatusChangedUtc = utcNow;
        SuspendedUntil = suspendedUntil;
        MarkAsModified(utcNow);
    }

    // Owner, 2026-10-31: a suspension with an end day is lifted once that day has come; true when it was.
    public bool LiftSuspension(string reason, DateTimeOffset utcNow)
    {
        if (Status != OperationsCustomerStatus.Suspended || SuspendedUntil is not { } until || until > utcNow)
        {
            return false;
        }

        Status = OperationsCustomerStatus.Active;
        StatusReason = Reason(reason);
        StatusChangedUtc = utcNow;
        SuspendedUntil = null;
        MarkAsModified(utcNow);
        return true;
    }

    public void SetCredit(bool allowed, string? reason, DateTimeOffset utcNow)
    {
        CreditReason = Reason(reason);
        CreditAllowed = allowed;
        CreditChangedUtc = utcNow;
        MarkAsModified(utcNow);
    }

    // Slice 5F step 2b (K-1): the mark follows the customer's credit account; the account keeps its own reason (none when opened or made active again).
    public void FollowCreditAccount(bool active, string? reason, DateTimeOffset utcNow)
    {
        string? text = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
        CreditReason = text is { Length: > OperationsCustomerLimits.ReasonMaxLength } ? text[..OperationsCustomerLimits.ReasonMaxLength] : text;
        CreditAllowed = active;
        CreditChangedUtc = utcNow;
        MarkAsModified(utcNow);
    }

    // The details a new customer needs, checked before a code is taken, so a refused form never uses a number.
    public static void CheckDetails(string? firstName, string? lastName, string? companyName, string? phone)
    {
        _ = Name(firstName);
        _ = Name(lastName);
        _ = Company(companyName);
        _ = RequirePhone(phone);
    }

    private static OperationsCustomer New(Guid tenantId, string? firstName, string? lastName, string? companyName, EmailAddress email, string? phone, LanguageCode language, DateTimeOffset utcNow)
    {
        ArgumentNullException.ThrowIfNull(email);
        ArgumentNullException.ThrowIfNull(language);

        return new OperationsCustomer(tenantId, utcNow)
        {
            FirstName = Name(firstName),
            LastName = Name(lastName),
            CompanyName = Company(companyName),
            Email = email,
            Phone = RequirePhone(phone),
            PreferredLanguage = language
        };
    }

    private static string Name(string? value)
    {
        string text = value?.Trim() ?? string.Empty;

        if (text.Length == 0)
        {
            throw new DomainException(OperationsCustomerErrors.NameRequired);
        }

        if (text.Length > OperationsCustomerLimits.NameMaxLength)
        {
            throw new DomainException(OperationsCustomerErrors.NameTooLong);
        }

        return CarriesMarkupOrLink(text) ? throw new DomainException(OperationsCustomerErrors.NameInvalid) : text;
    }

    private static bool CarriesMarkupOrLink(string text) =>
        text.IndexOfAny(['<', '>', '@']) >= 0 || text.Contains("://", StringComparison.Ordinal) || text.Contains("www.", StringComparison.OrdinalIgnoreCase);

    private static string? Company(string? value)
    {
        string? text = string.IsNullOrWhiteSpace(value) ? null : value.Trim();

        if (text is { Length: > OperationsCustomerLimits.CompanyMaxLength })
        {
            throw new DomainException(OperationsCustomerErrors.CompanyTooLong);
        }

        return text is not null && CarriesMarkupOrLink(text) ? throw new DomainException(OperationsCustomerErrors.NameInvalid) : text;
    }

    // International form only (+ and digits), like the tenant's contacts (Slice 1B).
    private static string RequirePhone(string? value) =>
        PhoneNumber.TryCreate(value, out PhoneNumber? phone) && phone.Value.StartsWith('+') && phone.Value.Length <= OperationsCustomerLimits.PhoneMaxLength
            ? phone.Value
            : throw new DomainException(OperationsCustomerErrors.PhoneInvalid);

    private static string RequireCode(string? code) =>
        code is { Length: > 0 and <= OperationsCustomerLimits.CodeMaxLength } && code.All(char.IsAsciiLetterOrDigit)
            ? code
            : throw new DomainException(OperationsCustomerErrors.CodeInvalid);

    private static string RequireHash(string? hash) =>
        string.IsNullOrWhiteSpace(hash) || hash.Length > OperationsIdentityLimits.PasswordHashMaxLength
            ? throw new DomainException(OperationsIdentityErrors.PasswordHashRequired)
            : hash;

    private static string Reason(string? reason)
    {
        string text = reason?.Trim() ?? string.Empty;

        return text.Length is 0 or > OperationsCustomerLimits.ReasonMaxLength
            ? throw new DomainException(OperationsCustomerErrors.ReasonRequired)
            : text;
    }
}
