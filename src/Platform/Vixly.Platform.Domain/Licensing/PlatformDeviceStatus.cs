namespace Vixly.Platform.Domain.Licensing;

// =========================================================
// FILE: PlatformDeviceStatus.cs
//
// PURPOSE:
// Where a device stands (D-L3).
//
// RESPONSIBILITIES:
// - Name the where a device stands (D-L3)
//
// IMPORTANT NOTES:
// - Released, Revoked, Denied and Expired are final
// - Active and Release pending hold a seat; Revoked holds one only while its release waits for review
// - Values are stored; never renumber
// =========================================================
public enum PlatformDeviceStatus
{
    Pending = 1,
    Active = 2,
    ReleasePending = 3,
    Released = 4,
    Revoked = 5,
    Denied = 6,
    Expired = 7
}
