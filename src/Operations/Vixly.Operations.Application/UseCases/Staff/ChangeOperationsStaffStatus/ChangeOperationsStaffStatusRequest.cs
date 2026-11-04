using Vixly.Operations.Domain.Identity;
namespace Vixly.Operations.Application.UseCases.Staff.ChangeOperationsStaffStatus;

// =========================================================
// FILE: ChangeOperationsStaffStatusRequest.cs
//
// PURPOSE:
// Suspends, locks, deactivates or reactivates a staff member, with a reason (Slice 1D).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class ChangeOperationsStaffStatusRequest
{
    public Guid UserId { get; init; }

    public OperationsUserAccountStatus Status { get; init; }

    public string Reason { get; init; } = string.Empty;
}
