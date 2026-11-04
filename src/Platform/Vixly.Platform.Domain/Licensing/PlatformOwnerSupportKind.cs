namespace Vixly.Platform.Domain.Licensing;

// =========================================================
// FILE: PlatformOwnerSupportKind.cs
//
// PURPOSE:
// What Vixly support does for an Owner who lost access (D-O13).
//
// RESPONSIBILITIES:
// - Name the two support actions
//
// IMPORTANT NOTES:
// - Values are stored; never renumber
// =========================================================
public enum PlatformOwnerSupportKind
{
    DeviceRecovery = 1,
    SecondFactorReset = 2
}
