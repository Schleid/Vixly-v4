using Vixly.Operations.Domain.Access;

namespace Vixly.Operations.Application.Staff;

// =========================================================
// FILE: OperationsStaffGrant.cs
//
// PURPOSE:
// A role a staff member holds, and where (Slice 1D).
//
// RESPONSIBILITIES:
// - Carry the grant, role and scope for display
//
// IMPORTANT NOTES:
// - ScopeName is the branch or warehouse name; empty for the whole tenant
// =========================================================
public sealed record OperationsStaffGrant(Guid GrantId, Guid RoleId, string RoleName, bool IsAdministratorRole, OperationsRoleScope Scope, Guid? ScopeId, string? ScopeName, DateTimeOffset GrantedUtc);
