using System.Globalization;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Domain.Licensing;

// =========================================================
// FILE: PlatformTenantLicensing.cs
//
// PURPOSE:
// A tenant's seats in use and its device numbers (D-L2).
//
// RESPONSIBILITIES:
// - Take a seat only when one is free; give one back
// - Hand out the next device number
//
// IMPORTANT NOTES:
// - One row per tenant with a row version: two hosts can never hand out the same seat
// - Seats bought come from the subscription (Slice 3)
// =========================================================
public sealed class PlatformTenantLicensing : AuditableEntity
{
    private PlatformTenantLicensing()
    {
    }

    private PlatformTenantLicensing(Guid tenantId, DateTimeOffset utcNow)
        : base(utcNow)
    {
        PlatformTenantId = tenantId;
    }

    public Guid PlatformTenantId { get; private set; }

    public int SeatsInUse { get; private set; }

    public int LastDeviceNumber { get; private set; }

    public static PlatformTenantLicensing Create(Guid tenantId, DateTimeOffset utcNow)
    {
        if (tenantId == Guid.Empty)
        {
            throw new DomainException(PlatformLicensingErrors.DeviceNotFound);
        }

        return new PlatformTenantLicensing(tenantId, utcNow);
    }

    public string NextNumber(DateTimeOffset utcNow)
    {
        LastDeviceNumber++;
        MarkAsModified(utcNow);
        return "D-" + LastDeviceNumber.ToString("D6", CultureInfo.InvariantCulture);
    }

    public void TakeSeat(int seats, DateTimeOffset utcNow)
    {
        if (SeatsInUse >= seats)
        {
            throw new DomainException(PlatformLicensingErrors.NoFreeSeat(SeatsInUse, seats));
        }

        SeatsInUse++;
        MarkAsModified(utcNow);
    }

    public void ReturnSeat(DateTimeOffset utcNow)
    {
        if (SeatsInUse > 0)
        {
            SeatsInUse--;
            MarkAsModified(utcNow);
        }
    }

    public void Apply(PlatformDeviceChange change, int seats, DateTimeOffset utcNow)
    {
        ArgumentNullException.ThrowIfNull(change);

        if (change.SeatTaken)
        {
            TakeSeat(seats, utcNow);
        }

        if (change.SeatFreed)
        {
            ReturnSeat(utcNow);
        }
    }
}
