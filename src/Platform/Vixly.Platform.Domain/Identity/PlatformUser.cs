using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Localization;
using Vixly.Shared.Kernel.ValueObjects;

namespace Vixly.Platform.Domain.Identity;

// =========================================================
// FILE: PlatformUser.cs
//
// PURPOSE:
// A Vixly staff member who uses the Platform console: profile,
// authority, account, roles and departments.
//
// RESPONSIBILITIES:
// - Hold the profile and preferred language
// - Own the account lifecycle (invite, activate, lock,
//   suspend, deactivate, reactivate) and sign-in tracking
// - Own role grants and department memberships
// - Own the second factors: passkeys, authenticator app and
//   recovery codes (ADR-0006)
// - Enforce the administrative safety rules carried from V3:
//   no administrative action on one's own account, and never
//   remove the last active SuperUser
//
// IMPORTANT NOTES:
// - Aggregate root. Account, role assignments and memberships
//   change only through this class
// - SuperUser is an authority level, not a role. A SuperUser
//   holds every permission, so there is no role to keep in
//   step (replaces V3's authority synchronisation service)
// - otherActiveSuperUsers is supplied by the use case from a
//   query in the same transaction; the domain refuses any
//   change that would leave the platform without an active
//   SuperUser
// - A user never removes their own last second factor; only
//   an administrator can reset second factors, and the user
//   must then enrol again before reaching the console
// - No date of birth (upgrade plan)
// =========================================================
public sealed class PlatformUser : AuditableEntity
{
    private readonly List<PlatformRoleAssignment> _roleAssignments = new();

    private readonly List<PlatformUserDepartment> _departments = new();

    private readonly List<PlatformUserPasskey> _passkeys = new();

    private readonly List<PlatformUserRecoveryCode> _recoveryCodes = new();

    private PlatformUser()
    {
        Account = null!;
        FirstName = string.Empty;
        LastName = string.Empty;
        Email = null!;
        PreferredLanguage = LanguageCode.Default;
    }

    private PlatformUser(
        string firstName,
        string lastName,
        EmailAddress email,
        LanguageCode preferredLanguage,
        PlatformUserAuthorityLevel authorityLevel,
        string? employeeNumber,
        PhoneNumber? phoneNumber,
        DateTimeOffset createdUtc)
        : base(createdUtc)
    {
        FirstName = firstName;
        LastName = lastName;
        Email = email;
        PreferredLanguage = preferredLanguage;
        AuthorityLevel = authorityLevel;
        EmployeeNumber = employeeNumber;
        PhoneNumber = phoneNumber;
        Account = new PlatformUserAccount(Id, createdUtc);
    }

    public string FirstName { get; private set; }

    public string LastName { get; private set; }

    public EmailAddress Email { get; private set; }

    public string? EmployeeNumber { get; private set; }

    public PhoneNumber? PhoneNumber { get; private set; }

    public LanguageCode PreferredLanguage { get; private set; }

    public PlatformUserAuthorityLevel AuthorityLevel { get; private set; }

    public PlatformUserAccount Account { get; private set; }

    public IReadOnlyList<PlatformRoleAssignment> RoleAssignments => _roleAssignments;

    public IReadOnlyList<PlatformUserDepartment> Departments => _departments;

    public IReadOnlyList<PlatformUserPasskey> Passkeys => _passkeys;

    public PlatformUserAuthenticatorApp? AuthenticatorApp { get; private set; }

    public IReadOnlyList<PlatformUserRecoveryCode> RecoveryCodes => _recoveryCodes;

    public bool HasSecondFactor => _passkeys.Count > 0 || AuthenticatorApp?.IsConfirmed == true;

    public int UnusedRecoveryCodeCount => _recoveryCodes.Count(code => !code.IsUsed);

    public bool IsActiveSuperUser =>
        AuthorityLevel == PlatformUserAuthorityLevel.SuperUser && Account.IsActive;

