namespace Vixly.Platform.Domain.Licensing;

// =========================================================
// FILE: PlatformDeviceSecretCheck.cs
//
// PURPOSE:
// The result of checking a rotating secret (D-L5).
//
// RESPONSIBILITIES:
// - Name the the result of checking a rotating secret (D-L5)
//
// IMPORTANT NOTES:
// - Reused: an old secret after its grace period; the device must be revoked
// - Computed, never stored
// =========================================================
public enum PlatformDeviceSecretCheck
{
    Invalid = 0,
    Valid = 1,
    ValidRotate = 2,
    Reused = 3
}
