namespace Vixly.Platform.Domain.Licensing;

// =========================================================
// FILE: PlatformDeviceReason.cs
//
// PURPOSE:
// Why a device was released, revoked or denied.
//
// RESPONSIBILITIES:
// - Name the why a device was released, revoked or denied
//
// IMPORTANT NOTES:
// - The first five are chosen by a tenant admin; the rest are set by Platform or Vixly staff
// - Values are stored; never renumber
// =========================================================
public enum PlatformDeviceReason
{
    Replaced = 1,
    LostOrStolen = 2,
    Broken = 3,
    StaffLeft = 4,
    Other = 5,
    CredentialReused = 6,
    SubscriptionEnded = 7,
    TenantClosed = 8,
    Security = 9,
    ReplacedByRecovery = 10,
    NotRecognized = 11
}