    // =====================================================
    // METHOD: INVITE
    // =====================================================
    public static PlatformUser Invite(
        string firstName,
        string lastName,
        EmailAddress email,
        LanguageCode preferredLanguage,
        PlatformUserAuthorityLevel authorityLevel,
        string? employeeNumber,
        PhoneNumber? phoneNumber,
        DateTimeOffset createdUtc)
    {
        ArgumentNullException.ThrowIfNull(email);
        ArgumentNullException.ThrowIfNull(preferredLanguage);
        EnsureDefined(authorityLevel);

        return new PlatformUser(
            PlatformIdentityText.RequiredName(firstName),
            PlatformIdentityText.RequiredName(lastName),
            email,
            preferredLanguage,
            authorityLevel,
            PlatformIdentityText.OptionalEmployeeNumber(employeeNumber),
            phoneNumber,
            createdUtc);
    }

    // =====================================================
    // METHOD: UPDATE PROFILE
    // =====================================================
    public void UpdateProfile(
        string firstName,
        string lastName,
        string? employeeNumber,
        PhoneNumber? phoneNumber,
        DateTimeOffset utcNow)
    {
        FirstName = PlatformIdentityText.RequiredName(firstName);
        LastName = PlatformIdentityText.RequiredName(lastName);
        EmployeeNumber = PlatformIdentityText.OptionalEmployeeNumber(employeeNumber);
        PhoneNumber = phoneNumber;
        MarkAsModified(utcNow);
    }

    public void ChangePreferredLanguage(LanguageCode language, DateTimeOffset utcNow)
    {
        ArgumentNullException.ThrowIfNull(language);

        PreferredLanguage = language;
        MarkAsModified(utcNow);
    }

    // =====================================================
    // METHOD: CHANGE AUTHORITY LEVEL
    // =====================================================
    public void ChangeAuthorityLevel(
        PlatformUserAuthorityLevel authorityLevel,
        Guid actorUserId,
        int otherActiveSuperUsers,
        DateTimeOffset utcNow)
    {
        EnsureDefined(authorityLevel);
        EnsureNotSelf(actorUserId);

        if (authorityLevel != PlatformUserAuthorityLevel.SuperUser)
        {
            EnsureAnotherSuperUserRemains(otherActiveSuperUsers);
        }

        AuthorityLevel = authorityLevel;
        MarkAsModified(utcNow);
    }

    // =====================================================
    // METHOD: ACCOUNT LIFECYCLE
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

    public void Lock(string reason, Guid actorUserId, int otherActiveSuperUsers, DateTimeOffset utcNow)
    {
        Restrict(PlatformUserAccountStatus.Locked, reason, actorUserId, otherActiveSuperUsers, utcNow);
    }

    public void Suspend(string reason, Guid actorUserId, int otherActiveSuperUsers, DateTimeOffset utcNow)
    {
        Restrict(PlatformUserAccountStatus.Suspended, reason, actorUserId, otherActiveSuperUsers, utcNow);
    }

    public void Deactivate(string reason, Guid actorUserId, int otherActiveSuperUsers, DateTimeOffset utcNow)
    {
        string validReason = PlatformIdentityText.RequiredReason(reason);
        EnsureNotSelf(actorUserId);
        EnsureSuperUserNotLost(otherActiveSuperUsers);

        Account.Deactivate(validReason, actorUserId, utcNow);
        MarkAsModified(utcNow);
    }

    public void Reactivate(string reason, Guid actorUserId, DateTimeOffset utcNow)
    {
        string validReason = PlatformIdentityText.RequiredReason(reason);
        EnsureNotSelf(actorUserId);

        Account.Reactivate(validReason, actorUserId, utcNow);
        MarkAsModified(utcNow);
    }

