namespace Vixly.Platform.Application.Queries;

// =========================================================
// FILE: PlatformUserDepartmentItem.cs
//
// PURPOSE:
// One department a staff user belongs to.
//
// RESPONSIBILITIES:
// - Carry the department, its state, and whether it is primary
//
// IMPORTANT NOTES:
// - Primary first, then by name
// =========================================================
public sealed record PlatformUserDepartmentItem(
    Guid PlatformDepartmentId,
    string Name,
    bool DepartmentIsActive,
    bool IsPrimary);
