namespace Vixly.Operations.Application.Staff;

// =========================================================
// FILE: OperationsRoleSummary.cs
//
// PURPOSE:
// A role as the Roles screens show it (Slice 1D).
//
// RESPONSIBILITIES:
// - Carry name, kind, state, permissions and how many people hold it
//
// IMPORTANT NOTES:
// - Read model
// =========================================================
public sealed record OperationsRoleSummary(Guid RoleId, string Name, bool IsAdministrator, bool IsActive, IReadOnlyList<string> Permissions, int Holders);
