namespace Vixly.Operations.Domain.Identity;

// =========================================================
// FILE: OperationsSessionEndReason.cs
//
// PURPOSE:
// Why a staff session ended.
//
// RESPONSIBILITIES:
// - Name the reasons
//
// IMPORTANT NOTES:
// - Stored as int; never renumber
// =========================================================
public enum OperationsSessionEndReason
{
    SignedOut = 0,
    EndedByUser = 1,
    EndedByAdministrator = 2,
    AccountRestricted = 3,
    IdleTimeout = 4,
    AbsoluteLifetime = 5,
    PasswordChanged = 6,
    SecondFactorReset = 7,
    DeviceRemoved = 8,

    // Owner, 2026-10-21: the device is used from another country than the one it was registered in, and this session was not confirmed there.
    CountryChanged = 9
}
