namespace Vixly.Platform.Domain.Identity;

// =========================================================
// FILE: PlatformSignInStage.cs
//
// PURPOSE:
// What a pending sign-in still needs.
//
// RESPONSIBILITIES:
// - Separate "prove your second factor" from "enrol your
//   first second factor"
//
// IMPORTANT NOTES:
// - Persisted as numbers; never renumber
// =========================================================
public enum PlatformSignInStage
{
    SecondFactor = 0,
    Enrolment = 1
}
