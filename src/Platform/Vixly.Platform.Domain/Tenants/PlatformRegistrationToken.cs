using Vixly.Platform.Domain.Identity;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Domain.Tenants;

// =========================================================
// FILE: PlatformRegistrationToken.cs
//
// PURPOSE:
// A single-use email-verification link of a registration.
//
// RESPONSIBILITIES:
// - Hold the SHA-256 hash of the link token, its expiry and use
// - Consume it once, before it expires
//
// IMPORTANT NOTES:
// - Only the hash is stored (V3 stored the token in clear)
// - Expired, used, revoked or unknown links give one error code,
//   so a caller learns nothing from the difference
// - Sending a new link revokes the older ones (use case)
// =========================================================
public sealed class PlatformRegistrationToken : AuditableEntity
{
    private PlatformRegistrationToken()
    {
        TokenHash = string.Empty;
    }

    private PlatformRegistrationToken(Guid registrationId, string tokenHash, DateTimeOffset createdUtc)
        : base(createdUtc)
    {
        PlatformTenantRegistrationId = registrationId;
        TokenHash = tokenHash;
        ExpiresUtc = createdUtc.Add(PlatformTenantLimits.VerificationLinkLifetime);
    }

    public Guid PlatformTenantRegistrationId { get; private set; }

    public string TokenHash { get; private set; }

    public DateTimeOffset ExpiresUtc { get; private set; }

    public DateTimeOffset? UsedUtc { get; private set; }

    public DateTimeOffset? RevokedUtc { get; private set; }

    // =====================================================
    // METHOD: ISSUE
    // =====================================================
    public static PlatformRegistrationToken Issue(Guid registrationId, string tokenHash, DateTimeOffset utcNow)
    {
        if (registrationId == Guid.Empty)
        {
            throw new DomainException(PlatformRegistrationErrors.NotFound);
        }

        return new PlatformRegistrationToken(registrationId, PlatformTokenHash.Require(tokenHash), utcNow);
    }

    // =====================================================
    // METHOD: CONSUME
    // =====================================================
    public void Consume(DateTimeOffset utcNow)
    {
        if (UsedUtc is not null || RevokedUtc is not null || utcNow >= ExpiresUtc)
        {
            throw new DomainException(PlatformRegistrationErrors.LinkInvalid);
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
