using Vixly.Operations.Domain.Common;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Operations.Domain.Identity;

// =========================================================
// FILE: OperationsUserAccount.cs
//
// PURPOSE:
// The sign-in side of a staff member: status, password and lockout (Slice 1).
//
// RESPONSIBILITIES:
// - Activate from an invitation
// - Count failed passwords and lock for 15 minutes after 5
// - Record successful sign-ins
//
// IMPORTANT NOTES:
// - Changed only through OperationsUser
// - Suspend, lock and deactivate arrive with staff management (Slice 1B)
// =========================================================
public sealed class OperationsUserAccount : OperationsEntity
{
    private OperationsUserAccount()
    {
    }

    internal OperationsUserAccount(Guid tenantId, Guid userId, DateTimeOffset createdUtc)
        : base(tenantId, createdUtc)
    {
        UserId = userId;
        Status = OperationsUserAccountStatus.Invited;
    }

    public Guid UserId { get; private set; }

    public OperationsUserAccountStatus Status { get; private set; }

    public string? PasswordHash { get; private set; }

    public int FailedSignInCount { get; private set; }

    public DateTimeOffset? SignInLockoutEndsUtc { get; private set; }

    public DateTimeOffset? LastSignInUtc { get; private set; }

    public DateTimeOffset? ActivatedUtc { get; private set; }

    public string? StatusReason { get; private set; }

    public DateTimeOffset? StatusChangedUtc { get; private set; }

    public bool IsActive => Status == OperationsUserAccountStatus.Active;

    public bool IsSignInAllowed(DateTimeOffset utcNow)
    {
        return IsActive && PasswordHash is not null && (SignInLockoutEndsUtc is null || SignInLockoutEndsUtc <= utcNow);
    }

    internal void Activate(string passwordHash, DateTimeOffset utcNow)
    {
        if (Status != OperationsUserAccountStatus.Invited)
        {
            throw new DomainException(OperationsIdentityErrors.InvitationNotPending);
        }

        SetPasswordHash(passwordHash);
        Status = OperationsUserAccountStatus.Active;
        ActivatedUtc = utcNow;
        MarkAsModified(utcNow);
    }

    // Suspend, lock or deactivate with a reason, or reactivate (Slice 1D). Invited accounts only move to Deactivated.
    internal void ChangeStatus(OperationsUserAccountStatus status, string? reason, DateTimeOffset utcNow)
    {
        string? text = reason?.Trim();
        bool allowed = status switch
        {
            OperationsUserAccountStatus.Suspended or OperationsUserAccountStatus.Locked => Status == OperationsUserAccountStatus.Active,
            OperationsUserAccountStatus.Deactivated => Status != OperationsUserAccountStatus.Deactivated,
            OperationsUserAccountStatus.Active => Status is OperationsUserAccountStatus.Suspended or OperationsUserAccountStatus.Locked or OperationsUserAccountStatus.Deactivated && PasswordHash is not null,
            _ => false
        };

        if (!allowed)
        {
            throw new DomainException(Vixly.Operations.Domain.Access.OperationsRoleErrors.StatusChangeInvalid);
        }

        if (string.IsNullOrEmpty(text) || text.Length > Vixly.Operations.Domain.Access.OperationsAccessLimits.ReasonMaxLength)
        {
            throw new DomainException(Vixly.Operations.Domain.Access.OperationsRoleErrors.ReasonRequired);
        }

        Status = status;
        StatusReason = text;
        StatusChangedUtc = utcNow;
        FailedSignInCount = 0;
        SignInLockoutEndsUtc = null;
        MarkAsModified(utcNow);
    }

    internal void ChangePasswordHash(string passwordHash, DateTimeOffset utcNow)
    {
        if (!IsActive)
        {
            throw new DomainException(OperationsIdentityErrors.AccountNotActive);
        }

        SetPasswordHash(passwordHash);
        FailedSignInCount = 0;
        SignInLockoutEndsUtc = null;
        MarkAsModified(utcNow);
    }

    internal void RecordFailedSignIn(DateTimeOffset utcNow)
    {
        FailedSignInCount++;

        if (FailedSignInCount >= OperationsIdentityLimits.MaxFailedSignIns)
        {
            SignInLockoutEndsUtc = utcNow.Add(OperationsIdentityLimits.SignInLockoutDuration);
            FailedSignInCount = 0;
        }

        MarkAsModified(utcNow);
    }

    internal void RecordSuccessfulSignIn(DateTimeOffset utcNow)
    {
        if (!IsSignInAllowed(utcNow))
        {
            throw new DomainException(OperationsIdentityErrors.AccountNotActive);
        }

        FailedSignInCount = 0;
        SignInLockoutEndsUtc = null;
        LastSignInUtc = utcNow;
        MarkAsModified(utcNow);
    }

    private void SetPasswordHash(string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(passwordHash) || passwordHash.Length > OperationsIdentityLimits.PasswordHashMaxLength)
        {
            throw new DomainException(OperationsIdentityErrors.PasswordHashRequired);
        }

        PasswordHash = passwordHash;
    }
}