    // =====================================================
    // METHOD: ROLES
    // =====================================================
    public PlatformRoleAssignment AssignRole(
        Guid platformRoleId,
        Guid actorUserId,
        DateTimeOffset? expiresUtc,
        DateTimeOffset utcNow)
    {
        if (platformRoleId == Guid.Empty)
        {
            throw new DomainException(PlatformIdentityErrors.RoleRequired);
        }

        EnsureNotSelf(actorUserId);

        if (expiresUtc is not null && expiresUtc <= utcNow)
        {
            throw new DomainException(PlatformIdentityErrors.AssignmentExpiryNotInFuture);
        }

        if (_roleAssignments.Any(assignment =>
            assignment.PlatformRoleId == platformRoleId && assignment.IsInForce(utcNow)))
        {
            throw new DomainException(PlatformIdentityErrors.RoleAlreadyAssigned);
        }

        PlatformRoleAssignment grant = new(Id, platformRoleId, actorUserId, expiresUtc, utcNow);
        _roleAssignments.Add(grant);
        MarkAsModified(utcNow);

        return grant;
    }

    public void RevokeRole(Guid platformRoleId, Guid actorUserId, DateTimeOffset utcNow)
    {
        EnsureNotSelf(actorUserId);

        PlatformRoleAssignment grant = _roleAssignments.FirstOrDefault(assignment =>
                assignment.PlatformRoleId == platformRoleId && assignment.IsInForce(utcNow))
            ?? throw new DomainException(PlatformIdentityErrors.RoleNotAssigned);

        grant.Revoke(actorUserId, utcNow);
        MarkAsModified(utcNow);
    }

    public IReadOnlyCollection<Guid> RolesInForce(DateTimeOffset utcNow)
    {
        return _roleAssignments
            .Where(assignment => assignment.IsInForce(utcNow))
            .Select(assignment => assignment.PlatformRoleId)
            .ToHashSet();
    }

    // =====================================================
    // METHOD: DEPARTMENTS
    // =====================================================
    public void JoinDepartment(Guid platformDepartmentId, DateTimeOffset utcNow)
    {
        if (platformDepartmentId == Guid.Empty)
        {
            throw new DomainException(PlatformIdentityErrors.DepartmentRequired);
        }

        if (_departments.Any(membership => membership.PlatformDepartmentId == platformDepartmentId))
        {
            throw new DomainException(PlatformIdentityErrors.AlreadyDepartmentMember);
        }

        bool isFirst = _departments.Count == 0;
        _departments.Add(new PlatformUserDepartment(Id, platformDepartmentId, isFirst, utcNow));
        MarkAsModified(utcNow);
    }

    public void LeaveDepartment(Guid platformDepartmentId, DateTimeOffset utcNow)
    {
        PlatformUserDepartment membership = FindMembership(platformDepartmentId);
        _departments.Remove(membership);

        if (membership.IsPrimary && _departments.Count > 0)
        {
            _departments[0].SetPrimary(true, utcNow);
        }

        MarkAsModified(utcNow);
    }

    public void SetPrimaryDepartment(Guid platformDepartmentId, DateTimeOffset utcNow)
    {
        PlatformUserDepartment target = FindMembership(platformDepartmentId);

        foreach (PlatformUserDepartment membership in _departments)
        {
            membership.SetPrimary(ReferenceEquals(membership, target), utcNow);
        }

        MarkAsModified(utcNow);
    }

