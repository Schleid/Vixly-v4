namespace Vixly.Platform.Domain.Identity;

// =========================================================
// FILE: PlatformUserAccountStatus.cs
//
// PURPOSE:
// The lifecycle state of a staff account (ADR-0006 section 7).
//
// RESPONSIBILITIES:
// - Name the states a staff account can be in
//
// IMPORTANT NOTES:
// - Persisted as numbers; never renumber
// - Locked is an administrator action. The temporary lockout
//   after failed sign-ins is separate and expires by itself
// - Deactivated is final for day-to-day use; reactivation is
//   still possible so history and audit stay attached
// =========================================================
public enum PlatformUserAccountStatus
{
    Invited = 0,
    Active = 1,
    Suspended = 2,
    Locked = 3,
    Deactivated = 4
}
