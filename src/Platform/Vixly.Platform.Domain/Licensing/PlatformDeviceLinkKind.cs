namespace Vixly.Platform.Domain.Licensing;

// =========================================================
// FILE: PlatformDeviceLinkKind.cs
//
// PURPOSE:
// What a device link enrols (D-L7, D-L8).
//
// RESPONSIBILITIES:
// - Name the what a device link enrols (D-L7, D-L8)
//
// IMPORTANT NOTES:
// - Values are stored; never renumber
// =========================================================
public enum PlatformDeviceLinkKind
{
    FirstDevice = 1,
    OwnerRecovery = 2
}
