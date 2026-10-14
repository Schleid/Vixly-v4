using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Domain.Identity;

// =========================================================
// FILE: PlatformUserAccount.cs
//
// PURPOSE:
// The sign-in state of one staff user: status, password hash
// and failed-attempt tracking.
//
// RESPONSIBILITIES:
// - Hold the account status and who changed it, when and why
// - Hold the password hash (never a password)
// - Count failed sign-ins and apply the temporary lockout
//
// IMPORTANT NOTES:
// - Part of the PlatformUser aggregate. Every change goes
//   through PlatformUser, which enforces the rules that need
//   the whole user (own-account and last-SuperUser checks)
// - Stored in its own table so credential columns are not
//   read with every profile query
// =========================================================
public sealed class PlatformUserAccount : AuditableEntity
{
    private PlatformUserAccount()
    {
    }

    internal PlatformUserAccount(Guid platformUserId, DateTimeOffset createdUtc)
        : base(createdUtc)
    {
        PlatformUserId = platformUserId;
        Status = PlatformUserAccountStatus.Invited;
    }

    public Guid PlatformUserId { get; private set; }

    public PlatformUserAccountStatus Status { get; private set; }

    public string? PasswordHash { get; private set; }

    public int FailedSignInCount { get; private set; }

    public DateTimeOffset? SignInLockoutEndsUtc { get; private set; }

    public DateTimeOffset? LastSignInUtc { get; private set; }

    public DateTimeOffset? ActivatedUtc { get; private set; }

    public string? StatusReason { get; private set; }

    public Guid? StatusChangedByUserId { get; private set; }

    public DateTimeOffset? StatusChangedUtc { get; private set; }

    public bool IsActive => Status == PlatformUserAccountStatus.Active;

    // =====================================================
    // METHOD: IS SIGN-IN ALLOWED
    // =====================================================
    public bool IsSignInAllowed(DateTimeOffset utcNow)
    {
        return IsActive
            && PasswordHash is not null
            && (SignInLockoutEndsUtc is null || SignInLockoutEndsUtc <= utcNow);
    }

    internal void Activate(string passwordHash, DateTimeOffset utcNow)
    {
        if (Status != PlatformUserAccountStatus.Invited)
        {
            throw new DomainException(PlatformIdentityErrors.InvitationNotPending);
        }

        SetPasswordHash(passwordHash);
        Status = PlatformUserAccountStatus.Active;
        ActivatedUtc = utcNow;
        StatusChangedUtc = utcNow;
        StatusChangedByUserId = PlatformUserId;
        StatusReason = null;
        MarkAsModified(utcNow);
    }

    internal void ChangePasswordHash(string passwordHash, DateTimeOffset utcNow)
    {
        if (!IsActive)
        {
            throw new DomainException(PlatformIdentityErrors.AccountNotActive);
        }

        SetPasswordHash(passwordHash);
        FailedSignInCount = 0;
        SignInLockoutEndsUtc = null;
        MarkAsModified(utcNow);
    }

    internal void RecordFailedSignIn(DateTimeOffset utcNow)
    {
        FailedSignInCount++;

        if (FailedSignInCount >= PlatformIdentityLimits.MaxFailedSignIns)
        {
            SignInLockoutEndsUtc = utcNow.Add(PlatformIdentityLimits.SignInLockoutDuration);
            FailedSignInCount = 0;
        }

        MarkAsModified(utcNow);
    }

    internal void RecordSuccessfulSignIn(DateTimeOffset utcNow)
    {
        if (!IsSignInAllowed(utcNow))
        {
            throw new DomainException(PlatformIdentityErrors.AccountNotActive);
        }

        FailedSignInCount = 0;
        SignInLockoutEndsUtc = null;
        LastSignInUtc = utcNow;
        MarkAsModified(utcNow);
    }

    internal void Restrict(
        PlatformUserAccountStatus status,
        string reason,
        Guid actorUserId,
        DateTimeOffset utcNow)
    {
        if (Status == PlatformUserAccountStatus.Deactivated)
        {
            throw new DomainException(PlatformIdentityErrors.AccountDeactivated);
        }

        ChangeStatus(status, reason, actorUserId, utcNow);
    }

    internal void Deactivate(string reason, Guid actorUserId, DateTimeOffset utcNow)
    {
        if (Status == PlatformUserAccountStatus.Deactivated)
        {
            throw new DomainException(PlatformIdentityErrors.AccountDeactivated);
        }

        ChangeStatus(PlatformUserAccountStatus.Deactivated, reason, actorUserId, utcNow);
    }

    internal void Reactivate(string reason, Guid actorUserId, DateTimeOffset utcNow)
    {
        if (Status is PlatformUserAccountStatus.Active or PlatformUserAccountStatus.Invited)
        {
            throw new DomainException(PlatformIdentityErrors.AccountNotRestricted);
        }

        PlatformUserAccountStatus target = ActivatedUtc is null
            ? PlatformUserAccountStatus.Invited
            : PlatformUserAccountStatus.Active;

        FailedSignInCount = 0;
        SignInLockoutEndsUtc = null;
        ChangeStatus(target, reason, actorUserId, utcNow);
    }

    private void ChangeStatus(
        PlatformUserAccountStatus status,
        string reason,
        Guid actorUserId,
        DateTimeOffset utcNow)
    {
        Status = status;
        StatusReason = reason;
        StatusChangedByUserId = actorUserId;
        StatusChangedUtc = utcNow;
        MarkAsModified(utcNow);
    }

    private void SetPasswordHash(string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(passwordHash)
            || passwordHash.Length > PlatformIdentityLimits.PasswordHashMaxLength)
        {
            throw new DomainException(PlatformIdentityErrors.PasswordHashRequired);
        }

        PasswordHash = passwordHash;
    }
}