    // =====================================================
    // METHOD: PASSKEYS
    // =====================================================
    public PlatformUserPasskey RegisterPasskey(
        byte[] credentialId,
        byte[] publicKey,
        long signatureCounter,
        Guid aaguid,
        string name,
        DateTimeOffset utcNow)
    {
        if (credentialId is null
            || credentialId.Length == 0
            || credentialId.Length > PlatformIdentityLimits.CredentialIdMaxLength
            || publicKey is null
            || publicKey.Length == 0
            || publicKey.Length > PlatformIdentityLimits.PublicKeyMaxLength
            || signatureCounter < 0)
        {
            throw new DomainException(PlatformIdentityErrors.PasskeyInvalid);
        }

        if (_passkeys.Any(passkey => passkey.Matches(credentialId)))
        {
            throw new DomainException(PlatformIdentityErrors.PasskeyAlreadyRegistered);
        }

        if (_passkeys.Count >= PlatformIdentityLimits.MaxPasskeysPerUser)
        {
            throw new DomainException(PlatformIdentityErrors.PasskeyLimitReached);
        }

        string validName = PlatformIdentityText.RequiredName(name);

        if (validName.Length > PlatformIdentityLimits.PasskeyNameMaxLength)
        {
            throw new DomainException(PlatformIdentityErrors.NameTooLong);
        }

        PlatformUserPasskey passkey = new(
            Id,
            credentialId.ToArray(),
            publicKey.ToArray(),
            signatureCounter,
            aaguid,
            validName,
            utcNow);
        _passkeys.Add(passkey);
        MarkAsModified(utcNow);

        return passkey;
    }

    public void RecordPasskeySignIn(byte[] credentialId, long signatureCounter, DateTimeOffset utcNow)
    {
        ArgumentNullException.ThrowIfNull(credentialId);

        PlatformUserPasskey passkey = _passkeys.FirstOrDefault(candidate => candidate.Matches(credentialId))
            ?? throw new DomainException(PlatformIdentityErrors.PasskeyNotFound);

        passkey.RecordUse(signatureCounter, utcNow);
    }

    public void RemovePasskey(Guid passkeyId, DateTimeOffset utcNow)
    {
        PlatformUserPasskey passkey = _passkeys.FirstOrDefault(candidate => candidate.Id == passkeyId)
            ?? throw new DomainException(PlatformIdentityErrors.PasskeyNotFound);

        bool otherFactorRemains = _passkeys.Count > 1 || AuthenticatorApp?.IsConfirmed == true;

        if (!otherFactorRemains)
        {
            throw new DomainException(PlatformIdentityErrors.LastSecondFactor);
        }

        _passkeys.Remove(passkey);
        MarkAsModified(utcNow);
    }

    // =====================================================
    // METHOD: AUTHENTICATOR APP
    // =====================================================
    public void BeginAuthenticatorEnrolment(string encryptedSecret, DateTimeOffset utcNow)
    {
        if (string.IsNullOrWhiteSpace(encryptedSecret)
            || encryptedSecret.Length > PlatformIdentityLimits.EncryptedSecretMaxLength)
        {
            throw new DomainException(PlatformIdentityErrors.AuthenticatorSecretInvalid);
        }

        if (AuthenticatorApp?.IsConfirmed == true)
        {
            throw new DomainException(PlatformIdentityErrors.AuthenticatorNotPending);
        }

        AuthenticatorApp = new PlatformUserAuthenticatorApp(Id, encryptedSecret, utcNow);
        MarkAsModified(utcNow);
    }

    public void ConfirmAuthenticatorApp(long timeStep, DateTimeOffset utcNow)
    {
        if (AuthenticatorApp is null)
        {
            throw new DomainException(PlatformIdentityErrors.AuthenticatorNotPending);
        }

        AuthenticatorApp.Confirm(timeStep, utcNow);
        MarkAsModified(utcNow);
    }

    public void RecordAuthenticatorSignIn(long timeStep, DateTimeOffset utcNow)
    {
        if (AuthenticatorApp is null)
        {
            throw new DomainException(PlatformIdentityErrors.AuthenticatorNotConfirmed);
        }

        AuthenticatorApp.RecordUse(timeStep, utcNow);
    }

    public void RemoveAuthenticatorApp(DateTimeOffset utcNow)
    {
        if (AuthenticatorApp is null)
        {
            throw new DomainException(PlatformIdentityErrors.AuthenticatorNotConfirmed);
        }

        if (AuthenticatorApp.IsConfirmed && _passkeys.Count == 0)
        {
            throw new DomainException(PlatformIdentityErrors.LastSecondFactor);
        }

        AuthenticatorApp = null;
        MarkAsModified(utcNow);
    }

