namespace Vixly.Platform.Domain.Licensing;

// =========================================================
// FILE: PlatformDeviceKind.cs
//
// PURPOSE:
// The kind of device a tenant uses Vixly on.
//
// RESPONSIBILITIES:
// - Name the the kind of device a tenant uses Vixly on
//
// IMPORTANT NOTES:
// - Values are stored; never renumber
// =========================================================
public enum PlatformDeviceKind
{
    Pc = 1,
    Phone = 2,
    Scanner = 3,
    Tablet = 4,
    Other = 5
}
