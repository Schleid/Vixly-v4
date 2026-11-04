using Vixly.Operations.Domain.Common;

namespace Vixly.Operations.Domain.Identity;

// =========================================================
// FILE: OperationsDevicePerson.cs
//
// PURPOSE:
// Who signed in on a licensed device, and when they last confirmed with their authenticator or password there
// (Slice 12A, owner H-8: the authenticator is asked once a day).
//
// RESPONSIBILITIES:
// - Remember the last sign-in of a person on a device (the PIN screen lists them)
// - Remember the last full sign-in, to know whether today's was done
//
// IMPORTANT NOTES:
// - The device lives in Platform: its id only
// - One row per device and person (unique index)
// - Bound to its tenant by OperationsEntity
// =========================================================
public sealed class OperationsDevicePerson : OperationsEntity
{
    private OperationsDevicePerson()
    {
    }

    private OperationsDevicePerson(Guid tenantId, Guid deviceId, Guid userId, DateTimeOffset utcNow)
        : base(tenantId, utcNow)
    {
        DeviceId = deviceId;
        UserId = userId;
        LastSignInUtc = utcNow;
    }

    public Guid DeviceId { get; private set; }

    public Guid UserId { get; private set; }

    public DateTimeOffset LastSignInUtc { get; private set; }

    // The last sign-in with the password and, when the person has one, the authenticator.
    public DateTimeOffset? LastFullSignInUtc { get; private set; }

    public static OperationsDevicePerson First(Guid tenantId, Guid deviceId, Guid userId, DateTimeOffset utcNow)
    {
        if (deviceId == Guid.Empty || userId == Guid.Empty)
        {
            throw new Vixly.Shared.Kernel.Domain.DomainException(OperationsIdentityErrors.UserRequired);
        }

        return new OperationsDevicePerson(tenantId, deviceId, userId, utcNow);
    }

    public void SignedIn(bool full, DateTimeOffset utcNow)
    {
        LastSignInUtc = utcNow;

        if (full)
        {
            LastFullSignInUtc = utcNow;
        }

        MarkAsModified(utcNow);
    }

    // Was the full sign-in done today, in the company's time zone?
    public bool FullSignInOn(DateOnly today, string? timeZone) =>
        LastFullSignInUtc is { } full && DateOnly.FromDateTime(Vixly.Operations.Domain.Locations.OperationsTimeZones.Local(full, timeZone)) == today;
}
