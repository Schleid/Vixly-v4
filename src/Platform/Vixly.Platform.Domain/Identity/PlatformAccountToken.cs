using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Domain.Identity;

// =========================================================
// FILE: PlatformAccountToken.cs
//
// PURPOSE:
// A single-use link sent by email to accept an invitation or
// reset a password.
//
// RESPONSIBILITIES:
// - Hold the token hash, its purpose and its expiry
//   (72 hours for invitations, 60 minutes for resets)
// - Be consumed once, before expiry
//
// IMPORTANT NOTES:
// - Aggregate root
// - Only the SHA-256 hash of the token is stored
// - Issuing a new token of the same purpose does not delete
//   older ones; the use case revokes them so only the newest
//   link works
// =========================================================
public sealed class PlatformAccountToken : AuditableEntity
{
    private PlatformAccountToken()
    {
        TokenHash = string.Empty;
    }

    private PlatformAccountToken(
        Guid platformUserId,
        PlatformAccountTokenPurpose purpose,
        string tokenHash,
        DateTimeOffset createdUtc)
        : base(createdUtc)
    {
        PlatformUserId = platformUserId;
        Purpose = purpose;
        TokenHash = tokenHash;
        ExpiresUtc = createdUtc.Add(purpose == PlatformAccountTokenPurpose.Invitation
            ? PlatformIdentityLimits.InvitationLifetime
            : PlatformIdentityLimits.PasswordResetLifetime);
    }

    public Guid PlatformUserId { get; private set; }

    public PlatformAccountTokenPurpose Purpose { get; private set; }

    public string TokenHash { get; private set; }

    public DateTimeOffset ExpiresUtc { get; private set; }

    public DateTimeOffset? UsedUtc { get; private set; }

    public DateTimeOffset? RevokedUtc { get; private set; }

    // =====================================================
    // METHOD: ISSUE
    // =====================================================
    public static PlatformAccountToken Issue(
        Guid platformUserId,
        PlatformAccountTokenPurpose purpose,
        string tokenHash,
        DateTimeOffset utcNow)
    {
        if (platformUserId == Guid.Empty)
        {
            throw new DomainException(PlatformIdentityErrors.UserRequired);
        }

        if (!Enum.IsDefined(purpose))
        {
            throw new DomainException(PlatformIdentityErrors.TokenPurposeInvalid);
        }

        return new PlatformAccountToken(platformUserId, purpose, PlatformTokenHash.Require(tokenHash), utcNow);
    }

    // =====================================================
    // METHOD: CONSUME
    // =====================================================
    public void Consume(PlatformAccountTokenPurpose purpose, DateTimeOffset utcNow)
    {
        if (purpose != Purpose)
        {
            throw new DomainException(PlatformIdentityErrors.TokenPurposeInvalid);
        }

        if (UsedUtc is not null || RevokedUtc is not null)
        {
            throw new DomainException(PlatformIdentityErrors.TokenAlreadyUsed);
        }

        if (utcNow >= ExpiresUtc)
        {
            throw new DomainException(PlatformIdentityErrors.TokenExpired);
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
