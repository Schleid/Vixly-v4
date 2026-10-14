namespace Vixly.Platform.Domain.Identity;

// =========================================================
// FILE: PlatformSessionEndReason.cs
//
// PURPOSE:
// Why a staff session ended (ADR-0006 section 6).
//
// RESPONSIBILITIES:
// - Name every way a session can end, for the session list
//   and the audit log
//
// IMPORTANT NOTES:
// - Persisted as numbers; never renumber
// =========================================================
public enum PlatformSessionEndReason
{
    SignedOut = 0,
    EndedByUser = 1,
    EndedByAdministrator = 2,
    AccountRestricted = 3,
    IdleTimeout = 4,
    AbsoluteLifetime = 5,
    PasswordChanged = 6,
    SecondFactorReset = 7,
    StepUpFailed = 8
}