    // =====================================================
    // METHOD: RECOVERY CODES
    // =====================================================
    public void ReplaceRecoveryCodes(IReadOnlyCollection<string> codeHashes, DateTimeOffset utcNow)
    {
        ArgumentNullException.ThrowIfNull(codeHashes);

        if (codeHashes.Count != PlatformIdentityLimits.RecoveryCodeCount
            || codeHashes.Distinct(StringComparer.Ordinal).Count() != codeHashes.Count)
        {
            throw new DomainException(PlatformIdentityErrors.RecoveryCodesInvalid);
        }

        List<string> validHashes = codeHashes.Select(PlatformTokenHash.Require).ToList();

        _recoveryCodes.Clear();
        _recoveryCodes.AddRange(validHashes.Select(hash => new PlatformUserRecoveryCode(Id, hash, utcNow)));
        MarkAsModified(utcNow);
    }

    public void UseRecoveryCode(string codeHash, DateTimeOffset utcNow)
    {
        PlatformUserRecoveryCode code = _recoveryCodes.FirstOrDefault(candidate =>
                !candidate.IsUsed && string.Equals(candidate.CodeHash, codeHash, StringComparison.Ordinal))
            ?? throw new DomainException(PlatformIdentityErrors.RecoveryCodeNotAccepted);

        code.MarkUsed(utcNow);
        MarkAsModified(utcNow);
    }

    // =====================================================
    // METHOD: RESET SECOND FACTOR (ADMINISTRATOR)
    // =====================================================
    public void ResetSecondFactor(string reason, Guid actorUserId, DateTimeOffset utcNow)
    {
        PlatformIdentityText.RequiredReason(reason);
        EnsureNotSelf(actorUserId);

        _passkeys.Clear();
        _recoveryCodes.Clear();
        AuthenticatorApp = null;
        MarkAsModified(utcNow);
    }

    private void Restrict(
        PlatformUserAccountStatus status,
        string reason,
        Guid actorUserId,
        int otherActiveSuperUsers,
        DateTimeOffset utcNow)
    {
        string validReason = PlatformIdentityText.RequiredReason(reason);
        EnsureNotSelf(actorUserId);
        EnsureSuperUserNotLost(otherActiveSuperUsers);

        Account.Restrict(status, validReason, actorUserId, utcNow);
        MarkAsModified(utcNow);
    }

    private PlatformUserDepartment FindMembership(Guid platformDepartmentId)
    {
        return _departments.FirstOrDefault(membership => membership.PlatformDepartmentId == platformDepartmentId)
            ?? throw new DomainException(PlatformIdentityErrors.NotDepartmentMember);
    }

    private void EnsureNotSelf(Guid actorUserId)
    {
        if (PlatformIdentityText.RequiredActor(actorUserId) == Id)
        {
            throw new DomainException(PlatformIdentityErrors.CannotActOnOwnAccount);
        }
    }

    private void EnsureSuperUserNotLost(int otherActiveSuperUsers)
    {
        if (IsActiveSuperUser)
        {
            EnsureAnotherSuperUserRemains(otherActiveSuperUsers);
        }
    }

    private void EnsureAnotherSuperUserRemains(int otherActiveSuperUsers)
    {
        if (otherActiveSuperUsers < 0)
        {
            throw new DomainException(PlatformIdentityErrors.SuperUserCountInvalid);
        }

        if (IsActiveSuperUser && otherActiveSuperUsers == 0)
        {
            throw new DomainException(PlatformIdentityErrors.LastSuperUser);
        }
    }

    private static void EnsureDefined(PlatformUserAuthorityLevel authorityLevel)
    {
        if (!Enum.IsDefined(authorityLevel))
        {
            throw new DomainException(PlatformIdentityErrors.AuthorityLevelInvalid);
        }
    }
}
