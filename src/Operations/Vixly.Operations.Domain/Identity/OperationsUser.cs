using Vixly.Operations.Domain.Common;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Localization;
using Vixly.Shared.Kernel.ValueObjects;

namespace Vixly.Operations.Domain.Identity;

// =========================================================
// FILE: OperationsUser.cs
//
// PURPOSE:
// A staff member of one tenant (Operations Slice 1).
//
// RESPONSIBILITIES:
// - Invite the tenant owner
// - Accept the invitation with a password
// - Hold the authenticator app and recovery codes
// - Say whether a second factor is required
//
// IMPORTANT NOTES:
// - Email is unique per tenant (sweep D-O4)
// - Exactly one Owner per tenant; the Owner always needs a second factor (owner decision D-O2)
// - Roles and scopes are separate records (OperationsRoleGrant); IsAdministrator mirrors the Administrator grant (Slice 1D)
// =========================================================
public sealed class OperationsUser : OperationsEntity
{
    private readonly List<OperationsUserRecoveryCode> _recoveryCodes = new();

    private OperationsUser()
    {
        Account = null!;
        FullName = string.Empty;
        Email = null!;
        PreferredLanguage = LanguageCode.Default;
    }

    private OperationsUser(Guid tenantId, string fullName, EmailAddress email, LanguageCode preferredLanguage, bool isOwner, DateTimeOffset createdUtc)
        : base(tenantId, createdUtc)
    {
        FullName = fullName;
        Email = email;
        PreferredLanguage = preferredLanguage;
        IsOwner = isOwner;
        Account = new OperationsUserAccount(tenantId, Id, createdUtc);
    }

    public string FullName { get; private set; }

    public EmailAddress Email { get; private set; }

    public LanguageCode PreferredLanguage { get; private set; }

    public bool IsOwner { get; private set; }

    public OperationsUserAccount Account { get; private set; }

    public OperationsUserAuthenticatorApp? AuthenticatorApp { get; private set; }

    public IReadOnlyList<OperationsUserRecoveryCode> RecoveryCodes => _recoveryCodes;

    public bool HasSecondFactor => AuthenticatorApp?.IsConfirmed == true;

    // The Owner always needs one (D-O2); Administrators and the tenant-wide setting come in Slice 1B.
    // The Owner and Administrators always need a second factor (owner decision D-O2); a tenant setting may require it for everyone (1E).
    public bool RequiresSecondFactor => IsOwner || IsAdministrator;

    // Kept by the role grants: true while the user holds an active Administrator grant (Slice 1D).
    public bool IsAdministrator { get; private set; }

    public int UnusedRecoveryCodeCount => _recoveryCodes.Count(code => !code.IsUsed);

    // =====================================================
    // METHOD: INVITE OWNER
    // =====================================================
    public static OperationsUser InviteStaff(Guid tenantId, string fullName, EmailAddress email, LanguageCode preferredLanguage, DateTimeOffset utcNow)
    {
        ArgumentNullException.ThrowIfNull(email);
        ArgumentNullException.ThrowIfNull(preferredLanguage);

        return new OperationsUser(tenantId, OperationsIdentityText.RequiredName(fullName), email, preferredLanguage, false, utcNow);
    }

    public void ChangePreferredLanguage(LanguageCode language, DateTimeOffset utcNow)
    {
        PreferredLanguage = language ?? throw new ArgumentNullException(nameof(language));
        MarkAsModified(utcNow);
    }

    public void SetAdministrator(bool isAdministrator, DateTimeOffset utcNow)
    {
        IsAdministrator = isAdministrator;
        MarkAsModified(utcNow);
    }

    // The Owner hands ownership to an active Administrator with a second factor and stays an Administrator.
    public void TransferOwnershipTo(OperationsUser administrator, DateTimeOffset utcNow)
    {
        ArgumentNullException.ThrowIfNull(administrator);

        if (!IsOwner || administrator.Id == Id || !administrator.IsAdministrator || !administrator.Account.IsActive || !administrator.HasSecondFactor)
        {
            throw new DomainException(Vixly.Operations.Domain.Access.OperationsRoleErrors.TransferNeedsAdministrator);
        }

        IsOwner = false;
        IsAdministrator = true;
        MarkAsModified(utcNow);
    }

    public void BecomeOwner(DateTimeOffset utcNow)
    {
        IsOwner = true;
        IsAdministrator = false;
        MarkAsModified(utcNow);
    }

