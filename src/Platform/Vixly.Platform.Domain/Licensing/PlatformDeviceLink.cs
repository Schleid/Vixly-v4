using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Domain.Licensing;

// =========================================================
// FILE: PlatformDeviceLink.cs
//
// PURPOSE:
// A single-use link that enrols a first device or an owner's recovery device (D-L7, D-L8).
//
// RESPONSIBILITIES:
// - Hold the hash of a random token, what it enrols and until when
// - Count tries; be used or voided once
//
// IMPORTANT NOTES:
// - Only the hash is stored; the token is only in the email, after "#"
// - A newer link of the same kind voids older ones
// =========================================================
public sealed class PlatformDeviceLink : AuditableEntity
{
    private PlatformDeviceLink()
    {
        TokenHash = string.Empty;
    }

    private PlatformDeviceLink(PlatformDeviceLinkKind kind, Guid tenantId, Guid? registrationId, string tokenHash, DateTimeOffset utcNow)
        : base(utcNow)
    {
        Kind = kind;
        PlatformTenantId = tenantId;
        PlatformTenantRegistrationId = registrationId;
        TokenHash = tokenHash;
        ExpiresUtc = utcNow.Add(kind == PlatformDeviceLinkKind.FirstDevice ? PlatformLicensingLimits.FirstDeviceLinkLifetime : PlatformLicensingLimits.RecoveryLinkLifetime);
    }

    public PlatformDeviceLinkKind Kind { get; private set; }

    public Guid PlatformTenantId { get; private set; }

    public Guid? PlatformTenantRegistrationId { get; private set; }

    public string TokenHash { get; private set; }

    public DateTimeOffset ExpiresUtc { get; private set; }

    public DateTimeOffset? UsedUtc { get; private set; }

    public DateTimeOffset? VoidedUtc { get; private set; }

    public int Tries { get; private set; }

    public static PlatformDeviceLink Create(PlatformDeviceLinkKind kind, Guid tenantId, Guid? registrationId, string? tokenHash, DateTimeOffset utcNow)
    {
        if (!Enum.IsDefined(kind) || tenantId == Guid.Empty || tokenHash is null || tokenHash.Length != PlatformLicensingLimits.HashLength)
        {
            throw new DomainException(PlatformLicensingErrors.LinkInvalid);
        }

        return new PlatformDeviceLink(kind, tenantId, registrationId, tokenHash, utcNow);
    }

    public bool IsUsableAt(DateTimeOffset utcNow) => UsedUtc is null && VoidedUtc is null && utcNow < ExpiresUtc && Tries < PlatformLicensingLimits.LinkMaxTries;

    // Counts an attempt; a link that was tried too often stops working.
    public bool Try(DateTimeOffset utcNow)
    {
        if (!IsUsableAt(utcNow))
        {
            return false;
        }

        Tries++;
        MarkAsModified(utcNow);
        return true;
    }

    public void MarkUsed(DateTimeOffset utcNow)
    {
        UsedUtc = utcNow;
        MarkAsModified(utcNow);
    }

    public void Void(DateTimeOffset utcNow)
    {
        if (UsedUtc is null && VoidedUtc is null)
        {
            VoidedUtc = utcNow;
            MarkAsModified(utcNow);
        }
    }
}
