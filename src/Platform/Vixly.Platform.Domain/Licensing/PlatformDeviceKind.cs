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
    Other = 5,

    // Owner, 2026-10-21: the type given at registration (desktop, laptop or mobile; mobile is Phone).
    Desktop = 6,
    Laptop = 7
}
