namespace Vixly.Platform.Domain.Identity;

// =========================================================
// FILE: PlatformPasskeyCeremonyPurpose.cs
//
// PURPOSE:
// What a passkey ceremony is for (ADR-0006 section 2).
//
// RESPONSIBILITIES:
// - Name the three ways a passkey is used
//
// IMPORTANT NOTES:
// - Persisted as numbers; never renumber
// =========================================================
public enum PlatformPasskeyCeremonyPurpose
{
    Registration = 0,
    SignIn = 1,
    StepUp = 2
}
