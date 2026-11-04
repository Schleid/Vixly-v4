using Vixly.Operations.Domain.Common;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Operations.Domain.Identity;

// =========================================================
// FILE: OperationsAccountToken.cs
//
// PURPOSE:
// A single-use emailed link: invitation (72 hours) or password reset (60 minutes).
//
// RESPONSIBILITIES:
// - Issue with a hashed token
// - Consume once before it expires
// - Revoke when replaced
//
// IMPORTANT NOTES:
// - Only the hash is stored
// =========================================================
public sealed class OperationsAccountToken : OperationsEntity
{
    private OperationsAccountToken()
    {
        TokenHash = string.Empty;
    }

    private OperationsAccountToken(Guid tenantId, Guid userId, OperationsAccountTokenPurpose purpose, string tokenHash, DateTimeOffset createdUtc)
        : base(tenantId, createdUtc)
    {
        UserId = userId;
        Purpose = purpose;
        TokenHash = tokenHash;
        ExpiresUtc = createdUtc.Add(purpose == OperationsAccountTokenPurpose.Invitation
            ? OperationsIdentityLimits.InvitationLifetime
            : OperationsIdentityLimits.PasswordResetLifetime);
    }

    public Guid UserId { get; private set; }

    public OperationsAccountTokenPurpose Purpose { get; private set; }

    public string TokenHash { get; private set; }

    public DateTimeOffset ExpiresUtc { get; private set; }

    public DateTimeOffset? UsedUtc { get; private set; }

    public DateTimeOffset? RevokedUtc { get; private set; }

    public static OperationsAccountToken Issue(Guid tenantId, Guid userId, OperationsAccountTokenPurpose purpose, string tokenHash, DateTimeOffset utcNow)
    {
        if (userId == Guid.Empty)
        {
            throw new DomainException(OperationsIdentityErrors.UserRequired);
        }

        if (!Enum.IsDefined(purpose))
        {
            throw new DomainException(OperationsIdentityErrors.TokenPurposeInvalid);
        }

        return new OperationsAccountToken(tenantId, userId, purpose, OperationsTokenHash.Require(tokenHash), utcNow);
    }

    public void Consume(OperationsAccountTokenPurpose purpose, DateTimeOffset utcNow)
    {
        if (purpose != Purpose)
        {
            throw new DomainException(OperationsIdentityErrors.TokenPurposeInvalid);
        }

        if (UsedUtc is not null || RevokedUtc is not null)
        {
            throw new DomainException(OperationsIdentityErrors.TokenAlreadyUsed);
        }

        if (utcNow >= ExpiresUtc)
        {
            throw new DomainException(OperationsIdentityErrors.TokenExpired);
        }

        UsedUtc = utcNow;
        MarkAsModified(utcNow);
    }

    public void Revoke(DateTimeOffset utcNow)
    {
        if (UsedUtc is not null || RevokedUtc is not null)
        {
            return;
        }

        RevokedUtc = utcNow;
        MarkAsModified(utcNow);
    }
}