    // Staff status changes (Slice 1D); the Owner is never suspended, locked or deactivated.
    public void ChangeStatus(OperationsUserAccountStatus status, string? reason, DateTimeOffset utcNow)
    {
        if (IsOwner)
        {
            throw new DomainException(Vixly.Operations.Domain.Access.OperationsRoleErrors.OwnerProtected);
        }

        Account.ChangeStatus(status, reason, utcNow);
        MarkAsModified(utcNow);
    }

    public static OperationsUser InviteOwner(Guid tenantId, string fullName, EmailAddress email, LanguageCode preferredLanguage, DateTimeOffset utcNow)
    {
        ArgumentNullException.ThrowIfNull(email);
        ArgumentNullException.ThrowIfNull(preferredLanguage);

        return new OperationsUser(tenantId, OperationsIdentityText.RequiredName(fullName), email, preferredLanguage, true, utcNow);
    }

    // =====================================================
    // METHOD: PASSWORD AND SIGN-IN
    // =====================================================
    public void AcceptInvitation(string passwordHash, DateTimeOffset utcNow)
    {
        Account.Activate(passwordHash, utcNow);
        MarkAsModified(utcNow);
    }

    public void ChangePasswordHash(string passwordHash, DateTimeOffset utcNow)
    {
        Account.ChangePasswordHash(passwordHash, utcNow);
    }

    public void RecordFailedSignIn(DateTimeOffset utcNow)
    {
        Account.RecordFailedSignIn(utcNow);
    }

    public void RecordSuccessfulSignIn(DateTimeOffset utcNow)
    {
        Account.RecordSuccessfulSignIn(utcNow);
    }

    // =====================================================
    // METHOD: AUTHENTICATOR APP
    // =====================================================
    public void BeginAuthenticatorEnrolment(string encryptedSecret, DateTimeOffset utcNow)
    {
        if (string.IsNullOrWhiteSpace(encryptedSecret) || encryptedSecret.Length > OperationsIdentityLimits.EncryptedSecretMaxLength)
        {
            throw new DomainException(OperationsIdentityErrors.AuthenticatorSecretInvalid);
        }

        if (AuthenticatorApp?.IsConfirmed == true)
        {
            throw new DomainException(OperationsIdentityErrors.AuthenticatorNotPending);
        }

        AuthenticatorApp = new OperationsUserAuthenticatorApp(TenantId, Id, encryptedSecret, utcNow);
        MarkAsModified(utcNow);
    }

    public void ConfirmAuthenticatorApp(long timeStep, DateTimeOffset utcNow)
    {
        if (AuthenticatorApp is null)
        {
            throw new DomainException(OperationsIdentityErrors.AuthenticatorNotPending);
        }

        AuthenticatorApp.Confirm(timeStep, utcNow);
        MarkAsModified(utcNow);
    }

    public void RecordAuthenticatorSignIn(long timeStep, DateTimeOffset utcNow)
    {
        if (AuthenticatorApp is null)
        {
            throw new DomainException(OperationsIdentityErrors.AuthenticatorNotConfirmed);
        }

        AuthenticatorApp.RecordUse(timeStep, utcNow);
    }

    // Support reset the second factor (D-O13): the app and the recovery codes go; the user enrols again at sign-in.
    public void ResetSecondFactor(DateTimeOffset utcNow)
    {
        AuthenticatorApp = null;
        _recoveryCodes.Clear();
        MarkAsModified(utcNow);
    }

    // =====================================================
    // METHOD: RECOVERY CODES
    // =====================================================
    public void ReplaceRecoveryCodes(IReadOnlyCollection<string> codeHashes, DateTimeOffset utcNow)
    {
        ArgumentNullException.ThrowIfNull(codeHashes);

        if (codeHashes.Count != OperationsIdentityLimits.RecoveryCodeCount || codeHashes.Distinct(StringComparer.Ordinal).Count() != codeHashes.Count)
        {
            throw new DomainException(OperationsIdentityErrors.RecoveryCodesInvalid);
        }

        List<string> hashes = codeHashes.Select(OperationsTokenHash.Require).ToList();
        _recoveryCodes.Clear();
        _recoveryCodes.AddRange(hashes.Select(hash => new OperationsUserRecoveryCode(TenantId, Id, hash, utcNow)));
        MarkAsModified(utcNow);
    }

    public void UseRecoveryCode(string codeHash, DateTimeOffset utcNow)
    {
        OperationsUserRecoveryCode code = _recoveryCodes.FirstOrDefault(candidate =>
                !candidate.IsUsed && string.Equals(candidate.CodeHash, codeHash, StringComparison.Ordinal))
            ?? throw new DomainException(OperationsIdentityErrors.RecoveryCodeNotAccepted);

        code.MarkUsed(utcNow);
        MarkAsModified(utcNow);
    }
}
