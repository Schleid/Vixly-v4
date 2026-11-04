namespace Vixly.Operations.Application.UseCases.Staff.ResetOperationsStaffSecondFactor;

// =========================================================
// FILE: ResetOperationsStaffSecondFactorRequest.cs
//
// PURPOSE:
// Removes a staff member's authenticator app and recovery codes so they enrol again (Slice 1D, D-O14).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class ResetOperationsStaffSecondFactorRequest
{
    public Guid UserId { get; init; }
}
