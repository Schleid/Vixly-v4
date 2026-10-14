namespace Vixly.Platform.Application.Queries;

// =========================================================
// FILE: PlatformDepartmentListItem.cs
//
// PURPOSE:
// One row of the departments list.
//
// RESPONSIBILITIES:
// - Carry the name, description, state and number of members
//
// IMPORTANT NOTES:
// - A department name is operational data, not translated (L-5)
// =========================================================
public sealed record PlatformDepartmentListItem(
    Guid PlatformDepartmentId,
    string Name,
    string? Description,
    bool IsActive,
    int MemberCount);
