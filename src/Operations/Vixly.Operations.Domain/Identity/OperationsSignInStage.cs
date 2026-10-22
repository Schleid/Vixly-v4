namespace Vixly.Operations.Domain.Identity;

// =========================================================
// FILE: OperationsSignInStage.cs
//
// PURPOSE:
// What a pending sign-in waits for.
//
// RESPONSIBILITIES:
// - Name the stages
//
// IMPORTANT NOTES:
// - Stored as int
// =========================================================
public enum OperationsSignInStage
{
    SecondFactor = 0,
    Enrolment = 1
}
