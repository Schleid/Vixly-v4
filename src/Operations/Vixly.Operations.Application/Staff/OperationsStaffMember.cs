using Vixly.Operations.Domain.Identity;

namespace Vixly.Operations.Application.Staff;

// =========================================================
// FILE: OperationsStaffMember.cs
//
// PURPOSE:
// A staff member as the Staff screens show them (Slice 1D).
//
// RESPONSIBILITIES:
// - Carry name, email, status, owner and administrator flags, second factor and roles
//
// IMPORTANT NOTES:
// - Read model; built by the staff use cases
// =========================================================
public sealed record OperationsStaffMember(
    Guid UserId,
    string FullName,
    string Email,
    OperationsUserAccountStatus Status,
    string? StatusReason,
    bool IsOwner,
    bool IsAdministrator,
    bool HasSecondFactor,
    DateTimeOffset? LastSignInUtc,
    IReadOnlyList<OperationsStaffGrant> Grants);
