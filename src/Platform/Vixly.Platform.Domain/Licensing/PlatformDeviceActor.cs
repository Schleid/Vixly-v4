using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Domain.Licensing;

// =========================================================
// FILE: PlatformDeviceActor.cs
//
// PURPOSE:
// Who did something to a device: a tenant user (reference and name from Operations), a Vixly staff member, or the system.
//
// RESPONSIBILITIES:
// - Carry the actor for history
// - Check the sizes of what Operations sends
//
// IMPORTANT NOTES:
// - Platform holds no tenant users (ADR-0003): only a reference and a display name are kept
// =========================================================
public sealed record PlatformDeviceActor
{
    private PlatformDeviceActor(string? reference, string? name, Guid? staffUserId)
    {
        Reference = reference;
        Name = name;
        StaffUserId = staffUserId;
    }

    public string? Reference { get; }

    public string? Name { get; }

    public Guid? StaffUserId { get; }

    public static PlatformDeviceActor System { get; } = new(null, null, null);

    public static PlatformDeviceActor Tenant(string? reference, string? name)
    {
        string refText = reference?.Trim() ?? string.Empty;
        string nameText = name?.Trim() ?? string.Empty;

        if (refText.Length == 0 || refText.Length > PlatformLicensingLimits.ActorReferenceMaxLength
            || nameText.Length == 0 || nameText.Length > PlatformLicensingLimits.ActorNameMaxLength)
        {
            throw new DomainException(PlatformLicensingErrors.ActorRequired);
        }

        return new PlatformDeviceActor(refText, nameText, null);
    }

    public static PlatformDeviceActor Staff(Guid userId)
    {
        if (userId == Guid.Empty)
        {
            throw new DomainException(PlatformLicensingErrors.ActorRequired);
        }

        return new PlatformDeviceActor(null, null, userId);
    }
}
