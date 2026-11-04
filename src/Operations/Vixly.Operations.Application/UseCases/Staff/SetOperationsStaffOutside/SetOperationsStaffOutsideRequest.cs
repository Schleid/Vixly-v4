namespace Vixly.Operations.Application.UseCases.Staff.SetOperationsStaffOutside;

// =========================================================
// FILE: SetOperationsStaffOutsideRequest.cs
//
// PURPOSE:
// Makes a staff account an outside one (a call centre's agent), with its end date and working hours, or an ordinary one again (owner, 2026-10-29, S-5).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class SetOperationsStaffOutsideRequest
{
    public Guid UserId { get; init; }

    public bool Outside { get; init; }

    public DateOnly? EndsOn { get; init; }

    public int Days { get; init; }

    public int? FromMinute { get; init; }

    public int? ToMinute { get; init; }
}
