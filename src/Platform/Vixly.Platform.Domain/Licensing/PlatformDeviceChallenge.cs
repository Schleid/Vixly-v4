using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Domain.Licensing;

// =========================================================
// FILE: PlatformDeviceChallenge.cs
//
// PURPOSE:
// A single-use challenge a key-pair device signs to prove itself (D-L5).
//
// RESPONSIBILITIES:
// - Hold the hash of a random challenge, its device and expiry
// - Be used once
//
// IMPORTANT NOTES:
// - Only the hash is stored; the challenge goes to the device once
// =========================================================
public sealed class PlatformDeviceChallenge : AuditableEntity
{
    private PlatformDeviceChallenge()
    {
        ChallengeHash = string.Empty;
    }

    private PlatformDeviceChallenge(Guid deviceId, string challengeHash, DateTimeOffset utcNow)
        : base(utcNow)
    {
        PlatformDeviceId = deviceId;
        ChallengeHash = challengeHash;
        ExpiresUtc = utcNow.Add(PlatformLicensingLimits.ChallengeLifetime);
    }

    public Guid PlatformDeviceId { get; private set; }

    public string ChallengeHash { get; private set; }

    public DateTimeOffset ExpiresUtc { get; private set; }

    public DateTimeOffset? UsedUtc { get; private set; }

    public static PlatformDeviceChallenge Create(Guid deviceId, string? challengeHash, DateTimeOffset utcNow)
    {
        if (deviceId == Guid.Empty || challengeHash is null || challengeHash.Length != PlatformLicensingLimits.HashLength)
        {
            throw new DomainException(PlatformLicensingErrors.ProofInvalid);
        }

        return new PlatformDeviceChallenge(deviceId, challengeHash, utcNow);
    }

    public bool Use(DateTimeOffset utcNow)
    {
        if (UsedUtc is not null || utcNow >= ExpiresUtc)
        {
            return false;
        }

        UsedUtc = utcNow;
        MarkAsModified(utcNow);
        return true;
    }
}
