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

    public LanguageCode PreferredLanguage { get; private set; }

    public OperationsCustomerStatus Status { get; private set; }

    public string? StatusReason { get; private set; }

    public DateTimeOffset? StatusChangedUtc { get; private set; }

    public bool CreditAllowed { get; private set; }

    public string? CreditReason { get; private set; }

    public DateTimeOffset? CreditChangedUtc { get; private set; }

    public string? PasswordHash { get; private set; }

    public int FailedSignInCount { get; private set; }

    public DateTimeOffset? LockoutEndsUtc { get; private set; }

    public DateTimeOffset? EmailVerifiedUtc { get; private set; }

    public DateTimeOffset? LastSignInUtc { get; private set; }

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
        Phone = checkedPhone;
        PreferredLanguage = language;
        MarkAsModified(utcNow);
    }

    // Suspend and reactivate an active account; close any account that is not closed. Closed is final.
    public void ChangeStatus(OperationsCustomerStatus status, string? reason, DateTimeOffset utcNow)
    {
        bool allowed = status switch
        {
            OperationsCustomerStatus.Suspended => Status == OperationsCustomerStatus.Active,
            OperationsCustomerStatus.Active => Status == OperationsCustomerStatus.Suspended,
            OperationsCustomerStatus.Closed => Status != OperationsCustomerStatus.Closed,
            _ => false
        };

        if (!allowed)
        {
            throw new DomainException(OperationsCustomerErrors.StatusChangeInvalid);
        }

        // Checked before anything changes, so a refused change leaves the customer as it was.
        string checkedReason = Reason(reason);
        Status = status;
        StatusReason = checkedReason;
        StatusChangedUtc = utcNow;
        MarkAsModified(utcNow);
    }

    public void SetCredit(bool allowed, string? reason, DateTimeOffset utcNow)
    {
        CreditReason = Reason(reason);
        CreditAllowed = allowed;
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
