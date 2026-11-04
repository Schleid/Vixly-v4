namespace Vixly.Platform.Application.Licensing;

// =========================================================
// FILE: PlatformLicensingNoticeKind.cs
//
// PURPOSE:
// The licensing emails Platform sends to a tenant owner (D-L16).
//
// RESPONSIBILITIES:
// - Name each email
//
// IMPORTANT NOTES:
// - Part of message payloads; never renumber
// =========================================================
public enum PlatformLicensingNoticeKind
{
    FirstDeviceLink = 1,
    RecoveryLink = 2,
    DeviceAdded = 3,
    DeviceReplaced = 4,
    ReleaseDecided = 5,
    DevicesRevoked = 6,
    CredentialReused = 7,

    // The last email of the onboarding: the tenant is active (owner, 2026-10-18).
    Welcome = 8,

    // Vixly support started a recovery or a second-factor reset (Owner and Billing, D-O13).
    OwnerSupportStarted = 9,

    SecondFactorResetLink = 10,

    SecondFactorResetDone = 11
}
